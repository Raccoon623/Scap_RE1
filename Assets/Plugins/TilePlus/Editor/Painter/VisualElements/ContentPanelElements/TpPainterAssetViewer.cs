// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 01-12-2024
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 01-14-2024
// ***********************************************************************
// <copyright file="TpPainterAssetViewer.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using static TilePlus.Editor.TpIconLib;

#nullable enable

namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// Main asset view panel. Has a number of sub-panels:
    /// TpPainterAssetViewControls, TpPainterBrushInspector for Paint Mode,
    /// TpPainterSelectionInspector for Edit mode.
    /// </summary>
    public class TpPainterAssetViewer : VisualElement
    {
        #region privateProperties
        
        //we want this to return null if the window isn't open. 
        private TilePlusPainterWindow? PainterWindow => TilePlusPainterWindow.RawInstance;
        
        private StyleColor EditModeAssetListViewOrigColor { get; }
        
        /// <summary>
        /// source for the list of tiles (center pane) when in EDIT view
        /// </summary>
        private List<TileBase> CurrentTileList =>
            PainterWindow == null
                ? new List<TileBase>()
                : PainterWindow.m_CurrentTileList;
        
        #endregion
        
        #region privateFields
        
        //a list of palettes
        /// <summary>
        /// The palettes
        /// </summary>
        private readonly List<PaintableObject> palettes = new();
        
        private readonly TpHelpBox mapSelectionNeededHelpBox;
        
        /// <summary>
        /// The Favorites palette item
        /// </summary>
        /// <remarks>place-holder for the Favorites 'palette' : note that this use of the parameterless ctor makes this a Favorites Palette.</remarks>
        private readonly PaintableObject favoritesPaletteItem = new();

        //Asset view (center pane)
        //the lists for the center column. Only one is visible, depending on Palette or Tilemaps mode.
        /// <summary>
        /// The Edit mode asset ListView
        /// </summary>
        private readonly TpListView tilesListView;
        /// <summary>
        /// The palettes ListView
        /// </summary>
        private readonly TpListView palettesListView;

        private readonly Label selectionLabel;
        private readonly Label headerLabel;

        //styles
        private readonly StyleColor           accentStyleColor;
        private readonly StyleEnum<FontStyle> boldStyle;
        private readonly StyleEnum<FontStyle> normalStyle;
        private readonly float                listItemHeight;
        /// <summary>
        /// The palette list item default color
        /// </summary>
        private StyleColor paletteListItemDefaultColor;
        
        #endregion
        
        #region publicProperties

        internal object SelectedTileObject => tilesListView.selectedItem;

        internal int TilesListSelectionIndex => tilesListView.selectedIndex;
        
        internal int SelectedPaletteIndex => palettesListView.selectedIndex;

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        internal int LastSelectedPaletteIndex { get; private set; } = -1;

        internal int PaletteListViewItemCount => palettesListView.itemsSource.Count;

        internal object PalettesItemSource => palettesListView.itemsSource;

        internal int TilesListViewItemCount => tilesListView.itemsSource.Count;
        
        #endregion

        #region ctor
        
        /// <summary>
        /// the center column display has a list of tiles or palettes
        /// </summary>
        /// <returns></returns>
        internal TpPainterAssetViewer(float listItemsHeight, float viewPanesMinWidth)
        {
            name           = "asset-view-panel";
            style.flexGrow = 1;
            
            accentStyleColor    = PainterWindow!.m_AccentStyleColor;
            boldStyle           = PainterWindow.m_BoldStyle;
            normalStyle         = PainterWindow.m_NormalStyle;
            this.listItemHeight = listItemsHeight;
            palettes.Clear();
            palettes.Add(favoritesPaletteItem);
            palettes.AddRange(TpPainterScanners.instance.CurrentPalettes);
            
            //in PALETTE mode this displays the list of Palettes, Chunks/TileFabs, and Favorites
            palettesListView = new TpListView(palettes,
                                              listItemHeight,
                                              true,
                                              MakePaletteListItem,
                                              BindPaletteListItem);
            palettesListView.unbindItem += UnbindTpListBoxItem;

            palettesListView.style.flexGrow = 1;
            palettesListView.style.minWidth = viewPanesMinWidth;
            palettesListView.name           = "palette-list-view";
            //Palettes View
            //at the top of the list is a label for the type of object,
            //and a second label for the selection's name.
            var assetViewDataPanel = new VisualElement
                                     {
                                         name = "source-view-outer-container",
                                         style =
                                         {
                                             flexGrow   = 1,
                                             flexShrink = 1,
                                             minWidth   = viewPanesMinWidth
                                         }
                                     };
            Add(assetViewDataPanel);
            
            headerLabel = new Label("Painting Source")
                                   {
                                       style =
                                       {
                                           alignSelf         = Align.Center, borderLeftWidth = 4, borderRightWidth = 2, borderTopWidth = 2,
                                           borderBottomWidth = 2
                                       }
                                   };

            assetViewDataPanel.Add(headerLabel);
            
            selectionLabel = new Label(TilePlusPainterWindow.EmptyFieldLabel)
                                      {
                                          style =
                                          {
                                              alignSelf         = Align.Center, borderLeftWidth = 4, borderRightWidth = 2, borderTopWidth = 2,
                                              borderBottomWidth = 2
                                          }
                                      };

            assetViewDataPanel.Add(selectionLabel);

            mapSelectionNeededHelpBox = new TpHelpBox("No selection. Choose a tilemap from the list in the left column",
                                                               "tilemap-list-needs-selection",
                                                               HelpBoxMessageType.Info) { style = { display = DisplayStyle.None }/*, visible = false*/ };
            assetViewDataPanel.Add(mapSelectionNeededHelpBox);
            assetViewDataPanel.Add(palettesListView);

            //tiles view

            //the second part of this panel is the Tiles view panel. This is used when in TILEMAP
            //mode, and displays a list of tiles from the selected tilemap (leftmost pane)
            tilesListView = new TpListView(CurrentTileList,
                                                   listItemHeight,
                                                   true,
                                                   MakeTileListItem,
                                                   BindTileListItem) {
                         unbindItem = UnbindTpListBoxItem,
                         style =
                         {
                             flexGrow = 1,
                             minWidth = viewPanesMinWidth,
                             display = new StyleEnum<DisplayStyle>(DisplayStyle.None)
                         },
                         name = "tiles-list-view"
                     };

            assetViewDataPanel.Add(tilesListView);
            EditModeAssetListViewOrigColor = tilesListView.style.color;
            schedule.Execute(() =>
                             {
                                 if (PainterWindow == null)
                                     return;
                                 var gMode          = TpPainterState.GlobalMode;
                                 if (gMode == GlobalMode.PaintingView)
                                 {
                                     Updater(palettesListView);
                                     var item = ((PaintableObject)palettesListView.selectedItem);
                                     if (item != null
                                         && palettesListView.selectedIndex != -1
                                         && selectionLabel.text == TilePlusPainterWindow.EmptyFieldLabel)
                                         selectionLabel.text = ((PaintableObject)palettesListView.selectedItem).ItemName;
                                 }
                                 else if (gMode == GlobalMode.EditingView)
                                 {
                                     Updater(tilesListView);
                                     var item = ((TileBase)tilesListView.selectedItem);
                                     
                                     if (item == null)  //note: don't OR these or potential nullref error when deref item in 'else' clause.
                                         selectionLabel.text = TilePlusPainterWindow.EmptyFieldLabel;
                                     else if(tilesListView.selectedIndex == -1)
                                         selectionLabel.text  = TilePlusPainterWindow.EmptyFieldLabel;
                                     else
                                     {
                                         // ReSharper disable once Unity.NoNullPatternMatching
                                         if (item is ITilePlus itp)
                                         {
                                             var s   = itp.TileName;
                                             var dex = s.IndexOf('(');
                                             if (dex != -1)
                                                 s = s[..dex];
                                             selectionLabel.text = s;
                                         }
                                         else
                                            selectionLabel.text = item.name;
                                     }

                                     /*if (item != null
                                         && tilesListView.selectedIndex != -1
                                         && selectionLabel.text == TilePlusPainterWindow.EmptyFieldLabel)
                                         selectionLabel.text = item.name;*/
                                 }
                             }).Every(100);

        }


        private void Updater(TpListView view)
        {
            var selected      = view.selectedItem;
            var selection     = selected != null;
            var selectedIndex = selection ? view.selectedIndex : -1;
            var cContainer    = view.Q<VisualElement>("unity-content-container");
            if (cContainer == null)
                return;
            foreach (var child in cContainer.Children())
            {
                if(child is TpListBoxItem item)
                    item.SetColor(item.m_Index == selectedIndex
                                      ? Color.white
                                      : Color.black);
            }
        }
        
        #endregion

        #region makebind
        
        /// <summary>
        /// PAINT mode: Make the list item to display information
        /// about a palette/chunk/favorites/tilefab for the center column 
        /// </summary>
        /// <returns>Visual Element</returns>
        private VisualElement MakePaletteListItem()
        {
            var item = new TpListBoxItem("imageWithLabelContainer", Color.black, 1f, 4f, PaletteListItemOnClickCallback);
            var h    = TilePlusPainterConfig.PainterListItemHeight;
            item.style.height = h;
            
            var iconHeight = h * 1.1f;
            item.Add(new Image
                     {
                         name = "image",
                         style =
                         {
                             paddingLeft = 8,
                             width       = iconHeight,
                             height      = iconHeight,
                             flexShrink  = 0
                         }
                     });
            //Label used for text
            var label = new Label
            {
                //tooltip = "Click to inspect",
                name    = "label",
                style =
                {
                    paddingRight = 8,
                    paddingLeft  = 2,
                    height       = listItemHeight,
                    overflow     = new StyleEnum<Overflow>(Overflow.Hidden),  //changed 12/23 for better appearance with long names and narrow columns. Next line too.
                    textOverflow = new StyleEnum<TextOverflow>(TextOverflow.Ellipsis)
                }
            };
            paletteListItemDefaultColor = label.style.color;
            item.Add(label);
            return item;
        }

        /// <summary>
        /// PAINT mode: The list item event handler calls this. 
        /// </summary>
        private void PaletteListItemOnClickCallback(int index, ClickEvent _)
        {
            if (PainterWindow == null)
                return;

            PainterWindow.m_TpPainterContentPanel.SelectPaletteOrOtherSource(index);
            LastSelectedPaletteIndex = index;
        }

        
        
        /// <summary>
        /// PAINT mode: Bind the palette list item
        /// </summary>
        /// <param name="element"></param>
        /// <param name="index"></param>
        private void BindPaletteListItem(VisualElement element, int index)
        {
            if (element is TpListBoxItem listBox)
            {
                listBox.m_Index = index;
                listBox.ResetColor();
            }

            //get the label element (text) and the icon element
            var labl      = element.Q<Label>("label");
            labl.style.color = paletteListItemDefaultColor;
            var img       = element.Q<Image>("image");
            var container = element.Q<VisualElement>("imageWithLabelContainer");
            container.tooltip               = string.Empty;

            if(index < 0 || index >= palettes.Count)
            {
                labl.text = "Index of list item out of array range!";
                return;
            }
            
            var item = palettes[index]; 
            if (item == null)
            {
                labl.text = "?";
                return;
            }

            var num = item.Count;
            
            switch (item.ItemType)
            {
                case TpPaletteListItemType.Palette:
                {
                    var palette = item.Palette;
                    img.image = FindIcon(TpIconType.PaletteIcon);
                    if (palette == null)
                        labl.text = "Invalid or null Palette!";
                    else if (num == -1)  //TpNoPaint installed on large palette to avoid blocking due to GetUsedTilesCount()
                        labl.text = $"{palette.name}";
                    else
                        labl.text = $"{palette.name}: {num} tile{(num!=1 ? "s" : string.Empty)}";
                    container.tooltip = "Click to view contents.";
                    break;
                }
                //tilefab chunk.
                case TpPaletteListItemType.Bundle:
                    var numPrefabs = item.Bundle != null
                                         ? item.Bundle.m_Prefabs.Count : 0;
                    container.tooltip = labl.text = $"{item.ItemName}: {num} tile{(num != 1 ? "s" : string.Empty)} {numPrefabs} prefab{(num != 1 ? "s" : string.Empty)}";
                    if (item.Bundle!=null && item.Bundle.AssetVersion > 2 && item.Bundle.m_Icon != null)
                        img.sprite = item.Bundle.m_Icon;
                    else
                        img.image = FindIcon(TpIconType.TpTileBundleIcon);
                    break;
                case TpPaletteListItemType.TileFab:
                    container.tooltip = labl.text = $"{item.ItemName}: {num} Bundle{(num !=1 ? "s" : string.Empty)}" ;
                    if (item.TileFab != null && item.TileFab.AssetVersion > 2 && item.TileFab.m_Icon != null)
                        img.sprite = item.TileFab.m_Icon;
                    else
                        img.image = FindIcon(TpIconType.TileFabIcon);
                    break;

                case TpPaletteListItemType.Favorites:
                    container.tooltip = "Favorites list is always available. \nAdd to it by clicking 'F' in the Clipboard -or- \n\n1. Hold down CTRL when clicking on a tile with the PICK tool.\n\n2. Select one or more tile assets or Prefabs in the Project folder and use the Assets or right-click context menu item 'Add To Painter Favorites' \n\nClear Favorites with Tools/TilePlus/Clear Painter Favorites.";
                    labl.style.color  = Color.red;
                    num               = TilePlusPainterFavorites.instance.FavoritesListSize;
                    labl.text         = $"{item.ItemName}: {num} item{(num !=1 ? "s" : string.Empty)}";
                    img.image         = FindIcon(TpIconType.ClipboardIcon);
                    break;
            }
        }
        
        /// <summary>
        /// EDIT mode: a list item for list of tiles (in center pane) from the selected tilemap in leftmost pane.
        /// </summary>
        /// <returns>Visual Element</returns>
        private VisualElement MakeTileListItem()
        {
            //container
            var item       = new TpListBoxItem("imageWithLabelContainer", Color.black, 1f, 4f, TileListOnClickCallback);
            var h          = TilePlusPainterConfig.PainterListItemHeight;
            var iconHeight = h - 2;
            item.style.height = h;
            //item.Add(new Image {name = "imageL", style = {paddingLeft = 8, width = iconHeight, height = iconHeight, flexShrink = 0}});
            item.Add(new Image {name = "imageR", style = {paddingLeft = 2, width = iconHeight, height = iconHeight, flexShrink = 0}});

            //Label used for text
            item.Add(new Label
            {
                tooltip = "Click to inspect",
                name    = "label",
                style =
                {
                    paddingRight                         = 2,
                    paddingLeft                          = 2,
                    height                               = listItemHeight,
                    overflow                             = new StyleEnum<Overflow>(Overflow.Hidden), //changed 12/23 for better appearance with long names and narrow columns. Next line too.
                                            textOverflow = new StyleEnum<TextOverflow>(TextOverflow.Ellipsis) }
            });
            
            return item;
        }

        /// <summary>
        /// EDIT mode: list item event callback
        /// </summary>
        /// <param name="index"></param>
        /// <param name="_"></param>
        private void TileListOnClickCallback(int index, ClickEvent _)
        {
            if(index < tilesListView.itemsSource.Count && PainterWindow != null)
                PainterWindow.m_TpPainterContentPanel.UseTileSelection(tilesListView.itemsSource[index]);
        }

        /// <summary>
        /// This is used when in EDIT mode: a list item for list of tiles from the selected tilemap (as chosen from leftmost pane)
        /// </summary>
        private void BindTileListItem(VisualElement element, int index)
        {
            if (element is TpListBoxItem listBox)
            {
                listBox.m_Index = index;
                listBox.ResetColor();
            }

            //get the label element (text) and the icon element
            var labl = element.Q<Label>("label");
            //var imgL = element.Q<Image>("imageL");
            var imgR = element.Q<Image>("imageR");
            
            var item      = CurrentTileList[index];
            // ReSharper disable once Unity.NoNullPatternMatching
            var itemIsTpb = item && item is TilePlusBase;
            labl.style.color = PainterWindow!.TilemapPaintTargetCount > TilePlusPainterConfig.MaxTilesForViewers
                                   ? new StyleColor(Color.yellow)
                                   : itemIsTpb ? accentStyleColor : EditModeAssetListViewOrigColor;

            labl.style.unityFontStyleAndWeight = itemIsTpb ? boldStyle : normalStyle;
            
            // ReSharper disable once Unity.NoNullPatternMatching
            if (item && item is TilePlusBase tpb) 
            {
                var tname = tpb.TileName;
                var dex = tname.IndexOf('(');
                if (dex != -1)
                    tname = tname.Substring(0, dex);

                labl.text =
                    $"{tname} {(TilePlusPainterConfig.TpPainterShowIid ? $"{tpb.TileGridPosition.ToString()} [id: {tpb.Id.ToString()}] " : tpb.TileGridPosition.ToString())}";
                
                Sprite? sprt = ((ITilePlus)tpb).EffectiveSprite;
                imgR.sprite = sprt != null
                                  ? sprt
                                  : tpb.sprite;
            }
            else
            {
                labl.text = TilePlusPainterConfig.TpPainterShowIid
                                ? $"{item.name} [id: {item.GetInstanceID().ToString()}]"
                                : item.name;
                // ReSharper disable once Unity.NoNullPatternMatching
                if (item && item is Tile t)
                    imgR.sprite = t.sprite;
                // ReSharper disable once Unity.NoNullPatternMatching
                // ReSharper disable once ConvertTypeCheckToNullCheck
                else if (item && item is TileBase)
                {
                    imgR.sprite = TpPreviewUtility.TryGetPlugin(item, out var plug) && plug != null
                                      ? plug.GetSpriteForTile(item)                           
                                      : FindIconAsSprite(TpIconType.UnityToolbarMinusIcon); 

                }
                
            }

        }

        // unbind list box item callback when item is released.        
        private void UnbindTpListBoxItem(VisualElement element, int index)
        {
            var item = element.Q<TpListBoxItem>("tilemap-list-item");
            item?.DoUnregisterCallback();
        }
        
        #endregion
        
        #region access
        
       
        
        
        
        /// <summary>
        /// refresh the Palettes list in Paint mode
        /// </summary>
        internal void RefreshPalettesListView()
        {
            palettes.Clear();
            palettes.Add(favoritesPaletteItem); 
            palettes.AddRange(TpPainterScanners.instance.CurrentPalettes);
            palettesListView.Rebuild();
        }

        /// <summary>
        /// Set the selection label text
        /// </summary>
        /// <param name="text"></param>
        internal void SetSelectionLabelText(string text)
        {
            selectionLabel.text = text;
        }

        /// <summary>
        /// Set the header label text
        /// </summary>
        /// <param name="text"></param>
        internal void SetHeaderLabelText(string text)
        {
            headerLabel.text = text;
        }

        /// <summary>
        /// Set the selection for the list of palettes
        /// </summary>
        /// <param name="index"></param>
        internal void SetPaletteListSelection(int index)
        {
            LastSelectedPaletteIndex = index;
            palettesListView.SetSelection(index);
        }
        
        /// <summary>
        /// Show the prompt for user to select a Tilemap
        /// </summary>
        /// <param name="show"></param>
        internal void ShowMapSelectionNeededHelpbox(bool show)
        {
            mapSelectionNeededHelpBox.style.display = show
                                                                   ? DisplayStyle.Flex
                                                                   : DisplayStyle.None;
            if(show)
                selectionLabel.text = TilePlusPainterWindow.EmptyFieldLabel;    
            
        }

        /// <summary>
        /// Show/hide palettes list view
        /// </summary>
        /// <param name="show">true/false = show/hide</param>
        internal void ShowPalettesListView(bool show)
        {
            palettesListView.style.display = show
                                                 ? DisplayStyle.Flex
                                                 : DisplayStyle.None;
        }

        /// <summary>
        /// Show/hide Tiles list view
        /// </summary>
        /// <param name="show">true/false = show/hide</param>
        internal void ShowTilesListView(bool show)
        {
            tilesListView.style.display = show
                                                      ? DisplayStyle.Flex
                                                      : DisplayStyle.None;
        }

        /// <summary>
        /// Get the palette list item at an index
        /// </summary>
        /// <param name="index">index value</param>
        /// <returns>Palette list item ref</returns>
        internal PaintableObject? GetPaletteListItemAtIndex(int index)
        {
            if (index >= palettesListView.itemsSource.Count)
                return null;
            return palettesListView.itemsSource[index] as PaintableObject;
        }

        /// <summary>
        /// Get the tile list item at an index
        /// </summary>
        /// <param name="index">index value</param>
        /// <returns>object</returns>
        internal object? GetTileListItemAtIndex(int index)
        {
            return index >= tilesListView.itemsSource.Count
                       ? null
                       : tilesListView.itemsSource[index];
        }

        /// <summary>
        /// Set the tiles list selection
        /// </summary>
        /// <param name="index">index of selection</param>
        internal void SetTilesListViewSelection(int index)
        {
            tilesListView.SetSelection(index);
        }

        /// <summary>
        /// Rebuild the tiles list view in EDIT mode.
        /// </summary>
        internal void RebuildEditModeTilesListView()
        {
            tilesListView.ClearSelection();
            tilesListView.Rebuild();
        }

       
        
        #endregion
        
    }
}
