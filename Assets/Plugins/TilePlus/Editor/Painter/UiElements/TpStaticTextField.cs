// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 01-04-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 01-12-2023
// ***********************************************************************
// <copyright file="TpStaticTextField.cs" company="Jeff Sasmor">
//     Copyright (c) 2023 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************

using UnityEngine.UIElements;
using FontStyle = UnityEngine.FontStyle;
#nullable enable

namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// A UI Elements multiline text field that's not focusable ie not editable. 
    /// </summary>
    public class TpStaticTextField : TextField
    {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="itemName">name of this element</param>
        /// <param name="displayInitially">display it initially? style.display control</param>
        /// <param name="boldFont">use bold font? style.unityFontStyleAndWeight control </param>
        /// <param name = "text" >initial text (optional)</param>
        public TpStaticTextField(string itemName, string text = "", bool displayInitially = true, bool boldFont = true)
        {
            focusable                     = false;
            multiline                     = true;
            name                          = itemName;
            style.whiteSpace              = WhiteSpace.Normal;
            style.flexGrow                = 0;
            if(boldFont)
                style.unityFontStyleAndWeight = FontStyle.Bold;
            if (!displayInitially)
                style.display = DisplayStyle.None;
            if (!string.IsNullOrWhiteSpace(text))
                this.text = text;
        }

    }
}
