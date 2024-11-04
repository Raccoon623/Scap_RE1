// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 04-26-2021
// ***********************************************************************
// <copyright file="TptShowAsLabelBrushInspectorAttribute.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
using System;
// ReSharper disable InvalidXmlDocComment

namespace TilePlus
{
    /// <summary>
    /// This attribute can be used in Tile classes to affect what is
    /// displayed in the Tile+Brush Brush inspector (ie in the palette).
    /// For a field or property, the value returned is displayed with ToString,
    /// so whatever you tag with this must have a ToString() method. This
    /// isn't an issue for most types of fields or properties that you might use,
    /// but keep it in mind.
    /// Sometimes you can create a property just to show it as a label, based on other
    /// values in your Tile class.
    /// </summary>
    /// <remarks>This isn't a subclass of TptAttributeBaseAttribute</remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class TptShowAsLabelBrushInspectorAttribute : Attribute
    {
        /// <summary>
        /// if true, use HelpBox instead of Label
        /// </summary>
        public readonly bool m_UseHelpBox; //note that if this is true then SplitCamelCaseName is forced false
        /// <summary>
        /// if true, split CamelCase names
        /// </summary>
        public readonly bool      m_SplitCamelCaseNames;
        /// <summary>
        /// Tooltip: overrides any [tooltip] attribute on this member if it's a field.
        /// </summary>
        public readonly string    m_Tooltip;

        /// <summary>
        /// Show this field or property as a label in the Selection Inspector
        /// </summary>
        /// <param name="useHelpBox">if true show as infobox instead of a label: splitCamelCaseNames forced false if this is true.</param>
        /// <param name="splitCamelCaseNames">if true, split camelcase names</param>
        /// <param name="toolTip">Optional tooltip. On a field, overrides any [Tooltip] attr on this field.</param>
        /// <inheritdoc/>
        public TptShowAsLabelBrushInspectorAttribute(bool      useHelpBox           = false,
                                                     bool      splitCamelCaseNames = true,
                                                     string    toolTip             = ""
                                                    )
        {
            m_UseHelpBox          = useHelpBox;
            m_SplitCamelCaseNames = splitCamelCaseNames;
            m_Tooltip             = toolTip;
        }
    }
}
