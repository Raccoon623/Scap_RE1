// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 02-24-2022
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 02-24-2022
// ***********************************************************************
// <copyright file="TpNoPaint.cs" company="Jeff Sasmor">
//     Copyright (c) 2022 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
using UnityEngine;

#if UNITY_EDITOR

namespace TilePlus
{
    /// <summary>
    /// Add this script to a tilemap to prevent it being used for painting.
    /// Add this script to the GameObject of a Palette prefab to inhibit reporting of # of different tiles in the palette.
    /// Using Get
    /// Override with the hotkey (default is alt+1 where 1 is on the normal number area above QWERTY)
    /// Note that this component is not present in a build, and is only used by the Tile+Brush and Tile+Painter.
    /// </summary>
    public class TpNoPaint : MonoBehaviour { } 
}
#endif
