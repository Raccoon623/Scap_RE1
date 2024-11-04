// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 01-01-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-17-2022
// ***********************************************************************
// <copyright file="TpHelpBox.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Custom helpbox</summary>
// ***********************************************************************
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TilePlus.Editor.Painter
{

    /// <summary>
    /// Customized Help box for Tile+Viewer
    /// </summary>
    public class TpHelpBox : HelpBox
    {
        /// <summary>
        /// A styled help box for the TPV
        /// </summary>
        /// <param name="message">text content for the message box</param>
        /// <param name="elementName">name of this element</param>
        /// <param name = "messageType" >HelpBoxMessageType, defaults to .None</param>
        public TpHelpBox (string message, string elementName, HelpBoxMessageType messageType = HelpBoxMessageType.None) : base (message,messageType )
        {
            name                    = elementName;
            style.color             = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            style.borderBottomWidth = 0;
            style.borderTopWidth    = 0;
            style.borderLeftWidth   = 0;
            style.borderRightWidth  = 0;
            style.paddingBottom     = 5;
            style.paddingTop        = 5;
            style.whiteSpace        = WhiteSpace.Normal;
            style.alignItems        = Align.FlexStart;
            style.alignContent      = Align.FlexStart;
            //style.alignSelf         = Align.FlexStart;
            style.unityTextAlign    = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);

        }

        
        
        
        /// <summary>
        /// Get/Set background color
        /// </summary>
        public StyleColor BackGroundColor
        {
            get => style.backgroundColor;
            set
            {
                style.backgroundColor                 = value;
                this.Q<Label>().style.backgroundColor = value;
            }
        }

        /// <summary>
        /// Get/Set background alpha.
        /// </summary>
        public float BackGroundAlpha
        {
            get => style.backgroundColor.value.a;
            set
            {
                var c = style.backgroundColor.value;
                c.a                   = value;
                style.backgroundColor = c;
            }
        }
        
    }

}
