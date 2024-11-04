// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 05-20-2021
// ***********************************************************************
// <copyright file="TptShowObjectFieldAttribute.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
using System;

// ReSharper disable InvalidXmlDocComment

namespace TilePlus
{
    /// <summary>
    /// This attribute can be used in Tile classes show an object field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class TptShowObjectFieldAttribute : TptAttributeBaseAttribute
    {
        /// <summary>
        /// Type to inspect
        /// </summary>
        public readonly Type m_DesiredType;
        /// <summary>
        /// Allow scene objects
        /// </summary>
        public readonly bool        m_AllowSceneObjects;
        /// <summary>
        /// Add inspector button
        /// </summary>
        public readonly bool        m_InspectorButton;
        /// <summary>
        /// Optional, for fields only (incl enums/obj field).
        /// Use when TpLib needs to be updated. See the m_Tag field in TilePlusBase for example.
        /// </summary>
        public readonly bool m_UpdateTpLib;
        
        /// <summary>
        /// Show an object field
        /// </summary>
        /// <param name="desiredType">Type to inspect from typeof</param>
        /// <param name="allowSceneObjects">Set false to only allow assets.</param>
        /// <param name="addInspectorButton">True to show an inspector button.</param>
        /// <param name="updateTpLib">Optional, for fields only (incl enums/obj field).
        /// Use when TpLib needs to be updated. See the m_Tag field in TilePlusBase for example. </param>
        /// <inheritdoc />
        public TptShowObjectFieldAttribute(Type      desiredType,
                                           bool      allowSceneObjects  = true,
                                           bool      addInspectorButton = false,
                                           SpaceMode spacemode          = SpaceMode.None,
                                           ShowMode  showMode           = ShowMode.Always,
                                           string    visibilityProperty = "",
                                           bool      updateTpLib        = false)
        {
            m_DesiredType        = desiredType;
            m_AllowSceneObjects  = allowSceneObjects;
            m_InspectorButton    = addInspectorButton;
            m_SpaceMode          = spacemode;
            m_ShowMode           = showMode;
            m_VisibilityProperty = visibilityProperty;
            m_UpdateTpLib        = updateTpLib;
        }
    }
}
