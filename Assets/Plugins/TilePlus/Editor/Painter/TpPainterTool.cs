// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 11-01-2022
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-31-2022
// ***********************************************************************
// <copyright file="TpPainterTool.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Unity tool for Tile+Painter</summary>
// ***********************************************************************

using System.Linq;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Tilemaps;
#nullable enable

namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// The Editor Tool for the Tile+Painter
    /// </summary>
    [EditorTool("Tile+Painter", typeof(Tilemap))]
    public class TilePlusPainterTool : EditorTool
    {
        private static GUIContent? s_ToolbarIcon;
        
        //Note that this gets called AFTER the editor has made this the active tool.
        /// <inheritdoc />
        public override void OnActivated()
        {
            // ReSharper disable once Unity.NoNullPatternMatching
            if (target==null || target is not Tilemap)
            {
                Debug.Log("NOT TILEMAP");
                return;
            }

            if (EditorWindow.HasOpenInstances<TilePlusPainterWindow>())
            {
                //note that using the RawInstance property will NOT automatically
                //open the window if it isn't open already
                var instance = TilePlusPainterWindow.RawInstance;
                if (instance != null)
                {
                    instance.m_ToolActivated = true;
                    instance.OnEditorSelectionChangeFromTool(target);
                    if(TpLibEditor.Informational)
                        TpLib.TpLog("Tile+Painter tool activated.");
                    return;
                }
            }
            
            //if no open instance, reading this property will create one.
            var p = TilePlusPainterWindow.instance; 
            if(p==null)
                return;
            p.m_ToolActivated = true;
            if(TpLibEditor.Informational)
                TpLib.TpLog("Tile+Painter tool activated.");
            p.OnEditorSelectionChangeFromTool(target);
        }

        /// <inheritdoc />
        public override bool IsAvailable()
        {
            if (targets==null || targets.Count() > 1)
                return false;

            // ReSharper disable once Unity.NoNullPatternMatching
            return target!=null && target is Tilemap;
            
        }

        /// <inheritdoc />
        public override void OnWillBeDeactivated()
        {
            base.OnWillBeDeactivated();
            var win = TilePlusPainterWindow.RawInstance; //this gets the instance w/o reopening the window. 
            if(win == null)
                return;
            win.m_ToolActivated = false;
            if(TpLibEditor.Informational)
                TpLib.TpLog("Tile+Painter tool deactivated.");
            //this becomes really annoying...
            /*if(TilePlusConfig.instance.TpPainterSyncSelection)
                win.Close();*/
        }

        /// <inheritdoc/>
        public override GUIContent toolbarIcon
        {
            get
            {
                if (s_ToolbarIcon != null)
                    return s_ToolbarIcon;
                s_ToolbarIcon = new GUIContent(
                                               TpIconLib.FindIcon(TpIconType.TptIcon),
                                               "TilePlus Painter");
                return s_ToolbarIcon;
            }
        }
        
        
    }


}
