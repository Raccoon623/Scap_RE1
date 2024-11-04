// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 12-28-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-28-2023
// ***********************************************************************
// <copyright file="StringEntryDialog.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
#nullable enable
namespace TilePlus.Editor.Painter
{

    /// <summary>
    /// A simple dialog that can be used to get a string from a user.
    /// </summary>
    public class StringEntryDialog : EditorWindow
    {
        private string          msg        = string.Empty;
        private TextField?      textField;
        private Action<string>? cBack;
        private string          initialVal = string.Empty;
        private string          ok         = string.Empty;
        private string          cancel     = string.Empty;
        private bool            complete;

        
        
        /// <summary>
        /// Show the dialog
        /// </summary>
        /// <param name="winTitle">Title of the window</param>
        /// <param name="message">a prompt</param>
        /// <param name="initialValue">initial string to put in textfield</param>
        /// <param name = "cancelButtonLegend" >text for the cancel button. if null or empty the button isn't shown.</param>
        /// <param name="callback">a callback to provide the string to the caller. An empty string indicates the cancel button (if present) was used.</param>
        /// <param name = "okButtonLegend" >text for the OK button</param>
        /// <remarks>clicking OK with an empty string in the textfield restores the initialValue.</remarks>
        public void ShowStringEntryDialog(string winTitle, string message, string initialValue, string okButtonLegend, string cancelButtonLegend, Action<string> callback)
        {
            msg         = message; 
            initialVal  = initialValue;
            cBack       = callback;
            ok     = okButtonLegend;
            cancel = cancelButtonLegend;
            var wnd = GetWindow<StringEntryDialog>();
            wnd.maxSize      = new Vector2(256, 256);
            wnd.titleContent = new GUIContent(winTitle);
            wnd.ShowModal();
        }

        internal void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // VisualElements objects can contain other VisualElement following a tree hierarchy.
            root.Add(new TpSpacer(10,10));
            VisualElement label = new Label(msg);
            root.Add(label);
            root.Add(new TpSpacer(10, 10));
            
            textField           = new TextField(32, false, false, ' ') {isDelayed = true, value = initialVal};
            textField.RegisterValueChangedCallback(OnEnterPressed);
            textField.focusable = true;
            textField.schedule.Execute(() => textField.Focus()).StartingIn(100);
            
            root.Add(textField);
            root.Add(new TpSpacer(10, 10));
            var region = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
            root.Add(region);
            region.Add(new Button(OnOk){text = (string.IsNullOrWhiteSpace(ok)? "OK" :ok)});
            if(!string.IsNullOrWhiteSpace(cancel))
                region.Add(new Button(OnCancel){text = cancel});
            
        }

        
        private void OnEnterPressed(ChangeEvent<string> evt)
        {
            if (string.IsNullOrWhiteSpace(evt.newValue))
                return;
            OnOk();
        }

        
        private string ProcessedString () 
        {
            var s = textField!.value;
            if (string.IsNullOrWhiteSpace(s))
            {
                textField.value = initialVal;
                return string.Empty;
            }

            s = s.Trim();
            if (string.IsNullOrWhiteSpace(s))
            {
                textField.value = initialVal;
                return string.Empty;
            }

            return s;
        }

        private void OnOk()
        {

            var s = ProcessedString();
            if(s == string.Empty)
                return;
            complete = true;
            cBack?.Invoke(s);
            Close();
        }

        private void OnCancel()
        {
            complete = true;
            cBack?.Invoke(string.Empty);
            Close();
        }

        private void OnDestroy()
        {
            if(!complete)
                cBack?.Invoke(ProcessedString());
        }
    }
}
