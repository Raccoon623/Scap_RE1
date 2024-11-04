// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 01-12-2024
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 01-14-2024
// ***********************************************************************
// <copyright file="TpPainterSelectionInspector.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************using TilePlus.Editor.Painter;

using UnityEngine.UIElements;

namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// "Selection" inspector for Painter.
    /// Uses ImGuiTileEditor in an IMGUIContainer Visual Element.
    /// </summary>
    public class TpPainterSelectionInspector : VisualElement
    {
        /// <summary>
        /// The tile display GUI
        /// </summary>
        private readonly IMGUIContainer selectionInspectorGui;

        /// <summary>
        /// Is this element set up properly?
        /// </summary>
        internal bool Valid => selectionInspectorGui != null;
        
        /// <summary>
        /// Builds the inspector UI (RIGHTmost column)
        /// for Tilemap selections (EDIT mode)
        /// </summary>
        /// <returns>Visual element</returns>
        internal TpPainterSelectionInspector(float viewPanesMinWidth)
        {
            name           = "selection-inspector";
            style.flexGrow = 1;
            style.minWidth = viewPanesMinWidth;
            
            //here's a container for the bottom (fixed) part of the split view
            //and the palette list container is not displayed.
            var scroller = new ScrollView(ScrollViewMode.VerticalAndHorizontal)
                                             {
                                                name = "tiles-info-container", style =
                                                 {
                                                     
                                                     flexGrow = 1,
                                                     minWidth                = viewPanesMinWidth,
                                                     minHeight               = 80,
                                                     borderTopWidth          = 2,
                                                     borderLeftWidth         = 2,
                                                     borderBottomWidth       = 4,
                                                     borderRightWidth        = 2,
                                                     borderBottomLeftRadius  = 3,
                                                     borderBottomRightRadius = 3,
                                                     borderTopLeftRadius     = 3,
                                                     borderTopRightRadius    = 3,
                                                     paddingBottom           = 2,
                                                     paddingLeft             = 4,
                                                     paddingTop              = 2,
                                                     paddingRight            = 2,
                                                     marginLeft              = 4,
                                                     marginBottom            = 8
                                                 }
                                             };

            //want same look as scrollviews inside the list-views.
            scroller.AddToClassList("unity-collection-view--with-border");
            scroller.AddToClassList("unity-collection-view__scroll-view");
            
        
            //the container contains an IMGUI view
            selectionInspectorGui                     = new IMGUIContainer(TpPainterDataGUI.ImitateSelectionInspector);
            selectionInspectorGui.style.paddingBottom = 2;
            selectionInspectorGui.cullingEnabled      = true;

            scroller.Add(selectionInspectorGui);
            Add(scroller); //add to container

        }

        /// <summary>
        /// Force a repaint of the IMGui Container.
        /// </summary>
        internal void RepaintGui()
        {
            selectionInspectorGui.MarkDirtyRepaint();
        }
    }
}
