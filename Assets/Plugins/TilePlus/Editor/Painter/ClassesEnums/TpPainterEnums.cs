// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 11-08-2022
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-31-2022
// ***********************************************************************
// <copyright file="TpPainterEnums.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Enums for Tile+Painter</summary>
// ***********************************************************************


namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// GlobalMode: Painting or Editing
    /// </summary>
    public enum GlobalMode
    {
        /// <summary>
        /// palettes view: view palettes in center column,
        /// palette contents in right column, click on r column to select for painting.
        /// </summary>
        PaintingView = 0,
        /// <summary>
        /// tilemaps view: view tilemap contents in center column,
        /// edit selection in right column
        /// </summary>
        EditingView = 1,
        /// <summary>
        /// Grid Selection view. View list of Grid Selections, create new ones
        /// </summary>
        GridSelView = 2
        
    }

    /// <summary>
    /// Tool varieties
    /// </summary>
    public enum TpPainterTool
    {
        /// <summary>
        /// No tool (corresponds to leftmost toolbar button)
        /// </summary>
        None = 0,
        /// <summary>
        /// Paint tool
        /// </summary>
        Paint,
        /// <summary>
        /// Erase tool
        /// </summary>
        Erase,
        /// <summary>
        /// Pick tool
        /// </summary>
        Pick,
        /// <summary>
        /// Move tool
        /// </summary>
        Move,
        /// <summary>
        /// Rotate CW tool
        /// </summary>
        RotateCw,
        /// <summary>
        /// Rotate CCW tool
        /// </summary>
        RotateCcw,
        /// <summary>
        /// Flip X tool
        /// </summary>
        FlipX,
        /// <summary>
        /// Flip Y tool
        /// </summary>
        FlipY,
        /// <summary>
        /// Reset Transform tool
        /// </summary>
        ResetTransform,
        /// <summary>
        /// Help tool
        /// </summary>
        Help,
        /// <summary>
        /// Settings tool
        /// </summary>
        Settings

        
    }

    /// <summary>
    /// Sequence states for MOVE tool
    /// </summary>
    public enum TpPainterMoveSequenceStates
    {
        /// <summary>
        /// Not in a Move sequence
        /// </summary>
        None,
        /// <summary>
        /// The pick state
        /// </summary>
        Pick,
        /// <summary>
        /// The paint state
        /// </summary>
        Paint
    }

    /// <summary>
    /// What sort of 'palette'
    /// </summary>
    internal enum TpPaletteListItemType
    {
        /// <summary>
        /// None
        /// </summary>
        None,
        /// <summary>
        /// A real palette
        /// </summary>
        Palette,
        /// <summary>
        /// A 'Bundle'  asset
        /// </summary>
        Bundle,
        /// <summary>
        /// Favorites
        /// </summary>
        Favorites,
        /// <summary>
        /// A TileFab asset
        /// </summary>
        TileFab
    }

    /// <summary>
    /// What is the picked tile
    /// </summary>
    internal enum TpPickedTileType
    {
        /// <summary>
        /// No picked tile
        /// </summary>
        None,
        /// <summary>
        /// A tile
        /// </summary>
        Tile,
        /// <summary>
        /// A tilefab
        /// </summary>
        TileFab,
        /// <summary>
        /// A Bundle asset or 'Bundle'
        /// </summary>
        Bundle,
        /// <summary>
        /// A bunch of cells from the Palette.
        /// </summary>
        Multiple,
        /// <summary>
        /// A prefab
        /// </summary>
        Prefab
    }

    /// <summary>
    /// What variety of tile sorting
    /// </summary>
    public enum TpTileSorting
    {
        /// <summary>
        /// No sorting
        /// </summary>
        None,
        /// <summary>
        /// Sort by Type
        /// </summary>
        Type,
        /// <summary>
        /// Sort by IID
        /// </summary>
        Id
    }

   
}

