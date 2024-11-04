// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 06-22-2021
// ***********************************************************************
// <copyright file="TptAttributeBaseAttribute.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
using System;

namespace TilePlus
{
    /// <summary>
    /// Control visual appearance of each item.
    /// </summary>
    public enum SpaceMode
    {
        /// <summary>
        /// No space
        /// </summary>
        None,
        /// <summary>
        /// Space before item
        /// </summary>
        SpaceBefore,
        /// <summary>
        /// Space after item
        /// </summary>
        SpaceAfter,
        /// <summary>
        /// Space before and after item
        /// </summary>
        SpaceBoth,
        /// <summary>
        /// Line before item
        /// </summary>
        LineBefore,
        /// <summary>
        /// Line after item
        /// </summary>
        LineAfter,
        /// <summary>
        /// Line before and after item
        /// </summary>
        LineBoth
    }

    /// <summary>
    /// Control how this item is shown.
    /// </summary>
    public enum ShowMode
    {
        /// <summary>
        /// Always show this
        /// </summary>
        Always,
        /// <summary>
        /// show only when playing
        /// </summary>
        InPlay,
        /// <summary>
        /// do not show when playing
        /// </summary>
        NotInPlay,
        /// <summary>
        /// show based on the bool return value of a named property.
        /// <remarks>If the value returned isn't a bool then the item is shown.</remarks>
        /// </summary>
        Property

    }

    /// <summary>
    /// This is the base class for all Tpt attributes except ShowAsLabelBrushInspector.
    /// </summary>
    public class TptAttributeBaseAttribute : Attribute
    {
        /// <summary>
        /// Space or draw-line control
        /// </summary>
        public SpaceMode m_SpaceMode;
        /// <summary>
        /// Controls how to show the item that the attr is attached to.
        /// </summary>
        public ShowMode  m_ShowMode;
        /// <summary>
        /// If showMode == Property, the name of a property to control visibility.
        /// </summary>
        public string    m_VisibilityProperty;


        /// <summary>
        /// Constructor for the Tpt base attribute.
        /// </summary>
        /// <param name="spaceMode">Space or line before or after or nothing</param>
        /// <param name="showMode">Show always, only in Play mode, only when Not playing, or use a named property</param>
        /// <param name="visibilityProperty">Named property when showMode == Property</param>
        protected TptAttributeBaseAttribute(SpaceMode spaceMode          = SpaceMode.None,
                                            ShowMode  showMode           = ShowMode.Always,
                                            string    visibilityProperty = "")
        
        {
            m_SpaceMode          = spaceMode;
            m_ShowMode           = showMode;
            m_VisibilityProperty = visibilityProperty;
        }
    }
}
