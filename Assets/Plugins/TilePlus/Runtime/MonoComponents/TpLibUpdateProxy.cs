// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 08-03-2021
// ***********************************************************************
// <copyright file="TpLibUpdateProxy.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************

using UnityEngine;

namespace TilePlus
{
    /// <summary>
    /// Simple component provides Update to TpLib.
    /// </summary>
    public class TpLibUpdateProxy : MonoBehaviour
    {
        /// <summary>
        /// Start event
        /// </summary>
        public void Run()
        {
            DontDestroyOnLoad(this.gameObject);
        }

        private void OnDisable()
        {
            if(Application.isPlaying)
                Destroy(this);
            else
                DestroyImmediate(this);
        }
        
        private void Update()
        {
            TpLib.Update();
        }
    }
}
