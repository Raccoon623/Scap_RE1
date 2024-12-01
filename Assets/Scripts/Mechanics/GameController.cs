using Platformer.Core;
using Platformer.Model;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Platformer.Mechanics
{
    public class GameController : MonoBehaviour
    {
        public static GameController Instance { get; private set; }

        // This model field is public and can be modified in the inspector.
        public PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        private void Awake()
        {
            // Ensure this GameController persists across scenes and enforce the singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Keep this GameController object persistent
            }
            else if (Instance != this)
            {
                Destroy(gameObject); // Destroy duplicate GameController instances
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded; // Subscribe to the SceneLoaded event
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded; // Unsubscribe from the SceneLoaded event
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"Scene loaded: {scene.name}");
            InitializeSceneReferences(); // Initialize references for the new scene
        }

        private void Update()
        {
            if (Instance == this)
            {
                Simulation.Tick(); // Tick the simulation
            }
        }

        /// <summary>
        /// Finds and assigns required references (Player, Virtual Camera, and Spawn Point) in the current scene.
        /// </summary>
        private void InitializeSceneReferences()
        {
            // Find and assign the PlayerController
            if (model.player == null)
            {
                model.player = FindObjectOfType<PlayerController>();
                if (model.player != null)
                {
                    Debug.Log("PlayerController found and assigned.");
                }
                else
                {
                    Debug.LogError("PlayerController not found in the scene.");
                }
            }

            // Find and assign the Virtual Camera
            if (model.virtualCamera == null)
            {
                model.virtualCamera = FindObjectOfType<Cinemachine.CinemachineVirtualCamera>();
                if (model.virtualCamera != null)
                {
                    Debug.Log("Cinemachine Virtual Camera found and assigned.");
                }
                else
                {
                    Debug.LogError("Cinemachine Virtual Camera not found in the scene.");
                }
            }

            // Find and assign the Spawn Point
            if (model.spawnPoint == null)
            {
                var spawnPointObject = GameObject.Find("SpawnPoint");
                if (spawnPointObject != null)
                {
                    model.spawnPoint = spawnPointObject.transform;
                    Debug.Log("Spawn Point found and assigned.");
                }
                else
                {
                    Debug.LogError("SpawnPoint not found in the scene. Make sure a GameObject named 'SpawnPoint' exists.");
                }
            }
        }
    }
}
