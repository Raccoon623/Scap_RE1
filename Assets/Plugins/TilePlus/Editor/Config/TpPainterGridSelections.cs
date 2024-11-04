// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 04-24-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 04-09-2023
// ***********************************************************************
// <copyright file="TpPainterGridSelections.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace TilePlus.Editor.Painter
{
    
    /// <summary>
    /// A wrapper class for the transform
    /// </summary>
    [Serializable]
    public class GridSelectionWrapper
    {
        /// <summary>
        /// The bounds of the GridSelection
        /// </summary>
        [FormerlySerializedAs("m_BoundInt")]
        [SerializeField]
        public BoundsInt m_BoundsInt;

        /// <summary>
        /// Initializes a new instance of the <see cref="GridSelectionWrapper"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public GridSelectionWrapper(BoundsInt value)
        {
            m_BoundsInt = value;
            if (m_BoundsInt.size.z != 0)
                return;
            var size = m_BoundsInt.size;
            size.z           = 1;
            m_BoundsInt.size = size;
        }
    }
    
   
    /// <summary>
    /// Storage of Grid Selections
    /// </summary>
    [FilePath("TpConfig/TpPainterGridSelections.asset",FilePathAttribute.Location.ProjectFolder)]
    public class TpPainterGridSelections : ScriptableSingleton<TpPainterGridSelections>
    {
        /// <summary>
        /// selection wrappers
        /// </summary>
        [SerializeField]
        public List<GridSelectionWrapper> m_GridSelectionWrappers = new();

        /// <summary>
        /// Save the grid selection data
        /// </summary>
        public void SaveData()
        {
            Save(true);
        }
    }
    
    
    
}
