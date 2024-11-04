// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-20-2022
// ***********************************************************************
// <copyright file="InspectorToolbar.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Generates IMGUI inspector toolbars</summary>
// ***********************************************************************

using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using static TilePlus.Editor.TpIconLib;
using Object = UnityEngine.Object;

// ReSharper disable InconsistentNaming

#nullable enable

namespace TilePlus.Editor.Painter
{
    /// <summary>
    ///     This class draws the toolbar for the Selection Inspector.
    ///     Also used by TilePlus Painter.
    /// </summary>
    public  class InspectorToolbar : ScriptableSingleton<InspectorToolbar>
    {
        private void OnDisable()
        {
            if(TpLibEditor.Informational)
                TpLib.TpLog("InspectorToolbar Scriptable Singleton released");

        }


        private const  float   ButtonSizeScale = 0.9f;
        
        [SerializeField]
        private  Vector2 buttonSize    = new(15, 15);
        [SerializeField]
        private  bool    iconsInitialized;
        [SerializeField]
        private GUIContent? focusButtonGuiContent;
        [SerializeField]
        private GUIContent? saveCloneAsNewAssetButtonGUIContent;
        [SerializeField]
        private GUIContent? openPrefabButtonGUIContent;
        [SerializeField]
        private GUIContent? refreshButtonGUIContent;
        [SerializeField]
        private GUIContent? deleteButtonGUIContent;
        [SerializeField]
        private GUIContent? simButtonRunning;
        [SerializeField]
        private GUIContent? simButtonStopped;
        [SerializeField]
        private GUIContent? collapseAllGUIContent;
        [SerializeField]
        private GUIContent? expandAllGUIContent;
        [SerializeField]
        private GUIContent? showClassInfo;
        [SerializeField]
        private GUIContent? hideClassInfo;
        [SerializeField]
        private GUIContent? openInInspectorButtonGUIContent;
        [SerializeField]
        private GUIContent? guidCopyGuiContent;    
        
        private  bool ConfirmDelete =>  TilePlusConfig.instance.ConfirmDeleteTile;
        private  float HighlightTime => TilePlusConfig.instance.TileHighlightTime;

        /// <summary>
        ///     Draw the toolbar
        /// </summary>
        /// <param name="tileBase">tile instance</param>
        /// <param name="tilemap">parent tilemap</param>
        /// <param name="position">position of the tile</param>
        /// <param name="isPlaying">is editor in play mode</param>
        public  void DrawToolbar(TileBase tileBase,
                                       Tilemap      tilemap,
                                       Vector3Int   position,
                                       bool         isPlaying)
        {
            if(TilePlusConfig.instance == null)
                return;
            var t          = tileBase as ITilePlus;
            var isTilePlus =  t != null;
            var asTpt      = tileBase as TilePlusBase;
            var isTpt      = asTpt != null;
            if (!isTpt)
                isTilePlus = false;

            var isLocked = isTilePlus && TpLib.IsTilemapLocked(tilemap);
            
            var bSize = TilePlusConfig.instance.SelInspectorButtonSize * ButtonSizeScale;
            this.buttonSize.x       = bSize;
            this.buttonSize.y       = bSize;
            if(!iconsInitialized)
                InitIcons();

            using (new EditorGUIUtility.IconSizeScope(buttonSize))
            {
                using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
                {
                    if (GUILayout.Button(focusButtonGuiContent))
                    {
                        if (isTilePlus)
                        {
                            TpLibEditor.FocusOnTile(t!.TileWorldPosition);
                            TpEditorUtilities.MakeHighlight(t, HighlightTime * 2f);
                        }

                        else
                        {
                            var pos = tilemap.CellToWorld(position);
                            TpLibEditor.FocusOnTile(pos);
                            TpEditorUtilities.MakeHighlight(position,tilemap, HighlightTime * 2f);
                        }
                    }

                    if (!isLocked || TilePlusConfig.instance.AllowPrefabEditing)
                    {
                        if (GUILayout.Button(openInInspectorButtonGUIContent))
                            TpEditorUtilities.OpenInspectorDelayed(tileBase, null);
                    }
                    else
                    {
                        if (GUILayout.Button(openInInspectorButtonGUIContent))
                            TpEditorUtilities.OpenInspectorDelayed(tileBase, null);
                    }

                    if (!isPlaying && isTilePlus)
                    {
                        if (!isLocked && GUILayout.Button(saveCloneAsNewAssetButtonGUIContent))
                        {
                            EndSimCheck(t!);
                            TpEditorUtilities.SaveTileToAssetDatabase_Delayed(asTpt!);
                        }

                        //two possibilities: if flags set for intantiateRuntimeOnly then show the ASSET
                        var tileGo =
                            (asTpt!.flags & TileFlags.InstantiateGameObjectRuntimeOnly) != 0
                                         ? asTpt.gameObject
                                         : t!.InstantiatedGameObject;

                        if (tileGo != null)
                        {
                            if (GUILayout.Button(openPrefabButtonGUIContent))
                            {
                                EndSimCheck(t!);
                                TpEditorUtilities.OpenInspectorDelayed(tileGo, null);
                            }
                        }
                    

                        if (GUILayout.Button(refreshButtonGUIContent))
                        {
                            EndSimCheck(t!);
                            TpEditorUtilities.RefreshTileDelayed(tilemap, position);
                        }

                        if (!t!.IsLocked)
                        {
                            if (GUILayout.Button(deleteButtonGUIContent))
                            {
                                EndSimCheck(t);
                                if (tilemap == null)
                                    return;
                                
                                TpLib.DelayedCallback(null,()=> DeleteTileAction((TilePlusBase) t),"Inspector Toolbar Delete Tile");
                                return;
                            }
                        }

                        if (t is { CanSimulate: true, IsLocked: false })
                        {
                            var simButtonGuiContent = t.IsSimulating ? simButtonRunning : simButtonStopped;

                            if (GUILayout.Button(simButtonGuiContent) && t.ParentTilemap != null)
                                t.Simulate(!t.IsSimulating);
                        }
                        
                        if(GUILayout.Button(guidCopyGuiContent))
                            TpEditorUtilities.CopyGuidFromTile(t as TilePlusBase);
                        
                    }

                    if (isTilePlus)
                    {
                        if (TilePlusConfig.instance.ClassHeaders)
                        {
                            if (GUILayout.Button(collapseAllGUIContent))
                                TilePlusConfig.instance.ClearFoldoutPrefsFor(ImGuiTileEditor.CurrentClassNames);
                            if (GUILayout.Button(expandAllGUIContent))
                                TilePlusConfig.instance.SetFoldoutPrefsFor(ImGuiTileEditor.CurrentClassNames);
                            if (!GUILayout.Button(hideClassInfo))
                                return;
                            TilePlusConfig.instance.ClassHeaders = false;
                            ImGuiTileEditor.ChangeClassHeaders(false);
                        }
                        else
                        {
                            if (!GUILayout.Button(showClassInfo))
                                return;
                            TilePlusConfig.instance.ClassHeaders = true;
                            ImGuiTileEditor.ChangeClassHeaders(true);
                        }
                    }
                } //end of toolbar
            }     //end of iconsizescope
        }

        private  void EndSimCheck(ITilePlus t)
        {
            if (t is { CanSimulate: true, IsSimulating: true })
                t.Simulate(false);
        }


        private  void DeleteTileAction(TilePlusBase tile)
        {
            var map = tile.ParentTilemap;
            if (map == null)
            {
                if(TpLibEditor.Errors)
                    TpLib.TpLogError("Null tilemap for tile passed to InspectorToolbar.DeleteTileAction");
                return;
            }
            
            if (ConfirmDelete)
            {
                var doDelete = EditorUtility.DisplayDialog("Delete this tile?", "Do you really want to delete this tile?", "OK", "NOPE");
                if (!doDelete)
                    return;
            }

            
            Undo.RegisterCompleteObjectUndo(new Object[] {map, tile.ParentTilemap!.gameObject}, GridBrushBase.Tool.Erase + "_TPP");
            TpLib.DeleteTile(map, tile.TileGridPosition);
        }


        private  void InitIcons()
        {
            iconsInitialized                      = true;
            focusButtonGuiContent               = new(FindIcon(TpIconType.UnityCameraIcon),"Focus on tile (use Config Editor to set zoom)");
            saveCloneAsNewAssetButtonGUIContent = new(FindIcon(TpIconType.UnitySaveIcon),"Save tile to project as new asset");
            openPrefabButtonGUIContent          = new(FindIcon(TpIconType.PrefabIcon), "Open Tile's prefab in Inspector");
            refreshButtonGUIContent             = new(FindIcon(TpIconType.UnityRefreshIcon), "Refresh tile");
            deleteButtonGUIContent              = new(FindIcon(TpIconType.TrashIcon), "Delete Tile from Tilemap");
            simButtonRunning                    = new(FindIcon(TpIconType.UnityRecordIcon),  "Stop simulation");
            simButtonStopped                    = new(FindIcon(TpIconType.UnityForwardIcon),"Start simulation");
            collapseAllGUIContent               = new(FindIcon(TpIconType.ArrowDown), "Collapse All");
            expandAllGUIContent                 = new(FindIcon(TpIconType.ArrowUp), "Expand All");
            showClassInfo                       = new(FindIcon(TpIconType.PlusIcon), "Show Class info");
            hideClassInfo                       = new(FindIcon(TpIconType.MinusIcon), "Hide class info");
            openInInspectorButtonGUIContent     = new(FindIcon(TpIconType.InfoIcon), "Open in Inspector");
            guidCopyGuiContent                  = new(FindIcon(TpIconType.UnityEyedropperIcon), "Copy GUID to clipboard");
        }
        
    }
}
