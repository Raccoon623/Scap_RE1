// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 06-07-2021
// ***********************************************************************
// <copyright file="TpSpriteAnimationClipSet.cs" company="Jeff Sasmor">
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
    /// specification for a single animation clip
    /// </summary>
    [Serializable]
     public class TpAniClip
    {
         #if ODIN_INSPECTOR
        /// <summary>
        /// Title of settings area - ODIN only
        /// </summary>
        protected const string SettingsAreaTitle = "Customization";
        #endif

        /// <summary>
        /// The name of the animation sequence
        /// </summary>
        [Tooltip("Name of this animation sequence")][SerializeField]
        public string m_Name;

        /// <summary>
        /// Which sprite to use when animation is off for this animation only.
        /// </summary>
        [Tooltip("Which sprite to use for static tile (when animation is off) if this is the active animation sequence")][SerializeField]
        #if ODIN_INSPECTOR
        [FoldoutGroup(SettingsAreaTitle, false)]
        #endif
        public int m_DefaultTileIndex;

        /// <summary>
        /// Multiplies the Tilemap animation speed.
        /// </summary>
        [Tooltip("Multiplies the Tilemap's animation speed. ")][SerializeField]
        #if ODIN_INSPECTOR
        [FoldoutGroup(SettingsAreaTitle, false)]
        #endif
        public float m_AnimationSpeed;


        /// <summary>
        /// Animation repeat control
        /// </summary>
        [Tooltip("Does animation repeat or just play once?")][SerializeField]
        #if ODIN_INSPECTOR
        [FoldoutGroup(SettingsAreaTitle, false)]
        #endif
        public bool m_OneShot;

        //this is new in asset version 2
        /// <summary>
        /// If one shot animation is used, this flag determines if a rewind occurs.
        /// </summary>
        #if ODIN_INSPECTOR
        [FoldoutGroup(SettingsAreaTitle, false)]
        #endif
        [Tooltip("If OneShot is checked, should the animation rewind after the one-shot is complete?")]
        [SerializeField]
        public bool m_RewindAfterOneShot;
        
        
        
        /// <summary>
        /// Unused
        /// </summary>
        [Tooltip("DEPRECATED: not used in asset version 2 or later")][SerializeField]
        #if ODIN_INSPECTOR
        [FoldoutGroup(SettingsAreaTitle, false)]
        #endif
        public float m_ManualTimeout;

        /// <summary>
        /// The List of Sprites set for the Animated Tile.
        /// This will be played in sequence.
        /// </summary>
        [Tooltip("The sprites to animate, in order of appearance.")]
         #if ODIN_INSPECTOR
        [InlineEditor(InlineEditorModes.FullEditor)]
         #endif
        public Sprite[] m_Sprites = new Sprite[1];

        /// <summary>
        /// Initializes a new instance of the <see cref="TpAniClip"/> class.
        /// </summary>
        public TpAniClip()
        {
            m_Name              = "default";
            m_AnimationSpeed    = 1f;
        }
        
        
    }

    /// <summary>
    /// An asset with sprites to animate
    /// </summary>
    [CreateAssetMenu(fileName = "TpSpriteAnimationClipSet.asset", menuName = "TilePlus/Create SpriteAnimationClipSet", order = 10000)]
    public class TpSpriteAnimationClipSet : ScriptableObject
    {
        /// <summary>
        /// List of animation clips
        /// </summary>
        #if ODIN_INSPECTOR
        [ListDrawerSettings(CustomAddFunction = "NextAnimSuffix")]
        #endif
        
        [Tooltip("List of clips")]
        public List<TpAniClip> m_Clips = new List<TpAniClip>();
       

        /// <summary>
        /// property to get number of clips
        /// </summary>
        /// <value>The number clips.</value>
        public int NumClips => this.m_Clips.Count;

        // ReSharper disable once UnusedMember.Local
        /// <summary>
        /// Nexts the anim suffix.
        /// </summary>
        /// <returns>TpAniClip</returns>
       
        private TpAniClip NextAnimSuffix()
        {
            var num  = NumClips;
            var clip = new TpAniClip
            {
                m_Name = num == 0 ? "default" : $"default{num.ToString()}",
            };
            return clip;

        }
        
        
        /// <summary>
        /// Gets the asset version.
        /// </summary>
        /// <value>The asset version.</value>
#if ODIN_INSPECTOR
        [ShowInInspector]
#endif
        public byte AssetVersion => 2;


        /// <summary>
        /// property to get array of clip names
        /// </summary>
        /// <value>The animation clip names.</value>
        public string[] AnimationClipNames
        {
            get
            {
                if (NumClips == 0)
                    return Array.Empty<string>();
                var clipNames = new string[NumClips];
                for (var i = 0; i < NumClips; i++)
                    clipNames[i] = m_Clips[i].m_Name;
                return clipNames;

            }
        }
    }
   
}
