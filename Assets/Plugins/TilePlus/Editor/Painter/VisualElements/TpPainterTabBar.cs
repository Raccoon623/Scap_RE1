// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 11-03-2022
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 05-27-2024
// ***********************************************************************
// <copyright file="TpPainterTabBar.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Creates the buttons bar at the top of the Tile+Painter window</summary>
// ***********************************************************************

using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using static TilePlus.Editor.TpIconLib;
using static TilePlus.Editor.Painter.TpPainterShortCuts;
using Color = UnityEngine.Color;
using Image = UnityEngine.UIElements.Image;
using Object = UnityEngine.Object;

#nullable enable

namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// TpPainterTabBar creates the buttons bar at the top of the Tile+Painter window
    /// Implements the <see cref="VisualElement" />
    /// </summary>
    /// <seealso cref="VisualElement" />
    internal class TpPainterTabBar : VisualElement
    {
        #region const
       
        private const string DefaultContentTooltip    = "This is the Clipboard. It contains the Object(s) picked from the palette or from the scene.";
        private const string DefaultTypeTooltip       = "X means that the Clipboard is empty. Other icons indicate the type of item in the Clipboard";
        private const string DisabledTooltip          = "Disabled...";
        private const string DefaultClearClipTooltip  = "Clear Clipboard";
        private const string DefaultCopyToFavTooltip  = "Copy to Favorites. Hold CTRL to also switch to Favorites. Duplicates rejected.\nNOTE: may cause Importing behaviour.";
        private const string DefaultMakeBundleTooltip = "Convert multiple-selection to a Bundle. Only active when Clipboard has a multiple-selection!";
        private const string DefaultMakeIconTooltip   = "Generate Icon for multiple-selection. Only active when Clipboard has a multiple-selection and Icon doesn't already exist and #tiles <=64. Note: can take some time";
        
        #endregion
        
        
        #region privateFields
        /// <summary>
        /// The tab bar tile pick item image
        /// </summary>
        private readonly Image tabBarTilePickedObjectImage;
        /// <summary>
        /// The main toolbar
        /// </summary>
        private MutuallyExclusiveToolbar? mainToolbar;
        /// <summary>
        /// The mode toolbar
        /// </summary>
        private MutuallyExclusiveToolbar? modeToolbar;
        /// <summary>
        /// Container for clipboard area
        /// </summary>
        private readonly VisualElement? clipBoardContainer;
        /// <summary>
        /// The tab bar tile picked icon
        /// </summary>
        private readonly Image tabBarTilePickedObjectIcon;
        /// <summary>
        /// The picked tile
        /// </summary>
        private TileBase? pickedTile;

        /// <summary>
        /// Clear Clipboard button
        /// </summary>
        private readonly Button? clearClipboardButton;

        /// <summary>
        /// Copy to Favorites button
        /// </summary>
        private readonly Button? copyToFavoritesButton;

        /// <summary>
        /// Make Bundle
        /// </summary>
        private readonly Button? makeBundleButton;

        /// <summary>
        /// Make Icon
        /// </summary>
        private readonly Button? makeIconButton;

        /// <summary>
        /// The picked Object. Might be a tile.
        /// </summary>
        private Object? pickedObject;
        
        /// <summary>
        /// The picked tile
        /// </summary>
        private TpPickedTileType         pickedTileType;
        
        /// <summary>
        /// set TRUE if an asset preview returned a null value, need to refresh.
        /// </summary>
        private bool requiresRefresh;

        
        #endregion
        
        #region privateProperties

        /// <summary>
        /// The parent window
        /// </summary>
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private TilePlusPainterWindow ParentWindow => TilePlusPainterWindow.instance!;
        
        private bool CanAddThisObjectToFavorites =>
            pickedTileType is TpPickedTileType.Tile
                              or TpPickedTileType.Prefab
                              or TpPickedTileType.Multiple
                              or TpPickedTileType.Bundle;
        
        private bool ObjectValid =>
            (pickedTileType is TpPickedTileType.Prefab && pickedObject != null)
            // ReSharper disable once Unity.NoNullPatternMatching
            || (pickedTileType is TpPickedTileType.Tile && pickedTile != null && pickedTile is not TilePlusBase {IsClone:true}) //no clone TPT tiles to Favorites
            || ((pickedTileType == TpPickedTileType.Multiple) && pickedObject != null)
            || (pickedTileType == TpPickedTileType.Bundle && pickedObject != null);

        private bool HasIcon =>
            // ReSharper disable once Unity.NoNullPatternMatching
            pickedTileType == TpPickedTileType.Multiple && pickedObject != null && (pickedObject is TileCellsWrapper w && w.Icon != null); 
        
        #endregion

        #region publicProperties
        
        /// <summary>
        /// What was picked
        /// </summary>
        internal TpPickedTileType PickedTileType => pickedTileType;

        /// <summary>
        /// The currently-picked object or NULL
        /// </summary>
        internal Object? PickedObjectInstance => pickedObject;
        
        #endregion
        
       
        
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="toolbarContainerHeight">Height of the toolbar container.</param>
        /// <param name="modeBarMinWidth">Minimum width of the mode bar.</param>
        internal TpPainterTabBar(float toolbarContainerHeight, float modeBarMinWidth)
        {
            name = "tab-bar-outer container";
            style.borderBottomColor = EditorGUIUtility.isProSkin
                                          ? Color.white
                                          : Color.gray;
            style.borderBottomWidth = 2;
            style.paddingBottom     = 1;
            style.flexShrink        = 0;
            style.height            = new StyleLength(StyleKeyword.Auto);

            schedule.Execute(UpdateEvent).Every(100);

            //main tab bar
            var tabBar = CreateTabBar(toolbarContainerHeight);
            Add(tabBar);

            //need the tab bar's container so we can add things to it
            var tabBarContainer = tabBar.Q<VisualElement>("main-toolbar-container");

            //add the mode bar toggles
            tabBarContainer.Insert(0, CreateModeBar(toolbarContainerHeight));
                        
            tabBarContainer.Insert(1,new TpSpacer(10, 10) { style = { minWidth = 10, flexGrow = 1} });


            //add a spacer
            tabBarContainer.Add(new TpSpacer(10, 10) { style = { minWidth = 10, flexGrow = 0.8f} });
            
            var dim = toolbarContainerHeight * 0.9f;

            clipBoardContainer = new VisualElement(){name="Clipboard", style =
                                                        {
                                                            flexDirection = FlexDirection.Row,
                                                            flexGrow      = 0,
                                                            flexShrink    = 0
                                                        }};
            tabBarContainer.Add(clipBoardContainer);

            var buttonSize = dim / 2;

            var clipboardInner0 = new VisualElement() { style =
                                                      {
                                                          flexGrow     =1, flexDirection = FlexDirection.Column, /*height = dim,*/ minHeight = dim,
                                                          paddingRight = 2
                                                      } };

            
            
            clipboardInner0.Add(makeBundleButton = new Button(MakeBundle){ text = "B", tooltip = DefaultMakeBundleTooltip,
                                               style =
                                           {
                                               fontSize                = 10,
                                               unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold),
                                               flexBasis               = dim *.7f,
                                               minWidth = buttonSize,
                                               minHeight = buttonSize,
                                               flexGrow        = 1,
                                               }});

            
            clipboardInner0.Add(makeIconButton = new Button(MakeIcon){ text = "I", tooltip = DefaultMakeIconTooltip,
                                               style =
                                           {
                                               fontSize = 10,
                                               unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold),
                                               flexBasis = dim * .7f,
                                               minWidth        = buttonSize,
                                               minHeight       = buttonSize,
                                               flexGrow        = 1,
                                           }});
            
            

            
            
            clipBoardContainer.Add(clipboardInner0);
            
            var clipboardInner1 = new VisualElement() { style =
                                                     {
                                                         flexGrow =1, flexDirection = FlexDirection.Column, /*height = dim,*/ minHeight = dim,
                                                         paddingRight = 2
                                                     } };
            
            clipboardInner1.Add(copyToFavoritesButton = new Button(){ text = "F", tooltip = DefaultCopyToFavTooltip,
                                               style =
                                           {
                                               fontSize                = 10,
                                               unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold),
                                               flexBasis               = dim *.7f,
                                               minWidth = buttonSize,
                                               minHeight = buttonSize,
                                               flexGrow        = 1,
                                               }});

            copyToFavoritesButton.RegisterCallback<ClickEvent>(CopyToFavorites);
            
            clipboardInner1.Add(clearClipboardButton = new Button(ClearClipbd){ text = "X", tooltip = DefaultClearClipTooltip,
                                               style =
                                           {
                                               fontSize = 10,
                                               unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold),
                                               flexBasis = dim * .7f,
                                               minWidth        = buttonSize,
                                               minHeight       = buttonSize,
                                               flexGrow        = 1,
                                           }});
            clipBoardContainer.Add(clipboardInner1);
            
            tabBarTilePickedObjectIcon = new Image
                                   {
                                       name = "tile-type-icon",
                                       tooltip =DefaultTypeTooltip,
                                       style = //note: matches TpImageToggle
                                       {
                                           alignSelf         = new StyleEnum<Align>(Align.Center),
                                           height            = dim,
                                           width             = dim,
                                           minHeight         = dim,
                                           minWidth          = dim,
                                           paddingBottom     = 1,
                                           borderBottomWidth = 1,
                                           borderTopWidth =1,
                                           borderLeftWidth = 1,
                                           borderRightWidth = 1,
                                           borderBottomColor = Color.black,
                                           borderTopColor = Color.black,
                                           borderLeftColor = Color.black,
                                           borderRightColor = Color.black,
                                           
                                           paddingTop        = 1,
                                           paddingLeft       = 1,
                                           paddingRight      = 1
                                       },
                                       image = TpIconLib.FindIcon(TpIconType.UnityXIcon)
                                   };
            clipBoardContainer.Add(tabBarTilePickedObjectIcon);
            
            clipBoardContainer.Add(new TpSpacer(10, 5) { style = { minWidth = 1 } });
            
            dim = toolbarContainerHeight * 1.25f;
            const float borderWidth = 1;
            var         borderColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            borderColor.a = 0.4f;
            tabBarTilePickedObjectImage = new Image
                                        {
                                            name = "toolbar-picked-tile",
                                            tooltip = DefaultContentTooltip,
                                                
                                            style =
                                            {
                                                borderBottomColor = borderColor,
                                                borderTopColor    = borderColor,
                                                borderLeftColor   = borderColor,
                                                borderRightColor  = borderColor,
                                                borderBottomWidth = borderWidth,
                                                borderLeftWidth   = borderWidth,
                                                borderRightWidth  = borderWidth,
                                                borderTopWidth    = borderWidth,
                                                marginTop         = 2,
                                                marginRight       = 4,
                                                alignSelf         = new StyleEnum<Align>(Align.Center),
                                                height            = dim,
                                                width             = dim,
                                                minHeight         = dim,
                                                minWidth          = dim
                                            },
                                            image = FindIcon(TpIconType.UnityToolbarMinusIcon)
                                        };

            
            clipBoardContainer.Add(tabBarTilePickedObjectImage);
            return;

            //local methods----------------------------------------------

            void ClearClipbd()
            {
                if (TpPainterState.CurrentTool == TpPainterTool.Paint)
                    TpLib.DelayedCallback(ParentWindow, () => mainToolbar!.SetButtonActive((int)TpPainterTool.None,
                                                                                           true),
                                          "T+V: ClipboardImageClick-ModeChangeToNone",
                                          10);
                ParentWindow.ClearClipboard();
            }
            
            /*void PickedTileImageClickHandlerCtrlKeyHandler(string additional="")
            {
                ParentWindow.ShowNotification(new GUIContent($"Can't add to Favorites!.\n{additional}"));
            }*/

            void MakeIcon()
            {
                // ReSharper disable once InvertIf
                // ReSharper disable once Unity.NoNullPatternMatching
                if(pickedTileType == TpPickedTileType.Multiple && pickedObject != null && (pickedObject is TileCellsWrapper wrapper))
                {
                    if (wrapper.Icon == null)
                        wrapper.Icon = TpImageLib.CreateMultipleTilesIcon(wrapper).sprite;
                    if(wrapper.Icon != null && tabBarTilePickedObjectImage != null)
                        tabBarTilePickedObjectImage.sprite = wrapper.Icon;
                }
            }


            void MakeBundle()
            {
                #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                TpPainterShortCuts.ClipboardBundler();
                #pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }

            void CopyToFavorites(ClickEvent? evt)
            {
                var ctlPressed = evt?.ctrlKey ?? false;
                TpLib.DelayedCallback(ParentWindow, () =>
                                                    {
                                                        TpPainterState.AddClipboardItemToFavorites();
                                                        if(ctlPressed)
                                                            ParentWindow.m_TpPainterContentPanel.SetPaletteSelectionDirect(0);
                                                        if(!pickedObject)
                                                            return;
                                                        var w = pickedObject as TileCellsWrapper;
                                                        if(w == null)
                                                            return;
                                                        if (tabBarTilePickedObjectImage != null)
                                                            tabBarTilePickedObjectImage.sprite = w.Icon;
                                                    }, "T+P: picked tile -> favorites", 20);
            }


            
            VisualElement CreateModeBar(float height)
            {
                var modeSpecs = new System.Collections.Generic.List<ToolbarItemSpec>
                                {
                                    new((int)GlobalMode.PaintingView,
                                        $"Painting {GetModeButtonTooltip()}",
                                        GetModeButtonAbbreviatedTooltip(),
                                        FindIcon(TpIconType.UnityPaintIcon)),
                                    new((int)GlobalMode.EditingView,
                                        $"Editing {GetModeButtonTooltip()}",
                                        GetModeButtonAbbreviatedTooltip(),
                                        FindIcon(TpIconType.TilemapIcon)),
                                    new((int)GlobalMode.GridSelView, 
                                    $"Grid Selection {GetModeButtonTooltip()}",
                                    GetModeButtonAbbreviatedTooltip(),
                                    FindIcon(TpIconType.UnityGridIcon))
                                };

                var modeBar = TpToolbar.CreateMutuallyExclusiveToolbar(modeSpecs, ParentWindow.OnModeBarChanged, height);
                modeToolbar                    = modeBar.Q<MutuallyExclusiveToolbar>("muex_toolbar");
                modeToolbar.style.minWidth     = modeBarMinWidth;
                return modeBar;
            }


            
            VisualElement CreateTabBar(float height)
            {
                
                var binding = ShortcutManager.instance.GetShortcutBinding("TilePlus/Painter/MarqueeDrag [C]");
                var marqSc = $"({binding.ToString()})";
                
                var spec =
                    new System.Collections.Generic.List<ToolbarItemSpec>() //note these need to be in the same order as in the enum TpPainterTool
                    {
                        new((int)TpPainterTool.None,
                            $"Inactivate Tile+Painter. {GetToolTipForTool(TpPainterTool.None)}", 
                            GetAbbreviatedToolTipForTool(TpPainterTool.None), 
                            FindIcon(TpIconType.UnityToolbarMinusIcon)),
                        new((int)TpPainterTool.Paint,
                            $"Painting requires choices in all columns below. Hold SHIFT to drag-paint. Hold CTRL to drag-paint in a row or column. Hold ALT when releasing the mouse to paint the Tile's GameObject. Hold {marqSc} to drag an area to Paint. {GetToolTipForTool(TpPainterTool.Paint)} ",
                            GetAbbreviatedToolTipForTool(TpPainterTool.Paint),
                            FindIcon(TpIconType.UnityPaintIcon)),
                        new((int)TpPainterTool.Erase,
                            $"Erasing requires the selection of a tilemap. Hold SHIFT to drag-erase. Hold CTRL to drag-erase in a row or column. NOTE: both types of 'drag-erase' ignore the 'confirm-deletions' setting! {GetToolTipForTool(TpPainterTool.Erase)}",
                            GetAbbreviatedToolTipForTool(TpPainterTool.Erase),
                            FindIcon(TpIconType.UnityEraseIcon)),
                        new((int)TpPainterTool.Pick,
                            $"Pick a tile in a map and put it in the clipboard (upper-right of this window). Hold CTRL to bypass the clipboard and add the pick to Favorites, SHIFT to override automatic mode change to PAINT after pick. See also the 'pin' mini button in the lower button-bar. {GetToolTipForTool(TpPainterTool.Pick)}",
                            GetAbbreviatedToolTipForTool(TpPainterTool.Pick),
                            FindIcon(TpIconType.UnityPickIcon)),
                        new((int)TpPainterTool.Move,
                            $"Pick a tile, then click again to move it. You can change the Tilemap selection after you pick the tile if you want to move the tile to a different map {GetToolTipForTool(TpPainterTool.Move)}",
                            GetAbbreviatedToolTipForTool(TpPainterTool.Move),
                            FindIcon(TpIconType.UnityMoveIcon)),
                        new((int)TpPainterTool.RotateCw,
                            $"Click on a tile to rotate CW -or- rotate CW while Painting {GetToolTipForTool(TpPainterTool.RotateCw)}",
                            GetAbbreviatedToolTipForTool(TpPainterTool.RotateCw),
                            FindIcon(TpIconType.UnityRotateCwIcon)),
                        new((int)TpPainterTool.RotateCcw,
                            $"Click on a tile to rotate CCW -or- rotate CCW while Painting {GetToolTipForTool(TpPainterTool.RotateCcw)}",
                            GetAbbreviatedToolTipForTool(TpPainterTool.RotateCcw),
                            FindIcon(TpIconType.UnityRotateCcwIcon)),
                        new((int)TpPainterTool.FlipX,
                            $"Click on a tile to Flip X -or- Flip X while Painting {GetToolTipForTool(TpPainterTool.FlipX)}",
                            GetAbbreviatedToolTipForTool(TpPainterTool.FlipX),
                            FindIcon(TpIconType.UnityFlipXIcon)),
                        new((int)TpPainterTool.FlipY,
                            $"Click on a tile to Flip Y  -or- Flip Y while Painting {GetToolTipForTool(TpPainterTool.FlipY)}",
                            GetAbbreviatedToolTipForTool(TpPainterTool.FlipY),
                            FindIcon(TpIconType.UnityFlipYIcon)),
                        new((int)TpPainterTool.ResetTransform,
                            $"Click on a tile to reset its transform -or- reset modified transform while Painting {GetToolTipForTool(TpPainterTool.ResetTransform)}",
                            GetAbbreviatedToolTipForTool(TpPainterTool.ResetTransform),
                            FindIcon(TpIconType.UnityXIcon)),
                        new((int)TpPainterTool.Help,
                            "What is this thing? Click here!",
                            string.Empty,
                            FindIcon(TpIconType.HelpIcon)),
                        new((int)TpPainterTool.Settings,
                            "Settings",
                            string.Empty,
                            FindIcon(TpIconType.SettingsIcon))
                    };
                var tabbar = TpToolbar.CreateMutuallyExclusiveToolbar(spec,
                                                                      ParentWindow.OnMainToolbarChanged,
                                                                      height);
                mainToolbar = tabbar.Q<MutuallyExclusiveToolbar>("muex_toolbar");
                return tabbar;
            }
        }

        private void UpdateEvent()
        {
            if (TpPainterState.InPaintMode)
            {
                if (pickedTileType == TpPickedTileType.None)
                {
                    clearClipboardButton?.SetEnabled(false);
                    copyToFavoritesButton?.SetEnabled(false);
                    makeBundleButton?.SetEnabled(false);
                    makeIconButton?.SetEnabled(false);
                }
                else
                {
                    clearClipboardButton?.SetEnabled(true);
                    copyToFavoritesButton?.SetEnabled(CanAddThisObjectToFavorites && ObjectValid);
                    makeBundleButton?.SetEnabled(pickedTileType == TpPickedTileType.Multiple && ObjectValid);
                    makeIconButton?.SetEnabled(pickedTileType == TpPickedTileType.Multiple && !HasIcon);
                }
            }

            if(!requiresRefresh)
                return;
            if (AssetPreview.IsLoadingAssetPreviews())
                return;
            requiresRefresh = false;
            if(pickedObject != null)
                GetPrefabImage();
        }

        /// <summary>
        /// Enable or disable the Clipboard area
        /// </summary>
        /// <param name="enable"></param>
        internal void EnableClipboard(bool enable)
        {
            clipBoardContainer!.style.visibility = enable ? Visibility.Visible : Visibility.Hidden;
            tabBarTilePickedObjectImage.tooltip  = enable ? DefaultContentTooltip : DisabledTooltip;
            tabBarTilePickedObjectIcon.tooltip   = enable ? DefaultTypeTooltip : DisabledTooltip;
            copyToFavoritesButton!.tooltip       = enable ? DefaultCopyToFavTooltip : DisabledTooltip;
            clearClipboardButton!.tooltip        = enable ? DefaultClearClipTooltip : DisabledTooltip;
            makeBundleButton!.tooltip            = enable ? DefaultMakeBundleTooltip : DisabledTooltip;
            makeIconButton!.tooltip              = enable ? DefaultMakeIconTooltip : DisabledTooltip;
            tabBarTilePickedObjectImage.SetEnabled(enable);
            tabBarTilePickedObjectIcon.SetEnabled(enable);
            copyToFavoritesButton!.SetEnabled(enable);
            clearClipboardButton!.SetEnabled(enable);
        }
        
        /// <summary>
        /// Enables a toolbar button.
        /// </summary>
        /// <param name="tool">which tool?</param>
        /// <param name="enable">enable/disable = true/false</param>
        internal void EnableToolbarButton(TpPainterTool tool, bool enable)
        {
            mainToolbar!.SetButtonEnabled((int)tool, enable);
        }


        /// <summary>
        /// Activates the toolbar button.
        /// </summary>
        /// <param name="tool">which tool?</param>
        /// <param name="withNotify">notification or not.</param>
        internal void ActivateToolbarButton(TpPainterTool tool, bool withNotify)
        {
            mainToolbar!.SetButtonActive((int)tool, withNotify);
        }

        /// <summary>
        /// make a toolbar button visible or invisible
        /// </summary>
        /// <param name="tool">which tool?</param>
        /// <param name="show">show or hide</param>
        internal void ShowToolbarButton(TpPainterTool tool, bool show)
        {
            mainToolbar!.SetButtonVisibility(tool, show);
        }

        /// <summary>
        /// Enables the mode bar button.
        /// </summary>
        /// <param name="mode">Global mode.</param>
        /// <param name="enable">enable/disable = true/false.</param>
        internal void EnableModeBarButton(GlobalMode mode, bool enable)
        {
            modeToolbar!.SetButtonEnabled((int)mode, enable);
        }

        /// <summary>
        /// Activates the mode bar button.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="notify">The notify.</param>
        internal void ActivateModeBarButton(GlobalMode mode, bool notify)
        {
            modeToolbar!.SetButtonActive((int)mode, notify);
        }

        /// <summary>
        /// Hide/show the transform buttons.
        /// </summary>
        /// <param name="show"></param>
        internal void ShowTransformActionButtons(bool show)
        {
            ShowToolbarButton(TpPainterTool.FlipX, show);
            ShowToolbarButton(TpPainterTool.FlipY, show);
            ShowToolbarButton(TpPainterTool.RotateCcw, show);
            ShowToolbarButton(TpPainterTool.RotateCw, show);
            ShowToolbarButton(TpPainterTool.ResetTransform, show);
        }

        /// <summary>
        /// Hide/show the pick and None buttons
        /// </summary>
        /// <param name="show"></param>
        internal void ShowPickAndNoneButtons(bool show)
        {
            ShowToolbarButton(TpPainterTool.Pick,show);
            mainToolbar!.SetToolTip(TpPainterTool.Pick, TpPainterState.InPaintMode ? string.Empty : "Pick a tile from the Scene to inspect");
            ShowToolbarButton(TpPainterTool.None,show);
        }

        /// <summary>
        /// Show/Hide the Move button
        /// </summary>
        /// <param name="show"></param>
        internal void ShowMoveButton(bool show)
        {
            ShowToolbarButton(TpPainterTool.Move,show);

        }

        /// <summary>
        /// Show/Hide the Erase button
        /// </summary>
        /// <param name="show"></param>
        internal void ShowEraseButton(bool show)
        {
            ShowToolbarButton(TpPainterTool.Erase,show);
        }

        /// <summary>
        /// Show/Hide the Paint button
        /// </summary>
        /// <param name="show"></param>
        
        internal void ShowPaintButton(bool show)
        {
            ShowToolbarButton(TpPainterTool.Paint,show);
        }
        
        
        
        
        /// <summary>
        /// Clear the tab bar
        /// </summary>
        internal void SetEmptyTabBar()
        {
            tabBarTilePickedObjectImage.image = FindIcon(TpIconType.UnityToolbarMinusIcon);
            tabBarTilePickedObjectIcon.image  = FindIcon(TpIconType.UnityXIcon);
            pickedTileType                    = TpPickedTileType.None;
            pickedObject                      = null;
            pickedTile                        = null;
        }
        
        /// <summary>
        /// Set the clipboard info into the visible clipboard representation
        /// </summary>
        /// <param name="clipboard">The active Clipboard instance</param>
        internal void SetPickedObject(TpPainterClipboard clipboard)
        {
            pickedObject                        = clipboard.Target;
            pickedTile                          = pickedObject as TileBase;
            pickedTileType                      = clipboard.PickType;
            (var previewSprite, var previewTex) = clipboard.GetClipboardImage();
            if (previewSprite != null)
                tabBarTilePickedObjectImage.sprite = previewSprite;
            else if (previewTex != null)
                tabBarTilePickedObjectImage.image = previewTex;
            else
                tabBarTilePickedObjectImage.image = TpIconLib.FindIcon(TpIconType.HelpIcon);
                    
            tabBarTilePickedObjectIcon.image = clipboard.GetClipboardIcon();
        }
        
        

        private void GetPrefabImage()
        {
            if (pickedObject!=null && pickedObject is GameObject go)
            {
                (var tex, var flag)             = TpPreviewUtility.PreviewGameObject(go);
                tabBarTilePickedObjectImage.image = tex;
                if (!flag)
                    requiresRefresh = true;
            }
            else
                tabBarTilePickedObjectImage.image = FindIcon(TpIconType.PrefabIcon);
        }

        private bool GetTileCellsWrapperImage(TileCellsWrapper wrapper, out Texture2D? icon)
        {
            
            icon = null;
            return false;
        }
            
        
        /// <summary>
        /// change the picked tile icon to show that the transform was changed OR NOT
        /// </summary>
        /// <param name="wasModified">modified or not</param>
        /// <param name="clipboard">active Clipboard instance.</param>
        internal void TabBarTransformModified(bool wasModified, TpPainterClipboard? clipboard)
        {
            if(!TpPainterState.InPaintMode)
                return;
            if (clipboard is not { Valid: true })
            {
                tabBarTilePickedObjectIcon.image       = FindIcon(TpIconType.UnityXIcon);
                return;
            }

            if (wasModified)
                tabBarTilePickedObjectIcon.image = FindIcon(TpIconType.UnityTransformIcon);
            else
                SetPickedObject(clipboard);
        }
        
    }
}
