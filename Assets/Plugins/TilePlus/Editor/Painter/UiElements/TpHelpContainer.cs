// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 02-05-2024
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 02-05-2024
// ***********************************************************************
// <copyright file="TpHelpContainer.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
using UnityEngine.UIElements;

namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// Expandable container for a prompt that expands with different text when button is clicked.
    /// </summary>
    public class TpHelpContainer : VisualElement
    {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="prompt">what's shown initially</param>
        /// <param name="content">what's shown after button click</param>
        public TpHelpContainer(string prompt, string content)
        {
            style.minHeight     = 20;
            style.paddingBottom = 1;
            style.paddingLeft   = 4;
            style.paddingRight  = 4;
            style.paddingTop    = 1;
            style.flexDirection = FlexDirection.Row;
            style.flexShrink    = 0;
            style.flexGrow      = 0;
            userData            = false;
            var helpLabel = new Label(prompt)
                            {
                                style = {whiteSpace          = WhiteSpace.Normal, 
                                               flexGrow      = 1,
                                               flexShrink = 0.1f, 
                                               paddingLeft = 2, paddingRight = 2,
                                               paddingBottom = 2, 
                                               overflow      = new StyleEnum<Overflow>(Overflow.Hidden),
                                               
                                           },
                                enableRichText = true
                                
                            };
            var iconSize = TilePlusConfig.instance.SelInspectorButtonSize;

            var toggle = new TpImageToggle(b =>
                                           {
                                               helpLabel.text = b
                                                                    ? content
                                                                    : prompt;
                                               
                                               
                                           }, "toggle", "Click to see more", iconSize, TpIconLib.FindIcon(TpIconType.InfoIcon));
            Add(toggle);
            Add(helpLabel);

        }
        
        
    }
}
