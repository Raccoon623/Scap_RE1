using System.Collections.Generic;
#if TPT_DOTWEEN || DOTWEEN
using DG.Tweening;
#endif
using TilePlus;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TilePlusDemo
{
    /// <summary>
    /// This is a simple example that shows how you can place
    /// a TilePlus tile, edit its parameters in the editor
    /// inspector, and NOT effect the original tile asset.
    /// EditingView the force parameters on placed tiles and see what
    /// happens.
    /// </summary>
    [CreateAssetMenu(fileName = "TileCollision.asset", menuName = "TilePlus/Demo/Create TileCollision", order = 1000)]
    public class TileCollision : TilePlusCollidableBase
    {
        /// <summary>
        /// Apply force?
        /// </summary>
        [Tooltip("Apply force to the ball on a collision?")][TptShowField]
        public bool    m_ApplyForce  = true;
        /// <summary>
        /// Force to apply
        /// </summary>
        [Tooltip("force to apply to the ball on a collision")][TptShowField]
        public Vector2 m_ForceVector = new Vector3(0, 1);

        /// <summary>
        /// Effect prefab
        /// </summary>
        [Tooltip("A prefab to show an effect when the Tile collides")]
        [TptShowObjectField(typeof(GameObject))]
        public GameObject m_EffectPrefab;
        
        /// <summary>
        /// SFX audiosource
        /// </summary>
        [Tooltip("An AudioSource for some noise when Tile collides")]
        [TptShowObjectField(typeof(AudioSource))]
        public AudioSource m_AudioSource;

        //what types of hits we want sent to OnCollisionOrTriggerHit
        private static readonly HashSet<HitType2D> s_DesiredHitTypes = new HashSet<HitType2D>() {HitType2D.CollisionEnter};
        /// <summary>
        /// Per ICollidableTile interface, supplies information about which hits we want to get.
        /// </summary>
        public override         HashSet<HitType2D> AcceptableCollisionsTriggers => s_DesiredHitTypes;

        /// <summary>
        /// Per ICollidableTile interface, recieves Collision and Trigger hit notifications.
        /// </summary>
        /// <param name="hitType">the type of hit (enter, exit, stay for Collision or Trigger)</param>
        /// <param name="sourcePosition">The world position of what hit us</param>
        /// <param name="collisionData">the original collision data</param>
        /// <param name="triggerData">the original trigger data</param>
        public override void OnCollisionOrTriggerHit(HitType2D hitType, Vector3 sourcePosition, Collision2D collisionData, Collider2D triggerData)
        {
            if (hitType != HitType2D.CollisionEnter) //is it the type we want? A bit redundant.
                return;
            if (collisionData == null)
                return;
            
            var pos    = TileWorldPosition; //where's the tile in world space
            var source = collisionData.gameObject; //what GameObject collided with the tile?
            if (source == null)
                return;
                
            if (m_ApplyForce) //from bool serialized field
            {
                var rb = source.GetComponent<Rigidbody2D>();
                rb.AddForce(m_ForceVector);
                rb.constraints = RigidbodyConstraints2D.None;
            }

            //play a sound and instantiate a prefab.
            if(m_AudioSource != null)
            {
                m_AudioSource.pitch = Random.Range(0.9f, 1.5f);
                m_AudioSource.Play();
            }
            
            if (m_EffectPrefab != null)
                SpawningUtil.SpawnPrefab(m_EffectPrefab, pos, null, "", false, true);
            
            #if TPT_DOTWEEN || DOTWEEN
            if(!scalingIsOn)
                ScalingSequence();
            #endif

        }

        #if TPT_DOTWEEN || DOTWEEN
        
        private readonly DOTweenAdapter dtAdapter = new();

        private void OnDisable()
        {
            dtAdapter.OnDisableHandler();
        }

        private bool scalingIsOn;
        
        /// <summary>
        /// Duration of the sequence
        /// </summary>
        [TptShowField()] [Tooltip("Duration of the tween/sequence")]
        public float m_Duration = 1f;
        
        /// <summary>
        /// The initial size for the sprite
        /// </summary>
        [TptShowField()]
        [Tooltip("Start size for the sprite when tweening scale")]
        public Vector3 m_StartSize = Vector3.one;
        /// <summary>
        /// The end size for the sprite
        /// </summary>
        [TptShowField()] 
        [Tooltip("End size for the sprite when tweening scale")]
        public Vector3 m_EndSize = new Vector3(2, 2, 1);

        /// <summary>
        /// Easing type
        /// </summary>
        [TptShowEnum()]
        [Tooltip("Select easing type")]
        public Ease m_EaseType = Ease.Linear;
        
        private void ScalingSequence()
        {
            scalingIsOn = true;
            //create a sequence
            var sequence = DOTween.Sequence();
            sequence.Append(DOTween.To(() => SizeProp, x => SizeProp = x, m_EndSize,   m_Duration).SetEase(m_EaseType));
            sequence.Append(DOTween.To(() => SizeProp, x => SizeProp = x, m_StartSize, m_Duration).SetEase(m_EaseType));
            sequence.SetLoops(-1, LoopType.Restart);
            dtAdapter.AddSequence(sequence); //add sequence to the per-tile controller
            sequence.Play();
        }


        /// <summary>
        /// The 'getter/setter' as required by DOTween.
        /// </summary>
        private Vector3 SizeProp
        {
            get => TileUtil.GetTransformScale(m_ParentTilemap!, m_TileGridPosition);
            set => TileUtil.SetTransform(m_ParentTilemap!, m_TileGridPosition, Vector3.zero, Vector3.zero, value);
        }
        
        
        
        #endif

        //override TilePlusBase implementation so collider is always a Grid collider.
        ///<inheritdoc />
        public override ColliderMode TileColliderMode => ColliderMode.Grid;

        
        #if UNITY_EDITOR
        ///<inheritdoc />
        public override string Description => "Tile supporting collision";
        
        #endif
    }
}
