// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 01-27-2023
// ***********************************************************************
// <copyright file="ITilePlus.cs" company="Jeff Sasmor">
//     Copyright (c) 2023 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
#nullable enable
namespace TilePlus
{
    /// <summary>
    /// All tiles that want to use the special features of Tile+Brush need to implement this interface.
    /// NOTE: ensure that the methods and properties that are in the UNITY_EDITOR section below
    /// are placed in similarly-demarked regions in your code.
    /// Also be sure to provide backing fields for properties (if appropriate) to ensure serialization.
    /// </summary>
    /// <remarks>Inherit from TilePlusBase if at all possible! Using this interface alone will probably not be enough. 
    /// Also note the use of default interface properties.
    /// --User code should not write to this property -- for some properties: this means that the implementation does
    /// not (or should not) allow writes to the backing field when the Editor is in Play mode.</remarks>
    public interface ITilePlus
    {
        #region All
        /// <summary>
        /// The GUID for the tile as bytes.
        /// An implementation of 'set' should be write-once.
        /// </summary>
        /// <value>The tile unique identifier bytes.</value>
        /// <remarks>Guid isn't serializable but byte[] is.</remarks>
        byte[]? TileGuidBytes { get; set; }


        /// <summary>
        /// Get the Tile GUID as a GUID struct by converting TileGuidBytes.
        /// </summary>
        /// <value>The tile's GUID.</value>
        Guid TileGuid { get; }
        
        
        /// <summary>
        /// string representation of GUID
        /// </summary>
        /// <value>The tile unique identifier string.</value>
        string TileGuidString { get; }

        /// <summary>
        /// Id of tile. Nominally GetInstanceId. Default returns 0.
        /// </summary>
        int Id => 0;
        

        /// <summary>
        /// Return a Tag for classes which implement a tag. 
        /// </summary>
        /// <value>The tag.</value>
        /// <remarks>User code should not write to this property </remarks>
        string Tag { get; set; }

        /// <summary>
        /// Property to get an array of individually-trimmed tags.
        /// </summary>
        /// <remarks>returns null if there are no tags, or an empty list if all trimmed tags eval to empty></remarks>
        (int count, string [] tags) TrimmedTags { get; }
        
        /// <summary>
        /// Return the scene that the Tile is in.
        /// Only works on a placed tile; on a non-cloned tile it should return default
        /// Note: check the return value for Scene.IsValid.
        /// </summary>
        /// <value>The parent scene.</value>
        /// <remarks>Intentionally non-virtual</remarks>
        Scene ParentScene => ParentTilemap == null ? default : ParentTilemap.gameObject.scene;

        /// <summary>
        /// Get the the tile sprite clear mode. 
        /// </summary>
        /// <value>The SpriteClearMode value.</value>
        /// <remarks>User code should not write to this property </remarks>
        SpriteClearMode TileSpriteClear { get; set; }

        /// <summary>
        /// Get the tile collider mode.
        /// </summary>
        /// <value>The tile collider mode.</value>
        /// <remarks>User code should not write to this property </remarks>
        ColliderMode TileColliderMode { get; set; }

        /// <summary>
        /// This is the tile grid position as set from the Tile's StartUp method.
        /// This does not need a serialized backing field as it's refreshed during edit time
        /// and is refreshed at application start when StartUp is called.
        /// </summary>
        /// <value>The tile grid position.</value>
        Vector3Int TileGridPosition { get;}


        /// <summary>
        /// Get the last tile grid position. Nonserialized if use backing field.
        /// </summary>
        /// <value>The last tile grid position.</value>
        Vector3Int LastTileGridPosition { get; }
        
        /// <summary>
        /// Get the last parent Tilemap. Nonserialized backing field.
        /// </summary>
        Tilemap? LastParentTilemap { get; }

        /// <summary>
        /// Has the tile position changed since the last StartUp?
        /// </summary>
        /// <value><c>true</c> if the tile grid position has changed; otherwise, <c>false</c>.</value>
        bool TileGridPosHasChanged { get; }

        
        /// <summary>
        /// Handy property to get the world position from the TileGridPosition property.
        /// returns Vector3.negativeInfinity for error SO CHECK FOR THAT!
        /// note: returns cell center as world position.
        /// </summary>
        /// <value>The tile world position.</value>
        Vector3 TileWorldPosition { get; }
        
        /// <summary>
        /// This is the Parent Tilemap as set from the Tile's StartUp method.
        /// This does not need a serialized backing field as it's refreshed during edit time
        /// and is refreshed at application start when StartUp is called.
        /// </summary>
        /// <value>The parent tilemap.</value>
        Tilemap? ParentTilemap { get; }

        /// <summary>
        /// This flag is true if the parent tilemap for a tile has changed.
        /// The backing field should NOT be serialized.
        /// </summary>
        bool ParentTilemapHasChanged { get; }

        /// <summary>
        /// Is this an asset? Read-only
        /// </summary>
        /// <value><c>true</c> if this instance is an asset; otherwise, <c>false</c>.</value>
        bool IsAsset { get; }

        /// <summary>
        /// String version of TileState
        /// </summary>
        /// <value>The tile state string.</value>
        string TileStateString { get; }
        
        /// <summary>
        /// Property to determine if animation is PAUSED.
        /// </summary>
        /// <value>true if animation is paused</value>
        bool AnimationIsPaused { get; }
        
        /// <summary>
        /// Property to determine if Tile animation is ACTIVE and not paused: IE *actual* animation is running.
        /// </summary>
        bool AnimationIsRunning { get; }
        
        /// <summary>
        /// Property to determine if animation is running in the tile. Note: running=true and paused=true is fine. 
        /// </summary>
        bool TileAnimationActive { get; }

        /// <summary>
        /// return a variable that denotes whether this Tile is a clone.
        /// </summary>
        /// <value><c>true</c> if this instance is a clone; otherwise, <c>false</c>.</value>
        bool IsClone { get;}

        /// <summary>
        /// return a variable that denotes whether this Tile is Locked.
        /// </summary>
        /// <value><c>true</c> if this instance is locked; otherwise, <c>false</c>.</value>
        /// <remarks>Locked means that the tile is no longer a scene object,
        /// it's from an asset file.</remarks>
        bool IsLocked { get; }

        /// <summary>
        /// Get the GameObject of the tile. Convenience, saves casting in editors 
        /// </summary> 
        GameObject? InstantiatedGameObject
        {
            get 
            {
                if (ParentTilemap == null || TileGridPosition == TilePlusBase.ImpossibleGridPosition)
                    return null;
                return ParentTilemap.GetInstantiatedObject(TileGridPosition); 
            }
        }
        
        /// <summary>
        /// The instance ID of the parent tilemap.
        /// </summary>
        int ParentTilemapInstanceId { get; }
        

        /// <summary>
        /// Reset the state of the tile
        /// </summary>
        /// <param name="op">The type of reset operation</param>
        void ResetState(TileResetOperation op);

        /// <summary>
        /// Turn animation on/off for tiles which support it. 
        /// </summary>
        /// <param name="turnOn">true/false = on/off</param>
        /// <param name = "startingFrame" >sets the current frame to 0 (for either operation). Use -1 to inhibit.</param>
        /// <param name = "ignoreRewindingState" >if false(default) then this method does not execute when waiting for a rewind - only when one-shot is used w/rewindAfterOneShot set true </param>
        ///  <remarks>Also see AnimationSupported property. Also,
        /// when turnOn==true, the startingFrame is set prior to starting animation. When false,
        /// the startingFrame is set after stopping the animation</remarks>
        void ActivateAnimation(bool turnOn, int startingFrame = 0, bool ignoreRewindingState = false)
        {
        }

        /// <summary>
        /// Is the animation waiting to rewind?
        /// </summary>
        bool IsOneShotWaitingToRewind { get;}
        
        /// <summary>
        /// Pause a running animation.
        /// </summary>
        /// <param name="pause"></param>
        void PauseAnimation(bool pause);

        /// <summary>
        /// This tile supports animation if true.
        /// </summary>
        bool AnimationSupported => false;
        
        #endregion

#if UNITY_EDITOR

        #region editorOnly
        
       
        /// <summary>
        /// Returns true if Application.Playmode was true when StartUp was called. Note: editor-only
        /// </summary>
        /// <value><c>true</c> if this instance is play mode; otherwise, <c>false</c>.</value>
        bool IsPlayMode { get; }

        
        /// <summary>
        /// Returns true if TileCustomGui should not allow transform editing.
        /// </summary>
        /// <value><c>true</c> if transform changes are inhibited in-editor; otherwise, <c>false</c>.</value>
        bool InternalLockTransform { get; }

        /// <summary>
        /// Returns true if TileCustomGui should not allow color editing
        /// </summary>
        /// <value><c>true</c> if color changes are inhibited in-editor; otherwise, <c>false</c>.</value>
        bool InternalLockColor { get; }

        /// <summary>
        /// Returns true if TileCustomGui should not allow Tag editing
        /// </summary>
        /// <value><c>true</c> if tags changes are inhibited in-editor; otherwise, <c>false</c>.</value>
        bool InternalLockTags { get; }

        /// <summary>
        /// Returns true if TileCustomGui should not allow Collider editing
        /// </summary>
        /// <value><c>true</c> if collider changes are inhibited in-editor; otherwise, <c>false</c>.</value>
        bool InternalLockCollider { get; }

        /// <summary>
        /// Simulate something about this tile.
        /// </summary>
        /// <param name="start">Begin or End</param>
        /// <remarks>Simulation will force-end if any fields are edited in the Selection Inspector</remarks>
        void Simulate(bool start) {Debug.Log("Unimplemented");}

        /// <summary>
        /// Can this tile perform simulation?
        /// </summary>
        /// <value><c>true</c> if this instance can simulate; otherwise, <c>false</c>.</value>
        bool CanSimulate => false;

        /// <summary>
        /// Is a simulation in progress?
        /// </summary>
        /// <value><c>true</c> if this instance is simulating; otherwise, <c>false</c>.</value>
        bool IsSimulating => false;

        /// <summary>
        /// number of ticks to skip when animating.
        /// The more skip-ticks the slower the animation
        /// note this can be editable with tags
        /// </summary>
        /// <value>The simulation skip ticks.</value>
        int SimulationSkipTicks => 0;

        /// <summary>
        /// Timeout. Note one can make this editable with tags
        /// </summary>
        /// <value>The simulation timeout.</value>
        int SimulationTimeout => 0;

        /// <summary>
        /// Change the tile's state.
        /// Use carefully!
        /// See virtual version in TilePlusBase.cs
        /// </summary>
        /// <param name="resetOp">The type of reset operation</param>
        /// <param name="optionalNewName">An optional new name for the tile</param>
        /// <returns><c>true</c> if the operation was successful, <c>false</c> otherwise.</returns>
        bool ChangeTileState(TileResetOperation resetOp, string optionalNewName);


        /// <summary>
        /// Optional description of tile for brush or inspector display. Keep short.
        /// --&gt;In the Palette's brush inspector this is truncated.
        /// Editor only.
        /// </summary>
        /// <value>The description.</value>
        string Description { get;}


        /// <summary>
        /// Used to restrict tilemap painting to specific layers. Note that this only
        /// affects Tile+Brush.
        /// Editor only.
        /// </summary>
        /// <value>The paint mask list.</value>
        List<string> PaintMaskList { get; }


        /// <summary>
        /// Used to show custom text in the Palette inspector
        /// Editor only.
        /// </summary>
        /// <value>The custom tile information.</value>
        string CustomTileInfo { get; }


        /// <summary>
        /// For tiles with a prefab, get the custom preview icon if needed. See TilePlus.cs
        /// Editor only.
        /// </summary>
        /// <value>The preview icon.</value>
        Texture2D? PreviewIcon { get; }


        /// <summary>
        /// for tiles with prefabs this should be FALSE, for other tiles (e.g., TpAnimatedTile) w/o prefabs, it can be true;
        /// </summary>
        /// <value><c>true</c> if this instance is rotatable; otherwise, <c>false</c>.</value>
        bool IsRotatable { get; }

        /// <summary>
        /// Keeps track of how many times the tile has been cloned.
        /// See non-inheritable implementation in TileplusBase.cs.
        /// </summary>
        /// <value>The version.</value>
        int Version { get; }


        /// <summary>
        /// The name of the source asset (ie the Tile asset). Used for sorting.
        /// backing field should serialize.
        /// See non-inheritable implementation in TilePlusBase.cs
        /// </summary>
        /// <value>The name of the tile source asset.</value>
        string TileSourceAssetName { get;}

        /// <summary>
        /// Is the name of the TPT tile locked in the GUI?
        /// </summary>
        /// <remarks>User code should not write to this property </remarks>
        bool NameLocked { get; set; }
        

        /// <summary>
        /// Get/Set a name for this tile. 
        /// </summary>
        /// <value>The name of the tile.</value>
        /// <remarks>User code should not write to this property </remarks>
        string TileName { get; set; }

        /// <summary>
        /// Get the effective sprite for the tile.
        /// Base class returns value of sprite property.
        /// Overrides can return what would be shown at runtime: see TpSlideShow.
        /// </summary>
        Sprite? EffectiveSprite => null; 


        /// <summary>
        /// Set true if any custom GUI tagged methods implement UI with UIElements rather than IMGUI.
        /// </summary>
        /// <remarks>As of TPT 1.5X this is added for future use only. Please consult the Programmer's Guide. It won't mention this unless it's been implemented.</remarks>
        bool UsesUiElements => false;

        /// <summary>
        /// When TptShowEnumAttribute,TptShowFieldAttribute, or TptShowObjectFieldAttribute
        /// is used AND the updateTpLib parameter is TRUE AND the user changes the field
        /// this gets called with an array of the field names that changed. It should really be only one name.
        /// </summary>
        /// <value>An array of field names changed in the ImGuiTileEditor.</value>
        void UpdateInstance(string[] value) { }


        

        #endregion

        #endif
    }
}
