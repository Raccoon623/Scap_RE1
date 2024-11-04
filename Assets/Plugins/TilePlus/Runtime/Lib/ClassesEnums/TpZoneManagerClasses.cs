// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 03-25-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 04-01-2023
// ***********************************************************************
// <copyright file="TpZoneManagerClasses.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace TilePlus
{
    /// <summary>
    /// Results from using LoadImportedTilefab
    /// </summary>
    public class TilefabLoadResults
    {
        /// <summary>
        /// ctor for failed load
        /// </summary>
        /// <param name="chunkLocator">The chunk locator.</param>
        public TilefabLoadResults(RectInt chunkLocator)
        {
            ChunkLocator = chunkLocator;
        }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="loadedBundles">which assets were loaded</param>
        /// <param name="registration">the registration (if any)</param>
        /// <param name="elapsedTimeString">informational</param>
        /// <param name="chunkLocator">Can be used to locate this chunk</param>
        public TilefabLoadResults(List<TpTileBundle> loadedBundles,
                                  ZoneReg?           registration,
                                  string             elapsedTimeString,
                                  RectInt            chunkLocator)
        {
            LoadedBundles     = loadedBundles;
            ElapsedTimeString = elapsedTimeString;
            ZoneReg           = registration!;
            ChunkLocator      = chunkLocator;
            Valid             = true;
        }

        /// <summary>
        /// The asset registration for this load (if any, could be null)
        /// </summary>
        /// <value>The zone reg.</value>
        public ZoneReg? ZoneReg { get; }

        /// <summary>
        /// If false, this locator caused a tilefab load error.
        /// </summary>
        /// <value><c>true</c> if valid; otherwise, <c>false</c>.</value>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public bool Valid { get; }

        /// <summary>
        /// The chunk locator is a rect int
        /// assigned to this Tilefab when it was placed.
        /// Number is meaningless if chunking not used.
        /// </summary>
        /// <value>The chunk locator.</value>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public RectInt? ChunkLocator { get; }


        /// <summary>
        /// Elapsed time for the load
        /// </summary>
        /// <value>The elapsed time string.</value>
        public string? ElapsedTimeString { get; }

        /// <summary>
        /// Array of all Bundles that were loaded.
        /// NOTE: do not hold references to these outside of
        /// local scope.
        /// </summary>
        /// <value>The loaded bundles.</value>
        /// <remarks>This is preallocated upon entry but may not be
        /// completely filled.</remarks>
        public List<TpTileBundle>? LoadedBundles { get; }

        
        /// <inheritdoc />
        public override string ToString()
        {
            var lb  = LoadedBundles?.Count ?? 0;
            return $"Elapsed time: {ElapsedTimeString} using {lb} Bundle assets";
        }
    }

    /*What is this for?
    * There's some discussion about this in the Docs but:
    * When a Bundle is placed and the Bundle contains TPT tiles then
    * the TPT tiles are cloned and given new GUIDS. THe new GUIDs are
    * required so that the TPT tiles can be registered in TpLib since
    * that tests TPT tile GUIDs for duplicates.
    *
    * What this means in practice is that every time that the bundle is loaded
    * (usually as part of a TileFab) the GUIDs of the TPT tiles are unique.
    *
    * But what if you want to be able to locate a TPT tile from a TileFab or Bundle?
    * EG, enable a waypoint that happens to be in a section of Tilemap that's loaded
    * dynamically from a bundle?
    *
    * So here is where the "breadCrumbs" facility in TileFabUtil comes in to play.
    *
    * When you load a TileFab a registration (breadcrumb) is created. This includes a BundleGuidMap
    * for each Bundle that was loaded.
    *
    * This is actually a pre-deconstructed dictionary.
    *
    * So when you save a level you also save the registrations:
    * you can get a JSON-ized list of registrations using GetAssetRegistrationJson.
    *
    * This describes all the tilemap sections that were loaded: one entry per bundle.
    *
    * When reloading you pass the JSON-ized registration data back into TileFabUtil.RestoreFromRegistrationJson.
    *
    * This reconsitututes all of the TileFabs used prior to the last save as reflected in the stored Registrations.
    *
    * RestoreFromRegistrationJson also creates a mapping between the previous and new GUIDs using
    * TpZoneManagerLib.UpdateGuidLookup. This mapping allows looking-up a tile via
    * TpLib.GetTilePlusBaseFromGuidString
    * 
    */
    /// <summary>
    /// Stores the positions and TPT tile GUIDs for a Bundle.
    /// </summary>
    [Serializable]
    public class BundleGuidMap
    {
        /// <summary>
        /// Array of TPT tile GUIDs
        /// </summary>
        [SerializeField]
        public string[]     m_TileGuids;
        /// <summary>
        /// Array of TPT tile positions
        /// </summary>
        [SerializeField]
        public Vector3Int[] m_Positions;
        /// <summary>
        /// The asset GUID for the bundle
        /// </summary>
        [SerializeField]
        public string m_AssetGuid;
        /// <summary>
        /// The name of the originating asset
        /// </summary>
        public string m_AssetName;
        /// <summary>
        /// Reconstitute the Position-&gt;TileGuid map
        /// </summary>
        /// <value>The position to tile unique identifier map.</value>
        public Dictionary<Vector3Int, string> PosToTileGuidMap
        {
            get
            {
                var num  = m_TileGuids.Length;
                var dict = new Dictionary<Vector3Int, string>(num);
                for (var i = 0; i < m_TileGuids.Length; i++)
                    dict.Add(m_Positions[i], m_TileGuids[i]);
                return dict;
            }
        }

        /// <summary>
        /// Ctor - Deconstructs the input dictionary
        /// </summary>
        /// <param name="posToGuidMap">A map V3Int to string where V3Ints are the positions of all TPT tiles in the Bundle
        /// and the GUIDs are the TPT tile GUIDs</param>
        /// <param name="assetGuid">Bundle Asset GUID</param>
        /// <param name="assetName">Bundle asset name</param>
        public BundleGuidMap(Dictionary<Vector3Int, string> posToGuidMap, string assetGuid, string assetName)
        {
            m_TileGuids = posToGuidMap.Values.ToArray();
            m_Positions = posToGuidMap.Keys.ToArray();
            m_AssetGuid = assetGuid;
            m_AssetName = assetName;
        }
        
    }

    /// <summary>
    /// used for quick HashSet lookups to see if an asset has already been loaded using a particular offset.
    /// </summary>
    public class AssetGuidPositionHash
    {
        /// <summary>
        /// TPT tile GUID
        /// </summary>
        private readonly Guid       guid;
        /// <summary>
        /// TPT tile position
        /// </summary>
        private readonly Vector3Int position;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="guid">TPT tile GUID</param>
        /// <param name="position">TPT tile position</param>
        public AssetGuidPositionHash(Guid guid, Vector3Int position)
        {
            this.guid     = guid;
            this.position = position;
        }

        /// <summary>
        /// Equals
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        private bool Equals(AssetGuidPositionHash other)
        {
            return guid.Equals(other.guid) && position.Equals(other.position);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            return obj.GetType() == this.GetType() && Equals((AssetGuidPositionHash)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(guid, position);
        }
    }

    /// <summary>
    /// Specification for individual items in the "breadcrumbs" list.
    /// Info in here is used for TileFab loading/persistence and for Chunking.
    /// There is one of these for each TileFab loaded. The names may seem odd
    /// but they're short in order to keep the file size shorter
    /// </summary>
    [Serializable]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class ZoneReg
    {
        /// <summary>
        /// An index for this registration. An ascending #, zeroed when this TpZoneManager is initialized
        /// </summary>
        public ulong dex;
        /// <summary>
        /// A flag that this is region should not be deleted if moved out of camera range.
        /// Note that this is NOT set in the constructor.
        /// </summary>
        public bool imm;
        /// <summary>
        /// The BoundsInt from any of the loaded Bundles of the TileFab (they're all the same when a Fab is a Chunk)
        /// </summary>
        public BoundsInt lb;
        /// <summary>
        /// The GUID of the TileFab
        /// </summary>
        [SerializeField]
        public string g;
        /// <summary>
        /// Name of the asset (mostly useful for diagnostics)
        /// </summary>
        public string aNam;
        /// <summary>
        /// The offset applied
        /// </summary>
        [SerializeField]
        public Vector3Int offs;
        /// <summary>
        /// Denotes that this is a reserved zone.
        /// That means that the Zone is empty, but reserved.
        /// NONSERIALIZED
        /// </summary>
        [NonSerialized]
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        public bool m_Reserved;
        /// <summary>
        /// The locator RectInt for this instance.
        /// NONSERIALIZED
        /// </summary>
        [NonSerialized]
        public RectInt m_MyLocator;
        /// <summary>
        /// The rotation applied
        /// </summary>
        [SerializeField]
        public TpTileBundle.TilemapRotation rot;
        /// <summary>
        /// Position to GUID mapping.
        /// </summary>
        [SerializeField]
        public BundleGuidMap[] ptgm;
        /// <summary>
        /// Array of all prefabs spawned when TileFab is loaded.
        /// NOT SERIALIZED NOR SAVED. CAN BE NULL
        /// </summary>
        [NonSerialized]
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        public List<GameObject>? m_Prefabs;
        /// <summary>
        /// A spot for custom data you may wish to add when filtering.
        /// NOT SERIALIZED NOR SAVED
        /// </summary>
        [NonSerialized]
        public object? m_CustomData;

        /// <summary>
        /// Ctor
        /// </summary>
#pragma warning disable CS8618
        public ZoneReg()
            #pragma warning restore CS8618
        {
        }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="index">index of this instance</param>
        /// <param name="locator">the RectInt locator for this instance. Not serialized.</param>
        /// <param name="assetGuid">The GUID of the TileFab or Bundle asset</param>
        /// <param name="assetName">the asset's name</param>
        /// <param name="offset">the offset that used to place the Bundles</param>
        /// <param name="rotation">the rotation</param>
        /// <param name="posToGuidMaps">An array of tile position to GUID maps. For tilefabs, collect these during loading Bundles. For Bundles, supply directly</param>
        /// <param name="bundleGuids">Matching array of bundle GUIDs, in the same order as posToGuidMaps</param>
        /// <param name="bundleNames">Matching array of bundle names, in the same order as posToGuidMaps</param>
        /// <param name="largestBounds">The largest bounds found in the Bundles</param>
        /// <param name="spawnedPrefabs">List of prefabs spawned when the TileFab was loaded. Note: not serialized
        /// in the AssetRegistration class instance created herein. Can be used when TileFabs unloaded.</param>
        public ZoneReg(ulong                                      index,
                                 RectInt                          locator,
                                 string                           assetGuid,
                                 string                           assetName,
                                 Vector3Int                       offset,
                                 TpTileBundle.TilemapRotation     rotation,
                                 Dictionary<Vector3Int, string>[] posToGuidMaps,
                                 string[]                         bundleGuids,
                                 string[]                         bundleNames,
                                 BoundsInt                        largestBounds,
                                 List<GameObject>?                spawnedPrefabs
            )
        {
            dex         = index;
            m_MyLocator = locator;
            g           = assetGuid;
            rot         = rotation;
            lb          = largestBounds;
            offs        = offset;
            aNam        = assetName;
            m_Prefabs   = spawnedPrefabs;
            var numMaps = posToGuidMaps.Length;
            if (numMaps == 0)
            {
                ptgm = new BundleGuidMap[1];
                return;
            }

            ptgm = new BundleGuidMap [numMaps];
            for (var i = 0; i < numMaps; i++)
            {
                var item       = posToGuidMaps[i];
                var bundleGuid = bundleGuids[i];
                var bundleName = bundleNames[i];
                ptgm[i] = new BundleGuidMap(item,
                                            bundleGuid,
                                            bundleName);
            }
        }
        
        
        /// <summary>
        /// Alternate abbreviated Ctor, Do not use except in TileFabLib.LoadTileFabs
        /// </summary>
        /// <param name="index">index of this instance</param>
        /// <param name="locator">the RectInt locator for this instance. Not serialized.</param>
        /// <param name="offset">the offset : may be superfluous</param>
        /// <remarks>This is used to create a reserved zone. That means: don't write there but ok to delete.</remarks>
        #pragma warning disable CS8618
        internal ZoneReg(ulong                            index,
                         #pragma warning restore CS8618
                         RectInt                          locator,
                         Vector3Int                       offset
            )
        {
            dex         = index;
            m_MyLocator = locator;
            g           = string.Empty;
            rot         = TpTileBundle.TilemapRotation.Zero;
            lb          = new BoundsInt();
            offs        = offset;
            aNam        = string.Empty;
            m_Reserved  = true;
        }
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        /// <inheritdoc />
        public override string ToString()
        {
            return $"Zone Reg -> Index: {dex.ToString()} from a TileFab with GUID:{g} using Offset:{offs.ToString()} and Locator: {m_MyLocator.ToString()}, Rotation:{rot}";
        }
    }

    /// <summary>
    /// A wrapper for AssetRegistration saves
    /// </summary>
    [Serializable]
    public class LoadWrapper
    {
        /// <summary>
        /// asset registrations
        /// </summary>
        [FormerlySerializedAs("m_res")]
        [FormerlySerializedAs("m_LoadResultsArray")]
        [SerializeField]
        public ZoneReg[] m_Res;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="res">The resource.</param>
        public LoadWrapper(ZoneReg[] res)
        {
            m_Res = res;
        }
    }

    
    
        

}
