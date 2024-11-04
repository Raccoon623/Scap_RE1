// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 05-23-2021
// ***********************************************************************
// <copyright file="DeferredTileEvent.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
namespace TilePlus
{

    /// <summary>
    /// When a tile posts an event and the type of
    /// event is Defer or DeferAndNotify then
    /// instances of this class are cached in the
    /// DeferredTileEvents list.
    /// This class isn't used outside of TpLib internals.
    /// </summary>
    public class DeferredTileEvent
    {
        /// <summary>
        /// The tile instance related to the event
        /// </summary>
        public TilePlusBase m_Instance;

        /// <summary>
        /// The event type for this event
        /// </summary>
        public TileEventType m_EventType;

    }
}
