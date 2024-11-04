// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 08-01-2022
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 10-15-2022
// ***********************************************************************
// <copyright file="TileCustomGui.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Creates the gui for TPT tiles. </summary>
// ***********************************************************************
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// Provides CustomGui for the TilePlusBase class. Encompasses most
    /// of the fields that all Tile derivatives have like color, transform, etc.
    /// </summary>
    [InitializeOnLoad]
    public static class TileCustomGui
    {
        static TileCustomGui()
        {
            TpEditorBridge.BaseCustomGui = BaseGui;
        }


        private static  GUIContent s_RuntimeGoFlagGuiContent     ;
        private static  GUIContent s_KeepGoOnEraseFlagGuiContent ;
        private static  GUIContent s_GameObjectGuiContent        ;
        private static  GUIContent s_ColorFlagGuiContent         ;
        private static  GUIContent s_TransFlagGuiContent         ;
        private static  GUIContent s_ColorGuiContent             ;
        private static  GUIContent s_PosGuiContent               ;
        private static  GUIContent s_RotGuiContent               ;
        private static  GUIContent s_ScaleGuiContent             ;
        private static  GUIContent s_ColliderGuiContent          ;
        private static  GUIContent s_TileSpriteClearGuiContent   ;
        private static  GUIContent s_TagsGuiContent              ;
        private static  GUIContent s_NameGuiContent              ;
        private static  GUIContent s_NameLockGuiContent          ;
        private static  GUIContent s_NameUnLockGuiContent        ;
        private static  GUIContent s_InvalidMapGuiContent        ;

        private const string ClearModeInfo = "Control sprite visibility. Ignore means do nothing. \n" +
                                             "ClearInSceneView clears the sprite when painted. \n" +
                                             "ClearOnStart clears the sprite when the game starts.\n" +
                                             "ClearInSceneViewAndOnStart does both.\n"
                                             + "Click the button again to close this.";

        private const string GameObjInfo = "See the TilePlus docs for more info about use of the GameObject options; can cause errors.";
        
        private static bool s_ShowTscInfo;
        private static bool s_ShowGameObjInfo;
        private static bool s_GuiContentInitialized;
        
        /// <summary>
        /// Easy way to return this value without having to instantiate it. The value is consumed immediately
        /// so no reason to ever have more than one of these.
        /// </summary>
        // ReSharper disable once MemberCanBeProtected.Global
        private static CustomGuiReturn NoActionRequiredCustomGuiReturn { get; } = new();


        private static void InitializeGuiContent()
        {
            s_RuntimeGoFlagGuiContent     = new("GameObj RuntimeOnly", "Instantiate the GameObject only during Play.");
            s_KeepGoOnEraseFlagGuiContent = new("GameObj Retain", "Keep the GO during play even if the tile is deleted or replaced.");
            s_GameObjectGuiContent        = new("GameObject", "GameObject to place at the tile position");
            s_ColorFlagGuiContent         = new("Lock Color",        "Lock/Unlock color in the tilemap.");
            s_TransFlagGuiContent         = new("Lock Transform",    "Lock/Unlock the Transform in the tilemap.");
            s_ColorGuiContent             = new("Color",             "Set tile sprite color on the tilemap and within the tile.");
            s_PosGuiContent               = new("Position",          "Set tile sprite position on the tilemap and within the tile.");
            s_RotGuiContent               = new("Rotation",          "Set tile sprite rotation on the tilemap and within the tile.");
            s_ScaleGuiContent             = new("Scale",             "Set tile sprite scale on the tilemap and within the tile.");
            s_ColliderGuiContent          = new("Collider override", "Set collider type for this tile. NoOverride means no change. Other settings override Tile class Collider param.");
            s_TileSpriteClearGuiContent   = new("Tile sprite",       "Controls tile sprite visibility. Click the 'i' button for more info");
            s_TagsGuiContent              = new("Tags",              "Optional tag(s) for this tile. Separate with commas. Do not use ------ as a tag.");
            s_NameGuiContent              = new("Name",              "Change the name of this tile.");
            s_NameLockGuiContent          = new("Lock Tile name",    "Check toggle to prevent changes to tile name");
            s_NameUnLockGuiContent        = new("Unlock Tile name",  "Un-check toggle to allow changes to tile name");
            s_InvalidMapGuiContent        = new("Could not format: Tilemap was null. Not an error. Try a Scripting Reload");
        }
        
        /// <summary>
        /// Gui for TilePlusBase
        /// </summary>
        /// <param name="skin">skin</param>
        /// <param name="buttonSize">buttonsize</param>
        /// <param name="tile">tile for display</param>
        /// <param name = "noEdit" >disallow editing? set true when tile is in a prefab</param>
        /// <returns>An instance of the CustomGuiReturn struct</returns>
        private static CustomGuiReturn BaseGui(TilePlusBase tile, GUISkin skin, Vector2 buttonSize, bool noEdit)
        {
            if (!s_GuiContentInitialized)
            {
                s_GuiContentInitialized = true;
                InitializeGuiContent(); //lazy init reduces domain reload time.
            }
            
            var target = tile; //avoids closure of input 'tile'
            if (target.ParentTilemap == null)
            {
                EditorGUILayout.HelpBox(s_InvalidMapGuiContent);
                return NoActionRequiredCustomGuiReturn;
            }

            var doRefresh         = false;
            var delaySceneSave    = false;
            var modifiedFieldName = string.Empty;
            var map               = target.ParentTilemap;
            var pos               = target.TileGridPosition;
            var iTarget           = target as ITilePlus;
            
            if (!target.IsPlayMode && !noEdit) //much of this is hidden in play mode or when in a prefab
            {
                using (new EditorGUIUtility.IconSizeScope(buttonSize))
                {
                    GUI.skin = skin;

                    if (!target.NameLocked)
                    {
                        using var check   = new EditorGUI.ChangeCheckScope();
                        var       newName = EditorGUILayout.DelayedTextField(s_NameGuiContent, target.name.Split('(')[0]);
                        if (check.changed)
                        {
                            target.name       = newName;
                            target.TileName = newName;
                            doRefresh  = true;
                        }
                    }

                    target.NameLocked = EditorGUILayout.ToggleLeft(target.NameLocked
                                                                ? s_NameUnLockGuiContent
                                                                : s_NameLockGuiContent, target.NameLocked);

                    EditorGUILayout.Separator();
                    DrawUILine(Color.blue);
                    if (s_ShowTscInfo)
                        EditorGUILayout.HelpBox(ClearModeInfo, MessageType.Info);
                    EditorGUILayout.BeginHorizontal();
                    //Gui for tileSpriteClear enumeration
                    var tscEnum = target.TileSpriteClear as Enum;
                    var newTsc  = (SpriteClearMode)EditorGUILayout.EnumPopup(s_TileSpriteClearGuiContent, tscEnum);
                    if (newTsc != target.TileSpriteClear)
                    {
                        target.TileSpriteClear = newTsc;
                        doRefresh         = true;
                    }

                    if (GUILayout.Button(TpIconLib.FindIcon(TpIconType.InfoIcon)))
                        s_ShowTscInfo = !s_ShowTscInfo;
                    EditorGUILayout.EndHorizontal();

                    DrawUILine(Color.blue);

                    //GUI for tile flags
                    var flagsFromTilemap = map.GetTileFlags(pos);

                    var cFlag     = (flagsFromTilemap & TileFlags.LockColor) != 0;
                    var tFlag     = (flagsFromTilemap & TileFlags.LockTransform) != 0;
                    if (!iTarget.InternalLockColor)
                    {
                        var colorFlag = EditorGUILayout.ToggleLeft(s_ColorFlagGuiContent, cFlag);
                        if (colorFlag != cFlag)
                        {
                            if (colorFlag)
                                target.flags |= TileFlags.LockColor;
                            else
                                target.flags &= ~TileFlags.LockColor;

                            TpLib.DelayedCallback(target, () => map.SetTileFlags(pos, target.flags), "T+Base-CFlag");
                            delaySceneSave = true;
                        }
                    }

                    if (!iTarget.InternalLockTransform)
                    {
                        var transformFlag = EditorGUILayout.ToggleLeft(s_TransFlagGuiContent, tFlag);
                        if (transformFlag != tFlag)
                        {
                            if (transformFlag)
                                target.flags |= TileFlags.LockTransform;
                            else
                                target.flags &= ~TileFlags.LockTransform;

                            TpLib.DelayedCallback(target, () => map.SetTileFlags(pos, target.flags), "T+Base-TFlag");
                            delaySceneSave = true;
                        }
                    }

                    DrawUILine(Color.red);

                    //Note that gameObject is the asset. It's NOT what's in the scene after tile is placed or at runtime.
                    if (target.gameObject != null) //if this is Tile or subclass and it has a GO, show these too.
                    {
                        DrawUILine(Color.red, 4, 2);
                        EditorGUILayout.HelpBox(GameObjInfo, MessageType.None);
                        var runtimeGoFlag     = (flagsFromTilemap & TileFlags.InstantiateGameObjectRuntimeOnly) != 0;
                        var keepGoOnEraseFlag = (flagsFromTilemap & TileFlags.KeepGameObjectRuntimeOnly) != 0;

                        var rtGoFlag = EditorGUILayout.ToggleLeft(s_RuntimeGoFlagGuiContent, runtimeGoFlag);
                        if (rtGoFlag != runtimeGoFlag)
                        {
                            if (rtGoFlag)
                                flagsFromTilemap |= TileFlags.InstantiateGameObjectRuntimeOnly;
                            else
                                flagsFromTilemap &= ~TileFlags.InstantiateGameObjectRuntimeOnly;

                            target.flags = flagsFromTilemap;
                            TpLib.DelayedCallback(target,() => map.SetTileFlags(pos, target.flags), "T+Base-RtGoFlag");
                        }

                        var rtGoKeepFlag = EditorGUILayout.ToggleLeft(s_KeepGoOnEraseFlagGuiContent, keepGoOnEraseFlag);
                        if (rtGoKeepFlag != keepGoOnEraseFlag)
                        {
                            if (rtGoKeepFlag)
                                flagsFromTilemap |= TileFlags.KeepGameObjectRuntimeOnly;
                            else
                                flagsFromTilemap &= ~TileFlags.KeepGameObjectRuntimeOnly;
                            target.flags = flagsFromTilemap;

                            TpLib.DelayedCallback(target, () => map.SetTileFlags(pos, target.flags), "T+Base-KeepGoFlag");
                        }
                    }

                    var possibleGo = EditorGUILayout.ObjectField(s_GameObjectGuiContent, target.gameObject, typeof(GameObject), false) as GameObject;
                    if (possibleGo != target.gameObject)
                    {
                        target.gameObject = possibleGo;
                        doRefresh  = true;
                    }

                    DrawUILine(Color.blue);

                    EditorGUILayout.Separator();

                    if (iTarget.InternalLockCollider)
                        EditorGUILayout.HelpBox($"ColliderMode: {target.TileColliderMode.ToString()}", MessageType.None);
                    else
                    {
                        //GUI for collider override setting
                        var val         = target.TileColliderMode as Enum;
                        var newCollMode = (ColliderMode)EditorGUILayout.EnumPopup(s_ColliderGuiContent, val);
                        if (newCollMode != target.TileColliderMode)
                        {
                            target.TileColliderMode = newCollMode;
                            doRefresh      = true;
                        }
                    }

                    if (iTarget.InternalLockTags)
                        EditorGUILayout.HelpBox($"Tags: {iTarget.Tag}", MessageType.None);
                    else
                    {
                        //GUI for tags
                        var newTags = EditorGUILayout.DelayedTextField(s_TagsGuiContent, iTarget.Tag);
                        if (newTags != iTarget.Tag)
                        {
                            iTarget.Tag             = newTags;
                            modifiedFieldName = "m_Tag";
                        }
                    }

                    DrawUILine(Color.blue);

                    //GUI for color: hidden if tile locks color or tileflags is set to lock color
                    if (!iTarget.InternalLockCollider && (target.flags & TileFlags.LockColor) != TileFlags.LockColor)
                    {
                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button("Reset"))
                        {
                            target.color = Color.white;
                            map.SetColor(pos,target.color);
                            doRefresh = true;
                        }

                        GUI.skin = null; //avoids skin problems with colorfield popup
                        EditorGUI.BeginChangeCheck();
                        var colorFromTilemap = map.GetColor(pos);
                        var newColor         = EditorGUILayout.ColorField(s_ColorGuiContent, colorFromTilemap);
                        GUI.skin = skin;
                        if (EditorGUI.EndChangeCheck())
                        {
                            target.color = newColor;
                            map.SetColor(pos, newColor);
                            doRefresh = true;
                        }

                        EditorGUILayout.EndHorizontal();
                    }

                    //GUI for transform controls: hidden if tile is manipulating transforms
                    //or if tileflags is set to lock transform.
                    if (!(iTarget.InternalLockTransform || (target.flags & TileFlags.LockTransform) == TileFlags.LockTransform))
                    {
                        if (!target.transform.ValidTRS())
                            EditorGUILayout.HelpBox("Bad transform matrix!", MessageType.Warning);
                        else
                        {
                            TileUtil.GetTransformComponents(map, pos, out var position, out var rotation, out var scale);
                            var newRotation = rotation;
                            position = TileUtil.RoundVector3(position, 4);
                            rotation = TileUtil.RoundVector3(rotation, 4);
                            scale    = TileUtil.RoundVector3(scale,    4);

                            EditorGUILayout.BeginHorizontal();
                            if (GUILayout.Button("Reset"))
                            {
                                target.transform = Matrix4x4.identity;
                                map.SetTransformMatrix(pos, target.transform);
                                doRefresh = true;
                            }

                            EditorGUILayout.BeginVertical();
                            EditorGUI.BeginChangeCheck();

                            var newPosition = EditorGUILayout.Vector3Field(s_PosGuiContent, position);
                            if (target.IsRotatable)
                                newRotation = EditorGUILayout.Vector3Field(s_RotGuiContent, rotation);
                            var newScale = EditorGUILayout.Vector3Field(s_ScaleGuiContent, scale);
                            if (EditorGUI.EndChangeCheck() && newScale != Vector3.zero)
                            {
                                TileUtil.SetTransform(map, pos, newPosition, newRotation, newScale);
                                doRefresh = true;
                            }

                            EditorGUILayout.EndVertical();
                            EditorGUILayout.EndHorizontal();

                        }
                    }
                }
            }
            else
            {
                if (map == null) //can happen in a transient manner in play mode if tiles are deleted.
                {
                    EditorGUILayout.HelpBox(s_InvalidMapGuiContent);
                    return NoActionRequiredCustomGuiReturn;
                }

                ImGuiTileEditor.StandardRuntimeInspector(map, pos);
            }

            return new(doRefresh, delaySceneSave, modifiedFieldName);

        }


        //HT to alexanderameye https://forum.unity.com/threads/horizontal-line-in-editor-window.520812/
        /// <summary>
        /// Draw a GUI line 
        /// </summary>
        /// <param name = "c" >Color of the line</param>
        /// <param name="padding">extra space</param>
        /// <param name="height">height (think width) of the line</param>
        private static void DrawUILine(Color c, float padding = 2, float height = 1)
        {
            var thickness = height > 0
                                ? height
                                : 1;
            var r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height =  thickness;
            r.y      += padding / 2;
            r.x      -= 2;
            //r.width  += 6;
            EditorGUI.DrawRect(r, c);
        }

        
    }
}
