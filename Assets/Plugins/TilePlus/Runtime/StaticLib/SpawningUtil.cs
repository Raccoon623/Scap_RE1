// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 07-10-2021
// ***********************************************************************
// <copyright file="SpawningUtil.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Tilemaps;
using static TilePlus.TpLib;
using static TilePlus.TpTileList;
#nullable enable

namespace TilePlus
{
   /// <summary>
    /// Utility methods for spawning prefabs and painting tiles.
    /// </summary>
    #if UNITY_EDITOR
    [InitializeOnLoad]
    #endif
   
    public static class SpawningUtil  
    {
        /// <summary>
        /// Enum PositioningMode
        /// </summary>
        public enum PositioningMode
        {
            /// <summary>
            /// Use the setting from the asset
            /// </summary>
            UseAssetSetting,
            /// <summary>
            /// At the tile's grid position
            /// </summary>
            AtTilePosition,
            /// <summary>
            /// At the contact position sent to MessageTarget
            /// </summary>
            AtContactPosition,
            /// <summary>
            /// Randomly in the Zone.
            /// </summary>
            RandomInZone
        }

        /// <summary>
        /// How to parent painted tiles.
        /// </summary>
        public enum TileParentingMode
        {
            /// <summary>
            /// Use the same parent as the Tile
            /// </summary>
            SameParentAsTile,

            /// <summary>
            /// Use a provided tilemap reference
            /// </summary>
            ParentByReference,

            /// <summary>
            /// Use a Tag
            /// </summary>
            ParentByTag,

            /// <summary>
            /// Use a Name
            /// </summary>
            ParentByName
        }


        /// <summary>
        /// Initializes static members, sets up PoolHost if present.
        /// </summary>
        static SpawningUtil()
        {
            ResetPools();
        }

        /// <summary>
        /// Reset on enter play mode.
        /// </summary>
        #if UNITY_EDITOR        
        [InitializeOnEnterPlayMode]
        #endif        
        private static void ResetOnEnterPlayMode()
        {
            if(Informational)
                TpLog("SpawningUtil: Pooling enabled (reset on enter Play).");
            ResetPools();  
            
        }


        /// <summary>
        /// Reset all prefab pools. 
        /// </summary>
        /// <remarks>Pools are cleared, which Destroys the pooled prefabs. Any prefabs that have been
        /// taken from the pool BUT are NOT released to the pool will NOT be destroyed.</remarks>
        public static void ResetPools()
        {
            foreach(var pool in s_Pools.Values)
                pool.Clear(); //note that this calls Destroy (or DestroyImmediate) on each item in the pool.
            s_Pools.Clear();  
            var go =   GameObject.Find("TPP_PoolHost");
            s_PooledObjectHost  = go == null ? null : go.transform;
            
        }

        
        //int is the prefab instance id.
        /// <summary>
        /// A dictionary which maps instance IDs to GameObject pools
        /// </summary>
        private static readonly Dictionary<int, ObjectPool<GameObject>> s_Pools = new(32);

        /// <summary>
        /// possible parent for the spawned prefabs
        /// </summary>
        private static Transform? s_PooledObjectHost;

        /// <summary>
        /// Gets the name of the pool host.
        /// </summary>
        /// <value>The name of the pool host.</value>
        public static string PoolHostName => s_PooledObjectHost!=null? s_PooledObjectHost.name : "No parent for prefabs. Add TPP_PoolHost prefab if desired.";

        
        /// <summary>
        /// Get the pool host name with no advice.
        /// </summary>
        public static string CleanPoolHostName => s_PooledObjectHost != null
                                                      ? s_PooledObjectHost.name
                                                      : "None";
        /// <summary>
        /// total Number of pooled prefabs
        /// </summary>
        /// <value>The number of pooled prefabs.</value>
        public static int NumPooledPrefabs => s_Pools.Count;

        /// <summary>
        /// Get a string with the pool status. outside editor
        /// environment it returns an empty string.
        /// </summary>
        /// <param name="limit">Limit the number of pools shown</param>
        ///<returns>A string with the Pools' status </returns>
        public static string PoolStatus(int limit = -1)
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying || s_Pools.Count == 0)
                return ("No Pools active");
            var s = $"Number of pools: {s_Pools.Count.ToString()}";

            var count = 0;
            foreach ((var key, var pool) in s_Pools)
            {
                var prefab     = EditorUtility.InstanceIDToObject(key);
                var prefabName = "??";
                if (prefab != null)
                    prefabName = prefab.name;
                    
                s += $"\n{prefabName}: Total={pool.CountAll.ToString()} Active={pool.CountActive.ToString()} Inactive={pool.CountInactive.ToString()}";
                if (limit == -1)
                    continue;
                if (++count >= limit)
                    break;
            }

            return s;

            #else
            return string.Empty;
            #endif
        }

        /// <summary>
        /// has this prefab already been preloaded?
        /// </summary>
        /// <param name="prefab">prefab to test</param>
        /// <returns>true if preloaded already</returns>
        public static bool IsPreloaded(GameObject prefab)
        {
            var prefabId = prefab.GetInstanceID();
            return s_Pools.ContainsKey(prefabId);
        }

        /// <summary>
        /// Preload a pool.
        /// </summary>
        /// <param name="prefab">Prefab to preload</param>
        /// <param name="quantity">how many instances to preload</param>
        public static void Preload(GameObject? prefab, int quantity)
        {
            if (prefab == null || quantity <= 0)
                return;
            var prefabId = prefab.GetInstanceID();
            if (s_Pools.ContainsKey(prefabId))  //no multiple preloads.
                return;
            var pool = new ObjectPool<GameObject>(() => UnityEngine.Object.Instantiate(prefab),
                OnGet, OnRelease, OnPrefabDestroy);
            
            s_Pools.Add(prefabId, pool);
            /*note that although the pool is created, the instances aren't
            preloaded: we have to do that manually. This has the side effect of forcing the pool's CountActive
            property to -quantity (e.g., quantity = 10 makes CountActive = -10. But that's not used
            within the pool and the pool is private to this library so it's not an issue.
            */
            var gameObjects = new GameObject[quantity];
            for (var i = 0; i < quantity; i++)
                gameObjects[i] = pool.Get();

            for (var i = 0; i < quantity; i++)
                pool.Release(gameObjects[i]);

            
            #if UNITY_EDITOR
            if (Informational)
                TpLog($"Preload: Added new pool for prefab {prefab.name}, size {quantity.ToString()}");
            #endif
        }

        /// <summary>
        /// Called when a prefab rcvs Destroy() and despawns itself. Called by pooler
        /// </summary>
        /// <param name="go">The gameobj.</param>
        private static void OnPrefabDestroy(GameObject go)
        {
            if(Application.isPlaying)
                UnityEngine.Object.Destroy(go);
            else
                UnityEngine.Object.DestroyImmediate(go);
        }

        /// <summary>
        /// Called when pool releases a Gameobj
        /// </summary>
        /// <param name="go">The gameobj</param>
        private static void OnRelease(GameObject go)
        {
            go.transform.parent = s_PooledObjectHost;
            go.SetActive(false);
        }

        /// <summary>
        /// Called when the pool provides a prefab.
        /// </summary>
        /// <param name="go">The gameobj</param>
        private static void OnGet(GameObject go)
        {
            go.transform.parent = s_PooledObjectHost; //this might be null.
            go.SetActive(true);
        }


        /// <summary>
        /// Despawn a prefab into the pool
        /// </summary>
        /// <param name="sourcePrefabId">Instance Id of source prefab</param>
        /// <param name="target">the instantiated prefab</param>
        /// <remarks>If the sourcePrefabId isn't found in m_Pools then the target is Destroyed.</remarks>
        public static void DespawnPrefab(int sourcePrefabId, GameObject? target)
        {
            if (target == null)
            {
                #if UNITY_EDITOR
                TpLogError("Attempt to despawn a null GameObject");
                #endif
                return;
            }
            if (s_Pools.TryGetValue(sourcePrefabId, out var pool))
            {
                #if UNITY_EDITOR
                if (Informational)
                    TpLog($"Despawned prefab: {target.name} @ {target.transform.position.ToString()}");
                #endif
                if (target.TryGetComponent<TpSpawnLink>(out var link))
                    link.OnTpDespawned();
                pool.Release(target);
            }
            else
            {
                #if UNITY_EDITOR
                if (Informational)
                    TpLogWarning($"Prefab ({target.name}) could not be stored in pool, was Destroyed (not necc an error) or was not created from pool.");
                #endif
                UnityEngine.Object.Destroy(target);
            }
        }



        /// <summary>
        /// Spawn a single prefab from a tile that implements the ISpawner interface.
        /// </summary>
        /// <param name="spawnerItem">item from the prefab list</param>
        /// <param name="tile">A tile instance that implements the ISpawner interface</param>
        /// <param name="altParent">if null, uses info from asset</param>
        /// <param name="keepWorldPosition">Keep world position relative to parent if using altParent. See Transform.SetParent</param>
        /// <returns>Spawned GameObj or null for error</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <remarks>Will log an error if tile does not implement ITpSpawnUtilClient</remarks>
        public static GameObject? SpawnPrefab(TilePlusPrefabSpawnerItem spawnerItem,
                                              TilePlusBase              tile,
                                              Transform?                altParent,
                                              bool                      keepWorldPosition)
        {
            var iIile = tile as ITpSpawnUtilClient;
            if (iIile == null)
            {
                TpLogError("input tile does not implement ISpawner!");
                return null;
            }
            
            
            var prefab = spawnerItem.m_Prefab;
            if (prefab == null)
            {
                #if UNITY_EDITOR
                if (Warnings)
                    TpLogWarning($"Prefab was null, paintPos:{tile.TileGridPosition.ToString()}");
                #endif
               return null;
            }
            
            var parent         = spawnerItem.m_Parent;
            var position       = tile.TileWorldPosition;
            var isRelative     = false;

            switch (iIile.PositioningMode)
            {
                case PositioningMode.UseAssetSetting:
                    //position from asset or from override?
                    position = spawnerItem.m_Position;
                    //isRelative from asset of override?
                    isRelative = spawnerItem.m_PositionIsRelative;
                    break;
                case PositioningMode.AtContactPosition:
                    if (tile.ParentTilemap == null)
                        return null;
                    //position = iIile.LastContactPosition; - this was wrong in 1.1 and earlier.
                    position = tile.ParentTilemap.GetCellCenterWorld(iIile.LastContactPosition);
                    break;
                case PositioningMode.AtTilePosition: //TileWorldPosition is already set as the default for position.
                    break;
                case PositioningMode.RandomInZone:
                    position   = TileUtil.RandomPosInBounds(iIile.ZoneBounds);
                    isRelative = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var prefabId = prefab.GetInstanceID(); //note this is the ASSET's instance ID and is invariant during the run.
            if( !s_Pools.TryGetValue(prefabId, out var pool))
            {
                pool = new ObjectPool<GameObject>(() => UnityEngine.Object.Instantiate(prefab),
                    OnGet, OnRelease, OnPrefabDestroy);
            
                s_Pools.Add(prefabId, pool);
                #if UNITY_EDITOR
                if (Informational)
                    TpLog($"Added new pool for prefab {prefab.name}");
                #endif
            }
            pool.Get(out var go);
            if (go == null)
            {
                #if UNITY_EDITOR
                if (Warnings)
                    TpLogWarning($"Couldn't create prefab named ({prefab.name}) paintPos:{tile.TileGridPosition.ToString()} ]");
                #endif
                return null;
            }
            
            
            if (isRelative)
                position += tile.TileWorldPosition;
            go.transform.position = position;
            
            if (altParent != null)
                go.transform.SetParent(altParent.transform, keepWorldPosition);
            else
            {
                //note that if a parent isn't provided or can't be found AND
                //   "TPP_PoolHost" is present and the prefab was preloaded
                // then the spawned prefab remains parented to "TPP_PoolHost"
                if (!string.IsNullOrWhiteSpace(parent))
                {
                    //try to find parent based on tag (reasonably fast) or GameObject.Find (slow...)
                    var possibleParent = spawnerItem.m_UseParentNameAsTag
                                             ? GameObject.FindWithTag(spawnerItem.m_Parent)
                                             : GameObject.Find(parent);
                    if (possibleParent != null)
                        go.transform.SetParent(possibleParent.transform, spawnerItem.m_KeepWorldPosition);
                }
            }

            if (!go.TryGetComponent<TpSpawnLink>(out var link))
                link = go.AddComponent<TpSpawnLink>();

            link.m_SourcePrefabId = prefabId; //used when the prefab is despawned
            link.OnTpSpawned();
            #if UNITY_EDITOR
            if (Informational)
                TpLog($"Spawned prefab from pool: {go.name}");
            #endif
            
            
            return go;
        }

        /// <summary>
        /// Spawn a single prefab. Always uses pooling. 
        /// </summary>
        /// <param name="prefab">the prefab asset reference</param>
        /// <param name="position">world position to spawn at</param>
        /// <param name="parentTransform">Parent transform - can be null</param>
        /// <param name="parentNameOrTag">Name or tag of parent if Transform is null</param>
        /// <param name="searchForTag">If transform is null AND parentNameOrTag isn't empty, search for tag if this is true, otherwise search for name</param>
        /// <param name="keepWorldPosition">Keep world position relative to parent if one is provided or located via name or tag. See Transform.SetParent</param>
        /// <returns>Spawned GameObj or null for error</returns>
        /// <remarks> If parent is NULL and parentNameOrTag is not an empty string then a parent is searched for
        /// by tag or name depending on value of searchForTag. If no parent is found then spawned item is not parented or remains parented to TppPoolHost.
        /// If that's what you want, leave parent=null and parentNameOrTag=string.Empty</remarks>
        public static GameObject? SpawnPrefab(GameObject? prefab,
                                              Vector3     position,
                                              Transform?  parentTransform,
                                              string      parentNameOrTag,
                                              bool        searchForTag,
                                              bool        keepWorldPosition)
        {
            if (prefab == null)
            {
               #if UNITY_EDITOR
               if (Warnings)
                    TpLogWarning("Prefab was null, nothing was done");
               #endif
               return null;
            }

            var prefabId = prefab.GetInstanceID(); //note this is the ASSET's instance ID and is invariant during the run.
            if( !s_Pools.TryGetValue(prefabId, out var pool))
            {
                pool = new ObjectPool<GameObject>(() => UnityEngine.Object.Instantiate(prefab),
                    OnGet, OnRelease, OnPrefabDestroy);
            
                s_Pools.Add(prefabId, pool);
                #if UNITY_EDITOR
                if (Informational)
                    TpLog($"Added new pool for prefab {prefab.name}");
                #endif
            }
            pool.Get(out var go);
            if (go == null)
            {
                #if UNITY_EDITOR
                if(Warnings)
                    TpLogWarning($"Couldn't get prefab named ({ prefab.name}) Pos:{position} ]");
                #endif
                return null;
            }
        
            go.transform.position = position;

            if (parentTransform != null)
                go.transform.SetParent(parentTransform, keepWorldPosition);
            else
            {
                if (!string.IsNullOrWhiteSpace(parentNameOrTag))
                {
                    //try to find parent based on tag (reasonably fast) or GameObject.Find (slow...)
                    var possibleParent = searchForTag
                                             ? GameObject.FindWithTag(parentNameOrTag)
                                             : GameObject.Find(parentNameOrTag);
                    if (possibleParent != null)
                        go.transform.SetParent(possibleParent.transform, keepWorldPosition);
                }
            }
            
            if (!go.TryGetComponent<TpSpawnLink>(out var link))
                link = go.AddComponent<TpSpawnLink>();

            link.m_SourcePrefabId = prefabId; //used when the prefab is despawned
            link.OnTpSpawned();
            #if UNITY_EDITOR
            if (Informational)
                TpLog($"Spawned prefab from pool: {go.name}");
            #endif

            return go;
        }
        
        
        /// <summary>
        /// Paint a tile from the TpTileList asset by index.
        /// Note that null is returned if the position is occupied.
        /// Also, you can change the tilemap to use for painting with
        /// the m_PaintingTilemap field in-editor or via code.
        /// </summary>
        /// <param name="item">Painting spec for the tile to paint</param>
        /// <param name="sourceTile">A tile instance that implements the ISpawner interface.</param>
        /// <param name="paintTarget">Target tilemap. If it's the same as the sourceTile's map and paint position ends up
        /// being the same as the sourceTile position then nothing is painted.</param>
        /// <param name="paintPos">None, use paintPos from tile asset, else overrides tile spec.</param>
        /// <returns>tile that was painted or null if error</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <remarks>If paintPos from asset is None then use a random position, if paintPos from
        /// asset is Top then if the target tilemap is different than this tile's parent
        /// then use the same grid position as this tile.</remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        public static TilePlusBase? PaintTile(PaintingSpec?     item,
                                              TilePlusBase      sourceTile,
                                              Tilemap?          paintTarget,
                                              TilePaintPosition paintPos = TilePaintPosition.None)
        {
            var iIile = sourceTile as ITpSpawnUtilClient;
            if (iIile == null)
            {
                TpLogError("input tile does not implement ISpawner!");
                return null;
            }
            
            if (item == null)
            {
                #if UNITY_EDITOR
                if(Warnings)
                    TpLogWarning("Null tile painter item!");
                #endif
                return null;
            }

            var tile = item.m_Tile;
            if (tile == null)
            {
                #if UNITY_EDITOR
                if(Warnings)
                    TpLogWarning("Null tile!");
                #endif
                return null;
            }

            //this position is that of the Tile asking to spawn something.
            var pos     = sourceTile.TileGridPosition; //the default
            var tilePos = pos; //need this later so avoid double-hit to TileGridPosition property

            switch (iIile.PositioningMode)
            {
                case PositioningMode.UseAssetSetting:
                    Vector3Int offset;
                    //if the input paintPos value is None then use the value from the tile painting spec.
                    paintPos = paintPos != TilePaintPosition.None ? paintPos : item.m_PaintPosition;

                    switch (paintPos)
                    {
                        // 'top' means paint at same position as tile, but this only works if PaintingTilemap set up properly
                        //use this tile's position? Only if map is different
                        case TilePaintPosition.Top:
                            offset = Vector3Int.zero; //paint on alternate map at same position as tile.
                            break;
                        case TilePaintPosition.Random:
                        //random
                        case TilePaintPosition.None:
                        {
                            var choice = UnityEngine.Random.Range(1, 9); //so we leave out 0, 9, 10 (recall 2nd param for Range is excluded)
                            paintPos = (TilePaintPosition) choice;
                            offset   = s_TileOffsets[(int) paintPos];
                            break;
                        }
                        //paintPos is 1...8 ie Up ... LeftUp
                        default:
                            offset = s_TileOffsets[(int) paintPos];
                            break;
                    }

                    pos += offset;
                    
                    break;
                case PositioningMode.AtContactPosition:
                    pos = iIile.LastContactPosition;
                    break;
                case PositioningMode.AtTilePosition: //already set up
                    break;
                case PositioningMode.RandomInZone:
                    pos        = tilePos + Vector3Int.FloorToInt(TileUtil.RandomPosInBounds(iIile.ZoneBounds));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            
            var map = paintTarget != null ? paintTarget : sourceTile.ParentTilemap;
            if (map == null)
                return null;

            if (pos == tilePos && map == sourceTile.ParentTilemap) 
            {
                #if UNITY_EDITOR
                if(Warnings)
                    TpLogWarning("Can't paint over spawner itself. Changing position for ya!");
                #endif
                pos = sourceTile.TileGridPosition + s_TileOffsets[1];
            }

            if (map.HasTile(pos))
            {
                #if UNITY_EDITOR
                if(Informational)
                    TpLog($"Can't paint a tile: position {pos} is in use");
                #endif
                return null;
            }

            map.SetTile(pos, tile);
            return tile;

        }

        /// <summary>
        /// Get the tilemap for painting
        /// </summary>
        /// <param name="parentingMode">search by tag,name,etc</param>
        /// <param name="mapNameOrTag">search string</param>
        /// <param name = "info" >will print this if an error/warning/log</param>
        /// <returns>Tilemap to use for painting or null for not found.</returns>
        public static Tilemap? GetPaintingTilemap(TileParentingMode parentingMode, string mapNameOrTag, string info)
        {
            if (string.IsNullOrWhiteSpace(mapNameOrTag))
                return null;
            
            switch (parentingMode)
            {
                case TileParentingMode.ParentByTag:
                {
                    var gameobjects = GameObject.FindGameObjectsWithTag(mapNameOrTag);
                    if (gameobjects.Length == 0)
                    {
                        #if UNITY_EDITOR
                        if(Informational)
                            TpLog($"No gameobjects tagged {mapNameOrTag}: {info}");
                        #endif
                        return null;
                    }

                    if (gameobjects.Length > 1)
                    {
                        #if UNITY_EDITOR
                        if(Informational)
                            TpLog($"More than one gameobject was tagged {mapNameOrTag}: {info} ");
                        #endif
                        return null;
                    }

                    if (!gameobjects[0].TryGetComponent<Tilemap>(out var map))
                    {
                        #if UNITY_EDITOR
                        if(Informational)
                            TpLog($"Gameobject tagged {mapNameOrTag} missing Tilemap component: {info}");
                        #endif
                        return null;
                    }

                    return map;
                }
                case TileParentingMode.ParentByName:
                {
                    var gameobject = GameObject.Find(mapNameOrTag);
                    if (gameobject == null)
                    {
                        #if UNITY_EDITOR
                        if(Informational)
                            TpLog($"No gameobject named {mapNameOrTag}: {info} ");
                        #endif
                        return null;
                    }

                    if (!gameobject.TryGetComponent<Tilemap>(out var map))
                    {
                        #if UNITY_EDITOR
                        if(Informational)
                            TpLog($"Gameobject named {mapNameOrTag} missing Tilemap component: {info}");
                        #endif
                        return null;
                    }

                    return map;
                }
                case TileParentingMode.SameParentAsTile:
                    break;
                case TileParentingMode.ParentByReference:
                    break;
                default:
                    return null;
            }

            return null;
        }





        /// <summary>
        /// tile placement offsets
        /// </summary>
        private static readonly Vector3Int[] s_TileOffsets = {
            Vector3Int.zero,           //None 
            new Vector3Int(0, 1, 0),   //Up
            new Vector3Int(1, 1, 0),   //UpRight
            new Vector3Int(1, 0, 0),   //right
            new Vector3Int(1, -1, 0),  //rightDown
            new Vector3Int(0, -1, 0),  //down
            new Vector3Int(-1, -1, 0), //downLeft
            new Vector3Int(-1, 0, 0),  //left
            new Vector3Int(-1, 1, 0)   //leftUp
        };
        
    }
}
