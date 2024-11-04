// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 06-12-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 06-25-2023
// ***********************************************************************
// <copyright file="TpShortcutViewer.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Label = UnityEngine.UIElements.Label;
using static TilePlus.Editor.TpIconLib;

namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// Shortcut Viewer window
    /// </summary>
    public class TpShortcutViewer : EditorWindow
    {
        private readonly List<string> shortcutIds = new();
        private ListView shortcutList;

        /// <summary>
        /// Show the window
        /// </summary>
        //[Shortcut("TilePlus/Painter:Open Shortcut Viewer", KeyCode.Alpha6, ShortcutModifiers.Alt)]
        [MenuItem("Tools/TilePlus/Shortcut Viewer", false, 100)]
        public static void ShowWindow()
        {
            var wnd = GetWindow<TpShortcutViewer>();
            wnd.titleContent = new GUIContent("TilePlus Shortcuts", FindIcon(TpIconType.TptIcon));
        }

        internal void CreateGUI()
        {
            GenerateContent();
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;
            root.Add(new TpHeader("Active Painter Shortcuts", "header"));
            root.Add(new TpSpacer(10, 10));
            shortcutList = new TpListView(shortcutIds, 32, true, MakeItem, BindItem);


            root.Add(shortcutList);
            root.Add(new TpSpacer(10, 10));
            root.Add(new Button(() =>
            {
                GenerateContent();
                UpdateTable();
            }){text = "Refresh", tooltip = "Refresh this window", style = {alignSelf = Align.FlexStart}});
            root.Add(new Label("[C] = 'Clutch' shortcut: hold the key down.") { style = { paddingLeft = 4 } });
            ShortcutManager.instance.shortcutBindingChanged += OnshortcutBindingChanged;
            UpdateTable();


        }

        private void UpdateTable()
        {
            shortcutList?.Rebuild();
        }

        // ReSharper disable once AnnotateNotNullTypeMember
        private VisualElement MakeItem()
        {

            var item = new TpListBoxItem("shortcut-list-item", Color.black);
            var label = new Label { name = "left-label", style = { minWidth = 64 } };
            item.Add(label);
            label = new Label { name = "right-label", style = { alignContent = Align.FlexEnd } };
            item.Add(label);
            return item;

        }

        private void BindItem(VisualElement ve, int index)
        {
            var left = ve.Q<Label>("left-label");
            var right = ve.Q<Label>("right-label");

            var key = shortcutIds[index];
            var val = ShortcutManager.instance.GetShortcutBinding(key);
            right.text = key;
            left.text = val.ToString();
        }


        private void OnshortcutBindingChanged(ShortcutBindingChangedEventArgs obj)
        {
            GenerateContent();
            UpdateTable();
        }


        private void GenerateContent()
        {
            var s = ShortcutManager.instance.GetAvailableShortcutIds();
            shortcutIds.Clear();
            shortcutIds.AddRange(s.Where(z => z.Contains("TilePlus/Painter")));
            shortcutIds.Sort(Comparison);

        }

        //note that the strings here are the ids, not the keycodes for the shortcut.
        private int Comparison(string x, string y)
        {
            var xKey = ShortcutManager.instance.GetShortcutBinding(x).ToString();
            var yKey = ShortcutManager.instance.GetShortcutBinding(y).ToString();
            
            var xDex = xKey.IndexOf('+'); //returns -1 if not found
            var yDex = yKey.IndexOf('+');

            if (xDex != -1)
                xKey = xKey.Substring(xDex);
            if (yDex != -1)
                yKey = yKey.Substring(yDex);

            return string.CompareOrdinal(xKey, yKey);

        }
    }
}
