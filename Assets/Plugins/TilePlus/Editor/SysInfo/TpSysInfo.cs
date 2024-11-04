// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 01-01-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-27-2022
// ***********************************************************************
// <copyright file="TpSysInfo.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>A simple editor window to show TilePlus Toolkit status</summary>
// ***********************************************************************

using TilePlus.Editor.Painter;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using static TilePlus.Editor.TpPreviewUtility;

// ReSharper disable AnnotateNotNullTypeMember

namespace TilePlus.Editor
{
    /// <summary>
    /// Editor window used to show TilePlus system info
    /// </summary>
    public class TpSysInfo : EditorWindow
    {
        private        TextField content;
        private        string    versionInfo;
        private static TpSysInfo s_Instance;
        
        /// <summary>
        /// Open this editor window
        /// </summary>
        [MenuItem("Tools/TilePlus/System Info", false, 10005)] 
        //[Shortcut("TilePlus/Open System Info", KeyCode.Alpha2, ShortcutModifiers.Alt)]
        public static void ShowWindow()
        {
            var wnd = GetWindow<TpSysInfo>();
            wnd.titleContent = new GUIContent(Si_Text.Stats_Title);
            wnd.minSize      = new Vector2(100, 100);
            s_Instance       = wnd;
        }

        /// <summary>
        /// UIElements CreateGUI callback
        /// </summary>
        public void CreateGUI()
        {
            versionInfo = TpLib.VersionInformation; //this is invariant
            var spacer = new VisualElement
            {
                name = "spacer",
                style = 
                {
                    height = 10,
                    width = 10,
                    minHeight = 10,
                    minWidth = 10
                }
                
            };
            
            rootVisualElement.Add(spacer);
            rootVisualElement.Add(new Label(Si_Text.Stats_OnPlayWarning));
            content = new TextField
                      {
                                               
                          focusable = false, multiline = true, name = "setup-sysinfo-textfield", style =
                          {
                              
                              marginTop = 10,
                              marginBottom = 10,
                              whiteSpace = WhiteSpace.Normal,
                              flexGrow                = 1,
                              flexShrink = 1,
                              //flexWrap = Wrap.Wrap,
                              unityFontStyleAndWeight = FontStyle.Bold
                          },
                          value = SysInfoText()
                      };

            
            #if UNITY_2023_1_OR_NEWER
            content.verticalScrollerVisibility = ScrollerVisibility.Auto;
            #else
            content.SetVerticalScrollerVisibility(ScrollerVisibility.Auto);
            #endif
            
            rootVisualElement.Add(content);
        }

        private void OnInspectorUpdate()
        {
            if(content != null) //can happen after a scripting reload.
                content.value = SysInfoText();
        }

        private long maxMem;

        private string SysInfoText()
        {
            const int limit = 50;
            var mem = Profiler.usedHeapSizeLong;
            if (mem > maxMem)
                maxMem = mem;

            var maxClonesPerUpdate   = TpLib.MaxNumClonesPerUpdate;
            var maxCallbackPerUpdate = TpLib.MaxNumDeferredCallbacksPerUpdate;
            var edUpdateRate         = TpLib.Editor_Refresh_Rate;
            if (string.IsNullOrWhiteSpace(edUpdateRate))
                edUpdateRate = "--Disabled--";

            var zmOn          = TileFabLib.AnyActiveZoneManagers ? "[ON]" :"[OFF]";
            var zmNumRegs     = TileFabLib.RegistrationIndex - 1; //-1 because index starts at 1. 
            var zmTotalChunks = TileFabLib.NumZoneManagerChunks;
            var zmTotalZms    = TileFabLib.NumZoneManagerInstances;
            
            var plugins = "Active TileBase plugins: ";

            //this should not occur, so rescan
            if (!Application.isPlaying && PluginCount == 0)
                ResetPlugins();

            var allPlugins = AllPlugins;
            
            if (allPlugins.Count == 0) //this should not occur, so rescan
                plugins += "None";
            else
            {
                foreach (var p in allPlugins)
                {
                    if(p != null)
                        plugins += $"{p!.name}  ";
                }
            }

            var mods = $"\nActive Mods: [{TpPainterModifiers.instance.Status}]";
            (var persistent, var timed) = TpLibEditor.MarqueeStatus;
                
            return "--GENERAL----------------------------------------------------------------------\n"
                   + $"{versionInfo}\n\n"
                   + $"Heap Memory: {mem:N0}, Max: {maxMem:N0}, Editor Update Rate: {edUpdateRate}\nDOTween"
                   #if TPT_DOTWEEN || DOTWEEN
                   + " [installed]"
                   #else
                   + " [Not present]"
                   #endif
                   + "\n--TpSystem-------------------------------------------------------------------\n"
                   + $"TpLib Mem Alloc setting: {TpLib.MemAllocSettings}\n\n"
                   + $"TpLib: {TpLib.TilemapsCount} tilemap(s), {TpLib.TaggedTilesCount} tag(s), {TpLib.TileTypesCount} type(s), {TpLib.TileInterfacesCount} interface(s), {TpLib.GuidToTileCount} GUID(s).\n"
                   + $"TpLib internal pools: TPB:{TpLib.TpbPoolStat}\nDICTs: {TpLib.s_DictOfV3IToTpb_PoolStat}\nDeferredEvents: {TpLib.DeferredEvtPoolStat}\nList_Tilemap: {TpLib.ListTilemapPoolStat}\nCloning_Data: {TpLib.CloningDataPoolStat}\nDeferredCallback {TpLib.DeferredCallbackPoolStat}\n\n"
                   + $"SpawnUtil: Pooled prefab parent: {SpawningUtil.CleanPoolHostName}\n{SpawningUtil.PoolStatus(limit)}\n\n"
                   + $"Events: Save:[{TpEvents.NumSaveEvents}], Trigger:[{TpEvents.NumTriggerEvents}]\n"
                   + $"Conditional Async callbacks (EditorOnly): {TpConditionalTasks.ConditionalTaskInfo}\n"
                   + $"TpLib Delayed Async callbacks: [{TpLib.CurrentActiveDelayedCallbacks.ToString()}]\n"
                   + $"TpLib Cloning Queue Depth: [{TpLib.CloneQueueDepth}], Max Depth:  [{TpLib.CloneQueueMaxDepth}]\nNum of clones per Update: [{maxClonesPerUpdate}] (unlimited when not in Play)\n"
                   + $"TpLib Callback Queue Depth: [{TpLib.DeferredQueueDepth}], Max Depth: [{TpLib.DeferredQueueMaxDepth}]\nNum of callbacks per Update: [{maxCallbackPerUpdate}] (unlimited when not in Play)\n"
                   + $"TileFabLib: ZoneManager [{zmOn}], #Instances:[{zmTotalZms}], [{zmNumRegs}] Regs, [{zmTotalChunks}] total managed Chunks."
                   + $"\nTpLibEditor: Timed Marquees [{timed}], Persistent Marquees [{persistent}]"
                   + "\n--SELECTION-------------------------------------------------------------------\n"
                   + $"Selection.ActiveObject: [{(Selection.activeObject != null ? Selection.activeObject.name+':'+Selection.activeObject.GetType() : "None")}]\n"
                   + $"Active Editor tool: [{ToolManager.activeToolType.ToString()}]\nActive Editor Tool Context: [{ToolManager.activeContextType.ToString()}]"
                   + "\n--GRID SELECTION--------------------------------------------------------------\n"
                   + $"Selection:[{GridSelection.position.ToString()}] Grid:[{GridSelection.grid}] Active?[{GridSelection.active}] Target:{GridSelection.target}"
                   + "\n--PAINTER---------------------------------------------------------------------\n"
                   + $"Mods: [#={TpPainterModifiers.instance.m_PTransformsList.Count}, Active Mod index: {TpPainterModifiers.instance.m_ActiveIndex}]\n\n"
                   + $"Preview: Loading?[{AssetPreview.IsLoadingAssetPreviews().ToString()}], #Preview Tiles [{NumPreviewTiles.ToString()}]\nPlaceholder?[{PreviewIsPlaceholderTile.ToString()}] Proxy?[{PreviewIsProxyTile.ToString()}] #Proxies: [{TpPreviewUtility.ProxyIndex}]  Max num proxies: [{MaxProxyIndex}]\n\n"
                   + PainterState() 
                   + plugins + mods + Importers;
        }

       
        /// <summary>
        /// The 'raw' instance of this window.
        /// </summary>
        public static TpSysInfo Instance => s_Instance;

        private string Importers
        {
            get
            {
                (var num, var max) = TpImageLib.NumTextureImporters;
                return $"\nTexture Importers: Current [{num}], max [{max}]";
            }
        }
        
        private string PainterState()
        {
            var win = TilePlusPainterWindow.RawInstance;
            if (win != null)
            {
                var tmap = TpPainterState.PaintableMap != null
                               ? TpPainterState.PaintableMap.Name
                               : "None";
                
                var over   = EditorWindow.mouseOverWindow;
                var isOver = over != null && over.GetType() == typeof(SceneView);
                    
                var currentMousePos = TpPainterSceneView.instance.CurrentMouseGridPosition;
                if(currentMousePos == TilePlusBase.ImpossibleGridPosition)
                    isOver = false;
                var tool = TpPainterState.CurrentTool;
                if(tool is TpPainterTool.Help or TpPainterTool.Settings or TpPainterTool.None)    
                    isOver = false;                        
                
                var pos = $"Mouse Position: [{(isOver? currentMousePos.ToString() : "Not in Scene")}]";
                
                return $"Painter: Selected Tilemap: {tmap} [valid:{TpPainterState.ValidTilemapSelection.ToString()}], {pos}\nMode [{TpPainterState.GlobalMode}],  CurrentTool: [{TpPainterState.CurrentTool}]\nDraggable? [{TpPainterSceneView.instance.Draggable.ToString()}], previousTool: [{TpPainterState.PreviousTool}]\nPermanentCtlid: [{win.PainterWindowControlId}], HotCtl: [{GUIUtility.hotControl}], KB Control: [{GUIUtility.keyboardControl}]\nMove State: [SEQ:{TpPainterState.TpPainterMoveSequenceState}, Source Map: {(TpPainterState.MovePickSequenceTilemap == null ? "----" : TpPainterState.MovePickSequenceTilemap)}]\nPainterUTEactive?[{win.PaintModeUtePaletteActive.ToString()}]\nTpLib ChangeEvent Pool:{win.PainterPoolStat}\n\n{ClipboardState}\n\n";
            }

            return $"Painter: OFF HotCtl: {GUIUtility.hotControl}\n\n";
        }

        private string ClipboardState
        {
            get
            {
                var clip = TpPainterState.Clipboard; 
                if (clip == null)
                    return $"[EMPTY]: Clipbd Reclones: {TpPainterClipboard.RecloneCount.ToString()}";
                var cellcount = clip.Cells?.Length ?? 0;
                return $"Clipboard: [Index:{clip.ItemIndex.ToString()}, Variety:{clip.ItemVariety}, Name:{clip.TargetName}]\nPicked?[{clip.WasPickedTile.ToString()}] ColorMod?[{clip.ColorModified.ToString()}] TransformMod?[{clip.TransformModified.ToString()}]\nEditActions:[{EditActionsString(clip.ModifierEditActions)}] \nVarietyCantModifyTransform?[{clip.VarietyCantModifyTransform}], VarietyCanBeMassPainted?[{clip.VarietyCanBeMassPainted}]\nFromConversion?[{clip.FromAConversion}], ClipBd Reclones: [{TpPainterClipboard.RecloneCount}], ClipBd_GC [{TpPainterClipboard.Discards}],\nClipbdObj_GUID: [{clip.ClipboardGuid}]\nMulti-selection :[Pivot: {clip.Pivot}, OffsetMod: {clip.OffsetModifier}, #cells: {cellcount} cells.mod? {clip.CellsModified}]\nUnary item mod?: [transform:{clip.TransformModified.ToString()}, Color:[{clip.ColorModified.ToString()}]"; }
        }

        private string EditActionsString(EditActions e)
        {
            return e == EditActions.None
                       ? "None"
                       : $"{(((e & EditActions.Transform) != 0)?"T":"t")} {(((e & EditActions.Color) != 0)?"C":"c")}";
        }
        

        private static class Si_Text
        {
            public const string Stats_Title         = "Tile+Toolkit System Information";
            public const string Stats_OnPlayWarning = "This panel refreshes automatically, even in Play mode.";

        }
    }
}
