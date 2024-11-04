// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 11-01-2022
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-31-2023
// ***********************************************************************
// <copyright file="TilePlusPainterConfig.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Configuration options for TilePlus Painter</summary>
// ***********************************************************************

using UnityEditor;
using UnityEngine;

namespace TilePlus.Editor.Painter
{
    // ReSharper disable once InconsistentNaming
    internal enum TPP_SettingThatChanged
    {
        /// <summary>
        /// For Painter - sync Rditor selection and Painter selection
        /// </summary>
        SyncSelection,
        /// <summary>
        /// Painter update-in-play
        /// </summary>
        UpdateInPlay,
        /// <summary>
        /// Painter max tiles shown setting
        /// </summary>
        MaxTilesInViewer,
        /// <summary>
        ///Fab authoring on/off
        /// </summary>
        FabAuthoring,
        /// <summary>
        /// Fab authoring chunk size 
        /// </summary>
        FabAuthoringChunkSize,
        /// <summary>
        /// FabAuthoring World origin
        /// </summary>
        FabAuthoringOrigin,
        /// <summary>
        /// Changed the Painter list item height.
        /// </summary>
        PainterListItemHeight,
        ///<summary>
        /// Size of tile sprite image as shown in RH column when a palette target is being displayed
        /// </summary>
        PainterPaletteItemSize,
        /// <summary>
        /// Painter content panel font size.
        /// </summary>
        PainterContentPanelFontSize,
        /// <summary>
        /// Painter base toolbar size
        /// </summary>
        ToolbarSize,
        /// <summary>
        /// The checkbox to use the Unity tile palette changed state.
        /// </summary>
        UnityPaletteChange
    }
    
    /// <summary>
    /// Scriptable Singleton for Painter config
    /// </summary>
    [FilePath("TpConfig/TilePlusPainterConfig.asset",FilePathAttribute.Location.ProjectFolder)]
    public class TilePlusPainterConfig : ScriptableSingleton<TilePlusPainterConfig>
    {
        [SerializeField]
        private bool m_AgressiveToolRestoration = true;

        /// <summary>
        /// Should painter agressively try to re-select paint tool when
        /// it had previously active but focus had switched to something else,
        /// then returned to a tilemap.
        /// </summary>
        public static bool AgressiveToolRestoration
        {
            get => instance.m_AgressiveToolRestoration;
            set => instance.m_AgressiveToolRestoration = value;
        }


        [SerializeField]
        private float m_ToolbarBaseSize = 20f;

        /// <summary>
        /// Base size for Painter toolbars
        /// </summary>
        public static float ToolbarBaseSize
        {
            get => instance.m_ToolbarBaseSize;
            set
            {
                value = value switch
                        {
                            < 15 => 15,
                            > 30 => 30,
                            _    => value
                        };
                instance.m_ToolbarBaseSize = value;
                instance.RegisterSave();
            }
        }
        
        
        [SerializeField]
        private float m_PainterListItemHeight = 22f;
        /// <summary>
        /// Base size for height of items in Lists (like the tilemaps list for example).
        /// </summary>
        public static float PainterListItemHeight
        {
            get => instance.m_PainterListItemHeight;
            set
            {
                if (value < 14)
                    value = 14;
                if (value > 30)
                    value = 30;
                var change = new ConfigChangeInfo(instance.m_PainterListItemHeight, value);

                instance.m_PainterListItemHeight = value;
                instance.RegisterSave();
                TpEditorUtilities.SettingHasChanged(TPP_SettingThatChanged.PainterListItemHeight.ToString(), change);  

            }
        }

        [SerializeField]
        private float m_PainterPaletteItemImageSize = 50f;

        /// <summary>
        /// Base size of thumbnails in lists of tiles and certain previews
        /// </summary>
        public static float PainterPaletteItemImageSize
        {
            get => instance.m_PainterPaletteItemImageSize;
            set
            {
                if (value < 20)
                    value = 20;
                if (value > 100)
                    value = 100;
                var change = new ConfigChangeInfo(instance.m_PainterPaletteItemImageSize, value);
                instance.m_PainterPaletteItemImageSize = value;
                instance.RegisterSave();
                TpEditorUtilities.SettingHasChanged(TPP_SettingThatChanged.PainterPaletteItemSize.ToString(),change);

            }
        }


        [SerializeField]
        private float m_ContentPanelFontSize = 12.8f;

        /// <summary>
        /// Font size in the Content panel (the two rightmost columns)
        /// </summary>
        public static float ContentPanelFontSize
        {
            get => instance.m_ContentPanelFontSize;
            set
            {
                if (value < 8)
                    value = 8;
                if (value > 20)
                    value = 20;

                var change = new ConfigChangeInfo(instance.m_ContentPanelFontSize, value);
                instance.m_ContentPanelFontSize = value;
                instance.RegisterSave();
                TpEditorUtilities.SettingHasChanged(TPP_SettingThatChanged.PainterContentPanelFontSize.ToString(),change);

            }
        }
        

        [SerializeField]
        private Color m_TpPainterMarqueeColor = Color.yellow;
        
        /// <summary>
        /// Marquee color. Note not same as BRUSH
        /// </summary>
        public static Color TpPainterMarqueeColor
        {
            get => instance.m_TpPainterMarqueeColor;
            set { instance.m_TpPainterMarqueeColor = value; instance.RegisterSave(); }
        }

        [SerializeField]
        private Color m_TpPainterSceneTextColor = Color.yellow;

        /// <summary>
        /// Scene text color for painter. Note not the same as BRUSH
        /// </summary>
        public static Color TpPainterSceneTextColor
        {
            get => instance.m_TpPainterSceneTextColor;
            set { instance.m_TpPainterSceneTextColor = value; instance.RegisterSave(); }
        }

        [SerializeField]
        private bool m_TpPainterUsedOnce;

        /// <summary>
        /// When set, won't force the HELP page to display when painter window is opened
        /// </summary>
        public static bool TpPainterUsedOnce
        {
            get => instance.m_TpPainterUsedOnce;
            set { instance.m_TpPainterUsedOnce = value; instance.RegisterSave(); }
        }

        [SerializeField]
        private bool m_TpPainterShowTilefabs;

        /// <summary>
        /// In Paint mode, should asset view (center column) show Tilefabs?
        /// </summary>
        public static bool TpPainterShowTilefabs
        {
            get => instance.m_TpPainterShowTilefabs;
            set { instance.m_TpPainterShowTilefabs = value; instance.RegisterSave(); }
        }

        [SerializeField]
        private bool m_TpPainterShowMatchingTileFabs = true;

        /// <summary>
        /// In Paint mode, when asset view (center column) shows Tilefabs filter
        /// out those that don't match what in scenes. Can be flaky.
        /// </summary>
        public static bool TpPainterShowMatchingTileFabs
        {
            get => instance.m_TpPainterShowMatchingTileFabs;
            set { instance.m_TpPainterShowMatchingTileFabs = value; instance.RegisterSave(); }
        }
        
        
        [SerializeField]
        private bool m_TpPainterShowTileBundles;

        /// <summary>
        /// In Paint mode, should asset view (center column) show Tile Bundles?
        /// </summary>
        public static bool TpPainterShowTileBundles
        {
            get => instance.m_TpPainterShowTileBundles;
            set { instance.m_TpPainterShowTileBundles = value; instance.RegisterSave(); }
        }
        
        [SerializeField]
        private bool m_TpPainterShowPalettes = true;

        /// <summary>
        /// In Paint mode, should asset view (center column) show Unity-style Palettes (ie show contents of Palette prefabs)?
        /// </summary>
        public static bool TpPainterShowPalettes
        {
            get => instance.m_TpPainterShowPalettes;
            set
            {
                var oldValue = instance.m_TpPainterShowPalettes;
                instance.m_TpPainterShowPalettes = value; 
                instance.RegisterSave();
                var change = new ConfigChangeInfo(oldValue, value);
                TpEditorUtilities.SettingHasChanged(TPP_SettingThatChanged.UnityPaletteChange.ToString(), change);

            }
        }

        [SerializeField]
        private bool m_TpPainterShowPaletteAsGrid = false;

        /// <summary>
        /// In Paint mode, when showing a Palette in the right column should the Unity grid-style palette
        /// display be used?
        /// </summary>
        public static bool TpPainterShowPaletteAsGrid
        {
            get => instance.m_TpPainterShowPaletteAsGrid;
            set { instance.m_TpPainterShowPaletteAsGrid = value;  instance.RegisterSave();}
        }


        [SerializeField]
        private bool m_TpPainterShowBundleAsPalette = false;

        /// <summary>
        /// In Paint mode, when showing Bundles in the right column should the Asset be displayed or its contents?
        /// </summary>
        public static bool TpPainterShowBundleAsPalette
        {
            get => instance.m_TpPainterShowBundleAsPalette;
            set { instance.m_TpPainterShowBundleAsPalette = value;  instance.RegisterSave();}
        }

        
        
        [SerializeField]
        private bool m_TpPainterShowIid;

        /// <summary>
        /// Should painter show instance IDs in EDIT mode (Center column)?
        /// </summary>
        public static bool TpPainterShowIid
        {
            get => instance.m_TpPainterShowIid;
            set { instance.m_TpPainterShowIid = value; instance.RegisterSave(); }
        }


        [SerializeField]
        private bool m_TpPainterPickToPaint;

        /// <summary>
        /// After Picking in Paint mode, should the paint tool be automatically engaged?
        /// </summary>
        public static bool TpPainterPickToPaint
        {
            get => instance.m_TpPainterPickToPaint;
            set { instance.m_TpPainterPickToPaint = value; instance.RegisterSave(); }

        }
    
        [SerializeField]
        private bool m_TpPainterSyncPalette = true;

        /// <summary>
        /// Controls whether or not Painter and the UTE will always use the same Palette selection.
        /// ***Please note that this value should NOT be cached anywhere.***
        /// </summary>
        public static bool TpPainterSyncPalette
        {
            get
            {
                //if palettes are enabled for display and the Palette Grid is enabled AND the painter is actually
                //displaying a palette, then force palette sync.
                var painter = TilePlusPainterWindow.RawInstance;
                if (instance.m_TpPainterShowPalettes && instance.m_TpPainterShowPaletteAsGrid &&
                    painter != null && TpPainterState.PaintableObject is { ItemType: TpPaletteListItemType.Palette })
                    return true;
                return instance.m_TpPainterSyncPalette;

            }
            set
            {
                instance.m_TpPainterSyncPalette = value;
                instance.RegisterSave();
            }
        }
        
        [SerializeField]
        private bool m_TpPainterSyncSelection = true;

        /// <summary>
        /// Controls whether or not the tilemap selection in the left column is mirrored in the heirarchy.
        /// This also works in reverse: when this is true, changes to the left column are mirrorred in the hierarchy
        /// </summary>
        public static bool TpPainterSyncSelection
        {
            get => instance.m_TpPainterSyncSelection;
            set
            {
                var change = new ConfigChangeInfo(instance.m_TpPainterSyncSelection, value);

                instance.m_TpPainterSyncSelection = value;
                TpEditorUtilities.SettingHasChanged(TPP_SettingThatChanged.SyncSelection.ToString(),change);
                instance.RegisterSave();
            }
        }

        [SerializeField] private bool m_TpPainterTilemapSorting; //when FALSE, simple alpha sort. When true, order by sorting layer then sorting order.

        /// <summary>
        /// How are tilemaps sorted in the left column? when true, sort by Renderer layer/order within layer.
        /// When false, alpha sort
        /// </summary>
        public static bool TpPainterTilemapSorting
        {
            get => instance.m_TpPainterTilemapSorting;
            set { instance.m_TpPainterTilemapSorting = value; instance.RegisterSave(); }
        }

        [SerializeField]
        private bool m_TpPainterTilemapSortingReverse;

        /// <summary>
        /// Reverse sorting as set by TpPainterTilemapSorting. Does not reverse alpha sort.
        /// </summary>
        public static bool TpPainterTilemapSortingReverse
        {
            get => instance.m_TpPainterTilemapSortingReverse;
            set { instance.m_TpPainterTilemapSortingReverse = value; instance.RegisterSave(); }
        }
        
        [SerializeField]
        private TpTileSorting m_TpPainterTileSorting = TpTileSorting.Type;

        /// <summary>
        /// Sorting mode in EDIT mode, center column.
        /// </summary>
        public static TpTileSorting TpPainterTileSorting
        {
            get => instance.m_TpPainterTileSorting;
            set { instance.m_TpPainterTileSorting = value; instance.RegisterSave(); }
        }

        // ReSharper disable once InconsistentNaming
        private const int maxTilesForViewers = 400;
        //max num tiles for viewers.
        [SerializeField]
        private int m_MaxTilesForViewers = maxTilesForViewers;

        /// <summary>
        /// Maximum number of items in certain lists.
        /// </summary>
        public static int MaxTilesForViewers
        {
            get => instance.m_MaxTilesForViewers;
            set
            {
                value = value >= 50 ? value : 50;
                value = value > 9999 ? 9999 : value;
                var change = new ConfigChangeInfo(instance.m_MaxTilesForViewers, value);

                instance.m_MaxTilesForViewers = value;
                TpEditorUtilities.SettingHasChanged(TPP_SettingThatChanged.MaxTilesInViewer.ToString(), change);  
                instance.RegisterSave(); }
        }
        
        
        //auto-refresh for TpPainter
        [SerializeField]
        private bool m_PainterAutoRefresh = true;

        //only used when m_PainterAutoRefresh is true. If so and this is false then tilemap add/del
        //not tested during PainterWindow's Heirarchy change event handler.
        [SerializeField]
        private bool m_PainterTestTilemapsInPlayAutoRefresh = true;

        /// <summary>
        /// Allows auto-refresh in EDIT mode when PLAY is active in the editor: refresh the IMGUI display of the
        /// Selection Inspector.
        /// </summary>
        public static bool PainterAutoRefresh
        {
            get => instance.m_PainterAutoRefresh;
            set
            {
                var change = new ConfigChangeInfo(instance.m_PainterAutoRefresh, value);

                instance.m_PainterAutoRefresh = value; 
                instance.RegisterSave(); 
                TpEditorUtilities.SettingHasChanged(TPP_SettingThatChanged.UpdateInPlay.ToString(),change);  

            }
        }

        /// <summary>
        /// When auto-refresh in play (PainterAutoRefresh) is true, then when
        /// PainterWindow.OnHierarchyChange is entered the test for changed tilemaps is performed.
        /// </summary>
        public static bool PainterAutoRefreshTestTilemaps
        {
            get => instance.m_PainterTestTilemapsInPlayAutoRefresh;

            set { instance.m_PainterTestTilemapsInPlayAutoRefresh = value; instance.RegisterSave(); }

        }
        
       
        [SerializeField]
        private bool m_PainterFabAuthoringMode;
        /// <summary>
        /// Special TileFab authoring mode.
        /// </summary>
        public static bool PainterFabAuthoringMode
        {
            get => instance.m_PainterFabAuthoringMode;
            set
            {
                var change = new ConfigChangeInfo(instance.m_PainterFabAuthoringMode, value);

                instance.m_PainterFabAuthoringMode       = value; 
                instance.RegisterSave(); 
                
                TpEditorUtilities.SettingHasChanged(TPP_SettingThatChanged.FabAuthoring.ToString(),change);  

            }
        }
        
        [SerializeField]
        private int m_PainterFabAuthoringChunkSize = 16;

        /// <summary>
        /// Chunk size used when FabAuthoring is active
        /// </summary>
        public static int PainterFabAuthoringChunkSize
        {
            get => instance.m_PainterFabAuthoringChunkSize;
            set
            {
                if (value < 4 || value % 2 != 0)
                    value = 4;
                var change = new ConfigChangeInfo(instance.m_PainterFabAuthoringChunkSize, value);

                instance.m_PainterFabAuthoringChunkSize = value;
                instance.RegisterSave();
                TpEditorUtilities.SettingHasChanged(TPP_SettingThatChanged.FabAuthoringChunkSize.ToString(), change);  

            }
        }
        
        [SerializeField]
        private Vector3Int m_FabAuthWorldOrigin;

        /// <summary>
        /// World origin when FabAuthoring is active
        /// </summary>
        public static Vector3Int FabAuthWorldOrigin
        {
            get => instance.m_FabAuthWorldOrigin;
            set
            {
                var change = new ConfigChangeInfo(instance.m_FabAuthWorldOrigin, value);

                instance.m_FabAuthWorldOrigin = value;
                instance.RegisterSave();
                TpEditorUtilities.SettingHasChanged(TPP_SettingThatChanged.FabAuthoringOrigin.ToString(), change);  

            }
        }
        
        
        private bool saveRequested;
        private void RegisterSave()
        {
            if (saveRequested)
                return;
            EditorApplication.delayCall += DoSave;
            saveRequested               =  true;
            return;

            void DoSave()
            {
                saveRequested = false;
                Save(true);
            }
        }


        /// <summary>
        /// Reset all values
        /// </summary>
        internal void Reset()
        {
            m_MaxTilesForViewers             = maxTilesForViewers;
            m_TpPainterMarqueeColor          = Color.yellow;
            m_TpPainterSceneTextColor        = Color.yellow;
            m_TpPainterUsedOnce              = false;
            m_TpPainterShowTilefabs          = false;
            m_TpPainterShowMatchingTileFabs  = true;
            m_TpPainterShowPalettes          = true;
            m_TpPainterShowTileBundles       = false;
            m_TpPainterSyncSelection         = true;
            m_TpPainterSyncPalette           = true;
            m_PainterListItemHeight          = 22f;
            m_TpPainterTileSorting           = TpTileSorting.Type;
            m_TpPainterTilemapSortingReverse = false;
            m_AgressiveToolRestoration       = true;
            m_PainterAutoRefresh             = true;
            m_PainterTestTilemapsInPlayAutoRefresh             = true;
            m_TpPainterShowIid               = false;
            m_TpPainterPickToPaint           = true;
            m_PainterFabAuthoringMode        = false;
            m_PainterFabAuthoringChunkSize   = 16;
            m_PainterPaletteItemImageSize    = 50f;
            m_FabAuthWorldOrigin             = new Vector3Int(0, 0, 0);
            m_TpPainterTilemapSorting        = false;
            m_ContentPanelFontSize           = 12.8f;
            m_TpPainterShowPaletteAsGrid     = false;
            m_TpPainterShowBundleAsPalette   = false;
            m_ToolbarBaseSize                = 20f;
            Save(true);
        }
        
        
        
    }
}
