// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 08-03-2021
// ***********************************************************************
// <copyright file="TpPrefabMarker.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

#nullable enable
namespace TilePlus
{
    /// <summary>
    /// Marks this Prefab as created by the TilePlus Prefab Bundler.
    /// Loads tilefabs when a prefab is dragged-in or viewed in a Stage (in-editor), or when instantiated at runtime.
    /// Should be placed on the Tilemaps' parent Grid's GameObject.
    /// </summary>
    [ExecuteAlways]  
    public class TpPrefabMarker : MonoBehaviour
    {
        /// <summary>
        /// The TileFab to use when instantiating this Prefab
        /// </summary>
        public TpTileFab? m_TileFabForPrefab;
        
        /// <summary>
        /// The TileFab load flags for this prefab. Normal is usually the best choice.
        /// </summary>
        [SerializeField]
        public FabOrBundleLoadFlags m_BundleLoadFlags = FabOrBundleLoadFlags.Normal;

        [SerializeField][HideInInspector]
        // ReSharper disable once InconsistentNaming
        private bool wasLoaded;

        private void OnEnable()
        {
            if (Application.isPlaying && Application.isEditor)
               StartCoroutine(Start());
        }

        private IEnumerator Start()
        {
            if (wasLoaded)
                yield break;

            var maps = GetComponentsInChildren<Tilemap>();
            if(maps == null || maps.Length == 0)
                yield break;
            
            Dictionary<string,Tilemap> nameToMapDict = maps.ToDictionary(k => k.name, v => v);
            
            while (!TpLib.TpLibIsInitialized)
                yield return null;

            TileFabLib.LoadTileFab(null, m_TileFabForPrefab, Vector3Int.zero, TpTileBundle.TilemapRotation.Zero,
                FabOrBundleLoadFlags.Normal | FabOrBundleLoadFlags.NewGuids, null, nameToMapDict);
            
            wasLoaded = true;

        }

        
    }
}
