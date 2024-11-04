// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 01-01-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-17-2022
// ***********************************************************************
// <copyright file="TpHeader.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Custom label</summary>
// ***********************************************************************

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TilePlus.Editor.Painter
{

    /// <summary>
    /// Customized content header label 
    /// </summary>
    public class TpHeader: Label
    {
        /// <summary>
        /// A styled Label for the TPV: use as a header on help pages, etc.
        /// </summary>
        /// <param name="message">text content</param>
        /// <param name="elementName">name of this element</param>
        /// <param name = "largeSize" >if true, use larger font size</param>
        public TpHeader(string message, string elementName, bool largeSize = true): base(message)
        {
            name                          = elementName;
            style.color                   = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            style.unityTextAlign          = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);
            style.paddingBottom           = new StyleLength(8);
            style.fontSize                =  largeSize ? 18 : 12;
            style.unityFontStyleAndWeight = FontStyle.Bold;
        }
        
    }

}


