// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 01-01-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 01-01-2023
// ***********************************************************************
// <copyright file="SelectionInspectorGui.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Creates the selection inspector</summary>
// ***********************************************************************

using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

#nullable enable
namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// SelectionInspectorGui creates the selection inspector
    /// </summary>
    public static class SelectionInspectorGui
    {
        /// <summary>
        /// Get a size for the thumbnail images.
        /// </summary>
        private static float PreviewSize => TilePlusPainterConfig.PainterPaletteItemImageSize * 1.2f;
        
        private static Texture2D? s_InfoIcon;
        private static GUIStyle?  s_WarningGuiStyle;
        

        /// <summary>
        /// Creates an inspector for a tile
        /// </summary>
        /// <param name="tilemap">The tilemap.</param>
        /// <param name="tile">The tile.</param>
        /// <param name="position">The position.</param>
        /// <param name="autoSave">automatic save?</param>
        /// <param name="isPlaying">Editor in Play mode?</param>
        /// <param name = "noEdit" >Set true when editing should be disallowed eg in a prefab etc.</param>
        /// <param name="forceInspectUnityTiles">force inspection for unity tiles too.</param>
        public static void Gui(Tilemap    tilemap,
                               TileBase   tile,
                               Vector3Int position,
                               bool       autoSave,
                               bool       isPlaying,
                               bool       noEdit,
                               bool       forceInspectUnityTiles = false)
        {
            GUI.skin = TpIconLib.TpSkin;
            if (s_InfoIcon == null)
            {
                s_InfoIcon        = TpIconLib.FindIcon(TpIconType.InfoIcon);
                s_WarningGuiStyle = new GUIStyle {richText = true, imagePosition = ImagePosition.ImageLeft, fontStyle = FontStyle.BoldAndItalic};

            }

            if (tile == null)
            {
                EditorGUILayout.LabelField($"No tile at this position [{position.ToString()}]", EditorStyles.boldLabel);
                return;
            }
            
            // ReSharper disable once Unity.NoNullPatternMatching
            if (tile is TilePlusBase  && (tilemap == null || position == TilePlusBase.ImpossibleGridPosition))
            {
                EditorGUILayout.LabelField($"No tile at this position [{position.ToString()}]", EditorStyles.boldLabel);
                return;
            }
            
            var config = TilePlusConfig.instance;

            // ReSharper disable once Unity.NoNullPatternMatching
            if (tile is TilePlusBase t)
            {
                var isLocked = t.IsLocked;
               
                //common tile information for all TilePlus tiles.
                using (new EditorGUILayout.VerticalScope("Box"))
                {
                    //tile info foldout
                    var showSelInsTileInfo = config.ShowSelectionInspectorTileInfo;
                    var result             = EditorGUILayout.BeginFoldoutHeaderGroup(showSelInsTileInfo, "Basic Info (Read-only)");
                    if (result != showSelInsTileInfo)
                        config.ShowSelectionInspectorTileInfo = result;
                    if (result)
                    {
                        using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
                        {
                            var icon      = TpPreviewUtility.PreviewIcon(tile);
                            int iconWidth = 64;
                            int iconHeight = 64;
                            if (icon != null)
                            {
                                iconWidth  = icon.width;
                                iconHeight = icon.height;
                            }
                            else
                                icon = Texture2D.redTexture;

                            if (iconHeight == 0)
                                iconHeight = 64;
                            if (iconWidth == 0)
                                iconWidth = 64;
                            var ratio        = (float)iconHeight / (float)iconWidth;
                            if (ratio > 2.0f)
                                ratio = 2.0f;
                            else if (ratio < 0.5)
                                ratio = 0.5f;
                            
                            var adjustedSize = PreviewSize * 0.75f;
                            var sz           = ratio < 0 ?  new Vector2(adjustedSize * ratio, adjustedSize) : new Vector2(adjustedSize, adjustedSize * ratio);
                            using (new EditorGUIUtility.IconSizeScope(sz))
                            {
                                GUILayout.Box(TpPreviewUtility.PreviewIcon(tile), GUILayout.MaxHeight(adjustedSize), GUILayout.MaxWidth(adjustedSize));

                            }
                            BasicTileInfoGui.Gui(t, isPlaying);
                        }
                    }

                    EditorGUILayout.EndFoldoutHeaderGroup();
                    
                    //always draw the toolbar
                    InspectorToolbar.instance.DrawToolbar(t,tilemap,position,isPlaying);
                }
                
               
                //custom tile data
                using (new EditorGUILayout.VerticalScope("Box"))
                {
                    if (isLocked)
                    {
                        var path = $"<color=Red>EDITING LOCKED ASSET! Asset Path: {AssetDatabase.GetAssetPath(t)}</color>";
                        EditorGUILayout.LabelField(new GUIContent(path, s_InfoIcon), s_WarningGuiStyle);
                    }

                    //call the gui formatter to create the display
                    var completionState = ImGuiTileEditor.GuiForTilePlus(t, TppInspectorSpec.Selection, tilemap.gameObject.scene,noEdit);
                    GUI.skin = TpIconLib.TpSkin;

                    if (completionState.m_FoundTaggedTile) //note that this means that a tile was found with [tp...] attrs. NOT that it has tags.
                    {
                        var inPrefab = PrefabUtility.IsPartOfPrefabInstance(tilemap.gameObject);

                        //if the user changed something, refresh tile and/or save the scene
                        if (!isLocked && !inPrefab)
                        {
                            if (completionState.m_UpdateTileInstance)
                                TpLib.UpdateInstance(t, completionState.m_FieldNames);

                            if (completionState.m_RefreshTile)
                            {
                                tilemap.RefreshTile(position);
                                if (!isPlaying && autoSave && !completionState.m_InhibitAutoSave)
                                {
                                    if( TpLibEditor.Informational)
                                        TpLib.TpLog("Autosave");
                                    var ps = ((ITilePlus)t).ParentScene;
                                    if (ps.IsValid())
                                        TpEditorUtilities.SaveSceneDelayed(ps); //GridSelection.target.scene);
                                }
                            }
                        }

                        //if tile is can simulate and is simulating and user altered a field then cancel the simulation.
                        // ReSharper disable once Unity.NoNullPatternMatching
                        if (t is ITilePlus { CanSimulate: true, IsSimulating: true } itp && completionState.m_RefreshTile)
                            itp.Simulate(false);
                    }

                    else
                        EditorGUILayout.LabelField("no custom data showing or none available");
                }

                
            }
            
            
            else if(forceInspectUnityTiles || !config.ShowDefaultSelInspector) //a normal Unity tile
            {
                using (new EditorGUILayout.VerticalScope("Box"))
                {
                    var preview = TpPreviewUtility.PreviewIcon(tile);
                    if (preview != null)
                        GUILayout.Box(preview, GUILayout.Height(PreviewSize), GUILayout.MaxWidth(PreviewSize));
                    InspectorToolbar.instance.DrawToolbar(tile, tilemap, position, isPlaying);
                    ImGuiTileEditor.GuiForTilePlus(tile, TppInspectorSpec.Selection, tilemap.gameObject.scene, noEdit, tilemap, position);
                }
            }

            
            GUI.skin = null;
        }
        
    }
    
}
