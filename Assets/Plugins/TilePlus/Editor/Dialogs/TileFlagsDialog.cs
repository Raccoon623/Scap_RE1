// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 04-11-2024
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 04-11-2024
// ***********************************************************************
// <copyright file="TileFlagsDialog.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using TilePlus.Editor.Painter;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;


#nullable enable

namespace TilePlus.Editor
{
    /// <summary>
    /// Editor window for Change TileFlags menu command.
    /// </summary>
    public class TileFlagsDialog : EditorWindow
    {
        /// <summary>
        /// A return value from this window
        /// </summary>
        public readonly struct TileFlagsDialogResult
        {
            /// <summary>
            /// new color flag value
            /// </summary>
            public readonly bool m_ColorFlagValue;

            /// <summary>
            /// new transform flag value
            /// </summary>
            public readonly bool m_TransformFlagValue;

            /// <summary>
            /// indicates that application of the new flag values was approved by user.
            /// </summary>
            public readonly bool m_Apply;

            /// <summary>
            /// Ctor
            /// </summary>
            /// <param name="colorFlag">color flag value</param>
            /// <param name="transformFlag">transform flag value</param>
            /// <param name="apply">if true user clicked CONTINUE</param>
            public TileFlagsDialogResult(bool colorFlag, bool transformFlag, bool apply)
            {
                m_ColorFlagValue     = colorFlag;
                m_TransformFlagValue = transformFlag;
                m_Apply              = apply;
            }
        }
        
        private TpToggleLeft?         tFlagToggle;
        private TpToggleLeft?         cFlagToggle;
        private TileFlagsDialogResult result;
        private Label?                infoLabel;
        
        
        /// <summary>
        /// Callback to get the flag values when the continue button is clicked.
        /// </summary>
        private Action<TileFlagsDialogResult>? callback;

        
        internal void Setup(Action<TileFlagsDialogResult>? cb, Tile? tileToMod =null, int numTiles = 1)
        {
            this.callback     = cb;
            this.minSize      = new Vector2(256,  128);
            this.maxSize      = new Vector2(256 , 256);
            this.titleContent = new GUIContent("Tile Flags");
            if (tileToMod != null)
            {
                tFlagToggle!.value = (tileToMod.flags & TileFlags.LockTransform) != 0;
                cFlagToggle!.value = (tileToMod.flags & TileFlags.LockColor) != 0;
                infoLabel!.text    = "Current Flags value is displayed <color=red><b>only</b></color> when a single tile was selected in the Project.";
            }
            else //multiple-selection
                infoLabel!.text = $"Multiple tiles ({numTiles}), checkboxes don't reflect current flag values: <color=red><b>be careful!</b></color>";
            this.ShowModal();
            this.Focus();
        }
        

        internal void CreateGUI()
        {
            rootVisualElement.Add(new Label("Select TileFlags and click 'Continue' or Exit to cancel"){  style =
                                                                                                      {
                                                                                                          whiteSpace = WhiteSpace.Normal
                                                                                                      }});
            
            tFlagToggle = new TpToggleLeft("Transform");
            cFlagToggle = new TpToggleLeft("Color");
            
            rootVisualElement.Add(new TpSpacer(20,10));
            rootVisualElement.Add(cFlagToggle);
            rootVisualElement.Add(tFlagToggle);
            rootVisualElement.Add(new TpSpacer(20, 10));
            var continueButton = new Button(Continue){text = "Continue"};
            
            rootVisualElement.Add(continueButton);
            rootVisualElement.Add(new TpSpacer(10, 10));
            rootVisualElement.Add(new Button(Exit){text = "Exit"});
            rootVisualElement.Add(new TpSpacer(20, 10));
            infoLabel = new Label("---"){  style = { whiteSpace = WhiteSpace.Normal }};
            rootVisualElement.Add(infoLabel); 
            rootVisualElement.Add(new TpSpacer(20, 10));
            return;

            void Exit()
            {
                result = new TileFlagsDialogResult(false, false, false);
                this.Close();
            }


            void Continue()
            {
                result = new TileFlagsDialogResult(cFlagToggle.value, tFlagToggle.value, true);
                this.Close();
                    
            }
        }

        private void OnDisable()
        {
            callback?.Invoke(result);
        }
    }
}
