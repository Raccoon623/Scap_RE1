// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 01-28-2022
// ***********************************************************************
// <copyright file="BasicTileInfoGui.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Generates IMGUI representation showing general info about a TilePlus instance</summary>
// ***********************************************************************
#nullable enable
using UnityEditor;
using UnityEngine;

namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// Helper class for common GUI elements
    /// </summary>
    public static class BasicTileInfoGui
    {
        /// <summary>
        /// Gui for common TilePlus info
        /// </summary>
        /// <param name="t">tile instance</param>
        /// <param name = "isPlaying" >true in Play mode</param>
        public static void Gui(ITilePlus t, bool isPlaying)
        {
            var guidString           = t.TileGuidString;
            var descriptionString    = t.Description;
            var customTileInfoString = t.CustomTileInfo;
           
            var displayString = $"Name: {t.TileName} Id:{t.Id.ToString()}\nPosition: {t.TileGridPosition.ToString()},Parent Tilemap: {(t.ParentTilemap != null ? t.ParentTilemap.name : "missing")}";
            var go = t.InstantiatedGameObject;
            if (go != null)
            {
                if(isPlaying)
                    displayString += $"\nGameObject: {go.name}";
                else
                    displayString += $"\nGameObject: {go.name}, path: {AssetDatabase.GetAssetPath(go)}";
            }

            if (guidString != string.Empty)
                displayString += $"\nGUID: {guidString}";
            if (descriptionString != string.Empty)
                displayString += $"\nDescription: {descriptionString}";

            if (customTileInfoString != string.Empty)
                displayString += $"\nInfo: {customTileInfoString}";
            var guiContent = new GUIContent(displayString, "first line: Name (Type) [State]");
            EditorGUILayout.HelpBox(guiContent);
        }
    }
}


