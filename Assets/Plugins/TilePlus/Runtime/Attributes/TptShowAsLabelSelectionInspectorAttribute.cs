// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 04-25-2021
// ***********************************************************************
// <copyright file="TptShowAsLabelSelectionInspectorAttribute.cs" company="Jeff Sasmor">
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
    /// For a field or property, the value returned is displayed with ToString,
    /// so whatever you tag with this must have a ToString() method. This
    /// isn't an issue for most types of fields or properties that you might use,
    /// but keep it in mind.
    /// Sometimes you can create a property just to show it as a label, based on other
    /// values in your Tile class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class TptShowAsLabelSelectionInspectorAttribute : TptAttributeBaseAttribute
    {
        /// <summary>
        /// if true, use HelpBox instead of Label
        /// </summary>
        public readonly bool m_UseHelpBox;
        /// <summary>
        /// if true, split CamelCase names
        /// </summary>
        public readonly bool m_SplitCamelCaseNames;
        /// <summary>
        /// Tooltip: overrides any [tooltip] attribute on this member if it's a field.
        /// </summary>
        public readonly string m_Tooltip;
        
        /// <summary>
        /// Show this field or property as a label in the Selection Inspector
        /// </summary>
        /// <param name="useHelpBox">if true show as infobox instead of a label</param>
        /// <param name="splitCamelCaseNames">if true, split camelcase names</param>
        /// <param name="toolTip">Optional tooltip. On a field, overrides any [Tooltip] attr on this field.</param>
        /// <inheritdoc />
        public TptShowAsLabelSelectionInspectorAttribute(bool      useHelpBox          = false,
                                                         bool      splitCamelCaseNames = true,
                                                         string    toolTip             = "",
                                                         SpaceMode spacemode           = SpaceMode.None,
                                                         ShowMode  showMode            = ShowMode.Always,
                                                         string    visibilityProperty  = "")
        {
            m_UseHelpBox          = useHelpBox;
            m_SplitCamelCaseNames = splitCamelCaseNames;
            m_Tooltip             = toolTip;
            m_SpaceMode           = spacemode;
            m_ShowMode            = showMode;
            m_VisibilityProperty  = visibilityProperty;

        }
        
    }
}
