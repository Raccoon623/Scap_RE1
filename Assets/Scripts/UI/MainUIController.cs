﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Platformer.UI
{
    /// <summary>
    /// A simple controller for switching between UI panels.
    /// </summary>
    public class MainUIController : MonoBehaviour
    {
        public GameObject[] panels;

        public void SetActivePanel(int index)
        {
            for (var i = 0; i < panels.Length; i++)
            {
                var active = i == index;
                var g = panels[i];
                if (g.activeSelf != active) g.SetActive(active);
            }
        }

        public void QuitGame()
        {
            Debug.Log("Exiting game..."); // This will display in the console when exiting in the editor.

            // Quit the application.
            Application.Quit();
        }

        void OnEnable()
        {
            SetActivePanel(0);
        }
    }
}
