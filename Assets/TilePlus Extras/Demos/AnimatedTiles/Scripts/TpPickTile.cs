using TilePlus;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TilePlusDemo
{
    /// <summary>
    /// An example of how to control a tile from a mouse-click.
    /// </summary>
    public class TpPickTile : MonoBehaviour
    {
        /// <summary>
        ///The tilemap
        /// </summary>
        [Tooltip("The tilemap")]
        public Tilemap m_Tilemap;

        /// <summary>
        /// The camera
        /// </summary>
        [Tooltip("The camera")]
        public Camera m_Camera;

        /// <summary>
        /// Delay between moves used for debouncing
        /// </summary>
        [Tooltip("Delay between moves for debouncing, min=0.2")]
        public float m_Delay = 0.2f;

        //used for debouncing
        private float lastTime;

        //used for debouncing
        private float timeAccum;

        private void Update()
        {
            // +debounce
            timeAccum += Time.deltaTime;

            if (!Input.GetMouseButton(0))
                return;

            if (m_Delay < 0.2f)
                m_Delay = 0.2f;
            if (timeAccum < (lastTime + m_Delay))
                return;
            lastTime = timeAccum;

            // -debounce
            
            
            //get the mouse position
            var screenPos = Input.mousePosition;
            //test to ensure that it's within the visible area
            if (screenPos.x < 0 ||
                screenPos.y < 0 ||
                screenPos.x > Screen.width ||
                screenPos.y > Screen.height)
                return;
            
            //get the tilemap grid position
            var worldPos = m_Camera.ScreenToWorldPoint(screenPos);
            var gridPos = m_Tilemap.WorldToCell(worldPos);

            //is there a tile there?
            var tile = TpLib.GetTile(m_Tilemap, gridPos);
            if (tile != null)
            {
                //toggle animation on/off for each of the two types of tiles.
                if (tile is TpAnimatedTile tpa) //this is a TpAnimatedTile
                    tpa.ActivateAnimation(!tpa.AnimationIsRunning);
                else if (tile is TpFlexAnimatedTile tpf) //this is a TpFlexAnimatedTile.
                    tpf.ActivateAnimation(!tpf.AnimationIsRunning);
            }
            else
            {
                Debug.Log($"No tile at {gridPos} ");
            }


        }
    }
}
