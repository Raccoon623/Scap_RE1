// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 01-01-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-21-2022
// ***********************************************************************
// <copyright file="TpListView.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Custom ListView</summary>
// ***********************************************************************
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace TilePlus.Editor.Painter
{

    /// <summary>
    /// Custom ListView
    /// </summary>
    public class TpListView : ListView
    {
        private readonly ScrollView scrollView;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="items">list of items to display</param>
        /// <param name="listItemHeight">height of items</param>
        /// <param name="showListBorder">show a border</param>
        /// <param name="makeListItem">make delgate</param>
        /// <param name="bindListItem">bind delegate</param>
        /// <param name = "background" >alt row bg mode</param>
        /// <param name = "gridSetup" >Unused at this time</param>
        /// <param name = "useDynamicHeight" >Use the dynamic height feature. If false, use fixed height/</param>
        /// <returns></returns>
        public TpListView(IList                      items,
                          float                      listItemHeight,
                          bool                       showListBorder,
                          Func<VisualElement>        makeListItem,
                          Action<VisualElement, int> bindListItem,
                          AlternatingRowBackground   background = AlternatingRowBackground.None, 
                          bool gridSetup = false,
                          // ReSharper disable once UnusedParameter.Local
                          bool useDynamicHeight = false):base(items, listItemHeight,makeListItem,bindListItem)            

        {
            showBorder                                    = showListBorder;
            showAlternatingRowBackgrounds                 = background;

            style.borderLeftWidth                    = 2;
            style.borderRightWidth                   = 2;
            style.borderTopWidth                     = 2;
            style.borderBottomWidth                  = 2;
            style.paddingLeft =                         4;

            virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            
            
            //unfortunately, the list view doesn't handle a 'grid' setup: viewport is messed up:
            // 1. only shows a few of the items when there are many even though there's room
            // 2. does not provide the correct selection.
            /*if (gridSetup)
            {
                //listView.virtualizationMethod       = CollectionVirtualizationMethod.DynamicHeight;
                var container = listView.Q<VisualElement>("unity-content-container");
                container.style.flexWrap      = new StyleEnum<Wrap>(Wrap.Wrap);
                container.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
                
                
            }*/
            if(gridSetup)
                Debug.LogError("GridSetup param for TpListView not supported yet.");
            
            
            
            scrollView = this.Q<ScrollView>(null, "unity-scroll-view");
            scrollView.style.borderBottomWidth = 4;
            scrollView.style.borderRightWidth  = 2;
        }

        /// <summary>
        /// Change the scroller mode for this list.
        /// </summary>
        public ScrollViewMode ScrollViewMode
        {
            get => scrollView?.mode ?? ScrollViewMode.VerticalAndHorizontal;
            set
            {
                if(scrollView != null)
                    scrollView.mode = value;
            }
        }

        /// <summary>
        /// Set vertical/horiz scroller visibility
        /// </summary>
        /// <param name="h">if true, Horiz scroller always on, false = auto</param>
        /// <param name="v">if true, Vert scroller always on, false = auto</param>
        public void ScrollerControl(bool h, bool v)
        {
            if(scrollView == null)
                return;

            
            scrollView.horizontalScrollerVisibility = h ? ScrollerVisibility.AlwaysVisible : ScrollerVisibility.Auto;
            scrollView.verticalScrollerVisibility   = v ? ScrollerVisibility.AlwaysVisible : ScrollerVisibility.Auto;
        }

        /// <summary>
        /// Set the virtualization method. Fixed or Dynamic
        /// </summary>
        /// <param name="fixedHeight">true for fixedHeight or false for Dynamic</param>
        public void SetVirtualizationMethod(bool fixedHeight)
        {
            virtualizationMethod = fixedHeight ? CollectionVirtualizationMethod.FixedHeight : CollectionVirtualizationMethod.DynamicHeight;
            this.Rebuild();
        }
        
        
    }
}
