// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 03-28-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 03-28-2023
// ***********************************************************************
// <copyright file="TpChunkSelectorBase.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TilePlus
{
    /// <summary>
    /// Not for use, just a concrete base class
    /// </summary>
    public class TpChunkSelectorBase : ScriptableObject, IChunkSelector
    {
        /// <inheritdoc />
        public virtual TileFabLoadParams Selector(RectInt locator, TpZoneLayout layout, Dictionary<string, Tilemap> monitoredMaps)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public virtual bool Initialize(TpZoneManager zm,      object      obj = null)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public virtual List<TpTileFab> UsedTileFabs => null;
    }
}
