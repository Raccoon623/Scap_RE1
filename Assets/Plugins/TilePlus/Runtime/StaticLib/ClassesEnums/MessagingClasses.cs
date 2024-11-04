// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 01-02-2022
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 01-02-2022
// ***********************************************************************
// <copyright file="MessagingClasses.cs" company="Jeff Sasmor">
//     Copyright (c) 2022 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************

using UnityEngine;

namespace TilePlus
{
    /// <summary>
    /// Abstract base class for message packets.
    /// </summary>
    /// <typeparam name="T">Type of this packet</typeparam>
    // ReSharper disable once UnusedTypeParameter
    public abstract class MessagePacket<T> { }

    /// <summary>
    /// Empty class to use when a placeholder is needed.
    /// </summary>
    public class EmptyPacket: MessagePacket<EmptyPacket> {}



    /// <summary>
    /// Common message packet for a Vector3Int position
    /// to a tile.
    /// </summary>
    public class PositionPacketIn : MessagePacket<PositionPacketIn> 
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PositionPacketIn"/> class.
        /// </summary>
        public PositionPacketIn() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionPacketIn"/> class.
        /// </summary>
        /// <param name="pos">The position.</param>
        public PositionPacketIn(Vector3Int pos)
        {
            m_Position = pos;
        }
        /// <summary>
        /// A grid position
        /// </summary>
        public Vector3Int m_Position;
    }


    /// <summary>
    /// PositionPacket with Zone Manager reference. 
    /// </summary>
    public class PositionZmPacketIn : MessagePacket<PositionZmPacketIn>
    {
        /// <summary>
        /// A Zone Manager instance
        /// </summary>
        public readonly TpZoneManager m_ZoneManager;
        /// <summary>
        /// A grid position
        /// </summary>
        public Vector3Int m_Position;
        
        /// <summary>
        /// Ctor
        /// </summary>
        public PositionZmPacketIn(){}
        
        /// <summary>
        /// Create a position packet with a ZoneManager reference
        /// </summary>
        /// <param name="pos">The position</param>
        /// <param name="zoneManager">a valid ZoneManager instance</param>
        public PositionZmPacketIn(Vector3Int pos, TpZoneManager zoneManager)
        {
            m_ZoneManager = zoneManager;
            m_Position    = pos;
        }
    }
    

    /*Note: PositionPacketOut *is* the same as PositionPacketIn.
     * This is intentional, and done to show that two different
     * subclasses of the abstract MessagePacket class can be
     * used with this interface.
     */
    /// <summary>
    /// Common message packet for a Vector3Int position
    /// from a tile.
    /// </summary>
    public class PositionPacketOut : MessagePacket<PositionPacketOut>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PositionPacketOut"/> class.
        /// </summary>
        public PositionPacketOut() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionPacketOut"/> class.
        /// </summary>
        /// <param name="pos">The position.</param>
        public PositionPacketOut(Vector3Int pos)
        {
            m_Position = pos;
        }

        /// <summary>
        /// A grid position
        /// </summary>
        public Vector3Int m_Position;
    }


    /// <summary>
    /// Simple packet with just a boolean value
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    public class BoolPacket : MessagePacket<BoolPacket>
    {
        /// <summary>
        /// payload
        /// </summary>
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        public bool m_Bool;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoolPacket"/> class.
        /// </summary>
        public BoolPacket() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="BoolPacket"/> class.
        /// </summary>
        /// <param name="value">value for this packet</param>
        public BoolPacket(bool value)
        {
            m_Bool = value;
        }
    }

    /// <summary>
    /// Common message packet for a string sent to a tile.
    /// E.G., a JSON string for the tile to expand and use
    /// to restore data
    /// </summary>
    public class StringPacketIn : MessagePacket<StringPacketIn>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringPacketIn"/> class.
        /// </summary>
        public StringPacketIn()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringPacketIn"/> class.
        /// </summary>
        /// <param name="s">The string.</param>
        public StringPacketIn(string s)
        {
            m_String = s;
        }

        /// <summary>
        /// the string sent to the tile.
        /// </summary>
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        public string m_String;
    }
}
