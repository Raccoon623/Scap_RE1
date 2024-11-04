using System;
using System.Collections.Generic;
using UnityEngine;
// ReSharper disable MissingXmlDoc

namespace TilePlusDemo
{
    /// <summary>
    /// used to describe a collision or a trigger.
    /// </summary>
    [Flags]
    public enum HitType2D
    {
        CollisionEnter = 1,
        CollisionExit  = 2,
        CollisionStay  = 4,
        TriggerEnter   = 8,
        TriggerExit    = 16,
        TriggerStay    = 32,
        None           = 0,
        All            = 512,
        Collision      = CollisionEnter | CollisionExit | CollisionStay,
        Trigger        = TriggerEnter   | TriggerExit   | TriggerStay,
        IsExit         = TriggerExit    | CollisionExit,
        IsEntry        = CollisionEnter | TriggerEnter,
        IsStay         = CollisionStay  | TriggerStay
        
        
    }
    
    public interface ICollidableTile
    {
        /// <summary>
        /// Target for being informed about a collision.
        /// </summary>
        /// <param name="hitType">the type of hit (enter, exit, stay for Collision or Trigger)</param>
        /// <param name="sourcePosition">The world position of what hit us</param>
        /// <param name="collisionData">the original collision data</param>
        /// <param name="triggerData">the original trigger data</param>
        void OnCollisionOrTriggerHit(HitType2D hitType, Vector3 sourcePosition, Collision2D collisionData, Collider2D triggerData);

        /// <summary>
        /// Used to specify what type of trigger and collider hits are acceptable.
        ///</summary>
        /// <remarks>
        /// NOTE implementation can return null to indicate it doesn't want to be pinged.
        /// Although then there's no reason to use this subsystem so...
        /// See TilePlusCollidableBase.cs
        /// </remarks>
        HashSet<HitType2D> AcceptableCollisionsTriggers { get; }

        /// <summary>
        /// Return a Bounds for the tile.
        /// </summary>
        /// <remarks>
        /// See implementation in TilePlusCollidableBase.cs
        /// </remarks>
        Bounds TileColliderBounds { get; }
        
    }



}
