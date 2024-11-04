using System.Collections.Generic;
using TilePlus;
using UnityEngine;

namespace TilePlusDemo
{
    /// <summary>
    /// An example collidable tile. Unsupported.
    /// </summary>
    public class TilePlusCollidableBase : TilePlusBase, ICollidableTile
    {

        /// <summary>
        /// Used to specify what type of trigger and collider hits are acceptable.
        /// Subclasses can return other info using different initializers.
        /// NOTE: this is designed to work with the component TilemapCollisionDirector,
        /// which should be attached to any tilemap for which you want to get collisions.
        /// 
        ///</summary>
        /// <remarks>
        /// For example:
        /// <code>
        ///  ...in a subclass...
        ///  {HitType2D.Collision};  for all collisions.
        ///  {HitType2D.All}; for all collisions and all triggers.
        ///  {HitType2D.CollisionEnter | HitType2D.CollisionExit}; for just collisionEnter and Exit 
        /// </code>
        ///
        /// for this base class (which you'll ALWAYS subclass) ALL hit types are accomodated;
        /// </remarks>
        public virtual HashSet<HitType2D> AcceptableCollisionsTriggers => s_DesiredHitTypes;

        //note that this is static so there's only one hashtable for all tiles of this class.
        //important to save initialization time. In a subclass use your own hashset since 
        //this can't be virtual AND static
        private static readonly HashSet<HitType2D> s_DesiredHitTypes = new HashSet<HitType2D>() {HitType2D.All};



        /// <summary>
        /// Get a collider rect for this tile based on the size of the sprite or on
        /// the cell size depending on m_UseSpriteBounds.
        /// This is overrideable but this is probably fine for most use cases.
        /// Note--> This property should not return an empty Bounds.
        /// Also--> there's no caching. This accomodates changes in the size of the sprite during Play.
        /// </summary>
        public virtual Bounds TileColliderBounds =>  TileUtil.GetTrueBoundsForTileSprite(m_ParentTilemap, m_TileGridPosition);


        //default implementation does nothing.
        /// <summary>
        /// Target for being informed about a collision or trigger.
        /// -------------NOTE----------
        /// this default implementation does nothing 
        /// SO- If your subclass overrides this there's no
        /// need to call base.OnCollisionHit.
        /// </summary>
        /// <param name="hitType">(enter, exit, stay for Collision or Trigger)</param>
        /// <param name="sourcePosition">The world position of what hit us</param>
        /// <param name="collisionData">Collision2D instance, valid if hitType is CollisionEnter,Exit,Stay</param>
        /// <param name="triggerData">Collider2D instance, valid if hitType is TriggerEnter,Exit,Stay</param>
        public virtual void OnCollisionOrTriggerHit(HitType2D hitType, Vector3 sourcePosition, Collision2D collisionData, Collider2D triggerData)
        {
        }
    }
}
