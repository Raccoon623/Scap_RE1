// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 08-03-2021
// Last Modified On : 08-03-2021
// ***********************************************************************
// <copyright file="TpAnimZoneLoader.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
using UnityEditor;
using UnityEngine;
#nullable enable
namespace TilePlus
{
    /// <summary>
    /// Zone-based tileset loader class
    /// </summary>
    [CreateAssetMenu(fileName = "TpAnimZoneLoader.asset", menuName = "TilePlus/Create TpAnimZoneLoader", order = 1000)]
    public class TpAnimZoneLoader : TpAnimZoneBase,ITpMessaging<EmptyPacket,PositionZmPacketIn>
    {
        #region publicFieldsProperties

        /// <summary>
        /// TileFab asset reference
        /// </summary>
        [SerializeField]
        [Tooltip("a TileFab from the Project folder")]
        [TptShowObjectField(typeof(TpTileFab),false,true,SpaceMode.None,ShowMode.NotInPlay)]
        public TpTileFab? m_TileFab;

        /// <summary>
        /// If true, and if zone manager is non-null when MessageTarget is called, then
        /// check with ZM to see if the zone is already occupied.
        /// </summary>
        [SerializeField]
        [TptShowField()][Tooltip("If zone manager in use, should I check to see if zone is occupied?")]
        public bool m_UseZoneManager = true;
        

        /// <summary>
        /// Returns true if a previous method call to MessageTarget
        /// resulted in trigger conditions being met.
        /// </summary>
        /// <value><c>true</c> if [was triggered]; otherwise, <c>false</c>.</value>
        // ReSharper disable once ConvertToAutoPropertyWhenPossible
        [TptShowAsLabelSelectionInspector(true,true,"Has this zone been triggered already?")]
        public bool WasTriggered
        {
            get => wasTriggered;
            set => wasTriggered = value;
        }

        /// <summary>
        /// Where to position the TileFab: loading offset + grid position
        /// </summary>
        public Vector3Int EffectiveAddress => m_LoadingOffset + m_TileGridPosition;
        
        /// <summary>
        /// the offset to load the imported Tilefab.
        /// </summary>
        /// <value>The loading offset.</value>
        public Vector3Int LoadingOffset => m_LoadingOffset;

        /// <summary>
        /// This class always uses a trigger when the zone matches, so inhibit showing this field. See TpAnimZoneBase.
        /// </summary>
        public override bool ShowTriggerToggle => false;

        #endregion

        #region privateFields

        /// <summary>
        /// The position the last time that a PositionPacket was rcvd
        /// </summary>
        // ReSharper disable once NotAccessedField.Local
        private Vector3Int previousTickPosition = ImpossibleGridPosition;
        /// <summary>
        /// The current position
        /// </summary>
        private Vector3Int currentTickPosition  = ImpossibleGridPosition;

        /// <summary>
        /// The zone was triggered
        /// </summary>
        
        private bool       wasTriggered;

        /// <summary>
        /// Offset from tile position to where tilefab is loaded
        /// </summary>
        [SerializeField]
        private Vector3Int m_LoadingOffset = Vector3Int.zero;



        #endregion


        #region code
        /*in this case, the object is a Vector3Int with a position to
        test for being within the trigger bounds. Response is to post 
        an event asking for a load of tiles.
        */
        /// <summary>
        /// Interface implementation to rcv a message using PositionPacket class
        /// </summary>
        /// <param name="packet">PositionPacket</param>
        void ITpMessaging<EmptyPacket, PositionZmPacketIn>.MessageTarget(PositionZmPacketIn packet)
        {
            if (wasTriggered)
            {
                Debug.Log("Was triggered");
                return;
            }
            
            if(m_TileFab == null)
            {
                Debug.Log("null tilefab");
                return;
            }


            var pos = packet.m_Position;
            previousTickPosition =  currentTickPosition;
            currentTickPosition  =  pos;
            pos                  -= m_TileGridPosition; //remove offset
            if (!m_ZoneBoundsInt.Contains(pos))
            {
                Debug.Log("NO trigger");
                return;
            }

            wasTriggered = true;

            if (m_UseZoneManager)
            {
                var zm = packet.m_ZoneManager;

                //if not yet triggered, should we ignore this loader since its tilefab was
                //already loaded when the game started OR some other loader already placed something there?
                //Note that loading the same TileFab to the same position will be ignored. 
                if (zm != null)
                {
                    //below, effective address is the placement or 'offset' position.
                    var locator = zm.GetLocatorForGridPosition(EffectiveAddress);
                    if (zm.HasZoneRegForLocator(locator)) //if there's something already there.
                    {
                        if(TpLib.Informational)
                            Debug.Log("can't place fab, already occupied.");
                        return;
                    }
                }
            }

            TpEvents.PostTileTriggerEvent(this);
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
            wasTriggered = false;
        }

        #endregion


        #region editor
#if UNITY_EDITOR

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        /// <inheritdoc />
  
        public override string Description => "Trigger Zone tile with TileFab";

        /// <summary>
        /// Display offset position  in editor
        /// </summary>
        [TptShowField][Tooltip("Show a gizmo at the loading position")]
        public bool m_ShowOffsetPositionGizmo;

        /// <summary>
        /// Custom GUI for this tile class
        /// </summary>
        /// <param name="skin">The skin.</param>
        /// <param name="buttonSize">Size of the button.</param>
        ///         /// <param name = "noEdit" >No editing: tile in a prefab</param>
        /// <returns><c>true</c> if tile needs refresh, <c>false</c> otherwise.</returns>
        [TptShowCustomGUI()]
        public CustomGuiReturn TpTrigLoaderGui(GUISkin skin, Vector2 buttonSize, bool noEdit)
        {
            if (noEdit || m_ParentTilemap == null)
                return NoActionRequiredCustomGuiReturn;

            if (m_ShowOffsetPositionGizmo)
            {
                var grid = TpLib.GetParentGrid(m_ParentTilemap.transform);
                var copyOfLoadOffset = m_LoadingOffset;
                copyOfLoadOffset.z = 0;
                TpEditorBridge.TilemapMarquee(grid, TileUtil.CreateBoundsInt(m_TileGridPosition + copyOfLoadOffset, Vector3Int.one), Color.white, -1, false,null);
            }

            var ignorePreviewButtons = false;
            EditorGUI.BeginChangeCheck();
            if(m_ShowOffsetPositionGizmo && !(TpEditorBridge.BridgeIsPreviewActive()))
                EditorGUILayout.HelpBox("Adjusting offset is easier to do if Preview is on...",MessageType.Info);
            var result               = EditorGUILayout.Vector3IntField("Loading offset", m_LoadingOffset);
            if (EditorGUI.EndChangeCheck() && result != m_LoadingOffset)
            {
                m_LoadingOffset   = result;
                m_LoadingOffset.z = 0;

                if (TpEditorBridge.BridgeIsPreviewActiveForId(Id) && m_TileFab != null)
                {
                    TpEditorBridge.BridgePreviewTileFab(Id, m_ParentTilemap, m_TileFab, EffectiveAddress, TpTileBundle.TilemapRotation.Zero, null);
                    //TpEditorPreviews.PreviewImportedTileFab(Id, m_ParentTilemap, m_TileFab, EffectiveAddress);
                    ignorePreviewButtons = true;
                }
            }
            
            if (m_TileFab != null ) //&& !ignorePreviewButtons)
            {
                if (TpEditorBridge.BridgeIsPreviewActiveForId(Id))
                {
                    if (GUILayout.Button("Preview->OFF"))
                    {
                        if (!ignorePreviewButtons)
                            TpEditorBridge.BridgeClearPreview();
                    }
                }
                else if (GUILayout.Button("Preview->ON"))
                {
                    if(!ignorePreviewButtons)
                        TpEditorBridge.BridgePreviewTileFab(Id, m_ParentTilemap, m_TileFab, EffectiveAddress, TpTileBundle.TilemapRotation.Zero, null);
                        
                }
            }
            return m_ParentTilemap == null
                       ? new CustomGuiReturn(true)
                       : NoActionRequiredCustomGuiReturn;
        }

        #endif
        #endregion

       
    }
}
