// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 08-03-2021
// ***********************************************************************
// <copyright file="ITpMessaging.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************


#nullable enable
namespace TilePlus
{
    /// <summary>
    /// Interface for using TpLib SendMessage methods.
    /// </summary>
    /// <typeparam name="TR">Type for getting data</typeparam>
    /// <typeparam name="T">Type for sending a message</typeparam>
    public interface ITpMessaging <out TR, in T> where T:MessagePacket<T> where TR:MessagePacket<TR>
    {
        /// <summary>
        /// Send a message of type T
        /// </summary>
        /// <param name="sentPacket">The sent packet.</param>
        void MessageTarget(T sentPacket);

        /// <summary>
        /// Get data of type TR
        /// </summary>
        /// <returns>instance of class TR</returns>
        /// <remarks>note that default implementation returns null</remarks>
        TR? GetData() { return null; }
    }
}
