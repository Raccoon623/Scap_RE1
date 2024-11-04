using System.Collections.Generic;
using TilePlus;
using UnityEngine;


namespace TilePlusDemo
{
    /// <summary>
    ///Similar to TileCollision, this will animate when hit.
    /// Note that since c# doesn't have multiple inheritance, we
    /// inherit from TpAnimatedTile and include the ICollidable interface
    /// elements.
    /// </summary>
    [CreateAssetMenu(fileName = "AnimatedTileCollisionExample.asset", menuName = "TilePlus/Demo/Create AnimatedTileCollisionExample", order = 1000)]
    public class AnimatedTileWithCollision : TpAnimatedTile, ICollidableTile
    {
        /// <summary>
        /// Apply force on a collision?
        /// </summary>
        [Tooltip("Apply force to the ball on a collision?")][TptShowField]
        public bool    m_ApplyForce  = true;
        /// <summary>
        /// Force to apply on a collision
        /// </summary>
        [Tooltip("force to apply to the ball on a collision")][TptShowField]
        public Vector2 m_ForceVector = new Vector3(0, 1);
        /// <summary>
        /// Audio source for collision SFX
        /// </summary>
        [Tooltip("An AudioSource for some noise when Tile collides")]
        [TptShowObjectField(typeof(AudioSource))]
        public AudioSource m_AudioSource;

       
        //what types of hits we want sent to OnCollisionOrTriggerHit
        private static readonly HashSet<HitType2D> s_DesiredHitTypes = new() {HitType2D.CollisionEnter};
        /// <summary>
        /// Per ICollidableTile interface, supplies information about which hits we want to get.
        /// </summary>
        public HashSet<HitType2D> AcceptableCollisionsTriggers => s_DesiredHitTypes;

        /// <summary>
        /// Per ICollidableTile interface, recieves Collision and Trigger hit notifications.
        /// </summary>
        /// <param name="hitType">the type of hit (enter, exit, stay for Collision or Trigger)</param>
        /// <param name="sourcePosition">The world position of what hit us</param>
        /// <param name="collisionData">the original collision data</param>
        /// <param name="triggerData">the original trigger data</param>
        public void OnCollisionOrTriggerHit(HitType2D hitType, Vector3 sourcePosition, Collision2D collisionData, Collider2D triggerData)
        {
            if (hitType != HitType2D.CollisionEnter) //is it the type we want? A bit redundant.
                return;
            if(collisionData == null)
                return;
            
            var source = collisionData.gameObject; //what GameObject collided with the tile?
            if (source == null)
                return;

            if (m_ApplyForce) //from bool serialized field
            {
                var rb = source.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.AddForce(m_ForceVector);
                    rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                }
            }
            
            if (IsOneShotWaitingToRewind) //this means an animation is already running but is waiting to complete.
                return;
            
            //play a sound
            if(m_AudioSource != null)
            {
                m_AudioSource.pitch = Random.Range(0.9f, 1.5f);
                m_AudioSource.Play();
            }
            
            
            //these animations are (or should be) set to one-shot with rewind.
            ActivateAnimation(true);
            
            /*else if (AnimationIsRunning) //already running. Want to restart, but this isn't supported directly.
            {
                ActivateAnimation(false, 0); //pauses it
                //this next line restarts the animation on the next frame.
                TpLib.DelayedCallback(this,()=>ActivateAnimation(true),"AnimTileWColl:delayed restart");
            }*/
            /*
            //if an animation is already running, just return
            if (AnimationIsPaused )
                PauseAnimation(false);
            else
                ActivateAnimation(true);
        */
        }

       
        
        /// <summary>
        /// override TilePlusBase implementation so collider is always a grid collider.
        /// </summary>
        public override ColliderMode TileColliderMode => ColliderMode.Grid;
        
        

        /*-----------------------------------------------------------
        *  THE FOLLOWING is pasted in from TilePlusCollidableBase
        *-----------------------------------------------------------
        */
        
        
        /// <summary>
        /// Get a collider rect for this tile based on the size of the sprite or on
        /// the cell size depending on m_UseSpriteBounds.
        /// This is overrideable but this is probably fine for most use cases.
        /// Note--> This property should not return an empty Bounds.
        /// </summary>
        public Bounds TileColliderBounds
        {
            get
            {
                if (boundsIsCached)
                    return cachedTileColliderBounds;

                boundsIsCached = true;
                var spBoundsSize = sprite.bounds.size;
                var map          = ParentTilemap;
                var cellSize     =  map != null ?  map.cellSize : Vector3.one;

                //is the sprite bigger than the cell?
                //If so, make a bounds based on the sprite size, else use grid cell size
                var size = (spBoundsSize.x > cellSize.x || spBoundsSize.y > cellSize.y) ? spBoundsSize : cellSize;

                return cachedTileColliderBounds = new Bounds(TileGridPosition, size);
            }
        }

        private bool   boundsIsCached;
        private Bounds cachedTileColliderBounds;

        
        #if UNITY_EDITOR

        
        /// <summary>
        /// Custom description for this tile (in editor)
        /// </summary>
        public override string Description => "Animated Tile supporting collision";

        #endif
    }
}
