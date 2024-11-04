// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 01-01-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-22-2022
// ***********************************************************************
// <copyright file="TpImageToggle.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Custom Toggle</summary>
// ***********************************************************************
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
#nullable enable

namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// Custom Toggle
    /// </summary>
    /// <seealso cref="Toggle" />
    public class TpImageToggle : Toggle
    {
        private const    float         ImgPad = 1;
        private readonly Action<bool>? callback;
        private readonly Image         imageField;

        /// <summary>
        /// Initializes a new instance of the <see cref="TpImageToggle"/> class.
        /// </summary>
        /// <param name="callback">click callback.</param>
        /// <param name="name">name of the toggle.</param>
        /// <param name="tooltip">tooltip for the toggle.</param>
        /// <param name="height">height.</param>
        /// <param name="icon">icon.</param>
        public TpImageToggle(Action<bool> callback,
            string name,
            string tooltip,
            float height,
            Texture2D icon
        )
        {
            const float imgPad = 1;


            this.callback = callback;
            this.name = name;
            this.tooltip = tooltip;

            style.flexDirection = FlexDirection.Column;

            style.minWidth = height;
            style.minHeight = height;
            style.alignItems = Align.Center;
            style.marginRight = 4;
            style.marginTop = 1;
            style.marginBottom = 1;

            style.width = height + ImgPad + 1;
            style.height = height + ImgPad + 1;
            style.paddingBottom = imgPad;
            style.paddingTop = 1;
            style.paddingLeft = 1;
            style.paddingRight = 1;

            style.borderBottomWidth = 2;

            style.borderTopWidth = 1;
            style.borderLeftWidth = 1;
            style.borderRightWidth = 1;

            var borderColor = Color.gray;
            borderColor.a = 0.3f;

            style.borderTopColor = borderColor;
            style.borderLeftColor = borderColor;
            style.borderRightColor = borderColor;

            style.justifyContent = Justify.Center;

            var tint = Color.white;
            if (!EditorGUIUtility.isProSkin)
                tint = Color.black;

            imageField = new Image
            {
                name = "image-toggle__image",
                image = icon,
                tintColor = tint,
                style =
                {
                    alignItems = new StyleEnum<Align>(Align.Center),
                    minWidth = height,
                    height = height,
                    width = height
                }
            };
            Add(imageField);

            this.Q<VisualElement>("unity-checkmark").parent.RemoveFromHierarchy();
            RegisterCallback<ChangeEvent<bool>>(ClickHandler);
        }


        /// <summary>
        /// Set or change the toggle button's image.
        /// </summary>
        /// <param name="icon">Tex2D for the icon</param>
        public void SetImage(Texture2D icon)
        {
            imageField.image = icon;
        }

        private void ClickHandler(ChangeEvent<bool> evt)
        {
            SetButtonSelected(evt.newValue);
            callback?.Invoke(evt.newValue);
        }

        private void SetButtonSelected(bool selected)
        {
            if (!selected)
                style.borderBottomColor = style.backgroundColor;
            else
                style.borderBottomColor = EditorGUIUtility.isProSkin
                    ? Color.white
                    : Color.black;
        }

        /// <inheritdoc />
        public override void SetValueWithoutNotify(bool newValue)
        {
            SetButtonSelected(newValue);
            base.SetValueWithoutNotify(newValue);
        }
    }
}
