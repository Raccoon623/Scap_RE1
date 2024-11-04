#nullable enable
// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 11-01-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-31-2023
// ***********************************************************************
// <copyright file="TilePlusPainterWindow.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Editor window for Tile+Painter</summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using static TilePlus.Editor.TpIconLib;
using static TilePlus.TpLib;
using Color = UnityEngine.Color;
using FontStyle = UnityEngine.FontStyle;
using Label = UnityEngine.UIElements.Label;
using Object = UnityEngine.Object;

namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// An alternate Palette for painting Unity Tilemaps 
    /// </summary>
    public class TilePlusPainterWindow : EditorWindow, IHasCustomMenu
    {
        #region constants
        /// <summary>
        /// The empty field label
        /// </summary>
        internal const string EmptyFieldLabel = "-----";
        /// <summary>
        /// The view panes minimum width
        /// </summary>
        internal const float ViewPanesMinWidth = 150;
        /// <summary>
        /// The mode bar minimum width
        /// </summary>
        private const float ModeBarMinWidth = 80;
        /// <summary>
        /// The window minimum width
        /// </summary>
        private const float WindowMinWidth = 550;
        /// <summary>
        /// The splitter size: width of the splitter handle
        /// </summary>
        internal const float SplitterSize = 4;
        #endregion
        
        #region privateAndInternalFields
        /// <summary>
        /// this window instance. Never more than 1.
        /// </summary>
        private static TilePlusPainterWindow? s_PainterWindow;
        /// <summary>
        /// source for the list of tiles (center pane) when in EDIT view
        /// </summary>
        [SerializeField]
        internal List<TileBase> m_CurrentTileList = new(128);
        /// <summary>
        /// current Type to filter on
        /// </summary>
        internal Type m_FilterType = typeof(TileBase);
        /// <summary>
        /// current tag to filter on  
        /// </summary>
        [SerializeField]
        internal string m_FilterTag = ReservedTag;
        /// <summary>
        /// The editor selection lock is used to avoid infinite recursion/stack overflow for certain UIElement selection operations.
        /// </summary>
        [SerializeField]
        private bool m_EditorSelectionLock;
        /// <summary>
        /// Is the GUI initialized?
        /// </summary>
        [NonSerialized]
        private bool guiInitialized;
        /// <summary>
        /// Version info control flag
        /// </summary>
        private bool versionWasShownOnce;
        /// <summary>
        /// Associated with 'versionWasShownOnce'
        /// </summary>
        private bool versionShownOnceCallbackActivated;
        /// <summary>
        /// Indicates that this window has created its own control ID .
        /// </summary>
        [SerializeField]
        private bool m_HasControlId;
        /// <summary>
        /// Indicates that an Inspector update should refresh the tiles list
        /// </summary>
        private bool wantsRefreshTilesList;
        /// <summary>
        /// Indicates that an Inspector update should reset the Tile filters.
        /// </summary>
        private bool wantsFilterReset;
        /// <summary>
        /// Indicates that an Inspector update should rebind the Tilemaps list.
        /// </summary>
        private bool wantsTilemapListRebind;
        /// <summary>
        /// Flag to refresh the tiles view when a Scene has been loaded/unloaded
        /// </summary>
        private bool updateOnSceneChange;
        /// <summary>
        /// TRUE if the TpPainterTool (on the UNITY toolbar) is activated.
        /// </summary>
        [SerializeField] 
        internal bool m_ToolActivated; 
        /// <summary>
        /// The current palette search string
        /// </summary>
        internal string m_CurrentPaletteSearchString = string.Empty;
        /// <summary>
        /// lists objects that want to get notified about changes to TilePluConfig
        /// </summary>
        private readonly List<ISettingsChangeWatcher> settingsChangeWatchers = new();
        /// <summary>
        /// List of DbChangedArgs from TpLib OnTpLibChanged events
        /// Note that in 3.3 the contents of this list are pooled.
        /// </summary>
        private readonly List<DbChangeArgs> tpLibDbChangeCache = new(128);
        /// <summary>
        /// Control ID for this window
        /// </summary>
        [SerializeField] 
        private int m_MyControlId;
        /// <summary>
        /// # of iterations of CreateGui whilst waiting for TpLib to be ready.
        /// </summary>
        [SerializeField] 
        private int m_CreateGuiIterations; 
        
        //uielements refs
        #nullable disable
        /// <summary>
        /// The tab bar
        /// </summary>
        private TpPainterTabBar        tabBar;
        /// <summary>
        /// The tp painter tilemaps panel: the view which shows all the tilemaps
        /// </summary>
        private TpPainterTilemapsPanel tpPainterTilemapsPanel;  
        /// <summary>
        /// The tp painter content panel: the view which shows palettes/palette content OR list of tiles/tile inspector
        /// </summary>
        internal TpPainterContentPanel  m_TpPainterContentPanel;
        /// <summary>
        /// The tp painter settings panel
        /// </summary>
        private TpPainterSettingsPanel tpPainterSettingsPanel;  
        /// <summary>
        /// The tp painter help panel
        /// </summary>
        private TpPainterHelpPanel     tpPainterHelpPanel;
        /// <summary>
        /// The tilemaps and content panel: contains tilemaps and content panels. Hidden when Settings or Help panels are displayed.
        /// </summary>
        private VisualElement          tilemapsAndContentPanel; 
        /// <summary>
        /// The status label: status shown next to minibuttons
        /// </summary>
        private Label                  statusLabel;
        /// <summary>
        /// The button to the right of the minibuttons. Focus transform editor window. Displays status.
        /// </summary>
        private Button presetsButton;
        /// <summary>
        /// The status area mini buttons: mini-buttons at the bottom of the window.
        /// </summary>
        private TpPainterMiniButtons statusAreaMiniButtons;
        /// <summary>
        /// Main split view for this window.
        /// </summary>
        private TpSplitter mainSplitView;
        
        #nullable enable
        
        /// <summary>
        /// Set to indicate it's time to refresh the tag filter
        /// </summary>
        [NonSerialized]
        private bool refreshTagFilter;
        /// <summary>
        /// Set to indicate it's time to refresh the Type filter
        /// </summary>
        [NonSerialized]
        private bool refreshTypeFilter;
        [SerializeField]
        private bool m_WasDocked;
        [SerializeField]
        private bool m_FirstDockCheck;

        /// <summary>
        /// A HashSet of all UTE tools. Used to detect if a UTE tool is the active EditorTool
        /// </summary>
        private static HashSet<Type> s_UteTools = new (); 
        
        private int lastTilemapSelectionIndex                = -1;
        private int lastEditModeSelectionIndex               = -1;
        private int lastPaintModeInspectorListSelectionIndex = -1;
        private int playModeSkipInspUpdateCount              = 0;
        /// <summary>
        /// Accented text color
        /// </summary>
        internal StyleColor           m_AccentStyleColor        = new(Color.cyan);
        /// <summary>
        /// Bold Style
        /// </summary>
        internal StyleEnum<FontStyle> m_BoldStyle               = new(FontStyle.Bold);
        /// <summary>
        /// Normal Style
        /// </summary>
        internal StyleEnum<FontStyle> m_NormalStyle             = new(FontStyle.Normal);
        /// <summary>
        /// cache border bottom color of window when it's opened.
        /// </summary>
        private StyleColor           originalBorderBottomColor = Color.black;
        #endregion

        #region properties

        
        /// <summary>
        /// Returns false if the app is playing but auto-refresh in Play mode is disabled.
        /// </summary>
        private static bool UpdatingAllowed =>
            !(Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            || TilePlusPainterConfig.PainterAutoRefresh;

        private TpPainterClipboard      TpsCb => TpPainterState.Clipboard!;
        
        /// <summary>
        /// Is this editor window active? Only true if control IDs match or focusedWindow == this window or its SceneView window..
        /// </summary>
        public bool IsActive
        {
            get
            {
                try //note: need try/catch here because this is polled from a static class and odd stuff can happen when computer 'unsleeps' & window is open.
                {
                    return guiInitialized && (GUIUtility.hotControl == m_MyControlId || focusedWindow == this || 
                                              (mouseOverWindow != null && mouseOverWindow.GetType() == typeof(SceneView)));
                }
                catch
                {
                    return false;
                }
            }
        }

        private TpPainterSceneView SceneView => TpPainterSceneView.instance;
        
        /// <summary>
        /// Is Preview active: is a preview being shown in the Scene view?
        /// </summary>
        internal bool PreviewActive => guiInitialized && SceneView.PreviewActive;
        /// <summary>
        /// Is the mouse over the Painter Editor window?
        /// </summary>
        internal bool MouseOverTpPainter =>
            mouseOverWindow != null && mouseOverWindow.GetType() == typeof(TilePlusPainterWindow);

        //is Painter in "Paint" mode and the Brush Inspector is using the Unity Palette instead of a list.
        internal bool PaintModeUtePaletteActive => TpPainterState.InPaintMode && BrushInspector is { BrushInspectorUnityPaletteActive: true };
        /// <summary>
        /// Control ID for this EditorWindow
        /// </summary>
        internal int PainterWindowControlId => m_MyControlId;
        /// <summary>
        /// Is the current Palette being viewed oversized?
        /// IE bigger than the config value MaxTilesForViewers
        /// </summary>
        internal bool OversizedPalette { get; private set; }
        /// <summary>
        /// Get the painter instance directly. will not open any window. Use with care...
        /// </summary>
        // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
        public static TilePlusPainterWindow? RawInstance => s_PainterWindow;
        /// <summary>
        /// STATIC method to gets the painter window instance.
        /// </summary>
        /// <value>The painter window instance</value>
        /// <remarks>Yes, this is a singleton editor window!</remarks>
        // ReSharper disable once InconsistentNaming
        public static TilePlusPainterWindow? instance
        {
            get
            {
                if(s_PainterWindow != null)
                    return s_PainterWindow;
                
                var objs = Resources.FindObjectsOfTypeAll<TilePlusPainterWindow>(); 
                if (objs is { Length: > 0 })
                {

                    s_PainterWindow = objs[0];
                    return s_PainterWindow;
                }

                s_PainterWindow              = GetWindow<TilePlusPainterWindow>();
                s_PainterWindow.titleContent = new GUIContent("Tile+Painter", FindIcon(TpIconType.TptIcon)); 
                s_PainterWindow.minSize      = new Vector2(WindowMinWidth, 384);
                s_PainterWindow.ConditionalResetState();
                return s_PainterWindow;
            }
            private set => s_PainterWindow = value;
        }

        /// <summary>
        /// Is the grid selection Marquee active (note currently unused)
        /// </summary>
        /// <value>true iif grid sel marquee is active.</value>
        internal bool GridSelMarqueeActive =>
            TpPainterShortCuts.MarqueeDragState
            || (GridSelectionPanel is { m_ActiveGridSelection: not null }
                && GridSelectionPanel.m_ActiveGridSelection.m_BoundsInt == GridSelection.position);
        
        /// <summary>
        /// a count of # of tiles in Edit mode (center column).
        /// </summary>
        internal int TilemapPaintTargetCount { get; private set; }
        /// <summary>
        /// is the GUI initialized?
        /// </summary>
        /// <value>T/F</value>
        internal bool GuiInitialized => guiInitialized;
        /// <summary>
        /// Are we discarding list selection events?
        /// </summary>
        /// <value>T/F</value>
        internal bool DiscardListSelectionEvents
        {
            get => m_EditorSelectionLock;
            set => m_EditorSelectionLock = value;
        }
        /// <summary>
        /// when changing palette with palette sync active, set this true to avoid
        /// loop where UTE event changes palette again. See OnPaletteChanged
        /// event handler.
        /// </summary>
        internal bool DiscardUnityPaletteEvents { get; set; }
        /// <summary>
        /// Set true to ignore tool changed events in certain circumstances.
        /// Mostly revolving around the Unity Palette UI Element's behaviour.
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        internal bool DiscardToolManagerActiveToolChangedEvents { get; set; }
        /// <summary>
        /// Get the tab bar instance
        /// </summary>
        /// <value>The tab bar.</value>
        internal TpPainterTabBar TabBar => tabBar;
        ///<summary>Get the Brush Inspector instance</summary>
        internal TpPainterBrushInspector? BrushInspector => m_TpPainterContentPanel?.m_TpPainterBrushInspector;
        /// <summary>
        /// Get the Selection Inspector instance
        /// </summary>
        private TpPainterSelectionInspector? SelectionInspector => m_TpPainterContentPanel?.m_TpPainterSelectionInspector;
        /// <summary>
        /// Get the Grid Selection panel instance.
        /// </summary>
        private TpPainterGridSelPanel? GridSelectionPanel => m_TpPainterContentPanel?.m_GridSelectionPanel;
        /// <summary>
        /// Get the asset Viewer instance (the center column, views palette etc assets in
        /// Paint mode or tiles from the selected Tilemap in Edit mode).
        /// </summary>
        internal TpPainterAssetViewer? AssetViewer => m_TpPainterContentPanel?.m_TpPainterAssetViewer;
        /// <summary>
        /// Get the active Grid Selection Element from the Grid Selection Panel
        /// </summary>
        internal SelectionElement? ActiveGridSelectionElement => GridSelectionPanel?.m_ActiveGridSelection;
        
        /// <summary>
        /// The toolbar container height
        /// </summary>
        private float ToolbarContainerHeight => TilePlusPainterConfig.ToolbarBaseSize; 
        /// <summary>
        /// Data source for the list of tiles (RIGHTMOST column) when in Paint mode
        /// </summary>
        /// <value>ListView items.</value>
        internal List<TpPainterClipboard> PaintModeInspectableObjects
        {
            get
            {
                PaintModeUpdateAssetsList();
                return TpPainterScanners.instance.ObjectsToInspect;
            }
        }

        private PaintableObject? PaintableObject => TpPainterState.PaintableObject;
        
        /// <summary>
        /// The Informational messages setting
        /// </summary>
        internal bool Informational => TpLibEditor.Informational;
        /// <summary>
        /// The Warning messages setting
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        internal bool Warnings => TpLibEditor.Warnings;
        /// <summary>
        /// The Errors messages setting
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        internal bool Errors => TpLibEditor.Errors;
        /// <summary>
        /// Set once in the CreateGui of this window.
        /// </summary>
        internal bool IsProSkin { get; private set; }
        #endregion

        #region init

        /// <summary>
        /// Open the TilePlusViewer window
        /// </summary>
        [MenuItem("Window/2D/Tile+Painter",false,priority = -1)]
        [MenuItem("Window/TilePlus/Tile+Painter", false, 100000)]
        [MenuItem("Tools/TilePlus/Tile+Painter", false, 0)] 
        public static void ShowWindow()
        {
            //just to ensure it's loaded.
            GridPaintingState.instance.GetInstanceID();
            TpPainterSceneView.instance.GetInstanceID();
            TpPainterState.instance.GetInstanceID();
            
            if (RawInstance != null) //ie window already created
            {
                GetWindow<TilePlusPainterWindow>();
                TpPainterState.SetGlobalMode(GlobalMode.PaintingView,GlobalMode.EditingView);
                instance!.m_FirstDockCheck     = false;
                instance.m_WasDocked          = instance.docked; //initial state
                return;
            }
            instance                      = GetWindow<TilePlusPainterWindow>();
            instance.m_FirstDockCheck     = false;
            instance.titleContent         = new GUIContent("Tile+Painter", FindIcon(TpIconType.TptIcon)); 
            instance.minSize              = new Vector2(WindowMinWidth, 384);
            TpPainterState.SetGlobalMode(GlobalMode.PaintingView,GlobalMode.EditingView);
            instance.m_WasDocked          = instance.docked; //initial state
            instance.ConditionalResetState(); 
        }

        private void ConditionalResetState()
        {
            var window = RawInstance;
            if (window == null)
            {
                TpLogError("No painter instance found!");
                return;
            }
            GC.Collect();
            //just to ensure it's loaded.
            GridPaintingState.instance.GetInstanceID();
            TpPainterSceneView.instance.GetInstanceID();
            
            if (TpConditionalTasks.IsActiveConditionalTask(window)!=0) //ie if task already running 
            {
                if (Informational)
                    TpLog($"T+P: CondReset: task already running... \n{TpConditionalTasks.ConditionalTaskInfo}");
                return;
            }
            
            //test the condition first, since ofter (eg attach/detach from tab) there's no need to do the callback
            if (Condition(0))
            {
                if (Informational)
                    TpLog($"T+P: Ready to complete reset, no need to defer...");

                AfterReset(TpConditionalTasks.ContinuationResult.Exec);
                return;
            }

            if (Informational)
                TpLog($"T+P: Not ready to complete reset, defer till ready.");

            TpConditionalTasks.ConditionalDelayedCallback(window,
                                                          AfterReset,
                                                          Condition,
                                                          "T+P: Conditional Reset State",
                                                          10,
                                                          10000); 
            return;

            //note: if this were a lambda there would be closures of the variables which is incorrect in this
            //case as we want to know current values.
            //Also, important to use RawInstance here. If instance is used and it were null, the window would reopen.
            bool Condition(int _) => TpLibIsInitialized && RawInstance != null && RawInstance.guiInitialized;
        }

        private void AfterReset(TpConditionalTasks.ContinuationResult result)
        {
            if (result != TpConditionalTasks.ContinuationResult.Exec)
            {
                if (result != TpConditionalTasks.ContinuationResult.Exception)
                    return;
                if(!TpLibIsInitialized && RawInstance != null)
                    ShowNotification(new GUIContent("Exception in initialization.\nFix error and do\na scripting reload."));
                return;
            }

            //use of RawInstance is important here otherwise window could/would re-open
            var win = RawInstance;
            if (win == null)
            {
                TpLib.TpLogError("No painter instance found!");
                return;
            }
                         
            refreshTagFilter                          = true;
            refreshTypeFilter                         = true;
            versionShownOnceCallbackActivated         = false;
            versionWasShownOnce                       = false;
            TpPainterState.TpPainterMoveSequenceState = TpPainterMoveSequenceStates.None;

            if (Application.isPlaying)
            {
                TpPainterState.SetGlobalMode(GlobalMode.EditingView,GlobalMode.PaintingView);
                tabBar.ActivateModeBarButton(GlobalMode.EditingView, true);
            }
            else
            {
                TpPainterState.SetGlobalMode(GlobalMode.PaintingView,GlobalMode.EditingView);
                tabBar.ActivateModeBarButton(GlobalMode.PaintingView, true);
            }

            var domainPrefs              = TpLibEditor.DomainPrefs;
            var domainPrefsDisableReload = domainPrefs is { EnterPlayModeOptionsEnabled: true, DisableDomainReload: true }; 
            wantsRefreshTilesList = domainPrefsDisableReload;
            updateOnSceneChange   = domainPrefsDisableReload;

            SceneView.ResetState();
                            
            m_EditorSelectionLock        = false;
            m_CurrentPaletteSearchString = string.Empty; 

            var      sel              = Selection.activeGameObject;
            Tilemap? map              = null;
            var      restoreSelection = sel != null && sel.TryGetComponent(out map);
                       
            TpPainterScanners.instance.TilemapsScan();
            var listViewItems = TpPainterScanners.instance.TilemapListViewItems;
            if (listViewItems.Count != 0)
            {
                tpPainterTilemapsPanel.UpdateTilemapsList(listViewItems);
                if (restoreSelection)
                    TryRestoreTilemapSelection(map!.GetInstanceID(), true);
                else
                    TpLib.DelayedCallback(this, () => tpPainterTilemapsPanel.SetSelectionWithoutNotify(-1),
                                          "T+P: RebuildTilemapsList");
                RebuildPaletteListIfChanged();
                BrushInspector?.RebuildBrushInspectorListView();
                BrushInspector?.ClearBrushInspectorSelection();
                BrushInspector?.SetBrushInspectorListViewSelectionIndex(0);
            }
            else
                SetTilemapSelectionLabel();
                       
            // ReSharper disable once Unity.NoNullPatternMatching
            if(!(TilePlusPainterConfig.TpPainterSyncPalette && TilePlusPainterConfig.TpPainterShowPalettes))
            //if (TilePlusPainterConfig.instance is not { instance.TpPainterSyncPalette: true, TpPainterShowPalettes: true })
                return;
            var palette = GridPaintingState.palette;
            if (palette != null)
                m_TpPainterContentPanel.SelectPaletteOrOtherSource(new PaintableObject(palette), false);
            else
                m_TpPainterContentPanel.SelectPaletteOrOtherSource(0);
        }

        /// <summary>
        /// Window menu controls.
        /// </summary>
        /// <param name="menu"></param>
        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Clear Painter Favorites", "Clear Painter Favorites"),   false, TpPainterShortCuts.ClearFavorites);
            menu.AddItem(new GUIContent("Refresh TP system", "Rescans tilemaps, rebuilds TP system internal data, then clears and rebuilds this window."), false, statusAreaMiniButtons.RefreshSystem);
        }

        //DO NOT REMOVE THIS!!
        /// <summary>
        /// Init On Load method. IE, this executes after a scripting reload.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void OnLoad()
        {
            //important to use RawInstance here to avoid auto-reopening the window.
            if (!HasOpenInstances<TilePlusPainterWindow>() || RawInstance == null)
            {
                if(TpLibEditor.Informational) //can't use instance property here
                    TpLog("T+P: OnLoad [InitializeOnLoadMethod] - no open window, terminating..."); 

                return;
            }
            if(TpLibEditor.Informational) 
                TpLog("T+P: OnLoad - [InitializeOnLoadMethod]"); 
            //in case this is a re-load, clear any active tasks.
            TpConditionalTasks.KillConditionalTasksForObject(RawInstance);
            RawInstance.ReInit();
        }

        #endregion

        #region Events
       
        /// <summary>
        /// UPDATE event
        /// </summary>
        private void Update()
        {
            if (!guiInitialized)
                return;

            if (m_FirstDockCheck)
            {
                var oldDockedState = m_WasDocked;
                m_WasDocked = docked; //current state
                if (m_WasDocked != oldDockedState)
                {
                    if (TpConditionalTasks.IsActiveConditionalTask(RawInstance) == 0)
                    {
                        ReInit();
                        return;
                    }
                }
            }
            m_FirstDockCheck = true;
            
            if (updateOnSceneChange && !IsSceneScanActive)
            {
                updateOnSceneChange = false;
                if (UpdatingAllowed)
                {
                    RebuildTilemapsList();
                    RefreshTilesView();
                }
            }

            if (!Application.isPlaying &&
                TpPreviewUtility.PluginCount == 0) //since there are always at least two, this would be an error
            {
                TpPreviewUtility.ResetPlugins();
            }
            
            if (Application.isPlaying)
            {
                if (TilePlusPainterConfig.PainterAutoRefresh)
                {
                    if(TpPainterState.InEditMode)
                        SelectionInspector?.RepaintGui();
                }
            }
            else if (TpPainterState.InEditMode)
            {
                if (refreshTagFilter && m_TpPainterContentPanel != null)
                {
                    refreshTagFilter = false;
                    m_TpPainterContentPanel.RecomputeTagFilter();
                }

                if (refreshTypeFilter && m_TpPainterContentPanel != null)
                {
                    refreshTypeFilter = false;
                    m_TpPainterContentPanel.RecomputeTypeFilter();
                }

                //in edit mode and clipbd has a non-TPT tile
                //then there's no position (just the tile asset is shown). 
                //if the map no longer contains that type of asset then clear the clipbd.
                var cb = TpsCb;
                if (cb is { Valid: true} && cb.SourceTilemap != null && cb.IsNotTilePlusBase)
                {
                    var num = cb.SourceTilemap.GetUsedTilesCount();
                    if (num == 0)
                        TpPainterState.Clipboard = null;
                    else
                    {
                        var ary    = new TileBase[num];
                        var numTypes = cb.SourceTilemap.GetUsedTilesNonAlloc(ary);
                        // ReSharper disable once InvertIf
                        if (numTypes > 0)
                        {
                            if (!ary.Contains(cb.Tile))
                               TpPainterState.Clipboard = null;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Unity Inspector Update event
        /// </summary>
        private void OnInspectorUpdate()
        {
            if (!guiInitialized )
                return;
            
            var activatePreviousTool = false;
            var doUpdate             = UpdatingAllowed;
            if (doUpdate)
            {
                if (TpPainterModifiers.instance.DefaultsChanged)
                {
                    var p = TpPainterModifiers.instance.PrefabDefault != null;
                    var t = TpPainterModifiers.instance.TilesDefault != null;
                    if (!p && !t)
                    {
                        presetsButton.text                  = "p.t";
                        presetsButton.style.backgroundColor = Color.gray;
                    }
                    else
                    {
                        presetsButton.text                  = $"{(p ? "P" : "p")}.{(t ? "T" : "t")}";
                        presetsButton.style.backgroundColor = Color.red;
                    }
                }

                var tpLibDbChangeCacheCount = tpLibDbChangeCache.Count;
                if (tpLibDbChangeCacheCount != 0)
                {
                    if (Informational && tpLibDbChangeCache.Count != 0)
                        TpLog($"Handling {tpLibDbChangeCacheCount} cached Tplib Db change items");

                    for (var i = 0; i < tpLibDbChangeCacheCount; i++)
                    {
                        //if all flags already set no need to continue loop
                        if (wantsRefreshTilesList && wantsTilemapListRebind && wantsFilterReset)
                        {
                            //need to release pooled items
                            if (Informational)
                                TpLog($"T+P: early exit evaluating TpLib change cache, all flags set. {(tpLibDbChangeCacheCount - i).ToString()} skipped...");
                            break;
                        }

                        var args = tpLibDbChangeCache[i];
                        if (Informational)
                            TpLog($"T+P: OnTilemapDbChanged [item:{(i + 1).ToString()}] [changeType: {args.m_ChangeType.ToString()}][PartOfGroup:{args.m_IsPartOfGroup}");

                        if (!wantsRefreshTilesList && (args.m_IsPartOfGroup || args.m_ChangeType != TpLibChangeType.TagsModified))
                            wantsRefreshTilesList = true;

                        //need to test if an addition of a TPT tile to a tilemap w/out any TPT tiles previously.
                        //In this case, need to update the bindings for the tilemap list so that
                        //the TPT icon is correctly shown. This is delayed because
                        //this can get called during Awake/Startup or OnEnable which will cause an exception.
                        if (args.m_ChangeType is TpLibChangeType.AddedToEmptyMap or TpLibChangeType.Added)
                            wantsTilemapListRebind = true;

                        else if (args.m_ChangeType is TpLibChangeType.Deleted or TpLibChangeType.Modified)
                        {
                            if (!TpLib.IsTilemapRegistered(args.m_Tilemap))
                                wantsTilemapListRebind = true;
                        }

                        else if (args.m_ChangeType == TpLibChangeType.TagsModified)
                        {
                            if (Informational)
                                TpLog("Tags were modified (T+Painter)");
                            if (m_FilterTag == ReservedTag)
                                continue;
                            wantsFilterReset = true;
                        }
                    }

                    //release any pooled items in the cache
                    for (var i = 0; i < tpLibDbChangeCacheCount; i++)
                        dbChangeArgsPool.Release(tpLibDbChangeCache[i]);
                    tpLibDbChangeCache.Clear();
                }

                if (wantsFilterReset)
                {
                    m_TpPainterContentPanel.ResetFilters();
                    wantsFilterReset = false;
                }

                if (wantsTilemapListRebind)
                {
                    wantsTilemapListRebind = false;
                    tpPainterTilemapsPanel.ReBindElement(TpPainterScanners.instance.TilemapListViewItems);
                }

                if (wantsRefreshTilesList)
                {
                    wantsRefreshTilesList = false;
                    switch (TpPainterState.GlobalMode)
                    {
                        case GlobalMode.EditingView:
                            RefreshTilesView();
                            break;
                        case GlobalMode.PaintingView:
                            BrushInspector?.RefreshBrushInspectorListView();
                            break;
                        case GlobalMode.GridSelView:
                        default:
                            break;
                    }
                }
                
               
                (var selectionIsTilemap, var selectedMap) = TpLibEditor.SelectionIsTilemap;
                if (ToolManager.activeToolType == typeof(TilePlusPainterTool))
                {
                    var dragLockInfo = SceneView.DragLock;
                    var text         = versionShownOnceCallbackActivated ? TpLib.ShortVersionInformation :  string.Empty;
                    if (dragLockInfo.m_DragX)
                        text = "Active + Drag Lock X";
                    else if (dragLockInfo.m_DragY)
                        text = "Active + Drag Lock Y";
                    else if (TpPainterShortCuts.TpPainterActive)
                        text = "Active";
                    if (TilePlusPainterConfig.PainterFabAuthoringMode)
                        text = $"{text} [Snapping:ON]";
                    (var active, var bounds, _) = SceneView.GridSelMarqueeState;
                    if (active)
                        text = $"{text} [Marquee-Drag][Selection:{bounds.ToString()}]";
                    if (text == string.Empty && selectionIsTilemap)
                        statusLabel.text = "Ready";
                    else
                        statusLabel.text = text;

                    var over   = EditorWindow.mouseOverWindow;
                    var isOver = over != null && over.GetType() == typeof(SceneView);

                    var currentMousePos = SceneView.CurrentMouseGridPosition;
                    if (currentMousePos == TilePlusBase.ImpossibleGridPosition)
                        isOver = false;
                    if (TpPainterState.CurrentTool is TpPainterTool.Help or TpPainterTool.Settings or TpPainterTool.None)
                        isOver = false;

                    RawInstance!.rootVisualElement.style.borderBottomColor = TpPainterShortCuts.TpPainterActive && isOver
                                                                                 ? Color.red
                                                                                 : originalBorderBottomColor;
                }
                else
                {
                    RawInstance!.rootVisualElement.style.borderBottomColor = originalBorderBottomColor;
                    statusLabel.text = selectionIsTilemap
                                           ? "Ready"
                                           : (versionWasShownOnce
                                                  ? string.Empty
                                                  : TpLib.ShortVersionInformation);

                    if (!versionWasShownOnce && !versionShownOnceCallbackActivated)
                    {
                        versionShownOnceCallbackActivated = true;
                        DelayedCallback(s_PainterWindow, () => versionWasShownOnce = true, "T+P: versionFlag", 5000);
                    }

                    if (TilePlusPainterConfig.AgressiveToolRestoration      //if AGG mode is ON.
                        && TpPainterState.InPaintMode                       //and this is Painting View
                        && TpPainterState.CurrentTool == TpPainterTool.None //and there's no active Painter tool
                        && DoesToolHaveTilemapEffect(TpPainterState.PreviousTool)
                        && ToolManager.activeToolType != typeof(TilePlusPainterTool) //And painter is not current tool
                        && !s_UteTools.Contains(ToolManager.activeToolType)          //and the current tool isn't the UTE
                        && TpsCb is { Valid: true }                                  //if a valid Clipboard
                        && selectionIsTilemap)                                       //and if a map is the active selection
                    {
                        SetPaintTarget(selectedMap);
                        ForcePainterTool(true);
                        activatePreviousTool = true;
                    }
                }
            }

            //in play mode, don't do these next UI updates as often.
            if(!doUpdate && ++playModeSkipInspUpdateCount < 16)
                return;
            playModeSkipInspUpdateCount               = 0;
            
            //Disable right-hand splitview when MOVE is active.
            //This dims/inactivates the list of Palettes and their assets.
            m_TpPainterContentPanel.EnableContentPanelSplitView(TpPainterState.CurrentTool != TpPainterTool.Move);

            //show/hide the red indicator for the SelectionSync mini-button
            statusAreaMiniButtons?.SetActivatedIndicator(m_ToolActivated);

            var notHelpOrSettings = TpPainterState.CurrentTool != TpPainterTool.Help && TpPainterState.CurrentTool != TpPainterTool.Settings;
            var enableAllActions  = !TpPainterState.InGridSelMode && notHelpOrSettings;
            var enableManipulators = enableAllActions && TpPainterState.ValidTilemapSelection;
            
            tabBar!.EnableModeBarButton(GlobalMode.GridSelView,  notHelpOrSettings);
            tabBar!.EnableModeBarButton(GlobalMode.PaintingView, notHelpOrSettings);
            tabBar!.EnableModeBarButton(GlobalMode.EditingView,  notHelpOrSettings);
            tabBar!.EnableToolbarButton(TpPainterTool.Help,     TpPainterState.CurrentTool != TpPainterTool.Settings);
            tabBar!.EnableToolbarButton(TpPainterTool.Settings, TpPainterState.CurrentTool != TpPainterTool.Help);

            if (TpPainterState.InPaintMode)
            {
                //enable/disable main toolbar buttons
                var enableManipulatorsExceptWhenSnapping = enableManipulators && (!TilePlusPainterConfig.PainterFabAuthoringMode || TpPainterState.InEditMode);
                //erase is a bit more complex. It's active for enableManipulators but only if (not chunk snapping) or if chunk snapping, need an active zone manager instance.

                //toolbar button enable/disable
                tabBar.EnableToolbarButton(TpPainterTool.None,           enableAllActions);
                // ReSharper disable once MergeIntoPattern
                tabBar.EnableToolbarButton(TpPainterTool.Paint,          TpPainterState.InPaintMode && TpPainterState.PaintingAllowed && enableAllActions);
                tabBar.EnableToolbarButton(TpPainterTool.Erase,          enableManipulators);
                tabBar.EnableToolbarButton(TpPainterTool.Pick,           enableManipulatorsExceptWhenSnapping);
                tabBar.EnableToolbarButton(TpPainterTool.Move,           enableManipulatorsExceptWhenSnapping);
                tabBar.EnableToolbarButton(TpPainterTool.RotateCw,       enableManipulatorsExceptWhenSnapping);
                tabBar.EnableToolbarButton(TpPainterTool.RotateCcw,      enableManipulatorsExceptWhenSnapping);
                tabBar.EnableToolbarButton(TpPainterTool.FlipX,          enableManipulatorsExceptWhenSnapping);
                tabBar.EnableToolbarButton(TpPainterTool.FlipY,          enableManipulatorsExceptWhenSnapping);
                tabBar.EnableToolbarButton(TpPainterTool.ResetTransform, enableManipulatorsExceptWhenSnapping);
            }
            else if (TpPainterState.InEditMode)
            {
                tabBar.EnableToolbarButton(TpPainterTool.None, enableAllActions);
                tabBar.EnableToolbarButton(TpPainterTool.Pick, enableManipulators);
            }

            if (activatePreviousTool)
                tabBar?.ActivateToolbarButton(TpPainterState.PreviousTool, true);
            
            //The tilemaps column (left) is always visible
            var selIndex = tpPainterTilemapsPanel.SelectionIndex;
            if (selIndex >= 0)
            {
                if (lastTilemapSelectionIndex == -1)
                    lastTilemapSelectionIndex = selIndex;
                else if (selIndex != lastTilemapSelectionIndex)
                {
                    tpPainterTilemapsPanel.SetTarget(selIndex);
                    lastTilemapSelectionIndex = selIndex;
                }
            }

            if (AssetViewer != null)
            {
                //painting view: check list selection for center column
                if (TpPainterState.InPaintMode)
                {
                    selIndex = AssetViewer.SelectedPaletteIndex;

                    if (selIndex < 0)
                        m_TpPainterContentPanel.SetPaletteSelectionDirect(0);

                    //don't mess with selections if the Unity palette is visible in the Brush inspector.
                    if (BrushInspector is { BrushInspectorUnityPaletteActive: false })
                    {
                        //and check selection for right column: list of tiles etc.
                        selIndex = BrushInspector.BrushInspectorListViewSelectedIndex;
                        if (selIndex >= 0)
                        {
                            if (lastPaintModeInspectorListSelectionIndex == -1)
                                lastPaintModeInspectorListSelectionIndex = selIndex;
                            else if (selIndex != lastPaintModeInspectorListSelectionIndex)
                            {
                                lastPaintModeInspectorListSelectionIndex = selIndex;
                                if (TpPainterState.Clipboard != null && TpPainterState.Clipboard.ItemIndex != selIndex)
                                    BrushInspector.SelectBrushInspectorTarget(selIndex);
                            }
                        }
                    }
                }
                //edit view: check list selection.
                else if (TpPainterState.InEditMode)
                {
                    selIndex = AssetViewer.TilesListSelectionIndex;
                    if (selIndex >= 0)
                    {
                        if (lastEditModeSelectionIndex == -1)
                            lastEditModeSelectionIndex = selIndex;
                        else if (selIndex != lastEditModeSelectionIndex)
                        {
                            lastEditModeSelectionIndex = selIndex;
                            m_TpPainterContentPanel.SelectTile(selIndex);
                        }
                    }
                }
            }

            var paintableMap = TpPainterState.PaintableMap;
            if (paintableMap != null)
            {
                SetTilemapSelectionLabel(paintableMap is { Valid: true }
                                             ? paintableMap.Name
                                             : EmptyFieldLabel);
            }

            //label for which tilemap is selected
            if (TpPainterState.InPaintMode)
                m_TpPainterContentPanel.ShowTilemapsListSelectionNeededHelpBox(false);
            else  if(TpPainterState.InEditMode)//editing view
                m_TpPainterContentPanel.ShowTilemapsListSelectionNeededHelpBox(paintableMap is not { Valid: true });
        }

        /// <summary>
        /// Fires when becoming visible in a tabbed collection of windows.
        /// </summary>
        private void OnBecameVisible()
        {
            OnFocus();
        }

        private void OnFocus()
        {
            if (!guiInitialized)
                return;
            rootVisualElement.MarkDirtyRepaint();
            //its not specified anywhere if MarkDirtyRepaint repaints the entire heirarchy
            mainSplitView.MarkDirtyRepaint();
            tilemapsAndContentPanel.MarkDirtyRepaint();
            if(TilePlusPainterConfig.AgressiveToolRestoration && TpPainterState.CurrentToolHasTilemapEffect)
                ForcePainterTool();
        }

        /// <summary>
        /// OnEnable event
        /// </summary>
        private void OnEnable()
        {
            var controlIdInfo = TpLibEditor.GetPermanentControlId(); //Get the control ID for this window.
            if (controlIdInfo.valid)
                m_HasControlId = true;
            else
                return;

            TpPainterScanners.instance.Reset();
            m_MyControlId        = controlIdInfo.id;
            TpPainterState.SetGlobalMode(GlobalMode.PaintingView,GlobalMode.PaintingView);
            TpPainterState.SetTools(TpPainterTool.None,TpPainterTool.None);
            
            //gather all the UTE editor tool TYPEs into a hashset.
            s_UteTools = TilemapEditorTool.tilemapEditorTools.Select(x=>x.GetType()).ToHashSet();

            TpPainterScanners.instance.ResetTilemapScanData();
            TpPainterScanners.instance.ResetPaletteScanData();
            m_CurrentTileList.Clear();

            TpLibEditor.ResetGridPalettesCache(); //hack to get the palette list code in this TileMaps internal class to work properly.
            //U3D Code for this is buggy IMO.
            //Part of the issue is that unless the Unity Palette painter window is opened the cache isn't set up correctly (?)

            EditorApplication.playModeStateChanged     += OnplayModeStateChanged;
            ToolManager.activeToolChanged              += OnToolmanagerActiveToolChanged;
            TpEditorUtilities.RefreshOnSettingsChange  += OnSettingsRefreshRequest;
            OnTpLibChanged                             += OnTilemapDbChanged; //when a single change happens
            OnSceneScanComplete                        += SceneScanComplete;
            EditorSceneManager.sceneClosed             += OnSceneClosed;
            EditorSceneManager.sceneOpened             += OnSceneOpened;
            TpLib.OnTypeOrTagChanged                   += OnTypeOrTagChanged; //when a Type or tag is added or removed from TpLib.
            
            TpEditorUtilities.RefreshOnTilemapsCleared += UpdateTilemaps;
            GridSelection.gridSelectionChanged         += OngridSelectionChanged;
            
            GridPaintingState.paletteChanged += OnPaletteChanged;
            Tilemap.tilemapTileChanged += OntilemapTileChanged;
        }
        
        private void OntilemapTileChanged(Tilemap map, Tilemap.SyncTile[] sTiles)
        {
            if(!UpdatingAllowed)
                return;
            if(IsTilemapFromPalette(map))
                return;
            
            // ReSharper disable once MergeIntoPattern
            if (TpPainterState.InEditMode && TpPainterState.Clipboard is { Valid: true, IsTile: true })
            {
                var testPos = TpsCb.Position;
                // ReSharper disable once LoopCanBePartlyConvertedToQuery
                foreach (var st in sTiles)
                {
                    if (st.position != testPos || st.tile != null)
                        continue;
                    wantsRefreshTilesList = true;
                    TpPainterState.Clipboard             = null;
                    break;
                }
                    
            }
            //if a TPT tile is added/deleted, TpLib.OnTpLibChanged fires. If it isn't, update the tilemaps after a delay.
            if (s_InhibitTileChanged || !UpdatingAllowed || TpLib.IsTilemapRegistered(map))
                return;
            s_InhibitTileChanged = true;
            TpLib.DelayedCallback(this, () =>
                                        {
                                            s_InhibitTileChanged = false; //this flag helps keep the number of these callbacks to a minimum.
                                            UpdateTilemaps();
                                        }, "T+P:Update tilemaps after non-tpt tile added.", 1, true);
        }

        private static bool s_InhibitTileChanged;

        /// <summary>
        /// Update tilemaps lists and refresh tilemaps list view.
        /// </summary>
        internal void UpdateTilemaps()
        {
            if(!UpdatingAllowed)
                return;
            TpPainterScanners.instance.TilemapsScan();
            tpPainterTilemapsPanel?.UpdateTilemapsList(TpPainterScanners.instance.TilemapListViewItems);
            //force rebuild of center column
            m_TpPainterContentPanel.UpdateCenterColumnView();
        }
        
        //this would be called when a grid sel is made from the Palette
        private void OngridSelectionChanged()
        {
            if(!TpPainterState.InGridSelMode)
                return;
            
            if (!GridSelection.active || !GridSelection.target.activeSelf || !GridSelection.target.TryGetComponent<Tilemap>(out _))
                return;
            GridSelectionPanel?.AddGridSelection(GridSelection.position);
        }

        /// <summary>
        /// OnDisable event
        /// </summary>
        private void OnDisable()
        {
            guiInitialized = false;
            TpPainterClipboard.Dispose(); //release the statically-embedded GridBrush
            BrushInspector?.Release();    //release the PaintableSceneViewGrid within GridPaintingState.
            
            rootVisualElement.Clear();
            DeselectGridSelPanel();
            TpPainterState.TpPainterMoveSequenceState = TpPainterMoveSequenceStates.None;
            instance                     = null!;
            
            //in case this really isn't a prequel to DESTRUCTION!!
            TpPainterState.SetGlobalMode(GlobalMode.PaintingView,GlobalMode.EditingView);
            TpPainterState.SetTools(TpPainterTool.None,TpPainterTool.None); //note sets toolToRestore to NONE
            
            EditorApplication.playModeStateChanged -= OnplayModeStateChanged;
            OnTpLibChanged                         -= OnTilemapDbChanged; //when a single change happens
            OnSceneScanComplete                    -= SceneScanComplete;
            TpLib.OnTypeOrTagChanged               -= OnTypeOrTagChanged;

            EditorSceneManager.sceneClosed -= OnSceneClosed;
            EditorSceneManager.sceneOpened -= OnSceneOpened;

            ToolManager.activeToolChanged                 -= OnToolmanagerActiveToolChanged;
            TpEditorUtilities.RefreshOnSettingsChange     -= OnSettingsRefreshRequest;
            TpEditorUtilities.RefreshOnTilemapsCleared    -= UpdateTilemaps;
            GridSelection.gridSelectionChanged            -= OngridSelectionChanged;
            GridPaintingState.paletteChanged              -= OnPaletteChanged;
            Tilemap.tilemapTileChanged                    -= OntilemapTileChanged;
           
            
            
            DestroyImmediate(TpPainterSceneView.instance);
            DestroyImmediate(TilePlusPainterFavorites.instance);
            DestroyImmediate(TpPainterScanners.instance);
            DestroyImmediate(TilePlusPainterConfig.instance);
            DestroyImmediate(TpPainterState.instance);
            DestroyImmediate(InspectorToolbar.instance);
            DestroyImmediate(TpPainterGridSelections.instance);
            
            if (ToolManager.activeToolType != typeof(TilePlusPainterTool))
                return;
            // ReSharper disable once Unity.NoNullPatternMatching
            if (Selection.activeObject != null && Selection.activeObject is GameObject go)
            {
                var possibleTilemap = go.GetComponent<Tilemap>();
                if (possibleTilemap != null)
                {
                    var grid = GetParentGrid(possibleTilemap.transform);
                    if (grid != null)
                        Selection.SetActiveObjectWithContext(grid.gameObject, null);
                }
            }

            // Try to activate previously used tool
            TpLib.DelayedCallback(this, ToolManager.RestorePreviousPersistentTool, "TpPainterTool.RestoreToolOnDisable");
        }
        
        /// <summary>
        /// Handler for change in palette (usually from the Unity palette)
        /// </summary>
        /// <param name="newPaletteGo">GO of the new palette</param>
        private void OnPaletteChanged(GameObject newPaletteGo)
        {
            if (DiscardUnityPaletteEvents)
            {
                if (Informational)
                    TpLog("Ignoring Palette change in T+P");
                DiscardUnityPaletteEvents = false;
                return;
            }
            
            if (!GuiInitialized || !TilePlusPainterConfig.TpPainterSyncPalette || !TilePlusPainterConfig.TpPainterShowPalettes)
                return;
            
            m_TpPainterContentPanel.SelectPaletteOrOtherSource(new PaintableObject(newPaletteGo), false); //false prevents the GridPaintingState's palette setting from being updated again.
        }

        /// <summary>
        /// PlayMode StateChange handler
        /// </summary>
        /// <param name="change">The change.</param>
        private void OnplayModeStateChanged(PlayModeStateChange change)
        {
            DeselectGridSelPanel();
            
            
            if (change is not (PlayModeStateChange.EnteredEditMode or PlayModeStateChange.EnteredPlayMode))
                return;
            
            if (change is PlayModeStateChange.EnteredPlayMode && UpdatingAllowed)
            {
                
                TpLib.DelayedCallback(this,() =>
                                           {
                                               TpPainterState.SetPreviousGlobalMode(GlobalMode.PaintingView); //needed for handler to work correctly
                                               tabBar?.ActivateModeBarButton(GlobalMode.EditingView, true);
                                           }, "T+P: EnterPlayMode-forceEditView", 100);

            }
            wantsRefreshTilesList = true;
            updateOnSceneChange   = true;
            m_CurrentTileList.Clear(); //added 1 feb 23
        }

        /// <summary>
        /// Callback from TpLib when something has changed. 
        /// </summary>
        /// <remarks>Note that the event instigator ensures that the 'map' param in 'args' is never the Palette.</remarks>
        private void OnTilemapDbChanged(TpLibChangeType changeType, bool isPartOfGroup, Vector3Int pos, Tilemap? map)
        {
            if(!guiInitialized || !UpdatingAllowed)
                return;
            
            if (IsSceneScanActive)
            {
                wantsFilterReset = true; //handled next Update
                return;
            }
            
            //this one is tricky: it means that a Unity tile (ie Tile or TileBase)
            //was added OR modified. If modified, we don't need to refresh the
            //tiles list: actually we really DON'T want to change it since that
            //will cause any Selection Inspector (in TileDataGui) to revert to 
            //a brush inspector.
            if (changeType != TpLibChangeType.ModifiedOrAdded) //ModifiedOrAdded is only processed here so don't cache it.
            {
                var args = dbChangeArgsPool.Get();
                args.Set(changeType,isPartOfGroup,pos,map);
                tpLibDbChangeCache.Add(args);
            }
            else //is ModifiedOrAdded
            {
                var numNonTppTilesAddedOrDeleted = NonTppTilesAddedOrModified.Count;
                if ( numNonTppTilesAddedOrDeleted == 0
                     || !TpPainterState.InEditMode
                     || TpsCb is not { Valid: true, WasPickedTile: false, IsNotTilePlusBase: false })
                    return;
                //if not editing view or if tiletarget is null or if not picked tile or if it is a TPT tile.
                //note that the args position param is not valid in this case.
                var changedPositions = NonTppTilesAddedOrModified; //this is created in the TpLib OntilemapTileChanged callback

                //was something added? We only care about avoiding a tilemap scan when the mode
                //is EDIT and the selection inspector is showing a picked Unity tile.
                for (var i = 0; i < numNonTppTilesAddedOrDeleted; i++)
                {
                    if (TpsCb.Position == changedPositions[i])
                        continue;
                    wantsRefreshTilesList = true;
                    break;
                }
            }
        }

        /// <summary>
        /// Subcribed to TpLib.OnTypeOrTagChanged
        /// </summary>
        /// <param name="variety">which tag changed?</param>
        private void OnTypeOrTagChanged(OnTypeOrTagChangedVariety variety)
        {
            if(!UpdatingAllowed)
                return;
            if (variety == OnTypeOrTagChangedVariety.Tag)
                refreshTagFilter = true;
            else
                refreshTypeFilter = true;
        }

        /// <summary>
        /// Scene Closed delegate
        /// </summary>
        /// <param name="_">The .</param>
        private void OnSceneClosed(Scene _)
        {
            DeselectGridSelPanel();
            /*if(!UpdatingAllowed)
                return;*/
            if(Informational)
                TpLog("T+P: OnSceneClosed");
            updateOnSceneChange = true;
        }

        /// <summary>
        /// Scene Opened delegate
        /// </summary>
        /// <param name="scene">The scene.</param>
        /// <param name="mode">The mode.</param>
        private void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            DeselectGridSelPanel();
            if(Informational)
                TpLog("T+P: OnSceneOpened");
            updateOnSceneChange = true;
        }
        
        /// <summary>
        /// Scene Scan Complete delegate
        /// </summary>
        private void SceneScanComplete()
        {
            if(Informational)
                TpLog("T+P: OnSceneScanComplete");
            updateOnSceneChange = true;
        }
        
        /// <summary>
        /// Distributes settings change events to visual elements.
        /// </summary>
        /// <param name="change">The change.</param>
        /// <param name="changes">The new value.</param>
        private void OnSettingsRefreshRequest(string change, ConfigChangeInfo changes)
        {
            if (change == TPP_SettingThatChanged.ToolbarSize.ToString())
            {
                if (Mathf.Abs((float)(changes.m_NewValue) - ((float)changes.m_OldValue)) > 0.25f)
                    TpLib.DelayedCallback(s_PainterWindow, ReInit, "T+P: reinit on toolbar size change", 50);
                return; 
            }

            if (change == TPP_SettingThatChanged.UnityPaletteChange.ToString())
            {
                BrushInspector?.ForceUnityPaletteChange(); 
                return; 
            }

            if (change == TPP_SettingThatChanged.FabAuthoring.ToString())
            {
                RebuildPaletteListIfChanged();
                return; 
            }

            if (change == TPP_SettingThatChanged.PainterPaletteItemSize.ToString() || change == TPP_SettingThatChanged.PainterListItemHeight.ToString() ||
                change == TPP_SettingThatChanged.PainterContentPanelFontSize.ToString())
            {
                RefreshTilesView();
                BrushInspector?.RebuildBrushInspectorListView(); 
                tpPainterTilemapsPanel.style.fontSize = TilePlusPainterConfig.ContentPanelFontSize;

                tpPainterTilemapsPanel.UpdateTilemapsList(TpPainterScanners.instance.TilemapListViewItems);
                m_TpPainterContentPanel.style.fontSize = TilePlusPainterConfig.ContentPanelFontSize;
                m_TpPainterContentPanel.ListItemHeight             = TilePlusPainterConfig.PainterListItemHeight;
                AssetViewer?.RebuildEditModeTilesListView();
                RebuildPalettesListView();
                return; 
            }
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < settingsChangeWatchers.Count; i++)
                settingsChangeWatchers[i].OnSettingsChange(change, changes);
        }

        /// <summary>
        /// OnHeirarchyChange delegate.
        /// </summary>
        private void OnHierarchyChange()
        {
            if (!guiInitialized || !UpdatingAllowed)
                return;

            if (IsSceneScanActive)
            {
                if (Informational)
                    TpLog("T+P: OnHierarchyChange - ignoring during Scene scan; Resetting filters");
                m_TpPainterContentPanel.ResetFilters();
                return;
            }
            
            if (SceneView.IgnoreNextHierarchyChange)
            {
                if (Informational)
                    TpLog("Ignoring Heirarchy change in T+P");
                SceneView.IgnoreNextHierarchyChange = false;
                return;
            }
            if (Informational)
                TpLog("Processing Heirarchy change in T+P");

            if (!guiInitialized)
                return;
            
            //if in editing mode and it's a tile then if the source tilemap is valid but the tile has changed then
            //clear the clipboard.
            if (TpPainterState.InEditMode && TpsCb is { Valid: true, ItemVariety: TpPainterClipboard.Variety.TileItem })
            {
                if (TpsCb.SourceTilemap != null)
                {
                    var t =TpsCb.SourceTilemap.GetTile(TpsCb.Position);
                    if (t != null && t != TpsCb.Tile)
                        TpPainterState.Clipboard = null;
                }
            }
            
            //moved to OnProjectChange
            /*if (m_GlobalMode == GlobalMode.PaintingView && !Application.isPlaying)
                RebuildPaletteListIfChanged(); //need to do this since the Palette is a scene with a tilemap so presumably "heirarchy" somehow.*/
            
            //stop here in EDIT mode if we're skipping checking Tilemaps.
            if (TpPainterState.InEditMode && !TilePlusPainterConfig.PainterAutoRefreshTestTilemaps)
                return;
            if (TpPainterScanners.instance.TilemapsScan(true)) //if this returns true then num/names of tilemaps has changed
                tpPainterTilemapsPanel.UpdateTilemapsList(TpPainterScanners.instance.TilemapListViewItems);

            if (TpPainterState.PaintableMap is { Valid: true })
            {
                #pragma warning disable CS8602 // Dereference of a possibly null reference.
                var existingTilemapSelectionId = TpPainterState.PaintableMap.TargetTilemap.GetInstanceID();
                #pragma warning restore CS8602 // Dereference of a possibly null reference.

                tpPainterTilemapsPanel.RebuildElement();
                if (!TryRestoreTilemapSelection(existingTilemapSelectionId))
                    SetTilemapSelectionLabel();
            }
            else
            {
                tpPainterTilemapsPanel.RebuildElement();
                SetTilemapSelectionLabel();
            }
        }

        //need to detect new assets like palettes etc. 
        /// <summary>
        /// OnProjectChange delegate
        /// </summary>
        internal void OnProjectChange()
        {
            if (!guiInitialized)
                return;
            if (Informational)
                TpLog("Processing Project change in T+P");
            DeselectGridSelPanel();
            if (TpPainterState.InPaintMode && !Application.isPlaying)
                RebuildPaletteListIfChanged(); 
            ClearClipboard();
            BrushInspector?.ClearBrushInspectorSelection();
        }

        /// <summary>
        /// Called from TpPainterTool when activated
        /// </summary>
        /// <param name="activeObject">object to select</param>
        internal void OnEditorSelectionChangeFromTool(Object activeObject)
        {
            if (!guiInitialized || activeObject == null)
                return;

            // ReSharper disable once Unity.NoNullPatternMatching
            if (!TilePlusPainterConfig.TpPainterSyncSelection || activeObject is not GameObject go)
                return;
            
            //get the tilemap target
            var tilemapComponents = go.GetComponentsInChildren<Tilemap>();
            if (tilemapComponents.Length != 1 || TpPainterState.PaintableMap is null )
                return;
            if(TpPainterState.PaintableMap.TargetTilemap == null)
                return;
            if (tilemapComponents[0].GetInstanceID() == TpPainterState.PaintableMap.TargetTilemap.GetInstanceID())
                return;
            
            EditorSelectionChangeHandler(activeObject);
        }

        /// <summary>
        /// Selection change event from Hierarchy
        /// </summary>
        private void OnSelectionChange()
        {
            if (!guiInitialized)
                return;

            if (!TilePlusPainterConfig.TpPainterSyncSelection || m_EditorSelectionLock)
                return;
            
            if (Selection.activeObject == null || Selection.count != 1)
                return;
            EditorSelectionChangeHandler(Selection.activeObject);
        }

        /// <summary>
        ///  a system selection change 
        /// </summary>
        /// <param name="selection">selected objects.</param>
        private void EditorSelectionChangeHandler(Object selection)
        {
            if (!guiInitialized || selection == null  || TpPainterState.InGridSelMode) 
                return;

            // ReSharper disable once Unity.NoNullPatternMatching
            if (selection is not GameObject go)
            {
                tabBar.ActivateToolbarButton(TpPainterTool.None, true); //added notify 12/9
                return;
            }

            var isGrid = go.TryGetComponent<Grid>(out _);
            var isMap  = go.TryGetComponent<Tilemap>(out _);
            if (!isGrid && !isMap)
            {
                tabBar.ActivateToolbarButton(TpPainterTool.None, true); //added notify 12/9
                //EditorClearSelection();
                return;
            }

            //if there's an active grid selection on the grid selection panel then
            //see if it's the same as the Tilemap Editor's GridSelection.
            //if it isn't then deselect the grid selection panel's grid selection.
            //this prevents inadvertent loss of the grid selection.
            var gridSelPanelSelection = ActiveGridSelectionElement?.m_BoundsInt;
            if (gridSelPanelSelection.HasValue && GridSelection.position != gridSelPanelSelection.Value)
                DeselectGridSelPanel();

            //reset the move sequence if in PICK state. Move state, no, because we want to be able to pick, change map, and THEN paint.
            if(TpPainterState.TpPainterMoveSequenceState is TpPainterMoveSequenceStates.Pick)
                TpPainterState.TpPainterMoveSequenceState = TpPainterMoveSequenceStates.None;

            var tilemapComponents = go.GetComponentsInChildren<Tilemap>();
            if (tilemapComponents.Length != 1) //if there are 0 or multiple tilemaps in the selection there's nothing to do but
            {
                tabBar.ActivateToolbarButton(TpPainterTool.None, true); //added notify 12/9
                //EditorClearSelection();
                return;
            }

            //look in the itemSource so see if we can find this tilemap.
            var items    = tpPainterTilemapsPanel.DataSource;
            var numItems = items.Count;
            if (numItems == 0)
            {
                tabBar.ActivateToolbarButton(TpPainterTool.None, true); //added notify 12/9
                EditorClearSelection();
                return;
            }

            var selectedMapId  = tilemapComponents[0].GetInstanceID();
            var selectionIndex = -1;
            for (var i = 0; i < numItems; i++)
            {
                var item = items[i];
                if (item is not TilemapData ttd) //unlikely to fail, but need the cast to TilemapData anyway
                    continue;
                if (ttd.TargetMap.GetInstanceID() != selectedMapId) //if this map doesn't match the map in the selection 
                    continue;
                selectionIndex = i;
                break;
            }

            if (selectionIndex >= 0)
            {
                if (items[selectionIndex] is not TilemapData tilemapTarget)
                    return;
                //is this selected tilemap the same as the current tilemapPaintTarget? If it is, don't call SetPaintTarget because of side effects.
                if (TpPainterState.PaintableMap is not null && TpPainterState.PaintableMap.Valid && TpPainterState.PaintableMap.TargetTilemap != null 
                    && tilemapTarget.TargetMap.GetInstanceID() != TpPainterState.PaintableMap.TargetTilemap.GetInstanceID())
                {
                    SetPaintTarget(tilemapTarget.TargetMap); //shouldn't fail
                    if (TpPainterState.InEditMode)
                        SetEditModeInspectorTarget(tilemapTarget.TargetMap);
                }
                else
                {
                    if (TpPainterState.InEditMode)
                        SetEditModeInspectorTarget(tilemapTarget.TargetMap);
                }
                tpPainterTilemapsPanel.SetSelectionWithoutNotify( selectionIndex );
                
            }
            else
            {
                EditorClearSelection();
                tabBar.ActivateToolbarButton(TpPainterTool.None, true);
            }
        }

        
        /// <summary>
        /// Called when Mode Bar is used to change the global mode
        /// </summary>
        /// <param name="choice">0,1, or 2 for Paint, Edit, or GridSel mode</param>
        internal void OnModeBarChanged(int choice)
        {
            //Clear previews for safety, but NOT when exiting Grid Selection mode 
            //as that'll clear the selection.
            if(!TpPainterState.InGridSelMode)
                TpPreviewUtility.ClearPreview();
            
            var candidate = (GlobalMode)choice;
            TpPainterState.SetGlobalModeWithPush(candidate);
            TpPainterState.TpPainterMoveSequenceState = TpPainterMoveSequenceStates.None;
            ClearClipboard();

            if ( (candidate == GlobalMode.GridSelView && TpPainterState.PreviousGlobalMode != GlobalMode.GridSelView) ||
                 (candidate != GlobalMode.GridSelView && TpPainterState.PreviousGlobalMode == GlobalMode.GridSelView))
            {
                TpPainterScanners.instance.TilemapsScan(); 
                tpPainterTilemapsPanel.UpdateTilemapsList(TpPainterScanners.instance.TilemapListViewItems);
                m_TpPainterContentPanel.SetDisplayState(TpPainterState.GlobalMode);
                tabBar.ShowPaintButton(TpPainterState.InPaintMode);
                tabBar.ShowMoveButton(TpPainterState.InPaintMode);
                tabBar.ShowEraseButton(TpPainterState.InPaintMode);
                tabBar.ShowTransformActionButtons(TpPainterState.InPaintMode);
                tabBar.ShowPickAndNoneButtons(!TpPainterState.InGridSelMode);
                if(candidate == GlobalMode.GridSelView)
                    return;
            }
            
            if (TpPainterState.PaintableMap is not null && TpPainterState.PaintableMap.Valid && TpPainterState.PaintableMap.TargetTilemap != null )
                SetEditModeInspectorTarget(TpPainterState.PaintableMap.TargetTilemap);
            
            switch (TpPainterState.GlobalMode)
            { 
                //changing to edit view
                case GlobalMode.EditingView: // when m_GlobalMode == GlobalMode.EditingView:
                    TpPainterState.TpPainterMoveSequenceState = TpPainterMoveSequenceStates.None;
                    m_TpPainterContentPanel.SetDisplayState(GlobalMode.EditingView);
                    AssetViewer?.RebuildEditModeTilesListView();
                    tabBar.ShowMoveButton(false);
                    tabBar.ShowEraseButton(false);
                    tabBar.ShowTransformActionButtons(false);
                    tabBar.ShowPickAndNoneButtons(true);
                    if (TpPainterState.CurrentTool == TpPainterTool.Paint)
                    {
                        TpPainterState.SetCurrentToolOnly(TpPainterTool.None);
                        TpLib.DelayedCallback(this,() =>
                                                   {
                                                       tabBar.ActivateToolbarButton(TpPainterTool.None, true);
                                                       tabBar.ShowPaintButton(false);
                                                   }, "T+P: GM->Palette_deactivate_PaintTool");
                    }
                    else
                        tabBar.ShowPaintButton(false);

                    break;
                //changing to painting view
                case GlobalMode.PaintingView:  // when m_GlobalMode == GlobalMode.PaintingView:
                    RebuildPaletteListIfChanged(); 
                    m_TpPainterContentPanel.SetDisplayState(GlobalMode.PaintingView);
                    tabBar.ShowPaintButton(true);
                    tabBar.ShowMoveButton(true);
                    tabBar.ShowEraseButton(true);
                    tabBar.ShowTransformActionButtons(true);
                    tabBar.ShowPickAndNoneButtons(true);
                    break;
            }

            if (TpPainterState.CurrentTool != TpPainterTool.Move)
                return;
            TpPainterState.SetCurrentToolOnly(TpPainterTool.None);
            TpLib.DelayedCallback(this,() =>
                                       {
                                           tabBar.ActivateToolbarButton(TpPainterTool.None, true);
                                       }, "T+P: GM->Palette_deactivate_MoveTool");

        }

       
        /// <summary>
        /// Target for the Tab bar at the top of the page. Controls what the
        /// user sees in the UI.
        /// </summary>
        /// <param name="index">index of the tab from the (int)enum.</param>
        internal void OnMainToolbarChanged(int index)
        {
            var candidate = (TpPainterTool)index;
            TpPainterState.PushCurrentToolToPrevious();
            
            if(Informational)
                TpLib.TpLog($"main toolbar changed: index = {index}, as TpPainterTool {candidate}");
            
            //if tool changing then interrupt any possible Move sequence
            if (candidate != TpPainterState.CurrentTool)
                TpPainterState.TpPainterMoveSequenceState = TpPainterMoveSequenceStates.None;
            

            //handle clicking on Settings or Help when either is activated
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if ((candidate == TpPainterTool.Settings && TpPainterState.PreviousTool == TpPainterTool.Settings) ||
                (candidate == TpPainterTool.Help && TpPainterState.PreviousTool == TpPainterTool.Help))
            {
                //since the prev tool = candidate, we want to restore the tool in use prior to using Help or Settings
                TpLib.DelayedCallback(this,() => tabBar.ActivateToolbarButton(TpPainterState.ToolToRestore, true), "T+P restore tool", 50);
                return;
            }

            //handle switching help pane on/off
            if (candidate == TpPainterTool.Help && TpPainterState.CurrentTool != TpPainterTool.Help)
            {
                tilemapsAndContentPanel.style.display = DisplayStyle.None;
                tpPainterSettingsPanel.style.display = DisplayStyle.None;
                tpPainterHelpPanel.style.display = DisplayStyle.Flex;
                TpPainterState.PushCurrentToolAsRestorationTool();
                TpPainterState.SetCurrentToolOnly(TpPainterTool.Help);
                return;
            }

            if (candidate != TpPainterTool.Help && TpPainterState.CurrentTool == TpPainterTool.Help)
            {
                tilemapsAndContentPanel.style.display     = DisplayStyle.Flex;
                tpPainterSettingsPanel.style.display = DisplayStyle.None;
                tpPainterHelpPanel.style.display     = DisplayStyle.None;
            }

            //handle switching settings pane on/off
            if (candidate == TpPainterTool.Settings && TpPainterState.CurrentTool != TpPainterTool.Settings)
            {
                tilemapsAndContentPanel.style.display = DisplayStyle.None;
                tpPainterSettingsPanel.style.display  = DisplayStyle.Flex;
                tpPainterHelpPanel.style.display     = DisplayStyle.None;
                TpPainterState.PushCurrentToolAsRestorationTool();
                TpPainterState.SetCurrentToolOnly(TpPainterTool.Settings);
                return;
            }

            if (candidate != TpPainterTool.Settings && TpPainterState.CurrentTool == TpPainterTool.Settings)
            {
                tilemapsAndContentPanel.style.display     = DisplayStyle.Flex;
                tpPainterSettingsPanel.style.display = DisplayStyle.None;
                tpPainterHelpPanel.style.display     = DisplayStyle.None;
            }
            
            //don't allow buttons to be selected if it doesn't make sense
            if ((candidate == TpPainterTool.Paint && !TpPainterState.PaintingAllowed) ||
                (candidate is TpPainterTool.Erase
                              or TpPainterTool.Pick
                              or TpPainterTool.Move
                              or TpPainterTool.RotateCw
                              or TpPainterTool.RotateCcw
                              or TpPainterTool.FlipX
                              or TpPainterTool.FlipY
                              or TpPainterTool.ResetTransform
                 && !TpPainterState.ValidTilemapSelection)) //ValidTilemapSelection means that a tilemap is selected
            {
                TpPainterState.SetCurrentToolOnly(TpPainterTool.None);
                TpLib.DelayedCallback(this,()=> tabBar.ActivateToolbarButton(TpPainterTool.None,false),"T+P force NONE tool",50); 
                return;
            }

            TpPainterState.SetCurrentToolOnly(candidate);
            
            if (TpPainterState.CurrentTool == TpPainterTool.None)
                tabBar.ActivateToolbarButton(TpPainterTool.None, false);
            else //if the current selection is a tilemap then change tools
            {
                if (TilePlusPainterConfig.TpPainterSyncSelection  
                    && TpPainterState.PaintableMap != null
                    && TpPainterState.PaintableMap.TargetTilemap != null) 
                    Selection.SetActiveObjectWithContext(TpPainterState.PaintableMap.TargetTilemap.gameObject, TpPainterState.PaintableMap.TargetTilemap);
                ForcePainterTool();
                
            }

            if (!TpPainterState.CurrentToolHasTilemapEffect)
            {
                if (GUIUtility.hotControl == m_MyControlId)
                    GUIUtility.hotControl = 0;
            }
            else if (SceneView.BrushOpInProgress)
            {
                if (GUIUtility.hotControl != m_MyControlId)
                    GUIUtility.hotControl = m_MyControlId;
            }

            var retestSelection = false;
            if (TpPainterState.CurrentTool is TpPainterTool.None && TpPainterState.PreviousTool is not TpPainterTool.None)
            {
                if (TilePlusPainterConfig.TpPainterSyncSelection)
                {
                    Selection.activeGameObject = null;
                    retestSelection            = true; //flag to test for selection of any tilemap. Just because someone clicked the leftmost (None) button doesn't mean that the selection is gone.
                }
                TpPainterState.TpPainterMoveSequenceState = TpPainterMoveSequenceStates.None;
            }

            if (TpPainterState.CurrentTool is TpPainterTool.None && PaintableObject == null)
            {
                TpPainterState.TpPainterMoveSequenceState = TpPainterMoveSequenceStates.None;
                ClearClipboard();
                if (retestSelection)
                    TestSelection();
                return;
            }

            if (TpPainterState.CurrentTool is not TpPainterTool.Move && TpPainterState.TpPainterMoveSequenceState != TpPainterMoveSequenceStates.None)
            {
                if (TpsCb is { WasPickedTile: true })
                    ClearClipboard();
                TpPainterState.TpPainterMoveSequenceState = TpPainterMoveSequenceStates.None;
            }

            if (TpPainterState.CurrentTool is TpPainterTool.Move)
            {
                TpPainterState.Clipboard = null;
                switch (TpPainterState.TpPainterMoveSequenceState)
                {
                    case TpPainterMoveSequenceStates.None:  //initial state.
                    case TpPainterMoveSequenceStates.Paint: //ie click in Paint state goes to Pick
                        TpPainterState.TpPainterMoveSequenceState = TpPainterMoveSequenceStates.Pick;
                        break;
                    case TpPainterMoveSequenceStates.Pick:
                    default: //ie click when in Pick state -> none
                        TpPainterState.TpPainterMoveSequenceState = TpPainterMoveSequenceStates.None;
                        TpPainterState.SetCurrentToolOnly(TpPainterTool.None);
                        TpLib.DelayedCallback(this,()=> tabBar.ActivateToolbarButton(TpPainterTool.None, false), "T+P force NONE tool on cancel move", 50); 
                        break;
                }
            }

            return;


            //private method
            void TestSelection()
            {
                var currentSelection = Selection.activeGameObject;
                if (currentSelection == null || Selection.count != 1)
                    return;

                var tilemapComponents = currentSelection.GetComponentsInChildren<Tilemap>();
                if (tilemapComponents.Length != 1) //if there are 0 or multiple tilemaps in the selection there's nothing to do but
                    return;

                var mapToTest = tilemapComponents[0];
                if (!CheckForValidPaintingTarget(mapToTest))
                    return;

                //look in the itemSource so see if we can find the same tilemap.
                var items    = tpPainterTilemapsPanel.DataSource;
                var numItems = items.Count;
                if (numItems == 0)
                    return;

                var selectedMapId  = mapToTest.GetInstanceID();
                var selectionIndex = -1;
                for (var i = 0; i < numItems; i++)
                {
                    var item = items[i];
                    if (item is not TilemapData ttd) //unlikely to fail, but need the cast to TilemapData anyway
                        continue;
                    if (ttd.TargetMap.GetInstanceID() != selectedMapId) //if this map doesn't match the map in the selection 
                        continue;
                    selectionIndex = i;
                    break;

                }

                if (selectionIndex < 0)
                    return;
                if (items[selectionIndex] is TilemapData tilemapTarget)
                    SetPaintTarget(tilemapTarget.TargetMap); //shouldn't fail
                tpPainterTilemapsPanel.SetSelectionWithoutNotify(selectionIndex);
            }
        }

        private Type? lastToolManagerActiveToolType;
        /// <summary>
        /// Toolmanager tool changed delegate: issued just after tool changes
        /// </summary>
        private void OnToolmanagerActiveToolChanged()
        {
            if (!guiInitialized || DiscardToolManagerActiveToolChangedEvents)
                return;

            var  activeToolType = ToolManager.activeToolType;
            var changed = lastToolManagerActiveToolType != null && lastToolManagerActiveToolType != activeToolType;
            
            //if the painter tool is already active then there's nothing to do. Also ignore if the Palette is active
            if (!changed) // && ( activeToolType == typeof(TilePlusPainterTool) || activeToolType == typeof(TilemapEditorTool)))
                return;

            DeselectGridSelPanel();

            if(Informational)
                TpLog($"Editor Tool change to {ToolManager.activeToolType} ");

            if(TpPainterState.CurrentTool != TpPainterTool.None)
                //since the painter is no longer the active tool, make it inactive.
                tabBar.ActivateToolbarButton(TpPainterTool.None, true);
            
            
            lastToolManagerActiveToolType = activeToolType;
        }

        #endregion

        #region CreateGui

        /// <summary>
        /// Used to populate the editor window's RootVisualElement.
        /// </summary>
        private async void CreateGUI()
        {
            if (!CreateGuiChecks()) 
                return;
            
            TpIconLib.Init();
            rootVisualElement.Clear();
            

            if(Informational)
                TpLog("Painter: Create Gui...");

            
            rootVisualElement.style.borderBottomWidth = 2;
            IsProSkin = EditorGUIUtility.isProSkin;
            originalBorderBottomColor = IsProSkin
                                            ? Color.black 
                                            : Color.gray;
            rootVisualElement.style.borderBottomColor = originalBorderBottomColor;

            if (!IsProSkin)
                m_AccentStyleColor = new StyleColor(Color.blue);
            
            TpPainterScanners.instance.TilemapsScan(); //Scan for Tilemaps
            TilePlusPainterFavorites.instance.Initialize();
            
            //redundant, initialized does this anyway. TilePlusPainterFavorites.instance.CleanFavoritesList();

            //the tab bar and status indicators
            tabBar = new TpPainterTabBar(ToolbarContainerHeight, ModeBarMinWidth);
            //tabBar.SetEmptyTabBar();
            rootVisualElement.Add(tabBar);

            //the help panel
            tpPainterHelpPanel = new TpPainterHelpPanel();
            rootVisualElement.Add(tpPainterHelpPanel);     
            
            //the settings panel.
            tpPainterSettingsPanel = new TpPainterSettingsPanel(this);
            settingsChangeWatchers.Add(tpPainterSettingsPanel);
            rootVisualElement.Add(tpPainterSettingsPanel);

            //the fourth one is the main content panel.
            /*create a panel for the three splitviews.
              One of them holds a panel with the Tilemaps List (left) 
            */
            tilemapsAndContentPanel = new VisualElement { name = "tilemaps-and-Content", viewDataKey = "TPT.TPPAINTER.MAIN",
                                                            style = { flexGrow = 1, flexDirection = FlexDirection.Row } };
            rootVisualElement.Add(tilemapsAndContentPanel);
            
            //build up the MainPanel. This is a splitview with the Tilemaps list on the left and another
            //splitview on the right. This second splitview has a list of palettes or tilemap tiles, on its left and yet a THIRD
            //splitview on its right.
            //Both of these splitviews contain vertically-oriented splitviews:
            // in the center pane (list of palettes or tilemap tiles) that show list of Palette, Chunks, or favorites (Palette mode)
            // along with some controls in the bottom part of the SV. 
            // in the right pane is a VE with two subviews, only one of which is active at a time depending on Paint or Edit modes
            // Paint mode: vertical splitview that shows what's in the selection in the center column.
            //             the top of this view shows the selection contents: what's in the selected palette.
            //             Select from here and the bottom part of the SV is a mini brush inspector.
            // Edit mode:  Selection Inspector showing info about the tile that was selected from the center column.
            // Grid Selection mode: A list of Grid Selections and some buttons. 
            mainSplitView = new TpSplitter("painter-splitview-outer",
                                                "TPT.TPPAINTER.SPLITVIEW.LEFT", 
                                                100,
                                                TwoPaneSplitViewOrientation.Horizontal,
                                                0,(evt =>
                                                   {
                                                      if(evt.newRect.size == Vector2.zero)
                                                         evt.StopImmediatePropagation(); //needed to preserve splitter pos when changing global modes. LEAVE AS IS!
                                                      else
                                                        evt.StopPropagation();
                                                   }
                                                    ));
            
            var splitterHandle = mainSplitView.Q<VisualElement>("unity-dragline-anchor");
            splitterHandle.style.backgroundColor = Color.red;
            
            tilemapsAndContentPanel.Add(mainSplitView); //add the splitter to the content panel

            //left split view requires two children: first, the left panel which always displays a list of Tilemaps
            tpPainterTilemapsPanel = new TpPainterTilemapsPanel(TpPainterScanners.instance.TilemapListViewItems, ViewPanesMinWidth);
            tpPainterTilemapsPanel.SetTilemapsListHeader(TpPainterState.GlobalMode,TpPainterTool.None, TpPainterMoveSequenceStates.None);
            mainSplitView.Add(tpPainterTilemapsPanel);
            
            //this child comprises the right side of the main split view. the child includes a splitview as well.
            //that splitview includes everything that appears to the right  of the tilemaps list.
            //info about what's selected on the right: either a brush inspector, a selection inspector, or the Grid Selection panel.
            m_TpPainterContentPanel = new TpPainterContentPanel(ViewPanesMinWidth); //content panel controls views and maintains refs to subpanels.
            mainSplitView.Add(m_TpPainterContentPanel);
            settingsChangeWatchers.Add(m_TpPainterContentPanel);

            //now add the minibuttons toolbar
            var container = new VisualElement {name = "bottom-toolbar",   style =
                                              {
                                                  flexGrow = 0,
                                                  flexShrink = 0,
                                                  minHeight = ToolbarContainerHeight + 2,
                                                  marginTop = 2,
                                                  flexDirection = FlexDirection.Row, alignContent = Align.Center
                                              } };
            rootVisualElement.Add(container);
            container.Add(statusAreaMiniButtons = new TpPainterMiniButtons(ToolbarContainerHeight));
            settingsChangeWatchers.Add(statusAreaMiniButtons);

            container.Add(new TpSpacer(1,4));
           
            presetsButton = new Button(() =>
                                           {
                                               if(EditorWindow.HasOpenInstances<TpPainterModifiersEditorWindow>())
                                                   FocusWindowIfItsOpen<TpPainterModifiersEditorWindow>();
                                               else
                                                   TpPainterModifiersEditorWindow.ShowWindow();
                                           }) { text = "p.t", 
                                                  tooltip = "Click to show the Modifiers Editor Window.\nCapitalized letters show Prefab or Tile preset transform preset is active.",
                                                  style =
                                              {
                                                  alignSelf = Align.Center,
                                                  backgroundColor = Color.gray,
                                                  unityFontStyleAndWeight = FontStyle.Bold,
                                                  minWidth = ToolbarContainerHeight,
                                                  minHeight = ToolbarContainerHeight,
                                                  borderBottomWidth       = 1,
                                                  borderTopWidth          = 1,
                                                  borderLeftWidth         = 1,
                                                  borderRightWidth        = 1,
                                                  borderTopColor          = Color.black,
                                                  borderBottomColor       = Color.black,
                                                  borderLeftColor         = Color.black,
                                                  borderRightColor        = Color.black,
                                                  borderBottomLeftRadius  = 4,
                                                  borderBottomRightRadius = 4,
                                                  borderTopLeftRadius     = 4,
                                                  borderTopRightRadius    = 4,
                                                  paddingLeft = 2,
                                                  paddingRight = 2
                                                  
                                              } };
            
            container.Add(presetsButton);
            container.AddToClassList("Button");
            container.Add(new TpSpacer(1, 4));
            statusLabel = new Label{
                                       text = TpLib.ShortVersionInformation,
                                       style =
                                   {
                                      unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter)
                                       
                                   }};
            container.Add(new TpSpacer(10,20));
            container.Add(statusLabel);
            // wait for any previews to load. Avoids "textureless sprite" warnings when the GUI actually generates visual content.
            // but don't wait more than two seconds. the delayed callback ensures that this doesn't continue past 2 seconds.
            var timedOut = false;
            TpLib.DelayedCallback(this, () => timedOut = true,"T+P asset pvu chk",2000);
            while (AssetPreview.IsLoadingAssetPreviews())
            {
                if (timedOut)
                    break;
                if(Informational)
                    TpLog("Waiting on Asset Preview loading....");
                await Task.Yield();
            }
            guiInitialized = true;

            //for convenience, when window opens, sync to current selection IF it is a Tilemap. Ignores config setting for Selection sync
            if (Selection.count == 0)
                return;

            var go = Selection.activeGameObject;
            if (go == null)
                return;

            var possibleMap = go.GetComponent<Tilemap>();
            if (possibleMap != null)
                SetPaintTarget(possibleMap);
            
            GuiComplete();
        }

       
        private void GuiComplete()
        {
            if (Application.isPlaying)
            {
                TpPainterState.SetPreviousGlobalMode(GlobalMode.PaintingView); //needed for handler to work correctly
                tabBar.ActivateModeBarButton(GlobalMode.EditingView, true);
            }
            else
            {
                TpPainterState.SetPreviousGlobalMode(GlobalMode.EditingView);
                tabBar.ActivateModeBarButton(GlobalMode.PaintingView, true);
            }

            var domainPrefs = TpLibEditor.DomainPrefs;
            if (domainPrefs is { EnterPlayModeOptionsEnabled: true, DisableDomainReload: true }) 
            {
                wantsRefreshTilesList = true;
                updateOnSceneChange   = true;
            }

            if (TilePlusPainterConfig.TpPainterUsedOnce)
                return;
            TilePlusPainterConfig.TpPainterUsedOnce = true;
            TpPainterState.SetCurrentToolOnly(TpPainterTool.None);
            //bugfix 3/31/24: ensures view set properly on Brush inspector.
            TpLib.DelayedCallback(this, () => OnMainToolbarChanged((int)TpPainterTool.Help), "T+P:force painter help tab", 100);

        }
        #endregion

        #region scanners
        
        /// <summary>
        /// creates a list of data to display in the rightmost pane in PAINT mode.
        /// </summary>
        /// <param name="refresh">refresh the associated view</param>
        internal void PaintModeUpdateAssetsList(bool refresh = true)
        {
            if(PaintableObject == null)
                return;
            OversizedPalette = TpPainterScanners.instance.PaintModeGetInspectables(PaintableObject);
            
            if (m_TpPainterContentPanel != null)
            {
                var isBundleOrFab =
                    PaintableObject.ItemType is TpPaletteListItemType.Bundle  or TpPaletteListItemType.TileFab;

                if (PaintableObject.ItemType == TpPaletteListItemType.Bundle && TilePlusPainterConfig.TpPainterShowBundleAsPalette)
                    isBundleOrFab = false;
                
                BrushInspector?.m_BrushInspectorListView.SetVirtualizationMethod(isBundleOrFab);
            }
            if (refresh && m_TpPainterContentPanel != null)
                BrushInspector?.RebuildBrushInspectorListView();
        }

        #endregion

        #region rebuilders

        /// <summary>
        /// Rebuilds the tilemaps list.
        /// </summary>
        internal void RebuildTilemapsList()
        {
            if(!UpdatingAllowed)
                return;
            var      sel              = Selection.activeGameObject;
            Tilemap? map              = null;
            var      restoreSelection = sel != null && sel.TryGetComponent(out map);
                                                       
            TpPainterScanners.instance.TilemapsScan();
            var listViewItems = TpPainterScanners.instance.TilemapListViewItems;
            if (listViewItems.Count == 0)
            {
                SetTilemapSelectionLabel();
                return;
            }

            tpPainterTilemapsPanel.UpdateTilemapsList(listViewItems);
            if(restoreSelection)
                TryRestoreTilemapSelection(map!.GetInstanceID(), true);
            else
                tpPainterTilemapsPanel.SetSelectionWithoutNotify(-1);
            RebuildPaletteListIfChanged();
            BrushInspector?.RebuildBrushInspectorListView();
            BrushInspector?.ClearBrushInspectorSelection();
            BrushInspector?.SetBrushInspectorListViewSelectionIndex(0);
        }

        /// <summary>
        /// Rebuilds the palette list if changed.
        /// </summary>
        internal void RebuildPaletteListIfChanged()
        {
            if (!guiInitialized)
                return;
            TpPainterScanners.instance.PalettesScan(m_CurrentPaletteSearchString);
            AssetViewer?.RefreshPalettesListView();
            PaintModeUpdateAssetsList();
            if (!TpPainterScanners.instance.RescanTilefabs) //error when scanning for matching tilefabs
                return;
            if (Warnings)
                TpLogWarning("Scheduling rebuild of palette list in 500 msec.");
            rootVisualElement.schedule.Execute(RebuildPaletteListIfChanged).ExecuteLater(500);
        }
        
        /// <summary>
        /// Refreshes the tiles view.
        /// </summary>
        internal void RefreshTilesView()
        {
            if (TpPainterState.PaintableMap is { Valid: true }  && TpPainterState.PaintableMap.TargetTilemap != null)
            {
                SetEditModeInspectorTarget(TpPainterState.PaintableMap.TargetTilemap);
            }
            else
            {
                SetEditModeInspectorTarget(null);
            }
        }

        #endregion

        #region paintTarget
        /// <summary>
        /// Sets the paint target (a TileMap).
        /// </summary>
        /// <param name="target">target tilemap</param>
        /// <returns>true for no errors.</returns>
        internal bool SetPaintTarget(Tilemap? target)
        {
            if (!guiInitialized || target == null)
                return true;
            
            //ensure a reasonable choice
            if (!CheckForValidPaintingTarget(target))
                return false;

            if (TpPainterState.PaintableMap != null
                && TpPainterState.PaintableMap.TargetTilemap != null
                && TpPainterState.PaintableMap.TargetTilemap == target) //no change, ignore
                return false;

            var oldTgt = PaintableObject;
            ResetSelections();
            TpPainterState.PaintableObject       = oldTgt;
            PaintModeUpdateAssetsList();
                
            //2.1 - ensures that the GridPaintingState is updated: this ensures that SceneViewGridManager
            //scriptable singleton is activated even if Unity Palette window isn't open.
            GridPaintingState.scenePaintTarget = target.gameObject;
            
            wantsRefreshTilesList = true;  
            
            TpPainterState.PaintableMap = new PaintableMap(target); //the tilemap to paint.
            
            SetTilemapSelectionLabel(target.name);
                      
            return true;
        }

        /// <summary>
        /// Checks for valid painting target, ie is the tilemap valid to paint.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns>bool.</returns>
        private bool CheckForValidPaintingTarget(Tilemap target)
        {
            return target.gameObject.activeInHierarchy &&
                   target.GetComponentInParent<Grid>() != null &&
                   !PrefabUtility.IsPartOfPrefabAsset(target); 
        }

        #endregion

        #region inspectorTarget
        
        /// <summary>
        /// updates a list of tiles from a tilemap in EDIT mode
        /// </summary>
        /// <param name="target">tilemap to obtain tiles from.</param>
        internal void SetEditModeInspectorTarget(Tilemap? target)
        {
            if (!guiInitialized)
                return;

            if (target == null)
            {
                m_CurrentTileList.Clear();
                TilemapPaintTargetCount = 0;
                AssetViewer?.RebuildEditModeTilesListView();
                return;
            }
            
            SetTilemapSelectionLabel(target.name);
            var limit         = TilePlusPainterConfig.MaxTilesForViewers;
            var previousCount = m_CurrentTileList.Count;
            m_CurrentTileList.Clear();

            //filterType being TileBase means 'wildcard'
            var wildcard = m_FilterType == typeof(TileBase);
            
            //usingPlugin will be true if the filter type is from a plugin
            //(typically means this is some other class of TileBase such as Rule tile)
            //but if the filter type is 'wildcard' then don't bother to check.
            var usingPlugin  = !wildcard && TpPreviewUtility.PluginExists(m_FilterType); 
            
            /* Type filtering
             * The filter type 'TileBase' means everything
             * The filter type 'TilePlusBase' means all TilePlus tiles
             * The filter type 'Tile' means all standard Unity Tiles + or any Subclasses like TilePlus tiles.
             * Others are dynamically added to the list of possible Types (see ComputeTypeFilter in TpPainterContentPanel.cs)
             */
            //process TPT tiles. First comes tag filtering
            //so we only do this step for TPT tiles ... if the filter isn't 'wildcard' or a subclass of TilePlusBase
            if(wildcard || m_FilterType == typeof(TilePlusBase) || m_FilterType.IsSubclassOf(typeof(TilePlusBase)))
            {
                var tpbList = GetAllTilePlusBaseForMap(target);
                if (tpbList != null)
                {
                    if (tpbList.Count > limit)
                    {
                        m_CurrentTileList.AddRange(tpbList.Take(limit).ToList());
                        TilemapPaintTargetCount = tpbList.Count;
                        AssetViewer?.RebuildEditModeTilesListView();
                        return; //don't sort.
                    }

                    m_CurrentTileList.AddRange(wildcard //if wildcard, use all tiles.
                                                   ? tpbList 
                                                   : tpbList.Where(tpb => tpb.GetType() == m_FilterType));

                    //tag filtering only for TPT tiles.
                    if (m_FilterTag != ReservedTag)
                    {
                        m_CurrentTileList.RemoveAll(tb =>
                                                    {
                                                        var tpb = tb as TilePlusBase;
                                                        if (tpb == null)
                                                            return true; //if a tile's somehow null then filter it out.
                                                        (var num, var tags) = tpb.TrimmedTags;
                                                        if (num == 0)
                                                            return true; //if no tag AND we're using tag filtering then remove this tile from the list.
                                                        for (var i = 0; i < num; i++)
                                                        {
                                                            var tag = tags[i];
                                                            if (string.IsNullOrWhiteSpace(tag))
                                                                continue;
                                                            if (m_FilterTag == tag)
                                                                return false; //tag match: keep this tile
                                                        }

                                                        return true; //no match, remove tile
                                                    });
                    }
                }
            }

            //if any tag filter at all, Unity tiles get filtered out.
            //usingPlugin is true when the type is derived from TileBase instead of Tile. This allows for Rule tiles,
            //which are NOT subclasses of Tile. 
            if (m_FilterTag == ReservedTag && (wildcard || usingPlugin || m_FilterType == typeof(Tile) )  ) 
            {
                //unity tiles: 
                var numTileAssets = target.GetUsedTilesCount();
                var arr           = new TileBase[numTileAssets];
                var end = limit < numTileAssets
                              ? limit
                              : numTileAssets;
                target.GetUsedTilesNonAlloc(arr);

                if (usingPlugin)
                {
                    for (var i = 0; i < end; i++)
                    {
                        var t = arr[i];
                        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                        if(t == null)
                            continue;
                        
                        if (t.GetType() == m_FilterType)
                            m_CurrentTileList.Add(t);
                    }
                }

                else if (wildcard) //ie no Type filtering 
                {
                    for (var i = 0; i < end; i++)
                    {
                        var t = arr[i];
                        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                        if(t == null)
                            continue;
                        
                        // ReSharper disable once Unity.NoNullPatternMatching
                        if (t is not ITilePlus) //don't want to add these twice
                            m_CurrentTileList.Add(t);
                    }
                }
                else
                {
                    m_CurrentTileList.AddRange(arr.Where(t => t != null
                                                              // ReSharper disable once Unity.NoNullPatternMatching
                                                              && t is not TilePlusBase
                                                              && t.GetType() == m_FilterType));
                }
            }
            if (TilePlusPainterConfig.TpPainterTileSorting != TpTileSorting.None)
                SortTileList();
            
            TilemapPaintTargetCount = m_CurrentTileList.Count;

            //now we try to get a selection. BUT we do not want to do that when in the Paint phase of
            //a MOVE operation.
            if (TpPainterState.TpPainterMoveSequenceState == TpPainterMoveSequenceStates.Paint)
            {
                AssetViewer?.SetTilesListViewSelection(-1);
                return;
            }
            
            var currentSelection = AssetViewer?.SelectedTileObject as TileBase;
            //if the currentSelectedObject can be found in the currentTilesList then use that as
            //the selection index
            if (currentSelection != null)
            {
                for (var ti = 0; ti < m_CurrentTileList.Count; ti++)
                {
                    if (m_CurrentTileList[ti] != currentSelection)
                        continue;
                    //else we found a match so use that
                    AssetViewer?.RebuildEditModeTilesListView();
                    AssetViewer?.SetTilesListViewSelection(ti);
                    return;
                }
            }
            
            AssetViewer?.RebuildEditModeTilesListView();

            var count = m_CurrentTileList.Count;
            if (previousCount != count)
                AssetViewer?.SetTilesListViewSelection(-1);
            else if (count != 0)
                AssetViewer?.SetTilesListViewSelection(0);

            void SortTileList()
            {

                if (TilePlusPainterConfig.TpPainterTileSorting == TpTileSorting.Type)
                {
                    m_CurrentTileList.Sort((t0, t1) =>
                                           {
                                               if (t0 == null || t1 == null)
                                                   return 0;
                                               var type0 = t0.GetType();
                                               var type1 = t1.GetType();
                                               return type0 == type1
                                                          ? 0
                                                          : StringComparer.InvariantCulture.Compare(type0.ToString(), type1.ToString());
                                           });
                }
                else if (TilePlusPainterConfig.TpPainterTileSorting == TpTileSorting.Id)
                {
                    m_CurrentTileList.Sort((t0, t1) =>
                                           {
                                               if (t0 == null || t1 == null)
                                                   return 0;
                                               var id0 = t0.GetInstanceID();
                                               var id1 = t1.GetInstanceID();
                                               return id0 == id1 ? 0 : id0 < id1 ? -1 : 1;

                                           });
                }
            }
        }

        #endregion

        #region localSelection

        /// <summary>
        /// Attempt to restore a tilemap selection.
        /// </summary>
        /// <param name="selectedMapId">The selected map identifier.</param>
        /// <param name = "notify" >notify or not when selection is changed</param>
        /// <returns>true for sucess</returns>
        private bool TryRestoreTilemapSelection(int selectedMapId, bool notify = false)
        {
            //look in the itemSource so see if we can find the same tilemap.
            var items    = tpPainterTilemapsPanel.DataSource;
            var numItems = items.Count;
            if (numItems == 0)
            {
                tabBar.ActivateToolbarButton(TpPainterTool.None, true); //added notify 12/9
                EditorClearSelection();
                return false;
            }

            var selectionIndex = -1;
            for (var i = 0; i < numItems; i++)
            {
                var item = items[i];
                if (item is not TilemapData ttd) //unlikely to fail, but need the cast to TilemapData anyway
                    continue;
                if (ttd.TargetMap.GetInstanceID() != selectedMapId) //if this map doesn't match the map in the selection 
                    continue;
                selectionIndex = i;
                break;

            }

            if (selectionIndex >= 0)
            {
                if (items[selectionIndex] is not TilemapData tilemapTarget)
                    return false;
                //is this selected tilemap the same as the current tilemapPaintTarget? If it is, don't call SetInspectorTarget because of side effects.
                if (TpPainterState.PaintableMap is not null && TpPainterState.PaintableMap.Valid && TpPainterState.PaintableMap.TargetTilemap != null
                    && tilemapTarget.TargetMap.GetInstanceID() != TpPainterState.PaintableMap.TargetTilemap.GetInstanceID()) 
                {
                    if(SetPaintTarget(tilemapTarget.TargetMap) && TpPainterState.InEditMode)
                        SetEditModeInspectorTarget(tilemapTarget.TargetMap);

                }
                else
                {
                    if (TpPainterState.InEditMode)
                        SetEditModeInspectorTarget(tilemapTarget.TargetMap);
                }
                if(notify)
                    tpPainterTilemapsPanel.SetSelection(selectionIndex);
                else
                    tpPainterTilemapsPanel.SetSelectionWithoutNotify(selectionIndex);
                return true;
            }
            else
            {
                EditorClearSelection();
                tabBar.ActivateToolbarButton(TpPainterTool.None, true); //added notify 12/9
                return false;
            }
        }

        /// <summary>
        /// Clear selection.
        /// </summary>
        private void EditorClearSelection()
        {
            if(Informational)
                TpLog("Editor cleared selection");
            if(TpPainterState.TpPainterMoveSequenceState != TpPainterMoveSequenceStates.Paint)
                TpPainterState.TpPainterMoveSequenceState = TpPainterMoveSequenceStates.None; 

            TpPainterState.PaintableMap              = null;
            SetTilemapSelectionLabel();

            m_EditorSelectionLock = true;
            tpPainterTilemapsPanel?.ClearSelection();
            m_EditorSelectionLock = false;
            
            //locking-hack needed since Unity's clearselection will
            //re-call the OnEditorSelectionChange delegate. No way to do this w/o notification.

            m_CurrentTileList.Clear();
            AssetViewer?.RebuildEditModeTilesListView();
            ClearClipboard();
            m_TpPainterContentPanel.SetAssetViewSelectionLabel(EmptyFieldLabel);
        }

        /// <summary>
        /// Resets the selections of this window.
        /// </summary>
        internal void ResetSelections(bool alsoClearTilemapsSelection = true)
        {
            if(alsoClearTilemapsSelection)
                TpPainterState.PaintableMap = null;
            
            switch (TpPainterState.GlobalMode)
            {
                case GlobalMode.PaintingView:
                    TpPainterState.PaintableObject = null;  
                    TpPainterScanners.instance.ObjectsToInspect.Clear();
                    break;
                case GlobalMode.EditingView:
                    TpPainterState.Clipboard = null;
                    TpPainterScanners.instance.ObjectsToInspect.Clear();
                    break;
                default:
                case GlobalMode.GridSelView:
                    break;
            }
        }
        

        /// <summary>
        /// Clears the clipboard.
        /// </summary>
        /// <param name="clearPaletteTileTarget">clear tileTarget if true.</param>
        internal void ClearClipboard(bool clearPaletteTileTarget = true)
        {
            if(Informational)
                TpLog($"Painter: {(clearPaletteTileTarget?"Clearing clipboard & resetting clipbd image":"resetting clipbd image")}");
            if (clearPaletteTileTarget)
            {
                TpPainterState.Clipboard = null;
                if(TpPainterState.InEditMode)
                    #pragma warning disable CS8602 // Dereference of a possibly null reference.
                    m_TpPainterContentPanel.m_TpPainterSelectionInspector.RepaintGui();
                #pragma warning restore CS8602 // Dereference of a possibly null reference.
                    
            }
            else
                SetEmptyTabBar();
        }

        /// <summary>
        /// Resets the tab bar's clipboard icons and thumbnails.
        /// </summary>
        internal void SetEmptyTabBar()
        {
            tabBar.SetEmptyTabBar();
            tabBar.TabBarTransformModified(false, null);
        }
        
        #endregion
        
        #region pool
        /// <summary>
        /// Pool for CloningData
        /// </summary>
        private readonly ObjectPool<DbChangeArgs> dbChangeArgsPool = new(() => new DbChangeArgs(),
                                                                                null,
                                                                                cd => cd.Reset());

        /// <summary>
        /// For Stats viewing
        /// </summary>
        public string PainterPoolStat => $"All:{dbChangeArgsPool.CountAll.ToString()}, Active:{dbChangeArgsPool.CountActive.ToString()}, Inactive:{dbChangeArgsPool.CountInactive.ToString()}";

        #endregion
        

        #region utils

        internal void SelectBrushInspectorTarget(TpPainterClipboard? clipboardObject, bool changeTool = true, bool ignoreIdentical = false)
        {
            BrushInspector?.SelectBrushInspectorTarget(clipboardObject, changeTool, ignoreIdentical);
        }
        
        
        /// <summary>
        /// Deselect anything on the Grid Select panel
        /// </summary>
        internal void DeselectGridSelPanel()
        {
            if(!guiInitialized)
                return;
            GridSelectionPanel?.Deselect();
        }

        /// <summary>
        /// Does a specific tool have a tilemap effect?
        /// </summary>
        /// <param name="tool">a TpPainterTool enum value</param>
        /// <returns>true if tool has an effect on tilemaps.</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        internal bool DoesToolHaveTilemapEffect(TpPainterTool tool)
        {
            return tool != TpPainterTool.None
                   && tool != TpPainterTool.Help
                   && tool != TpPainterTool.Settings;
        }

        //this is called when adding a grid sel from TpPainterSceneView after ALT+5 dragging
        internal void AddGridSelection(BoundsInt bounds, bool silent = false)
        {
            GridSelectionPanel?.AddGridSelection(bounds, silent);
        }
        
        /// <summary>
        /// Rebuild the palettes list view
        /// </summary>
        private void RebuildPalettesListView()
        {
            AssetViewer?.RefreshPalettesListView();
            AssetViewer?.SetSelectionLabelText(TilePlusPainterWindow.EmptyFieldLabel);
            BrushInspector?.ClearBrushInspectorSelection();
        }
        
        /// <summary>
        /// Adds tiles to the to Favorites and refreshes views.
        /// </summary>
        /// <param name="objects">The objects to add.</param>
        /// <remarks>only TileBase and GameObjects accepted. Ensure that GameObjects
        /// are part of prefabs, that's not checked here.</remarks>
        internal void AddToFavorites(Object[]? objects)
        {
            if(objects==null)
                return;

            var fail = false;
            foreach (var obj in objects)
            {
                // ReSharper disable once Unity.NoNullPatternMatching
                if (obj != null && obj is not TilePlusBase { IsClone: true })
                    continue;
                fail = true;
                break;
            }

            if (fail)
            {
                ShowNotification(new GUIContent("Cannot add Cloned tiles to Favorites!!\nOperation cancelled; nothing was added."));
                return;
            }
            
            TilePlusPainterFavorites.AddToFavorites(objects);


        }

        /// <summary>
        /// Update the palette view: for use after adding to favorites above and via shortcut.
        /// </summary>
        internal void UpdatePaletteView()
        {
            AssetViewer?.RefreshPalettesListView();
            s_PainterWindow!.PaintModeUpdateAssetsList();
            BrushInspector?.RefreshBrushInspectorListView();
        }

        /// <summary>
        /// Forces a refresh of the tiles list.
        /// Need as a method since the underlying bool variable
        /// needs to stay as private and this is called within lambdas.
        /// 
        /// </summary> 
        internal void ForceRefreshTilesList()
        {
            wantsRefreshTilesList = true;
        }

        /// <summary>
        /// Used when Picking in Scene View.
        /// </summary>
        /// <param name="tile">a single tile</param>
        /// <param name="pickType">what type of pick from enum TpPickedTileType</param>
        /// <param name="tPosition">grid position</param>
        /// <param name="map">parent tilemap.</param>
        /// <remarks>ALWAYS sends to Tab Bar and always marks the Clipboard item as Picked.</remarks>
        internal void SetTileTarget(TileBase tile, TpPickedTileType pickType, Vector3Int tPosition, Tilemap map)
        {
            var cb = new TpPainterClipboard(tile, tPosition, map, true);
            TpPainterState.Clipboard = cb; 
            tabBar.SetPickedObject(cb); 
            if(TpPainterState.InEditMode && SelectionInspector != null)
                SelectionInspector.RepaintGui();
        }

        /// <summary>
        /// Set the label on the leftmost column (tilemap list)
        /// </summary>
        /// <param name="text">string to use. If omitted, use EmptyFieldLabel</param>
        private void SetTilemapSelectionLabel(string? text = EmptyFieldLabel)
        {
            tpPainterTilemapsPanel.SetSelectionLabel(text);
        }

        /// <summary>
        /// Activates a toolbar button.
        /// </summary>
        /// <param name="tool">A value from the TpPainterTool enumeration.</param>
        /// <param name="withNotify">true if the toolbar button should 'notify' of the activation.</param>
        internal void ActivateToolbarButton(TpPainterTool tool, bool withNotify)
        {
            tabBar.ActivateToolbarButton(tool, withNotify);
        }


        /// <summary>
        /// If the tool is NOT TilePlusPainterTool AND
        /// the selection is a Tilemap AND
        /// the Painter tool isn't 'NONE' then force a change to the Painter Tool in Editor's ToolManager, after a delay.
        /// </summary>
        /// <param name="ignoreState">set TRUE to ignore current state (active tool type and current painter tool settings)</param>
        /// <remarks>This method automatically adds a 200 msec delay. That eliminates the possibility of a race-condition
        /// error when calling this method from an event handler</remarks>
        internal void ForcePainterTool(bool ignoreState = false)
        {
            if (!ignoreState
                && (ToolManager.activeToolType == typeof(TilePlusPainterTool) || !TpPainterState.CurrentToolHasTilemapEffect))
                return;
            
            TpLib.DelayedCallback(this,DelayedForcer,"T+P:DelayedForceTool",200);
            return;

            void DelayedForcer()
            {
                (var selectionIsMap, _) = TpLibEditor.SelectionIsTilemap;
                if (!selectionIsMap)
                    return;
                try
                {
                    ToolManager.SetActiveTool(typeof(TilePlusPainterTool));
                }
                catch (Exception e)
                {
                    var selObj = Selection.activeObject;
                    // ReSharper disable once Unity.NoNullPatternMatching
                    var mapInfo = selObj != null && selObj is GameObject go 
                                                 && go.TryGetComponent<Tilemap>(out var tmap)
                                      ? tmap.name
                                      : "none";

                    var info = selObj == null
                                   ? "null"
                                   : selObj.ToString();
                    if(Errors)
                        TpLib.TpLogError($"{e} [EXTRA INFO: SELECTION = {info} map?:{mapInfo}]");
                }
            }
        }
        
        /// <summary>
        /// Checks prior to executing CreateGui
        /// </summary>
        /// <returns>bool.</returns>
        private bool CreateGuiChecks()
        {
            if (!m_HasControlId) //failure if this is missing.
            {
                rootVisualElement.Clear();
                rootVisualElement.Add(new Label("Fatal error: could not obtain control ID"));
                return false;
            }

            //this can happen if you open Unity and switch to a different PC/Mac/Linux app while Unity is loading,
            //Or if you're mucking about in your code editor and a domain reload occurs.
            if (!TpLibIsInitialized ) 
            {
                //note cannot use the conditional version here since that one can time out.
                TpLib.DelayedCallback(this, CreateGUI, "T+P:CreateGuiWaitOnTpLib", 200,true); 
                rootVisualElement.Clear();
                rootVisualElement.Add(new Label($"Not ready yet...  {++m_CreateGuiIterations}" ));
                return false;
            }

            if (UpdatingAllowed)
            {
                TpPainterScanners.instance.TilemapsScan();
                TpPainterScanners.instance.PalettesScan(string.Empty);
            }

            return true;
        }

       
        /// <summary>
        /// Reinitialize this window.
        /// </summary>
        internal void ReInit()
        {
            if(Informational)
                TpLog("Reinitializing Tile+Painter...");
            TpPreviewUtility.ResetPlugins();
            TpPreviewUtility.ResetPreviews();
            SceneView.ResetState();
            TpPainterScanners.instance.ResetTilemapScanData();
            TpPainterScanners.instance.ResetPaletteScanData();
            TpPainterState.instance.ResetInstance();
            BrushInspector?.Release(); //release GridPaintingState's PaintableSceneViewGrid. Does nothing Pre-Unity6
            
            rootVisualElement.Clear();
            rootVisualElement.Add(new Label("Wait...."));
            guiInitialized                            = false;
            versionWasShownOnce                       = false;
            versionShownOnceCallbackActivated         = false;
            wantsRefreshTilesList                     = false;
            m_EditorSelectionLock                     = false;
            m_CurrentPaletteSearchString              = string.Empty;
            TpPainterState.TpPainterMoveSequenceState = TpPainterMoveSequenceStates.None;
            m_ToolActivated                           = false;
            updateOnSceneChange                       = false;
            settingsChangeWatchers.Clear();
            m_FilterType      = typeof(TileBase);
            m_FilterTag       = ReservedTag;
            refreshTypeFilter = false;
            m_CurrentTileList.Clear();
            m_CreateGuiIterations   = 0;
            tabBar                  = null;
            tpPainterTilemapsPanel  = null;
            m_TpPainterContentPanel = null;
            tpPainterSettingsPanel  = null;
            tpPainterHelpPanel      = null;
            tilemapsAndContentPanel = null;
            statusAreaMiniButtons   = null;
            statusLabel             = null;
            presetsButton           = null;
            mainSplitView           = null;
            lastTilemapSelectionIndex                = -1;
            lastEditModeSelectionIndex               = -1;
            lastPaintModeInspectorListSelectionIndex = -1;
            m_FirstDockCheck                         = false;
            dbChangeArgsPool.Clear();
            
            TpLib.DelayedCallback(this,DelayedReinitSequence,"T+P: Reinit",50); 
        }

        private void DelayedReinitSequence()
        {
            CreateGUI();
            ConditionalResetState();  //pushes another delayed, but conditional action.
        }
        
        #endregion
    }
}

