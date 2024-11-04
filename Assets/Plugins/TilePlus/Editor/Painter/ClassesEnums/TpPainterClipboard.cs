// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 11-09-2022
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 01-22-2024
// ***********************************************************************
// <copyright file="TppClipboardItem.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEngine.GridBrushBase;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using static TilePlus.Editor.TpIconLib;


#nullable enable

namespace TilePlus.Editor.Painter
{
     /// <summary>
    /// Describes what type of thing to display after a palette or tile is selected.
    /// Also used as list items in the brush inspector.
    /// </summary>
    ///<remarks>This object is referred to as the CLIPBOARD in most of the docs.
    /// See TilePlusPainterWindow - it has a Clipboard property. Instances of
    /// TppClipboard item are written to that property, which automatically
    /// clones the input and saves it for any possible restore operations.</remarks>;

    [Serializable]
    internal class TpPainterClipboard
    {
        #region enumsAndConstants        
        /// <summary>
        /// What type of Object is in the Clipboard?
        /// </summary>
        public enum Variety
        {
            /// <summary>
            /// This instance is a Unity or TPT tile
            /// </summary>
            TileItem,
            /// <summary>
            /// This instance is a TpTileBundle asset
            /// </summary>
            BundleItem,
            /// <summary>
            /// This instance is a TileFabItem
            /// </summary>
            TileFabItem, 
            /// <summary>
            /// Unity or TPT tiles, but a group of them rather than one.
            /// </summary>
            MultipleTilesItem,
            /// <summary>
            /// A prefab. ONLY used when a Bundle is displayed in a List.
            /// </summary>
            PrefabItem,
            /// <summary>
            /// Represents an empty field. 
            /// </summary>
            EmptyItem 
        }

        private enum CellOps
        {
            RotateCw,
            RotateCcw,
            FlipX,
            FlipY
        }

        /// <summary>
        /// What is the Pivot location of a multiple-select item
        /// </summary>
        public enum PivotLocation
        {
            Original,
            LeftBottom,
            LeftTop,
            RightTop,
            RightBottom
        }
        
        #endregion
        
        #region publicProperties
        /// <summary>
        /// Get target as Object. CLIPBOARD OBJECT
        /// </summary>
        public Object? Target
        {
            get
            {
                switch (ItemVariety)
                {
                    case Variety.TileItem:
                        return Tile;
                    case Variety.BundleItem:
                        return Bundle;
                    case Variety.PrefabItem:
                        return Prefab;
                    case Variety.TileFabItem:
                        return TileFab;
                    case Variety.MultipleTilesItem:
                        var so = ScriptableObject.CreateInstance<TileCellsWrapper>();
                        so.m_MultipleTilesSelectionBoundsInt = MultipleTilesSelectionBoundsInt;
                        so.m_Bounds                          = BoundsInt;
                        so.Cells                             = Cells!;
                        so.Icon                              = Icon;
                        so.Pivot                             = Pivot;
                        return so;
                    default:
                    case Variety.EmptyItem:
                        return null;
                }
            }
        }

        /// <summary>
        /// Get a string for display for the current Variety
        /// </summary>
        /// <value>string</value>
        public string TargetName
        {
            get
            {
                return ItemVariety switch
                       {
                           Variety.TileItem when Tile != null           => Tile.name,
                           Variety.BundleItem when Bundle != null       => Bundle.name,
                           Variety.PrefabItem when Prefab != null       => Prefab.name,
                           Variety.TileFabItem when TileFab != null     => TileFab.name,
                           Variety.MultipleTilesItem when Cells != null => $"Selection: size={Cells.Length}",
                           Variety.EmptyItem                            => "----",
                           _                                            => "Invalid Clipboard"
                       };
            }
        }
        
        
        /// <summary>
        /// A Tile reference. May be either a Unity or TPT tile
        /// </summary>
        public TileBase? Tile { get; private set; }  
        /// <summary>
        /// the Type of the tile. Only valid when Tile != null. In
        /// that case the type will always be TileBase
        /// </summary>
        public Type? TileType { get; private set; } 
        /// <summary>
        /// The position, only used when variety is TileItem. 
        /// </summary>
        public Vector3Int Position      { get; }
        /// <summary>
        ///  An array of tile cells - used only when type is MultipleTilesItem. 
        /// </summary>
        public TileCell[]? Cells { get; private set; }
        /// <summary>
        /// The bounds when type is MultipleTilesItem. Pos is 0.
        /// </summary>
        public BoundsInt BoundsInt {get; private set; }
        /// <summary>
        /// A GUID that's maintained the same through all clones.
        /// </summary>
        public Guid? ClipboardGuid { get; private set; } 
        /// <summary>
        /// DO NOT USE: BACKING FIELD.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        [SerializeField]
        private BoundsInt m_Backing_MultipleTilesSelectionBoundsInt;
        /// <summary>
        /// When the variety is MultipleTiles then this is the
        /// orginal selection BoundsInt prior to adjustment.
        /// </summary>
        public BoundsInt MultipleTilesSelectionBoundsInt
        {
            get
            {
                if (m_Backing_MultipleTilesSelectionBoundsInt.size.z == 0)
                    m_Backing_MultipleTilesSelectionBoundsInt.size = new Vector3Int(BoundsInt.size.x, BoundsInt.size.y, 1);
                return m_Backing_MultipleTilesSelectionBoundsInt;
            }
            set => m_Backing_MultipleTilesSelectionBoundsInt = value;
        }
        /// <summary>
        /// When the variety is MultipleTiles then this is the pivot
        /// </summary>
        public Vector3Int Pivot { get; private set; }
        /// <summary>
        /// the location of the Cells' pivot when item is multiple-tiles
        /// </summary>
        public PivotLocation CellsPivotLocation { get; private set; } = PivotLocation.Original;
        /// <summary>
        /// The location of the Pivot when item is multiple-tiles
        /// </summary>
        public PivotLocation PivotPivotLocation { get; private set; } = PivotLocation.Original;
        /// <summary>
        /// A prefab ref - ONLY when a Bundle is displayed in a list rather than as an atomic entity.
        /// </summary>
        public GameObject? Prefab { get; private set; }
        /// <summary>
        /// What sort of thing is this?
        /// </summary>
        public Variety ItemVariety { get; private set; }
        /// <summary>
        /// Is the transform modified? For TileItem variety only.
        /// </summary>
        /// <remarks>Note that this can only be SET and not cleared.
        /// Clearing only happens internal to the instance by affecting m_TransformModified.</remarks>
        public bool TransformModified
        {
            get => m_TransformModified;
            private set
            {
                m_TransformModified |= value;  //note that this means it can't be reset
                if (value && TilePlusPainterWindow.RawInstance != null)
                    TilePlusPainterWindow.RawInstance.TabBar.TabBarTransformModified(true, this); 
            }
        }
        /// <summary>
        /// True if this Clipbd instance was created from a Bundle or some other external item. Flags NOT to apply default tile transforms.
        /// </summary>
        public bool FromAConversion { get; private set; }
        /// <summary>
        /// When a mods happen but it's cells (a block of tiles) vs a single tile, set this true.
        /// </summary>
        public bool CellsModified { get; private set; }
        /// <summary>
        /// Returns true if Cells or transform were modified.
        /// </summary>
        public bool AnyModifications => CellsModified || TransformModified;
        /// <summary>
        /// returns true if the Color was modified
        /// </summary>
        public bool ColorModified
        {
            get => m_ColorModified;
            private set
            {
                m_ColorModified |= value;  //ie, once set, cannot be reset.
                if (value && TilePlusPainterWindow.RawInstance != null)
                    TilePlusPainterWindow.RawInstance.TabBar.TabBarTransformModified(true, this); 
            }
        }
        
        /// <summary>
        /// TRUE if any of the tile(s) in the Clipbd have the lock-color flag set.
        /// </summary>
        public bool HasColorLockedTiles { get; private set; }
        /// <summary>
        /// TRUE if any of the tile(s) in the Clipbd have the lock-transform flag set.
        /// </summary>
        public bool HasTransformLockedTiles { get; private set; }

        /// <summary>
        /// Returns true if this item isn't a Tile or a Prefab.
        /// </summary>
        public bool VarietyCantModifyTransform => ItemVariety is not (Variety.TileItem or Variety.PrefabItem or Variety.MultipleTilesItem) || FromAConversion;
        /// <summary>
        /// returns true if this item can't be mass-painted by drawing out a marquee
        /// </summary>
        public bool VarietyCanBeMassPainted => (ItemVariety is Variety.TileItem or Variety.PrefabItem);
        /// <summary>
        /// The index of this item
        /// </summary>
        public uint ItemIndex { get; private set; }
        /// <summary>
        /// Icon for this set of tile cells; valid only when variety is MultipleTilesItem.
        /// Will be null until populated 
        /// </summary>
        public Sprite? Icon
        {
            get => ItemVariety == Variety.MultipleTilesItem ? m_Icon : null;
            set => m_Icon = value;
        }

        /// <summary>
        /// Is this data instance valid?
        /// </summary>
        public bool Valid
        {
            get
            {
                switch (ItemVariety)
                {
                    case Variety.EmptyItem:
                        return false;
                    case Variety.BundleItem:
                        return Bundle != null && Bundle.Valid;
                    case Variety.TileFabItem:
                        return TileFab != null;
                    case Variety.MultipleTilesItem:
                        return Cells != null && Cells.Length != 0;
                    case Variety.PrefabItem:
                        return Prefab != null;
                }

                //OK, it is a tile. 
                //here, test for validity of tile items
                if (Tile == null) //tile being null is fail for any source
                    return false;

                //non-null tile but invalid grid position means Favorites List is where this tile came from.
                if (Position == TilePlusBase.ImpossibleGridPosition)  
                    return true;

                //any other source: tile or map can't be null and position needs to be valid.
                //short circuit the tilemap test for not null if the tile spec was from a bundle displayed as a palette (Paint mode only)
                return (SourceTilemap != null || IsFromBundleAsPalette) && Position != TilePlusBase.ImpossibleGridPosition;
            }
        }
        
        /// <summary>
        /// Was this a picked tile?
        /// </summary>
        public bool WasPickedTile => m_WasPickedTile;
        /// <summary>
        /// What is the type of pick that was made, if any.
        /// </summary>
        public TpPickedTileType PickType
        {
            get
            {
                return ItemVariety switch
                       {
                           Variety.BundleItem        => TpPickedTileType.Bundle,
                           Variety.EmptyItem         => TpPickedTileType.None,
                           Variety.PrefabItem        => TpPickedTileType.Prefab,
                           Variety.TileItem          => TpPickedTileType.Tile,
                           Variety.MultipleTilesItem => TpPickedTileType.Multiple,
                           Variety.TileFabItem       => TpPickedTileType.TileFab,
                           _                         => TpPickedTileType.None
                       };
            }
        }

        /// <summary>
        /// Instance Id of the contents, if applicable
        /// </summary>
        public int Id { get; }
        /// <summary>
        /// Get a TpTileBundle instance 
        /// </summary>
        public TpTileBundle? Bundle { get; private set; }
        /// <summary>
        /// Get a TpTileFab instance
        /// </summary>
        public TpTileFab? TileFab { get; private set; }
        /// <summary>
        /// Get the tilemap for the contents, if applicable 
        /// </summary>
        public Tilemap? SourceTilemap { get; private set; }
        
        /// <summary>
        /// Is this a Tile or subclass? This will also be true for when IsTilePlusBase is true. 
        /// </summary>
        public bool IsTile { get; private set; }
        /// <summary>
        /// Is this a TileBase class tile? Only true if NOT a Tile or TilePlusBase
        /// </summary>
        public bool IsTileBase { get; private set; }
        /// <summary>
        /// Is this a TilePlusBase class. NOT true if TileBase class.
        /// </summary>
        public bool IsTilePlusBase { get; private set; }
        /// <summary>
        /// If the tile is TilePlusBase or derived type it'll have the ITilePlus interface
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public ITilePlus? ITilePlusInstance { get; private set; }
        /// <summary>
        /// this is a TpBundleTile tile, which requires special handling in Preview.
        /// </summary>
        public bool IsTpBundleTile { get; private set; }
        /// <summary>
        /// Is this a clone of a TilePlusBase
        /// </summary>
        public bool IsClonedTilePlusBase { get; private set;}
        /// <summary>
        /// TRUE when this is NOT a TilePlus tile
        /// </summary>
        public bool IsNotTilePlusBase { get; private set;}
        /// <summary>
        /// TRUE when this is a Prefab item
        /// </summary>
        public bool IsPrefab => ItemVariety == Variety.PrefabItem;
        /// <summary>
        /// True when this is a multiple tiles selection.
        /// </summary>
        public bool IsMultiple => ItemVariety == Variety.MultipleTilesItem;
        /// <summary>
        /// True when this is a TpTileBundle
        /// </summary>
        public bool IsBundle => ItemVariety == Variety.BundleItem;
        /// <summary>
        /// True when this is a TpTileFab
        /// </summary>
        public bool IsTileFab => ItemVariety == Variety.TileFabItem;
        /// <summary>
        /// True when this is an Empty item.
        /// </summary>
        public bool IsEmpty => ItemVariety == Variety.EmptyItem;
        /// <summary>
        /// True when this IS NOT an Empty item.
        /// </summary>
        public bool IsNotEmpty => ItemVariety != Variety.EmptyItem;
        /// <summary>
        /// TRUE if this item is from the Favorites list.
        /// </summary>
        public bool IsFromFavorites { get; set; }
        /// <summary>
        /// TRUE if this item is from a tile that was part of a bundle (used only in PAINT mode)
        /// </summary>
        public bool IsFromBundleAsPalette { get; private set;}
        /// <summary>
        /// TRUE if the tile is a TPT or Tile AND the gameObject
        /// field isn't null. Note that this is NOT the
        /// instantiated GameObject that may be instantiated
        /// by the tilemap after calling GetTileData.
        /// </summary>
        public bool HasGameObject { get; private set;}

        /// <summary>
        /// If a default or ALT+V M OD is added to this clipbd instance this is the EditActions for that m od.
        /// </summary>
        public EditActions ModifierEditActions { get; private set; } = EditActions.None;
        
        // ReSharper disable once InconsistentNaming
        /// <summary>
        /// The transform for this Object, if appropriate
        /// </summary>
        public Matrix4x4 transform { get; private set; } = Matrix4x4.identity;
        /// <summary>
        /// The color for this Object, if appropriate.
        /// </summary>
        public Color AColor { get; private set; } = Color.white;
        /// <summary>
        /// This prop gets an offset to mouse position to apply when painting a multiple-tiles item.
        /// The offset is different depending on changes to rotation ops and pivot changes.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public Vector3Int OffsetModifier
        {
            get
            {
                Vector3Int cellPivotOffset = CellsPivotLocation switch
                 {
                     PivotLocation.LeftBottom or PivotLocation.Original => Vector3Int.zero,
                     PivotLocation.LeftTop                              => new Vector3Int(0,-BoundsInt.size.y + 1, 0),
                     PivotLocation.RightTop                             => new Vector3Int(-BoundsInt.size.x + 1, -BoundsInt.size.y + 1, 0),
                     PivotLocation.RightBottom                          => new Vector3Int(-BoundsInt.size.x + 1, 0,0),
                     _                                                  => throw new ArgumentOutOfRangeException()
                 };
                return cellPivotOffset + Pivot;
            }
        }
        
        /// <summary>
        /// Get or create the GridBrush instance
        /// </summary>
        private static GridBrush? GridBrushInternal
        {
            get
            {
                if (s_GridBrushInternal == null)
                    s_GridBrushInternal = ScriptableObject.CreateInstance<TilePlusBrush>();
                return s_GridBrushInternal;
            }
        }

        /// <summary>
        /// For diags
        /// </summary>
        public static ulong RecloneCount => s_RecloneCount;


       

        #endregion
        
        #region privateFields
        /// <summary>
        /// An Icon for this item, if appropriate
        /// </summary>
        [SerializeField]
        private Sprite? m_Icon;
        /// <summary>
        /// An index for each item is obtained from here. Note that it's static.
        /// </summary>
        private static uint s_Index;
        /// <summary>
        /// Indicates that the transform has been modified
        /// </summary>
        [SerializeField]
        private bool m_TransformModified;
        /// <summary>
        /// Color has been modified.
        /// </summary>
        [SerializeField]
        private bool m_ColorModified;
        [SerializeField]
        private bool m_WasPickedTile;
        /// <summary>
        /// a GridBrush is instantiated if necc and a ref held here.
        /// </summary>
        private static TilePlusBrush? s_GridBrushInternal;
        /// <summary>
        /// for diags
        /// </summary>
        private static ulong s_RecloneCount;
        /// <summary>
        /// internal state.
        /// </summary>
        private bool isRecloning;
        /// <summary>
        /// Set true after array size mismatch.
        /// </summary>
        private bool gridBrushInvalid;
        #endregion
        
        #region Ctors
        /// <summary>
        /// A standard tile item
        /// </summary>
        /// <param name="aTile"></param>
        /// <param name="aPosition">Tile position : if ImpossibleGridPosition then source can be null</param>
        /// <param name = "source" >Source tilemap or null for Favorites List (for example)</param>
        /// <param name="wasPickedTile">was this a picked tile?</param>
        /// <param name = "isFromBundleAsPalette" >set true if this is from a bundle being shown as a palette.</param>
        /// <remarks>If aPosition is ImpossibleGridPosition then this tile is treated as coming from the Favorites List
        /// list, where the tilemap source isn't available.</remarks>
        public TpPainterClipboard(TileBase? aTile, Vector3Int aPosition, Tilemap? source, bool wasPickedTile = false, bool isFromBundleAsPalette = false)
        {
            if(!isRecloning)
                ClipboardGuid         = Guid.NewGuid();
            ItemIndex             = s_Index++;
            IsFromBundleAsPalette = isFromBundleAsPalette;
            ResetBrushIfExists();
            
            switch (aPosition == TilePlusBase.ImpossibleGridPosition) //this pos means its from Favorites List stack.
            {
                case false when source == null && !isFromBundleAsPalette:
                    TpLib.TpLogError("NULL tilemap passed to TileTargetData Ctor");
                    break;
                case true:
                    IsFromFavorites = true;
                    wasPickedTile = false; //ensures use of tile's transform for items from Favorites List Stack.
                    break;
            }

            Tile          = aTile;

            if (aTile != null && (source != null || IsFromFavorites || isFromBundleAsPalette))
            {
                Id       = aTile.GetInstanceID();
                TileType = aTile.GetType();
                
                // ReSharper disable once ConvertIfStatementToSwitchStatement
                // ReSharper disable once Unity.NoNullPatternMatching
                if (aTile is ITilePlus itp)
                {
                    IsClonedTilePlusBase = itp.IsClone; 
                    IsTilePlusBase       = true;
                    ITilePlusInstance    = itp;
                    IsTile               = true;
                    HasGameObject        = ((Tile)aTile).gameObject != null;
                    transform            = wasPickedTile && source!=null ? source.GetTransformMatrix(aPosition) : (((aTile as Tile)!)).transform;
                    #pragma warning disable CS8602 // Dereference of a possibly null reference.
                    AColor = wasPickedTile && source!=null ? source.GetColor(aPosition) : (((aTile as Tile))).color;
                    #pragma warning restore CS8602 // Dereference of a possibly null reference.
                    // ReSharper disable once Unity.NoNullPatternMatching
                    if (aTile is Tile tileForFlags)
                    {
                        HasColorLockedTiles     = (tileForFlags.flags & TileFlags.LockColor) != 0;
                        HasTransformLockedTiles = (tileForFlags.flags & TileFlags.LockTransform) != 0;
                    }
                }
                // ReSharper disable once Unity.NoNullPatternMatching
                else if (aTile is Tile tile)
                {
                    IsTile                  = true;
                    IsNotTilePlusBase       = true;
                    HasGameObject           = tile.gameObject != null;
                    transform               = wasPickedTile && source!= null ? source.GetTransformMatrix(aPosition) :  tile.transform;
                    AColor                  = wasPickedTile && source!=null ? source.GetColor(aPosition) : tile.color;
                    // ReSharper disable once Unity.NoNullPatternMatching
                    IsTpBundleTile          = tile is TpBundleTile;
                    HasColorLockedTiles     = (tile.flags & TileFlags.LockColor) != 0;
                    HasTransformLockedTiles = (tile.flags & TileFlags.LockTransform) != 0;
                }
                else
                {
                    IsTileBase        = true; //note TileBase has no transform NOR color, sprite etc.
                    IsNotTilePlusBase = true;
                }
            }
            else
                TileType = typeof(TileBase);
            
            Position          = aPosition;
            m_WasPickedTile     = wasPickedTile;
            SourceTilemap     = source;
            ItemVariety       = Variety.TileItem;
            var t = TpPainterModifiers.instance;
            if (t == null)
                return;
            var tileTransformWrapper = t.TilesDefault;
            if (tileTransformWrapper == null)
                return;
            if(tileTransformWrapper.AffectsTransform)
            {
                transform           = tileTransformWrapper.m_Matrix;
                m_TransformModified = true;
            }

            if (tileTransformWrapper.AffectsColor)
            {
                AColor = tileTransformWrapper.m_Color;
                m_ColorModified = true;
            }
        }

        /// <summary>
        /// Create a new Clipboard instance from a Bundle
        /// </summary>
        /// <param name="bundle">The source bundle.</param>

        public TpPainterClipboard(TpTileBundle? bundle)
        {
            if (bundle == null)
            {
                if(TpLibEditor.Errors)
                    Debug.LogError("Null Bundle passed to Clipboard ctor!");
                return;
            }

            if(!isRecloning)
                ClipboardGuid = Guid.NewGuid();
            ResetBrushIfExists();
            ItemIndex   = s_Index++;
            Bundle      = bundle;
            Icon        = bundle.m_Icon;
            ItemVariety = Variety.BundleItem;
            transform   = Matrix4x4.identity;
        }

        /// <summary>
        /// Create a new Clipboard instance from a TileFab
        /// </summary>
        /// <param name="fab">The source TileFab.</param>
        public TpPainterClipboard(TpTileFab? fab)
        {
            if (fab == null)
            {
                if(TpLibEditor.Errors)
                    Debug.LogError("Null Tilefab passed to Clipboard ctor!");
                return;
            }
            if(!isRecloning)
                ClipboardGuid = Guid.NewGuid();

            ResetBrushIfExists();
            ItemIndex   = s_Index++;
            TileFab     = fab;
            m_Icon      = fab.m_Icon;
            ItemVariety = Variety.TileFabItem;
            transform   = Matrix4x4.identity;
        }

        /// <summary>
        /// Create a new Clipboard instance as EMPTY item
        /// </summary>
        public TpPainterClipboard()
        {
            if(!isRecloning)
                ClipboardGuid = Guid.NewGuid();

            ResetBrushIfExists();
            ItemIndex = s_Index++;
            ItemVariety       = Variety.EmptyItem;
            transform = Matrix4x4.identity;
        }

        /// <summary>
        /// Memberwise clone of this object.
        /// </summary>
        /// <returns></returns>
        public TpPainterClipboard CloneInstance()
        {
            TpPainterClipboard cbItem;
            isRecloning = true;
            switch (ItemVariety)
            {
                case Variety.TileItem:
                    cbItem = new TpPainterClipboard(Tile, Position, SourceTilemap, WasPickedTile,IsFromBundleAsPalette);
                    break;
                case Variety.BundleItem:
                    cbItem                 = new TpPainterClipboard(Bundle);
                    break;
                case Variety.PrefabItem when Prefab != null:
                    cbItem                 = new TpPainterClipboard(Prefab);
                    break;
                case Variety.TileFabItem:
                    cbItem = new TpPainterClipboard(TileFab);
                    break;
                case Variety.MultipleTilesItem when Cells != null && Cells.Length != 0:
                    var cels = new TileCell[Cells.Length];
                    for (var i = 0; i < Cells.Length; i++)
                        cels[i] = new TileCell(Cells[i]);
                        
                    //The copying is important to ensure that the new clipboard instance
                    //won't ref the same cells, which isn't what we want.
                    //otherwise, clone everything else, even value types (which may be redundant but could be boxed items).
                    cbItem = new TpPainterClipboard(cels, new BoundsInt(BoundsInt.position,BoundsInt.size), 
                                                new BoundsInt(MultipleTilesSelectionBoundsInt.position,MultipleTilesSelectionBoundsInt.size), 
                                                WasPickedTile, FromAConversion);
                    break;
                default:
                case Variety.EmptyItem:
                    cbItem = new TpPainterClipboard();
                    break;
            }

            isRecloning            = false;
            cbItem.IsFromFavorites = IsFromFavorites;
            cbItem.ClipboardGuid   = ClipboardGuid;
            s_RecloneCount++;
            return cbItem;
        }

        /// <summary>
        /// Create a multiple-tiles item from the Favorites TileCellWrapper S.O. instances
        /// </summary>
        /// <param name="wrapper"></param>
        public TpPainterClipboard(TileCellsWrapper wrapper)
        {
            if(!isRecloning)
                ClipboardGuid = Guid.NewGuid();

            ResetBrushIfExists();
            ItemIndex                       = s_Index++;
            ItemVariety                     = Variety.MultipleTilesItem;
            if (wrapper.Cells == null)
                Cells = null;
            else
            {
                //important to copy the cells since we don't want to affect the source.
                Cells = new TileCell[wrapper.Cells.Length];
                Array.Copy(wrapper.Cells,Cells,wrapper.Cells.Length);
            }

            CellsModified = wrapper.m_CellsModified;
            
            var tmpBounds = wrapper.m_Bounds;
            tmpBounds.position                        = Vector3Int.zero;                                       //ensure that the position of this bounds is zero.
            tmpBounds.size                            = new Vector3Int(tmpBounds.size.x, tmpBounds.size.y, 1); //ensure size.z = 1
            BoundsInt                               = tmpBounds;
            tmpBounds                                 = wrapper.m_MultipleTilesSelectionBoundsInt;
            tmpBounds.size                            = new Vector3Int(tmpBounds.size.x, tmpBounds.size.y, 1); //ensure size.z = 1
            MultipleTilesSelectionBoundsInt         = tmpBounds;
            m_WasPickedTile                           = false;
            FromAConversion                           = false;
            Icon                                      = wrapper.Icon;
            Pivot                                     = wrapper.Pivot;
            CellsPivotLocation                        = PivotLocation.Original;
            SetupGridBrush();
        }

        /// <summary>
        /// Create a multiple-tiles item.
        /// </summary>
        /// <param name="cells">Array of TileCells</param>
        /// <param name="bounds">Zero-based (.position == 0) bounds for array position info. Note that .position forced to zero.</param>
        /// <param name="selectionBounds">Bounds of the selection: original bounds from the source</param>
        /// <param name="picked">Was this a pick?</param>
        /// <param name = "fromConversion" >This ttd data comes from somewhere else, eg conversion from a Bundle. This flags NOT to apply default transform mods.</param>
        public TpPainterClipboard(TileCell[]? cells, BoundsInt bounds, BoundsInt selectionBounds, 
                                bool       picked         = false,
                                bool       fromConversion = false)
        {
            if(!isRecloning)
                ClipboardGuid = Guid.NewGuid();

            ResetBrushIfExists();
            bounds.size              = new Vector3Int(bounds.size.x, bounds.size.y, 1);
            bounds.position          = Vector3Int.zero; //ensure that the position of this bounds is zero.
            selectionBounds.size     = new Vector3Int(selectionBounds.size.x,     selectionBounds.size.y,     1);
            selectionBounds.position = new Vector3Int(selectionBounds.position.x, selectionBounds.position.y, 0);
            
            ItemIndex                                 = s_Index++;
            ItemVariety                               = Variety.MultipleTilesItem;
            
            if (cells == null)
                Cells = null;
            else
            {
                //important to copy the cells since we don't want to affect the source.
                Cells = new TileCell[cells.Length];
                Array.Copy(cells, Cells, cells.Length);
            }
            //detect if any cell's transform isn't identity. Flag this for proper handling
            if (Cells != null)
            {
                if (Cells.Any(cell => cell.m_Transform != Matrix4x4.identity))
                    CellsModified = true;
                // ReSharper disable once Unity.NoNullPatternMatching
                if (Cells.Any(cell => cell.IsTile && cell.TileBase != null &&  cell.TileBase is Tile t && (t.flags & TileFlags.LockColor) != 0))
                    HasColorLockedTiles = true;
                // ReSharper disable once Unity.NoNullPatternMatching
                if (Cells.Any(cell => cell.IsTile && cell.TileBase != null && cell.TileBase is Tile t && (t.flags & TileFlags.LockTransform) != 0))
                    HasTransformLockedTiles = true;
            }

            BoundsInt                               = bounds;
            MultipleTilesSelectionBoundsInt         = selectionBounds;
            AColor    = Color.white;
            m_WasPickedTile                           = picked;
            FromAConversion                           = fromConversion;
            Pivot                                     = bounds.min;
            CellsPivotLocation                        = PivotLocation.Original;
            SetupGridBrush();
        }

        /// <summary>
        /// Create a prefab item,
        /// </summary>
        /// <param name="prefab">the prefab asset ref</param>
        public TpPainterClipboard(GameObject prefab)
        {
            if(!isRecloning)
                ClipboardGuid = Guid.NewGuid();

            ItemIndex = s_Index++;
            ItemVariety       = Variety.PrefabItem;
            Prefab            = prefab;
            transform = Matrix4x4.identity;
            //start the process so this is cached.
            AssetPreview.GetAssetPreview(prefab);
            
        }

        #endregion

        #region cellOperations

        /// <summary>
        /// Setup the grid brush for use in rotation/flip operations.
        /// </summary>
        internal void SetupGridBrush()
        {
            gridBrushInvalid = false;
            if(ItemVariety is not Variety.MultipleTilesItem)
                return;
            
            if (Cells == null || GridBrushInternal == null)
            {
                if(TpLibEditor.Warnings)
                    TpLib.TpLogWarning($"SetupGridBrush: Null cells: [{Cells==null}] or Null Grid Brush: [{GridBrushInternal == null}] ");
                gridBrushInvalid = true;
                return;
            }
            
            GridBrushInternal.Reset();
            var bi = BoundsInt;
            if (BoundsInt.size.z == 0)
                bi.size = new Vector3Int(BoundsInt.size.x, BoundsInt.size.y, 1);
            GridBrushInternal.UpdateSizeAndPivot(bi.size, Pivot);
            CopyToGridBrush(); //copy cells to brush
        }

        /// <summary>
        /// Copy Clipboard Cells to GridBrush cells.
        /// </summary>
        private void CopyToGridBrush()
        {
            if (Cells == null || GridBrushInternal == null)
            {
                if(TpLibEditor.Warnings)
                    TpLib.TpLogWarning($"Error in Clipboard.CopyToGridBrush: Null cells: [{Cells==null}] or Null Grid Brush: [{GridBrushInternal == null}] ");
                return;
            }

            var num = GridBrushInternal.cells.Length;
            if (num != Cells.Length)
            {
                if (TpLib.Warnings)
                    TpLib.TpLogWarning("Clipboard.CopyToGridBrush: Size mismatch, resetting Grid Brush. Not an error!");
                //try again
                SetupGridBrush();
                num = GridBrushInternal.cells.Length;
                if (num != Cells.Length)
                {
                    if (TpLib.Warnings)
                        TpLib.TpLogWarning($"Error in Clipboard.CopyToGridBrush: Clipboard's GridBrush can't be initialized: Cell Operations like Rotate/Flip entire array are inactive for this Pick! Brush size: {num}, Cells size: {Cells.Length}");
                    gridBrushInvalid = true;
                    return;
                }
            }

            var      dict = Cells.Where(tc=>tc != null).ToDictionary((tc) => tc.m_Position + Pivot);
            var      bi   = BoundsInt; //new BoundsInt(Vector3Int.zero, GridBrushInternal.size);
            // ReSharper disable once InlineOutVariableDeclaration
            // ReSharper disable once TooWideLocalVariableScope
            TileCell cel;
            foreach (var pos in bi.allPositionsWithin)
            {
                if(!dict.TryGetValue(pos, out cel ))
                    continue;
                var bPos =GridBrushInternal.GetCellIndex(pos);  
                GridBrushInternal.cells[bPos] = new GridBrush.BrushCell() { color = cel.m_Color, tile = cel.TileBase, matrix = cel.m_Transform };
            }
        }
        
       
        private void ResetBrushIfExists()
        {
            if(s_GridBrushInternal != null)
                s_GridBrushInternal.Reset();
        }

        /// <summary>
        /// Uses a GridBrush to rotate and flip groups of tiles.
        /// </summary>
        /// <param name="op"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <remarks>Note that using this method sets the CellsPivotLocation to lower-left.</remarks>
        private void CellOperations(CellOps op)
        {
            if (gridBrushInvalid)
            {
                TpLib.TpLog("Invalid Grid Brush setup: Cell Op cancelled...");
                return;
            }
            if(Cells == null ||  GridBrushInternal == null)
                return;
            
            //note: called from shortcut that ensures PainterWindow is open.
            var layout = TpPainterState.PaintableMap!.TargetTilemapGridLayout;
            if (layout == null)
            {
                TpLib.TpLogError("No layout: can't proceed");
                return;
            }
            
            switch (op)
                {
                    case CellOps.RotateCcw:
                        GridBrushInternal.Rotate(RotationDirection.CounterClockwise, layout.cellLayout);
                        break;
                    case CellOps.RotateCw:
                        GridBrushInternal.Rotate(RotationDirection.Clockwise, layout.cellLayout);
                        break;
                    case CellOps.FlipX:
                        GridBrushInternal.Flip(FlipAxis.X, layout.cellLayout);
                        break;
                    case CellOps.FlipY:
                        GridBrushInternal.Flip(FlipAxis.Y, layout.cellLayout);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(op), op, null);
                }
            CellsModified = true;

            //restore TileCells from GridBrush
            
            var pivotOut = GridBrushInternal.pivot;
            pivotOut.z = 0;
            
            Array.Clear(Cells,0,Cells.Length);
            var sz        = GridBrushInternal.size;
            sz.z = 1;
            var newBounds = new BoundsInt(pivotOut, sz);
            MultipleTilesSelectionBoundsInt = newBounds;
            BoundsInt                       = new BoundsInt(Vector3Int.zero, sz);
            Pivot                             = pivotOut;

            UpdatePivotLocation(pivotOut,sz);
            CellsPivotLocation = PivotPivotLocation;
            
            var index    = 0;
            //Debug.Log($"pivotout {pivotOut}  cellsPL {CellsPivotLocation}");
            foreach (var pos in BoundsInt.allPositionsWithin)
            {
                var gridBrushCellIndex = GridBrushInternal.GetCellIndex(pos);  
                var cell               = GridBrushInternal.cells[gridBrushCellIndex];
                Cells[index++] = new TileCell(cell, pos - pivotOut);
            }
        }

        private void UpdatePivotLocation(Vector3Int p, Vector3Int sz)
        {
            if (p is { x: 0, y: 0 })
                PivotPivotLocation = PivotLocation.LeftBottom;
            else if (p.x == 0 && p.y == sz.y - 1)
                PivotPivotLocation = PivotLocation.LeftTop;
            else if (p.y == 0 && p.x == sz.x - 1)
                PivotPivotLocation = PivotLocation.RightBottom;
            else
                PivotPivotLocation = PivotLocation.RightTop;
            //Debug.Log($"update piv loc {PivotPivotLocation}");
        }
        
        
        /// <summary>
        /// Rotate a single or group of tiles
        /// </summary>
        /// <param name="ccw">ccw=true, cw = false</param>
        /// <param name="affectsCells">true if this should affect the tiles as a group (multiple sel only).</param>
        public void Rotate(bool ccw = false, bool affectsCells = true)
        {
            if (HasTransformLockedTiles)
            {
                TpPainterSceneView.instance.SceneViewNotification = "Warning: Rotating with Lock Transform flags set!";
            }
            if (affectsCells)
            {
                if(ItemVariety != Variety.MultipleTilesItem || Cells == null)
                    return;
                CellOperations(ccw ? CellOps.RotateCcw : CellOps.RotateCw );
                TransformModified = true;
                return;
            }
            
            if (IsMultiple && Cells != null)
            {
                var scaler = TileUtil.RotatationMatixZ(ccw ? 90 : -90);
                for (var i = 0; i < Cells.Length; i++)
                {
                    var cel = Cells[i];
                    cel.m_Transform *= scaler;
                }
                CopyToGridBrush(); //important: otherwise subsequent CellOperation calls will operate on stale data.
                CellsModified     = true;
                TransformModified = true;
                return;
            }
            
            if (VarietyCantModifyTransform || IsTileBase)
                return;
            transform         *= TileUtil.RotatationMatixZ(ccw ? 90 : -90);
            TransformModified =  true;
        }

        /// <summary>
        /// Flip a single or group of tiles
        /// </summary>
        /// <param name="flipX">flip X = true, flip Y = false</param>
        /// <param name="affectsCells">true if this should affect the tiles as a group (Multiple sel only).</param>
        public void Flip(bool flipX = false, bool affectsCells = true)
        {
            if (HasTransformLockedTiles)
            {
                TpPainterSceneView.instance.SceneViewNotification = "Warning: Flipping with Lock Transform flags set!";
            }

            if (affectsCells)
            {
                if(!IsMultiple || Cells == null)
                    return;
                CellOperations(flipX ? CellOps.FlipX : CellOps.FlipY );
                TransformModified = true;
                return;
            }

            if (IsMultiple && Cells != null)
            {
                var scaler = TileUtil.ScaleMatrix(flipX ? new Vector3(-1, 1,  1)
                                                      : new Vector3(1,    -1, 1),
                                                  Vector3Int.zero);
                for (var i = 0; i < Cells.Length; i++)
                {
                    var cel = Cells[i];
                    cel.m_Transform *= scaler;
                }
                CopyToGridBrush();
                TransformModified = true;
                CellsModified     = true;
                return;
            }
            
            if (VarietyCantModifyTransform || IsTileBase)
                return;

            transform *= TileUtil.ScaleMatrix(flipX ? new Vector3(-1, 1,  1)
                                     : new Vector3(1,    -1, 1),
                                 Vector3Int.zero);
            TransformModified =  true;
        }


        /// <summary>
        /// Apply a new transform to the Clipboard if possible.
        /// </summary>
        /// <param name="newTransform">The new transform matrix.</param>
        /// <param name = "editActions" >The EditActions for the modification that this call represents</param>
        public void Apply(Matrix4x4 newTransform, EditActions editActions)
        {
            if (VarietyCantModifyTransform || IsTileBase || (editActions & EditActions.Transform) == 0)
                return;

            if (HasTransformLockedTiles)
                TpPainterSceneView.instance.SceneViewNotification = "Warning: Lock Transform flags set!";
            
            ModifierEditActions = editActions;
            if (ItemVariety == Variety.MultipleTilesItem && Cells!= null && Cells.Length != 0)
            {
                foreach (var cell in Cells)
                    cell.m_Transform = newTransform;
                CopyToGridBrush();
                TransformModified = true;
                CellsModified     = true;
                return;
            }
            
            
            transform         = newTransform;
            TransformModified = true;
        }

        /// <summary>
        /// Apply a new Color to the Clipboard if possible.
        /// </summary>
        /// <param name="color">The new Color</param>
        /// <param name = "editActions" >The EditActions for the modification that this call represents</param>
        public void Apply(Color color, EditActions editActions)
        {
            if (ItemVariety == Variety.PrefabItem || VarietyCantModifyTransform || IsTileBase || (editActions & EditActions.Color) == 0)
                return;
            
            if (HasColorLockedTiles)
                TpPainterSceneView.instance.SceneViewNotification = "Warning: Lock Color flags set!";

            
            ModifierEditActions = editActions;
            if (ItemVariety == Variety.MultipleTilesItem && Cells!= null && Cells.Length != 0)
            {
                foreach (var cell in Cells)
                    cell.m_Color = color;
                CopyToGridBrush();
                ColorModified = true;
                CellsModified = true;
                return;
            }

            AColor        = color;
            ColorModified = true;
        }
        
        #endregion
        
        #region preview

        // ReSharper disable once ReturnTypeCanBeNotNullable
        internal Texture2D? GetClipboardIcon()
        {
            switch (ItemVariety)
            {
                case Variety.BundleItem:
                    return FindIcon(TpIconType.TpTileBundleIcon);
                case Variety.TileFabItem:
                    return FindIcon(TpIconType.TileFabIcon);
                case Variety.PrefabItem:
                    return FindIcon(TpIconType.PrefabIcon);
                case Variety.EmptyItem:
                    return FindIcon(TpIconType.UnityXIcon);
                case Variety.TileItem:
                {
                    if (Tile == null)
                        return FindIcon(TpIconType.UnityXIcon);
                    return IsTilePlusBase
                               ? FindIcon(TpIconType.TptIcon)
                               : FindIcon(TpIconType.TileIcon);
                }
                
                case Variety.MultipleTilesItem:
                    return FindIcon(TpIconType.UnityEyedropperIcon);
                default:
                    return FindIcon(TpIconType.UnityXIcon);
                    
            }
        }
        
        internal (Sprite? sprite, Texture2D? texture) GetClipboardImage()
        {
            Sprite? GetSprite(TileBase t)
            {
                return (TpPreviewUtility.TryGetPlugin(t, out var plug) && plug != null)
                           ? plug.GetSpriteForTile(t)
                           : FindIconAsSprite(TpIconType.UnityToolbarMinusIcon);

            }
            
            Texture2D GetPrefabImage()
            {
                Texture2D? tex = null;
                if(Prefab != null)
                    tex = TpPreviewUtility.PreviewGameObject(Prefab).preview;
                if(tex == null)
                    tex = FindIcon(TpIconType.PrefabIcon);
                return tex;
            }
            
            
            

            if(IsEmpty)
                return(null, FindIcon(TpIconType.UnityToolbarMinusIcon));

            if (IsTile)
            {
                if (Tile == null)
                    return (FindIconAsSprite(TpIconType.HelpIcon), null);
                    
                if (IsTileBase)
                    return (GetSprite(Tile), null);

                if (IsTilePlusBase)
                    return (((ITilePlus)Tile).EffectiveSprite, null);

                if (IsTile)
                    return (((Tile)Tile).sprite, null);
            }

            if (IsBundle)
            {
                if (Bundle!=null && Bundle.m_Icon != null)
                    return (Bundle.m_Icon, null);
                return(null, FindIcon(TpIconType.TpTileBundleIcon));
            }

            if (IsTileFab)
            {
                if (TileFab != null && TileFab.m_Icon != null)
                    return (TileFab.m_Icon, null);
                return (null, FindIcon(TpIconType.TileFabIcon));
            }

            if (IsPrefab)
                return (null,GetPrefabImage());

            if (IsMultiple)
            {
                if (m_Icon != null)
                    return (m_Icon,null);
                return (null,FindIcon(TpIconType.UnityEyedropperIcon));
            }
                

            return (null, null);

        }
        
        #endregion

        #region utility
        [SuppressMessage("ReSharper", "Unity.NoNullPatternMatching")]
        private GridBrush? GetBrush(string warningMessage = "Operation cancelled.")
        {
            var currentBrush = GridPaintingState.gridBrush;
            if (currentBrush == null)
                return null;
            // Note: Rider's InvertIf of the next statement is logically incorrect!
            // ReSharper disable once InvertIf
            if (currentBrush is not TilePlusBrush && currentBrush is not GridBrush)
            {
                //if neither of these, try and find the TPbrush.
                var tpBrush = GridPaintingState.brushes.FirstOrDefault(x => x is TilePlusBrush);
                if (tpBrush == null) //if not found, try to get the default GridBrush
                {
                    //if no Tile+Brush, get the GridBrush
                    var gridBrush = GridPaintingState.brushes.FirstOrDefault(x => x is GridBrush);
                    if (gridBrush == null) //should never be the case though?
                    {
                        TpLib.TpLogError($"{warningMessage}: Could not find Tile+Brush or stock GridBrush.");
                        return null;
                    }
                    else
                        GridPaintingState.gridBrush = gridBrush;
                }
                else
                    GridPaintingState.gridBrush = tpBrush;

                return (GridBrush)  GridPaintingState.gridBrush;
            }
            return null;
        }

        
        /// <summary>
        /// Rotate the Cells pivot. Note that it always begins in the LL corner.
        /// Also modifies Pivot.
        /// </summary>
        public void RotatePivot()
        {
            if(!IsMultiple)
            {
                Debug.LogError("Not multiple!");
                return;
            }
            
            var b        = BoundsInt;
            b.z = 0;
            
            //small state machine proceeds clockwise from LL corner (ORIGINAL/LeftBottom)
            //to UL corner (LeftTop)
            //to UR corner (RightTop) to LR (RightBottom) corner and back to LL (LeftBottom) corner.
            //note that .Original would mean unmodified as that state doesn't get set here.
            CellsPivotLocation = CellsPivotLocation switch
                                 {
                                     PivotLocation.Original or PivotLocation.LeftBottom => PivotLocation.LeftTop,
                                     PivotLocation.LeftTop                              => PivotLocation.RightTop,
                                     PivotLocation.RightTop                             => PivotLocation.RightBottom,
                                     PivotLocation.RightBottom                          => PivotLocation.LeftBottom,
                                     _                                                  => CellsPivotLocation
                                 };
            //BoundsInt = b;
            TpPreviewUtility.ClearPreview();
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public static void Dispose()
        {
            //todo
            /*if (s_GridBrushInternal != null)
                Object.DestroyImmediate(s_GridBrushInternal);*/
        }

        /// <summary>
        /// Gets the Garbage-collected Clipboard Instances count.
        /// For Sysinfo diags only.
        /// </summary>
        public static uint Discards { get; private set; }

        ~TpPainterClipboard()
        {
            Discards++;
            //Reset();  //FOR FUTURE USE IF POOLED.
        }

        /// <summary>
        /// Resets the Clipboard
        /// </summary>
        public void Reset()
        {
            Tile                            = null;
            TileType                        = null;
            Cells                           = null;
            ClipboardGuid                   = null;
            Prefab                          = null;
            ItemVariety                     = Variety.EmptyItem;
            BoundsInt                       = new BoundsInt();
            MultipleTilesSelectionBoundsInt = new BoundsInt();
            FromAConversion                 = false;
            CellsModified                   = false;
            ItemIndex                       = 0;
            Icon                            = null;
            Bundle                          = null;
            TileFab                         = null;
            SourceTilemap                   = null;
            transform                       = Matrix4x4.identity;
            CellsPivotLocation              = PivotLocation.Original;
            PivotPivotLocation              = PivotLocation.Original;

            IsTile                = false;
            IsTileBase            = false;
            IsTilePlusBase        = false;
            ITilePlusInstance     = null;
            IsTpBundleTile        = false;
            IsClonedTilePlusBase  = false;
            IsNotTilePlusBase     = false;
            IsFromFavorites       = false;
            IsFromBundleAsPalette = false;
            HasGameObject         = false;
            AColor                = Color.white;
            

            m_Icon                                    = null;
            m_ColorModified                           = false;
            m_WasPickedTile                           = false;
            m_TransformModified                       = false;
            m_Backing_MultipleTilesSelectionBoundsInt = new BoundsInt();
        }
        
        
        /// <inheritdoc />
        public override string ToString()
        {
            return $"Clipboard Data for Variety {ItemVariety}";
        }

        #endregion
        
    }

}
