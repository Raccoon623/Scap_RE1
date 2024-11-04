// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 12-03-2022
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-22-2022
// ***********************************************************************
// <copyright file="TpPainterHelpPanel.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Help info panel</summary>
// ***********************************************************************

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// Help panel
    /// </summary>
    /// <seealso cref="VisualElement" />
    internal class TpPainterHelpPanel : VisualElement
    {
        /// <summary>
        /// Ctor
        /// </summary>
        internal TpPainterHelpPanel()
        {
            name               = "help-panel";
            style.flexGrow     = 1;
            style.marginBottom = 2;
            style.display      = DisplayStyle.None;
            
            
            Add(new TpHelpBox("Click the Help (?) button again to close this panel",
                              "help-helpbox")
                {
                    style =
                    {
                        alignSelf = Align.Center
                    }
                });
            
            Add(new Button(() =>
            {
                EditorWindow.GetWindow<TpShortcutViewer>();
            }){text = "Shortcuts viewer", tooltip = "Open the shortcut viewer", style = {alignSelf = Align.Center }});
            Add(new TpSpacer(5, 10));
            
            var           scroller = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            Add(scroller);

            var helpImage = Resources.Load<Texture2D>($"TilePlus/HelpImages/{TpEditorUtilities.SystemLanguageAbbreviation}/help");
            var basis     = helpImage.height;
            scroller.Add(new Image
                          {
                              image     = helpImage,
                              style =
                              {
                                  flexBasis = basis,
                                  //flexGrow  = 1,
                                  alignSelf = Align.Center
                              }
                          });
        }

    }
    
    
    
}
