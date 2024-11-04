// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 07-14-2021
// ***********************************************************************
// <copyright file="TpAnimZoneSpawner.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using static TilePlus.SpawningUtil;
using static TilePlus.TpTileList;

#nullable enable

namespace TilePlus
{
    /// <summary>
    /// Data to be returned after spawning
    /// </summary>
    public class SpawningResults : MessagePacket<SpawningResults>
    {
        /// <summary>
        /// The last prefab that was spawned
        /// </summary>
        public GameObject? m_LastSpawnedPrefab;

        /// <summary>
        /// the last tile that was painted
        /// </summary>
        public TilePlusBase? m_LastPaintedTile;

        /// <summary>
        /// Ctor
        /// </summary>
        public SpawningResults()
        {
            m_LastPaintedTile   = null;
            m_LastSpawnedPrefab = null;
        }
    }



    /// <summary>
    /// Animated zone spawner/painter tile class
    /// </summary>
    [CreateAssetMenu(fileName = "TpAnimZoneSpawner.asset", menuName = "TilePlus/Create TpAnimZoneSpawner", order = 1000)]
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class TpAnimZoneSpawner : TpAnimZoneBase, ITpSpawnUtilClient, ITpMessaging<SpawningResults,PositionPacketIn>
    {
        /// <summary>
        /// Spawning mode enumeration
        /// </summary>
        public enum SpawnMode
        {
            /// <summary>
            /// Prefabs in same order as in asset
            /// </summary>
            PrefabsInOrder,

            /// <summary>
            /// Random prefabs from asset
            /// </summary>
            RandomPrefabs,

            /// <summary>
            /// Tiles in same order as asset
            /// </summary>
            TilesInOrder,

            /// <summary>
            /// Random Tiles from asset
            /// </summary>
            RandomTiles
        }

        #region private

        //const
        /// <summary>
        /// const error string
        /// </summary>
        private const string       AssetMissingError = "Asset missing, can't spawn prefabs or paint tiles!";
        /// <summary>
        /// const error string
        /// </summary>
        private const string       AssetIndexInvalid = "Index of prefab to spawn was out of range.";
        /// <summary>
        /// const error string
        /// </summary>
        private const string       BadTileNameFmt    = "Could not find named tile {0}";
        /// <summary>
        /// const error string
        /// </summary>
        private const string       MissingMapRefFmt  = "Please add a reference for PaintingTilemap. Map:{0} Pos:{1} Painting disabled...";


        /// <summary>
        /// The last spawned prefab
        /// </summary>
        private GameObject? lastSpawnedPrefab;
        /// <summary>
        /// The last painted tile
        /// </summary>
        private TilePlusBase? lastPaintedTile;
        /// <summary>
        /// The prefab index
        /// </summary>
        private int          prefabIndex, tileIndex;
        /// <summary>
        /// The tile has executed its preload op already
        /// </summary>
        private bool         hasPreloaded;
        /// <summary>
        /// The last contact position
        /// </summary>
        private Vector3Int   lastContactPosition = Vector3Int.zero;


        //properties
        /// <summary>
        /// Gets the prefabs.
        /// </summary>
        /// <value>The prefabs.</value>
        private List<TilePlusPrefabSpawnerItem> Prefabs => m_PrefabList !=null ? m_PrefabList.m_Prefabs : new List<TilePlusPrefabSpawnerItem>();
        /// <summary>
        /// Gets the tiles.
        /// </summary>
        /// <value>The tiles.</value>
        private List<PaintingSpec> Tiles   => m_TileList != null ? m_TileList.m_Tiles : new List<PaintingSpec>() ;
        
        #endregion

        #region publicFields

        /// <summary>
        /// Asset with prefabs to spawn
        /// </summary>
        [TptShowObjectField(typeof(TpPrefabList), false, true, SpaceMode.None, ShowMode.NotInPlay)] 
        [Tooltip("Asset with prefabs to spawn goes here")]
        [TptShowAsLabelBrushInspector(true, true, "The asset with the prefabs to spawn")]
        public TpPrefabList? m_PrefabList;

        /// <summary>
        /// Asset with tiles to spawn
        /// </summary>
        [TptShowObjectField(typeof(TpTileList), false, true, SpaceMode.None, ShowMode.NotInPlay)]
        [Tooltip("Asset with Tiles to spawn goes here")] 
        [TptShowAsLabelBrushInspector(true, true, "The asset with the Tiles to spawn")]
        public TpTileList? m_TileList;

        /// <summary>
        /// Choose normal or random spawning
        /// </summary>
        [TptShowEnum][Tooltip("Normal or Random spawning")]
        public SpawnMode m_SpawnMode;

        /// <summary>
        /// Asset positioning mode.
        /// </summary>
        [TptShowEnum] [Tooltip("how to position prefabs or tiles. UseAssetSetting uses info from asset. Any other ignores that info.")]
        public PositioningMode m_PositioningMode = PositioningMode.UseAssetSetting;



        /// <summary>
        /// How to parent a painted tile.
        /// </summary>
        [TptShowEnum][Tooltip("How to parent a painted tile")]
        public TileParentingMode m_ParentingMode = TileParentingMode.SameParentAsTile;

        /// <summary>
        /// Optional different tilemap for spawning. Null = parent of tile unless Tag or Name are used.
        /// </summary>
        [Tooltip("Optional alternative tilemap for Tile painting. Leave null to use parent tilemap of tile.")]
        [TptShowObjectField(typeof(Tilemap),true,false,SpaceMode.None,ShowMode.NotInPlay)]
        public Tilemap? m_PaintingTilemap;

        /// <summary>
        /// Optional different tilemap for spawning located by tilemap name or tag
        /// </summary>
        [TptShowField][Tooltip("Name or tag when ParentingMode is ParentByTag or ParentByName")]
        public string m_PaintingTilemapNameOrTag = string.Empty;


        #endregion

        #region publicProperties

       

        /// <inheritdoc />
        public PositioningMode PositioningMode => m_PositioningMode;

        /// <inheritdoc />
        public Vector3Int LastContactPosition => lastContactPosition;

        #endregion

        #region code

        /// <summary>
        /// Gets a SpawningResults instance with the last tile painted or prefab spawned.
        /// </summary>
        /// <returns>instance of class TR</returns>
        SpawningResults ITpMessaging<SpawningResults, PositionPacketIn>.GetData()
        {
            return new SpawningResults
            {
                m_LastPaintedTile   = lastPaintedTile,
                m_LastSpawnedPrefab = lastSpawnedPrefab
            };
        }

        //note that 'new' is used here so that the method hides the implementation
        //in TpAnimZoneBase.
        /// <summary>
        /// in this case, the object is a Vector3Int with a position to
        /// test for being  within the trigger bounds. Response is to post
        /// an simple trigger event.
        /// </summary>
        /// <param name="sentPacket">The data packet.</param>
        void ITpMessaging<SpawningResults, PositionPacketIn>.MessageTarget(PositionPacketIn sentPacket)

        {
            var pos = sentPacket.m_Position;
            lastContactPosition = pos;

            pos -= m_TileGridPosition; //remove offset
            if (!m_ZoneBoundsInt.Contains(pos))
            {
                lastSpawnedPrefab = null;
                lastPaintedTile   = null;
                return;
            }

            GameObject?   spawned      = null;
            TilePlusBase? painted      = null;
            
            if (m_SpawnMode is SpawnMode.PrefabsInOrder or SpawnMode.RandomPrefabs)
            {
                if (m_PrefabList == null)
                    return;
                var limit = m_PrefabList.NumPrefabs;
                if (limit == 0)
                    return;

                if (m_SpawnMode == SpawnMode.PrefabsInOrder)
                {
                    spawned = SpawningUtil.SpawnPrefab(m_PrefabList.m_Prefabs[prefabIndex++], this, null, false);
                    if (prefabIndex >= limit)
                        prefabIndex = 0;
                }
                else //random
                {
                    var index = Random.Range(0, limit);
                    spawned = SpawningUtil.SpawnPrefab(m_PrefabList.m_Prefabs[index], this, null, false);
                }
            }
            else
            {
                if (m_TileList == null || m_PaintingTilemap == null)
                    return;
                var limit = m_TileList.NumTiles;
                if (limit == 0)
                    return;
                if (m_SpawnMode == SpawnMode.TilesInOrder)
                {
                    var tileSpec = Tiles[tileIndex++];
                    if (tileIndex >= limit)
                        tileIndex = 0;
                    painted = SpawningUtil.PaintTile(tileSpec, this, m_PaintingTilemap, tileSpec.m_PaintPosition);
                }
                else
                {
                    var index    = Random.Range(0, limit);
                    var tileSpec = Tiles[index];
                    painted = SpawningUtil.PaintTile(tileSpec, this, m_PaintingTilemap, tileSpec.m_PaintPosition);
                }
            }

            lastSpawnedPrefab = spawned;
            lastPaintedTile   = painted;
            if(m_UseTrigger && (spawned != null || painted != null))
                TpEvents.PostTileTriggerEvent(this);

        

        }



        /// <summary>
        /// Reset the internal indexes
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        private void ResetItemIndex()
        {
            prefabIndex = tileIndex = 0;
        }



        /// <summary>
        /// Spawn a single, named prefab.
        /// </summary>
        /// <param name="prefabName">name of the prefab</param>
        /// <param name="parent">if null, uses info from asset</param>
        /// <param name="keepWorldPosition">Keep world position relative to parent. See Transform.SetParent</param>
        /// <returns>Spawned GameObject or null if name not found or for
        /// reasons related to SpawnPrefab(itemIndex)</returns>
        public GameObject? SpawnPrefab(string    prefabName,
                                       Transform parent,
                                       bool      keepWorldPosition)
        {
            if (m_PrefabList == null)
            {
                Debug.LogError("Asset missing, can't spawn!");
                return null;
            }

            var itemIndex = -1;
            for (var i = 0; i < Prefabs.Count; i++)
            {
                var go = Prefabs[i].m_Prefab;
                if(go == null || go.name != prefabName)
                    continue;
                itemIndex = i;
                break;
            }

            return itemIndex != -1 ? SpawnPrefab(itemIndex, parent, keepWorldPosition) : null;
        }




        /// <summary>
        /// Spawn a single item from the prefab list.
        /// </summary>
        /// <param name="itemIndex">index into the prefab list</param>
        /// <param name="parent">if null, uses info from asset</param>
        /// <param name="keepWorldPosition">Keep world position relative to parent. See Transform.SetParent</param>
        /// <returns>Spawned GameObj or null for error</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public GameObject? SpawnPrefab(int       itemIndex, 
                                       Transform parent,
                                       bool      keepWorldPosition)
        {
            if (m_PrefabList == null)
            {
                #if UNITY_EDITOR
                Debug.LogError(AssetMissingError);
                #endif
                return null;
            }

            if (itemIndex < 0 || itemIndex >= m_PrefabList.NumPrefabs)
            {
                #if UNITY_EDITOR
                Debug.LogError(AssetIndexInvalid);
                #endif
                return null;
            }

            var spawnerItem = Prefabs[itemIndex];
            return spawnerItem == null ? null : SpawningUtil.SpawnPrefab(spawnerItem, this, parent, keepWorldPosition);
        }



        /// <summary>
        /// Paint a tile from the TpTileList asset by name.
        /// Note that null is returned if the position is occupied.
        /// </summary>
        /// <param name="tileName">Name of tile</param>
        /// <param name="paintPos">if = .None, use paintPos from tile asset, else override</param>
        /// <returns>tile that was painted or null if error</returns>
        public TilePlusBase? PaintTile(string tileName, TilePaintPosition paintPos = TilePaintPosition.None)
        {
            if (m_TileList == null)
            {
                #if UNITY_EDITOR
                Debug.LogError(AssetMissingError);
                #endif
                return null;
            }

            if (m_PaintingTilemap == null)
                return null;

            var itemIndex = -1;
            for (var i = 0; i < Prefabs.Count; i++)
            {
                var tile = Tiles[i].m_Tile;
                if (tile != null &&  tile.name != tileName)
                    continue;
                itemIndex = i;
                break;
            }

            if (itemIndex != -1)
            {
                var tileSpec = Tiles[tileIndex];
                return SpawningUtil.PaintTile(tileSpec, this, m_PaintingTilemap, paintPos);
            }
            
            #if UNITY_EDITOR
            Debug.LogErrorFormat(BadTileNameFmt,tileName);
            #endif
            return null;

        }

        

        /// <summary>
        /// StartUp for TpAnimZoneSpawner
        /// </summary>
        /// <param name="position">the tile position</param>
        /// <param name="tilemap">the parent tilemap</param>
        /// <param name="go">GameObject (unused)</param>
        /// <returns></returns>
        public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject go)
        {
            if (!base.StartUp(position, tilemap, go))
                return false;

            if (m_SpawnMode is SpawnMode.RandomPrefabs or SpawnMode.PrefabsInOrder)
            {
                //do preloading?
                #if UNITY_EDITOR
                if(IsAsset || TpLib.IsTilemapFromPalette(tilemap))
                    return true;
                #endif
                if (!Application.isPlaying || hasPreloaded || m_PrefabList == null)
                    return true;
                var prefabs = Prefabs;
                var num     = prefabs.Count;
                if (num == 0)
                    return true;
                hasPreloaded = true;
                for (var i = 0; i < num; i++)
                {
                    var item = prefabs[i];
                    if(item.m_PoolInitialSize > 0)
                        SpawningUtil.Preload(item.m_Prefab,item.m_PoolInitialSize);
                }

                return true;
            }



            if (m_ParentingMode == TileParentingMode.SameParentAsTile)
                return true;

            //otherwise ensure that painting on alternate tilemaps is set up properly

            if (m_ParentingMode == TileParentingMode.ParentByReference && m_PaintingTilemap == null)
            {
                #if UNITY_EDITOR
                Debug.LogFormat(MissingMapRefFmt, m_ParentTilemap==null? "???":m_ParentTilemap.name, m_TileGridPosition.ToString());
                #endif
                return true;
            }

            var possibleMap = GetPaintingTilemap(m_ParentingMode,
                                                 m_PaintingTilemapNameOrTag,
                                                 $"{GetType()} @ {TileGridPosition.ToString()} on {(m_ParentTilemap != null ? m_ParentTilemap.name : "???")}");
            if (possibleMap != null)
                m_PaintingTilemap = possibleMap;
            return true;

        }


        /// <summary>
        /// Used to reset state variables. May need overriding
        /// in subclasses.
        /// See programmer's guide for info on overriding this.
        /// </summary>
        /// <param name="op">The type of reset operation</param>
        public override void ResetState(TileResetOperation op)
        {
            base.ResetState(op);
            ResetItemIndex();
        }

        #endregion

#if UNITY_EDITOR

        #region EditorCode
        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        /// <inheritdoc />
        public override string Description => "Prefab/Tile triggered spawner with static sprite";

        /// <summary>
        /// Property to get number of prefabs in asset
        /// </summary>
        /// <value>The number of prefabs.</value>
        [TptShowAsLabelSelectionInspector(true, true, "# of prefabs in asset")]
        [TptShowAsLabelBrushInspector(true, true, "# of prefabs in asset")]
        public string NumberOfPrefabs => m_PrefabList != null ? m_PrefabList.NumPrefabs.ToString() : "0 (no asset)";


        /// <summary>
        /// Property to get number of tiles in asset
        /// </summary>
        /// <value>The number of tiles.</value>
        [TptShowAsLabelSelectionInspector(true, true, "# of tiles in asset")]
        [TptShowAsLabelBrushInspector(true, true, "# of tiles in asset")]
        public string NumberOfTiles => m_TileList != null ? m_TileList.NumTiles.ToString() : "0 (no asset)";

        #endregion

        #endif
    }
}


