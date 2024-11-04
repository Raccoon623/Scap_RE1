// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 08-03-2021
// ***********************************************************************
// <copyright file="TpSlideShowSpriteSet.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
#nullable enable

namespace TilePlus
{
    /// <summary>
    /// Specification for a slideshow
    /// </summary>
    [Serializable]
     public class TpSlidesClip
     {
        /// <summary>
        /// The name of the slide show
        /// </summary>
        [Tooltip("Name of this slide show")]
         public string m_Name;

        /// <summary>
        /// If true, slide show wraps-around at last sprite or zeroth
        /// sprite, depending on whether SlideIndex is being
        /// incremented or decremented.
        /// </summary>
        [Tooltip("True to wrap around, false to halt.")]
         public bool m_WrapAround;

        /// <summary>
        /// The slide to show first. Does not change wraparound behavior
        /// </summary>
        [Tooltip("Which slide to show first. Doesn't affect wraparound.")]
         public int m_StartIndex;

        /// <summary>
        /// The List of Sprites for the slide show.
        /// </summary>
        [Tooltip("The sprites to show as slides, in order of appearance.")]
         #if ODIN_INSPECTOR
         [InlineEditor(InlineEditorModes.FullEditor)]
         #endif
         public Sprite[] m_Sprites = new Sprite[1];

        /// <summary>
        /// Initializes a new instance of the <see cref="TpSlidesClip"/> class.
        /// </summary>
        /// <param name="newName">The new name for this slide clip.</param>
        
        public TpSlidesClip(string newName)
         {
             m_Name = newName;
         }
     }

    /// <summary>
    /// An asset with a set of slide show clips
    /// </summary>
    [CreateAssetMenu(fileName = "TpSlideShowSpriteSet.asset", menuName = "TilePlus/Create SlideShowSpriteSet", order = 10000)]
    public class TpSlideShowSpriteSet : ScriptableObject
    {

#if USE_ODIN
        [ListDrawerSettings(CustomAddFunction = "NextSlideSuffix")]
#endif
        /// <summary>
        /// List of slide sprites
        /// </summary>
        [Tooltip("List of sprites")]
        public List<TpSlidesClip> m_Slides = new();

        /// <summary>
        /// property to get number of slide show clips
        /// </summary>
        /// <value>The number clips.</value>
        public int NumClips => this.m_Slides.Count;

        // ReSharper disable once UnusedMember.Local
        /// <summary>
        /// Next slide suffix.
        /// </summary>
        /// <returns>TpSlidesClip instance</returns>
        private TpSlidesClip NextSlideSuffix()
        {
            var num = NumClips;
            return new TpSlidesClip(num == 0 ? "default" : $"default{num.ToString()}");
             
        }

        /// <summary>
        /// Gets the asset version.
        /// </summary>
        /// <value>The asset version.</value>
        #if ODIN_INSPECTOR
        [ShowInInspector]
        #endif
        public byte AssetVersion => 1;



        /// <summary>
        /// Property to get an array of clip names
        /// </summary>
        /// <value>The slide clip names.</value>
        public string[] SlideClipNames
        {
            get
            {
                if (NumClips == 0)
                    return Array.Empty<string>();
                var clipNames = new string[NumClips];
                for (var i = 0; i < NumClips; i++)
                    clipNames[i] = m_Slides[i].m_Name;

                return clipNames;

            }
        }
        
        
        /// <summary>
        /// Unity event method. Tests for duplicate clip names
        /// </summary>
        public void OnEnable()
        {

            var clipNames = new HashSet<string>(NumClips);
            for (var i = 0; i < NumClips; i++)
            {
                if (clipNames.Add(m_Slides[i].m_Name))
                    continue;
                Debug.LogError($"Duplicate clip name '{m_Slides[i].m_Name}' in TpSlideShowSpriteSet asset '{name}'. Other exceptions may occur as a result!!");
                break;

            }
        }
    }
    
    
    
}
