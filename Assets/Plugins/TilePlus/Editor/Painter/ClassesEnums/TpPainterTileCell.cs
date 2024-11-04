// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 11-09-2022
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 01-22-2024
// ***********************************************************************
// <copyright file="TpPainterTileCell.cs" company="Jeff Sasmor">
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
using Color = UnityEngine.Color;

#nullable enable

namespace TilePlus.Editor.Painter
{
    
    /// <summary>
    /// A single brush cell with position added.
    /// </summary>
    [Serializable]
    public class TileCell 
    {
        //Note: DO NOT set these to readonly despite what your IDE may say. Serialization won't work correctly!
        
        /// <summary>
        /// The tile ref
        /// </summary>
        [SerializeField]
        private TileBase? m_TileBase;
        
        [SerializeField]
        private bool m_IsTilePlus;

        /// <summary>
        /// Is this a TilePlus tile? Needed to be able to tell if a null is a tileplus tile.
        /// </summary>
        public bool IsTilePlus => m_IsTilePlus;

        [SerializeField]
        private bool m_IsTile;

        /// <summary>
        /// Is this a Tile and not a TileBase?
        /// Note this will be true if IsTilePlus is true.
        /// </summary>
        public bool IsTile => m_IsTile;

        /// <summary>
        /// returns true if this tile is a TileBase (not tile or tileplus)
        /// </summary>
        public bool IsPlainTileBase => !(m_IsTilePlus || m_IsTile);

        /// <summary>
        /// The tile ref
        /// </summary>
        public TileBase? TileBase
        {
            get => m_TileBase;
            set
            {
                m_TileBase   = value;
                if (value == null)
                    return;
                m_IsTilePlus = (value as ITilePlus) != null;
                m_IsTile     = (value as Tile) != null;
            }
        }

        /// <summary>
        /// Does this tile have a visible sprite?
        /// </summary>
        public bool HasSprite
        {
            get
            {
                //casts cant fail here which explains pragmas and null ref ignore '!'
                if (IsTile)
                    #pragma warning disable CS8602 // Dereference of a possibly null reference.
                    return ((Tile)TileBase!).sprite != null;
                #pragma warning restore CS8602 // Dereference of a possibly null reference.
                else if (IsTilePlus)
                    #pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                    return ((ITilePlus)TileBase)!.EffectiveSprite != null;
                #pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                else
                    return false;
            }
        }
       
        /// <summary>
        /// the Color
        /// </summary>
        [SerializeField]
        public Color     m_Color;
        /// <summary>
        /// The position
        /// </summary>
        [SerializeField]
        public Vector3Int m_Position;
        /// <summary>
        /// The transform
        /// </summary>
        [SerializeField]
        public Matrix4x4 m_Transform;

        /// <summary>
        /// If the cell contains a TPT tile, this string MAY be filled with the JSON version of the tile.
        /// Note that this is only done within TilePlusPainterFavorites and ONLY gets
        /// populated if this cell has been sent to Favorites within a TileCellsWrapper S.O.
        /// </summary>
        [SerializeField]
        public string? m_TptJson;

        /// <summary>
        /// The Type of TPT tile.
        /// </summary>
        [SerializeField]
        public string? m_TptJsonType;

        
        /// <summary>
        /// Constructor for cell and position
        /// </summary>
        /// <param name="cell">a GridBrush Cell</param>
        /// <param name="position">position on Grid</param>
        public TileCell(GridBrush.BrushCell cell, Vector3Int position)
        {
            TileBase      = cell.tile;
            m_Color = cell.color;
            m_Position  = position;
            m_Transform = cell.matrix;
        }

        /// <summary>
        /// Constructor for a single TilePlus tile.
        /// Does not use color,position,transform info.
        /// </summary>
        /// <remarks>Only for use in TilePlusPainterFavorites.cs</remarks>
        /// <param name="tpb">a TilePlusBase instance</param>
        public TileCell(TilePlusBase tpb)
        {
            TileBase   = tpb;
            m_Color    = Color.white;
            m_Position = TilePlusBase.ImpossibleGridPosition;
            m_Transform = Matrix4x4.identity;
        }
        
        
        /// <summary>
        /// Constructor for TileCell with all param
        /// </summary>
        /// <param name="tile">tile</param>
        /// <param name="position">position</param>
        /// <param name="color">color</param>
        /// <param name="transform">transform</param>
        public TileCell(TileBase? tile, Vector3Int position, Color color, Matrix4x4 transform)
        {
            TileBase  = tile;
            m_Color = color;
            m_Position  = position;
            m_Transform =  transform;
        }
        
        /// <summary>
        /// Copy an existing cell.
        /// </summary>
        /// <param name="cell"></param>
        public TileCell(TileCell cell)
        {
            TileBase  = cell.TileBase;
            m_Position  = cell.m_Position;
            m_Color =  cell.m_Color;
            m_Transform = cell.m_Transform;
        }

        /// <summary>
        /// Create a TileCell from a TpTileBundles's TileSetItem.
        /// </summary>
        /// <param name="tilesetItem">A TileSetItem instance.</param>
        public TileCell(TpTileBundle.TilesetItem tilesetItem)
        {
            TileBase    = tilesetItem.m_Tile;
            m_Position  = tilesetItem.m_Position;
            m_Color     = tilesetItem.m_Color;
            m_Transform = tilesetItem.m_TransformMatrix;
        }

        /// <summary>
        /// If this is a TPT tile, serialize it. ONLY for use from within TpPainterFavorites code!
        /// </summary>
        public void SerializeTptCell()
        {
            if(!IsTilePlus || m_TileBase == null)
                return;
            m_TptJsonType = m_TileBase.GetType().ToString();
            //have to make a copy and change it to an asset
            var tileToSave = UnityEngine.Object.Instantiate(m_TileBase) as TilePlusBase;
            if (tileToSave == null)
                return;
            //reset it and change to asset state. Then serialize it, finally, destroy it.
            tileToSave.ChangeTileState(TileResetOperation.MakeNormalAsset);
            m_TptJson     = EditorJsonUtility.ToJson(tileToSave);
            UnityEngine.Object.DestroyImmediate(tileToSave);
            
        }

        /// <summary>
        /// Deserialize TPT data for this cell, if any.
        /// </summary>
        public void DeserializeTptCell()
        {
            if(string.IsNullOrEmpty(m_TptJson) || string.IsNullOrEmpty(m_TptJsonType))
                return;
            var obj = ScriptableObject.CreateInstance(m_TptJsonType);
            if (obj == null)
                return;
            EditorJsonUtility.FromJsonOverwrite(m_TptJson, obj);
            if (obj != null)
                m_TileBase = (TileBase) obj;
        }
        
        
        /// <inheritdoc />
        public override string ToString()
        {
            var nam = "Null tile";
            if (m_TileBase != null)
                nam = m_TileBase.name;

            return $"TileCell: tile: {nam} isTilePlus? {IsTilePlus} pos: {m_Position} color: {m_Color} ";
        }

        /// <summary>
        /// Equals
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        protected bool Equals(TileCell other)
        {
            return TileBase != null && TileBase.Equals(other.TileBase) && m_Color.Equals(other.m_Color) && m_Position.Equals(other.m_Position) && m_Transform.Equals(other.m_Transform);
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
            return Equals((TileCell)obj);
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            return HashCode.Combine(TileBase, m_Color, m_Position, m_Transform);
        }
    }


   

    /// <summary>
    /// Wrapper to wrap TppClipboardItem with a UnityEngine.Object
    /// </summary>
    [Serializable]
    public class TileCellsWrapper : ScriptableObject
    {
        /// <summary>
        ///Cells 
        /// </summary>
        [SerializeField]
        private TileCell[]? m_Cells;

        /// <summary>
        /// Cells  with tiles.
        /// </summary>
        public TileCell[]? Cells
        {
            get => m_Cells;
            set => m_Cells       =   value;
        }
        
        

        /// <summary>
        /// If sent to favorites this gets populated to save recreating this all the time.
        /// </summary>
        public TileCellsBundle? TileCellBundle { get; private set; }   
        
        /// <summary>
        /// The pivot. Only valid for multiple-tile selections.
        /// </summary>
        public Vector3Int Pivot { get; set; }
        
        /// <summary>
        /// Bounds of the cells with origin 0
        /// </summary>
        [SerializeField]
        public BoundsInt m_Bounds;
        /// <summary>
        /// Actual selection bounds as picked from the Scene.
        /// </summary>
        [SerializeField]
        public BoundsInt m_MultipleTilesSelectionBoundsInt;
        /// <summary>
        /// Is this a valid wrapper?
        /// </summary>
        public bool Valid => m_Cells != null && m_Cells.Length != 0;
        /// <summary>
        /// Serialization of the icon data.
        /// </summary>
        [SerializeField]
        public byte[]? m_IconData;

        /// <summary>
        /// were the cells modified?
        /// </summary>
        [SerializeField]
        public bool m_CellsModified;
        
        [SerializeField]
        private Sprite? m_Icon;
        
        private bool noConversion; 
       
        
        /// <summary>
        /// Does not get serialized to asset file. Pregenerated Icon
        /// </summary>
        /// <remarks>Returns null if no icondata or the conversion fails. The caller should provide an alternate icon based on context</remarks>
        public Sprite? Icon
        {
            get
            {
                if (m_Icon != null)
                    return m_Icon;
                if (m_IconData == null || noConversion)
                    return null;
                var tex     = new Texture2D(512, 512, TextureFormat.ARGB32,false);
                // ReSharper disable once InvokeAsExtensionMethod
                var success = ImageConversion.LoadImage(tex, m_IconData);
                if (success)
                {
                    var texRect = new Rect(0, 0, tex.width, tex.height);
                    if(TpLib.Informational)
                        TpLib.TpLog($"Icon load from embedded PNG: sprite rect {texRect.ToString()}");
                    m_Icon = Sprite.Create(tex, texRect, new Vector2(0.5f, 0.5f),100);
                    return m_Icon;
                }
                else
                    Debug.LogError("Could not create sprite!");

                noConversion = true;
                return null;
            }
            set
            {
                if(value != null && m_Icon == null)
                    m_Icon = value;
            }
        }

        private bool scannedAlready;
        /// <summary>
        /// Create TileCellBundle for archiving from Favorites list.
        /// Scan for TPT tiles and archive. DO NOT use until all fields are populated.
        /// </summary>
        public void CreateTileCellsBundle()
        {
            if(scannedAlready)
                return;
            if(Cells == null)
                return;
            scannedAlready = true;
            if (Cells.Any(x => x.IsTilePlus))
            {
                foreach (var cell in Cells)
                    cell.SerializeTptCell();
            }

            TileCellBundle = new TileCellsBundle(this);
        }

        /// <summary>
        /// Deserialize TPT tile
        /// </summary>
        public void UnpackCells()
        {
            if(Cells == null)
                return;
            foreach (var cell in Cells)
            {
                cell.DeserializeTptCell();
            }
        }
        
    }

    /// <summary>
    /// Used to serialize TileCellWrappers when TilePlusPainterFavorites is Saved.
    /// </summary>
    [Serializable]
    public class TileCellsBundle
    {
        /// <summary>
        /// A TTD reference.
        /// </summary>
        [SerializeField]
        public TileCell[]? m_Cells;
        /// <summary>
        /// Bounds of the cells with origin 0
        /// </summary>
        [SerializeField]
        public BoundsInt m_Bounds;
        /// <summary>
        /// Actual selection bounds as picked from the Scene.
        /// </summary>
        [SerializeField]
        public BoundsInt m_MultipleTilesSelectionBoundsInt;

        /// <summary>
        /// The icon data for this cell bundle.
        /// </summary>
        [SerializeField]
        public byte[]? m_IconData;

        
        /// <summary>
        /// Construct from a TileCellWrapper
        /// </summary>
        /// <param name="wrapper">a TileCellsWrapper</param>
        /// <remarks>This uses wrapper.</remarks>
        public TileCellsBundle(TileCellsWrapper wrapper)
        {
            m_Cells                           = wrapper.Cells;
            m_Bounds                          = wrapper.m_Bounds;
            m_MultipleTilesSelectionBoundsInt = wrapper.m_MultipleTilesSelectionBoundsInt;
            if (wrapper.m_IconData != null && wrapper.m_IconData.Length != 0)
            {
                var len  = wrapper.m_IconData.Length;
                m_IconData = new byte[len];
                Array.Copy(wrapper.m_IconData, m_IconData, wrapper.m_IconData.Length);
                //Debug.Log($"array copy {m_IconData.Length}");

            }
            else
            {
                var icon = wrapper.Icon;
                if (icon == null)
                {
                    if(TpLib.Informational)
                        TpLib.TpLogWarning("Null icon: can't encode to PNG");
                    return;
                }

                //var data  = icon.texture.GetRawTextureData();
                
                var xSize = (uint)icon.texture.width;
                var ySize = (uint)icon.texture.height;
                try
                {
                    m_IconData = icon.texture.EncodeToPNG();
                    //EncodeArrayToPNG(data, GraphicsFormat.R8G8B8A8_SRGB, xSize, ySize);
                }
                catch (Exception e)
                {
                    if(TpLibEditor.Informational)
                        TpLib.TpLog($" Error in PNG conversion: {e.ToString()}");
                    m_IconData = TpImageLib.DefaultTexture.EncodeToPNG();
                }


                if(TpLib.Informational)
                    TpLib.TpLog($"Encode to PNG: data size = {m_IconData.Length} size x/y ={xSize}/{ySize}");
            }
        }

        /// <summary>
        /// Equals
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        protected bool Equals(TileCellsBundle other)
        {
            return Equals(m_Cells, other.m_Cells) && m_Bounds.Equals(other.m_Bounds) && m_MultipleTilesSelectionBoundsInt.Equals(other.m_MultipleTilesSelectionBoundsInt);
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
            return Equals((TileCellsBundle)obj);
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            var hc = HashCode.Combine(m_Cells, m_Bounds, m_MultipleTilesSelectionBoundsInt);
            return HashCode.Combine(hc); //diffuse
        }
    }
}
