// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 01-01-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-30-2022
// ***********************************************************************
// <copyright file="TpPreviewUtility.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Allows Painter to handle previews properly for various Objects. </summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TilePlus.Editor.Painter;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using static TilePlus.Editor.Painter.TpPainterClipboard;
using static UnityEngine.ScriptableObject;
using static TilePlus.TpLib;
using Object = UnityEngine.Object;

#nullable enable
//[assembly: InternalsVisibleTo("TilePlusPainter")]
namespace TilePlus.Editor
{
    /// <summary>
    /// Utilities for handling tile previews, including control over TpPainterPlugins
    /// for TileBase tiles (those without sprite properties)
    /// </summary>
    [InitializeOnLoad]
    internal static class TpPreviewUtility
    {
        #region constants
        
        /// <summary>
        /// init value for proxy tile pool size
        /// </summary>
        private const int ProxyTileListInitialSize = 8;
        

        #endregion
        #region ctor

        /// <summary>
        /// Ctor, called implicity via InitializeOnLoad
        /// this allows TilePlus tile editors (embedded in the tile code)
        /// to use this normally editor-only method. 
        /// </summary>
        static TpPreviewUtility()
        {
            TpEditorBridge.BridgeGameObjPreview       = PreviewGameObject;
            TpEditorBridge.BridgeIsPreviewActiveForId = IsPreviewActiveForId;
            TpEditorBridge.BridgeIsPreviewActive      = ()=>PreviewActive;
            TpEditorBridge.BridgeClearPreview         = ClearPreview;
            TpEditorBridge.BridgePreviewTileFab             = PreviewTileFab;
            
            Selection.selectionChanged += () =>
                                          {
                                              if (Application.isPlaying)
                                                  return;
                                              ClearPreview();
                                          };
           
            EditorApplication.playModeStateChanged += x =>
                                                      {
                                                          if (x is not (PlayModeStateChange.ExitingEditMode
                                                                        or PlayModeStateChange.ExitingPlayMode))
                                                              return;
                                                          ClearPreview();
                                                      };
        }

        #endregion

        #region privateFields

        /// <summary>
        /// Index into the proxy pool DONT ACCESS DIRECTLY this is a backing value for a property.
        /// </summary>
        private static int s_ProxyIndexBacking;

        /// <summary>
        /// Maximum value of proxy index.
        /// </summary>
        private static int s_MaxProxyIndex;
        
        /// <summary>
        /// A cached placeholder tile. This is used when a TilePlus tile's
        /// sprite is invisible due to the  sprite mode being ClearInSceneView or
        /// ClearInSceneViewAndOnStart. It's just so that the preview isn't blank.
        /// </summary>
        private static Tile? s_PlaceholderTile;

        /// <summary>
        /// A pool of Proxy tiles. These are used to display sprites for prefabs.
        /// </summary>
        private static List<ProxyTile>? s_ProxyTiles;

        /// <summary>
        /// Initialized with a constant 1-position bounds int at 0, see SingleTilePreview
        /// </summary>
        private static BoundsInt? s_SinglePositionBoundsInt;
        
        /// <summary>
        /// A List of editor preview tilemaps
        /// </summary>
        private static readonly List<Tilemap> s_EditorPreviewTilemaps = new();

        /// <summary>
        /// A mapping from tile Types to plugins.
        /// </summary>
        private static readonly Dictionary<Type, TpPainterPluginBase?> s_TileTypeToPluginMap = new();

        private static bool s_Initialized;

        //used to detect empty textures
        private static Hash128? s_Hash128;   

        #endregion

        #region publicFieldsProperties
        /// <summary>
        /// How many plugins are there?
        /// </summary>
        internal static int PluginCount => s_TileTypeToPluginMap.Count;
        
        /// <summary>
        /// Get a list of all plugins
        /// </summary>
        internal static List<TpPainterPluginBase?> AllPlugins => s_TileTypeToPluginMap.Values.ToList();
        
        /// <summary>
        /// is the current preview the placeholder tile? This is used when a TilePlus tile's
        /// sprite is invisible due to the sprite mode being ClearInSceneView or
        /// ClearInSceneViewAndOnStart. It's just so that the preview isn't blank.
        /// </summary>
        public static bool PreviewIsPlaceholderTile { get; private set; }

        /// <summary>
        /// is the current preview the Proxy tile (used when Prefabs are being painted)
        /// </summary>
        public static bool PreviewIsProxyTile => ProxyIndex > 0;

        /// <summary>
        /// Preview index
        /// </summary>
        public static int MaxProxyIndex => s_MaxProxyIndex;
        
        /// <summary>
        /// The base position (ie the mouse grid position) where the preview is
        /// created.
        /// </summary>
        private static Vector3Int CurrentPreviewPosition { get; set; }
        
        /// <summary>
        /// Get the placeholder tile. Used when a TilePlus tile
        /// has a hidden sprite
        /// </summary>
        private static Tile PlaceHolderTile 
        {
            get
            {
                if (s_PlaceholderTile != null)
                    return s_PlaceholderTile;
                s_PlaceholderTile           = CreateInstance<Tile>();
                s_PlaceholderTile.hideFlags = HideFlags.HideAndDontSave;
                var tex = TpIconLib.FindIcon(TpIconType.UnityToolbarMinusIcon);
                s_PlaceholderTile.sprite = TpImageLib.SpriteFromTexture(tex);
                return s_PlaceholderTile;
            }
        }

        /// <summary>
        /// The IID of the last UnityEngine.Object which created a preview.
        /// Returns 0 if last preview was from TpPainterSceneView
        /// </summary>
        private static int PreviewId { get; set; }
        
        /// <summary>
        /// Get the Prefab Proxy tile. Used when the preview is for a prefab.
        /// </summary>
        private static ProxyTile GetProxyTile
        {
            get 
            {
                s_ProxyTiles         ??= new List<ProxyTile>(ProxyTileListInitialSize);
                if (ProxyIndex < s_ProxyTiles.Count)
                {
                    var proxy = s_ProxyTiles[ProxyIndex++];
                    proxy.Init();
                    return proxy;
                }

                ProxyIndex++; 
                var t = CreateInstance<ProxyTile>();
                t.Init();
                t.hideFlags = HideFlags.HideAndDontSave;
                t.sprite    = TpIconLib.FindIconAsSprite(TpIconType.PrefabIcon);
                s_ProxyTiles.Add(t);
                return t;
            }
        }
        
        /// <summary>
        /// is a tilemap preview active?
        /// </summary>
        /// <value><c>true</c> if tilemap preview; otherwise, <c>false</c>.</value>
        public static bool PreviewActive => s_EditorPreviewTilemaps.Count != 0;
        
        /// <summary>
        /// How many previews are being shown (number of tilemaps)
        /// </summary>
        public static int NumPreviews => s_EditorPreviewTilemaps.Count;
        
        /// <summary>
        /// total # of preview tiles at a moment in time
        /// </summary>
        public static int NumPreviewTiles { get; private set; }
        
        public static int ProxyIndex
        {
            get => s_ProxyIndexBacking;
            private set
            {
                s_ProxyIndexBacking = value;
                if (value > s_MaxProxyIndex)
                    s_MaxProxyIndex = value;
            }
        }

        #endregion
        
        
        #region previews

        public static void ResetPreviews()
        {
            ClearPreview(); //in case
            s_EditorPreviewTilemaps.Clear();
            s_MaxProxyIndex = 0;
            ProxyIndex      = 0;
            NumPreviewTiles = 0;

        }
        
        /// <summary>
        /// Preview one or more tilemaps.
        /// </summary>
        /// <param name="id">Instance ID for UnityEngine.Object invokers. 0 otherwise.</param>
        /// <param name="tileParent">the parent of the calling tile (if any, can be null)</param>
        /// <param name="tileFab">a TpTileFab asset</param>
        /// <param name="offset">optional offset from tiles' stored positions</param>
        /// <param name="rotation">optional rotation (not implemented)</param>
        /// <param name="targets">optional tilemap targets: if not null
        /// and the list is the same # of tilemaps as in the TileFab then these maps are used. Obv need to be in same order as asset</param>
        // ReSharper disable once MemberCanBePrivate.Global
        public static void PreviewTileFab(int id,  Tilemap?                     tileParent,
                                          TpTileFab                    tileFab,
                                          Vector3Int                   offset,
                                          TpTileBundle.TilemapRotation rotation = TpTileBundle.TilemapRotation.Zero,
                                          List<Tilemap>?               targets  = null)
        {
            CurrentPreviewPosition = offset;
            PreviewId              = id;
            NumPreviewTiles        = 0;
            
            if (PreviewActive)
                ClearPreview();

            #pragma warning disable CS0219 // Variable is assigned but its value is never used
            var found = false;
            #pragma warning restore CS0219 // Variable is assigned but its value is never used

            //get the grid that's the parent of the caller's parent tilemap.
            var grid = tileParent != null
                           ? GetParentGrid(tileParent.transform)
                           : null;

            if (targets != null && targets.Count != tileFab.m_TileAssets!.Count)
            {
                TpLogWarning("Tilefab preview cancelled: mismatch between # of TpTileBundle assets and the number of Tilemaps provided!");
                return;
            }

            var targetIndex   = 0;
            var useTargetList = targets != null;
            var firstOfGroup  = true;
            foreach (var assetSpec in tileFab.m_TileAssets!)
            {
                var tileSet = assetSpec.m_Asset;

                if (useTargetList)
                {
                    found = true;
                    PreviewTileBundle(targets![targetIndex++], offset, tileSet,FabOrBundleLoadFlags.None,firstOfGroup,firstOfGroup);
                }
                //todo is "Untagged" language-invariant?
                //try using tag to find the tilemap's GameObject.
                if (assetSpec.m_TilemapTag != NoTagString)
                {
                    var taggedGo = GameObject.FindWithTag(assetSpec.m_TilemapTag);
                    if (taggedGo != null && taggedGo.TryGetComponent<Tilemap>(out var taggedMap))
                    {
                        found = true;
                        PreviewTileBundle(taggedMap, offset, tileSet, FabOrBundleLoadFlags.None, firstOfGroup, firstOfGroup);
                        firstOfGroup = false;
                        continue;
                    }
                }

                //if that didn't work, try using the tilemap's name.
                var mapName = assetSpec.m_TilemapName;
                if (grid == null) //if no grid somehow, then try to find by name
                {
                    var go = GameObject.Find(mapName);
                    if (go != null && go.TryGetComponent<Tilemap>(out var namedMap))
                    {
                        found = true;
                        PreviewTileBundle(namedMap, offset, tileSet, FabOrBundleLoadFlags.None, firstOfGroup,firstOfGroup);
                        firstOfGroup = false;
                        continue;
                    }
                }
                else //try to look thru the Grid's children for the tilemap
                {
                    var possibleMaps = grid.GetComponentsInChildren<Tilemap>();
                    // ReSharper disable once LoopCanBePartlyConvertedToQuery
                    foreach (var child in possibleMaps)
                    {
                        if (child.name != mapName)
                            continue;
                        found = true;
                        PreviewTileBundle(child, offset, tileSet,FabOrBundleLoadFlags.None,firstOfGroup,firstOfGroup);
                        firstOfGroup = false;
                        break;
                    }
                }

                /*
                if (!found && Warnings)
                {
                    //commented out to reduce console spamming.
                    //TpLogWarning($"Could not find tilemap to create preview: tag=[{assetSpec.m_TilemapTag}], name={mapName}.");
                    PreviewId = id;
                    return; 
                }
                */
                PreviewId                    = id; //necc since PreviewTileBundle sets this to zero.
                
            }
        }


        /// <summary>
        /// Previews the imported tilemap. Note that is set to 'internal' visibility intentionally.
        /// Cannot be used by Tile GUI code.
        /// </summary>
        /// <param name="map">The map.</param>
        /// <param name="offset">The offset to place the tileset at.</param>
        /// <param name="tileSet">The tileset.</param>
        /// <param name = "fabOrBundleLoadFlags" >Flags for the Bundle's TileSet method</param>
        /// <param name = "clearExistingPreviews" >Clear any exising previews.
        /// If loading multiple bundles (e.g., when loading several bundles from a TileFab)
        /// set this TRUE for the first one ONLY.</param>
        /// <param name = "clearNumPreviewTilesCount" >Set true for bundles, set false when called from PreviewTilefab so NumPreviewTiles count not zeroed</param>
        /// <remarks>NOTE that this sets the Preview ID to zero.</remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        internal static void PreviewTileBundle(Tilemap              map,
                                               Vector3Int           offset,
                                               TpTileBundle         tileSet,
                                               FabOrBundleLoadFlags fabOrBundleLoadFlags      = FabOrBundleLoadFlags.None,
                                               bool                 clearExistingPreviews     = true,
                                               bool                 clearNumPreviewTilesCount = true)
        {
            if(clearExistingPreviews && PreviewActive)
                ClearPreview();
            if (clearNumPreviewTilesCount)
                NumPreviewTiles = 0;
            
            if(map == null )
                return;
            PreviewId              =  0;
            CurrentPreviewPosition =  offset;
            fabOrBundleLoadFlags   |= FabOrBundleLoadFlags.NoClone;
            var tiles = tileSet.Tileset(TpTileBundle.TilemapRotation.Zero, fabOrBundleLoadFlags);
            
            s_EditorPreviewTilemaps.Add(map);
            
            foreach (var item in tiles)
            {
                NumPreviewTiles++;
                var pos = item.m_Position + offset;
                map.SetEditorPreviewTile(pos, item.m_Tile);
                map.SetEditorPreviewColor(pos, item.m_Color);
                map.SetEditorPreviewTransformMatrix(pos, item.m_TransformMatrix);
            }

            var limitCount = 0;
            foreach (var item in tileSet.m_Prefabs)
            {
                if (++limitCount > 128)
                    break;
                
                var pos       = Vector3Int.FloorToInt( item.m_Position + offset );
                
                
                (var img, _) = PreviewGameObject(item.m_Prefab);
                var sprt = TpImageLib.SpriteFromTexture(img); //note caching in TpIconLib not used for these sprites as we cache them here.
                
                var proxy = GetProxyTile;
                if (sprt != null)
                    proxy.sprite = sprt; 
                proxy.transform = Matrix4x4.identity;
                proxy.color     = Color.white;
                map.SetEditorPreviewTile(pos, proxy);
                map.SetEditorPreviewColor(pos, Color.white);

                if (item.m_V3PrefabHandling)
                {
                    
                }
                else
                    map.SetEditorPreviewTransformMatrix(pos, Matrix4x4.identity);
                NumPreviewTiles++;
            }
        }


        /// <summary>
        /// Shows a single-tile preview.
        /// </summary>
        /// <param name="id">Instance ID for UnityEngine.Object invokers. 0 otherwise.</param>
        /// <param name="map">target tilemap.</param>
        /// <param name="pos">target position.</param>
        /// <param name="tile">tile to preview</param>
        /// <param name="transform">transform.</param>
        /// <param name="color">color.</param>
        /// 
        private static void SingleTilePreview(int          id,
                                              Tilemap?     map,
                                              Vector3Int   pos,
                                              TileBase     tile,
                                              Matrix4x4?   transform   = null,
                                              Color?       color       = null)
        {
            if(PreviewActive)
                ClearPreview();
            NumPreviewTiles = 1;
            if(map == null )
                return;

            PreviewId =   id;
            transform ??= Matrix4x4.identity;
            color     ??= Color.white;
           
            CurrentPreviewPosition = pos;
            PreviewIsPlaceholderTile = tile == s_PlaceholderTile;

            //create the single-position BoundsInt if it hasn't already been created.
            s_SinglePositionBoundsInt ??= TileUtil.CreateBoundsInt(Vector3Int.zero, Vector3Int.one);
            
            s_EditorPreviewTilemaps.Add(map);

            map.SetEditorPreviewTile(pos, tile);
            map.SetEditorPreviewTransformMatrix(pos, (Matrix4x4)transform);
            map.SetEditorPreviewColor(pos, (Color)color);
            
        }
        
        
        /// <summary>
        /// Preview a multiple-selection
        /// </summary>
        /// <param name="id">Instance ID for UnityEngine.Object invokers. 0 otherwise.</param>
        /// <param name="map">the target map</param>
        /// <param name="offset">base location</param>
        /// <param name="clipboardItem">targetTileData</param>
        private static void PreviewTileCells(int id, Tilemap map, Vector3Int offset, TpPainterClipboard clipboardItem)
        {
            if (PreviewActive)
                ClearPreview();
            NumPreviewTiles = 0;
            
            if(clipboardItem.Cells == null || clipboardItem.Cells.Length == 0)
                return;
            
            if(map == null )
                return;
            PreviewId              = id;

            
            CurrentPreviewPosition =  offset;
            s_EditorPreviewTilemaps.Add(map);

            offset += clipboardItem.OffsetModifier; 
            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (var cell in clipboardItem.Cells)
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if(cell == null)
                    continue;
                if(cell.TileBase == null)
                    continue;

                var pos = cell.m_Position + offset;
                pos.z = 0;
                map.SetEditorPreviewTile(pos, cell.TileBase);
                map.SetEditorPreviewColor(pos, cell.m_Color); 
                map.SetEditorPreviewTransformMatrix(pos, cell.m_Transform);
                NumPreviewTiles++;

            }
 
        }

        /// <summary>
        /// Is Preview active for a particular instance ID?
        /// </summary>
        /// <param name="id">IID to test</param>
        /// <returns>T/F</returns>
        /// <remarks>Note that SceneView previews' ID is always 0</remarks>
        public static bool IsPreviewActiveForId(int id)
        {
            return id == PreviewId && PreviewActive;
        }
        
        
        /// <summary>
        /// Clear tilemap previews if any
        /// </summary>
        public static void ClearPreview()
        {
            NumPreviewTiles = 0;
            if (!PreviewActive)
                return;
            PreviewId                    = 0;
            
            PreviewIsPlaceholderTile     = false;
            //the dictionary s_EditorPreviewTilemaps has the tilemaps being
            //previewed and the boundsInt for the area affected 
            foreach (var map in s_EditorPreviewTilemaps.Where(m=>m != null))
                map.ClearAllEditorPreviewTiles();

            s_EditorPreviewTilemaps.Clear();
            
            if(s_ProxyTiles == null)
                return;
            //remove refs from proxy tiles
            var max = Mathf.Min(ProxyIndex, s_ProxyTiles.Count);
            for (var i = 0; i < max; i++)
            {
                var proxy = s_ProxyTiles[i];
                if (proxy != null)
                    proxy.sprite = null;
            }
            //reset pool index
            ProxyIndex = 0;
        }

        #endregion
        
        
        
        #region plugins
        /// <summary>
        /// ResetPlugins the Type->Plugin mapping
        /// </summary>
        internal static void ResetPlugins()
        {
            s_TileTypeToPluginMap.Clear();
            BuildTilePluginsMap();
        }

        private static void UpdateMapIfEmpty()
        {
            if (s_Initialized || s_TileTypeToPluginMap.Count != 0)
                return;
            BuildTilePluginsMap();
            s_Initialized = true;

        }

        /// <summary>
        /// Does a plugin exist for a Type
        /// </summary>
        /// <param name="t">a Type</param>
        /// <returns>true if a plugin exists for this Type</returns>
        internal static bool PluginExists(Type t)
        {
            if(!s_Initialized)
                UpdateMapIfEmpty();
            return s_TileTypeToPluginMap.ContainsKey(t);
        }

        /// <summary>
        /// Get a plugin if it exists
        /// </summary>
        /// <param name="tileType">The type of tile</param>
        /// <param name="plug">out param for the plugin</param>
        /// <returns>true if a plugin for this type exists</returns>
        internal static bool TryGetPlugin(Type? tileType, out TpPainterPluginBase? plug)
        {
            if (tileType != null)
            {
                if(!s_Initialized)
                    UpdateMapIfEmpty();
                return s_TileTypeToPluginMap.TryGetValue(tileType, out plug);
            }

            plug = null;
            return false;

        }

        /// <summary>
        /// Get a plugin if it exists
        /// </summary>
        /// <param name="tileBase">a tile instance</param>
        /// <param name="plug">out param for the plugin</param>
        /// <returns>true if a plugin for this type exists</returns>
        internal static bool TryGetPlugin(TileBase tileBase, out TpPainterPluginBase? plug)
        {
            if(!s_Initialized)
                UpdateMapIfEmpty();
            return s_TileTypeToPluginMap.TryGetValue(tileBase.GetType(), out plug);
        }
        
                
        /// <summary>
        /// Look for any  tile-type plugins and build the mapping.
        /// </summary>
        private static void BuildTilePluginsMap()
        {
            var guids     = AssetDatabase.FindAssets("t:TpPainterPluginBase");
            var ttPlugins = new TpPainterPluginBase?[guids.Length];
            var index     = 0;
            for (var i = 0; i < guids.Length; i++)
            {
                var pluginScrObj = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[i]), typeof(TpPainterPluginBase)) as TpPainterPluginBase;
                if (pluginScrObj == null)
                    continue;
                ttPlugins[index++] = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(pluginScrObj)) as TpPainterPluginBase;
            }

            s_TileTypeToPluginMap.Clear();
            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (var plug in ttPlugins)
            {
                if(plug == null)
                    continue;
                if(plug.m_IgnoreThis)
                    continue;
                s_TileTypeToPluginMap.Add(plug.GetTargetTileType, plug);
            }

            if(TpLibEditor.Informational)
                TpLib.TpLog($"Found {s_TileTypeToPluginMap.Count} TileType Plugins");
        }

        #endregion
       
        #region Preview
        /// <summary>
        /// Get a preview icon for a tile. 
        /// </summary>
        /// <param name="tileBase">tile to get a preview for</param>
        /// <returns>a preview icon or a '?' if none available</returns>
        [SuppressMessage("ReSharper", "Unity.NoNullPatternMatching")]
        internal static Texture2D? PreviewIcon(TileBase tileBase)
        {
            if(!s_Initialized)
                UpdateMapIfEmpty();

            if (tileBase == null)
                return TpIconLib.FindIcon(TpIconType.HelpIcon);               

            if (tileBase is ITilePlus itp && itp.PreviewIcon != null)
                return itp.PreviewIcon;

            Texture2D? preview = null;

            if ((tileBase is ITilePlus { IsClone: true } itileplus) && itileplus.EffectiveSprite != null)
                preview = TpImageLib.TextureFromSprite(itileplus.EffectiveSprite,false); //false here means don't change READABLE flag if not set.
            
            else if (tileBase is Tile uTile)                   //this will include all TilePlus tiles or anything else subclassing Tile
                preview = AssetPreview.GetAssetPreview(uTile); 

            // ReSharper disable once InvertIf
            if (preview == null)
            {
               if (s_TileTypeToPluginMap.TryGetValue(tileBase.GetType(), out var plug) && plug != null)
                   preview = AssetPreview.GetAssetPreview(plug.GetSpriteForTile(tileBase));
            }

            return preview == null
                       ? TpIconLib.FindIcon(TpIconType.HelpIcon)
                       : preview;
        }

        /// <summary>
        /// Get a preview icon for a GameObject. 
        /// </summary>
        /// <param name="gameObject">GameObject to get a preview for</param>
        /// <returns>tuple of ('tex': a preview icon or a '?', "found": true if the preview was available, false if null or the HelpIcon)</returns>
        /// <remarks>Variant prefab previews don't work properly so the Generic PrefabIcon is returned. Also, if the
        /// preview height or width is zero (IDK why this would happen) 'found' is returned false.</remarks>
        internal static (Texture2D? preview, bool rVal) PreviewGameObject(GameObject gameObject)
        {
            if (gameObject == null)
                return (TpIconLib.FindIcon(TpIconType.HelpIcon), false);

            if (!s_Hash128.HasValue)
            {
                var discard = new Texture2D(128, 128);
                s_Hash128 = discard.imageContentsHash;
                Object.DestroyImmediate(discard);
            }
            
            var preview = AssetPreview.GetAssetPreview(gameObject);
            if (preview == null)
                return (TpIconLib.FindIcon(TpIconType.HelpIcon), false);
            
            if (preview.imageContentsHash == s_Hash128.Value)
            {
                TpLib.TpLogWarning($"GO {gameObject.name} had empty preview. [1]");
                return (TpIconLib.FindIcon(TpIconType.HelpIcon), false);
            }

            if (preview.height != 0 && preview.width != 0)
                return (preview, true);
            TpLib.TpLogWarning($"GO {gameObject.name} had empty preview. [2]");
            return (TpIconLib.FindIcon(TpIconType.HelpIcon), false);

        }
        
        
        
        /// <summary>
        /// Handles previews from Scene View.
        /// </summary>
        internal static void HandlePreviews(Tilemap targetMap, TpPainterClipboard clipboardObject, Vector3Int mousePosition, TpPainterTool currentTool, TpPainterMoveSequenceStates moveState)
        {
            if (!PreviewActive && TpPainterSceneView.instance.ValidPaintTargetAndPaintableObject && (currentTool == TpPainterTool.Paint || (currentTool == TpPainterTool.Move && moveState == TpPainterMoveSequenceStates.Paint)))
            {
                if (clipboardObject is { ItemVariety: Variety.TileItem, IsTpBundleTile: false })
                {
                    if(clipboardObject is {IsTilePlusBase: true} && clipboardObject.Tile != null) //is TilePlusBase subclass
                    {
                        //if the tile sprite isn't being shown, use the placeholderTile
                        var usingPh = ((ITilePlus)clipboardObject.Tile).TileSpriteClear is SpriteClearMode.ClearInSceneView
                                                                                      or SpriteClearMode.ClearInSceneViewAndOnStart;
                        var t = clipboardObject.Tile as Tile;
                        if (t != null)
                        {
                            if (usingPh)
                            {
                                var phTile = PlaceHolderTile;
                                SingleTilePreview(0, targetMap, mousePosition, phTile);
                            }
                            else
                                SingleTilePreview(0, targetMap, mousePosition, t, clipboardObject.transform);
                        }
                    }
                    else if (clipboardObject is {IsTile:true} && clipboardObject.Tile != null) //is Tile subclass but NOT TilePlusBase
                    {
                        var t = clipboardObject.Tile as Tile;
                        if (t != null)
                            SingleTilePreview(0, targetMap, mousePosition,
                                                               clipboardObject.Tile,
                                                               clipboardObject.transform,
                                                               clipboardObject.AColor);

                    }
                    else if (clipboardObject.Tile != null) //is a TileBase tile, needs special handling
                    {
                        var tile = clipboardObject.Tile;
                        if (!TryGetPlugin(tile, out var plug) || plug == null)
                            return;

                        SingleTilePreview(0, targetMap, mousePosition, tile,
                                                           plug.GetTransformForTile(tile),
                                                           plug.GetColorForTile(tile));
                    }

                }
                
                else if (clipboardObject.ItemVariety == Variety.BundleItem  || clipboardObject.IsTpBundleTile)
                {
                    if(!clipboardObject.IsTpBundleTile && !TpPreviewUtility.PreviewActive && targetMap != null && clipboardObject.Bundle != null)
                        PreviewTileBundle(targetMap, mousePosition, clipboardObject.Bundle);
                    //2.1 added support for TpBundleTile preview
                    else if (clipboardObject.IsTpBundleTile && !TpPreviewUtility.PreviewActive && targetMap != null)
                    {
                        // ReSharper disable once Unity.NoNullPatternMatching
                        if (clipboardObject.Tile && clipboardObject.Tile is TpBundleTile tpBundleTile)
                            PreviewTileBundle(targetMap, mousePosition, tpBundleTile.m_TileBundle);
                    }
                }
                
                else if (clipboardObject.ItemVariety == Variety.TileFabItem)
                {
                    if (!TpPreviewUtility.PreviewActive && targetMap != null && clipboardObject.TileFab != null)
                        PreviewTileFab(0, targetMap, clipboardObject.TileFab, mousePosition);
                }
                //new in 2.1 - multiple tile picks.
                else if (clipboardObject is { ItemVariety: Variety.MultipleTilesItem, Cells: not null }) 
                {
                    if (!TpPreviewUtility.PreviewActive && targetMap != null && clipboardObject.Cells.Length != 0)
                        PreviewTileCells(0, targetMap, mousePosition, clipboardObject);
                }
                //also new in 2.1: prefabs
                else if (clipboardObject is { ItemVariety: Variety.PrefabItem} && clipboardObject.Prefab)
                {
                    #pragma warning disable CS8604 // Possible null reference argument.
                    (var tex, _) = PreviewGameObject(clipboardObject.Prefab);
                    #pragma warning restore CS8604 // Possible null reference argument.
                    var proxy = GetProxyTile;
                    var sprt  = TpImageLib.SpriteFromTexture(tex);
                    if (sprt != null)
                        proxy.sprite = sprt; 
                    proxy.transform = Matrix4x4.identity;
                    proxy.color     = Color.white;
                    SingleTilePreview(0, targetMap, mousePosition, proxy, clipboardObject.transform, proxy.color);
                }
            }
            
            if (PreviewActive && !(currentTool == TpPainterTool.Paint || (currentTool == TpPainterTool.Move && moveState == TpPainterMoveSequenceStates.Paint)))
                ClearPreview();
            if (PreviewActive && mousePosition != CurrentPreviewPosition) 
                ClearPreview();
        }
        
        
        
        
        
        
        #endregion


        
    }
}
