#if ODIN_INSPECTOR && UNITY_EDITOR
#define USE_ODIN
using Sirenix.OdinInspector;
#endif
using System;
using System.Collections.Generic;
using TilePlus;
using UnityEngine;
using UnityEngine.Tilemaps;
#nullable enable
namespace TilePlusDemo
{

    /// <summary>
    /// This Component should be placed on a Grid that's a
    /// parent to one or more Tilemaps. 
    /// </summary>
    [RequireComponent(typeof(Grid))]
    public class Pathfinder : MonoBehaviour
    {
        private int numMobileMaps;
        #if USE_ODIN
        [ShowInInspector, ReadOnly]
        #endif
        private readonly List<Tilemap> mobileObstaclesMaps = new List<Tilemap>(4);
        #if USE_ODIN
        [ShowInInspector, ReadOnly]
        #endif
        private readonly HashSet<int> mobileObstacleMapIds = new HashSet<int>(4);
        #if USE_ODIN
        [ShowInInspector, ReadOnly]
        #endif
        private HashSet<Vector3Int> staticObstaclePositions = new HashSet<Vector3Int>(128);
        #if USE_ODIN
        [ShowInInspector, ReadOnly]
        #endif
        private HashSet<Vector3Int> mobileObstaclePositions = new HashSet<Vector3Int>(128);

        private Tilemap? floorLevelTilemap;
        

        private bool initialized;

        /// <summary>
        /// Scan all maps upon init, set up callback to TpLib
        /// </summary>
        public void ScanMaps()
        {
            if (initialized)
                return;
            initialized = true;
            Scan();
            TpLib.OnTpLibChanged += UpdateTarget;
        }

        
        /// <summary>
        /// Scan maps upon init, or when a new tile zone is loaded
        /// </summary>
        /// 
        public void Scan()
        {
            mobileObstaclesMaps.Clear();
            mobileObstacleMapIds.Clear();
            staticObstaclePositions.Clear();
            
            //get all maps
            var tilemaps = GetComponentsInChildren<Tilemap>();
            if (tilemaps == null || tilemaps.Length == 0)
            {
                Debug.LogError("No tilemaps found");
                return;
            }
            
            staticObstaclePositions.Clear();
            //look at each tilemap and find it's PathfinderInfo component
            for (var i = 0; i < tilemaps.Length; i++)
            {
                var map = tilemaps[i];
                if (!map.TryGetComponent<PathFinderInfo>(out var pathFinderInfo))
                    continue;
                //what sort of obstacles on this map?
                // ReSharper disable once ConvertIfStatementToSwitchStatement
                if (pathFinderInfo.m_ObstacleSpec == PathFinderInfo.TilemapObstacleSpec.Mobile)
                {
                    //add mobile obstacles IDs, scan later
                    mobileObstaclesMaps.Add(map);
                    mobileObstacleMapIds.Add(map.GetInstanceID());
                }
                else if (pathFinderInfo.m_ObstacleSpec == PathFinderInfo.TilemapObstacleSpec.Static)
                    TpLib.GetAllPositionsForMap(map,ref staticObstaclePositions, true, false);
               
                else //is floor
                    floorLevelTilemap = map;

                //scan the mobile maps if there are any
                numMobileMaps = mobileObstaclesMaps.Count;
                if(numMobileMaps != 0)
                    ScanMobileObstacles();
            }
        }
        
        /// <summary>
        /// Is the specified position walkable, ie not
        /// blocked by obstacles?
        /// </summary>
        /// <param name="position">position to test</param>
        /// <returns>true if the position is OK to move to</returns>
        public bool IsWalkablePosition(Vector3Int position)
        {
            //if the player would end up walking outside the floor map, return false
            if (floorLevelTilemap != null && !floorLevelTilemap.HasTile(position))
                return false;
            //if either hashset contains the input position then return false.
            return !(staticObstaclePositions.Contains(position) || mobileObstaclePositions.Contains(position));
        }

        
        
        private void UpdateTarget(TpLibChangeType changeType, bool isPartOfGroup, Vector3Int position, Tilemap? map)
        {
            if (isPartOfGroup)
                return;
            
            if(map == null)
                return;
            
            if (!mobileObstacleMapIds.Contains(map.GetInstanceID()))
                return;
            
            switch (changeType)
            {
                case TpLibChangeType.Added:
                case TpLibChangeType.AddedToEmptyMap:
                case TpLibChangeType.ModifiedOrAdded:
                    mobileObstaclePositions.Add(position);
                    break;
                case TpLibChangeType.Deleted:
                    mobileObstaclePositions.Remove(position);
                    break;
                case TpLibChangeType.Modified: //nothing to do
                case TpLibChangeType.TagsModified:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Scan for new obstacles or those with changed positions
        /// </summary>
        public void ScanMobileObstacles()
        {
            mobileObstaclePositions.Clear(); //this isn't redundant since the clearOutput param in the call below is false 
            for (var i = 0; i < numMobileMaps; i++)
                TpLib.GetAllPositionsForMap(mobileObstaclesMaps[i], ref mobileObstaclePositions, true, false);
        }

        
    }
}
