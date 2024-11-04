using System;
using System.Collections.Generic;
using TilePlus;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TilePlusDemo
{
    /// <summary>
    /// A simple Waypoint tile.
    /// </summary>
    [CreateAssetMenu(fileName = "TopDownDemo-Waypoint.asset", menuName = "TilePlus/Demo/Create TopDownDemo-Waypoint", order = 1000)]
    public class TopDownWaypointTile : TpSlideShow, ITpPersistence<SaveDataWrapper,StringPacketIn>, ITpMessaging<EmptyPacket,PositionPacketIn>
    {
        /// <summary>
        /// Displays enable state
        /// </summary>
        [TptShowAsLabelSelectionInspector]
        // ReSharper disable once MemberCanBePrivate.Global
        public bool IsEnabled { get; set; }
        
        /// <summary>
        /// Each wp keeps track of the number of lives.
        /// </summary>
        public int Lives { get; private set; }

        /// <summary>
        /// marks this as the Starting waypoint
        /// </summary>
        [TptShowField()][Tooltip("this should be checked (true) on only ONE waypoint")]
        public bool m_IsStartWaypoint;
        

        /// <inheritdoc />
        public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject go)
        {
            m_WrappingOverride = WrapOverride.ForceHalt;
            return base.StartUp(position, tilemap, go);
        }


        /// <summary>
        /// create a data object for saving
        /// </summary>
        // ReSharper disable once AnnotateNotNullTypeMember
        public SaveDataWrapper GetSaveData(object options)
        {
            var prettyPrint = false;
            if (options is bool b)
                prettyPrint = b;
            var lives = 9;
            
            //done this way to show how a TilePlus tile can access other things in the scene
            var ctlr  = GameObject.FindWithTag("GameController");
            if (ctlr != null && ctlr.TryGetComponent(typeof(TdDemoGameController), out var component))
                lives = ((TdDemoGameController)component).Player.Lives;

            var obj = new WaypointSaveData { m_Enabled = IsEnabled, m_Lives = lives, m_Position = m_TileGridPosition};
            return new SaveDataWrapper() {m_Json = JsonUtility.ToJson(obj, prettyPrint), m_Guid = TileGuidString};
        }

        /// <summary>
        /// restore data
        /// </summary>
        public void RestoreSaveData(StringPacketIn value)
        {
            if(value == null)
                return;
            var data = JsonUtility.FromJson<WaypointSaveData>(value.m_String);
            IsEnabled = data.m_Enabled;
            Lives     = data.m_Lives;
            //the position info in the data is discarded.
            

            if (IsEnabled)
            {
                if (AnimationIsRunning) //startup has run already
                    ChangeSlide(true);
                else  //hasn't run, so just change the start slide param.
                    m_SlideIndexAtStart = 1;

            }
        }
        
        
        

        private List<TilePlusBase> others = new();

        /// <inheritdoc />
        public void MessageTarget(PositionPacketIn sentPacket)
        {
            if (sentPacket.m_Position != m_TileGridPosition)
                return;
            ChangeSlide(true);

            TpLib.GetAllTilesOfType(null, typeof(TopDownWaypointTile), ref others, (tpb) => tpb.TileGridPosition != m_TileGridPosition);
            foreach (var other in others)
            {
                ((TopDownWaypointTile)other).IsEnabled = false; //mark as disabled
                ((TopDownWaypointTile)other).SetSlide(0); //show the Disabled sprite
                TpEvents.PostTileSaveDataEvent(other); 
            }

            IsEnabled = true;
            TpEvents.PostTileSaveDataEvent(this);
        }
        
        
        #if UNITY_EDITOR
        /// <summary>
        /// Description field
        /// </summary>
        public override string Description => "Waypoint tile for TopDownDemo";

        #endif
        
        
    }

    /// <summary>
    /// Data object for Player tile
    /// </summary>
    [Serializable]
    public class WaypointSaveData : MessagePacket<WaypointSaveData>
    {
        /// <summary>
        /// Enabled state of this Waypoint
        /// </summary>
        [SerializeField]
        public bool m_Enabled;

        /// <summary>
        /// Position of this waypoint
        /// </summary>
        [SerializeField]
        public Vector3Int m_Position;

        /// <summary>
        /// Current # lives
        /// </summary>
        /// <remarks>Each wp saves the # of lives</remarks>
        public int m_Lives;

    }
        

}
