// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 02-17-2022
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 02-17-2022
// ***********************************************************************
// <copyright file="TpEvents.cs" company="Jeff Sasmor">
//     Copyright (c) 2022 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************

#if ODIN_INSPECTOR
#define USE_ODIN
using Sirenix.OdinInspector;
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Pool;
using static TilePlus.TpLib;

#nullable enable

namespace TilePlus
{

    /// <summary>
    /// Events library for TilePlus tiles.
    /// </summary>
    public static class TpEvents
    {
        /// <summary>
        /// Subscribe to get notified when your tile sends a event notice in immediate mode or for communication.
        /// </summary>
        public static event Action<TileEventType>? OnTileEvent;
        
        
        /// <summary>
        /// This is a list of tile events.
        /// </summary>
        #if USE_ODIN
        [ShowInInspector, ReadOnly]
        #endif
        private static readonly List<DeferredTileEvent> s_DeferredTileEvents = new(8);

        /// <summary>
        /// This Dictionary of Tile InstanceIDs to TileEventType enums
        /// is used to ensure that a tile can only post one event of each variety between flushes.
        /// Note that TileEventType is a flags enum.
        /// </summary>
        #if USE_ODIN
        [ShowInInspector, ReadOnly]
        #endif
        private static readonly Dictionary<int, TileEventType> s_DeferredEventsTracker = new(8);
        
        /// <summary>
        /// number of tile save events
        /// </summary>
        #if USE_ODIN
        [ShowInInspector, ReadOnly]
        #endif
        private static int s_NumSaveEvents;

        /// <summary>
        /// number of tile trigger events
        /// </summary>
        #if USE_ODIN
        [ShowInInspector, ReadOnly]
        #endif
        private static int s_NumTriggerEvents;
        
        
        
        /// <summary>
        /// Are there any deferred Saveable instances in the List?
        /// </summary>
        /// <value><c>true</c> if this instance has deferred tile events; otherwise, <c>false</c>.</value>
        public static bool HasDeferredTileEvents => s_DeferredTileEvents.Count != 0;

        /// <summary>
        /// Get all Queued deferred Tile Events. DON'T modify the list.
        /// </summary>
        /// <value>The deferred tile events.</value>
        public static List<DeferredTileEvent> DeferredTileEvents => s_DeferredTileEvents;

        /// <summary>
        /// Are there any tile save events?
        /// </summary>
        /// <value><c>true</c> if there are any save events; otherwise, <c>false</c>.</value>
        public static bool AnySaveEvents => s_NumSaveEvents != 0;

        /// <summary>
        /// Are there any tile trigger events
        /// </summary>
        /// <value><c>true</c> if there are any trigger events; otherwise, <c>false</c>.</value>
        public static bool AnyTriggerEvents => s_NumTriggerEvents != 0;

        /// <summary>
        /// Get the number of SaveEvents
        /// </summary>
        public static int NumSaveEvents => s_NumSaveEvents;
        
        /// <summary>
        /// Get the number of TriggerEvents
        /// </summary>
        public static int NumTriggerEvents => s_NumTriggerEvents;

        /// <summary>
        /// Clears OnTileEvent events. 
        /// </summary>
        public static void ClearOnTileEventSubscribers()
        {
            OnTileEvent = null;
        }

        /// <summary>
        /// Post a Trigger event
        /// </summary>
        /// <param name="tpb">tile instance sending the event.
        /// usually 'this' is OK</param>
        public static void PostTileTriggerEvent(TilePlusBase tpb)
        {
            PostTileEvent(TileEventType.TriggerEvent, tpb);
        }

        /// <summary>
        /// Post a Save Data  event
        /// </summary>
        /// <param name="tpb">tile instance sending the event.
        /// usually 'this' is OK</param>
        public static void PostTileSaveDataEvent(TilePlusBase tpb)
        {
            PostTileEvent(TileEventType.SaveDataEvent, tpb);
        }



        /// <summary>
        /// Post a tile event. Trigger events have different possible values
        /// for evtType than SaveData events.
        /// </summary>
        /// <param name="evtType">TileEventType enum value</param>
        /// <param name="tpb">TilePlusBase instance</param>
        /// <returns>true for success, false for param error</returns>
        /// <remarks>if TileEventType.Both is used the this method will ignore it! (why it's private) </remarks>
        /// <exception cref="System.ArgumentOutOfRangeException">evtType - null</exception>
        private static void PostTileEvent(TileEventType evtType, TilePlusBase tpb)
        {
            if (evtType == TileEventType.Both)
                return;
            
            var id = tpb.Id;

            //if id matches, test for same type of event
            if (s_DeferredEventsTracker.TryGetValue(id, out var eventType)) 
            {
                //if the existing event type is the same as the new one then we're done
                if ((eventType & evtType) == evtType) 
                    return;
                s_DeferredEventsTracker[id] |= evtType;
            }
            else
                s_DeferredEventsTracker.Add(id, evtType);
            
            //nb: moved this section to after the s_DeferredEventsTracker tests.
            //Makes count more accurate for cases where a tile posts multiple events of same type
            //(although no reason to post > 1 save event from same tile)            
            switch (evtType)
            {
                case TileEventType.TriggerEvent:
                    s_NumTriggerEvents++;
                    break;
                case TileEventType.SaveDataEvent:
                    s_NumSaveEvents++;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(evtType), evtType, null);
            }

            var deferredEvent = S_DeferredEvent_Pool.Get();
            deferredEvent.m_Instance  = tpb;
            deferredEvent.m_EventType = evtType;
            
            s_DeferredTileEvents.Add(deferredEvent);
            OnTileEvent?.Invoke(evtType);
        }

        /// <summary>
        /// Is there at least one event that matches an EventType and satisfies a filter Func?
        /// </summary>
        /// <param name="evtType">The event type. Note that this is a flags enum so you can OR the event types.</param>
        /// <param name="filter">A func that takes a TilePlusBase instance and returns a bool (true) for a match</param>
        /// <returns>true if there's at least one event of the specified event type that matches the filter</returns>
        /// <remarks>returns false if there are no events or if the Func is null. Note that if all you want to know
        /// is 'any events of type x' use properties AnySaveEvents or AnyTriggerEvents.</remarks>
        public static bool AnyMatchingFilteredEvents(TileEventType evtType, Func<TilePlusBase, bool>? filter)
        {
            if (filter == null || s_DeferredTileEvents.Count == 0)
                return false;
            //nb .Any is from IEnumerable and not LINQ
            return s_DeferredTileEvents.Any(evt => (evt.m_EventType & evtType) == evtType && filter(evt.m_Instance));
        }

        /// <summary>
        /// Get all events for an event type.
        /// </summary>
        /// <param name="evtType">the event type.  Note that this is a flags enum so you can OR the event types.</param>
        /// <param name="output">ref List for result, if null = error. Cleared after null-check</param>
        /// <param name="filter">if not null, is a func that takes a TilePlusBase instance as input and returns true/false</param>
        /// <remarks>Use the DeferredTileEvents property to get the entire list.</remarks>
        public static void GetFilteredEvents(TileEventType evtType,
            ref List<TilePlusBase>?                        output,
            Func<TilePlusBase, bool>?                      filter = null)
        {
            var num = s_DeferredTileEvents.Count;
            if (num == 0)
                return;
            
            if (output == null)
            {
                TpLogError("Null output list passed to GetFilteredEvents");
                return;
            }
            output.Clear();
            
            if (filter == null)
            {
                for (var i = 0; i < num; i++)
                {
                    if ( (s_DeferredTileEvents[i].m_EventType & evtType) == evtType)
                        output.Add(s_DeferredTileEvents[i].m_Instance);
                }
                return;
            }
            for (var i = 0; i < num; i++)
            {
                var evt = s_DeferredTileEvents[i];
                if(evt.m_Instance == null)
                    continue;
                if ((evt.m_EventType & evtType) == evtType && filter(evt.m_Instance))
                    output.Add(evt.m_Instance);
            }
        }

        
        /// <summary>
        /// Remove a single event from the Tile events queue
        /// </summary>
        /// <param name="eventType">the type of event (trigger/save).</param>
        /// <param name="instance">the tile instance that originally sent the event</param>
        /// <returns>true if successful</returns>
        /// <remarks>To remove a tile from the events list use TileEventType.Both</remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        public static bool RemoveEvent(TileEventType eventType, TilePlusBase instance)
        {
            
            //any events at all for this instance?
            if (!s_DeferredEventsTracker.TryGetValue(instance.Id, out var evtType))
                return false; //no

            //handle complete removal of an instance from both types of events.
            if (eventType == TileEventType.Both)
            {
                //Remove all events for this instance (should not be more than two. Warning if that happens)1
                var instances = s_DeferredTileEvents.FindAll(evt => evt.m_Instance == instance);
                var nInstances = instances.Count;
                if (nInstances == 0)
                    return false; //should not happen
                if(nInstances >= 2)
                    TpLogWarning($"Too many event instances for TPT tile on map {instance.ParentTilemap} at pos {instance.TileGridPosition.ToString()}");
                
                //remove all the instances
                for (var x = 0; x < nInstances; x++)
                {
                    var inst = instances[x];
                   //order is important here: release instance then remove from List
                    S_DeferredEvent_Pool.Release(inst);
                    s_DeferredTileEvents.Remove(inst);
                }

                //decrement the two counts. Note that even though 'Both' was specified, we don't know if there were two events for this instance. Have to check.
                if ( (evtType & TileEventType.TriggerEvent)  == TileEventType.TriggerEvent)  //if a trigger evt was pending, remove it from the count.
                {
                    s_NumTriggerEvents--;
                    if (s_NumTriggerEvents < 0)
                        s_NumTriggerEvents = 0;
                }

                if ((evtType & TileEventType.SaveDataEvent) != TileEventType.SaveDataEvent) //similar for savedata evt.
                    return true;
                s_NumSaveEvents--;
                if (s_NumSaveEvents < 0)
                    s_NumSaveEvents = 0;

                return true;
            }

             
            //handle removal of only ONE type of event
            var evt = evtType ^ eventType; //ie if there are both trigger (bit 1) and a save (bit 0) events, reset only the removed bit.
            if (evt == 0)                  //if no event left
                s_DeferredEventsTracker.Remove(instance.Id);
            else //restore modified evt
                s_DeferredEventsTracker[instance.Id] = evt;
        
            var index = s_DeferredTileEvents.FindIndex(evtX => evtX.m_EventType == eventType && evtX.m_Instance == instance);
            if (index == -1)
                return false; //should not happen

            
            //order is important here: release instance then remove from List
            S_DeferredEvent_Pool.Release(s_DeferredTileEvents[index]);
            s_DeferredTileEvents.RemoveAt(index);

            
            if (eventType == TileEventType.TriggerEvent)
            {
                s_NumTriggerEvents--;
                if (s_NumTriggerEvents < 0)
                    s_NumTriggerEvents = 0;
            }
            else
            {
                s_NumSaveEvents--;
                if (s_NumSaveEvents < 0)
                    s_NumSaveEvents = 0;
            }
            return true;
        }

        /// <summary>
        /// clear all tile events
        /// </summary>
        public static void ClearQueuedTileEvents()
        {
            s_DeferredEventsTracker.Clear();
            
            //new
            var num = s_DeferredTileEvents.Count;
            for(var i = 0; i < num; i++)
                S_DeferredEvent_Pool.Release(s_DeferredTileEvents[i]);
            
            s_DeferredTileEvents.Clear();
            s_NumSaveEvents    = 0;
            s_NumTriggerEvents = 0;
        }
        
        /// <summary>
        /// Pool for DeferredTileEvents
        /// </summary>
        #if ODIN_INSPECTOR
        [ShowInInspector,ReadOnly]
        #endif
        internal static readonly ObjectPool<DeferredTileEvent> S_DeferredEvent_Pool =
            new(() => new DeferredTileEvent(),
                null,
                evt => evt.m_Instance = null);
    }
}
