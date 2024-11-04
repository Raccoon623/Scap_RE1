// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 03-28-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 03-29-2023
// ***********************************************************************
// <copyright file="IChunkSelector.cs" company="Jeff Sasmor">
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
    /// Interface IChunkSelector
    /// </summary>
    public interface IChunkSelector
    {
        /// <summary>
        /// Obtain TileFab load params given a locator
        /// </summary>
        /// <param name="locator">The Locator.</param>
        /// <param name="layout">The Zone Layout instance.</param>
        /// <param name="monitoredMaps">Tilemaps used by the ZoneManager.</param>
        /// <returns>System.Nullable&lt;TileFabLoadParams&gt;.</returns>
        TileFabLoadParams? Selector(RectInt locator, TpZoneLayout layout, Dictionary<string, Tilemap> monitoredMaps)
        {
            return null;
        }

        /// <summary>
        /// Initialization
        /// </summary>
        /// <param name="zm">The Zone Manager this Selector is using</param>
        /// <param name="obj">Arbitrary data</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool Initialize(TpZoneManager zm, object? obj = null)
        {
            return true;
        }

        /// <summary>
        /// A list of the TileFabs used by this selector. If not appropriate, return null.
        /// </summary>
        List<TpTileFab>? UsedTileFabs => null;

    }
}
