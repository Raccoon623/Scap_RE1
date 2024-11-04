// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-22-2022
// ***********************************************************************
// <copyright file="TilePlusBase.cs" company="Jeff Sasmor">
//     Copyright (c) 2022 Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>TilePlus tiles base class</summary>
// ***********************************************************************
#if ODIN_INSPECTOR && UNITY_EDITOR
#define USE_ODIN
using Sirenix.OdinInspector;
#endif

#if UNITY_EDITOR
//do NOT want to remove this despite what ReSharper says
// ReSharper disable once RedundantUsingDirective
using UnityEditor;
#endif

#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TilePlus
{
    /// <summary>
    /// This is a base class for any TilePlus Tile. It implements all of the
    /// interfaces required by ITilePlus, with many virtual methods so you can
    /// override to add functionality as desired.
    /// </summary>
    [CreateAssetMenu(fileName = "TilePlusBase.asset", menuName = "TilePlus/TilePlusBase", order = 100000)]
    #if UNITY_EDITOR
    public class TilePlusBase : Tile, ITilePlus, IComparable
        #else
    public class TilePlusBase : Tile, ITilePlus
        #endif
    {
        #region GUID

        /// <summary> 
        /// The clone's GUID. Note that IDs are individual for each clone.
        /// This value can only be set once. Calling ResetState allows it to be
        /// changed until the next StartUp. If a new GUID isn't added before
        /// StartUp then that method will add it.
        /// For example: see TpLib.CopyAndPasteTile
        /// Note: generally speaking, don't reset the GUID.
        /// </summary>
        /// <value>The tile unique identifier bytes.</value>
        /// <remarks>Guid isn't serializable but byte[] is.</remarks>
        public byte[]? TileGuidBytes //GUID struct isn't serializable by Unity so use the byte[] representation.
        {
            get => m_TileGuid;
            set
            {
                //Debug.Log($"Setting Tile guid {value.Length}");
                if (m_TileGuid is not { Length: 16 })
                    m_TileGuid = value;
            }
        }

        /// <summary>
        /// The GUID for this tile.
        /// </summary>
        [HideInInspector]
        [SerializeField]
        private byte[]? m_TileGuid = new byte[1];

        

        #endregion

        #region constants

        /// <summary>
        /// A constant that is the same as the Palette tilemap layer.
        /// </summary>
        public const int PaletteTilemapLayer = 31;
        /// <summary>
        /// A constant string 
        /// </summary>
        private const string Clone = "Clone";
        /// <summary>
        /// A constant string 
        /// </summary>
        private const string Locked = "Locked";
        /// <summary>
        /// A constant string 
        /// </summary>
        private const string Asset = "Asset";
        /// <summary>
        /// A constant string 
        /// </summary>
        private const string CloningError = "Cloning error!";

        #endregion

        #region properties
                
        //this handles an odd edge-case when running a built app
        #if !UNITY_EDITOR
        public string TileName
        {
            get
            {
                if (this == null) 
                    return ("Destroyed!");
                return name;
            }
        }
        #endif


        ///<inheritdoc/>
        public int Id => GetInstanceID();
        
        /// <summary>
        /// Returns true if Application.Playmode was true when StartUp was called. Note: editor-only, always TRUE in a built app.
        /// </summary>
        /// <value><c>true</c> if this instance is play mode; otherwise, <c>false</c>.</value>
        /// <remarks>always returns TRUE in a built application</remarks>
        // ReSharper disable once ConvertToAutoProperty
        public bool IsPlayMode
        {
            get
            {
                #if UNITY_EDITOR
                return isPlayMode;
                #else
                return true;
                #endif
            }
        }

       
        
        /// <summary>
        /// No tile will ever be here!
        /// </summary>
        /// <value>An impossible grid position.</value>
        /// <remarks>This is essentially a constant</remarks>
        public static Vector3Int ImpossibleGridPosition { get; } = new(int.MinValue, int.MinValue, int.MinValue);

        /// <summary>
        /// This is the tile grid position as set from StartUp. Intentionally non-virtual.
        /// Does not need (and should not have) serialized backing field.
        /// Note that backing field is initialized to "Impossible Grid Position"
        /// </summary>
        /// <value>The tile grid position.</value>
        // ReSharper disable once ConvertToAutoProperty
        #if USE_ODIN
        [ShowInInspector]
        #endif
        public Vector3Int TileGridPosition => m_TileGridPosition;

        /// <summary>
        /// Handy property to get the world position from the TileGridPosition property.
        /// returns Vector3.negativeInfinity for error SO CHECK FOR THAT!
        /// note: returns cell center as world position.
        /// </summary>
        /// <value>The tile world position.</value>
        public Vector3 TileWorldPosition =>
            m_ParentTilemap == null || m_TileGridPosition == ImpossibleGridPosition
                ? Vector3.negativeInfinity
                : m_ParentTilemap.GetCellCenterWorld(TileGridPosition);


        /// <summary>
        /// Get the last tile grid position
        /// Note that backing field is initialized to "Impossible Grid Position"
        /// </summary>
        /// <value>The last tile grid position.</value>
        public Vector3Int LastTileGridPosition => m_LastTileGridPosition;

        /// <summary>
        /// Get the last parent Tilemap.
        /// Note that backing firld is initialized to NULL
        /// </summary>
        public Tilemap? LastParentTilemap => m_LastParentMap;

        /// <summary>
        /// Has the tile position changed since the last StartUp?
        /// </summary>
        /// <value><c>true</c> if the tile's grid position has changed; otherwise, <c>false</c>.</value>
        public bool TileGridPosHasChanged => m_TileGridPosHasChanged;

        /// <summary>
        /// Has the tile's parent tilemap changed since the last StartUp?
        /// </summary>
        /// <value><c>true</c> if the tile's parent tilemap has changed; otherwise, <c>false</c>.</value>
        public bool ParentTilemapHasChanged => m_ParentTilemapHasChanged;

        /// <summary>
        /// Parent tilemap for this tile (set in clone). Intentionally non-virtual.
        /// Does not need (and should not have) serialized backing field.
        /// </summary>
        /// <value>The parent tilemap.</value>
        public Tilemap? ParentTilemap => m_ParentTilemap;

        /// <summary>
        /// The instance ID of the parent tilemap.
        /// </summary>
        public int ParentTilemapInstanceId
        {
            get
            {
                if (parentTilemapInstanceId == 0 && m_ParentTilemap != null)
                    parentTilemapInstanceId = m_ParentTilemap.GetInstanceID();
                return parentTilemapInstanceId;
            }
        }

        /// <summary>
        /// Property to determine if animation is PAUSED.
        /// </summary>
        /// <value>true if animation is paused</value>
        /// <remarks>Checking the tile's PAUSE flag</remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        public bool AnimationIsPaused => m_ParentTilemap != null &&
                                         (m_ParentTilemap.GetTileAnimationFlags(m_TileGridPosition) & TileAnimationFlags.PauseAnimation)
                                         == TileAnimationFlags.PauseAnimation;
       
        //Animation-related property here is a stub required by the ITilePlus interface
        /// <summary>
        /// Override in any animated tiles.
        /// </summary>
        public virtual bool IsOneShotWaitingToRewind => false;
        
        /// <summary>
        /// Property to determine if animation is running in the tile. Note: running=true and paused=true is fine. 
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public bool TileAnimationActive => m_ParentTilemap != null &&  
                                           (((int)m_ParentTilemap.GetTileFlags(m_TileGridPosition)) & 0x20000000) != 0;

        /// <summary>
        /// Property to determine if Tile animation is ACTIVE and not paused: IE *actual* animation is running.
        /// </summary>
        public bool AnimationIsRunning => TileAnimationActive && !AnimationIsPaused;
        
        
        /// <summary>
        /// Is this a clone? Read-only
        /// </summary>
        /// <value><c>true</c> if this instance is clone; otherwise, <c>false</c>.</value>
        public bool IsClone => m_State == TilePlusState.Clone;

        /// <summary>
        /// Is this an asset? Read-only
        /// </summary>
        /// <value><c>true</c> if this instance is asset; otherwise, <c>false</c>.</value>
        public bool IsAsset => m_State == TilePlusState.Asset;

        /// <summary>
        /// Is this a locked tile? Read-only
        /// </summary>
        /// <value><c>true</c> if this instance is locked; otherwise, <c>false</c>.</value>
        /// <remarks>Locked means that the tile is no longer a scene object,
        /// it's from an asset file.</remarks>
        public bool IsLocked => m_State == TilePlusState.Locked;

        /// <summary>
        /// String version of TileState
        /// </summary>
        /// <value>The tile state string.</value>
        #if USE_ODIN
        [FoldoutGroup(SettingsAreaTitle, false)]
        [InfoBox("TilePlus tile state: Asset/Locked/Clone", InfoMessageType.None)]
        [ShowInInspector]
        #endif
        public string TileStateString
        {
            get
            {
                var s = m_State switch
                        {
                            TilePlusState.Asset  => Asset,
                            TilePlusState.Clone  => Clone,
                            TilePlusState.Locked => Locked,
                            _                    => throw new ArgumentOutOfRangeException()
                        };
                return s;
            }
        }

        /// <summary>
        /// Get the tile's GUID as a GUID struct.
        /// </summary>
        /// <value>The tile unique identifier.</value>
        public Guid TileGuid =>  m_TileGuid is not { Length: 16 } ? Guid.Empty  : new Guid(m_TileGuid);

        /// <summary>
        /// The clone's GUID as a string
        /// </summary>
        /// <value>The tile unique identifier string.</value>
        #if USE_ODIN
        [FoldoutGroup(SettingsAreaTitle, false)]
        [ShowInInspector]
        #endif
        public string TileGuidString
        {
            get
            {
                if (m_CachedGuidString != string.Empty)
                    return m_CachedGuidString;
                if (m_TileGuid is not { Length: 16 })
                    return string.Empty;
                return m_CachedGuidString = new Guid(m_TileGuid).ToString();
            }
        }

        /// <summary>
        /// Property to get a tuple of (count, array of individually-trimmed tags). not cached.
        /// </summary>
        /// <remarks>returns null if there are no tags, or an empty list if all trimmed tags eval to empty></remarks>
        public (int count, string[] tags) TrimmedTags
        {
            get
            {
                if (string.IsNullOrEmpty(m_Tag)) //Ver 1.3 change
                    return (0, null)!;
                var tags   = m_Tag.Trim().Split(',');
                var output = new string [tags.Length];
                var j      = 0;
                // ReSharper disable once ForCanBeConvertedToForeach
                for (var i = 0; i < tags.Length; i++)
                {
                    var tag = tags[i];
                    if (string.IsNullOrWhiteSpace(tag))
                        continue;
                    var s = tag.Trim();
                    if (s != string.Empty)
                        output[j++] = s;
                }
                return (j, output);
            }
        }

        #endregion

        #region privateOrProtectedFields

        /// <summary>
        /// This private field indicates the state of this tile.
        /// If you need access, use the read-only Properties IsClone,
        /// IsAsset, or IsLocked.
        /// </summary>
        [HideInInspector]
        [SerializeField]
        private TilePlusState m_State;

        /// <summary>
        /// The tile grid position gets initialized in Startup.
        ///  It's initialized with 'ImpossibleGridPosition'
        /// </summary>
        [NonSerialized]
        // ReSharper disable once MemberCanBePrivate.Global
        protected Vector3Int m_TileGridPosition = new(int.MinValue, int.MinValue, int.MinValue);

        /// <summary>
        /// The previous tile grid position.
        /// </summary>
        [NonSerialized]
        // ReSharper disable once MemberCanBePrivate.Global
        protected Vector3Int m_LastTileGridPosition = new(int.MinValue, int.MinValue, int.MinValue);

        /// <summary>
        /// The previous parent Tilemap
        /// </summary>
        [NonSerialized]
        // ReSharper disable once MemberCanBePrivate.Global
        protected Tilemap? m_LastParentMap;

        // ReSharper disable once MemberCanBePrivate.Global
        /// <summary>
        /// True if the tile grid position has changed
        /// </summary>
        [NonSerialized]
        protected bool m_TileGridPosHasChanged;

        /// <summary>
        /// Parent tilemap has changed if true.
        /// </summary>
        [NonSerialized]
        // ReSharper disable once MemberCanBePrivate.Global
        protected bool m_ParentTilemapHasChanged;

        /// <summary>
        /// Parent tile map is initialized in Startup
        /// </summary>
        [NonSerialized]
        // ReSharper disable once MemberCanBePrivate.Global
        protected Tilemap? m_ParentTilemap;

        /// <summary>
        /// Cached instance ID of the parent Tilemap
        /// </summary>
        [NonSerialized]
        private int parentTilemapInstanceId;

        /// <summary>
        /// The cached GUID string
        /// </summary>

        [SerializeField]
        [HideInInspector]
        private string m_CachedGuidString = string.Empty;

       

        
        #if UNITY_EDITOR
        /// <summary>
        /// Backing field for IsPlayMode property
        /// </summary>
        [NonSerialized]
        private bool isPlayMode;

        
        #endif

        #endregion

        #region propertiesWithPublicFields

        /// <summary>
        /// Used to set up the tile's collider.
        /// </summary>
        /// <value>The tile collider mode.</value>
        public virtual ColliderMode TileColliderMode
        {
            get => m_ColliderMode;
            set
            {
                #if UNITY_EDITOR
                if(!isPlayMode)
                #endif    
                    m_ColliderMode = value;
            }
        }

        /// <summary>
        /// Used to set up the tile's collider.
        /// </summary>
        #if USE_ODIN
        [FoldoutGroup(SettingsAreaTitle, false)]
        [InfoBox("Set collider type for this tile. Ignore means no change. Other settings override Tile class Collider param.", InfoMessageType.None)]
        #endif
        [Tooltip("Set collider type for this tile. Ignore means no change. Other settings override Tile class Collider param.")]
        public ColliderMode m_ColliderMode = ColliderMode.NoCollider;
        //note that since this is used in GetTileData implementation herein, it's always active and used whenever
        //the tile gets refreshed.

        /// <summary>
        /// Get the the tile sprite clear mode. 
        /// </summary>
        /// <value>The SpriteClearMode value.</value>
        /// <remarks>default implementation in interface returns SpriteClearMode.Ignore </remarks>
        public SpriteClearMode TileSpriteClear
        {
            get => m_TileSpriteClear;
            set
            {
                #if UNITY_EDITOR
                if(!isPlayMode)
                #endif    
                    m_TileSpriteClear = value;
            }
        }

        /// <summary>
        /// m_TileClear controls whether or not the tile's sprite is cleared or if the tile is deleted at runtime, or nothing.
        /// </summary>
        #if USE_ODIN
        [FoldoutGroup(SettingsAreaTitle, false)]
        [InfoBox("ClearOnStart: clears tile sprite when Playing. ClearInSceneView clears tile sprite in the Scene View. ClearInSceneViewAndOnStart never shows the sprite except in the Palette. Ignore does nothing.", InfoMessageType.None)]
        #endif
        [Tooltip("ClearOnStart clears the tile sprite when Playing. ClearInSceneView clears tile sprite in the Scene View. ClearInSceneViewAndOnStart never shows the sprite except in the Palette. Ignore does nothing.")]
        public SpriteClearMode m_TileSpriteClear = SpriteClearMode.Ignore;

        /// <summary>
        /// Property to get the optional tag
        /// </summary>
        /// <value>The tag.</value>
        [TptShowAsLabelBrushInspector(true)]
        public string Tag
        {
            get => string.IsNullOrEmpty(m_Tag)
                       ? string.Empty
                       : m_Tag.Trim();
            set
            {
                #if UNITY_EDITOR
                if (!isPlayMode)
                #endif    
                    m_Tag = value.Trim();
            }
        }


        /// <summary>
        /// The tag for this tile
        /// </summary>
        #if USE_ODIN
        [FoldoutGroup(SettingsAreaTitle, false)]
        [InfoBox("Optional Tag(s) for this tile instance. Separate with commas. Do not use ------ as a tag.", InfoMessageType.None)]
        [ShowInInspector]
        #endif
        [Tooltip("Optional tag(s) for this tile. Separate with commas. Do not use ------ as a tag.")]
        public string m_Tag = string.Empty;

        #endregion

        #region events
       
        /// <summary>
        /// Tiles can be in three states: Asset, Clone, Locked.
        /// See the Programmer's Guide for more info.
        /// Advised to not change anything in here!
        /// NOTE: if overriding this BE SURE to call this base method as the
        /// FIRST thing you do! Note the return value and return FALSE if this
        /// base method returns FALSE.
        /// </summary>
        /// <param name="position">Position of the Tile on the Tilemap.</param>
        /// <param name="tilemap">The Tilemap the tile is present on.</param>
        /// <param name="go">The GameObject instantiated for the Tile.</param>
        /// <returns>Whether the call was successful.</returns>
        /// <remarks> Note that the order of execution for StartUp is AFTER GetTileData.
        /// Note that the gameObject passed-in to StartUp is in the Scene. Explanation:
        ///In GetTileData, the gameObject to instantiate is being provided to the TileMap.
        ///Here, that instantiated GO is being passed in to this method.
        ///so the go referred to here is the actual GameObject in the scene and not the asset.
        /// </remarks> 

        public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject go)
        {   
            var map = tilemap.GetComponent<Tilemap>();
            parentTilemapInstanceId = 0;

            #if UNITY_EDITOR
            isPlayMode = Application.isPlaying;
            /*during asset import the tilemap is not active/enabled. For some reason,
              Unity runs StartUp during import of say, a scene with a tilemap in it.
              This test ensures no bogus error messages when importing.  */
            if (!map.isActiveAndEnabled)
                return false;

            //if not a clone and the tile is in the Palette's tilemap, we're done.
            if (m_State != TilePlusState.Clone && IsTilemapFromPalette(map))
                return false;
            #endif

            if (IsAsset || IsLocked)
            {
                /*Normal Asset | Locked asset :: cloned, the Clone's state is changed to "Clone",
                * and the clone reference replaces that in Tilemap.
                * Note that using Tilemap.SetTile when the Application is running
                * in Play mode (or in a built app) with a TilePlus tile in the
                * Asset state will clone the tile. 
                */
                if (IsLocked)
                {
                    m_TileGuid = null;
                    m_CachedGuidString = string.Empty;
                }
                TpLib.CloneTilePlus(this,position,map, IsLocked);
                return false;  //false tells subclasses to return and not do anything else.
            }
             
            /* if not Asset or Locked, update the tilemap state fields for map and position.
             Used to detect changes in tilemap or position as well as used by many methods in TpLib etc.*/
            UpdatePositionAndMap(position,map);
        
            //GUID sanity test
            var guidToTest = TileGuid;
            //if the instance has a GUID and the GUID is empty, add one
            //Note that if this tile is in a PREFAB then it's a locked tile, but the prefab creator nulls the GUID.
            //this is so each time that the Prefab is placed during edit time (or opened in a stage) the TPT tiles all have new GUIDs.
            //PREFABS loaded at runtime will also have new GUIDs. Remapping GUIDs (like with TileFabs) is not provided.
            if ( m_TileGuid is not { Length: 16 })
            {
                guidToTest    = Guid.NewGuid();
                m_TileGuid = guidToTest.ToByteArray();
                m_CachedGuidString = string.Empty;
            }
            
            if (TpLib.HasGuid(guidToTest))
                return true;  

            TpLib.RegisterTilePlus(this, position, map);
            return true;
        }
        
        internal TilePlusBase? Cloner(bool newGuid = false)
        {
            //clone the Tile instance
            var clone = Instantiate(this);
            if (clone == null) //just in case
            {
                Debug.LogError(CloningError);
                return null;
            }

            //check to see if the GUID already exists.
            //if it does then we don't want to change it.
            //if the clone has a GUID and the GUID is empty, add one
            //BUT if the newGuid param is TRUE then replace the old GUID with a new one.
            //This is used when loading Combined-Tile assets.
            if (newGuid || clone.m_TileGuid is not { Length: 16 })
            {
                clone.m_TileGuid = Guid.NewGuid().ToByteArray();
                clone.m_CachedGuidString = string.Empty;
            }

            #if UNITY_EDITOR
            Version++;
            clone.m_SourceAssetName = name;
            clone.m_TileName        = name;
            #endif

            if (IsLocked)
            {
                clone.parentTilemapInstanceId   = 0;
                clone.m_TileGridPosition        = ImpossibleGridPosition; 
                clone.m_LastTileGridPosition    = ImpossibleGridPosition;
                clone.m_TileGridPosHasChanged   = false;
                clone.m_ParentTilemapHasChanged = false;
                clone.m_ParentTilemap           = null;
                clone.m_LastParentMap           = null;
            }
                
            clone.m_State = TilePlusState.Clone;
            return clone;
        }
        
        

        // Implementation handles flags and collider type
        /// <summary>
        /// Get data for this tile.
        /// Override of Tile. Note that Tilemap.RefreshTile calls this
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="tilemap">The tilemap.</param>
        /// <param name="tileData">The tile data.</param>
        /// <remarks>See subclasses for how to override. 
        /// Note that the order of execution for GetTileData is BEFORE StartUp</remarks>
        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            #if UNITY_EDITOR
            //note that GetTileData is called BEFORE StartUp.
            isPlayMode = Application.isPlaying;

            if (m_State != TilePlusState.Clone && IsTilemapFromPalette(tilemap))
            {
                tileData.color        = color;
                tileData.colliderType = colliderType;
                tileData.flags        = flags;
                tileData.transform    = Matrix4x4.identity;
                tileData.sprite       = sprite;
                return;
            }
            #endif
            m_ParentTilemap = tilemap.GetComponent<Tilemap>();
            base.GetTileData(position, tilemap, ref tileData);
            if(m_TileGridPosition == ImpossibleGridPosition)
                UpdatePositionAndMap(position,m_ParentTilemap);
            if(SpriteShouldBeCleared)
                tileData.sprite = null;
            SetColliderType(ref tileData);
            
            //2.02 Fixes tilemap.RrefreshAllTiles error where tiles w/ GOs don't get Startup called.
            //this is present at least till 2023.1.20
            if (!IsClone || m_TileGuid is not { Length: 16 }) //test for clone state AND a valid GUID
                return;
            if (TpLib.HasGuid(TileGuid)) //is this tile already registered?
                return;
            TpLib.RegisterTilePlus(this, position, m_ParentTilemap);
        }

        /// <summary>
        /// Returns true if tile sprite should be cleared based on setting
        /// of TileSpriteClear property, tile state, and whether in Play mode or not.
        /// </summary>
        /// <value><c>true</c> if the sprite should be cleared; otherwise, <c>false</c>.</value>
        protected bool SpriteShouldBeCleared
        {
            get
            {
                if (m_TileSpriteClear == SpriteClearMode.Ignore || IsAsset)
                    return false;
                #if UNITY_EDITOR
                return m_TileSpriteClear == SpriteClearMode.ClearInSceneViewAndOnStart ||
                       (m_TileSpriteClear == SpriteClearMode.ClearInSceneView && !IsPlayMode) ||
                       (m_TileSpriteClear == SpriteClearMode.ClearOnStart && IsPlayMode);
                #else
                return m_TileSpriteClear == SpriteClearMode.ClearOnStart || 
                        m_TileSpriteClear == SpriteClearMode.ClearInSceneViewAndOnStart ;
                #endif

            }
        }

        /// <summary>
        /// Sets up colliders
        /// </summary>
        /// <param name="tileData">ref to the tileData</param>
        protected void SetColliderType(ref TileData tileData)
        {
            tileData.colliderType = m_ColliderMode switch
                                    {
                                        ColliderMode.NoOverride => colliderType, //use the default collider type from the Tile superclass.
                                        ColliderMode.Grid       => ColliderType.Grid,
                                        ColliderMode.Sprite     => ColliderType.Sprite,
                                        ColliderMode.NoCollider => ColliderType.None,
                                        _                       => tileData.colliderType
                                    };
        }
        
        private void UpdatePositionAndMap(Vector3Int position, Tilemap map)
        {
            //Update the tilemap state fields for map and position.
            //position:
            m_LastTileGridPosition  = m_TileGridPosition; //the position from the last StartUp call
            m_TileGridPosHasChanged = m_TileGridPosition != ImpossibleGridPosition && m_TileGridPosition != position;
            m_TileGridPosition      = position;
            //map:
            m_LastParentMap           = m_ParentTilemap;
            m_ParentTilemapHasChanged = map != m_ParentTilemap;
            m_ParentTilemap           = map;
        }

        #endregion
        
        #region utils

      
        
        
        //Animation-related method here is a stub required by the ITilePlus interface
        /// <inheritdoc />
        public virtual void ActivateAnimation(bool turnOn, int startingFrame = 0, bool ignoreRewindingState = false)
        {
            Debug.LogError("Should not be trying to activate animation on non-animated tile!");
        }

        //Animation-related method here is a stub required by the ITilePlus interface
        /// <inheritdoc />
        public virtual void PauseAnimation(bool pause)
        {
            Debug.LogError("Should not be trying to pause animation on non-animated tile!");
        }
        
        
        /// <inheritdoc />
        public override string ToString()
        {
            if (TileGridPosition != ImpossibleGridPosition && ParentTilemap != null)
                return $"Tile+ named {name} @ {TileGridPosition} on map {ParentTilemap} has IID {Id}";
            return $"Tile+ has IID {Id}";
        }

        /// <summary>
        /// Used to reset state variables. May need overriding
        /// in subclasses.
        /// See programmer's guide for info on overriding this.
        /// </summary>
        /// <param name="op">The type of reset operation</param>
        /// <remarks>
        /// op=Restore is used only in editor mode, when picked tiles
        /// are painted. As the picked tiles are moved around the
        /// map, their Startup is called many times by Editor code, placing incorrect
        /// grid position and possible incorrect map refs in m_TileGridPosition and
        /// m_ParentTilemap. 
        /// </remarks>
        public virtual void ResetState(TileResetOperation op)
        {
            // Debug.Log($"State reset op:{op}");
            //specialized for creating Prefabs or for a particular "Update GUIDs" menu item. Don't use otherwise
            if (op == TileResetOperation.ClearGuid)
            {
                m_TileGuid         = null; //this allows code to change the GUID see TpLib.CopyAndPasteTile
                m_CachedGuidString = string.Empty;
                return;
            }

            //note that this changes the GUID
            if (op == TileResetOperation.SetCloneState) //change to clone state from Asset. Use carefully.
            {
                m_State            = TilePlusState.Clone;
                m_CachedGuidString = string.Empty;
                m_TileGuid         = Guid.NewGuid().ToByteArray();
                return;
            }

            if (op == TileResetOperation.Restore)
            {
                m_TileGridPosition = m_LastTileGridPosition;
                m_ParentTilemap    = m_LastParentMap;
                return;
            }

            parentTilemapInstanceId   = 0;
            m_TileGridPosition        = ImpossibleGridPosition; 
            m_LastTileGridPosition    = ImpossibleGridPosition;
            m_ParentTilemap           = null;
            m_TileGridPosHasChanged   = false;
            m_ParentTilemapHasChanged = false;
            m_ParentTilemap           = null;


            m_CachedGuidString = string.Empty;
            m_TileGuid         = null; //this allows code to change the GUID see TpLib.CopyAndPasteTile
            m_CachedGuidString = string.Empty;

            if (op is TileResetOperation.MakeNormalAsset)
                m_State = TilePlusState.Asset;
            
            if (op != TileResetOperation.MakeCopy)
                return;
            name          = name.Split('(')[0];
            #if UNITY_EDITOR
            m_TileName = name;
            #endif

        }

        


        /// <summary>
        /// Default inplementation of UpdateInstance.
        /// </summary>
        /// <value>an array of field names that have been changed in the Editor
        /// via the ImGuiTileEditor module</value> 
        /// <returns>TRUE if the modification should cause a OnTpLibChanged event with DbChangedArgs.ChangeType.Modified</returns>
        /// <remarks>when this is called as base, be sure to OR with the returned value if your override wants to return false.</remarks>
        /// <remarks>when TRUE is returned any clients of the OnTpLibChanged event need to handle all the appropriate cases. See TilePlusViewer for an example.</remarks>
        /// <remarks>NOTE: changes to Tags are handled in TpLib.</remarks>
        public virtual bool UpdateInstance(string[] value)
        {
            /*
             foreach(var fieldName in value)
                Debug.Log($"UpdateInstance {fieldName}");
            */
            return false;
        }


        /// <summary>
        /// Execute a tile refresh later: can't refresh a tile from within tile code.
        /// </summary>
        /// <param name="info">Informational string</param>
        /// <param name = "callback" >if not null, callback after the delay</param>
        internal void RefreshLater(string info, Action? callback =null)
        {
            TpLib.DelayedCallback(this, DelayedRefresh, info);
            void DelayedRefresh()
            {
                if (m_ParentTilemap == null)
                    return;
                m_ParentTilemap.RefreshTile(m_TileGridPosition);
                callback?.Invoke();
            }
        }

        

        #endregion

        #if UNITY_EDITOR

        #region editorcode

        /// <summary>
        /// Gui for this class
        /// </summary>
        /// <param name="skin">skin</param>
        /// <param name="buttonSize">buttonsize</param>
        /// <param name = "noEdit" >No editing: tile in a prefab</param>
        /// <returns>An instance of the CustomGuiReturn struct</returns>
        /// <remarks>This implementation is just a wrapper</remarks>
        [TptShowCustomGUI]
        public CustomGuiReturn BaseGui(GUISkin skin, Vector2 buttonSize, bool noEdit)
        {
            return TpEditorBridge.BaseCustomGui(this, skin, buttonSize,noEdit);
        }

        /// <summary>
        /// Reset the tile. For use in-editor.
        /// </summary>
        /// <param name="resetOp">Type of reset operation: make a new asset, a locked asset, or locked-&gt;clone</param>
        /// <param name="optionalNewName">An optional new name for the tile</param>
        /// <returns><c>true</c> if sucessful, <c>false</c> otherwise.</returns>
        public bool ChangeTileState(TileResetOperation resetOp, string optionalNewName = "")
        {
            //Can only UnlockAndClone if the tile is locked;
            if (resetOp == TileResetOperation.UnlockAndClone)
            {
                if (m_State != TilePlusState.Locked)
                    return false;
                m_TileName      = m_TileName.Split('(')[0];
                name            = m_TileName;
                m_State         = TilePlusState.Asset;
                m_ParentTilemap = null;
                ResetState(resetOp);
                return true;
            }

            //the other two resetOps require the current state to not be Asset
            if (m_State == TilePlusState.Asset)
                return false;
            //change state    
            var lastState = m_State;
            m_State = resetOp == TileResetOperation.MakeLockedAsset
                          ? TilePlusState.Locked
                          : TilePlusState.Asset;
            m_ParentTilemap = null;
            ResetState(resetOp);

            //required for case where making locked tilemap and this tile is already locked
            if (lastState == TilePlusState.Locked)
                return true;
            //unlocked clone tile needs name change
            m_TileName = string.IsNullOrWhiteSpace(optionalNewName)
                             ? m_TileName.Split('(')[0]
                             : optionalNewName;
            name = m_TileName;

            return true;
        }

        /// <summary>
        /// Is this tilemap actually the palette?
        /// </summary>
        /// <param name="tilemap">tilemap ref</param>
        /// <returns>true if is from palette</returns>
        /// <remarks>It shouldn't be this obtuse... </remarks>
        internal static bool IsTilemapFromPalette(Tilemap tilemap)
        {
            /*although it's tempting to use PrefabUtility.IsPartOfAnyPrefab() here
              because the palette is actually a Prefab, it won't work because any 
              tilemap can be validly part of a prefab. 

            aside from a Palette's name being Layer1, which could change,
            the hideflags are different. A normal tilemap should never
            have DontSave for flags.
            Also, the tilemap's transform.parent's layer is set to 31. So check for that too.
            Note: the tilemap within the Palette is inside a prefab. It's hide flags
            are set to HideAndDontSave which is DontSave | NotEditable | HideInHierarchy
            but when a palette prefab is opened in a prefab context, the tilemap created
            for the prefab stage is set to DontSave. Since HideAndDontSave includes DontSave
            its easier to use that. It's unlikely that a tilemap in a scene would have DontSave
            as a flag. (Note that in 2021.2 this may not be true, so the "Layer1" check is still used)
            */
            //so the tilemap is a palette if the hideflags DontSave bit is set
            
            if (tilemap.name == "Layer1" ||
                (tilemap.hideFlags & HideFlags.DontSave) == HideFlags.DontSave)
                return true;

            var parent = tilemap.transform.parent;
            if (parent == null) //should never happen since a tilemap always has a Grid as parent.
                return false;

            //or the tilemap's parent GO layer is 31
            return parent.gameObject.layer == TilePlusBase.PaletteTilemapLayer;
            
        }

        /// <summary>
        /// Overload for IsTilemapFromPalette(Tilemap)
        /// </summary>
        /// <param name="itilemap">An ITilemap instance</param>
        /// <returns>true if tilemap is from the palette</returns>
        private static bool IsTilemapFromPalette(ITilemap itilemap)
        {
            var component = itilemap.GetComponent<Tilemap>();
            return component != null && IsTilemapFromPalette(component);
        }


        #endregion

        #region editorPrivate

        /// <summary>
        /// If true then editor GUI can't modify the tile's name
        /// </summary>
        [SerializeField]
        private bool m_LockName = true;
       
        #endregion

        #region editorProperties

        /// <summary>
        /// Is the name locked?
        /// </summary>
        public bool NameLocked
        {
            get => m_LockName;
            set
            {
                if(!isPlayMode)
                    m_LockName = value;
            }
        }

        /// <summary>
        /// Returns true if TilePlusBase should not allow transform editing.
        /// </summary>
        /// <value><c>true</c> if transform editing is disabled; otherwise, <c>false</c>.</value>
        public virtual bool InternalLockTransform => false;

        /// <summary>
        /// Returns true if TilePlusBase should not allow color editing
        /// </summary>
        /// <value><c>true</c> if color editing is disabled; otherwise, <c>false</c>.</value>
        public virtual bool InternalLockColor => false;


        /// <summary>
        /// Returns true if TilePlusBase should not allow Tag editing
        /// </summary>
        /// <value><c>true</c> if tags editing is disabled ; otherwise, <c>false</c>.</value>
        public virtual bool InternalLockTags => false;

        /// <summary>
        /// Returns true if TilePlusBase should not allow Collider editing
        /// </summary>
        /// <value><c>true</c> if collider editing is disabled; otherwise, <c>false</c>.</value>
        public virtual bool InternalLockCollider => false;

        
        /// <summary>
        /// The version of the asset
        /// </summary>
        [HideInInspector]
        [SerializeField]
        private int m_Version;


        /// <summary>
        /// Keeps track of how many times the tile has been cloned.
        /// See non-inheritable implementation in TileplusBase.cs.
        /// </summary>
        /// <value>The version of the asset.</value>

        public int Version
        {
            get => m_Version;
            set => m_Version = value;
        }

        /// <summary>
        /// get a preview icon for this tile. If you don't need to
        /// provide one, just return null. not available at runtime.
        /// Note only works with Tile+Brush.
        /// </summary>
        /// <value>The preview icon.</value>
        public virtual Texture2D? PreviewIcon => null;


        /// <summary>
        /// If true, the tile can be rotated by the brush.
        /// Some types of tiles shouldn't be rotated.
        /// For tiles with prefabs this should be false. If true, the tile will be rotated but the prefab will not.
        /// not available at runtime
        /// Note only works with Tile+Brush.
        /// </summary>
        /// <value><c>true</c> if this instance is rotatable; otherwise, <c>false</c>.</value>
        public virtual bool IsRotatable => true;

        /// <summary>
        /// Used to restrict tilemap painting to specific layers.
        /// Note this only works with Tile+Brush
        /// </summary>
        /// <value>The paint mask list.</value>
        public virtual List<string> PaintMaskList => m_PaintMask;

        /// <summary>
        /// Custom string data to show in the Palette's brush inspector
        /// </summary>
        /// <value>The custom tile information.</value>
        public virtual string CustomTileInfo => m_CustomTileInfo;

        /// <summary>
        /// this is something that's on a per-tile-type basis. IE a description of the tile type.
        /// In the Palette's brush inspector this could be truncated.
        /// Override this in subclass to provide something else.
        /// </summary>
        /// <value>The description of this tile.</value>
        public virtual string Description => "-none-";


        /// <summary>
        /// get the name of the tile. Editor-only
        /// </summary>
        /// <value>The name of the tile.</value>
        public string TileName
        {
            get
            {
                //needed check because a deleted tile might still be the target in the Brush inspector or elsewhere
                //and Object.name comes from the engine side which could be null even though the c# side isnt GC'd yet.
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (this == null)
                    return "Destroyed!";
                var s = name;
                if (!string.IsNullOrEmpty(m_TileName))
                    s = m_TileName;
                else if (!string.IsNullOrEmpty(m_SourceAssetName))
                    s = m_SourceAssetName;
                var typ         = GetType().ToString();
                var firstPeriod = typ.IndexOf('.');
                if (firstPeriod != -1)
                    typ = typ.Substring(firstPeriod + 1);
                return $"{s} ({typ}) [{TileStateString}]";
            }
            set
            {
                if(!isPlayMode)
                    m_TileName = value;
            }
        }

        /// <summary>
        /// Easy way to return this value without having to instantiate it. The value is consumed immediately
        /// so no reason to ever have more than one of these.
        /// </summary>
        // ReSharper disable once MemberCanBeProtected.Global
        public static CustomGuiReturn NoActionRequiredCustomGuiReturn { get; } = new();

        #endregion

        #region editorMisc



        /// <summary>
        /// The source asset name
        /// </summary>
        [SerializeField]
        [HideInInspector]
        private string m_SourceAssetName = string.Empty;

        /// <summary>
        /// when cloned, the source asset name is stored here. Used for sorting.
        /// editor-only
        /// </summary>
        /// <value>The name of the tile source asset.</value>
        public string TileSourceAssetName => m_SourceAssetName;

        /// <summary>
        /// Get the effective sprite for the tile.
        /// Base class returns value of sprite property.
        /// Overrides can return what would be shown at runtime: see TpSlideShow.
        /// </summary>
        public virtual Sprite? EffectiveSprite => sprite; 

        /// <summary>
        /// Name of the tile. not the same as  Object.name
        /// </summary>
        #if USE_ODIN && UNITY_EDITOR
        [FoldoutGroup(SettingsAreaTitle, false)]
        [ShowInInspector, ReadOnly]
        #else
        [HideInInspector]
        #endif
        [SerializeField]
        protected string m_TileName = string.Empty;


        //used for sorting in editor UI.
        /// <summary>
        /// Compares asset names.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>-1, 0, 1 as usual</returns>
        public int CompareTo(object obj)
        {
            return obj is not TilePlusBase tpb
                       ? 1
                       : StringComparer.InvariantCulture.Compare(m_SourceAssetName, tpb.m_SourceAssetName);
        }

        #endregion


        #region editorPublicFields

        #if USE_ODIN
        /// <summary>
        /// Title of settings area - ODIN only
        /// </summary>
        private const string SettingsAreaTitle = "TilePlus Basic settings";
        #endif

        /// <summary>
        /// Restrict which tilemaps this tile can paint on
        /// </summary>
        #if USE_ODIN
        [InfoBox("Select or Exclude (first char = !) tilemaps for painting.", InfoMessageType.None)]
        [FoldoutGroup(SettingsAreaTitle, false)]
        #endif
        [Tooltip("Leave this array EMPTY (size=0) to paint this tile on any tilemap. Add tilemap names as List elements to restrict painting to NAMED tilemaps, If the first character is ! then the tilemap is excluded. ((Tile++Brush only)) .")]
        public List<string> m_PaintMask = new List<string>();

        /// <summary>
        /// List of strings used to restrict painting to
        /// specific tilemaps when used with the Tile+Brush
        /// </summary>
        /// <value>The paint mask.</value>
        [TptShowAsLabelBrushInspector(true,
                                      false,
                                      "Restrict painting to specific Tilemaps by adding Tilemap names to this List")]
        public string PaintMask => m_PaintMask.Count == 0
                                       ? "Paint on Any Tilemap"
                                       : string.Join(",", m_PaintMask);

        /// <summary>
        /// Used to show custom string data in the palette brush inspector.
        /// </summary>
        #if USE_ODIN
        [DetailedInfoBox("Optional custom string to display in the Palette's Brush inspector. Click for more...", "Note: only matters for original tile asset. Changes in cloned tiles are ignored)", InfoMessageType.None)]
        [FoldoutGroup(SettingsAreaTitle)]
        #endif
        [Tooltip("Optional custom string to display in the Palette's Brush inspector. Original asset only.")]
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        public string m_CustomTileInfo = string.Empty;

        


        #endregion   
        #endif

    }
    
    #if UNITY_EDITOR && !ODIN_INSPECTOR
    /// <summary>
    /// Simple custom editor for TilePlusBase when inspected in project.
    /// </summary>
    [CustomEditor(typeof(TilePlusBase),true)]
    public class TilePlusBaseEditor : UnityEditor.Editor
    {
        
        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            var instance = target as TilePlusBase;
            if(instance == null)
                return;
            EditorGUILayout.HelpBox($"Tile state: {instance.TileStateString}",MessageType.Info);
            EditorGUILayout.Space(10);
            base.OnInspectorGUI();
        }
    }
    #endif
    
    #if UNITY_6000_0_OR_NEWER && UNITY_EDITOR && ODIN_INSPECTOR
    /// <summary>
    /// Simple custom editor for TilePlusBase when inspected in project.
    /// </summary>
    [CustomEditor(typeof(TilePlusBase),true)]
    public class TilePlusBaseEditor : UnityEditor.Editor
    {
        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            var instance = target as TilePlusBase;
            if(instance == null)
                return;
            EditorGUILayout.HelpBox($"Tile state: {instance.TileStateString}",MessageType.Info);
            EditorGUILayout.Space(10);
            base.OnInspectorGUI();
        } 
    }
    
    #endif    
}
