// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 05-06-2021
// ***********************************************************************
// <copyright file="TpTileFab.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TilePlus
{

    /// <summary>
    /// Class used when bundling tilemaps/grids
    /// </summary>
    /// <remarks>Version 2 adds GUIDs</remarks>
    [Serializable]
    public class TpTileFab : ScriptableObject
    {
        /// <summary>
        /// class used to describe a single tile archive and where it should be loaded.
        /// </summary>
        [Serializable]
        public class AssetSpec
        {
            /// <summary>
            /// A TpTileBundle asset
            /// </summary>
            [SerializeField]
            public TpTileBundle m_Asset = null!;
            /// <summary>
            /// The name of its originating tilemap
            /// </summary>
            [SerializeField]
            public string m_TilemapName = null!;
            /// <summary>
            /// the tag of its originating tilemap
            /// </summary>
            [SerializeField]
            public string m_TilemapTag = null!;
        }

        /// <summary>
        /// A list of TpTileBundle assets, tilemap names and tags
        /// </summary>
        [SerializeField]
        public List<AssetSpec>? m_TileAssets =new();
        /// <summary>
        /// The scene path for this asset
        /// </summary>
        [SerializeField]
        public string? m_ScenePath;
        /// <summary>
        /// The timestamp for this asset
        /// </summary>
        [SerializeField]
        public string? m_TimeStamp;
        
        
        /// <summary>
        /// The original scene this was created from.
        /// </summary>
        public string? m_OriginalScene;
        
        /// <summary>
        /// set true if this TileFab was created from a GridSelection
        /// rather than an entire tilemap.
        /// </summary>
        [Tooltip("Indicates that this is from a Grid Selection. DO NOT change it yourself")]
        public bool m_FromGridSelection;

        /// <summary>
        /// Set true if Painter should not show this in its Palettes list
        /// </summary>
        [Tooltip("If checked this won't appear in Tile+Painter lists.")] 
        public bool m_IgnoreInPainter;

        /// <summary>
        /// Arbitrary boolean for use when filtering layouts
        /// </summary>
        [Tooltip("Use for any arbitrary purpose in your filter")]
        public bool m_UserFlag;

        /// <summary>
        /// Arbitrary string for use when filtering layouts
        /// </summary>
        [Tooltip("Use for any arbitrary purpose in your filter")]
        public string m_UserString = string.Empty;
        
        
        /// <summary>
        /// Added in V3. A representative sprite. Painter will display this.
        /// </summary>
        [Tooltip("A sprite to represent this TileFab. Painter will display it instead of the TileFab icon.")]
        public Sprite? m_Icon;
        
        /// <summary>
        /// Gets the asset version.
        /// </summary>
        /// <value>The asset version.</value>
        #if ODIN_INSPECTOR
        [ShowInInspector]
        #endif
        public byte AssetVersion => 3;

        
        /// <summary>
        /// GUID for this Tilefab. 
        /// </summary>
        [HideInInspector]
        [SerializeField]
        private byte[]? m_Guid = new byte[1];
        
        /// <summary>
        /// Get the asset's GUID as a GUID struct.
        /// </summary>
        /// <value>The unique identifier.</value>
        public Guid TileFabGuid => new Guid(m_Guid!);

        /// <summary>
        /// Add a GUID to this asset. Only works once.
        /// </summary>
        /// <returns>true if the GUID was added, false if there already was a GUID</returns>
        public bool AddGuid()
        {
            if (m_Guid is { Length: 16 })
                return false;
            m_Guid = Guid.NewGuid().ToByteArray();
            return true;

        }

        /// <summary>
        /// Get the largest bounds
        /// </summary>
        public BoundsInt LargestBounds
        {
            get
            {
                if (m_TileAssets == null)
                    return new BoundsInt();
                
                return m_FromGridSelection
                           ? m_TileAssets[0].m_Asset.m_TilemapBoundsInt
                           : TileUtil.LargestBoundsInt(m_TileAssets.Select(spec => spec.m_Asset.m_TilemapBoundsInt));
            }
        }

       
        /// <summary>
        /// Get a string representation of the GUID. 
        /// </summary>
        #if ODIN_INSPECTOR
        [ShowInInspector]
        #endif
        public string AssetGuidString
        {
            get
            {
                return m_Guid is not { Length: 16 }
                           ? string.Empty
                           : new Guid(m_Guid).ToString();
            }
        }
        
        #if UNITY_EDITOR
        /// <summary>
        /// Reset the GUID. USER CODE SHOULD NEVER DO THIS!
        /// </summary>
        public void ResetGuid()
        {
            m_Guid = null;
        }
       #endif
       
    }

    /// <summary>
    /// Shows GUID
    /// </summary>
    #if UNITY_EDITOR
    [CustomEditor(typeof(TpTileFab))]
    public class TpTileFabEditor : UnityEditor.Editor
    {
        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var instance = target as TpTileFab;
            EditorGUILayout.LabelField($"Guid: {instance!.AssetGuidString}");
        }
    }
    
    #endif
    
    
    
   
}
