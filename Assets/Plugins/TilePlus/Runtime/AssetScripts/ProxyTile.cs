#if UNITY_EDITOR
// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 01-10-2024
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 01-21-2024
// ***********************************************************************
// <copyright file="ProxyTile.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************

using UnityEngine;
using UnityEngine.Tilemaps;

namespace TilePlus.Editor
{
    /// <summary>
    /// This tile is nothing more than a normal Tile with a different class name.
    /// Used when prefabs or Tile/TPB with Locked color/transform are being previewed. Not included in a build.
    /// </summary>
    public class ProxyTile : Tile
    {
        /// <summary>
        /// Reset flags to None, transform to identity, color to White.
        /// </summary>
        public void Init()
        {
            flags     = TileFlags.None;
            transform = Matrix4x4.identity;
            color     = Color.white;
        }
    }
}
#endif
