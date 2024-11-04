// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 06-08-2021
// ***********************************************************************
// <copyright file="TpAnimZoneBase.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
#nullable enable

namespace TilePlus
{
    /// <summary>
    /// Animated zone base tile.
    /// </summary>
    [CreateAssetMenu(fileName = "TpAnimZoneBase.asset", menuName = "TilePlus/Create TpAnimZoneBase", order = 1000)]
    public class TpAnimZoneBase : TpFlexAnimatedTile, ITpMessaging<PositionPacketOut,PositionPacketIn>
    {
        #region privateFields
        /// <summary>
        /// lock x position
        /// </summary>
        [SerializeField]
        private bool m_LockXPosition;
        /// <summary>
        /// lock y position
        /// </summary>
        [SerializeField]
        private bool m_LockYPosition;
        /// <summary>
        /// lock z position
        /// </summary>
        [SerializeField]
        private bool m_LockZPosition;
        /// <summary>
        /// lock z size
        /// </summary>
        [SerializeField]
        private bool m_LockZSize;
        /// <summary>
        /// lock all
        /// </summary>
        [SerializeField] 
        private bool m_LockAll;

        /// <summary>
        /// BoundsInt of trigger zone
        /// </summary>
        [SerializeField]
        protected BoundsInt m_ZoneBoundsInt;

        /// <summary>
        /// The last zone match position
        /// </summary>
        private Vector3Int lastZoneMatchPosition = new(int.MinValue, int.MinValue, int.MinValue);



        #endregion

        #region publicFields
        
        /// <summary>
        /// Control whether or not a trigger is used when a zone match is made.
        /// </summary>
        [TptShowField(0,0,SpaceMode.None,ShowMode.Property,"ShowTriggerToggle")]
        [Tooltip("If set, a TileTriggerEvent is sent if a zone match occurs ")]
        public bool m_UseTrigger;
        
        #if UNITY_EDITOR
        /// <summary>
        /// Internal use property, editor only
        /// </summary>
        public string Modifying => $" {(m_ModifySprite ? "Zone position/size changes affect the sprite" : "Zone position/size changes do not affect the sprite")}.";
        #endif    
            
        /// <summary>
        /// GUI transform changes affect sprite transform if this is true
        /// </summary>
        [TptShowField()]
        #if UNITY_EDITOR
        [TptNote(true,"Modifying")][Tooltip("Zone changes affect sprite transform if this is set")]
        #endif
        public bool m_ModifySprite;
        
        
        #endregion


        #region publicProperties

       
        
        /// <summary>
        /// The BoundsInt for the trigger zone.
        /// </summary>
        /// <value>boundsint for trigger zone.</value>
        // ReSharper disable once MemberCanBePrivate.Global
        [TptShowAsLabelSelectionInspector(true,true,"",SpaceMode.None,ShowMode.Property,"ShowZoneBoundsInt")]
        // ReSharper disable once MemberCanBePrivate.Global
        public BoundsInt ZoneBoundsInt
        {
            get => m_ZoneBoundsInt;
            // ReSharper disable once MemberCanBePrivate.Global
            set => m_ZoneBoundsInt = value;
        }

        /// <summary>
        /// A bounds based on the ZoneBoundsInt
        /// </summary>
        /// <value>The zone bounds.</value>
        // ReSharper disable once MemberCanBePrivate.Global
        public Bounds ZoneBounds => new Bounds(ZoneBoundsInt.center, m_ZoneBoundsInt.size);

        /// <summary>
        /// Property controls whether or not the trigger toggle field is shown in the inspector.
        /// </summary>
       public virtual bool ShowTriggerToggle => true;

        #endregion

        #region code

        
        /// <summary>
        /// in this case, the object is a Vector3Int with a position to
        /// test for being  within the trigger bounds. Response is to post
        /// an simple trigger event.
        /// </summary>
        /// <param name="sentPacket">The sent packet.</param>
        void ITpMessaging<PositionPacketOut, PositionPacketIn>.MessageTarget(PositionPacketIn sentPacket)
        {
            lastZoneMatchPosition = !m_ZoneBoundsInt.Contains(sentPacket.m_Position - m_TileGridPosition)
                ? ImpossibleGridPosition
                : sentPacket.m_Position;
            if(m_UseTrigger)
                TpEvents.PostTileTriggerEvent(this);        
        }


        /// <summary>
        /// here we return the last zone match position
        /// </summary>
        /// <returns>last zone match position</returns>
        PositionPacketOut ITpMessaging<PositionPacketOut, PositionPacketIn>.GetData()
        {
            return new PositionPacketOut(lastZoneMatchPosition);
        }
         
       
        
        
        /// <inheritdoc />
        public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject go)
        {
            if (!base.StartUp(position, tilemap, go))
                return false;
            var size        = m_ZoneBoundsInt.size;
            var sizeChanged = false;
            
            if (size.x == 0) 
            {
                size.x      = 1;
                sizeChanged = true;
            }

            if (size.y == 0) 
            {
                size.y      = 1;
                sizeChanged = true;
            }
            if (size.z == 0) 
            {
                size.z      = 1;
                sizeChanged = true;
            }

            if (sizeChanged)
                m_ZoneBoundsInt.size = size;
            return true;
        }

        /// <summary>
        /// property for use with the ResetZone method's TppShowMethodAsButton attr. Editor-only
        /// </summary>
        /// <value><c>true</c> if [hide zone controls]; otherwise, <c>false</c>.</value>
        // ReSharper disable once UnusedMember.Global
        // ReSharper disable once MemberCanBePrivate.Global
#if UNITY_EDITOR
        public bool HideZoneControls => m_LockAll || IsPlayMode; 
        
        /// <summary>
        /// Internal use: provides the string value for the following TptNote attribute
        /// </summary>
        /// <value>string</value>
        public string ZoneNote => $"{(m_ModifySprite ? "Reset Zone size/position and Sprite transform" : "Reset Zone size/position only")}. Toggle 'Modify Sprite' to change.";
#endif

        
        
        /// <summary>
        /// Invoked from editors, or can be used with code.
        /// </summary>
#if UNITY_EDITOR
        [TptNote(true,"ZoneNote")]
#endif        
        [TptShowMethodAsButton("",SpaceMode.None,ShowMode.Property,"!HideZoneControls")]
        public virtual void ResetZone()
        {
            if(m_LockAll)
                return;
            
            if(m_ModifySprite)
                transform           = Matrix4x4.identity;
            ZoneBoundsInt       = new BoundsInt(Vector3Int.zero, Vector3Int.one);
            if(m_ParentTilemap != null)
                m_ParentTilemap.SetTransformMatrix(m_TileGridPosition,transform); 
            
            
            /*#if UNITY_EDITOR
            if (!m_ModifySprite)
                UpdateMarquee();
            #endif*/
        }

        /// <summary>
        /// Sets the transform.
        /// </summary>
        private void SetTransform()
        {
            if (!m_ModifySprite)
                return;
            //if any of these is zero the sprite is invisible (depending on tilemap orientation)
            var boundsSize = m_ZoneBoundsInt.size;
            if (boundsSize.x == 0)
                boundsSize.x = 1;
            if (boundsSize.y == 0)
                boundsSize.y = 1;
            if (boundsSize.z == 0)
                boundsSize.z = 1;
            m_ZoneBoundsInt.size = boundsSize;

            var cellsize = Vector3.one;
            if (m_ParentTilemap != null)
                cellsize = m_ParentTilemap.cellSize;
            
            cellsize  /= 2;
            
            
            transform =  TileUtil.ScaleMatrix(m_ZoneBoundsInt.size, m_ZoneBoundsInt.center - cellsize);

        }







        #endregion

        #region editor
#if UNITY_EDITOR
        
        /// <summary>
        /// Used to show ZoneBoundsInt 
        /// </summary>
        public bool ShowZoneBoundsInt => m_LockAll || IsPlayMode;

        /// <summary>
        /// OVERRIDE TilePlusBase prop: disallow transform editing.
        /// </summary>
        /// <value><c>true</c> if transform editing disabled; otherwise, <c>false</c>.</value>
        public override bool InternalLockTransform => m_ModifySprite;  //since we modify the transform here, inhibit modification of it in tileplusbase CustomGui code.

        /// <summary>
        /// GUI content
        /// </summary>
        private static readonly GUIContent s_BoundsIntGUIContent = new GUIContent("Zone Position and Size", "Set the trigger zone position and size");
        /// <summary>
        /// GUI content
        /// </summary>
        private static readonly GUIContent s_LockXPosGUIContent  = new GUIContent("X position", "Lock trigger zone X position");
        /// <summary>
        /// GUI content
        /// </summary>
        private static readonly GUIContent s_LockYPosGUIContent  = new GUIContent("Y position", "Lock trigger zone Y position");
        /// <summary>
        /// GUI content
        /// </summary>
        private static readonly GUIContent s_LockZPosGUIContent  = new GUIContent("Z position", "Lock trigger zone Z position");
        /// <summary>
        /// GUI content
        /// </summary>
        private static readonly GUIContent s_LockZSizeGUIContent = new GUIContent("Z size", "Lock trigger zone Z size to 1");
        /// <summary>
        /// GUI content
        /// </summary>
        private static readonly GUIContent s_LockAllGuiContent             = new GUIContent("Lock All");
        /// <summary>
        /// Const string for GUI header
        /// </summary>
        private const           string     LocksHeader           = "Lock Position and Size ";

        /// <summary>
        /// Icon to use for brush preview
        /// </summary>
        public Texture2D? m_PreviewIcon;

        /// <summary>
        /// get a preview icon for this tile. 
        /// Note only works with Tile+Brush.
        /// </summary>
        /// <value>The preview icon.</value>
        public override Texture2D? PreviewIcon => m_PreviewIcon;

       

        /// <inheritdoc />
        public override string Description => "Tile used to create trigger zones.";


        /// <summary>
        /// Internal use property, editor only
        /// </summary>
        // ReSharper disable once UnusedMember.Local
        public bool ModifySprite => m_ModifySprite;
        
        /// <summary>
        /// Visualize the trigger zone in editor
        /// </summary>
        [TptShowMethodAsButton("Momentarily visualize the trigger zone when sprite modification is active.",SpaceMode.None,ShowMode.Property,"ModifySprite")]
        public void VisualizeSprite()
        {
            if (m_ParentTilemap == null)
                return;
            var grid                = TpLib.GetParentGrid(m_ParentTilemap.transform);
            var offsetZoneBoundsInt = new BoundsInt(m_TileGridPosition + m_ZoneBoundsInt.position , m_ZoneBoundsInt.size);
            
            TpEditorBridge.TilemapMarquee(grid, offsetZoneBoundsInt, Color.clear,2,false,null);
        }
        

        /// <summary>
        /// Internal use property - editor only
        /// </summary>
        // ReSharper disable once UnusedMember.Local
        public bool ModifyAndMarqueeOff => !m_ModifySprite && marqueeId == 0; // && !TpEditorBridge.UntimedMarqueeActive(marqueeId,false);

        
        [NonSerialized]
        private ulong marqueeId;
        /// <summary>
        /// Internal use method - editor only
        /// </summary>
        [TptShowMethodAsButton("Show the trigger zone.",SpaceMode.None,ShowMode.Property,"ModifyAndMarqueeOff")]
        public void VisualizeArea()
        {
            if (m_ParentTilemap == null)
                return;
            var grid                = TpLib.GetParentGrid(m_ParentTilemap.transform);
            var offsetZoneBoundsInt = new BoundsInt(m_TileGridPosition + m_ZoneBoundsInt.position , m_ZoneBoundsInt.size);
            marqueeId = TpEditorBridge.TilemapMarquee(grid, offsetZoneBoundsInt, Color.clear,0,true, TileGuid);
        }

        /// <summary>
        /// Internal use property - editor only
        /// </summary>
        // ReSharper disable once UnusedMember.Local
        public bool ModifyAndMarqueeOn => !m_ModifySprite && marqueeId != 0 && TpEditorBridge.UntimedMarqueeActive(marqueeId,false);

        /// <summary>
        /// Internal use method - editor only
        /// </summary>
        [TptShowMethodAsButton("Hide the trigger zone.",SpaceMode.None,ShowMode.Property,"ModifyAndMarqueeOn")]
        public void VisualizeOff()
        {
            
            if (m_ParentTilemap == null || marqueeId == 0)
                return;
            /*var grid                = TpLib.GetParentGrid(m_ParentTilemap.transform);
            var offsetZoneBoundsInt = new BoundsInt(m_TileGridPosition + m_ZoneBoundsInt.position , m_ZoneBoundsInt.size);*/
            TpEditorBridge.UntimedMarqueeActive(marqueeId, true);
            marqueeId = 0;

        }

        

        /// <summary>
        /// Custom gui for this class
        /// </summary>
        /// <param name="skin">The skin.</param>
        /// <param name="buttonSize">Size of the button.</param>
        /// <param name = "noEdit" >No editing: tile in a prefab or otherwise uneditable</param>
        /// <returns><c>true</c> if tile should be refreshed, <c>false</c> otherwise.</returns>
        [TptShowCustomGUI]
        // ReSharper disable once UnusedMember.Global
        public CustomGuiReturn TpTrigBaseGui(GUISkin skin, Vector2 buttonSize, bool noEdit)
        {
            if (m_ParentTilemap == null || IsAsset) 
                return new CustomGuiReturn();
            
            
            var       doUpdate = false;

            //V2.02 fix for visualize area marquee id needing reset if tile being inspected changes. 4 Dec 23 
            if (marqueeId != 0 && !TpEditorBridge.UntimedMarqueeActive(marqueeId, false))
                marqueeId = 0;

            var result   = m_ZoneBoundsInt;
            
            if(HideZoneControls || noEdit)
                EditorGUILayout.HelpBox($"Zone Bounds: {m_ZoneBoundsInt.ToString()}",MessageType.None);
            else
                result = EditorGUILayout.BoundsIntField(s_BoundsIntGUIContent,  m_ZoneBoundsInt);
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(LocksHeader,MessageType.None,true);
            if (/*HideZoneControls ||*/ noEdit)
                return NoActionRequiredCustomGuiReturn;
            
            using (new EditorGUILayout.HorizontalScope())
            {
                //lock toggles
                if (!m_LockAll)
                {
                    var lockX     = GUILayout.Toggle(m_LockXPosition, s_LockXPosGUIContent);
                    var lockY     = GUILayout.Toggle(m_LockYPosition, s_LockYPosGUIContent);
                    var lockZ     = GUILayout.Toggle(m_LockZPosition, s_LockZPosGUIContent);
                    var lockZSize = GUILayout.Toggle(m_LockZSize, s_LockZSizeGUIContent);

                    if (!noEdit)
                    {
                        //updating
                        if (lockX != m_LockXPosition)
                        {
                            m_LockXPosition = lockX;
                            doUpdate        = true;
                        }

                        if (lockY != m_LockYPosition)
                        {
                            m_LockYPosition = lockY;
                            doUpdate        = true;
                        }

                        if (lockZ != m_LockZPosition)
                        {
                            m_LockZPosition = lockZ;
                            doUpdate        = true;
                        }

                        if (lockZSize != m_LockZSize)
                        {
                            m_LockZSize = lockZSize;
                            doUpdate    = true;
                        }
                    }
                }

                var lockAll = GUILayout.Toggle(m_LockAll, s_LockAllGuiContent);
                
                if (!noEdit && (lockAll != m_LockAll))
                {
                    m_LockAll = lockAll;
                    doUpdate  = true;
                }
            }

            if (noEdit)
                return NoActionRequiredCustomGuiReturn;
            
            //process any changes
            if (m_LockAll || result == m_ZoneBoundsInt) //nothing to do.
                return new CustomGuiReturn(doUpdate);

            var resultPos = result.position;
            if (m_LockXPosition)
                resultPos.x = m_ZoneBoundsInt.position.x;
            if (m_LockYPosition)
                resultPos.y = m_ZoneBoundsInt.position.y;
            if (m_LockZPosition)
                resultPos.z = m_ZoneBoundsInt.position.z;
            result.position = resultPos;

            var size = result.size;
            if (m_LockZSize)
                size.z = m_ZoneBoundsInt.size.z;

            if (size.x < 1)
                size.x = 1;
            if (size.y < 1)
                size.y = 1;
            if (size.z < 1)
                size.z = 1;
            result.size = size;


            doUpdate |= result != m_ZoneBoundsInt;
            if (!doUpdate)
                return new CustomGuiReturn();

            m_ZoneBoundsInt = result;

            if (!m_ModifySprite)
            {
                if(marqueeId != 0) 
                {
                    VisualizeOff(); //this deletes the previous marquee and creates a new one.
                    VisualizeArea();
                }
                return new CustomGuiReturn(false);
            }

            SetTransform();
            //ensure transform not locked.
            var tFlags = m_ParentTilemap.GetTileFlags(m_TileGridPosition);
            m_ParentTilemap.SetTileFlags(m_TileGridPosition, tFlags & ~TileFlags.LockTransform  );
            m_ParentTilemap.SetTransformMatrix(m_TileGridPosition, transform);

            return new CustomGuiReturn(true);
        }
        #endif
        #endregion


        
    }
}
