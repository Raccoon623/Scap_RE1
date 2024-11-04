// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 08-03-2021
// ***********************************************************************
// <copyright file="SetDontDestroy.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
using UnityEngine;

namespace TilePlus
{
    /// <summary>
    /// Simple component to set HideFlags to HideFlags.DontSave
    /// </summary>
    public class SetDontDestroy : MonoBehaviour
    {
        /// <summary>
        /// Start event
        /// </summary>
        private void Start()
        {
            DontDestroyOnLoad(this.gameObject);
        }
        
        


    }
}
