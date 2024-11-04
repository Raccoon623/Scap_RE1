// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 11-09-2022
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-31-2022
// ***********************************************************************
// <copyright file="TpPainterClasses.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Data structures for Tile+Painter</summary>
// ***********************************************************************
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

// ReSharper disable NonReadonlyMemberInGetHashCode
#nullable enable
namespace TilePlus.Editor.Painter 
{
    /// <summary>
    /// A data structure for items in the palette list (Center column),
    /// and Generically, Paintable Objects.
    /// </summary>
    [Serializable]
    internal class PaintableObject : IEquatable<PaintableObject>
    {
        /// <inheritdoc />
        public bool Equals(PaintableObject? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Equals(Palette, other.Palette) && Equals(Bundle, other.Bundle) && Equals(TileFab, other.TileFab) && ItemType == other.ItemType && ItemName == other.ItemName;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((PaintableObject)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(Palette, Bundle, TileFab, (int)ItemType, ItemName);
        }

        public static bool operator ==(PaintableObject? left, PaintableObject? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(PaintableObject? left, PaintableObject? right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Reference to a Palette, if appropriate
        /// </summary>
        public GameObject? Palette { get; }
        /// <summary>
        /// Reference to a TpTileBundle asset, if appropriate
        /// </summary>
        public TpTileBundle? Bundle { get; }
        /// <summary>
        /// Reference to a TileFab, if appropriate.
        /// </summary>
        public TpTileFab? TileFab { get; }
        /// <summary>
        /// What's in this instance? 
        /// </summary>
        public TpPaletteListItemType ItemType { get; }
        /// <summary>
        /// Name of this item
        /// </summary>
        public string ItemName { get; }

        [SerializeField]
        // ReSharper disable once InconsistentNaming
        private int count;
        
        /// <summary>
        /// Get a count appropriate for the contents.
        /// Note that for Palettes, -1 can be returned by GetUsedTilesForPalette. That means
        /// that the TpNoPaint component was added to the Palette because it was very large
        /// and GetUsedTilesCount blocks execution.
        /// </summary>
        public int Count
        {
            get
            {
                switch (ItemType)
                {
                    case TpPaletteListItemType.Favorites:
                        return TilePlusPainterFavorites.instance.FavoritesListSize;
                    case TpPaletteListItemType.Bundle:
                        return Bundle != null ? Bundle.m_UnityTiles.Count + Bundle.m_TilePlusTiles.Count : 0;
                    case TpPaletteListItemType.TileFab:
                        return TileFab != null  ?  TileFab.m_TileAssets!.Count : 0;
                    case TpPaletteListItemType.Palette:
                    {
                        return Palette == null
                                   ? 0
                                   : TpPainterScanners.instance.GetUsedTilesForPalette(Palette);
                    }
                    default:
                        return count;
                }
            }
            private set => count = value;
        }

        /// <summary>
        /// Gets the inspectable asset for this PaintableObject.
        /// </summary>
        /// <value>The inspectable asset.</value>
        
        public Object? InspectableAsset
        {
            get
            {
                switch (ItemType)
                {
                    case TpPaletteListItemType.Bundle:
                        return Bundle;
                    case TpPaletteListItemType.Favorites:
                    case TpPaletteListItemType.None:
                        return null;
                    case TpPaletteListItemType.TileFab:
                        return TileFab;
                    case TpPaletteListItemType.Palette:
                        return Palette;
                        
                    default:
                        return null;
                }
            }
        }

        /// <summary>
        /// Ctor when this instance is describing a Unity palette
        /// </summary>
        /// <param name="palette">a Palette ref</param>
        public PaintableObject(GameObject palette)
        {
            Palette  = palette;
            ItemType = TpPaletteListItemType.Palette;
            ItemName = palette.name;
            Count = -1; 
        }

        /// <summary>
        /// Ctor when this instance is describing a TpTileBundle asset
        /// </summary>
        /// <param name="bundle">a TpTileBundle asset ref</param>
        public PaintableObject(TpTileBundle bundle)
        {
            Bundle    = bundle;
            ItemType = TpPaletteListItemType.Bundle;
            ItemName = bundle.name;
            Count    = bundle.m_UnityTiles.Count + bundle.m_TilePlusTiles.Count;
        }

        /// <summary>
        /// Ctor when this instance is describing the Favorites List List.
        /// </summary>
        public PaintableObject()
        {
            
            ItemType    = TpPaletteListItemType.Favorites;
            ItemName    = "Favorites";
            Count       = -1;
        }

        /// <summary>
        /// Ctor when this instance is describing a TileFab asset
        /// </summary>
        /// <param name="tileFab">TileFab asset ref</param>
        public PaintableObject(TpTileFab tileFab)
        {
            TileFab  = tileFab;
            ItemType = TpPaletteListItemType.TileFab;
            ItemName = tileFab.name;
            Count    = tileFab.m_TileAssets!.Count;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Palette List Item [{ItemName}] : Variety=[{ItemType.ToString()}], Count = {Count.ToString()} ";
        }
    }

    /// <summary>
    /// describes a tilemap we can paint on
    /// </summary>
    [Serializable]
    internal class PaintableMap
    {
        /// <summary>
        /// The tilemap that gets painted on
        /// </summary>
        public Tilemap? TargetTilemap { get; }
        /// <summary>
        /// The Grid Layout of the paintable tilemap
        /// </summary>
        public GridLayout? TargetTilemapGridLayout { get; }
        //the Transform of the paintable tilemap
        // ReSharper disable once MemberCanBePrivate.Global
        public Transform? TargetTilemapTransform { get; }
        /// <summary>
        /// The Transform of the parent grid.
        /// </summary>
        public Transform? ParentGridTransform { get; }
        /// <summary>
        /// The name of the tilemap
        /// </summary>
        public string? Name { get; }

        private bool valid;
        /// <summary>
        /// is this data strucure valid?
        /// </summary>
        public bool Valid => valid && TargetTilemap != null; 
        
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="map">tilemap</param>
        public PaintableMap(Tilemap? map)
        {
            TargetTilemap = map;
            if (map == null)
            {
                Debug.LogError("Passed NULL tilemap to PaintableMap ctor");
                return;
            }

            Name                    = map.name;
            TargetTilemapGridLayout = map.GetComponent<GridLayout>();
            TargetTilemapTransform  = map.transform;
            ParentGridTransform     = GetParentGridTransform(TargetTilemapTransform);

            if (ParentGridTransform == null)
            {
                Debug.LogError("Could not find tilemap's parent grid in PaintableMap ctor");
                return;
            }

            if (TargetTilemapTransform != null && TargetTilemapGridLayout != null)
                valid = true;
            else //note Valid defaults to false.
                Debug.LogError("Invalid data in PaintableMap ctor");

            //<summary>Local Method: get the transform of the parent grid.</summary>
            Transform? GetParentGridTransform(Transform current)
            {
                //perhaps the current transform has a Grid
                if (current.TryGetComponent<Grid>(out var output))
                    return output.transform;
                //otherwise, look at its parent. If == null then
                //current is a root transform.
                while ((current = current.parent) != null) 
                {
                    // ReSharper disable once RedundantTypeArgumentsOfMethod
                    if (current.TryGetComponent<Grid>(out output))
                        return output.transform;
                }
                return null;
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Name??"Null";
        }
    }
    

    /// <summary>
    /// Data used by the UIElements list. Describes a Tilemap
    /// </summary>
    [Serializable]
    internal class TilemapData
    {
        /// <summary>
        /// Parent tilemap for tile
        /// </summary>
        public Tilemap TargetMap { get; }

        /// <summary>
        /// Is this tile or tilemap part of a prefab?
        /// </summary>
        public bool InPrefab { get; }
        
        /// <summary>
        /// Is this tile or tilemap being shown in a prefab stage
        /// </summary>
        public bool InPrefabStage { get; }

        /// <summary>
        /// The tilemap's parent scene
        /// </summary>
        public Scene ParentSceneOfMap { get; }

        /// <summary>
        /// TRUE if this map has TPT tiles in it.
        /// </summary>
        public bool HasTptTilesInMap => TpLib.IsTilemapRegistered(TargetMap);
        
        private TilemapRenderer tmapRenderer;
        private bool            validRenderer;

        /// <summary>
        /// Renderer Info
        /// </summary>
        public string RendererInfo =>
            validRenderer
                ? $"[{tmapRenderer.sortingLayerName}/{tmapRenderer.sortingOrder.ToString()}] "
                : "[?/?] ";


        /// <summary>
        /// Constructor when desiring to display info for a tilemap
        /// </summary>
        /// <param name="mapRef">the map</param>
        /// <param name="inPrefab">is it in a prefab?</param>
        /// <param name = "inPrefabStage" >is it in a prefab STAGE (Editor)</param>
        public TilemapData(Tilemap mapRef, bool inPrefab, bool inPrefabStage)
        {
            TargetMap        = mapRef;
            InPrefab         = inPrefab;
            InPrefabStage    = inPrefabStage;
            ParentSceneOfMap = mapRef.gameObject.scene;

            validRenderer = mapRef.TryGetComponent(out tmapRenderer);

        }

    }
    
     /// <summary>
    /// Contains information about what has
    /// changed in the TpLib tile data. Used
    /// in TilePlusPainterWindow event cache.
    /// -POOLED-
    /// </summary>
    internal class DbChangeArgs
    {
        /// <summary>
        /// True for an addition, false for a deletion
        /// </summary>
        public TpLibChangeType m_ChangeType;
        /// <summary>
        /// Is this part of a group? 
        /// </summary>
        public bool m_IsPartOfGroup;
        /// <summary>
        /// The Tile location
        /// </summary>
        public  Vector3Int m_GridPosition = TilePlusBase.ImpossibleGridPosition;
        /// <summary>
        /// The tilemap where changes occurred
        /// </summary>
        public Tilemap? m_Tilemap;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="changeType">Value of ChangeType enum</param>
        /// <param name="isPartOfGroup">part of group or a singleton</param>
        /// <param name="gridPosition">The position affected</param>
        /// <param name="map">The tilemap affected: NOTE can be null, eg when entire tilemaps are deleted.</param>
        public void Set(TpLibChangeType changeType, bool isPartOfGroup, Vector3Int gridPosition, Tilemap? map)
        {
            m_ChangeType = changeType;
            m_IsPartOfGroup = isPartOfGroup;
            m_GridPosition = gridPosition;
            m_Tilemap      = map;
        }

        public void Reset()
        {
            m_Tilemap       = null;
            m_IsPartOfGroup = false;
            m_GridPosition  = TilePlusBase.ImpossibleGridPosition;
        }

        public DbChangeArgs() {}

    }

    
}
