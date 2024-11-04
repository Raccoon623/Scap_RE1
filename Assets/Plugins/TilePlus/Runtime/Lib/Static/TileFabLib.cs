#nullable enable
// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 02-17-2022
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 02-17-2022
// ***********************************************************************
// <copyright file="TileFabLib.cs" company="Jeff Sasmor">
//     Copyright (c) 2022 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using static TilePlus.TpLib;
using Object = UnityEngine.Object;


namespace TilePlus
{
    /// <summary>
    /// Utilities for Tilefab loading
    /// </summary>
    #if UNITY_EDITOR 
    [InitializeOnLoad]
    #endif
    public static class TileFabLib
    {
        #region ctor
        
        #if UNITY_EDITOR
        static TileFabLib()
        {
            EditorApplication.playModeStateChanged += x =>
                                                      {
                                                          if (x is not (PlayModeStateChange.ExitingEditMode
                                                                        or PlayModeStateChange.ExitingPlayMode))
                                                              return;
                                                          s_RegistrationIndex = 1;
                                                          if (AnyActiveZoneManagers)
                                                              EnableZoneManagers(false);
                                                      };

            if (s_Instances != null && s_Instances.Count != 0)
            {
                foreach(var zm in s_Instances.Values)
                    Object.Destroy(zm);
            }
            s_Instances?.Clear();
            m_LoadedGuids?.Clear();
            s_RegistrationIndex = 1;
            s_LoadedGuidLookup?.Clear();
            s_ReverseLoadedGuidLookup?.Clear();
            s_FabGuidToFabMap?.Clear();
        }
        #endif
        
        
        
        #endregion

        
       
        #region Loading

        /// <summary>
        /// load several tilefabs at once from a list of specifications.
        /// </summary>
        /// <param name="loadParams">A list of TileFabLoadParams</param>
        /// <param name="loadResultsList">if not null, TileFab LoadResults added to list. Note it is NOT cleared here.</param>
        /// <param name = "zoneManager" >the ZoneManager instance, if you're using it.</param>
        /// <returns>false if any errors</returns>
        public static bool LoadTileFabs(List<TileFabLoadParams>      loadParams,
                                        ref List<TilefabLoadResults> loadResultsList,
                                        TpZoneManager?               zoneManager = null)
        {
            foreach(var p in loadParams)
            {
                if (p.m_Reserved && zoneManager != null)
                {
                    //mark this zone as reserved. For ZoneLayout that's only used when filling-in empty zones.
                    //reserved zones will be removed by a ZoneLayout when outside of the Camera Viewport.
                    //for complete persistence use the immortal flag.
                    var locator = zoneManager.GetLocatorForGridPosition(p.m_Offset);
                    if (zoneManager.HasZoneRegForLocator(locator))
                        continue;  //shouldn't happen...

                   
                    //note that zero isn't normally an index: they begin at 1
                    var reg = new ZoneReg(0, 
                                          locator,
                                          p.m_Offset);
                    zoneManager.AddRegistration(reg, new AssetGuidPositionHash(new Guid(), Vector3Int.zero));
                    continue;
                }

                if (p.m_Immortal)
                    p.m_LoadFlags |= FabOrBundleLoadFlags.MarkZoneRegAsImmortal;
                
                var results = LoadTileFab(p.m_TileParent,
                                          p.m_TileFab,
                                          p.m_Offset,
                                          p.m_Rotation,
                                          p.m_LoadFlags,  
                                          p.m_Filter,
                                          p.m_TargetMap,
                                          zoneManager);
                if (results == null)
                    continue;
                loadResultsList.Add(results);
            }

            return true;
        }

        

    
        
        /// <summary>
        /// Load one or more tilemaps and associated prefabs, if any.
        /// </summary>
        /// <param name="tileParent">the parent of the calling tile (if any, can be null if not a tile)</param>
        /// <param name="tileFab">a TpTileFab asset</param>
        /// <param name="offset">optional offset from tiles' stored positions</param>
        /// <param name="rotation">optional rotation (not implemented)</param>
        /// <param name="fabOrBundleLoadFlags">Flags from the LoadFlags enum. </param>
        /// <param name = "filter" >a Func of returning a bool. See also LoadBundle. </param>
        /// <param name = "targetMap" >A dictionary mapping tilemap names as found in the TileFab to actual Tilemap instances. Used to override the names from the Tilefab. Case-sensitive. It's also much faster than a name lookup as done without a provided mapping.</param>
        /// <param name = "zoneManagerInstance" >If using TpZoneManager, pass its instance.</param>
        /// <returns>Instance of TilefabLoadResults record. If null is returned then there was an error of some kind.</returns>
        public static TilefabLoadResults? LoadTileFab(Tilemap?                                              tileParent,
                                                      TpTileFab?                                            tileFab,
                                                      Vector3Int                                            offset,
                                                      TpTileBundle.TilemapRotation                          rotation             = TpTileBundle.TilemapRotation.Zero,
                                                      FabOrBundleLoadFlags                                  fabOrBundleLoadFlags = FabOrBundleLoadFlags.NormalWithFilter,
                                                      Func<FabOrBundleFilterType, BoundsInt, object, bool>? filter               = null,
                                                      Dictionary<string, Tilemap>?                          targetMap            = null,
                                                      TpZoneManager?                                        zoneManagerInstance  = null
            )
        {
            if (tileFab == null)
            {
                TpLogError("Input to LoadImportedTilefab error: tilefab==null");
                return null;
            }
            
            //loading the same TileFab at the same offset is ignored.
            var usingZoneManager = zoneManagerInstance != null;
            if(usingZoneManager)
            {
                var locator            = zoneManagerInstance!.GetLocatorForGridPosition(offset);
                if(zoneManagerInstance.HasZoneRegForLocator(locator))
                {
                    if(Warnings)
                        TpLogWarning($"Loading the same TileFab [{tileFab.name} guid:{tileFab.AssetGuidString}] to the same place: [locator:{locator}] ignored");
                    return new TilefabLoadResults(locator);
                    //note that the solution to this is to A. not provide a ZoneManager instance or B. delete the zone first.
                }
            }

            var startTime = Time.realtimeSinceStartup;

            #if UNITY_EDITOR
            if (Informational)
                TpLog($"Loading from TileFab {tileFab.name}");
            #endif

            var loadedAssets     = new List<TpTileBundle>(tileFab.m_TileAssets!.Count);
            
            //get the grid that's the parent of the caller's parent tilemap.
            var grid       = tileParent != null ? GetParentGrid(tileParent.transform) : null;

            var numBundles       = tileFab.m_TileAssets.Count;
            var posToGuidMaps    = new Dictionary<Vector3Int, string>[numBundles];
            var bundleAssetGuids = new string[numBundles];
            var bundleAssetNames = new string[numBundles];
            var arrIndex         = 0;
            var allLoadedPrefabs = new List<GameObject>();

            foreach (var assetSpec in tileFab.m_TileAssets)
            {
                var map = FindTilemap(assetSpec.m_TilemapName, assetSpec.m_TilemapTag, grid, targetMap);
                if (map == null)
                    continue;


                (var posToGuidMap, var loadedPrefabs)
                    = LoadBundle(assetSpec.m_Asset, map, offset, rotation, fabOrBundleLoadFlags, filter);
                
                if(loadedPrefabs != null)
                    allLoadedPrefabs.AddRange(loadedPrefabs);
                loadedAssets.Add(assetSpec.m_Asset);
                posToGuidMaps[arrIndex]      = posToGuidMap;
                bundleAssetNames[arrIndex]   = assetSpec.m_Asset.name;
                bundleAssetGuids[arrIndex++] = assetSpec.m_Asset.AssetGuidString;
                if (Informational)
                    TpLog($"Loading: tag = {assetSpec.m_TilemapTag}, name = {map}.");
            }

            var thisTime     = Time.realtimeSinceStartup - startTime;
            var changedGuids =  (fabOrBundleLoadFlags & FabOrBundleLoadFlags.NewGuids) != 0;
            var s            = $"Tilefab {tileFab.name} loading time mode: {thisTime:#.000000} [Register: {usingZoneManager.ToString()}] [New GUIDs: {changedGuids.ToString()}]  ";
            if (targetMap != null)
            {
                var mapNames = targetMap.Keys.Aggregate("Maps retargeted: ", (current, k) => $"{current} {k}");
                s = $"{s}\n{mapNames}\n";
            }

            ZoneReg? possibleRegistration = null;
            var  possibleLocator      = new RectInt();
            if (!usingZoneManager)
                return new TilefabLoadResults(loadedAssets, possibleRegistration, s, possibleLocator);
            else
            {
                (var registration, var locator) = zoneManagerInstance!.AddZone(tileFab,
                                                                               (fabOrBundleLoadFlags & FabOrBundleLoadFlags.MarkZoneRegAsImmortal) != 0,
                                                                               offset,
                                                                               rotation,
                                                                               posToGuidMaps,
                                                                               bundleAssetGuids,
                                                                               bundleAssetNames,
                                                                               allLoadedPrefabs);

                possibleRegistration = registration;
                possibleLocator      = locator;
            }

            return new TilefabLoadResults(loadedAssets,possibleRegistration, s,possibleLocator);
        }

        /// <summary>
        /// Find a tilemap given an asset spec from a TileFab
        /// </summary>
        /// <param name="spec">The Asset Spec from a TileFab</param>
        /// <returns>Found Tilemap or null</returns>
        public static Tilemap? FindTilemap(TpTileFab.AssetSpec spec)
        {
            return FindTilemap(spec.m_TilemapName, spec.m_TilemapTag);
        }
        
        

        /// <summary>
        /// Find a tilemap. 
        /// </summary>
        /// <param name="tilemapName">possible name to use</param>
        /// <param name="tilemapTag">possible tag</param>
        /// <param name="grid">grid ref if available</param>
        /// <param name="targetMap">map from tilemap names to instances</param>
        /// <returns></returns>
        private static Tilemap? FindTilemap(string tilemapName, string tilemapTag, Grid? grid = null, Dictionary<string, Tilemap>? targetMap = null)
        {
            var useMap = targetMap != null;
            //user provided mapping from asset names to tilemap destinations. FASTEST
            if (useMap && targetMap!.TryGetValue(tilemapName, out var tileMapFromMapping))
                return tileMapFromMapping;
            //if mapping doesn't find anything, try searching
            
            //todo is "Untagged" language-invariant?
            //try using tag to find the tilemap's GameObject.
            if (tilemapTag != NoTagString)  //moderately quick
            {
                var taggedGo = GameObject.FindWithTag(tilemapTag);
                if (taggedGo != null && taggedGo.TryGetComponent<Tilemap>(out var taggedMap))
                    return taggedMap;
            }

            //if that didn't work, try using the tilemap's name.
            if (grid == null) //if no grid somehow, then try to find by name
            {
                var go = GameObject.Find(tilemapName);  //SLOWEST
                if (go != null && go.TryGetComponent<Tilemap>(out var namedMap))
                    return namedMap;
            }
            
            else //try to look thru the Grid's children for the tilemap. Speed is variable
            {
                var possibleMaps = grid.GetComponentsInChildren<Tilemap>();
                // ReSharper disable once LoopCanBePartlyConvertedToQuery
                foreach (var child in possibleMaps)
                {
                    if (child.name != tilemapName)
                        continue;
                    return child;
                }
            }

            return null;
        }
        
        
        /// <summary>
        /// Load the contents of a Bundle asset to a tilemap.
        /// Note: assumes tilemap is at 0,0,0. If not, adjust offset
        /// prior to call.
        /// ***************************************************
        /// NOTE that TpTileBundles do NOT create
        /// AssetRegistration entries, ONLY calling LoadTileFab does that.
        /// ***************************************************
        /// </summary>
        /// <param name="tileBundleAsset">the asset reference</param>
        /// <param name="map">target Tilemap</param>
        /// <param name="offset">offset to position</param>
        /// <param name="rotation">optional rotation (unsupported)</param>
        /// <param name="fabOrBundleLoadFlags">Flags from the LoadFlags enum. </param>
        /// <param name = "filter" >Func of returning a bool.
        ///  The filter should return true if the tile/prefab is acceptable or false to omit it.
        /// </param>
        /// <returns> pos-to-GUID map and list of prefabs</returns>
        /// <remarks>map.SetTiles(tileChangeData) is used: flags are not restored. When the
        /// tiles are started, the tiles' flags are used. Also, lock flags on the tilemap are ignored.</remarks>
        public static (Dictionary<Vector3Int, string> posToGuidMap, List<GameObject>? loadedPrefabs)
            LoadBundle(TpTileBundle                               tileBundleAsset,
                       Tilemap                                    map,
                       Vector3Int                                 offset,
                       TpTileBundle.TilemapRotation               rotation  = TpTileBundle.TilemapRotation.Zero,
                       FabOrBundleLoadFlags                                  fabOrBundleLoadFlags = FabOrBundleLoadFlags.NormalWithFilter,
                       Func<FabOrBundleFilterType, BoundsInt, object, bool>? filter = null
                )
                                                 
        {
            if (Informational)
            {
                var unityTilesCount    = tileBundleAsset.m_UnityTiles.Count;
                var tilePlusTilesCount = tileBundleAsset.m_TilePlusTiles.Count;
                TpLog($"Loading [{map.name}] @ offset {offset.ToString()} -- Unity Tiles {unityTilesCount}, TPT tiles {tilePlusTilesCount} ");
            }

            var              prefabs          = tileBundleAsset.m_Prefabs;
           
            List<GameObject>? loadedPrefabs = null;
            
            //load prefabs first 
            if ( (fabOrBundleLoadFlags & FabOrBundleLoadFlags.LoadPrefabs) != 0)
            {
                loadedPrefabs = new List<GameObject>(prefabs.Count);
                if ( (fabOrBundleLoadFlags & FabOrBundleLoadFlags.ClearPrefabs) != 0)
                {
                    var playing   = Application.isPlaying;
                    var transform = map.transform;
                    var nChildren = transform.childCount;
                    for (var i = 0; i < nChildren; i++)
                    {
                        var child = transform.GetChild(i);
                        #if UNITY_EDITOR
                        if (playing)
                            Object.Destroy(child);
                        else
                            Object.DestroyImmediate(child);
                        #else
                    UnityEngine.Object.Destroy(child);
                        #endif
                    }
                }

                //place prefabs.
                var trans     = map.transform;
                var useFilter = filter != null;
                var bounds    = tileBundleAsset.m_TilemapBoundsInt;
                foreach (var item in prefabs)
                {
                    if (useFilter && !filter!(FabOrBundleFilterType.Prefab, bounds, item))
                            continue;
                    #if UNITY_EDITOR
                    var prefab = PrefabUtility.InstantiatePrefab(item.m_Prefab) as GameObject;
                    #else
                    var prefab = Object.Instantiate(item.m_Prefab) as GameObject;
                    #endif
                    
                    if (prefab == null)
                        continue;
                    
                    prefab.transform.parent = trans;
                    
                    if (item.m_V3PrefabHandling) //V3 prefab handling - transform/rotation from this bundle asset
                    {
                        prefab.transform.SetPositionAndRotation(item.m_Position + offset, item.m_Rotation);
                        
                        /*var prefab= Object.Instantiate
                                              (item.m_Prefab,
                                               item.m_Position + offset,
                                               item.m_Rotation,
                                               trans);
                        if (prefab == null)
                            continue;*/
                        prefab.transform.localScale = item.m_Scale;
                        
                    }
                    else
                    {
                        prefab.transform.SetPositionAndRotation(item.m_Position + offset,item.m_Prefab.transform.rotation);
                        /*loadedPrefabs.Add(Object.Instantiate //V1/2 prefab handling - transform/rotation from the archived prefab
                                              (item.m_Prefab,
                                               item.m_Position + offset,
                                               item.m_Prefab.transform.rotation,
                                               trans));*/
                        
                    }
                    loadedPrefabs.Add(prefab);
                }
                
            }

            if ( (fabOrBundleLoadFlags & FabOrBundleLoadFlags.ClearTilemap) != 0)
                map.ClearAllTiles();


            (var data, var dict) = tileBundleAsset.TilesetChangeData(rotation,
                                                                     offset,
                                                                     fabOrBundleLoadFlags,
                                                                     filter);
            map.SetTiles(data, true);
            if ((fabOrBundleLoadFlags & FabOrBundleLoadFlags.ForceRefresh) != 0)
                map.RefreshAllTiles();
            return (dict, loadedPrefabs);
        
        }
       
        
        #endregion
        
        #region zoneManagerControl
        //PRIVATE FIELDS
        #if ODIN_INSPECTOR
        [ShowInInspector, ReadOnly]
        #endif
        private static Dictionary<string,TpZoneManager>? s_Instances;
        #if ODIN_INSPECTOR
        [ShowInInspector, ReadOnly]
        #endif
        private static bool                              s_ZoneManagerEnabled;

        /// <summary>
        /// Used for fast lookups to see if a TileFab or Bundle is already loaded
        /// </summary>
        #if ODIN_INSPECTOR
        [ShowInInspector, ReadOnly]
        #endif
        internal static HashSet<AssetGuidPositionHash>? m_LoadedGuids;

        #if ODIN_INSPECTOR
        [ShowInInspector,ReadOnly]
        #endif
        private static ulong s_RegistrationIndex;

        /// <summary>
        /// Remaps the GUIDs of TPT tiles to the new ones created when the Bundle is loaded.
        /// </summary>
        #if ODIN_INSPECTOR
        [ShowInInspector, ReadOnly]
        #endif
        private static Dictionary<Guid, Guid>? s_LoadedGuidLookup;

        //a lookup from the NEW guids of TPT tiles to the old ones from the last load.
        #if ODIN_INSPECTOR
        [ShowInInspector, ReadOnly]
        #endif
        private static Dictionary<Guid, Guid>? s_ReverseLoadedGuidLookup;
    
        //a lookup for finding TileFabs from GUIDs
        #if ODIN_INSPECTOR
        [ShowInInspector, ReadOnly]
        #endif
        private static Dictionary<string, TpTileFab>? s_FabGuidToFabMap;

        /// <summary>
        /// Get a TileFab instance from a GUID string
        /// </summary>
        /// <param name="guid">GUID string</param>
        /// <param name="fab">TileFab instance. Null if not found</param>
        /// <returns>true if found</returns>
        public static bool GetTileFabFromGuid(string guid, out TpTileFab? fab)
        {
            if (guid == string.Empty || !Guid.TryParse(guid, out _))
            {
                fab = default;
                return false;    
            }
                
            if (s_FabGuidToFabMap != null)
                return s_FabGuidToFabMap.TryGetValue(guid, out fab);
            fab = default;
            return false;

        }
        
        //PROPERTIES
        /// <summary>
        /// Any Zone Manager instances?
        /// </summary>
        #if ODIN_INSPECTOR
        [ShowInInspector]
        #endif
        public static bool AnyActiveZoneManagers => s_ZoneManagerEnabled && s_Instances != null &&  s_Instances.Count != 0;

        /// <summary>
        /// Is the creation of Zone Manager instances enabled?
        /// </summary>
        #if ODIN_INSPECTOR
        [ShowInInspector]
        #endif
        public static bool ZoneManagerCreationEnabled => s_ZoneManagerEnabled;

        //SETUP
        /// <summary>
        /// Enable/Disable the Zone Manager subsystem. 
        /// </summary>
        /// <param name="enable">on/off</param>
        /// <param name="initialNumManagers">best guess at # of managers required</param>
        /// <param name="guidTableSize">best guess at GUID translation table size</param>
        /// <param name="stringToFabMap">Optional map from string to TpTileFab. If omitted, it's created here.</param>
        /// <returns>true for success</returns>
        /// <remarks> Note that TileFabs have to be in Resource Folders to be
        /// found since they're only referred to by GUIDs - unless you create a ScriptableObject parented to some GameObject
        /// component so that the TileFabs are included in a build.</remarks>
        public static bool EnableZoneManagers(bool enable = true, int initialNumManagers = 1, int guidTableSize = 128, Dictionary<string, TpTileFab>? stringToFabMap = null)
        {
            switch (enable)
            {
                case true when s_ZoneManagerEnabled:
                    Debug.LogWarning("Zone Manager already enabled");
                    return false;
                case false when !s_ZoneManagerEnabled:
                    Debug.LogWarning("Zone Manager not enabled, can't disable.");
                    return false;
                case true:
                    s_Instances               = new Dictionary<string, TpZoneManager>(initialNumManagers);
                    s_RegistrationIndex       = 1;
                    m_LoadedGuids             = new HashSet<AssetGuidPositionHash>(guidTableSize);
                    s_LoadedGuidLookup        = new Dictionary<Guid, Guid>(guidTableSize);
                    s_ReverseLoadedGuidLookup = new Dictionary<Guid, Guid>(guidTableSize);
                    if (stringToFabMap != null)
                        s_FabGuidToFabMap = stringToFabMap;
                    else
                        s_FabGuidToFabMap     ??= FabMap(); 
                    s_ZoneManagerEnabled      =   true;
                    
                    break;
                default:
                {
                    //nullable warning supressed here for s_Instances, since logically it can't be null.
                    foreach (var x in s_Instances!.Values)
                    {
                        x.ResetInstance();
                        #if UNITY_EDITOR
                        Object.DestroyImmediate(x);
                        #else
                        Object.Destroy(x);
                        #endif
                    }

                    s_Instances               = null;
                    s_ZoneManagerEnabled      = false;
                    m_LoadedGuids             = null;
                    s_LoadedGuidLookup        = null;
                    s_ReverseLoadedGuidLookup = null;
                    s_FabGuidToFabMap         = null;
                    s_RegistrationIndex       = 1;
                    
                    break;
                }
            }

            return true;
        }
        
        //Create Fab GUID -> Fab instance mapping just once.
        private static Dictionary<string, TpTileFab>? FabMap()
        {
            if(Informational)
                TpLog("Searching for TpTileFabs in TileFabLib");
            Resources.LoadAll<TpTileBundle>(string.Empty);
            var objs = Resources.FindObjectsOfTypeAll<TpTileFab>();
            if (objs != null && objs.Length != 0)
                return objs.ToDictionary(k => k.AssetGuidString, v => v);
            return null;
        }

      
        
        /// <summary>
        /// Create a ZM instance or get an existing one.
        /// </summary>
        /// <param name="instance">out param gets the instance</param>
        /// <param name="iName">unique name. If already used, fetches existing ZM instance</param>
        /// <param name="targetMap">Tilemaps to be controlled by this ZM</param>
        /// <returns></returns>
        public static bool CreateZoneManagerInstance(out TpZoneManager? instance, string iName, Dictionary<string, Tilemap> targetMap)
        {
            #if UNITY_EDITOR
            if(!s_ZoneManagerEnabled)
                Debug.LogError("ZONE MANAGER NOT ENABLED!!");
            #endif

            if (!s_ZoneManagerEnabled || string.IsNullOrEmpty(iName))
            {
                instance = default;
                return false;
            }

            if (s_Instances!.TryGetValue(iName, out instance))
                return true;
            
            instance              = ScriptableObject.CreateInstance<TpZoneManager>();
            return instance.SetNameAndMap(iName,targetMap) && s_Instances.TryAdd(iName, instance);
        }

        /// <summary>
        /// Does the named ZoneLayout controller instance exist?
        /// </summary>
        /// <param name="iName">the unique name for the ZoneLayout</param>
        /// <returns>True if it exists</returns>
        public static bool HasNamedInstance(string iName)
        {
            return s_ZoneManagerEnabled && s_Instances!.ContainsKey(iName);
        }

        /// <summary>
        /// Delete a named ZoneLayout controller instance 
        /// </summary>
        /// <param name="iName">the unique name for the ZoneLayout</param>
        /// <param name="disableWhenNone">Auto-shutoff zone management when there are no more ZMs</param>
        /// <returns>true if the deletion was successful. False if name not found or ZoneManagement is not active </returns>
        public static bool DeleteNamedInstance(string iName, bool disableWhenNone=true)
        {
            if (!s_ZoneManagerEnabled || !s_Instances!.TryGetValue(iName, out var instance))
                return false;
            if(disableWhenNone && s_Instances.Count == 1) //ie removing this zone leaves no more zones.
                return EnableZoneManagers(false);
            
            instance.ResetInstance();
            Object.Destroy(instance);
            return s_Instances.Remove(iName);
        }

        
        /// <summary>
        /// Get a named ZoneLayout controller instance
        /// </summary>
        /// <param name="iName">the unique name for the ZoneLayout</param>
        /// <param name="instance">an out param for the instance</param>
        /// <returns>true if found, false if not. If not found, instance is set to null.</returns>
        public static bool GetNamedInstance(string iName, out TpZoneManager? instance)
        {
            #if UNITY_EDITOR
            if(!s_ZoneManagerEnabled)
                Debug.LogError("ZONE MANAGER NOT ENABLED!!");
            #endif
            instance = null;
            return s_ZoneManagerEnabled && s_Instances!.TryGetValue(iName, out  instance);
        }
        
        
        /// <summary>
        /// Attempt to remap a GUID.
        /// </summary>
        /// <param name="oldGuid">the old one</param>
        /// <param name="newGuid">the remapped one</param>
        /// <returns>true if the remap was successful</returns>
        public static bool RemapTileGuid(Guid oldGuid, out Guid newGuid)
        {
            newGuid = default;
            
            return s_ZoneManagerEnabled && s_LoadedGuidLookup!.TryGetValue(oldGuid, out newGuid);
        }

        
        /// <summary>
        /// Delete from the loadedGuidLookup when you are not using the built-in system.
        /// It's important to remove these when you are deleting a TileFab or you'll have a memory leak;
        /// </summary>
        /// <param name="alias">The current Guid for a tile</param>
        /// <returns>false if error: should consider this a fatal error.</returns>
        public static bool RemoveFromGuidLookup(Guid alias)
        {
            if (!s_ZoneManagerEnabled)
                return false;
            
            if (s_ReverseLoadedGuidLookup!.TryGetValue(alias, out var oldGuid))
                return false;

            if (!s_ReverseLoadedGuidLookup.Remove(alias))
                return false;

            try
            {
                return s_LoadedGuidLookup!.Remove(oldGuid);
            }
            catch
            {
                return false;
            }

        }

        /// <summary>
        /// Update the GUID lookup. Used to alias previously-used TPT tile GUIDs to new TPT tile GUIDs.
        /// </summary>
        /// <param name="oldReg">the previously-used (saved) reg</param>
        /// <param name="newReg">the new reg, with new GUIDs, after loading</param>
        public static void UpdateGuidLookup(ZoneReg oldReg, ZoneReg newReg )
        {
            TpZoneManagerLib.UpdateGuidLookup(oldReg, newReg,
                ref s_LoadedGuidLookup, ref s_ReverseLoadedGuidLookup );
        }
        
        /// <summary>
        /// Add to the loadedGuidLookup so that TpLib's Guid-to-tile lookup works.
        /// This is only for use if you aren't using the built-in system.
        /// </summary>
        /// <param name="original">Previous Guid from when the game state was saved</param>
        /// <param name="alias">alias Guid from TileFab loading's guid change operation or elsewhere</param>
        /// <returns>true unless there was an existing and matching Guid key in either dictionary. Treat
        /// that as a fatal error.</returns>
        public static bool AddGuidLookup(Guid original, Guid alias)
        {
            if (!s_ZoneManagerEnabled)
                return false;
            return s_LoadedGuidLookup!.TryAdd(original, alias) && s_ReverseLoadedGuidLookup!.TryAdd(alias, original);
        }

        /// <summary>
        /// Remove a GUID lookup. Used when deleting a ZoneReg
        /// </summary>
        /// <param name="bundleGuidMap">the mapping from the active ZoneReg (i.e., one generated after loading a TileFab)</param>
        /// <returns>true if this worked. False if ZoneManagement isn't enabled.</returns>
        public static bool RemoveGuidLookup(BundleGuidMap bundleGuidMap)
        {
            if (!s_ZoneManagerEnabled)
                return false;
            var newGuids = bundleGuidMap.m_TileGuids; //these are the current, NEW guid strings
            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (var newGuid in newGuids) //use the reverse lookup
            {
                var newAsGuid = new Guid(newGuid);
                if(s_ReverseLoadedGuidLookup!.TryGetValue(newAsGuid , out var oldGuid))
                    continue;
                s_LoadedGuidLookup!.Remove(oldGuid);
                s_ReverseLoadedGuidLookup.Remove(newAsGuid);
            }

            return true;
        }

        /// <summary>
        /// Gets the current, unused registration index.
        /// </summary>
        public static ulong RegistrationIndex => s_RegistrationIndex;

        internal static void IncrementRegistrationIndex()
        {
            s_RegistrationIndex++;
            //Debug.Log($"Reg index update to {s_RegistrationIndex}");
        }

       
        /// <summary>
        /// how many chunks are being managed by ALL zone managers.
        /// </summary>
        public static int NumZoneManagerChunks
        {
            get
            {
                if (!s_ZoneManagerEnabled || s_Instances == null)
                    return 0;
                var count = 0;
                // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
                foreach (var instance in s_Instances.Values)
                    count += instance.NumChunksInUse;
                return count;
            }
        }

        /// <summary>
        /// how many active zone managers
        /// </summary>
        public static int NumZoneManagerInstances => s_Instances?.Count ?? 0;
        #endregion
       
    }
}
