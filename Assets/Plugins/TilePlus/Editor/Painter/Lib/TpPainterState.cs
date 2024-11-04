// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 05-31-2024
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 05-31-2024
// ***********************************************************************
// <copyright file="TPainterState.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

#nullable enable

namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// Painter runtime state info. Note this ScriptableSingeton not saved in filesystem. 
    /// </summary>
    public class TpPainterState : ScriptableSingleton<TpPainterState>
    {
        #region subscriptions
        /// <summary>
        /// Subscribe to this to get notified when painter's modes change.
        /// </summary>
        public static event Action<GlobalMode, TpPainterTool, TpPainterMoveSequenceStates>? OnPainterModeChange;
        #endregion
        
        #region events
        private void OnEnable()
        {
            if(TpLibEditor.Informational)
                TpLib.TpLog("TpPainterState Scriptable Singleton initialized");
        }

        private void OnDisable()
        {
            if(TpLibEditor.Informational)
                TpLib.TpLog("TpPainterState Scriptable Singleton released");
        }
        
        #endregion
        
        
        

        #region properties
       
        [SerializeField]
        private GlobalMode m_GlobalMode = GlobalMode.PaintingView;
        //overall mode: Painting or Editing
        /// <summary>
        /// The global mode indicates Painting or Editing modes
        /// </summary>
        public static GlobalMode GlobalMode => instance.m_GlobalMode;

        /// <summary>
        /// Is Painter in Paint mode?
        /// </summary>
        public static bool InPaintMode => instance.m_GlobalMode == GlobalMode.PaintingView;

        /// <summary>
        /// Is Painter in Edit mode?
        /// </summary>
        public static bool InEditMode => instance.m_GlobalMode == GlobalMode.EditingView;

        /// <summary>
        /// Is Painter in Grid Selection mode.
        /// </summary>
        public static bool InGridSelMode => instance.m_GlobalMode == GlobalMode.GridSelView;
        
        [SerializeField]
        private GlobalMode m_PreviousGlobalMode = GlobalMode.EditingView;
        /// <summary>
        /// The previous global mode
        /// </summary>
        public static GlobalMode PreviousGlobalMode => instance.m_PreviousGlobalMode;

        [SerializeField]
        private TpPainterTool m_CurrentTool   = TpPainterTool.None;
        /// <summary>
        /// The current tool
        /// </summary>
        public static TpPainterTool CurrentTool => instance.m_CurrentTool;
        
        /// <summary>
        /// The previous tool
        /// </summary>
        [SerializeField]
        private TpPainterTool m_PreviousTool  = TpPainterTool.None;

        /// <summary>
        /// The previous tool
        /// </summary>
        public static TpPainterTool PreviousTool => instance.m_PreviousTool;

        /// <summary>
        /// The tool to restore
        /// </summary>
        [SerializeField]
        private TpPainterTool m_ToolToRestore = TpPainterTool.None;

        /// <summary>
        /// The tool to restore
        /// </summary>
        public static TpPainterTool ToolToRestore => instance.m_ToolToRestore;

        //operation targets
        /// <summary>
        /// Which tilemap to paint or edit
        /// </summary>
        [SerializeField]
        private PaintableMap? m_PaintableMap; //tilemap to use

        /// <summary>
        /// Which tilemap to paint or edit
        /// </summary>
        internal static PaintableMap? PaintableMap
        {
            get => instance.m_PaintableMap;
            set => instance.m_PaintableMap = value;
        }

        /// <summary>
        /// Which Source (Palette,Tilefab|Bundle, Favorites) is selected
        /// </summary>
        [SerializeField]
        private PaintableObject? m_PaintableObject;

        /// <summary>
        /// Which Source (Palette,Tilefab|Bundle, Favorites) is selected
        /// </summary>
        internal static PaintableObject? PaintableObject
        {
            get => instance.m_PaintableObject;
            set => instance.m_PaintableObject = value;
        }
        
        /// <summary>
        /// The sequence state for Move operations
        /// </summary>
        [SerializeField]
        private TpPainterMoveSequenceStates m_TpPainterMoveSequenceState;
        /// <summary>
        /// What is the Move sequence state?
        /// </summary>
        /// <value>A value from the TpPainterMoveSequenceStates enumeration</value>
        internal static TpPainterMoveSequenceStates TpPainterMoveSequenceState
        {
            get => instance.m_TpPainterMoveSequenceState;
            set
            { 
                if (instance.m_TpPainterMoveSequenceState == TpPainterMoveSequenceStates.Pick && value == TpPainterMoveSequenceStates.Paint)
                    instance.m_MovePickSequenceTilemap = instance.m_PaintableMap?.TargetTilemap;
                else
                    instance.m_MovePickSequenceTilemap = null;
                instance.m_TpPainterMoveSequenceState = value;
                OnPainterModeChange?.Invoke(GlobalMode, CurrentTool, TpPainterMoveSequenceState);
            }
        }

        [SerializeField]
        private Tilemap? m_MovePickSequenceTilemap;

        /// <summary>
        /// When a move is in the pick phase, this is the Tilemap used for the pick operation.
        /// </summary>
        /// <value>tilemap.</value>
        internal static Tilemap? MovePickSequenceTilemap
        {
            get => instance.m_MovePickSequenceTilemap;
            set => instance.m_MovePickSequenceTilemap = value;
        }


        /// <summary>
        /// Is the tilemap selection valid?
        /// </summary>
        /// <value>T/F</value>
        internal static bool ValidTilemapSelection => PaintableMap is { Valid: true };
        /// <summary>
        /// Is painting allowed?
        /// </summary>
        /// <value>true if painting allowed, which means a valid tilemap to paint has been set up
        /// and a valid clipboard item exists, ready to paint..</value>
        internal static bool PaintingAllowed
        {
            get
            {
                //note that if a tile is picked this implies that one had selected a tilemap and that s_PaletteTileTarget != null
                var wasPicked           = Clipboard is { WasPickedTile: true, Valid: true };
                var isValid             = Clipboard is { Valid        : true };
                var painterIsActiveTool = ToolManager.activeToolType == typeof(TilePlusPainterTool);
                //next line determines if we have a valid place to paint. This is overridden if the thing to paint is a TileFab since the Tilemap selection in the window is irrelevant.
                var hasPaintTarget = PaintableMap is
                                     {
                                         Valid: true
                                     }
                                                    ////TileFabs don't need a Tilemap selection.
                                     || (isValid && painterIsActiveTool
                                                 && (Clipboard is not null
                                                 && Clipboard.ItemVariety == TpPainterClipboard.Variety.TileFabItem));   
                return hasPaintTarget && (wasPicked || isValid);        //OK to paint if we have a paint target AND the tile was Picked or is just Valid
            }
        }
        

        /// <summary>
        /// Get a clone of the unmodified Clipboard.
        /// </summary>
        internal static TpPainterClipboard? UnmodifiedClipboard => instance.m_DoNotUseClipboardRestore?.CloneInstance() ?? null;
        
        /// <summary>
        /// Does the current tool have any Tilemap effect?
        /// </summary>
        internal static bool CurrentToolHasTilemapEffect => 
            instance.m_CurrentTool != TpPainterTool.Help
            && instance.m_CurrentTool != TpPainterTool.Settings
            && instance.m_CurrentTool != TpPainterTool.None;
        
        
        
        /// <summary>
        /// Backup of each new clipboard instance: used for Restore (undo).
        /// DO NOT access this outside of the Clipboard property.
        /// </summary>
        [SerializeField]
        private TpPainterClipboard? m_DoNotUseClipboardRestore;
        /// <summary>
        /// DO NOT access this outside of the Clipboard property.
        /// </summary>
        [SerializeField]
        private TpPainterClipboard? m_DoNotUse_Clipboard_Backing;
        /// <summary>
        /// the selected or picked tile to paint/move (Paint mode) or the selected/picked tile to inspect (Edit mode).
        /// </summary>
        /// <remarks>This object is referred to as the CLIPBOARD in most of the docs.
        /// A backup copy is maintained for recovery.  NEVER EVER access the backing fields.</remarks>
        internal static TpPainterClipboard? Clipboard
        {
            get => instance.m_DoNotUse_Clipboard_Backing;
            set
            {
                instance.m_DoNotUse_Clipboard_Backing = value ?? new TpPainterClipboard();
                instance.m_DoNotUseClipboardRestore = value == null
                                                          ? new TpPainterClipboard()
                                                          : instance.m_DoNotUse_Clipboard_Backing
                                                                    .CloneInstance(); //backup copy.
                if (value != null)
                    return;
                // ReSharper disable once Unity.NoNullPropagation
                TilePlusPainterWindow.RawInstance?.SetEmptyTabBar();
            }
        }
        
        #endregion
        
        #region utils
        
        internal void ResetInstance()
        {
            m_CurrentTool                = TpPainterTool.None;
            m_GlobalMode                 = GlobalMode.PaintingView;
            m_PreviousGlobalMode         = GlobalMode.EditingView;
            m_PreviousTool               = TpPainterTool.None;
            m_ToolToRestore              = TpPainterTool.None;
            m_DoNotUse_Clipboard_Backing = new TpPainterClipboard();
            m_DoNotUseClipboardRestore   = new TpPainterClipboard();
            m_PaintableMap               = null;
            m_PaintableObject            = null;
        }
        
        /// <summary>
        /// Adds the clipboard item to Favorites. ALWAYS uses the backup clipboard
        /// </summary>
        internal static void AddClipboardItemToFavorites()
        {
            var clip = instance.m_DoNotUseClipboardRestore;
            if(clip is not { Valid: true })
                return;
            Object? o = null;
            switch (clip.ItemVariety)
            {
                case TpPainterClipboard.Variety.TileItem when clip.Tile != null:
                    if (clip is { IsTilePlusBase: true, IsClonedTilePlusBase: true })
                    {
                        TilePlusPainterWindow.RawInstance!.ShowNotification(new GUIContent("Can't add Cloned tiles to Favorites!!"));
                        return;
                    }
                    o = (Object) clip.Tile;
                    break;
                
                case TpPainterClipboard.Variety.BundleItem when clip.Bundle != null:
                    o = (Object)clip.Bundle;
                    break;
                
                case TpPainterClipboard.Variety.TileFabItem when clip.TileFab != null:
                    o = (Object)clip.TileFab;
                    break;
                
                case TpPainterClipboard.Variety.MultipleTilesItem  :
                    var so = ScriptableObject.CreateInstance<TileCellsWrapper>();
                    if (so != null)
                    {
                        so.Cells                             = clip.Cells;
                        so.m_Bounds                          = clip.BoundsInt;
                        so.m_MultipleTilesSelectionBoundsInt = clip.MultipleTilesSelectionBoundsInt;
                        so.Icon                              = clip.Icon;
                        so.Pivot                             = clip.Pivot;
                        so.m_CellsModified                     = clip.CellsModified;
                    }
                    o = so;
                    break;
                
                case TpPainterClipboard.Variety.PrefabItem  when clip.Prefab != null:
                    o = (Object)clip.Prefab;
                    break;

                case TpPainterClipboard.Variety.EmptyItem:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            if(o != null)
                TilePlusPainterWindow.RawInstance!.AddToFavorites(new[] { o });
        }



        /// <summary>
        /// Restores the clipboard after editing (eg, rotations, flips etc)
        /// </summary>
        internal static void RestoreClipboard()
        {
            if (instance.m_DoNotUseClipboardRestore == null)
            {
                if(TpLibEditor.Errors)
                    TpLib.TpLogError("Clipboard restore cache was null??!!");
                return;
            }

            instance.m_DoNotUse_Clipboard_Backing = instance.m_DoNotUseClipboardRestore.CloneInstance();
            instance.m_DoNotUse_Clipboard_Backing.SetupGridBrush();
            TilePlusPainterWindow.RawInstance!.SelectBrushInspectorTarget(Clipboard,false,true);
        }
        #endregion
        
        #region access

        /// <summary>
        /// Set a new Global Mode, specifying the previous mode as well
        /// </summary>
        /// <param name="current">Value from GlobalMode enum</param>
        /// <param name="previous">Value from GlobalMode enum</param>
        internal static void SetGlobalMode(GlobalMode current, GlobalMode previous)
        {
            instance.m_GlobalMode         = current;
            instance.m_PreviousGlobalMode = previous;
            OnPainterModeChange?.Invoke(GlobalMode, CurrentTool, TpPainterMoveSequenceState);
        }

        /// <summary>
        /// Set a new Global Mode, push the current => previous
        /// </summary>
        /// <param name="newMode">Value from GlobalMode enum</param>
        internal static void SetGlobalModeWithPush(GlobalMode newMode)
        {
            instance.m_PreviousGlobalMode = instance.m_GlobalMode;
            instance.m_GlobalMode         = newMode;
            OnPainterModeChange?.Invoke(GlobalMode, CurrentTool, TpPainterMoveSequenceState);
        }

        /// <summary>
        /// Set just the previous global mode
        /// </summary>
        /// <param name="prevGlobalMode">Value from GlobalMode enum</param>
        internal static void SetPreviousGlobalMode(GlobalMode prevGlobalMode)
        {
            instance.m_PreviousGlobalMode = prevGlobalMode;
        }

        /// <summary>
        /// Set the current Painter tool, specifying previous tool, with optional restoration tool.
        /// </summary>
        /// <param name="current">Value from GlobalMode enum</param>
        /// <param name="previous">Value from GlobalMode enum</param>
        /// <param name="toRestore">Value from GlobalMode enum</param>
        internal static void SetTools(TpPainterTool current, TpPainterTool previous, TpPainterTool toRestore = TpPainterTool.None)
        {
            instance.m_CurrentTool   = current;
            instance.m_PreviousTool  = previous;
            instance.m_ToolToRestore = toRestore;
            OnPainterModeChange?.Invoke(GlobalMode, CurrentTool, TpPainterMoveSequenceState);
        }

        /// <summary>
        /// Make the current tool -> previous
        /// </summary>
        internal static void PushCurrentToolToPrevious()
        {
            instance.m_PreviousTool = instance.m_CurrentTool;
        }

        /// <summary>
        /// Make the current tool the tool to restore
        /// </summary>
        internal static void PushCurrentToolAsRestorationTool()
        {
            instance.m_ToolToRestore = instance.m_CurrentTool;
        }

        /// <summary>
        /// Set just the current tool
        /// </summary>
        /// <param name="tool">Value from GlobalMode enum</param>
        internal static void SetCurrentToolOnly(TpPainterTool tool)
        {
            instance.m_CurrentTool = tool;
            OnPainterModeChange?.Invoke(GlobalMode, CurrentTool, TpPainterMoveSequenceState);
        }
        
        #endregion
        
    }
}
