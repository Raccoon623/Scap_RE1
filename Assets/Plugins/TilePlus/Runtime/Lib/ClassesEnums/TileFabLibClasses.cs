// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 03-25-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 04-01-2023
// ***********************************************************************
// <copyright file="TileFabLibClasses.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


namespace TilePlus
{

    /// <summary>
    /// Options for LoadTileFab and LoadBundle
    /// </summary>
    [Flags]
    public enum FabOrBundleLoadFlags
    {
        /// <summary>
        /// No flags used.
        /// </summary>
        None = 0,
        /// <summary>
        /// Load Prefabs. Normally true
        /// </summary>
        LoadPrefabs = 1,
        /// <summary>
        /// Clear Prefabs. Normally false
        /// </summary>
        ClearPrefabs = 2,
        /// <summary>
        /// Clear Tilemap. Normally false
        /// </summary>
        ClearTilemap = 4,
        /// <summary>
        /// Force Refresh. Normally false.
        /// </summary>
        ForceRefresh = 8,
        /// <summary>
        /// New GUIDs for TilePlus tiles. Normally true
        /// </summary>
        NewGuids = 16,
        /// <summary>
        /// Apply filtering only to TilePlus tiles. Normally true
        /// </summary>
        FilterOnlyTilePlusTiles = 32,
        /// <summary>
        /// Do not clone TPT tiles in TpTileBundle.Tileset
        /// </summary>
        NoClone = 64,
        /// <summary>
        /// Mark a Zone Reg as immortal. Note: ONLY valid when using ZoneManager and Layouts.
        /// </summary>
        MarkZoneRegAsImmortal = 128,
        /// <summary>
        /// Most common set of options, with filtering only TPT tiles
        /// </summary>
        NormalWithFilter = LoadPrefabs | NewGuids | FilterOnlyTilePlusTiles,
        /// <summary>
        /// Most common set of options, with filtering for anything (if a filter is provided)
        /// </summary>
        Normal = LoadPrefabs | NewGuids
        
        
        
                           
                           
    }

    /// <summary>
    /// Tells a TileFab/Bundle load filter what data it has been given
    /// </summary>
    public enum FabOrBundleFilterType
    {
        /// <summary>
        /// The prefab
        /// </summary>
        Prefab,
        /// <summary>
        /// The tile plus
        /// </summary>
        TilePlus,
        /// <summary>
        /// The unity
        /// </summary>
        Unity
    }

    /// <summary>
    /// Parameter list items for using
    /// LoadTileFabs(List&lt;TileFabLoadParams&gt; loadParams, ref List&lt;TilefabLoadResults&gt; loadResultsList)
    /// INTENTIONALLY not serializable since contents are not completely serializable.
    /// </summary>
    public class TileFabLoadParams
    {
        /// <summary>
        /// Mark this zone reserved. See ZoneRegistration class.
        /// </summary>
        public bool m_Reserved;
        /// <summary>
        /// Mark this zone as immortal. Zone will never be removed by Layout.
        /// </summary>
        public bool m_Immortal;
        /// <summary>
        /// Parent Tilemap of Tile, if the request is coming from a tile. Otherwise leave null
        /// </summary>
        public readonly Tilemap? m_TileParent;
        /// <summary>
        /// The TileFab to load
        /// </summary>
        public readonly TpTileFab m_TileFab;
        /// <summary>
        /// The position to place the TileFab
        /// </summary>
        public readonly Vector3Int                           m_Offset;
        /// <summary>
        /// The rotation to be applied (not implemented)
        /// </summary>
        public readonly TpTileBundle.TilemapRotation         m_Rotation;
        /// <summary>
        /// Control Flags. See TileFabLib
        /// </summary>
        public  FabOrBundleLoadFlags                m_LoadFlags;
        /// <summary>
        /// The filter, as described in the docs
        /// </summary>
        public readonly Func<FabOrBundleFilterType, BoundsInt, object, bool>? m_Filter;
        /// <summary>
        /// Mapping from Tilemap Names in the TileFab to Tilemap references in the scene: a speedup.
        /// </summary>
        public readonly Dictionary<string, Tilemap>          m_TargetMap;
        /// <summary>
        /// Normally true, but is false if non-parameter constructor used. Indicates an error if false.
        /// </summary>
        /// <value><c>true</c> if valid; otherwise, <c>false</c>.</value>
        public bool Valid { get; private set; }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="tileParent">Parent Tilemap of Tile, if the request is coming from a tile. Otherwise leave null</param>
        /// <param name="tileFab">The TileFab to load</param>
        /// <param name="offset">The position to place the TileFab</param>
        /// <param name="rotation">The rotation to be applied (not implemented)</param>
        /// <param name="loadFlags">Control flags. See TileFabLib</param>
        /// <param name="targetMap">Mapping from Tilemap Names in the TileFab to Tilemap references in the scene: a speedup.</param>
        /// <param name="filter">The filter, as described in the docs</param>
        public TileFabLoadParams(Tilemap?                      tileParent,
                                 TpTileFab                    tileFab,
                                 Vector3Int                   offset,
                                 TpTileBundle.TilemapRotation rotation,
                                 FabOrBundleLoadFlags         loadFlags,
                                 Dictionary<string, Tilemap>  targetMap,
                                 Func<FabOrBundleFilterType, BoundsInt, object, bool>? filter = null)
        {
            m_TileParent              = tileParent;
            m_TileFab                 = tileFab;
            m_Offset                  = offset;
            m_Rotation                = rotation;
            m_LoadFlags               = loadFlags;
            m_Filter                  = filter;
            m_TargetMap               = targetMap;
            Valid                     = true;
        }

       

    }

    
}
