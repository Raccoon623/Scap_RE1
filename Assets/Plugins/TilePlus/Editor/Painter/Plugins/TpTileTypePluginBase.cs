// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 01-01-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 01-05-2023
// ***********************************************************************
// <copyright file="TpTileTypePluginBase.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Base class for TileBase-derived tiles plugins: provide
// sprite/transform etc for TileBase-derived tiles that don't have such properties</summary>
// ***********************************************************************

using UnityEngine;
using UnityEngine.Tilemaps;
// ReSharper disable UnusedParameter.Global
// ReSharper disable VirtualMemberNeverOverridden.Global
#nullable enable
namespace TilePlus.Editor
{
    /// <summary>
    /// This is a base class for Painter plugins to
    /// support TileBase-type (ie tiles subclassed from
    /// TileBase instead of Tile).
    /// TileBase doesn't have a sprite, transform or color
    /// and existing derived types like RuleTile or AnimatedTile
    /// don't have  a sprite field or property
    /// but (for example in RuleTile) m_DefaultSprite 
    /// </summary>
    public class TpPainterPluginBase : ScriptableObject
    {
        /// <summary>
        /// If this is TRUE then this plugin is ignored.
        /// </summary>
        [Tooltip("If this is checked then this plugin is  ignored")]
        public bool m_IgnoreThis = true;


        /// <summary>
        /// What's the Type of this tile?
        /// </summary>
        /// <remarks>Used in T+P to build a map of Types to TpPainterPlugin instances </remarks>
        // ReSharper disable once AnnotateNotNullTypeMember
        public virtual System.Type GetTargetTileType => typeof(TileBase);

        /// <summary>
        /// Get the sprite for this tile
        /// </summary>
        /// <param name="tileBase"></param>
        /// <returns>Sprite or null</returns>
        // ReSharper disable once AnnotateCanBeNullTypeMember
        public virtual Sprite? GetSpriteForTile(TileBase tileBase)
        {
            return null; //note that a tileBase has no sprite!
        }

        /// <summary>
        /// Get the transform for this tile
        /// </summary>
        /// <param name="tilebase"></param>
        /// <returns>Matrix4x4 (identity is the default)</returns>
        public virtual Matrix4x4 GetTransformForTile(TileBase tilebase)
        {
            return Matrix4x4.identity;
        }

        /// <summary>
        /// Get the Color for this tile.
        /// </summary>
        /// <param name="tilebase"></param>
        /// <returns>Color (white is the default)</returns>
        public virtual Color GetColorForTile(TileBase tilebase)
        {
            return Color.white;
        }

    }
}
