using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using TilePlus;

// ReSharper disable MissingXmlDoc

namespace TilePlusDemo
{
    /// <summary>
    /// Attach this component to a Tilemap's game object.
    /// The tilemap needs a TilemapCollider2D
    /// It'll intercept collisions and triggers, and send them to
    /// Tiles which implement ICollidableTile, typically based
    /// on TilePlusCollidableBase.
    /// 
    /// NOTE: this was created for demo purposes.
    /// It's not thorougly tested for all possible combinations of hit types,
    /// and collision detection won't be accurate if sprites are placed
    /// too-closely together. Triggers haven't been tested at all.
    /// </summary>
    /// <remarks>Unsupported</remarks>
    
    [RequireComponent(typeof(TilemapCollider2D))]
    [RequireComponent(typeof(Tilemap))]
    public class TilemapCollisionDirector : MonoBehaviour
    {
        #region publics
        [Tooltip("Spam the console")]
        public bool m_ReportToConsole;
        [Header("Enable Events for TilePlus tiles")] 
        public bool m_CollisionEnter;
        public bool m_CollisionExit, m_CollisionStay;
        public bool m_TriggerEnter,  m_TriggerExit, m_TriggerStay;
        [Header("Padding for collision source bounds")]    
        [Tooltip("Bounds is expanded by this number * (tilemap cell size)")]
        public float m_BoundsSizePadding = 1f;

        [Header("Filter multiple collisions")]
        [Tooltip("Filter out multiple Enters before a matching Exit")]
        public bool m_FilterEnter;
        [Tooltip("Filter out multiple Stays after a matching Enter")]
        public bool m_FilterStay;
        #endregion

        #region privates
        /// <summary>
        /// a value used to ensure a nonzero 'z' for a boundsint used in GetTilesBlock
        /// </summary>
        // ReSharper disable once UnusedMember.Local
        private static readonly Vector3Int s_ZOne = new Vector3Int(0, 0, 1);

        
        private  List<ICollidableTile> collidedTiles = new(16);
        
        /// <summary>
        /// Hashset of tiles entered
        /// </summary>
        private readonly HashSet<int> entered = new(); //tiles that were 'entered'
        /// <summary>
        /// Hashset of tiles stayed
        /// </summary>
        private readonly HashSet<int> stayed = new(); //tiles that were 'stayed'
        /// <summary>
        /// ref to parent tilemap.
        /// </summary>
        private          Tilemap         map;
        /// <summary>
        /// cached tilemap cell size
        /// </summary>
        private          Vector3         cellSize;
      

        #endregion

        public void ResetDirector()
        {
            collidedTiles.Clear();
            entered.Clear();
            stayed.Clear();
        }
        
        private void Start()
        {
            map      = GetComponent<Tilemap>();
            cellSize = map.cellSize;
        }
 

        private void OnCollisionEnter2D(Collision2D collisionData)
        {
            if (m_CollisionEnter)
                SendToTile(HitType2D.CollisionEnter, collisionData, null);
        }

        private void OnCollisionExit2D(Collision2D collisionData)
        {
            if (m_CollisionExit)
                SendToTile(HitType2D.CollisionExit, collisionData, null);
        }

        private void OnCollisionStay2D(Collision2D collisionData)
        {
            if (m_CollisionStay)
                SendToTile(HitType2D.CollisionStay, collisionData, null);
        }


        private void OnTriggerEnter2D(Collider2D triggerData)
        {
            if (m_TriggerEnter)
                SendToTile(HitType2D.TriggerEnter, null, triggerData);
        }

        private void OnTriggerExit2D(Collider2D triggerData)
        {
            if (m_TriggerExit)
                SendToTile(HitType2D.TriggerExit, null, triggerData);
        }

        private void OnTriggerStay2D(Collider2D triggerData)
        {
            if (m_TriggerStay)
                SendToTile(HitType2D.TriggerStay, null, triggerData);
        }


        private void SendToTile(HitType2D hitType, Collision2D collisionData, Collider2D triggerData)
        {
            /* get bounds for the source - the moving thing
             * increase by N units
             * get tiles block in that bounds
             * look for ICollidableTile tiles in that block
             * examine the Bounds for those tiles for intersection.
             */
            //this is the collider from the GO that might have hit a tile
            
            var sourceCollider = (hitType & HitType2D.Collision)!=0 ? collisionData.collider : triggerData;

            var bounds     = sourceCollider.bounds; //bounds of the collision
            var sourcePosition = sourceCollider.gameObject.transform.position;
            if (m_BoundsSizePadding > 0)
                bounds.Expand(cellSize * m_BoundsSizePadding); //expand it by one * padding. 

           
            
            //this next line fills the collidedTiles list with all the tiles (as ICollidableTile) where the bounds
            //from the source collider intersects with the bounds of the tile sprite. 
            //The TileColliderBounds method returns a real-time bounds based on the current sprite size.
            // ReSharper disable once RedundantTypeArgumentsOfMethod
            TpLib.GetAllTilesWithInterface<ICollidableTile>(ref collidedTiles,
                                                            (ict, _) => bounds.Intersects(ict.TileColliderBounds)); 
            
            
            var count = collidedTiles.Count;
            if (count <= 0)
            {
                if(m_ReportToConsole)
                    Debug.Log($"{this}: collision without a target");
                return;
            }
            if(m_ReportToConsole)
                Debug.Log($"{this}: found {count} targets");


            for (var index = 0; index < count; index++)
            {
                var tpb   = collidedTiles[index] as TilePlusBase;
                if(tpb is not ICollidableTile t)
                    continue;
                
                var iId = tpb.GetInstanceID();
                if ( (hitType & HitType2D.IsExit) != 0)
                {
                    if (!entered.Contains(iId))
                        return;
                    entered.Remove(iId);
                    stayed.Remove(iId);
                    t.OnCollisionOrTriggerHit(hitType, sourcePosition, collisionData, triggerData);

                    return;
                }

                if ( (hitType & HitType2D.IsEntry) != 0)
                {
                    if (m_FilterEnter && entered.Contains(iId))
                        return;

                    entered.Add(iId);
                    t.OnCollisionOrTriggerHit(hitType, sourcePosition, collisionData, triggerData);
                    return;
                }

                if ( (hitType & HitType2D.IsStay) != 0)
                {
                    if (m_FilterStay && stayed.Contains(iId))
                        return;
                    stayed.Add(iId);
                    t.OnCollisionOrTriggerHit(hitType, sourcePosition, collisionData, triggerData);
                    return;
                }
            }
        
        }
    }

}
