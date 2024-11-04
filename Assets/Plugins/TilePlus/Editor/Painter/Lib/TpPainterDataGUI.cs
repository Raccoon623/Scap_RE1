// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 11-08-2022
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-31-2022
// ***********************************************************************
// <copyright file="TpPainterDataGUI.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>GUI for inspectors in RIGHTMOST col of Tile+Painter</summary>
// ***********************************************************************

using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using static TilePlus.Editor.TpIconLib;
using static TilePlus.TpLib;
using Object = UnityEngine.Object;

#nullable enable

namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// Static class with GUI for RIGHTMOST col of Tile+Painter.
    /// Note this is IMGUI code
    /// </summary>
    internal static class TpPainterDataGUI
    {
        private static readonly GUIContent s_BrInsFocusGUIContent            = new(FindIcon(TpIconType.UnityOrbitIcon), "Focus Project Window on asset");
        private static readonly GUIContent s_BrInsOpenInEditorGUIContent     = new(FindIcon(TpIconType.UnityTextAssetIcon), "Open Tile script in Editor");
        private static readonly GUIContent s_OpenInInspectorButtonGUIContent = new GUIContent(FindIcon(TpIconType.InfoIcon), "Open in Inspector");
        private static readonly GUIContent s_CreatePickGUIContent = new GUIContent(FindIcon(TpIconType.UnityPickIcon), "Convert to a multiple-tile pick");

        /// <summary>
        /// Get a size for the thumbnail images.
        /// </summary>
        private static float PreviewSize => TilePlusPainterConfig.PainterPaletteItemImageSize;
        
        private const  string                GuiText_UpdatingWhilePlaying    = "Updating while Playing is ON";
        private const  string                GuiText_NotUpdatingWhilePlaying = "Hidden while Playing. Toggle the 'Update in Play' setting to change.";

        private static TilePlusPainterWindow Win          => TilePlusPainterWindow.instance!;
        
       
       
        
        /// <summary>
        /// Used in EDIT mde
        /// </summary>
        internal static void ImitateSelectionInspector()
        {
            if(!TpPainterState.InEditMode)
                return;
            GUI.skin = TpSkin;
            var buttonSize = TilePlusConfig.instance.BrushInspectorButtonSize;

            var clipBd = TpPainterState.Clipboard;
            GUILayout.Space(2);

            if (clipBd is not { Valid: true }  || clipBd.IsEmpty )
            {
                using (new EditorGUILayout.VerticalScope("Box"))
                {
                    EditorGUILayout.HelpBox("No selection: choose a tile from the list in the\nmiddle column or use the PICK tool.", MessageType.Info);
                }
                GUI.skin = null;
                return;
            }
            
            var tile         = clipBd.Tile;
            if (tile == null ) //can happen after ending Play mode
            {
                if(clipBd.SourceTilemap!= null)
                    tile = clipBd.SourceTilemap.GetTile(clipBd.Position);
                else
                {
                    GUI.skin = null;
                    return;
                }
            }
            
            var pos          = clipBd.Position; //invalid when it's a TileBaseSubclass variety
            var tilemap      = clipBd.SourceTilemap;

            var noEdit = false;
            (var noPaintLocked, (_, _, var inPrefab, var inStage)) = TpLibEditor.NoPaint(tilemap);

            if(noPaintLocked || inPrefab)
            {
                noEdit = true;
                var msg = inStage
                              ? "Please don't modify this locked tilemap in a Prefab editing context."
                              : "This Tilemap is in a prefab. DO NOT edit if there are any TilePlus tiles!!!";
                EditorGUILayout.HelpBox(msg, MessageType.Warning);
            }
        
            if (clipBd.WasPickedTile)
                EditorGUILayout.HelpBox($"This tile was picked from the scene @ {clipBd.Position}.", MessageType.Info);
            
            if (Application.isPlaying)
            {
                if (TilePlusPainterConfig.PainterAutoRefresh)
                {
                    EditorGUILayout.HelpBox(GuiText_UpdatingWhilePlaying, MessageType.Warning);
                    EditorGUILayout.Separator();
                }
                else
                {
                    EditorGUILayout.HelpBox(GuiText_NotUpdatingWhilePlaying, MessageType.Warning);
                    GUI.skin = null;
                    return;
                }
            }
            
            
            
            //this means that a TILE asset is the target, this was selected from a list of a tilemap's tiles
            //can't show much since this is just an asset (note that IsTile also includes IsTileBase state
            if (clipBd is { IsNotTilePlusBase: true, WasPickedTile: false } ) 
            {
                using (new EditorGUILayout.VerticalScope("Box"))
                {
                    var asset       = clipBd.Tile;
                    var assetIsNull = asset == null;
                    EditorGUILayout.HelpBox($"Inspecting tile asset [{(assetIsNull ? "???":asset!.name)}]\nTo edit a tile in a scene,\nselect a tile with the PICK tool.", MessageType.None);
                    EditorGUILayout.Separator();

                    GUILayout.Box(assetIsNull
                                      ? TpIconLib.FindIcon(TpIconType.HelpIcon)
                                      : TpPreviewUtility.PreviewIcon(asset!), GUILayout.Height(PreviewSize), GUILayout.MaxWidth(PreviewSize));

                    GUILayout.Space(20);
                    using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
                    {
                        using (new EditorGUIUtility.IconSizeScope(new Vector2(buttonSize, buttonSize)))
                        {
                            if (GUILayout.Button(s_OpenInInspectorButtonGUIContent))
                                TpEditorUtilities.OpenInspectorDelayed(asset, null);
                            if (GUILayout.Button(s_BrInsFocusGUIContent))
                                TpEditorUtilities.FocusProjectWindowDelayed(asset);
                        }
                    }

                    GUI.skin = null;
                    return;
                }
            }
            
            
            //now inspect Tileplus tiles
            SelectionInspectorGui.Gui(tilemap!,
                                      tile,
                                      pos,
                                      TilePlusConfig.instance.AutoSave,
                                      Application.isPlaying,
                                      noEdit);
            GUILayout.Space(2);

            GUI.skin = null;

        }

        /// <summary>
        /// Used in PAINT mode.
        /// </summary>
        internal static void ImitateBrushInspector()
        {
            var clipboard = TpPainterState.Clipboard;         
            GUI.skin = TpSkin;
            GUILayout.Space(2);
            var config = TilePlusConfig.instance;
            if (config == null)
            {
                EditorGUILayout.HelpBox("Can't get configuration.", MessageType.Error);
                GUI.skin = null;
                return;
            }
            var buttonSize   = config.BrushInspectorButtonSize;
            var paintableObj = TpPainterState.PaintableObject;

            if (clipboard is not { Valid: true }) //null or invalid
            {
                using (new EditorGUILayout.VerticalScope("Box"))
                {
                    EditorGUILayout.HelpBox(paintableObj == null
                                                ? "Select a Palette from the center column"
                                                : paintableObj.Count != 0 ?  "Select an item from the above area" : "Empty source" , MessageType.Info);
                }

                if (paintableObj is { Count: 0 } && paintableObj.InspectableAsset != null)
                {
                    var obj = paintableObj.InspectableAsset;
                    using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
                    {
                        using (new EditorGUIUtility.IconSizeScope(new Vector2(buttonSize, buttonSize)))
                        {
                            if (GUILayout.Button(s_OpenInInspectorButtonGUIContent))
                                TpEditorUtilities.OpenInspectorDelayed(obj, null);
                            
                            if (GUILayout.Button(s_BrInsFocusGUIContent))
                                TpEditorUtilities.FocusProjectWindowDelayed(obj);
                                
                        }
                    }
                }
                
                
                GUI.skin = null;
                return;
            }

            if (clipboard.ItemVariety == TpPainterClipboard.Variety.MultipleTilesItem)
            {
                var num = clipboard.Cells != null
                              ? clipboard.Cells.Length.ToString()
                              : "unknown!";
                EditorGUILayout.HelpBox($"Multiple tiles [{num}] selected...", MessageType.Info);
                GUI.skin = null;
                return;
            }
            
            var tile      = clipboard.Tile;
            var wasPicked = clipboard.WasPickedTile;


           


            if (wasPicked)
            {
                using (new EditorGUILayout.VerticalScope("Box"))
                {
                    // EditorGUILayout.HelpBox(tile is TilePlusBase && win.CurrentTool != TpPainterTool.Move
                    EditorGUILayout.HelpBox(clipboard is { IsTilePlusBase: true } &&  TpPainterState.CurrentTool != TpPainterTool.Move
                                                ? "Picked from scene: TilePlus tile in Clipboard will be re-cloned if painted."
                                                : "Picked from scene", MessageType.None);
                }
            }

            if(!wasPicked && clipboard is { ItemVariety: TpPainterClipboard.Variety.TileItem, IsClonedTilePlusBase: true })
                EditorGUILayout.HelpBox("Selected tile is a clone: it'll be be re-cloned if painted.", MessageType.None);

            else if (clipboard.ItemVariety == TpPainterClipboard.Variety.BundleItem)
            {
                var chunk = clipboard.Bundle;
                using (new EditorGUILayout.VerticalScope("Box"))
                {
                    EditorGUILayout.HelpBox($"Inspecting Bundle [{(chunk == null ? "Unknown!" : chunk.name)}]", MessageType.None);

                    
                    using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
                    {
                        using (new EditorGUIUtility.IconSizeScope(new Vector2(buttonSize, buttonSize)))
                        {
                            if (GUILayout.Button(s_OpenInInspectorButtonGUIContent))
                                TpEditorUtilities.OpenInspectorDelayed(chunk, null);
                            
                            if (GUILayout.Button(s_BrInsFocusGUIContent))
                                TpEditorUtilities.FocusProjectWindowDelayed(chunk);

                            if (TilePlusPainterWindow.RawInstance != null)
                            {
                                if (GUILayout.Button(s_CreatePickGUIContent))
                                    DelayedCallback(Win,()=>ConvertBundleToPick(chunk),"T+P: Bundle->Pick");
                            }



                        }
                    }
                }
                GUI.skin = null;
                return;
            }
            
            else if (clipboard.ItemVariety == TpPainterClipboard.Variety.PrefabItem)
            {
                using (new EditorGUILayout.VerticalScope("Box"))
                {
                    var prefab = clipboard.Prefab;
                    EditorGUILayout.HelpBox($"Inspecting a Prefab [{(prefab == null ? "Unknown!": prefab.name)}]", MessageType.None);

                    if (prefab != null)
                    {
                        (var preview, _) = TpPreviewUtility.PreviewGameObject(prefab);
                        if (preview != null)
                        {
                            using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
                            {
                                GUILayout.Box(preview, GUILayout.Height(PreviewSize), GUILayout.MaxWidth(PreviewSize));
                            }
                        }
                        using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
                        {
                            using (new EditorGUIUtility.IconSizeScope(new Vector2(buttonSize, buttonSize)))
                            {
                                if (GUILayout.Button(s_OpenInInspectorButtonGUIContent))
                                    TpEditorUtilities.OpenInspectorDelayed(prefab, null);
                            
                                if (GUILayout.Button(s_BrInsFocusGUIContent))
                                    TpEditorUtilities.FocusProjectWindowDelayed(prefab);
                                
                            }
                        }
                    }
                }
                GUI.skin = null;
                return;
            }

            else if (clipboard.ItemVariety == TpPainterClipboard.Variety.TileFabItem)
            {
                var fab = clipboard.TileFab;
                using (new EditorGUILayout.VerticalScope("Box"))
                {
                    EditorGUILayout.HelpBox($"Inspecting a TileFab [{(fab == null ? "Unknown!": fab.name)}]", MessageType.None);

                    using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
                    {
                        using (new EditorGUIUtility.IconSizeScope(new Vector2(buttonSize, buttonSize)))
                        {
                            if (GUILayout.Button(s_OpenInInspectorButtonGUIContent))
                                TpEditorUtilities.OpenInspectorDelayed(fab, null);
                            
                            if (GUILayout.Button(s_BrInsFocusGUIContent))
                                TpEditorUtilities.FocusProjectWindowDelayed(fab);
                                
                        }
                    }
                }
                GUI.skin = null;
                return;
            }

            //so it's a tile of some sort: Tile, TileBase, TilePlus. Which is it???
            if (clipboard is { IsTilePlusBase:true, ITilePlusInstance: not null })
            {   
                GameObject? prefab            = null;
                var         hasPrefab         = false;
                var         t                 = clipboard.Tile as Tile;
                var         id                = clipboard.Id;
                var         iTilePlusInstance = clipboard.ITilePlusInstance;
                
                if (iTilePlusInstance.InstantiatedGameObject != null)
                {
                    prefab    = iTilePlusInstance.InstantiatedGameObject;
                    hasPrefab = prefab != null;
                }

                const int truncateAt = 40;
                EditorGUILayout.BeginHorizontal();
                using (new EditorGUILayout.VerticalScope("Box"))
                {
                    var prefabInfo = string.Empty;
                    if (hasPrefab)
                        prefabInfo = $"\nPrefab: {prefab!.name}";

                    var displayString = $"Name: {iTilePlusInstance.TileName}  {(wasPicked ? "ID:" : string.Empty)} {(wasPicked ? id : string.Empty)}{prefabInfo} ";
                    var desc          = iTilePlusInstance.Description;
                    if (desc == string.Empty)
                        desc = "Description: missing";
                    else if (desc.Length > truncateAt)
                        desc = desc.Substring(0, truncateAt - 1);
                    displayString += $"\nDescription:{desc}";
                    displayString += $"\nClearmode: {iTilePlusInstance.TileSpriteClear.ToString()}, ColliderMode: {iTilePlusInstance.TileColliderMode.ToString()}";
                    if (!string.IsNullOrEmpty(iTilePlusInstance.CustomTileInfo))
                        displayString += $"\nCustomInfo: {iTilePlusInstance.CustomTileInfo}";
                    displayString += $"\nFlags: Lock Color [{(t!.flags & TileFlags.LockColor) != 0}], Lock Transform[{(t.flags & TileFlags.LockTransform) != 0}]";
                    var guiContent = new GUIContent(displayString) { tooltip = "Basic info about the tile selected in the palette. First line: Name (Type) [State]" };

                    EditorGUILayout.HelpBox(guiContent);
                    
                    using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
                    {
                        using (new EditorGUIUtility.IconSizeScope(new Vector2(buttonSize, buttonSize)))
                        {
                            if (GUILayout.Button(s_OpenInInspectorButtonGUIContent))
                                TpEditorUtilities.OpenInspectorDelayed(tile, null);
                            
                            if (GUILayout.Button(s_BrInsFocusGUIContent))
                                TpEditorUtilities.FocusProjectWindowDelayed(tile);
                            if (GUILayout.Button(s_BrInsOpenInEditorGUIContent))
                                TpEditorUtilities.OpenInIdeDelayed(tile);
                        }
                    }

                    //show the custom content
                    using (new EditorGUILayout.VerticalScope("Box"))
                    {
                        var obj             = (Object)((TilePlusBase)iTilePlusInstance);
                        var completionState = ImGuiTileEditor.GuiForTilePlus(obj,
                                                                             TppInspectorSpec.Brush,
                                                                             iTilePlusInstance.ParentScene,
                                                                             false);
                        GUI.skin = TpSkin;
                        if (!completionState.m_FoundTaggedTile)
                            EditorGUILayout.LabelField("no custom data");
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
            else //and also end up here if a tile in a Palette, Bundle, etc
            {
                if (tile != null)
                {
                    var typ = tile.GetType();
                    using (new EditorGUILayout.VerticalScope("Box"))
                    {
                        EditorGUILayout.LabelField($"Normal tile asset ('{tile.name}')\nType [{typ}]",EditorStyles.wordWrappedLabel);
                        // ReSharper disable once Unity.NoNullPatternMatching
                        if(tile!=null && tile is  Tile t)
                            EditorGUILayout.LabelField($"Flags: Lock Color [{(t.flags & TileFlags.LockColor) != 0}]\nLock Transform[{(t.flags & TileFlags.LockTransform) != 0}]", EditorStyles.wordWrappedLabel);
                        EditorGUILayout.Space(2f);

                        using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
                        {
                            using (new EditorGUIUtility.IconSizeScope(new Vector2(buttonSize, buttonSize)))
                            {
                                if (GUILayout.Button(s_OpenInInspectorButtonGUIContent))
                                    TpEditorUtilities.OpenInspectorDelayed(tile, null);
                                if (GUILayout.Button(s_BrInsFocusGUIContent))
                                    TpEditorUtilities.FocusProjectWindowDelayed(tile);
                            }
                        }
                    }
                }
                else
                {
                    using (new EditorGUILayout.VerticalScope("Box"))
                    {
                        EditorGUILayout.HelpBox("No tile selected or Null tile!", MessageType.Warning, true);
                        EditorGUILayout.Space(2f);
                    }
                }
            }

            EditorGUILayout.Separator();
            GUI.skin = null;
        }


        private static readonly GUIContent s_Notification = new GUIContent("Bundle must be of a defined area and not the entire Tilemap to use this feature", TpIconLib.FindIcon(TpIconType.InfoIcon));
        private static void ConvertBundleToPick(TpTileBundle? bundle)
        {
            if(bundle == null)
                return;

            if (!bundle.m_FromGridSelection)
            {
                Win.ShowNotification(s_Notification);
                return;
            }
            
            bundle.ClearCache();
            var tileSet = bundle.Tileset(TpTileBundle.TilemapRotation.Zero, FabOrBundleLoadFlags.NoClone);
            
            if (tileSet.Count == 0)
                return;

            var bundleBounds = bundle.m_TilemapBoundsInt;
            var sizeX        = bundleBounds.size.x;
            var sizeY        = bundleBounds.size.y;
            var num          = sizeX * sizeY;
            
            var output = new TileCell[num];  //it's important that the size of the array matches the number of items in the Bundle.
            foreach (var pos in bundleBounds.allPositionsWithin)
            {
                var address = (pos.x % sizeX) + sizeX * (pos.y % sizeY);
                output[address] = new TileCell(null, new Vector3Int(pos.x, pos.y), Color.clear, Matrix4x4.identity);
            }

            foreach (var item in tileSet)
            {
                var tile = item.m_Tile;
                var address = (item.m_Position.x % sizeX) + sizeX * (item.m_Position.y % sizeY);
                output[address] = new TileCell(tile, item.m_Position, item.m_Color, item.m_TransformMatrix);
            }

            var num2 = output.Length;
            if (num2 == 0)
            {
                if(Warnings)
                    TpLogWarning("Post-evaluation Selection was empty when converting Bundle to a pick");
                return;
            }
            
            if (num2 == 1)
            {
                if(Informational)
                    TpLog("Post-evaluation Selection was too small (must be > 1 tile) when converting Bundle to a pick");
                return;
            }
           
            var ttd = new TpPainterClipboard(output,
                                             new BoundsInt(Vector3Int.zero, bundle.m_TilemapBoundsInt.size), bundle.m_TilemapBoundsInt, true,true);
            Win.SelectBrushInspectorTarget(ttd, false);
        }

        
    }
}
