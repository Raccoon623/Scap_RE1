using TilePlus;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TilePlusDemo
{
    /// <summary>
    /// A simple NPC tile.
    /// </summary>
    [CreateAssetMenu(fileName = "TopDownDemo-Agent.asset", menuName = "TilePlus/Demo/Create TopDownDemo-Agent", order = 1000)]

    public class AgentTile : TpFlexAnimatedTile
    {
        /// <summary>
        /// Minimum lifetime 
        /// </summary>
        [TptShowAsLabelBrushInspector(true)]
        [TptShowField(0,0,SpaceMode.None,ShowMode.NotInPlay)][Tooltip("Minimum Lifetime in # moves")]
        public int m_LifeTimeMin = 5;
        /// <summary>
        /// Maximum lifetime
        /// </summary>
        [TptShowAsLabelBrushInspector(true)]
        [TptShowField(0,0,SpaceMode.None,ShowMode.NotInPlay)][Tooltip("Maximum Lifetime in # moves")]
        public int m_LifeTimeMax = 25;

        /// <summary>
        /// How much life is left 
        /// </summary>
        [TptShowAsLabelSelectionInspector(true,true,"Number of moves before this agent expires.",SpaceMode.None,ShowMode.InPlay)]
        public  int m_LifeLeft;

        private bool lifetimeInitialized;

        /// <inheritdoc/>
        public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject go)
        {
            if (!base.StartUp(position, tilemap, go))
                return false;
            if (!Application.isPlaying || lifetimeInitialized)
                return true;
            lifetimeInitialized = true; //so this doesn't occur each time tile moves: startup is called every time agent moves.
            if (m_LifeTimeMin < 2)
                m_LifeTimeMin = 2;
            m_LifeLeft = Random.Range(m_LifeTimeMin, m_LifeTimeMax + 1);
            return true;

        }

        /// <summary>
        /// Rotate a tile
        /// </summary>
        /// <param name="target">what to look at</param>
        public void RotateTile(Vector3 target)
        {
            if(m_ParentTilemap == null)
                return;
            var heading      = target - TileWorldPosition;
            var angle        = (Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg) - 180;
            var newTransform = TileUtil.RotatationMatixZ(angle);
            m_ParentTilemap.SetTransformMatrix(TileGridPosition, newTransform);
        }
        
        
        #if UNITY_EDITOR
        
        /// <summary>
        /// Description of this tile
        /// </summary>
        public override string Description => "Agent tile for Top Down demo";

       
        #endif
    }
}
