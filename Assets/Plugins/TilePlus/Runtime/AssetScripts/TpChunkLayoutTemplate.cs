// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 03-25-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 03-28-2023
// ***********************************************************************
// <copyright file="TpChunkLayoutTemplate.cs" company="">
//     Copyright (c) 2023 Jeff Sasmor. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

using UnityEngine;

namespace TilePlus
{

    /// <summary>
    /// Class TpChunkLayoutTemplate.
    /// Implements the <see cref="ScriptableObject" />
    /// </summary>
    /// <seealso cref="ScriptableObject" />
    public class TpChunkLayoutTemplate : ScriptableObject
    {
        /// <summary>
        /// The TileFab guids
        /// </summary>
        [SerializeField]
        public string[] m_TileFabGuids;
        /// <summary>
        /// The TileFab references
        /// </summary>
        public TpTileFab[] m_TileFabs;
    }
}
