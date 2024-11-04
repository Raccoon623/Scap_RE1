// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 03-25-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 04-01-2023
// ***********************************************************************
// <copyright file="TpSingleFabChunkSelector.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TilePlus
{
    /// <summary>
    /// Base-class for Chunk Selectors as used by TpZoneLayout.
    /// This just returns the same TileFab each time
    /// </summary>
    [CreateAssetMenu(fileName = "TpSingleFabChunkSelector.asset", menuName = "TilePlus/Create SingleFab Chunk Selector", order = 100000)]
    public class TpSingleFabChunkSelector : TpChunkSelectorBase
    {
        /// <summary>
        /// The single tilefab to return to the caller
        /// </summary>
        [Tooltip("Single TileFab to return from this Selector")]
        #pragma warning disable CS8618
        public TpTileFab m_TileFab;
        #pragma warning restore CS8618

        /// <summary>
        /// Basic selector that only uses one TileFab for filling in empty areas
        /// </summary>
        /// <param name="locator">the locator</param>
        /// <param name="_">the ZoneLayout instance</param>
        /// <param name="monitoredMaps">maps for this layout</param>
        /// <returns>TileFab load params</returns>
        public override TileFabLoadParams Selector(RectInt locator, TpZoneLayout _, Dictionary<string,Tilemap> monitoredMaps)
        {
            return new TileFabLoadParams(null,
                                             m_TileFab,
                                             (Vector3Int)locator.position,
                                             TpTileBundle.TilemapRotation.Zero,
                                             FabOrBundleLoadFlags.NormalWithFilter,
                                             monitoredMaps);
        }

        /// <summary>
        /// Use for any initialization.
        /// </summary>
        /// <param name="zoneManager">A Zone manager instance</param>
        /// <param name="obj">Arbitrary data you provide</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <inheritdoc />
        public override bool Initialize(TpZoneManager zoneManager, object? obj = null)
        {
            return true;
        }
        
        /// <summary>
        /// return just this single TileFab
        /// </summary>
        public override List<TpTileFab> UsedTileFabs => new() { m_TileFab };
    }
}
