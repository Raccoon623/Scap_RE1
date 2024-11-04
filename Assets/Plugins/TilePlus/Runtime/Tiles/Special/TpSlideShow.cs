// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 06-08-2021
// ***********************************************************************
// <copyright file="TpSlideShow.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
#nullable enable
namespace TilePlus
{
    /// <summary>
    /// Show a sprite at a time. Not animation.
    /// </summary>
    [CreateAssetMenu(fileName = "TpSlideShow.asset", menuName = "TilePlus/Create TpSlideShow", order = 1000)]
    public class TpSlideShow : TilePlusBase, ITilePlus, ITpMessaging<EmptyPacket,BoolPacket>
    {
        #region enum
        /// <summary>
        /// Wrapping override control
        /// </summary>
        public enum WrapOverride
        {
            /// <summary>
            /// No override
            /// </summary>
            None,
            /// <summary>
            /// Force slide index wrapping
            /// </summary>
            ForceWrap,
            /// <summary>
            /// Force slide index limiting
            /// </summary>
            ForceHalt

        }

        #endregion


        #region privateFieldsProperties

        /// <summary>
        /// The current clip index
        /// </summary>
        //[SerializeField] //added v1.3 With this set at -1, ChooseSlideShow runs on startup AND getTiledata which sets this to zero. 
        private int    currentClipIndex = -1;
        /// <summary>
        /// The current clip name
        /// </summary>
        private string? currentClipName;
        /// <summary>
        /// The current clip's number of slides
        /// </summary>
        private int    currentClipNumSlides = -1;
        /// <summary>
        /// The slide index
        /// </summary>
        private int    slideIndex;
        /// <summary>
        /// The current clip wrap around mode
        /// </summary>
        private bool   currentClipWrapAroundMode;
       

        #endregion

        #region publicFields
        /// <summary>
        /// Slide clip asset
        /// </summary>
        [TptShowObjectField(typeof(TpSlideShowSpriteSet),false,true,SpaceMode.None,ShowMode.NotInPlay,"",true)]
        [Tooltip("A SlideShowClipSet asset")]
        public TpSlideShowSpriteSet? m_SlidesClipSet;

        /// <summary>
        /// Default slideshow sequence
        /// </summary>
        [Tooltip("The slideshow sequence name to be used at startup of this tile")][Delayed]
        [TptShowField]
        public string m_SlideshowAtStart = "default";

        /// <summary>
        /// Default start index.
        /// </summary>
        [Tooltip("The slide index to use when the game starts")]
        [TptShowField()] 
        public int m_SlideIndexAtStart;
        
        
        
        /// <summary>
        /// Allow overriding wrap/limit setting from slideshow-sprites asset
        /// </summary>
        [TptShowEnum(SpaceMode.None,ShowMode.NotInPlay)][Tooltip("Override Wrap/Limit behaviour from SlideShow sprites asset. ")]
        public WrapOverride m_WrappingOverride = WrapOverride.None;

        #endregion

        #region publicProperties
        
        /// <summary>
        /// Property to get the currently-selected clip's number of slides.
        /// Returns -1 when unitialized.
        /// </summary>
        /// <value>The current clip number of slides.</value>
        [TptShowAsLabelSelectionInspector(true,true,"Number of slides in current clip")]
        public int CurrentClipNumberOfSlides => currentClipNumSlides;


        /// <summary>
        /// Property to get current slide show name from clip asset
        /// </summary>
        /// <value>The name of the slide show.</value>
        [TptShowAsLabelSelectionInspector(true,true,"Current slide show name")] 
        public string SlideShowName => m_SlidesClipSet != null && currentClipName != null ? currentClipName : "Missing SlideShow asset!";

        /// <summary>
        /// Property to get number of slide shows in asset
        /// </summary>
        /// <value>The number of slide shows.</value>
        [TptShowAsLabelSelectionInspector(true,true,"# of slide shows")]
        [TptShowAsLabelBrushInspector(true,true,"# of slide shows for this asset.")]
        public int NumberOfSlideShows => m_SlidesClipSet == null ? 0 : m_SlidesClipSet.NumClips;

        /// <summary>
        /// Convenience property for slide index - avoids accessing SlideIndex, below.
        /// </summary>
        /// <value>The index of the current slide.</value>
        [TptShowAsLabelSelectionInspector(true,true,"Current Slide Index")]
        // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
        public int CurrentSlideIndex => slideIndex;


        /// <summary>
        /// The index of the current sprite. Normally, change
        /// slides with SetSlide, not this.
        /// But if you want to, please ensure that you check the
        /// CurrentSlideIndex if you want to avoid wrap-around or
        /// nothing happening because you're at the zeroth or last slide.
        /// Note that you CAN use SlideIndex++ or SlideIndex-- but
        /// be aware of wrapping behaviour.
        /// </summary>
        /// <value>The index of the slide.</value>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        // ReSharper disable once MemberCanBePrivate.Global
        public int SlideIndex
        {
            get => slideIndex < 0 ? 0 : slideIndex;
            set
            {
                if (m_SlidesClipSet == null)
                {
                    slideIndex = 0;
                    return;
                }

                var wrapMode = currentClipWrapAroundMode;  //true to wrap, false to limit
                switch (m_WrappingOverride)
                {
                    case WrapOverride.None:
                        break;
                    case WrapOverride.ForceWrap:
                        wrapMode = true;
                        break;
                    case WrapOverride.ForceHalt:
                        wrapMode = false;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                //wrap or limit index if necc.
                if (value >= currentClipNumSlides)
                    slideIndex = wrapMode ? 0 : currentClipNumSlides - 1;
                else if (value < 0)
                    slideIndex = wrapMode ? currentClipNumSlides - 1 : 0;
                else
                    slideIndex = value;
            }
        }

        /// <summary>
        /// True if there's a clipset with &gt; 0 clips
        /// </summary>
        /// <value><c>true</c> if [valid slide show]; otherwise, <c>false</c>.</value>
        // ReSharper disable once MemberCanBePrivate.Global
        public bool ValidSlideShow => m_SlidesClipSet != null && m_SlidesClipSet.NumClips != 0;


        #endregion


        #region code

        /// <summary>
        /// Choose current slide show by name
        /// </summary>
        /// <param name="selectedName">the name to use</param>
        /// <param name="forceRefresh">force a refresh: default=true.</param>
        /// <param name = "ignoreNullSelectedName" ></param>
        /// <returns>true for success</returns>
        /// <remarks>Uses SetAnimationFrame </remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        public bool ChooseSlideShow(string selectedName, bool forceRefresh = true, bool ignoreNullSelectedName = false) 
        {
            #if UNITY_EDITOR
            if(IsAsset)
                return false;
            #endif
            
            if (m_SlidesClipSet == null)
            {
                Debug.LogError("Missing slide show clip asset");
                return false;
            }

            if (m_SlidesClipSet.NumClips == 0)
            {
                Debug.LogError("Empty slide show clip asset");
                return false;
            }

            var clipNames             = m_SlidesClipSet.SlideClipNames;
            int currentClipStartIndex;
            if (string.IsNullOrWhiteSpace(selectedName) || !clipNames.Contains(selectedName))
            {
                if(!ignoreNullSelectedName && selectedName !="default")  //todo languages?
                    Debug.LogWarning($"Invalid SlideShow clipset name '{selectedName}' on tile at {m_TileGridPosition.ToString()} on map {m_ParentTilemap}. Using first clipset. Not a big deal. "); 
                var set = m_SlidesClipSet.m_Slides[0];
                currentClipIndex          = 0;
                currentClipName           = set.m_Name;
                m_SlideshowAtStart        = currentClipName;
                m_SlideIndexAtStart       = 0;
                currentClipNumSlides      = set.m_Sprites.Length;
                currentClipWrapAroundMode = set.m_WrapAround;
                currentClipStartIndex     = set.m_StartIndex;
                if (currentClipStartIndex < 0 || currentClipStartIndex >= currentClipNumSlides)
                    currentClipStartIndex = 0;

                SlideIndex = currentClipStartIndex;
                if (forceRefresh && m_ParentTilemap != null)
                    m_ParentTilemap.SetAnimationFrame(m_TileGridPosition, SlideIndex);
                return true;
            }

            for(var index = 0; index < m_SlidesClipSet.NumClips; index++)
            {
                var set = m_SlidesClipSet.m_Slides[index];
                if (set.m_Name != selectedName)
                    continue;
                currentClipIndex          = index;
                currentClipName           = set.m_Name;
                m_SlideshowAtStart        = currentClipName;
                currentClipNumSlides      = set.m_Sprites.Length;
                currentClipWrapAroundMode = set.m_WrapAround;
                currentClipStartIndex     = set.m_StartIndex;
                if (currentClipStartIndex < 0 || currentClipStartIndex >= currentClipNumSlides)
                    currentClipStartIndex = 0;

                SlideIndex = currentClipStartIndex > 0
                                 ? currentClipStartIndex 
                                 : m_SlideIndexAtStart;
                
                
                if(forceRefresh && m_ParentTilemap != null)
                    m_ParentTilemap.SetAnimationFrame(m_TileGridPosition, SlideIndex);
                return true;
            }

            currentClipIndex     = -1;
            currentClipNumSlides = 0;
            SlideIndex           = 0;
            currentClipName      = "Missing asset! Does 'SlideShowAtStart' have a valid entry?";
            return false;
        }

        
        /// <summary>
        /// StartUp for TpSlideShow
        /// </summary>
        /// <param name="position">Position of the Tile on the Tilemap.</param>
        /// <param name="tilemap">The Tilemap the tile is present on.</param>
        /// <param name="go">The GameObject instantiated for the Tile.</param>
        /// <returns>Whether the call was successful.</returns>
        public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject go)
        {
            if (!base.StartUp(position, tilemap, go))
                return false;
            if (m_SlidesClipSet == null)
                return false;
            
            if (currentClipIndex < 0)
                ChooseSlideShow(m_SlideshowAtStart,false);  //note: no refresh - would be infinite loop

            if (m_SlideIndexAtStart < 0) 
                return true;
            
            if (slideIndex < 0)
                slideIndex = 0;
            
            #if UNITY_EDITOR
            if (!Application.isPlaying)
                return true;
            #endif
            
            TpLib.DelayedCallback(this, ()=>SetSlide(m_SlideIndexAtStart), "TP-SlideShow-Init");
            return true;
        }
        
        
        
        
        //// <inheritdoc />
        void ITpMessaging<EmptyPacket, BoolPacket>.MessageTarget(BoolPacket sentPacket)
        {
            ChangeSlide(sentPacket.m_Bool);
        }

        /*/// <inheritdoc />
        EmptyPacket ITpMessaging<EmptyPacket, BoolPacket>.GetData()
        {
            return null;
        }*/

        /// <summary>
        /// Directly set the slide index
        /// </summary>
        /// <param name="index">Index of the slide to view. </param>
        /// <remarks>Checked for out-of-range before use (if out-of-range, result depends on wrapping setting).</remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once MemberCanBeProtected.Global
        public void SetSlide(int index)
        {
            SlideIndex = index;
            if(m_ParentTilemap != null)
                m_ParentTilemap.SetAnimationFrame(m_TileGridPosition, SlideIndex);
        }
        

        /// <summary>
        /// Use this to change the slide.
        /// </summary>
        /// <param name="forward">true=forward, false=backwards</param>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once MemberCanBeProtected.Global
        public void ChangeSlide(bool forward)
        {
            if(forward)
                SlideIndex++;
            else
                SlideIndex--; 
            if(m_ParentTilemap != null)
                m_ParentTilemap.SetAnimationFrame(m_TileGridPosition,SlideIndex);
        }


        /// <summary>
        /// Advance to the next clip or go back to the previous clip.
        /// No wrap-around.
        /// </summary>
        /// <param name="forward">Next clip if true, previous if false</param>
        public void ChangeClipSet(bool forward)
        {
            if (currentClipIndex < 0)
                currentClipIndex = 0;
            if (m_SlidesClipSet == null)
                return;
            var numClips = m_SlidesClipSet.NumClips;
            if (numClips <= 1)
                return;
            if (forward)
            {
                if (currentClipIndex + 1 >= numClips)
                    return;
                currentClipIndex++;
                ChooseSlideShow(m_SlidesClipSet.m_Slides[currentClipIndex].m_Name);
                    
            }
            else
            {
                if (currentClipIndex - 1 < 0)
                    return;
                currentClipIndex--;
                ChooseSlideShow(m_SlidesClipSet.m_Slides[currentClipIndex].m_Name);
                    
            }

        }




        /// <summary>
        /// Get data for this tile.
        /// Override of Tile. Note that Tilemap.RefreshTile calls this
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="tilemap">The tilemap.</param>
        /// <param name="tileData">The tile data.</param>
       
        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            //note that base is not called.
            if (m_SlidesClipSet != null && (currentClipIndex < 0 || currentClipIndex >= m_SlidesClipSet.NumClips))
                ChooseSlideShow(m_SlideshowAtStart,false);
            
            
            tileData.flags     = flags;
            tileData.color     = color;
            
            #if UNITY_EDITOR
            if(m_SlidesClipSet == null || currentClipIndex < 0 || (!IsClone && TpLib.IsTilemapFromPalette(tilemap)))
            #else
            if (m_SlidesClipSet == null || currentClipIndex < 0)
            #endif
            {
                tileData.sprite = sprite;
            }
            else
            {
                var slides = m_SlidesClipSet.m_Slides[currentClipIndex];
                if (SlideIndex >= currentClipNumSlides)
                    slideIndex = currentClipNumSlides - 1;
                else if (slideIndex < 0)
                    slideIndex = 0;
                tileData.sprite = SpriteShouldBeCleared
                                      ? null
                                      : slides.m_Sprites[slideIndex];
            }
            

            tileData.transform = transform;
            SetColliderType(ref tileData);
        }


        
        /// <inheritdoc />
        public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
        {
            if (m_SlidesClipSet == null)
                return false;

            #if UNITY_EDITOR
            //this is important to prevent animation in the palette window.
            if (isSimulating || (!IsClone && TpLib.IsTilemapFromPalette(tilemap)))
                return false;
            #endif
            
            
            var index = currentClipIndex <= 0 ? 0 : currentClipIndex;
            var numClips = m_SlidesClipSet.NumClips;
            if (index >= numClips)
                index = numClips - 1;
            if (index < 0)
                index = 0;
            tileAnimationData.animationSpeed     = 0;
            tileAnimationData.animationStartTime = 0;    
            tileAnimationData.animatedSprites    = m_SlidesClipSet.m_Slides[index].m_Sprites; 
            

            return true;
        }
        

        /// <inheritdoc />
        public override bool UpdateInstance(string [] value) 
        {
            var status = base.UpdateInstance(value);
            if (value.Contains("m_SlidesClipSet"))
                ChooseSlideShow("",true,true); //reinitializes
            return status;
        }

        #endregion

#if UNITY_EDITOR
        #region Editor

        /// <summary>
        /// Copy the result of ChangeSlideUp/Down to the default tile index (for convenience)
        /// </summary>
        [TptShowField(0,0,SpaceMode.None,ShowMode.NotInPlay)][Tooltip("Copies the current slide index to SlideIndexAtStart.")]
        public bool m_CopyToSlideIndex = true;  

        /// <inheritdoc />
        /// <remarks>Here we try to return the first sprite from the slideshow which would actually be shown.</remarks>
        public override Sprite? EffectiveSprite 
        {
            get
            {
                if (m_SlidesClipSet == null || m_SlidesClipSet.NumClips == 0)
                    return sprite;
                var clipIndex = currentClipIndex < 0
                                    ? 0
                                    : currentClipIndex;
                if (clipIndex >= m_SlidesClipSet.NumClips)
                    return sprite;
                
                var slidesClip  = m_SlidesClipSet.m_Slides[clipIndex];
                var nSlides = slidesClip.m_Sprites.Length;
                var index   = IsAsset || IsLocked ? m_SlideIndexAtStart : slideIndex;
                if (index >= nSlides)
                    index = nSlides - 1;
                else if (index < 0)
                    index = 0;
                return slidesClip.m_Sprites[index];
            }
        }


        /// <summary>
        /// visibility property for ChangeSlideUp/Down method buttons
        /// </summary>
        /// <value><c>true</c> if [show method buttons]; otherwise, <c>false</c>.</value>
        public bool ShowMethodButtons => ValidSlideShow && !Application.isPlaying;

        /// <summary>
        /// Method button
        /// </summary>
        [TptShowMethodAsButton("Next slide",SpaceMode.None,ShowMode.Property,"ShowMethodButtons")]
        public void ChangeSlideUp()
        {
            ChangeSlide(true);
            if (m_CopyToSlideIndex)
                m_SlideIndexAtStart = slideIndex;
        }

        /// <summary>
        /// Method button
        /// </summary>
        [TptShowMethodAsButton("Previous slide",SpaceMode.None,ShowMode.Property,"ShowMethodButtons")] 
        public void ChangeSlideDown()
        {
            ChangeSlide(false);
            if (m_CopyToSlideIndex)
                m_SlideIndexAtStart = slideIndex;

        }

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
        /// the remainder of this is for simulation.
        public int SimulationTimeout => m_SimulationTimeout;

        /// <summary>
        /// Used to adjust simulation speed.
        /// </summary>
        [Tooltip("Adjust simulation speed in editor. Higher #s slow down simulation.")] 
        [TptShowField(128,1, SpaceMode.SpaceBefore,ShowMode.NotInPlay)]
        public int m_SimulationSpeed = 2;

        /// <summary>
        /// How long the simulation lasts, in editor ticks
        /// </summary>
        [Tooltip("Simulation timeout in Editor update ticks")] 
        [TptShowField(100,10000,SpaceMode.None,ShowMode.NotInPlay)]
        public int m_SimulationTimeout = 1000;


        /// <summary>
        /// Returns true if this Tile can perform a simulation
        /// </summary>
        /// <value><c>true</c> if this instance can simulate; otherwise, <c>false</c>.</value>
        public bool CanSimulate  => true;         //implementation says yes we can simulate 
        /// <summary>
        /// Returns true when this tile is actively simulating
        /// </summary>
        /// <value><c>true</c> if this instance is simulating; otherwise, <c>false</c>.</value>
        public bool IsSimulating => isSimulating; //are we simulating currently?
        //simulation state variables. No need for serializing.
        /// <summary>
        /// The simulation is running
        /// </summary>
        private bool isSimulating;
        /// <summary>
        /// The simulation timeout
        /// </summary>
        private int  simTimeout;
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
            if (start)
            {
                if (currentClipNumSlides < 2)
                    return;

                ticksToSkip = SimulationSkipTicks;
                simTimeout  = SimulationTimeout > 60 ? SimulationTimeout : 60;
                skipCounter = 0;
                
                Debug.Log($"STARTING simulation for {simTimeout} ticks, skip {ticksToSkip})");
                SlideIndex         =  0;
                isSimulating        =  true;
                TpLib.TimerHookForSimulation += Simulator;
            }
            else
            {
                Debug.Log("ENDING simulation");
                TpLib.TimerHookForSimulation -= Simulator;
                SetSlide(0);
            }
        }

        /// <summary>
        /// Simulation for this tile
        /// </summary>
        /// <param name="selectionChanged">Sim stops if selection changed</param>
        private void Simulator(bool selectionChanged)
        {
            if(!isSimulating)
                return;
            if (selectionChanged)
            {
                Simulate(false);
                return;
            }
            
            if (--simTimeout > 0)
            {
                if (--skipCounter > 0)
                    return;
                skipCounter = ticksToSkip;
                ChangeSlide(true);
            }
            else
                Simulate(false);
        }


        /// <summary>
        /// The previous   slideshowspriteset 
        /// </summary>
        private TpSlideShowSpriteSet? lastSpriteSet;
        
        /// <summary>
        /// A custom GUI for changing slide sets.
        /// </summary>
        /// <param name="skin">The skin.</param>
        /// <param name="buttonSize">Size of the button.</param>
        /// <param name = "noEdit" >No editing: tile in a prefab</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        [TptShowCustomGUI()]
        public CustomGuiReturn SlideShowTileGui(GUISkin skin, Vector2 buttonSize, bool noEdit)
        {
            if (Application.isPlaying)
                return NoActionRequiredCustomGuiReturn;
            
            if (m_SlidesClipSet == null)
            {
                EditorGUILayout.HelpBox("Missing a Slides Set!",MessageType.Warning,true);
                return NoActionRequiredCustomGuiReturn;
            }

            var numClips = m_SlidesClipSet.NumClips;
            
            
            if (numClips == 0)
            {
                EditorGUILayout.HelpBox("Slides Set has no sprites!",MessageType.Warning,true);
                return NoActionRequiredCustomGuiReturn;
            }
            
            //in case the slide show had changed. 
            if (lastSpriteSet != null && m_SlidesClipSet != lastSpriteSet)
            {
                lastSpriteSet = m_SlidesClipSet;

                if (numClips == 1)
                {
                    ChooseSlideShow(m_SlidesClipSet.SlideClipNames[0],false);
                    return NoActionRequiredCustomGuiReturn;
                }
            }

            if (numClips < 2)  //only one clip, don't show the dropdown intpopup box
                return NoActionRequiredCustomGuiReturn;
            
            if (noEdit)
            {
                EditorGUILayout.HelpBox($"Selected slide show: {currentClipName}", MessageType.None, true);
                return NoActionRequiredCustomGuiReturn;
            }

            var names           = m_SlidesClipSet.SlideClipNames;
            var numNames        = names.Length;
            var namesGUIContent = new GUIContent[numNames];
            var optionValues    = new int[numNames];
            for (var i = 0; i < numNames; i++)
            {
                namesGUIContent[i] = new GUIContent( names[i] );
                optionValues[i]    = i;
            }

            var selected = 0;
            if (!string.IsNullOrWhiteSpace(currentClipName))
            {
                selected = names.ToList().FindIndex(s => s == currentClipName);
                if (selected == -1)
                    selected = 0;
            }

            var guiCont = new GUIContent("Clip to use:", "Select the clip to use when animating");
            var result  = EditorGUILayout.IntPopup(guiCont, selected, namesGUIContent, optionValues,GUILayout.ExpandWidth(true));
            if (result < 0 || result == selected)
                return NoActionRequiredCustomGuiReturn;
            var resultName = names[result];
            if (resultName == currentClipName)
                return NoActionRequiredCustomGuiReturn;
            ChooseSlideShow(resultName);
            return new CustomGuiReturn(true);
        }


        /// <summary>
        /// this is something that's on a per-tile basis. IE a description of the tile type.
        /// In the Palette's brush inspector this could be truncated.
        /// Override this in subclass to provide something else.
        /// </summary>
        /// <value>The description.</value>
 
        public override string Description => "Slideshow tile";
        #endregion
        #endif

        
    }
}
