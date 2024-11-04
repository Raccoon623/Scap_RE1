// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 12-03-2022
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-31-2022
// ***********************************************************************
// <copyright file="TpPainterContentPanel.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Panel for the center and right columns</summary>
// ***********************************************************************

using System.Collections.Generic;
using System.Linq;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using Color = UnityEngine.Color;
#nullable enable

namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// Builds the UI for the right-half of the outer splitview (ie the center and right columns)
    /// This splitview's left side contains a vertical splitview which shows either the list of
    /// palettes and a control region OR a lis tof tiles and a control region.
    /// The splitview's right side contains a vertical splitview which shows a palette-item in
    /// its top part with a 'brush inspector' in its bottom part (palette mode). In tilemap
    /// mode this top part is collapsed and the bottom part occupies the entire pane: shows the
    /// 'Selection Inspector'
    /// </summary>
    internal class TpPainterContentPanel : VisualElement, ISettingsChangeWatcher
    {
        #region privateFieldsProperties
        /// <summary>
        /// The content panel split view
        /// </summary>
        private readonly TpSplitter? contentPanelSplitView;
        /// <summary>
        /// The asset view splitter
        /// </summary>
        private readonly TpSplitter? assetViewSplitter;
        /// <summary>
        /// The controls panel
        /// </summary>
        private readonly TpPainterAssetViewControls? tpPainterAssetViewControls;
        /// <summary>
        /// The Asset Viewer panel
        /// </summary>
        internal readonly TpPainterAssetViewer? m_TpPainterAssetViewer;
        
        /// PAINT mode brush inspector view
        /// <summary>
        /// container for Paint mode rightmost column
        /// </summary>
        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        internal readonly TpPainterBrushInspector? m_TpPainterBrushInspector;
        
        /// <summary>
        /// Edit mode Selection inspector view
        /// </summary>
        internal readonly TpPainterSelectionInspector? m_TpPainterSelectionInspector;

        /// <summary>
        /// Grid selections GUI
        /// </summary>
        internal readonly TpPainterGridSelPanel? m_GridSelectionPanel;

        /// <summary>
        /// This window
        /// </summary>
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private  TilePlusPainterWindow PainterWindow => TilePlusPainterWindow.instance!;
        /// <summary>
        /// The view panes minimum width
        /// </summary>
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly float                 viewPanesMinWidth;

        private readonly List<VisualElement> contentPanelChildren = new(3);

       
        
        #endregion
        
        #region internalProperties
        /// <summary>
        /// The list item height
        /// </summary>
        internal float ListItemHeight {get; set; }
        
        #endregion
        
        #region ctor

        /// <summary>
        /// this is a panel with all the UI for the right-hand side of the main splitview.
        /// </summary>
        /// <param name="viewPanesMinWidthValue">Minimum width dimension for view panes</param>
        internal TpPainterContentPanel(float viewPanesMinWidthValue)
        {
            viewPanesMinWidth = viewPanesMinWidthValue;
            ListItemHeight         = TilePlusPainterConfig.PainterListItemHeight;
            style.fontSize         = TilePlusPainterConfig.ContentPanelFontSize;

            m_GridSelectionPanel = new TpPainterGridSelPanel {style = {display = DisplayStyle.None}};
            Add(m_GridSelectionPanel);
            
            contentPanelSplitView = new TpSplitter("painter-splitview-inner",
                         "TPT.TPPAINTER.SPLITVIEW.RIGHT",
                         100,
                         TwoPaneSplitViewOrientation.Horizontal,
                         0,(evt =>
                            {
                                if(evt.newRect.size == Vector2.zero)
                                    evt.StopImmediatePropagation(); //needed to preserve splitter pos when changing global modes. LEAVE AS IS!
                                else
                                    evt.StopPropagation();
                            })) { style = { minHeight = 0 } };
            
            var splitterHandle = contentPanelSplitView.Q<VisualElement>("unity-dragline-anchor");
            splitterHandle.style.backgroundColor = Color.red;
            Add(contentPanelSplitView);

            /*left side of this splitview also a splitView: AssetView.
            
            This has two major panels: Palette/Tilemap (PTV) View and Tile View (TV) 
            
            PTV has two panels: one for each of the two Global modes:  PaintingView and EditingView
            
            PaintingView is a list of palettes/tilefabs|Chunks/Favorites with an area for options at the bottom
            this one has a vertical orientation.
            the fixed pane is a container. 
               label - title
               label - context
               listView with names of palettes, tilefabg/chunks, or Favorites
            EditingView
            a list of tiles in a tilemap selected from column 1.
                label - title
                label - context
                list view with tiles
            */


            //the vertical split view gets added to this container.
            assetViewSplitter = new TpSplitter("painter-splitview-assetinfo",
                                                 "TPT.TPPAINTER.SPLITVIEW.ASSETS",
                                                 100,
                                                 TwoPaneSplitViewOrientation.Vertical,
                                                 1, SourceSplitterFix) { style = { minWidth = viewPanesMinWidth } };

            //this is the top part of this split
            //in PAINT mode this displays the list of Palettes, Tilefabs|Chunks, and the Favorites List
            //in EDIT mode this displays the list of tiles from the tilemap chosen in the left column of the window.
            //Paint mode is what appears when this window opens.
            assetViewSplitter.Add(m_TpPainterAssetViewer = new TpPainterAssetViewer( ListItemHeight, this.viewPanesMinWidth)); //add first panel to palette view splitter

            //the bottom part of the palette view is a small options panel
            //this has two different types of info: for palettes view or for tilemaps view
            assetViewSplitter.Add(tpPainterAssetViewControls = new TpPainterAssetViewControls(this.viewPanesMinWidth));
            
            contentPanelSplitView.Add(assetViewSplitter); //add source view controls to inner splitview
            contentPanelChildren.Add(assetViewSplitter);

            //The rightmost panel holds two 'Inspector' subpanels,
            //only one of which is available at a time, depending on Global mode
            var inspectorPanel = new VisualElement {name = "inspector-panel", style = { flexGrow = 1 } }; 
            contentPanelSplitView.Add(inspectorPanel);
            contentPanelChildren.Add(inspectorPanel);
            //For PAINT mode, right side of InnerSplitView is itself a splitview, this one has a vertical orientation.
            //the fixed pane is a container 
            //   label - title
            //   label - context
            //   listView (palette content display)
            // bottom of split is 'brush' inspector
            inspectorPanel.Add( m_TpPainterBrushInspector = new TpPainterBrushInspector(this.viewPanesMinWidth,ListItemHeight));
            //in EDIT mode, right side of InnerSplitView is a TpPainterSelectionInspector
            // label - content
            // 'Selection' Inspector
            m_TpPainterSelectionInspector = new TpPainterSelectionInspector(viewPanesMinWidth)
                                 {
                                     style = { display = DisplayStyle.None} //initially OFF
                                 };
            inspectorPanel.Add(m_TpPainterSelectionInspector);
            contentPanelChildren.Add(inspectorPanel);
        }
        #endregion
        
        #region access

        #pragma warning disable CS8602 // Dereference of a possibly null reference.

        /// <summary>
        /// Set the content panel for Tilemaps or Palette
        /// </summary>
        /// <param name="mode">Palette or Tilemaps mode from GlobalMode enum</param>
        internal void SetDisplayState(GlobalMode mode)
        {
            PainterWindow.TabBar.EnableClipboard(mode == GlobalMode.PaintingView);
            
            if (mode == GlobalMode.GridSelView)
            {
                //inspectorPanel.style.display = DisplayStyle.None;
                foreach (var child in contentPanelChildren)
                    child.style.display = DisplayStyle.None;
                        
                contentPanelSplitView.SendToBack();
                contentPanelSplitView.style.flexGrow = 0;

                //contentPanelSplitView.style.display = DisplayStyle.None;
                m_GridSelectionPanel.style.display  = DisplayStyle.Flex;
                m_GridSelectionPanel.BringToFront();
                m_GridSelectionPanel.EnableScheduledUpdate(true);
                return;
            }

            m_GridSelectionPanel.style.display  = DisplayStyle.None;
            m_GridSelectionPanel.EnableScheduledUpdate(false);
            foreach (var child in contentPanelChildren)
                child.style.display = DisplayStyle.Flex;

            //contentPanelSplitView.style.display = DisplayStyle.Flex;
            //inspectorPanel.style.display = DisplayStyle.Flex;

            contentPanelSplitView.style.flexGrow = 1;
            contentPanelSplitView.BringToFront();
            m_GridSelectionPanel.SendToBack();
            
            if (mode == GlobalMode.EditingView)
            {
                m_TpPainterBrushInspector.style.display = DisplayStyle.None;
                m_TpPainterAssetViewer.ShowPalettesListView(false); 
                tpPainterAssetViewControls.ShowSourceViewPaletteOptions(false);
                m_TpPainterAssetViewer.ShowTilesListView(true);
                tpPainterAssetViewControls.ShowSourceViewTileOptions(true);
                m_TpPainterAssetViewer.SetHeaderLabelText("Tiles");
                ShowTilemapsListSelectionNeededHelpBox(false);
                SetAssetViewSelectionLabel(TilePlusPainterWindow.EmptyFieldLabel);
                m_TpPainterSelectionInspector.style.display = DisplayStyle.Flex;
            }
            else
            {
                m_TpPainterSelectionInspector.style.display = DisplayStyle.None;
                m_TpPainterAssetViewer.ShowPalettesListView(true);
                tpPainterAssetViewControls.ShowSourceViewPaletteOptions(true);
                m_TpPainterAssetViewer.ShowTilesListView(false);
                tpPainterAssetViewControls.ShowSourceViewTileOptions(false);
                ShowTilemapsListSelectionNeededHelpBox(false);
                m_TpPainterAssetViewer.SetHeaderLabelText("Painting Source");
                SetAssetViewSelectionLabel(TpPainterState.PaintableObject != null
                    ? TpPainterState.PaintableObject.ItemName
                    : TilePlusPainterWindow.EmptyFieldLabel);
                
                m_TpPainterBrushInspector.style.display = DisplayStyle.Flex;
                
                
                
                //if the brush inspector tiles list has a selection, use that for the brush inspector target. Otherwise, null this.
                var selectedItem = (TpPainterClipboard) m_TpPainterBrushInspector.m_BrushInspectorListView.selectedItem; 
                if(selectedItem == null)
                    TpPainterState.Clipboard = null;
                else
                    m_TpPainterBrushInspector.SelectBrushInspectorTarget(selectedItem); 
            }
        }
        #pragma warning restore CS8602 // Dereference of a possibly null reference.


        /// <summary>
        /// used to enable or disable the entire content panel when the MOVE tabbar function is selected.
        /// </summary>
        /// <param name="enable">true/false to enable/disable</param>
        internal void EnableContentPanelSplitView(bool enable)
        {
            contentPanelSplitView!.SetEnabled(enable);
            m_TpPainterBrushInspector!.SetPaletteVisibility(enable);
        }

        /// <summary>
        /// Recompute data for the Tag filter
        /// </summary>
        internal void RecomputeTagFilter()
        {
            tpPainterAssetViewControls!.RecomputeTagFilter();
        }

        /// <summary>
        /// Recompute data for the Type filter
        /// </summary>
        internal void RecomputeTypeFilter()
        {
            tpPainterAssetViewControls!.RecomputeTypeFilter();
        }

        
        /// <summary>
        /// Show a message that the user needs to make a tilemap selection in the leftmost column.
        /// </summary>
        /// <param name="show">true to show or false to hide</param>
        internal void ShowTilemapsListSelectionNeededHelpBox(bool show)
        {
            if (TpPainterState.InPaintMode)
                show = false;
            m_TpPainterAssetViewer!.ShowMapSelectionNeededHelpbox(show);
        }

        
        /// <summary>
        /// Set the selection label for the center column
        /// </summary>
        /// <param name="text">text to display</param>
        internal void SetAssetViewSelectionLabel(string text)
        {
            m_TpPainterAssetViewer!.SetSelectionLabelText(text);
        }

        

        /// <summary>
        /// Is this set of UIElements properly set up!
        /// </summary>
        public bool Valid =>
            assetViewSplitter != null
            && contentPanelSplitView != null
            && m_TpPainterBrushInspector != null
            && m_TpPainterSelectionInspector != null
            && assetViewSplitter.Valid
            && contentPanelSplitView.Valid
            && m_TpPainterSelectionInspector.Valid
            && m_TpPainterBrushInspector.Valid;


        /// <summary>
        /// Reset the filters
        /// </summary>
        internal void ResetFilters()
        {
            tpPainterAssetViewControls!.ResetFilters();
        }
        
        #endregion
        
        #region events
        
        /// <inheritdoc />
        public void OnSettingsChange(string change, ConfigChangeInfo _)
        {
            if(change == TPP_SettingThatChanged.MaxTilesInViewer.ToString())
                UpdateCenterColumnView();
        }

        /// <summary>
        /// Update the view in the center column for Edit and Paint modes
        /// </summary>
        internal void UpdateCenterColumnView()
        {
            switch (TpPainterState.GlobalMode)
            {
                case GlobalMode.EditingView:
                {
                    if (TpPainterState.PaintableMap is { Valid: true } && TpPainterState.PaintableMap.TargetTilemap != null)
                        PainterWindow.SetEditModeInspectorTarget(TpPainterState.PaintableMap.TargetTilemap);
                    else
                        PainterWindow.SetEditModeInspectorTarget(null);
                    m_TpPainterAssetViewer!.RebuildEditModeTilesListView();
                    break;
                }
                case GlobalMode.PaintingView:
                    PainterWindow.PaintModeUpdateAssetsList();
                    break;
            }
        }
       
        /// <summary>
        /// Directly set a palette selection
        /// </summary>
        /// <param name="index">item index</param>
        /// <remarks>Normally ONLY used to set index 0 to select the Favorites List. Not tested with any other index.</remarks>
        internal void SetPaletteSelectionDirect(int index)
        {
            if (index < 0)
            {
                TpLib.TpLogError("TpPainterContentPanel.SetPaletteSelectionDirect cannot accept index < 0. Ignored...");
                return;
            }

            m_TpPainterAssetViewer!.SetPaletteListSelection(index);
            var item = m_TpPainterAssetViewer.GetPaletteListItemAtIndex(0);
            if(item == null)
                return;
            TpPainterState.PaintableObject = item;
            
            m_TpPainterAssetViewer.SetSelectionLabelText(item.ItemName);
            m_TpPainterBrushInspector!.ClearBrushInspectorSelection();
            PainterWindow.ClearClipboard();
            PainterWindow.PaintModeUpdateAssetsList();
        }

        /// <summary>
        /// Selects the painting source
        /// </summary>
        /// <param name="itemIndex">Index of the source item.</param>
        internal void SelectPaletteOrOtherSource(int itemIndex)
        {
            if (itemIndex >= m_TpPainterAssetViewer!.PaletteListViewItemCount)
                return;
            var item = m_TpPainterAssetViewer.GetPaletteListItemAtIndex(itemIndex);
            //Debug.Log($"index {itemIndex} item {item}");
            if(item != null)
                SelectPaletteOrOtherSource(item);
        }

        /// <summary>
        /// Selects the painting source
        /// </summary>
        /// <param name="item">A PaintableObject instance</param>
        /// <param name="notifyGridPaintingState">true if UTE's GridPaintingState should be notified</param>        
        /// <remarks>notify is used as part of PaletteSync operations</remarks>
        internal void SelectPaletteOrOtherSource(PaintableObject? item, bool notifyGridPaintingState = true)
        {
            if(PainterWindow == null || item == null)
                return;

            if (TpPainterState.PaintableObject == item) 
            {
                if(TpLibEditor.Informational)
                    TpLib.TpLog("Select Palette: same paintable object - don't update");
                return;
            }
        
            TpPainterState.PaintableObject = item; //the PaintableObject that we want to use

            m_TpPainterAssetViewer!.SetSelectionLabelText(item.ItemName);

            if (item.ItemType is TpPaletteListItemType.Palette or TpPaletteListItemType.Favorites)
            {
                m_TpPainterBrushInspector!.ClearBrushInspectorSelection();
                PainterWindow.ClearClipboard();
                PainterWindow.PaintModeUpdateAssetsList();
            }
            else //is TileFab or Bundle
            {
                PainterWindow.ClearClipboard();
                PainterWindow.PaintModeUpdateAssetsList();

                if ((item.ItemType == TpPaletteListItemType.Bundle && !TilePlusPainterConfig.TpPainterShowBundleAsPalette)
                    || item.ItemType == TpPaletteListItemType.TileFab)
                    m_TpPainterBrushInspector!.SelectBrushInspectorTarget(0, true);
            }

            m_TpPainterBrushInspector!.SelectView();
            m_TpPainterBrushInspector.RefreshBrushInspectorListView();
            

            //added dec 9 2023. Sync palette selection (optional) betw T+P and Unity Palette.
            if (item.ItemType != TpPaletteListItemType.Palette)
                return;
            // ReSharper disable once Unity.NoNullPatternMatching
            if(!(TilePlusPainterConfig.TpPainterShowPalettes && TilePlusPainterConfig.TpPainterSyncPalette))
                return;
            //if notify grid painting state is true then this method was called when the ContentPanel's list of palettes as clicked on
            if (notifyGridPaintingState)
            {
                TpPainterSceneView.instance.IgnoreNextHierarchyChange = true;  //palettes are used in mock scenes so hanging palette this way creates a hierarchy-change event which is not needed.
                PainterWindow.DiscardUnityPaletteEvents               = true; //this avoids a loop where this method gets called repeatedly when the palette-change event hits PainterWindow.
                GridPaintingState.palette                             = item.Palette;
            }

            //otherwise, this method was called when the Unity Palette's Palette selection was changed.
            else
            {
                if (!TilePlusPainterConfig.TpPainterShowPalettes) //probably redundant?
                    return;
                // Select a palette in the list. Silently fails if input is not a palette.
                //if (palettesListView.itemsSource is not IList<PaletteListItem> iItems)
                if(m_TpPainterAssetViewer!.PalettesItemSource is not IList<PaintableObject> iItems)
                    return;
                var items = iItems.ToList();
                var match = items.Find(pal => pal.ItemType == TpPaletteListItemType.Palette && pal.ItemName == item.ItemName);
                if (match == null)
                    return;
                var index = items.IndexOf(match);
                if (index == -1)
                    return;
                PainterWindow.DiscardListSelectionEvents = true;
                m_TpPainterAssetViewer.SetPaletteListSelection(index);
                PainterWindow.DiscardListSelectionEvents = false;
            }
        }

        /// <summary>
        /// Select a tile from the Editing list
        /// </summary>
        /// <param name="index">which item?</param>

        internal void SelectTile(int index)
        {
            if (index >= m_TpPainterAssetViewer!.TilesListViewItemCount)
                return;
            var item = m_TpPainterAssetViewer.GetTileListItemAtIndex(index);
            if(item != null)
                UseTileSelection(item,false);
        }


        /// <summary>
        /// Select a tile from the Editing list
        /// </summary>
        /// <param name="selection">the object to select</param>
        /// <param name = "clearMarquees" >Clear scene view marquees if true (default)</param>
        // ReSharper disable once MemberCanBePrivate.Global
        internal void UseTileSelection(object selection, bool clearMarquees = true)
        {
            if(clearMarquees)
                TpLibEditor.OnSelectionChanged(); //this cancels any marquees and/or lines
            
            if (selection is TilePlusBase tpb) 
            {
                var clipbd = TpPainterState.Clipboard;
                if(clipbd == null)
                    return;
                          
                if (tpb.ParentTilemap == null)
                    return;
                if (clipbd.Id != tpb.Id)
                {
                    var tname = tpb.TileName;
                    var dex   = tname.IndexOf('(');
                    if (dex != -1)
                        tname = tname[..dex];
                    m_TpPainterAssetViewer!.SetSelectionLabelText(tname);
                    TpPainterState.Clipboard = new TpPainterClipboard(tpb, tpb.TileGridPosition, tpb.ParentTilemap);
                }
                TpEditorUtilities.MakeHighlight(tpb, TilePlusConfig.instance.TileHighlightTime);
            }
            else if (selection is Tile tile && TpPainterState.PaintableMap != null)
            {
                var clipbd = TpPainterState.Clipboard;
                if (clipbd is { IsTile: true } && clipbd.Tile!.GetInstanceID() == tile.GetInstanceID())
                    return;

                m_TpPainterAssetViewer!.SetSelectionLabelText(tile.name);
                var map = TpPainterState.PaintableMap.TargetTilemap;
                TpPainterState.Clipboard = new TpPainterClipboard(tile, Vector3Int.zero, map);
            }
            //must be TileBase  NOTE that anything subclassed from TileBase needs a TpPainterPlugin
            else if (selection is TileBase ti && TpPainterState.PaintableMap != null)
            {
                var clipbd = TpPainterState.Clipboard;
                if (clipbd is { IsTileBase: true } && clipbd.Tile!.GetInstanceID() == ti.GetInstanceID())
                    return;

                m_TpPainterAssetViewer!.SetSelectionLabelText(ti.name);
                var map = TpPainterState.PaintableMap.TargetTilemap;
                TpPainterState.Clipboard = new TpPainterClipboard(ti, Vector3Int.zero, map);
            }
        }
        
        #endregion
        

        #region splitterGeometry
        /// <summary>
        /// adjust a splitter in a vertical splitview
        /// </summary>
        /// <param name="evt">The event</param>
        private void SourceSplitterFix(GeometryChangedEvent evt)
        {
            var handle = assetViewSplitter.Q<VisualElement>("unity-dragline-anchor");
            handle.style.width           = tpPainterAssetViewControls!.ScrollviewWidth;
            handle.style.height          = TilePlusPainterWindow.SplitterSize;
            handle.style.backgroundColor = Color.red;
            evt.StopPropagation();

        }
        #endregion
      
    }
}
