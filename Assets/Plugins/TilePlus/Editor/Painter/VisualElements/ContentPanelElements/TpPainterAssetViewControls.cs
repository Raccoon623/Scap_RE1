// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 01-12-2024
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 01-14-2024
// ***********************************************************************
// <copyright file="TpPainterAssetViewControls.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using static TilePlus.Editor.TpIconLib;
#nullable enable

namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// Controls for the center column: two versions depending on context.
    /// </summary>
    public class TpPainterAssetViewControls : VisualElement
    {
        #region constants
        /// <summary>
        /// The search field's textfield outline radius
        /// </summary>
        private const float SearchFieldRadius = 4;
        
        /// <summary>
        /// The filter type dropdown label
        /// </summary>
        private const string Filter_Type_Dropdown_Label = "Type Filter";
        /// <summary>
        /// The filter type dropdown tooltip
        /// </summary>
        private const string Filter_Type_Dropdown_Tip = "Select TileBase to show all Tiles.\nSelect any other type to filter what is displayed in the Tilemaps foldouts.\nNote: this setting isn't persistent and it ANDs with the Tag filter setting.";
        /// <summary>
        /// The filter tag dropdown label
        /// </summary>
        private const string Filter_Tag_Dropdown_Label = "Tag Filter";
        /// <summary>
        /// The filter tag dropdown tooltip
        /// </summary>
        private const string Filter_Tag_Dropdown_Tip = "shows all Tiles.\nSelect any other tag to filter (TilePlus tiles only) what is displayed in the Tilemaps foldouts.\nNote: this setting isn't persistent and it ANDs with the Type filter setting.";
        #endregion
        
        #region privateProperties
        
        
        private TilePlusPainterWindow? PainterWindow => TilePlusPainterWindow.RawInstance;
        
        
        #endregion
        
        #region publicProperties

        internal StyleLength ScrollviewWidth => assetViewControlsScrollView != null
                ? assetViewControlsScrollView.style.width
                : 10;

        #endregion
        
        #region privateFields
        
        /// <summary>
        /// The asset view tile options panel
        /// </summary>
        private VisualElement? assetViewTileOptions;
        /// <summary>
        /// The asset view palette options panel
        /// </summary>
        private VisualElement? assetViewPaletteOptions;
        /// <summary>
        /// The show grid toggle in the palette options element
        /// </summary>
        private TpToggleLeft? assetViewPalOptShowGrid;
        /// <summary>
        /// The Show a bundle as list toggle
        /// </summary>
        private TpToggleLeft? assetViewPalOptBundleAsList;
        /// <summary>
        /// Show TileFabs toggle
        /// </summary>
        private TpToggleLeft? assetViewShowTilefabs;
        /// <summary>
        /// Show Only matching (tilemap name, etc) Tilefabs.
        /// </summary>
        private TpToggleLeft? assetViewShowMatchingTileFabs;
        
        /// <summary>
        /// Scroller for the top part of the window with the Painter options.
        /// </summary>
        private readonly ScrollView? assetViewControlsScrollView;



        //for the type filter
        /// <summary>
        ///used when computing type filter
        /// </summary>
        private List<Type> typeList = new(16);
        /// <summary>
        /// used when computing type filter
        /// </summary>
        private readonly Dictionary<string, Type> typeFilterDict = new(16); //used when computing type filter

        //for the tag filter
        /// <summary>
        ///used when computing tag filter
        /// </summary>
        private List<string>? validTags;      
        
        //styles
        private readonly StyleEnum<DisplayStyle> showStyle = new(DisplayStyle.Flex);
        private readonly StyleEnum<DisplayStyle> hideStyle = new(DisplayStyle.None);
        
        
        #endregion

        #region ctor

        /// <summary>
        /// Control Panels for Palette lists or Tile Lists (center column)
        /// </summary>
        /// <returns>Visual Element</returns>
        internal TpPainterAssetViewControls(float viewPanesMinWidth)
        {
            name            = "asset-view-controls";
            style.minHeight = 80;
            
            assetViewControlsScrollView = new ScrollView(ScrollViewMode.VerticalAndHorizontal)
                                          {
                                              name = "asset-view-control-container", style =
                                              {
                                                  minWidth            = viewPanesMinWidth, minHeight = 80, borderTopWidth        = 6, borderLeftWidth         = 2,
                                                  borderBottomWidth   = 4, borderRightWidth          = 2, borderBottomLeftRadius = 3, borderBottomRightRadius = 3,
                                                  borderTopLeftRadius = 3, borderTopRightRadius      = 3, paddingBottom          = 2, paddingLeft             = 4,
                                                  paddingTop          = 2, paddingRight              = 2, marginLeft             = 4
                                              }
                                          };
            Add(assetViewControlsScrollView);
            //want same look as scrollviews inside the list-views.
            assetViewControlsScrollView.AddToClassList("unity-collection-view--with-border");
            assetViewControlsScrollView.AddToClassList("unity-collection-view__scroll-view");
            assetViewControlsScrollView.Q("unity-content-container").style.flexGrow = 1;

            assetViewControlsScrollView.Add(BuildPaletteOptions());
            assetViewControlsScrollView.Add(BuildTileOptions());
        }
        #endregion
        
        #region builders
        private VisualElement BuildTileOptions()
        {
            assetViewTileOptions = new VisualElement { name = "tile-options", style = {minHeight = 80, display = DisplayStyle.None } }; //a container so we can switch visible/invisible

            assetViewTileOptions.Add(new Label("Options") { style = { alignSelf = Align.Center, marginBottom = 2 } });

            var toggle = new Toggle("Show IDs") { tooltip = "Show instance IDs next to tile position", name = "setting-show-iid", value = TilePlusPainterConfig.TpPainterShowIid };
            toggle.RegisterValueChangedCallback(evt =>
                                                {
                                                    TilePlusPainterConfig.TpPainterShowIid = evt.newValue;
                                                    PainterWindow!.RefreshTilesView();
                                                });
            assetViewTileOptions.Add(toggle);

            //note that choices match enum TpTileSorting
            var choices = new List<string> { "Unsorted", "Type", "IID" };
            var radioGroup = new RadioButtonGroup("Tile Sorting",
                                                  choices)
                             {
                                 style =
                                 {
                                     flexDirection           = FlexDirection.Column, borderBottomColor = Color.red, borderTopColor = Color.red, borderLeftColor = Color.red,
                                     borderRightColor        = Color.red, borderBottomWidth            = 1, borderTopWidth         = 1, borderLeftWidth         = 1,
                                     borderRightWidth        = 1, borderBottomLeftRadius               = 4, borderTopLeftRadius    = 4, borderTopRightRadius    = 4,
                                     borderBottomRightRadius = 4,
                                 }
                             };
            radioGroup.SetValueWithoutNotify((int)TilePlusPainterConfig.TpPainterTileSorting);

            radioGroup.RegisterValueChangedCallback(evt =>
                                                    {
                                                        TilePlusPainterConfig.TpPainterTileSorting = (TpTileSorting)evt.newValue;
                                                        PainterWindow!.RefreshTilesView();
                                                    });
            assetViewTileOptions.Add(radioGroup);

            assetViewTileOptions.Add(new TpSpacer(10, 10));

            assetViewTileOptions.Add(CreateFilterGui());


            return assetViewTileOptions;
        }


        private VisualElement BuildPaletteOptions()
        {
            assetViewPaletteOptions = new VisualElement { name = "palette-options" , style =
                                                        {
                                                            minHeight = 80
                                                        }}; //just a container so we can switch it visible/invisible

            var searchFieldContainer = new VisualElement { name = "search-field-container", style = { flexGrow = 0, borderBottomWidth = 2, borderBottomColor = Color.black, marginBottom = 2 } };

            var searchInnerContainer = new VisualElement { name = "search-field-inner-container", style = { flexGrow = 1, flexDirection = FlexDirection.Row } };


            var sf = new TextField(16,
                                   false,
                                   false,
                                   ' ')
                     {
                         style =
                         {
                             flexGrow            = 1, borderBottomRightRadius = SearchFieldRadius, borderBottomLeftRadius = SearchFieldRadius, borderTopRightRadius = SearchFieldRadius,
                             borderTopLeftRadius = SearchFieldRadius
                         }
                     };

            void SearchFieldCallback(ChangeEvent<string> evt)
            {
                PainterWindow!.m_CurrentPaletteSearchString = evt.newValue;
                PainterWindow.RebuildPaletteListIfChanged();
            }

            sf.RegisterValueChangedCallback(SearchFieldCallback);
            searchInnerContainer.Add(sf);
            searchInnerContainer.Add(new TpSpacer(4, 4));


            void ClearTextButtonClickEvent()
            {
                sf.value                                   = string.Empty;
                PainterWindow!.m_CurrentPaletteSearchString = string.Empty;
                PainterWindow.RebuildPaletteListIfChanged();
            }

            var clearTextButton = new Button(ClearTextButtonClickEvent) { style = { backgroundImage = FindIcon(TpIconType.UnityXIcon) } };
            searchInnerContainer.Add(clearTextButton);

            searchFieldContainer.Add(searchInnerContainer);
            searchFieldContainer.Add(new Label("Search is case-insensitive") { style = { scale = new StyleScale(new Vector2(0.8f, 0.8f)), alignSelf = Align.Center, unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Italic) } });
            assetViewPaletteOptions.Add(searchFieldContainer);


            assetViewPaletteOptions.Add(new Label("Options") { style = { alignSelf = Align.Center, marginBottom = 2 } });

            var toggle = new TpToggleLeft("Show Palettes") { name = "setting-show-palettes", value = TilePlusPainterConfig.TpPainterShowPalettes };
            toggle.RegisterValueChangedCallback(ChangeShowPalettes);
            assetViewPaletteOptions.Add(toggle);

            assetViewPalOptShowGrid = new TpToggleLeft("Use Unity Palette")
                                      {
                                          name    = "setting-show-palette-grid",
                                          tooltip = "[EXPERIMENTAL!!] Use the Unity Tile palette to display Palettes",
                                          value   = TilePlusPainterConfig.TpPainterShowPaletteAsGrid,
                                          style =
                                          {
                                              paddingLeft = 10,
                                              unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Italic)
                                          }
                                      };

            void ShowGridCallback(ChangeEvent<bool> evt)
            {
                TilePlusPainterConfig.TpPainterShowPaletteAsGrid = evt.newValue;
                PainterWindow!.BrushInspector?.ForceUnityPaletteChange();
                var currentTgt = TpPainterState.PaintableObject;
                if (currentTgt != null)
                    PainterWindow.m_TpPainterContentPanel.SelectPaletteOrOtherSource(currentTgt);
            }

            assetViewPalOptShowGrid.RegisterValueChangedCallback(ShowGridCallback);
            assetViewPaletteOptions.Add(assetViewPalOptShowGrid);
            
            assetViewShowTilefabs = new TpToggleLeft("Show TileFabs") { name = "setting-show-tilefabs", value = TilePlusPainterConfig.TpPainterShowTilefabs };

            void ShowTilefabsCallback(ChangeEvent<bool> evt)
            {
                TilePlusPainterConfig.TpPainterShowTilefabs = evt.newValue;
                PainterWindow!.RebuildPaletteListIfChanged();
                PainterWindow.ClearClipboard();
                PainterWindow.ResetSelections(false);
                PainterWindow.BrushInspector?.RebuildBrushInspectorListView();
                ClearBrushInspectorSelection();
                SetBrushInspectorListViewSelectionIndex(0);
            }

            assetViewShowTilefabs.RegisterValueChangedCallback(ShowTilefabsCallback);
            assetViewPaletteOptions.Add(assetViewShowTilefabs);


            assetViewShowMatchingTileFabs = new TpToggleLeft("Matches Only")
                                            {
                                                name  = "setting-matching-tilefabs",
                                                value = TilePlusPainterConfig.TpPainterShowMatchingTileFabs,
                                                tooltip =
                                                    "Show only those Tilefabs whose names and/or tags match those embedded in the TileFab asset",
                                                style =
                                                {
                                                    paddingLeft             = 10,
                                                    unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Italic)
                                                }
                                            };

            void MatchingTilefabsCallback(ChangeEvent<bool> evt)
            {
                TilePlusPainterConfig.TpPainterShowMatchingTileFabs = evt.newValue;
                PainterWindow!.RebuildPaletteListIfChanged();
                PainterWindow.ClearClipboard();
                PainterWindow.ResetSelections(false);
                PainterWindow.BrushInspector?.RebuildBrushInspectorListView();
                ClearBrushInspectorSelection();
                SetBrushInspectorListViewSelectionIndex(0);
            }

            assetViewShowMatchingTileFabs.RegisterValueChangedCallback(MatchingTilefabsCallback);
            assetViewPaletteOptions.Add(assetViewShowMatchingTileFabs);
                
            toggle = new TpToggleLeft("Show Tile Bundles") { name = "setting-show-combined-tiles", value = TilePlusPainterConfig.TpPainterShowTileBundles };

            void ShowTileBundlesCallback(ChangeEvent<bool> evt)
            {
                TilePlusPainterConfig.TpPainterShowTileBundles = evt.newValue;
                PainterWindow!.RebuildPaletteListIfChanged();
                PainterWindow.ClearClipboard();
                PainterWindow.ResetSelections(false);
                PainterWindow.BrushInspector?.RebuildBrushInspectorListView();
                ClearBrushInspectorSelection();
                SetBrushInspectorListViewSelectionIndex(0);
            }

            toggle.RegisterValueChangedCallback(ShowTileBundlesCallback);
            assetViewPaletteOptions.Add(toggle);

            assetViewPalOptBundleAsList = new TpToggleLeft("Bundle Tiles View")
                                          {
                                              name = "setting-bundle-aspalette-mode", value = TilePlusPainterConfig.TpPainterShowBundleAsPalette, tooltip = "Show a tile bundle as a palette of individual tiles.", 
                                              style =
                                              {
                                                  paddingLeft             = 10,
                                                  unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Italic)
                                              }
                                          };

            void BundleAsListCallback(ChangeEvent<bool> evt)
            {
                TilePlusPainterConfig.TpPainterShowBundleAsPalette = evt.newValue;
                PainterWindow!.PaintModeUpdateAssetsList();
                PainterWindow.RebuildPaletteListIfChanged();
                PainterWindow.ClearClipboard();
                PainterWindow.OnMainToolbarChanged((int)TpPainterTool.None);
                ClearBrushInspectorSelection();
                SetBrushInspectorListViewSelectionIndex(0);
            }

            assetViewPalOptBundleAsList.RegisterValueChangedCallback(BundleAsListCallback);
            schedule.Execute(ScheduledUpdateEvent).Every(500);
            assetViewPaletteOptions.Add(assetViewPalOptBundleAsList);
            return assetViewPaletteOptions;
            
            void ScheduledUpdateEvent()
            {
                if (PainterWindow != null && !TpPainterState.InPaintMode)
                    return;
                assetViewPalOptBundleAsList.style.display = TilePlusPainterConfig.TpPainterShowTileBundles
                                                                ? showStyle
                                                                : hideStyle;
                assetViewPalOptShowGrid.style.display = TilePlusPainterConfig.TpPainterShowPalettes
                                                            ? showStyle
                                                            : hideStyle;
                assetViewShowMatchingTileFabs.style.display = TilePlusPainterConfig.TpPainterShowTilefabs
                                                                  ? showStyle 
                                                                  :hideStyle;
            }
        }

        private async void ChangeShowPalettes(ChangeEvent<bool>evt)
        {
            TilePlusPainterConfig.TpPainterShowPalettes = evt.newValue;
            await Task.Yield();
            
            PainterWindow!.RebuildPaletteListIfChanged();
            PainterWindow.ClearClipboard(false);
            PainterWindow.ResetSelections(false);
            PainterWindow.BrushInspector?.RebuildBrushInspectorListView(); 
            ClearBrushInspectorSelection();
            var currentTgt = TpPainterState.PaintableObject;
            if(currentTgt != null)
                PainterWindow.m_TpPainterContentPanel.SelectPaletteOrOtherSource( currentTgt);
            else
                PainterWindow.m_TpPainterContentPanel.SelectPaletteOrOtherSource(0); 

        }
        
        
        #endregion
        
        #region access
        /// <summary>
        /// used to hide or unhide the list of painting source options
        /// </summary>
        /// <param name="show">true/false to unhide/hide</param>
        internal void ShowSourceViewPaletteOptions(bool show)
        {
            assetViewPaletteOptions!.style.display = show ? showStyle : hideStyle;
        }

        /// <summary>
        /// used to hide or unhide the list of tile editing options
        /// </summary>
        /// <param name="show">true/false to unhide/hide</param>
        internal void ShowSourceViewTileOptions(bool show)
        {
            assetViewTileOptions!.style.display = show ? showStyle : hideStyle;
        }
        
        /// <summary>
        /// Set the Brush inspector list to a particular index
        /// </summary>
        /// <param name="index">list item to select</param>
        /// <remarks>Note that this method inhibits the list view selection events from being recognized.</remarks>
        private void SetBrushInspectorListViewSelectionIndex(int index)
        {
            PainterWindow!.BrushInspector?.SetBrushInspectorListViewSelectionIndex(index);
        }
        
        private void ClearBrushInspectorSelection()
        {
            PainterWindow!.BrushInspector?.ClearBrushInspectorSelection();
        }
        
        #endregion
        
        #region tilefilters
                

        /// <summary>
        /// Recomputes the tag filter.
        /// </summary>
        internal void RecomputeTagFilter()
        {
            PainterWindow!.m_FilterTag = TpLib.ReservedTag;
            ComputeTagFilter();
            PainterWindow.ForceRefreshTilesList();
        }

        /// <summary>
        /// Recomputes the type filter.
        /// </summary>
        internal void RecomputeTypeFilter()
        {
            PainterWindow!.m_FilterType = typeof(TileBase);
            ComputeTypeFilter();
            PainterWindow.ForceRefreshTilesList();
        }

        /// <summary>
        /// Resets the Type and Tag filters.
        /// </summary>
        internal void ResetFilters()
        {
            PainterWindow!.m_FilterType            = typeof(TileBase);
            PainterWindow.m_FilterTag             = TpLib.ReservedTag;
            ComputeTagFilter();
            ComputeTypeFilter();
            PainterWindow.ForceRefreshTilesList();
        }

        /// <summary>
        /// Actual work of creating the Type filter
        /// </summary>
        private void ComputeTypeFilter()
        {
            typeFilterDict.Clear();

            typeFilterDict.Add(nameof(TileBase), typeof(TileBase));  //ie, everything, including Rule tiles (need plugins)
            typeFilterDict.Add(nameof(Tile),         typeof(Tile)); //ie only normal Unity Tiles

            foreach (var plugin in TpPreviewUtility.AllPlugins)
            {
                if (plugin != null)
                    typeFilterDict.TryAdd(plugin.GetTargetTileType.Name, plugin.GetTargetTileType); //2.02 changed to TryAdd
            }

            TpLib.GetAllTypesInDb(ref typeList);
            foreach (var item in typeList)
                typeFilterDict.TryAdd(GetShortTypeName(item), item);

            if (typeDropDown == null)
                return;

            typeDropDown.choices = typeFilterDict.Keys.ToList();

            if (!typeFilterDict.ContainsValue(PainterWindow!.m_FilterType))
                PainterWindow.m_FilterType = typeof(TileBase);
            typeDropDown.value = GetShortTypeName(PainterWindow.m_FilterType);


        }

        /// <summary>
        /// Gets the short name of the type.
        /// </summary>
        /// <param name="item">Type to get the short name of</param>
        /// <returns>string</returns>
        private string GetShortTypeName(Type item)
        {
            var substrings = item.ToString().Split('.');
            var len        = substrings.Length;
            return len == 0
                       ? string.Empty
                       : substrings[len - 1]; //string name of the type
        }


        /// <summary>
        /// Computes the tag filter.
        /// </summary>
        private void ComputeTagFilter()
        {
            validTags = TpLib.GetAllTagsInDb.ToList();
            validTags.Insert(0, TpLib.ReservedTag);

            if (tagDropDown == null)
                return;

            tagDropDown.choices = validTags;
            tagDropDown.value = !validTags.Contains(PainterWindow!.m_FilterTag)
                                    ? TpLib.ReservedTag
                                    : PainterWindow.m_FilterTag;

        }
        
        
        /// <summary>
        /// The type drop down UI Element
        /// </summary>
        private DropdownField? typeDropDown;
        /// <summary>
        /// The tag drop down UI Element
        /// </summary>
        private DropdownField? tagDropDown;

        /// <summary>
        /// Creates the filter GUI.
        /// </summary>
        /// <returns>UnityEngine.UIElements.VisualElement.</returns>
        private VisualElement CreateFilterGui()
        {
            //filters
            
            ComputeTypeFilter();
            ComputeTagFilter();

            var container = new VisualElement{ style              =
                                             {
                                                 flexGrow = 1, marginBottom = 2,
                                                 borderBottomColor = Color.red,
                                                 borderTopColor = Color.red,
                                                 borderLeftColor = Color.red,
                                                 borderRightColor = Color.red,
                                                 borderBottomWidth = 1,
                                                 borderTopWidth = 1,
                                                 borderLeftWidth = 1,
                                                 borderRightWidth = 1,
                                                 borderBottomLeftRadius = 4,
                                                 borderTopLeftRadius = 4,
                                                 borderTopRightRadius = 4,
                                                 borderBottomRightRadius = 4,
                                                 
                                             }, name = "filter-container" };
            var label     = new Label("Type/Tag filters") { style = { alignSelf = Align.Center, unityFontStyleAndWeight = FontStyle.Bold} };
            container.Add(label);

            container.Add(new TpSpacer(4, 10));
            
            //reset button
            var button = new Button(ResetFilters)
                         {
                             name = "reset-filter-button",
                             text = "Reset filters",
                             style =
                             {
                                 flexGrow = 0
                             }
                         };
            container.Add(button);
            
            container.Add(new TpSpacer(4,10));
            
            //add type-filter
            typeDropDown = new DropdownField
                               {
                                   label   = Filter_Type_Dropdown_Label,
                                   tooltip = Filter_Type_Dropdown_Tip,
                                   choices = typeFilterDict.Keys.ToList(),
                                   value   = GetShortTypeName(PainterWindow!.m_FilterType)
                               };
            typeDropDown.Q<Label>().style.minWidth = 30;
            
            typeDropDown.RegisterValueChangedCallback(evt =>
                                                      {
                                                          typeFilterDict.TryGetValue(evt.newValue, out PainterWindow.m_FilterType);

                                                          PainterWindow.ForceRefreshTilesList();
                                                      });
            container.Add(typeDropDown);
            container.Add( new TpSpacer(4, 20));
            container.Add(new Label("Tags are case-insensitive\nand only apply to TilePlus tiles.")
                          {
                              style =
                              {
                                  color                   = Color.red,
                                  overflow = new StyleEnum<Overflow>(Overflow.Hidden),
                                  textOverflow            = TextOverflow.Clip,
                                  alignSelf               = Align.Center,
                                  unityFontStyleAndWeight = FontStyle.Bold,
                                  scale                   = new StyleScale(new Vector2(0.8f,0.8f))
                              }
                          });
            //add tag-filter
            tagDropDown = new DropdownField
                              {
                                  
                                  label = Filter_Tag_Dropdown_Label,
                                  tooltip = $"'{TpLib.ReservedTag}' {Filter_Tag_Dropdown_Tip}"
                              };
            if (validTags != null && validTags.Count != 0)
            {
                tagDropDown.choices = validTags;
                tagDropDown.value   = validTags[0];
            }

            tagDropDown.Q<Label>().style.minWidth = 30;
            
            tagDropDown.RegisterValueChangedCallback(evt =>
                                                     {
                                                             PainterWindow.m_FilterTag = evt.newValue;
                                                             PainterWindow.ForceRefreshTilesList();

                                                     });
            
            container.Add(tagDropDown);
            return container;
        }
        
        
        #endregion
    }
}
