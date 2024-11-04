// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 01-01-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-31-2022
// ***********************************************************************
// <copyright file="TpToolbar.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Custom toolbar</summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

#nullable enable

namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// A specification for a toolbar button item
    /// </summary>
    public class ToolbarItemSpec
    {
        /// <summary>
        /// An arbitrary index, can be an (int)enum value if desired.
        /// </summary>
        public readonly int m_Index;
        /// <summary>
        /// Tooltip for the button
        /// </summary>
        public readonly string m_ToolTip;
        /// <summary>
        /// Abbreviated tooltip 
        /// </summary>
        public readonly string m_AbbreviatedToolTip;
        
        /// <summary>
        /// Optional image for the button
        /// </summary>
        public readonly Texture2D? m_ButtonTexture;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="index">index to be used for decoding where to send an event</param>
        /// <param name="toolTip">tooltip for the button</param>
        /// <param name = "abbreviatedToolTip" >Abbreviated tooltip (like AO for ALT+O) generated programmatically</param>
        /// <param name="buttonTexture">Texture for the button</param>
        public ToolbarItemSpec(int index, string toolTip, string abbreviatedToolTip, Texture2D? buttonTexture = null)
        {
            m_Index              = index;
            m_ToolTip            = toolTip;
            m_AbbreviatedToolTip = abbreviatedToolTip;
            m_ButtonTexture      = buttonTexture;
        }
    }

    /// <summary>
    /// Buttons are told that this is the event eventTarget. An instance of this class
    /// is bound to the button and the EventHandler method sends it to the correct eventTarget.
    /// </summary>
    public class EventDirector
    {
        /// <summary>
        /// ref to the toggle for which this is the event eventTarget
        /// </summary>
        public ToolbarToggle? m_MyToggle;
        /// <summary>
        /// All the toggles
        /// </summary>
        private readonly List<ToolbarToggle> toggles;
        /// <summary>
        /// The specific eventTarget for this button, which is an Action or method w an int parameter.
        /// </summary>
        private readonly Action<int> target;
        /// <summary>
        /// The index ie which button, is sent to the eventTarget as a parameter
        /// </summary>
        private readonly int myIndex;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="index"></param>
        /// <param name="target"></param>
        /// <param name = "toggles" >A list of all the toggles</param>
        public EventDirector(int index, Action<int> target, List<ToolbarToggle> toggles)
        {
            myIndex    = index;
            this.target  = target;
            this.toggles = toggles;
        }

        /// <summary>
        /// One of these is created for each button. All have the same eventTarget.
        /// The eventTarget is an Action&lt;int&gt; and the int is the index of the particular button.
        /// </summary>
        public void EventHandler(ChangeEvent<bool> evt, ToolbarToggle _)
        {
            foreach (var toggle in toggles)
            {
                var selected = toggle == m_MyToggle;
                toggle.SetValueWithoutNotify(selected);
                if (!selected)
                    toggle.style.borderBottomColor = toggle.style.backgroundColor;
                else
                    toggle.style.borderBottomColor = EditorGUIUtility.isProSkin
                                                         ? Color.white
                                                         : Color.black;
            }

            evt.StopPropagation();
            target.Invoke(myIndex);   
        }

    }

    /// <summary>
    /// Create a mutually exclusive toggle-button toolbar. 
    /// </summary>
    public class MutuallyExclusiveToolbar: Toolbar
    {
        private readonly List<ToolbarToggle>? toggles;
        private readonly List<string>?        tooltips;
        private          List<EventDirector>? directors;
        
        /// <summary>
        /// Create a toggle-button toolbar
        /// </summary>
        /// <param name="specs">specifications for each button</param>
        /// <param name="eventTarget">Where to send events. This eventTarget is sent the index value from a ToolbarItemSpec</param>
        /// <param name = "height" ></param>
        /// <param name="initialSelectionIndex">Which button is initially active. Note that this value must match
        /// one of the index values in 'specs'</param>
        public MutuallyExclusiveToolbar(List<ToolbarItemSpec> specs, Action<int> eventTarget, float height, int initialSelectionIndex = 0)
        {
            var num = specs.Count;

            toggles                 = new List<ToolbarToggle>(num);
            tooltips                = new List<string>(num);
            name                    = "muex_toolbar";
            style.height            = new StyleLength(StyleKeyword.Auto);
            style.minWidth          = 100;
            style.flexGrow          = 0;
            style.flexShrink        = 1;
            style.marginBottom      = 1;
            style.marginTop         = 2;
            style.paddingBottom     = 1;
            style.borderBottomWidth = 0;
            style.flexWrap          = new StyleEnum<Wrap>(Wrap.Wrap);
            

            for (var i = 0; i < num; i++)
            {
                var spec = specs[i];

                var director             = new EventDirector(spec.m_Index, eventTarget, toggles);
                var thisToggleIsSelected = initialSelectionIndex == spec.m_Index;

                var tog = TpToolbar.CreateToolbarToggleWithStyle(director.EventHandler,
                                                                        spec.m_ToolTip,
                                                                        thisToggleIsSelected,
                                                                        height,
                                                                        spec.m_ButtonTexture);


                tog.userData = director; //ensures no GC for this instance of director
                director.m_MyToggle         = tog;
                toggles.Add(tog);
                tooltips.Add(spec.m_ToolTip);
                Add(tog); 
            }

        }

        /// <summary>
        /// Set a button active by changing its appearance.
        /// </summary>
        /// <param name="index">index of the button</param>
        /// <param name="notify">if false, uses SetValueWithoutNotify</param>
        public void SetButtonActive(int index, bool notify = false)
        {
            if (toggles == null)
                return;
            for(var i = 0; i < toggles.Count; i++)
            {
                
                var toggle   = toggles[i];
                var selected = i == index;

                if (!selected)
                {
                    toggle.style.borderBottomColor = toggle.style.backgroundColor;
                    
                }
                else
                {
                    toggle.style.borderBottomColor = EditorGUIUtility.isProSkin
                                                         ? Color.white
                                                         : Color.black;
                }

                if (notify && selected)  // if we're deactivating the button then selected==false and we don't notify. Necc to avoid extra notifications.
                    toggle.value = true;
                else
                    toggle.SetValueWithoutNotify(selected);

                
            }

        }

       /// <summary>
       /// Enable or disable a button
       /// </summary>
       /// <param name="index">index of the button </param>
       /// <param name="enable">enable or disable</param>
        public void SetButtonEnabled(int index, bool enable)
        {
            if(toggles == null)
                return;
            var toggle = toggles[index];
            toggle.SetEnabled(enable);
        }

        /// <summary>
        /// Set a button visible or invisible
        /// </summary>
        /// <param name="tool">which button</param>
        /// <param name="willBeVisible">true to make visible</param>
        public void SetButtonVisibility(TpPainterTool tool, bool willBeVisible)
        {
            if(toggles == null)
                return;
            var toggle = toggles[(int)tool];
            toggle.style.display = new StyleEnum<DisplayStyle>(willBeVisible
                                                                   ? DisplayStyle.Flex
                                                                   : DisplayStyle.None);
        }

        /// <summary>
        /// Set the tooltip for a tool button
        /// </summary>
        /// <param name="tool">TpPainterTool enum value</param>
        /// <param name="tip">the string to use, if empty uses the default</param>
        public void SetToolTip(TpPainterTool tool, string tip)
        {
            if(toggles == null)
                return;
            var index  = (int)tool;
            var toggle = toggles[index];
            toggle.tooltip = string.IsNullOrEmpty(tip)
                                 ? tooltips![index]
                                 : $"{tip} {TpPainterShortCuts.GetToolTipForTool(tool)}";
        }
        
        
    }

    


    /// <summary>
    /// UiElements classes
    /// </summary>
    public static class TpToolbar 
    {
        /// <summary>
        /// Create a mutually-exclusive, Toggle-button based, toolbar
        /// </summary>
        /// <param name="spec">A list of ToolbarItemSpec</param>
        /// <param name="eventTarget">The destination for events</param>
        /// <param name = "baseHeight" ></param>
        /// <param name="initialSelectionIndex">which toggle is initially ON. Note that this value must match one of the index values in 'spec'</param>
        /// <returns></returns>
        
        public static VisualElement CreateMutuallyExclusiveToolbar(List<ToolbarItemSpec> spec,
            Action<int> eventTarget,
            float baseHeight = 25,
            int initialSelectionIndex = 0)
        {
            var ve = new VisualElement
                     {
                         name = "main-toolbar-container",
                         style =
                         {
                             marginTop     = 1,
                             flexGrow      = 0,
                             flexDirection = FlexDirection.Row,
                             flexShrink    = 0,
                             paddingRight = 10
                         }
                     };
            
            
            var toolbar = new MutuallyExclusiveToolbar(spec,
                                                       eventTarget,
                                                       baseHeight,
                                                       initialSelectionIndex)
                          {
                              style =
                              {
                                  backgroundColor = new StyleColor(Color.clear)
                              }
                          };
            ve.Add(toolbar);
            return ve;
        }


        

        /// <summary> 
        /// Create a styled toolbar toggle
        /// </summary>
        /// <param name="clickEvent">eventTarget event</param>
        /// <param name="tooltip">tooltip for the toggle</param>
        /// <param name="initialValue">initital value for the toggle</param>
        /// <param name = "height" >height of the toggle</param>
        /// <param name="image">if supplied, controlLabel not used</param>
        /// <returns>ToolbarToggle instance</returns>
        public static ToolbarToggle CreateToolbarToggleWithStyle(
            EventCallback<ChangeEvent<bool>, ToolbarToggle> clickEvent,
            string                                          tooltip,
            bool                                            initialValue,
            float                                           height,
            Texture2D?                                      image
            )
            
        {
            //const float imgPad     = 4;
            var         thisToggle = new ToolbarToggle {  tooltip = tooltip, userData = tooltip};
            var         style      = thisToggle.style;
            var         sHeight    = (StyleLength)height;

            var isProSkin = EditorGUIUtility.isProSkin;

            style.flexDirection = FlexDirection.Column;
            
            style.minWidth    = sHeight;
            style.minHeight   = sHeight;
            style.alignItems  = new StyleEnum<Align>(Align.Center); 
            style.marginLeft  = 4;
            style.marginRight = 4;
            
            /*style.width         = height + imgPad  ;
            style.height        = height + imgPad + 2 ;*/
            style.paddingBottom = 0;
            style.paddingTop    = 0;
            style.paddingLeft   = 0;
            style.paddingRight  = 0;
            style.paddingBottom = 0;
            
            style.borderBottomWidth = 2;
            style.borderTopWidth    = 1;
            style.borderLeftWidth   = 1;
            style.borderRightWidth  = 1;

            var borderColor = Color.gray; 
            borderColor.a          = 0.2f;
            
            style.borderTopColor   = borderColor;
            style.borderLeftColor  = borderColor;
            style.borderRightColor = borderColor;

            style.justifyContent = Justify.Center;

            if (!initialValue)
                style.borderBottomColor = style.backgroundColor;
            else
                style.borderBottomColor = isProSkin
                                              ? Color.white
                                              : Color.black;


            var tint = Color.white;
            if(!EditorGUIUtility.isProSkin) 
                tint = Color.black;
            thisToggle.Add(new Image
                           {
                               name = "toggle-icon",
                               image = image,
                               tintColor = tint,
                               style =
                               {
                                   //alignItems = Align.Center,
                                   //alignSelf = Align.Center,
                                   minWidth = sHeight,
                                   minHeight = sHeight,
                                   height = sHeight,
                                   width = sHeight,
                                   
                               }
                           }); 
                
            thisToggle.Q<VisualElement>("unity-checkmark").parent.RemoveFromHierarchy();

            thisToggle.SetValueWithoutNotify(initialValue);
            thisToggle.RegisterCallback(clickEvent,thisToggle);
            return thisToggle;
        }
    }
}
