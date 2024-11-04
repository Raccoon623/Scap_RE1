// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 11-01-2022
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-31-2022
// ***********************************************************************
// <copyright file="TpPainterMiniButtons.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Creates toolbar with small buttons for use at the bottom of the painter window</summary>
// ***********************************************************************

using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.UIElements;
using static TilePlus.Editor.TpIconLib;
#nullable enable

namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// TpPainterMiniButtons creates the toolbar at the bottom of the Painter window.
    /// </summary>
    /// <seealso cref="VisualElement" />
    /// <seealso cref="TilePlus.Editor.ISettingsChangeWatcher" />
    internal class TpPainterMiniButtons : VisualElement, ISettingsChangeWatcher
    {
        #region privateFields
        private readonly TpImageToggle miniUpdateInPlayButton;
        private readonly TpImageToggle miniAutoSaveButton;
        private readonly TpImageToggle miniConfirmDeleteButton;
        private readonly TpImageToggle miniSelectionSyncButton;
        private readonly TpImageToggle miniOverwriteButton;
        private readonly TpImageToggle miniRefreshButton;
        private readonly double         creationTime;
        #endregion
        
        #region Ctor
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="dim">base size</param>
        internal TpPainterMiniButtons (float dim)
        {
            style.flexGrow      = 0;
            style.flexShrink    = 0;
            style.flexDirection = FlexDirection.Row;
            style.minHeight     = dim + 4;
            style.marginBottom  = 2;
            style.marginTop     = 2;
            style.marginLeft    = 4;

            creationTime = EditorApplication.timeSinceStartup;

            

            miniRefreshButton = new TpImageToggle(RefreshTpSystem,

                                                  "force-refresh",
                                                  "Rescans all tilemaps, rebuilds TP system internal data, then clears and rebuilds this window.",
                                                  dim,
                                                  FindIcon(TpIconType.UnityRefreshIcon)) 
                                { style = { alignSelf = new StyleEnum<Align>(Align.FlexEnd) } };
            
            
            Add(miniRefreshButton);

            Add(new TpSpacer(10, 10));

            miniUpdateInPlayButton = new TpImageToggle(MiniUpdateInPlayButtonCallback,
                                                       "mini-autoupdate",
                                                       "Shows if UPDATE IN PLAY is active, click to toggle",
                                                       dim,
                                                       FindIcon(TpIconType.EyeballIcon));

            miniUpdateInPlayButton.SetValueWithoutNotify(TilePlusPainterConfig.PainterAutoRefresh);
            Add(miniUpdateInPlayButton);

            miniAutoSaveButton = new TpImageToggle(value => TilePlusConfig.instance.AutoSave = value,
                                                     "mini-autorefresh",
                                                     "Shows if AUTOSAVE is active, click to toggle.",
                                                     dim,
                                                     FindIcon(TpIconType.LifepreserverIcon));
            miniAutoSaveButton.SetValueWithoutNotify(TilePlusConfig.instance.AutoSave);
            Add(miniAutoSaveButton);

            miniConfirmDeleteButton = new TpImageToggle(value => TilePlusConfig.instance.ConfirmDeleteTile = value,
                                                          "mini-confdel",
                                                          "Shows if CONFIRM-DELETE is active, click to toggle.",
                                                          dim,
                                                          FindIcon(TpIconType.TrashIcon));

            miniConfirmDeleteButton.SetValueWithoutNotify(TilePlusConfig.instance.ConfirmDeleteTile);
            Add(miniConfirmDeleteButton);

            void MiniSelectionSyncButtonCallback(bool value) => TilePlusPainterConfig.TpPainterSyncSelection = value;

            miniSelectionSyncButton = new TpImageToggle(MiniSelectionSyncButtonCallback,
                                                        "mini-syncsel",
                                                        "Shows if EDITOR SELECTION SYNC is active, click to toggle.",
                                                        dim,
                                                        FindIcon(TpIconType.SyncIcon));

            miniSelectionSyncButton.SetValueWithoutNotify(TilePlusPainterConfig.TpPainterSyncSelection);
            Add(miniSelectionSyncButton);

            var binding = ShortcutManager.instance.GetShortcutBinding("TilePlus/Painter: Overwrite protection override [C]");
            var s  = $"({binding.ToString()})";
            
            
            miniOverwriteButton = new TpImageToggle(value => TilePlusConfig.instance.NoOverwriteFromPalette = value,
                                                      "mini-overwrite",
                                                      $"Shows if overwrite protection is active, click to toggle. If active, use the hotkey {s} to override.",
                                                      dim,
                                                      FindIcon(TpIconType.LockedIcon));
            miniOverwriteButton.SetValueWithoutNotify(TilePlusConfig.instance.NoOverwriteFromPalette);
            Add(miniOverwriteButton);

            void MiniPickToPaintCallback(bool value) => TilePlusPainterConfig.TpPainterPickToPaint = value;

            var miniPickToPaint = new TpImageToggle(MiniPickToPaintCallback,
                                                    "mini-picktopaint",
                                                    "When active, a PICK action in the Scene window changes the Painter Tool to PAINT automatically (PAINT mode only). Click to toggle. \nSHIFT toggles this intention.",
                                                    dim,
                                                    FindIcon(TpIconType.PinIcon));
            miniPickToPaint.SetValueWithoutNotify(TilePlusPainterConfig.TpPainterPickToPaint);

            Add(miniPickToPaint);
            return;


            void MiniUpdateInPlayButtonCallback(bool value) => TilePlusPainterConfig.PainterAutoRefresh = value;
        }

        #endregion
       
        #region utils

        internal void RefreshSystem()
        {
            RefreshTpSystem(false);
        }

        private void RefreshTpSystem(bool _)
        {
            var lockout = EditorApplication.timeSinceStartup - creationTime < 5;
            
            if (TpLib.IsSceneScanActive || lockout)  //avoids errors if someone clicks this button quickly/repeatedly.
            {
                //Debug.Log($"{Time.time} - {creationTime}");
                miniRefreshButton.SetValueWithoutNotify(false);
                return;
            }
            
            
            TpLib.OnSceneScanComplete += OnSceneScanComplete;
            TpLib.SceneScan();
            TpLib.OnSceneScanComplete -= OnSceneScanComplete;
            return;

            void OnSceneScanComplete()
            {
                miniRefreshButton.SetValueWithoutNotify(false);
                TilePlusPainterWindow.RawInstance!.ReInit(); //note that this causes this class instance to be destroyed
            }
        }
        
        
        /// <summary>
        /// Show/hide the "Tool Activated" indicator.
        /// </summary>
        /// <param name="on">On/Off = True/False</param> 
        internal void SetActivatedIndicator(bool on)
        {
            miniSelectionSyncButton.style.backgroundColor = on
                                                           ? Color.red
                                                           : Color.clear;
        }
        #endregion
        
        #region events
        
        /// <summary>
        /// Called when TilePlusConfig settings change.
        /// </summary>
        /// <param name="change">what changed</param>
        /// <param name="changes">The new values as a small class instance with old and new values.</param>
        public void OnSettingsChange(string change, ConfigChangeInfo changes)
        {
            //these are all bool so get the newValue as a bool
            if(changes.m_NewValue is not bool b)
                return;

            if (change == TPC_SettingThatChanged.NoOverwriteFromPalette.ToString())
            {
                if (miniOverwriteButton.value != b)
                    miniOverwriteButton.SetValueWithoutNotify(b);
            }
            else if (change == TPC_SettingThatChanged.ConfirmDelete.ToString())
            {
                if (miniConfirmDeleteButton.value != b)
                    miniConfirmDeleteButton.SetValueWithoutNotify(b);
            }
            else if (change == TPP_SettingThatChanged.SyncSelection.ToString())
            {
                if (miniSelectionSyncButton.value != b)
                    miniSelectionSyncButton.SetValueWithoutNotify(b);
            }
            else if (change == TPC_SettingThatChanged.AutoSave.ToString())
            {
                if (miniAutoSaveButton.value != b)
                    miniAutoSaveButton.SetValueWithoutNotify(b);
            }
            else if (change == TPP_SettingThatChanged.UpdateInPlay.ToString())
            {
                if (miniUpdateInPlayButton.value != b)
                    miniUpdateInPlayButton.SetValueWithoutNotify(b);
            }
        }
        
        #endregion
    }
    
}
