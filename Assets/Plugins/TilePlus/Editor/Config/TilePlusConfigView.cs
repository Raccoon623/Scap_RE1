// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 01-01-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 01-01-2024
// ***********************************************************************
// <copyright file="TilePlusConfigView.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Generates IMGUI view of TilePlusConfig scriptable singleton</summary>
// ***********************************************************************

using TilePlus.Editor;
using UnityEditor;
using UnityEngine;
#nullable enable
namespace TilePlus
{
    /// <summary>
    /// Generate the display for the Configuration window
    /// </summary>
    public static class TilePlusConfigView
    {
        private static class Content
        {
            
            public static readonly GUIContent S_BkspDel           = new GUIContent("Allow Backspace/Delete",           "Allow use of BKSP/DEL in Brush++ Selection Inspector");
            public static readonly GUIContent S_ShowDefaultSelIns = new GUIContent("Show default Selection Inspector", "Show the default Selection Inspector foldout in the Tile+Brush Selection Inspector");
            public static readonly GUIContent S_SafePlay          = new GUIContent("*Safe Play Mode",                  "Check to inhibit use of attributes in Inspectors during Play mode. Click Reload if changed.");
            public static readonly GUIContent S_AutoSave          = new GUIContent("AutoSave",                         "Check to automatically save Scene after editing a TilePlus in the Selection Inspector or TilePlus Utility Window. ");
            public static readonly GUIContent S_RangeMode         = new GUIContent("*Allow Sliders",                   "Uncheck to inhibit sliders for int and float fields. Click Reload if changed.");
            public static readonly GUIContent S_ButtonSizeSelInsp = new GUIContent("*Selection Insp Buttonsize",       "Set Toolbar Button Size for Selection Inspector. Click Reload if changed.");
            public static readonly GUIContent S_ButtonSizeBrInsp  = new GUIContent("*Brush Insp Buttonsize ",          "Set Toolbar Button Size for Brush Inspector. Click Reload if changed.");
            public static readonly GUIContent S_ButtonSizeHelpBox = new GUIContent("Set Toolbar button sizes for Selection and Brush Inspectors");
            public static readonly GUIContent S_DebugMsgHelpBox   = new GUIContent("Debugging messages. Note that in a built app these settings have no effect.");
            public static readonly GUIContent S_InfoMsg           = new GUIContent("*Informational", "Show informational messages. Click Reload if changed.");
            public static readonly GUIContent S_WarningMsg        = new GUIContent("*Warning",       "Show warning messages. Click Reload if changed.");
            public static readonly GUIContent S_ErrorMsg          = new GUIContent("*Error",         "Show error messages. Click Reload if changed.");
            public static readonly GUIContent S_ResetButton       = new GUIContent("Reset to Defaults",                                          "Reset all settings to Defaults, Does RELOAD");
            public static readonly GUIContent S_ReloadButton      = new GUIContent("Reload",                                                     "RELOAD causes a Domain (scripting) reload after a small delay");
            public static readonly GUIContent S_NameSpaces        = new GUIContent("Namespaces",                                                 "Add comma-delimited namespaces to those used by TilePlus inspectors. Click Reload if changed.");
            public static readonly GUIContent S_ConfirmDeletion   = new GUIContent("Confirm Delete",                                             "Confirm tile deletions.");
            public static readonly GUIContent S_AllowPrefabEdit   = new GUIContent("*Allow prefab editing",                                      "Allow editing of TilePlus-created prefabs. Click Reload if changed. Will usually result in prefab corruption.");
            public static readonly GUIContent S_GizmoColor        = new GUIContent("Gizmo color",                                                "Color for Gizmos (Tile+Viewer window)");
            public static readonly GUIContent S_FocusZoom         = new GUIContent("Focus Zoom",                                                 "Zoom for toolbar focus button");
            public static readonly GUIContent S_ShowBrushPos      = new GUIContent("Show Brush Position",                                        "Show/Hide brush position at cursor");
            public static readonly GUIContent S_BrushPosColor     = new GUIContent("Brush Position text color",                                  "Select brush position text color");
            public static readonly GUIContent S_BrushPosFontSize  = new GUIContent("Brush Position font size",                                   "Font size for brush position text");
        }
        
        
        
        private static GUIStyle BorderedBoxStyle
        {
            get
            {
                var style = TpIconLib.TpSkin.box;
                style.border = new RectOffset(1, 1, 1, 1);
                return style;
            }
            
        }

        
        

        private static Vector2 s_ScrollPosition;

        /// <summary>
        /// Generate configuration info display
        /// </summary>
        public static void ConfigOnGUI(bool addScroller = true)
        {
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("For safety: hidden in PLAY mode", MessageType.Info);
                return;
            }

            GUI.skin = TpIconLib.TpSkin;

            if(addScroller)
                s_ScrollPosition = EditorGUILayout.BeginScrollView(s_ScrollPosition);
            var config = TilePlusConfig.instance;

            EditorGUILayout.HelpBox("'*' means: Click the RELOAD button for the change to take effect",MessageType.None);
            
            var boolParam = config.HideHints;
            var result    = EditorGUILayout.ToggleLeft("Hide Hints and compact", boolParam);
            if (result != boolParam)
                config.HideHints = result;
            EditorGUILayout.Separator();
            var showHints = !config.HideHints;

            if (config.ShowDefaultSelInspector)
            {
                if (showHints)
                    EditorGUILayout.HelpBox(Content.S_BkspDel.tooltip, MessageType.None, true);
                boolParam = config.AllowBackspaceDelInSelectionInspector;

                result = EditorGUILayout.ToggleLeft(Content.S_BkspDel, boolParam);
                if (result != boolParam)
                    config.AllowBackspaceDelInSelectionInspector = result;

                var s = result
                            ? "ALLOWS"
                            : "DOES NOT ALLOW";
                var s2 = result
                             ? "Editable fields for custom data won't work properly."
                             : string.Empty;
                var msg = $"The Tile++Brush Selection Inspector {s} BKSP/DEL to delete tiles. {s2} ";
                EditorGUILayout.HelpBox(msg, result
                                                 ? MessageType.Warning
                                                 : MessageType.None, true);
                EditorGUILayout.Separator();
            }

            //ShowDefaultSelInspector
            if (showHints)
                EditorGUILayout.HelpBox(Content.S_ShowDefaultSelIns.tooltip, MessageType.None, true);


            boolParam = config.ShowDefaultSelInspector;
            result    = EditorGUILayout.ToggleLeft(Content.S_ShowDefaultSelIns, boolParam);
            if (result != boolParam)
                config.ShowDefaultSelInspector = result;

            if (showHints)
                EditorGUILayout.HelpBox(Content.S_SafePlay.tooltip, MessageType.None, true);

            boolParam = config.SafePlayMode;
            result    = EditorGUILayout.ToggleLeft(Content.S_SafePlay, boolParam);
            if (result != boolParam)
                config.SafePlayMode = result;

            if (showHints)
            {
                EditorGUILayout.Separator();
                EditorGUILayout.HelpBox(Content.S_AutoSave.tooltip, MessageType.None, true);
            }

            boolParam = config.AutoSave;
            result    = EditorGUILayout.ToggleLeft(Content.S_AutoSave, boolParam);
            if (result != boolParam)
                config.AutoSave = result;

            if (showHints)
            {
                EditorGUILayout.Separator();
                EditorGUILayout.HelpBox(Content.S_ConfirmDeletion.tooltip, MessageType.None, true);
            }

            boolParam = config.ConfirmDeleteTile;
            result    = EditorGUILayout.ToggleLeft(Content.S_ConfirmDeletion, boolParam);
            if (result != boolParam)
                config.ConfirmDeleteTile = result;


            if (showHints)
            {
                EditorGUILayout.Separator();
                EditorGUILayout.HelpBox(Content.S_AllowPrefabEdit.tooltip, MessageType.None, true);
            }

            boolParam = config.AllowPrefabEditing;
            result    = EditorGUILayout.ToggleLeft(Content.S_AllowPrefabEdit, boolParam);
            if (result != boolParam)
                config.AllowPrefabEditing = result;

            if (showHints)
            {
                EditorGUILayout.Separator();
                EditorGUILayout.HelpBox(Content.S_RangeMode.tooltip, MessageType.None, true);
            }

            boolParam = config.SlidersAllowed;
            result    = EditorGUILayout.ToggleLeft(Content.S_RangeMode, boolParam);
            if (result != boolParam)
                config.SlidersAllowed = result;


            if (showHints)
            {
                EditorGUILayout.Separator();
                EditorGUILayout.HelpBox(Content.S_ButtonSizeHelpBox.text, MessageType.None, true);
            }

            var floatParam = config.SelInspectorButtonSize;
            var fResult    = EditorGUILayout.DelayedFloatField(Content.S_ButtonSizeSelInsp, floatParam);
            if (!Mathf.Approximately(floatParam, fResult))
                config.SelInspectorButtonSize = fResult;
            floatParam = config.BrushInspectorButtonSize;
            fResult    = EditorGUILayout.DelayedFloatField(Content.S_ButtonSizeBrInsp, floatParam);
            if (!Mathf.Approximately(floatParam, fResult))
                config.BrushInspectorButtonSize = fResult;

            using (new EditorGUILayout.VerticalScope(BorderedBoxStyle))
            {
                if (showHints)
                {
                    EditorGUILayout.Separator();
                    EditorGUILayout.HelpBox(Content.S_DebugMsgHelpBox.text, MessageType.None, true);
                }
                else
                    EditorGUILayout.LabelField("Debug Level", GUILayout.ExpandWidth(false));

                boolParam = config.InformationalMessages;
                result    = EditorGUILayout.ToggleLeft(Content.S_InfoMsg, boolParam);
                if (result != boolParam)
                    config.InformationalMessages = result; //note that writing to this saves the config file

                boolParam = config.WarningMessages;
                result    = EditorGUILayout.ToggleLeft(Content.S_WarningMsg, boolParam);
                if (result != boolParam)
                    config.WarningMessages = result; //note that writing to this saves the config file

                boolParam = config.ErrorMessages;
                result    = EditorGUILayout.ToggleLeft(Content.S_ErrorMsg, boolParam);
                if (result != boolParam)
                    config.ErrorMessages = result; //note that writing to this saves the config file
            }

            
            if (showHints)
            {
                EditorGUILayout.Separator();
                EditorGUILayout.HelpBox(Content.S_GizmoColor.tooltip, MessageType.None, true);
            }

            var colorParam = config.GizmoColor;
            GUI.skin = null; //needed for color picker to work correctly.
            var colorResult = EditorGUILayout.ColorField(Content.S_GizmoColor, colorParam, true, false, false);
            GUI.skin = TpIconLib.TpSkin;
            if (colorResult != colorParam)
                config.GizmoColor = colorResult;

            using (new EditorGUILayout.VerticalScope(BorderedBoxStyle))
            {
                if (showHints)
                {
                    EditorGUILayout.Separator();
                    EditorGUILayout.HelpBox(Content.S_ShowBrushPos.tooltip, MessageType.None, true);
                }

                boolParam = config.ShowBrushPosition;
                result    = EditorGUILayout.ToggleLeft(Content.S_ShowBrushPos, boolParam);
                if (result != boolParam)
                    config.ShowBrushPosition = result; //note that writing to this saves the config file

                if (showHints)
                    EditorGUILayout.HelpBox(Content.S_BrushPosColor.tooltip, MessageType.None, true);

                colorParam  = config.BrushPositionTextColor;
                GUI.skin    = null; //needed for color picker to work correctly.
                colorResult = EditorGUILayout.ColorField(Content.S_BrushPosColor, colorParam, true, false, false);
                GUI.skin    = TpIconLib.TpSkin;
                if (colorResult != colorParam)
                    config.BrushPositionTextColor = colorResult;

                if (showHints)
                    EditorGUILayout.HelpBox(Content.S_BrushPosFontSize.tooltip, MessageType.None, true);

                var fontSize = config.BrushPositionFontSize;
                var fSize    = EditorGUILayout.IntSlider(Content.S_BrushPosFontSize, fontSize, 10, 20);
                if (fontSize != fSize)
                    config.BrushPositionFontSize = fSize;


            }

            

            if (showHints)
            {
                EditorGUILayout.Separator();
                EditorGUILayout.HelpBox(Content.S_NameSpaces.tooltip, MessageType.None, true);
            }

            var stringParam = config.NameSpaces;
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Content.S_NameSpaces, GUILayout.ExpandWidth(false));
            var stringResult = EditorGUILayout.TextArea(stringParam, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            if (stringResult != stringParam)
                config.NameSpaces = stringResult;


            if (showHints)
            {
                EditorGUILayout.Separator();
                EditorGUILayout.HelpBox("Zoom factor for Focus button. Larger #s show more of the scene.", MessageType.None);
            }

            

            floatParam = config.FocusSize;
            using (new EditorGUILayout.HorizontalScope(BorderedBoxStyle))
            {
                var focusGc = new GUIContent($"{floatParam,2:F1}");
                EditorGUILayout.LabelField(Content.S_FocusZoom, focusGc, GUILayout.ExpandWidth(false));
                var zoom = GUILayout.HorizontalSlider(floatParam, TpLibEditor.MaxZoomValue, TpLibEditor.MinimumZoomValue, GUILayout.ExpandWidth(true));
                if (!Mathf.Approximately(floatParam, zoom))
                    config.FocusSize = zoom;
            }
            
            

            if (showHints)
            {
                EditorGUILayout.Separator();
                EditorGUILayout.HelpBox(Content.S_ResetButton.tooltip, MessageType.None);
            }

            if (GUILayout.Button(Content.S_ResetButton))
            {
                config.Restore();
            }

            if (showHints)
            {
                EditorGUILayout.Space(20);
                EditorGUILayout.HelpBox(Content.S_ReloadButton.tooltip, MessageType.None);
            }

            
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();
            if (GUILayout.Button(Content.S_ReloadButton))
                TpEditorUtilities.ForceHotReloadDelayed();

            EditorGUILayout.Separator();
            EditorGUILayout.Separator();
            EditorGUILayout.HelpBox($"Editor Refresh Rate {TpLib.Editor_Refresh_Rate}", MessageType.None);

            if(addScroller)
                EditorGUILayout.EndScrollView();
            GUI.skin = null;
        }


    }
}
