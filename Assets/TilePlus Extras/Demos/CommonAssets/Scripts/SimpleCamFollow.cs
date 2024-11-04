using UnityEngine;

namespace TilePlusCommon
{
    /// <summary>
    /// A very simple camera follow script. 
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class SimpleCamFollow : MonoBehaviour
    {
        /// <summary>
        /// what to follow
        /// </summary>
        [Tooltip("The target to follow")]
        public  Transform m_TargetToFollow;
        /// <summary>
        /// Damping factor for follow easing
        /// </summary>
        [Tooltip("Damping factor - higher is more jittery")]
        public  float     m_Damping   = 0.9f;
        /// <summary>
        /// Error tolerance for final position
        /// </summary>
        [Tooltip("Error tolerance")]
        public  float     m_Tolerance = 0.1f;

        private void Update()
        {
            if (m_TargetToFollow == null) //if there's no transform reference, we're waiting for TdDemoGameController to add the reference.
                return;
            
            //simple cam follow with Lerping
            var camPos       = transform.position;  //current position
            var targetPos = m_TargetToFollow.position; //target position
            //how far apart are these positions
            var diffX     = Mathf.Abs(camPos.x - targetPos.x); 
            var diffY     = Mathf.Abs(camPos.y - targetPos.y);
            
            //are we close enough to stop?
            if (diffX < m_Tolerance && diffY < m_Tolerance)
                return; //yes
            //keep moving
            var lerpedPos = Vector3.Lerp(camPos, targetPos, Time.deltaTime * m_Damping);
            lerpedPos.z  = -10;
            transform.position = lerpedPos;
        }
    }

}
