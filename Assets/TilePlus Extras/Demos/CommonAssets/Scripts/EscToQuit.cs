using UnityEngine;

namespace TilePlusCommon
{
    /// <summary>
    /// Simple component that causes the application to quit when user presses escape key.
    /// </summary>
   public class EscToQuit : MonoBehaviour
    {
        void Update()
        {
            if (Input.GetKey(KeyCode.Escape))
                Application.Quit();
        }
    }
}
