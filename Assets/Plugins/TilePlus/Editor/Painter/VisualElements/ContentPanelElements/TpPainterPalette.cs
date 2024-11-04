// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 01-05-2024
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 1-05-2024
// ***********************************************************************
// <copyright file="TpPainterPalette.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
using System;
// ReSharper disable once RedundantUsingDirective
using System.Diagnostics.CodeAnalysis; //don't remove: required for certain states of conditional compilation.
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

#nullable enable
namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// This VE adapts the Unity tile palette visual element (TilePaletteClipboardElement)
    /// to Tile+Painter. EXPERIMENTAL
    /// NOTE: in UNITY6.005 ish a change was made that affects this module. See conditional compilation for more info,
    /// 
    /// </summary>
    public class TpPainterPalette : VisualElement
    {
        //the UTE palette "clipboard"
        //don't make readonly
        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private TilePaletteClipboardElement pal;
        //the callback used when a pick is made on the palette Grid
        private readonly Action<Tilemap, GridBrush.BrushCell[], BoundsInt, Vector3Int, GridBrush> callback;

        #if !UNITY_6000_0_OR_NEWER
        //Stored reflection info for the TilePaletteClipboardElement instance 
        private FieldInfo? activePickFi;

        private FieldInfo? activePivotFi;

        private EventInfo? onBrushPickedEventInfo;
        private object?    clipboardObject;
        #endif
        
        private readonly TpSpacer indicatorLight;
        private bool isActive;

        
        /// <summary>
        /// Control enabling/disabling the palette
        /// </summary>
        public bool IsActive
        {
            get => isActive;
            set
            {
                indicatorLight.style.backgroundColor = value
                                                   ? Color.red
                                                   : Color.clear;
                isActive = value;
                if (!isActive)
                    return;
                TestBrush();
                ClearBrush();
            }
        }

        
        /// <summary>
        /// Constructor for the palette panel
        /// </summary>
        /// <param name="callback"></param>
        public TpPainterPalette(Action<Tilemap, GridBrush.BrushCell[], BoundsInt, Vector3Int, GridBrush> callback)
        {
            this.callback    = callback;
            style.flexGrow   = 1;
            style.flexShrink = 1;
            
            AddToClassList("tpp-palette-wrapper");
            
            Add(indicatorLight = new TpSpacer(10, 10));
            pal = new TilePaletteClipboardElement() {style = { flexGrow = 1 } };

            #if UNITY_6000_0_OR_NEWER //U6 changed how brush picks in the palette scene report to GridPaintingState.
            //unfortunately they made it worse since can't have two completely independent palettes anymore as 
            //selections made on one reflect on the other. Sigh.

            RegisterPainterInterest(true);
            //RegisterPainterInterest(false);
            pal.SendEvent(new AttachToPanelEvent());
            RepaintGridPaintPaletteWindow();
            GridPaintingState.paletteChanged += GridPaintingStateOnpalettesChanged;
            var brush = GridPaintingState.gridBrush;
            // ReSharper disable once Unity.NoNullPatternMatching
            if (brush != null && brush is GridBrush gridBrush)
                gridBrush.Init(Vector3Int.one);

            void GridPaintingStateOnpalettesChanged(GameObject _)
            {
                if (TpLibEditor.Informational)
                    TpLib.TpLog("T+P: Repaint UTE palette on Palette change");
                RepaintGridPaintPaletteWindow();
            }
            #endif
            Add(pal);
            TestBrush();
            
            
            
            //this can't be done in ctor since the palette isn't completely set up and the reflection process will fail.
            #if !UNITY_6000_0_OR_NEWER
            TpLib.DelayedCallback(TilePlusPainterWindow.RawInstance, InitPreUnity6,"T+P:Palette init pre unity6",1000);
            #endif
        }

       
        #if UNITY_6000_0_OR_NEWER
        /// <summary>
        /// Register this element with GridPaintingState. Required in U6
        /// </summary>
        /// <param name="register">true to register, false to deregister</param>
        internal void RegisterPainterInterest(bool register)
        {
            
            //need to do this so that PaintableSceneViewGrid instance is initialized within GridPaintingState.
            var typ    = typeof(GridPaintingState);
            if (register)
            {
                pal.onBrushPicked += Handler;
                var method1 = typ.GetMethod("RegisterPainterInterest", BindingFlags.Static | BindingFlags.NonPublic);
                if (method1 != null)
                {
                    method1.Invoke(null, new object[] { this });
                    if (TpLibEditor.Informational)
                        TpLib.TpLog("T+P.Palette: RegisteredPainterInterest");
                }
                else
                {
                    TpLib.TpLogError("T+P:Palette - 'GridPaintingState.RegisterPainterInterest' Reflection fail. Report with Unity version info.");
                }
                return;
            }
            pal.onBrushPicked -= Handler;
            var method2 = typ.GetMethod("UnregisterPainterInterest", BindingFlags.Static | BindingFlags.NonPublic);
            if (method2 != null)
            {
                method2.Invoke(null, new object[] { this });
                if (TpLibEditor.Informational)
                    TpLib.TpLog("T+P.Palette: UnRegisteredPainterInterest");
            }
            else
            {
                TpLib.TpLogError("T+P:Palette - 'GridPaintingState.UnRegisterPainterInterest' Reflection fail. Report with Unity version info.");
            }

        }

        private MethodInfo? repaintMethodInfo;
        /// <summary>
        /// Repaint ALL grid palette windows
        /// </summary>
        private void RepaintGridPaintPaletteWindow()
        {
            if (repaintMethodInfo == null)
            {
                var typ    = typeof(GridPaintingState);
                repaintMethodInfo = typ.GetMethod("RepaintGridPaintPaletteWindow", BindingFlags.Static | BindingFlags.NonPublic);
            }

            repaintMethodInfo?.Invoke(null,null);
        }


        
        #endif
      
        #if !UNITY_6000_0_OR_NEWER
        //need to keep this as a field and not an automatic variable.
        private Delegate? handlerDelegate;

        [SuppressMessage("ReSharper", "Unity.NoNullPatternMatching")]
        private void InitPreUnity6()
        {
            //do some reflection trickery to get the Palette to tell us what tile was picked.
            var typ = pal.GetType();

            //get the fields of the Palette
            var paletteFields = typ.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

            //get the FieldInfo for the TilePaletteClipboard hidden internal in the palette
            var clipboardField = paletteFields.FirstOrDefault(m => m.Name == "m_TilePaletteClipboard");
            if (clipboardField == null)
            {
                TpLib.TpLogError("Error setting up TpPainterPalette [A]!");
                return;
            }

            Type? clipboardType=null;
            
            try
            {
                //get the value of that field
                clipboardObject = clipboardField.GetValue(pal);
                //get the clipboard type info
                clipboardType = clipboardObject.GetType();
            }
            catch (Exception e)
            {
                TpLib.TpLogError($"Exception: {e} : [pal=null? {pal==null}] [cbObject=null? {clipboardObject==null}\nThis is caused by an internal Unity race condition, try again. ");
                return;
            }

            var clipboardInternalFields = clipboardType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            //get the BoundsInt m_ActivePick and Vector3Int m_ActivePivot fields
            activePickFi = clipboardInternalFields.FirstOrDefault(m => m.Name == "m_ActivePick");
            activePivotFi = clipboardInternalFields.FirstOrDefault(m => m.Name == "m_ActivePivot");
            
            //get the onBrushPicked event info
            onBrushPickedEventInfo = clipboardType.GetEvent("onBrushPicked");
            //get the Type of the event
            var onBrushPickedEventHandlerType = onBrushPickedEventInfo.EventHandlerType;
            
            //get the Type of this Visual Element
            var veType = GetType();
            //get the method info for the Handler method in this class
            var handlerMethodInfo = veType.GetMethod("Handler");
            if (handlerMethodInfo == null)
            {
                TpLib.TpLogError("Error setting up TpPainterPalette [B] !");
                return;
            }

            //create a delegate for that method
            handlerDelegate = Delegate.CreateDelegate(onBrushPickedEventHandlerType, this, handlerMethodInfo);
            //get the method info for adding a subscriber to the event
            var eventAddMethod = onBrushPickedEventInfo.GetAddMethod();
            
            //create the argument list for the invocation        
            // ReSharper disable once BuiltInTypeReferenceStyle
            System.Object[] args = { handlerDelegate };
            //finally, add a new subscriber to the event
            eventAddMethod.Invoke(clipboardObject, args);
        }
        #endif
        
        /// <summary>
        /// Handler when the Palette makes a pick
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        // This isn't unused! In U6 it is called directly, pre U6 is called via a delegate added to an event via reflection.
        // DO NOT change access from public.
        // ReSharper disable once MemberCanBePrivate.Global
        public void Handler()
        {
            if(TpPainterState.InPaintMode && TpPainterState.CurrentTool == TpPainterTool.Move)
                return;
            #if UNITY_6000_0_OR_NEWER
            if (TpPainterState.PaintableMap == null)
            {
                Warn();
                ClearBrush();
                ToolManager.RestorePreviousTool();
                return;
            }

            if (!TpPainterState.PaintableMap.Valid)
            {
                Warn();
                ClearBrush();
                ToolManager.RestorePreviousTool();
                return;
            }
            #endif
            indicatorLight.style.backgroundColor = Color.yellow;
            indicatorLight.MarkDirtyRepaint();
            TpLib.DelayedCallback(null, ()=>
                                        {
                                            indicatorLight.style.backgroundColor = Color.red;
                                            indicatorLight.MarkDirtyRepaint();
                                        },"T+P:PalColor-goodselection",500,true);
            
            var paintableObj = TpPainterState.PaintableObject;
            var win          = TilePlusPainterWindow.RawInstance;

            if (win == null || paintableObj == null || paintableObj.Palette == null)
            {
                TpLib.TpLog("invalid Painter window, palette target. or palette");
                return;
            }

            //get the tilemap of the palette in use
            var map  = paintableObj.Palette.GetComponentInChildren<Tilemap>();
            if (map == null)
            {
                TpLib.TpLogError("Tile+Painter: Null Tilemap in Palette!! pick operation failed.");
                return;
            }

            //if selection is a tilemap then can change tool. Will be exception otherwise.
            if (Selection.activeGameObject != null 
                && Selection.activeGameObject.TryGetComponent<Tilemap>(out _)
                && win.MouseOverTpPainter
                && (ToolManager.activeToolType != typeof(TilePlusPainterTool)))
            {
                ToolManager.SetActiveTool(typeof(TilePlusPainterTool));
            }
            
            // brush check
            TestBrush();

            var brush = GridPaintingState.gridBrush as GridBrush;
            if (brush == null)
            {
                TpLib.TpLogError("T+P: Unexpected null brush!");
                return;
            }
            //get the cells
            var cells     = brush.cells;
            //and the bounds/pivot

            #if UNITY_6000_0_OR_NEWER
            var        gridPos = pal.clipboardMouseGridPosition;
            gridPos.z = 0;
            var pos     = new BoundsInt(gridPos,brush.size);
            var pivot   = brush.pivot;
            
            #else
            if (activePickFi == null || activePivotFi == null)
            {
                InitPreUnity6();
                if (activePickFi == null || activePivotFi == null)
                    TpLib.TpLogError("Unrecoverable error in TpPainterPalette. Click the refresh button in the lower left corner of the Painter window to reset.");
            }

            var  pos   = (BoundsInt)activePickFi!.GetValue(clipboardObject);
            var pivot = (Vector3Int)activePivotFi!.GetValue(clipboardObject);
            #endif
            
            
            //exec the callback.
            callback(map,cells,pos,pivot,brush);
            brush.Reset(); 
        }

        
        private void ClearBrush()
        {
            var brush = GridPaintingState.gridBrush as GridBrush;
            if (brush == null)
                return;
            brush.Reset();
            
        }

        #if UNITY_6000_0_OR_NEWER
        private void Warn()
        {
            #pragma warning disable CS8602 // Dereference of a possibly null reference.
            TilePlusPainterWindow.RawInstance.ShowNotification(new GUIContent("Please select a Tilemap first!"));
            #pragma warning restore CS8602 // Dereference of a possibly null reference.
        }
        #endif

        internal void Release()
        {
            #if !UNITY_6000_0_OR_NEWER
            pal.Dispose();
            #endif
            
        }
        
        private void TestBrush()
        {
            var brush = GridPaintingState.gridBrush as GridBrush;
            if (brush != null)
                return;
            // ReSharper disable once Unity.NoNullPatternMatching
            var tpBrush = GridPaintingState.brushes.FirstOrDefault(x => x!=null && x is TilePlusBrush);
            if (tpBrush == null) //if not found, try to get the default GridBrush
            {
                //if no Tile+Brush, get the GridBrush
                // ReSharper disable once Unity.NoNullPatternMatching
                var gridBrush = GridPaintingState.brushes.FirstOrDefault(x => x != null && x is GridBrush);
                if (gridBrush == null)
                { 
                    if(TpLibEditor.Errors)
                        TpLib.TpLogError("TpPainterPalette: Could not find Tile+Brush or stock GridBrush.\nThis is probably because you have the GameObject brush set in the Unity Tile Palette.\nUpdates halted: click the Refresh button on the lower-left corner of the Painter window when this has been resolved.");
                }
                else
                {
                    if(TpLibEditor.Warnings)
                        TpLib.TpLogWarning("T+P: forcing Unity Palette brush to GridBrush");
                    GridPaintingState.gridBrush                       = gridBrush;
                    ClearBrush();
                    TpPainterSceneView.instance.SceneViewNotification = "Palette brush changed: try again!"; //mod make these strings constants
                    TilePlusPainterWindow.RawInstance!.ShowNotification(new GUIContent("Palette brush changed: try again!"));
                }
            }
            else
            {
                if(TpLibEditor.Warnings)
                    TpLib.TpLogWarning("T+P: forcing Unity Palette brush to Tile+Brush");
                GridPaintingState.gridBrush                       = tpBrush;
                ClearBrush();
                TpPainterSceneView.instance.SceneViewNotification = "Palette brush changed: try again!";
                TilePlusPainterWindow.RawInstance!.ShowNotification(new GUIContent("Palette brush changed: try again!"));
            }
        }
    }
}
