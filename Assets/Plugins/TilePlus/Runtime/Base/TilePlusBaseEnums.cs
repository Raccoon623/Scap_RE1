// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 06-16-2021
// ***********************************************************************
// <copyright file="TilePlusBaseEnums.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
namespace TilePlus
{


    /// <summary>
    /// SpriteClearMode:
    /// ClearInSceneView:
    /// the tile sprite is cleared during edit time
    /// Note that the tile itself is still present, just not visible.
    /// Why do this? You can have a tile showing in the palette but when placed you don't want
    /// to see it anymore, ie you're only using the tile's sprite so you can see something in
    /// the palette. You could for example have a sprite that's some text like "waypoint"
    /// so it'd display that in the palette but not appear after the tile is placed. Useful w/ prefabs.
    /// ClearOnStart:  Clears the sprite when the app begins
    /// ClearInSceneViewAndOnStart = ClearInSceneView + ClearOnStart
    /// </summary>
    public enum SpriteClearMode
    {
        /// <summary>
        /// Nothing is done.
        /// </summary>
        Ignore,

        /// <summary>
        /// Clear the tile sprite when it's in the scene
        /// </summary>
        ClearInSceneView,

        /// <summary>
        /// Clear the tile sprite when the Application starts.
        /// </summary>
        ClearOnStart,

        /// <summary>
        /// Clear the tile sprite in the scene and keep clear in a running App.
        /// </summary>
        ClearInSceneViewAndOnStart
    }

    /// <summary>
    /// Force the tile sprite collider mode
    /// </summary>
    public enum ColliderMode
    {
        /// <summary>
        /// Nothing is done
        /// </summary>
        NoOverride,

        /// <summary>
        /// Set the sprite collider mode to Sprite
        /// </summary>
        Sprite,

        /// <summary>
        /// Set the sprite collider mode to Grid
        /// </summary>
        Grid,

        /// <summary>
        /// Set the sprite collider mode to No collider.
        /// </summary>
        NoCollider
    }

    /// <summary>
    /// Indicates the state of the tile.
    /// </summary>
    public enum TilePlusState
    {
        /// <summary>
        /// This tile is an asset in the project
        /// </summary>
        Asset,

        /// <summary>
        /// This tile is a clone living in a Scene
        /// </summary>
        Clone,

        /// <summary>
        /// This is a locked tile, bound to an asset file in the project
        /// </summary>
        Locked
    }

    /// <summary>
    /// how is this tile being reset?
    /// </summary>
    public enum TileResetOperation
    {
        /// <summary>
        /// Want to make a locked asset
        /// </summary>
        MakeLockedAsset,

        /// <summary>
        /// Want to make a normal asset
        /// </summary>
        MakeNormalAsset,

        /// <summary>
        /// Unlock a tile and clone it.
        /// </summary>
        UnlockAndClone,

        /// <summary>
        /// Used when a clone tile is being duplicated.
        /// </summary>
        MakeCopy,
        /// <summary>
        /// Used to force a TPT tile into the clone state from the asset state. Use with caution!
        /// </summary>
        SetCloneState,
        
        /// <summary>
        /// Clear the GUID. 
        /// </summary>
        ClearGuid,
       
        ///<summary>
        /// op=Restore is used only in editor mode, when picked tiles
        /// are painted. As the picked tiles are moved around the
        /// map, their Startup is called many times by Editor code, placing incorrect
        /// grid position and possible incorrect map refs in m_TileGridPosition and
        /// m_ParentTilemap. Restore uses the saved values in TpLib's TileIdToTile
        /// dictionary  to restore the proper values. Only used in TilePlusBase
        /// implementations but might be of use in other situations.</summary>
        Restore
        
       
        
        
    }

}
