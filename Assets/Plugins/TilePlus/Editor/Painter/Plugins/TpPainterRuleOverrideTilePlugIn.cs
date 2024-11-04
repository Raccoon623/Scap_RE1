// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 01-01-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 01-05-2023
// ***********************************************************************
// <copyright file="TpPainterRuleOverrideTilePlugIn.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Plugin for Rule Override tiles </summary>
// ***********************************************************************

using UnityEngine;
using UnityEngine.Tilemaps;
using static TilePlus.Editor.TpIconLib;
#nullable enable
namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// Plug-in to support Rule Override Tiles in Tile+Painter
    /// </summary>
    public class TpPainterRuleOverrideTilePlugIn : TpPainterPluginBase
    {
       /// <inheritdoc />
        public override System.Type GetTargetTileType => typeof(RuleOverrideTile);

        /// <inheritdoc />
        public override Sprite? GetSpriteForTile(TileBase tileBase)
        {
            if (tileBase == null)
                FindIconAsSprite(TpIconType.UnityToolbarMinusIcon);
            // ReSharper disable once Unity.NoNullPatternMatching
            return tileBase is not RuleOverrideTile rt
                       ? FindIconAsSprite(TpIconType.UnityToolbarMinusIcon)
                       :  rt.m_Sprites[0].m_OriginalSprite; //hard to say what's right here...  
        }
    }
}
