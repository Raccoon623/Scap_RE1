// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 08-02-2021
// ***********************************************************************
// <copyright file="TpPrefabUtilities.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TilePlus.Editor.Painter;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;
using PrefabUtility = UnityEditor.PrefabUtility;
#nullable enable

namespace TilePlus.Editor
{
    /// <summary>
    /// Utilities for dealing with prefabs and tile archives.
    /// </summary>
    public static class TpPrefabUtilities
    {
        /// <summary>
        /// Used when the Unity palette makes a
        /// selection and then MakeBundle is called from the Tools menu
        /// </summary>
        public enum SelectionBundling
        {
            /// <summary>
            /// Grid selection not used
            /// </summary>
            None,
            /// <summary>
            /// Bundle the Grid Selection target (ie tilemap) ONLY
            /// </summary>
            Target,
            /// <summary>
            /// Bundle ALL the tilemaps that are components on GO's that are chilren of the Grid
            /// </summary>
            All
        }

        
        /// <summary>
        /// Guide user thru creating tilemap archives of different types
        /// </summary>
        [MenuItem("Tools/TilePlus/Prefabs/Bundle Tilemaps", false, 3)]
        public static async void MakeBundle()
        {
            SelectionBundling selectionBundling = SelectionBundling.None;
            GameObject?        selectedGo        = null;
            var               hasGridSelection  = GridSelection.active;
           
            
            if (hasGridSelection)
            {
                var  selectionBoundsInt = GridSelection.position;

                if (selectionBoundsInt.size != Vector3Int.zero)
                {
                    if(GridSelection.grid != null) //if this is a grid, I have a few questions for ya
                    {
                        if (!EditorUtility.DisplayDialog("Use Grid Selection?", "There's an active Grid Selection and a Grid is selected in the Heirarchy.\nI can bundle the tiles within the Grid Selection for all child Tilemaps. How to proceed?", "Continue", "Cancel"))
                            return;
                        selectionBundling = SelectionBundling.All;
                        selectedGo        = GridSelection.grid.gameObject;
                    }
                    else if (GridSelection.target.TryGetComponent<Tilemap>(out _))
                    {
                        if (!EditorUtility.DisplayDialog("Use Grid Selection?", "There's an active Grid Selection and a Tilemap is selected in the Heirarchy.\nI can bundle the tiles within the Grid Selection for just this one tilemap. How to proceed?", "Continue", "Cancel"))
                            return;
                        selectionBundling = SelectionBundling.Target;                                                                        
                        selectedGo = GridSelection.target;
                    }
                }
            }

            if (selectionBundling == SelectionBundling.None)
            {
                if (Selection.count != 1)
                {
                    EditorUtility.DisplayDialog("SORRY!", "Cannot use multiple selection", "Continue");
                    return;
                }

                //Is the selected object a gameobject
                selectedGo = Selection.activeTransform != null
                             ? Selection.activeTransform.gameObject
                             : null;
                
            }
            if (selectedGo == null) //nope, we're done here. 
            {
                OopsMessage();
                return;
            }

            if (selectedGo.scene.name == string.Empty)
            {
                EditorUtility.DisplayDialog("Unsaved Scene!", "Please save this scene first!", "Exit");
                return;

            }
            
            var selectionIsGrid =    selectedGo.TryGetComponent<Grid>(out _);

            
            //can't proceed within prefab editing context
            if (EditorSceneManager.IsPreviewSceneObject(selectedGo))
            {
                EditorUtility.DisplayDialog("SORRY!", "You can't do this in a prefab editing context.", "Continue");
                return;
            }

            //can't proceed if selection is part of a prefab
            if (PrefabUtility.IsPartOfPrefabAsset(selectedGo) || PrefabUtility.IsPartOfAnyPrefab(selectedGo))
            {
                EditorUtility.DisplayDialog("SORRY!", "The selected GameObject is part of a prefab. Can't proceed.", "Continue");
                return;
            }

            //does it have any tilemaps?
            var maps = selectedGo.GetComponentsInChildren<Tilemap>(true);
            if (maps == null || maps.Length == 0) //no tilemaps in the selection
            {
                OopsMessage();
                return;
            }

                
            var grids           = selectedGo.GetComponentsInChildren<Grid>(true);
            
            if (selectionIsGrid)
            {
                //should only be one grid
                if (grids == null) //should not happen
                    return;
                if (grids.Length != 1)
                {
                    EditorUtility.DisplayDialog("SORRY!", "Too many grids! Must be a single grid with child tilemaps. Can't proceed.", "Continue");
                    return;
                }
            }
            else
            {
                EditorUtility.DisplayDialog("No Grid", "Please Select a grid! Can't proceed.", "Continue");
                return;
            }
            
            //Are any maps already in a prefab? 
            if (maps.Any(map => PrefabUtility.IsPartOfPrefabAsset(map) || PrefabUtility.IsPartOfAnyPrefab(map)))
            {
                EditorUtility.DisplayDialog("SORRY!", "One or more Tilemaps are already in Prefabs. Can't proceed.", "Continue");
                return;
            }

            var path = TpEditorUtilities.GetPathFromUser("Select destination folder for saving Bundle and TileFab assets.");
            if(path == string.Empty)
                return;
            
            //if parent GO or the heirarchy had a grid component somewhere, ask if we want to make a prefab.
            var makeTotalBundle = selectionIsGrid &&
                                  EditorUtility.DisplayDialog("Make a prefab?",
                                      "Make a prefab of the Grid and child Tilemaps?",
                                      "Yes",
                                      "No");

            var includePrefabs = EditorUtility.DisplayDialog("Bundle prefabs?",
                                                               "Bundle child Prefabs (as references)?", "Yes", "No");
            //get a base name for the assets. Added in 2.1
            var wait             = true;
            var possibleFilename = selectedGo.scene.name;
            var dialog           = ScriptableObject.CreateInstance<StringEntryDialog>();
            dialog.ShowStringEntryDialog("Choose a name",
                                         "Enter a name for the generated assets.",
                                         possibleFilename,
                                         "Ok",
                                         string.Empty,
                                         (s) =>
                                         {
                                             possibleFilename = s;
                                             wait             = false;
                                         });
            while (wait)
                await Task.Yield();
            

            Pack(selectedGo.scene.name,
                 path,
                 selectedGo,
                 hasGridSelection
                     ? GridSelection.position
                     : new BoundsInt(),
                 maps,
                 false,
                 makeTotalBundle,
                 selectionBundling,
                 includePrefabs,
                 false,
                 false,
                 false,
                 possibleFilename);
        }


        /// <summary>
        /// Pack a TileFab without all of the incessant questions! 
        /// </summary>
        /// <param name="sceneName">Name of the scene that the TileFab came from</param>
        /// <param name="path">Path for the output files</param>
        /// <param name="selectedGo">The selection object</param>
        /// <param name = "selectionBounds" >Bounds of the selection, if applicable. Can be null</param>
        /// <param name="maps">the maps to be bundled</param>
        /// <param name = "silent" >No messages if true</param>
        /// <param name="createPrefab">Create a prefab after the TileFab is created</param>
        /// <param name="selectionBundling">A value from the SelectionBundling Enum</param>
        /// <param name="includePrefabs">include prefabs in the bundle</param>
        /// <param name="unpack">create variant prefabs (NO LONGER USED)</param>
        /// <param name = "hideFromPainter" >set the 'Ignore in Painter' field to true if this is true</param>
        /// <param name = "omitTilefab" >If true, don't create the TileFab. IGNORE the return values if you do so (aside from 'success'</param>
        /// <param name = "optionalAssetBaseName" >Optional name used instead of extracting the name from the tilemaps</param>
        /// <returns> tuple of bounds (can be zero size), assetName (can be empty), path (can be empty),
        /// the fab instance (do not retain ourside of calling scope),
        /// and a bool (false if any sort of failure) and a second bool that indicates that failure was due to an empty region being examined,
        /// and (new in 2.1) a list of the created bundles</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public static (BoundsInt boundInt, string tileFabAssetName, string tileFabAssetPath, TpTileFab? fab, bool success, bool failureWasEmptyArea, List<TpTileBundle>bundles) 
            Pack(string            sceneName,
                 string            path,
                 GameObject?       selectedGo,
                 BoundsInt?        selectionBounds,
                 Tilemap[]         maps,
                 bool              silent                = false,
                 bool              createPrefab          = false,
                 SelectionBundling selectionBundling     = SelectionBundling.All,
                 bool              includePrefabs        = true,
                 bool              unpack                = false,
                 bool              hideFromPainter       = false,
                 bool              omitTilefab           = false,
                 string?           optionalAssetBaseName = null) 
        {

            //map tilemaps to generated assets
            var mapToBlobDict = new Dictionary<Tilemap, TpTileBundle>();

            //new in 2.1 - return list of created Bundles
            var bundleInstances = new List<TpTileBundle>(maps.Length);
            
            var totalTpTilesCount    = 0;
            var totalUnityTilesCount = 0;
            var totalPrefabsCount    = 0;
            var timeStamp            = DateTime.UtcNow.ToString("M/d/yyyy hh:mm:ss tt") + " UTC";
            var blobPaths            = new List<string>();
            var useOptName           = !string.IsNullOrWhiteSpace(optionalAssetBaseName);
            
            if (selectedGo == null ||  !selectedGo.TryGetComponent<Grid>(out _))
            {
                Debug.LogError("Top-level object was not GRID!");
                return (new BoundsInt(), string.Empty, string.Empty, null, false,false,bundleInstances);
            }


            var mapIndex  = 0;
            var indexMaps = maps.Length != 0;
            //scan all maps
            foreach (var map in maps)
            {
                //these two are used to adjust the position values if selectionBundling
                var gridSelOffset   = Vector3Int.zero;
                var worldSelOffset  = Vector3.zero;
                var zeroBasedBounds = new BoundsInt();

                BoundsInt bounds;
                if (selectionBundling == SelectionBundling.None || !selectionBounds.HasValue)
                {
                    map.CompressBounds();
                    bounds = map.cellBounds;
                }
                else
                {
                    bounds          = selectionBounds.Value;
                    gridSelOffset   = selectionBounds.Value.position;
                    worldSelOffset  = map.CellToWorld(gridSelOffset); 
                    zeroBasedBounds = new BoundsInt(Vector3Int.zero, selectionBounds.Value.size);
                }

                //Debug.Log($"BundlingMode: {selectionBundling} Bounds: {bounds}  map{map}");
                //adjust for case where selection bounds size > map bounds. Important for prefabs.
                if (includePrefabs
                    && selectionBounds.HasValue
                    && (selectionBounds.Value.size.x > bounds.size.x || selectionBounds.Value.size.y > bounds.size.y))
                    bounds        = new BoundsInt(bounds.position, selectionBounds.Value.size);


                List<Transform>? childPrefabs = null;
                var             childPrefabCount = 0;
                if (includePrefabs)
                {
                    childPrefabs = new List<Transform>();
                    var transform = map.transform;
                    var cCount    = transform.childCount;
                    //if children, add them to child prefabs list for later use
                    if (cCount != 0)
                    {
                        for (var i = 0; i < cCount; i++)
                        {
                            var child = transform.GetChild(i);
                            if (selectionBundling != SelectionBundling.None)
                            {
                                var cellPos = map.WorldToCell(child.transform.position);
                                if(!bounds.Contains(cellPos))
                                   continue;
                            }
                            childPrefabs.Add(child);
                        }
                    }

                    childPrefabCount = childPrefabs.Count;
                }

                //now deal with the map
                
                //separate lists for TP and UNITY tiles
                var tpTilesList        = new List<(Vector3Int pos, TilePlusBase t)>();
                var unityTilesList     = new List<(Vector3Int pos, TileBase tb)>();
                foreach (var pos in bounds.allPositionsWithin)
                {
                    var tile = map.GetTile(pos);
                    if (tile == null)
                        continue;
                    // ReSharper disable once ConvertIfStatementToSwitchStatement
                    // ReSharper disable once Unity.NoNullPatternMatching
                    if (tile is TilePlusBase tpb)
                        tpTilesList.Add((pos - gridSelOffset, tpb));
                    else  //TileBase is the only other possibility (this includes Tile)
                        unityTilesList.Add((pos - gridSelOffset, tile));
                }

                var tpTilesCount    = tpTilesList.Count;
                var unityTilesCount = unityTilesList.Count;
                //if nothing found, nothing to do on this map.
                if (tpTilesCount == 0 && unityTilesCount == 0 && childPrefabCount == 0) 
                {
                    Debug.Log($"No tiles or prefabs found on map {map} within bounds {bounds}");
                    continue;
                }

                totalTpTilesCount    += tpTilesCount;
                totalUnityTilesCount += unityTilesCount;
                totalPrefabsCount += childPrefabCount;

                //create the combined tiles asset.
                var sObj = ScriptableObject.CreateInstance<TpTileBundle>();
                if (sObj == null)
                {
                    Debug.LogError("Could not create TpTileBundle instance. Cancelling operation.");
                    return (new BoundsInt(), string.Empty, string.Empty, null, false,false,bundleInstances);
                }

                var baseName = useOptName
                                   ? optionalAssetBaseName
                                   : map.transform.parent.name;
                if (indexMaps)
                    baseName = $"{baseName}_{(mapIndex++).ToString()}";
                
                var blobAssetName = useOptName ? $"{path}/{baseName}_Bundle.Asset" :
                                        $"{path}/{baseName}_{map.name}.Asset";
                sObj.m_TimeStamp         = timeStamp;
                sObj.m_ScenePath         = GetScenePathUpwards(map.gameObject);
                sObj.m_OriginalScene     = sceneName;
                sObj.m_TilemapBoundsInt = selectionBundling == SelectionBundling.None
                                              ? bounds
                                              : zeroBasedBounds;
                sObj.m_FromGridSelection = selectionBundling != SelectionBundling.None;
                sObj.AddGuid();
                var objPath = AssetDatabase.GenerateUniqueAssetPath(blobAssetName);
                AssetDatabase.CreateAsset(sObj, objPath);
                blobPaths.Add(objPath);

                //load it up
                var blob = AssetDatabase.LoadAssetAtPath<TpTileBundle>(objPath);
                //update the map->blobasset mapping
                mapToBlobDict.Add(map, blob);
                if (hideFromPainter)
                    blob.m_IgnoreInPainter = true;

                //deal with prefabs if necc.
                if (includePrefabs && childPrefabs != null && childPrefabs.Count != 0)
                {
                    foreach (var transform in childPrefabs)
                    {
                        var gameObj   = transform.gameObject;
                        if (gameObj == null)
                        {
                            Debug.LogError("Null gameObj found! Skipping it...");
                            continue;
                        }

                        if (!PrefabUtility.IsAnyPrefabInstanceRoot(gameObj))
                        {
                            Debug.LogError($"GameObject '{gameObj.name}' isn't a prefab; skipping it...");
                            continue;
                        }

                        var sourceObjFromProject = PrefabUtility.GetCorrespondingObjectFromSource(gameObj);
                        if (sourceObjFromProject == null)
                        {
                            Debug.LogError($"GameObject '{gameObj.name}' source in Project not found; skipping it...");
                            continue;
                        }

                        blob.AddPrefab(sourceObjFromProject, transform.position - worldSelOffset, transform.rotation, transform.localScale);
                    }
                }

                //lock up the TP tiles and add to blob                
                foreach ((var pos, var t) in tpTilesList)
                {
                    if (t.IsClone)
                    {
                        var copy = UnityEngine.Object.Instantiate(t);
                        if (copy == null)
                            continue;
                        if (!copy.ChangeTileState(TileResetOperation.MakeLockedAsset))
                        {
                            Debug.Log("State change failed.");
                            continue;
                        }
                        AssetDatabase.AddObjectToAsset(copy, blob);
                        blob.AddTpbToListOfTiles(map, pos, copy, t);
                    }
                    else
                        blob.AddTpbToListOfTiles(map, pos, t, t);
                }

                //add the UNITY tiles to blob
                foreach ((var pos, var ut) in unityTilesList)
                    blob.AddUnityTileToListOfTiles(map, pos, ut);

                TpLibEditor.PrefabBuildingActive = true;
                map.RefreshAllTiles(); //a bit of magic
                TpLibEditor.PrefabBuildingActive = false;

                blob.Seal();           //finalize the data structures in the blob
                bundleInstances.Add(blob);
                
                //save it all
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
            }
            AssetDatabase.ForceReserializeAssets(blobPaths,ForceReserializeAssetsOptions.ReserializeAssets);
            //ensure proper asset save time is updated. Can cause infrequent errors w/o the next 2 lines. 
            //If not here, TpPainterGridSelPanel's Bundle Palette Selection button will create a Bundle that will
            //cause error #4 (time on disk != time in assetDB) when inspected.
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            var totalNumTiles = totalTpTilesCount + totalUnityTilesCount;

            //report to user
            if (!silent)
            {
                var actualBounds = selectionBundling !=  SelectionBundling.All
                                       ? GridSelection.position
                                       : selectionBounds ?? new BoundsInt();
                
                var msg = $"Nothing found. Selection: Sel= {actualBounds.ToString()} Obj = {selectedGo} ";
                var mapS = maps.Length == 1
                               ? string.Empty
                               : "s";
                EditorUtility.DisplayDialog("Results",
                                            totalNumTiles == 0 && totalPrefabsCount == 0
                                                ? msg
                                                : $"Bundled {totalTpTilesCount + totalUnityTilesCount} Tiles {(includePrefabs && totalPrefabsCount != 0 ? ", " + totalPrefabsCount.ToString() + " Prefabs":"")} for {maps.Length} Tilemap{mapS}. Output is in the Project folder: {path}",
                                            "Continue");
            }

            
            if (totalNumTiles == 0 && totalPrefabsCount == 0) 
                return  (new BoundsInt(), string.Empty, string.Empty,null, false,true,bundleInstances);
            
            if(omitTilefab)
                return  (new BoundsInt(), String.Empty, string.Empty, null, true, false,bundleInstances);

            //create tilefab asset
            var tileFabSobj = ScriptableObject.CreateInstance<TpTileFab>();
            if (tileFabSobj == null)
            {
                Debug.LogError("Could not create TpTileFab instance. Cancelling operation.");
                return  (new BoundsInt(),string.Empty,string.Empty,null, false, false,bundleInstances);
            }
            
            if (hideFromPainter)
                tileFabSobj.m_IgnoreInPainter = true;
            
           
            var tileFabName = $"{(useOptName ? optionalAssetBaseName : selectedGo.name)}_TileFab";
            var tileFabPath = $"{path}/{tileFabName}.asset";
            
            tileFabSobj.m_TimeStamp         = timeStamp;
            tileFabSobj.m_ScenePath         = GetScenePathUpwards(selectedGo);
            tileFabSobj.m_OriginalScene     = sceneName;
            tileFabSobj.m_FromGridSelection = selectionBundling != SelectionBundling.None;
            tileFabSobj.AddGuid(); 
            foreach (var map in maps)
            {
                if (mapToBlobDict.TryGetValue(map, out var bundle) && tileFabSobj.m_TileAssets != null)
                    tileFabSobj.m_TileAssets.Add(new TpTileFab.AssetSpec
                    {
                        m_Asset       = bundle,
                        m_TilemapName = map.name,
                        m_TilemapTag  = map.tag
                    });
            }
            
            var tileFabObjPath     = AssetDatabase.GenerateUniqueAssetPath(tileFabPath);

            AssetDatabase.CreateAsset(tileFabSobj, tileFabObjPath);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            //if not creating a prefab we are done.
            if (!createPrefab)
            {
                return (tileFabSobj.LargestBounds, tileFabName, tileFabPath, tileFabSobj, true, false,bundleInstances);
            }

            var prefabBaseName = $"{(useOptName ? optionalAssetBaseName : selectedGo.name)}";
            //else make a big prefab out of all we've done so far, excluding the tilefab.
            var bundleName = $"{prefabBaseName}_prefab";
            var prefabPath = $"{path}/{bundleName}.prefab";

            TpLibEditor.PrefabBuildingActive = true;
            var baseGameObject = UnityEngine.Object.Instantiate(selectedGo, null); //copy the grid or top-level GO
            if(!baseGameObject.TryGetComponent<Grid>(out _))
            {
                Debug.LogError("Error: top level object was not a Grid. The Prefab was not created but the TileFabs and Bundles are fine.");
                UnityEngine.Object.DestroyImmediate(baseGameObject);
                return (new BoundsInt(), string.Empty, string.Empty, null, false,false,bundleInstances);
            }
            
            //add the prefabMarker (loads up the tilemaps when prefab is placed)
            var marker = baseGameObject.AddComponent<TpPrefabMarker>();
            if (marker == null)
            {
                Debug.LogError("Error: Could not add component to top level Grid's GO. The Prefab was not created but the TileFabs and Bundles are fine.");
                UnityEngine.Object.DestroyImmediate(baseGameObject);
                return (new BoundsInt(), string.Empty, string.Empty, null, false,false,bundleInstances);
            }

            marker.m_TileFabForPrefab = tileFabSobj;
            
            var tilemaps       = baseGameObject.GetComponentsInChildren<Tilemap>();
            //don't want these tilemaps to have any tiles.
            foreach (var map in tilemaps)
                map.ClearAllTiles();
            
            //save this new Grid prefab
            PrefabUtility.SaveAsPrefabAsset(baseGameObject, prefabPath);
            TpLibEditor.PrefabBuildingActive = false;
           
            
            UnityEngine.Object.DestroyImmediate(baseGameObject); //don't need this anymore
            AssetDatabase.SaveAssets();                          //just to be sure!
            AssetDatabase.Refresh();
            TpLibEditor.PrefabBuildingActive = false;
            return  (tileFabSobj.LargestBounds, tileFabName, tileFabPath, tileFabSobj, true, false,bundleInstances);
        }

        
        /// <summary>
        /// Create a bundle from a clipboard multiple-selection item
        /// </summary>
        /// <param name="callback">required: returns t/f</param>
        /// <param name="sceneName">name of scene</param>
        /// <param name="path">path</param>
        /// <param name="source">a TileCellsWrapper ref</param>
        /// <param name="hideFromPainter">sets the bundle's hide from painter toggle. If true, a TpBundleTile is created as well.</param>
        /// <param name="optionalAssetBaseName">an optional name for the assets</param>
        /// <param name="createIcon">try to create an icon.</param>
        public static async Task CreateBundleFromCells(Action<bool> callback,
                                                string           sceneName,
                                                 string           path,
                                                 TileCellsWrapper source,
                                                 bool             hideFromPainter       = false,
                                                 string?          optionalAssetBaseName = null,
                                                 bool             createIcon            = false)
        {
            if (source == null || source.Cells == null)
            {
                callback.Invoke(false);
                return;
            }
            
            var timeStamp  = DateTime.UtcNow.ToString("M/d/yyyy hh:mm:ss tt") + " UTC";
            var useOptName = !string.IsNullOrWhiteSpace(optionalAssetBaseName);

            //split cells into UnityTiles and TPT tiles
            //these two are used to adjust the position values if selectionBundling
            var tpTilesList        = new List<TileCell>();
            var unityTilesList     = new List<TileCell>();
            
            foreach(var cell in source.Cells) 
            {
                if( cell.TileBase as TilePlusBase)
                    tpTilesList.Add(cell);
                else
                    unityTilesList.Add(cell);
            }

            var tpTilesCount    = tpTilesList.Count;
            var unityTilesCount = unityTilesList.Count;

            if (tpTilesCount == 0 && unityTilesCount == 0)
            {
                callback.Invoke(false);
                return;
            }
            //create the Bundle asset.
            var sObj = ScriptableObject.CreateInstance<TpTileBundle>();
            if (sObj == null)
            {
                callback.Invoke(false);
                return;
            }
            var baseName = useOptName
                                   ? optionalAssetBaseName
                                   : $"{sceneName}_PICK";
            var blobAssetName = $"{path}/{baseName}_Bundle.Asset";

            sObj.m_TimeStamp         = timeStamp;
            sObj.m_ScenePath         = "SAVED_PICK_NO_SCENE";
            sObj.m_OriginalScene     = "NO_SCENE";
            sObj.m_TilemapBoundsInt  = source.m_Bounds; //note this should be zero based
            sObj.m_FromGridSelection = true;
            sObj.AddGuid();
            var objPath = AssetDatabase.GenerateUniqueAssetPath(blobAssetName);
            AssetDatabase.CreateAsset(sObj, objPath);

            //load it up
            var blob = AssetDatabase.LoadAssetAtPath<TpTileBundle>(objPath);
            if (hideFromPainter)
               blob.m_IgnoreInPainter = true;

            //lock up the TP tiles and add to blob                
            foreach (var cell in tpTilesList)
            {
                var tptTile = cell.TileBase as TilePlusBase;
                if(tptTile == null)
                    continue;
                if (tptTile.IsClone) 
                {
                    var copy = UnityEngine.Object.Instantiate(tptTile);
                    if (copy == null)
                        continue;
                    if (!copy.ChangeTileState(TileResetOperation.MakeLockedAsset))
                    {
                        Debug.Log("State change failed.");
                        continue;
                    }
                    AssetDatabase.AddObjectToAsset(copy, blob);
                    blob.AddTpbToListOfTiles(copy, cell.m_Position, cell.m_Transform, cell.m_Color);
                }
                else
                {
                    //AssetDatabase.AddObjectToAsset(tptTile, blob);
                    blob.AddTpbToListOfTiles(tptTile, cell.m_Position, cell.m_Transform, cell.m_Color);
                }
            }

            //add the UNITY tiles to blob
            foreach (var cell in unityTilesList.Where(c=> c.TileBase != null))
                blob.AddUnityTileToListOfTiles(cell.TileBase!, cell.m_Position, cell.m_Transform, cell.m_Color);

            blob.Seal();           //finalize the data structures in the blob
            
            //create icon? MUST be done prior to saving blob asset
            if (createIcon)
                await TpPrefabUtilities.CreateThumbnail(source,  objPath, source.m_MultipleTilesSelectionBoundsInt, blob,false,source.Icon);

            //save it all
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            var paths = new[] { objPath };
            AssetDatabase.ForceReserializeAssets(paths ,ForceReserializeAssetsOptions.ReserializeAssets);
            //ensure proper asset save time is updated. Can cause infrequent errors w/o the next 2 lines. 
            //If not here, may cause error #4 (time on disk != time in assetDB) when inspected.
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            callback.Invoke(true);

        }

        
         /// <summary>
        /// Create a bundle from a clipboard multiple-selection item
        /// </summary>
        /// <param name="callback">required: returns t/f</param>
        /// <param name="path">path</param>
        /// <param name="source">an array of Prefabs. Caller should ensure that these are prefabs!</param>
        /// <param name="optionalAssetBaseName">an optional name for the assets</param>
        public static void CreateBundleFromPrefabs(Action<bool> callback, 
                                                 string                path,
                                                 GameObject[]       source,
                                                 string?               optionalAssetBaseName = null)
        {
            if (source.Length == 0)
            {
                callback.Invoke(false);
                return;
            }
            
            var timeStamp  = DateTime.UtcNow.ToString("M/d/yyyy hh:mm:ss tt") + " UTC";
            var useOptName = !string.IsNullOrWhiteSpace(optionalAssetBaseName);

            //create the combined tiles asset.
            var sObj = ScriptableObject.CreateInstance<TpTileBundle>();
            if (sObj == null)
            {
                callback.Invoke(false);
                return;
            }
            var baseName = useOptName
                                   ? optionalAssetBaseName
                                   : $"PrefabBundle";
            var blobAssetName = $"{path}/{baseName}_Bundle.Asset";

            sObj.m_TimeStamp         = timeStamp;
            sObj.m_ScenePath         = "SAVED_PREFAB_BUNDLE_NO_SCENE";
            sObj.m_OriginalScene     = "NO_SCENE";
            sObj.m_TilemapBoundsInt  = new BoundsInt(); 
            sObj.AddGuid();
            var objPath = AssetDatabase.GenerateUniqueAssetPath(blobAssetName);
            AssetDatabase.CreateAsset(sObj, objPath);

            //load it up
            var blob = AssetDatabase.LoadAssetAtPath<TpTileBundle>(objPath);

            var pos = Vector3Int.zero;
            foreach (var prefab in source)
            {
                blob.AddPrefab(prefab,pos, prefab.transform.rotation, prefab.transform.lossyScale);
                pos.x += 2;
            }                


            blob.Seal();           //finalize the data structures in the blob
            
           
            //save it all
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            var paths = new[] { objPath };
            AssetDatabase.ForceReserializeAssets(paths ,ForceReserializeAssetsOptions.ReserializeAssets);
            //ensure proper asset save time is updated. Can cause infrequent errors w/o the next 2 lines. 
            //If not here, may cause error #4 (time on disk != time in assetDB) when inspected.
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            callback.Invoke(true);

        }
        
        

        

        /// <summary>
        /// Create a thumbnail icon from the contents of a Bundle
        /// </summary>
        /// <param name = "source" >A TileCellsWrapper instance</param>
        /// <param name="path"></param>
        /// <param name = "selection" ></param>
        /// <param name = "bundle" ></param>
        /// <param name = "createBundleTile" ></param>
        /// <param name="existingIcon"></param>
        /// <returns></returns>
        /// <remarks>Note that the Bundle should still be open and not finally saved yet since
        /// the bundle has the icon added to it.</remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        internal static async Task<bool> CreateThumbnail(TileCellsWrapper source,
                                                         string           path,
                                                         BoundsInt        selection,
                                                         TpTileBundle     bundle,
                                                         bool             createBundleTile = false,
                                                         Sprite?          existingIcon     = null)
        {
            var destroyTempTex = existingIcon == null;

            var outputTex = existingIcon == null
                                ? TpImageLib.CreateMultipleTilesIcon(source).tex
                                : existingIcon.texture;

            if (outputTex != null)
            {
                var bytes = outputTex.EncodeToPNG();
                if(destroyTempTex) //don't want to destroy the source sprite if it was provided...
                    UnityEngine.Object.DestroyImmediate(outputTex);

                var    basePath = Path.GetDirectoryName(path);
                string iconPath;
                if (!string.IsNullOrWhiteSpace(basePath))
                    iconPath = Path.Combine(basePath, $"{bundle.name}_Icon.png");
                else
                    return false;


                await File.WriteAllBytesAsync(iconPath, bytes, Application.exitCancellationToken);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

                var tObj = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);

                //update the importer
                // ReSharper disable once AccessToStaticMemberViaDerivedType
                var importer = TextureImporter.GetAtPath(iconPath) as TextureImporter;
                if (importer == null)
                    Debug.LogError($"**Could not locate texture importer for sprite at path {iconPath} ");
                else
                {
                    EditorUtility.SetDirty(tObj);
                    importer.isReadable       = true;
                    importer.textureType      = TextureImporterType.Sprite;
                    importer.mipmapEnabled    = false;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    //AssetDatabase.WriteImportSettingsIfDirty(iconPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                }

                await Task.Delay(1000);

                //load the sprite for later assignments to the assets
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
                if (sprite != null)
                    bundle.m_Icon = sprite;
            }

            if (!createBundleTile) //need to create a TpBundleTile
                return true;

            //create and set up the TpBundleTile
            var sObj = ScriptableObject.CreateInstance<TpBundleTile>();
            if (sObj == null)
            {
                Debug.LogError("Could not create TpBundle tile instance. Cancelling operation.");
                return false;
            }

            sObj.m_TileBundle = bundle;
            var assetName = sObj.m_TileBundle.name + "_Tile";

            //populate the sprite field.
            if (bundle.m_Icon != null)
                sObj.sprite = bundle.m_Icon;
            else
            {
                var t = sObj.m_TileBundle.m_UnityTiles[0]?.m_UnityTile as Tile;
                if (t!=null && t.sprite) 
                    sObj.sprite = t.sprite;
                else
                    sObj.sprite = TpIconLib.FindIconAsSprite(TpIconType.InfoIcon);
            }

            //and save it.
            var tilePath = AssetDatabase.GenerateUniqueAssetPath($"{path}/{assetName}.asset");

            AssetDatabase.CreateAsset(sObj, tilePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
        }

        /// <summary>
        /// Used from a menu item to check if a tilemap has any unlocked tiles.
        /// </summary>
        [MenuItem("Tools/TilePlus/Prefabs/Unlocked Tiles test", false, 1000)]
        public static void TilemapStatus()
        {
            var go = Selection.activeTransform != null ? Selection.activeTransform.gameObject : null;
            if (go == null)
            {
                SelectOnlyOneMsg();
                return;
            }

            var map = go.GetComponent<Tilemap>();
            if (map == null)
            {
                SelectOnlyOneMsg();
                return;
            }


            if(!TpLib.IsTilemapLocked(map))
            {
                Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "{0}", "This isn't a locked Tilemap...");
                return;
            }

            map.CompressBounds();
            var bounds = map.cellBounds;
            var cells  = map.GetTilesBlock(bounds);

            var output = new List<string>();
            foreach (var tile in cells)
            {
                if(!tile)
                    continue;
                // ReSharper disable once Unity.NoNullPatternMatching
                if (tile is TilePlusBase {IsClone: true} tpb)
                    output.Add($"{tpb.name} at {tpb.TileGridPosition}");
            }

            if (output.Count == 0)
                Debug.Log($"No unlocked tiles on Tilemap {map.name}");
            else
            {
                Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "Found unlocked tiles on Tilemap {0}...", map.name);

                foreach (var s in output)
                    Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "{0}", s);
            }
        }

        private static void SelectOnlyOneMsg()
        {
            EditorUtility.DisplayDialog("OOPS!", "For this to work, select a single Tilemap. Try again...", "Continue");
        }
        
        private static void OopsMessage(string msg = "")
        {
            var message = string.IsNullOrWhiteSpace(msg) ? "For this to work, select a Tilemap or a GameObject in a Scene, with one or more child Tilemaps. Try again..." : msg;
            EditorUtility.DisplayDialog("OOPS!", message, "Exit");

        }

        
        private static string GetScenePathUpwards(GameObject current)
        {
            var s = current.name;
            var t = current.transform;
            while ((t = t.parent) != null)
                s = $"{s}.{t.name}";
            return s;
        }
        
        /// <summary>
        /// Menu item handler for Bundle
        /// </summary>
        [MenuItem("GameObject/TilePlus Bundler",false,1000)]
        public static void Bundle()
        {
            MakeBundle();
        }

        /// <summary>
        /// Validator for 'Bundle' command
        /// </summary>
        /// <returns>true if GO has grid or tilemap component</returns>
        [MenuItem("GameObject/TilePlus Bundler",true,1000)]
        public static bool BundleValidate()
        {
            if (Selection.activeTransform == null)
                return false;
            var go = Selection.activeGameObject;
            if (PrefabUtility.IsPartOfAnyPrefab(go))
                return false;
            return go.TryGetComponent<Grid>(out _) /*|| go.TryGetComponent<Tilemap>(out _)*/;
        }



        
        
        
        
        
   }
}
