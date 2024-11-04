// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 11-04-2022
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-31-2022
// ***********************************************************************
// <copyright file="TpPainterSettingsPanel.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Create the settings panel for Tile+Painter</summary>
// ***********************************************************************

using System.Globalization;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
#nullable enable

namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// TpPainterSettingsPanel creates the settings panel
    /// Implements the <see cref="VisualElement" />
    /// Implements the <see cref="TilePlus.Editor.ISettingsChangeWatcher" />
    /// </summary>
    /// <seealso cref="VisualElement" />
    /// <seealso cref="TilePlus.Editor.ISettingsChangeWatcher" />
    internal class TpPainterSettingsPanel : VisualElement, ISettingsChangeWatcher
    {
        private const string ButtonText_PlayUpdate       = "Update in Play";
        private const string ToolTip_PlayUpdate          = "Updates displays of TilePlus tile data when in Play mode.";
        private const string LabelText_AutoRefMaps       = "Validate Tilemaps in Play";
        private const string Tooltip_AutoRefMaps         = "When Update In Play is checked, checking THIS item will test for added/deleted Tilemaps in Play mode.\nNote: see Painter Manual section on this topic.";
        private const string FieldText_MaxNumTiles       = "Max #tiles to display";
        private const string FieldText_SnappingChunkSize = "Chunk Size for Snapping";
        private const string FieldText_SnappingOrigin    = "Chunk Snapping World Origin";
        private const string ToggleText_Snapping         = "Chunk Snapping";
        private const string ToolTip_SnappingChunkSize   = "Grid Size:  (>= 4 and must be even number, if not even, value reduced by 1)";
        private const string ToolTip_SnappingOrigin      = "Chunk Snapping World Origin (usually just 0,0,0)";
        private const string ToolTip_SnappingMode        = "Turn on Chunk Snapping. ";
        private const string LabelText_HighlightTime     = "Highlight Time";
        private const string LabelToolTip_HighlightTime  = "Tile Highlight Time when selected a list of scene tiles.";
        private const string ToolTip_UiSize              = "Relative size of UI elements in Lists. From 14-30."; //" Click the Refresh button (lower-left corner of window) to update.";
        private const string LabelText_UiSize            = "UI Size";
        private const string ToolTip_SpriteSize          = "Size of sprite in Right column. From 20-99";
        private const string LabelText_SpriteSize        = "Palette Sprite Size";
        private const string Label_MapSorting            = "Tilemap Sorting";
        //private const string 

        private const string ToolTip_MapSorting =
            "When checked, the Tilemaps list is sorted by Sorting Layer ID and then by Sorting Order within the Layer. Otherwise an Alpha-sort is used.";

        private readonly Toggle          overwriteToggle;
        private readonly Toggle          syncToggle;
        private readonly Toggle          updateToggle,updateTilemapsToggle;
        private readonly TpListBoxItem   updateListBoxItem;
        private readonly IntegerField    chunkSize;
        private readonly Vector3IntField originField;
        private readonly TpSplitter      splitter;

        
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="win">parent window</param>
        internal TpPainterSettingsPanel( TilePlusPainterWindow win)
        {
            
            style.display       = DisplayStyle.None;
            style.flexGrow      = 1;
            style.marginBottom  = 2;
            style.marginTop     = 4;
            style.alignItems    = Align.Stretch;

            name = "settings-container";
            Add(new TpHelpBox("Click the Settings button again to close this panel",
                              "settings-helpbox")
                {
                    style =
                    {
                        alignSelf = Align.Center, 
                        paddingBottom = 2
                    }
                });
            
            //add a centered header
            Add(new TpHeader("Settings","settings-header") { style = { paddingBottom = 1, alignSelf = Align.Center } });

            splitter = new TpSplitter("painter-setup-panel-splittter",
                                      "TPT.TPPAINTER.SETUP.SPLITVIEW",
                                      192,
                                      TwoPaneSplitViewOrientation.Vertical,
                                      0,
                                      SetupSplitterFix){ style = { minWidth = TilePlusPainterWindow.ViewPanesMinWidth } };
            
            Add(splitter);
            var topSplit = new ScrollView()
                           {
                               name = "setup-panel-top-of-split",
                               
                               style   = {
                                             overflow = Overflow.Hidden, minHeight = 40f}
                           };

            //add local controls to top part
            topSplit.Add(new TpHelpBox("Tile+Painter settings (Certain settings are also controlled by the small buttons at the bottom of the window)", "settings-header")
                         {
                             style= {marginBottom = 2}
                         });
           
            
            topSplit.Add(new TpSpacer(4, 20));

            var toggle = new TpToggleLeft("Overwrite Protection")
                         {
                             tooltip =
                                 "When checked, placed TilePlus tiles can't be overwritten. This can be overwritten with a shortcut (default='1')",
                             name  = "setting-overwrite",
                             value = TilePlusConfig.instance.NoOverwriteFromPalette,
                         };
              
            toggle.RegisterValueChangedCallback(evt =>
                                                {
                                                    TilePlusConfig.instance.NoOverwriteFromPalette = evt.newValue;
                                                    
                                                });
            topSplit.Add(toggle);
            overwriteToggle = toggle;

            toggle = new TpToggleLeft(Label_MapSorting)
            {
                tooltip = ToolTip_MapSorting,
                name = "setting-map-sorting",
                value = TilePlusPainterConfig.TpPainterTilemapSorting
            };

            void LabelMapSortingCallback(ChangeEvent<bool> evt)
            {
                TilePlusPainterConfig.TpPainterTilemapSorting = evt.newValue;
                TpLib.DelayedCallback(win, win.RebuildTilemapsList, "T+P:ChangeMapSort");
            }

            toggle.RegisterValueChangedCallback(LabelMapSortingCallback);
            topSplit.Add(toggle);

            toggle = new TpToggleLeft("Reverse Tilemap Sorting") 
                     { tooltip = "When Tilemap Sorting is active, check this to reverse the order.", 
                       name = "setting-reverse-sort", 
                       value = TilePlusPainterConfig.TpPainterTilemapSortingReverse };

            void ReverseTilemapSortingCallback(ChangeEvent<bool> evt)
            {
                TilePlusPainterConfig.TpPainterTilemapSortingReverse = evt.newValue;
                TpLib.DelayedCallback(win, win.RebuildTilemapsList, "T+P:ChangeMapSort");
            }

            toggle.RegisterValueChangedCallback(ReverseTilemapSortingCallback);
            
            topSplit.Add(toggle);
            toggle = new TpToggleLeft("Sync Selection")
                     {
                         tooltip =
                             "[RECOMMENDING = ON] When checked, this window will select a tilemap in the heirarchy when you click it in the window's Tilemap list, and when you select a Tilemap in the heirarchy with the mouse the selection in this window's Tilemap list will match.",
                         name  = "setting-selection-sync",
                         value = TilePlusPainterConfig.TpPainterSyncSelection
                     };
            toggle.RegisterValueChangedCallback(evt => { TilePlusPainterConfig.TpPainterSyncSelection = evt.newValue; });
            topSplit.Add(toggle);
            syncToggle = toggle;

            var syncPaletteToggle = new TpToggleLeft("Sync Palettes")
                                    {
                                        tooltip = "[RECOMMENDING = ON] When checked: palette selection is synchronized between Tile+Painter and the Unity Palette when both are open.", 
                                        name = "setting-palette-sync",
                                        value = TilePlusPainterConfig.TpPainterSyncPalette
                                    };
            syncPaletteToggle.RegisterValueChangedCallback(evt => TilePlusPainterConfig.TpPainterSyncPalette = evt.newValue);
            topSplit.Add(syncPaletteToggle);



            var agressiveToggle = new TpToggleLeft("Aggressive  selection")
                                  {
                                      tooltip = "[RECOMMENDING=ON] if checked, when in PAINT mode with any Tilemap-affecting action selected (Paint, Erase, etc.), then changing selection away from a Tilemap, then returning to a Tilemap, should the previous tool be restored?",
                                      name = "setting-agressivemode",
                                      value = TilePlusPainterConfig.AgressiveToolRestoration
                                  };
            agressiveToggle.RegisterValueChangedCallback(evt => TilePlusPainterConfig.AgressiveToolRestoration = evt.newValue);
            topSplit.Add(agressiveToggle);

            var marqColor = new ColorField("Scene Marquee Color")
                            {
                                tooltip = "Sets the color of the box at the cursor position",
                                value   = TilePlusPainterConfig.TpPainterMarqueeColor,
                                style =
                                {
                                    flexGrow = 0
                                }
                            };
            marqColor.RegisterValueChangedCallback(evt => { TilePlusPainterConfig.TpPainterMarqueeColor = evt.newValue; });
            topSplit.Add(marqColor);

            var textColor = new ColorField("Scene Text Color")
                            {
                                tooltip = "Sets the color of the text at the cursor position",
                                value   = TilePlusPainterConfig.TpPainterSceneTextColor,
                                style =
                                {
                                    flexGrow = 0
                                }
                            };
            textColor.RegisterValueChangedCallback(evt =>
                                                   {
                                                       TilePlusPainterConfig.TpPainterSceneTextColor = evt.newValue;
                                                       TpPainterSceneView.instance.ReinitializeGuiContent();
                                                   });
            topSplit.Add(textColor);

            var highlightTime = new DropdownField
                                {
                                    label   = LabelText_HighlightTime,
                                    tooltip = LabelToolTip_HighlightTime,
                                    choices =  new() { "1", "2", "3", "4", "5" },
                                    value   = TilePlusConfig.instance.TileHighlightTime.ToString(CultureInfo.InvariantCulture)
                                };

            highlightTime.RegisterValueChangedCallback(evt =>
                                                       {
                                                           if (float.TryParse(evt.newValue, out var result))
                                                               TilePlusConfig.instance.TileHighlightTime = result;
                                                       });

            topSplit.Add(highlightTime);
            
            
            updateListBoxItem= new TpListBoxItem("update-group",Color.clear){style = {flexDirection = FlexDirection.Row, alignItems = Align.FlexStart, alignContent = Align.Stretch, flexGrow = 0}};
            topSplit.Add(updateListBoxItem);
            
            toggle = new TpToggleLeft(ButtonText_PlayUpdate)
                     {
                         tooltip = ToolTip_PlayUpdate,
                         name  = "setting-auto-refresh",
                         value = TilePlusPainterConfig.PainterAutoRefresh
                     };
            toggle.RegisterValueChangedCallback(evt => { TilePlusPainterConfig.PainterAutoRefresh = evt.newValue; });
            updateListBoxItem.Add(toggle);
            updateToggle = toggle;
            updateToggle.schedule.Execute(UpdateEvent).Every(500);
            
            toggle = new TpToggleLeft(LabelText_AutoRefMaps) { name = "setting-auto-refresh-tilemaps", tooltip = Tooltip_AutoRefMaps, value = TilePlusPainterConfig.PainterAutoRefreshTestTilemaps };
            toggle.RegisterValueChangedCallback(evt => { TilePlusPainterConfig.PainterAutoRefreshTestTilemaps = evt.newValue;});
            updateTilemapsToggle = toggle;
            updateListBoxItem.Add(toggle);
            

            //topSplit.Add(new TpSpacer(10, 10));
            topSplit.Add(new TpHelpBox(ToolTip_UiSize,"ui-size-tip"));
            //topSplit.Add(new TpSpacer(10, 10));
            var uiSize2 = new Slider(LabelText_UiSize,
                                     14f,
                                     30f,
                                     SliderDirection.Horizontal,
                                     0.5f)
                          {
                              showInputField = true,
                              tooltip        = ToolTip_UiSize,
                              name           = "setting-ui-size",
                              value          = TilePlusPainterConfig.PainterListItemHeight
                          };

            void UiSizeCallback(ChangeEvent<float> f)
            {
                TilePlusPainterConfig.PainterListItemHeight = f.newValue;

                void Callback() => uiSize2.value = TilePlusPainterConfig.PainterListItemHeight;

                TpLib.DelayedCallback(win, (Callback), "T+P settings update list item height ui", 1000);
            }

            uiSize2.RegisterValueChangedCallback(UiSizeCallback);  
            topSplit.Add(uiSize2);
            topSplit.Add(new TpSpacer(10, 10));

            
            topSplit.Add(new TpHelpBox(ToolTip_SpriteSize, "ui-size-tip"));
            
            var uiSpriteSize = new Slider(LabelText_SpriteSize,
                       20f,
                       99f,
                       SliderDirection.Horizontal,
                       0.5f)
            {
                showInputField = true,
                tooltip        = ToolTip_SpriteSize,
                name           = "setting-ui-sprite-size",
                value          = TilePlusPainterConfig.PainterPaletteItemImageSize
            };


            void UiSpriteSizeCallback(ChangeEvent<float> f)
            {
                TilePlusPainterConfig.PainterPaletteItemImageSize = f.newValue;

                void Callback() => uiSpriteSize.value = TilePlusPainterConfig.PainterPaletteItemImageSize;

                TpLib.DelayedCallback(win, (Callback), "T+P settings update palette item sprite size", 1000);
            }

            uiSpriteSize.RegisterValueChangedCallback(UiSpriteSizeCallback);
            topSplit.Add(uiSpriteSize);
            topSplit.Add(new TpSpacer(10, 10));

            topSplit.Add(new TpHelpBox("Font size in lists (8-20)", "ui-font-size-tip"));

            var contentPanelFontSize = new Slider("List font size",
                                                  8f,
                                                  20f,
                                                  SliderDirection.Horizontal,
                                                  0.5f)
            {
                showInputField = true,
                tooltip        = "Font size for lists",
                name           = "setting-list-font-size",
                value          = TilePlusPainterConfig.ContentPanelFontSize
            };


            void ContentPanelFontSizeCallback(ChangeEvent<float> f)
            {
                TilePlusPainterConfig.ContentPanelFontSize = f.newValue;

                void Callback() => contentPanelFontSize.value = TilePlusPainterConfig.ContentPanelFontSize;

                TpLib.DelayedCallback(win, Callback, "T+P settings update content panel font size", 1000);
            }

            contentPanelFontSize.RegisterValueChangedCallback(ContentPanelFontSizeCallback);

            topSplit.Add(contentPanelFontSize);
            topSplit.Add(new TpSpacer(10,10));

            TpHelpBox helpBox;
            topSplit.Add(helpBox = new TpHelpBox("Relative toolbar size in lists (15-30). Affects top and bottom.\nClick the refresh button at the bottom of the Painter window to update. ", "ui-toobar-size-tip"));
            helpBox.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleLeft);

            var toolbarHeight = new Slider("Toolbars relative size",
                                                  15f,
                                                  30f,
                                                  SliderDirection.Horizontal,
                                                  0.5f)
                                       {
                                           showInputField = true,
                                           tooltip        = "Toolbar size",
                                           name           = "setting-toolbar-size",
                                           value          = TilePlusPainterConfig.ToolbarBaseSize
                                       };

            void ToolbarHeightCallback(ChangeEvent<float> f)
            {
                TilePlusPainterConfig.ToolbarBaseSize = f.newValue;

                void Callback() => toolbarHeight.value = TilePlusPainterConfig.ToolbarBaseSize;

                TpLib.DelayedCallback(win, Callback, "T+P settings update toolbar size", 100);
            }

            toolbarHeight.RegisterValueChangedCallback(ToolbarHeightCallback);

            topSplit.Add(toolbarHeight);
            
            topSplit.Add(new TpSpacer(10, 10));
           
            var textField = new TextField(FieldText_MaxNumTiles,
                                          4,
                                          false,
                                          false,
                                          'x')
                            {
                                isDelayed = true,
                                tooltip =
                                    "50-9999: Defines maximum number of tiles in any list of tiles. Avoids extremely long lists for performance."
                            };
            textField.SetValueWithoutNotify(TilePlusPainterConfig.MaxTilesForViewers.ToString());

            void MaxTilesCallback(ChangeEvent<string> evt)
            {
                if (int.TryParse(evt.newValue, out var num))
                {
                    TilePlusPainterConfig.MaxTilesForViewers = num;
                    textField.value                          = TilePlusPainterConfig.MaxTilesForViewers.ToString();
                }
            }

            textField.RegisterValueChangedCallback(MaxTilesCallback);
            topSplit.Add(textField);
            
            
            var box = new TpListBoxItem("chunk-group",Color.yellow){style = {flexDirection = FlexDirection.Column, alignItems = Align.FlexStart, alignContent = Align.Stretch, flexGrow = 0}};
            topSplit.Add(box);
            box.Add(new TpHelpBox("!!Read the Painter user guide before turning the below toggle ON!!","warning"){tooltip = "No kidding!! You'll be confused if you don't!!"});
            
            var authoringOn = TilePlusPainterConfig.PainterFabAuthoringMode;
            var snappingToggle = new TpToggleLeft(ToggleText_Snapping)
                                 {
                                     tooltip = ToolTip_SnappingMode,
                                     name    = "setting-fab-authoring",
                                     value   = authoringOn
                                 };
            snappingToggle.RegisterValueChangedCallback(evt =>
                                                {
                                                    TilePlusPainterConfig.PainterFabAuthoringMode = evt.newValue;
                                                    evt.StopImmediatePropagation();
                                                });
            box.Add(snappingToggle);

            chunkSize = new IntegerField(FieldText_SnappingChunkSize, 5)
                            {
                                isDelayed = true,
                                tooltip = ToolTip_SnappingChunkSize,
                                name    = "setting-fab-chunksize",
                                value   = TilePlusPainterConfig.PainterFabAuthoringChunkSize,
                                style = {flexGrow = 1}
                            };

            void ChunkSizeCallback(ChangeEvent<int> evt)
            {
                var val     = evt.newValue;
                var refresh = false;
                if (val < 4)
                {
                    val     = 4;
                    refresh = true;
                }

                if (val % 2 != 0)
                {
                    val--;
                    refresh = true;
                }

                TilePlusPainterConfig.PainterFabAuthoringChunkSize = val;
                if (refresh)
                {
                    void Callback() => chunkSize.SetValueWithoutNotify(val);

                    TpLib.DelayedCallback(win, Callback, "TPV+ChunkSizeUpdate", 500);
                }

                evt.StopImmediatePropagation();
            }

            chunkSize.RegisterValueChangedCallback(ChunkSizeCallback);
            box.Add(chunkSize);
            chunkSize.SetEnabled(!authoringOn);
            

            originField = new Vector3IntField(FieldText_SnappingOrigin) { tooltip = ToolTip_SnappingOrigin, name = "setting-fab-auth-origin", value = TilePlusPainterConfig.FabAuthWorldOrigin, style = {flexGrow = .5f} };
            
            originField.RegisterValueChangedCallback(evt =>
                                                     {
                                                         TilePlusPainterConfig.FabAuthWorldOrigin = evt.newValue;
                                                         evt.StopImmediatePropagation();
                                                     });
            box.Add(originField);
            originField.SetEnabled(!authoringOn);
            
            topSplit.Add(new TpSpacer(16, 20){style = {flexGrow = 0}});

            void ResetButtonClickEvent()
            {
                TilePlusPainterConfig.instance.Reset();

                //prevents forcing the initial display of help pane.
                //instance.reset makes this false which will force the UI to show the help screen.
                TilePlusPainterConfig.TpPainterUsedOnce = true;

                //re-init this window after a short delay.
                TpLib.DelayedCallback(win, win.ReInit, "TPP-reset-button-win-reinit", 100);
            }

            var resetButton = new Button(ResetButtonClickEvent)
                              {
                                  tooltip = "Reset Painter configuration to defaults & rebuild the Painter UI.",
                                  name = "reset_button", text = "Reset Painter Settings",  style = {alignSelf = Align.FlexStart}
                              };
            topSplit.Add(resetButton);
            topSplit.Add(new TpSpacer(4, 20){style = {flexGrow = 0} });
            
            
            splitter.Add(topSplit);

            var imgui = new IMGUIContainer(() =>
                                           {
                                               EditorGUILayout.HelpBox("Global TilePlus Toolkit Settings", MessageType.None);
                                               TilePlusConfigView.ConfigOnGUI();
                                           }){style = {minHeight = 25f}};
            imgui.style.whiteSpace = new StyleEnum<WhiteSpace>(WhiteSpace.Normal);
            splitter.Add(imgui);
            Add(new TpSpacer(2,20));
            Add(new TpHelpBox("Top section: Painter settings, Bottom: TPToolkit global", "settings-helpbox"));
            Add(new TpSpacer(2, 20));

            
        }

        private void UpdateEvent()
        {
            updateTilemapsToggle.style.display = updateToggle.value
                                                     ? DisplayStyle.Flex
                                                     : DisplayStyle.None;
            updateListBoxItem.SetColor(updateToggle.value ? Color.white : Color.clear);
        }


        /// <inheritdoc />
        public void OnSettingsChange(string change, ConfigChangeInfo changes)
        {
            var newValue = changes.m_NewValue;
                
            if(newValue is not bool b)
                return;
            if (change == TPC_SettingThatChanged.NoOverwriteFromPalette.ToString() && overwriteToggle.value != b)
                    overwriteToggle.value = b;
            else if (change == TPP_SettingThatChanged.SyncSelection.ToString()   && syncToggle.value != b)
                syncToggle.value = TilePlusPainterConfig.TpPainterSyncSelection;
            else if (change == TPP_SettingThatChanged.UpdateInPlay.ToString() && updateToggle.value != b)
                updateToggle.value = TilePlusPainterConfig.PainterAutoRefresh;
            else if (change == TPP_SettingThatChanged.FabAuthoring.ToString())
            {
                chunkSize.SetEnabled(!b);
                originField.SetEnabled(!b);
            }
            
                
        }


       
        /// <summary>
        /// Adjust a splitter in a vertical splitview
        /// </summary>
        /// <param name="evt">The event</param>
        private void SetupSplitterFix(GeometryChangedEvent evt)
        {
            var handle = splitter.Q<VisualElement>("unity-dragline-anchor");
            handle.style.width           = style.width;
            handle.style.height          = TilePlusPainterWindow.SplitterSize;
            handle.style.backgroundColor = Color.red;
            evt.StopImmediatePropagation();

        }
        
    }
}
