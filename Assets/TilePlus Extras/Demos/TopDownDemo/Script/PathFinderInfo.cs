using UnityEngine;

namespace TilePlusDemo
{
    
    /// <summary>
    /// This component is used in conjunction with the Pathfinder
    /// component. Placed on a tilemap, it tells the Pathfinder
    /// how to interpret Tiles on this map.
    /// If a Tilemap has no effect on movement 
    /// then there's no need to add this component. 
    /// </summary>
    public class PathFinderInfo : MonoBehaviour
    {
        /// <summary>
        /// What type of tilemap this is.
        /// </summary>
        public enum TilemapObstacleSpec
        {
            /// <summary>
            /// Static obstacles
            /// </summary>
            Static,
            /// <summary>
            /// Obstacles whose positions might change
            /// </summary>
            Mobile,
            /// <summary>
            /// This is a floor layer. Pathfinder will avoid leaving it
            /// </summary>
            Floor
        }

        /// <summary>
        /// What type of tilemap this is.
        /// </summary>
        public TilemapObstacleSpec m_ObstacleSpec;

    }
}
