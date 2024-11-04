// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 03-25-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 04-01-2023
// ***********************************************************************
// <copyright file="TpChunkTemplateSelector.cs" company="Jeff Sasmor">
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
    /// A chunk selector plugin that indexes into an array of chunks.
    /// note that this is a subclass of TpChunkSelectorBase
    /// </summary>
    [CreateAssetMenu(fileName = "TpChunkTemplateSelector.asset", menuName = "TilePlus/Create TemplateChunkSelector", order = 100000)]
    public class TpChunkTemplateSelector : TpChunkSelectorBase
    {
        /// <summary>
        /// The template
        /// </summary>
        [Tooltip("The Chunk Layout Template created by the Template Creator tool.")]
        public TpChunkLayoutTemplate? m_ChunkLayoutTemplate ;

        /// <summary>
        /// The chunks
        /// </summary>
        [NonSerialized]
        private readonly List<TpTileFab> chunks = new();


        /// <summary>
        /// Template selector that uses multiple TileFabs for filling in empty areas
        /// </summary>
        /// <param name="locator">the locator</param>
        /// <param name="layout">the ZoneLayout instance</param>
        /// <param name="monitoredMaps">maps used by this layout</param>
        /// <returns>TileFab load params</returns>
        /// <inheritdoc />
        public override TileFabLoadParams? Selector(RectInt locator, TpZoneLayout layout, Dictionary<string,Tilemap> monitoredMaps)
        {
            var templateLength = chunks.Count;
            if (templateLength == 0)
            {
                Debug.LogError($"Could not configure this ChunkSelector! IID:{GetInstanceID()}");
                return null;
            }
                
            //the position can be found from the Locator
            var pos      = locator.position;

            //the size of a tilefab is the same as the locator's size.
            var locatorSize     = locator.size;
            if (locatorSize == Vector2Int.zero)  //just in case
                return null;
            var templateSize = templateLength / 2;
            
            //now index into the array to get the correct Chunk
            var effPosition = pos - (Vector2Int) layout.m_WorldOrigin;
            var colX        = (Mathf.Abs(effPosition.x) / locatorSize.x) % templateSize;
            var rowY        = (Mathf.Abs(effPosition.y) / locatorSize.y) % templateSize;
            var index       = (rowY * templateSize) + colX;
            if (index < 0 || index >= templateLength)
                return null;
            var chunk = chunks[index];
            if (chunk == null)
                return null;
                
            return new TileFabLoadParams(null,
                                         chunk,
                                         (Vector3Int)pos,
                                         TpTileBundle.TilemapRotation.Zero,
                                         FabOrBundleLoadFlags.NormalWithFilter,
                                         monitoredMaps);
        }

        ///<inheritdoc/>
        public override bool Initialize(TpZoneManager zm, object? obj = null)
        {
            if (m_ChunkLayoutTemplate == null)
            {
                Debug.LogError($"Missing ChunkLayoutTemplate reference in TpLayoutTemplate component attached to {name}");
                return false;
            }

            chunks.Clear();
            chunks.AddRange(m_ChunkLayoutTemplate.m_TileFabs);
            
            return true;
        }


        /// <summary>
        /// return the list of chunks
        /// </summary>
        public override List<TpTileFab> UsedTileFabs => chunks;
    }
}
