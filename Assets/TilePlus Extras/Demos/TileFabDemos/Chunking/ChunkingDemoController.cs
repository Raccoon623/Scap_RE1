using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using TilePlus;
using TilePlusCommon;
using UnityEngine;
using UnityEngine.Tilemaps;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;
// ReSharper disable AnnotateNotNullParameter

namespace TilePlusDemo
{
    /// <summary>
    /// The game controller for the Chunking demo.
    /// </summary>
    [RequireComponent(typeof(TpZoneLayout))]
    public class ChunkingDemoController : MonoBehaviour
    {
        #region publics
            
        /// <summary>
        /// The initial position of the Player
        /// </summary>
        public Vector3Int m_InitialPlayerPosition = Vector3Int.zero;

        /// <summary>
        /// Reference to Player tile's prefab
        /// </summary>
        [Tooltip("A reference to the 'Baddie' prefab in the Project")]
        public GameObject m_PlayerPrefab;

        /// <summary>
        /// Reference to the Grid
        /// </summary>
        [Tooltip("A reference to the GRID in the scene.")]
        public Grid m_Grid;

        /// <summary>
        /// Initial z offset for camera
        /// </summary>
        [Tooltip("Initial z-offset for camera.")]
        public float m_CameraOffset = -10;


        /// <summary>
        /// Random value above which a 4x4 chunk is placed or not.
        /// </summary>
        [Tooltip("This value is compared to a random # to determine whether or not to place a 4x4 chunk.")]
        public float m_Randomness = 0.7f;
        
        
        /// <summary>
        /// The camera ref
        /// </summary>
        [Tooltip("Camera to use for following the Player")]
        #pragma warning disable CS8618
        public Camera m_Camera;
        #pragma warning restore CS8618

        /// <summary>
        /// If true, fill the camera view with chunks during startup.
        /// false state is useful for diags.
        /// </summary>
        [Tooltip("Fill the camera view on Start")]
        public bool m_FillCameraViewOnStart = true;

        /// <summary>
        /// When checked, fill-in chunks on the 4x4 map are marked immortal and won't be deleted.
        /// </summary>
        [Tooltip("When checked, fill-in chunks on the 4x4 map are randomly marked immortal and won't be deleted")]
        public bool m_RandomlyImmortal;
        
        /// <summary>
        /// Enable pretty print for save files. More readable but larger.
        /// </summary>
        [Tooltip("Enable human-readable save files.")]
        public bool m_PrettyPrint; 
        
        /// <summary>
        /// An informational field.
        /// </summary>
        [Header("Informational")]
        // ReSharper disable once MemberCanBePrivate.Global
        #pragma warning disable CS8618
        public string m_Direction;
        #pragma warning restore CS8618

        /// <summary>
        /// Keycode for UP
        /// </summary>
        [Header("Keycodes to use for this player")]
        public KeyCode m_Up = KeyCode.W;

        /// <summary>
        /// Keycode for LEFT
        /// </summary>
        public KeyCode m_Left = KeyCode.A;

        /// <summary>
        /// Keycode for DOWN
        /// </summary>
        public KeyCode m_Down = KeyCode.S;

        /// <summary>
        /// Keycode for RIGHT
        /// </summary>
        public KeyCode m_Right = KeyCode.D;

        
        
        /// <summary>
        /// Delay between moves used for debouncing
        /// </summary>
        [Tooltip("Delay between moves for debouncing, min=0.2")]
        public float m_Delay = 0.2f;

        #endregion

        #region constants
        private const string RegistrationsFileName      = "ChunkingDemo_RegData.txt";
        private const string SaveFileName = "ChunkingDemo_SaveData.txt";

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

        #pragma warning disable CS8618
        private TdDemoPlayerPrefabLink playerController;
        //the Player GameObject
        private GameObject instantiatedPrefab;
        private SimpleCamFollow     camFollower;
        #pragma warning restore CS8618

        //used to keep Update passive until init is complete
        private bool initialized;
        //used for debouncing
        private float lastTime;
        //used for debouncing
        private float timeAccum;


        private TpZoneLayout[] layouts;
        private TpZoneLayout layoutWithWaypoints;
        private bool saveDataNow;


        #endregion

        #region Events

        //Unity event function Start used as Coroutine
        private IEnumerator Start()
        {
            if (m_Camera == null)
            {
                Debug.LogError("Missing Camera reference");
                yield break;

            }

            
            //wait till TpLib static class is ready for use. 
            while (!TpLib.TpLibIsInitialized)
                yield return null;

            //preload TpLib timing subsystem (optional step, happens automatically when used if not already done)
            TpLib.PreloadTimingSubsystem();

            //the ZoneLayout components.
            layouts = GetComponents<TpZoneLayout>();
            layouts = layouts.Where(l => l.m_Active).ToArray();
            
            var numLayouts = layouts.Length;
            if (numLayouts == 0)
            {
                Debug.LogError("No layouts found: add a layout component!!");
                yield break;
            }
            else
            
                Debug.Log($"Found {numLayouts} layouts.");

            //get the camera follower component.
            camFollower = m_Camera.GetComponent<SimpleCamFollow>();

            //create the TileFabGuid to TileFab map
            //this is optional, but speeds up EnableZoneManagers.
            //If this is NOT provided then the TileFabs used by ChunkSelectors need to be in a Resources folder.
            //If they are NOT then RestoreRegisteredTileFabs won't be able to find the TileFab.
            //In this demo, the asset (TileFab) referencees can be found by
            //examining the layouts' Selectors, which have refs to the TileFabs.
            //This ought to be a common use case. If not, you can
            //have a custom component or Scriptable Object attached to some component,
            //or some other method to keep the references in the Scene so that you could
            //get them (which is essentially what's done here: the Layouts always have
            //have refs to the Selectors, which always have refs to the TileFabs). 
            var assetGuidToInstanceMap = new Dictionary<string, TpTileFab>();
            foreach (var layout in layouts)
            {
                if(layout == null)
                    continue;
                if(layout.m_ChunkSelector == null)
                    continue;
                if (layout.m_ChunkSelector.UsedTileFabs == null)
                    continue;
                foreach(var fab in layout.m_ChunkSelector.UsedTileFabs)
                    assetGuidToInstanceMap.Add(fab.name,fab);
            }
            
            TileFabLib.EnableZoneManagers(true,numLayouts,128,assetGuidToInstanceMap);

            // ReSharper disable once LoopCanBePartlyConvertedToQuery
            foreach (var layout in layouts)
            {
                if (layout.Initialize(out _))
                {
                    if (layout.m_ChunkSize == 16) //this is the layout with waypoints. 
                        layoutWithWaypoints = layout;
                    continue;
                }

                Debug.LogError("Could not initialize layout");
                yield break;
            }

            var didRestore = RestoreData(out var pos, out var wpGuid);
            if(didRestore)
            {
                m_InitialPlayerPosition = pos;
            }
            
            
            //instantiate the Player prefab
            if (m_PlayerPrefab == null)
            {
                Debug.LogError("PlayerPrefab field on TdDemoGameController was null!");
                yield break;
            }

            //spawn player prefab at the world position specified by the WorldOrigin param in the ZoneLayout component.
            //the ZoneLayout component has moved the Camera to the WorldOrigin  position.
            //those values are in the ZoneLayout component

            var originInWorldCoords = m_Grid.GetCellCenterWorld(m_InitialPlayerPosition);
            var gridTransform       = m_Grid.transform;
            originInWorldCoords.z = gridTransform.position.z;

            instantiatedPrefab = SpawningUtil.SpawnPrefab(m_PlayerPrefab,
                                                          originInWorldCoords,
                                                          gridTransform,
                                                          string.Empty,
                                                          false,
                                                          true)!;
            if (instantiatedPrefab == null)
            {
                Debug.LogError($"Could not instantiate {m_PlayerPrefab}");
                yield break;
            }

            var camPosition = new Vector3(originInWorldCoords.x, originInWorldCoords.y, -10);
            m_Camera.transform.position = camPosition;
            Debug.Log($"Setting cam position to {camPosition}");

            //the prefab is the camera follower's target.
            camFollower.m_TargetToFollow = instantiatedPrefab.transform;

            //the prefab's controller, mainly just lerps from one position to the next
            playerController = instantiatedPrefab.GetComponent<TdDemoPlayerPrefabLink>();

            if (playerController == null)
            {
                Debug.LogError($"Could not find TdDemoPlayerPrefabLink component on {m_PlayerPrefab} GameObject");
                yield break;
            }

            RestoreRegisteredTileFabs();
            if (m_FillCameraViewOnStart)
            {
                foreach (var layout in layouts)
                {
                    //note that UpdateTick returns true for an ERROR
                    if (layout.UpdateTick(m_InitialPlayerPosition, null, null, LoadingFilter, layout.m_ZoneManagerName))
                        Debug.LogError($"UpdateTick for layout using ZoneManager: {layout.m_ZoneManagerName} exited with an error.");
                }
            }

            //now if there was a save-game file, the player position is the saved position from that file. Now locate
            //and enable the waypoint.
            if (didRestore)
            {
                var wp = TpLib.GetTilePlusBaseFromGuid(wpGuid);
                var wpTile  = wp as TopDownWaypointTile;
                if (wpTile)
                {
                    Debug.Log($"Enabling Waypoint at {wp.TileGridPosition}");
                    wpTile.IsEnabled = true;
                }
            }

            //subscribe to the TpLib event about events sent from tiles to listeners
            TpEvents.OnTileEvent += TpEventsOnOnTileEvent;
            initialized = true; //now the Update event will do more than just return.
        }

        private void TpEventsOnOnTileEvent(TileEventType flags)
        {
            if ((flags & TileEventType.SaveDataEvent) == TileEventType.SaveDataEvent)
                saveDataNow = true;

        }


        //event loop
        private void Update()
        {
            //do nothing for certain situations
            if (!initialized)
                return;

            // +debounce
            timeAccum += Time.deltaTime;

            if (!Input.anyKey)
                return;

            if (m_Delay < 0.2f)
                m_Delay = 0.2f;
            if (timeAccum < (lastTime + m_Delay))
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
            d |= Input.GetKey(m_Up)
                     ? 1
                     : 0;
            d |= Input.GetKey(m_Right)
                     ? 2
                     : 0;
            d |= Input.GetKey(m_Down)
                     ? 4
                     : 0;
            d |= Input.GetKey(m_Left)
                     ? 8
                     : 0;

            m_Direction = ((GridDirection)d).ToString(); //this is just for troubleshooting, it's a string in the inspector.
            if (d is <= 0 or > 15)                       //range check
                return;

            //get the new direction
            var newDirection = s_DirectionVectors[d];
            if (newDirection == Vector3Int.zero) //no move possible depends on keys pressed.
                return;

            //current and new positions
            var currentPlayerPos     = instantiatedPrefab.transform.position;
            var currentPlayerGridPos = m_Grid.WorldToCell(currentPlayerPos);
            var newGridPos           = currentPlayerGridPos + newDirection;
            var newPlayerWorldPos    = m_Grid.GetCellCenterWorld(newGridPos);
            //rotation angle for Player's prefab
            var angle = s_DirectionAngles[d];

            //rotate the prefab to face in the direction of motion
            instantiatedPrefab.transform.rotation = Quaternion.identity;
            if (angle != 0f)
                instantiatedPrefab.transform.Rotate(Vector3.forward, angle);

            //move the prefab
            playerController.Move(newPlayerWorldPos);

            if (layouts == null)
                return;

            //update the layout controller(s). Note that the zone manager name will be passed as an object to the filter.
            foreach (var layout in layouts)
                layout.UpdateTick(currentPlayerPos, null, null, LoadingFilter, layout.m_ZoneManagerName);
            
            //now, update waypoints about new position
            //Send a message to all tiles accepting a position packet, not returning anything, and are of Type TopDownWaypointTile.
            var pObj = new PositionPacketIn(newGridPos);    
            TpMessaging.SendMessage<EmptyPacket, PositionPacketIn>(null, typeof(TopDownWaypointTile), pObj, null);
            //if a waypoint's position matches newGridPos then it'll create a Save event
            if (saveDataNow)
                SaveGame();

        }


        
        
        

        //for the purpose of this demo:
        //16x16 chunks: the filter doesn't load any TileFabs that have the 'UserFlag' toggle set.
        //4x4 chunks: the filter randomly omits TileFabs
        private TpZoneLayout.LoadFilterResult LoadingFilter(object userData, TpZoneManager zm, TileFabLoadParams loadParams)
        {
            
            //var zmName = userData as string;  //do not need it, this is just to show how to pass something arbitrary to the filter.  

            if (zm.ChunkSize == 4) //the zm with smaller chunks on the BackgroundGrid randomly leaves out zones
                //and even more randomly marks zones immortal if the corresponding UI toggle is checked.
            {
                return Random.value > m_Randomness
                    ? TpZoneLayout.LoadFilterResult.MarkZoneFilledButLeaveEmpty
                    : m_RandomlyImmortal && Random.value > 0.9f
                        ? TpZoneLayout.LoadFilterResult.FillZoneAndMarkImmortal
                        : TpZoneLayout.LoadFilterResult.FillZone;
            }

            //the zm with larger chunks leaves a zone empty if it's m_UserFlag variable is true.
            //note there are both a string and a boolean user 'flags'. 
            return loadParams.m_TileFab.m_UserFlag
                       ? TpZoneLayout.LoadFilterResult.LeaveZoneEmpty
                       : TpZoneLayout.LoadFilterResult.FillZone;
        }

        #endregion

        #region persistence
        
        private void SaveGame()
        {
            saveDataNow = false;
            
            //handle save event from Waypoint tiles.
            if (TpEvents.AnySaveEvents)
            {
                var saveEventTiles = new List<TilePlusBase>(2);
                // ReSharper disable once Unity.NoNullPatternMatching
                TpEvents.GetFilteredEvents(TileEventType.SaveDataEvent, ref saveEventTiles, 
                                           tile =>  tile && tile is TopDownWaypointTile
                                                                 {
                                                                     IsEnabled: true
                                                                 });
                TpEvents.ClearQueuedTileEvents();
                //Debug.Log($"Found {saveEventTiles.Count} Save Data events");
                
                if (saveEventTiles != null && saveEventTiles.Count != 0)
                {
                    //what's the GUID of the trigger waypoint?
                    var wp = saveEventTiles[0].TileGuidString;
                    var pos = saveEventTiles[0].TileGridPosition;
                    //Debug.Log($"Saving: pos {pos}, wpGuid: {wp}");

                    //Save results from uses of LoadTileFab
                    TileFabLib.GetNamedInstance(layoutWithWaypoints.m_ZoneManagerName, out var zm);

                    var registrationJson = zm.GetZoneRegJson(m_PrettyPrint);
                    var path = Path.Combine(Application.persistentDataPath, RegistrationsFileName);
                    try
                    {
                        File.WriteAllText(path, registrationJson);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("data save failed. " + e.Message);
                    }

                    //save Game data as well: this is only the GUID of the active waypoint and the x,y position.
                    path = Path.Combine(Application.persistentDataPath, SaveFileName);
                    try
                    {
                        File.WriteAllText(path, $"{wp},{pos.x},{pos.y}");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("data save failed. " + e.Message);
                    }
                }
            }

            
            TpEvents.ClearQueuedTileEvents();
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
            
            //is the layout with waypoints enabled?
            if(layoutWithWaypoints == null)
                return;
            //now the maps list is full of all Tilemap component references in every scene. The next line creates
            //the Tilemap name to Tilemap instance mapping dictionary.
            var dict = maps.ToDictionary(source => source.name, tilemap => tilemap);
            TileFabLib.GetNamedInstance(layoutWithWaypoints.m_ZoneManagerName, out var zm);
            zm.RestoreFromZoneRegJson(jsonString,dict);
        }


        private bool RestoreData(out Vector3Int position, out string waypointGuid)
        {
            var    path = Path.Combine(Application.persistentDataPath, SaveFileName);
            string jsonString;
            try
            {
                jsonString = File.ReadAllText(path);
            }
            catch (FileNotFoundException)
            {
                Debug.Log("File not found: " + path);
                position = Vector3Int.zero;
                waypointGuid = string.Empty;
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError("Data load failed. " + e.Message);
                position = Vector3Int.zero;
                waypointGuid = string.Empty;
                return false;
            }

            Debug.Log($"Loading from {path}...");
        
            var sections = jsonString.Split(',' , StringSplitOptions.RemoveEmptyEntries);
            waypointGuid = sections[0];
            var x = int.Parse(sections[1]);
            var y = int.Parse(sections[2]);
            position = new Vector3Int(x, y);
            return true;
        }
        
        #endregion



        #region staticData

        //corresponds to GridDirection enum. no move (0), up(1), right(2), upRight(3, ie up|right) etc.
        private static readonly Vector3Int[] s_DirectionVectors = new Vector3Int[]
                                                                  {
                                                                      Vector3Int.zero,
                                                                      Vector3Int.up,
                                                                      Vector3Int.right,                   //no move, up, right = 0,1,2
                                                                      Vector3Int.up + Vector3Int.right,   //up-right = 3
                                                                      Vector3Int.down,                    //down = 4
                                                                      Vector3Int.zero,                    //5 is down and up at same time = ng
                                                                      Vector3Int.down + Vector3Int.right, //6 is down and right
                                                                      Vector3Int.zero,                    //7 is down, right, and up = ng
                                                                      Vector3Int.left,                    //8 is left
                                                                      Vector3Int.left + Vector3Int.up,    // 9 is up-left
                                                                      Vector3Int.zero,                    //10 is left-right = ng
                                                                      Vector3Int.zero,                    //11 is left-right-up = ng
                                                                      Vector3Int.left + Vector3Int.down,  //12 is left-down
                                                                      Vector3Int.zero,                    //13 is left-down-up = ng
                                                                      Vector3Int.zero,                    //14 is left-down-right = ng
                                                                      Vector3Int.zero                     //15 is left-down-right-up = ng;
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
}
