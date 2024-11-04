// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 08-03-2021
// ***********************************************************************
// <copyright file="TptShowEnumAttribute.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
using System;

namespace TilePlus
{
    /// <summary>
    /// This attribute can be used in Tile classes to affect what is
    /// displayed in the Tile++Brush selection inspector.
    /// This is used to show Enums in a pop-up.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Enum)]
    public class TptShowEnumAttribute : TptAttributeBaseAttribute
    {
        /// <summary>
        /// Optional, for fields only (incl enums/obj field).
        /// Use when TpLib needs to be updated. See the m_Tag field in TilePlusBase for example.
        /// </summary>
        public readonly bool m_UpdateTpLib;

        ///  <summary>
        ///  Show an enum
        ///  </summary>
        ///  <param name="visibilityProperty">The name of a property used to control visibility of this field when showMode is Property</param>
        ///  <param name="updateTpLib">Optional, for fields only (incl enums/obj field).
        ///  Use when TpLib needs to be updated. See the m_Tag field in TilePlusBase for example. </param>
        ///  <param name="spaceMode">Space before or after this field</param>
        ///  <param name="showMode">visibility control</param>
        ///  <inheritdoc />
        public TptShowEnumAttribute(SpaceMode spaceMode          = SpaceMode.None,
                                    ShowMode  showMode           = ShowMode.Always,
                                    string    visibilityProperty = "",
                                    bool      updateTpLib        = false)
        {
            m_SpaceMode          = spaceMode;
            m_ShowMode           = showMode;
            m_VisibilityProperty = visibilityProperty;
            m_UpdateTpLib        = updateTpLib;
        }
    }
}
