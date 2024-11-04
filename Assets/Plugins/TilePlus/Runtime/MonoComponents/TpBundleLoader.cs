// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 05-22-2021
// ***********************************************************************
// <copyright file="TpBundleLoader.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TilePlus
{
    /// <summary>
    /// A component that can be added to a Tilemap's GameObject
    /// that loads a tilemap archive Bundle.
    /// </summary>
    [RequireComponent(typeof(Tilemap))]
    public class TpBundleLoader : MonoBehaviour
    {
        /// <summary>
        /// The tileset archive
        /// </summary>
        public TpTileBundle m_TileBundleAsset;
        /// <summary>
        /// Optional load on run
        /// </summary>
        public bool            m_LoadOnRun = true;
        /// <summary>
        /// delay time when loading @ runtime
        /// </summary>
        public float           m_DelayTime;
        /// <summary>
        /// load prefabs or not
        /// </summary>
        public bool            m_LoadPrefabs                = true;
        /// <summary>
        /// clear existing prefabs optioin
        /// </summary>
        public bool            m_ClearExistingPrefabsOnLoad = true;
        /// <summary>
        /// clear tilemap option
        /// </summary>
        public bool            m_ClearMap;
        /// <summary>
        /// Optional offset
        /// </summary>
        public Vector3Int      m_Offset = Vector3Int.zero;

        /// <summary>
        /// Tilemap target
        /// </summary>
        private Tilemap myMap;
        /// <summary>
        /// const string with error message
        /// </summary>
        private const string  MissingAssetError = "TpTileBundle asset missing!";


        /// <summary>
        /// Start 
        /// </summary>
        private IEnumerator Start()
        {
            if (!m_LoadOnRun || !Application.isPlaying || m_TileBundleAsset == null)
                yield break;
            while (!TpLib.TpLibIsInitialized)
                yield return null;

            if (m_DelayTime != 0f)
                yield return new WaitForSeconds(m_DelayTime);
            Load(m_Offset, m_LoadPrefabs,m_ClearMap);
        }

        /// <summary>
        /// Load the contents of the TpTileBundle asset. A shortcut for
        /// TpLib.LoadBundle.
        /// </summary>
        /// <param name="offset">Add an offset to each loaded item.</param>
        /// <param name="loadPrefabs">load and parent prefabs if true</param>
        /// <param name="clearPrefabs">clear all child GameObjects if true.</param>
        /// <param name="resetTilemap">use ClearAllTiles on tilemap if true</param>
        /// <returns>true/false : ok/fail</returns>
        public bool Load(Vector3Int offset,  bool loadPrefabs = true, bool clearPrefabs = false, bool resetTilemap = false )
        {
            
            if (m_TileBundleAsset == null)
            {
                Debug.LogError(MissingAssetError);
                return false;
            }
            if(myMap == null)
                myMap = GetComponent<Tilemap>();

            var loadFlags = FabOrBundleLoadFlags.None;
            if (loadPrefabs)
                loadFlags |= FabOrBundleLoadFlags.LoadPrefabs;
            if(clearPrefabs)
                loadFlags |= FabOrBundleLoadFlags.ClearPrefabs;
            if(resetTilemap)
                loadFlags |= FabOrBundleLoadFlags.ClearTilemap;
                                        
            TileFabLib.LoadBundle(m_TileBundleAsset, myMap, offset, TpTileBundle.TilemapRotation.Zero, loadFlags);
            return true;

            
        }
        
    }

#if UNITY_EDITOR

    /// <summary>
    /// A Custom editor for this class
    /// </summary>
    [CustomEditor(typeof(TpBundleLoader),true)]
    public class TilePlusLoaderEditor : UnityEditor.Editor
    {
        /// <summary>
        /// GUI content
        /// </summary>
        private static readonly GUIContent s_ObjFieldGuiContent   = new GUIContent("Bundle Asset", "Bundle Asset from Project folder");
        /// <summary>
        /// GUI content
        /// </summary>
        private static readonly GUIContent s_FloatFieldGuiContent = new GUIContent("Delay time", "At runtime, delay before loading data. Note that a delay of four frames is built-in");
        /// <summary>
        /// GUI content
        /// </summary>
        private static readonly GUIContent s_LoadButtonGuiContent = new GUIContent("Load", "Load Tilemap data, overwrites existing positions.");
        /// <summary>
        /// GUI content
        /// </summary>
        private static readonly GUIContent s_LoadPrefabs          = new GUIContent("Load Prefabs", "Load and parent prefabs when LOAD button clicked or app run");
        /// <summary>
        /// GUI content
        /// </summary>
        private static readonly GUIContent s_ClearMap             = new GUIContent("Clear map", "Clear tilemap when LOAD button clicked or on app run");
        /// <summary>
        /// GUI content
        /// </summary>
        private static readonly GUIContent s_LoadOnRun            = new GUIContent("Load on Run", "Load when app runs.");
        /// <summary>
        /// GUI content
        /// </summary>
        private static readonly GUIContent s_ClearPrefabs         = new GUIContent("Clear Prefabs on Run", "Delete existing prefabs when LOAD button clicked or app run");
        /// <summary>
        /// GUI content
        /// </summary>
        private static readonly GUIContent s_Offset               = new GUIContent("Offset", "Offset to use when loading");

        /// <summary>
        /// Implement this function to make a custom inspector.
        /// </summary>
        public override void OnInspectorGUI()
        {
            var instance = target as TpBundleLoader;
            if (instance == null)
            {
                base.OnInspectorGUI();
                return; 
            }
            
            instance.m_TileBundleAsset = (TpTileBundle) EditorGUILayout.ObjectField(s_ObjFieldGuiContent, instance.m_TileBundleAsset,
                typeof(TpTileBundle),
                false);
            instance.m_LoadOnRun                  = EditorGUILayout.Toggle(s_LoadOnRun, instance.m_LoadOnRun);
            instance.m_Offset                     = EditorGUILayout.Vector3IntField(s_Offset, instance.m_Offset);
            instance.m_DelayTime                  = EditorGUILayout.DelayedFloatField(s_FloatFieldGuiContent, instance.m_DelayTime);
            instance.m_ClearMap                   = EditorGUILayout.Toggle(s_ClearMap, instance.m_ClearMap);
            instance.m_LoadPrefabs                = EditorGUILayout.Toggle(s_LoadPrefabs, instance.m_LoadPrefabs);
            instance.m_ClearExistingPrefabsOnLoad = EditorGUILayout.Toggle(s_ClearPrefabs, instance.m_ClearExistingPrefabsOnLoad);
            
            if (instance.m_TileBundleAsset == null)
                return;
            if (PrefabUtility.IsPartOfAnyPrefab(instance))
                GUILayout.Label("Load button hidden: this Tilemap is part of a prefab.");
            else if(EditorSceneManager.IsPreviewSceneObject(instance))
                GUILayout.Label("Load button hidden: In Prefab editing context.");
            else
            {
                if (GUILayout.Button(s_LoadButtonGuiContent, GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false)))
                    instance.Load(instance.m_Offset, instance.m_LoadPrefabs, instance.m_ClearMap);
            }
        }
    }
    #endif
}
