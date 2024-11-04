// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 11-07-2022
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 01-05-2023
// ***********************************************************************
// <copyright file="TpPainterSceneView.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>

// ***********************************************************************
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;
using static TilePlus.Editor.Painter.TpPainterBulkOps;
using static TilePlus.Editor.Painter.TpPainterClipboard;
using static TilePlus.Editor.Painter.TpPainterState;
using static TilePlus.Editor.TpPreviewUtility;
using static TilePlus.TpLib;
using Object = UnityEngine.Object;

namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// Manager class used to control the Scene View window with Painter.
    /// </summary>
    internal class TpPainterSceneView : ScriptableSingleton<TpPainterSceneView>
    {
        /// <summary>
        /// Information about draglock status
        /// </summary>
        public readonly struct DragLockInfo
        {
            /// <summary>
            /// Drag Lock X
            /// </summary>
            public readonly bool m_DragX;
            /// <summary>
            /// Drag Lock Y
            /// </summary>
            public readonly bool m_DragY;
            /// <summary>
            /// Ctor
            /// </summary>
            /// <param name="x">true if X values are locked</param>
            /// <param name="y">true if Y values are locked</param>
            public DragLockInfo(bool x, bool y)
            {
                m_DragX = x;
                m_DragY = y;
            }
        }

        
        #region properties

        /// <summary>
        /// Ignore next heirarchy change. Used when painting a Prefab
        /// instead of a tile so we can ignore the heirarchy event
        /// </summary>
        public  bool IgnoreNextHierarchyChange { get; set; }

        // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
        /// <summary>
        /// Is preview currently-active
        /// </summary>
        public  bool PreviewActive => TpPreviewUtility.PreviewActive; 

        /// <summary>
        /// Is a brush operation in progress
        /// </summary>
        public  bool BrushOpInProgress => brushOpInProgress;

        

        /// <summary>
        /// Get information about the Drag Lock state
        /// </summary>
        internal  DragLockInfo DragLock => new(dragLockX, dragLockY);

        /// <summary>
        /// for diags
        /// </summary>
        internal  Vector3Int CurrentMouseGridPosition => currentMouseGridPosition;
        
        /// <summary>
        /// Get the ScreenToLocal method.
        /// </summary>
        /// <remarks>Can't be done in constructor since
        /// it's possible that TpLib editor isn't inited prior to this class</remarks>
        private  MethodInfo? ScreenToLocal
        {
            get
            {
                if (geuFuncScreenToLocalMi != null)
                    return geuFuncScreenToLocalMi;
                geuFuncScreenToLocalMi = TpLibEditor.GetGeuMethodInfo("ScreenToLocal", 2);
                return geuFuncScreenToLocalMi;
            }
        }

        /// <summary>
        /// Get the state of the allow overwrite/ignore map hotkey
        /// </summary>
        /// <value>T/F</value>
        private  bool AllowOverwriteOrIgnoreMap => TpEditorUtilities.AllowPaintingOverwrite;

        internal  (bool active, BoundsInt bounds, Vector3Int startPosition) GridSelMarqueeState =>
            (marqueeDragActiveLastPass, marqueeDragBounds, marqueeStartMousePosition);

        private  bool DragConditionsMet()
        {
            return
                InGridSelMode
                || (CurrentTool == TpPainterTool.Paint &&
                    (InPaintMode && Clipboard is
                     {
                         IsNotEmpty             : true,
                         VarietyCanBeMassPainted: true
                     }))
                || CurrentTool == TpPainterTool.Erase
                || (CurrentTool == TpPainterTool.Pick && InPaintMode)
                || CurrentTool == TpPainterTool.Move;

        }
        
        /// <summary>
        /// is preview inhibited because editor isn't in 2D mode.
        /// </summary>
        private  bool PreviewInhibited { get; set; }
        private TilePlusPainterWindow? Win                  => TilePlusPainterWindow.RawInstance;
        private Tilemap?               PaintableTilemap     => TpPainterState.PaintableMap?.TargetTilemap;
        private GridLayout?            TargetGridLayout     => TpPainterState.PaintableMap?.TargetTilemapGridLayout;

        private Transform? GridTransform => TpPainterState.PaintableMap?.ParentGridTransform; 
        private string     notificationBacking = string.Empty;
        private bool       showNotification;
        
        /// <summary>
        /// A string poked into this property will cause a notification over the scene view.
        /// Supports two modifications one immediately after the other. More than two within the
        /// notification elapsed time (TileHighlightTime config value) will be lost.
        /// </summary>
        public  string SceneViewNotification
        {
            set
            {
                //simple two-level 'stack' to allow for two notifications in a row.
                if (showNotification) //already have one, its very nice.
                {
                    var s = value;
                    TpLib.DelayedCallback(null, () =>
                                                {
                                                    SceneViewNotification = s ;
                                                },"T+P:Sceneview:notification retry",
                                          (int)(TilePlusConfig.instance.TileHighlightTime * 1000) + 500);
                    return;
                }
                notificationBacking = value;
                showNotification    = true;
            }
        }
        
        /// <summary>
        /// is the paint target valid and the paintable object valid: ie can we paint?
        /// </summary>
        internal  bool ValidPaintTargetAndPaintableObject =>
            TpPainterState.PaintableMap != null && Clipboard != null &&
            currentMouseGridPosition != TilePlusBase.ImpossibleGridPosition
            && TpPainterState.PaintableMap is { Valid    : true }
            && TpPainterState.Clipboard is { Valid: true };

        private  int ControlId => Win == null
                                            ? 0
                                            : Win.PainterWindowControlId;
        private  bool OnGrid => fabAuthoringMode && PositionAlignedWithSgrid(currentMouseGridPosition);
        
        /// <summary>
        /// Is the current action draggable?
        /// </summary>
        internal  bool Draggable { get; private set; }
        
        /// <summary>
        /// is the current action the pick phase of a move action?
        /// </summary>
        private  bool IsPickPhaseOfMove { get; set; }
        
        /// <summary>
        /// True if the move phase of a move actually had a pick
        /// </summary>
        private  bool MovePickPhaseHadPick { get; set; }
        
        private const string Unknown = "Unknown";
        private  string MapName
        {
            get
            {
                if (TpPainterState.PaintableMap == null)
                    return Unknown;
                var aname = TpPainterState.PaintableMap.Name;
                if(string.IsNullOrEmpty(aname))
                    return Unknown;
                return aname ?? Unknown;
            }
        }

       
        #endregion

        #region fields

        //for sceneview handler
        //note: the GUIStyle fields have to be initialized in Ctor. Do it right here and Unity throws an error.
        /// <summary>
        /// A preset GUI style
        /// </summary>
        private  GUIStyle? positionTextGuiStyle;
        /// <summary>
        /// A preset GUI style
        /// </summary>
        private  GUIStyle? positionTextAltGuiStyle;
        /// <summary>
        /// A preset GUI style
        /// </summary>
        private  GUIStyle? pickMsgGuiStyle;
        //these lists will contain tilemaps which are excluded from painting and included for painting.
        /// <summary>
        /// Tilemaps which are excluded
        /// </summary>
        private  List<string> excludedMaps = new();
        /// <summary>
        /// Tilemaps which are included
        /// </summary>
        private  List<string> includedMaps = new();
        /// <summary>
        /// Allowed events in SceneViewHandler
        /// </summary>
        private  readonly HashSet<EventType> allowedEventsInSceneGui = new() { EventType.Repaint, EventType.MouseDown, EventType.MouseUp, EventType.MouseDrag };
        /// <summary>
        /// part of reflection-hack to access some Unity palette methods
        /// </summary>
        private  MethodInfo? geuFuncScreenToLocalMi;
        //Paint/Erase dragging variables
        /// <summary>
        /// Is dragging in progress?
        /// </summary>
        private  bool dragClutchShortcutActive;
        /// <summary>
        /// drag lock x
        /// </summary>
        private  bool dragLockX;
        /// <summary>
        /// drag lock y
        /// </summary>
        private  bool dragLockY;
        /// <summary>
        /// Has the mouse position changed?
        /// </summary>
        private  bool positionChanged;
        /// <summary>
        /// drag lock: constant X or Y
        /// </summary>
        private  int constantXorY;
        /// <summary>
        /// Indicates that a brush operation is in progress
        /// </summary>
        private  bool brushOpInProgress; //NO serializeField on this
        /// <summary>
        /// The current mouse grid position
        /// </summary>
        private  Vector3Int currentMouseGridPosition = TilePlusBase.ImpossibleGridPosition; //the current mouse position.
        /// <summary>
        /// The last mouse grid position
        /// </summary>
        private  Vector3Int lastMouseGridPosition = TilePlusBase.ImpossibleGridPosition; //the previous mouse position
        private  Vector3       currentMouseLocalPosition = Vector3.zero;
        private  bool          currentPaintingTilemapHasOriginZero;
        private  bool          fabAuthoringMode;
        
        //This mechanism ensures that the message occurs only once until painter mode changes
        //then the message is enabled again. Avoids spamming these messages.
        private  bool          allowOverwriteMessageToSceneView = true;
        private  GlobalMode    lastGlobalMode                   = GlobalMode.PaintingView;
        private  TpPainterTool lastPainterTool                  = TpPainterTool.None;
        /// <summary>
        /// True if spot where user clicked isn't paintable
        /// </summary>
        private  bool cantPaintHereInterlock;
        /// <summary>
        /// State variable
        /// </summary>
        private  bool marqueeDragActiveLastPass;
        /// <summary>
        /// Bounds for marquee when dragged. When drag ends, becomes GridSelection.
        /// </summary>
        private  BoundsInt marqueeDragBounds;
        /// <summary>
        /// starting position for marquee dragging
        /// </summary>
        private  Vector3Int marqueeStartMousePosition;
        // ReSharper disable once NotAccessedField.Local
        private  Vector3Int marqueeEndMousePosition;
        private  float      lastSizeAdjustment = 1;
        private  string     marqueeDragShortcutKey = "-";

        #endregion
        
        #region events

        private void OnEnable()
        {
            SceneView.duringSceneGui                                         += SceneGuiMain;
            TpPainterState.OnPainterModeChange += OnPainterWindowModeChange;
            ReinitializeGuiContent();
            if(TpLibEditor.Informational)
                TpLog("TpPainterSceneView ScriptableSingleton initialized.");
        }

        private void OnDisable()
        {
            if(TpLibEditor.Informational)
                TpLog("TpPainterSceneView ScriptableSingleton released.");

            SceneView.duringSceneGui                                         -= SceneGuiMain;
            TpPainterState.OnPainterModeChange -= OnPainterWindowModeChange;
        }

        private  void OnPainterWindowModeChange(GlobalMode mode, TpPainterTool tool, TpPainterMoveSequenceStates _)
        {
            if (mode != lastGlobalMode || tool != lastPainterTool)
                allowOverwriteMessageToSceneView = true;
            lastGlobalMode  = mode;
            lastPainterTool = tool;
            if (mode != GlobalMode.GridSelView && tool != TpPainterTool.None)
                return;
            ClearPreview();
            GUIUtility.hotControl = 0;
        }
        
        #endregion

        #region sceneview
        /// <summary>
        /// SceneView delegate
        /// </summary>
        /// <param name="sceneView">The scene view.</param>
        private  void SceneGuiMain(SceneView sceneView)
        {
            if (showNotification)
            {
                sceneView.ShowNotification(new GUIContent($"Painter: {notificationBacking}"), TilePlusConfig.instance.TileHighlightTime);
                notificationBacking = string.Empty;
                showNotification    = false;
            }

            
            
            if(Win == null || !Win.GuiInitialized || (!InGridSelMode && ToolManager.activeToolType != typeof(TilePlusPainterTool)))
                return;

            var evt            = Event.current;
            var currentEvtType = evt.type;
            PreviewInhibited          = !sceneView.in2DMode;
            IgnoreNextHierarchyChange = false;
            var currentTool = TpPainterState.CurrentTool;
            IsPickPhaseOfMove = currentTool is TpPainterTool.Move
                                && (TpPainterState.TpPainterMoveSequenceState == TpPainterMoveSequenceStates.Pick);
            
            if(TpPainterState.InPaintMode)
                Draggable = (currentTool is TpPainterTool.Paint or TpPainterTool.Erase or TpPainterTool.Pick)
                        || IsPickPhaseOfMove;
            else
                Draggable = (currentTool is TpPainterTool.Paint or TpPainterTool.Erase)
                            || IsPickPhaseOfMove;
            
            dragClutchShortcutActive = TpPainterShortCuts.MarqueeDragState;
            
            //Grid Mode has a separate OnSceneGui()
            if (InGridSelMode)
            {
                if (PaintableTilemap == null || TpPainterState.PaintableMap is
                    {
                        Valid: false
                    })
                    return;
                //mouse position calcs
                currentMouseLocalPosition = (Vector3)ScreenToLocal!.Invoke(null, new[] { GridTransform, (object)evt.mousePosition });
                lastMouseGridPosition     = currentMouseGridPosition;
                currentMouseGridPosition  = PaintableTilemap.LocalToCell(currentMouseLocalPosition); //  WorldToCell(s_CurrentMouseLocalPosition);
                positionChanged = lastMouseGridPosition != TilePlusBase.ImpossibleGridPosition
                                    && lastMouseGridPosition != currentMouseGridPosition;
                GridSelOnSceneGui(currentEvtType);
                return;
            }

            //prevent the two palettes from interfering.
            if(!Win.IsActive)
            {
                if (GUIUtility.hotControl == ControlId)
                    GUIUtility.hotControl = 0;
                if (IsPreviewActiveForId(0)) //is there a preview active for SceneView (ID is 0)?
                    ClearPreview();
                return;
            }

            //the remainder of this method is used for Paint or Edit modes
            // ReSharper disable once Unity.NoNullPatternMatching
            if(!TpPainterState.CurrentToolHasTilemapEffect
               
                 || PaintableTilemap == null
                 || TpPainterState.PaintableMap is
                 {
                     Valid: false
                 })
            {
                if (GUIUtility.hotControl == ControlId)
                    GUIUtility.hotControl = 0;
                if (IsPreviewActiveForId(0)) //is there a preview active for SceneView (ID is 0)?
                    ClearPreview();
                return;
            }

            IgnoreNextHierarchyChange = false;

            //is the event appropriate? //allowed evts = mouseup/down, drag, and repaint ONLY.
            if (!allowedEventsInSceneGui.Contains(currentEvtType))
                return;
            //is a valid tool selected?
            if (currentEvtType != EventType.Repaint)
            {
                if ( TpPainterState.CurrentTool is TpPainterTool.None or TpPainterTool.Help or TpPainterTool.Settings)
                {
                    if (brushOpInProgress)
                    {
                        if (TpLibEditor.Informational)
                            TpLog("Cancelling brush operation from OnSceneGui");
                        brushOpInProgress = false;
                    }

                    TpPainterState.TpPainterMoveSequenceState = TpPainterMoveSequenceStates.None;
                    ResetState(false); 
                    if (GUIUtility.hotControl == ControlId)
                        GUIUtility.hotControl = 0;
                    return;
                }
            }
            
            //assemble required data for continuing with SceneView
            var validTilemapSelection = TpPainterState.ValidTilemapSelection;
            currentPaintingTilemapHasOriginZero = PaintableTilemap.transform.position == Vector3.zero;
            //mouse position calcs
            currentMouseLocalPosition = (Vector3)ScreenToLocal!.Invoke(null, new[] { GridTransform, (object)evt.mousePosition });
            lastMouseGridPosition     = currentMouseGridPosition;
            currentMouseGridPosition  = PaintableTilemap.LocalToCell(currentMouseLocalPosition); //  WorldToCell(s_CurrentMouseLocalPosition);
            positionChanged = lastMouseGridPosition != TilePlusBase.ImpossibleGridPosition
                                && lastMouseGridPosition != currentMouseGridPosition;
            
            (var noPaintLocked, (var allowPrefabEditing, _, _, _)) = TpLibEditor.NoPaint(PaintableTilemap);
            var noPaint = noPaintLocked && !allowPrefabEditing;

            //are we in the correct scene view for a preview?
            if (MouseOverSceneView() &&  TpPainterState.Clipboard != null && !PreviewInhibited && !noPaint && currentTool != TpPainterTool.None)
                TpPreviewUtility.HandlePreviews(PaintableTilemap, TpPainterState.Clipboard, currentMouseGridPosition, currentTool, TpPainterState.TpPainterMoveSequenceState);
            else if (IsPreviewActiveForId(0)) //is there a preview active for SceneView (ID is 0)?
                ClearPreview();
            

            //TileFab authoring mode (Chunk Snapping)
            fabAuthoringMode = false;
            if (TilePlusPainterConfig.PainterFabAuthoringMode && TpPainterState.InPaintMode)
            {
                if (currentTool == TpPainterTool.Paint)
                    fabAuthoringMode = TpPainterState.Clipboard is { Valid: true, ItemVariety: Variety.TileFabItem };
                else if (currentTool == TpPainterTool.Erase)
                    fabAuthoringMode = true;
            }

            var didPick              = false;
            MovePickPhaseHadPick = false;

            //Dragging
            if (dragClutchShortcutActive && DragConditionsMet())
            {
                didPick              = HandleUserMarquee();
                MovePickPhaseHadPick = didPick && IsPickPhaseOfMove;
                if (MovePickPhaseHadPick)
                {
                    DoMove();
                    return;
                }
            }
            else if(currentEvtType != EventType.MouseDown)
                marqueeDragActiveLastPass = false;
            
            if(marqueeDragActiveLastPass || TpPainterState.CurrentToolHasTilemapEffect) 
                TpLibEditor.TilemapMarquee(TargetGridLayout!, marqueeDragActiveLastPass
                                                              ? marqueeDragBounds
                                                              : new BoundsInt(currentMouseGridPosition, Vector3Int.one), TilePlusPainterConfig.TpPainterMarqueeColor);
            else if(!MovePickPhaseHadPick) //if a pick during move-pick phase we dont do this
            {
                ResetState();
                if (GUIUtility.hotControl == ControlId)
                    GUIUtility.hotControl = 0;
                return;
            }
            
            //Chunk Snapping marquee
            if (fabAuthoringMode) 
            {
                var color = TilePlusPainterConfig.TpPainterMarqueeColor;
                var size  = TilePlusPainterConfig.PainterFabAuthoringChunkSize;

                if (OnGrid)
                    color *= Color.red;
                else
                {
                    var localAlignedPos = AlignToGrid(currentMouseGridPosition);
                    TpLibEditor.TilemapMarquee(TargetGridLayout!,
                                               new BoundsInt(localAlignedPos, new Vector3Int(size, size)),
                                               color);
                }

                TpLibEditor.TilemapMarquee(TargetGridLayout!,
                                           new BoundsInt(currentMouseGridPosition, new Vector3Int(size, size)),
                                           color);
            }

          
            //REPAINT: show brush position and other info if that's enabled. If in GridSel view and hot key not active then skip this.
            if ( currentEvtType == EventType.Repaint && TargetGridLayout != null)
            {
                if(!TpPainterState.CurrentToolHasTilemapEffect || TpPainterState.Clipboard == null)
                    return;
                HandleRepaint(PaintableTilemap,
                              noPaint,
                              TargetGridLayout,
                              currentTool,
                              TpPainterState.TpPainterMoveSequenceState,
                              TpPainterState.GlobalMode,
                              OnGrid,
                              Draggable && dragClutchShortcutActive);
                return;
            }

            if (brushOpInProgress)
            {
                //now handle mouse up event, marks the end of an operation.
                if (currentEvtType == EventType.MouseUp)
                {
                    cantPaintHereInterlock  = false;
                    IgnoreNextHierarchyChange = false;
                    brushOpInProgress       = false;
                    dragLockX               = dragLockY = false;
                    GUIUtility.hotControl     = 0;
                    Event.current.Use();
                    GUI.changed = true;
                    return;
                }

                //handle mouse drag for repeat paint/erase ops while mouse button held down
                if (currentEvtType == EventType.MouseDrag && Draggable)
                {
                    if (!positionChanged || !HandleDrag(evt))
                    {
                        GUIUtility.hotControl = ControlId;
                        Event.current.Use();
                        GUI.changed              = true;
                        cantPaintHereInterlock = false;
                        return;
                    }
                }
            }

            //now handle the various painting modes
            if (currentTool != TpPainterTool.Move)
                TpPainterState.TpPainterMoveSequenceState = TpPainterMoveSequenceStates.None;

            if (noPaint || didPick || marqueeDragActiveLastPass)
            {
                GUIUtility.hotControl = ControlId;
                Event.current.Use();
                cantPaintHereInterlock = false;
                return;
            }

            //this allows right-drag within the scene window.
            if (evt.isMouse && evt.button != 0)
                return;

            //NOW handle the tools like Paint, Erase, etc.
            switch (currentTool)
            {
                //-----PAINT TOOL ------
                case TpPainterTool.Paint when  ValidPaintTargetAndPaintableObject:
                {
                    if(TpPainterState.Clipboard is { IsNotEmpty: true })
                        DoPaint();
                    break; 
                }
                //--------ERASE TOOL -------
                case TpPainterTool.Erase when validTilemapSelection:
                {
                    DoErase();
                    break;
                }
                //---------PICK TOOL----------------------
                //ValidTilemapSelection just means that a tilemap is selected
                case TpPainterTool.Pick when validTilemapSelection:
                {
                    DoPick();
                    break;
                }
                //---------MOVE TOOL-----------------
                case TpPainterTool.Move when validTilemapSelection:
                {
                    DoMove();
                    break;
                }
                //-------------ROTATE TOOLS----------------------
                case TpPainterTool.RotateCw or TpPainterTool.RotateCcw when validTilemapSelection:
                {
                    DoRotate();
                    break;
                }
                //----------FLIP TOOLS-------------------
                case TpPainterTool.FlipX or TpPainterTool.FlipY when validTilemapSelection:
                {
                    DoFlip();
                    break;

                }
                //----------RESET TRANSFORM TOOL-------------------
                case TpPainterTool.ResetTransform when validTilemapSelection:
                {
                    DoResetTransform();
                    break;
                }
            }
        }

        /// <summary>
        /// Handler for Scene View callback when Painter is in GridSelection mode.
        /// </summary>
        private  void GridSelOnSceneGui(EventType currentEvtType)
        {
            //display any marquee set up by the Grid Selection panel.
            var gridSelPanelActiveGridSelection = Win!.ActiveGridSelectionElement;
            if (gridSelPanelActiveGridSelection != null && TpPainterState.PaintableMap != null)
            {
                var gridSelMap = PaintableTilemap;
                if (gridSelMap != null)
                {
                    var bounds = gridSelPanelActiveGridSelection.m_BoundsInt;
                    var layout = gridSelMap.layoutGrid;
                    bounds.position += layout.LocalToCell(gridSelMap.transform.localPosition);
                    TpLibEditor.TilemapMarquee(layout, bounds, TilePlusPainterConfig.TpPainterMarqueeColor);
                }
            }

            //handle the dragging/display of the marquee.
            if (!allowedEventsInSceneGui.Contains(currentEvtType))
                return;
            if (dragClutchShortcutActive)
                HandleUserMarquee();
            else if(Event.current.type != EventType.MouseDown)
                marqueeDragActiveLastPass = false;

            TpLibEditor.TilemapMarquee(TargetGridLayout!, marqueeDragActiveLastPass
                                                                  ? marqueeDragBounds
                                                                  : new BoundsInt(currentMouseGridPosition, Vector3Int.one), TilePlusPainterConfig.TpPainterMarqueeColor);
            
            if (currentEvtType == EventType.Repaint)
            {
                HandleRepaint(PaintableTilemap!, false, TargetGridLayout!, TpPainterTool.None, TpPainterMoveSequenceStates.None, GlobalMode.GridSelView, false,true);
                return;
            }
            
            //handle mouse drag for repeat paint/erase ops while mouse button held down
            if (currentEvtType != EventType.MouseDrag)
                return;
            if (positionChanged && HandleDrag(Event.current))
                return;
            GUIUtility.hotControl = ControlId;
            Event.current.Use();
            GUI.changed = true;
        }
        
        #endregion
        
        #region toolImplementation

        private  void DoPaint()
        {
            if(PaintableTilemap == null  || TpPainterState.Clipboard == null )
                return;
            
            switch (TpPainterState.Clipboard)
            {
                case { ItemVariety: Variety.TileItem }:
                    DoPaintSingleTile();
                    break;
                case { ItemVariety: Variety.BundleItem, Valid: true }:
                    DoPaintBundle();
                    break;
                case { ItemVariety: Variety.TileFabItem }:
                    DoPaintTileFab();
                    break;
                case { ItemVariety: Variety.MultipleTilesItem, Cells: not null }:
                    DoPaintSelection();
                    break;
                case { ItemVariety: Variety.PrefabItem }:
                    DoPaintPrefab(null);
                    break;
                case {ItemVariety: Variety.EmptyItem}:
                    TpLogError("Empty Paintable sent to Sceneview.Paint!");
                    break;
            }
        }

        //PaintableObject and PaintableTilemap cannot be null here so the null warnings are ! out.
        private  void DoPaintSingleTile()
        {
            GUIUtility.hotControl = ControlId;
            Event.current.Use();
            
            if (PaintableTilemap == null
                || TpPainterState.Clipboard == null
                || (TpPainterState.Clipboard.AnyModifications && TileFlagsInappropriate(TpPainterState.Clipboard)))
            {
                return;
            }
            //if the clutch shortcut override key isn't active, and the no-overwrite flag in Config is ON
            if (!AllowOverwriteOrIgnoreMap && TilePlusConfig.instance.NoOverwriteFromPalette)
            {
                var tpb = PaintableTilemap.GetTile<TilePlusBase>(currentMouseGridPosition);
                if (tpb != null)
                {
                    if (TpLibEditor.Warnings)
                        TpLogWarning($"*** skipping location {currentMouseGridPosition.ToString()} due to potential overwrite of TilePlus tile [type:{tpb.GetType()}].");
                    cantPaintHereInterlock = true;
                    //This mechanism ensures that the message occurs only once until painter mode changes
                    //then the message is enabled again. Avoids spamming these messages.
                    if (!allowOverwriteMessageToSceneView) 
                        return;
                    SceneViewNotification              = "Overwriting TilePlus tiles is inhibited.\nSee Console for more info.";
                    allowOverwriteMessageToSceneView = false;
                    return;
                }
            }

            //for TPT tiles we can restrict where they are painted.
            var tileToPaint = TpPainterState.Clipboard.Tile;
            if (TpPainterState.Clipboard is { IsTilePlusBase: true } && TpPainterState.Clipboard.Tile != null)
            {
                if (AnyRestrictionsForThisTile((ITilePlus)TpPainterState.Clipboard.Tile, MapName, TpPainterState.Clipboard.IsFromBundleAsPalette))
                {
                    cantPaintHereInterlock = true;
                    return;
                }
                else
                    cantPaintHereInterlock = false;
            }
            else
                cantPaintHereInterlock = false;
            
            //Filling a grid selection?
            if (GridSelection.active
                     && GridSelection.target.activeSelf
                     && GridSelection.target.TryGetComponent<Tilemap>(out var testmap)
                     && testmap == PaintableTilemap
                     && tileToPaint != null
                     // ReSharper disable once Unity.NoNullPatternMatching
                     && tileToPaint is not TilePlusBase { IsClone: true }
                     && GridSelection.position.Contains(currentMouseGridPosition))
            {
                brushOpInProgress   = true;
                RegisterUndo(PaintableTilemap, $"T+P: Painting Tiles in GridSelection [{tileToPaint.name}] on Tilemap [{MapName}] at [{currentMouseGridPosition}] ");
                Win!.AddGridSelection(GridSelection.position, true); //note that dupes are rejected.
               
                var arraySize    = GridSelection.position.size.x * GridSelection.position.size.y;
                var bPositions   = new Vector3Int[arraySize];
                var bTiles       = new TileBase[arraySize];
                
                //is this a clone tile? Reclone it.
                // ReSharper disable once Unity.NoNullPatternMatching
                if (tileToPaint != null && tileToPaint is TilePlusBase { IsClone: true } tpb)
                {
                    var reClone = Object.Instantiate(tpb); //clone the copied tile; we need a new instance.
                    if (reClone != null)                               //unlikely fail, but make this silent
                    {
                        reClone.ChangeTileState(TileResetOperation.MakeCopy);
                        tileToPaint = reClone;
                    }
                }

                
                Array.Fill(bTiles,tileToPaint); //fill the array with one tile.

                var index = 0;
                foreach (var pos in GridSelection.position.allPositionsWithin)
                    bPositions[index++] = pos;

                Matrix4x4? forcedTransform = TpPainterState.Clipboard.TransformModified || TpPainterState.Clipboard.WasPickedTile
                                                 ? TpPainterState.Clipboard.transform : null;
                Color? forcedColor = TpPainterState.Clipboard.ColorModified || TpPainterState.Clipboard.WasPickedTile
                                         ? TpPainterState.Clipboard.AColor
                                         : null;

                BulkOp(PaintableTilemap, bTiles, bPositions, null,null, forcedTransform,forcedColor);       
                
                GUI.changed = true;
            }
            else if (tileToPaint != null) //a single tile.
            {
                brushOpInProgress = true;
                GUIUtility.hotControl = ControlId;
                
                // ReSharper disable once Unity.NoNullPatternMatching
                if (TpPainterState.Clipboard.Tile is Tile t) //if a tile or subclass
                {
                    // ReSharper disable once Unity.NoNullPatternMatching
                    if (t is TilePlusBase cloningCandidate)
                    {
                        var newT = CloneTpb(cloningCandidate);
                        if (newT == null)
                        {
                            GUI.changed = true;
                            TpLib.TpLogWarning("Could not clone TPT tile, paint operation cancelled!");
                            return;
                        }

                        t = newT;
                    }
                    RegisterUndo(PaintableTilemap, $"T+P: Painting Tile [{tileToPaint.name}] on Tilemap [{MapName}] at [{currentMouseGridPosition}] ");
                    var tc = new TileChangeData(currentMouseGridPosition, t, TpPainterState.Clipboard.AColor, TpPainterState.Clipboard.transform);
                    PaintableTilemap.SetTiles(new []{tc},true);
                    GUI.changed = true;
                    return;
                }

                //TileBase tiles have no transform or color props so just place the tile.
                RegisterUndo(PaintableTilemap, $"T+P: Painting TileBase [{tileToPaint.name}] on Tilemap [{MapName}] at [{currentMouseGridPosition}] ");
                PaintableTilemap.SetTile(currentMouseGridPosition, TpPainterState.Clipboard.Tile);
               
                GUI.changed = true;
            }
        }

        
        
        private  void DoPaintBundle()
        {
            //this checks for rogue Bundles that someone might have created manually.
            //the only appropriate time for that is to make a prefab palette ie just 
            //drag prefabs into a Bundle asset w no tiles.
            if (TpPainterState.Clipboard is { IsNotEmpty: true, IsBundle: true }
                && TpPainterState.Clipboard.Bundle != null
                && !TpPainterState.Clipboard.Bundle.Valid)
            {
                Event.current.Use();
                GUI.changed = true;
                return;
            }

            var chunk = TpPainterState.Clipboard!.Bundle;
            brushOpInProgress   = true;
            GUIUtility.hotControl = ControlId;

            var pos = currentMouseGridPosition;

            RegisterUndo(PaintableTilemap!, $"T+P: Painting Bundle [{chunk!.name}] on Tilemap [{MapName}] at [{currentMouseGridPosition}] ");
            TileFabLib.LoadBundle(chunk,
                                  PaintableTilemap!,
                                  pos,
                                  TpTileBundle.TilemapRotation.Zero,
                                  FabOrBundleLoadFlags.LoadPrefabs);
            Event.current.Use();
            GUI.changed = true;
        }

        private  void DoPaintTileFab()
        {
            brushOpInProgress   = true;
            GUIUtility.hotControl = ControlId;

            //in 'fab authoring mode' ie placing things on a chunk-sized grid.
            var snapPosition = fabAuthoringMode && !OnGrid;

            //in order for UNDO to work properly, need the tilemaps.
            var assets = TpPainterState.Clipboard!.TileFab!.m_TileAssets;
            if (assets!.Count == 0)
                return;
            var mapLookup = new Dictionary<string, Tilemap>(assets.Count);
            foreach (var item in assets)
            {
                var foundMap = TileFabLib.FindTilemap(item);
                if (foundMap != null)
                    mapLookup.Add(item.m_TilemapName, foundMap);
            }

            if (mapLookup.Count != assets.Count)
            {
                Event.current.Use();
                GUI.changed = true;
                return;
            }

            foreach (var kvp in mapLookup)
                RegisterUndo(kvp.Value, $"T+P: Painting TileFab [{TpPainterState.Clipboard.TileFab.name}] on Tilemap [{kvp.Key}] at [{currentMouseGridPosition}] ");

            var placementPos = snapPosition
                                   ? AlignToGrid(currentMouseGridPosition)
                                   : currentMouseGridPosition;


            var result = TileFabLib.LoadTileFab(PaintableTilemap,
                                                TpPainterState.Clipboard.TileFab,
                                                placementPos,
                                                TpTileBundle.TilemapRotation.Zero,
                                                FabOrBundleLoadFlags.LoadPrefabs | FabOrBundleLoadFlags.NewGuids | FabOrBundleLoadFlags.NewGuids,
                                                null,
                                                mapLookup);
            if (result == null)
                TpLogError($"Loading of TileFab {TpPainterState.Clipboard.TileFab.name} failed!");
            else if (TpLibEditor.Informational)
            {
                TpLog($"Placed tiles from TileFab {TpPainterState.Clipboard.TileFab}. Elapsed Time: {result.ElapsedTimeString} Results:");
                if (result.LoadedBundles != null)
                {
                    foreach (var item in result.LoadedBundles)
                        TpLog($"Asset: {item}");
                }

            }

            Event.current.Use();
            GUI.changed = true;
        }


        private  void DoPaintSelection()
        {
            
            brushOpInProgress = true;
            RegisterUndo(PaintableTilemap!, $"T+P: Painting Multiple items on Tilemap [{MapName}] at [{currentMouseGridPosition}] ");
            GUIUtility.hotControl = ControlId;
            Event.current.Use();

            if (TileFlagsInappropriate(TpPainterState.Clipboard!))
                return;


            //transform the array of TileCells into arrays of tiles, positions, colors, and matrices.
            var bounds     = TpPainterState.Clipboard!.BoundsInt; //this is a zero-origined bounds.
            var arraySize  = bounds.size.x * bounds.size.y;
            var bPositions = new Vector3Int[arraySize];
            var bTiles     = new TileBase[arraySize];
            var bColors    = new Color[arraySize];
            var bMatrices  = new Matrix4x4[arraySize];

            var index = 0;
            var basePos   = currentMouseGridPosition;
            basePos += TpPainterState.Clipboard.OffsetModifier; //this mods the offset based on changes from rotation and pivot changes (default=PGUP)
            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (var cell in TpPainterState.Clipboard.Cells!)
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if(cell == null)
                    continue;
                bPositions[index] = basePos + cell.m_Position;
                var tile = cell.TileBase;
                if (cell.IsTilePlus)
                {
                    var tpb     = cell.TileBase as TilePlusBase;
                    var reClone = Object.Instantiate(tpb); //clone the copied tile; we need a new instance.
                    if (reClone != null)                   //unlikely fail, but make this silent
                    {
                        reClone.ChangeTileState(TileResetOperation.MakeCopy);
                        tile = reClone;
                    }
                }

                bTiles[index]      = tile!;
                bColors[index]     = cell.m_Color;
                bMatrices[index++] = cell.m_Transform;
            }

            Matrix4x4? forcedTransform = TpPainterState.Clipboard.TransformModified || TpPainterState.Clipboard.WasPickedTile
                                             ? TpPainterState.Clipboard.transform : null;
            Color? forcedColor = TpPainterState.Clipboard.ColorModified || TpPainterState.Clipboard.WasPickedTile
                                     ? TpPainterState.Clipboard.AColor
                                     : null;

            //now paint them in a bunch.
            BulkOp(PaintableTilemap!, bTiles, bPositions,bColors,bMatrices, forcedTransform,forcedColor,TpPainterState.Clipboard.AnyModifications);     
            
            GUI.changed = true;
        }

        private  void DoPaintPrefab(BoundsInt? selection)
        {
            var prefabToPaint = TpPainterState.Clipboard!.Prefab;
            if (prefabToPaint == null || PaintableTilemap == null)
                return;
            
            var position = new BoundsInt();
            var usingSelection = false;
            if (selection.HasValue)
            {
                position       = selection.Value;
                usingSelection = true;
            }
            else if (GridSelection.active && GridSelection.target.activeSelf
                                          && GridSelection.target.TryGetComponent<Tilemap>(out var targetMap)
                                          && targetMap == PaintableTilemap)
            {
                position       = GridSelection.position;
                usingSelection = true;
            }

            
            if (usingSelection && position.Contains(currentMouseGridPosition))
            {
                brushOpInProgress   = true;
                GUIUtility.hotControl = ControlId;

                RegisterUndo(PaintableTilemap, $"T+P: Painting Prefabs in GridSelection [{prefabToPaint.name}] on Tilemap [{PaintableTilemap.name}] at [{currentMouseGridPosition}] ",true);
                Win!.AddGridSelection(position, true); //note that dupes are rejected.

                var layer = PaintableTilemap.gameObject.layer;
                foreach (var pos in position.allPositionsWithin)
                {
                    var go = PrefabUtility.InstantiatePrefab(prefabToPaint, PaintableTilemap.transform) as GameObject;
                    if (go==null )
                    {
                        TpLib.TpLogError($"Could not instantiate prefab {prefabToPaint.name}");
                        continue;
                    }

                    var basePosition = PaintableTilemap.GetCellCenterWorld(pos);
                    var tRotation    = Vector3.zero;
                    if (TpPainterState.Clipboard.TransformModified)
                    {
                        var trans     = TpPainterState.Clipboard.transform; //note that this is for a tile sprite...
                        var tPosition = trans.GetPosition();
                        tRotation = trans.rotation.eulerAngles;
                        var tScale = trans.lossyScale;
                        //use position as an offset to base position
                        basePosition            += tPosition;
                        go.transform.localScale =  tScale;
                    }

                    go.transform.position    = basePosition;
                    go.transform.eulerAngles = tRotation;
                    go.layer                 = layer;
                    var children = go.transform.childCount;
                    if (children == 0)
                        continue;
                    for (var i = 0; i < children; i++)
                    {
                        var child = go.transform.GetChild(i);
                        child.gameObject.layer = layer;
                    }
                }

                Event.current.Use();
                GUI.changed = true;
            }

            else //single prefab
            {
                brushOpInProgress = true;
                RegisterUndo(PaintableTilemap, $"T+P: Painting Prefab on Tilemap [{MapName}] at [{currentMouseGridPosition}] ", true);
                GUIUtility.hotControl = ControlId;
                var go = PrefabUtility.InstantiatePrefab(TpPainterState.Clipboard.Prefab, PaintableTilemap.transform) as GameObject;
                if (go != null)
                {
                    var basePosition = PaintableTilemap.GetCellCenterWorld(currentMouseGridPosition);
                    var tRotation    = Vector3.zero;
                    if (TpPainterState.Clipboard.TransformModified)
                    {
                        var trans     = TpPainterState.Clipboard.transform; //note that this is for a tile sprite...
                        var tPosition = trans.GetPosition();
                        tRotation = trans.rotation.eulerAngles;
                        var tScale = trans.lossyScale;
                        //use position as an offset to base position
                        basePosition            += tPosition;
                        go.transform.localScale =  tScale;
                    }

                    go.transform.position    = basePosition;
                    go.transform.eulerAngles = tRotation;
                    var layer = PaintableTilemap.gameObject.layer;
                    go.layer = layer;
                    var children = go.transform.childCount;
                    if (children != 0)
                    {
                        for (var i = 0; i < children; i++)
                        {
                            var child = go.transform.GetChild(i);
                            child.gameObject.layer = layer;
                        }
                    }
                }

                Event.current.Use();
                GUI.changed = true;
            }
        }

        private  void DoErase()
        {
            if (fabAuthoringMode)
            {
                Event.current.Use();
                GUI.changed = true;
               
                if (!OnGrid)
                    return;

                if (TpPainterState.Clipboard == null || TpPainterState.Clipboard.TileFab == null || !TpPainterState.Clipboard.Valid)
                {
                    TpLib.DelayedCallback(Win, () => { EditorUtility.DisplayDialog("Please Select....", "Please select a reference TileFab with the same Tilemaps and size as the area you want to erase.", "Continue"); }, "T+P-fab-erase-no-map-available,20");
                    return;
                }
                //in order for UNDO to work properly, need the tilemaps.

                var assets = TpPainterState.Clipboard.TileFab.m_TileAssets;
                if (assets!.Count == 0)
                    return;
                var mapLookup = new Dictionary<string, Tilemap>(assets.Count);
                foreach (var item in assets)
                {
                    var foundMap = TileFabLib.FindTilemap(item);
                    if (foundMap != null)
                        mapLookup.Add(item.m_TilemapName, foundMap);
                }

                if (mapLookup.Count != assets.Count)
                    return;
                
                brushOpInProgress   = true;
                GUIUtility.hotControl = ControlId;
                foreach (var kvp in mapLookup)
                    RegisterUndo(kvp.Value, $"T+P: Erasing area [{TpPainterState.Clipboard.TileFab.name}] on Tilemap [{kvp.Key}] at [{currentMouseGridPosition}] ");

                var placementPos = AlignToGrid(currentMouseGridPosition);

                //area is 'largestbounds' from the asset (they're all the same for chunks)
                var eraseBounds = TpPainterState.Clipboard.TileFab.LargestBounds;
                //offset it 
                eraseBounds.position += placementPos;
                var sz = eraseBounds.size;
                sz.z             = 1;
                eraseBounds.size = sz;

                var nulls = new TileBase[sz.x * sz.y]; //these should all be null.

                foreach (var map in mapLookup.Values)
                {
                    map.SetTilesBlock(eraseBounds, nulls);

                    var trans      = map.transform;
                    var numPrefabs = trans.childCount;
                    for (var i = 0; i < numPrefabs; i++)
                    {
                        var t    = trans.GetChild(0);
                        var tPos = map.WorldToCell(t.position);
                        if (!eraseBounds.Contains(tPos))
                            continue;
                        Object.DestroyImmediate(t.gameObject, false);
                    }
                }
                return;
            }
            
            var possibleTileToDelete = PaintableTilemap!.GetTile(currentMouseGridPosition);
            if (possibleTileToDelete == null)
            {
                GUIUtility.hotControl = ControlId;
                Event.current.Use();
                GUI.changed = true;
                return;
            }

            if (Event.current.shift || Event.current.control)
            {
                brushOpInProgress = true;
                DeleteTileActionWithConfirm(possibleTileToDelete, PaintableTilemap, currentMouseGridPosition, true);
                GUIUtility.hotControl = ControlId;
                Event.current.Use();
                GUI.changed = true;
                return;
            }

            brushOpInProgress = true;
            //has to be delayed since can't open a modal dialog box from within scene view
            TpLib.DelayedCallback(null, () => DeleteTileActionWithConfirm(possibleTileToDelete, PaintableTilemap, currentMouseGridPosition, false),
                                  "Inspector Toolbar Delete Tile", 40);
            GUIUtility.hotControl = ControlId;
            Event.current.Use();
            GUI.changed = true;
        }

        private  void DoPick()
        {
            brushOpInProgress   = true;
            GUIUtility.hotControl = ControlId;

            //note that here we get the tile BUT the transform etc are out of the tilemap.
            var tile = PaintableTilemap!.GetTile(currentMouseGridPosition);
            if (tile != null)
            {

                // ReSharper disable once Unity.NoNullPatternMatching
                if (tile is ITilePlus { IsLocked: true } ||            //can't pick locked tiles.
                    Event.current is { control            : true, shift: true }) //illegal combination
                {
                    Event.current.Use();
                    GUI.changed = true;
                    return;
                }

                if (Event.current.control)
                {
                    // ReSharper disable once Unity.NoNullPatternMatching
                    if(tile is TilePlusBase { IsClone: true })
                         SceneViewNotification = "Cannot add a Clone tile to Favorites!!";
                    else
                        TpLib.DelayedCallback(null, () => { Win!.AddToFavorites(new Object[] { tile }); }, "T+P: SceneviewPick->Favorites", 50);
                }
                else
                {
                    Win!.SetTileTarget(tile, TpPickedTileType.Tile, currentMouseGridPosition, PaintableTilemap);

                    var pickIntent = TilePlusPainterConfig.TpPainterPickToPaint ^ Event.current.shift;

                    // ReSharper disable once Unity.NoNullPatternMatching
                    if (pickIntent && TpPainterState.InPaintMode && TpPainterState.PaintingAllowed)
                    {
                        //note: local function used here to avoid capture of tile ref: poss memory leak
                        void Callback() => Win.ActivateToolbarButton(TpPainterTool.Paint, true);

                        TpLib.DelayedCallback(null, Callback, "T+V: SceneGuiModeChangeToPaint", 50);
                    }
                }
            }
            else
                Win!.ClearClipboard();

            Event.current.Use();
            GUI.changed = true;
        }

        private  void DoMove()
        {
            brushOpInProgress   = true;
            GUIUtility.hotControl = ControlId;

            if (TpPainterState.TpPainterMoveSequenceState == TpPainterMoveSequenceStates.None)
                TpPainterState.TpPainterMoveSequenceState = TpPainterMoveSequenceStates.Pick;
            
            if (TpPainterState.TpPainterMoveSequenceState == TpPainterMoveSequenceStates.Pick )
            {
                if (dragClutchShortcutActive && MovePickPhaseHadPick)
                {
                    var pos  = new Vector3Int(marqueeDragBounds.xMin,              marqueeDragBounds.yMin,              marqueeDragBounds.zMin);
                    var size = new Vector3Int(Mathf.Abs(marqueeDragBounds.size.x), Mathf.Abs(marqueeDragBounds.size.y), 1);
                    var sel  = new BoundsInt(pos, size);
                    if (size.x != 0 && size.y != 0)
                    {
                        HandleMarqueePick(sel);
                        TpPainterState.TpPainterMoveSequenceState = TpPainterMoveSequenceStates.Paint;
                    }
                }
                else
                {
                    var tile = PaintableTilemap!.GetTile(currentMouseGridPosition);
                    if (tile != null)
                    {
                        //note that targetTilemap is the map when the tile was picked. The destination map could be different!
                        Win!.SetTileTarget(tile, TpPickedTileType.Tile, currentMouseGridPosition, PaintableTilemap); 
                        TpPainterState.TpPainterMoveSequenceState = TpPainterMoveSequenceStates.Paint;
                    }
                }
            }
            else if (TpPainterState.TpPainterMoveSequenceState == TpPainterMoveSequenceStates.Paint && ValidPaintTargetAndPaintableObject)
            {
                var fromMarqueePick = TpPainterState.Clipboard!.ItemVariety == Variety.MultipleTilesItem; 
                if (fromMarqueePick)
                {
                    //Can't use the bounds or multipleTilesSelectionBoundsInt from the Paintable Object because it could have been modified, which could
                    //change the bounds x/y size or the multiple... position and size. So get them from the backup copy used for restore.
                    var unmodifiedClipboard = TpPainterState.UnmodifiedClipboard;
                    
                    if (TpPainterState.MovePickSequenceTilemap == null || unmodifiedClipboard == null)
                    {
                        if(Informational)
                            TpLog("Incomplete pick=>move transition: null source Tilemap. Cancelling to avoid erasing the wrong thing");
                    }
                    else
                    {
                        //before erasing the source, test for flag problems in the selection, that is, trans or color modded but tiles have flag set wrong.
                        var fail = false;
                        if (Clipboard.CellsModified)
                        {
                            var tMod = Clipboard.TransformModified;
                            var cMod = Clipboard.ColorModified;
                            foreach (var item in TpPainterState.Clipboard.Cells!.Where(cell => cell.IsTile
                                                                                  && cell.TileBase != null)
                                                                .Select(item => item.TileBase as Tile))
                            {
                                if (tMod && (item!.flags & TileFlags.LockTransform) != 0)
                                {
                                    fail = true;
                                    break;
                                }

                                if (cMod && (item!.flags & TileFlags.LockColor) != 0)
                                {
                                    fail = true;
                                    break;
                                }
                            }
                        }

                        if (fail)
                            InappropriateTransformOrColorInMove();
                        else
                        {
                            //erase the source
                            RegisterUndo(TpPainterState.MovePickSequenceTilemap, $"T+P: ERASING Multiple Tiles on Tilemap [{TpPainterState.MovePickSequenceTilemap}] at [{currentMouseGridPosition}] using MOVE tool ");
                            IgnoreNextHierarchyChange = true;

                            //Can't use the bounds or multipleTilesSelectionBoundsInt from the Paintable Object because it could have been modified, which could
                            //change the bounds x/y size or the multiple... position and size. So get them from the backup copy used for restore.

                            var bounds    = unmodifiedClipboard.BoundsInt; //this is a zero-origined bounds.
                            var arraySize = bounds.size.x * bounds.size.y;

                            //note that TppClipboardItem.MultipleSelectionBoundsInt and TppClipboardItem.BoundsInt have the same size, but BoundsInt's position is ALWAYS zero.
                            //MultipleSelectionBoundsInt is the position of the original source; needed for erasing the origin
                            var bTiles = new TileBase[arraySize]; //these should all be null.
                            TpLib.InhibitOnTpLibChanged = true;
                            //note that BulkOp will use TpLib.ForceOnTpLibChanged to emit the event.
                            TpPainterState.MovePickSequenceTilemap.SetTilesBlock(unmodifiedClipboard.MultipleTilesSelectionBoundsInt, bTiles); //clear source area

                            //paint the target
                            RegisterUndo(PaintableTilemap!, $"T+P: Painting Multiple Tiles on Tilemap [{MapName}] at [{currentMouseGridPosition}] using MOVE tool ");
                            GUIUtility.hotControl = ControlId;

                            //now re-use bTiles and set up other arrays required by BulkOp
                            var bPositions = new Vector3Int[arraySize];
                            var bColors    = new Color[arraySize];
                            var bMatrices  = new Matrix4x4[arraySize];

                            //set up the arrays. NOTE that we don't have to re-clone TPT tiles since this is a MOVE (cut/paste)
                            var index = 0;
                            foreach (var cell in TpPainterState.Clipboard.Cells!)
                            {
                                bPositions[index] = currentMouseGridPosition + cell.m_Position;
                                var tile = cell.TileBase; //NOTE that since we are MOVING tiles, we don't have to check for clone tiles to reclone.
                                bTiles[index]      = tile!;
                                bColors[index]     = cell.m_Color;
                                bMatrices[index++] = cell.m_Transform;
                            }

                            Matrix4x4? forcedTransform = TpPainterState.Clipboard.TransformModified || TpPainterState.Clipboard.WasPickedTile
                                                             ? TpPainterState.Clipboard.transform
                                                             : null;
                            Color? forcedColor = TpPainterState.Clipboard.ColorModified || TpPainterState.Clipboard.WasPickedTile
                                                     ? TpPainterState.Clipboard.AColor
                                                     : null;
                            //now paint them in a bunch.
                            BulkOp(PaintableTilemap!,
                                   bTiles,
                                   bPositions,
                                   bColors,
                                   bMatrices,
                                   forcedTransform,
                                   forcedColor,
                                   Clipboard.CellsModified, 
                                   true);
                        }
                    }

                    Event.current.Use();
                    GUI.changed = true;
                }

                if (!fromMarqueePick && TpPainterState.Clipboard.Valid)
                {
                    cantPaintHereInterlock = false;

                    if (TpPainterState.MovePickSequenceTilemap == null)
                    {   
                        if(Informational)
                            TpLog("Incomplete pick=>move transition: null source Tilemap. Cancelling to avoid erasing the wrong thing");
                    }
                    else
                    {
                        var t = TpPainterState.Clipboard.Tile;
                        // ReSharper disable once Unity.NoNullPatternMatching
                        if (t != null && t is ITilePlus itpTile)
                        {
                            //restrictions apply to the destination.
                            if (AnyRestrictionsForThisTile(itpTile, MapName, TpPainterState.Clipboard.IsFromBundleAsPalette))
                            {
                                GUIUtility.hotControl    = ControlId;
                                Event.current.Use();
                                Win!.ClearClipboard();
                                TpPainterState.TpPainterMoveSequenceState = TpPainterMoveSequenceStates.None;
                                return;
                            }
                        }
                    
                        // ReSharper disable once Unity.NoNullPatternMatching
                        if(t!=null && t is Tile tile)
                        {
                            var fail = false;
                            if (((tile.flags & TileFlags.LockTransform) != 0) && TpPainterState.Clipboard.TransformModified)
                            {
                                fail = true;
                                InappropriateTransformOrColorInMove();
                            }
                            
                            if(((tile.flags & TileFlags.LockColor) != 0) && TpPainterState.Clipboard.ColorModified)
                            {
                                fail = true;
                                InappropriateTransformOrColorInMove();
                            }

                            if (fail)
                            {
                                GUIUtility.hotControl = ControlId;
                                Event.current.Use();
                                GUI.changed = true;
                                Win!.ClearClipboard();
                                TpPainterState.TpPainterMoveSequenceState = TpPainterMoveSequenceStates.Pick;
                                return;
                            }
                        }
                        
                        //register undo for the tilemap that the picked tile came from
                        RegisterUndo(TpPainterState.MovePickSequenceTilemap, $"Move Op delete Tile on Tilemap [{TpPainterState.MovePickSequenceTilemap}] at [{currentMouseGridPosition}] ");
                        //using the tilemap and position from the saved target, clear the tile in the source tilemap and position
                        TpPainterState.MovePickSequenceTilemap.SetTile(TpPainterState.Clipboard.Position, null);

                        //now paint the tile at its destination. 
                        RegisterUndo(PaintableTilemap!, $"Move Op placed Tile on Tilemap [{TpPainterState.Clipboard.Tile!.name}] at [{currentMouseGridPosition}] ");

                        PaintableTilemap!.SetTile(currentMouseGridPosition, TpPainterState.Clipboard.Tile);
                        if (TpPainterState.Clipboard.TransformModified)
                            PaintableTilemap.SetTransformMatrix(currentMouseGridPosition, TpPainterState.Clipboard.transform);
                        if(TpPainterState.Clipboard.ColorModified)
                            PaintableTilemap.SetColor(currentMouseGridPosition,TpPainterState.Clipboard.AColor);
                    }
                }

                Win!.ClearClipboard();
                TpPainterState.TpPainterMoveSequenceState = TpPainterMoveSequenceStates.Pick;
            }

            Event.current.Use();
            GUI.changed = true;
        }

        private  void DoRotate()
        {
            var tile = PaintableTilemap!.GetTile(currentMouseGridPosition) as Tile;
            if (tile != null)
            {
                if((tile.flags & TileFlags.LockTransform) != 0)
                {
                    InappropriateSingleTileTransform();
                    GUIUtility.hotControl = ControlId;
                    Event.current.Use();
                    GUI.changed = true;
                    return;
                }
                brushOpInProgress = true;
                RegisterUndo(PaintableTilemap, $"Rotate Tile  [{tile.name}] on Tilemap [{MapName}] at [{currentMouseGridPosition}] ");
                var rotationMatrix = TileUtil.RotatationMatixZ(TpPainterState.CurrentTool == TpPainterTool.RotateCw
                                                                   ? -90
                                                                   : 90);
                var transFromMap = PaintableTilemap.GetTransformMatrix(currentMouseGridPosition);
                var trans        = transFromMap * rotationMatrix;
                PaintableTilemap.SetTransformMatrix(currentMouseGridPosition, trans);
            }

            GUIUtility.hotControl = ControlId;
            Event.current.Use();
            GUI.changed = true;
        }

        private  void DoFlip()
        {
            var map  = PaintableTilemap;
            if(map == null)
                return;
            var tile = map.GetTile(currentMouseGridPosition) as Tile;
            if (tile != null)
            {
                if((tile.flags & TileFlags.LockTransform) != 0)
                {
                    InappropriateSingleTileTransform();
                    GUIUtility.hotControl = ControlId;
                    Event.current.Use();
                    GUI.changed = true;
                    return;
                }
                
                brushOpInProgress = true;
                RegisterUndo(map, $"Flip Tile [{tile.name}] on Tilemap [{map.name}] at [{currentMouseGridPosition}] ");

                var transFromMap = map.GetTransformMatrix(currentMouseGridPosition);
                var flipMatrix = TileUtil.ScaleMatrix(TpPainterState.CurrentTool == TpPainterTool.FlipX
                                                          ? new Vector3(-1, 1,  1)
                                                          : new Vector3(1,  -1, 1),
                                                      Vector3Int.zero);
                var trans = transFromMap * flipMatrix;
                map.SetTransformMatrix(currentMouseGridPosition, trans);
            }

            GUIUtility.hotControl = ControlId;
            Event.current.Use();
            GUI.changed = true;
        }

        private  void DoResetTransform()
        {
            var tile = PaintableTilemap!.GetTile(currentMouseGridPosition) as Tile;
            if (tile != null)
            {
                if((tile.flags & TileFlags.LockTransform) != 0)
                {
                    InappropriateSingleTileTransform();
                    GUIUtility.hotControl = ControlId;
                    Event.current.Use();
                    GUI.changed = true;
                    return;
                }
                
                brushOpInProgress = true;
                RegisterUndo(PaintableTilemap, $"ResetPlugins Transform on Tile [{tile.name}] on Tilemap [{MapName}] at [{currentMouseGridPosition}] ");
                var trans = Matrix4x4.TRS(Vector3Int.zero, Quaternion.identity, Vector3.one);
                tile.transform = trans;
                PaintableTilemap.SetTransformMatrix(currentMouseGridPosition, trans);
            }

            GUIUtility.hotControl = ControlId;
            Event.current.Use();
            GUI.changed = true;
        }
        
        
        #endregion
        
        
        
        #region dragging

        /* if shift held down AND position (CELL) changed and mode is paint or erase then let it paint OR erase again
         * if ctrl held down restrict to row or col movement (let it paint again as if it were shift held down).
         */
        /// <summary>
        /// Handles dragging.
        /// </summary>
        /// <param name="evt">The current event</param>
        /// <returns>false to cancel drag</returns>
        private  bool HandleDrag(Event evt)
        {
            if (evt is { shift: true, control: false })
                return true;

            //drag-lock when CTRL held down forces constant X or Y
            if (!evt.control)
                return false;
            //so, it is a mouse drag for Paint or Erase. If CTRL is held down rather than SHIFT
            //(or CTRL+SHIFT) then look at delta X or delta Y.
            //If the change is in X, then keep the old Y position, etc

            if (dragLockX)
                currentMouseGridPosition.x = constantXorY;
            else if (dragLockY)
                currentMouseGridPosition.y = constantXorY;
            else //if not drag-locked, should that state be entered? Let's see...
            {
                var deltaX = Mathf.Abs(currentMouseGridPosition.x - lastMouseGridPosition.x);
                var deltaY = Mathf.Abs(currentMouseGridPosition.y - lastMouseGridPosition.y);
                if (dragLockY || deltaX > 0)
                {
                    dragLockY                  = true;
                    constantXorY               = lastMouseGridPosition.y;
                    currentMouseGridPosition.y = lastMouseGridPosition.y;
                }
                else if (dragLockX || deltaY > 0)
                {
                    constantXorY               = lastMouseGridPosition.x;
                    dragLockX                  = true;
                    currentMouseGridPosition.x = lastMouseGridPosition.x;
                }
            }

            return true;
        }

        #endregion

        #region userMarquee

        private  bool HandleUserMarquee()
        {
            //Note that although drawing the marquees is done during a repaint, they're set up here and executed in
            //TpLibEditor.SceneGuiMain which only handles repaint and handles all Marquee drawing.

            /* Handle marquee drag if TpPainterShortcuts MarqueeDragState is true 
             * If that's true, manipulate a marquee. If it was true the last pass
             * but not this one, then the marquee size is sent to the GridSelection static class
             * (from the 2D Tilemap Editor) as a new GridSelection.
            */
            var currentEvtType = Event.current.type;
            var didPaint       = false;
            var didPick        = false;
            if (currentEvtType == EventType.MouseDown)
            {
                Win!.DeselectGridSelPanel();

                marqueeDragActiveLastPass  = true;
                marqueeDragBounds.position = currentMouseGridPosition; //bounds position is initially the mouse position right now
                marqueeStartMousePosition  = currentMouseGridPosition;
                marqueeEndMousePosition    = currentMouseGridPosition;
                marqueeDragBounds.size     = Vector3Int.one;
            }
            else if (currentEvtType == EventType.MouseDrag && marqueeDragActiveLastPass) // && positionChanged)
            {
                marqueeDragActiveLastPass = true;
                var pos     = new Vector3Int(Mathf.Min(marqueeStartMousePosition.x, lastMouseGridPosition.x),      Mathf.Min(marqueeStartMousePosition.y, lastMouseGridPosition.y),      0);
                var newSize = new Vector3Int(Mathf.Abs(marqueeStartMousePosition.x - lastMouseGridPosition.x) + 1, Mathf.Abs(marqueeStartMousePosition.y - lastMouseGridPosition.y) + 1, 1);
                marqueeDragBounds.size     = newSize;
                marqueeDragBounds.position = pos;
            }
            else if (currentEvtType == EventType.Repaint && marqueeDragActiveLastPass)
                marqueeDragActiveLastPass = true;
            else if (currentEvtType == EventType.MouseUp && marqueeDragActiveLastPass)
            {
                marqueeDragActiveLastPass = false;

                if (dragClutchShortcutActive)
                {
                    marqueeEndMousePosition = currentMouseGridPosition;
                    var pos  = new Vector3Int(marqueeDragBounds.xMin, marqueeDragBounds.yMin, marqueeDragBounds.zMin);
                    var size = new Vector3Int(Mathf.Abs(marqueeDragBounds.size.x), Mathf.Abs(marqueeDragBounds.size.y), 1);
                    var sel  = new BoundsInt(pos, size);
                    if (size.x != 0 && size.y != 0)
                    {
                        Win!.AddGridSelection(sel, true); //fail silently

                        if (InPaintMode &&
                            CurrentTool is TpPainterTool.Paint &&
                            ValidPaintTargetAndPaintableObject &&
                            Clipboard!.ItemVariety is Variety.TileItem or Variety.PrefabItem)
                        {
                            if (Clipboard.ItemVariety is Variety.TileItem)
                            {
                                if (Clipboard.AnyModifications && TileFlagsInappropriate(TpPainterState.Clipboard))
                                {
                                    GUIUtility.hotControl = ControlId;
                                    Event.current.Use();
                                    
                                    cantPaintHereInterlock  = false;
                                    IgnoreNextHierarchyChange = false;
                                    brushOpInProgress       = true; 
                                    dragLockX               = dragLockY = false;
                                    
                                    return false;
                                }
                                var tileToPaint = TpPainterState.Clipboard.Tile;

                                // ReSharper disable once Unity.NoNullPatternMatching
                                if (tileToPaint != null && tileToPaint is not TilePlusBase { IsClone: true })
                                {
                                    didPaint = true;
                                    RegisterUndo(PaintableTilemap!,
                                                 $"T+P: Painting Tiles in GridSelection [{tileToPaint.name}] on Tilemap [{MapName}] at [{currentMouseGridPosition}] ");
                                    IgnoreNextHierarchyChange = true;

                                    var arraySize  = sel.size.x * sel.size.y;
                                    var bPositions = new Vector3Int[arraySize];
                                    var bTiles     = new TileBase[arraySize];

                                    //is this a clone tile? Reclone it.
                                    // ReSharper disable once Unity.NoNullPatternMatching
                                    if (tileToPaint is TilePlusBase { IsClone: true } tpb)
                                    {
                                        var reClone = Object.Instantiate(tpb); //clone the copied tile; we need a new instance.
                                        if (reClone != null)                   //unlikely fail, but make this silent
                                        {
                                            reClone.ChangeTileState(TileResetOperation.MakeNormalAsset); //each placed tile will reclone itself. Need separate instances. Slow...
                                            tileToPaint = reClone;
                                        } //note that as a normal asset, the tile will clone itself when painted.
                                    }

                                    Array.Fill(bTiles, tileToPaint); //fill the array with one tile.

                                    var index = 0;
                                    foreach (var position in sel.allPositionsWithin)
                                        bPositions[index++] = position;

                                    Matrix4x4? forcedTransform = TpPainterState.Clipboard.TransformModified || TpPainterState.Clipboard.WasPickedTile
                                                                     ? TpPainterState.Clipboard.transform
                                                                     : null;
                                    Color? forcedColor = TpPainterState.Clipboard.ColorModified || TpPainterState.Clipboard.WasPickedTile
                                                             ? TpPainterState.Clipboard.AColor
                                                             : null;

                                    BulkOp(PaintableTilemap!, bTiles, bPositions, null, null, forcedTransform, forcedColor);
                                }
                            }
                            else //must be prefab item
                                DoPaintPrefab(sel); //this will use the Grid Selection
                        }

                        if (!InGridSelMode &&
                            CurrentTool is TpPainterTool.Erase &&
                            currentMouseGridPosition != TilePlusBase.ImpossibleGridPosition &&
                            TpPainterState.PaintableMap is { Valid: true })
                        {
                            didPaint = true; 
                            RegisterUndo(PaintableTilemap!,
                                         $"T+P: Deleting Tiles in GridSelection on Tilemap [{MapName}] at [{currentMouseGridPosition}] ");
                            IgnoreNextHierarchyChange = true;

                            var arraySize = sel.size.x * sel.size.y;
                            var bTiles = new TileBase[arraySize]; //these should all be null.
                            TpLib.InhibitOnTpLibChanged = true;
                            PaintableTilemap!.SetTilesBlock(sel, bTiles); //clear source area
                            TpLib.InhibitOnTpLibChanged = false;
                            //send ONE OnTpLibChanged event.
                            TpLib.ForceOnTpLibChanged(TpLibChangeType.Deleted, true, Vector3Int.zero, PaintableTilemap);
                        }

                        if (!InGridSelMode &&
                            (CurrentTool == TpPainterTool.Pick || IsPickPhaseOfMove )&&
                            currentMouseGridPosition != TilePlusBase.ImpossibleGridPosition &&
                            TpPainterState.PaintableMap is { Valid: true })
                        {
                            didPick = true;
                            HandleMarqueePick(sel);
                        }

                        Event.current.Use();
                        GUI.changed = true;

                        cantPaintHereInterlock  = false;
                        IgnoreNextHierarchyChange = false;
                        brushOpInProgress       = true; 
                        dragLockX               = dragLockY = false;
                        GUIUtility.hotControl = didPaint || didPick
                                                    ? 0
                                                    : ControlId;

                    }
                }

                marqueeDragActiveLastPass = false;
            }
            return didPick;
        }

        
        private  void HandleMarqueePick(BoundsInt sel) 
        {
            if(sel.size.x == 0 || sel.size.y == 0)
                return;
            
            var output = new List<TileCell>();
            var map    = PaintableTilemap;
            foreach (var p in sel.allPositionsWithin)
            {
                 
                var tb = map!.GetTile(p);
                
                var b = new GridBrush.BrushCell { tile = tb };
                // ReSharper disable once Unity.NoNullPatternMatching
                if (tb != null && tb is Tile )
                {
                    b.color  = map.GetColor(p);
                    b.matrix = map.GetTransformMatrix(p);
                }
                else
                {
                    b.color = Color.white;
                    b.matrix = Matrix4x4.identity;
                }

                var local = p - sel.min;
                output.Add(new TileCell(b, local));
            }
            
            var num = output.Count;
            if (num == 0)
            {
                if(Informational)
                    TpLog("Empty selection! Operation terminated.");
                return;
            }
            var ttd = output.Count > 1
                          ? new TpPainterClipboard(output.ToArray(),new BoundsInt(Vector3Int.zero, sel.size),sel,true) 
                          : new TpPainterClipboard(output[0].TileBase, output[0].m_Position, PaintableTilemap);
            if (Event.current.control)
            {
                var obj = ScriptableObject.CreateInstance<TileCellsWrapper>();
                // ReSharper disable once InvertIf
                if (obj != null)
                {
                    obj.Cells                             = ttd.Cells;
                    obj.m_Bounds                          = ttd.BoundsInt;
                    obj.m_MultipleTilesSelectionBoundsInt = ttd.MultipleTilesSelectionBoundsInt;
                    obj.Pivot                             = ttd.Pivot;
                    Win!.AddToFavorites(new Object[]{ obj });
                }
            }
            else
                Win!.SelectBrushInspectorTarget(ttd, TilePlusPainterConfig.TpPainterPickToPaint && TpPainterState.CurrentTool != TpPainterTool.Move);
        }
        
        #endregion
        
        #region repaint

        /// <summary>
        /// Handles window repaint. Text drawing.
        /// </summary>
        /// <param name="target">current tilemap</param>
        /// <param name="targetGridLayout">the tilemap's grid layout.</param>
        /// <param name = "currentTool" >The current painter tool</param>
        /// <param name = "moveSeqState" >current move sequence state</param>
        /// <param name = "globalMode" >Painter global mode (paint/edit)</param>
        /// <param name = "noPaint" >True if this location isn't paintable</param>
        /// <param name = "onGrid" >True if Fab authoring is on and pointer is on the sGrid.</param>
        /// <param name = "draggableAction" >TRUE if the Action button can use Drag. (Paint, Erase, Move)</param>
        private  void HandleRepaint(Tilemap                     target,
                                          bool                        noPaint,
                                          GridLayout                  targetGridLayout,
                                          TpPainterTool               currentTool,
                                          TpPainterMoveSequenceStates moveSeqState,
                                          GlobalMode                  globalMode,
                                          bool                        onGrid,
                                          bool                        draggableAction)
        {
            var cellSize      = targetGridLayout.cellSize;
            var mouseWorldPos = currentPaintingTilemapHasOriginZero ? target.GetCellCenterWorld(currentMouseGridPosition) : currentMouseLocalPosition;
            var hSize         = HandleUtility.GetHandleSize(new Vector3(currentMouseLocalPosition.x, currentMouseLocalPosition.y, 0.1f));
            
            if ( !Mathf.Approximately(hSize, lastSizeAdjustment))
            {
                var adjHsize = (1 / hSize) * 3;
                adjHsize = Mathf.Min(adjHsize, 10f);
                adjHsize = Mathf.Max(adjHsize, 1f);
                //Debug.Log($"Hs {hSize} ahs { adjHsize}");
                ReinitializeGuiContent(adjHsize );
            }

            lastSizeAdjustment = hSize;

            const float fx                  = 2f;
            const float fy                  = 1.0f;
            const float factorAdj           = 0.2f;
            var         labelsPosition      = mouseWorldPos - (new Vector3(+(cellSize.x / fx), cellSize.y * fy) );
            var         positionLabelOffset = mouseWorldPos + new Vector3(-(cellSize.x / fx), cellSize.y * fy);
            var         factor              = hSize * factorAdj;
            var         hOffset             = new Vector3(factor, factor, 0);
            labelsPosition        += hOffset;
            positionLabelOffset   += hOffset;
            labelsPosition.z      =  0.1f;
            positionLabelOffset.z =  0.1f;
           
            if (PreviewInhibited)
            {
                Handles.Label(labelsPosition, "!2D", positionTextGuiStyle);
                return;
            }
            
            if (marqueeDragActiveLastPass)
            {
                Handles.Label(positionLabelOffset, globalMode == GlobalMode.GridSelView
                                                       ? "Create Selection"
                                                       : ToolNames(currentTool), positionTextGuiStyle);
                var size     = marqueeDragBounds.size;
                var pos      = marqueeDragBounds.position;
                var marqText = $" [XY {pos.x}:{pos.y}]\n Size:[{size.x}*{size.y}]";
                Handles.Label(labelsPosition, marqText, positionTextGuiStyle);
                return;
            }
            
            else if(globalMode == GlobalMode.GridSelView && !dragClutchShortcutActive)
            {
                Handles.Label(labelsPosition, marqueeDragShortcutKey, positionTextGuiStyle);
                return;
            }

            if (noPaint)
            {
                Handles.Label(positionLabelOffset, "Locked/Prefab", positionTextGuiStyle);
                return;
            }

            if (globalMode == GlobalMode.PaintingView && currentTool == TpPainterTool.Paint && TpPainterState.Clipboard is { IsTilePlusBase: true } && TpPainterState.Clipboard.Tile != null)
                cantPaintHereInterlock = AnyRestrictionsForThisTile((ITilePlus)TpPainterState.Clipboard.Tile, target.name, TpPainterState.Clipboard.IsFromBundleAsPalette);
            else
                cantPaintHereInterlock = false;
            
            var text = globalMode != GlobalMode.GridSelView
                           ? onGrid
                                 ? $"<G>{ToolNames(currentTool)}"
                                 : $"{ToolNames(currentTool)}{(draggableAction?":D":string.Empty)}"
                           : //allows for multiple langs
                           "Create Grid Selection";
            if (currentTool == TpPainterTool.Move && moveSeqState == TpPainterMoveSequenceStates.Pick)
                text = $"{text}-Picking";

            if (TilePlusConfig.instance.ShowBrushPosition)
                text = $"{text} [{currentMouseGridPosition.x.ToString()}:{currentMouseGridPosition.y.ToString()}]";
            
            Handles.Label(positionLabelOffset, text, onGrid ? positionTextAltGuiStyle : positionTextGuiStyle);

            var previewIsPlaceholderTile = PreviewIsPlaceholderTile;
            // ReSharper disable once InvertIf
            if (TpPainterState.Clipboard is {Valid: true}
                && ((currentTool == TpPainterTool.Move && moveSeqState == TpPainterMoveSequenceStates.Paint)
                    || currentTool == TpPainterTool.Paint))
            {
                if (cantPaintHereInterlock)
                    Handles.Label(labelsPosition+hOffset, "Can't paint here", positionTextGuiStyle);
                else if (previewIsPlaceholderTile)
                    Handles.Label(labelsPosition+hOffset, "Tile sprite is hidden", positionTextGuiStyle);

                if (target.GetTile<TilePlusBase>(currentMouseGridPosition) != null)
                {
                    var overwriteLabelOffset = labelsPosition;
                    if (previewIsPlaceholderTile)
                        overwriteLabelOffset.y -= 0.25f;
                    var noOvr = TilePlusConfig.instance.NoOverwriteFromPalette;
                    Handles.Label(overwriteLabelOffset, noOvr ^ AllowOverwriteOrIgnoreMap
                                                            ? "Protected"
                                                            : "Will Overwrite", positionTextGuiStyle);
                }

                if (!AllowOverwriteOrIgnoreMap)
                    return;
                var center = new BoundsInt(currentMouseGridPosition, Vector3Int.one).center + target.transform.position;
                var len    = Vector2.one / 2f;
                var end    = new Vector2(center.x - len.x, center.y - len.y);
                var start  = new Vector2(center.x + len.x, center.y + len.y);
                TpLibEditor.TilemapLine(start, end, Color.black, 0);
            }
            
            if (globalMode == GlobalMode.PaintingView && currentTool == TpPainterTool.Pick)
            {
                /* if the pin button is 'true' then pick->clipboard->paint.
                 * if the pin button is 'false' then pick->clipboard.
                 * if the SHIFT key is depressed then this intent is reversed.
                 * if the CTRL key is depressed then pick->Favorites. Pin button ignored.
                 * if SHIFT and CTRL both depressed then no pick action is to Favorites.
                 * -- below, pickIntent is false for pick->clipboard, true for pick->clipboard->paint.
                 */
                if (Event.current.control)
                    Handles.Label(/*mouseWorldPos - */labelsPosition, "Pick=>Favorites", pickMsgGuiStyle);
                else
                {
                    var pickIntent = TilePlusPainterConfig.TpPainterPickToPaint ^ Event.current.shift;
                    Handles.Label(labelsPosition, pickIntent
                                                      ? "Pick=>Clipboard=>Paint"
                                                      : "Pick=>Clipboard",
                                                      pickMsgGuiStyle);
                }
            }
        }

        #endregion
        
        
        #region utils

        internal void Reset()
        {
            ResetState();
        }

        internal  void ResetState(bool reinitGuiContent = true, bool clearPreview = true)
        {
            lastMouseGridPosition     = TilePlusBase.ImpossibleGridPosition;
            currentMouseGridPosition  = TilePlusBase.ImpossibleGridPosition;
            marqueeDragActiveLastPass = false;
            cantPaintHereInterlock    = false;
            IgnoreNextHierarchyChange   = false;
            brushOpInProgress         = false;
            dragLockX                 = dragLockY = false;
            
            if(clearPreview)
                ClearPreview();
            if (!reinitGuiContent)
                return;
            ReinitializeGuiContent();

        }

        /// <summary>
        /// (Re) init the GUIContent for the scene view cursor overlays
        /// </summary>
        internal  void ReinitializeGuiContent(float sizeAdj = 1f)
        {
            marqueeDragShortcutKey    = $"Use {TpPainterShortCuts.MarqueeDragTooltip} + Drag";
            positionTextGuiStyle    = new GUIStyle { normal = { textColor = TilePlusPainterConfig.TpPainterSceneTextColor }, fontStyle = FontStyle.Bold, fontSize          = (int)(TilePlusConfig.instance.BrushPositionFontSize * sizeAdj) };
            positionTextAltGuiStyle = new GUIStyle { normal = { textColor = TilePlusPainterConfig.TpPainterSceneTextColor }, fontStyle = FontStyle.BoldAndItalic, fontSize = (int)(TilePlusConfig.instance.BrushPositionFontSize * sizeAdj) };
            pickMsgGuiStyle         = new GUIStyle { normal = { textColor = TilePlusPainterConfig.TpPainterSceneTextColor }, fontStyle = FontStyle.Bold, fontSize          = (int)(TilePlusConfig.instance.BrushPositionFontSize * 0.75f * sizeAdj) };
        }

        /// <summary>
        /// Register a Unity undo for a tilemap.
        /// </summary>
        /// <param name="map">tilemap.</param>
        /// <param name="description">Undo description.</param>
        /// <param name = "useTransform" >Set true when prefabs added to a map's transform as children.</param>
        private  void RegisterUndo(Tilemap map, string description, bool useTransform = false)
        {
            if (useTransform)
                Undo.RegisterCompleteObjectUndo(new Object[] { map, map.transform }, $"Tile+Painter[Transform]: {description}");
            else
                Undo.RegisterCompleteObjectUndo(new Object[] { map, map.gameObject }, $"Tile+Painter[GameObject]: {description}");
        }

        /// <summary>
        /// Is the mouse over the Scene View and
        /// position OK and Current Tool requires SceneView activity?
        /// </summary>
        private  bool MouseOverSceneView()
        {
            var over        = EditorWindow.mouseOverWindow;
            var currentTool = TpPainterState.CurrentTool;
            return over != null && over.GetType() == typeof(SceneView)
                                && currentMouseGridPosition != TilePlusBase.ImpossibleGridPosition
                                && currentTool != TpPainterTool.Help && currentTool != TpPainterTool.Settings && currentTool != TpPainterTool.None;
        }

        private  bool PositionAlignedWithSgrid(Vector3Int position)
        {
            var size             = TilePlusPainterConfig.PainterFabAuthoringChunkSize;
            var relativePosition = position - TilePlusPainterConfig.FabAuthWorldOrigin;
            return relativePosition.x % size == 0 && relativePosition.y % size == 0;
        }

        private  Vector3Int AlignToGrid(Vector3Int position)
        {
            var relPos = position - TilePlusPainterConfig.FabAuthWorldOrigin;
            var size   = TilePlusPainterConfig.PainterFabAuthoringChunkSize;

            var diffX = relPos.x % size;
            var diffY = relPos.y % size;

            return new Vector3Int(relPos.x - diffX, relPos.y - diffY, position.z);
        }

        /// <summary>
        /// Test if a TilePlus tile has painting restrictions.
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="mapName"></param>
        /// <param name = "ignoreLock" >Ignore the locked state - added so locked tiles from a Bundle can be painted.</param>
        /// <returns>true if this tile can't paint on this map</returns>
        private  bool AnyRestrictionsForThisTile(ITilePlus tile, string mapName, bool ignoreLock)
        {
            if (!ignoreLock && tile.IsLocked)
            {
                TpLog("Can't paint Locked tiles.");
                return true; 
            }

            
            var restrictions = AllowOverwriteOrIgnoreMap
                                   ? null
                                   : tile.PaintMaskList;
            //if the paintmasklist has restrictions, separate them into included and excluded maps.
            if (restrictions is { Count: > 0 })
            {
                TpLibEditor.ParsePaintMask(restrictions, ref includedMaps, ref excludedMaps);

                //see if this tilemap is paintable for this tile instance
                var noPaint        = false;
                var cleanLcMapName = mapName.ToLowerInvariant();
                if (excludedMaps.Count > 0 && excludedMaps.Contains(cleanLcMapName))
                    noPaint = true;
                if (!noPaint && includedMaps.Count > 0 && !includedMaps.Contains(cleanLcMapName))
                    noPaint = true;
                return noPaint;
            }
            return false;
        }

        /// <summary>
        /// Deletes the tile  with confirmation.
        /// </summary>
        /// <param name="tile">tile to delete</param>
        /// <param name="parentMap">tile's parent map.</param>
        /// <param name="pos">tile position.</param>
        /// <param name="skipConfirm">skip confirmation?</param>
        private  void DeleteTileActionWithConfirm(TileBase? tile, Tilemap? parentMap, Vector3Int pos, bool skipConfirm)
        {
            if (tile == null || parentMap == null || pos == TilePlusBase.ImpossibleGridPosition)
            {
                TpLogError("Invalid parameter: cancelling deletion");
                return;
            }

            if (!skipConfirm && TilePlusConfig.instance.ConfirmDeleteTile)
            {
                var doDelete = EditorUtility.DisplayDialog("Delete this tile?", "Do you really want to delete this tile?", "OK", "NOPE");
                if (!doDelete)
                    return;
            }

            // ReSharper disable once Unity.NoNullPatternMatching
            if (tile is ITilePlus)
            {
                Undo.RegisterCompleteObjectUndo(new Object[] { parentMap, parentMap.gameObject }, $"T+P: delete TilePlus tile on Tilemap [{parentMap.name}] at [{currentMouseGridPosition}]");
                DeleteTile(parentMap, pos);
            }
            else
            {
                RegisterUndo(parentMap, $"Delete Tile on Tilemap [{parentMap.name}] at [{currentMouseGridPosition}] ");
                parentMap.SetTile(currentMouseGridPosition, null);
            }
        }

        /// <summary>
        /// Clone a TPT tile
        /// </summary>
        /// <param name="original">tpb instance to clone</param>
        /// <returns>clone or null</returns>
        private  TilePlusBase? CloneTpb(TilePlusBase? original)
        {
            if (original == null)
                return null;
            var reClone = Object.Instantiate(original); //clone the copied tile; we need a new instance.
            if (reClone == null)
                return null;
            reClone.ResetState(TileResetOperation.MakeCopy); //reset state variables like grid position etc.
            //note that ResetState nulls the GUID so we have a brief chance to change it before placing the tile.
            reClone.TileGuidBytes = Guid.NewGuid().ToByteArray();
            return reClone;
        }
        
        
        /// <summary>
        /// Test to see if tile flags are set properly when using color/transform mods.
        /// </summary>
        /// <param name="clipbd"></param>
        /// <returns>true if unpaintable due to modifier/tileflag mismatch</returns>
        private  bool TileFlagsInappropriate(TpPainterClipboard clipbd)
        {
            if (!clipbd.AnyModifications)
                return false;
            
            //for a single tile
            if (clipbd.IsTile)
            {
                // ReSharper disable once Unity.NoNullPatternMatching
                if (clipbd.Tile == null || clipbd.Tile is not Tile t)
                    return false;
                if (!IsInappropriate(t))
                    return false;
                if (Informational)
                    TpLib.TpLog(InappropriateTile);
                TpLib.DelayedCallback(null, () => { EditorUtility.DisplayDialog("!!Tile Flags (single)!!", InappropriateTile, "Continue"); }, "T+P:SV:Inapprop flags for modded clipbd.");
                return true;
            }

            if (!clipbd.IsMultiple)
                return false;
            
            //else check all the cells.            
            if (clipbd.Cells == null)
                return true; //shouldn't happen.

            var count = clipbd.Cells.Count(cell => cell.TileBase != null
                                                   // ReSharper disable once Unity.NoNullPatternMatching
                                                   && cell.TileBase is Tile t
                                                   && IsInappropriate(t));
            if (count == 0)
                return false;

            

            TpLib.DelayedCallback(null,
                                  Callback, //nb using local function avoids capture of Clipboard ie avoid mem leak
                                  "T+P:SV:Inapprop flags for modded clipbd.");
            return true;

            bool IsInappropriate(Tile t)
            {
                var transLocked = clipbd.TransformModified && ((t.flags & TileFlags.LockTransform) != 0);
                var colorLocked = clipbd.ColorModified && ((t.flags & TileFlags.LockColor) != 0);
                return transLocked || colorLocked;
            }
            
            void Callback()
            {
                EditorUtility.DisplayDialog("!!Tile Flags (multiple)!!", $"Modified set of tiles can't be painted.\n{count} tile{(count > 1 ? "s" : string.Empty)} have the LockColor or LockTransform flags set but modifications to the transform or color were made. This is incompatible.\nPlease see the FAQ: ‘I Can’t Paint Modified Tiles’ in the Painter User Guide.", "Continue");
            }

        }

        

        private const string InappropriateTile = "Modified tile can't be painted.\n" 
                                                 + "A tile has the LockColor or LockTransform flags set but modifications to the transform or color were made. " 
                                                 + "This is incompatible.\nPlease see the FAQ: ‘I Can’t Paint Modified Tiles’ in the Painter User Guide.\n\n"
                                                 + "Tip: click the Eye icon in the Tile's property area in Painter, then use the context menu command 'TilePlus/Change Tile Flags'";


        private  void InappropriateSingleTileTransform()
        {
            TpLib.DelayedCallback(null,
                                  () =>
                                  {
                                      EditorUtility.DisplayDialog("!!Tile Flags (single-t)!!",
                                                                  $"The Lock Transform flag is set for this position. \nPlease see the FAQ: ‘I Can’t Paint Modified Tiles’ in the Painter User Guide.",
                                                                  "Continue");
                                  },
                                  "T+P:SV:Inapprop transform flags, single");
        }

        private  void InappropriateTransformOrColorInMove()
        {
            TpLib.DelayedCallback(null,
                                  () =>
                                  {
                                      EditorUtility.DisplayDialog("!!Tile Flags (move) !!",
                                                                  $"Lock Transform or Lock Color flags set inappropriately during a move.\nPlease see the FAQ: ‘I Can’t Paint Modified Tiles’ in the Painter User Guide.",
                                                                  "Continue");
                                  },
                                  "T+P:SV:Inapprop transform flags, move");
        }
        
        #endregion
        
        
        #region content
        private  readonly Dictionary<int, string[]> toolNames = new()
                                                                        {
                                                                            {
                                                                                (int)SystemLanguage.English, new[] // strings for English
                                                                                                             {
                                                                                                                 "None", "Paint", "Erase", "Pick", "Move", "RotCW", "RotCCW", "FlipX", "FlipY", "Rst Transform", "Help", "Settings"
                                                                                                             }
                                                                            }
                                                                        };


        private  string ToolNames(TpPainterTool tool)
        {
            var index = (int)tool;
            var lang  = (int)Application.systemLanguage;
            if (!toolNames.TryGetValue(lang, out var arr))
                return "?????";
            
            return index >= arr.Length
                       ? "?????"
                       : arr[index];

        }
        #endregion
    }
}
