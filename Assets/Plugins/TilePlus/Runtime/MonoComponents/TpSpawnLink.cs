// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 06-15-2021
// ***********************************************************************
// <copyright file="TpSpawnLink.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace TilePlus
{
    /// <summary>
    /// simple class that exists to provide context for a spawned prefab
    /// </summary>
    public class TpSpawnLink : MonoBehaviour
    {
        /// <summary>
        /// Controls auto-destroy use
        /// </summary>
        [Tooltip("Add Auto-Destroy when checked.")]
        public bool m_AutoDestroy;
        
        /// <summary>
        /// Set the auto-destroy timeout 
        /// </summary>
        [Tooltip("Auto-Destroy timeout used only when AutoDestroy is true.")]
        public float m_AutoDestroyTimeout = 5f;

        private Coroutine task;
        
        /// <summary>
        /// instance ID of source (ie project folder) prefab that this was spawned from
        /// </summary>
        [NonSerialized] public int m_SourcePrefabId;

        /// <summary>
        /// This is called when the prefab is instantiated or fetched from pool.
        /// BE SURE TO CALL THIS BASE CLASS WHEN OVERRIDING
        /// </summary>
        public virtual void OnTpSpawned()
        {
            if(m_AutoDestroy)
                task = StartCoroutine(Timeout());

        }

        /// <summary>
        /// This is called when the prefab is returned to the pool.
        /// BE SURE TO CALL THIS BASE CLASS WHEN OVERRIDING
        /// </summary>
        public virtual void OnTpDespawned()
        {
            if(task != null)
                StopCoroutine(task);
        }

        /// <summary>
        /// Use this to despawn your prefab. Normally you don't need to override this.
        /// BE SURE TO CALL THIS BASE CLASS WHEN OVERRIDING
        /// </summary>
        // ReSharper disable once MemberCanBeProtected.Global
        public virtual void DespawnMe()
        {
            SpawningUtil.DespawnPrefab(m_SourcePrefabId, gameObject);
        }
        
        

        /// <summary>
        /// Timeout, then despawn or destroy the prefab.
        /// </summary>
        /// <returns>IEnumerator.</returns>
        private IEnumerator Timeout()
        {
            yield return new WaitForSeconds(m_AutoDestroyTimeout);
            DespawnMe(); 
            
        }
        
        
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor for TpSpawnLink component
    /// </summary>
    [CustomEditor(typeof(TpSpawnLink))]
    public class TpSpawnLinkEditor : UnityEditor.Editor
    {
        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            var instance = target as TpSpawnLink; 
            if(instance == null)
            {
                base.OnInspectorGUI();
                return;
            }

            EditorGUILayout.LabelField("Source Prefab ID", instance.m_SourcePrefabId.ToString());
            base.OnInspectorGUI();
        }

       
    }
#endif    
}

