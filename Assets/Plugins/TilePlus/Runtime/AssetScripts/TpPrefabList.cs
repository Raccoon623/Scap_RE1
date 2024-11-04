// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 08-03-2021
// ***********************************************************************
// <copyright file="TpPrefabList.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
#nullable enable

namespace TilePlus
{
    /// <summary>
    /// specification for spawning a prefab
    /// </summary>
    [Serializable]
    public class TilePlusPrefabSpawnerItem
    {
        /// <summary>
        /// the prefab to instantiate
        /// </summary>
        [Tooltip("Prefab to instantiate: no scene references, please!")]
        public GameObject? m_Prefab;
        /// <summary>
        /// The name or tag of parent for spawned obj
        /// </summary>
        [Tooltip("Name or tag of Parent of the spawned object. Can be empty")]
        public string? m_Parent;
        /// <summary>
        /// set true to use Parent as a tag
        /// </summary>
        [Tooltip("Use 'Parent' as a tag to find parent GameObject. Ignored if 'Parent' is empty")]
        public bool m_UseParentNameAsTag;
        /// <summary>
        /// position for spawned prefab
        /// </summary>
        [Tooltip("The position")]
        public Vector3 m_Position;
        /// <summary>
        /// Set true to make position relative to tile position
        /// </summary>
        [Tooltip("Check to make position relative to tile grid position.")]
        public bool m_PositionIsRelative;
        /// <summary>
        /// Autospawning keeps world position relative to parent
        /// </summary>
        [Tooltip("When autospawning, keep world position relative to parent.")]
        public bool m_KeepWorldPosition;

        /// <summary>
        /// If pooling is enabled, how many additional instances to add to pool when this
        /// prefab is first encountered.
        /// </summary>
        [Tooltip("If pooling enabled, how many additional instances to add when this prefab is spawned for the first time.")]
        public int m_PoolInitialSize = 10;

        /// <summary>
        /// Property to get name of prefab
        /// </summary>
        /// <value>The name of the prefab.</value>
        public string PrefabName => m_Prefab != null ? m_Prefab.name : string.Empty;
    }

    /// <summary>
    /// an asset with prefabs to spawn
    /// </summary>
    [CreateAssetMenu(fileName = "TpPrefabList.asset", menuName = "TilePlus/Create PrefabList", order = 10000)]
    public class TpPrefabList: ScriptableObject
    {
        /// <summary>
        /// A list of prefab assets
        /// </summary>
#if ODIN_INSPECTOR
        [AssetsOnly]
#endif
        [Tooltip("List of Prefab assets")]
        
        public List<TilePlusPrefabSpawnerItem> m_Prefabs = new List<TilePlusPrefabSpawnerItem>();
        /// <summary>
        /// number of prefabs
        /// </summary>
        /// <value>The number prefabs.</value>
        public int NumPrefabs => this.m_Prefabs.Count;


        /// <summary>
        /// The prefab names
        /// </summary>
        private string[]? prefabNames;

        /// <summary>
        /// Gets the asset version.
        /// </summary>
        /// <value>The asset version.</value>
        #if ODIN_INSPECTOR
        [ShowInInspector]
        #endif
        public byte AssetVersion => 1;

        /// <summary>
        /// property to get array of prefab names
        /// </summary>
        /// <value>The prefab names.</value>
        public string[] PrefabNames
        {
            get
            {
                if (NumPrefabs == 0)
                    return Array.Empty<string>();
                if (prefabNames != null && prefabNames.Length != 0)
                    return prefabNames;
                prefabNames = new string[NumPrefabs];
                for (var i = 0; i < NumPrefabs; i++)
                    prefabNames[i] = m_Prefabs[i].PrefabName;

                return prefabNames;
            }
        }
    }
}
