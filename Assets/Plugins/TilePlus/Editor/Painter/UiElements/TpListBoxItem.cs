// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 01-01-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 01-22-2023
// ***********************************************************************
// <copyright file="TpListBoxItem.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Custom list box item</summary>
// ***********************************************************************

using System;
using UnityEngine;
using UnityEngine.UIElements;
#nullable enable

namespace TilePlus.Editor.Painter
{

    /// <summary>
    /// A list box item for the Tile+Painter
    /// </summary>
    public class TpListBoxItem : VisualElement
    {
        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        [SerializeField]
        private  Action<int,ClickEvent>? callback; //this should NOT be readonly since these are pooled (I think).
        internal int                     m_Index;
        [SerializeField]
        private  Color                   bColor;

        private readonly Color initialColor;
        
        /// <summary>
        /// Create a list box item
        /// </summary>
        /// <param name = "elementName" >Name of this element</param>
        /// <param name="borderColor">Border Color</param>
        /// <param name="borderWidth">Border Width (1)</param>
        /// <param name="radius">Border radius (4)</param>
        /// <param name = "onClickCallback" >Optional callback if this item is clicked.</param>
        public TpListBoxItem(string elementName, Color borderColor, float borderWidth = 1f, float radius = 4f, Action<int, ClickEvent>?onClickCallback=null )
        {
            name                          = elementName;
            bColor                        = borderColor;
            initialColor                  = borderColor;
            style.flexDirection           = FlexDirection.Row;
            style.alignItems              = Align.Center;
            style.unityTextAlign          = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);
            style.paddingBottom           = 1;
            style.paddingTop              = 1;
            style.borderBottomWidth       = borderWidth;
            style.borderTopWidth          = borderWidth;
            style.borderRightWidth        = borderWidth;
            style.borderLeftWidth         = borderWidth;
            style.borderBottomColor       = borderColor;
            style.borderTopColor          = borderColor;
            style.borderLeftColor         = borderColor;
            style.borderRightColor        = borderColor;
            style.borderBottomLeftRadius  = radius;
            style.borderBottomRightRadius = radius;
            style.borderTopLeftRadius     = radius;
            style.borderTopRightRadius    = radius;
            style.overflow                = Overflow.Hidden; 


            if (onClickCallback == null)
                return;
            callback = onClickCallback;
            RegisterCallback<ClickEvent>(ClickEventCallback);
        }

        /// <summary>
        /// Change the border color. Ignores if it's the same color.
        /// </summary>
        /// <param name="c"></param>
        public void SetColor(Color c)
        {
            if (bColor == c)
                return;
            bColor                  = c;
            style.borderBottomColor = c;
            style.borderTopColor    = c;
            style.borderLeftColor   = c;
            style.borderRightColor  = c;

        }

        /// <summary>
        /// restore the original color.
        /// </summary>
        public void ResetColor()
        {
            SetColor(initialColor);
        }

        /// <summary>
        /// The current border color.
        /// </summary>
        public Color BorderColor => bColor;
        
        private void ClickEventCallback(ClickEvent evt)
        {
            callback?.Invoke(m_Index,evt);
        }

        internal void DoUnregisterCallback()
        {
            UnregisterCallback<ClickEvent>(ClickEventCallback);
        }
        
    }
    
    
}
