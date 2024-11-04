// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 01-01-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 01-05-2023
// ***********************************************************************
// <copyright file="TpPainterModifiers.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using FilePathAttribute = UnityEditor.FilePathAttribute;


#nullable enable
namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// The target for a default.
    /// </summary>
    public enum DefaultTarget
    {
        /// <summary>
        /// No target
        /// </summary>
        None,
        /// <summary>
        /// Tiles are the target
        /// </summary>
        Tiles,
        /// <summary>
        /// Prefabs are the target.
        /// </summary>
        Prefabs
    }

    /// <summary>
    /// Describes what parts of the individual settings item
    /// are actually used when the setting is applied
    /// </summary>
    [Flags]
    public enum EditActions
    {
        /// <summary>
        /// No actions
        /// </summary>
        None = 0,
        /// <summary>
        /// Transform can change
        /// </summary>
        Transform = 1,
        /// <summary>
        /// Color can change
        /// </summary>
        Color = 2
        
    }

    
        
        
    
    /// <summary>
    /// A wrapper class for the transform
    /// </summary>
    [Serializable]
    public class ModifierWrapper
    {
        /// <summary>
        /// A name for this modifier. No real use but nice in UI
        /// </summary>
        [SerializeField]
        public string m_ModName = "Modifier";
        
        /// <summary>
        /// Is this a default setting for Prefabs or Tiles
        /// </summary>
        [SerializeField]
        public DefaultTarget m_DefaultTarget = DefaultTarget.None;
        /// <summary>
        /// transform matrix
        /// </summary>
        [SerializeField]
        public Matrix4x4 m_Matrix;
        /// <summary>
        /// Color value
        /// </summary>
        [SerializeField]
        public Color m_Color = Color.white;
        /// <summary>
        /// User-selected editing actions
        /// </summary>
        [SerializeField]
        public EditActions m_EditActions = TpPainterModifiers.EditActionsInit;
        
        /// <summary>
        /// Ctor
        /// </summary>
        public ModifierWrapper()
        {
            m_Matrix =  Matrix4x4.identity;
        }

        
        /// <summary>
        /// Does this affect the transform?
        /// </summary>
        public bool AffectsTransform => (m_EditActions & EditActions.Transform) != 0;
        /// <summary>
        /// Does this affect color?
        /// </summary>
        public bool AffectsColor => (m_EditActions & EditActions.Color) != 0;
        /// <summary>
        /// Are there any enabled actions at all?
        /// </summary>
        public bool AnyEnabledActions => m_EditActions != EditActions.None;

    }
    
    /// <summary>
    /// Asset for applying transforms/colors/etc to tiles as they're being painted by T+Painter.
    /// Note that there should only be one of these.
    /// Note that the other
    /// save option is FilePathAttribute.Location.PreferencesFolder which will cause
    /// all sorts of issues if you have multiple Unity instances open at the same time
    /// since they'll all try to ref this at the same time and all sorts of 'stuff' goes
    /// bezerko.
    /// </summary>
    [FilePath("TpConfig/TpPainterModifiers.asset",FilePathAttribute.Location.ProjectFolder)]
    public class TpPainterModifiers : ScriptableSingleton<TpPainterModifiers>
    {
        
        #region fieldsProps

        internal const EditActions EditActionsInit = EditActions.Color |  EditActions.Transform;

        /// <summary>
        /// A list of modifier wrappers
        /// </summary>
        public List<ModifierWrapper> m_PTransformsList = new (1);
        /// <summary>
        /// Which modifier is active
        /// </summary>
        public int m_ActiveIndex;// = -1;

        /// <summary>
        /// Get the current default for tiles
        /// </summary>
        public ModifierWrapper? TilesDefault { get; private set; }

        /// <summary>
        /// Get the current default for prefabs
        /// </summary>
        public ModifierWrapper? PrefabDefault { get; private set; }

        private bool defaultsChanged;
        /// <summary>
        /// Have the defaults changed? CLEARED WHEN READ!
        /// </summary>
        public bool DefaultsChanged {
            get
            {
                var val = defaultsChanged;
                defaultsChanged = false;
                return val;
            }
            private set => defaultsChanged = value;
        }
        
        /// <summary>
        /// Get the current modifier wrapper. May return null.
        /// </summary>
        public ModifierWrapper? CurrentModWrapper
        {
            get
            {
                if (m_ActiveIndex < 0|| m_ActiveIndex >= m_PTransformsList.Count)
                    return null;
                return m_PTransformsList[m_ActiveIndex];
            }
        }

        /// <summary>
        /// Status string for Sysinfo window.
        /// </summary>
        public string Status
        {
            get
            {
                var curr = CurrentModWrapper;
                var current = curr == null                   ? "NONE" :
                              (string.IsNullOrEmpty(curr.m_ModName) ? m_ActiveIndex.ToString() : curr.m_ModName);

                var pd = PrefabDefault == null                         ? "NONE" :
                         string.IsNullOrEmpty(PrefabDefault.m_ModName) ? "No name" : PrefabDefault.m_ModName;
                var td = TilesDefault == null                         ? "NONE" :
                         string.IsNullOrEmpty(TilesDefault.m_ModName) ? "No name" : TilesDefault.m_ModName;

                return $"Current: {current}, PrefabDefault: {pd}, TilesDefault: {td}";
            }
        }
        
        #endregion
        
        #region utils

        private void OnEnable()
        {
            SetDefaultsIfAny();
        }


        private void OnDisable()
        {
            if(TpLibEditor.Informational)
                TpLib.TpLog("PainterModifiers ScriptableSingleton released.");
        }


        private void SetDefaultsIfAny()
        {
            DefaultsChanged = true;
            var index = m_PTransformsList.FindIndex(x => x.m_DefaultTarget == DefaultTarget.Tiles);
            TilesDefault = index == -1
                               ? null
                               : m_PTransformsList[index];
            
            index = m_PTransformsList.FindIndex(x => x.m_DefaultTarget == DefaultTarget.Prefabs);
            PrefabDefault = index == -1
                                ? null
                                : m_PTransformsList[index];
        }


        private bool updatePainter;
        
        /// <summary>
        /// Save data and update the defaults
        /// </summary>
        public void SaveData()
        {
          
            SetDefaultsIfAny();

            if (EditorWindow.HasOpenInstances<TilePlusPainterWindow>() && TilePlusPainterWindow.RawInstance != null)
            {
                if (TpPainterState.InPaintMode)
                {
                    var clipBd = TpPainterState.Clipboard;
                    if (clipBd != null)
                    {
                        if(PrefabDefault != null && clipBd.IsPrefab)
                            clipBd.Apply(PrefabDefault.m_Matrix, PrefabDefault.m_EditActions);
                        if (TilesDefault != null && clipBd.IsTile) //note that TileBase ish items are ignored.
                        {
                            if((TilesDefault.m_EditActions & EditActions.Transform) != 0)
                                clipBd.Apply(TilesDefault.m_Matrix,TilesDefault.m_EditActions);
                            if((TilesDefault.m_EditActions & EditActions.Color) != 0)
                                clipBd.Apply(TilesDefault.m_Color,TilesDefault.m_EditActions);
                        }
                    }
                }
            }
            
            Save(true);
        }
        
        #endregion
    }
   
}
