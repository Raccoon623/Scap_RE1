// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-31-2022
// ***********************************************************************
// <copyright file="TilePlusConfig.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Scriptable singleton with configuration for the entire TilePlus project</summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
#nullable enable

namespace TilePlus.Editor
{
    #region SettingsChanges
    /// <summary>
    /// Which setting has changed?
    /// </summary>
    // ReSharper disable once InconsistentNaming
    internal enum TPC_SettingThatChanged
    {
        /// <summary>
        /// Autosave
        /// </summary>
        AutoSave,
        /// <summary>
        /// Confirm/Delete
        /// </summary>
        ConfirmDelete,
        /// <summary>
        /// Tile Highlight time
        /// </summary>
        TileHighlights,
        
        /// <summary>
        /// No overwrite of painted tiles.
        /// </summary>
        NoOverwriteFromPalette
        
        
    }

    /// <summary>
    /// Sent to subscribers of TpEditorUtilities.SettingHasChanged
    /// </summary>
    public class ConfigChangeInfo
    {
        /// <summary>
        /// previous
        /// </summary>
        public readonly object m_OldValue;
        /// <summary>
        /// new
        /// </summary>
        public readonly object m_NewValue;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        public ConfigChangeInfo(object oldValue, object newValue)
        {
            m_OldValue = oldValue;
            m_NewValue = newValue;
        }
        
    }
    #endregion
    
    
    
    /// <summary>
    /// Configuration options for TilePlus and Tile+Brush. Note that the other
    /// save option is FilePathAttribute.Location.PreferencesFolder which will cause
    /// all sorts of issues if you have multiple Unity instances open at the same time
    /// since they'll all try to ref this at the same time and all sorts of 'stuff' goes
    /// bezerko.
    /// </summary>
    [FilePath("TpConfig/TilePlusConfig.asset",FilePathAttribute.Location.ProjectFolder)]
    public class TilePlusConfig : ScriptableSingleton<TilePlusConfig>
    {
        #region foldouts
        /// <summary>
        /// Preferences for class-level foldouts in SelectionInspectorGui
        /// Note that changes here affect all uses of SelectionInspectorGui
        /// </summary>
        [Serializable]
        public class FoldoutPref
        {
            /// <summary>
            /// The class name
            /// </summary>
            [SerializeField]
            public string m_ClassName=string.Empty;
            /// <summary>
            /// Is the foldout open?
            /// </summary>
            [SerializeField]
            public bool   m_FoldoutOpen;
        }
        
        [SerializeField]
        private List<FoldoutPref> m_FoldoutPrefs = new List<FoldoutPref>();

        /// <summary>
        /// Get the foldout pref for a class
        /// </summary>
        /// <param name="className">name of the class</param>
        /// <returns></returns>
        public bool GetFoldoutPref(string className)
        {
            var pref = m_FoldoutPrefs.Find(x => x.m_ClassName == className);
            return pref == null || pref.m_FoldoutOpen;
        }

        /// <summary>
        /// Set the foldout pref for a class
        /// </summary>
        /// <param name="className">name of the class</param>
        /// <param name="on">open or closed?</param>
        public void SetFoldoutPref(string className, bool on)
        {
            var pref = m_FoldoutPrefs.Find(x => x.m_ClassName == className);
            if (pref == null)
                m_FoldoutPrefs.Add(new FoldoutPref{m_ClassName = className, m_FoldoutOpen = on});
            else
                pref.m_FoldoutOpen = on;
            RegisterSave();
        }

        /// <summary>
        /// Turn off the foldouts for a list of classnames
        /// </summary>
        /// <param name="classNames">class names to clear prefs for</param>
        public void ClearFoldoutPrefsFor(IEnumerable<string> classNames)
        {
            foreach(var classname in classNames)
                SetFoldoutPref(classname, false);
        }
        
        /// <summary>
        /// Turn on the foldouts for a list of classnames
        /// </summary>
        /// <param name="classNames">class names to set prefs for</param>
        public void SetFoldoutPrefsFor(IEnumerable<string> classNames)
        {
            foreach(var classname in classNames)
                SetFoldoutPref(classname, true);
        }
        
        [SerializeField]
        private bool m_ClassHeaders = true;
        /// <summary>
        /// IMGUI tile editor class headers control.
        /// </summary>
        public bool ClassHeaders
        {
            get => m_ClassHeaders;
            set { m_ClassHeaders = value; RegisterSave(); }
        }

        
        #endregion
        
       
        
        #region BrushAndPainter

        
        [SerializeField]
        private bool m_NoOverwriteFromPalette = true;
        /// <summary>
        /// Overwrite control for painter and brush
        /// </summary>
        public bool NoOverwriteFromPalette
        {
            get => m_NoOverwriteFromPalette;
            set
            {
                var change = new ConfigChangeInfo(m_NoOverwriteFromPalette, value);
                m_NoOverwriteFromPalette = value; 
                RegisterSave();
                TpEditorUtilities.SettingHasChanged(TPC_SettingThatChanged.NoOverwriteFromPalette.ToString(),change);  
            }
        }

        
        
        //Params for cursor position feature
        [SerializeField]
        private bool m_ShowBrushPosition = true; 
        [SerializeField]
        private Color m_BrushPositionTextColor = Color.black;
        [SerializeField]
        private int m_BrushPositionFontSize = 14;
        /// <summary>
        /// Show BRUSH position (used by Painter in Painter)
        /// </summary>
        public bool ShowBrushPosition
        {
            get => m_ShowBrushPosition;
            set { m_ShowBrushPosition = value; RegisterSave(); }
        }

        /// <summary>
        /// Text color for BRUSH only (painter uses its own config for this value)
        /// </summary>
        public Color BrushPositionTextColor
        {
            get => m_BrushPositionTextColor;
            set { m_BrushPositionTextColor = value; RegisterSave(); }
        }

        /// <summary>
        /// Font size for BRUSH and Painter (note only affects font size of sceneview text)
        /// </summary>
        public int BrushPositionFontSize
        {
            get => m_BrushPositionFontSize;
            set { m_BrushPositionFontSize = value; RegisterSave(); }
        }

        //params for toolbar focus button
        [SerializeField]
        private float m_FocusSize = (TpLibEditor.MaxZoomValue - TpLibEditor.MinimumZoomValue) / 2;

        /// <summary>
        /// What's the camera zoom? 
        /// </summary>
        public float FocusSize
        {
            get => m_FocusSize;
            set { m_FocusSize = value; RegisterSave(); }
        }
        

        //for the TilePlusConfigEditor window
        [SerializeField]
        private bool m_HideHints;
        
        /// <summary>
        /// hide hints property
        /// </summary>
        public bool HideHints
        {
            get => m_HideHints;
            set { m_HideHints = value; RegisterSave(); }
        }

       



        [SerializeField]
        private Color m_GizmoColor = Color.white;
        /// <summary>
        /// Color for gizmos for Painter window
        /// </summary>
        public Color GizmoColor
        {
            get => m_GizmoColor;
            set
            {
                value.a      = 1f;
                m_GizmoColor = value; RegisterSave();
            }
        }
        
        [SerializeField]
        private bool m_SlidersOk = true;
        /// <summary>
        /// Sliders allowed property
        /// </summary>
        public bool SlidersAllowed
        {
            get => m_SlidersOk;
            set { m_SlidersOk = value; RegisterSave(); }
        }
        
        [SerializeField]
        private bool m_AutoSave = true;
        /// <summary>
        /// Autosave property
        /// </summary>
        public bool AutoSave
        {
            get => m_AutoSave;
            set
            {
                var change = new ConfigChangeInfo(m_AutoSave, value);

                m_AutoSave = value; RegisterSave(); 
                TpEditorUtilities.SettingHasChanged(TPC_SettingThatChanged.AutoSave.ToString(),change);  

            }
        }
        
        //control over icon sizes for inspector buttons
        [SerializeField]
        private float m_SelInspectorButtonSize = 15f;
        /// <summary>
        /// Button sizes for selection insp.
        /// </summary>
        public float SelInspectorButtonSize
        {
            get => m_SelInspectorButtonSize;
            set
            {
                if (value < 7) 
                    value = 7;
                m_SelInspectorButtonSize = value; 
                RegisterSave();
            }
        }

        
        [SerializeField]
        private float m_BrushInspectorButtonSize = 15f;
        /// <summary>
        /// Button Sizes for Brush insp.
        /// </summary>
        public float BrushInspectorButtonSize
        {
            get => m_BrushInspectorButtonSize;
            set
            {
                if (value < 5)
                    value = 5;
                m_BrushInspectorButtonSize = value; 
                RegisterSave();
            }
        }

        [SerializeField]
        private float m_TileHighlightTime = 1f;
        /// <summary>
        /// highlight time for tiles in Painter
        /// </summary>
        public float TileHighlightTime
        {
            get => m_TileHighlightTime;
            set
            {
                var change = new ConfigChangeInfo(m_TileHighlightTime, value);

                m_TileHighlightTime = value; 
                RegisterSave();
                TpEditorUtilities.SettingHasChanged(TPC_SettingThatChanged.TileHighlights.ToString(), change);  
                
            }
        }

        [SerializeField]
        private bool m_ConfirmDeleteTile = true;
        /// <summary>
        /// confirm tile deletions
        /// </summary>
        public bool ConfirmDeleteTile
        {
            get => m_ConfirmDeleteTile;
            set
            {
                var change = new ConfigChangeInfo(m_ConfirmDeleteTile, value);

                m_ConfirmDeleteTile = value;
                RegisterSave();
                TpEditorUtilities.SettingHasChanged(TPC_SettingThatChanged.ConfirmDelete.ToString(), change);  
                
            }
        }
        
        
        [SerializeField]
        private bool m_AllowBackspaceDelInSelectionInspector;
        /// <summary>
        /// Default selection inspector hotkey-ignore control.
        /// </summary>
        public bool AllowBackspaceDelInSelectionInspector
        {
            get => m_AllowBackspaceDelInSelectionInspector;
            set { m_AllowBackspaceDelInSelectionInspector = value; RegisterSave(); }
        }
        
        
        
        [SerializeField]
        private bool m_ShowSelectionInspectorTileInfo = true;
        /// <summary>
        /// Hide or show tile info foldout in Sel Insp
        /// </summary>
        public bool ShowSelectionInspectorTileInfo
        {
            get => m_ShowSelectionInspectorTileInfo;
            set { m_ShowSelectionInspectorTileInfo = value; RegisterSave(); }
        }

        [SerializeField]
        private bool m_OpenSelectionInspectorDefault;

        /// <summary>
        /// Hide or show contents of default inspector foldout in Sel Insp
        /// </summary>
        public bool OpenSelectionInspectorDefault
        {
            get => m_OpenSelectionInspectorDefault;
            set { m_OpenSelectionInspectorDefault = value; RegisterSave(); }
        }

        //for the selection inspector, which has no persistent state
        [SerializeField]
        private bool m_ShowDefaultSelInspector;
        /// <summary>
        /// Hide or show default inspector foldout
        /// </summary>
        public bool ShowDefaultSelInspector
        {
            get => m_ShowDefaultSelInspector;
            set { 
                m_ShowDefaultSelInspector = value;
                if (value) //if showing this, open foldout for convenience.
                    m_OpenSelectionInspectorDefault = true;
                RegisterSave(); 
            }
        }
        

        
        #endregion
        
        #region system

        
        [SerializeField]
        private bool m_SafePlayMode;
        /// <summary>
        /// Safe Play mode property
        /// </summary>
        public bool SafePlayMode
        {
            get => m_SafePlayMode;
            set { m_SafePlayMode = value; RegisterSave(); }
        }


        //messages in TpLib/TpLibEditor
        [SerializeField]
        private bool m_InformationalMessages;
        [SerializeField]
        private bool m_WarningMessages;
        [SerializeField] 
        private bool m_ErrorMessages = true;
        /// <summary>
        /// show warning messages?
        /// </summary>
        public bool WarningMessages
        {
            get => m_WarningMessages;
            set { m_WarningMessages = value; RegisterSave();}
        }

        /// <summary>
        /// show informational messages?
        /// </summary>
        public bool InformationalMessages
        {
            get => m_InformationalMessages;
            set { m_InformationalMessages = value; RegisterSave(); }
        }

        /// <summary>
        /// Show Error messages?
        /// </summary>
        public bool ErrorMessages
        {
            get => m_ErrorMessages;
            set { m_ErrorMessages = value; RegisterSave(); }
        }

        /*/// <summary>
        /// get menu command for opening an inspector
        /// </summary>
        public string InspectorCommand 
        {
            get => m_InspectorCommand;
            set { m_InspectorCommand = value; RegisterSave(); }
        }

        

        /// <summary>
        /// Get menu command to open popup inspector
        /// </summary>
        public string PropertiesCommand
        {
            get => m_PropertiesCommand;
            set { m_PropertiesCommand = value; RegisterSave(); }
        }*/
        
        

        [SerializeField]
        private string m_NameSpaces = "TilePlusDemo";
        /// <summary>
        /// IMGUI tile editor namespace control
        /// </summary>
        public string NameSpaces
        {
            get => m_NameSpaces;
            set
            { m_NameSpaces = value; RegisterSave();}
        }

       
        
        [SerializeField]
        private bool m_AllowPrefabEditing;
        /// <summary>
        /// Override all(?) protections for tilemap prefab editing. Uh oh...
        /// </summary>
        public bool AllowPrefabEditing
        {
            get => m_AllowPrefabEditing;
            set { m_AllowPrefabEditing = value; RegisterSave(); }
        }
        
       
        /// <summary>
        /// Restore default settings
        /// </summary>
        public void Restore()
        {
            m_ErrorMessages                         = true;
            m_WarningMessages                       = false;
            m_InformationalMessages                 = false;
            m_AllowBackspaceDelInSelectionInspector = false;
            m_AutoSave                              = true;
            m_HideHints                             = false;
            m_ShowDefaultSelInspector               = false;
            m_TileHighlightTime                     = 1f;
            m_ConfirmDeleteTile                     = true;
            m_ShowSelectionInspectorTileInfo        = true;
            m_OpenSelectionInspectorDefault         = false;
            m_SafePlayMode                          = false;
            m_SlidersOk                             = true;
            m_SelInspectorButtonSize                = 15f;
            m_BrushInspectorButtonSize              = 15f;
            m_NameSpaces                            = "TilePlusDemo";
            m_ClassHeaders                          = true;
            m_AllowPrefabEditing                    = false;
            m_GizmoColor                            = Color.white;
            m_BrushPositionTextColor                = Color.black;
            m_FocusSize                             = (TpLibEditor.MaxZoomValue - TpLibEditor.MinimumZoomValue) / 2;
            m_NoOverwriteFromPalette                = true;
            m_ShowBrushPosition                     = true;
            m_BrushPositionFontSize                 = 14;
            
            m_FoldoutPrefs.Clear();

            RegisterSave();
            TpEditorUtilities.ForceHotReloadDelayed();


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



        #endregion
        
    }
    
    
    
}



