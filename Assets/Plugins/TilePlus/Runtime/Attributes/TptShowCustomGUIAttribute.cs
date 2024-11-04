// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 07-12-2021
// ***********************************************************************
// <copyright file="TptShowCustomGUIAttribute.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
using System;

namespace TilePlus
{
    /// <summary>
    /// This attribute is used to provide customGUI for a tile class.
    /// note that the method needs to be:
    /// [TptShowCustomGUI]
    /// public CustomGuiReturn BaseGui(GUISkin skin, Vector2 buttonSize, bool inPrefab)
    /// The return value should be an instance of CustomGuiReturn.
    /// See TilePlusBase, TpFlexAnimatedTile, etc for an example.
    /// NOTE access has to be public or protected.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method )]
    public class TptShowCustomGUIAttribute : TptAttributeBaseAttribute
    {
        /// <summary>
        /// Create ShowCustomGUI attr;
        /// Note that method should return true if a change has been made or false if not.
        /// If the method has a void return type then nothing can happen but it's not an error.
        /// </summary>
        /// <inheritdoc />
        public TptShowCustomGUIAttribute(SpaceMode spaceMode          = SpaceMode.None,
                                         ShowMode  showMode           = ShowMode.Always,
                                         string    visibilityProperty = "")
        {
            m_SpaceMode          = spaceMode;
            m_ShowMode           = showMode;
            m_VisibilityProperty = visibilityProperty;
        }
    }
}
