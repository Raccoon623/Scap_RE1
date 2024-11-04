// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 05-23-2021
// ***********************************************************************
// <copyright file="TpLibEnums.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
using System;
namespace TilePlus
{
    /// <summary>
    /// Indicates what type of event a TPT tile is sending.
    /// </summary>
    [Flags]
    public enum TileEventType
    {
        /// <summary>
        /// Tile posts that it has data to save.
        /// </summary>
        SaveDataEvent  = 1,

        /// <summary>
        /// The tile wants to notify something that it has been triggered, eg,
        /// trigger zone has been entered or similar.
        /// </summary>
        TriggerEvent = 2 ,
        
        /// <summary>
        /// Both event types
        /// </summary>
        Both = SaveDataEvent | TriggerEvent
    }

    /// <summary>
    /// Return values for filter used in NumTileWithInterface
    /// </summary>
    public enum NumTileWithInterfaceFilterResult
    {
        /// <summary>
        /// Failed test
        /// </summary>
        Fail,
        /// <summary>
        /// Test passes
        /// </summary>
        Pass,
        /// <summary>
        /// test passes, exit test.
        /// </summary>
        /// <remarks>Used when you just want to test for ONE match as in 'Any'</remarks>
        PassAndQuit
    }

    /// <summary>
    /// Value passed when TpLib OnTypeOrTagChanged event fires
    /// </summary>
    public enum OnTypeOrTagChangedVariety
    {
        /// <summary>
        /// A TYPE has changed
        /// </summary>
        Type,

        /// <summary>
        /// A TAG has changed
        /// </summary>
        Tag
    }
    
    /// <summary>
    /// When the OnTpLibChanged event fires,
    /// this param is the type of change that occurred
    /// </summary>
    public enum TpLibChangeType
    {
        /// <summary>
        /// A TilePlus tile was added
        /// </summary>
        Added,
        /// <summary>
        /// A TilePlus tile was added to a previously-empty tilemap
        /// </summary>
        AddedToEmptyMap,
        /// <summary>
        /// A tile (any sort of tile) was deleted
        /// </summary>
        Deleted,
        /// <summary>
        /// A tile (any sort of tile) was modified
        /// </summary>
        Modified,
        /// <summary>
        /// Something was either modified or added.
        /// ONLY for Tile or TileBase class instances
        /// Note that position will be invalid 
        /// </summary>
        ModifiedOrAdded,
        /// <summary>
        /// Tags were modified 
        /// </summary>
        TagsModified
    }

}
