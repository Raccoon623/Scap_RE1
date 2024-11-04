using System.Collections;
using TilePlus;
using TilePlusCommon;
using UnityEditor;
using UnityEngine;

// ReSharper disable MissingXmlDoc

namespace TilePlusDemo
{
    /// <summary>
    /// A subclass of TpLink for the Player character's prefab.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(SimpleSpriteAnimator))]
    public class TdDemoPlayerPrefabLink : TpSpawnLink
    {
        [Tooltip("How often the prefab blinks when hit")] 
        public  float          m_BlinkInterval = 0.2f;
        [Tooltip("Initial # of lives (also used when game over and you reset)")]
        public int m_InitialLives;
        /// <summary>
        /// Damping factor for follow easing
        /// </summary>
        [Tooltip("Movement speed")]
        public float m_Speed = 1f;
        /// <summary>
        /// Error tolerance for final position
        /// </summary>
        [Tooltip("Error tolerance")]
        public float m_Tolerance = 0.1f;
        
        private SpriteRenderer       spriteRenderer;
        private SimpleSpriteAnimator animator;
        private WaitForSeconds       wait;
        private Coroutine            task;
        private int                  currentLives;
        private bool                 isMoving;
        private Vector3              destination;
        private float                moveStartTime;

        
        public bool PrefabMoving => isMoving;
        
        /// <summary>
        /// Get or Set lifetime. When set the animator runs
        /// </summary>
        public int Lives
        {
            set
            {
                if (currentLives > value) //ie a decrease
                    task = StartCoroutine(DoFlash());
                currentLives = value;
                
                animator.StartAnimation();
            }
            get => currentLives;
        }
        
        
        
        

        /// <inheritdoc />
        public override void           OnTpSpawned()
        {
            base.OnTpSpawned();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator       = GetComponent<SimpleSpriteAnimator>();
            wait           = new WaitForSeconds(m_BlinkInterval);
            currentLives   = m_InitialLives; //current lives gets overwritten during startup and during operation.
        }

        public void Move(Vector3 newPosition)
        {
            destination   = newPosition;
            isMoving      = true;
            moveStartTime = Time.time;
        }

        public void GameOver()
        {
            animator.StopAnimation();
        }

        public void ResetLives()
        {
            Lives = m_InitialLives;
        }
        
        /// <inheritdoc />
        public override void OnTpDespawned()
        {
            base.OnTpDespawned();
            if(task != null)
                StopCoroutine(task);
        }

        private void Update()
        {
            if(!isMoving)
                return;
            
            
            if (m_Speed <= 0.01f)
                m_Speed = 0.1f;
            var theMoveSoFar = (Time.time - moveStartTime) * m_Speed;
            
            //simple move with Lerping
            var pos       = transform.position;
            var targetPos = destination; //target position
            targetPos.z = -1;
            pos.z       = -1;
            //how far apart are these positions
            var diffX = Mathf.Abs(pos.x - targetPos.x); 
            var diffY = Mathf.Abs(pos.y - targetPos.y);
            
            //are we close enough to stop?
            if (diffX < m_Tolerance && diffY < m_Tolerance)
            {
                isMoving           = false;
                transform.position = targetPos;
                return; //yes
            }

            //keep moving
            var lerpedPosX = Mathf.Lerp(pos.x, targetPos.x, theMoveSoFar);
            var lerpedPosY = Mathf.Lerp(pos.y, targetPos.y, theMoveSoFar);
            transform.position = new Vector3(lerpedPosX, lerpedPosY, -1);
        }

        private IEnumerator DoFlash()
        {
            for (var i = 0; i < 4; i++)
            {
                spriteRenderer.color = Color.red;
                yield return wait;
                // ReSharper disable once Unity.InefficientPropertyAccess
                spriteRenderer.color = Color.white;
                yield return wait;
                
            }
            spriteRenderer.color = Color.white; //probably redundant!
        }
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Editor for TpLink. 
    /// </summary>
    [CustomEditor(typeof(TdDemoPlayerPrefabLink))]
    public class TdDemoPlayerPrefabLinkEditor : TpSpawnLinkEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            var instance = target as TdDemoPlayerPrefabLink;
            if (instance == null)
                return;
            
            EditorGUILayout.HelpBox($"IsMoving {instance.PrefabMoving}",MessageType.None);
            
        }
    }
    #endif
}
