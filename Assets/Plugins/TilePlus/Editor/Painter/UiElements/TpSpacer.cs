// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 01-01-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 01-01-2023
// ***********************************************************************
// <copyright file="TpSpacer.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Simple spacer element</summary>
// ***********************************************************************

using UnityEngine.UIElements;

namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// Simple spacer element
    /// </summary>
    public class TpSpacer : VisualElement

    {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="height">spacer height</param>
        /// <param name="width">spacer width</param>
        public TpSpacer(float height, float width)
        {
            name = $"tp-spacer-{height}H-{width}W";
            style.height = height;
            style.width = width;
            style.minHeight = height;
            style.minWidth = width;
            
        }
    }
}
