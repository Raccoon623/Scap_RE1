using System.Collections;
using UnityEngine;

namespace TilePlusCommon
{
    /// <summary>
    /// This is a simple, really simple, sprite animator.
    /// It just runs a coroutine which swaps sprites at
    /// the interval set in the inspector. 
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SimpleSpriteAnimator : MonoBehaviour
    {
        /// <summary>
        /// time between sprites
        /// </summary>
        [Header("Time between sprites (seconds)")]
        [Tooltip("Amount of time between sprites")]
        public float m_Interval = 0.5f;

        /// <summary>
        /// sprites to animate
        /// </summary>
        [Header("At least two sprites")]
        public Sprite[] m_Sprites;

        private SpriteRenderer spriteRenderer;
        private int            nSprites;
        private WaitForSeconds timer;
        private Coroutine      animator;

        private void Start()
        {
            if ((nSprites = m_Sprites.Length) < 2)
            {
                Debug.LogError($"AnimatedSprite at {transform.position.ToString()} has nothing to animate!");
                return;
            }

            spriteRenderer = GetComponent<SpriteRenderer>();
            timer          = new WaitForSeconds(m_Interval);
            animator = StartCoroutine(_animate());
        }

        /// <summary>
        /// Stop animating
        /// </summary>
        public void StopAnimation()
        {
            if(animator != null)
                StopCoroutine(animator);
            animator = null;
        }


        private void OnDisable()
        {
            StopAnimation();
        }

        /// <summary>
        /// start animation
        /// </summary>
        public void StartAnimation()
        {
            // ReSharper disable once ConvertIfStatementToNullCoalescingAssignment
            if (animator == null)
                animator = StartCoroutine(_animate());
        }

        /// <summary>
        /// Property shows if animating or not.
        /// </summary>
        public bool IsAnimating => animator != null;

        private IEnumerator _animate()
        {
            var index = 0;
            while (true)
            {
                if (spriteRenderer == null || spriteRenderer.sprite == null)
                    yield return null;
                spriteRenderer!.sprite = m_Sprites[index++];
                if (index >= nSprites)
                    index = 0;
                yield return timer;
            }

            
            // ReSharper disable once IteratorNeverReturns
        }
    }
}
