#nullable enable
// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 08-03-2021
// ***********************************************************************
// <copyright file="TpTileBundle.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************if
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TilePlus
{
    /// <summary>
    /// An asset used to bundle-up tiles for later
    /// re-creation. Since Transforms, Colors, and Flags
    /// values are often the same, those are indexed to
    /// save space.
    /// </summary>
    /// <remarks>Version 2 adds GUIDs</remarks>
    [Serializable]    [CreateAssetMenu(fileName = "TpTileBundle.asset", menuName = "TilePlus/Create TpTileBundle", order = 100001)]

    public class TpTileBundle : ScriptableObject
    {
        #region classDefs
        /// <summary>
        /// Enum of rotation types
        /// </summary>
        public enum TilemapRotation
        {
            /// <summary>
            /// No rotation
            /// </summary>
            Zero,
            /// <summary>
            /// 90 degrees rotation
            /// </summary>
            Ninety,
            /// <summary>
            /// 180 degrees rotation
            /// </summary>
            OneEighty,
            /// <summary>
            /// 270 degrees rotation
            /// </summary>
            TwoSeventy
        }

        /// <summary>
        /// Class with info about tiles. IEnumerable of instances of this
        /// is returned from Tileset method
        /// </summary>
        public class TilesetItem
        {
            /// <summary>
            /// The tile (in asset V2 this was changed from Tile to TileBase)
            /// </summary>
            public TileBase? m_Tile;
            /// <summary>
            /// its position
            /// </summary>
            public Vector3Int m_Position;
            /// <summary>
            /// its transform matrix
            /// </summary>
            public Matrix4x4  m_TransformMatrix;
            /// <summary>
            /// its color
            /// </summary>
            public Color      m_Color;
            /// <summary>
            /// its flags
            /// </summary>
            public TileFlags  m_Flags;
            /// <summary>
            /// is this a TilePlus tile?
            /// </summary>
            public bool m_IsTptTile;
        }


        /// <summary>
        /// Specification for one TilePlus tile
        /// </summary>
        [Serializable]
        public class TilePlusItem
        {
            /// <summary>
            /// The tile
            /// </summary>
            [SerializeField] public TilePlusBase m_Tile;
            /// <summary>
            /// its position
            /// </summary>
            [SerializeField] public TilePosition m_TilePosition;

            /// <summary>
            /// Initializes a new instance of the <see cref="TilePlusItem"/> class.
            /// </summary>
            /// <param name="pos">The position of the tile.</param>
            /// <param name="tile">The tile instance.</param>
            public TilePlusItem(TilePosition pos, TilePlusBase tile)
            {
                m_TilePosition = pos;
                m_Tile         = tile;
            }
        }

        /// <summary>
        /// specification for one Unity tile
        /// </summary>
        [Serializable]
        public class UnityTileItem
        {
            //todo - use Run-Length-Encoding to reduce the need for a huge list of positions.
            /// <summary>
            /// A list of positions where this tile is at. it's in column order, ie
            ///             0,2   1,2   2,2
            ///             0,1   1,1   2,1
            /// Xcol,Yrow:  0,0   1,0   2,0
            /// </summary>
            [SerializeField] public List<TilePosition> m_Positions;
            /// <summary>
            /// The Unity tile. In Asset Version 2 this was changed from Tile to TileBase.
            /// </summary>
            [SerializeField] public TileBase        m_UnityTile;

            /// <summary>
            /// Initializes a new instance of the <see cref="UnityTileItem"/> class.
            /// </summary>
            /// <param name="pos">The position of the tile.</param>
            /// <param name="tile">The tile instance.</param>
            public UnityTileItem(TilePosition pos, TileBase tile)
            {
                m_Positions = new List<TilePosition> {pos};
                m_UnityTile = tile;
            }
        }


        /// <summary>
        /// Prefab info for use when loading tilesets
        /// </summary>
        [Serializable]
        public class PrefabItem
        {
            /// <summary>
            /// The prefab
            /// </summary>
            [SerializeField] public GameObject m_Prefab;
            /// <summary>
            /// its position
            /// </summary>
            [SerializeField] public Vector3    m_Position;

            /// <summary>
            /// Rotation
            /// </summary>
            [SerializeField]
            public Quaternion m_Rotation;

            /// <summary>
            /// Scale
            /// </summary>
            [SerializeField]
            public Vector3 m_Scale;

            /// <summary>
            /// If true, is V3 prefab handling-enabled.
            /// </summary>
            [SerializeField]
            public bool m_V3PrefabHandling;

            /// <summary>
            /// Matrix for preview tiles for V3.
            /// </summary>
            [SerializeField]
            public Matrix4x4 m_MatrixForPreview = Matrix4x4.identity;

            /// <summary>
            /// Initializes a new instance of the <see cref="PrefabItem"/> class.
            /// </summary>
            /// <param name="prefab">The prefab.</param>
            /// <param name="position">The position.</param>
            /// <param name="rotation">Optional rotation (V3 support)</param>
            /// <param name="scale">Optional scale (V3 support)</param>
            public PrefabItem(GameObject prefab, Vector3 position, Quaternion? rotation, Vector3? scale)
            {
                m_Prefab   = prefab;
                m_Position = position;
                if (rotation.HasValue || scale.HasValue)
                {
                    m_V3PrefabHandling = true;
                    m_Rotation            = (Quaternion)(rotation ?? Quaternion.identity);
                    m_Scale               = (Vector3)(scale ?? Vector3.one);
                    m_MatrixForPreview    = Matrix4x4.TRS(prefab.transform.localPosition, m_Rotation,m_Scale);
                }
                else
                {
                    m_Rotation = Quaternion.identity;
                    m_Scale    = Vector3.one;
                }

            }
        }


        /// <summary>
        /// What's at one particular position
        /// </summary>
        [Serializable]
        public class TilePosition
        {
            /// <summary>
            /// The position
            /// </summary>
            [SerializeField] public Vector3Int m_Position;
            /// <summary>
            /// The transform index
            /// </summary>
            [SerializeField] public int m_TransformIndex;
            /// <summary>
            /// the color index
            /// </summary>
            [SerializeField] public int     m_ColorIndex;
            /// <summary>
            /// the flags index
            /// </summary>
            [SerializeField] public int     m_FlagsIndex;

            /// <summary>
            /// Initializes a new instance of the <see cref="TilePosition"/> class.
            /// </summary>
            /// <param name="position">The position.</param>
            /// <param name="transformIndex">Index of the transform.</param>
            /// <param name="colorIndex">Index of the color.</param>
            /// <param name="flagsIndex">Index of the flags.</param>
            public TilePosition(Vector3Int position, int transformIndex, int colorIndex, int flagsIndex)
            {
                m_Position       = position;
                m_TransformIndex = transformIndex;
                m_ColorIndex     = colorIndex;
                m_FlagsIndex     = flagsIndex;
            }
        }
        #endregion
        
        #region publicfields
        /// <summary>
        /// The timestamp for this asset
        /// </summary>
        public string? m_TimeStamp;
        /// <summary>
        /// The scene path when this asset was created
        /// </summary>
        public string    m_ScenePath = string.Empty;
        /// <summary>
        /// The original scene this was created from.
        /// </summary>
        public string m_OriginalScene = string.Empty;
        
        /// <summary>
        /// set true if this bundle was created from a GridSelection
        /// rather than an entire tilemap.
        /// </summary>
        [Tooltip("Indicates that this is from a Grid Selection. ")]
        public bool m_FromGridSelection;

        /// <summary>
        /// Set true if Painter should not show this in its Palettes list
        /// </summary>
        [Tooltip("If checked this won't appear in Tile+Painter lists.")]
        public bool m_IgnoreInPainter;

        /// <summary>
        /// Arbitrary boolean for use when filtering layouts
        /// </summary>
        public bool m_UserFlag;

        /// <summary>
        /// Arbitrary string for use when filtering layouts
        /// </summary>
        public string m_UserString = string.Empty;


        /// <summary>
        /// Added in V3. A representative sprite. Painter will display this UNLESS it's packed in an atlas.
        /// </summary>
        [Tooltip("A Sprite to represent this Bundle. Painter will display it instead of the Bundle icon.")]
        public Sprite? m_Icon;
        
        
        /// <summary>
        /// The bounds int for this tilemap
        /// </summary>
        [SerializeField] public BoundsInt m_TilemapBoundsInt;
        
       

        /// <summary>
        /// The tileplus tiles for  this tilemap
        /// </summary>
        [SerializeField] public List<TilePlusItem> m_TilePlusTiles = new();

        /// <summary>
        /// The Unity tiles for this tilemap
        /// </summary>
        [SerializeField] public List<UnityTileItem> m_UnityTiles = new();

        /// <summary>
        /// The prefabs for this tilemap
        /// </summary>
        [SerializeField] public List<PrefabItem> m_Prefabs = new();

        /// <summary>
        /// An array of indexed tile flags
        /// </summary>
        [SerializeField] public TileFlags[]? m_TileFlags;

        /// <summary>
        /// An array of indexed tile transforms
        /// </summary>
        [SerializeField] public Matrix4x4[]? m_TileTransforms;

        /// <summary>
        /// An array of indexed tile colors
        /// </summary>
        [SerializeField] public Color[]? m_TileColors;
        
        

        #endregion
        
        #region private

        /// <summary>
        /// GUID for this Bundle asset. 
        /// </summary>
        [HideInInspector]
        [SerializeField]
        private byte[]? m_Guid = new byte[1];

        /// <summary>
        /// The cached set of Unity tiles
        /// </summary>
        [NonSerialized]  
        private List<TilesetItem> unityTileSet = new();

        /// <summary>
        /// the last complete tileset
        /// </summary>
        [NonSerialized] 
        private List<TilesetItem>? lastCompleteTileset;
        
        #endregion
        
        #region properties
        
        /// <summary>
        /// Get the current cache of Unity tiles. This may have been affected by filtering.
        /// </summary>
        public List<TilesetItem> UnityTileCache => unityTileSet;
        /// <summary>
        /// Get the last complete Tileset created by Tileset. May have been affected by filtering.
        /// </summary>
        public List<TilesetItem>? LastCompleteTileset => lastCompleteTileset;
        
        
        /// <summary>
        /// Get the asset's GUID as a GUID struct.
        /// </summary>
        /// <value>The unique identifier.</value>
        public Guid BundleGuid => new Guid(m_Guid!);

        /// <summary>
        /// Returns true if this asset was sealed.
        /// That's determined by checking to see if m_TileFlags != null,
        /// since if Seal was never called then that field would be null. 
        /// </summary>
        /// <remarks>Updated in 2.1 to include a check for no prefabs. Permits use of prefab-only bundles.</remarks>
        public bool Valid => m_TileFlags is { Length: > 0 } || m_Prefabs.Count > 0;
        
        
        /// <summary>
        /// Get a string representation of the GUID. 
        /// </summary>
        #if ODIN_INSPECTOR
        [ShowInInspector]
        #endif
        public string AssetGuidString
        {
            get
            {
                return m_Guid is not { Length: 16 }
                           ? string.Empty
                           : new Guid(m_Guid).ToString();
            }
        }
        
        /// <summary>
        /// Gets the asset version.
        /// </summary>
        /// <value>The asset version.</value>
        #if ODIN_INSPECTOR
        [ShowInInspector]
        #endif
        public byte AssetVersion => 3;

        
        /// <summary>
        /// Display cache state 
        /// </summary>
        /// <value>string</value>
        #if ODIN_INSPECTOR
        [ShowInInspector]
        #endif
        public string CacheState => $"U-tiles:{unityTileSet.Count}";

        private bool cacheForceFilter;

        /// <summary>
        /// Set true to force the U-tiles cache to use a provided filter.
        /// THE VALUE IF THIS PROPERTY IS CLEARED AFTER THE NEXT USE OF TileSet() or TileSetChangeData
        /// </summary>
        /// <remarks>Also clears the cache for you.</remarks>
        public bool CacheForceFilter
        {
            get => cacheForceFilter;
            set
            {
                cacheForceFilter = value; 
                ClearCache();
            }
        }
        
        
        
        

        #endregion
        
        #region tileset

        /// <summary>
        /// Get TileChangeData from the Bundle's TileSet
        /// </summary>
        /// <param name="rotation"></param>
        /// <param name="offset">The offset to use for normalizing positions if this Bundle is
        /// from a grid selection. </param>
        /// <param name="loadFlags">control flags. See TileFabLib</param>
        /// <param name="filter">Func returning a bool</param>
        /// <returns> Tuple with TileChangeData array and position->GUID dictionary</returns>
        public (TileChangeData[] data, Dictionary<Vector3Int, string> dict) TilesetChangeData(TilemapRotation rotation,
            Vector3Int                                                                                        offset,
            FabOrBundleLoadFlags                                                                              loadFlags,
            Func<FabOrBundleFilterType, BoundsInt, object, bool>?          filter = null
            )
                                                                                              
        {
            var posToGuidMap = new Dictionary<Vector3Int, string>();
            //get the optionally-filtered tileset
            var tileSet = Tileset(rotation, loadFlags, filter);
            //create the TileChangeData.
            var num     = tileSet.Count;
            var tileChangeData = new TileChangeData[num];
            
            
            for (var index = 0; index < num; index++)
            {
                var tsItem = tileSet[index];
                var itp      = tsItem.m_Tile as ITilePlus;
                if(itp != null)
                    posToGuidMap.Add(tsItem.m_Position+offset, itp.TileGuidString); //note that this is the new GUID if newGuids is true (which it ought to be for TPT tiles!)
                tileChangeData[index] = new TileChangeData(tsItem.m_Position + offset, tsItem.m_Tile, tsItem.m_Color, tsItem.m_TransformMatrix);
            }

            return (tileChangeData,posToGuidMap);

        }

        /// <summary>
        /// Get an array of TilesetItem instances with
        /// all the information that's needed to populate
        /// a Tilemap with the tiles contained in the asset.
        /// </summary>
        /// <param name="rotation">Rotation (unimplemented)</param>
        /// <param name = "filter" >a Func  returning a bool. </param>
        /// <param name = "loadFlags"> Control Flags. See TileFabLib  </param>
        /// <returns>List&lt;TilesetItem&gt;.</returns>
        public List<TilesetItem> Tileset(TilemapRotation                            rotation,
                                         FabOrBundleLoadFlags                       loadFlags,
                                         Func<FabOrBundleFilterType, BoundsInt, object, bool>? filter = null)
        {
            var unityTilesCount = m_UnityTiles.Count;
            var tptTilesCount   = m_TilePlusTiles.Count;
            var size            = unityTilesCount + tptTilesCount;
            
            if (unityTileSet.Count < unityTilesCount)
            {
                unityTileSet.Clear();
                unityTileSet.Capacity = unityTilesCount;
            }

            var filterOnlyTilePlusTiles = (loadFlags & FabOrBundleLoadFlags.FilterOnlyTilePlusTiles) != 0;
            
            //if CacheForceFilter property is set, then we want to use the filter when building the cache.
            var useFilterInCacheBuilding = CacheForceFilter && filter != null && !filterOnlyTilePlusTiles;
            CacheForceFilter = false;
            
            //if there are Unity tiles but the cache is empty then fill the cache.
            if (unityTilesCount != 0 && unityTileSet.Count == 0)
            {
                //use info from UnityTiles to create tileset items
                foreach (var unityTile in m_UnityTiles)
                {
                    var tile = unityTile.m_UnityTile;
                    foreach (var tilePosition in unityTile.m_Positions)
                    {
                        var tsItem = new TilesetItem
                                         {
                                             m_Tile = tile, m_Position = tilePosition.m_Position,
                                             //for the other fields, use the position to get color,flags,transform
                                             m_Color = m_TileColors![tilePosition.m_ColorIndex], m_Flags = m_TileFlags![tilePosition.m_FlagsIndex], m_TransformMatrix = m_TileTransforms![tilePosition.m_TransformIndex]
                                         };
                        if(useFilterInCacheBuilding && !filter!(FabOrBundleFilterType.Unity, m_TilemapBoundsInt, tsItem))
                           continue;
                        unityTileSet.Add(tsItem);
                    }
                }
           
            }

            if(lastCompleteTileset == null)
                lastCompleteTileset = new List<TilesetItem>(size);
            else
                lastCompleteTileset.Clear();

            if (filter != null && !filterOnlyTilePlusTiles) //appropriate for next section only
            {
                foreach (var item in unityTileSet)
                {
                    if(filter(FabOrBundleFilterType.Unity, m_TilemapBoundsInt,item))
                        lastCompleteTileset.Add(item);
                } 
            }
            else
                lastCompleteTileset.AddRange(unityTileSet);

            var noCloning = (loadFlags & FabOrBundleLoadFlags.NoClone) == FabOrBundleLoadFlags.NoClone;
            
            var useFilter = !noCloning && filter != null; //need to check this again since here we don't care about filterOnlyTilePlusTiles
            var newGuids  = (loadFlags & FabOrBundleLoadFlags.NewGuids) == FabOrBundleLoadFlags.NewGuids;
            //same process for the tileplus tiles except there's no cache for them.
            foreach (var tilePlusItem in m_TilePlusTiles)
            {
                if(useFilter && !filter!(FabOrBundleFilterType.TilePlus,m_TilemapBoundsInt,tilePlusItem))
                    continue;
                var tilePosition = tilePlusItem.m_TilePosition;
                
                var tile = tilePlusItem.m_Tile;
                if (noCloning)
                {
                    if(tile.IsLocked)
                        tile.ResetState(TileResetOperation.ClearGuid);
                }
                else
                {
                    if (tile.IsClone)
                    {
                        //just in case this tile had been changed to Clone state somehow eg someone saved a prefab. Might help
                        tile.ResetState(TileResetOperation.MakeNormalAsset);
                    }
                    else if (tile.IsLocked) //should be locked but not a bad idea to check
                    {
                        tile = tile.Cloner(newGuids); //get the clone. This is faster than placing the locked tile and having it refresh itself.
                        //note that the newGuids = true means that these tiles get a new GUID
                    }
                }

                var tsItem = new TilesetItem
                             {
                                 m_IsTptTile       = true,
                                 m_Position        = tilePosition.m_Position, 
                                 m_Tile            = tile, 
                                 m_Color           = m_TileColors![tilePosition.m_ColorIndex], 
                                 m_Flags           = m_TileFlags![tilePosition.m_FlagsIndex],
                                 m_TransformMatrix = tile!.transform 
                             };
                lastCompleteTileset.Add(tsItem);
            }

            return lastCompleteTileset;
        }
        #endregion
        
        #region utils

        

        /// <summary>
        /// Clear the Unity tiles' cache
        /// </summary>
        public void ClearCache()
        {
            unityTileSet.Clear();
        }

        /// <summary>
        /// Add a GUID to this asset. Only works once.
        /// </summary>
        /// <returns>true if the GUID was added, false if there already was a GUID</returns>
        public bool AddGuid()
        {
            if (m_Guid is { Length: 16 })
                return false;
            m_Guid = Guid.NewGuid().ToByteArray();
            return true;

        }

       
        
        /// <summary>
        /// Find the TilePlus tile for a position.
        /// </summary>
        /// <param name="position">Position</param>
        /// <returns>Tile instance or null if not found</returns>
        public TilePlusBase FindMatchingTpTile(Vector3Int position)
        {
            var indexOfExisting = m_TilePlusTiles.FindIndex(tpItem => tpItem.m_TilePosition.m_Position == position);
            return (indexOfExisting == -1 ? null : m_TilePlusTiles[indexOfExisting].m_Tile)!;
        }

        /// <summary>
        /// Find the Unity tile for a position
        /// </summary>
        /// <param name="position">Position</param>
        /// <returns>the tile instance or null if not found</returns>
        public TileBase? FindMatchingUnityTile(Vector3Int position)
        {
            foreach (var unityTileItem in m_UnityTiles)
            {
                var indexOfExisting = unityTileItem.m_Positions.FindIndex(pos => pos.m_Position == position);
                if (indexOfExisting != -1)
                    return unityTileItem.m_UnityTile;
            }

            return null;
        }
        #endregion

        
        #if UNITY_EDITOR
        #region editorOnly
        

        #if ODIN_INSPECTOR
        /// <summary>
        /// Diags
        /// </summary>
        [ShowInInspector]
        public string BoundsCenter => m_TilemapBoundsInt.center.ToString();
        #endif
        
        /*these four dictionaries are used to compile information while the asset is being created.
         Then the Seal() method creates the final lists of transform matrices, colors, and flags.
         */
        /// <summary>
        /// The working set of tile instances
        /// </summary>
        private readonly Dictionary<TileBase, int> workingSet = new Dictionary<TileBase, int>();

        /// <summary>
        /// The working set of matrix instances
        /// </summary>
        private readonly Dictionary<Matrix4x4, int> workingSetMatrix = new Dictionary<Matrix4x4, int>() {{Matrix4x4.identity, 0}};

        /// <summary>
        /// The working set of colors
        /// </summary>
        private readonly Dictionary<Color, int> workingSetColor = new Dictionary<Color, int>() {{Color.white, 0}};

        /// <summary>
        /// The working set of tile flags
        /// </summary>
        private readonly Dictionary<TileFlags, int> workingSetFlags = new Dictionary<TileFlags, int>();


        /// <summary>
        /// The matrix index
        /// </summary>
        private int matrixIndex, colorIndex, flagsIndex;

        /// <summary>
        /// Add a prefab to the prefabs list
        /// </summary>
        /// <param name="prefab">The prefab instance</param>
        /// <param name="position">The position</param>
        /// <param name = "rotation" >Optional rotation (new in v3 with changes to prefab bundling)</param>
        /// <param name = "scale" >Optional scale (new in v3 with changes to prefab bundling)</param>
        public void AddPrefab(GameObject prefab, Vector3 position, Quaternion? rotation = null, Vector3? scale = null)
        {
            m_Prefabs.Add(new PrefabItem(prefab, position, rotation, scale));
        }

        /// <summary>
        /// Add a tileplus tile.
        /// </summary>
        /// <param name="map">the map it's on</param>
        /// <param name="position">the position</param>
        /// <param name="instance">tileplus tile instance</param>
        /// <param name = "sourceForInstance" >the orginal TPB instance has the hard (unaltered by Bundle processing in
        /// a grid selection) grid position, which we need.</param>
        public void AddTpbToListOfTiles(Tilemap map, Vector3Int position, TilePlusBase instance, TilePlusBase sourceForInstance)
        {
            //is there already a tile at this position?
            var indexOfExisting = m_TilePlusTiles.FindIndex(item => item.m_TilePosition.m_Position == position);
            if (indexOfExisting == -1)
            {
                //get indices for transform, color, flags
                /*Important: since the position passed in won't be the same as the actual
                 *Tilemap position when the Bundle is being created from a GridSelection.
                 * Since the Tpb has the actual 'hard' position, we'll use that when
                 * addressing the tilemap
                 */
                var tileHardPosition = sourceForInstance.TileGridPosition;
                var tIndex           = GetTransformMatrixIndex(map, tileHardPosition);
                var cIndex           = GetColorIndex(map, tileHardPosition);
                var fIndex           = GetFlagsIndex(map, tileHardPosition);
                //create a new tile position class instance. Here, we use the passed-in position.
                var tilePosition = new TilePosition(position, tIndex, cIndex, fIndex);
                //create a new tileplus item with that position
                m_TilePlusTiles.Add(new TilePlusItem(tilePosition, instance));
            }
            else //an error if there's already a tile at this position.
                Debug.Log($"Duplicate TilePlusBase @ {position}");
        }

        /// <summary>
        /// Add a single TilePlus tile to the bundle.
        /// </summary>
        /// <param name="instance">TilePlusBase instance</param>
        /// <param name="position">position</param>
        /// <param name="transform">transform</param>
        /// <param name="color">color</param>
        public void AddTpbToListOfTiles(TilePlusBase instance, Vector3Int position, Matrix4x4 transform, Color color)
        {
            var indexOfExisting = m_TilePlusTiles.FindIndex(item => item.m_TilePosition.m_Position == position);
            if (indexOfExisting == -1)
            {
                var tIndex = 0;
                if (transform == Matrix4x4.identity)
                    tIndex = 0;
                else if (workingSetMatrix.TryGetValue(transform, out var indexT))
                    tIndex = indexT;
                else
                {
                    workingSetMatrix.Add(transform, ++matrixIndex);
                    tIndex = matrixIndex;
                }

                var cIndex = 0;
                if(color == Color.white)
                   cIndex = 0;
                else if(workingSetColor.TryGetValue(color, out var indexC))
                    cIndex = indexC;
                else
                {
                    workingSetColor.Add(color, ++colorIndex);
                    cIndex = colorIndex;
                }

                if(workingSetFlags.Count == 0)
                    workingSetFlags.Add(TileFlags.None,0);
                //create a new tile position class instance. Here, we use the passed-in position.
                var tilePosition = new TilePosition(position, tIndex, cIndex, 0);
                //create a new tileplus item with that position
                m_TilePlusTiles.Add(new TilePlusItem(tilePosition, instance));
            }
            else //an error if there's already a tile at this position.
                Debug.Log($"Duplicate TilePlusBase @ {position}");
        }
        
        /*if there's an exisitng transform matrix then return the
          index for that. otherwise create a new dictionary entry and return
          the new index.
         */
        /// <summary>
        /// Gets the index of the transform matrix.
        /// </summary>
        /// <param name="map">The map that the tile is on.</param>
        /// <param name="position">The position of the tile.</param>
        /// <returns>The index for this position</returns>
        private int GetTransformMatrixIndex(Tilemap map, Vector3Int position)
        {
            var tIndex    = 0; //location of Matrix4x4.identity
            var transform = map.GetTransformMatrix(position);  
            if (transform == Matrix4x4.identity)
                return tIndex;
            if (workingSetMatrix.TryGetValue(transform, out var indexT))
                tIndex = indexT;
            else
            {
                workingSetMatrix.Add(transform, ++matrixIndex);
                tIndex = matrixIndex;
            }

            return tIndex;
        }

        //see GetTransformMatrixIndex
        /// <summary>
        /// Gets the index of the color.
        /// </summary>
        /// <param name="map">The map that the tile is on.</param>
        /// <param name="position">The position of the tile.</param>
        /// <returns>The index for the color</returns>
        private int GetColorIndex(Tilemap map, Vector3Int position)
        {
            var cIndex = 0; //location of Color.white
            var color  = map.GetColor(position);
            if (color == Color.white)
                return cIndex;
            if (workingSetColor.TryGetValue(color, out var indexC))
                cIndex = indexC;
            else
            {
                workingSetColor.Add(color, ++colorIndex);
                cIndex = colorIndex;
            }

            return cIndex;
        }

        //see GetTransformMatrixIndex
        /// <summary>
        /// Gets the index of the flags.
        /// </summary>
        /// <param name="map">The map that the tile is on.</param>
        /// <param name="position">The position of the tile.</param>
        /// <returns>The index of the flags</returns>
        private int GetFlagsIndex(Tilemap map, Vector3Int position)
        {
            int fIndex;
            var flags = map.GetTileFlags(position);
            if (workingSetFlags.TryGetValue(flags, out var indexF))
                fIndex = indexF;
            else
            {
                //post-inc is correct here since the dict isn't preset with any values 
                //as are the other two dicts used in this way
                workingSetFlags.Add(flags, flagsIndex);
                fIndex = flagsIndex++;
            }

            return fIndex;
        }


        /// <summary>
        /// Used by bundler to add a unity tile.
        /// </summary>
        /// <param name="map">the tilemap</param>
        /// <param name="position">the position of the tile</param>
        /// <param name="instance">the Tile instance</param>
        /// <returns>a TileBase instance</returns>
        public TileBase? AddUnityTileToListOfTiles(Tilemap map, Vector3Int position, TileBase instance)
        {
            //get the indices for transform, color, flags
            var tIndex = GetTransformMatrixIndex(map, position);
            var cIndex = GetColorIndex(map, position);
            var fIndex = GetFlagsIndex(map, position);

            //create info for this position
            var unityTilePos = new TilePosition(position, tIndex, cIndex, fIndex);

            //is this tile asset already present? common for unity tiles which may be heavily repeated.
            if (workingSet.TryGetValue(instance, out var index))
            {
                //if it is, get its index
                var temp  = m_UnityTiles[index];
                //add to the positions used by this tile
                temp.m_Positions.Add(unityTilePos);
                return null;
            }
            
            //note that originally this method made copies of the unity tiles but this
            //seemed like a waste so it's commented out.
            
            //create a clone of the tile and remove the '(Clone)' from the name
            //var copy = UnityEngine.Object.Instantiate(instance);
            //copy.name = copy.name.Split('(')[0];

            //update data structures
            m_UnityTiles.Add(new UnityTileItem(unityTilePos, instance));
            workingSet.Add(instance, m_UnityTiles.Count - 1);
            return null;// copy;
        }

        /// <summary>
        /// Add a single  tile to the bundle.
        /// </summary>
        /// <param name="instance">TileBase instance</param>
        /// <param name="position">position</param>
        /// <param name="transform">transform</param>
        /// <param name="color">color</param>
        public void AddUnityTileToListOfTiles(TileBase instance, Vector3Int position, Matrix4x4 transform, Color color)
        {
            if(instance == null)
                return;
            var tIndex = 0;
            if (transform == Matrix4x4.identity)
                tIndex = 0;
            else if (workingSetMatrix.TryGetValue(transform, out var indexT))
                tIndex = indexT;
            else
            {
                workingSetMatrix.Add(transform, ++matrixIndex);
                tIndex = matrixIndex;
            }

            var cIndex = 0;
            if (color == Color.white)
                cIndex = 0;
            if(workingSetColor.TryGetValue(color, out var indexC))
                cIndex = indexC;
            else
            {
                workingSetColor.Add(color, ++colorIndex);
                cIndex = colorIndex;
            }
                
            if(workingSetFlags.Count == 0)
                workingSetFlags.Add(TileFlags.None, 0);
            
            //create info for this position
            var unityTilePos = new TilePosition(position, tIndex, cIndex, 0);

            //is this tile asset already present? common for unity tiles which may be heavily repeated.
            if (workingSet.TryGetValue(instance, out var index))
            {
                //if it is, get its index
                var temp = m_UnityTiles[index];
                //add to the positions used by this tile
                temp.m_Positions.Add(unityTilePos);
                return ;
            }
            
            //update data structures
            m_UnityTiles.Add(new UnityTileItem(unityTilePos, instance));
            workingSet.Add(instance, m_UnityTiles.Count - 1);
        }
        
        
        /// <summary>
        /// Reset the GUID. USER CODE SHOULD NEVER DO THIS!
        /// </summary>
        public void ResetGuid()
        {
            m_Guid = null;
        }

        /// <summary>
        /// Get all tiles with their positions. Returns unknown values if bundle isn't sealed yet.
        /// Note that TileBase tiles like Rule tiles and AnimatedTiles aren't returned.
        /// </summary>
        public List<(TileBase tileBase, Vector3Int position, Matrix4x4 transform)>? AllTiles 
        {
            get
            {
                if (m_TileFlags == null) //unsealed
                    return null;
                var list = new List<(TileBase tile, Vector3Int position, Matrix4x4 transform)>();
                
                list.AddRange(m_UnityTiles.Select(t => ((TileBase) t.m_UnityTile,t.m_Positions[0].m_Position, UnityTileTransform(t))));                    
                list.AddRange(m_TilePlusTiles.Select(t => ((TileBase) t.m_Tile, t.m_TilePosition.m_Position, TpTileTransform(t))));
                return list;

                Matrix4x4 UnityTileTransform(UnityTileItem item)
                {
                    return m_TileTransforms != null
                                                 ? m_TileTransforms[item.m_Positions[0].m_TransformIndex]
                                                 : Matrix4x4.identity;
                }

                Matrix4x4 TpTileTransform(TilePlusItem item)
                {
                    return m_TileTransforms != null
                               ? m_TileTransforms[item.m_TilePosition.m_TransformIndex]
                               : Matrix4x4.identity;
                }
                

            }
        }
        
        /// <summary>
        /// When the bundler is finishing building this asset
        /// it calls this method to finalize the flags, color, and
        /// transform matrix arrays.
        /// </summary>
        public void Seal()
        {
            m_TileFlags      = workingSetFlags.Keys.ToArray();
            m_TileColors     = workingSetColor.Keys.ToArray();
            m_TileTransforms = workingSetMatrix.Keys.ToArray();
        }
        
        /// <summary>
        /// Shows GUID
        /// </summary>
        [CustomEditor(typeof(TpTileBundle))]
        public class TpTileBundleEditor : UnityEditor.Editor
        {
            /// <inheritdoc />
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
                var instance = target as TpTileBundle;
                EditorGUILayout.LabelField($"Guid: {instance!.AssetGuidString}");
               
            }
        }
    
        
        #endregion
        
        #endif
    }
}
