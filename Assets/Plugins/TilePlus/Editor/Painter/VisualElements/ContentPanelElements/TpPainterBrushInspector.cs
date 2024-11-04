// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 01-12-2024
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 01-14-2024
// ***********************************************************************
// <copyright file="TpPainterBrushInspector.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using static TilePlus.Editor.TpIconLib;
#nullable enable

namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// "Brush" inspector for Painter. Shows asset info for tiles, prefabs, bundles, etc.
    /// </summary>
    public class TpPainterBrushInspector : VisualElement
    {
        #region constants
        /// <summary>
        /// The palette target item border width
        /// </summary>
        private const float PaletteTargetItemBorderWidth = 4;

        private const float ImguiAreaMinHeight = 75;
        
        #endregion
        
        #region privateProperties
        
        private TilePlusPainterWindow PainterWindow => TilePlusPainterWindow.RawInstance!;

        
        /// <summary>
        /// Get a size for the palette target image.
        /// </summary>
        /// <value>The size of the palette target image.</value>
        private float PaletteTargetImageSize => TilePlusPainterConfig.PainterPaletteItemImageSize + PaletteTargetItemBorderWidth;

        /// <summary>
        /// Is the brush inspector splitter set up properly.
        /// </summary>
        /// <value>T/F</value>

        internal bool Valid => inspectorViewSplitter.Valid;
    
        #endregion        
        
        #region privateFields

        /// <summary>
        /// flag set TRUE when an AssetPreview is used but didn't return an image right away.
        /// </summary>
        private bool requiresRebuild;

        /// <summary>
        /// Flag set true when painter mode had changed.
        /// </summary>
        private bool painterModeChanged = true;
        
        /// <summary>
        /// The inspector list selection label
        /// </summary>
        private readonly Label listSelectionLabel;
        /// <summary>
        /// The inspector list type-of-object label.
        /// </summary>
        private readonly Label listObjectTypeLabel;
        /// <summary>
        /// The tiles information container
        /// </summary>
        private readonly VisualElement tileInfoContainer;
        /// <summary>
        /// The inspector view splitter
        /// </summary>
        private readonly TpSplitter inspectorViewSplitter;
        /// <summary>
        /// The Paint mode ListView: tiles, chunks, fabs, prefabs, etc. 
        /// </summary>
        internal readonly TpListView m_BrushInspectorListView;
        /// <summary>
        /// Paint mode tiles Palette view
        /// </summary>
        private readonly TpPainterPalette paletteView;
        /// <summary>
        /// The tile display GUI
        /// </summary>
        private readonly IMGUIContainer brushInspectorGui;
        /// <summary>
        /// cached color for tilesListView color changes when palette lists get too long.
        /// </summary>
        private readonly StyleColor brushInspectorTilesListViewOriginalColor;


        //styles
        private          StyleColor           originalColorForTileItems;
        private readonly StyleColor           accentStyleColor;
        private readonly StyleEnum<FontStyle> boldStyle;
        private readonly StyleEnum<FontStyle> normalStyle;
        #endregion

        #region publicProperties

        /// <summary>
        /// Splitter fixed panel height calcs
        /// </summary>
        /// <value>The height to use.</value>

        internal float FixedPanelHeight
        {
            get => brushInspectorGui.style.height.value.value;
            set
            {
                if (value < ImguiAreaMinHeight)
                    value = ImguiAreaMinHeight;
                brushInspectorGui.style.height = new StyleLength(value);
            }
        }

        /// <summary>
        /// The Brush Inspector list view index.
        /// </summary>
        internal int BrushInspectorListViewSelectedIndex => m_BrushInspectorListView.selectedIndex;

        /// <summary>
        /// returns true if the palette view is instantiated and is currently active.
        /// </summary>
        internal bool BrushInspectorUnityPaletteActive => paletteView is { IsActive: true };

        
        #endregion
        
        #region ctor
        /// <summary>
        /// Ctor: Builds the inspector UI (RIGHTmost column)
        /// for palette items (Paint mode)
        /// </summary>
        /// <param name="viewPanesMinWidth">min width of panel</param>
        /// <param name="listItemHeight">height of list item containers</param>
        internal TpPainterBrushInspector(float viewPanesMinWidth, float listItemHeight)
        {
            name             = "asset-view-brush-inspector";
            accentStyleColor = PainterWindow.m_AccentStyleColor;
            boldStyle        = PainterWindow.m_BoldStyle;
            normalStyle      = PainterWindow.m_NormalStyle;
            style.flexGrow   = 1;
            style.flexShrink = 1;
            style.minWidth   = viewPanesMinWidth;
            
            //the vertical split view gets added to this container.
            inspectorViewSplitter = new TpSplitter("painter-splitview-tilesinfo",
                                                    "TPT.TPPAINTER.SPLITVIEW.BRUSHINSPECTOR",
                                                    150,
                                                    TwoPaneSplitViewOrientation.Vertical,
                                                    1, BrushInspectorSplitterFix);
            
            Add(inspectorViewSplitter); //split view added to the container for this pane (which itself is the RH side of a splitview )
            
            //the top element is a container with two labels and a list
            var inspectorListContainer = new VisualElement
                                        {
                                            name = "inspector-list-container",
                                            style =
                                            {
                                                minWidth  = viewPanesMinWidth,
                                                minHeight = 150,
                                                flexGrow  = 1
                                            }
                                        };
            
            inspectorViewSplitter.Add(inspectorListContainer); //added to splitview as fixed-pane
            
            //here's a container for the bottom (fixed) part of the split view
            //and the palette list container is not displayed.
            tileInfoContainer = new VisualElement
                                                      {
                                                          name = "paint-inspector-scrollview-container",
                                                          style =
                                                          {
                                                              minWidth = viewPanesMinWidth,
                                                              minHeight = 100,
                                                              flexGrow = 1
                                                          }
                                                      };
                
                
           //the container includes a scrollview     
           var brushInspectorScrollView = new ScrollView(ScrollViewMode.VerticalAndHorizontal)
                                             {
                                                 name = "tiles-info-container", style =
                                                 {
                                                     flexGrow = 1,
                                                     minWidth                = viewPanesMinWidth,
                                                     minHeight               = 80,
                                                     borderTopWidth          = 6,
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
            brushInspectorScrollView.AddToClassList("unity-collection-view--with-border");
            brushInspectorScrollView.AddToClassList("unity-collection-view__scroll-view");
            tileInfoContainer.Add(brushInspectorScrollView);
            inspectorViewSplitter.Add(tileInfoContainer); //container w bottom portion of splitview added to splitview


            //now build the List view which is the upper portion of the splitview

            //header label
            listObjectTypeLabel = (new Label("-----")
                                 {
                                     name = "inspector-list-label",
                                     style =
                                     {
                                         alignSelf         = Align.Center,
                                         borderLeftWidth   = 4,
                                         borderRightWidth  = 2,
                                         borderTopWidth    = 2,
                                         borderBottomWidth = 2
                                     }
                                 });

            inspectorListContainer.Add(listObjectTypeLabel);
            //a label for the name of the selected tile
            listSelectionLabel = new Label(TilePlusPainterWindow.EmptyFieldLabel)
                                           {
                                               name = "inspector-list-tilename",
                                               style =
                                               {
                                                   alignSelf         = Align.Center,
                                                   borderLeftWidth   = 4,
                                                   borderRightWidth  = 2,
                                                   borderTopWidth    = 2,
                                                   borderBottomWidth = 2
                                               }
                                           };

            inspectorListContainer.Add(listSelectionLabel);

            
            
            //the list-view for the tiles/fabs/chunks -> RH column
            m_BrushInspectorListView = new TpListView(PainterWindow.PaintModeInspectableObjects,
                                                         listItemHeight,
                                                  true,
                                                  MakePaintModeInspectorListItem,
                                                  BindPaintModeInspectorListItem)
                                  {
                                      name = "palette-item-list-view", style = { flexGrow = 1 }
                                  };
            m_BrushInspectorListView.unbindItem += UnbindTpListBoxItem;
            
            brushInspectorTilesListViewOriginalColor = m_BrushInspectorListView.style.color;
            SetPaletteModeTilesListViewHighlite();
            
            m_BrushInspectorListView.Q<VisualElement>("unity-content-container").style.flexGrow =  1;

            inspectorListContainer.Add(m_BrushInspectorListView);   //add to container

            //new in 2.1 - a palette!
            paletteView = new TpPainterPalette(PaletteSelectionHandler);
            inspectorListContainer.Add(paletteView);
            paletteView.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            schedule.Execute(() =>
                             {
                                 if(PainterWindow ==null || !painterModeChanged || !TpPainterState.InPaintMode)
                                     return;
                                 painterModeChanged = false;
                                 SelectView(true);
                             }).Every(16);

            schedule.Execute(UpdateEvent).Every(200);
            
            //just once. Needed for Unity6 and later. Doesn't hurt for earlier versions.
            schedule.Execute(() => paletteView.MarkDirtyRepaint()).StartingIn(200);

            //the container contains an IMGUI view
            brushInspectorGui                            = new IMGUIContainer(TpPainterDataGUI.ImitateBrushInspector);
            brushInspectorGui.cullingEnabled             = true;
            brushInspectorGui.style.paddingBottom        = 10;
            brushInspectorGui.style.minHeight            = ImguiAreaMinHeight;
            brushInspectorScrollView.Add(brushInspectorGui); //add to container
            TpPainterState.OnPainterModeChange += OnPainterModeChange;
            TpLib.DelayedCallback(PainterWindow,()=>SelectView(),"T+P:BrushInsp initial select view",20);
            
        }

        private void OnPainterModeChange(GlobalMode _, TpPainterTool __, TpPainterMoveSequenceStates ___)
        {
            painterModeChanged = true;  
        }


        private void UpdateEvent()
        {
            if (!paletteView.IsActive)
                ItemHighlighter(m_BrushInspectorListView);

            if(!requiresRebuild)
                return;
            if (AssetPreview.IsLoadingAssetPreviews())
                return;
            RebuildBrushInspectorListView();
            requiresRebuild = false;
        }


        private void ItemHighlighter(TpListView view)
        {
            var selected      = view.selectedItem;
            var selection     = selected != null;
            var selectedIndex = selection ? view.selectedIndex : -1;
            var cContainer    = view.Q<VisualElement>("unity-content-container");
            if (cContainer == null)
                return;
            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (var child in cContainer.Children())
            {
                var item = child.Q<TpListBoxItem>("palette-inspector-list-item-item");
                item?.SetColor(item.m_Index == selectedIndex
                                   ? Color.white
                                   : Color.black);
            }
        }




        /// <summary>
        /// Select the Painting view to List or UTE-palette view
        /// </summary>
        internal void SelectView(bool isFromSchedule = false)
        {
            
            if (PainterWindow == null || !TpPainterState.InPaintMode)
                return;

            var paintableObj        = TpPainterState.PaintableObject;
            var paintableObjNotNull = paintableObj != null;
            
            if (isFromSchedule)
            {
                if (paintableObjNotNull && paintableObj!.ItemType != TpPaletteListItemType.Palette)
                    return;
            }
            
            
            if (paintableObjNotNull
                && paintableObj!.Palette != null
                && TilePlusPainterConfig.TpPainterShowPalettes
                && TilePlusPainterConfig.TpPainterShowPaletteAsGrid)
               /*// ReSharper disable once Unity.NoNullPatternMatching
               Config is { TpPainterShowPalettes: true, TpPainterShowPaletteAsGrid: true })*/
            {
                if (paletteView.IsActive) //if already active don't do this.
                    return;
                m_BrushInspectorListView.style.display = DisplayStyle.None;
                m_BrushInspectorListView.SendToBack();
                paletteView.style.display              = DisplayStyle.Flex;
                paletteView.IsActive                   = true;
                paletteView.BringToFront();
                
            }
            else
            {
                m_BrushInspectorListView.style.display = DisplayStyle.Flex;
                m_BrushInspectorListView.BringToFront();
                paletteView.style.display              = DisplayStyle.None;
                paletteView.IsActive                   = false;
                paletteView.SendToBack();
                
            }
        }

        #endregion
        
        #region handlers
        
        //callback for the TpPainterPalette visual element when a tile is selected.
        // ReSharper disable once AnnotateCanBeNullParameter
        private void PaletteSelectionHandler(Tilemap map, GridBrush.BrushCell[]? tileCells, BoundsInt position, Vector3Int pivot, GridBrush brush)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (tileCells == null || tileCells.Length == 0 || (tileCells.Length == 1 && tileCells[0]==null))
            {
                PainterWindow.ClearClipboard();
                ClearBrushInspectorSelection();
                return;
            }

            var output = new List<TileCell>(tileCells.Length);

            foreach (var gridPosition in position.allPositionsWithin)
            {
                var                  local = gridPosition - position.min;
                var cell  = tileCells[brush.GetCellIndexWrapAround(local.x, local.y, local.z)];
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                /*if (cell == null || cell.tile == null)
                    continue;*/
                output.Add(new TileCell(cell, local));
            }

            var num = output.Count;
            if (num == 0)
            {
                if(PainterWindow != null && PainterWindow.Informational)
                    TpLib.TpLog("Post-evaluation Selection was empty in Painter's palette window.");
                TpPainterState.Clipboard = null;
                PainterWindow!.ClearClipboard();
                ClearBrushInspectorSelection();
                return;
            }

            var newClipboardItem = num > 1
                          ? new TpPainterClipboard(output.ToArray(),
                                               new BoundsInt(Vector3Int.zero, position.size),
                                               position)
                          : new TpPainterClipboard(output[0].TileBase, output[0].m_Position, map);
            SelectBrushInspectorTarget(newClipboardItem);
        }
        
        private void OnBrushInspectorListItemClick(int index, ClickEvent evt)
        {
            if(index < m_BrushInspectorListView.itemsSource.Count)
                SelectBrushInspectorTarget(index,true);
        }

        /// <summary>
        /// Set the inspector target (Rightmost column) in paint mode.
        /// </summary>
        /// <param name="itemIndex">index of the item</param>
        /// <param name = "changeTool" >Change to Paint tool possible if this is true</param>
        internal void SelectBrushInspectorTarget(int itemIndex, bool changeTool = false)
        {
            if (itemIndex >= m_BrushInspectorListView.itemsSource.Count)
                return;
            var cbItem = (TpPainterClipboard)m_BrushInspectorListView.itemsSource[itemIndex];
            if (TpPainterState.Clipboard != null && TpPainterState.Clipboard.ClipboardGuid == cbItem.ClipboardGuid)
            {
                PainterWindow.ForcePainterTool();
                return;
            }

            SelectBrushInspectorTarget(cbItem, changeTool);
        }

        /// <summary>
        /// Set the inspector target (Rightmost column) in paint mode.
        /// </summary>
        /// <param name = "changeTool" >Change to Paint tool possible if this is true</param>
        /// <param name="clipboardObject">Clipboard instance</param>
        /// <param name="ignoreIdentical">Ignore if the same target</param>
        internal void SelectBrushInspectorTarget(TpPainterClipboard? clipboardObject, bool changeTool = true, bool ignoreIdentical = false)
        {
            if (!ignoreIdentical)
            {
                //ignore if the same target.
                var clipbd = TpPainterState.Clipboard;
                if (clipbd != null && clipboardObject != null &&
                    (clipboardObject.ItemIndex == clipbd.ItemIndex || clipboardObject.ClipboardGuid == clipbd.ClipboardGuid))
                {
                    PainterWindow.ForcePainterTool();
                    if (TpLibEditor.Informational)
                        TpLib.TpLogWarning($"Ignored same item: no new selection: index [{clipboardObject.ItemIndex}], GUID [{clipboardObject.ClipboardGuid}]");
                    return;
                }
            }
            
            var painterTransforms = TpPainterModifiers.instance;
            if (painterTransforms != null && clipboardObject is { VarietyCantModifyTransform: false, IsTileBase: false })
            {
                var variety = clipboardObject.ItemVariety;
                ModifierWrapper? wrapper = null;
                if (variety == TpPainterClipboard.Variety.TileItem)
                    wrapper = painterTransforms.TilesDefault;
                if (variety == TpPainterClipboard.Variety.PrefabItem)
                    wrapper = painterTransforms.PrefabDefault;
                if (wrapper != null)
                {
                    var applies = wrapper.m_EditActions;
                    if((applies & EditActions.Transform) != 0)
                        clipboardObject.Apply(wrapper.m_Matrix, applies);
                    if((applies & EditActions.Color) != 0)
                        clipboardObject.Apply(wrapper.m_Color, applies);
                }
            }

            TpPainterState.Clipboard = clipboardObject; //the Object we want to paint
            if (clipboardObject != null)
            {
                PainterWindow.TabBar.TabBarTransformModified(clipboardObject.TransformModified, clipboardObject);
            }
            else
                PainterWindow.TabBar.TabBarTransformModified(false, null);
            
            
            //if sync selection is ON and we've picked a tile, ensure the proper tilemap is selected so Grid gizmo appears.
            var forceToolbarButtonNotification = !TilePlusPainterConfig.TpPainterSyncSelection;
            if (TilePlusPainterConfig.TpPainterSyncSelection )
            {
                //if there's an active grid selection on the grid selection panel then
                //see if it's the same as the Tilemap Editor's GridSelection.
                //if it isn't then deselect the grid selection panel's grid selection.
                //this prevents inadvertent loss of the grid selection.
                var gridSelPanelSelection = PainterWindow.ActiveGridSelectionElement?.m_BoundsInt;
                if (gridSelPanelSelection.HasValue && GridSelection.position != gridSelPanelSelection.Value)
                {

                    var tgt    = TpPainterState.PaintableMap;
                    var tgtMap = tgt?.TargetTilemap;
                    var sel    = Selection.activeGameObject;

                    if (tgtMap != null && (sel == null || (sel != null && sel != tgtMap.gameObject)))
                        Selection.SetActiveObjectWithContext(tgtMap.gameObject, null);
                    else
                        forceToolbarButtonNotification = true;
                }
                else
                    forceToolbarButtonNotification = true;
            }

            //note that this has to be prior to changing the tab bar picked tile image or the handler OnMainToolbarChanged will reset the image.
            // ReSharper disable once Unity.NoNullPatternMatching
            if (changeTool &&  TpPainterState.PaintingAllowed && TpPainterState.InPaintMode)
                //note in next line that notification only occurs if there's no SyncSelection or there is but the SetActiveObjectWithContext call didn't occur. 
                PainterWindow.TabBar.ActivateToolbarButton(TpPainterTool.Paint, forceToolbarButtonNotification); //change to paint tool with notification so context changes.  

            switch (clipboardObject!.ItemVariety)
            {
                case TpPainterClipboard.Variety.TileItem:
                    listObjectTypeLabel.text = clipboardObject.IsTilePlusBase
                                                   ? "TilePlus Tile"
                                                   : "Tile";
                    break;
                case TpPainterClipboard.Variety.BundleItem:
                    listObjectTypeLabel.text = "Bundle";
                    break;
                case TpPainterClipboard.Variety.TileFabItem:
                    listObjectTypeLabel.text = "TileFab";
                    break;
                case TpPainterClipboard.Variety.PrefabItem:
                    listObjectTypeLabel.text = "Prefab";
                    break;
                case TpPainterClipboard.Variety.MultipleTilesItem:
                    listObjectTypeLabel.text = "Selection";
                    break;
                case TpPainterClipboard.Variety.EmptyItem:
                default:
                    listObjectTypeLabel.text = "Tiles";
                    break;
            }

            PainterWindow.TabBar.SetPickedObject(clipboardObject);

            
            switch (clipboardObject.ItemVariety)
            {
                case TpPainterClipboard.Variety.TileItem when clipboardObject.Tile != null: 
                    listSelectionLabel.text = clipboardObject.Valid ? clipboardObject.Tile.name : "NULL tile";
                    break;
                
                case TpPainterClipboard.Variety.BundleItem when clipboardObject.Bundle != null:
                    listSelectionLabel.text = clipboardObject.Bundle.name;
                    break;
                
                case TpPainterClipboard.Variety.TileFabItem when clipboardObject.TileFab != null:
                    listSelectionLabel.text = clipboardObject.TileFab.name;
                    break;
                
                case TpPainterClipboard.Variety.MultipleTilesItem when clipboardObject.Valid :
                    var num = clipboardObject.Cells != null
                                  ? clipboardObject.Cells.Length.ToString()
                                  : "??";
                    listSelectionLabel.text = $"{num} tiles";
                    break;
                
                case TpPainterClipboard.Variety.PrefabItem when clipboardObject.Valid :
                    listSelectionLabel.text = clipboardObject.Prefab!=null?clipboardObject.Prefab.name : "unknown";
                    break;
            }
        }
        
        #endregion
        
        #region makeBind
        
        /// <summary>
        /// Make a list item to be used in the Rightmost column (inspector) when displaying items from a
        /// palette, chunk, tilefab or FavoritesList in PAINT mode.
        /// </summary>
        /// <returns>Visual item</returns>
        private VisualElement MakePaintModeInspectorListItem()
        {
            var height = TilePlusPainterConfig.PainterListItemHeight;
            var outer = new VisualElement { 
                                              name = "palette-inspector-list-item",
                                              style =
                                              {
                                                  /*flexGrow    = 1,*/ alignContent = Align.FlexStart,
                                                  paddingLeft = 2, paddingRight  = 2, paddingBottom = 1, 
                                                  paddingTop = 1,
                                              } };


            var listBox = new TpListBoxItem("palette-inspector-list-item-item", Color.black,1,4,(OnBrushInspectorListItemClick)); 

            var imgSize = PaletteTargetImageSize / 3;
            switch (imgSize)
            {
                case < 14:
                    imgSize = 14;
                    break;
                case > 20:
                    imgSize = 20;
                    break;
            }
            
            var deleteButton = new Button(OnFavoritesItemDelete)
                               {
                                   name = "palette-item-delete", 
                                   style =
                                   {
                                       alignItems = Align.Center,
                                       alignSelf  = Align.Center,
                                       height     = imgSize, width = imgSize,
                                       flexGrow   = 0, flexShrink  = 1, 
                                       backgroundColor = PainterWindow.IsProSkin ? Color.clear:Color.black
                                   }
                               };
            var delButtonImage = (new Image()
                                  {
                                      image = FindIcon(TpIconType.UnityXIcon),
                                      style =
                                      {
                                          alignItems = Align.Center,
                                          alignSelf = Align.Center,
                                          height     = imgSize,
                                          width      = imgSize,
                                          flexGrow   = 0, flexShrink = 1
                                      }
                                  });
            
            deleteButton.Add(delButtonImage);
            listBox.Add(deleteButton);
            
            
            outer.Add(listBox);
            var image = new Image
                        {
                            name = "palette-item-image",
                            style =
                            {
                                minWidth          = PaletteTargetImageSize - PaletteTargetItemBorderWidth,
                                minHeight         = PaletteTargetImageSize - PaletteTargetItemBorderWidth,
                                maxWidth          = height,
                                maxHeight         = height,
                                borderBottomWidth = PaletteTargetItemBorderWidth,
                                borderTopWidth    = PaletteTargetItemBorderWidth,
                                borderRightWidth  = PaletteTargetItemBorderWidth,
                                borderLeftWidth   = PaletteTargetItemBorderWidth,
                            }
                        };

            listBox.Add(image);
            var label = new Label
                        {
                            name = "palette-item-label",
                            style =
                            {
                                whiteSpace = WhiteSpace.Normal,
                                alignContent = new StyleEnum<Align>(Align.FlexStart),
                                unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleLeft),
                                
                                flexGrow   = 0,
                                //changed 12/23 for better appearance with long names and narrow columns. Next line too.
                                overflow     = new StyleEnum<Overflow>(Overflow.Hidden), //changed 12/23 for better appearance with long names and narrow columns. Next line too.
                                textOverflow = new StyleEnum<TextOverflow>(TextOverflow.Ellipsis)
                            }
                        };
            originalColorForTileItems = label.style.color;
            listBox.Add(label);
            listBox.Add(new TpSpacer(10, 2));
            
            var textField = new TextField
                            {
                                focusable = false,
                                multiline = true,
                                name = "palette-item-textfield",
                                maxLength = 8192,
                                style =
                                {
                                    whiteSpace              = WhiteSpace.Normal,
                                    flexGrow                = 1,
                                    unityFontStyleAndWeight = FontStyle.Bold,
                                    display                 = DisplayStyle.None
                                    
                                }
                            };
            textField.Q<TextElement>().enableRichText = true;
            var sv = new ScrollView(){name = "palette-item-scrollview", style={display = DisplayStyle.None, flexGrow = 0}};
            sv.contentContainer.style.flexGrow = 1;
            sv.Add(textField);
            outer.Add(sv);
            return outer;


            void OnFavoritesItemDelete()
            {
                var index = listBox.m_Index;
                var item  = m_BrushInspectorListView.itemsSource[index];
               
                if (item == null || index == -1)
                {
                    PainterWindow.ShowNotification(new GUIContent("Try again: Select a Favorites item, then click the Delete button..."));
                    return;
                }

                if (item is not TpPainterClipboard)
                    return;

                TilePlusPainterFavorites.RemoveFromFavorites(index);

                TpLib.DelayedCallback(PainterWindow, Refresh, "T+P: Rebuild on Favorites Deletion", 50);
                return;

                void Refresh()
                {
                    PainterWindow.AssetViewer?.RefreshPalettesListView();
                    PainterWindow.PaintModeUpdateAssetsList();
                    RebuildBrushInspectorListView();
                    PainterWindow.ClearClipboard();
                    m_BrushInspectorListView.RefreshItems();
                    m_BrushInspectorListView.ScrollToItem(0);
                }
            }

        }

       
        /// <summary>
        /// Bind a list item to be used in the Rightmost column (inspector) when displaying items from a
        /// palette, chunk, or Favorites list
        /// </summary>
        private void BindPaintModeInspectorListItem(VisualElement element, int index)
        {
            var item      = TpPainterScanners.instance.ObjectsToInspect[index];
            var outer     = element.Q<VisualElement>("palette-inspector-list-item");
            var listBoxItem = element.Q<TpListBoxItem>("palette-inspector-list-item-item");
            listBoxItem.m_Index = index;
            var textfield    = element.Q<TextField>("palette-item-textfield");
            var label        = element.Q<Label>("palette-item-label");
            var img          = element.Q<Image>("palette-item-image");
            var deleteButton = element.Q<Button>("palette-item-delete");
            var scrollView   = element.Q<ScrollView>("palette-item-scrollview");
            scrollView.contentContainer.style.flexGrow = 0;
            outer.style.flexGrow       = 0; 
            listBoxItem.style.flexGrow = 0;
            var spriteColor = Color.white;
            
            if (item.ItemVariety is TpPainterClipboard.Variety.TileItem && item.Tile != null)
            {
                scrollView.style.display = DisplayStyle.None;
                var sprite = FindIconAsSprite(TpIconType.HelpIcon);
                if (item.IsTilePlusBase)
                    sprite = ((ITilePlus)item.Tile).EffectiveSprite;  //TilePlusBase uses this prop which is overridden in derived classes.
                else if (item.IsTile)                                    //Tile or TilePlusBase
                    sprite = ((Tile)item.Tile).sprite;
                else if (item.Tile != null) //must be a TileBase
                    sprite = GetSprite(item.Tile);
                if (sprite == null)
                {
                    sprite      = FindIconAsSprite(TpIconType.HelpIcon); 
                    spriteColor = Color.yellow;
                }

                // ReSharper disable once Unity.NoNullPatternMatching
                var isTptTile = item.Tile!=null && item.Tile is ITilePlus;
                
                label.style.color = isTptTile
                                        ? accentStyleColor
                                        : originalColorForTileItems;
                label.style.unityFontStyleAndWeight = isTptTile
                                                          ? boldStyle
                                                          : normalStyle;
                img.sprite      = sprite;
                img.tintColor = spriteColor;
                deleteButton.style.display = item.IsFromFavorites
                                                 ? DisplayStyle.Flex
                                                 : DisplayStyle.None;
                deleteButton.visible = item.IsFromFavorites;
                deleteButton.SetEnabled(item.IsFromFavorites);
                                          
                if (item.Valid)
                {
                    var itemName = item.Tile!.name.Trim();
                    if (sprite == null) 
                        label.text = itemName;
                    else
                    {
                        var locked = isTptTile && !item.IsFromBundleAsPalette && ((ITilePlus)item.Tile).IsLocked
                                         ? "<i><b> Locked</b></i>"
                                         : "";
                        var extents = sprite.bounds.extents;
                        var height  = extents.y * 2;
                        var width   = extents.x * 2;
                        label.text        = $"{itemName}{locked} [{height:N2} X {width:N2}]";
                        
                    }
                }
                else
                    label.text = "Null tile in Palette!"; 
                textfield.style.display = DisplayStyle.None;
            }
            
            //ONLY if a bundle is displayed as a List AND that bundle had any prefabs OR a Prefab is displayed in Favorites.
            else if (item.ItemVariety == TpPainterClipboard.Variety.PrefabItem)
            {
                
                scrollView.style.display = DisplayStyle.None;
                var go       = item.Prefab;
                if (go == null)
                { 
                    spriteColor = Color.yellow;
                    label.text  = "Item was null!";
                    img.sprite = FindIconAsSprite(TpIconType.PrefabIcon);  
                }
                else
                {
                    label.text          = go.name;
                    (var tex, var flag) = TpPreviewUtility.PreviewGameObject(go);
                    if (flag)
                        img.image = tex;
                    else
                    {
                        img.sprite      = FindIconAsSprite(TpIconType.PrefabIcon);
                        requiresRebuild = true;
                    }
                }

                outer.style.flexGrow = 0; //something resets this! WT!#@^%$
                
                img.tintColor                       = spriteColor;
                label.style.color                   = originalColorForTileItems;
                label.style.unityFontStyleAndWeight = normalStyle;
                
                deleteButton.style.display = item.IsFromFavorites
                                                 ? DisplayStyle.Flex
                                                 : DisplayStyle.None;
                deleteButton.visible = item.IsFromFavorites;
                deleteButton.SetEnabled(item.IsFromFavorites);
                textfield.style.display    = DisplayStyle.None;
                
            }
            
            //note never should be null tiles in a chunk but it doesn't matter here anyway.
            else if (item.ItemVariety == TpPainterClipboard.Variety.BundleItem)
            {
                scrollView.style.display            = item.IsFromFavorites? DisplayStyle.None : DisplayStyle.Flex;
                label.style.color                   = originalColorForTileItems;
                label.style.unityFontStyleAndWeight = normalStyle;
                outer.style.flexGrow                = 1; //something resets this! WT!#@^%$
                listBoxItem.style.flexGrow          = .2f;
                
                /*deleteButton.visible                = false;
                deleteButton.SetEnabled(false);
                */
                
                deleteButton.style.display = item.IsFromFavorites
                                                 ? DisplayStyle.Flex
                                                 : DisplayStyle.None;
                deleteButton.visible = item.IsFromFavorites;
                deleteButton.SetEnabled(item.IsFromFavorites);
                
                
                scrollView.contentContainer.style.flexGrow = item.IsFromFavorites?1:0;
                if (item.Bundle!.AssetVersion > 2 && item.Bundle.m_Icon != null)
                    img.sprite = item.Bundle.m_Icon;
                else
                    img.image = FindIcon(TpIconType.TpTileBundleIcon);
                textfield.style.display    = DisplayStyle.Flex;
                textfield.style.flexGrow   = 0;
                textfield.style.flexShrink = 0;
                
                var bundle     = item.Bundle;
                if (bundle == null)
                    textfield.value          = "Invalid or Null Bundle!!";
                else
                {
                   
                    var numTpTiles = bundle.m_TilePlusTiles.Count;
                    var numUtiles  = bundle.m_UnityTiles.Count;
                    var numPrefabs = bundle.m_Prefabs.Count;
                    var size       = bundle.m_TilemapBoundsInt.size;
                    var bPath      = AssetDatabase.GetAssetPath(bundle);
                    var sel = bundle.m_FromGridSelection
                                  ? $"[From Selection, Size:{bundle.m_TilemapBoundsInt.size}]"
                                  : $"[Entire Map, Size:{bundle.m_TilemapBoundsInt.size}]";
                    
                    
                    if (item.IsFromFavorites)
                    {
                        outer.style.flexGrow                       = 0; 
                        scrollView.contentContainer.style.flexGrow = 0;
                        listBoxItem.style.flexGrow                 = 0;
                        label.text                                 = $"TpTileBundle asset: {bundle.name}\nSize:{size.x}X{size.y},\n{numTpTiles} TPT tiles, {numUtiles} Unity tiles, {numPrefabs} prefabs.";
                    }
                    else
                    {
                        label.text = $"TpTileBundle asset: {bundle.name}";
                        /*textfield.style.flexGrow   = .8f;
                        textfield.style.flexShrink = 1;*/
                        textfield.value = $"Size:{size.x}X{size.y}, Variety: {(bundle.m_FromGridSelection ? "From Grid Selection" : "Entire Tilemap")}.\n"
                                          + $"GUID: {bundle.AssetGuidString}\n"
                                          + $"Created: {bundle.m_TimeStamp}\n"
                                          + $"\n{numTpTiles} TPT tiles, {numUtiles} Unity tiles, {numPrefabs} prefabs. {sel}\n\n"
                                          + "-------------------------------------------------------------------------------------------------\n"
                                          + "\n1. Please note that errors are NORMAL if any TPT tiles in the asset refer to a Tilemap"
                                          + " by name but that tilemap can't be located."
                                          + "\nFor example: 'No gameobject named SomeName'"
                                          + "\n2. <color=red>Painting this asset will overwrite tiles.</color>"
                                          + $"\n\n<i>Path: {bPath}</i>\n";
                    }
                }
            }
            else if (item.ItemVariety == TpPainterClipboard.Variety.TileFabItem)
            {
                label.style.color                          = originalColorForTileItems;
                label.style.unityFontStyleAndWeight        = normalStyle;
                outer.style.flexGrow                       = 1; //something resets this! WT!#@^%$
                //listBoxItem.style.flexGrow                 = .2f;
                //scrollView.contentContainer.style.flexGrow = 1;
                scrollView.style.display                   = DisplayStyle.Flex;

                deleteButton.visible                = false;
                deleteButton.SetEnabled(false);
                if (item.TileFab!.AssetVersion > 2 && item.TileFab.m_Icon != null)
                    img.sprite = item.TileFab.m_Icon;
                else
                    img.image = FindIcon(TpIconType.TileFabIcon);
                textfield.style.display = DisplayStyle.Flex;
                textfield.style.flexGrow = .8f;

                var fab       = item.TileFab;
                if (fab == null)
                    textfield.value = "Invalid or Null TileFab!!";
                else
                {
                    var numChunks = fab.m_TileAssets!.Count;
                    var size      = fab.LargestBounds.size;
                    var fPath     = AssetDatabase.GetAssetPath(fab);

                    var requiredMaps = fab.m_TileAssets.Select(assetSpec => assetSpec.m_TilemapName);
                    var requiredTags = fab.m_TileAssets.Select(assetSpec => assetSpec.m_TilemapTag);

                    var mapsMessage = string.Join(',', requiredMaps);
                    var tagsMessage = string.Join(',', requiredTags);

                    label.text = $"TpTileFab asset: {fab.name}";
                    textfield.value = $"Size:{size.x}X{size.y}, Variety: {(fab.m_FromGridSelection ? "CHUNK" : "TILEFAB")}.\n"
                                      + $"GUID: {fab.AssetGuidString}\n"
                                      + $"Created: {fab.m_TimeStamp}\n"
                                      + $"{numChunks} TileBundle Assets in this TileFab. \n\n"
                                      + "-------------------------------------------------------------------------------------------------\n"
                                      + "<b><color=red>If the named Tilemaps or tags can't be found THEN this TileFab can't be painted or previewed.</color></b>\n"
                                      + "\n1. Please note that errors are NORMAL if any TPT tiles in the asset refer to a Tilemap"
                                      + " by name but that tilemap can't be located."
                                      + "\nFor example: 'No gameobject named SomeName'"
                                      + "\n2. <color=red>Painting this asset will overwrite tiles.</color>\n\n"
                                      + "NOTE: the following named Tilemaps and/or tags must be present to Paint this.\n"
                                      + $"Tilemaps: {mapsMessage}\nTags: {tagsMessage}\n\n"
                                      + "You can edit the TileFab asset to change these values.\n"
                                      + "'Untagged' means that the Tilemap that the tiles were extracted from didn't have a tag. "
                                      + $"\n\n<i>Path: {fPath}</i>\n";
                }
            }
            else if (item is { ItemVariety: TpPainterClipboard.Variety.MultipleTilesItem, Cells: not null })
            {
                scrollView.style.display = DisplayStyle.None;
                outer.style.flexGrow     = 0;                       //something resets this! WT!#@^%$
                
                //for this type of item, which only occurs if you make a grid selection with a marquee and ctrl-click on the clipboard,
                //OR convert a tile bundle to a Pick.
                //just use the first non-null sprite from the list of cells as the sprite if one can't be created.
                
                // ReSharper disable once Unity.NoNullPatternMatching
                var sprite = item.Target is TileCellsWrapper w ? w.Icon : null;
                
                if (sprite == null)
                {
                    sprite      = FindIconAsSprite(TpIconType.HelpIcon);
                    spriteColor = Color.yellow;
                }
                //Debug.Log($"Brush Insp bind: {sprite.rect.ToString()}");
                
                label.style.color                   = Color.yellow;
                label.style.unityFontStyleAndWeight = boldStyle;
                if(sprite != null)
                    img.sprite                      = sprite;
                img.tintColor                       = spriteColor;
                deleteButton.style.display          = DisplayStyle.Flex; //note that this sort of item only displays in Favorites and only gets on that list from a CTRL-Click on the Clipboard or from the SceneView when -> Favorites is used. 
                                      
                //note that this item only appears in the list when it's been added to favorites from a pick somewhere
                deleteButton.visible = true;
                deleteButton.SetEnabled(true);
                                          
                label.text = item.Valid
                                 ? $"Pick from {item.MultipleTilesSelectionBoundsInt.ToString()}"
                                 : "Invalid multiple-tile selection from History!"; 
                textfield.style.display = DisplayStyle.None;
            }
            else
            {
                label.style.color                          = Color.red;
                label.style.unityFontStyleAndWeight        = new StyleEnum<FontStyle>(FontStyle.BoldAndItalic);
                label.text                                 = "Could not evalutate. Re-select or Double-click item in center column";
                outer.style.flexGrow                       = 1; //something resets this! WT!#@^%$
                listBoxItem.style.flexGrow                 = .2f;
                scrollView.contentContainer.style.flexGrow = 0;
                scrollView.style.display                   = DisplayStyle.None;

                deleteButton.visible = false;
                deleteButton.SetEnabled(false);
                img.sprite = null; 
                textfield.style.display  = DisplayStyle.None;
            }

            return;
            
            Sprite? GetSprite(TileBase tile)
            {
                return TpPreviewUtility.TryGetPlugin(tile, out var plug) && plug != null
                           ? plug.GetSpriteForTile(tile)
                           : FindIconAsSprite(TpIconType.UnityToolbarMinusIcon); 
            }
        }

        #endregion

        #region access

        /// <summary>
        /// Repaint
        /// </summary>
        internal void RepaintBrushInspectorGui()
        {
            brushInspectorGui.MarkDirtyRepaint();
        }

        /// <summary>
        /// Clear selection
        /// </summary>
        internal void ClearBrushInspectorSelection()
        {
            m_BrushInspectorListView.SetSelectionWithoutNotify(new[] { -1 });
            listSelectionLabel.text  = TilePlusPainterWindow.EmptyFieldLabel;
            listObjectTypeLabel.text = TilePlusPainterWindow.EmptyFieldLabel;
        }

        /// <summary>
        /// Fixes a Unity bug.
        /// </summary>
        internal void FixSelectionBug()
        {
            m_BrushInspectorListView.SetSelection(-1);
            m_BrushInspectorListView.SetSelection(0);
        }

        /// <summary>
        /// Rebuild the ListView
        /// </summary>
        internal void RebuildBrushInspectorListView()
        {
            m_BrushInspectorListView.Rebuild();
            SetPaletteModeTilesListViewHighlite();
            ItemHighlighter(m_BrushInspectorListView);
        }

        /// <summary>
        /// Refresh the ListView
        /// </summary>
        internal void RefreshBrushInspectorListView(int scrollPosition = 0)
        {
            m_BrushInspectorListView.RefreshItems();
            if(scrollPosition >= 0)
                m_BrushInspectorListView.ScrollToItem(scrollPosition);
        }

        /// <summary>
        /// Set the brush inspector list view to a specific index.
        /// </summary>
        /// <param name="index">the index for the list view</param>
        /// <remarks>Note that this method inhibits the list view selection events from being recognized.</remarks>
        internal void SetBrushInspectorListViewSelectionIndex(int index) 
        {
            var num = m_BrushInspectorListView.itemsSource.Count;
            if (num == 0 || index >= num)
                return;
            PainterWindow.DiscardListSelectionEvents = true;
            m_BrushInspectorListView.SetSelectionWithoutNotify(new[] { index});
            m_BrushInspectorListView.ScrollToItem(index);
            PainterWindow.DiscardListSelectionEvents = false;
        }
        
        private void UnbindTpListBoxItem(VisualElement element, int index)
        {
            var item = element.Q<TpListBoxItem>("tilemap-list-item");
            item?.DoUnregisterCallback();
        }
        
        /// <summary>
        /// Set highlight if palettes were too big and size limited.
        /// </summary>
        private void SetPaletteModeTilesListViewHighlite()
        {
            var c = PainterWindow != null && PainterWindow.OversizedPalette
                        ? Color.yellow
                        : brushInspectorTilesListViewOriginalColor;
            m_BrushInspectorListView.style.borderBottomColor = c;
            m_BrushInspectorListView.style.borderTopColor    = c;
            m_BrushInspectorListView.style.borderLeftColor   = c;
            m_BrushInspectorListView.style.borderRightColor  = c;
        }


        /// <summary>
        /// Force a palette->List view change or vice versa
        /// </summary>
        internal void ForceUnityPaletteChange()
        {
            painterModeChanged = true;
        }

        internal void SetPaletteVisibility(bool show)
        {
            paletteView.SetEnabled(show);
            //Note that pre-unity6 the palette does not properly disable itself.
            #if !UNITY_6000_0_OR_NEWER
            paletteView.style.visibility = show ? Visibility.Visible : Visibility.Hidden;
            #endif
        }
     
        private void BrushInspectorSplitterFix(GeometryChangedEvent evt)
        {
            var handle  = inspectorViewSplitter.Q<VisualElement>("unity-dragline-anchor");
            
            handle.style.width           = tileInfoContainer.style.width;
            handle.style.height          = TilePlusPainterWindow.SplitterSize;
            handle.style.backgroundColor = Color.red;
            if(evt.newRect.size == Vector2.zero)
                evt.StopImmediatePropagation(); //needed to preserve splitter pos when changing global modes. LEAVE AS IS!
            else
                evt.StopPropagation();
        }


        /// <summary>
        /// Release subitems
        /// </summary>
        internal void Release()
        {
            #if UNITY_6000_0_OR_NEWER
            paletteView.RegisterPainterInterest(false);
            #endif
            paletteView.Release();
        }
        
        #endregion
    }
}
