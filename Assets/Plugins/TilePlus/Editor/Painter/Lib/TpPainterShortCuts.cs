// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 11-18-2022
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 01-13-23
// ***********************************************************************
// <copyright file="TpPainterShortCuts.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Shortcut handlers for Tile+Painter</summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// ReSharper disable AnnotateNotNullTypeMember
// ReSharper disable PossibleNullReferenceException
// ReSharper disable AnnotateCanBeNullParameter
// ReSharper disable AnnotateCanBeNullTypeMember


/*
 * IMPORTANT NOTE: CHANGING ANY OF THE BINDING PATHS REQUIRE CORRESPONDING CHANGES ELSEWHERE FOR
 * LOOKUPS TO WORK CORRECTLY!!!!!
 */
#nullable enable

namespace TilePlus.Editor.Painter
{

    /// <summary>
    /// This static class tracks the activity of the TilePlusPainterWindow and enables/disables shortcut buttons.
    /// It also is the tooltip provider for the Global mode and PainterTool action buttons.
    /// </summary>
    [InitializeOnLoad]
    internal static class TpPainterShortCuts
    {
        
        /// <summary>
        /// Control info for ClipboardItem rotate/flip
        /// </summary>
        private class ClipboardRotateFlipControl
        {
            internal readonly bool m_AffectsCells;
            internal readonly bool m_Mode;

            internal ClipboardRotateFlipControl() { }

            internal ClipboardRotateFlipControl(bool mode, bool affectsCells)
            {
                m_Mode         = mode;
                m_AffectsCells = affectsCells;
            }
        }

        private static readonly Dictionary<TpPainterTool, string> s_ToolToShortCutId;
        
        /// <summary>
        /// Default Ctor called by InitializeOnLoad.
        /// </summary>
        static TpPainterShortCuts()
        {
            EditorApplication.update += PainterActiveCheck;
            s_ToolToShortCutId = new Dictionary<TpPainterTool, string>(8)
                               {
                                   { TpPainterTool.Paint, "TilePlus/Painter:Paint" },
                                   { TpPainterTool.Move, "TilePlus/Painter:Move" },
                                   { TpPainterTool.Erase, "TilePlus/Painter:Erase" },
                                   { TpPainterTool.Pick, "TilePlus/Painter:Pick" },
                                   { TpPainterTool.RotateCw, "TilePlus/Painter:RotateCW" },
                                   { TpPainterTool.RotateCcw, "TilePlus/Painter:RotateCCW" },
                                   { TpPainterTool.FlipX, "TilePlus/Painter:Flip X" },
                                   { TpPainterTool.FlipY, "TilePlus/Painter:Flip Y" },
                                   { TpPainterTool.ResetTransform, "TilePlus/Painter:Restore Clipboard" },
                                   { TpPainterTool.None, "TilePlus/Painter:Deactivate" }
                               };
           
        }

        /// <summary>
        /// Menu item handler for Clear Painter Favorites.
        /// </summary>
        [MenuItem("Tools/TilePlus/Clear Painter Favorites", false, 10000)]
        public static void ClearFavorites()
        {
            if(EditorUtility.DisplayDialog("Confirm", "Confirm clearing Painter Favorites", "Confirm", "Cancel"))
                TilePlusPainterFavorites.ClearFavorites();
            
        }
        
        
        /// <summary>
        /// Handler for Tools/TilePlus/Bundle Clipboard 
        /// </summary>
        [MenuItem("Tools/TilePlus/Bundle Clipboard", false, 10000)]
        public static  void ClipboardMultipleSelectionToBundle()
        {
            #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            ClipboardBundler();
            #pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }
        
        /// <summary>
        /// Validator for Clipboard->Bundle
        /// </summary>
        /// <returns></returns>
        [MenuItem("Tools/TilePlus/Bundle Clipboard", true, 10000)]
        public static bool ClipboardValidator()
        {
            if (!EditorWindow.HasOpenInstances<TilePlusPainterWindow>() || TilePlusPainterWindow.RawInstance == null) 
                return false;
            #pragma warning disable CS8602 // Dereference of a possibly null reference.
            return TilePlusPainterWindow.instance.TabBar.PickedTileType == TpPickedTileType.Multiple;
            #pragma warning restore CS8602 // Dereference of a possibly null reference.

        }
        
        internal static async Task ClipboardBundler() 
        {
            if (!EditorWindow.HasOpenInstances<TilePlusPainterWindow>() || TilePlusPainterWindow.RawInstance == null)
                return;
            var path = TpEditorUtilities.GetPathFromUser("Select destination folder for saving the Bundle.");
            if(path == string.Empty)
                return;
           
            #pragma warning disable CS8602 // Dereference of a possibly null reference.
            var obj = TilePlusPainterWindow.instance.TabBar.PickedObjectInstance;
            if(obj == null)
                return;
            #pragma warning restore CS8602 // Dereference of a possibly null reference.
            // ReSharper disable once Unity.NoNullPatternMatching
            if(obj is not TileCellsWrapper w)
                return;
            
            //get a base name for the assets.
            var possibleFilename = "ClipboardPick" ;
            var dialog           = ScriptableObject.CreateInstance<StringEntryDialog>();
            var wait             = true;
            dialog.ShowStringEntryDialog("Choose a name",
                                                    "Enter a name for the generated assets.",
                                                    possibleFilename,
                                                    "Ok",
                                                    string.Empty,
                                                    s =>
                                                    {
                                                        wait             = false;
                                                        possibleFilename = s; 
                                                    });
            
            while (wait)
               await Task.Yield();
            if(w != null)
                await TpPrefabUtilities.CreateBundleFromCells(Cb,
                                                              "CLIPBOARD_PICK",
                                                              path,
                                                              w,
                                                              false,
                                                              possibleFilename,
                                                              true);
            
            return;

            
            void Cb(bool _){ }


        }
        
        
        /// <summary>
        /// Handler for Tools/TilePlus/Bundle Project Prefabs 
        /// </summary>
        [MenuItem("Assets/TilePlus/Bundle Project Prefabs", false, 10000)]
        public static void ProjectMultipleSelectionToBundle()
        {
            #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            ProjectPrefabsBundler();
            #pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }
        
        /// <summary>
        /// Validator for Project Prefabs ->Bundle
        /// </summary>
        /// <returns></returns>
        [MenuItem("Assets/TilePlus/Bundle Project Prefabs", true, 10000)]
        public static bool ProjectMultipleSelectionToBundleValidator()
        {
            var objects = Selection.objects;
            var count   = 0;
            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (var obj in objects)
            {
                if(obj == null)
                    continue;
                // ReSharper disable once Unity.NoNullPatternMatching
                if (obj is GameObject && PrefabUtility.IsPartOfAnyPrefab(obj))
                    count++;
                else
                {
                    count = 0;
                    break;
                }
            }

            return count != 0;
            
        }

        private static async Task ProjectPrefabsBundler()
        {
            var path = TpEditorUtilities.GetPathFromUser("Select destination folder for saving the Bundle.");
            if(path == string.Empty)
                return;
           
            //get a base name for the assets.
            var possibleFilename = "ProjectPrefabsBundle" ;
            var dialog           = ScriptableObject.CreateInstance<StringEntryDialog>();
            var wait             = true;
            dialog.ShowStringEntryDialog("Choose a name",
                                                    "Enter a name for the generated assets.",
                                                    possibleFilename,
                                                    "Ok",
                                                    string.Empty,
                                                    s =>
                                                    {
                                                        wait             = false;
                                                        possibleFilename = s; 
                                                    });
            
            while (wait)
               await Task.Yield();
            
            var objects = Selection.objects;
            if (objects == null)
            {
                TpPainterSceneView.instance.SceneViewNotification = "Nothing to bundle....";
                return;
            }
            
            if(objects.Any(x=>x==null))
            {
                TpPainterSceneView.instance.SceneViewNotification = "Null items found: can't bundle....";
                return;
            }    

            // ReSharper disable once Unity.NoNullPatternMatching
            var l = objects.Where(obj => obj is GameObject && PrefabUtility.IsPartOfAnyPrefab(obj))
                           .Cast<GameObject>()
                           .ToArray();
            if (l.Length == 0)
            {
                TpPainterSceneView.instance.SceneViewNotification = "Nothing to bundle....";
                return;
            }    
            
            TpPrefabUtilities.CreateBundleFromPrefabs(Cb, path, l, possibleFilename);
            
            return;

            
            void Cb(bool _){ }


        }
        
        
        
        /// <summary>
        /// This is called repeatedly by EditorApplication.update
        /// and tests the Painter state to see if the shortcuts should
        /// be enabled or not.
        /// </summary>
        private static void PainterActiveCheck()
        {
            var ri = TilePlusPainterWindow.RawInstance;
            if (ri == null)
            {
                TpPainterActive = false;
                return;
            }
            if(!ri.GuiInitialized)
                return;
            // ReSharper disable once Unity.NoNullPatternMatching
            TpPainterActive = ri is { IsActive: true} && TpPainterState.CurrentToolHasTilemapEffect ;
        }

        /// <summary>
        /// Get a formatted tooltip string for a tool. Accomodates user changing shortcut bindings
        /// </summary>
        /// <param name="tool">TpPainterTool</param>
        /// <returns>formatted string</returns>
        internal static string GetToolTipForTool(TpPainterTool tool)
        {
            if (!s_ToolToShortCutId.TryGetValue(tool, out var s))
                return "(Shortcut not found!)";
            var binding = ShortcutManager.instance.GetShortcutBinding(s);
            return $"({binding.ToString()})";
        }

        
        
        /// <summary>
        /// Get a 2-letter abbreviated tooltip for the Painter action buttons
        /// </summary>
        /// <param name="tool">value of TpPainterTool enum</param>
        /// <returns>string</returns>
        /// <remarks>Note that the 2-letter approach works for Ctrl+X or ALT+X but if someone uses Ctrl+Alt+X this fails.</remarks>
        internal static string GetAbbreviatedToolTipForTool(TpPainterTool tool)
        {
            var tip           = GetToolTipForTool(tool);
            var plusSignIndex = tip.IndexOf('+');
            if (plusSignIndex == -1)
                return tip.Substring(1,1); //note that '1' is used vs '0' that's because the first char is a (
            return $"{tip.Substring(1, 1)}{tip.Substring(plusSignIndex + 1, 1)}";
        }
        
        
        /// <summary>
        /// Get a tooltip for the T+P mode buttons
        /// </summary>
        /// <returns></returns>
        internal static string GetModeButtonTooltip()
        {
            var binding = ShortcutManager.instance.GetShortcutBinding("TilePlus/Painter:Toggle Mode");
            return $"({binding.ToString()} to toggle mode)";
        }
        
        //NB - this isn't really used anymore.
        /// <summary>
        /// Get abbreviated tooltip for Mode buttons. 
        /// </summary>
        /// <returns>string</returns>
        /// <remarks>Note that the 2-letter approach works for Ctrl+X or ALT+X but if someone uses Ctrl+Alt+X this fails.</remarks>
        internal static string GetModeButtonAbbreviatedTooltip()
        {
            var tip           = GetModeButtonTooltip();
            var plusSignIndex = tip.IndexOf('+');
            if (plusSignIndex == -1)
                return tip.Substring(1,1);
            return $"{tip.Substring(1, 1)}{tip.Substring(plusSignIndex + 1, 1)}";

        }

        
        
        /// <summary>
        /// Add one or more assets from the Project folder to the Favorites
        /// </summary>
        [MenuItem("Assets/TilePlus/Add To Painter Favorites",false,10000)]
        [SuppressMessage("ReSharper", "Unity.NoNullPatternMatching")]
        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        internal static void CopyToFavorites()
        {
            var objs = Selection.objects.Where(x=>x!=null);
            
            var gameObjects = objs.Where(o => o is GameObject && PrefabUtility.IsPartOfAnyPrefab(o)).ToArray();
            var tiles       = objs.Where(o => o is TileBase).ToArray();
            var bundles     = objs.Where(o => o is TpTileBundle).ToArray();

            var objects = new List<UnityEngine.Object>(gameObjects.Length + tiles.Length + bundles.Length);
            objects.AddRange(gameObjects);
            objects.AddRange(tiles);
            objects.AddRange(bundles);
            TilePlusPainterFavorites.AddToFavorites(objects.ToArray());
        }
        
        /// <summary>
        /// Validator for CopyToFavorites
        /// </summary>
        /// <returns></returns>
        [MenuItem("Assets/TilePlus/Add To Painter Favorites",true,10000)]
        [SuppressMessage("ReSharper", "Unity.NoNullPatternMatching")]
        internal static bool CopyToFavoritesValidator()
        {
            var  objects = Selection.objects;
            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (var obj in objects)
            {
                if(!obj)
                    continue;
                // ReSharper disable once ConvertIfStatementToSwitchStatement
                if (obj is GameObject && PrefabUtility.IsPartOfAnyPrefab(obj))
                    continue;
                if (obj is TpTileBundle)
                    continue;
                if (obj is not TileBase)
                    return false;
                if (obj is ITilePlus { IsAsset: false })
                    return false;
            }

            return true;
        }

       
        
        
        
        internal static bool MarqueeDragState { get; private set; }
        
        
        //NOTE: if this shortcut id changes then the MarqueeDragTooltip property needs to be changed too.
        //Also see TpPainterTab bar to update this shortcut ref as well.
        [ClutchShortcut("TilePlus/Painter/MarqueeDrag [C]",KeyCode.Alpha1, ShortcutModifiers.Alt)] 
        internal static void MarqueeDrag(ShortcutArguments args)
        {
            MarqueeDragState = args.stage switch
                                     {
                                         ShortcutStage.Begin => true,
                                         ShortcutStage.End   => false,
                                         _                   => false
                                     };
        }
        
        
        /// <summary>
        /// Get a tooltip for the Marquee-Drag function
        /// </summary>
        /// <returns></returns>
        internal static string MarqueeDragTooltip => ShortcutManager.instance.GetShortcutBinding("TilePlus/Painter/MarqueeDrag [C]").ToString();
           
        
        
        /// <summary>
        /// Is the painter active?
        /// </summary>
        internal static bool TpPainterActive { get; private set; }

        /// <summary>
        /// if Painter window not open, ask. 
        /// </summary>
        /// <returns>null if user declined to open window or window not found</returns>
        private static TilePlusPainterWindow? PainterOpenCheck()
        {
            var p = TilePlusPainterWindow.RawInstance;
            if (p != null)
                return p;
            if (!Guidance())
                return null;
            TilePlusPainterWindow.ShowWindow();
            return TilePlusPainterWindow.instance;
        }
        

        [Shortcut("TilePlus/Painter:Toggle Mode" , KeyCode.Q, ShortcutModifiers.Alt)] 
        internal static void TogglePaintEditMode(ShortcutArguments args)
        {
            var instance = PainterOpenCheck();
            if(instance == null)
                return;
           
            //toggle mode
            var current = (int)TpPainterState.GlobalMode;
            var count   = Enum.GetValues(typeof(GlobalMode)).Length;
            current = (current + 1) % count;
            instance.TabBar.ActivateModeBarButton((GlobalMode)current, true);

        }
        
        /// <summary>
        /// Activate paint tool
        /// </summary>
        [Shortcut("TilePlus/Painter:Paint",  KeyCode.B, ShortcutModifiers.Alt)]
        internal static void ActivatePaintbrush()
        {
            if(!TpPainterState.InPaintMode)
                return;

            var p = PainterOpenCheck();
            if(p == null)
                return;
            
            p.TabBar.ActivateToolbarButton(TpPainterTool.Paint,true);
        }

        /// <summary>
        /// Activate Move tool
        /// </summary>
        [Shortcut("TilePlus/Painter:Move",KeyCode.M, ShortcutModifiers.Alt)]
        internal static void ActivateMoveTool()
        {
            if(!TpPainterState.InPaintMode)
                return;

            ActivateNormalTool(TpPainterTool.Move);
        }

        /// <summary>
        /// Activate Erase tool
        /// </summary>

        [Shortcut("TilePlus/Painter:Erase", KeyCode.D, ShortcutModifiers.Alt)]
        internal static void ActivateEraseTool()
        {
            if(!TpPainterState.InPaintMode)
                return;

            ActivateNormalTool(TpPainterTool.Erase);
        }

        /// <summary>
        /// Activate Pick tool
        /// </summary>
        [Shortcut("TilePlus/Painter:Pick",  KeyCode.I, ShortcutModifiers.Alt)]
        internal static void ActivatePickTool()
        {
            if(TpPainterState.InGridSelMode)
               return; 
            ActivateNormalTool(TpPainterTool.Pick);
        }

        //This shortcut has two IDs: that way if you're holding down SHIFT to rotate cells, you don't have to release it to do ALT-E, SHIFT-ALT-E works too. 
        [Shortcut(id:"TilePlus/Painter: Rotate Pivot 2", KeyCode.E, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        [Shortcut("TilePlus/Painter: Rotate Pivot 1", KeyCode.E, ShortcutModifiers.Alt)] 
        internal static void ChangePivot()
        {
            if(!TpPainterState.InPaintMode)
                return;
            var paintableObject = TpPainterState.Clipboard;
            if (paintableObject is {IsNotEmpty:true, IsMultiple: true }) 
                paintableObject.RotatePivot();
        }
        
        
        /// <summary>
        /// Activate RotateCW tool
        /// </summary>
        [Shortcut("TilePlus/Painter:RotateCW",  KeyCode.R, ShortcutModifiers.Alt)]
        internal static void ActivateRotateCwTool()
        {
            if(!TpPainterState.InPaintMode)
                return;
            ActivateNormalTool(TpPainterTool.RotateCw, RotatePaintingTile, new ClipboardRotateFlipControl(false,false));
        }
        
        /// <summary>
        /// Activate Rotate CCW tool
        /// </summary>
        [Shortcut("TilePlus/Painter:RotateCCW", KeyCode.T, ShortcutModifiers.Alt)]
        internal static void ActivateRotateCcwTool()
        {
            if(!TpPainterState.InPaintMode)
                return;
            ActivateNormalTool(TpPainterTool.RotateCcw, RotatePaintingTile, new ClipboardRotateFlipControl(true,false));
        }
        
        /// <summary>
        /// Activate RotateCW tool for cells
        /// </summary>
        [Shortcut("TilePlus/Painter:Cells Rotate CW", KeyCode.R, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        internal static void ActivateRotateCwToolForCells()
        {
            if(!TpPainterState.InPaintMode)
                return;
            ActivateNormalTool(TpPainterTool.RotateCw, RotatePaintingTile, new ClipboardRotateFlipControl(false, true));
        }
        
        /// <summary>
        /// Activate Rotate CCW tool for cells
        /// </summary>
        [Shortcut("TilePlus/Painter:Cells Rotate CCW", KeyCode.T, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        internal static void ActivateRotateCcwToolForCells()
        {
            if(!TpPainterState.InPaintMode)
                return;
            ActivateNormalTool(TpPainterTool.RotateCcw, RotatePaintingTile, new ClipboardRotateFlipControl(true, true));
        }
        
        
        /// <summary>
        /// Activate FlipX tool
        /// </summary>
        [Shortcut("TilePlus/Painter:Flip X", KeyCode.X, ShortcutModifiers.Alt)]
        internal static void ActivateFlipXTool()
        {
            if(!TpPainterState.InPaintMode)
                return;
            ActivateNormalTool(TpPainterTool.FlipX, FlipPaintingTile, new ClipboardRotateFlipControl(true,false));
        }

        /// <summary>
        /// Activate FlipX tool
        /// </summary>
        [Shortcut("TilePlus/Painter:Cells Flip X", KeyCode.X, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        internal static void ActivateFlipXForCellsTool()
        {
            if(!TpPainterState.InPaintMode)
                return;
            ActivateNormalTool(TpPainterTool.FlipX, FlipPaintingTile, new ClipboardRotateFlipControl(true, true));
        }

        
        /// <summary>
        /// Activate FlipY tool
        /// </summary>
        [Shortcut("TilePlus/Painter:Flip Y",  KeyCode.C, ShortcutModifiers.Alt)]
        internal static void ActivateFlipYTool()
        {
            if(!TpPainterState.InPaintMode)
                return;
            ActivateNormalTool(TpPainterTool.FlipY, FlipPaintingTile, new ClipboardRotateFlipControl(false,false));
        }
        
        
        /// <summary>
        /// Activate FlipY tool for cells
        /// </summary>
        [Shortcut("TilePlus/Painter:Cells Flip Y", KeyCode.C, ShortcutModifiers.Alt | ShortcutModifiers.Shift)]
        internal static void ActivateFlipYToolOnCells()
        {
            if(!TpPainterState.InPaintMode)
                return;
            ActivateNormalTool(TpPainterTool.FlipY, FlipPaintingTile, new ClipboardRotateFlipControl(false, true));
        }
        
        
        
        /// <summary>
        /// Activate Reset Transform tool
        /// </summary>
        [Shortcut("TilePlus/Painter:Restore Clipboard",  KeyCode.Z, ShortcutModifiers.Alt)]
        internal static void ActivateRestoreClipboard()
        {
            if(!TpPainterState.InPaintMode)
                return;
            ActivateNormalTool(TpPainterTool.ResetTransform, RestoreClipboard, new ClipboardRotateFlipControl());
        }
        
        /// <summary>
        /// Activate Deactivate tool
        /// </summary>
        [Shortcut("TilePlus/Painter:Deactivate",  KeyCode.O, ShortcutModifiers.Alt)]
        internal static void ActivateNullTool()
        {
            ActivateNormalTool(TpPainterTool.None);
        }
        
        /// <summary>
        /// perform Apply Modifier
        /// </summary>
        [Shortcut("TilePlus/Painter:Apply Modifier", KeyCode.V, ShortcutModifiers.Alt)]
        internal static void ApplyCustomTransform()
        {
            if(!TpPainterActive)
                return;
            if(!TpPainterState.InPaintMode)
                return;
            
            var tgt = TpPainterState.Clipboard;
            
            if (tgt is not { Valid: true })
            {
                TpPainterSceneView.instance.SceneViewNotification = "Can't apply a mod to an empty or invalid Clipboard!";
                return;
            }

            var wrappers = TpPainterModifiers.instance.m_PTransformsList;
            
            var customTransformsCount = wrappers.Count;
            var selection             = TpPainterModifiers.instance.m_ActiveIndex;
            if (customTransformsCount >= 0 && selection >= 0 && selection < customTransformsCount)
            {
                if(TpLibEditor.Informational)
                    TpLib.TpLog($"Apply selected modification {selection} ");

                var wrapper = wrappers[selection];
                var applies = wrapper.m_EditActions;
                if((applies & EditActions.Transform) != 0)
                    tgt.Apply(wrapper.m_Matrix,applies);
                if((applies & EditActions.Color) != 0)
                    tgt.Apply(wrapper.m_Color,applies);

                TpPreviewUtility.ClearPreview();

            }
            else
            {
                TpPainterSceneView.instance.SceneViewNotification = "No modifications or\nno selection in Modifiers window!";
            }
        }
        
        
        private static void RotatePaintingTile(ClipboardRotateFlipControl ctrl)
        {
            var p = PainterOpenCheck();
            if(p == null)
                return;
            var tgt = TpPainterState.Clipboard;
            if(tgt == null)
                return;
            if (!tgt.Valid)
                return;
            if (tgt.IsTileBase)
                TpPainterSceneView.instance.SceneViewNotification = "Cannot Rotate TileBase tile!";
            else
                tgt.Rotate(ctrl.m_Mode, ctrl.m_AffectsCells);
            TpPreviewUtility.ClearPreview();
        }



        private static void FlipPaintingTile(ClipboardRotateFlipControl ctrl)
        {
            var p = PainterOpenCheck();
            if(p == null)
                return;
            var tgt = TpPainterState.Clipboard;
            if(tgt == null)
                return;
            if (!tgt.Valid)
                return;
            if (tgt.IsTileBase)
                TpPainterSceneView.instance.SceneViewNotification = "Cannot Flip TileBase tile!";
            else
                tgt.Flip(ctrl.m_Mode, ctrl.m_AffectsCells);
            TpPreviewUtility.ClearPreview();
            
        }


        private static void RestoreClipboard(ClipboardRotateFlipControl _)
        {
            var p = PainterOpenCheck();
            if(p == null)
                return;
            var tgt = TpPainterState.Clipboard;
            if(tgt == null)
                return;
            if (!tgt.Valid)
                return;
            TpPainterState.RestoreClipboard();
            TpPreviewUtility.ClearPreview();
            p.TabBar.TabBarTransformModified(false, tgt);
        }
        
        private static void ActivateNormalTool(TpPainterTool tool, Action<ClipboardRotateFlipControl>? previewAction = null, ClipboardRotateFlipControl? previewActionControl = null )
        {
            var p = PainterOpenCheck();
            if(p == null)
                return;
            if (!TpPainterState.ValidTilemapSelection)
                return;

            if (previewAction != null && previewActionControl != null)
            {
                if (p.PreviewActive)
                    previewAction(previewActionControl);
                else if(p.MouseOverTpPainter)
                    p.TabBar.ActivateToolbarButton(tool, true);
            }
            else
                p.TabBar.ActivateToolbarButton(tool, true);
            
        }
        
        
        
        
        private static bool Guidance()
        {
            return EditorUtility.DisplayDialog("Help me out here!",
                                               "You clicked a shortcut for the Tile+Painter but there's no Painter window open. \nDo you want to open one?\n\n(Note that you'll have to try the shortcut again after the window opens)",
                                               "YES",
                                               "NOPE");
        }

    }
}
