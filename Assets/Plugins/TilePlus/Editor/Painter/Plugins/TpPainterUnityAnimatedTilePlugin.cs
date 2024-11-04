
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 02-03-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 02-03-2023
// ***********************************************************************
// <copyright file="TpPainterUnityAnimatedTilePlugin.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Plugin for 2D Tilemap Extras' Animated Tile</summary>
// ***********************************************************************

using UnityEngine;
using UnityEngine.Tilemaps;
#nullable enable
namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// Plug-in to support Rule tiles in the Tile+Painter
    /// </summary>
    [CreateAssetMenu(fileName = "UnityAnimatedTilePlugin.asset", menuName = "TilePlus/Create Unity AnimatedTile plugin", order = 100000)]
    public class TpPainterUnityAnimatedTilePlugin : TpPainterPluginBase
    {

        /// <inheritdoc />
        public override System.Type GetTargetTileType => typeof(AnimatedTile);

        /// <inheritdoc />
        public override Sprite? GetSpriteForTile(TileBase tileBase)
        {
            if (tileBase == null)
                return TpIconLib.FindIconAsSprite(TpIconType.UnityToolbarMinusIcon);
            // ReSharper disable once Unity.NoNullPatternMatching
            return tileBase is not AnimatedTile t
                       ? null
                       : t.m_AnimatedSprites[0];
        }


    }
}

