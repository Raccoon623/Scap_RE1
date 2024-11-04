// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-28-2022
// ***********************************************************************
// <copyright file="TpLibEditor.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>TilePlus Toolkit Library for Editor</summary>
// ***********************************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using static TilePlus.TpLib;
#nullable enable

namespace TilePlus.Editor
{
    /// <summary>
    /// This part of TpLib resides in the Editor assembly.
    /// Nothing in here is available in a build.
    /// </summary>
    [InitializeOnLoad]
    public static class TpLibEditor
    {
        #region privateFields

        private static bool          s_Initialized;
        private static MethodInfo?   s_IsLoadingAssetPreviewMi;
        private static MethodInfo?   s_HasAssetPreviewMi;
        private static bool          s_CouldNotFindAssetPreviewLoading;
        private static MethodInfo[]? s_AssetPreviewMethods;
        private static MethodInfo[]? s_GuiUtilityMethods;
        private static MethodInfo[]? s_GridEditorUtilityMethods;
        private static bool          s_PrefabStageDirtied;
        private static int           s_UntimedMarqueeLastCount;
        #endregion
        
        
        
        #region init
        //affected by InitializeOnLoad attribute
        static TpLibEditor()
        {
            
            TpEditorBridge.TilemapMarquee       = TilemapMarquee;
            TpEditorBridge.UntimedMarqueeActive = IsUntimedMarqueeActive;
            TpEditorBridge.TimedMarqueeActive   = TimedMarqueeActive;
            TpEditorBridge.FocusSceneCamera     = FocusOnTile;
            TpEditorBridge.SetMessageTypes(Informational, Warnings, Errors);

            TpEditorBridge.DomainPrefs = DomainPrefs;
            //Debug.Log($"TpLibEditor static constructor\n Editor Domain Prefs: {DomainPrefs}");

            Initialize();
        }

        /// <summary>
        /// Obtain live Domain preferences info
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public static EditorDomainPrefs DomainPrefs 
        {
            get
            {
                var opts = EditorSettings.enterPlayModeOptions;
                return new EditorDomainPrefs(EditorSettings.enterPlayModeOptionsEnabled,
                                             (opts & EnterPlayModeOptions.DisableDomainReload) == EnterPlayModeOptions.DisableDomainReload,
                                             (opts & EnterPlayModeOptions.DisableSceneReload) == EnterPlayModeOptions.DisableSceneReload);
            }
        }

        //this method is invoked when the Editor begins play mode
        /// due to the InitializeOnEnterPlayMode attribute.
        [InitializeOnEnterPlayMode] 
        private static void ResetOnEnterPlayMode(EnterPlayModeOptions playModeOptions)
        {
            if ((playModeOptions & EnterPlayModeOptions.DisableDomainReload) == 0)
                return;
            s_TimedMarquees.Clear();
            s_MarqueeId          = 1;
            s_PrefabStageDirtied = false;
        }


        /// <summary>
        /// Called by the static constructor or attribute-tagged
        /// methods such as ResetOnEnterPlayMode
        /// </summary>
        private static void Initialize()
        {
            FindGridEditorUtility();
            FindAssetPreviewMethods();
            FindGuiUtilityMethods();
            s_TimedMarquees.Clear();
            s_MarqueeId = 1;
            s_PrefabStageDirtied = false;
            
            if (s_Initialized) 
            {
                if(Informational)
                    TpLog("TpLibEditor: unregistering callbacks (not an error)");
                
                //clear the event
                if (!Application.isPlaying)
                {
                    EditorSceneManager.sceneClosing            -= EditorSceneManagerOnSceneClosed;
                    PrefabUtility.prefabInstanceUpdated        -= OnPrefabInstanceUpdated;
                    PrefabStage.prefabStageOpened              -= OnPrefabStageOpened;
                    PrefabStage.prefabStageClosing             -= OnPrefabStageClosing;
                    PrefabStage.prefabStageDirtied             -= OnprefabStageDirtied;
                    EditorApplication.hierarchyWindowItemOnGUI -= OnHeirarchyOnGUI;
                    Selection.selectionChanged                 -= OnSelectionChanged;
                    GridSelection.gridSelectionChanged         -= OnSelectionChanged;
                    Undo.undoRedoEvent                         -= OnUndoRedo;
                    SceneView.duringSceneGui                   -= OnSceneViewSceneGui;

                }
                EditorApplication.playModeStateChanged -= OnEditorPlayModeStateChanged;
            }
            
            s_Initialized = true;
            if (!Application.isPlaying)
            {
                EditorSceneManager.sceneClosing            += EditorSceneManagerOnSceneClosed;
                PrefabUtility.prefabInstanceUpdated        += OnPrefabInstanceUpdated;
                PrefabStage.prefabStageOpened              += OnPrefabStageOpened;
                PrefabStage.prefabStageClosing             += OnPrefabStageClosing;
                PrefabStage.prefabStageDirtied             += OnprefabStageDirtied;
                EditorApplication.hierarchyWindowItemOnGUI += OnHeirarchyOnGUI;
                Selection.selectionChanged                 += OnSelectionChanged;
                GridSelection.gridSelectionChanged         += OnSelectionChanged;
                Undo.undoRedoEvent                         += OnUndoRedo;

                SceneView.duringSceneGui                   += OnSceneViewSceneGui;
                
            }
            EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;
            
        }

        private static void OnprefabStageDirtied(PrefabStage obj)
        {
            s_PrefabStageDirtied = true;
        }

        #endregion
        
        #region properties
        
        
        /// <summary>
        /// Corresponds to the config setting for Informational messages
        /// </summary>
        public static bool Informational =>
            TilePlusConfig.instance == null || TilePlusConfig.instance.InformationalMessages;

        /// <summary>
        /// Corresponds to the config setting for warning messages
        /// </summary>
        public static bool Warnings => TilePlusConfig.instance == null || TilePlusConfig.instance.WarningMessages;

        /// <summary>
        /// Corresponds to the config setting for Error messages
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public static bool Errors => TilePlusConfig.instance == null || TilePlusConfig.instance.ErrorMessages;

        /// <summary>
        /// Get a string with Name, Version, Build Timestamp
        /// </summary>
        
        
        /// <summary>
        /// Color of Gizmos for Marquee and Line drawing
        /// </summary>
        public static Color GizmoColor => TilePlusConfig.instance == null ? Color.yellow : TilePlusConfig.instance.GizmoColor;

        /// <summary>
        /// Corresponds to the config setting for allowing prefab editing.
        /// </summary>
        private static bool PrefabEditingAllowed => TilePlusConfig.instance != null && TilePlusConfig.instance.AllowPrefabEditing; 
        
        private static bool s_PrefabBuildingActive;

        /// <summary>
        /// True if prefabs are being built by the Packer. Stops zillions of TpLib.OnTpLibChanged events.
        /// </summary>
        public static bool PrefabBuildingActive
        {
            get => s_PrefabBuildingActive;
            set
            {
                s_PrefabBuildingActive     = value;
                TpLib.InhibitOnTpLibChanged = value;
            }
        }

        /// <summary>
        /// Get a tuple with # persistent & timed marquees.
        /// </summary>
        internal static (int persistent, int timed) MarqueeStatus => (s_CurrentMarquees.Count, s_TimedMarquees.Count);

        #endregion
        
        #region GuiUtilityAccess

        private static void FindGuiUtilityMethods()
        {
            if (s_GuiUtilityMethods != null)
            {
                if(Informational)
                    TpLog("TpLibEditor: Skipping search for GuiUtility methods, already found");    
                return;
            }

            s_GuiUtilityMethods = null;
            
            var guiUtilAssembly = typeof(GUIUtility).Assembly;
            var typeInfo        = guiUtilAssembly.DefinedTypes.FirstOrDefault(ti => ti.Name == "GUIUtility");
            
            if (typeInfo == null)
            {
                TpLogWarning("TpLibEditor: Could not find GUIUtility");
                return;
            }

            s_GuiUtilityMethods = typeInfo.DeclaredMethods.ToArray();
            if(Informational)
                TpLog($"TpLibEditor: Found {s_GuiUtilityMethods.Length.ToString()} GUIUtility methods");
        }

        internal static (bool valid, int id) GetPermanentControlId()
        {
            var mi = s_GuiUtilityMethods?.FirstOrDefault(dm => dm.Name == "GetPermanentControlID");
            if (mi == null)
            {
                TpLogError("TpLibEditor: Could not get GUIUtility.GetPermanentControlId. ");
                return (false, 0);
            }

            return (true, (int) mi.Invoke(null,null));
        }
        
        #endregion
        
        
        #region AssetPreviewUtil

        /*
         * Use reflection to access internal methods in AssetPreview
         */
        private static void FindAssetPreviewMethods()
        {
            if (s_AssetPreviewMethods != null)
            {
                if(Informational)
                    TpLog("TpLibEditor: Skipping search for AssetPreview methods, already found");    
                return;
            }
            s_AssetPreviewMethods = null;
            var assetPreviewAssembly = typeof(AssetPreview).Assembly;

            var typeInfo = assetPreviewAssembly.DefinedTypes.FirstOrDefault(ti => ti.Name == "AssetPreview");
            if (typeInfo == null)
            {
                TpLogWarning("TpLibEditor:: Could not find AssetPreview");
                return;
            }

            s_AssetPreviewMethods = typeInfo.DeclaredMethods.ToArray(); 
            if(Informational)
                TpLog($"TpLibEditor: Found {s_AssetPreviewMethods.Length.ToString()} AssetPreview methods");
        }

       
        
        
        private static MethodInfo? GetAssetPreviewMethodInfo(string methodName, int numParams = 0)
        {
            
            return s_AssetPreviewMethods?.FirstOrDefault(dm => dm.Name == methodName && dm.GetParameters().Length == numParams);
        }
        
        /// <summary>
        /// Is an asset preview being loaded for a window?
        /// </summary>
        /// <param name="clientId">The instance ID of the window</param>
        /// <returns>true if any asset previews being loaded.</returns>
        /// <remarks>uses reflection into Unity's core module.</remarks>
        public static bool Tp_IsLoadingAssetPreviews(int clientId)
        {
        
            if (s_IsLoadingAssetPreviewMi == null)
            {
                s_IsLoadingAssetPreviewMi = GetAssetPreviewMethodInfo("IsLoadingAssetPreview",1);
                if (s_IsLoadingAssetPreviewMi == null)
                {
                    if (s_CouldNotFindAssetPreviewLoading)
                        return AssetPreview.IsLoadingAssetPreviews();
                    s_CouldNotFindAssetPreviewLoading = true;
                    if (Informational)
                        TpLogError("TpLibEditor: Could not find IsLoadingAssetPreview(clientId)");

                    return AssetPreview.IsLoadingAssetPreviews();
                }
            }
            var result = s_IsLoadingAssetPreviewMi.Invoke(null, new object[] {clientId});
            return (result is true);
        }
        
        /// <summary>
        /// Is there an existing asset preview?
        /// </summary>
        /// <param name="instanceId">Instance Id of object</param>
        /// <param name="clientId">client Id (usu 0)</param>
        /// <returns></returns>
        public static bool Tp_HasAssetPreview(int instanceId, int clientId=0)
        {
            if (s_HasAssetPreviewMi == null)
            {
                s_HasAssetPreviewMi = GetAssetPreviewMethodInfo("HasAssetPreview",2);
                if (s_HasAssetPreviewMi == null)
                {
                    if (Informational)
                        TpLogError("TpLibEditor: Could not find IsLoadingAssetPreview(clientId)");
                    return false;
                }
            }
            var result = s_HasAssetPreviewMi.Invoke(null, new object[] {instanceId, clientId});
            return (result is true);
        }
        
        #endregion
        
        #region GridEdUtil
        
        /*Use reflection to access methods in the internal static class
         *GridEditorUtility. Namespace = UnityEditor.Tilemaps
         * and GridPalettes (Same assembly) for palette utilities
         */
        

        //During init this is used to get the methods for this class
        private static void FindGridEditorUtility()
        {
            if(s_GridEditorUtilityMethods != null)
            {
                if(Informational)
                    TpLog("TpLibEditor: Skipping search for GEU methods, already found");
                return;
    
            }
            s_GridEditorUtilityMethods = null;
 
            
            var editorAssembly = typeof(GridBrushEditorBase).Assembly;
            
            var typeInfo       = editorAssembly.DefinedTypes.FirstOrDefault(ti=>ti.Name.Contains("GridEditorUtility"));
            if (typeInfo == null)
            {
                TpLogError("TpLibEditor: Could not find GEU!");
                return;
            }
            
            s_GridEditorUtilityMethods = typeInfo.DeclaredMethods.ToArray(); 
            if(Informational)
                TpLog($"TpLibEditor: Found {s_GridEditorUtilityMethods.Length.ToString()} GEU methods");

            
            typeInfo = editorAssembly.DefinedTypes.FirstOrDefault(ti => ti.Name.Contains("GridPalettes"));
            if (typeInfo == null)
            {
                TpLogError("TpLibEditor: Could not find GridPalettes!");
                return;
            }

            var gridPaletteMethods          = typeInfo.DeclaredMethods.ToArray();
            s_GridPalettesRefreshCacheMethodInfo = gridPaletteMethods.FirstOrDefault(method => method.Name == "CleanCache");
        }


        private static MethodInfo? s_GridPalettesRefreshCacheMethodInfo;
        /// <summary>
        /// Custom for Tile+Painter use. 
        /// </summary>
        public static void ResetGridPalettesCache()
        {
            s_GridPalettesRefreshCacheMethodInfo?.Invoke(s_GridPalettesRefreshCacheMethodInfo,null); 
        }

        /// <summary>
        /// Get a named GridEditorUtility methodInfo.
        /// </summary>
        /// <param name="methodName">the name of a method</param>
        /// <param name = "numParams" >optional # of params for methods with parameters</param>
        /// <returns>MethodInfo or null for not found</returns>
        /// <remarks>Cache the result if used often</remarks>
        public static MethodInfo? GetGeuMethodInfo(string methodName, int numParams = -1) 
        {
            if(numParams == -1)
                return s_GridEditorUtilityMethods?.FirstOrDefault(dm => dm.Name == methodName);
            var methodInfos = s_GridEditorUtilityMethods?.Where(dm => dm.Name == methodName);
            if (methodInfos == null)
                return null;
            foreach (var mi in methodInfos)
            {
                if (mi.GetParameters().Length == numParams)
                    return mi;
            }

            return null;
        }

        
        
        //settings for marquee. grid, and line.
        private static readonly List<MarqueeSettings> s_CurrentMarquees = new(4);

        private static readonly List<MarqueeSettings> s_TimedMarquees = new(4);

        private static GridLineSettings? s_LineSpec;
        
        
        
        //cached method-info instances
        private static MethodInfo? s_DrawGridMarqueeMi;
        private static MethodInfo? s_DrawGridLineMi;

        private static ulong s_MarqueeId = 1; 
        
        //cached SceneViewHandler instance
        private static SceneViewHandler? s_SceneViewHandler;

        /// <summary>
        /// Draw a marquee at a tile location. Ignored in PLAY mode
        /// </summary>
        /// <param name="layout">The grid layout</param>
        /// <param name="boundsInt">Describes the size of the area to draw</param>
        /// <param name="color">the color. If clear, then GizmoColor is used.</param>
        /// <param name="timeout">how long the line should appear. Negative timeouts are changed to positive timeouts, and will also work in Play (others won't)</param>
        /// <param name="persistent">When timeout=0 if this is true then the marquee is persistent</param>
        /// <param name="monitoredGuid"> (nullable) TilePlus System.Guid to monitor. </param>
        ///  <returns>a unique identifier for this request.</returns>
        /// <remarks> if using this followed by TilemapLine, do this first. Note that when timeout>0 a task is created.
        ///</remarks>
        public static ulong TilemapMarquee(GridLayout layout, BoundsInt boundsInt, Color color, float timeout = 0, bool persistent = false, Guid? monitoredGuid = null)
        {
            if (Application.isPlaying && timeout >= 0)
                return 0;
            if (timeout < 0)
                timeout = -timeout;
            if (color == Color.clear)
                color = GizmoColor;
            
            //create a new marquee spec for the OnGui (OnSceneViewSceneGui)
            
            if (timeout > 0) //if not drawn for only one frame.
            {
                var id       = s_MarqueeId++;
                var settings = new MarqueeSettings(layout, boundsInt, color, id);
                s_TimedMarquees.Add(settings);
                var temp = settings; //avoids closure and possible removal of wrong marquee.
                TpLib.DelayedCallback(null,() =>
                                           {
                                               s_TimedMarquees.Remove(temp);
                                               if (s_SceneViewHandler != null
                                                   && s_LineSpec == null
                                                   && s_CurrentMarquees.Count == 0
                                                   && s_TimedMarquees.Count == 0)
                                                   s_SceneViewHandler.SetRefresh(false);
                                           },"TPLibEd:Marquee",(int)timeout * 1000, true);
                return id;
            }
            else
            {
                var id = persistent ? s_MarqueeId++ : 0;
                var settings = new MarqueeSettings(layout, boundsInt, color, id)
                               {
                                   m_Persistent = persistent,
                                   m_Guid       =  monitoredGuid
                               };
                s_CurrentMarquees.Add(settings);
                return id;
            }

        }

        /// <summary>
        /// Is a particular timed marquee still active?
        /// </summary>
        /// <param name="id">the identifier for this marquee</param>
        /// <param name="kill">If the marquee is found, delete it if true.</param>
        /// <returns>true if active, id of zero always returns false</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public static bool TimedMarqueeActive(ulong id, bool kill=false)
        {
            bool Match(MarqueeSettings x) => x.m_Id == id;

            if (id == 0)
                return false;
            return s_TimedMarquees.FindIndex(Match) != -1;
        }
        
        

        /// <summary>
        /// Draw a 2D line from one position to another. WILL WORK in PLAY mode.
        /// </summary>
        /// <param name="start">starting position (much more comfortable)</param>
        /// <param name="end">ending position (may make arm hurt)</param>
        /// <param name="color">the color</param>
        /// <param name="timeout">how long the line should appear</param>
        /// <remarks> if using this TilemapMarquee by this, do TilemapMarquee first</remarks>
        public static void TilemapLine(Vector2 start, Vector2 end, Color color, float timeout = 1f)
        {
            s_LineSpec          = new GridLineSettings(start, end, color,timeout);
            if (timeout > 0)
            {
                TpLib.DelayedCallback(null,() =>
                                           {
                                               s_LineSpec          = null;
                                               if (s_SceneViewHandler != null
                                                   && s_LineSpec == null
                                                   && s_CurrentMarquees.Count == 0
                                                   && s_TimedMarquees.Count == 0)
                                                   s_SceneViewHandler.SetRefresh(false);
                                           }, "TPlEd:tilemap-line",(int)timeout * 1000);
            }
                
        }

        
        

        

        /// <summary>
        /// Is the marquee active? Note, available in TpLib as TilemapMarqueeStatus
        /// </summary>
        /// <param name="id">optional ID to use (only if the marquee had been set persistent via the function call)</param>
        /// <param name="kill">If optional ID is used AND the marquee exists THEN if this is true the marquee is deleted.</param>
        /// <returns>true if the marquee is active</returns>
        private static bool IsUntimedMarqueeActive(ulong id = 0, bool kill=false)
        {
            if(id == 0)
                return s_UntimedMarqueeLastCount != 0;
            if(!kill)
                return s_CurrentMarquees.Any(x => x.m_Id == id);
            var found = s_CurrentMarquees.FirstOrDefault(x => x.m_Id == id);
            if (found == null)
                return false;
            s_CurrentMarquees.Remove(found);
            return true;

        }
        
        /// <summary>
        /// Focus scene view on a tile.
        /// </summary>
        /// <param name="position"></param>
        public static void FocusOnTile(Vector3 position)
        {
            s_DoZoom  = true;
            s_ZoomPos = position;
        }

        private static bool    s_DoZoom;
        private static Vector3 s_ZoomPos;
        /// <summary>
        /// Min zoom for toolbar focus
        /// </summary>
        public const   float   MinimumZoomValue = 1f;
        /// <summary>
        /// Max zoom for toolbar focus
        /// </summary>
        public const   float   MaxZoomValue     = 16f;
        
        //OnSceneViewGUI handler. If a marker or line spec is not null then use it to draw.
        private static void OnSceneViewSceneGui(SceneView sceneView)
        {
            
            if (Event.current.type != EventType.Repaint)
                return;
            
            if (s_SceneViewHandler == null)
                s_SceneViewHandler = new SceneViewHandler();  //note this calls init and turns on sceneview refresh
            else
                s_SceneViewHandler.Init();
            
            var currentSv = SceneView.currentDrawingSceneView;

            if (s_DoZoom)
            {
                s_DoZoom = false;
                var zoom = 1f;
                if (TilePlusConfig.instance != null)
                    zoom = TilePlusConfig.instance.FocusSize;
                var cam = currentSv.camera;
                var rot = cam.transform.rotation;
                //note these two limits below also show up for the slide limits in TilePlusConfigEditor.
                if (zoom < MinimumZoomValue)
                    zoom = MinimumZoomValue;
                else if (zoom > MaxZoomValue)
                    zoom = MaxZoomValue;
                currentSv.LookAt(s_ZoomPos,rot,zoom,true);
            }
            
            var repaint = false;
            s_UntimedMarqueeLastCount = s_CurrentMarquees.Count;
            if (s_UntimedMarqueeLastCount != 0)
            {
                for(var i = 0; i < s_UntimedMarqueeLastCount; i++)
                    GridMarquee(s_CurrentMarquees[i]);
                repaint = true;
                //changed 4 Dec 23: part of fix for TpAnimZoneBase "visualize area" 
                s_CurrentMarquees.RemoveAll(x => !x.m_Persistent
                                                 || (x.m_Guid != null
                                                     && x.m_Guid != ImGuiTileEditor.CurrentTilePlusGuid));
            }

            
            var n = s_TimedMarquees.Count;
            if (n != 0)
            {
                for(var i = 0; i < n; i++)
                    GridMarquee(s_TimedMarquees[i]);
                repaint = true;
            }
            

            if (s_LineSpec != null)
            {
                GridLine(s_LineSpec);
                if (s_LineSpec.m_Timeout == 0)
                    s_LineSpec = null;
                repaint = true;
            }
            if(repaint)
                currentSv.Repaint();

            
            if(s_LineSpec == null && s_CurrentMarquees.Count == 0 && s_TimedMarquees.Count == 0)
                s_SceneViewHandler.SetRefresh(false);

            

        }
        
        //draw a grid marquee
        private static void GridMarquee(MarqueeSettings marq)
        {
            if (s_DrawGridMarqueeMi == null)
            {
                s_DrawGridMarqueeMi = GetGeuMethodInfo("DrawGridMarquee");
                if (s_DrawGridMarqueeMi == null)
                {
                    TpLogError("TpLibEditor: Could not find DrawGridMarquee method!");
                    return;
                }
            }
            s_DrawGridMarqueeMi.Invoke(null, new object[] {marq.m_GridLayout,marq.m_BoundsInt, marq.m_Color});
        }

        //draw a grid line
        private static void GridLine(GridLineSettings line)
        {
            if (s_DrawGridLineMi == null)
            {
                s_DrawGridLineMi = GetGeuMethodInfo("DrawLine");
                if (s_DrawGridLineMi == null)
                {
                    TpLog("TpLibEditor: Could not find DrawLine method!");
                    return;
                }
            }
            s_DrawGridLineMi.Invoke(null, new object[] {line.m_Start, line.m_End, line.m_Color});
           
            
        }

        
        
        #endregion
        

        #region editorStylingAndCallbacks
        private static GUIStyle? s_HeirarchyGUIStyle;

        private static GUIStyle HeirarchyGuiStyle
        {
            get
            {
                if (s_HeirarchyGUIStyle != null)
                    return s_HeirarchyGUIStyle;
                s_HeirarchyGUIStyle = new GUIStyle(EditorStyles.label) {alignment = TextAnchor.MiddleRight};
                return s_HeirarchyGUIStyle;
            }
        }

        
        private static GUIContent? s_LockIconGC;
        private static GUIContent? s_UnlockIconGC;

        
        /// <summary>
        /// paints the locked or unlocked icons next to tilemaps.
        /// </summary>
        /// <param name="instanceId"></param>
        /// <param name="selectionRect"></param>
        private static void OnHeirarchyOnGUI(int instanceId, Rect selectionRect)
        {
            if (Application.isPlaying)
                return;
            var go = (GameObject)EditorUtility.InstanceIDToObject(instanceId);
            if (go == null || !go.TryGetComponent<Tilemap>(out var map))
                return;
            if(IsTilemapLocked(map))
            {
                //cache the icon first time thru
                s_LockIconGC ??= new GUIContent(TpIconLib.FindIcon(TpIconType.LockedIcon));
                GUI.Label(selectionRect,s_LockIconGC,HeirarchyGuiStyle);
            }
            else if (IsTilemapRegistered(map))
            {
                //cache the icon first time thru
                s_UnlockIconGC ??= new GUIContent(TpIconLib.FindIcon(TpIconType.UnLockedIcon));
                GUI.Label(selectionRect,s_UnlockIconGC,HeirarchyGuiStyle);
            }
        }
        
        /// <summary>
        /// Refreshes all tiles when a prefab stage opens with tilemaps.
        /// Unity ought to do this automatically but noooooo.
        /// </summary>
        /// <param name="stage"></param>
        private static void OnPrefabStageOpened(PrefabStage stage)
        {
            s_PrefabStageDirtied = false;
            if (Application.isPlaying || stage == null)
                return;
            
            var maps = stage.FindComponentsOfType<Tilemap>();
            if(maps.Length != 0)
                TpLib.DelayedCallback(null, SceneScan, "TpLibEditor-OnPrefabStageOpening");

            var markers = stage.FindComponentsOfType<TpPrefabMarker>();
            if (markers.Length <= 0)
                return;
            if (PrefabEditingAllowed)
            {
                //TpLogWarning("TpLibEditor: WARNING: you are editing a TilePlus-created prefab. Probably a mistake!!");
                return;
            }
            TpEditorUtilities.ReturnToMainStageDelayed();
            const string msg = "!! you can't edit these prefabs !!";
            TpLogError(msg);
            EditorUtility.DisplayDialog("SORRY!", msg, "Continue");
        }

        /// <summary>
        /// invoked when a prefab stage closes.
        /// </summary>
        /// <param name="stage"></param>
        private static void OnPrefabStageClosing(PrefabStage stage)
        {
            if(PrefabEditingAllowed || !s_PrefabStageDirtied )
                return;
            s_PrefabStageDirtied = false;
            
            var maps          = stage.FindComponentsOfType<Tilemap>();
            if (maps.Length != 0)
            {
                var markers = stage.FindComponentsOfType<TpPrefabMarker>();
                if (markers.Length != 0)
                {
                    
                    DelayedCallback(null, SceneScan, "TpLibEditor-OnPrefabStageClosing");
                    const string msg = "WARNING: you edited a TilePlus-created prefab. Probably a mistake!!"; 
                    TpLog(msg);
                    EditorUtility.DisplayDialog("%$#^%$#", msg, "Continue");
                }
            }
            stage.ClearDirtiness();
        }
        
        
        /// <summary>
        /// Invoked when a scene closes
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="_"></param>
        private static void EditorSceneManagerOnSceneClosed(Scene scene, bool _)
        {
            if (EditorApplication.isCompiling)
                return;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if(Informational)
                    TpLog("TPLibEditor: ignoring Scene Closing callback from Unity Editor: play mode beginning");
                return;
            }
            if(Informational) 
                TpLog($"TpLibEditor: Scene [{scene.name}] unloaded. [EditorSceneManager.sceneClosing] ");
            
            var tilemaps = new List<Tilemap>();
            GetTilemapsInScene(scene, ref tilemaps);
            foreach (var map in tilemaps)
                RemoveMap(map);
            
        }


        /// <summary>
        /// Invoked when changing to and from Play mode in the Editor
        /// </summary>
        /// <param name="change"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private static void OnEditorPlayModeStateChanged(PlayModeStateChange change)
        {   
            if(Informational)
                TpLog($"TpLibEditor: Play mode state change: {change.ToString()}");
            
            s_TimedMarquees.Clear();
            s_MarqueeId = 1;
            
            switch (change)
            {
                case PlayModeStateChange.EnteredPlayMode:
                {
                    var tilesInSim = GetAllTilesRaw.Where(tpb => tpb
                                                                 // ReSharper disable once Unity.NoNullPatternMatching
                                                                 && tpb is ITilePlus
                                                                           {
                                                                               CanSimulate : true,
                                                                               IsSimulating: true
                                                                           });
                    foreach(var tile in tilesInSim)
                        ((ITilePlus) tile).Simulate(false);
                    break;
                }
                case PlayModeStateChange.ExitingPlayMode:
                {  
                    TpLib.CallbacksControl();
                    TpLib.MaxNumClonesPerUpdate            = MaxClonesInitialValue;
                    TpLib.MaxNumDeferredCallbacksPerUpdate = MaxDeferredCallbackInitialValue;
                    SpawningUtil.ResetPools();
                    EditorApplication.delayCall += SceneScanWithReport;
                }
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(change), change, null);
            }
        }
        
        internal static void OnSelectionChanged()
        {
            if (Application.isPlaying)
                return;
            s_CurrentMarquees.Clear();
            s_TimedMarquees.Clear();
            s_LineSpec = null;
            s_SceneViewHandler?.SetRefresh(false);
        }
        
        
        /// <summary>
        /// Invoked after exiting play mode.
        /// </summary>
        private static void SceneScanWithReport()
        {
            if (TilemapsCount == 0)
                return;
           
            SceneScan();
        }
        
        
        /// <summary>
        /// Invoked when a prefab changes
        /// </summary>
        /// <param name="go"></param>
        private static void OnPrefabInstanceUpdated(GameObject go)
        {
            if (Application.isPlaying)
                return;
            var components = go.GetComponentsInChildren<Tilemap>();
            if (components == null)
                return;

            //create a list of any cloned tiles. Shouldn't be any. Note this is a change from 1.0.0
            var cloneTiles  = GetAllTilesRaw.ToList().FindAll(tile=>tile.IsClone);
            var nCloneTiles = cloneTiles.Count;
            if (nCloneTiles == 0)
                return;
            var maps = cloneTiles.Select(tile => tile.ParentTilemap).ToList(); //ie, all maps with cloned tiles.
            
            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (var map in components)
            {
                //Tilemap inside Palette window is a prefab.
                if (map.transform.parent.gameObject.layer == TilePlusBase.PaletteTilemapLayer) 
                    continue;
                /*if (!TpLib.AnyUnlockedTiles(map))
                    continue;*/
                if(!maps.Contains(map))
                    continue;
                
                var prefabInstanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(map);
                if (prefabInstanceRoot == null)
                    continue;
                if (!PrefabEditingAllowed)
                {
                    PrefabUtility.RevertPrefabInstance(prefabInstanceRoot, InteractionMode.AutomatedAction);
                    TpEditorUtilities.PerformUndoDelayed();  //unlinks any possible linked prefab in the scene if this was a drag-drop. Avoids ruining tilemaps in source tilemaps
                    var warning = $"Undoing changes to prefab {prefabInstanceRoot.name} and if this was a drag/drop from scene->project, unlinking source scene objects.";
                    TpLog(warning);
                    EditorUtility.DisplayDialog("Warning", $"{warning}. Please read the User Guide!", "Continue");
                    return;
                }
                else
                {
                    TpLog("WARNING: you are editing a TilePlus-created prefab. Probably a mistake!!");
                    EditorUtility.DisplayDialog("Warning", "You just created/modified a prefab of a Tilemap but it had TilePlus tiles in it.\nThe prefab loses those tiles, so 'unpack completely' the in-scene Prefab then please read the User Guide!", "Continue");
                    return;
                }
            }
        }
        
        private static void OnUndoRedo(in UndoRedoInfo info)
        {
            if(Informational) 
                TpLog("TpLibEditor: Rescan tilemaps on Undo");
            //crude but necc as tilemapchanged event does not fire on Undo and there's no way to know what was undone!
            TpLib.DelayedCallback(null,TpLib.SceneScan, "TpLibEditor-OnUndoRedo");
            
        }
        
        
        #endregion

        #region utils
        
        
        
        
        /// <summary>
        /// Is this position NOT paintable? IE in a prefab or tilemap is locked
        /// </summary>
        /// <param name="tilemap">map to test</param>
        /// <returns>Tuple(wasLocked,inPrefab,inStage) all= false means map can be modified.</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public static (bool allowPrefabEditing, bool wasLocked, bool inPrefab, bool inStage) IsMapInPrefabOrLockedPrefabTilemap( Tilemap? tilemap)
        {
            if (tilemap == null)
                return (false, true, true, true);
            
            var stage      = PrefabStageUtility.GetCurrentPrefabStage();
            var objInStage = stage != null && stage.IsPartOfPrefabContents(tilemap.gameObject);

            return (TilePlusConfig.instance.AllowPrefabEditing, IsTilemapLocked(tilemap), PrefabUtility.IsPartOfAnyPrefab(tilemap.transform), objInStage);
        }
        
        /// <summary>
        /// Test for no painting on locked prefabs/tilemaps
        /// </summary>
        /// <param name="map">Tilemap</param>
        /// <returns>tuple of (true,(return from IsMapInPrefabOrLockedPrefabTilemap) if DO NOT paint</returns>
        /// <remarks>The tuple can be ignored but can be useful in UI</remarks>
        public static (bool noPaint, (bool allowPrefabEditing, bool wasLocked, bool inPrefab, bool inStage)  ) NoPaint(Tilemap? map)
        {
            (var allowPrefabEditing, var wasLocked, var inPrefab, var inStage) = IsMapInPrefabOrLockedPrefabTilemap(map);
            return (  (wasLocked || inPrefab || inStage) && (!allowPrefabEditing /*|| !inStage*/), 
                      (allowPrefabEditing, wasLocked, inPrefab, inStage));
        }
        
        
        private const char Bang = '!';
        /// <summary>
        /// Parse the supplied PaintMask into included and excluded maps
        /// </summary>
        /// <param name="paintMask">the paint mask of a tile </param>
        /// <param name="includedMaps">list of maps to include</param>
        /// <param name="excludedMaps">list of maps to exclude</param>
        public static void ParsePaintMask( List<string> paintMask,  ref List<string> includedMaps, ref List<string> excludedMaps)
        {
            var n = paintMask.Count;
            includedMaps.Clear();
            excludedMaps.Clear();
            for (var i = 0; i < n; i++)
            {
                var s = paintMask[i];
                if (string.IsNullOrWhiteSpace(s))
                    continue;
                if (s[0] == Bang)
                {
                    if (s.Length > 1)
                        excludedMaps.Add(s[1..].ToLowerInvariant());
                }
                else
                    includedMaps.Add(s.ToLowerInvariant());
            }
        }
        
        
        /// <summary>
        /// Returns true if the Selection is a tilemap.
        /// </summary>
        internal static (bool isMap, Tilemap? map) SelectionIsTilemap
        {
            get
            {
                var selection = Selection.activeGameObject;
                if (selection == null)
                    return (false, null);
                var hadMap = selection.TryGetComponent<Tilemap>(out var map);
                return (hadMap,map);
            }
        }
        #endregion
       
        
       
        
        
        #region privClass
        //these two small classes are used to maintain the settings
        // for the Marquee/Line drawing(s) which reoccur every frame
        //until they time out.
        private class MarqueeSettings
        {
            public readonly GridLayout m_GridLayout;
            public readonly BoundsInt  m_BoundsInt;
            public readonly Color      m_Color;
            public readonly ulong      m_Id;
            public          bool       m_Persistent;
            public          Guid?      m_Guid; 

            public MarqueeSettings(GridLayout layout, BoundsInt boundsInt, Color color, ulong id)
            {
                m_GridLayout = layout;
                m_BoundsInt  = boundsInt;
                m_Color      = color;
                m_Id         = id;

            }
        }

        private class GridLineSettings
        {
            public readonly Vector2 m_Start;
            public readonly Vector2 m_End;
            public readonly Color   m_Color;
            public readonly float   m_Timeout;

            public GridLineSettings(Vector2 start, Vector2 end, Color color, float timeout)
            {
                m_Start   = start;
                m_End     = end;
                m_Color   = color;
                m_Timeout = timeout;
            }
        }

        
        
        #endregion
        
        
        
    }
    /// <summary>
    /// This class is used to manage the Scene view so that
    /// the drawn lines from TilemapMarquee and TilemapLine
    /// will appear even if the Scene window (or windows)
    /// isn't the focused window. Otherwise, the marquee or
    /// line won't appear under many conditions where the
    /// mouse pointer is not over a Scene window.
    /// </summary>
    public class SceneViewHandler
    {
        private ArrayList? sceneViews;
        private int        num;

        //constructor: note that this sets
        //AlwaysRefresh on for all sceneviews
        /// <summary>
        /// Ctor
        /// </summary>
        public SceneViewHandler()
        {
            Init();
        }

        /// <summary>
        /// Reinit. Note: always turns on AlwaysRefresh
        /// for all sceneviews.
        /// </summary>
        public void Init()
        {
            sceneViews = SceneView.sceneViews;
            num        = sceneViews.Count;
            SetRefresh(true);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="on"></param>
        public void SetRefresh(bool on)
        {
            if (sceneViews == null)
                return;
            
            //ensure that there are sceneviews before indexing! 
            sceneViews = SceneView.sceneViews;
            num        = sceneViews.Count;
            
            for (var i = 0; i < num; i++)
            {
                if (sceneViews[i] is not SceneView v)
                    continue;
                v.sceneViewState.alwaysRefresh = on;
                v.Repaint();
            }
            
        }
    }
    
    
   
}
