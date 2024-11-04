// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 02-17-2022
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 02-17-2022
// ***********************************************************************
// <copyright file="TpMessaging.cs" company="Jeff Sasmor">
//     Copyright (c) 2022 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
using System;
using System.Collections.Generic;
using UnityEngine.Pool;
using UnityEngine.Tilemaps;
using static TilePlus.TpLib;
#nullable enable
namespace TilePlus
{
    /// <summary>
    /// Library of tile messaging methods
    /// </summary>
    public static class TpMessaging
    {
        /// <summary>
        /// send a message to one or more tagged tiles. Optional prefilter
        /// </summary>
        /// <typeparam name="TR">MessagePacket concrete class</typeparam>
        /// <typeparam name="T">MessagePacket concrete class</typeparam>
        /// <param name="map">tilemap to use. If null, uses all maps.</param>
        /// <param name="tag">tag to look for</param>
        /// <param name="packet">Data to send of type MessagePacket or subclass</param>
        /// <param name="output">List&lt;TI&gt; is cleared, then will have instances of type &lt;TI&gt;that the message was sent to. Ignored if null</param>
        /// <param name="filter">Optional prefilter delegate</param>
        /// <remarks>if 'output' is null, an internal list is allocated from a pool.
        /// If tiles matching the tag do not implement TR,T then those tiles will be in 'output' (if output isn't a null list)
        /// but the tiles will not be messaged.
        /// IMPORTANT: if using the output list, DO NOT hold the refs in the output list outside of the caller. </remarks>
        public static void SendMessage<TR, T>(Tilemap                   map,
                                              string                    tag,
                                              T                         packet,
                                              List<TilePlusBase>?        output = null,
                                              Func<TilePlusBase, bool>? filter = null)
            where T : MessagePacket<T>
            where TR : MessagePacket<TR>
        {
            if (output != null)
            {
                GetTilesWithTag(map, tag, ref output, filter);
                var num = output.Count;
                for (var i = 0; i < num; i++)
                {
                    var tgt = output[i] as ITpMessaging<TR, T>;
                    tgt?.MessageTarget(packet);
                }
            }
            else
            {
                using (S_TilePlusBaseList_Pool.Get(out var list))
                {
                    GetTilesWithTag(map, tag, ref list, filter);

                    var num = list.Count;
                    for (var i = 0; i < num; i++)
                    {
                        var tgt = list[i] as ITpMessaging<TR, T>;
                        tgt?.MessageTarget(packet);
                    }
                }
            }
        }

        /// <summary>
        /// send a message to one or more tiles of a particular type. Optional prefilter.
        /// </summary>
        /// <typeparam name="TR">MessagePacket concrete class</typeparam>
        /// <typeparam name="T">MessagePacket concrete class</typeparam>
        /// <param name="map">tilemap to use. If null uses all maps. See GetAllTilesOfType</param>
        /// <param name="typeSpec">What Type of tile to send the message to</param>
        /// <param name="packet">Data to send of type MessagePacket or subclass</param>
        /// <param name="output">List&lt;TI&gt; is cleared, then will have instances of type &lt;TI&gt;that the message was sent to. Ignored if null</param>
        /// <param name="filter">Optional filter delegate</param>
        /// <remarks>if 'output' is null, an internal list is allocated from a pool.
        /// If tiles matching typeSpec do not implement TR,T then those tiles will be in 'output' (if output isn't a null list)
        /// but the tiles will not be messaged.
        /// IMPORTANT: if using the output list, DO NOT hold the refs in the output list outside of the caller. </remarks>
        public static void SendMessage<TR, T>(Tilemap map,
            Type                                      typeSpec,
            T                                         packet,
            List<TilePlusBase>?                       output,
            Func<TilePlusBase, bool>?                 filter = null)
            where T : MessagePacket<T>
            where TR : MessagePacket<TR>
        {
            if (output != null)
            {
                GetAllTilesOfType(map, typeSpec, ref output, filter);
                var num = output.Count;
                
                for (var i = 0; i < num; i++)
                {
                    var tgt = output[i] as ITpMessaging<TR, T>;
                    tgt?.MessageTarget(packet);
                }
            }
            else
            {
                using (S_TilePlusBaseList_Pool.Get(out var list))
                {
                    GetAllTilesOfType(map, typeSpec, ref list, filter);

                    var num = list.Count;
                    for (var i = 0; i < num; i++)
                    {
                        var tgt = list[i] as ITpMessaging<TR, T>;
                        tgt?.MessageTarget(packet);
                    }
                } 
            }
        }
        
        
        /// <summary>
        /// send a message to one or more tiles with a particular interface
        /// </summary>
        /// <typeparam name="TR">MessagePacket concrete class</typeparam>
        /// <typeparam name="T">MessagePacket concrete class</typeparam>
        /// <typeparam name="TI">Interface. Note that it's implicit that the Tiles implementing this interface also must implement ITpMessaging&lt;TR, T&gt; </typeparam>
        /// <param name="packet">Data to send of type MessagePacket or subclass</param>
        /// <param name="output">List&lt;TI&gt; is cleared, then will have instances of type &lt;TI&gt;that the message was sent to. Ignored if null</param>
        /// <param name="filter">Optional delegate for test prior to sending message</param>
        /// <remarks>if 'output' is null, an internal list is allocated from a pool.
        /// If tiles implementing TI do not implement TR,T then those tiles will be in 'output' (if output isn't a null list)
        /// but the tiles will not be messaged.
        /// IMPORTANT: if using the output list, DO NOT hold the refs in the output list outside of the caller. </remarks>
        public static void SendMessage<TR, T, TI>(T packet, List<TI>? output = null, //note losing 'ref' here
            Func<TI, TilePlusBase, bool>?           filter = null)
            where T : MessagePacket<T>
            where TR : MessagePacket<TR>

        {
            if (output != null)
            {
                //output.Clear(); //redundant - is done in method call below
                TpLib.GetAllTilesWithInterface(ref output, filter);

                var num = output.Count;
                if (num == 0)
                    return;

                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < num; i++)
                {
                    var tgt = output[i] as ITpMessaging<TR, T>;
                    tgt?.MessageTarget(packet);
                }
            }
            else 
            {
                using (ListPool<TI>.Get(out var list))
                {
                    TpLib.GetAllTilesWithInterface(ref list, filter);

                    var num = list.Count;
                    if (num == 0)
                        return;

                    // ReSharper disable once ForCanBeConvertedToForeach
                    for (var i = 0; i < num; i++)
                    {
                        var tgt = list[i] as ITpMessaging<TR, T>;
                        tgt?.MessageTarget(packet);
                    }
                }

            }
        }
        
        
        
    }
}
