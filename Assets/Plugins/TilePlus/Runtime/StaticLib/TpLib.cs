// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021 
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-31-2023
// ***********************************************************************
// <copyright file="TpLib.cs" company="Jeff Sasmor">
//     Copyright (c) 2022 Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Main TilePlus static library</summary>
// ***********************************************************************
#nullable enable

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Tilemaps;
using UnityEditor.SceneManagement;
// ReSharper disable once RedundantUsingDirective
using UnityEditor.Build; //do not remove. Really. For some reason builds fail without this.
#endif 

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
// ReSharper disable InconsistentNaming 

// ReSharper disable RedundantNameQualifier

[assembly: AssemblyDescription("F")]
[assembly: AssemblyCompany("Jeff Sasmor")]
[assembly:AssemblyVersion("3.0.0.3")]
[assembly:AssemblyCopyright("Copyright (C) 2024")]

#if UNITY_EDITOR
[assembly: InternalsVisibleTo("TilePlus.Editor")] 
#endif

// ReSharper disable Unity.RedundantHideInInspectorAttribute
namespace TilePlus
{
    /// <summary>
    /// This static class maintains edit-time and run-time dictionaries which
    /// track TilePlusBase items on a per-tilemap basis.
    /// In-Editor, heirarchy and tilemap changes are tracked and dictionaries are
    /// updated as necessary. 
    /// </summary>
    #if UNITY_EDITOR
    [InitializeOnLoad]
    #endif
    public static class TpLib
    {
        #region privateClasses
        /// <summary>
        /// This is pooled, and used when cloning TPT tiles.
        /// </summary>
        private class CloningData
        {
            /// <summary>
            /// The tile to clone
            /// </summary>
            public TilePlusBase? Tile     { get; set; }
            /// <summary>
            /// The position to clone the tile
            /// </summary>
            public Vector3Int    Position { get; set; }

            /// <summary>
            /// The tilemap to place the clone
            /// </summary>
            public Tilemap?      Tilemap  { get; set; }

            /// <summary>
            /// If set, this data reflects a TilePlus tile trying to register itself prior to TpLib being initialized.
            /// </summary>
            public bool IsDelayedRegistration { get; set; }
            
            /// <summary>
            /// Called when an instance of this class is returned to the pooler
            /// so that Tile and Tilemap references are deleted.
            /// </summary>
            public void Reset()
            {
                Tile                  = null;
                Position              = Vector3Int.zero;
                Tilemap               = null;
                IsDelayedRegistration = false;
            }
        }


        private class DeferredCallbackData
        {
            /// <summary>
            /// The calling object. Tested for null if not null initially
            /// </summary>
            public Object? Target { get; set; }

            /// <summary>
            /// The callback to execute
            /// </summary>
            public Action? m_Callback; 

            /// <summary>
            /// Debug info: some info to show when an exception occurs.
            /// </summary>
            public string? Info { get; set; }
            
            /// <summary>
            /// Should the Target get tested for null
            /// </summary>
            public bool TestForNull { get; set; }
            
            /// <summary>
            /// Inhibit all messages if true
            /// </summary>
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public bool Silent { get; set; }

            /// <summary>
            /// Used to reset this instance when returned to pool
            /// </summary>
            public void Reset()
            {
                Target      = null;
                m_Callback  = null;
                Info        = null;
                TestForNull = false;
                Silent      = false;

            }
        }
        
        #endregion

        
        #region subscriptions

        /// <summary>
        /// Event when the TpLib changes due to an addition or deletion.
        /// NOTE that this event is NOT cleared when a scene loads or unloads, so if you have
        /// anything that could be deleted on a scene (un)load, be sure to use
        /// OnEnable and OnDisable to add and remove your callback target.
        /// </summary>
        public static event Action<TpLibChangeType,bool,Vector3Int,Tilemap? >? OnTpLibChanged;

        #if UNITY_EDITOR
        
        /// <summary>
        /// event fires IN EDITOR ONLY when either the TpLib
        /// tag data or type data has changed. The value passed is
        /// from the OnTypeOrTagChangedVariety enum.
        /// NOTE: the subscriber should only note the event and act on it
        /// during an inspector or editor update. No calls to TpLib or other
        /// TilePlus code should be made at the time of the event.
        /// </summary>
        public static event Action<OnTypeOrTagChangedVariety>? OnTypeOrTagChanged;

        /// <summary>
        /// event fires IN EDITOR ONLY when a Scene Scan has completed.
        /// </summary>
        public static event Action? OnSceneScanComplete;
        
        #endif

        #endregion

        #region constants

        /// <summary>
        /// a string for "Untagged" as Unity shows it.
        /// </summary>
        [HideInInspector] internal const string NoTagString = "Untagged";
        /// <summary>
        /// The initialization size for certain dicts
        /// </summary>
        [HideInInspector] public const int    TilemapAndTagDictsInitSize  = 128;
        /// <summary>
        /// The initialization size for the GUID-to-tile Dictionary
        /// </summary>
        [HideInInspector] public const int GuidDictInitialSize = 64;
        /// <summary>
        /// The initialization size for certain dicts
        /// </summary>
        [HideInInspector] public const int    TypesInitSize = 16;
        /// <summary>
        /// Size of Dictionaries (Vector3Int=to=TilePlusBase)
        /// when new ones are created in the pool
        /// </summary>
        [HideInInspector] public const int PoolNewItemSize_Dict_V3I_Tpb = 4; 
        /// <summary>
        /// Size of Lists of TilePlusBase
        /// when new ones are created in the pool
        /// </summary>
        [HideInInspector] public const int PoolNewItemSize_List_Tpb = 8; 
        /// <summary>
        /// This tag can't be used by users.
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        [HideInInspector] public const string ReservedTag   = "------";

        /// <summary>
        /// The name of the Prefab-GameObject with a TpLibUpdateProxy component.
        /// </summary>
        [HideInInspector] private const string TimingObjName = "_TPP___TIMING_";
        
        
        /// <summary>
        /// Initial value for maximum number of deferred callbacks per update
        /// </summary>
        [HideInInspector]
        public const uint MaxDeferredCallbackInitialValue = 16;
        /// <summary>
        /// Initial value for maximum number of Clonings per update
        /// </summary>
        [HideInInspector]
        public const uint MaxClonesInitialValue           = 16;
        
        
        #endregion

        #region privates

        /// <summary>
        /// true after TpLib is initialized. 
        /// </summary>
        #if ODIN_INSPECTOR
        [Sirenix.OdinInspector.ReadOnly] //name conflict with unity attr. Use explicit def of ReadOnly for Odin.
        #endif
        private static bool s_Initialized;

        /// <summary>
        /// A scene scan should be forced
        /// </summary>
        #if ODIN_INSPECTOR
        // ReSharper disable once Unity.RedundantHideInInspectorAttribute
        [HideInInspector]
        #endif
        private static bool s_ForceSceneScan;

        /// <summary>
        /// Scene scan is active
        /// </summary>
        #if ODIN_INSPECTOR
        // ReSharper disable once Unity.RedundantHideInInspectorAttribute
        [HideInInspector]
        #endif
        private static bool s_SceneScanActive;

        /// <summary>
        /// this is the main data structure in this class. It links
        /// maps (their instance IDs), positions, and TilePlusBase instances.
        /// </summary>
        #if ODIN_INSPECTOR
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly]
        #endif
        private static Dictionary<int, Dictionary<Vector3Int, TilePlusBase>> s_TilemapsDict = new(TilemapAndTagDictsInitSize);

        /// <summary>
        /// A mapping from strings (tags) to all tile instances with that tag
        /// </summary>
        #if ODIN_INSPECTOR
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly]
        #endif
        private static Dictionary<string, List<TilePlusBase>> s_TaggedTiles = new(TilemapAndTagDictsInitSize);

        /// <summary>
        /// Mapping from Types to Lists of Tiles of that Type
        /// </summary>
        #if ODIN_INSPECTOR
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly]
        #endif
        private static Dictionary<Type, List<TilePlusBase>> s_TileTypes = new(TypesInitSize);

        /// <summary>
        /// Mapping from Interfaces to Lists of Tiles implementing that interface
        /// </summary>
        #if ODIN_INSPECTOR
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly]
        #endif
        private static Dictionary<Type, List<TilePlusBase>> s_TileInterfaces = new(TypesInitSize);

        /// <summary>
        /// Map GUIDs to tiles.
        /// </summary>
        #if ODIN_INSPECTOR
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly]
        #endif
        private static  Dictionary<Guid, TilePlusBase> s_GuidToTile = new(GuidDictInitialSize);

        //These next two dicts are used to remap GUIDs from loaded prefabs to new ones.
        //this is necc since if the same prefab is loaded more than once it'll re-use the
        //same GUIDs which will cause the tiles to become rejected when they try to Register in TpLib.
        
        //a lookup from the NEW guids of TPT tiles from Prefabs to the old ones from the Prefab itself.
        #if ODIN_INSPECTOR
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly]
        #endif
        // ReSharper disable once CollectionNeverUpdated.Local
        private static Dictionary<Guid, Guid>? s_ReverseLoadedGuidLookup;
    
        //a lookup for finding TileFabs from GUIDs
        #if ODIN_INSPECTOR
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly]
        #endif
        // ReSharper disable once CollectionNeverUpdated.Local
        private static Dictionary<string, TpTileFab>? s_FabGuidToFabMap;
        
        
        /// <summary>
        /// hash of locked tilemaps
        /// </summary>
        #if ODIN_INSPECTOR
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly]
        #endif
        private static readonly HashSet<int> s_LockedTilemaps = new();
        
        //used in tilemap callbacks
        /// <summary>
        /// A list of positions where normal (ie Unity) tiles were added.
        /// </summary>
        private static readonly List<Vector3Int> s_NonTppTilesAddedOrModified = new(8);

        /// <summary>
        /// Used in  GetAllScenes
        /// </summary>
        private static readonly List<Scene> s_GetAllScenesList = new(8);
        
        /// <summary>
        /// used in DeleteFromTagDb
        /// </summary>
        private static readonly List<string> s_CurrentTags = new(8);

        /// <summary>
        /// Queue of TPT tiles waiting to be cloned. Also used for early registrations, see CloningData.
        /// </summary>
        private static readonly Queue<CloningData> s_CloningQueue = new(32);

        /// <summary>
        /// Queue of callbacks: these are filtered from calls to DelayedCallback when the delay is 10 msec or less.
        /// There's no need to use Task.Delay for these since they'd be executed on the next Update
        /// anyway.
        /// </summary>
        private static readonly Queue<DeferredCallbackData> s_CallbackQueue = new(8);

        //In editor-play or in a build, this is spawned to get Update.
        private static TpLibUpdateProxy? s_TimingProxy;

        //Size of new pooled Dicts and Lists        
        private static int s_DictPoolItemSize = PoolNewItemSize_Dict_V3I_Tpb;
        private static int s_ListPoolItemSize = PoolNewItemSize_List_Tpb;
        
        ///<summary>
        /// Backing field for MaxNumClonesPerUpdate.
        /// </summary> 
        private static uint s_MaxNumClonesPerUpdate = MaxClonesInitialValue;
        /// <summary>
        /// Backing field for MaxNumDeferredCallbacksPerUpdare 
        /// </summary>
        private static uint s_MaxNumDeferredCallbacksPerUpdate = MaxDeferredCallbackInitialValue;
        #endregion

        #region publicProperties

        /// <summary>
        /// Property to determine if a scene scan is in progress
        /// </summary>
        /// <value><c>true</c> if scene scan active; otherwise, <c>false</c>.</value>
        public static bool IsSceneScanActive => s_SceneScanActive;

        /// <summary>
        /// Inhibits OnTpLibChanegedEvents: Editor ONLY
        /// </summary>
        public static bool InhibitOnTpLibChanged { get; set; }
        
        /// <summary>
        /// Inhibits tilemap callbacks (OnTilemapPositionChanged, OntilemapTileChanged).
        /// Useful if you're doing a lot of Tilemap operations but don't want the
        /// system to handle these callbacks. Note that the internal DBs of TpLib will
        /// be incorrect; use SceneScan afterwards. That's not automatic!!
        /// </summary>
        public static bool InhibitTilemapCallbacks { get; set; }
        
        
        /// <summary>
        /// Returns true if the TpLib is ready to access. Doesn't
        /// mean that the databases are up to date, as that happens
        /// when the tilemap starts up and tiles are refreshed.
        /// </summary>
        /// <value><c>true</c> if TpLib has been initialized]; otherwise, <c>false</c>.</value>
        // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
        public static bool TpLibIsInitialized => s_Initialized;

        /// <summary>
        /// How many tilemaps are being tracked.
        /// </summary>
        /// <value>The tilemaps count.</value>
        public static int TilemapsCount => s_TilemapsDict.Count;

        /// <summary>
        /// Allows accessing the optional Guid maps. See GetTilePlusBaseFromGuid variants. 
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public static Dictionary<Guid, TilePlusBase>? OptionalGuidToTileMapping { get; set; }

        /// <summary>
        ///  Number of different tags.
        /// </summary>
        public static int TaggedTilesCount => s_TaggedTiles.Count;

        /// <summary>
        /// Number of different Types
        /// </summary>
        public static int TileTypesCount => s_TileTypes.Count;
        
        /// <summary>
        /// Number of interfaces
        /// </summary>
        public static int TileInterfacesCount => s_TileInterfaces.Count;
        
        /// <summary>
        /// Number of GUIDs
        /// </summary>
        public static int GuidToTileCount => s_GuidToTile.Count;

        /// <summary>
        /// Get all tags
        /// </summary>
        /// <returns>an array of strings with the tags or null if there aren't any.</returns>
        public static IEnumerable<string> GetAllTagsInDb => s_TaggedTiles.Keys.Where(key=>s_TaggedTiles[key].Count != 0);

        /// <summary>
        /// Get all tiles. Note: uses GuidToTile dictionary's Values.
        /// </summary>
        public static Dictionary<Guid, TilePlusBase>.ValueCollection GetAllTilesRaw => s_GuidToTile.Values;
        
        /// <summary>
        /// A list of non tileplus tiles that were added or modified
        /// </summary>
        // ReSharper disable once ConvertToAutoPropertyWhenPossible
        public static List<Vector3Int> NonTppTilesAddedOrModified => s_NonTppTilesAddedOrModified;
        
        /// <summary>
        /// How many active delayed callbacks
        /// </summary>
        public static int CurrentActiveDelayedCallbacks { get; private set; }

        /// <summary>
        /// For statistics. Observable in SysInfo window.
        /// </summary>
        public static int CloneQueueMaxDepth { get; private set; }

        /// <summary>
        /// For statistics. Observable in SysInfo window.
        /// </summary>
        public static int CloneQueueDepth => s_CloningQueue.Count;
        
        /// <summary>
        /// For statistics. Observable in SysInfo window.
        /// </summary>
        public static int DeferredQueueMaxDepth { get; private set; }

        /// <summary>
        /// For statistics. Observable in SysInfo window.
        /// </summary>
        public static int DeferredQueueDepth => s_CallbackQueue.Count;

        /// <summary>
        /// Set the maximum number of tile clones that can occur on one Update. Note that if you set this
        /// to zero or LT 0 then the value used is uint.maxvalue which is a pretty big number.
        /// This value should be relatively small, but the exact value is app-dependent. If you never
        /// place TPT tiles in a running game then this can be ignored. A too-small value will increase
        /// the delay from when a TPT tile is painted to when it is cloned. That can affect the appearance
        /// of animated tiles.  
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public static uint MaxNumClonesPerUpdate
        {
            get => s_MaxNumClonesPerUpdate;
            set
            {
                if(value == 0)
                    value = uint.MaxValue;
                s_MaxNumClonesPerUpdate = value;
            }
        }

        /// <summary>
        /// Set the maximum number of deferred callbacks that can occur on one Update. Note that if you set this
        /// to zero or LT 0 then the value used is long.maxvalue which is a pretty big number.
        /// This value should be relatively small, but the exact value is app-dependent.
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public static uint MaxNumDeferredCallbacksPerUpdate
        {
            get => s_MaxNumDeferredCallbacksPerUpdate;
            set
            {
                if(value == 0)
                    value = uint.MaxValue;
                s_MaxNumDeferredCallbacksPerUpdate = value;
            }
        }

        /// <summary>
        /// Get the current memory allocation settings for TpLib
        /// </summary>
        public static TpLibMemAlloc MemAllocSettings { get; private set; }

        /// <summary>
        /// Get a string with Name, Version, Build Timestamp
        /// </summary>
        public static string VersionInformation  
        {
            get
            {
                var assembly      = typeof(TpLib).Assembly;
                var copyrightData = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;
                var desc          = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>().Description;
                var timeStamp     = File.GetLastWriteTime(assembly.Location).ToUniversalTime().ToString(CultureInfo.InvariantCulture);
                return $"TilePlus Toolkit (TM) TpLib Version [{assembly.GetName().Version}-{desc}], Build-Time[{timeStamp} UTC] {copyrightData}";
            }
        }

        /// <summary>
        /// Get a string with abbreviated version information.
        /// </summary>
        public static string ShortVersionInformation
        {
            get
            {
                var assembly      = typeof(TpLib).Assembly;
                var desc          = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>().Description;
                return ($"{assembly.GetName().Version}-{desc}");
            }
        }
        
        #if UNITY_EDITOR
        /// <summary>
        /// show info messages
        /// </summary>
        /// <value><c>true</c> if informational messages can be shown; otherwise, <c>false</c>.</value>
        internal static bool Informational => TpEditorBridge.Informational;

        /// <summary>
        /// show warning messages 
        /// </summary>
        /// <value><c>true</c> if warning messages can be shown; otherwise, <c>false</c>.</value>
        internal static bool Warnings => TpEditorBridge.Warnings;

        /// <summary>
        /// show error messages 
        /// </summary>
        /// <value><c>true</c> if error messages can be shown; otherwise, <c>false</c>.</value>
        // ReSharper disable once MemberCanBePrivate.Global
        internal static bool Errors => TpEditorBridge.Errors;
        
        #else
        internal static bool Informational => false;
        internal static bool Warnings => false;
        internal static bool Errors => true;
        #endif
        
        #endregion

        #region init
        
        //static constructor invoked via [InitializeOnLoad] 
        /// <summary>
        /// Static class ctor
        /// </summary>
        static TpLib()
        {
            //Debug.Log("TpLib static constructor.");

            //set default memory allocations
            MemAllocSettings = new TpLibMemAlloc
            {
                m_TilemapAndTagDictsSize = TilemapAndTagDictsInitSize,
                m_GuidDictSize = GuidDictInitialSize,
                m_TypesSize = TypesInitSize,
                m_PoolNewItemSizeForDicts = PoolNewItemSize_Dict_V3I_Tpb,
                m_PoolNewItemSizeForLists = PoolNewItemSize_List_Tpb
            };
                     
            InitializeTpLib();
        }
       

        /// <summary>
        /// Callbacks control.
        /// </summary>
        internal static void CallbacksControl()
        {
            #if UNITY_EDITOR
            var isPlaying = EditorApplication.isPlayingOrWillChangePlaymode;
            #endif
            
            if (s_Initialized)
            {
                if (Informational)
                    TpLog("TpLib: unregistering callbacks (not an error)");
                #if UNITY_EDITOR
                if(!isPlaying)
                {
                    Selection.selectionChanged             -= OnSelectionChanged;
                    EditorApplication.update               -= OnEditorApplicationUpdate;
                }
                
                SceneManager.sceneUnloaded      -= SceneManagerOnSceneUnloaded;
               
                #endif

                Tilemap.tilemapPositionsChanged -= OntilemapPositionsChanged;
                Tilemap.tilemapTileChanged      -= OntilemapTileChanged;

                OnTpLibChanged                  =  null;
                TpEvents.ClearOnTileEventSubscribers();
            }

            s_Initialized = true;
            #if UNITY_EDITOR
            if (!isPlaying)
            {
                Selection.selectionChanged += OnSelectionChanged;
                EditorApplication.update   += OnEditorApplicationUpdate;
            }

            SceneManager.sceneUnloaded      += SceneManagerOnSceneUnloaded;
            #endif
            
            Tilemap.tilemapPositionsChanged += OntilemapPositionsChanged;
            Tilemap.tilemapTileChanged      += OntilemapTileChanged;
        }
        

        /// <summary>
        /// Actual initializer for TpLib.
        /// </summary>
        private static void InitializeTpLib() 
        { 
            //Debug.Log("Initializing TpLib runtime...");
            CallbacksControl(); //set up callbacks
            MaxNumClonesPerUpdate            = MaxClonesInitialValue;
            MaxNumDeferredCallbacksPerUpdate = MaxDeferredCallbackInitialValue;
            
            var cloningQueueCount = s_CloningQueue.Count;
            if (cloningQueueCount != 0)
            {
                TpLog($"{cloningQueueCount} deferred TilePlus registrations activating...");

                for (var i = 0; i < cloningQueueCount; i++)
                {
                    var item = s_CloningQueue.Dequeue();
                    RegisterTilePlus(item.Tile, item.Position, item.Tilemap);
                    s_CloningDataPool.Release(item);
                }
            }
            
            CleanState();
            
        }

        #endregion

        #region SceneScan

        

        /// <summary>
        /// Clears TpLib state.
        /// </summary>
        private static void CleanState()
        {
            MaxNumClonesPerUpdate            = MaxClonesInitialValue;
            MaxNumDeferredCallbacksPerUpdate = MaxDeferredCallbackInitialValue;
            
            //unpooled
            s_LockedTilemaps.Clear();
            s_GuidToTile.Clear();
            s_CurrentTags.Clear();
            s_NonTppTilesAddedOrModified.Clear();
            s_ReverseLoadedGuidLookup?.Clear();
            s_FabGuidToFabMap?.Clear();
            
            TpEvents.ClearQueuedTileEvents();
           
            //release pooled items for the rest.
            //while it might not seem important to do so, given that domain reloads MAY NOT occur
            //based on the project's domain/scene reload settings regarding entering play mode,
            //it's important to always ensure that all references that might be held in
            //these lists and/or pools be released in order to avoid memory leaks as much as possible.
            foreach (var dict in s_TilemapsDict.Values)
                s_DictOfV3IToTpb_Pool.Release(dict);
            s_TilemapsDict.Clear();
            
            foreach(var l in s_TaggedTiles.Values)
                S_TilePlusBaseList_Pool.Release(l);
            s_TaggedTiles.Clear();            
            
            foreach(var l in s_TileTypes.Values)
                S_TilePlusBaseList_Pool.Release(l);
            s_TileTypes.Clear();
            
            foreach (var l in s_TileInterfaces.Values)
                S_TilePlusBaseList_Pool.Release(l);
            s_TileInterfaces.Clear();
            
            foreach(var l in s_CloningQueue.ToArray())
                s_CloningDataPool.Release(l);
            s_CloningQueue.Clear();
            
            foreach(var l in s_CallbackQueue.ToArray())
                s_DeferredCallbackPool.Release(l);
            s_CallbackQueue.Clear();

        }

        /// <summary>
        /// Scan all scenes including complete scan of all scenes and their Tilemaps
        /// </summary>
        public static void SceneScan()
        {
            SceneScan(null);
        }

        /// <summary>
        /// Scans all scenes, gets all tilemaps, populate database.
        /// </summary>
        /// <param name="maps">Optional tilemaps list. If provided, scenes not scanned nor state cleaned.</param>
        /// <param name="cleanState">execute CleanState (default=true)</param>
        /// <remarks>Clears the database if tilemaps list not provided.</remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        public static void SceneScan(List<Tilemap>? maps, bool cleanState=true)
        {
            if (Informational)
                TpLog("Scene-scan in progress...");

            s_SceneScanActive = true;

            if (maps != null)
            {
                foreach (var map in maps)
                {
                    var usedTilesCount = map.GetUsedTilesCount();
                    if (usedTilesCount == 0)
                        continue;
                    var arr = new TileBase[usedTilesCount];
                    map.GetUsedTilesNonAlloc(arr);
                    //if any of the tiles are locked, add to the list of locked maps
                    if (arr.Any(tb =>
                        {
                            var tpb = tb as TilePlusBase;
                            return tpb != null && tpb.IsLocked;
                        }))
                        s_LockedTilemaps.Add(map.GetInstanceID());
                    map.RefreshAllTiles();
                }
            }

            else
            {
                if(cleanState)
                    CleanState();

                using (s_TileMapList_Pool.Get(out var tilemaps))
                {
                    foreach (var scene in GetAllScenes())
                        GetTilemapsInScene(scene, ref tilemaps);

                    foreach (var map in tilemaps)
                    {
                        var usedTilesCount = map.GetUsedTilesCount();
                        if (usedTilesCount == 0)
                            continue;
                        var arr = new TileBase[usedTilesCount];
                        map.GetUsedTilesNonAlloc(arr);
                        //if any of the tiles are locked, add to the list of locked maps
                        if (arr.Any(tb =>
                            {
                                var tpb = tb as TilePlusBase;
                                return tpb != null && tpb.IsLocked;
                            }))
                            s_LockedTilemaps.Add(map.GetInstanceID());
                        map.RefreshAllTiles();
                    }
                }
            }

            s_SceneScanActive = false;
            if (Informational)
                TpLog("Scene-scan complete...");
            #if UNITY_EDITOR
            OnSceneScanComplete?.Invoke();
            #endif

            

        }

        /// <summary>
        /// Get a list of all scenes
        /// </summary>
        /// <param name="ignorePrefabStages">Set true to ignore prefab stages</param>
        /// <returns>a list of scenes</returns>
        /// <remarks>NOTE that the returned list is the SAME list for each method call,
        /// so use care.</remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once ReturnTypeCanBeEnumerable.Global
        public static List<Scene> GetAllScenes(bool ignorePrefabStages = false)
        {
            var numScenes = SceneManager.sceneCount;
            s_GetAllScenesList.Clear();
            
            #if UNITY_EDITOR
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                s_GetAllScenesList.Add(stage.scene);
                return s_GetAllScenesList;
            }
            #endif
            
            for (var i = 0; i < numScenes; i++)
                s_GetAllScenesList.Add(SceneManager.GetSceneAt(i));

            return s_GetAllScenesList;
        }


        /// <summary>
        /// Get all tilemaps in the scene. Optional filtering.
        /// </summary>
        /// <param name="aScene">A scene to test</param>
        /// <param name="tilemapsList">ref to a pre-allocated List of tilemaps.</param>
        /// <param name="includeNonTptMaps">if true include maps with no TPT tiles.</param>
        /// <returns>true for success, or false for invalid scene/null ref variable (compiler should warn)</returns>
        /// <remarks>Note that tilemapsList isn't cleared: intentional</remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        public static bool GetTilemapsInScene(Scene aScene, ref List<Tilemap> tilemapsList, bool includeNonTptMaps = false)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (!aScene.IsValid() || !aScene.isLoaded || tilemapsList == null)
                return false;

            //get the root GameObjects in the scene
            var rootGameObjects = aScene.GetRootGameObjects();
            var len             = rootGameObjects.Length;
            if (len == 0) //if there aren't any (????) bail out.
                return false;

            for (var gameObjIndex = 0; gameObjIndex < len; gameObjIndex++)
            {
                var tmaps  = rootGameObjects[gameObjIndex].GetComponentsInChildren<Tilemap>();
                var nTmaps = tmaps.Length;
                if (nTmaps == 0) //no tilemaps? No problem!
                    continue;
                
                for (var mapIndex = 0; mapIndex < nTmaps; mapIndex++)
                {
                    var map = tmaps[mapIndex];
                    if (map == null || !map.gameObject.activeInHierarchy)  
                        continue;

                    //Ignore tilemaps with NoPaint, editor only. Change in 2.02
                    #if UNITY_EDITOR
                    if (map.TryGetComponent<TpNoPaint>(out _))
                        continue;
                    #endif
                    
                    //in-editor, ignore the Palette tilemap
                    #if UNITY_EDITOR
                    if (IsTilemapFromPalette(map))
                        continue;
                    #endif

                    if (includeNonTptMaps)
                    {
                        tilemapsList.Add(map);
                        continue;
                    }

                    var numTiles  = map.GetUsedTilesCount();             //number of different tile types
                    var usedTiles = new TileBase[numTiles];              //allocate array
                    var found     = map.GetUsedTilesNonAlloc(usedTiles); //get the different tile types
                    //if any of the tiles in usedTiles is TilePlusBase then add that tilemap to the input list.
                    for (var i = 0; i < found; i++)
                    {
                        if (!(usedTiles[i] as TilePlusBase))
                            continue;
                        tilemapsList.Add(map);
                        break;
                    }
                }
            }  
            return true;
        }
        #endregion

        #region AddDeleteRegister

        /// <summary>
        /// A TPT tile calls this in StartUp when cloning itself. NOT for general-purpose use.
        /// </summary>
        /// <param name="instance">TilePlusBase instance</param>
        /// <param name = "position" >current tile position</param>
        /// <param name = "map" >current tile's parent Tilemap</param>
        /// <param name="allowLockedTiles">clone a locked tile if true. Normally false.</param>
        /// <remarks>The clone 'requests' are put into a Queue and execute on the next
        /// EditorUpdate (not playing, in Editor) or Monobehaviour Update sourced from a DontDestroyOnLoad GameObject (playing, or in a build).
        /// When an item is taken out of the Queue the tile is cloned. If at that time the Tilemap or TPB instance values saved
        /// in the CloningData class instance removed from the queen turn out to be null then no action is taken and
        /// the tile isn't cloned. Also note: if you create tiles at Runtime then the first time that this method is used
        /// the DontDestoyOnLoad GameObject is instantiated which takes a little time. To avoid that, use PreloadTimingSubsytem().
        /// And yet another note about runtime use: many tiles painted at once will enlarge the queue and when the
        /// next Update occurs all of the Cloning will be done at once. If that's an issue, use the
        /// MaxNumClonesPerUpdate property to non-zero (zero indicates do all of them).</remarks>
        public static void CloneTilePlus(TilePlusBase instance, Vector3Int position, Tilemap map, bool allowLockedTiles = false)
        {
            if (!allowLockedTiles && instance.IsLocked)
            {
                TpLogError("Cannot clone a locked tile!");
                return;
            }
            var cd = s_CloningDataPool.Get();
            cd.Position = position;
            cd.Tilemap  = map;
            cd.Tile     = instance;
            s_CloningQueue.Enqueue(cd );
            PreloadTimingSubsystem();


        }

        /// <summary>
        /// Preload timing subsystem for TpLib. Ignored in Editor mode unless PLAYing.
        /// Ignored if the timing subsystem is already loaded. 
        /// </summary>
        /// <remarks>Your app can call this during its initialization so that the
        /// init within this method doesn't have to run during your game's loop.
        /// </remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        public static void PreloadTimingSubsystem()
        {
            #if UNITY_EDITOR
            //Added V2.02 14 Nov 23. Ensures that cloning TPT tiles will always work. Note that
            // cloning only occurs when a tile is painted at runtime or in an editor session ie
            // using the Palette or Tile+Painter. TileFab and Bundle loading handles cloning
            // differently.
            if (!Application.isPlaying)
            {
                var callbacks = EditorApplication.update.GetInvocationList().Select(x=>x.Method.ToString());
                if (callbacks.Contains("Void OnEditorApplicationUpdate()"))
                    return;
                Debug.LogWarning("Reinitializing TpLib callbacks...");
                CallbacksControl();
                return;
            }
            #endif
            
            //if in Play mode, ensure that the GO which passes Update ticks to TpLib is present.
            //if not, create one. Note that the TpLibUpdateProxy component sets the GO to DontDestroyOnLoad,
            //so in Editor-run or in a build this returns immediately unless the GO isn't present.
            if (s_TimingProxy != null) //already found
                return; 
            var go = GameObject.Find(TimingObjName); //brute-force find. 
            if (go != null)  //found it!
            {
                s_TimingProxy = go.GetComponent<TpLibUpdateProxy>();
                s_TimingProxy.Run();
            }
            //else programmatically create one.
            go = new GameObject(TimingObjName, new[] { typeof(TpLibUpdateProxy) });
            if (go != null) //all good
            {
                s_TimingProxy = go.GetComponent<TpLibUpdateProxy>();
                s_TimingProxy.Run();
                return;
            }
            //this really should not occur...
            Debug.LogError($"Fatal error: can't create {TimingObjName}. Fix error and do a scripting reload!!");
            s_Initialized = false;
        }
        
        /// <summary>
        /// Tiles must call this as part of their StartUp method in order to
        /// register the tile in the various data structures.
        /// This is automatically handled in TilePlusBase
        /// so anything that inherits from that doesn't need to do anything special.
        /// </summary>
        /// <param name="instance">The tile plus base instance.</param>
        /// <param name = "position" >The tile position</param>
        /// <param name = "tilemap" >The tile's parent tilemap</param>
        /// <remarks>Note that Adding a tilemap programmatically and then
        /// adding TilePlusBase-derived tiles programatically will
        /// automatically update the DB when the tiles' StartUp method
        /// is invoked. </remarks>
        public static void RegisterTilePlus(TilePlusBase? instance, Vector3Int position, Tilemap? tilemap)
        {
            if (instance == null)
            {
                #if UNITY_EDITOR
                if (Errors)
                    TpLogError("Null TilePlusBase instance passed to TpLib.AddTileToDb. ");
                #endif
                return;
            }
            
            if (!s_Initialized) //scan will pick it up later.
            {
                var cd = s_CloningDataPool.Get();
                cd.IsDelayedRegistration = true;
                cd.Position = position;
                cd.Tilemap = tilemap;
                cd.Tile = instance;
                s_CloningQueue.Enqueue(cd);
                return;
            }

            if (tilemap == null)
            {
                #if UNITY_EDITOR
                if (Errors)
                    TpLogError("Null map from tile in TpLib.AddTileToDb.");
                #endif
                return;
            }
            
           

            if (instance.IsAsset)
            {
                #if UNITY_EDITOR
                if (Errors)
                    TpLogError("Cannot add Asset-state tiles!");
                #endif
                return;
            }

            var mapId = tilemap.GetInstanceID();
            var tileGuid = instance.TileGuid;
            
            //if this tile is locked but it's tilemap isn't already known as a locked tilemap, "make it so"
            if (instance.IsLocked) 
            {
                s_LockedTilemaps.Add(mapId);
            }
            
            else //this only applies to clones - locked assets are in the Project and copying is fine since it's all the same locked asset.
            {
                //is there a tile with the same GUID?
                if(s_GuidToTile.ContainsKey(tileGuid))
                {
                    //it's the same tile - is it being refreshed or copied?
                    //change in pos or map for a tile instance infers either a move OR a copy/paste.
                    if (instance.TileGridPosHasChanged || instance.ParentTilemapHasChanged) //pos or map changed
                    {
                        #if UNITY_EDITOR
                        //but not for the MOVE tool when the Editor isn't in PLAY mode.
                        //if app isn't playing and it isn't the move tool then copy/paste
                        //otherwise drop down to the DB add below.
                        if (!Application.isPlaying && ToolManager.activeToolType != typeof(MoveTool))
                        #endif
                        {
                            //see the remarks for ResetState for an explanation of what this does.
                            instance.ResetState(TileResetOperation.Restore);
                            CopyAndPasteTile(tilemap, instance, position);
                            return;
                        }
                    }
                    else //no change so don't do anything
                    {
                        #if UNITY_EDITOR
                        var isPreview = tilemap.HasEditorPreviewTile(position);
                        if (Informational)
                            TpLog($"Repeated add [preview?:{isPreview}] {instance.name} id {instance.Id} map: {tilemap.name} position: {position} ");
                        #endif
                        return; //don't do anything else
                    }
                }
            }

            var addedToPreviouslyEmptyTilemap = false;
            //add to map->dict(position,tile)
            //does an inner dict already exist for this tilemap?
            if (!s_TilemapsDict.TryGetValue(mapId, out var currentTppDict))
            {
                //if not, create it and add the instance
                s_TilemapsDict.Add(mapId, currentTppDict = s_DictOfV3IToTpb_Pool.Get());
                currentTppDict.Add(position, instance);
                addedToPreviouslyEmptyTilemap = true;
            }
            else
                currentTppDict[position] = instance; //new entry

            
            //add GUID
            var guid = instance.TileGuid;
            s_GuidToTile.TryAdd(guid, instance);
            
            //add Tag
            AddToTagDb(instance );

            //type
            var typ = instance.GetType();

            if (s_TileTypes.ContainsKey(typ))
            {
                if (!s_TileTypes[typ].Contains(instance))
                    s_TileTypes[typ].Add(instance);
            }
            else
            {
                var l = S_TilePlusBaseList_Pool.Get(); 
                l.Add(instance);
                s_TileTypes.Add(typ, l);
                #if UNITY_EDITOR
                OnTypeOrTagChanged?.Invoke(OnTypeOrTagChangedVariety.Type); //raise Type changed event
                #endif

            }

            //interfaces
            var myInterfaces = typ.GetInterfaces();
            // ReSharper disable once ForCanBeConvertedToForeach
            for(var i = 0; i < myInterfaces.Length; i++)
            {
                var interf = myInterfaces[i];
                if (s_TileInterfaces.ContainsKey(interf))
                {
                    if (!s_TileInterfaces[interf].Contains(instance))
                        s_TileInterfaces[interf].Add(instance);
                }
                else
                {
                    var l = S_TilePlusBaseList_Pool.Get();
                    l.Add(instance);
                    s_TileInterfaces.Add(interf, l);
                }
            }
            

            #if UNITY_EDITOR
            if (Informational)
                TpLog($"[SceneScanActive:{s_SceneScanActive}] Tile added to DB: {instance.TileName} [id:{instance.Id}]  on Tilemap {(instance.ParentTilemap!=null?instance.ParentTilemap.name:"Unknown or null Tilemap")} @ {instance.TileGridPosition.ToString()}  GUID:{instance.TileGuidString}");

            #endif
            if(s_SceneScanActive || InhibitOnTpLibChanged)
                return;
            OnTpLibChanged?.Invoke(addedToPreviouslyEmptyTilemap
                                                             ? TpLibChangeType.AddedToEmptyMap
                                                             : TpLibChangeType.Added,
                                                         false,
                                                         position,
                                                         tilemap);
        }

        /// <summary>
        /// Remove a tile from the database.
        /// NB - although this is public you probably want to use DeleteTile().
        /// This does not actually delete a tile.
        /// </summary>
        /// <param name="map">What map is it on?</param>
        /// <param name="position">what's its position?</param>
        /// <param name="fromGroup">Set true if you're deleting this tile as one of a group of deletions.
        /// This parameter is not used at runtime</param>
        /// <returns>True for success, false for errors.</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public static bool RemoveTileFromDb(Tilemap?   map,
                                            Vector3Int position,
                                            bool       fromGroup = false)
        {
            if (map != null)
                return RemoveTileFromDb(map.GetInstanceID(), map, position, fromGroup);
            #if UNITY_EDITOR
            if (Errors)
                TpLogError("Null map passed to TpLib.RemoveTileFromDb.");
            #endif
            return false;

        }

        /// <summary>
        /// Remove a tile from the database.
        /// This does not actually delete a tile.
        /// </summary>
        /// <param name="tilemapInstanceId">Instance Id of tile's parent tilemap</param>
        /// <param name="map">Tilemap instance or null - note is only used in Event invoke at end of method.</param>
        /// <param name="position">what's its position?</param>
        /// <param name="fromGroup">Set true if you're deleting this tile as one of a group of deletions.
        /// This parameter is not used at runtime</param>
        /// <returns>True for success, false for errors.</returns>
        private static bool RemoveTileFromDb(int tilemapInstanceId, Tilemap? map, Vector3Int position, bool fromGroup = false)
        {
            if (position == TilePlusBase.ImpossibleGridPosition)
            {
                #if UNITY_EDITOR
                if (Warnings)
                    TpLogWarning("Invalid grid position was passed to TpLib.RemoveTileFromDb. Usually can be ignored.");
                #endif
                return false;
            }

            if (!s_TilemapsDict.TryGetValue(tilemapInstanceId, out var currentDict))
            {
                #if UNITY_EDITOR
                if (Errors)
                    TpLogError($"Invalid map instance ID [{tilemapInstanceId.ToString()}] was passed to TpLib.RemoveTileFromDb.");
                #endif
                return false;
            }

            if (!currentDict.TryGetValue(position, out var instance))
            {
                #if UNITY_EDITOR
                if (Warnings)
                    TpLogWarning($"Invalid position ({position.ToString()}) on Tilemap {map} passed to TpLib.RemoveTileFromDb.");

                #endif
                return false;
            }

           

            //remove any pending events for this tile.
            TpEvents.RemoveEvent(TileEventType.Both, instance); 
            
            //with instance,map, and position, handle the type and tag dictionaries
            DeleteFromTagDb(instance);

            
            //type
            var typ        = instance.GetType();
            if (s_TileTypes.TryGetValue(typ, out var typList))
            {
                typList.Remove(instance);
                if (typList.Count == 0)
                {
                    s_TileTypes.Remove(typ);
                    S_TilePlusBaseList_Pool.Release(typList);
                    #if UNITY_EDITOR
                    OnTypeOrTagChanged?.Invoke(OnTypeOrTagChangedVariety.Type); //raise Type changed event
                    #endif
                }
            }

            //interfaces
            var interfaces = typ.GetInterfaces();
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < interfaces.Length; i++)
            {
                var intf = interfaces[i];
                if (!s_TileInterfaces.TryGetValue(intf, out var intfList)) 
                    continue;
                intfList.Remove(instance);
                if (intfList.Count != 0) 
                    continue;
                s_TileInterfaces.Remove(intf);
                S_TilePlusBaseList_Pool.Release(intfList);
            }
            
            //delete this tile from the GUID->tile mapping
            var guid = instance.TileGuid;
            s_GuidToTile.Remove(guid);

            currentDict.Remove(position);

            if (currentDict.Count == 0) //if the dictionary is now empty
            {
                s_DictOfV3IToTpb_Pool.Release(currentDict);
                s_TilemapsDict.Remove(tilemapInstanceId); //remove it
                s_LockedTilemaps.Remove(tilemapInstanceId); //if there are no TPT tiles on this map then logically this map isn't locked wrt locked tiles.
            }
            else //if the dict entry for this map isn't empty then are there any locked tiles left?
            {
                var tiles = currentDict.Values;
                if(!tiles.Any(tpb => tpb.IsLocked)) //are there any locked tiles at all?
                    s_LockedTilemaps.Remove(tilemapInstanceId); 
            }

            #if UNITY_EDITOR
            if (Informational) 
                TpLog($"Tile deleted from DB: {instance.TileName} [id:{instance.Id}]  on Tilemap Id {tilemapInstanceId} @ {position}  GUID:{guid}");
            #endif

            if(!InhibitOnTpLibChanged)
                OnTpLibChanged?.Invoke(TpLibChangeType.Deleted, fromGroup, position, map); //map param could be null here!
            return true;
        }
        
        
        /// <summary>
        /// Adds to tag database.
        /// </summary>
        /// <param name="instance">TilePlusBase instance</param>
        private static void AddToTagDb(TilePlusBase instance)
        {
            (var num, var tags) = instance.TrimmedTags;
            
            if (num == 0)
                return;

            #if UNITY_EDITOR
            var didAdd = false;
            #endif
            
            for (var i = 0; i < num; i++)
            {
                var tag = tags[i];
                
                if (tag == ReservedTag)
                {
                    TpLogError($"Error: Tag {ReservedTag} cannot be added to a tile!");
                    continue;
                }

                
                if (s_TaggedTiles.ContainsKey(tag))
                {
                    if (!s_TaggedTiles[tag].Contains(instance))
                        s_TaggedTiles[tag].Add(instance);
                }
                else
                {
                    var l = S_TilePlusBaseList_Pool.Get(); 
                    l.Add(instance);
                    s_TaggedTiles.Add(tag,l);
                    #if UNITY_EDITOR
                    didAdd = true;
                    #endif
                }
            }
            #if UNITY_EDITOR
            if(didAdd)
                OnTypeOrTagChanged?.Invoke(OnTypeOrTagChangedVariety.Tag); //raise tag changed event
            #endif

        }

        
        /// <summary>
        /// If a tag field is empty or if a tile is being deleted
        /// then this can be used to delete the tile from s_TaggedTiles
        /// </summary>
        /// <param name="instance">TilePlusBase instance</param>
        private static void DeleteFromTagDb(TilePlusBase instance)
        {
            (var num, var tags) = instance.TrimmedTags;
            if (num == 0)
            {
                //if tags don't exist now, the instance might have had tags before.
                //look at EVERY tag list, remove the instance from the tag list if it's there, and if the tag list is empty then delete the list.
                
                s_CurrentTags.Clear();
                
                foreach ((var tag, var instanceList) in s_TaggedTiles)
                {
                    if(!instanceList.Contains(instance))
                        continue;
                    if (instanceList.Count == 1)
                    {
                        s_CurrentTags.Add(tag);
                        continue;
                    }

                    instanceList.Remove(instance);
                    if (instanceList.Count == 0)
                        s_CurrentTags.Add(tag);
                }
                if(s_CurrentTags.Count == 0)
                    return;

                foreach (var key in s_CurrentTags)
                    s_TaggedTiles.Remove(key);
                
                s_TaggedTiles.Clear();
                #if UNITY_EDITOR
                OnTypeOrTagChanged?.Invoke(OnTypeOrTagChangedVariety.Tag); //raise tag changed event
                #endif

                return;
            }

            #if UNITY_EDITOR
            var didRemove = false;
            #endif
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var tagIndex = 0; tagIndex < tags.Length; tagIndex++)
            {
                if(!s_TaggedTiles.TryGetValue(tags[tagIndex], out var list))
                    continue;
                //so this list has the tag (key) of one of the tile's tags. 
                //AddToTagDb ensures that only one instance of the tile is in the list.
                var lIndex = list.FindIndex(tpb => tpb == instance);
                if (lIndex == -1)
                {
                    #if UNITY_EDITOR
                    TpLogWarning("Warning: removing tagged tile: could not find it. NonFatal");
                    continue;
                    #endif
                }

                #if UNITY_EDITOR
                didRemove = true;
                #endif
                list.RemoveAt(lIndex);
                if (list.Count != 0)
                    continue;
                S_TilePlusBaseList_Pool.Release(list);
                s_TaggedTiles.Remove(tags[tagIndex]);
            }
            #if UNITY_EDITOR
            if(didRemove)
                OnTypeOrTagChanged?.Invoke(OnTypeOrTagChangedVariety.Tag); //raise tag changed event
            #endif

        }

        /// <summary>
        /// Unload a scene.  Optional: generally the SceneManagerOnSceneUnloaded callback handles this.
        /// </summary>
        /// <param name="scene">Name of a scene. </param>
        public static void UnloadScene(Scene scene)
        {
            if (Informational)
                TpLog($"Scene {scene.name} unloaded");

            using (s_TileMapList_Pool.Get(out var tilemaps))
            {
                GetTilemapsInScene(scene, ref tilemaps);
                foreach (var map in tilemaps)
                    RemoveMap(map);
            }
            
        }
        
        /// <summary>
        /// Callback for SceneManager.sceneUnloaded
        /// </summary>
        /// <param name="scene">The scene.</param>
        private static void SceneManagerOnSceneUnloaded(Scene scene)
        {
            if (Informational)
                TpLog($"Scene {scene.name} unloaded");

            using (s_TileMapList_Pool.Get(out var tilemaps))
            {
                GetTilemapsInScene(scene, ref tilemaps);
                foreach (var map in tilemaps)
                    RemoveMap(map);
            }
        }

        #endregion

        #region dbUtils

        /// <summary>
        /// Does a tilemap have any Tileplus tiles?
        /// </summary>
        /// <param name="map">The map.</param>
        /// <returns><c>true</c> if [is tilemap registered] [the specified map]; otherwise, <c>false</c>.</returns>
        public static bool IsTilemapRegistered(Tilemap? map)
        {
            return map != null && s_TilemapsDict.ContainsKey(map.GetInstanceID());
        }

        
        /// <summary>
        /// Get a tile from the master dictionary
        /// </summary>
        /// <param name="map">which tilemap</param>
        /// <param name="position">grid position</param>
        /// <returns>the TilePlusBase instance or null if not found or the map has no TilePlus tiles</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public static TilePlusBase? GetTile(Tilemap map, Vector3Int position)
        {
            return !s_TilemapsDict.TryGetValue(map.GetInstanceID(), out var innerDict)
                       ? null
                       : innerDict.GetValueOrDefault(position); //returns null if nothing found.
        }

        /// <summary>
        /// Is there a tile at the provided position on the provided map: TilePlus tiles only!
        /// </summary>
        /// <param name="map">tilemap to look at</param>
        /// <param name="position">position to examine</param>
        /// <returns><c>true</c> if the specified map has tile; otherwise, <c>false</c>.</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public static bool HasTile(Tilemap map, Vector3Int position)
        {
            return s_TilemapsDict.TryGetValue(map.GetInstanceID(), out var innerDict) && innerDict.ContainsKey(position);
        }

        /// <summary>
        /// To use at runtime if you want the database functions. Use this
        /// to delete a tile rather than deleting it directly in the tilemap.
        /// Doesn't need to be used in-editor. Note that there's no need
        /// to programmably add a tile to this DB. using Tilemap.SetTile()
        /// will add it automagically when the tile's StartUp method is
        /// invoked.
        /// </summary>
        /// <param name="map">tilemap to use</param>
        /// <param name="position">grid position</param>
        /// <param name="fromGroup">from a group</param>
        /// <returns>true for success</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public static bool DeleteTile(Tilemap? map,
            Vector3Int                         position,
            bool                               fromGroup = false)
        {
            //order of these is important
            var success = RemoveTileFromDb(map!, position, fromGroup);
            if (success)
                map!.SetTile(position, null);
            return success;
        }


        /// <summary>
        /// Delete a tile alternative.
        /// See DeleteTile(map, position, ....)
        /// </summary>
        /// <param name="tile">A TilePlusBase instance</param>
        /// <param name="fromGroup">from a group</param>
        /// <remarks>Will fail with error to console if tile is uninitialized.
        /// IE StartUp has not yet run or if a tilemap was deleted then the tilemap ref becomes null.</remarks>
        public static bool DeleteTile(TilePlusBase? tile, bool fromGroup = false)
        {
            if(tile == null)
            {
                TpLogError("TpLib.DeleteTile was passed a null tile.");
                return false;
            }
                
            var map = tile.ParentTilemap;
            if (map != null && tile.TileGridPosition != TilePlusBase.ImpossibleGridPosition)
                return DeleteTile(map, tile.TileGridPosition,  fromGroup);
            
            TpLogWarning($"TpLib.DeleteTile was passed an uninitialized tile. [wasMapNull?{(map == null).ToString()}]");
            return false;

        }

        /// <summary>
        /// Delete tiles from an BoundsInt region
        /// </summary>
        /// <param name="map">The map.</param>
        /// <param name="bounds">IEnumerable of any tiles</param>
        /// <remarks>Works with Unity tiles as well.</remarks>
        public static void DeleteTileBlock(Tilemap map, BoundsInt bounds)
        {
            foreach (var pos in bounds.allPositionsWithin)
            {
                if (HasTile(map, pos)) //if it's a tileplus tile use TpLib to delete
                    DeleteTile(map, pos);
                else //otherwise just delete normally
                    map.SetTile(pos, null);
            }
        }
        
        
        
        /// <summary>
        /// Delete TilePlus tiles using provided List or array 
        /// </summary>
        /// <param name="map">tilemap</param>
        /// <param name="tilesToDelete">List of tiles</param>
        /// <remarks>IList allows for both List and Array values for 'tilesToDelete'</remarks>
        public static void DeleteTileBlock(Tilemap map, IList<TilePlusBase> tilesToDelete)
        {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < tilesToDelete.Count; i++)
                DeleteTile(map, tilesToDelete[i].TileGridPosition,true);
        }



        /// <summary>
        /// Use this to replace a tile rather than deleting it directly in the tilemap.
        /// Doesn't need to be used in-editor. </summary>
        /// <param name="newTile">the tile to place.</param>
        /// <param name="map">the tilemap</param>
        /// <param name="position">the grid position</param>
        public static void ReplaceTile(TilePlusBase newTile,
                                       Tilemap      map,
                                       Vector3Int   position)
        {
            DeleteTile(map, position);
            map.SetTile(position, newTile); //note that this will add the tile into the dictionary!
        }

        /// <summary>
        /// Cut-and-paste operation:
        /// Move a tile from currentPosition to newPosition.
        /// </summary>
        /// <param name="map">The tilemap</param>
        /// <param name="currentPosition">The current position</param>
        /// <param name="newPosition">The new position</param>
        /// <param name="positionValidator">Func that accepts a Vector3Int position and returns a bool.
        /// Can be used to test if the move position is OK.</param>
        /// <param name="overwrite">Overwrite new positions if true, if false, will not overwrite.</param>
        /// <returns>true for successful move, false if no tile or optional positionValidator returns false
        /// at current position OR if there IS a tile at the newPos</returns>
        /// <remarks>Cut/Paste from one tilemap to another isn't supported.</remarks>
        public static bool CutAndPasteTile(Tilemap map,
            Vector3Int                             currentPosition,
            Vector3Int                             newPosition,
            Func<Vector3Int, bool>?                positionValidator = null,
            bool                                   overwrite = false
            )
        {
            if (positionValidator != null && !positionValidator(newPosition))
                return false;
            if (!overwrite && map.HasTile(newPosition))
                return false;
            var tile = GetTile(map, currentPosition);
            if (tile == null)
                return false;

            RemoveTileFromDb(map, currentPosition);
            //the order of these next two should not be changed.
            map.SetTile(newPosition,     tile); 
            map.SetTile(currentPosition, null);

            return true;
        }


        /// <summary>
        /// Copy and paste a tile.
        /// Makes a copy of the passed-in TPT tile instance.
        /// </summary>
        /// <param name="destinationMap">tilemap to paste the tile</param>
        /// <param name="instance">tile to copy and paste</param>
        /// <param name="newPosition">the position. Note if there's a tile
        ///     there then nothing happens and the return value is false.</param>
        /// <param name="positionValidator">Func that accepts a Vector3Int position and returns a bool.
        ///     Can be used to test if the move position is OK.</param>
        /// <returns>false for error (instantiation error, positionValidator returns false,
        /// destination position occupied</returns>
        public static bool CopyAndPasteTile(
            Tilemap?                destinationMap,
            TilePlusBase?           instance,
            Vector3Int              newPosition,
            Func<Vector3Int, bool>? positionValidator = null)
        {
            if (instance == null || destinationMap == null)
            {
                TpLogError("Null destination map or tile instance passed to TpLib.CopyAndPasteTile");
                return false;
            }
            
            #if UNITY_EDITOR
            //note that when copy/paste is used in-editor (e.g. the pick then paint situation)
            //then instance.TileGridPosition == newPosition. Not an error of any kind. The
            //brush is painting a copy of a cloned tile and RegisterTilePlus recognizes this
            //and calls CopyAndPaste tile. 'It's a feature, not a bbug'
            if (Informational)
                TpLog($"Copy/Paste: map {destinationMap}, tile {instance.TileName}, pos old:{instance.TileGridPosition} new:{newPosition}");
            #endif

            //is the destination occupied?
            if (destinationMap.HasTile(newPosition))
                return false;
            
            //also run optional position validator.
            if (positionValidator != null && !positionValidator(newPosition))
                return false;
            
            //make a new copy
            var reClone = UnityEngine.Object.Instantiate(instance); //clone the copied tile; we need a new instance.
            if (reClone == null)
                return false;
            reClone.ResetState(TileResetOperation.MakeCopy); //reset state variables like grid position etc.
            //note that ResetState nulls the GUID so we have a brief chance to change it before placing the tile.
            reClone.TileGuidBytes = Guid.NewGuid().ToByteArray();
            destinationMap.SetTile(newPosition, reClone);
            return true;
        }


        /// <summary>
        /// Delete all entries for specified Tilemap.
        /// Useful if you delete tilemaps at runtime.
        /// Note that Adding a tilemap programmatically and then
        /// adding TilePlusBase-derived tiles programatically will
        /// automatically update the DB when the tiles' StartUp method
        /// is invoked. So you do not have to add the tiles with RegisterTile
        /// or AddTileToDb.
        /// </summary>
        /// <param name="tilemap">The tilemap.</param>
        /// <returns>true for success, false if tilemap not found</returns>
        /// <remarks>IMPORTANT: DOES NOT DELETE A TILEMAP, USE BEFORE YOU DELETE THE TILEMAP.</remarks>
        // ReSharper disable once MemberCanBePrivate.Global
        public static bool RemoveMap(Tilemap tilemap)
        {
            var id = tilemap.GetInstanceID();
            return TpLib.IsTilemapRegistered(id) && RemoveMap(id);
        }

        /// <summary>
        /// Remove a single map and all tagged/typed entries. DOES NOT remove tiles, DOES NOT delete the tilemap itself
        /// See also: <see cref="RemoveMap(Tilemap)"/> 
        /// </summary>
        /// <param name="mapInstanceId">the instance ID of the map to remove</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public static bool RemoveMap(int mapInstanceId)
        {
            if (!s_TilemapsDict.TryGetValue(mapInstanceId, out var mapDict))
            {
                #if UNITY_EDITOR
                if(Warnings)
                    TpLogWarning($"Map with iid {mapInstanceId} not found. Nothing was done");
                #endif
                
                return false;
            }

            s_LockedTilemaps.Remove(mapInstanceId);
            var tiles   = mapDict.Values.ToList();
            var success = true;
            if (tiles.Count != 0) //this really should not happen...
            {
                foreach (var tile in tiles)
                {
                    if(!RemoveTileFromDb(mapInstanceId, null, tile.TileGridPosition))
                        success = false;
                }
            }
            #if UNITY_EDITOR
            else if(Warnings)
                TpLogWarning($"Map with iid {mapInstanceId} had no TPT tiles!");
            #endif

            s_TilemapsDict.Remove(mapInstanceId);
            //note that after the last DeleteTile in the loop above, the mapDict will be empty and the
            //dict is returned to the pool in RemoveTileFromDb() so exec-ing the line below will cause an exception.
            //Leaving this in, but set up so that release happens only if DeleteTile fails.
            if(!success)
                s_DictOfV3IToTpb_Pool.Release(mapDict);
            
            return true;

        }

        
        
        /// <summary>
        /// Get the TilePlusBase instance for a particular Tilemap and position
        /// </summary>
        /// <param name="map">The Tilemap</param>
        /// <param name="position">the position</param>
        /// <returns>TilePlusBase instance or null for invalid map or position</returns>
        public static TilePlusBase? GetTilePlusBaseForMapAndPosition(Tilemap map, Vector3Int position)
        {
            if(map == null)
                return null;
            return !s_TilemapsDict.TryGetValue(map.GetInstanceID(), out var dict)
                       ? null
                       : dict.GetValueOrDefault(position);
        }

        /// <summary>
        /// Get a list of all of the TilePlusBase instances for a particular Tilemap
        /// </summary>
        /// <param name="map">The Tilemap</param>
        /// <returns>a List of TilePlusBase instances for the map or null for invalid map</returns>
        public static List<TilePlusBase>? GetAllTilePlusBaseForMap(Tilemap map)
        {
            if(map == null)
                return null;
            return !s_TilemapsDict.TryGetValue(map.GetInstanceID(), out var dict) ? null : dict.Values.ToList();
        }


        /// <summary>
        /// Place all tiles of Type T (not interfaces!) in all tilemaps into the output list. 
        /// </summary>
        /// <param name="output">results list. Is cleared. If list is null it's an error</param>
        /// <param name="filter">Optional delegate for filtering, Func with an instance of Type T and a TilePlusBase - returns bool</param>
        /// <typeparam name="T">Type is TilePlusBase or subclasses. NOT interfaces!</typeparam>
        /// <remarks>Will only find the exact type specified by the TypeParam, not super or subclasses.</remarks>
        public static void GetAllTiles<T>(ref List<T> output,
            Func<T, TilePlusBase, bool>?              filter = null) where T : TilePlusBase
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (output == null)
            {
                TpLogError("Null output list passed to GetAllTilesOfType");
                return;
            }

            output.Clear();
            if (!s_TileTypes.TryGetValue(typeof(T), out var tiles))
                return;
            var numTiles = tiles.Count;
            if (filter == null)
            {
                for (var inner = 0; inner < numTiles; inner++)
                {
                    var t = tiles[inner] as T;
                    if(t != null)
                        output.Add(t);
                }

                return;
            }

            for (var inner = 0; inner < numTiles; inner++)
            {
                var tile = tiles[inner];
                var t = tile as T;
                if (t != null && filter.Invoke(t, tile))
                    output.Add(t);
            }
        }

        /// <summary>
        /// Get all tiles of a particular type from a specified tilemap. Optional filter Func delegate
        /// for pre-filtering of results.
        /// </summary>
        /// <param name="map">a tilemap reference. If map==null, then all tiles of Type are returned regardless of map.</param>
        /// <param name="matchThis">A type, usually from Typeof() but could be GetType().
        /// NOTE: if this is null then ALL tile instances are returned</param>
        /// <param name="output">A List for the output. Is Cleared. Error if null.</param>
        /// <param name="filter">Optional delegate for filtering</param>
        /// <remarks>faster without map specification or filter delegate.
        /// If nothing is found then the list will be empty</remarks>
        public static void GetAllTilesOfType(Tilemap?                  map,
                                             Type?                     matchThis,
                                             ref List<TilePlusBase>    output,
                                             Func<TilePlusBase, bool>? filter = null)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (output == null)
            {
                TpLogError("Null output list passed to GetAllTilesOfType");
                return;
            }
            output.Clear();

            
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (matchThis != null)
            {
                //look in the list of Types for a matching type list. 
                if (!s_TileTypes.TryGetValue(matchThis, out var tiles))
                    return;
                var numTiles = tiles.Count;
                if (map == null)
                {
                    if (filter == null)
                    {
                        for (var inner = 0; inner < numTiles; inner++)
                            output.Add(tiles[inner]);
                    }
                    else
                    {
                        for (var inner = 0; inner < numTiles; inner++)
                        {
                            if(filter(tiles[inner]))
                                output.Add(tiles[inner]);
                        }
                    }
                }
                else
                {
                    var mapId = map.GetInstanceID();
                    for (var inner = 0; inner < numTiles; inner++)
                    {
                        var tpb = tiles[inner];
                        if (tpb.ParentTilemapInstanceId == mapId && (filter?.Invoke(tpb) ?? true) )
                            output.Add(tpb);
                    }
                }

                return;
            }

            //otherwise return ALL tiles in the TileTypes db optionally filtered by map
            var lists    = s_TileTypes.Values.ToArray();
            var numLists = lists.Length;
            if (map == null)
            {
                for (var outer = 0; outer < numLists; outer++)
                {
                    var list     = lists[outer];
                    var numTiles = list.Count;
                    for (var inner = 0; inner < numTiles; inner++)
                    {
                        if(filter?.Invoke(list[inner]) ?? true)
                            output.Add(list[inner]);
                    }
                }

                return;
            }

            var mapId2 = map.GetInstanceID();

            for (var outer = 0; outer < numLists; outer++)
            {
                var list     = lists[outer];
                var numTiles = list.Count;
                for (var inner = 0; inner < numTiles; inner++)
                {
                    var tpb = list[inner];
                    if (tpb.ParentTilemapInstanceId == mapId2 && (filter?.Invoke(tpb) ?? true))
                        output.Add(tpb);
                }
            }
        }

        /// <summary>
        /// Are there any tiles with the specified interface? Most
        /// useful with the filter. For example, if the filter tests
        /// TilePlusBase.TileGridPosition one can filter for 'is there
        /// any tile at this position with this interface'
        /// </summary>
        /// <param name="filter">optional filter</param>
        /// <typeparam name="T">Interface type. NOT tile type</typeparam>
        /// <returns>0 for none found, 1..N count of tiles found. Note dependence on return value of filter delegate</returns>
        /// <remarks>The filter accepts a TilePlusBase instance and returns a value from the
        /// NumTileWithInterfaceFilterResult enum. Use PassAndQuit if the method should exit after the filter test.
        /// This can be used to avoid counting the exact number of matching tiles if all you care about
        /// is that at least one was found.</remarks>
        public static int NumTilesWithInterface<T>(Func<TilePlusBase, NumTileWithInterfaceFilterResult>? filter = null)
        {
            using (S_TilePlusBaseList_Pool.Get(out var tiles))
            {
                if (!s_TileInterfaces.TryGetValue(typeof(T), out tiles))
                    return 0;

                var num = tiles.Count;
                if (filter == null)
                    return num;
                var count = 0;
                for (var i = 0; i < num; i++)
                {
                    var tile = tiles[i];
                    var result = filter.Invoke(tile);
                    {
                        if (result == NumTileWithInterfaceFilterResult.PassAndQuit)
                            return ++count;
                        else if (result == NumTileWithInterfaceFilterResult.Pass)
                            count++;
                    }
                }

                return count;
            }
        }
        
        
        
        
        /// <summary>
        /// Get all tiles from a particular tilemap, optionally filtered
        /// for a tile class type, but always filtered for a particular
        /// interface being implemented. Optional pre-filtering of results.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="output">a ref to a List&lt;TilePlusBase&gt;. Is Cleared. if null is error. </param>
        /// <param name="map">a Tilemap reference. If null, then use all tilemaps</param>
        /// <param name="matchThisTileType">Optional prefilter for a particular tile Type.
        /// If null, all tiles are examined in one map (map!=null) or all maps (map==null).</param>
        /// <param name="filter">Optional results filter.</param>
        /// <remarks>The T is the interface.  The list 'output' will be empty if nothing is found.</remarks>
        public static void GetAllTilesWithInterface<T>(ref List<TilePlusBase> output,
            Tilemap?                                                          map               = null,
            Type?                                                             matchThisTileType = null,
            Func<TilePlusBase, bool>?                                         filter            = null) 
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (output == null)
            {
                TpLogError("Null output list passed to GetAllTilesWithInterface");
                return;
            }
            output.Clear();

            if (map == null && matchThisTileType == null) 
            {
                if (filter == null)
                {
                    s_TileInterfaces.TryGetValue(typeof(T), out output);
                        return;
                }

                using (S_TilePlusBaseList_Pool.Get(out var tiles))
                {
                    if (!s_TileInterfaces.TryGetValue(typeof(T), out tiles))
                        return;
                    var num = tiles.Count;
                    for (var i = 0; i < num; i++)
                    {
                        var tile = tiles[i];
                        if (filter.Invoke(tile))
                            output.Add(tile);
                    }
                    return;
                }
            }

            using (S_TilePlusBaseList_Pool.Get(out var tiles))
            {
                GetAllTilesOfType(map, matchThisTileType, ref tiles, filter);
                var num = tiles.Count;
                if (num == 0)
                    return;
                for (var i = 0; i < num; i++)
                {
                    if(tiles[i]==null)
                        continue;
                    // ReSharper disable once Unity.NoNullPatternMatching
                    if(tiles[i] is T)
                        output.Add(tiles[i]);
                }
            }
        }



        /// <summary>
        /// Get all tiles with an interface of type T from all Maps. Optional prefilter delegate
        /// </summary>
        /// <remarks>This is the best choice for most uses. The filter delegate is like
        /// (T,tpb)=> bool func --  using the param available by the interface type T or
        /// the TilePlusBase tpb.</remarks>
        /// <param name="output">List of type T. Is Cleared or, if null is error. </param>
        /// <typeparam name="T">Type of the interface desired</typeparam>
        /// <param name="filter">Optional results filter.</param>
        public static void GetAllTilesWithInterface<T>(ref List<T> output, Func<T, TilePlusBase, bool>? filter = null)
        {
            
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (output == null)
            {
                TpLogError("Null output list passed to GetAllTilesWithInterface");
                return;
            }

            output.Clear();

            if (!s_TileInterfaces.TryGetValue(typeof(T), out var iList))
                return;

            var num = iList.Count;
            if (num == 0)
                return;

            if (filter == null)
            {
                for (var i = 0; i < num; i++)
                {
                    if(iList[i]==null)
                        continue;
                    // ReSharper disable once Unity.NoNullPatternMatching
                    if (iList[i] is T t)
                        output.Add(t);
                }

                return;
            }

            for (var i = 0; i < num; i++)
            {
                var tile = iList[i];
                if(!tile)
                    continue;
                // ReSharper disable once Unity.NoNullPatternMatching
                if (tile is T t && filter.Invoke(t, tile))
                    output.Add(t);
            }
        }

        /// <summary>
        /// Populate a HashSet&lt;string&gt; with all tags used by a tile Type. 
        /// </summary>
        /// <param name="tileType">the Type of tile</param>
        /// <param name="tags">ref HashSet&lt;string&gt; Cleared before use.</param>
        /// <remarks>Tags are modified with Trim() to remove leading and trailing spaces.</remarks>
        public static void GetAllTagsUsedByTileType(Type tileType, ref HashSet<string>? tags)
        {
            if (tags == null)
                return;
            tags.Clear();

            if (!s_TileTypes.TryGetValue(tileType, out var tiles))
                return;

            var nTiles = tiles.Count;
            for (var i = 0; i < nTiles; i++)
            {
                (var count, var tileTags) = tiles[i].TrimmedTags;
                
                for (var j = 0; j < count; j++)
                {
                    var tag = tileTags[j]; 
                    if(tag != string.Empty)
                        tags.Add(tag);
                }
            }
        }

        /// <summary>
        /// Get all tiles with a particular Tag. Optional prefilter
        /// </summary>
        /// <param name="map">a Tilemap reference, if null, search all Tilemaps w TilePlus tiles.</param>
        /// <param name="tag">the tag to search for</param>
        /// <param name="output">a ref to a List&lt;TilePlusBase&gt; - if list is null is an error otherwise list is cleared</param>
        /// <param name="filter">Optional prefilter</param>
        /// <remarks>list is empty if no tiles found.</remarks>
        public static void GetTilesWithTag(Tilemap? map,
            string                                  tag,
            ref List<TilePlusBase>                  output,
            Func<TilePlusBase, bool>?               filter = null)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (output == null)
            {
                TpLogError("Null output list passed to GetTilesWithTag");
                return;
            }

            output.Clear();

            tag = tag.Trim(); //1.1.0 added this to ensure tag supplied by caller doesn't have leading/trailing spaces

            //look in the list of tags for a matching tag
            if (!s_TaggedTiles.TryGetValue(tag, out var taggedRefs))
                return;
            var num = taggedRefs.Count;
            if (map == null)
            {
                for (var i = 0; i < num; i++)
                {
                    if (filter?.Invoke(taggedRefs[i]) ?? true)
                        output.Add(taggedRefs[i]);
                }

                return;
            }

            var mapId = map.GetInstanceID();
            for (var i = 0; i < num; i++)
            {
                if (taggedRefs[i].ParentTilemapInstanceId == mapId &&
                    (filter?.Invoke(taggedRefs[i]) ?? true))
                    output.Add(taggedRefs[i]);
            }
        }

        /// <summary>
        /// Get the first tile with a particular tag
        /// </summary>
        /// <param name="map">map to check, can be null (note is faster if map is null)</param>
        /// <param name="tag">tag to look for</param>
        /// <returns>TilePlusBase.</returns>
        public static TilePlusBase GetFirstTileWithTag(Tilemap? map, string tag)
        {
            using(S_TilePlusBaseList_Pool.Get(out var tagged))
            {
                GetTilesWithTag(map, tag, ref tagged);
                return tagged.FirstOrDefault()!;
            }
        }

        
        /// <summary>
        /// Get all Types in the DB.
        /// </summary>
        /// <param name="output">ref list of Types</param>
        /// <param name="includeBase">include tileplusbase type in output</param>
        /// <param name="getNonTilePlusTypes">Get all types from TileBase on up.
        /// Note: much slower since all open scenes and tilemaps are scanned.</param>
        public static void GetAllTypesInDb(ref List<Type> output,
            bool includeBase = false,
            bool getNonTilePlusTypes = false)
        {
            output.Clear();
            output.AddRange(s_TileTypes.Keys);
            if (includeBase && !output.Contains(typeof(TilePlusBase)))
                output.Add(typeof(TilePlusBase));

            if (!getNonTilePlusTypes)
                return;

            using (s_TileMapList_Pool.Get(out var tilemaps))
            {
               var scenes = GetAllScenes();
                var num    = scenes.Count;
                for (var si = 0; si < num; si++)
                {
                    var scene = scenes[si];
                    GetTilemapsInScene(scene, ref tilemaps);
                    var nMaps = tilemaps.Count;
                    for (var mi = 0; mi < nMaps; mi++)
                    {
                        var map       = tilemaps[mi];
                        var numTiles  = map.GetUsedTilesCount();
                        var usedTiles = new TileBase[numTiles];
                        var found     = map.GetUsedTilesNonAlloc(usedTiles);
                        for (var ti = 0; ti < found; ti++)
                        {
                            var t = usedTiles[ti];
                            if ( t as TilePlusBase) //we already have these
                                continue;
                            var typ = t.GetType();
                            if (!output.Contains(typ))
                                output.Add(typ);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Is a tile with a GUID already in the DB?
        /// </summary>
        /// <param name="guid">a GUID</param>
        /// <returns>true if it exists</returns>
        /// <remarks>Used primarily by TilePlusBase code to
        /// determine if a TilePlusBase instance is already registered</remarks>
        public static bool HasGuid(Guid guid)
        {
            return s_GuidToTile.ContainsKey(guid);
        }
        
        /// <summary>
        /// Get the TilePlusBase that's refenced by a GUID byte array.
        /// </summary>
        /// <param name="guid">The GUID byte array</param>
        /// /// <param name = "extendedSearch" >true (default) use the ZoneManager and/or
        /// s_OptionalGuidToTileMappings to try and find the TPT tile from the guid.
        /// if the primary lookup fails.</param>
        /// <returns>null if not found or TilePlusBase instance.</returns>
        public static TilePlusBase? GetTilePlusBaseFromGuid(byte[] guid, bool extendedSearch = true)
        {
            return guid is not {Length: 16}
                       ? null
                       : GetTilePlusBaseFromGuid(new Guid(guid), extendedSearch);
        }

        /// <summary>
        /// Get the TilePlusBase that's refenced by a GUID string.
        /// </summary>
        /// <param name="guid">Guid string</param>
        /// <param name = "extendedSearch" >true (default) use the ZoneManager and/or
        /// s_OptionalGuidToTileMappings to try and find the TPT tile from the guid.
        /// if the primary lookup fails.</param>
        /// <returns>null if not found or a TilePlusBase instance</returns>
        public static TilePlusBase? GetTilePlusBaseFromGuid(string guid, bool extendedSearch = true)
        {
            return !Guid.TryParse(guid, out var newGuid)
                       ? null
                       : GetTilePlusBaseFromGuid(newGuid, extendedSearch);
        }

        /// <summary>
        /// Get the TilePlusBase that's refenced by a GUID struct.
        /// </summary>
        /// <param name="guid">Guid struct</param>
        /// <param name = "extendedSearch" >true (default) use the ZoneManager and/or
        /// s_OptionalGuidToTileMappings to try and find the TPT tile from the guid.
        /// if the primary lookup fails.</param>
        /// <returns>null if not found or a TilePlusBase instance</returns>

        // ReSharper disable once MemberCanBePrivate.Global
        public static TilePlusBase? GetTilePlusBaseFromGuid(Guid guid, bool extendedSearch = true)
        {
            //try primary lookup
            if(s_GuidToTile.TryGetValue(guid, out var tpb))
                return tpb;
            
            //if that failed, is extended search enabled?
            if (!extendedSearch)
                return null; //exit here if the remapping failed
            
            //is the ZoneManager running? If so, try this first.
            if (TileFabLib.AnyActiveZoneManagers)// TpZoneManager.TpZoneManagerExists)
            {
                if (TileFabLib.RemapTileGuid(guid, out var realGuid))
                {
                    //yes, a remapped GUID was found. Try that one.
                    //found it
                    return s_GuidToTile.GetValueOrDefault(realGuid); //no such luck
                    //note we intentionally do not drop down into checking the optional mapping since the 
                    //Zone manager's GUID remapping actually worked but there really was no such Tile.
                    //this is an actual error. 
                }
            }
                
            //Is there a optional Guid to Tile map? Note this doesn't remap GUIDs but
            //rather maps GUIDs to TilePlusBase instances.
            if (OptionalGuidToTileMapping != null && OptionalGuidToTileMapping.TryGetValue(guid, out tpb))
                return tpb;
            return null; //exit here if the remapping failed

        }

        
       
        /// <summary>
        /// Get a HashSet of Vector3Ints representing all occupied positions on a tilemap
        /// </summary>
        /// <param name="map">The tilemap to use</param>
        /// <param name="output">ref HashSet for output. Note is cleared unless clearOutput is false</param>
        /// <param name="includeUnityTiles">report on Unity tiles as well (slower)</param>
        /// <param name="clearOutput">Clear the ref HashSet if true (default)</param>
        /// <remarks>Map.CompressBounds is used when includeUnityTiles=true</remarks>
        public static void GetAllPositionsForMap(Tilemap? map,
            ref HashSet<Vector3Int>                       output,
            bool                                          includeUnityTiles = true,
            bool                                          clearOutput       = true)
        {
            if (map == null)
                return;
            
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (output == null)
            {
                TpLogError("Null output list passed to GetAllPositionsForMap");
                return;
            }

            if(clearOutput)
                output.Clear();
            
            if (s_TilemapsDict.TryGetValue(map.GetInstanceID(), out var innerDict))
                output.UnionWith(innerDict.Keys);

            if (!includeUnityTiles)
                return;

            //make the map a little easier to deal with by compressing the bounds.
            map.CompressBounds();
            var bounds = map.cellBounds;
            //get the block of tiles within the bounds
            var block  = map.GetTilesBlock(bounds);
            var width  = bounds.size.x;
            var height = bounds.size.y;
            //loop thru the block
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var pTile = block[x + (y * width)]; //any tile here?
                    //might be nothing at that position
                    if (pTile == null)
                        continue; //no
                    //compute proper position relative to bounds
                    var pos = new Vector3Int(x + bounds.xMin, y + bounds.yMin, 0);
                    output.Add(pos); //note that hashset ignores duplicates
                }
            }
        }



        /// <summary>
        /// Is the specified map Locked? Note that this will return true
        /// if there are any locked tilemaps in the same scene.
        /// </summary>
        /// <param name="map">a tilemap</param>
        /// <returns>true if the map is locked</returns>
        public static bool IsTilemapLocked(Tilemap? map)
        {
            return map != null && s_LockedTilemaps.Contains(map.GetInstanceID());
        }

        /// <summary>
        /// Is the specified map Locked?
        /// </summary>
        /// <param name="mapId">a tilemap instance ID</param>
        /// <returns>true if the map is locked</returns>
        public static bool IsTilemapLocked(int mapId)
        {
            return s_LockedTilemaps.Contains(mapId);
        }

        /// <summary>
        /// Is the specified map 'registered'
        /// ie is it being monitored because it has TPT tile?
        /// </summary>
        /// <param name="mapId">a tilemap instance Id</param>
        /// <returns>true if the map is being monitored</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public static bool IsTilemapRegistered(int mapId)
        {
            return s_TilemapsDict.ContainsKey(mapId);
        }

        #endregion

        #region otherUtils
        
        /// <summary>
        /// Provide a Transform and this will look 'up' the
        /// heirarchy to find the parent Grid
        /// </summary>
        /// <param name="current">the starting point</param>
        /// <returns>a Grid or null if not found</returns>
        public static Grid? GetParentGrid(Transform current)
        {
            //perhaps the current transform has a Grid
            if (current.TryGetComponent<Grid>(out var output))
                return output;
            //otherwise, look at its parent. If == null then
            //current is a root transform.
            while ((current = current.parent) != null) 
            {
                // ReSharper disable once RedundantTypeArgumentsOfMethod
                if (current.TryGetComponent<Grid>(out output))
                    return output;
            }
            return output;
        }


        /// <summary>
        /// Reallocate memory sizes. 
        /// </summary>
        /// <param name="m">an instance of TpLibMemAlloc</param>
        /// <param name="rescan">call TpLib.SceneScan after reallocation. This is generally
        /// required since reallocation wipes out all of the stored TilePlus tile data.</param>
        /// <remarks>This should be done immediately/soon after startup. Intended for
        /// runtime use only. Use in editor mode isn't useful since each
        /// scripting reload would wipe this out. Also, statically-declared
        /// lists and dictionaries won't be resized below their current size.
        /// Also, CleanState is used to release pooled items. Note that doing so
        /// resets MaxNumClonesPerUpdate and MaxNumDeferredCallbacksPerUpdate to their initial values.
        /// </remarks>
        public static void Resize(TpLibMemAlloc m, bool rescan = true)
        {
            CleanState();  //releases all items in the dictionaries.
            MemAllocSettings = m;
            s_TilemapsDict = new Dictionary<int, Dictionary<Vector3Int, TilePlusBase>>(m.m_TilemapAndTagDictsSize);
            s_TaggedTiles = new Dictionary<string, List<TilePlusBase>>(m.m_TilemapAndTagDictsSize);
            s_TileTypes = new Dictionary<Type, List<TilePlusBase>>(m.m_TypesSize);
            s_TileInterfaces = new Dictionary<Type, List<TilePlusBase>>(m.m_TypesSize);
            s_GuidToTile = new Dictionary<Guid, TilePlusBase>(m.m_GuidDictSize);
            s_DictPoolItemSize = m.m_PoolNewItemSizeForDicts;
            s_ListPoolItemSize = m.m_PoolNewItemSizeForLists;
            
            if(rescan)
                SceneScan(null,false);
            
        }
        
        
       

        /// <summary>
        /// Execute a simple callback after a delay in milliseconds  
        /// </summary>
        /// <param name = "parent" >Parent UnityEngine.Object. If non-null, checks for null before callback invoked.</param>
        /// <param name="callback">Callback </param>
        /// <param name="delayInMsec">delay before callback is excuted in MILLISECONDS. If LTEQ 10 then the callback is placed in the Update queue</param>
        /// <param name = "info" >Info string for logs</param>
        /// <param name = "silent" >if true, no messages except errors</param>
        /// <param name = "forceTaskDelay" >Force using Task.Delay even if delay is LTEQ 10. More accurate timing but more GC pressure.</param>
        /// <remarks>If delayInMsec is omitted, the default is 1 msec which causes the request to be added to the deferred callback queue:
        /// basically means the next pass thru the playerloop (Play mode) or the next EditorApplication.update invocation. This is VERY useful
        /// for trivial delays like when you want to call some method from an within a tile's initialization (see TilePlusBase)
        /// or from within OnGUI or the like eg from IMGUI code: situations where many sorts of ordinary things you'd like
        /// to do will cause beaucoup errors or crash the Editor.
        /// 
        /// NOTE: this is not intended to be used in a loop where this method is entered very frequently.
        /// Since Task.Delay is used for delays > 10 msec, you can very easily overload your computer.
        /// 
        /// In-editor, the Async or Queue are updated in time with EditorApplication.update but
        /// when PLAYING (even in editor) it occurs right after the normal Update. Usually
        /// works out fine, but must be aware of this.</remarks>
        public static async void DelayedCallback(UnityEngine.Object? parent,
                                                 Action?             callback,
                                                 string              info,
                                                 int                 delayInMsec    = 1,
                                                 bool                silent         = false,
                                                 bool                forceTaskDelay = false)
        {
            if (callback == null)
            {
                TpLogError("Null actionOnComplete passed to DelayedCallback!");
                return;
            }

            var testForNull = parent != null;
            
            #if UNITY_EDITOR
            if (!silent)
            {
                if (Informational)
                    TpLog($"Delayed Callback Task push: {info}, delay: {delayInMsec.ToString()}");
            }
            #endif

            //requests with short delays are relegated to the Update method.
            if (!forceTaskDelay && delayInMsec <= 10)
            {
                var cbd = s_DeferredCallbackPool.Get();
                cbd.Target      = parent;
                cbd.m_Callback    = callback;
                cbd.Info        = info;
                cbd.TestForNull = testForNull;
                cbd.Silent      = silent;
                s_CallbackQueue.Enqueue(cbd);
                PreloadTimingSubsystem();
                return;
            }
                

            try
            {
                CurrentActiveDelayedCallbacks++;
                await Task.Delay(delayInMsec, Application.exitCancellationToken); //msec
            }
            #pragma warning disable CS0168
            catch (TaskCanceledException _)
                #pragma warning restore CS0168
            {
                CurrentActiveDelayedCallbacks--;
                if(Informational)
                    TpLogWarning($"Delayed Callback for [{info}] had task cancelled exception: probably when changing to/from Play mode. Generally: Ignore");
                return;
            }

            CurrentActiveDelayedCallbacks--;
            
            try
            {
                if (testForNull && parent != null)
                    callback.Invoke();
                else
                    callback.Invoke();
            }
            catch (NullReferenceException e)
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                TpLib.TpLogError($"Delayed callback for [{info}] had a null-ref exception: {e} [testfornull?{testForNull}][parent=null?{parent==null}][callback=null?{callback==null}]");
            }
        }

        
        /// <summary>
        /// Force a OnTpLibChanged event
        /// </summary>
        /// <remarks>This can be used when you're doing a bulk operation that affects a lot of tiles.
        /// That avoids many change events. Use InhibitOnTpLibChanged=true, then false, then this method.
        /// </remarks>
        /// 
        /// <param name="changeType">A value from the TpLibChangeType enum</param>
        /// <param name="isPartOfGroup">true if this is part of a group of changes</param>
        /// <param name="position">position, if appropriate</param>
        /// <param name="map">Tilemap (can be null)</param>
        public static void ForceOnTpLibChanged(TpLibChangeType changeType, bool isPartOfGroup, Vector3Int position, Tilemap? map)
        {
            OnTpLibChanged?.Invoke(changeType,isPartOfGroup,position,map);
        }
        
        #endregion
        
        #region update

        /// <summary>
        /// Update handler. Editor-Play uses a monobehaviour, Editor-Edit = EditorApplication.Update.
        /// This should never be called from user code. Use the MaxNumClonesPerUpdate property to limit
        /// how many clones per update AND how many deferred callbacks are executed per update. 
        /// </summary>
        public static void Update()
        {
            var numDeferred = s_CallbackQueue.Count;
            if (numDeferred != 0)
            {
                if (numDeferred > DeferredQueueMaxDepth)
                    DeferredQueueMaxDepth = numDeferred;
                var numCallbacksExecuted = 0;
                
                //in-editor, rate is unlimited when not in Play mode. 
                #if UNITY_EDITOR
                var limit = Application.isPlaying ? MaxNumDeferredCallbacksPerUpdate : uint.MaxValue;
                #else //when not in-editor, rate is set from this property value.
                var limit = MaxNumDeferredCallbacksPerUpdate;
                #endif
                
                if (limit == 0)
                    limit = uint.MaxValue;

                while (s_CallbackQueue.Count != 0)
                {
                    var cbd         = s_CallbackQueue.Dequeue();
                    var testForNull = cbd.TestForNull;
                    var target      = cbd.Target;
                    var cb          = cbd.m_Callback;
                    //fix 2.1 - the release on the next line resets the cbd instance so cbd.info not
                    //available in exception handling below. Extract the info string here.
                    var info        = cbd.Info;  
                    
                    s_DeferredCallbackPool.Release(cbd);
                    
                    //if the Object that created the callback is now null then skip this one.
                    if (testForNull && target == null) 
                        continue;
                    try
                    {
                        cb?.Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Exception executing callback. \nInfo: {info} Exception:{e}");
                    }

                    //exit if the max number of callbacks have already been executed.
                    if (++numCallbacksExecuted > limit)
                        break;
                }
            }
            
            
            
            
            var count = s_CloningQueue.Count; 
            if (count == 0)
                return;
            if (count > CloneQueueMaxDepth)
                CloneQueueMaxDepth = count;
            
            #if UNITY_EDITOR
            var max = Application.isPlaying ? MaxNumClonesPerUpdate : uint.MaxValue;
            #else //when not in-editor, rate is set from this property value.
                var max = MaxNumClonesPerUpdate;
            #endif
            
            var num = 0;
            
            while (s_CloningQueue.Count != 0)
            {
                var data = s_CloningQueue.Dequeue();
                var map  = data.Tilemap;
                var pos  = data.Position;
                var tile = data.Tile;
                s_CloningDataPool.Release(data);

                if (tile == null || map == null)
                {
                    if(Warnings)
                        TpLogWarning($"Map or Tile were null when cloning position {pos}");
                    continue; //note that this does not inc 'num'
                }

                var clone = tile.Cloner();
                if (clone == null)
                {
                    if(Errors)
                        TpLogError($"Error cloning TPT tile: pos: {pos}, map: {map}, tileType: {tile.GetType()}");
                }
                else
                    map.SetTile(pos, clone);
                

                if (++num > max)
                    break;

                //Debug.Log($"{map} {pos} {clone}");
            }
        }
        
        
        
        #endregion
        
        
        #region tilemapCallbacks

        
        /// <summary>
        /// Position changed callback from Tilemap
        /// </summary>
        /// <param name="map">tilemap ref</param>
        /// <param name="positions">array of positions</param>
        /// <remarks>The title of this callback is misleading.
        /// It should be called: something affected this position/these positions
        /// since this event fires for any change to the tile, for example, change color or transform.</remarks>
        private static void OntilemapPositionsChanged(Tilemap? map, NativeArray<Vector3Int> positions)
        {
            if (InhibitTilemapCallbacks || s_SceneScanActive //many refreshes are happening so ignore these events
                   || map == null    //should not happen
                   || !s_TilemapsDict.ContainsKey(map.GetInstanceID()) //speedup added in 2.02
                   #if UNITY_EDITOR
                   //||  InhibitOnTpLibChanged 
                   || map.gameObject.layer == TilePlusBase.PaletteTilemapLayer //the palette is a tilemap!
                    #endif
                    )
                return;
            
            var num = positions.Length;
            for (var i = 0; i < num; i++)
            {
                var pos  = positions[i];
                var tile = map.GetTile(pos); //try to get a tile from the map. If none then this is a removal
                if (tile == null)            //a tile has been removed
                {
                    //is there an existing TilePlus tile at this position? If it is, there'll be a dictionary entry for it.
                    var hasTpTile = HasTile(map, pos);
                    if (hasTpTile) //if there is one then delete it from the dictionary since it's been removed from the map
                        RemoveTileFromDb(map, pos, true);
                    else if(!InhibitOnTpLibChanged) //some other sort of tile is being deleted. Editors need to rescan
                        OnTpLibChanged?.Invoke(TpLibChangeType.Deleted,false,pos,map);
                }
                else //can be an addition. What sort of tile is it? Did it overwrite an existing TPT tile?
                {
                    var newTile = map.GetTile(pos);
                    if(newTile == null)
                        continue;
                    if ((newTile as ITilePlus) != null) //TPT tiles will register themselves but Unity tiles will not so...
                        continue;
                    //is there an existing TilePlus tile at this position? If it is, there'll be a dictionary entry for it.
                    var hasTpTile = HasTile(map, pos);
                    if (hasTpTile)                        //if there is one then delete it from the dictionary since it's been removed from the map
                        RemoveTileFromDb(map, pos, true); //note this fires OnTpLibChanged
                    /*
                    else 
                    {
                        //so it looks as if a tile has been added, right? Well no. It could be a Unity Tile or TileBase that has it's color, transform, or flags changed.
                        //how do we know this? Can't really do anything here: need the OnTilemapTileChanged callback to do the work
                        s_DeletedChangeArgs.m_GridPosition = pos;
                        s_DeletedChangeArgs.m_Tilemap      = map;
                        OnTpLibChanged(s_AddedChangeArgs);
                    }
                */
                }
            }
        }
        
        
        
        private static void OntilemapTileChanged(Tilemap? map, Tilemap.SyncTile[] syncTiles)
        {
            //speedup added in 2.02 
            //If callbacks inhibited OR If no delegates for this event then don't bother doing the computations herein.
            if (InhibitTilemapCallbacks ||  OnTpLibChanged == null || map == null)
                return;
            
            #if UNITY_EDITOR
            if(IsTilemapFromPalette(map))
                return;
            #endif
            
            if (   s_SceneScanActive                                   //many refreshes are happening so ignore these events
                   || !s_TilemapsDict.ContainsKey(map.GetInstanceID()) //speedup added in 2.02
                   #if UNITY_EDITOR
                   //||  InhibitOnTpLibChanged                                  
                   || map.gameObject.layer == TilePlusBase.PaletteTilemapLayer //the palette is a tilemap!
                #endif
               )
                return;

            var count = syncTiles.Length; 
            if (count == 0) 
                return;

            s_NonTppTilesAddedOrModified.Clear();

            //scan thru the synctiles and gather info about what's been added (or modified).
            for (var index = 0; index < count; index++)
            {
                var syncTile = syncTiles[index];
                var stPos = syncTile.position;
                // ReSharper disable once Unity.NoNullPatternMatching
                if (syncTile.tile != null && syncTile.tile is not TilePlusBase) //addition or change to Unity Tile or TileBase
                   s_NonTppTilesAddedOrModified.Add(stPos);
            }

            if (InhibitOnTpLibChanged || s_NonTppTilesAddedOrModified.Count == 0)
                return;
            OnTpLibChanged.Invoke(TpLibChangeType.ModifiedOrAdded,false,TilePlusBase.ImpossibleGridPosition,map);
        }
        
        #endregion
        
        #region pools

        /// <summary>
        /// Pool for lists of TilePlusBase
        /// </summary>
        internal static readonly ObjectPool<List<TilePlusBase>> S_TilePlusBaseList_Pool = new
            (() => new List<TilePlusBase>
                 (s_ListPoolItemSize),
             null,
             l => l.Clear());

        /// <summary>
        /// Pool for Dictionaries of Vector3Int => TilePlusBase
        /// </summary>
        private static readonly ObjectPool<Dictionary<Vector3Int, TilePlusBase>> s_DictOfV3IToTpb_Pool =
            new(() => new Dictionary<Vector3Int, TilePlusBase>(s_DictPoolItemSize),
                null,
                l => l.Clear());

       /// <summary>
       /// Pool for Lists of Tilemaps
       /// </summary>
        private static readonly ObjectPool<List<Tilemap>> s_TileMapList_Pool = new
           (() => new List<Tilemap>
                (PoolNewItemSize_List_Tpb), //note that this list size is set by the constant, and not s_ListPoolItemSize.
            null,
            l => l.Clear());
       
       /// <summary>
       /// Pool for CloningData
       /// </summary>
       private static readonly ObjectPool<CloningData> s_CloningDataPool = new(() => new CloningData(),
                                                                               null,
                                                                               cd => cd.Reset());
       
       
       /// <summary>
       /// Pool for DeferredCallbackData
       /// </summary>
       private static readonly ObjectPool<DeferredCallbackData> s_DeferredCallbackPool = new(() => new DeferredCallbackData(),
                                                                               null,
                                                                                cbd => cbd.Reset());

       
       
        #endregion

        #region debugOutput

        /// <summary>
        /// Simpler Log for normal messages. 
        /// </summary>
        /// <param name="logString">text to add to the log</param>
        public static void TpLog(string logString)
        {
            #if UNITY_EDITOR || TPT_DEBUG
            Debug.LogFormat(LogType.Log, LogOption.None, null , logString);
            #endif

        } 

        /// <summary>
        /// Simpler Log for warning messages.
        /// </summary>
        /// <param name="logString">text to add to the log</param>
        public static void TpLogWarning(string logString)
        {
            #if UNITY_EDITOR || TPT_DEBUG
            Debug.LogFormat(LogType.Warning, LogOption.None, null , logString);
            #endif
        }


        /// <summary>
        /// Simpler log for error messages
        /// </summary>
        /// <param name="logString">text to add to the log</param>
        public static void TpLogError(string logString)
        {
            Debug.LogFormat(LogType.Error, LogOption.None, null, logString);
        }
        
 
        #endregion


        //----------------------------------------------------------------------------------------------//
        // ***************** The remainder of this code is Editor-only *******************************  //
        //----------------------------------------------------------------------------------------------//


#if UNITY_EDITOR
        
        #region editor-properties

        
        
        /// <summary>
        /// For Stats viewing
        /// </summary>
        public static string TpbPoolStat => $"All:{S_TilePlusBaseList_Pool.CountAll.ToString()}, Active:{S_TilePlusBaseList_Pool.CountActive.ToString()}, Inactive:{S_TilePlusBaseList_Pool.CountInactive.ToString()}";
        
        /// <summary>
        /// For Stats viewing
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static string s_DictOfV3IToTpb_PoolStat => $"All:{s_DictOfV3IToTpb_Pool.CountAll.ToString()}, Active:{s_DictOfV3IToTpb_Pool.CountActive.ToString()}, Inactive:{s_DictOfV3IToTpb_Pool.CountInactive.ToString()}";

        /// <summary>
        /// For Stats viewing
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static string DeferredEvtPoolStat => $"All:{TpEvents.S_DeferredEvent_Pool.CountAll.ToString()}, Active:{ TpEvents.S_DeferredEvent_Pool.CountActive.ToString()}, Inactive:{TpEvents.S_DeferredEvent_Pool.CountInactive.ToString()}";
        /// <summary>
        /// For Stats viewing
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static string ListTilemapPoolStat => $"All:{s_TileMapList_Pool.CountAll.ToString()}, Active:{ s_TileMapList_Pool.CountActive.ToString()}, Inactive:{s_TileMapList_Pool.CountInactive.ToString()}";
        
        /// <summary>
        /// For Stats viewing
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static string CloningDataPoolStat => $"All:{s_CloningDataPool.CountAll.ToString()}, Active:{ s_CloningDataPool.CountActive.ToString()}, Inactive:{s_CloningDataPool.CountInactive.ToString()}";

        /// <summary>
        /// For Stats viewing
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static string DeferredCallbackPoolStat => $"All:{s_DeferredCallbackPool.CountAll.ToString()}, Active:{ s_DeferredCallbackPool.CountActive.ToString()}, Inactive:{s_DeferredCallbackPool.CountInactive.ToString()}";


       

        #endregion
        
        
        #region editor-code
        
        /// <summary>
        /// Resets TpLib when Play mode is entered
        /// </summary>
        [InitializeOnEnterPlayMode] 
        private static void ResetOnEnterPlayMode(EnterPlayModeOptions playModeOptions)
        {
            if((playModeOptions & EnterPlayModeOptions.DisableDomainReload) != 0)
                InitializeTpLib();
        }
        
        

        /// <summary>
        /// Update certain data structures after an instance has changed.
        /// This is intended for use in-editor only when a tile's fields
        /// have been edited.
        /// </summary>
        /// <param name="instance">the tile instance</param>
        /// <param name="fieldNames">An array of field names that have changed.</param>
        /// <returns>true if a field was changed.</returns>
        /// <remarks>Currently this only updates the Tag data and sends the list of field names to the tile instance.</remarks>
         public static bool UpdateInstance(TilePlusBase? instance, string[] fieldNames)
        {
            if (fieldNames.Length == 0)
            {

                TpLogWarning($"Tile {instance} indicated changed fields but didn't specify any?!?!");
                return false;
            }

            if (instance == null)
            {
                #if UNITY_EDITOR
                if (Errors)
                    TpLogError("Null TilePlusBase instance passed to TpLib.UpdateInstance. ");
                #endif
                return false;
            }


            var map      = instance.ParentTilemap;
            if (map == null)
            {
                #if UNITY_EDITOR
                if (Errors)
                    TpLogError("Null map from tile in TpLib.UpdateInstance.");
                #endif
                return false;
            }

            var position = instance.TileGridPosition;
            var numFields = fieldNames.Length;
            if (numFields == 0 || (numFields==1 && fieldNames[0] == string.Empty))
            {
                TpLogWarning($"Tile {instance} on map {map} @ {position} wants to update tiles but no fields specified!");
                return false;
            }

            #if UNITY_EDITOR
            if (Informational)
            {
                TpLog($"TPT tile {instance} is updating its TpLib data. Map: {map.name}  Pos: {position}");
                if (numFields == 1)
                    TpLog($"Field changed: {fieldNames[0]} ");
                else
                {
                    TpLog("Fields changed: ");
                    foreach (var s in fieldNames)
                        TpLog(s);
                }
            }
            #endif


            

            var result = false;
            if (fieldNames.Contains("m_Tag"))
            {
                /* Tags: it's a bit tricky, this: one or more tag(s) might have been deleted,
                * or one or more tag(s) might have been added, or there
                * were both additions and deletions. Since this is usually only
                * used in-editor, the best approach is to delete all the tag entries for this instance and then re-add them
                */

                DeleteFromTagDb(instance);
                AddToTagDb(instance);
                if(!InhibitOnTpLibChanged)
                    OnTpLibChanged?.Invoke(TpLibChangeType.TagsModified,false,position,map);
                if (numFields == 1) //usually only one field is modified at a time, so this saves time. Note that tiles won't be notified about tag changes!
                    return true;
                result = true;
            }

            //pass the field names to the instance in case it needs to do something.
            if (!instance.UpdateInstance(fieldNames))
                return result;
            if(!InhibitOnTpLibChanged)
                OnTpLibChanged?.Invoke(TpLibChangeType.Modified,false,position,map);
            return true;


        }

        
        /// <summary>
        /// Overload for IsTilemapFromPalette(Tilemap)
        /// </summary>
        /// <param name="itilemap">An ITilemap instance</param>
        /// <returns>true if tilemap is from the palette</returns>
        public static bool IsTilemapFromPalette(ITilemap itilemap)
        {
            var component = itilemap.GetComponent<Tilemap>();
            return component != null && IsTilemapFromPalette(component);
        }
        
        /// <summary>
        /// Is this tilemap actually the palette?
        /// </summary>
        /// <param name="tilemap">tilemap ref</param>
        /// <returns>true if is from palette</returns>
        /// <remarks>It shouldn't be this obtuse... </remarks>
        //todo add inlining attribute.
        public static bool IsTilemapFromPalette(Tilemap tilemap)
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


       
        #endregion

       
        #region systemevents
        
        /// <summary>
        /// In editor, use this to get an event when the editor update occurs.
        /// This is cleared when Selection changes, and the callback bool reflects that.
        /// </summary>
        public static event Action<bool>? TimerHookForSimulation;

        /// <summary>
        /// editorapplication.update handler.
        /// </summary>
        /// <remarks>
        /// Note that if this name is changed, do a search for the old string value
        /// since it's used in a MethodInfo test elsewhere.
        /// in this file. 
        /// </remarks>
        private static void OnEditorApplicationUpdate()
        {
            s_UpdateCount += 1;
            s_UpTime       =  EditorApplication.timeSinceStartup;
            if (s_UpTimeAtStart == 0)
            {
                s_UpTimeAtStart    = s_UpTime;
                s_LastRefreshTime = s_UpTime;
            }

            if (s_UpTime - s_LastRefreshTime > 1)
            {
                Editor_Refresh_Rate            = s_UpdateCount.ToString();
                s_UpdateCount     = 0;
                s_LastRefreshTime = s_UpTime;
            }

            Update();
            TimerHookForSimulation?.Invoke(s_SelectionHasChanged);
            s_SelectionHasChanged = false;
            
        }


        /// <summary>
        /// Editor tick update count
        /// </summary>
        #if ODIN_INSPECTOR
        // ReSharper disable once Unity.RedundantHideInInspectorAttribute
        [HideInInspector]
        #endif
        private static uint s_UpdateCount;

        /// <summary>
        /// total run time
        /// </summary>
        #if ODIN_INSPECTOR
        // ReSharper disable once Unity.RedundantHideInInspectorAttribute
        [HideInInspector]
        #endif
        private static double s_UpTime;

        /// <summary>
        /// The last refresh time
        /// </summary>
        #if ODIN_INSPECTOR
        // ReSharper disable once Unity.RedundantHideInInspectorAttribute
        [HideInInspector]
        #endif
        private static double s_LastRefreshTime;

        /// <summary>
        /// uptime at start
        /// </summary>
        #if ODIN_INSPECTOR
        // ReSharper disable once Unity.RedundantHideInInspectorAttribute
        [HideInInspector]
        #endif
        private static double s_UpTimeAtStart;

        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// Editor refresh rate, used for display
        /// </summary>
        #if ODIN_INSPECTOR
        [ShowInInspector, Sirenix.OdinInspector.ReadOnly]
        #endif
        public static string? Editor_Refresh_Rate;


        /// <summary>
        /// The selection has changed
        /// </summary>
        #if ODIN_INSPECTOR
        // ReSharper disable once Unity.RedundantHideInInspectorAttribute
        [HideInInspector]
        #endif
        private static bool s_SelectionHasChanged;

        

        /// <summary>
        /// SelectionChanged handler.
        /// </summary>
        private static void OnSelectionChanged()
        {
            s_SelectionHasChanged = true;
        }

        #endregion
        #endif
        
    }
}
