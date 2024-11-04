// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 11-05-2022
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-31-2022
// ***********************************************************************
// <copyright file="TpPainterTilemapsPanel.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Panel with list of tilemaps</summary>
// ***********************************************************************

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
#nullable enable

namespace TilePlus.Editor.Painter
{

    /// <summary>
    /// TpPainterTilemapsPanel creates the list of tilemaps in the left col of Tile+Painter
    /// Implements the <see cref="VisualElement" />
    /// </summary>
    /// <seealso cref="VisualElement" />
    internal class TpPainterTilemapsPanel : VisualElement
    {
        #region constants
        private const string DefaultTooltip = "['+NZ':origin != 0]";
        #endregion
        
        #region privateFields
        private readonly Label          tilemapsListHeader;
        private readonly Label          tilemapsListSelectionLabel;
        private readonly TpListView     tilemapsListView;
        
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private TilePlusPainterWindow Win => TilePlusPainterWindow.instance!;
        
        //styles
        private readonly StyleColor           inStageColor     = new(new Color(1, 0, 0, 0.5f));
        private readonly StyleColor           accentStyleColor = new(Color.cyan);
        private          StyleColor           normalStyleColor;
        private readonly StyleColor           nullStyleColor = new(StyleKeyword.Null);
        private readonly StyleEnum<FontStyle> boldStyle      = new(FontStyle.Bold);
        private readonly StyleEnum<FontStyle> normalStyle    = new(FontStyle.Normal);


        #endregion
        
        #region publicProperties
        /// <summary>
        /// Gets the data source of the list view.
        /// </summary>
        /// <value>The data source of the list view.</value>
        internal IList DataSource => tilemapsListView.itemsSource;
        
        /// <summary>
        /// The current selection index
        /// </summary>
        internal int SelectionIndex => tilemapsListView.selectedIndex;
        
        #endregion
        
        

        #region ctor
        
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="dataSource">data source for listview.</param>
        /// <param name="viewPanesMinWidth">Minimum width of the view panes.</param>
        internal TpPainterTilemapsPanel(List<TilemapData>          dataSource,
                                        float                       viewPanesMinWidth)
        {
            
            TpPainterState.OnPainterModeChange += SetTilemapsListHeader;
            
            //listview for Tilemaps is in the leftmost pane
            name           = "tilemap-list-outer-container";
            style.minWidth = viewPanesMinWidth;
            style.fontSize = TilePlusPainterConfig.ContentPanelFontSize;

            //add a label for the selected tilemap
            Add( tilemapsListHeader =new Label("Tilemaps")
                {
                    style =
                    {
                        alignSelf         = Align.Center, borderLeftWidth = 2, borderRightWidth = 2, borderTopWidth = 2,
                        borderBottomWidth = 2
                    }
                });

          
            //this label shows the name of the selected tilemap
            tilemapsListSelectionLabel = new Label(TilePlusPainterWindow.EmptyFieldLabel)
                                         {
                                             style =
                                             {
                                                 alignSelf         = Align.Center, borderLeftWidth = 4, borderRightWidth = 2, borderTopWidth = 2,
                                                 borderBottomWidth = 2
                                             }
                                         };
            Add(tilemapsListSelectionLabel);

            //this listview has the names of the tilemaps
            tilemapsListView = new TpListView(dataSource,
                                              TilePlusPainterConfig.PainterListItemHeight,
                                               true,
                                               MakeTilemapListItem,
                                               BindTilemapsListItem);

            tilemapsListView.itemsChosen                     += OnItemsChosen;
            tilemapsListView.selectionChanged                += OnTilemapListViewSelectionChange;
            tilemapsListView.style.flexGrow                  =  1;
            tilemapsListView.unbindItem += UnbindTilemapListItem;
            tilemapsListView.schedule.Execute(() =>
                                              {
                                                if(DataSource.Count == 0)
                                                    Win.UpdateTilemaps();
                                              }).Every(1000);
            Add(tilemapsListView);
            schedule.Execute(Updater).Every(500);
        }

        private void Updater()
        {
            var selected      = tilemapsListView.selectedItem;
            var selection     = selected != null;
            var selectedIndex = selection ? tilemapsListView.selectedIndex : -1;
            var cContainer    = tilemapsListView.Q<VisualElement>("unity-content-container");
            var numItems      = 0;
            if (cContainer != null)
                numItems = cContainer.childCount;

            for(var i = 0; i < numItems; i++)
            {
                if (tilemapsListView.GetRootElementForIndex(i) is TpListBoxItem item)
                {
                    item.SetColor(i == selectedIndex
                                      ? Color.white
                                      : Color.black);
                }

            }
        }
        
        
        #endregion

        #region  events

        /// <summary>
        /// In EDIT mode this is called when DBL clicking on a Tile in the center column.
        /// Unlike OnBrushInspectorListViewSelectionChange this just checks to see
        /// if the selection is the same as before, and if it is the highlight is shown.
        /// Basically a convenience function
        /// </summary>
        /// <param name="objs">selected items</param>
        private void OnItemsChosen( IEnumerable<object>? objs)
        {
            if (Win.DiscardListSelectionEvents || objs == null || !objs.Any())
                return;

            if (objs.First() is not TilemapData ttd)
                return;
            
            SetTarget(ttd);
            Updater();
        }

        private void OnTilemapListViewSelectionChange( IEnumerable<object>? objs)
        {
            if (Win.DiscardListSelectionEvents || objs == null || !objs.Any())
                return;

            if (objs.First() is not TilemapData ttd)
                return;

            SetTarget(ttd);
        }

        #endregion

        #region access

        internal void SetTarget(int index)
        {
            SetTarget((TilemapData) tilemapsListView.itemsSource[index]);
        }

        private void SetTarget(TilemapData ttd)
        {
            if(Win.SetPaintTarget(ttd.TargetMap) && !TpPainterState.InPaintMode)
                Win.SetEditModeInspectorTarget(ttd.TargetMap);

            if (!TilePlusPainterConfig.TpPainterSyncSelection)
                return;

            //if map is null or if the active selection wouldn't be changed just return
            if (ttd.TargetMap == null || Selection.activeGameObject == ttd.TargetMap.gameObject)
                return;
            var go = ttd.TargetMap.gameObject;
            Selection.SetActiveObjectWithContext(go, go); 
        }

        /// <summary>
        /// Sets the tilemaps list header.
        /// </summary>
        internal void SetTilemapsListHeader(GlobalMode mode, TpPainterTool _, TpPainterMoveSequenceStates __)
        {
        
            var text = mode switch
                       {
                           GlobalMode.EditingView  => "Editing Tilemap",
                           GlobalMode.PaintingView => "Painting Tilemap",
                           _                       => "GridSelection Target"
                       };
        
            tilemapsListHeader.text = text;
        }

        /// <summary>
        /// Sets the selection label.
        /// </summary>
        /// <param name="text">The text.</param>
        internal void SetSelectionLabel(string? text)
        {
            if(text != null)
                tilemapsListSelectionLabel.text = text;
        }

        /// <summary>
        /// Sets the list selection.
        /// </summary>
        /// <param name="selectionIndex">Index of the selection.</param>
        internal void SetSelection(int selectionIndex)
        {
            tilemapsListView.SetSelection(new[] { selectionIndex });
        }

        /// <summary>
        /// Sets the selection without notify.
        /// </summary>
        /// <param name="selectionIndex">Index of the selection.</param>
        internal void SetSelectionWithoutNotify(int selectionIndex)
        {
            tilemapsListView.SetSelectionWithoutNotify(new[] { selectionIndex });
            Updater();
        }

        /// <summary>
        /// Updates the tilemaps list.
        /// </summary>
        /// <param name="dataSource">The data source.</param>
        internal void UpdateTilemapsList(List<TilemapData> dataSource)
        {
            tilemapsListView.itemsSource = dataSource;
            tilemapsListView.Rebuild();
        }
        
        /// <summary>
        /// Clears the selection.
        /// </summary>
        internal void ClearSelection()
        {
            tilemapsListView.ClearSelection();
        }

        /// <summary>
        /// Rebuilds the element.
        /// </summary>
        internal void RebuildElement()
        {
            tilemapsListView.Rebuild();
        }

        /// <summary>
        /// Rebind all items
        /// </summary>
        internal void ReBindElement(List<TilemapData> src)
        {
            tilemapsListView.itemsSource = src;
            tilemapsListView.RefreshItems();
        }

        
        
       
        #endregion

        
        #region makebind
        
        
        private VisualElement MakeTilemapListItem()
        {
            //used in MakeTilemapListItem
            var imageWidth  = TilePlusPainterConfig.PainterListItemHeight * 0.75f;
            //container
            var item = new TpListBoxItem("tilemap-list-item", Color.black, 1f,4f, OnClickCallback); 
           
            var rightImage = new Image
                            {   
                                name    = "rightimage",
                                tooltip = "If visible, the Tilemap is part of a Prefab: can't be edited unless in a Prefab stage, if in a Prefab stage the icon is red.",
                                image   = TpIconLib.FindIcon(TpIconType.PrefabIcon),
                                style =
                                {
                                    width     = imageWidth ,
                                    height    = imageWidth,
                                    minHeight = imageWidth,
                                    minWidth  = imageWidth,
                                    marginRight = 1
                                }
                            };
            
            //Label used for text
            var label = new Label { name = "label", 
                style =
                {
                    flexGrow = 1, 
                    unityTextAlign = TextAnchor.MiddleLeft,
                    marginRight  = 2,
                    overflow     = new StyleEnum<Overflow>(Overflow.Hidden), //changed 12/23 for better appearance with long names and narrow columns. Next line too.
                    //this wipes out tooltips! textOverflow = new StyleEnum<TextOverflow>(TextOverflow.Ellipsis)
                } };
            normalStyleColor = label.style.color;
            item.Add(label);
            item.Add(rightImage);
            return item;
        }

        private void OnClickCallback(int index, ClickEvent _)
        {
            SetTarget(index);
        }

        private void BindTilemapsListItem( VisualElement element, int index)
        {
            if (element is TpListBoxItem listBox)
                listBox.m_Index = index;
            else
                return;
            
            //get the label element (text) and the icon element
            var labl      = element.Q<Label>("label");
            var rightIcon = element.Q<Image>("rightimage"); //get the image

            if (TpPainterScanners.instance.NumCurrentTilemapListViewItems <= index) //avoids unlikely indexing exception
                return;
            
            var item      = TpPainterScanners.instance.TilemapListViewItems[index];
            var parentMap = item.TargetMap;
            
            var nScenes   = EditorSceneManager.loadedRootSceneCount;
            var gridName = string.Empty;
            if (TpPainterScanners.instance.MoreThanOneGrid)
            {
                if (TpPainterScanners.instance.GetGridForTilemap(parentMap, out var grid))
                {
                    gridName = $"{grid!.name}.";
                }
            }

            //updated in 2.02 to show ???? for unsaved new scenes.
            var sceneName = item.ParentSceneOfMap.name;
            
            var offset       = parentMap.transform.position != Vector3.zero ? "+NZ" : string.Empty;
            var numDiffTiles = parentMap.GetUsedTilesCount();
            var numstring = numDiffTiles == 0
                                ? ""
                                : $"#{numDiffTiles.ToString()}";
            labl.text = nScenes == 1 || !TpPainterScanners.instance.MoreThanOneGrid
                            ? $" {gridName}{parentMap.name}{offset} {numstring}"
                            : $"{(string.IsNullOrEmpty(sceneName) ? "????" : sceneName) }.{gridName}{parentMap.name}{offset} {numstring}";
            labl.style.color = item.HasTptTilesInMap
                                   ? accentStyleColor
                                   : normalStyleColor;
            labl.style.unityFontStyleAndWeight = item.HasTptTilesInMap ? boldStyle : normalStyle;  
            
            var ttip = $"{item.RendererInfo}{DefaultTooltip}";
            labl.tooltip    = ttip;
            listBox.tooltip = ttip;
            
            
            var inStage = item.InPrefabStage;

            element.style.backgroundColor = inStage ? inStageColor : nullStyleColor;

            rightIcon.style.opacity = item.InPrefab || inStage ? 1f : 0f;

            rightIcon.style.backgroundColor = inStage
                                                  ? inStageColor
                                                  : nullStyleColor;
        }

        private void UnbindTilemapListItem(VisualElement element, int index)
        {
            var item = element.Q<TpListBoxItem>("tilemap-list-item");
            item?.DoUnregisterCallback();
        }
        
        #endregion        
        
    }
}
