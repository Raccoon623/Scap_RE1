// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-18-2022
// ***********************************************************************
// <copyright file="TpEditorUtilities.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Editor Utilities for TilePlus Toolkit</summary>
// ***********************************************************************
#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using static TilePlus.Editor.TpLibEditor;
using static TilePlus.TpLib;
using Object = UnityEngine.Object;

namespace TilePlus.Editor
{
    /// <summary>
    ///     Utilities used in-editor only
    /// </summary>
    [InitializeOnLoad]
    public static class TpEditorUtilities
    {
        
        #region fields
        
        //a queue for delayed actions. Used ONLY by methods of this class. See TpTaskManager for other ways to delay.
        private static readonly Queue<Action> s_Tasks = new(8);

        private static          bool          s_SaveRequestQueued;
        private static          string        s_LastPath = string.Empty;

        #endregion

        #region properties

        /// <summary>
        /// A property that indicates whether or not the AllowPaintingOverwrite shortcut key is pressed.
        /// </summary>
        public static bool AllowPaintingOverwrite { get; private set; }

        /// <summary>
        /// Get the abbreviation for system language. Currently only  EN is supported.
        /// </summary>
        public static string SystemLanguageAbbreviation { get; private set; }
        

        #endregion


        #region events
        /// <summary>
        /// Subscribe to this to be notified when a setting changes.
        /// </summary>
        public static event Action<string, ConfigChangeInfo>? RefreshOnSettingsChange;

        /// <summary>
        /// Subscribe to this to be notified when the ClearAllSelectedTilemaps menu item has been used.
        /// </summary>
        public static event Action? RefreshOnTilemapsCleared;

        #endregion

        #region Ctor
        
        //constructor just sets up a callback
        static TpEditorUtilities()
        {
            RefreshOnSettingsChange  =  null;
            s_Tasks.Clear();
            EditorApplication.update += EditorUpdate;
            
            //currently only EN is supported
            SystemLanguageAbbreviation = "en";
            var lang = Application.systemLanguage;
            if (lang != SystemLanguage.English)
            {
                Debug.LogWarning("Unsupported language selected");
            }
        }

        //this method is invoked when the Editor begins play mode
        /// due to the InitializeOnEnterPlayMode attribute.
        [InitializeOnEnterPlayMode]
        private static void ResetOnEnterPlayMode(EnterPlayModeOptions playModeOptions)
        {
            if ((playModeOptions & EnterPlayModeOptions.DisableDomainReload) == 0)
                return;
            s_Tasks.Clear();
        }

        
        #endregion
        
        #region taskQueue
        private static void EditorUpdate()
        {
            if (s_Tasks.Count == 0)
                return;
            var task = s_Tasks.Dequeue();
            task?.Invoke();

        }
        
        #endregion

        
        #region utils

        
        /// <summary>
        ///Open an inspector delayed. Used from within editor code where opening an 
        ///inspector directly from that code would cause all sorts of GUI errors. 
        /// </summary>
        /// <param name="obj">Object to inspect</param>
        /// <param name="context">context Object</param>
        /// <param name="useEvent">consume the current event</param>
        public static void OpenInspectorDelayed(Object? obj, Object? context, bool useEvent = true)
        {
            if (obj == null)
                return;

            if (useEvent)
                Event.current.Use();
           
            EditorUtility.OpenPropertyEditor(obj);
        }


        /// <summary>
        /// Used to propogate editor-time settings changes from TilePlusConfig to editor windows like TilePlusViewer.
        /// </summary>
        /// <param name="change">specified what changed</param>
        /// <param name = "info" >class instance with new and old values as object</param>
        public static void SettingHasChanged(string change, ConfigChangeInfo info)
        {
            RefreshOnSettingsChange?.Invoke(change, info);
        }
        
        
        /// <summary>
        /// Copy the GUID from a tile instance to the system clipboard
        /// </summary>
        /// <param name="instance"></param>
        public static void CopyGuidFromTile(TilePlusBase? instance)
        {
            if (instance == null)
                return;
            var guid = instance.TileGuidString;
            EditorGUIUtility.systemCopyBuffer = guid;
        }
        
        

        /// <summary>
        /// Perform undo after a delay
        /// </summary>
        public static void PerformUndoDelayed()
        {
            s_Tasks.Enqueue(Undo.PerformUndo);
        }

        /// <summary>
        /// Focus the project window after a delay.
        /// </summary>
        /// <param name="obj">Object to focus on</param>
        public static void FocusProjectWindowDelayed(Object? obj)
        {
            if(obj == null)
                return;
            var o = obj;
            s_Tasks.Enqueue(() =>
            {
                Selection.activeInstanceID = o.GetInstanceID();
                EditorUtility.FocusProjectWindow();
            });
        }

        /// <summary>
        /// Exit a prefab editing context
        /// </summary>
        public static void ReturnToMainStageDelayed()
        {
            s_Tasks.Enqueue(StageUtility.GoToMainStage);
        }
        

        /// <summary>
        /// force a scripting reload
        /// </summary>
        public static void ForceHotReloadDelayed()
        {
            s_Tasks.Enqueue(EditorUtility.RequestScriptReload);
        }

        //nb improved reliability of opening in proper IDE vs just notepad.
        /// <summary>
        /// Open the ScriptableObject in your IDE
        /// </summary>
        /// <param name="obj">Scriptable Object to open in the IDE</param>
        public static void OpenInIdeDelayed(ScriptableObject? obj)
        {
            var sObjScript = MonoScript.FromScriptableObject(obj);
            if (sObjScript != null)
                s_Tasks.Enqueue(() => AssetDatabase.OpenAsset(sObjScript));
        }


        
        /// <summary>
        /// Save the scene after a delay.
        /// </summary>
        /// <param name="scene">the scene to save</param>
        public static void SaveSceneDelayed(Scene scene)
        {
            //if() allows editing inside a prefab stage- inhibits scene saving after ImGuiTileEditor changes.
            //also inhibits during editor-play
            if (Application.isPlaying || PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                if(TpLibEditor.Informational)
                    TpLog("Ignoring save-scene request when the Editor is in PLAY mode...");
                return;
            }

            if (s_SaveRequestQueued)
                return;   
            s_SaveRequestQueued = true;
            s_Tasks.Enqueue(() => SaveScene(scene));
        }

        private static void SaveScene(Scene scene)
        {
            s_SaveRequestQueued = false;
            EditorSceneManager.SaveScene(scene);
        }
        

        /// <summary>
        /// Refresh a tile after a delay
        /// </summary>
        /// <param name="map"></param>
        /// <param name="pos"></param>
        public static void RefreshTileDelayed(Tilemap map, Vector3Int pos)
        {
            s_Tasks.Enqueue(() => map.RefreshTile(pos));
        }

        /// <summary>
        /// Save tile as new asset after a delay
        /// </summary>
        /// <param name="tileToSave">tile to save</param>
        public static void SaveTileToAssetDatabase_Delayed(TilePlusBase tileToSave)
        {
            s_Tasks.Enqueue(() => SaveTileAsNewAsset(tileToSave));
        }


        private static void SaveTileAsNewAsset(TilePlusBase tile)
        {
            var path = TpEditorUtilities.GetPathFromUser("Select destination folder for new tile asset.");
            if (path == string.Empty)
                return;
            
            //create an appropriate filename
            var filename = tile.name.Split('(')[0];
            var version  = ++tile.Version;

            //clone the tile
            var tileToSave = Object.Instantiate(tile);
            //reset it and change to asset state
            tileToSave.ChangeTileState(TileResetOperation.MakeNormalAsset);

            //create final asset path
            var initialPath = $"{path}/{filename}[V{version.ToString()}].asset";

            //ensure it's unique
            var objPath = AssetDatabase.GenerateUniqueAssetPath(initialPath);

            //create it and save it.
            AssetDatabase.CreateAsset(tileToSave, objPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }


        

        /// <summary>
        /// Get the Tp IMGUI skin
        /// </summary>
        /// <returns>the skin. Note that this is not cached, so the caller should since this doesn't change</returns>
        public static GUISkin GetSkin()
        {
            return Resources.Load<GUISkin>(EditorGUIUtility.isProSkin ? "TilePlus/TpGuiSkinPro" : "TilePlus/TpGuiSkin");
        }
        

        /// <summary>
        /// Make a box around a TilePlus tile. Ignored when editor is in Play mode
        /// </summary>
        /// <param name="target">tile instance</param>
        /// <param name = "highlightTime" >timeout for marquee</param>
        public static void MakeHighlight(ITilePlus? target, float highlightTime)
        {
            if (target == null)
                return;
            
            var map = target.ParentTilemap;
            if (map == null)
                return;
            
            var grid = GetParentGrid(map.transform);
            if (grid == null)
                return;

            
            TilemapMarquee(grid, new BoundsInt(target.TileGridPosition, Vector3Int.one), GizmoColor, highlightTime);

            
        }

        /// <summary>
        /// Make a box around a Unity tile. Ignored when editor is in Play mode
        /// </summary>
        /// <param name = "parentMap" >Parent tilemap for the tile (the map it is on)</param>
        /// <param name = "highlightTime" >timeout for marquee</param>
        /// <param name = "position" >Grid position of the Tile</param>
        public static void MakeHighlight(Vector3Int position, Tilemap? parentMap,  float highlightTime)
        {
            if (parentMap == null)
                return;
            
            var grid = GetParentGrid(parentMap.transform);
            if(grid == null)
                return;
            
            TilemapMarquee(grid, new BoundsInt(position, Vector3Int.one), GizmoColor, highlightTime);
        }
        
        #endregion
        
        #region shortcuts
        
        /// <summary>
        /// Validator for 'Clear Tilemaps' command
        /// </summary>
        /// <returns>true if GO has grid or tilemap component OR if there's a NOPAINT component on the grid or any tilemaps.</returns>
        [MenuItem("Tools/TilePlus/Clear Selected Tilemaps", true, 101)]
        [MenuItem("GameObject/TilePlus Clear Tilemaps",true,1001)]
        public static bool ClearMapsValidate()
        {
            if (Selection.activeTransform == null)
                return false;
            var go = Selection.activeGameObject;
            if (PrefabUtility.IsPartOfAnyPrefab(go))
                return false;
            //if selection isn't a Grid or a Tilemap return false.
            var isGrid = go.TryGetComponent<Grid>(out var grid);
            var isMap = go.TryGetComponent<Tilemap>(out var tmap);
            
            //if selection isn't a grid or a map then validation fails. 
            if (!(isGrid || isMap)) 
                return false;
            
            //if selection is a grid but the grid has the TPNOPAINT component then return false
            if(isGrid && grid!.TryGetComponent<TpNoPaint>(out var _))
                return false;
            //if selection is a tilemap but the map has the TPNOPAINT component then return false.
            if (isMap && tmap!.TryGetComponent<TpNoPaint>(out var _))
                return false;
            
            //at this point we know that it isn't a grid with NOPAINT nor
            //is it a map with NoPaint. 
            
            //now check:  if the selection's a grid check the child tilemaps
            if (isMap) //if it's a map then PASS
                return true;
            var maps = go.GetComponentsInChildren<Tilemap>();
            if (maps == null) //if no child tilemaps can't do anything
                return false; //unlikely fail: grid w/o child maps??
            //otherwise test each map for TPNOPAINT
            foreach (var map in maps)
            {
                if (map.TryGetComponent<TpNoPaint>(out var _))
                    return false;
            }
            return true;
        }

       
        
        
        /// <summary>
        /// Menu item handler for Clear tilemaps GO menu item
        /// </summary>
        [MenuItem("GameObject/TilePlus Clear Tilemaps",false,1001)]
        [MenuItem("Tools/TilePlus/Clear Selected Tilemaps", false, 101)]
        private static void ClearAllSelectedTilemaps()
        {
            GameObject? go;
            
            var hasGridSelection = GridSelection.active;
            if (hasGridSelection && GridSelection.position.size != Vector3Int.zero && GridSelection.grid != null)   
                go = GridSelection.grid.gameObject;
            else
                go = Selection.activeTransform != null ? Selection.activeTransform.gameObject : null;
            
            if (go == null)
                return;
            ClearSelectedTilemaps(go,hasGridSelection ? GridSelection.position : (BoundsInt?)null);
        }

        /// <summary>
        /// Clear tile or tiles+prefabs from an area or all maps
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="area"></param>
        /// <param name = "forceUseGrid" ></param>
        internal static void ClearSelectedTilemaps(GameObject parent, BoundsInt? area, bool forceUseGrid = false )
        {
            var fromGridSelection = (forceUseGrid && area.HasValue) || (parent.TryGetComponent<Grid>(out _) && area.HasValue);
            
            if (EditorSceneManager.IsPreviewSceneObject(parent))
            {
                EditorUtility.DisplayDialog("SORRY!", "You can't do this in a prefab editing context.", "Continue");
                return;
            }

            if (PrefabUtility.IsAnyPrefabInstanceRoot(parent) || PrefabUtility.IsPartOfAnyPrefab(parent))
            {
                EditorUtility.DisplayDialog("SORRY!", "You can't do this to a prefab", "Continue");
                return;
            }

            //get the tilemaps
            var maps = parent.GetComponentsInChildren<Tilemap>();
            if (maps.Length == 0) //no selected maps
            {
                EditorUtility.DisplayDialog("SORRY!", "This function requires a single Tilemap or a Grid with child Tilemaps", "Continue");
                return;
            }

            var msg = fromGridSelection
                          ? $"About to delete all tiles and child GameObjects from the Grid [{parent.name}] Tilemaps using a Grid Selection with position {area!.Value.position.ToString()} of size {area.Value.size.ToString()}. Proceed?"
                          : "About to delete all tiles and child GameObjects from Tilemaps.  Proceed?";
            
            if (!EditorUtility.DisplayDialog("Ready?", msg, "Yep", "Nope"))
                return;

            var tileDelCount        = 0;
            var prefabDelCount      = 0;
            var totalPositions      = 0;
            var matchingGameObjectTransforms = new List<Transform>();
            if (fromGridSelection)
            {
                var boundsInt    = area!.Value;  //area can't logically be null here in spite of warnings 
               
                foreach (var map in maps)
                {
                    RegisterUndo(map, "Clearing all tiles and parented GameObjects from Grid Selection");
                    map.ClearAllEditorPreviewTiles();
                    var trans      = map.transform;
                    var numPrefabs = trans.childCount;
                    for(var i = 0; i < numPrefabs; i++)
                    {
                        var t  = trans.GetChild(i);
                        var tPos = map.WorldToCell(t.position);
                        if(!boundsInt.Contains(tPos))
                               continue;
                        matchingGameObjectTransforms.Add(t);
                        prefabDelCount++;
                    }
                    foreach (var pos in boundsInt.allPositionsWithin)
                    {
                        totalPositions++;
                        if (!map.HasTile(pos))
                            continue;
                        map.SetTile(pos, null);
                        tileDelCount++;
                    }
                }
                foreach(var t in matchingGameObjectTransforms)
                    Undo.DestroyObjectImmediate(t.gameObject);
                
                if(TpLibEditor.Informational)
                    Debug.Log($"{totalPositions} Grid Locations examined: {prefabDelCount} prefab(s) deleted, {tileDelCount} tile(s) deleted");
            }
            
            else
            {
                // ReSharper disable once LoopCanBePartlyConvertedToQuery
                foreach (var map in maps)
                {   
                    if(map == null)
                        continue;
                    RegisterUndo(map, "Clearing all tiles and parented GameObjects");
                    map.ClearAllTiles();
                    map.ClearAllEditorPreviewTiles();
                    map.CompressBounds();
                    
                    var trans      = map.transform;
                    for (var i = 0; i < trans.childCount; i++)
                    {
                        var t = trans.GetChild(i);
                        matchingGameObjectTransforms.Add(t);
                    }
                }
                foreach(var t in matchingGameObjectTransforms)
                    Undo.DestroyObjectImmediate(t.gameObject);
            }
            

            RefreshOnTilemapsCleared?.Invoke();

        }
        
        //note if the ID changes, see TpPainterMiniButtons.cs and update there as well.
        [ClutchShortcut("TilePlus/Painter: Overwrite protection override [C]", KeyCode.Alpha1)]
        private static void PaintOverwriteHandler(ShortcutArguments args)
        {
            AllowPaintingOverwrite = args.stage switch
                                     {
                                         ShortcutStage.Begin => true,
                                         ShortcutStage.End   => false,
                                         _                   => AllowPaintingOverwrite
                                     };
        }

        
        

        /// <summary>
        /// Refresh TpLib menu command
        /// </summary>
        [MenuItem("Tools/TilePlus/Refresh System", false, 6)]
        public static void RefreshTpLib()
        {
            SceneScan();
        }


        [MenuItem("Tools/TilePlus/Utilities/Update TileFab Asset GUIDs", false, 100)]
        [SuppressMessage("ReSharper", "Unity.NoNullPatternMatching")]
        private static void UpdateAssetGuids()
        {
            var targets = Selection.objects;
            if (targets.Length == 0)
            {
                EditorUtility.DisplayDialog("SORRY!", "Nothing Selected!", "Exit");
                return;
            }

            var correctTypes = true;
            foreach (var t in targets)
            {
                if(!t)
                    continue;
                if(t is TpTileBundle or TpTileFab)
                    continue;
                correctTypes = false;
                break;
            }

            if (!correctTypes)
            {
                EditorUtility.DisplayDialog("SORRY!", "Objects in selection can only include TpTileBundle and TpTilefab.", "Exit");
                return;
            }
            
            if (!EditorUtility.DisplayDialog("Ready?", $"About to change GUIDs on {targets.Length} asset(s). Proceed?\n****NOT undo-able****", "Yep", "Nope"))
                return;

            foreach (var asset in targets)
            {
                if(!asset)
                    continue;
                switch (asset)
                {
                    case TpTileBundle bundle:
                        bundle.ResetGuid();
                        bundle.AddGuid();
                        break;
                    case TpTileFab tpTileFab:
                        tpTileFab.ResetGuid();
                        tpTileFab.AddGuid();
                        break;
                }
            }

            AssetDatabase.SaveAssets();
            
        }
        
        [MenuItem("Tools/TilePlus/Utilities/Change Tile+ tile state", false, 101)]
        private static void ChangeTileState()
        {
            var targets = Selection.objects;
            if (targets.Length != 1 || !targets[0])
            {
                EditorUtility.DisplayDialog("SORRY!", "Nothing Selected or multiple selection!", "Exit");
                return;
            }

            // ReSharper disable once Unity.NoNullPatternMatching
            if (targets[0] is not TilePlusBase { IsClone: false } tpb) //second test ensures not in the scene although shouldn't be possible.
            {
                EditorUtility.DisplayDialog("SORRY!", "The selection must be an TilePlusBase asset or a derived tile class of TilePlusBase - in the Project folder", "Exit");
                return;
            }

            if (tpb.IsAsset)
            {
                if (EditorUtility.DisplayDialog("Ready?", $"About to change state of {tpb.name} to LOCKED. OK?", "OK", "Cancel"))
                {
                    tpb.ResetState(TileResetOperation.SetCloneState);  //has to be clone to transition to lock. Does not actually clone anything.
                    tpb.ChangeTileState(TileResetOperation.MakeLockedAsset);
                }
            }
            else
            {
                if(EditorUtility.DisplayDialog("Ready?", $"About to change state of {tpb.name} to ASSET. OK?", "OK", "Cancel"))
                    tpb.ChangeTileState(TileResetOperation.MakeNormalAsset);        

            }
            
            AssetDatabase.SaveAssets();
            
        }

        
        [MenuItem("Tools/TilePlus/Utilities/Delete Null Tiles", false, 100)]
        private static void DeleteNulls()
        {
            var go = Selection.activeTransform != null ? Selection.activeTransform.gameObject : null;
            if (go == null)
                return;

            if (!TilePlusConfig.instance.AllowPrefabEditing)
            {

                if (EditorSceneManager.IsPreviewSceneObject(go))
                {
                    EditorUtility.DisplayDialog("SORRY!", "You can't do this in a prefab editing context.", "Continue");
                    return;
                }

                if (PrefabUtility.IsAnyPrefabInstanceRoot(go) || PrefabUtility.IsPartOfAnyPrefab(go))
                {
                    EditorUtility.DisplayDialog("SORRY!", "You can't do this to a prefab", "Continue");
                    return;
                }
            }

            //get the tilemaps
            var maps = go.GetComponentsInChildren<Tilemap>();
            if (maps is not { Length: 1 }) //no selected maps
            {
                EditorUtility.DisplayDialog("SORRY!", "No Tilemaps found in Selection!!", "Continue");
                return;
            }

            var map = maps[0];

            var msg = $"About to delete null tiles from Tilemap {map.name}.  Proceed?";
            
            if (!EditorUtility.DisplayDialog("Ready?", msg,  "Yep", "Nope"))
                return;
            

            map.CompressBounds();
            var count = 0;
            
            RegisterUndo(map,"Deleting null Tiles");
            
            foreach (var pos in map.cellBounds.allPositionsWithin)
            {
                var tile   = map.GetTile(pos);
                var sprite = map.GetSprite(pos);
                if (tile == null && sprite != null)
                {
                    map.SetTile(pos, null);
                    count++;
                }
            }
            
            EditorUtility.DisplayDialog("Results", $"{count} null tile(s) were deleted from Tilemap named: {map.name}", "Continue");
            
        }
        
        
        
        [MenuItem("Tools/TilePlus/Utilities/Update Tile+ GUIDS", false, 100)]
        private static void UpdateGUIDs()
        {
            var go = Selection.activeTransform != null ? Selection.activeTransform.gameObject : null;
            if (go == null)
                return;

            if (EditorSceneManager.IsPreviewSceneObject(go))
            {
                EditorUtility.DisplayDialog("SORRY!", "You can't do this in a prefab editing context.", "Continue");
                return;
            }

            if (PrefabUtility.IsAnyPrefabInstanceRoot(go) || PrefabUtility.IsPartOfAnyPrefab(go))
            {
                EditorUtility.DisplayDialog("SORRY!", "You can't do this to a prefab", "Continue");
                return;
            }

            //get the tilemaps
            var maps = go.GetComponentsInChildren<Tilemap>();
            if (maps == null || maps.Length != 1) //no selected maps
            {
                EditorUtility.DisplayDialog("SORRY!", "No Tilemaps found in Selection!!", "Continue");
                return;
            }

            var map = maps[0];

            var msg = $"About to update all TilePLus tile GUIDs on Tilemap {map.name}.  Proceed?";
            
            if (!EditorUtility.DisplayDialog("Ready?", msg,  "Yep", "Nope"))
                return;
            

            map.CompressBounds();
            var count = 0;
            
            RegisterUndo(map,"Updating Tile+ GUIDs");
            
            foreach (var pos in map.cellBounds.allPositionsWithin)
            {
                var tile   = map.GetTile(pos);
                if(!tile)
                    continue;
                // ReSharper disable once Unity.NoNullPatternMatching
                if (tile is not TilePlusBase tpb)
                    continue;
                count++;
                tpb.ResetState(TileResetOperation.ClearGuid);
                tpb.ResetState(TileResetOperation.MakeNormalAsset);
            }
            
            map.RefreshAllTiles();  //this will re-clone all the tiles since they were temporarily made 'normal' assets again.
            SceneScan();
            
            EditorUtility.DisplayDialog("Results", $"{count} TilePlus tile(s) have new GUIDs on Tilemap named: {map.name}", "Continue");
            
        }

        [MenuItem("Assets/TilePlus/Change Tile Flags", true, 10000)]
        private static bool ChangeTileFlagsValidator()
        {
            var objects = Selection.objects;
            foreach (var obj in objects)
            {
                if(!obj)
                    continue;
                // ReSharper disable once Unity.NoNullPatternMatching
                if (obj is not Tile)
                    return false;
            }

            return true;
        }
        
        
        [MenuItem("Assets/TilePlus/Change Tile Flags", false, 10000)]
        [MenuItem("Tools/TilePlus/Utilities/Change Tile Flags", false, 109)]
        private static void ChangeTileFlags()
        {
            if (Selection.count == 0)
            {
                TpLib.TpLog("Change Tile Flags utility: Nothing Selected!");
                return;
            }

            var wnd = EditorWindow.CreateWindow<TileFlagsDialog>();
            // ReSharper disable once Unity.NoNullPatternMatching
            if (Selection.count == 1 && Selection.activeObject && Selection.activeObject is Tile t)
            {
                wnd.Setup(Cb,t);
            }
            else
                wnd.Setup(Cb, null, Selection.count);
            
            return;
            
            void Cb(TileFlagsDialog.TileFlagsDialogResult result)
            {

                if (!result.m_Apply)
                    return;
                var valueForT      = result.m_TransformFlagValue;
                var valueForC      = result.m_ColorFlagValue;
                var changedItems   = new List<Object>();
                var unchangedItems = 0;
                foreach (var obj in Selection.objects)
                {
                    if(!obj)
                        continue;
                    // ReSharper disable once Unity.NoNullPatternMatching
                    if (obj is Tile)
                        changedItems.Add(obj);
                    else
                        unchangedItems++;
                }

                if (changedItems.Count != 0)
                {
                    Undo.RegisterCompleteObjectUndo(changedItems.ToArray(),"TPT:Changed Tile Flags");

                    foreach (var obj in changedItems)
                    {
                        if(!obj)
                            continue;
                        // ReSharper disable once Unity.NoNullPatternMatching
                        if(obj is not Tile t2) //redundant but also a null check which is a good idea.
                            continue;
                        ChangeFlags(t2, valueForT, valueForC);
                    }

                    var paths = new List<string>(changedItems.Count);
                    foreach (var obj in changedItems)
                        paths.Add(AssetDatabase.GetAssetPath(obj));

                    AssetDatabase.ForceReserializeAssets(paths);
                    AssetDatabase.SaveAssets();
                }

                var msg = $"Changed flags on {changedItems.Count} Tile(s).\nIgnored (not Tile) {unchangedItems}";

                TpLib.TpLog($"Results: {msg}");

                EditorUtility.DisplayDialog("Results", msg, "Continue");
            }


        }

        //note lockColor is LSB, lockTransform is bit 1.
        
        private static void ChangeFlags(Tile tile, bool t, bool c)
        {
            if(tile == null)
                return;
            var flags = (int)tile.flags;
            if (!t && !c) //clear both flags
            {
                flags      &= -4;
                tile.flags =  (TileFlags)flags;
                return;
            }

            if (t && c)
            {
                flags      |= 3;
                tile.flags =  (TileFlags)flags;
                return;
            }

            if (t & !c) //lt=1, lc=0
            {
                flags      |= 2; //lock transform
                flags      &= -2; //clear lock color
                tile.flags =  (TileFlags)flags;
                return;
            }
            
            if (!t & c) //lt=0, lc=1
            {
                flags      |= 1;  //lock color
                flags      &= -3; //clear lock transform
                tile.flags =  (TileFlags)flags;
                return;
            }
            
                
            
            if (t)
                flags |= 2;
            if (c)
                flags |= 1;
            if (!t)
                flags &= -3;
            if (!c)
                flags &= -2;

            tile.flags = (TileFlags)flags;
        }

        
        /// <summary>
        /// Register a Unity undo.
        /// </summary>
        /// <param name="map">tilemap.</param>
        /// <param name="description">Undo description.</param>
        private static void RegisterUndo(Tilemap? map, string description)
        {
            if(map != null)
                Undo.RegisterCompleteObjectUndo(new Object[] { map, map.gameObject }, $"Tile+Painter: {description}");
        }

        #endregion
        
        #region userInputUtils

        /// <summary>
        /// Get a path from the user. Last path is maintained during session
        /// </summary>
        /// <param name="folderPanelPrompt">Prompt for the folder panel</param>
        /// <param name = "defaultName" >A default name for the folder, or string.empty</param>
        /// <returns>path or string.empty if user quits</returns>
        public static string GetPathFromUser(string folderPanelPrompt, string defaultName = "")
        {
            //get the output path
            var acceptable = false;
            // ReSharper disable once RedundantAssignment
            var path = string.Empty;
            do
            {
                //get a destination folder

                var basePath = s_LastPath != string.Empty
                                    ? s_LastPath
                                    : Application.dataPath +"/Assets";
                
                    
                var assetsPath = Path.GetDirectoryName(basePath);
                path = EditorUtility.SaveFolderPanel(folderPanelPrompt, assetsPath, defaultName);
                if (string.IsNullOrWhiteSpace(path))
                {
                    Debug.Log("User cancelled operation.");
                    return string.Empty;
                }

                s_LastPath = path;
                //don't want root of assets folder
                var index = path.IndexOf("Assets", StringComparison.Ordinal);
                path = path[index..];
                if (path is "Assets" or "")
                    EditorUtility.DisplayDialog("Not there!", "Choose a subfolder of 'Assets' - try again", "Continue");
                else
                    acceptable = true;
            } while (!acceptable);

            return path;
        }
            
        

        #endregion
    }
}
