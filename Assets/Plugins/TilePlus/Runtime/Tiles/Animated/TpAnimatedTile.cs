// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 02-03-2023
// ***********************************************************************
// <copyright file="TpAnimatedTile.cs" company="Jeff Sasmor">
//     Copyright (c) 2023 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
#if ODIN_INSPECTOR
#define USE_ODIN
using Sirenix.OdinInspector;
#endif
using UnityEngine;
using UnityEngine.Tilemaps;

#nullable enable

namespace TilePlus
{
    /// <summary>
    /// Simple version of an animated tile that works with TilePlus.
    /// </summary>
    [CreateAssetMenu(fileName = "TpAnimatedTile.asset", menuName = "TilePlus/Create TpAnimatedTile", order = 1000)]
    public class TpAnimatedTile : TilePlusBase, ITilePlus
    {
        #region publicfields
        /// <summary>
        /// Set true to have the animation play on start
        /// </summary>
        [Tooltip("Play on start (or not).")]
        [TptShowField(0,0,SpaceMode.None,ShowMode.NotInPlay)]
        public bool m_PlayOnStart;

        /// <summary>
        /// Set animation speed. Note to change via code, turn animation off, change value, then turn animation on.
        /// Alternatively, you could change the value and use Tilemap.RefreshTile
        /// </summary>
        [Tooltip("Animation speed relative to the tilemap's speed. See code for runtime use. NOT used for simulation.")]
        [TptShowField]
        public float m_AnimationSpeed = 1f;

        /// <summary>
        /// See the description of TileAnimationFlags in the Unity scripting reference.
        /// Control whether or not the animation repeats
        /// </summary>
        [Tooltip("Does animation repeat or just play once?")]
        [TptShowField(0, 0, SpaceMode.None, ShowMode.Always, "", false, true)]
        public bool m_OneShot;

        /// <summary>
        /// If one shot animation is used, this flag determines if a rewind occurs.
        /// </summary>
        [Tooltip("If OneShot is checked, should the animation rewind after the one-shot is complete?")]
        [TptShowField(0, 0, SpaceMode.None, ShowMode.Always, "", false, true)]        
        public bool m_RewindAfterOneShot;

        /// <summary>
        /// The List of Sprites set for the Animated Tile.
        /// This will be played in sequence.
        /// </summary>
        [Tooltip("The sprites to animate, in order of appearance.")]
        #if USE_ODIN
        [InlineEditor(InlineEditorModes.FullEditor)]
        #endif
        public Sprite[]? m_AnimatedSprites;

        /// <summary>
        /// See the description of TileAnimationFlags in the Unity scripting reference.
        /// This controls the UpdatePhysics flags
        /// </summary>
        [Tooltip("Update Physics Shape for TilemapCollider2D when animation switches to each new Sprite")]
        [TptShowField()]
        public bool m_UpdatePhysics;

        #endregion

        #region publicProperties

        /// <inheritdoc />
        public bool AnimationSupported => true;

        /// <summary>
        /// Is the animation waiting to rewind?
        /// </summary>
        // ReSharper disable once ConvertToAutoProperty
        [TptShowAsLabelSelectionInspector(true,true,"",SpaceMode.None,ShowMode.InPlay)]
        public override bool IsOneShotWaitingToRewind => isWaitingForRewind;

        
        #endregion
        
        #region privateFields
        /// <summary>
        /// Used to control animation on and pause states 
        /// </summary>
        private bool animationShouldActivate;
        
        /// <summary>
        /// Is this the first time StartUp has been run on this tile instance?
        /// </summary>
        private bool firstStartUp;

        /// <summary>
        /// Backing field for IsOneShotWaitingToRewind property. Do not want a public 'set' for this.
        /// </summary>
        private bool isWaitingForRewind;

        
        #endregion
        
        
        #region privateProperties
        
        private bool AnimationIsLoopOnce => m_ParentTilemap != null &&
                                         (m_ParentTilemap.GetTileAnimationFlags(m_TileGridPosition) & TileAnimationFlags.LoopOnce)
                                         == TileAnimationFlags.LoopOnce;

        #endregion
        
        #region constants
      
        /// <summary>
        /// const error msg
        /// </summary>
        private const string InvalidAnimStart       = "Error starting Animation: Parent tilemap = null or invalid tile grid position";

        #endregion

        #region animControl

        /// <summary>
        /// Pause a running animation.
        /// </summary>
        /// <param name="pause"></param>
        public override void PauseAnimation(bool pause)
        {
            if(m_ParentTilemap == null || m_TileGridPosition == ImpossibleGridPosition)
                return;
            if(pause)
                m_ParentTilemap.AddTileAnimationFlags(m_TileGridPosition, TileAnimationFlags.PauseAnimation);
            else
            {
                if(m_OneShot)
                    m_ParentTilemap.AddTileAnimationFlags(m_TileGridPosition, TileAnimationFlags.LoopOnce);
                m_ParentTilemap.RemoveTileAnimationFlags(m_TileGridPosition, TileAnimationFlags.PauseAnimation);
            }
        }

        /// <summary>
        /// Use this to turn animation on/off.
        /// Note that restarting a running animation isn't automatic.
        /// </summary>
        /// <param name="turnOn">On or Off</param>
        /// <param name = "startingFrame" >sets the current frame to 0 (for either operation). Set to -1 for this param to be ignored</param>
        /// <param name = "ignoreRewindingState" >if false(default) then this method does not execute when waiting for a rewind - only when one-shot is used w/rewindAfterOneShot set true </param>
        /// <remarks>When turnOn==true, the startingFrame is set prior to starting animation. When false,
        /// the startingFrame is set after stopping the animation. </remarks>
        // ReSharper disable once MemberCanBeProtected.Global
        public override void ActivateAnimation(bool turnOn, int startingFrame = 0, bool ignoreRewindingState = false)
        {
            if (m_ParentTilemap == null || TileGridPosition == ImpossibleGridPosition)
            {
                Debug.LogError(InvalidAnimStart);
                return;
            }

            if (!ignoreRewindingState && IsOneShotWaitingToRewind)
                return;
            
            #if UNITY_EDITOR
            if (!IsPlayMode || (!IsClone && IsTilemapFromPalette(m_ParentTilemap!)))
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
                        if (m_OneShot && m_RewindAfterOneShot)
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
                    if (m_ParentTilemap != null)
                        m_ParentTilemap.SetAnimationFrame(m_TileGridPosition, 0);
                }
                
                //animation is inactive so activate it. 
                animationShouldActivate = true;
                RefreshLater("TpAnimTile:ActivationRefresh", (m_OneShot && m_RewindAfterOneShot ? SetTimeout : null));
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
            //note timeout is in seconds but DelayedCallback requires milliseconds.
            var timeout = TileUtil.GetOneShotTimeOut(m_ParentTilemap!.GetAnimationFrameCount(m_TileGridPosition),
                                                     m_AnimationSpeed,
                                                     m_ParentTilemap);
            isWaitingForRewind = true;
            //note that the 'forceTaskDelay' param is true in this method call. Forces use of Task.Delay.
            TpLib.DelayedCallback(this, Rewind, "TPanimTile:rewind",(int)(timeout * 1000),false,true); 

            void Rewind()
            {
                isWaitingForRewind = false;
                if (m_ParentTilemap == null)
                    return;
                m_ParentTilemap.SetAnimationFrame(m_TileGridPosition, 0);
                m_ParentTilemap.AddTileAnimationFlags(m_TileGridPosition, TileAnimationFlags.PauseAnimation);
            }
        }
        
        #endregion
        
        #region overrides
        /// <inheritdoc />
        public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject go)
        {
            if(!base.StartUp(position, tilemap, go))
                return false;
            #if UNITY_EDITOR
            if (!IsPlayMode)
                return true;
            #endif
            
            
            if (!m_PlayOnStart || firstStartUp)
                return true;
            firstStartUp = true;
            ActivateAnimation(true);
            
            return true;
        }


        /// <summary>
        /// get data for this tile.
        /// Override of TilePlusBase and its superclasses
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="tilemap">The tilemap.</param>
        /// <param name="tileData">The tile data.</param>
        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            base.GetTileData(position, tilemap, ref tileData);
            #if UNITY_EDITOR
            //this is important to prevent visual oddities in palette, which is itself a tilemap.
            //note that often GetTileData is called w/o a previous call to StartUp. 
            if(!IsClone && TpLib.IsTilemapFromPalette(tilemap))
                return;
            #endif
            
            if (SpriteShouldBeCleared)
                return;
            #if UNITY_EDITOR
            tileData.sprite    = IsPlayMode  ? sprite : ActiveSprite;
            #else
            tileData.sprite    = sprite;
            #endif
        }

        /// <summary>
        /// Get animation info for this tile.
        /// Override of TileBase
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="tilemap">The tilemap.</param>
        /// <param name="tileAnimationData">The tile animation data.</param>
        /// <returns>true if animation should be performed</returns>
        public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
        {
            //note no need to call base since all it does is return false;
            if (!animationShouldActivate || m_AnimatedSprites == null)
                return false;
            
            #if UNITY_EDITOR
            //this is important to prevent animation in the palette window.
            if(isSimulating || (!IsClone && TpLib.IsTilemapFromPalette(tilemap)) )
                return false;
            #endif

            if (SpriteShouldBeCleared || m_AnimatedSprites.Length <= 0 )
                return false;
            
            tileAnimationData.animatedSprites    = m_AnimatedSprites;
            tileAnimationData.animationSpeed     = m_AnimationSpeed >= 0 ? m_AnimationSpeed : 1;
            tileAnimationData.animationStartTime = 0;
            tileAnimationData.flags              = GetTileAnimationFlags();
           
            return true;
        }


        private TileAnimationFlags GetTileAnimationFlags()
        {
            // ReSharper disable once ReplaceWithSingleAssignment.True
            var pause = true;
            
            #if UNITY_EDITOR
            if( (m_PlayOnStart || animationShouldActivate)  && Application.isPlaying)
            #else
            if(m_PlayOnStart || animationShouldActivate)  
            #endif
                pause                      = false;

            var tFlags = TileAnimationFlags.None;
            if (pause)
                tFlags      |= TileAnimationFlags.PauseAnimation;

            if(m_UpdatePhysics)
                tFlags |= TileAnimationFlags.UpdatePhysics;
            if (m_OneShot) 
                tFlags |= TileAnimationFlags.LoopOnce;

            return tFlags;
        }
        
        #endregion
        
        
        

        #region editor

#if UNITY_EDITOR


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

        //some methods to allow animation stop/start during play
        /// <summary>
        /// Start animation
        /// </summary>
        [TptShowMethodAsButton("Start Animation",SpaceMode.None,ShowMode.Property,"ShowRunAnimButton")]
        public void StartAnimation()
        {
            ActivateAnimation(true,0,true);
        }

        /// <summary>
        /// Stop animation
        /// </summary>
        [TptShowMethodAsButton("Stop Animation",SpaceMode.None,ShowMode.Property,"ShowStopAnimButton")]
        public void StopAnimation()
        {
            ActivateAnimation(false,0,true);
        }



        //simulation example
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

        /// <summary>
        /// get the next sprite when simulating
        /// </summary>
        /// <value>The active sprite.</value>
        protected virtual Sprite ActiveSprite => isSimulating && m_AnimatedSprites != null ?
                                                     m_AnimatedSprites[NextSpriteIndex]: 
                                                     sprite;

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
        [TptShowField(128,1,SpaceMode.None,ShowMode.NotInPlay)]
        public int m_SimulationSpeed = 2;

        /// <summary>
        /// Used to adjust how long simulation runs
        /// </summary>
        [Tooltip("Simulation timeout in Editor update ticks")]
        [TptShowField(100,10000,SpaceMode.None,ShowMode.NotInPlay)]
        public int m_SimulationTimeout = 1000;


        //get the next sprite index while simulating
        /// <summary>
        /// Next sprite index during simulation
        /// </summary>
        /// <value>The index of the next sprite.</value>
        protected virtual int NextSpriteIndex
        {
            get
            {
                if (spriteIndexForSim > totalNumSprites - 1)
                    spriteIndexForSim = 0;

                return spriteIndexForSim;
            }
        }

        //simulation state variables. No need for serializing.
        /// <summary>
        /// simulating in progress if true
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
        /// The # of ticks to skip
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
            isSimulating    = false; //this isn't a mistake
            if(ParentTilemap == null || m_AnimatedSprites == null)
                return;
            if (start)
            {
                totalNumSprites = m_AnimatedSprites.Length;
                if (totalNumSprites < 2)
                    return;
                
                ticksToSkip = SimulationSkipTicks;
                simTimeout  = SimulationTimeout > 60 ? SimulationTimeout : 60;
                skipCounter = 0;
                
                //Debug.Log($"STARTING simulation for {simTimeout} ticks, skip {ticksToSkip})");

                spriteIndexForSim   =  0;
                isSimulating        =  true;
                TpLib.TimerHookForSimulation += Simulator;
            }
            else
            {
                //Debug.Log("ENDING simulation");
                TpLib.TimerHookForSimulation -= Simulator;
                ParentTilemap.RefreshTile(TileGridPosition);
            }
        }

        //performs the simulation

        /// <summary>
        /// The simulator for this tile
        /// </summary>
        /// <param name="selectionChanged">true if the selection changed: means 'quit'</param>
        protected virtual void Simulator(bool selectionChanged)
        {
            if (!isSimulating || m_ParentTilemap == null)
                return;

            if (selectionChanged)
            {
                Simulate(false);
                return;
            }

            if (m_OneShot && (spriteIndexForSim + 1) > (totalNumSprites - 1) )
                Simulate(false);
            
            if (--simTimeout > 0)
            {
                if (--skipCounter > 0)
                    return;
                skipCounter = ticksToSkip;
                spriteIndexForSim++;
                
                m_ParentTilemap.RefreshTile(m_TileGridPosition);
            }

            else
                Simulate(false);
        }




        /// <summary>
        /// this is something that's on a per-tile basis. IE a description of the tile type.
        /// In the Palette's brush inspector this could be truncated.
        /// Override this in subclass to provide something else.
        /// </summary>
        /// <value>The description.</value>
        // ReSharper disable once AnnotateNotNullTypeMember
        public override string Description => "Single animation animated tile";

        /// <summary>
        /// Alternate icon to show in Brush inspector
        /// </summary>
        [Tooltip("If an icon is in this field then it will be used instead of one that's generated by the brush inspector.")]
        public Texture2D? m_PreviewIcon;

        /// <summary>
        /// get a preview icon for this tile. If you don't need to
        /// provide one, just return null. not available at runtime.
        /// Note only works with Tile+Brush.
        /// </summary>
        /// <value>The preview icon.</value>
        public override Texture2D? PreviewIcon => m_PreviewIcon;

        
        
        #endif
        
        #endregion
        
    }


}
