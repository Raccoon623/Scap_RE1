// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 01-01-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 1-15-2024
// ***********************************************************************
// <copyright file="TpEditorBridge.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Editor functions made available for Non-Editor callers</summary>
// ***********************************************************************

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TilePlus
{
    /// <summary>
    /// Enum for getting an Icon from Resources.
    /// It's here way so TPT tile editors can access
    /// Icons. But those do NOT exist in a built app.
    /// Aside from BridgeFindIcon, none of your code should
    /// use any bridged item found in this class.
    /// </summary>
    [SuppressMessage("ReSharper", "MissingXmlDoc")]
    public enum TpIconType
    {
        UnityPaintIcon,
        UnityEraseIcon,
        UnityRotateCcwIcon,
        UnityRotateCwIcon,
        UnityFlipXIcon,
        UnityFlipYIcon,
        UnityXIcon,
        UnityMoveIcon,
        UnityPickIcon,
        UnityToolbarMinusIcon,
        TptIcon,
        LockedIcon,
        UnLockedIcon,
        PaletteIcon,
        ClipboardIcon,
        PrefabIcon,
        TileIcon,
        TileFabIcon,
        TilemapIcon,
        HelpIcon,
        SettingsIcon,
        InfoIcon,
        FilterIcon,
        ArrowUp,
        ArrowDown,
        ArrowRight,
        TrashIcon,
        PlusIcon,
        MinusIcon,
        EyeballIcon,
        LifepreserverIcon,
        SyncIcon,
        RefreshIcon,
        UnityRefreshIcon,
        UnityOrbitIcon,
        UnityTextAssetIcon,
        TpTileBundleIcon,
        PinIcon,
        UnityTransformIcon,
        UnityCameraIcon,
        UnitySaveIcon,
        UnityRecordIcon,
        UnityForwardIcon,
        UnityEyedropperIcon,
        UnityGridIcon,
        UnitySelectIcon
    }
    
    /// <summary>
    /// Bridge to access specific editor functionality in non-editor code.
    /// DO NOT USE in an app destined to build because
    /// this static class is editor-only!
    /// </summary>
    /// <remarks>NEVER write to these. After a reload, the first
    /// value written to any of these properties is the last one
    /// until a reload.</remarks>
    public static class TpEditorBridge
    {
        
        private static EditorDomainPrefs s_DomainPrefs;

        /// <summary>
        /// Get domain reload preferences from Editor
        /// </summary>
        public static EditorDomainPrefs DomainPrefs
        {
            get => s_DomainPrefs;
            set => s_DomainPrefs ??= value;
        }
        
        private static Func<TpIconType, Texture2D> s_IconLink;
        
        /// <summary>
        /// Use this to get an editor icon.
        /// for example: TpEditorBridge.BridgeFindIcon(TpIconType.InfoIcon)
        /// will return an InfoIcon as a Texture2D
        /// </summary>
        public static Func<TpIconType,Texture2D>BridgeFindIcon 
        {
            get => s_IconLink;
            set => s_IconLink ??= value;
        }

        private static Func<Texture2D, Sprite> s_SpriteFromTexLink;

        //TpEditorBridge.BridgeSpriteFromTex = SpriteFromTexture;
        /// <summary>
        /// Use this to convert a texture2D to a sprite.
        /// </summary>
        public static Func<Texture2D, Sprite> BridgeSpriteFromTex
        {
            get => s_SpriteFromTexLink;
            set => s_SpriteFromTexLink ??= value;
        }

       
        private static Func<int, bool> s_IsPreviewActiveForIdLink;

        /// <summary>
        /// Is a preview active for Object with IID?
        /// </summary>
        public static Func<int,bool> BridgeIsPreviewActiveForId
        {
            get => s_IsPreviewActiveForIdLink;
            set => s_IsPreviewActiveForIdLink ??= value;
        }


        private static Func<bool> s_IsPreviewActive;

        /// <summary>
        /// Are any previews active
        /// </summary>
        public static Func<bool> BridgeIsPreviewActive
        {
            get => s_IsPreviewActive;
            set => s_IsPreviewActive ??= value;
        }


        private static Action s_BridgeClearPreview;

        /// <summary>
        /// Clear previews
        /// </summary>
        public static Action BridgeClearPreview
        {
            get => s_BridgeClearPreview;
            set => s_BridgeClearPreview ??= value;
        }
        /*
         int id,  Tilemap?                     tileParent,
                                                  TpTileFab                    tileFab,
                                                  Vector3Int                   offset,
                                                  TpTileBundle.TilemapRotation rotation = TpTileBundle.TilemapRotation.Zero,
                                                  List<Tilemap>?               targets  = null)
         */
        private static Action<int, Tilemap, TpTileFab, Vector3Int, TpTileBundle.TilemapRotation, List<Tilemap>> s_PreviewTileFab;

        /// <summary>
        /// Preview a tilefab
        /// </summary>
        public static Action<int, Tilemap, TpTileFab, Vector3Int, TpTileBundle.TilemapRotation, List<Tilemap>> BridgePreviewTileFab
        {
            get => s_PreviewTileFab;
            set => s_PreviewTileFab ??= value;
        }
        
        

        /// <summary>
        /// Get a Game Object preview
        /// </summary>
        public static Func<GameObject, (Texture2D tex, bool found)> BridgeGameObjPreview { get; set; }
        
        /// <summary>
        /// Preview a game object. 
        /// </summary>
        /// <param name="gameObject">the source GO </param>
        /// <returns>(Texture2D tex, bool found)</returns>
        public static (Texture2D tex, bool found) BridgePreviewGameObject(GameObject gameObject)
        {
            if (BridgeGameObjPreview == null || gameObject == null)
                return (null, false);
            return BridgeGameObjPreview(gameObject);
        }
        
        /// <summary>
        /// Draw a marquee
        /// </summary>
        /// <value>The tilemap marquee.</value>
        // ReSharper disable once Unity.RedundantAttributeOnTarget
        public static Func<GridLayout, BoundsInt, Color, float, bool, Guid?, ulong> TilemapMarquee
        {
            get => s_TilemapMarquee;
            set => s_TilemapMarquee ??= value;
        }
        /// <summary>
        /// TpLibEditor initializes this so that code that's not
        /// in editor-space can use the Marquee feature.
        /// </summary>>
        private static Func<GridLayout, BoundsInt, Color, float, bool, Guid?, ulong> s_TilemapMarquee;

        /// <summary>
        /// Support for persistent marquee
        /// </summary>
        public static Func<ulong,bool, bool> UntimedMarqueeActive
        {
            get => s_UntimedMarqueeActive;
            set => s_UntimedMarqueeActive ??= value;
        }
        
        /// <summary>
        /// TpLibEditor initializes this so that code that's not
        /// in editor-space can use the Marquee feature.
        /// </summary>>
        private static Func<ulong,bool, bool> s_UntimedMarqueeActive;


        /// <summary>
        /// Support for persistent marquee
        /// </summary>
        public static Func<ulong,bool,bool> TimedMarqueeActive
        {
            get => s_TimedMarqueeActive;
            set => s_TimedMarqueeActive ??= value;
        }

        /// <summary>
        /// TpLibEditor initializes this so that code that's not
        /// in editor-space can use the Marquee feature.
        /// </summary>>
        private static Func<ulong,bool, bool> s_TimedMarqueeActive;
        
        
        
        
        
        
        /// <summary>
        /// TileCustomGui initializes this.
        /// </summary>
        private static Func<TilePlusBase, GUISkin, Vector2, bool, CustomGuiReturn> s_BaseCustomGui;

        /// <summary>
        /// TileCustomGui initializes this.
        /// </summary>
        public static Func<TilePlusBase, GUISkin, Vector2, bool, CustomGuiReturn> BaseCustomGui
        {
            get => s_BaseCustomGui;
            set => s_BaseCustomGui ??= value;
        }

        /// <summary>
        /// Focus the scene camera.
        /// </summary>
        public static Action<Vector3> FocusSceneCamera
        {
            get => s_Focus;
            set => s_Focus ??= value;
        }
        
        /// <summary>
        /// TpLibEditor initializes this so that code that's not
        /// in editor-space can use the Marquee feature.
        /// </summary>>
        private static Action<Vector3> s_Focus;

        
        /// <summary>
        /// Show info messages
        /// </summary>
        private static bool s_Informational;
        /// <summary>
        /// Show warnings
        /// </summary>
        private static bool s_Warnings;
        /// <summary>
        /// Show errors
        /// </summary>
        private static bool s_Errors = true;
        
        /// <summary>
        /// Set up message types
        /// </summary>
        /// <param name="informational"></param>
        /// <param name="warnings"></param>
        /// <param name="errors"></param>
        public static void SetMessageTypes(bool informational, bool warnings, bool errors)
        {
            s_Informational = informational;
            s_Warnings      = warnings;
            s_Errors        = errors;
        }
        
        
        /// <summary>
        /// Show informational messages. Very verbose
        /// </summary>
        public static bool Informational => s_Informational;
        /// <summary>
        /// Show warnings
        /// </summary>
        public static bool Warnings      => s_Warnings;
        /// <summary>
        /// show errors
        /// </summary>
        public static bool Errors        => s_Errors;
        
    }
}

#endif
