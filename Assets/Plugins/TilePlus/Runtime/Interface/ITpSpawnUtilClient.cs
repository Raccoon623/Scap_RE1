// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 08-03-2021
// ***********************************************************************
// <copyright file="ITpSpawnUtilClient.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
using UnityEngine;

namespace TilePlus
{

    /// <summary>
    /// Contract for tiles that spawn prefabs or paint tiles
    /// and want to use methods in SpawningUtil.
    /// </summary>
    public interface ITpSpawnUtilClient
    {
        /// <summary>
        /// Positioning mode for spawning or painting
        /// </summary>
        /// <value>The positioning mode.</value>
        SpawningUtil.PositioningMode PositioningMode     { get; }
        /// <summary>
        /// Bounds for the zone
        /// </summary>
        /// <value>The zone bounds.</value>
        Bounds ZoneBounds      { get; }
        /// <summary>
        /// Last contact position
        /// </summary>
        /// <value>The last contact position.</value>
        Vector3Int LastContactPosition { get; }
        
    }
}
