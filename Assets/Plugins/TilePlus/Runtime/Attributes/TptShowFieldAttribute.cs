// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 06-22-2021
// ***********************************************************************
// <copyright file="TptShowFieldAttribute.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
using System;
// ReSharper disable InvalidXmlDocComment

namespace TilePlus
{
    /// <summary>
    /// This attribute can be used in Tile classes to affect what is
    /// displayed in the Tile++Brush selection inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class TptShowFieldAttribute : TptAttributeBaseAttribute
    {
        /// <summary>
        /// Minimum value for range
        /// </summary>
        public readonly float m_RangeMin;
        /// <summary>
        /// Maximum value for range
        /// </summary>
        public readonly float m_RangeMax;

        /// <summary>
        /// Optional, for fields only (incl enums/obj field).
        /// Use when TpLib needs to be updated. See the m_Tag field in TilePlusBase for example.
        /// </summary>
        public readonly bool m_UpdateTpLib;

        /// <summary>
        /// Force showing field if showmode
        /// </summary>
        public readonly bool m_ForceShowField;

        /// <summary>
        /// Show a field. 
        /// </summary>
        /// <param name="rangeMin">int/float only - minimum value for range slider. min and max must both be nonzero for sliders to appear.</param>
        /// <param name="rangeMax">int/float only - maximum value for range slider. min and max must both be nonzero for sliders to appear.</param>
        /// <param name="updateTpLib">Optional, for fields only (incl enums/obj field).
        /// Use when TpLib needs to be updated. See the m_Tag field in TilePlusBase for example. </param>
        /// <param name="forceFieldInPlay"> Force showing this field in Play: requires ShowMode = InPlay,Always, or Property when eval=true)
        /// -- Use when you want to force a field to be editable @ runtime. Normally they're forced to plain text helpboxes.
        /// </param>
        /// 
        /// <inheritdoc />
        public TptShowFieldAttribute(float     rangeMin           = 0,
                                     float     rangeMax           = 0,
                                     SpaceMode spaceMode          = SpaceMode.None,
                                     ShowMode  showMode           = ShowMode.Always,
                                     string    visibilityProperty = "",
                                     bool      updateTpLib        = false,
                                     bool      forceFieldInPlay   = false)
        {
            m_RangeMax           = rangeMax;
            m_RangeMin           = rangeMin;
            m_ShowMode           = showMode;
            m_SpaceMode          = spaceMode;
            m_VisibilityProperty = visibilityProperty;
            m_UpdateTpLib        = updateTpLib;
            m_ForceShowField = forceFieldInPlay;

        }
    }
}
