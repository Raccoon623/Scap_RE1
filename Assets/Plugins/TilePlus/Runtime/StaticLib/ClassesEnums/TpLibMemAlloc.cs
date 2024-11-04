// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 02 Feb 2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : May 27 2023
// ***********************************************************************
// <copyright file="TpLibMemAlloc.cs" company="Jeff Sasmor">
//     Copyright (c) 2023 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************

namespace TilePlus
{
    /// <summary>
    /// This class is used when reallocating data structure sizes in TpLib.
    /// See TpLib.Resize
    /// </summary>
    public class TpLibMemAlloc
    {
        /// <summary>
        /// Size for Tilemap and Tag dictionaries
        /// </summary>
        public int m_TilemapAndTagDictsSize = TpLib.TilemapAndTagDictsInitSize;

        /// <summary>
        /// Size for Guid dictionary
        /// </summary>
        public int m_GuidDictSize = TpLib.GuidDictInitialSize;

        /// <summary>
        /// size for Types and Interfaces dictionaries
        /// </summary>
        public int m_TypesSize = TpLib.TypesInitSize;

        /// <summary>
        /// size for pooled dictionaries (Vector3Int,TilePlusBase)
        /// </summary>
        public int m_PoolNewItemSizeForDicts = TpLib.PoolNewItemSize_Dict_V3I_Tpb;

        /// <summary>
        /// size for pooled lists of TilePlusBase
        /// </summary>
        public int m_PoolNewItemSizeForLists = TpLib.PoolNewItemSize_List_Tpb;

        /// <inheritdoc />
        // ReSharper disable once AnnotateNotNullTypeMember
        public override string ToString()
        {
            return $"Tilemap and Tag Dicts: {m_TilemapAndTagDictsSize}, GuidDict: {m_GuidDictSize}, Types and Interfaces: {m_TypesSize},\nPool collection item sizes: V3I->TPB Dicts {m_PoolNewItemSizeForDicts}, TPB Lists: {m_PoolNewItemSizeForLists}";
        }
    }

}
