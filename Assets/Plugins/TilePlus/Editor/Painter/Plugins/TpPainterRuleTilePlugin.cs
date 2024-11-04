// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 01-01-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 01-05-2023
// ***********************************************************************
// <copyright file="TpPainterRuleTilePlugin.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Plugin for Rule tiles</summary>
// ***********************************************************************

using UnityEngine;
using UnityEngine.Tilemaps;
using static TilePlus.Editor.TpIconLib;
#nullable enable
namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// Plug-in to support Rule tiles in the Tile+Painter
    /// </summary>
   [CreateAssetMenu(fileName = "RuleTilePlugin.asset", menuName = "TilePlus/Create RuleTile plugin", order = 100000)]
    public class TpPainterRuleTilePlugin : TpPainterPluginBase
    {

        /// <inheritdoc />
        public override System.Type GetTargetTileType => typeof(RuleTile);

        /// <inheritdoc />
        public override Sprite? GetSpriteForTile(TileBase tileBase)
        {
            if (tileBase == null)
                return FindIconAsSprite(TpIconType.UnityToolbarMinusIcon);
            
            // ReSharper disable once Unity.NoNullPatternMatching
            return tileBase is not RuleTile rt
                       ? FindIconAsSprite(TpIconType.UnityToolbarMinusIcon)
                       : rt.m_DefaultSprite;
        }


    }
}
