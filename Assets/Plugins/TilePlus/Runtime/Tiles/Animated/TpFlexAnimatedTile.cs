// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 02-03-2023
// ***********************************************************************
// <copyright file="TpFlexAnimatedTile.cs" company="Jeff Sasmor">
//     Copyright (c) 2023 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
#if ODIN_INSPECTOR
#define USE_ODIN
#endif

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

#nullable enable

namespace TilePlus
{

    /// <summary>
    /// FlexAnimated Tiles are animated tiles using animation clips from an asset.
    /// Several animation sequences can be set up and selected at runtime.
    /// Animation can be started and stopped at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "TpFlexAnimatedTile.asset", menuName = "TilePlus/Create TpFlexAnimatedTile", order = 1000)]
    public class TpFlexAnimatedTile : TilePlusBase, ITilePlus
    {
        #region properties

        /// <inheritdoc />
        public bool AnimationSupported => true;
        
        /// <summary>
        /// read-only access to animation index: which clip is being used.
        /// </summary>
        // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
        public int AnimationClipIndex => animationIndex;

        /// <summary>
        /// Is the animation waiting to rewind?
        /// </summary>
        [TptShowAsLabelSelectionInspector(true, true, "", SpaceMode.None, ShowMode.InPlay)]
        public override bool IsOneShotWaitingToRewind => isWaitingForRewind;

        /// <summary>
        /// Get the current animation clip. Will return NULL if no valid clip available.
        /// </summary>
        private TpAniClip? CurrentClip
        {
            get
            {
                var validClipSet = m_ClipSet != null && m_ClipSet.NumClips > 0;
                if (!validClipSet || animationIndex < 0 || animationIndex >= m_ClipSet!.NumClips)
                    return null;
                return m_ClipSet.m_Clips[animationIndex];
            }
        }

        /// <summary>
        /// Is this a loop-once animation?
        /// </summary>
        private bool AnimationIsLoopOnce => m_ParentTilemap != null &&
                                            (m_ParentTilemap.GetTileAnimationFlags(m_TileGridPosition) & TileAnimationFlags.LoopOnce)
                                            == TileAnimationFlags.LoopOnce;
        //Get the animation speed
        private float AnimationSpeed => m_UseAnimationSpeedOverride ? m_AnimationSpeedOverride : 
                                            CurrentClip?.m_AnimationSpeed ?? 1f;
        
        /// <summary>
        /// A string with animation clipset name
        /// </summary>
        [TptShowAsLabelSelectionInspector(true, false, "Active animation clip", SpaceMode.None, ShowMode.Property, "!ShowTpFlexAnimCustomGui")]
        public string TileAniClip => m_ClipSet == null ? "Empty" : m_ClipSet.name;
        
        #endregion
        
        #region privateFields
        
        /// <summary>
        /// Is this the first time StartUp has been run on this tile instance?
        /// </summary>
        private bool firstStartUp;

        /// <summary>
        /// index into the individual animations in the clipset
        /// </summary>
        private int animationIndex = -1;

        /// <summary>
        /// state variable used for RestartAnimation
        /// </summary>
        private bool hasRefreshed;

        /// <summary>
        /// The new animation name
        /// </summary>
        private string newAnimationName = string.Empty;

        //Used to control animation on and pause states
        private bool animationShouldActivate;

        /// <summary>
        /// Backing field for IsOneShotWaitingToRewind property. Do not want a public 'set' for this.
        /// </summary>
        private bool isWaitingForRewind;

        
        #endregion

        #region publicfields

        /// <summary>
        /// Play animation on Startup
        /// </summary>
        [Tooltip("Check this to have the selected animation begin at Startup")]
        [TptShowField(0, 0, SpaceMode.None, ShowMode.NotInPlay)]
        public bool m_PlayOnStart = true;

        /// <summary>
        /// Default sprite when selected animation sequence is invalid.
        /// </summary>
        [Tooltip("The default sprite when clip is null or of length 0")]
        [TptShowObjectField(typeof(Sprite), false, false, SpaceMode.None, ShowMode.NotInPlay)] //false means no scene objects
        public Sprite? m_DefaultSprite;

        /// <summary> 
        /// The asset with the animation clips
        /// </summary>
        [TptShowObjectField(typeof(TpSpriteAnimationClipSet), false, true, SpaceMode.None, ShowMode.Property, "ShowTpFlexAnimCustomGui", true)]
        [Tooltip("A SpriteAnimationClipSet asset.")]
        public TpSpriteAnimationClipSet? m_ClipSet;

        /// <summary>
        /// Override clip animation speed if true
        /// </summary>
        [Tooltip("Check to override the animation speed in clip.")]
        [TptShowField(0, 0, SpaceMode.None, ShowMode.Property, "ShowUseAnimationSpeedOverride")]
        public bool m_UseAnimationSpeedOverride;

        /// <summary>
        /// Value used to override clip animation speed.
        /// </summary>
        [TptShowField(0.1f, 50f, SpaceMode.None, ShowMode.Property, "UseAnimSpdOvrd")]
        [Tooltip("Override for Animation Speed in clip.")]
        public float m_AnimationSpeedOverride = 1;

        /// <summary>
        /// See the description of TileAnimationFlags in the Unity scripting reference.
        /// Override animation clip one-shot setting to FORCE one-shot for any clip.
        /// </summary>
        [Tooltip("Check to force one-shot animation,  overriding settings in the Animation Clip Set")]
        [TptShowField(0, 0, SpaceMode.None, ShowMode.Always, "", false, true)]
        public bool m_ForceOneShot;

        /// <summary>
        /// If Force-One shot animation is used, this flag determines if a rewind occurs.
        /// </summary>
        [Tooltip("If ForceOneShot is checked, should the animation rewind after the one-shot is complete?")]
        [TptShowField(0, 0, SpaceMode.None, ShowMode.Always, "", false, true)]        
        public bool m_RewindAfterForcedOneShot;
        
        /// <summary>
        /// See the description of TileAnimationFlags in the Unity scripting reference.
        /// This controls the UpdatePhysics flags
        /// </summary>
        [Tooltip("Update Physics Shape for TilemapCollider2D when animation switches to each new Sprite")]
        [TptShowField()]
        public bool m_UpdatePhysics;
        
        /// <summary>
        /// Name of the active animation sequence
        /// </summary>
        [SerializeField]
        [Tooltip("Initial clip name. Overwritten if inspector dropdown is used.")]
        protected string? m_ActiveAnimationClipName;


        #endregion

        #region constants

        /// <summary>
        /// const error string
        /// </summary>
        private const string InvalidAnimStart = "Error starting Animation: Parent tilemap = null or invalid tile grid position";

        #endregion


        #region events

        /// <summary>
        /// OnEnable event handler.
        /// </summary>
        public virtual void OnEnable()
        {
            if (IsAsset || m_ClipSet == null)
                return;

            if (string.IsNullOrEmpty(m_ActiveAnimationClipName))
            {
                animationIndex = 0;
                return;
            }

            if (!SetAnimation(m_ActiveAnimationClipName))
                animationIndex = 0;
        }
        
        #endregion

        #region animControl
        
        /// <summary>
        /// Change animation. Animation is turned on and the tile is refreshed.
        /// If animation is already running then RestartAnimation is used.
        /// </summary>
        /// <param name="newAnimName">The new animation sequence</param>
        public void ChangeAnimation(string newAnimName)
        {
            if (AnimationIsPaused)
            {
                RestartAnimation(newAnimName);
                return;
            }

            SetAnimation(newAnimName);
            ActivateAnimation(true);
        }

        /// <summary>
        /// Restart an animation. If animation isn't running,
        /// uses ActivateAnimation(true).
        /// </summary>
        /// <param name="optionalNewAnimation">name of an optional animation to change to.</param>
        // ReSharper disable once MemberCanBeProtected.Global
        public virtual void RestartAnimation(string optionalNewAnimation = "")
        {
            OneShotEndedCallback = null;
            if (!AnimationIsPaused)
            {
                if (optionalNewAnimation != string.Empty)
                    SetAnimation(optionalNewAnimation);
                ActivateAnimation(true);
                return;
            }

            hasRefreshed = false;
            ActivateAnimation(false);
            newAnimationName = optionalNewAnimation;
            TpLib.DelayedCallback(this, DelayedActivateAnimation,"TPFlexAnimTile: DelayedActivateAnimation");
            
            void DelayedActivateAnimation()
            {
                if (!hasRefreshed)
                    return;
                if (newAnimationName != string.Empty)
                {
                    SetAnimation(newAnimationName);
                    newAnimationName = string.Empty;
                }

                ActivateAnimation(true);
            }
        }

        /// <summary>
        /// Use this to turn animation on and off.
        /// Note that restarting a running animation isn't
        /// automatic. Check animationOn field (in a subclass)
        /// or AnimationIsOn property from other code,
        /// and if it's true then use RestartAnimation instead.
        /// </summary>
        /// <param name="turnOn">true to start animation or false to shut it off</param>
        /// <param name = "startingFrame" >sets the current frame to 0 (for either operation)</param>
        /// <param name = "ignoreRewindingState" >if false(default) then this method does not execute when waiting for a rewind - only when one-shot is used w/rewindAfterOneShot set true </param>
        ///  <remarks>When turnOn==true, the startingFrame is set prior to starting animation. When false,
        /// the startingFrame is set after stopping the animation</remarks>
        // ReSharper disable once MemberCanBeProtected.Global
        public override void ActivateAnimation(bool turnOn, int startingFrame = 0, bool ignoreRewindingState = false)
        {
            if (m_ParentTilemap == null || TileGridPosition == ImpossibleGridPosition)
            {
                OneShotEndedCallback = null;
                Debug.LogError(InvalidAnimStart);
                return;
            }
            if (!ignoreRewindingState && IsOneShotWaitingToRewind)
                return;

            var clip = CurrentClip;
            if(clip == null)
                return;
            
            //In-editor, don't do anything if this is the palette or the tile is an asset.
            #if UNITY_EDITOR
            if(!IsPlayMode || (!IsClone && TpLib.IsTilemapFromPalette(m_ParentTilemap)) )
                return;
            #endif
            
            animationShouldActivate = false;
            if (turnOn)
            {
                var hasRewound = false;
                if (TileAnimationActive)
                {
                    if(AnimationIsLoopOnce) //note that when GetTileAnimationData is called, the looponce flag is restored.
                        m_ParentTilemap.RemoveTileAnimationFlags(m_TileGridPosition, TileAnimationFlags.LoopOnce);

                    if (startingFrame >= 0)
                    {
                        //test for invalid startingFrame
                        hasRewound = true;
                        var nFrames = m_ParentTilemap.GetAnimationFrameCount(m_TileGridPosition);
                        if (startingFrame >= nFrames)
                            startingFrame = nFrames - 1;

                        m_ParentTilemap.SetAnimationFrame(m_TileGridPosition, startingFrame);
                    }
                    else //reset to zeroth frame
                        m_ParentTilemap.SetAnimationFrame(m_TileGridPosition, 0);
                    
                    if (AnimationIsPaused) //here, animation is active but currently paused.
                    {
                        PauseAnimation(false); //so un-pause it.
                        if (clip is { m_OneShot: true, m_RewindAfterOneShot: true } || 
                            (m_ForceOneShot && m_RewindAfterForcedOneShot))
                            SetTimeout();
                        return;
                    }    
                }  //drop-down into the next block if animation isn't paused.

                //if TileAnimationActive was true but the AnimationIsPaused test failed,
                //then this may have been done already. Otherwise still need to make this check
                //anyway since the startingFrame param could still be >= 0
                if (!hasRewound) 
                {
                    if (startingFrame >= 0)
                    {
                        //test for invalid startingFrame
                        var nFrames = m_ParentTilemap.GetAnimationFrameCount(m_TileGridPosition);
                        if (startingFrame >= nFrames)
                            startingFrame = nFrames - 1;

                        m_ParentTilemap.SetAnimationFrame(m_TileGridPosition, startingFrame);
                    }
                    else //reset to zeroth frame
                        m_ParentTilemap.SetAnimationFrame(m_TileGridPosition, 0);
                }
                
                //animation is inactive so activate it. 
                animationShouldActivate = true;
                RefreshLater("TpAnimTile:ActivationRefresh",
                             (clip is { m_OneShot: true, m_RewindAfterOneShot: true } || (m_ForceOneShot && m_RewindAfterForcedOneShot))
                                  ? SetTimeout
                                  : null);
            }
            else
            {
                if (!TileAnimationActive) //here, animation is active so just pause it
                    return;               //if not active, nothing else to do

                if(AnimationIsLoopOnce)
                    m_ParentTilemap.RemoveTileAnimationFlags(m_TileGridPosition, TileAnimationFlags.LoopOnce);
                PauseAnimation(true);
                if (startingFrame < 0)
                    return;
                //test for invalid startingFrame
                var nFrames = m_ParentTilemap.GetAnimationFrameCount(m_TileGridPosition);
                if (startingFrame >= nFrames)
                    startingFrame = nFrames - 1;

                m_ParentTilemap.SetAnimationFrame(m_TileGridPosition, startingFrame);
            }
        }
        
        
        private void SetTimeout()
        {
            if(m_ParentTilemap == null)
                return;
            //note timeout is in seconds but DelayedCallback requires milliseconds.
            var timeout = TileUtil.GetOneShotTimeOut(m_ParentTilemap.GetAnimationFrameCount(m_TileGridPosition),
                                                     AnimationSpeed,
                                                     m_ParentTilemap);
            isWaitingForRewind = true;

            //note that the 'forceTaskDelay' param is true in this method call. Forces use of Task.Delay.
            TpLib.DelayedCallback(this, Rewind, "TPFlexAnimTile:rewind", (int)(timeout * 1000), false, true);

            void Rewind()
            {
                isWaitingForRewind = false;

                if (m_ParentTilemap == null)
                    return;
                PauseAnimation(true);
                m_ParentTilemap.SetAnimationFrame(m_TileGridPosition, 0);
                OneShotEndedCallback?.Invoke();
                OneShotEndedCallback = null;
            }
        }
        

        /// <inheritdoc />
        public override void PauseAnimation(bool pause)
        {
            if(m_ParentTilemap == null || m_ClipSet == null || m_TileGridPosition == ImpossibleGridPosition)
                return;
            if(pause)
                m_ParentTilemap.AddTileAnimationFlags(m_TileGridPosition, TileAnimationFlags.PauseAnimation);
            else
            {
                if ( CurrentClip is { m_OneShot: true } ||  m_ForceOneShot)
                    m_ParentTilemap.AddTileAnimationFlags(m_TileGridPosition, TileAnimationFlags.LoopOnce);
                m_ParentTilemap.RemoveTileAnimationFlags(m_TileGridPosition, TileAnimationFlags.PauseAnimation);
            }
        }

        
        
        
        /// <summary>
        /// see ActivateAnimation
        /// </summary>
        /// <param name="aName">Name of animation sequence</param>
        /// <returns>true for success or false if any error.</returns>
        private bool SetAnimation(string aName)
        {
            //test for uninitialized state, do nothing. This is called from OnEnable so this check is necc.
            if (IsAsset || m_ClipSet == null)  
                return false;

            var numClips = m_ClipSet.NumClips;
            switch (numClips)
            {
                case 0:
                    return false;
                case 1:
                    animationIndex = 0;
                    return true;
            }
            
            var index = m_ClipSet.m_Clips.FindIndex(def => def.m_Name == aName);
            if (index >= 0 && index < m_ClipSet.NumClips)
            {
                animationIndex  = index;
                return true;
            }
            else
            {
                animationIndex = 0;
                return true;
            }
        }
        
        #endregion
        
        #region overrides

        /// <summary>
        /// StartUp method for TpFlexAnimatedTile
        /// </summary>
        /// <param name="position">Position of the Tile on the Tilemap.</param>
        /// <param name="tilemap">The Tilemap the tile is present on.</param>
        /// <param name="go">The GameObject instantiated for the Tile.</param>
        /// <returns>Whether the call was successful.</returns>
        public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject go)
        {
            if (m_AnimationSpeedOverride <= 0)
                m_AnimationSpeedOverride = 1;
            
            //this has to be the first thing to do.
            if (!base.StartUp(position, tilemap, go)) //returns false if target is palette
                return false;
            
            hasRefreshed = true;


            #if UNITY_EDITOR
            if (!Application.isPlaying || m_ClipSet == null || m_ClipSet.NumClips == 0)
                return true;
            #else
            if (m_ClipSet == null)
                return true;
            #endif
           
            if (animationIndex == -1)
                SetAnimation(m_ClipSet.AnimationClipNames[0]);

            if (!m_PlayOnStart || firstStartUp)
                return true;
            firstStartUp = true;
            ActivateAnimation(true);

            return true;
        }




        /// <summary>
        /// Retrieves any tile rendering data from the scripted tile.
        /// </summary>
        /// <param name="position">Position of the Tile on the Tilemap.</param>
        /// <param name="tilemap">The Tilemap the tile is present on.</param>
        /// <param name="tileData">Data to render the tile.</param>
        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            base.GetTileData(position, tilemap, ref tileData);
            
            #if UNITY_EDITOR
            //this is important to prevent visual oddities in palette, which is itself a tilemap.
            if(!IsClone && TpLib.IsTilemapFromPalette(tilemap) )
                return;
            #endif
            
            if (SpriteShouldBeCleared || m_ClipSet == null)
                return;

            if (animationIndex == -1)
            {
                if (string.IsNullOrEmpty(m_ActiveAnimationClipName))
                    m_ActiveAnimationClipName = "default";
                SetAnimation(m_ActiveAnimationClipName);
            }

            var validClipSet = m_ClipSet != null && m_ClipSet.NumClips > 0;
            if (!validClipSet || animationIndex < 0 || (m_ClipSet != null && animationIndex >= m_ClipSet.NumClips))
            {
                tileData.sprite = m_DefaultSprite == null ? sprite : m_DefaultSprite;
                return;
            }
            
            #if UNITY_EDITOR
            if (isSimulating )
            {
                tileData.sprite = m_ClipSet!.m_Clips[animationIndex].m_Sprites[NextSpriteIndex];
                return;
            }
            #endif

            
            var animDef   = m_ClipSet!.m_Clips[animationIndex];
            var animCount = animDef.m_Sprites.Length;
            if (animCount == 0)
                tileData.sprite = m_DefaultSprite == null ? sprite : m_DefaultSprite;
            else
            {
                var defaultTileIndex = animDef.m_DefaultTileIndex;
                if (defaultTileIndex < 0 || defaultTileIndex >= animCount)
                    tileData.sprite = m_DefaultSprite == null ? sprite : m_DefaultSprite;
                else
                    tileData.sprite = animDef.m_Sprites[defaultTileIndex];
            }
        }

        /// <summary>
        /// Get animation info for this tile.
        /// </summary>
        /// <param name="position">Position.</param>
        /// <param name="tilemap">An ITilemap instance for accessing the tile</param>
        /// <param name="tileAnimationData">ref to TileAnimationData for this method to fill in.</param>
        /// <returns>Whether the call was successful.</returns>
        public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
        {
            //note no need to call base since all it does is return false;
            
            if (!animationShouldActivate)
                return false;
            
            #if UNITY_EDITOR
            //this is important to prevent animation in the palette window.
            if(isSimulating || (!IsClone && TpLib.IsTilemapFromPalette(tilemap)) )
                return false;
            #endif
            
            if (SpriteShouldBeCleared)
                return false;

            var animDef = CurrentClip;
            /*
            var validClipSet  = m_ClipSet != null && m_ClipSet.NumClips > 0;
            
            if (!validClipSet || animationIndex < 0 || animationIndex >= m_ClipSet.NumClips)
                return false;
            */
            if (animDef == null)
                return false;
            
            //var animDef = m_ClipSet.m_Clips[animationIndex];
            tileAnimationData.animatedSprites = animDef.m_Sprites; //note this is an array. 
            var animationSpeed = m_UseAnimationSpeedOverride ? m_AnimationSpeedOverride : animDef.m_AnimationSpeed;
            tileAnimationData.animationSpeed     = animationSpeed > 0 ? animationSpeed : 1f;
            tileAnimationData.animationStartTime = 0;
            tileAnimationData.flags              = GetTileAnimationFlags(animDef);
            return true;
        }
        
        
        private TileAnimationFlags GetTileAnimationFlags(TpAniClip clip)
        {
            // ReSharper disable once ReplaceWithSingleAssignment.True
            var pause = true;

            var playOnStart = clip.m_OneShot || m_PlayOnStart;
            
            #if UNITY_EDITOR
            if( (playOnStart || animationShouldActivate)  && Application.isPlaying)
                #else
            if(playOnStart || animationShouldActivate)  
                #endif
                pause = false;

            var tFlags = TileAnimationFlags.None;
            if (pause)
                tFlags |= TileAnimationFlags.PauseAnimation;

            if(m_UpdatePhysics)
                tFlags |= TileAnimationFlags.UpdatePhysics;
            if (m_ForceOneShot || clip.m_OneShot) 
                tFlags |= TileAnimationFlags.LoopOnce;

            return tFlags;
        }
        #endregion
        
        #region utility

        /// <summary>
        /// Callback invoked after a one-shot animation has completed. Note: this should be a very short method.
        /// </summary>
        /// <remarks>This callback is cleared after each use or animation ending for any other reason. </remarks>>
        public Action? OneShotEndedCallback { get; set; }
        
        /// <summary>
        /// Used to reset state variables. May need overriding
        /// in subclasses.
        /// See programmer's guide for info on overriding this.
        /// </summary>
        /// <param name="op">The type of reset operation</param>
        public override void ResetState(TileResetOperation op)
        {
            base.ResetState(op);
            hasRefreshed  = false;
            firstStartUp  = false;
        }
        
        /// <inheritdoc />
        public override bool UpdateInstance(string [] value)
        {
            var status = base.UpdateInstance(value);

            if (!value.Contains("m_ClipSet"))
                return status;
            m_ActiveAnimationClipName = "default";
            if (m_ClipSet == null)
                return status;
            var names = m_ClipSet.AnimationClipNames;
            if(names.Length != 0)
                m_ActiveAnimationClipName = names[0];
            SetAnimation(m_ActiveAnimationClipName);
            return status;

        }


        #endregion

#if UNITY_EDITOR

        #region EditorPublicProperties
        /// <summary>
        /// internal use property, editor-only
        /// </summary>
        public virtual bool ShowUseAnimationSpeedOverride => true; 
        
        
        /// <summary>
        /// internal use property, editor-only
        /// </summary>
        /// <value><c>true</c> if [use anim SPD ovrd]; otherwise, <c>false</c>.</value>
        public bool UseAnimSpdOvrd => m_UseAnimationSpeedOverride;
        
        
        /// <summary>
        /// used for Run anim button GUI
        /// </summary>
        /// <value><c>true</c> if [show run anim button]; otherwise, <c>false</c>.</value>
        public bool ShowRunAnimButton => IsPlayMode && !AnimationIsRunning; 

        /// <summary>
        /// used for Stop anim button GUI
        /// </summary>
        /// <value><c>true</c> if [show stop anim button]; otherwise, <c>false</c>.</value>
        public bool ShowStopAnimButton => IsPlayMode && AnimationIsRunning; 

        /// <summary>
        /// Subclasses use this to inhibit showing the custom gui in this class.
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public virtual bool ShowTpFlexAnimCustomGui => true; 


        

        /// <summary>
        /// Override of base class to provide a different description
        /// </summary>
        /// <value>The description.</value>
        public override string Description => "Animated tile with multiple animations";

        /// <summary>
        /// Property to get number of clips in the animation clip asset
        /// </summary>
        /// <value>The animations in asset.</value>
        [TptShowAsLabelBrushInspector(true,true,"Number of available animation sequences")]
        public string AnimationsInAsset => m_ClipSet == null ? "0 (no asset)" : this.m_ClipSet.NumClips.ToString();
        #endregion

        #region EditorCode

        /// <summary>
        /// Start animating: Editor Only
        /// </summary>
        [TptShowMethodAsButton("Run Animation",SpaceMode.None,ShowMode.Property,"ShowRunAnimButton")]
        public void RunAnimation()
        {
            ActivateAnimation(true);
        }

        /// <summary>
        /// Stop animating: editor only
        /// </summary>
        [TptShowMethodAsButton("Stop Animation",SpaceMode.None,ShowMode.Property,"ShowStopAnimButton")]
        public void StopAnimation()
        {
            ActivateAnimation(false);
        }



        
         /// <summary>
        /// A custom GUI for changing clip sets.
        /// Note is NOT virtual for a reason.
        /// Subclasses should use a different name
        /// </summary>
        /// <param name="skin">The skin.</param>
        /// <param name="buttonSize">Size of the button.</param>
        /// <param name = "noEdit" >No editing: tile in a prefab</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        [TptShowCustomGUI(SpaceMode.None,ShowMode.Property,"ShowTpFlexAnimCustomGui")]
         public CustomGuiReturn FlexAnimTileGui(GUISkin skin, Vector2 buttonSize, bool noEdit)
        {
            if (m_ClipSet == null)
            {
                EditorGUILayout.HelpBox("Missing a Clip Set!",MessageType.Warning,true);
                return NoActionRequiredCustomGuiReturn;
            }

            if (m_ClipSet.NumClips != 0 && animationIndex != -1)
            {
                var currentClipFrameCount = m_ClipSet.m_Clips[animationIndex].m_Sprites.Length;
                EditorGUILayout.HelpBox($"Current clip has {currentClipFrameCount} frames",MessageType.None);
            }
            var names    = m_ClipSet.AnimationClipNames;
            var numNames = names.Length;
            switch (numNames)
            {
                case 0:
                    EditorGUILayout.HelpBox("Asset has no animations!",MessageType.Warning,true);
                    return NoActionRequiredCustomGuiReturn;
                default: 
                    EditorGUILayout.HelpBox($"Asset has {numNames} animation clip{(numNames>1?"s":string.Empty)}",MessageType.None,true);
                    break;
            }

            if (noEdit)
            {
                EditorGUILayout.HelpBox($"Selected animation clip: {m_ActiveAnimationClipName}", MessageType.None, true);
                return NoActionRequiredCustomGuiReturn;
            }
                
            var namesGUIContent      = new GUIContent[numNames];
            var optionValues = new int[numNames];
            for (var i = 0; i < numNames; i++)
            {
                namesGUIContent[i]      = new GUIContent( names[i] );
                optionValues[i] = i;
            }

            var selected     = 0;
            if (!string.IsNullOrWhiteSpace(m_ActiveAnimationClipName))
            {
                selected = names.ToList().FindIndex(s => s == m_ActiveAnimationClipName);
                if (selected == -1)
                    selected = 0;
            }

            var guiCont = new GUIContent("Clip to use:", "Select the clip to use when animating");
            var result  = EditorGUILayout.IntPopup(guiCont, selected, namesGUIContent, optionValues,GUILayout.ExpandWidth(true));
            if (result < 0 || result == selected)
                return NoActionRequiredCustomGuiReturn;
            var resultName = names[result];
            if (resultName == m_ActiveAnimationClipName)
                return NoActionRequiredCustomGuiReturn;
            m_ActiveAnimationClipName = resultName;
            animationIndex      = result;
            return new CustomGuiReturn(true);
        }

        #endregion

        #region Simulation
        /// <summary>
        /// Returns true if this Tile can perform a simulation
        /// </summary>
        /// <value><c>true</c> if this instance can simulate; otherwise, <c>false</c>.</value>
        public bool CanSimulate  => true; //implementation says yes we can simulate
        /// <summary>
        /// Returns true when this tile is actively simulating
        /// </summary>
        /// <value><c>true</c> if this instance is simulating; otherwise, <c>false</c>.</value>
        public bool IsSimulating => isSimulating; //are we simulating currently?

        //how many ticks to skip
        /// <summary>
        /// The number of ticks to skip when simulating.
        /// Implementation needs an editable field.
        /// </summary>
        /// <value>The simulation skip ticks.</value>
        public int SimulationSkipTicks => m_SimulationSpeed;

        /// <summary>
        /// How long before the simulation ends, in Editor ticks.
        /// </summary>
        /// <value>The simulation timeout.</value>
        public int SimulationTimeout   => m_SimulationTimeout;

        /// <summary>
        /// Used to adjust simulation speed.
        /// </summary>
        [Tooltip("Adjust simulation speed in editor. Higher #s slow down simulation.")] 
        [TptShowField(128,1, SpaceMode.SpaceBefore, ShowMode.NotInPlay)]
        public int m_SimulationSpeed = 2;

        /// <summary>
        /// Used to adjust sim timeout
        /// </summary>
        [Tooltip("Simulation timeout in Editor update ticks")] 
        [TptShowField(100,10000, SpaceMode.SpaceAfter, ShowMode.NotInPlay)]
        public int m_SimulationTimeout = 1000;


        /// <summary>
        /// get the next sprite index while simulating
        /// </summary>
        /// <value>The index of the next sprite.</value>
        protected virtual int NextSpriteIndex
        {
            get
            {
                if (spriteIndexForSim <= totalNumSprites - 1)
                    return spriteIndexForSim;
                spriteIndexForSim = 0;
                return spriteIndexForSim;
            }
        }

        //simulation state variables. No need for serializing.
        /// <summary>
        /// True when simulation is running
        /// </summary>
        private bool isSimulating;
        /// <summary>
        /// The sim timeout
        /// </summary>
        private int  simTimeout;
        /// <summary>
        /// The sprite index for sim
        /// </summary>
        private int  spriteIndexForSim;
        /// <summary>
        /// The total number of sprites
        /// </summary>
        private int  totalNumSprites;
        /// <summary>
        /// The ticks to skip
        /// </summary>
        private int  ticksToSkip;
        /// <summary>
        /// The skip counter
        /// </summary>
        private int  skipCounter;
        

        /// <summary>
        /// Simulation control.
        /// </summary>
        /// <param name="start">true to start, false to stop</param>
        /// <remarks>Simulation will force-end if any fields are edited in the Selection Inspector</remarks>
        public void Simulate(bool start)
        {
            
            isSimulating = false; //this isn't a mistake
            if (m_ParentTilemap == null || m_ClipSet == null || m_ClipSet.NumClips == 0 || animationIndex < 0)
                return;
            if (start)
            {
                totalNumSprites = m_ClipSet.m_Clips[animationIndex].m_Sprites.Length;
                if (totalNumSprites < 2)
                    return;

                ticksToSkip     = SimulationSkipTicks;
                simTimeout      = SimulationTimeout > 60 ? SimulationTimeout : 60;
                skipCounter     = 0;

                //Debug.Log($"STARTING simulation for {simTimeout.ToString()} ticks, skip {ticksToSkip.ToString()}, map {m_ParentTilemap}, position {m_TileGridPosition.ToString()})");
                spriteIndexForSim = 0;
                isSimulating      = true;
                TpLib.TimerHookForSimulation += Simulator;
            }
            else
            {
               // Debug.Log("ENDING simulation");
                TpLib.TimerHookForSimulation -= Simulator;

                m_ParentTilemap.RefreshTile(m_TileGridPosition);
            }
        }

        /// <summary>
        /// performs the simulation
        /// </summary>
        /// <param name="selectionChanged">if set to <c>true</c> [selection changed].</param>
        protected virtual void Simulator(bool selectionChanged)
        {
            if(!isSimulating)
                return;

            if (selectionChanged || m_ClipSet == null || m_ParentTilemap == null)
            {
                Simulate(false);
                return;
            }

            
            var oneShot = m_ForceOneShot || m_ClipSet.m_Clips[animationIndex].m_OneShot;
            if(oneShot && (spriteIndexForSim + 1) > (totalNumSprites - 1) )
                Simulate(false);
            
            if (--simTimeout > 0)
            {
                /*if (oneShot && (spriteIndexForSim+1) > (totalNumSprites -1 ) )
                    spriteIndexForSim = totalNumSprites - 1;
                else
                {*/
                    if (--skipCounter > 0)
                        return;
                    skipCounter = ticksToSkip;
                    spriteIndexForSim++;
                    m_ParentTilemap.RefreshTile(TileGridPosition);
               // }
            }
            else
                Simulate(false);
        }

        

        #endregion
        
        #endif

       
        
        
    }
}
