// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 05-09-2021
// ***********************************************************************
// <copyright file="TpTileList.cs" company="Jeff Sasmor">
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
    /// A list of tilespawneritems
    /// </summary>
    [CreateAssetMenu(fileName = "TpTileList.asset", menuName = "TilePlus/Create TileList", order = 10001)]
   public class TpTileList : ScriptableObject
    {
        /// <summary>
        /// Enum to choose where to paint a tile
        /// relative to the spawner tile. Ignored if PositioningMode != UseAssetSetting
        /// </summary>
        public enum TilePaintPosition
        {
            /// <summary>
            /// Indicates to Paint method that value from this spec should be used.
            /// </summary>
            None = 0,
            /// <summary>
            /// Y += 1
            /// </summary>
            Up = 1,
            /// <summary>
            /// X += 1, Y += 1
            /// </summary>
            UpRight = 2,
            /// <summary>
            /// x += 1
            /// </summary>
            Right = 3,
            /// <summary>
            /// x += 1, y -= 1
            /// </summary>
            RightDown = 4,
            /// <summary>
            /// y -= 1
            /// </summary>
            Down = 5,
            /// <summary>
            /// x -= 1, y -= 1
            /// </summary>
            DownLeft = 6,
            /// <summary>
            /// x -= 1
            /// </summary>
            Left = 7,
            /// <summary>
            /// x -= 1, y += 1
            /// </summary>
            LeftUp = 8,
            /// <summary>
            /// Use a random position
            /// </summary>
            Random = 9,
            /// <summary>
            /// Use the tile's position if the target tilemap is different. If not, uses random position.
            /// </summary>
            Top = 10
            
        }

        /// <summary>
        /// An individual tile painting specification
        /// </summary>
        [Serializable]
        public class PaintingSpec
        {
            /// <summary>
            /// The tile to spawn
            /// </summary>
            [Tooltip("Tile to spawn")] public TilePlusBase? m_Tile;

            /// <summary>
            /// Where to spawn it
            /// </summary>
            [Tooltip("The position")] public TilePaintPosition m_PaintPosition;

            /// <summary>
            /// The tile's name
            /// </summary>
            /// <value>The name of the tile.</value>
            public string TileName => m_Tile != null ? m_Tile.name : string.Empty;
        }

        
        /// <summary>
        /// List of tile specs
        /// </summary> 
#if ODIN_INSPECTOR
        [AssetsOnly]
#endif
        [Tooltip("List of Tile assets")]
        public List<PaintingSpec> m_Tiles = new List<PaintingSpec>();

        /// <summary>
        /// How many tile specs?
        /// </summary>
        /// <value>The number of tile specs.</value>
        public int NumTiles => this.m_Tiles.Count;

        /// <summary>
        /// The tile names
        /// </summary>
        private string[]? tileNames;


        /// <summary>
        /// Gets the asset version.
        /// </summary>
        /// <value>The asset version.</value>
        #if ODIN_INSPECTOR
        [ShowInInspector]
        #endif
        public byte AssetVersion => 1;

        /// <summary>
        /// Get a list of all the tilenames
        /// </summary>
        /// <value>The tile names.</value>
        public string[] TileNames
        {
            get
            {
                if (NumTiles == 0)
                    return Array.Empty<string>();
                if (tileNames == null || tileNames.Length == 0)
                {
                    tileNames = new string[NumTiles];
                    for (var i = 0; i < NumTiles; i++)
                        tileNames[i] = m_Tiles[i].TileName;
                }

                return tileNames;
            }
        }
    }
}
