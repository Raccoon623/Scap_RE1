using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using TilePlus;
using TilePlusCommon;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;
// ReSharper disable AnnotateNotNullTypeMember
// ReSharper disable AnnotateCanBeNullParameter

namespace TilePlusDemo
{
    /// <summary>
    /// The game controller for the Top Down demo
    /// </summary>
    public class TdDemoGameController : MonoBehaviour
    {
        #region publics

        /// <summary>
        /// Reference to Player tile's prefab
        /// </summary>
        [FormerlySerializedAs("m_playerPrefab")]
        [Tooltip("A reference to the 'Baddie' prefab in the Project")]
        public GameObject m_PlayerPrefab;
        
        
        /// <summary>
        /// Interactive elements tilemap
        /// </summary>
        [Tooltip("Tilemap with interactive elements like Agents and Player")]
        public Tilemap m_InteractiveMap;

        /// <summary>
        /// Control/Effect elements tilemap
        /// </summary>
        [Tooltip("Tilemap with control elements like spawners and triggers")]
        public Tilemap m_EffectsMap;

        /// <summary>
        /// Toggle fixup of effects map bounds
        /// </summary>
        [Tooltip("Fix effects map bounds. Set 'Off' to show example from User Guide.")]
        public bool m_FixEffectsMapBounds;

        /// <summary>
        /// Tag for spawners
        /// </summary>
        [Tooltip("Tag used for spawners")]
        public string m_SpawnerTag = "spawner";

        /// <summary>
        /// tag for zone loaders
        /// </summary>
        [Tooltip("Tag used for zone loaders")]
        public string m_LoaderTag = "loader";

        /// <summary>
        /// tag for raw zones (which don't do anything in this demo, but tests the tile)
        /// </summary>
        [Tooltip("Tag used for raw zone tiles")]
        public string m_RawZoneTag = "rawZone";

        /// <summary>
        /// The UI text element for score
        /// </summary>
        [Tooltip("Reference to UI text")] 
        public Text m_Score;

        /// <summary>
        /// Ref to the pathfinder component
        /// </summary>
        [Tooltip("Pathfinder component on Grid")]
        public Pathfinder m_Pathfinder;

        /// <summary>
        /// Ref to the Simple Cam Follow component.
        /// </summary>
        [Tooltip("Simple Cam Follow component on Camera")]
        public SimpleCamFollow m_SimpleCamFollow; 
        
        /// <summary>
        /// A sound effect clip
        /// </summary>
        [Tooltip("Sound fx clip")]
        public AudioClip m_Hit_Sfx;

        /// <summary>
        /// The audio source
        /// </summary>
        [Tooltip("Audio source on Camera")]
        public AudioSource m_AudioSource;


        /// <summary>
        /// The name to use when creating a zone manager. Must be unique.
        /// </summary>
        [Tooltip("The name to use when creating a zone manager. Must be unique.")]
        public string m_ZoneManagerName = "TopDownDemo";


        /// <summary>
        /// Controls how many Clones can be made per update. See the Programmer's Guide for more information.
        /// </summary>
        [Tooltip("Controls how many clones per update. Tweak to see effect on performance.")]
        public uint m_MaxNumClonesPerUpdate = 2;

        /// <summary>
        /// Controls how many deferred-callbacks can be made per update. See the Programmer's Guide for more information.
        /// </summary>
        [Tooltip("Controls how many callbacks per update. Tweak to see effect on performance.")]
        public uint m_MaxNumCallbacksPerUpdate = 8;
        
        
        
        /// <summary>
        /// An informational field.
        /// </summary>
        [Header("Informational")]
        [NonSerialized]
        // ReSharper disable once MemberCanBePrivate.Global
        public string m_Direction;

        /// <summary>
        /// Keycode for UP
        /// </summary>
        [Header("Keycodes to use for this player")]
        public KeyCode m_Up = KeyCode.W;
        /// <summary>
        /// Keycode for LEFT
        /// </summary>
        public KeyCode m_Left  = KeyCode.A;
        /// <summary>
        /// Keycode for DOWN
        /// </summary>
        public KeyCode m_Down  = KeyCode.S;
        /// <summary>
        /// Keycode for RIGHT
        /// </summary>
        public KeyCode m_Right = KeyCode.D;

        /// <summary>
        /// Debug messages toggle
        /// </summary>
        [Tooltip("Spam console or ... not")] 
        public bool m_DebugMessages;

        /// <summary>
        /// Enable pretty print for save files. More readable but larger.
        /// </summary>
        [Tooltip("Enable human-readable save files.")]
        public bool m_PrettyPrint; 
        
        /// <summary>
        /// Delay between moves used for debouncing
        /// </summary>
        [Tooltip("Delay between moves for debouncing, min=0.2")]
        public float m_Delay = 0.2f;
        
        #endregion
        
        #region constants
        //filename for save file
        private const           string GameDataFileName           = "TopDownDemo_playerData.txt";
        private const           string RegistrationsFileName      = "TopDownDemo_RegData.txt";
        private static readonly char[] s_ParenDelim       = {'('};
        private static readonly char[] s_LeftSqBrktDelim  = {'['};
        private static readonly char[] s_RightSqBrktDelim = {']'};
        
        [Flags]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private enum GridDirection
        {
            None  = 0,
            Up    = 1,
            Right = 2,
            Down  = 4,
            Left  = 8
        }
        #endregion
        
        #region properties

        /// <summary>
        /// Access to the Player's controller.
        /// </summary>
        public TdDemoPlayerPrefabLink Player => playerController;
        
        #endregion
        
        
        #region privates

        private TdDemoPlayerPrefabLink playerController;

        private List<TilePlusBase> tpbList = new(8);

        //a hashset of positions where the agents hit something
        private readonly HashSet<Vector3Int> hitPositions = new();
        
        //set when game is over ie no lives left for the player
        private bool gameOver;
        
        //gets set when a tile event occurs
        private bool hadTileEvent;

        //the Player GameObject
        private GameObject instantiatedPrefab;

        //cached reference to Player tile's parent tilemap
        private Tilemap playerTilemap; 
        
        //used to keep Update passive until init is complete
        private bool  initialized; 
        
        //used for debouncing
        private float lastTime;
        
        //used for debouncing
        private float timeAccum;
        
        //flag to rescan pathfinder because of zone changes.
        private bool rescanOnNextUpdate;

        //list required for some TpLib method calls
        private readonly List<TilePlusBase> messagedTiles = new(16);
        
        //list required to obtain filtered events
        private List<TilePlusBase> eventTiles = new();
        
        //list of previously-loaded TileFabs, created when game starts and save files read.
        private ZoneReg[] loadResultsArray;

        private TpZoneManager zoneManager;
        
        #endregion
        
        #region UnityEvents
        //Unity event function Start used as Coroutine
        private IEnumerator Start()
        {
            Debug.Log($"Data save location: {Application.persistentDataPath}");

            //wait till TpLib static class is ready for use. 
            while (!TpLib.TpLibIsInitialized)
                yield return null;
            
            TileFabLib.EnableZoneManagers();

            //even though this demo does not use chunking, we are 
            //using the ZoneManager when Zones are reloaded from save files. 
            //The initialMaxNumChunks is important only in that it sets initial
            //size of data structures in the ZoneManager instance. But no need to
            //worry about it too much since these will expand as needed anyway.
            //Correctly sizing  this value in a real app will save memory fragmentation and GC.
            //The size should be the size of Tilefabs, and since there's only one tilefab used, we use
            //its information.
            var loader = TpLib.GetFirstTileWithTag(null, "loader") as TpAnimZoneLoader;
            if (loader == null)
            {
                Debug.LogError("Could not find the ZoneLoader in initial scene");
                yield break;
            }
            
            
            
            //if there isn't an existing ZM, create an instance of one.
            if (!TileFabLib.GetNamedInstance(m_ZoneManagerName, out zoneManager) )
            {
                //create the mapName-to-mapInstance dict.
                var grid = TpLib.GetParentGrid(m_EffectsMap.transform);
                if(grid == null)
                {
                    Debug.LogError("Could not find parent grid to scene Tilemaps!!");
                    yield break;
                }

                var maps = grid.GetComponentsInChildren<Tilemap>();
                if (maps == null)
                {
                    Debug.LogError("No Tilemaps found!!");
                    yield break;
                }
                //create dictionary
                var nameToMapDict = maps.ToDictionary(k => k.name, v => v);
                if (!TileFabLib.CreateZoneManagerInstance(out zoneManager, m_ZoneManagerName, nameToMapDict))
                {
                    Debug.LogError("Could not get a ZoneManager instance!!");
                    yield break;
                }
            }

            if(zoneManager == null)
            {
                Debug.LogError("Could not get a ZoneManager instance!!");
                yield break;
            }

            //note that chunking assumes chunks are square, and the ones used in this demo are square.
            //so the size param below is the X size of the largest bounds in the TileFab.
            
            zoneManager.Initialize(loader.m_TileFab!.LargestBounds.size.x, Vector3Int.zero, 32);
                
            
            RestoreRegisteredTileFabs();
            

            TpLib.MaxNumClonesPerUpdate            = m_MaxNumClonesPerUpdate;
            TpLib.MaxNumDeferredCallbacksPerUpdate = m_MaxNumCallbacksPerUpdate;
            TpLib.PreloadTimingSubsystem();
            
            //instantiate the Player prefab
            if (m_PlayerPrefab == null)
            {
                Debug.LogError("PlayerPrefab field on TdDemoGameController was null!");
                yield break;
                
            }

            //note: position gets set for real when save data are loaded.
            instantiatedPrefab = SpawningUtil.SpawnPrefab(m_PlayerPrefab,Vector3.zero,m_InteractiveMap.transform,string.Empty,false,true);
            if (instantiatedPrefab == null)
            {
                Debug.LogError($"Could not instantiate {m_PlayerPrefab}");
                yield break;
            }

            m_SimpleCamFollow.m_TargetToFollow = instantiatedPrefab.transform;

            playerController = instantiatedPrefab.GetComponent<TdDemoPlayerPrefabLink>();
            
            if (playerController == null)
            {
                Debug.LogError($"Could not find TdDemoPlayerPrefabLink component on {m_PlayerPrefab} GameObject");
                yield break;
            }
            
            
            //increase the chunk culling bounds. Explained in useR guide's FAQ section.
            if (m_FixEffectsMapBounds && m_EffectsMap != null)
            {
                
                var tmRenderer = m_EffectsMap.GetComponent<TilemapRenderer>();
                if (tmRenderer != null)
                {
                    tmRenderer.detectChunkCullingBounds = TilemapRenderer.DetectChunkCullingBounds.Manual;
                    var b = tmRenderer.chunkCullingBounds;
                    b.x                           += 10;
                    tmRenderer.chunkCullingBounds =  b;
                }
            }

            
            
            //get the bounds for the interactive map. That's the one where the playable character is.
            m_InteractiveMap.CompressBounds();

            var didRestore = RestoreData();
            if(!didRestore)
                Debug.Log("Could not load save file: starting fresh!");
            
            //once data is restored, look at the waypoints to see which is enabled. This is our starting
            //position. If none enabled then go to the start waypoint. If can't find that, place player at Vector3Int.Zero.
            TopDownWaypointTile enabledWp = null;
            
            //try to find an enabled waypoint. Note that since all TileFabs have been restored, the Waypoint
            //tiles loaded in from TileFabs will have had their data restored correctly. Therefore if one  of the 
            //TileFabs' Waypoints was the last enabled one it'll be located correctly.

            //Create a list for waypoint tiles. Since we only need this once, don't bother with pooling.
            var waypoints = new List<TopDownWaypointTile>(2);
            //this next line fills the waypoints list with any enabled waypoints (should only be 1)
            TpLib.GetAllTiles(ref waypoints, (waypointInstance, _) => waypointInstance.IsEnabled);
            if(waypoints.Count == 0) //did not find one, try to find a start waypoint
                TpLib.GetAllTiles(ref waypoints, (waypointInstance, _) => waypointInstance.m_IsStartWaypoint);

            //found an enabled OR start waypoint
            if (waypoints.Count != 0)
            {
                enabledWp              = waypoints[0];
                //if we found a save file and it restored correctly, the enabled WP has the # of lives. 
                //Otherwise this is a new game and we set the lives to 9.
                playerController.Lives = didRestore ? enabledWp.Lives : 9;
            }
            else
                playerController.Lives = 9;  //this shouldn't happen since the starting area has several waypoints.

            var startingPosition = enabledWp != null
                                       ? enabledWp.TileGridPosition
                                       : Vector3Int.zero;

            //get the world position for the Starting Position (which currently is in Grid coordinates rather than World)
            var posForPrefab = m_InteractiveMap.GetCellCenterWorld(startingPosition);
            //now move the player and camera there.
            instantiatedPrefab.transform.position = posForPrefab;
            posForPrefab.z                        = -10; //adjust z for proper cam offset from tilemap
            m_SimpleCamFollow.transform.position  = posForPrefab;
            
            //rotate any existing agents to point towards the Player
            InitialAgentsRotate(m_InteractiveMap);
            //init maps of obstacles both static and moveable
            m_Pathfinder.ScanMaps();
            
            //show initial score
            ShowScore();
            //subscribe to the TpLib event about events sent from tiles to listeners
            TpEvents.OnTileEvent += TpLibOnTileEvent;
            initialized       =  true;
        }
        
        
         //event loop
        private void Update()
        {
            //do nothing for certain situations
            if ( m_InteractiveMap == null || m_EffectsMap == null || gameOver ||!initialized)
                return;
            
            //if rescan flag was set, do a rescan of the pathfinder and loading zones
            if (rescanOnNextUpdate)
            {
                rescanOnNextUpdate = false;
                m_Pathfinder.Scan();
            }
            
            // +debounce
            timeAccum += Time.deltaTime;
            
            if (!Input.anyKey )
                return;

            if (m_Delay < 0.2f)
                m_Delay = 0.2f;
            if(timeAccum < (lastTime + m_Delay))
                return;
            lastTime = timeAccum;
            
            // -debounce
            
            
            if (playerController == null)
                return;
            
            //if prefab still moving, return
            if (playerController.PrefabMoving)
                return;
            
            //determine direction from the keys. Note that diagonals work for two keys pressed when sensible.
            //i.e., up and down at the same time don't do anything but up and right move diagonally up/right.
            var d = 0;
            //note correspondence to GridDirection enum
            d |= Input.GetKey(m_Up) ? 1 : 0;
            d |= Input.GetKey(m_Right) ? 2 : 0;
            d |= Input.GetKey(m_Down) ? 4 : 0;
            d |= Input.GetKey(m_Left) ? 8 : 0;

            m_Direction = ((GridDirection) d).ToString(); //this is just for troubleshooting, it's a string in the inspector.
            if (d is <= 0 or > 15)                        //range check
                return;
            
            //get the new direction
            var newDirection = s_DirectionVectors[d]; 
            if (newDirection == Vector3Int.zero) //no move possible depends on keys pressed.
                return;

            //current and new positions
            var currentPlayerPos     = instantiatedPrefab.transform.position;
            var currentPlayerGridPos = m_InteractiveMap.WorldToCell(currentPlayerPos);
            var newGridPos               = currentPlayerGridPos + newDirection;
            var newPlayerWorldPos           = m_InteractiveMap.GetCellCenterWorld(newGridPos);
            //rotation angle for Player's prefab
            var angle      = s_DirectionAngles[d];
            
            //rotate the prefab to face in the direction of motion
            
            instantiatedPrefab.transform.rotation = Quaternion.identity;
            if (angle != 0f)
                instantiatedPrefab.transform.Rotate(Vector3.forward, angle);
            
            //can we walk there?
            if(!m_Pathfinder.IsWalkablePosition(newGridPos))
            {
                if(m_DebugMessages)
                    Debug.Log($"Can't walk from {currentPlayerPos} to {newGridPos}");
                return;
            }
            
            playerController.Move(newPlayerWorldPos);
            
            //update agent tiles with our new position and check for hits.
            UpdateAgents();     
            //create a message packet to send to certain tiles.
            var pObj = new PositionPacketIn(newGridPos);    
            
            //update spawners about our new position
            //Send a message to all tiles which accept a positionpacket, return SpawningResults, on the EffectMap with
            //the m_Spawner tag.
            TpMessaging.SendMessage<SpawningResults,PositionPacketIn>(m_EffectsMap, m_SpawnerTag, pObj,  messagedTiles);
            if (m_DebugMessages) //print out the spawning results.
            {
                foreach (var tile in messagedTiles)
                {
                    if (tile is not ITpMessaging<SpawningResults, PositionPacketIn> t)
                        continue;
                    var results = t.GetData();
                    if(results == null)
                        continue;
                    if (results.m_LastPaintedTile == null && results.m_LastSpawnedPrefab == null)
                        continue;
                    var spawned = results.m_LastSpawnedPrefab != null ? results.m_LastSpawnedPrefab.name: "None";
                    var painted = results.m_LastPaintedTile   != null ? results.m_LastPaintedTile.name : "None";
                    Debug.Log($"Spawner tile at position {tile.TileGridPosition} spawned: {spawned}, painted {painted}");
                }
            }
            
            /*Update tiles on the effects layer:
            update loaders about our new position, output placed in messagedTiles isn't used in this case since
            there's no return (hence, EmptyPacket) however the tile sends a TpLib trigger event when the send
            position is in the zone of a loader.            
            */
            //Send a message to all tiles accepting a positionZmpacket, not returning anything (emptypacket) and having the tag = whataver m_LoaderTag is.
            TpMessaging.SendMessage<EmptyPacket, PositionZmPacketIn>(m_EffectsMap, m_LoaderTag, new PositionZmPacketIn(newGridPos, zoneManager), messagedTiles);
            
            //now, update waypoints about new position
            //Send a message to all tiles accepting a position packet, not returning anything, and are of Type TopDownWaypointTile.
            TpMessaging.SendMessage<EmptyPacket, PositionPacketIn>(m_EffectsMap, typeof(TopDownWaypointTile), pObj, messagedTiles);
            
            //update raw zones (for testing only). Debug messages need to be on to see a response
            //Send a message to all tiles accepting a position packet, returning a position packet, and using the tag = whatver m_RawZoneTag is. 
            TpMessaging.SendMessage<PositionPacketOut,PositionPacketIn>(m_EffectsMap, m_RawZoneTag, pObj, messagedTiles);
            if (m_DebugMessages) //only show this if DebugMessages is true 
            {
                //here, messagedTiles is a list cleared, then filled in during the LAST call to SendMessage. It 
                //contains all of the tiles actually messaged, ie, all the 'RawZoneTag' tiles.
                foreach (var tile in messagedTiles)
                {
                    if (tile is not ITpMessaging<PositionPacketOut, PositionPacketIn> t) //you could just cast, too. This is a bit redundant.
                        continue;
                    var results = t.GetData(); //get the data from each tile
                    if(results == null)
                        continue;
                    if(results.m_Position != TilePlusBase.ImpossibleGridPosition) //if it's a real position, it is a contact position.
                        Debug.Log($"Zone tile at position {tile.TileGridPosition} reported contact at {results.m_Position}");
                }
            }

            if(hadTileEvent)
                ProcessTileEvents();
            
           
            //position of player
            if (hitPositions.Count == 0)
                return;

            //if there was a hit to the player
            if (hitPositions.Contains(currentPlayerGridPos))
            {
                m_AudioSource.clip = m_Hit_Sfx;
                m_AudioSource.Play();
                if (--playerController.Lives <= 0)
                {
                    playerController.GameOver();
                    m_Score.text = "GAME OVER!!";
                    gameOver     = true;
                }
                else
                    ShowScore();
            }

            hitPositions.Clear();
            m_Pathfinder.ScanMobileObstacles(); //since there may be deletions and additions, do update

        }
        
        #endregion
        
        #region TpLibEvents
        
        //tile event handler
        private void TpLibOnTileEvent(TileEventType _)
        {
            hadTileEvent = true;
        }
     
        
        
        

        //process any tile events accumulated during UpDate
        private void ProcessTileEvents()
        {
            if (!hadTileEvent)
                return;
            hadTileEvent = false;
            
            //handle save event from Waypoint tiles.
            if (TpEvents.AnySaveEvents)
            {
                var saveEventTiles = new List<TilePlusBase>(2);
                TpEvents.GetFilteredEvents(TileEventType.SaveDataEvent, ref saveEventTiles, (tile) => tile is TopDownWaypointTile );
                if (saveEventTiles!=null && saveEventTiles.Count != 0)
                    SaveData();
            }

            //handle trigger events.
            if (TpEvents.AnyTriggerEvents)
            {
                TpEvents.GetFilteredEvents(TileEventType.TriggerEvent, ref eventTiles,(tile)=> tile is TpAnimZoneLoader );
                foreach (var tile in eventTiles!)
                    rescanOnNextUpdate = LoadZone((TpAnimZoneLoader)tile);
            }
            
            TpEvents.ClearQueuedTileEvents();
        }
        
        #endregion

        #region ZoneLoading
        
        private bool LoadZone(TpAnimZoneLoader loader)
        {
            if (loader == null || loader.m_TileFab == null) 
                return false;
            const FabOrBundleLoadFlags loadFlags = FabOrBundleLoadFlags.LoadPrefabs | FabOrBundleLoadFlags.NewGuids;
            
            //load the tilemap(s) specified in the zone. Get back the map instances loaded-to and the bounds for each map.
            TileFabLib.LoadTileFab(loader.ParentTilemap,
                                    loader.m_TileFab,
                                    loader.EffectiveAddress, //EffectiveAddress is the tile position + the LoadingOffset 
                                    TpTileBundle.TilemapRotation.Zero,
                                    loadFlags,
                                    null,
                                    null,
                                    zoneManager);

            //loaded zone could have some initial agents
            InitialAgentsRotate(m_InteractiveMap);

            if (!m_DebugMessages)
                return true;
            var loadResults = zoneManager.GetLastRegistrations();
            var assetRegistration = loadResults.First();
            Debug.Log($"Loading Results: {assetRegistration}");

            return true;
        }

        #endregion    
        
        #region NPCs
        
        //update agents to follow the player tile
        private void UpdateAgents()
        {
            if(instantiatedPrefab == null)
                return;
            
            var playerGridPos  = m_InteractiveMap.WorldToCell(instantiatedPrefab.transform.position);
            var playerWorldPos = m_InteractiveMap.GetCellCenterWorld(playerGridPos);
            var area           = GetAdjacentUpRightDownLeft(playerGridPos); //area surrounding playerGridPos
            
            //get pooled List<TilePlusBase>
            using (ListPool<TilePlusBase>.Get(out var agents))
            {
                //all the agents
                TpLib.GetTilesWithTag(m_InteractiveMap, "Agent", ref agents);
                if (agents.Count == 0)
                    return;

                //loop thru the agent tiles
                for (var i = 0; i < agents.Count; i++)
                {
                    if (agents[i] is not AgentTile agentTile)
                        continue;

                    var agentWorldPos       = agentTile.TileWorldPosition;
                    var agentCurrentGridPos = agentTile.TileGridPosition;
                    var newAgentGridPos     = TilePlusBase.ImpossibleGridPosition;
                    var moveAgent           = true;

                    //expired agent lifetime
                    if (--agentTile.m_LifeLeft <= 0)
                    {
                        TpLib.DeleteTile(agentTile);
                        continue;
                    }

                    //init hit position to "nope"
                    var hitPosition = TilePlusBase.ImpossibleGridPosition;

                    //follow-character behaviour
                    var dir      = playerWorldPos - agentWorldPos;
                    var distance = Mathf.Abs(dir.magnitude);
                    if (distance > 0.1f) //avoid floating point exception in next line 
                    {
                        var newAgentWorldPos = agentWorldPos + (dir / distance);
                        newAgentGridPos = Vector3Int.FloorToInt(newAgentWorldPos);
                    }
                    else
                    {
                        hitPosition = playerGridPos;
                        moveAgent   = false;
                    }

                    //agent should look at Player
                    agentTile.RotateTile(playerWorldPos);

                    // if(m_DebugMessages)
                    //     Debug.Log($"Moveagent {moveAgent} old position {agentCurrentGridPos}, new position {newAgentGridPos}, dir {dir},  distance {distance}");
                    if (moveAgent && newAgentGridPos != agentCurrentGridPos) //not the same position
                    {
                        hitPosition = !TpLib.CutAndPasteTile(agentTile.ParentTilemap!,
                            agentCurrentGridPos,
                            newAgentGridPos,
                            x => m_Pathfinder.IsWalkablePosition(x))
                            ? newAgentGridPos
                            : agentCurrentGridPos;
                    }

                    //no hit
                    if (hitPosition == TilePlusBase.ImpossibleGridPosition)
                        continue;

                    //add the hit for processing during the next Update
                    AddHit(hitPosition);

                    
                    //if the agent hit the Player then the agent expires.
                    //note that 'hit' means being next to.
                    if (!area.Contains(hitPosition))
                        continue;
                    agentTile.m_LifeLeft = 0; //so this Agent will get deleted on the next pass.
                    
                }
            }

        }

        #endregion
        
        #region utils
        /// <summary>
        /// Get an array of Vector3Int objects with row/col coords adjacent to the provided centerpoint
        /// </summary>
        /// <param name="center">Centerpoint</param>
        /// <returns>array of four TileIndexes with modified coordinates</returns>
        private static Vector3Int[] GetAdjacentUpRightDownLeft(Vector3Int center)
        {
            var output = new[] { center, center, center, center };
            output[0].y++; //up is a less-negative Y
            output[1].x++;
            output[2].y--;
            output[3].x--;
            return output;
        }

        //connected to the UI reset lives button.
        /// <summary>
        /// Reset lives
        /// </summary>
        public void ResetLives()
        {
            gameOver = false;
            
            //remove all tiles on the effects layer (these are the enemies)
            TpLib.GetTilesWithTag(m_InteractiveMap, "Agent", ref tpbList);
            foreach (var tpb in tpbList)
                TpLib.DeleteTile(tpb);

            if (playerController != null)
            {
                if(m_DebugMessages)
                    Debug.Log("Reset Lives");
                playerController.ResetLives();
                ShowScore();
            }
            else
                Debug.LogError("couldn't find player GameObject, restart game!");
        }

        private void ShowScore()
        {
            m_Score.text = playerController == null
                               ? "Error: can't find player tile."
                               : $"Lives: {playerController.Lives.ToString()}";
        }

        private void AddHit(Vector3Int hitPosition)
        {
            //note this is a hashset so duplicate positions are ignored.
            hitPositions.Add(hitPosition);
        }

        //initial rotate agents to point to player tile
        private void InitialAgentsRotate(Tilemap map)
        {
            var agents         = new List<TilePlusBase>();
            TpLib.GetTilesWithTag(map, "Agent", ref agents);
            if (agents.Count == 0)
                return;

            var playerGridPos = m_InteractiveMap.WorldToCell(instantiatedPrefab.transform.position);
            for (var i = 0; i < agents.Count; i++)
            {
                if(agents[i] is not AgentTile agentTile)
                    continue;
                agentTile.RotateTile(playerGridPos);    
            }
        }
        #endregion
        
        #region Persistence
        
        //save game data
        private void SaveData()
        {
            TpEvents.GetFilteredEvents(TileEventType.SaveDataEvent, ref eventTiles);
            var numEvents  = eventTiles!.Count;

            if (numEvents == 0)
            {
                Debug.LogError("No Data to save ??");
                return;
            }
            
            //Save results from uses of LoadTileFab
            var registrationJson = zoneManager.GetZoneRegJson(m_PrettyPrint);
            var path             = Path.Combine(Application.persistentDataPath, RegistrationsFileName);
            try
            {
                File.WriteAllText(path, registrationJson);
            }
            catch (Exception e)
            {
                Debug.LogError("data save failed. " + e.Message);
            }
            
            
            //Save game data
            var wrappers = new List<SaveDataWrapper>();

            for (var i = 0; i < numEvents; i++)
            {
                var tile  = eventTiles[i];
                
                if (tile is not ITpPersistence<SaveDataWrapper,StringPacketIn> t) //shouldn't fail, though
                    continue;
                wrappers.Add(t.GetSaveData(m_PrettyPrint));
            }
            

            var json = string.Empty;
            foreach (var wrapper in wrappers)
            {
               json += $"([{wrapper.m_Guid}][{wrapper.m_Json}])";
            }

            path = Path.Combine(Application.persistentDataPath, GameDataFileName);
            try
            {
                File.WriteAllText(path, json);
            }
            catch (Exception e)
            {
                Debug.LogError("data save failed. " + e.Message);
            }
        }

        private void RestoreRegisteredTileFabs()
        {
            //load in the registration data for previously-loaded TileFabs
            var    path = Path.Combine(Application.persistentDataPath, RegistrationsFileName);
            var jsonString = string.Empty;
            try
            {
                jsonString = File.ReadAllText(path);
            }
            catch (FileNotFoundException)
            {
                Debug.Log("File not found: " + path);
            }
            catch (Exception e)
            {
                Debug.LogError("Data load failed. " + e.Message);
            }

            if (string.IsNullOrEmpty(jsonString))
                return;

            //RestoreFromRegistrationJson can use a lookup that maps Tilemap names to Tilemap instances.
            //Providing this mapping is optional but saves quite a bit of time by avoiding
            //using Find to search for an GameObject by tag or name.
            //
            //This map could be created in Start() for this game but in general this mapping should
            //be created at this point since the scenes could have changed.
            
            //This simple example is sufficient for a single-scene game such as this demo. 
            //For games with multiple scenes you'd want a save file of this sort for every scene
            //or some other way of associating the data with a particular scene. Additionally, there
            //are other ways to use the TileFab loading registration data to create save files.
            
            var maps = new List<Tilemap>();
            foreach (var scene in TpLib.GetAllScenes())
                TpLib.GetTilemapsInScene(scene, ref maps, true);
            //now the maps list is full of all Tilemap component references in every scene. The next line creates
            //the Tilemap name to Tilemap instance mapping dictionary.
            var dict = maps.ToDictionary(source => source.name, tilemap => tilemap);
            
            zoneManager.RestoreFromZoneRegJson(jsonString,dict);
        }


        private bool RestoreData()
        {
            var    path = Path.Combine(Application.persistentDataPath, GameDataFileName);
            string jsonString;
            try
            {
                jsonString = File.ReadAllText(path);
            }
            catch (FileNotFoundException)
            {
                Debug.Log("File not found: " + path);
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError("Data load failed. " + e.Message);
                return false;
            }

            Debug.Log($"Loading from {GameDataFileName}...");
            
            var sections = jsonString.Split(s_ParenDelim, StringSplitOptions.RemoveEmptyEntries);
            foreach (var section in sections)
            {
                var wrapper     = section.Split(s_LeftSqBrktDelim, StringSplitOptions.RemoveEmptyEntries);
                var guid     = wrapper[0].Split(s_RightSqBrktDelim)[0];
                var jsonPart    = wrapper[1].Split(s_RightSqBrktDelim)[0];
                var destination = TpLib.GetTilePlusBaseFromGuid(guid);
                if (destination != null && destination is ITpPersistence<SaveDataWrapper,StringPacketIn> tile)
                    tile.RestoreSaveData(new StringPacketIn(jsonPart));
            }
            return true;
        }
        
        #endregion
        
        #region staticData
        
        //corresponds to GridDirection enum. no move (0), up(1), right(2), upRight(3, ie up|right) etc.
        private static readonly Vector3Int[] s_DirectionVectors = new Vector3Int[]
        {
            Vector3Int.zero, Vector3Int.up, Vector3Int.right, //no move, up, right = 0,1,2
            Vector3Int.up + Vector3Int.right,                 //up-right = 3
            Vector3Int.down,                                  //down = 4
            Vector3Int.zero,                                  //5 is down and up at same time = ng
            Vector3Int.down + Vector3Int.right,               //6 is down and right
            Vector3Int.zero,                                  //7 is down, right, and up = ng
            Vector3Int.left,                                  //8 is left
            Vector3Int.left + Vector3Int.up,                  // 9 is up-left
            Vector3Int.zero,                                  //10 is left-right = ng
            Vector3Int.zero,                                  //11 is left-right-up = ng
            Vector3Int.left + Vector3Int.down,                //12 is left-down
            Vector3Int.zero,                                  //13 is left-down-up = ng
            Vector3Int.zero,                                  //14 is left-down-right = ng
            Vector3Int.zero                                   //15 is left-down-right-up = ng;
        };

        private static readonly float[] s_DirectionAngles = new float[]
        {
            0,       //0=no move
            0,       //1=up
            -90,     //2=right
            -45,     //3=up-right
            180,     //4=down
            0,       //5=ng
            -135,    //6 down-right 
            0,       //7=ng
            90,      //8 = left
            45,      //9=up-left
            0,       //10 = ng
            0,       //11=ng
            90 + 45, //12=left-down
            0,       //13=ng
            0,       //14=ng
            0        //15=ng
        };
        #endregion        
        

    }

    #region classdef
    /// <summary>
    /// A wrapper for AssetRegistration saves
    /// </summary>
    [Serializable]
    public class LoadWrapper
    {
        /// <summary>
        /// asset registrations
        /// </summary>
        [SerializeField]
        public ZoneReg[] m_LoadResultsArray;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="res"></param>
        public LoadWrapper(ZoneReg[] res)
        {
            m_LoadResultsArray = res;
        }
    }
    
    
    /// <summary>
    /// A simple wrapper class for saving data
    /// </summary>
    [Serializable]
    public class SaveDataWrapper : MessagePacket<SaveDataWrapper>
    {
        /// <summary>
        /// The JSON string for this object
        /// </summary>
        [SerializeField]
        public string m_Json;
        /// <summary>
        /// The GUID to be used when restoring data
        /// </summary>
        [SerializeField]
        public string m_Guid;
    }
    
    #endregion
    
    
    
}
