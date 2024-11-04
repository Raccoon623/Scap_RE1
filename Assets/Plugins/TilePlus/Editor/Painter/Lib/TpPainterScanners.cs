// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 11-10-2022
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 03-26-2024
// ***********************************************************************
// <copyright file="TpPainterScanners.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;
using static TilePlus.TpLib;
#nullable enable

namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// Scriptable Singleton used to scan for tilemaps and Unity Palettes.
    /// Also scans for TpPainter transform assets.
    /// </summary>
    public class TpPainterScanners : ScriptableSingleton<TpPainterScanners> 
    {
        #region scannerDataForTilemaps

        private          List<Tilemap>             tilemapScannerList         = new(8);
        private readonly HashSet<Tilemap>          previousTilemapsHash       = new(8);
        private readonly HashSet<Tilemap>          currentTilemapsHash        = new(8);
        private          List<Tilemap>             currentlyAvailableTilemaps = new(8);
        private readonly Dictionary<int, string>   mapIdToMapNameMap          = new(8);
        private readonly Dictionary<Tilemap, Grid> tilemapToGridDict          = new(8);
        private readonly List<string>              allTilemapTags             = new(4);
        private readonly Dictionary<int, int>      perPaletteUsedTiles        = new(4);
        private readonly HashSet<string>           gridPaletteGuidHash        = new(8);
        private readonly List<GameObject>          paletteGameObjectCache     = new(8);
        private readonly List<TilemapData>         tilemapListViewItems       = new(8);
        private readonly List<TpPainterClipboard>  objectsToInspect         = new(128);

        
        /// <summary>
        /// # of tilemap data items
        /// </summary>
        internal int               NumCurrentTilemapListViewItems => tilemapListViewItems.Count;

        /// <summary>
        /// Objects to display in rh column in paint mode.
        /// </summary>
        internal List<TpPainterClipboard> ObjectsToInspect => objectsToInspect;

        #endregion
        
        
        
        #region scannerDataForPalettes
        /// <summary>
        /// source for the list of palettes, tilefabs, chunks, and Favorites when in Palettes view.
        /// </summary>
        private readonly List<PaintableObject> currentlyAvailablePalettes = new(32);
        /// <summary>
        /// Active palettes
        /// </summary>
        internal List<PaintableObject> CurrentPalettes => currentlyAvailablePalettes;
        
        /// <summary>
        /// Matching had to be inhibited, retry later.
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        internal bool RescanTilefabs { get; set; }
        
        #endregion
        
        #region events
        
        void OnEnable()
        {
            GridPaintingState.paletteChanged += OnPaletteChanged;
            ResetTilemapScanData();
            ResetPaletteScanData();
            if(TpLibEditor.Informational)
                TpLog("TpPainterScanners Scriptable Singleton initialized.");
        }

        private void OnDisable()
        {
            GridPaintingState.paletteChanged -= OnPaletteChanged;
            if(TpLibEditor.Informational)
                TpLog("TpPainterScanners ScriptableSingleton released.");

        }


        internal void Reset()
        {
            ResetTilemapScanData();
            ResetPaletteScanData();
        }
        #endregion
        
        #region resetters
        
        /// <summary>
        /// Reset the tilemap data structures
        /// </summary>
        internal  void ResetTilemapScanData()
        {
            perPaletteUsedTiles.Clear();
            tilemapScannerList.Clear();
            previousTilemapsHash.Clear();
            currentTilemapsHash.Clear();
            currentlyAvailableTilemaps.Clear(); //this could possibly be null, see LINQ code below
            mapIdToMapNameMap.Clear();
            tilemapToGridDict.Clear();
            allTilemapTags.Clear();  
            tilemapListViewItems.Clear();
            MoreThanOneGrid = false;

        }

        /// <summary>
        /// Reset the palette data
        /// </summary>
        internal  void ResetPaletteScanData()
        {
            currentlyAvailablePalettes.Clear();
            gridPaletteGuidHash.Clear();
            paletteGameObjectCache.Clear();
            objectsToInspect.Clear();

        }
        
        #endregion

        #region Scanners

        private void ValidateMapCache()
        {
            //are there any cached tilemap instances which are now null?
            var valid = currentlyAvailableTilemaps.Any(map => map != null);
            //if that's the case then rescan for Tilemaps.
            if (!valid)
                TilemapsScan();
        }
        
        /// <summary>
        /// Scans scene for tilemaps
        /// </summary>
        /// <param name="testForChanges">test for changes to # or instances of tilemaps</param>
        /// <returns>TRUE for changes</returns>
        internal  bool TilemapsScan(bool testForChanges = false)
        {
            tilemapScannerList.Clear();
            tilemapToGridDict.Clear();
            allTilemapTags.Clear();
            MoreThanOneGrid = false;
            foreach (var scene in GetAllScenes())
                GetTilemapsInScene(scene, ref tilemapScannerList, true);
            
            if(tilemapScannerList.Count == 0)
            {
                currentlyAvailableTilemaps = tilemapScannerList; //zero-length list
                return true;
            }

            previousTilemapsHash.Clear();
            for(var i= 0; i < currentlyAvailableTilemaps.Count; i++)
                previousTilemapsHash.Add(currentlyAvailableTilemaps[i]);
            
            currentlyAvailableTilemaps = tilemapScannerList.Where(map =>
                                                                    {
                                                                        GameObject gameObject;
                                                                        return map.layoutGrid != null                            //eliminates palette maps
                                                                               && !map.TryGetComponent(typeof(TpNoPaint), out _) //eliminates unpaintable maps
                                                                               && (gameObject = map.gameObject).hideFlags != HideFlags.NotEditable
                                                                               && gameObject.hideFlags != HideFlags.HideAndDontSave;
                                                                    }).ToList();
            
            if (TilePlusPainterConfig.TpPainterTilemapSorting)
            {
                currentlyAvailableTilemaps = currentlyAvailableTilemaps.OrderBy
                (tmap =>
                {
                    var renderer = tmap.gameObject.GetComponent<TilemapRenderer>();
                    return renderer.sortingLayerID;

                }).ThenBy(map =>
                {
                    var renderer = map.gameObject.GetComponent<TilemapRenderer>();
                    return renderer.sortingOrder;
                }).ToList();
                if (TilePlusPainterConfig.TpPainterTilemapSortingReverse)
                    currentlyAvailableTilemaps.Reverse();
            }
            else
                currentlyAvailableTilemaps = currentlyAvailableTilemaps.OrderBy(tmap => tmap.name).ToList();
            

            foreach (var map in currentlyAvailableTilemaps)
            {
                var grid = GetParentGrid(map.transform);
                if (grid != null)
                    tilemapToGridDict.Add(map, grid);
            }

            var gridHash = tilemapToGridDict.Values.ToHashSet();
            MoreThanOneGrid = gridHash.Count  > 1;
            
            var previousMapsCount = previousTilemapsHash.Count; //prior # of maps.
            
            //simplest case for different set of maps is if these two counts are different.
            if (previousMapsCount == 0 || !testForChanges || currentlyAvailableTilemaps.Count != previousMapsCount)
                return true;
            
            //a bit more work to do otherwise
            //has one of the NAMES changed?
            var nameHasChanged = false;
            var numMaps        = currentlyAvailableTilemaps.Count;
            for(var i = 0; i < numMaps; i++)
            {
                var map = currentlyAvailableTilemaps[i];
                //is this map a new map?
                if(!mapIdToMapNameMap.TryGetValue(map.GetInstanceID(), out var mapName))
                {
                    nameHasChanged = true;
                    break;
                }
                //if not, has the name changed?
                if(mapName != map.name)
                {
                    nameHasChanged = true;
                    break;
                }
            }

            mapIdToMapNameMap.Clear();
            for(var i = 0; i < numMaps; i++)
            {
                var map = currentlyAvailableTilemaps[i];
                mapIdToMapNameMap.Add(map.GetInstanceID(),map.name);
                if(map.CompareTag("Untagged"))
                    continue;
                allTilemapTags.Add(map.tag);
            }

            if (nameHasChanged)
                return true;
            
            //are there any changes to the number of maps?
            currentTilemapsHash.Clear();
            for(var i= 0; i < currentlyAvailableTilemaps.Count; i++)
                currentTilemapsHash.Add(currentlyAvailableTilemaps[i]);
            var same    = previousTilemapsHash.SetEquals(currentTilemapsHash);
            return !same;
        }

        /// <summary>
        /// Get a grid corresponding to the tilemap
        /// </summary>
        /// <param name="map">tilemap</param>
        /// <param name="grid">output placed here</param>
        /// <returns></returns>
        public  bool GetGridForTilemap(Tilemap map, out Grid? grid)
        {
            if (map != null)
                return tilemapToGridDict.TryGetValue(map, out grid);
            grid = null;
            return false;
        }
        
        /// <summary>
        /// Is there more than one Grid?
        /// </summary>
        public  bool MoreThanOneGrid { get; private set; }
        
        
        /// <summary>
        /// Scans for palettes, chunks, tilefabs
        /// </summary>
        internal  void PalettesScan(string searchString)
        {
            RescanTilefabs = false;
            currentlyAvailablePalettes.Clear();
            perPaletteUsedTiles.Clear();
           
            //when this is true, only show chunk-tilefabs with proper chunk size
            var chunkSizeCheck       = TilePlusPainterConfig.PainterFabAuthoringMode;
            var chunkSize            = TilePlusPainterConfig.PainterFabAuthoringChunkSize;
            var onlyMatchingTilefabs = TilePlusPainterConfig.TpPainterShowMatchingTileFabs;
            var filter    = searchString.Trim().ToLowerInvariant();
            var useFilter = filter != string.Empty;
            
            if (!chunkSizeCheck && TilePlusPainterConfig.TpPainterShowPalettes)
            {
                //NOTE: using GridPaintingState.palettes can induce asset importing
                var pGuids = AssetDatabase.FindAssets("t:GridPalette");
                var hash   = pGuids.ToHashSet();
                if (hash.SetEquals(gridPaletteGuidHash))  //same? If no then no changes, use cache
                {
                    if (Informational)
                        TpLog("TpPainterScanners.PaletteScan re-used same palettes");
                    foreach (var go in paletteGameObjectCache)
                    {
                        if (useFilter)
                        {
                            var assetName = go.name.Split('(');
                            if (assetName != null
                                && assetName.Length != 0
                                && assetName[0] != string.Empty
                                && !assetName[0].ToLowerInvariant().Contains(filter))
                                continue;
                        }

                        currentlyAvailablePalettes.Add(new PaintableObject(go));
                    }
                    
                }
                
                
                if (Informational)
                    TpLog("TpPainterScanners.PaletteScan rescanning palettes...");

                //clear cache
                paletteGameObjectCache.Clear();
                currentlyAvailablePalettes.Clear();
                
                //update hashset
                gridPaletteGuidHash.Clear();
                foreach (var guid in hash)
                    gridPaletteGuidHash.Add(guid);

                for (var i = 0; i < pGuids.Length; i++)
                {
                    var pal = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(pGuids[i]), typeof(GridPalette)) as GridPalette;
                    if (pal == null)
                        continue;
                    var palGo = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(pal)) as GameObject;
                    if (palGo == null)
                        continue;
                    if (palGo.layer != 0)
                        continue;

                    paletteGameObjectCache.Add(palGo);
                }
                paletteGameObjectCache.Sort(GameObjNameComparison);

                foreach( var paletteGo in paletteGameObjectCache)
                { 
                    if (useFilter)
                    {
                        var assetName = paletteGo.name.Split('(');
                        if (assetName != null
                            && assetName.Length != 0
                            && assetName[0] != string.Empty
                            && !assetName[0].ToLowerInvariant().Contains(filter))
                            continue;
                    }

                    var po = new PaintableObject(paletteGo);
                    currentlyAvailablePalettes.Add(po);
                }

                //local
                int GameObjNameComparison(GameObject x, GameObject y)
                {
                    return string.CompareOrdinal(x.name, y.name);
                }
            
            }
            
            

            if (!chunkSizeCheck && TilePlusPainterConfig.TpPainterShowTileBundles)
            {
                //find Bundles
                var guids = AssetDatabase.FindAssets("t:TpTileBundle");
                for (var i = 0; i < guids.Length; i++)
                {
                    var bundle = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[i]), typeof(TpTileBundle)) as TpTileBundle;
                    if (bundle == null)
                        continue;
                    if (useFilter)
                    {
                        var assetName = bundle.name.Split('(');
                        if (assetName != null
                            && assetName.Length != 0
                            && assetName[0] != string.Empty
                            && !assetName[0].ToLowerInvariant().Contains(filter))
                            continue;

                    }

                    var chunk = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(bundle)) as TpTileBundle;
                    if (chunk == null || chunk.m_IgnoreInPainter)
                        continue;
                    

                    currentlyAvailablePalettes.Add(new PaintableObject(chunk));
                }
            }

            // ReSharper disable once InvertIf
            if (chunkSizeCheck || TilePlusPainterConfig.TpPainterShowTilefabs) 
            {
                //find tilefabs
                var guids          = AssetDatabase.FindAssets("t:TpTileFab");
                for (var i = 0; i < guids.Length; i++)
                {
                    var fabAsset = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[i]), typeof(TpTileFab)) as TpTileFab;
                    if (fabAsset == null)
                        continue;
                    if (useFilter)
                    {
                        var assetName = fabAsset.name.Split('(');
                        if (assetName != null
                            && assetName.Length != 0
                            && assetName[0] != string.Empty
                            && !assetName[0].ToLowerInvariant().Contains(filter))
                            continue;
                    }


                    var fab = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(fabAsset)) as TpTileFab;
                    if (fab == null || fab.m_IgnoreInPainter || fab.m_TileAssets == null || fab.m_TileAssets.Count == 0)
                        continue;
                    
                    if(chunkSizeCheck && (!fab.m_FromGridSelection || fab.LargestBounds.size.x != chunkSize || fab.LargestBounds.size.y != chunkSize))  
                       continue;

                    var skipThis = false;
                    if (onlyMatchingTilefabs)
                    {
                        var allNames = mapIdToMapNameMap.Values.ToList(); //all tilemap names not inhibited by 'no paint'
                        if (allNames.Count == 0)                            //if no tilemaps check again
                        {
                            TilemapsScan(true); 
                            allNames = mapIdToMapNameMap.Values.ToList();
                            if (allNames.Count == 0)
                            {
                                RescanTilefabs = true;
                                if(TpLibEditor.Warnings) 
                                    TpLogWarning("No Tilemaps found when looking for matching Tilefab map names. Matching ignored");
                            }
                        }

                        if (allNames.Count != 0)
                        { 
                            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
                            foreach (var spec in fab.m_TileAssets)
                            {
                                //if the map name isn't found and the tag isn't found then skip this fab
                                if (allNames.Contains(spec.m_TilemapName) || allTilemapTags.Contains(spec.m_TilemapTag))
                                    continue;
                                skipThis = true;
                                //Debug.Log("Match fail " + spec.m_TilemapName);
                                break;
                            }
                        }
                    }
                    if(!skipThis)
                        currentlyAvailablePalettes.Add(new PaintableObject(fab));
                }
                
            }
        }
        
        /// <summary>
        /// Get used tiles for palette
        /// </summary>
        /// <param name="palette">a UTE palette ref</param>
        /// <returns># tile-types in palette</returns>
        /// <remarks>Use when #tile-types to be used a lot as its slow. Return value of -1
        /// indicates size not provided: typically this is because the TpNoPaint component is placed on a palette.
        /// That's handy for huge palettes because GetUsedTilesCount() is slow and blocks execution.</remarks>
        public int GetUsedTilesForPalette(GameObject palette)
        {
            if (perPaletteUsedTiles.Count != 0)
                return perPaletteUsedTiles.GetValueOrDefault(palette.GetInstanceID(), -1);
            GetUsedTilesForPalettes();
            return perPaletteUsedTiles.GetValueOrDefault(palette.GetInstanceID(), -1);
        }
        private void GetUsedTilesForPalettes()
        {
            if (Application.isPlaying) 
                return;
            foreach (var po in currentlyAvailablePalettes.Where(
                                                                po => po.ItemType == TpPaletteListItemType.Palette
                                                                && po.Palette != null
                                                                && !po.Palette.TryGetComponent<TpNoPaint>(out _)))
                perPaletteUsedTiles.TryAdd(po.Palette!.GetInstanceID(), po.Palette.GetComponentInChildren<Tilemap>().GetUsedTilesCount());
        }

        private void OnPaletteChanged(GameObject? _)
        {
            perPaletteUsedTiles.Clear();
            GetUsedTilesForPalettes();
        }

        #endregion
        
        #region sources
        /// <summary>
        /// Data source for the list of tilemaps in the LEFTMOST column
        /// </summary>
        /// <value>ListView items.</value>
        internal List<TilemapData> TilemapListViewItems
        {
            get
            {
                tilemapListViewItems.Clear();
                TpPainterScanners.instance.ValidateMapCache();
                foreach (var map in currentlyAvailableTilemaps)
                {
                    var stage      = PrefabStageUtility.GetCurrentPrefabStage();
                    var objInStage = stage != null && stage.IsPartOfPrefabContents(map.gameObject);
                    tilemapListViewItems.Add(new TilemapData(map, PrefabUtility.IsPartOfPrefabInstance(map.gameObject),objInStage));
                }

                return tilemapListViewItems;
            }
        }
        
        
        
        /// <summary>
        /// given a source of objects (palette, favs, etc)
        /// creates a list of data to display in the rightmost pane in PAINT mode.
        /// </summary>
        /// <param name = "source" >Source of things to paint: tiles, prefabs, bundles, etc.</param>
        /// <returns>true if the source was a palette and it is oversized as defined by TilePlusPainterConfig.MaxTilesForViewers</returns>
        [SuppressMessage("ReSharper", "Unity.NoNullPatternMatching")]
        internal bool PaintModeGetInspectables(PaintableObject? source)
        {
            objectsToInspect.Clear();
            if (source == null)
                return false;
            var oversizedPalette = false;
            
            //since the Favorites List can have clone TPT tiles and the stack is saved via serialization, lose
            //any null values in the stack that may have persisted if a clone TPT tile was added to the stack
            //and the window closed/reopened or Unity restarts. This is also important for Prefabs in the
            //Favorites List since they could have been deleted.

            TilePlusPainterFavorites.CleanFavoritesList();
            //which source did this data come from?
            //A palette, Favorites, bundle, or Tilefab ?
            if (source.ItemType == TpPaletteListItemType.Palette)
            {
                if(source.Palette == null)
                    return false;
                
                var map = source.Palette.GetComponentInChildren<Tilemap>();
                if (map == null)
                    return false;
                
                
                
                map.CompressBounds();
                var        bounds     = map.cellBounds;
                var        count      = 0;
                var        maxPalSize = TilePlusPainterConfig.MaxTilesForViewers;
                //unfortunately we need the positions so have to do this.
                foreach (var pos in bounds.allPositionsWithin)
                {
                    var t = map.GetTile(pos);
                    if (t == null)
                        continue;
                    objectsToInspect.Add (new TpPainterClipboard(t, pos, map));
                    if (++count <= maxPalSize)
                        continue;
                    oversizedPalette = true;
                    break;
                }
                if (objectsToInspect.Count > 1)
                    objectsToInspect.Reverse();
                
            }

            else if (source.ItemType == TpPaletteListItemType.Bundle)
            {
                if (TilePlusPainterConfig.TpPainterShowBundleAsPalette && source.Bundle != null)
                {
                    foreach(var item in source.Bundle.m_TilePlusTiles)
                        objectsToInspect.Add(new TpPainterClipboard(item.m_Tile, Vector3Int.one, null, false,true));
                    foreach(var item in source.Bundle.m_UnityTiles)
                        objectsToInspect.Add(new TpPainterClipboard(item.m_UnityTile, Vector3Int.one, null, false, true));
                    foreach(var item in source.Bundle.m_Prefabs)
                        objectsToInspect.Add(new TpPainterClipboard(item.m_Prefab));
                }
                else
                    objectsToInspect.Add(new TpPainterClipboard(source.Bundle));
            }

            else if (source.ItemType == TpPaletteListItemType.TileFab)
                objectsToInspect.Add(new TpPainterClipboard(source.TileFab));
            
            else if (source.ItemType == TpPaletteListItemType.Favorites)
            {
                //nb - skipping null checks is OK since Favorites are never null
                objectsToInspect.AddRange(TilePlusPainterFavorites.instance.Favorites
                                                      .Where(o => o is GameObject or TileBase or TileCellsWrapper or TpTileBundle)
                                                      .Select(obj =>
                                                       {
                                                           var cbItem = obj switch
                                                                                {
                                                                                    TileBase tb              => new TpPainterClipboard(tb,TilePlusBase.ImpossibleGridPosition, null),
                                                                                    GameObject go            => new TpPainterClipboard(go),
                                                                                    TileCellsWrapper wrapper => new TpPainterClipboard(wrapper),
                                                                                    TpTileBundle bundle => new TpPainterClipboard(bundle),
                                                                                    _                        => new TpPainterClipboard()
                                                                                };
                                                           cbItem.IsFromFavorites = true;
                                                           return cbItem;
                                                       }));
                
            }
            //note ImpossibleGridPosition tells TppClipboardItem to ignore null tilemap source param

            return oversizedPalette;
        }
        
        
    #endregion
    }
}
