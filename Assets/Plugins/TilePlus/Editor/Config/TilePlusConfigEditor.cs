// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-20-2023
// ***********************************************************************
// <copyright file="TilePlusConfigEditor.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Editor for TilePlus config scriptable singleton</summary>
// ***********************************************************************

using UnityEditor;
using UnityEngine;


namespace TilePlus.Editor
{
    /// <summary>
    /// Editor for  ScriptableSingleton TilePlusConfig 
    /// </summary>
    public class TilePlusConfigEditorWindow : EditorWindow
    {
        /// <summary>
        /// Open the window
        /// </summary>
        [MenuItem("Tools/TilePlus/Configuration Editor", false, 2)]
        public static void OpenWindow()
        {
            var win = GetWindow(typeof(TilePlusConfigEditorWindow), true, "TilePlus configuration");
            win.minSize      = new Vector2(200, 200);
            win.titleContent = new GUIContent("TilePlus Configuration", TpIconLib.FindIcon(TpIconType.TptIcon));
        }

        private void OnGUI()
        {
            TilePlusConfigView.ConfigOnGUI();
        }        
    }
}
