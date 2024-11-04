// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 12-10-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-30-2023
// ***********************************************************************
// <copyright file="TpImageLib.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using TilePlus.Editor.Painter;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

#nullable enable

namespace TilePlus.Editor
{
    /// <summary>
    /// Image manipulation library and utilities for TilePlus
    /// </summary>
    [InitializeOnLoad]
    public static class TpImageLib
    {
        #region ctor
        static TpImageLib()
        {
            TpEditorBridge.BridgeSpriteFromTex = SpriteFromTexture;
        }
        #endregion
        
        #region private fields
        private static readonly Dictionary<Texture2D,TextureImporter> s_TextureImporters = new();
        private static          int                                   s_MaxTextureImporters;
        #endregion
        
        #region properties

        /// <summary>
        /// Get the default texture
        /// </summary>
        public static Texture2D DefaultTexture => TpIconLib.FindIcon(TpIconType.HelpIcon);


        /// <summary>
        /// Get the default texture
        /// </summary>
        public static Sprite? DefaultSprite => TpIconLib.FindIconAsSprite(TpIconType.HelpIcon);


        /// <summary>
        /// Texture importer stats for SysInfo window
        /// </summary>
        public static (int num, int max) NumTextureImporters => (s_TextureImporters.Count, s_MaxTextureImporters);
        #endregion
        
        

        #region spriteFromTex
        
        /// <summary>
        /// Create a sprite from a Texture2D. 
        /// </summary>
        /// <param name="tex">input texture</param>
        /// <returns>sprite instance</returns>
        public static Sprite? SpriteFromTexture(Texture2D? tex)
        {
            if (tex == null)
            {
                TpLib.TpLogError("SpriteFromTexture passed a null texture input!");
                return null;
            }
            if (tex.width == 0 || tex.height == 0)
                return null;
            
            var texRect   = new Rect(0, 0, tex.width, tex.height);
            var newSprite =  Sprite.Create(tex, texRect, new Vector2(0.5f, 0.5f),100f,1,SpriteMeshType.FullRect);
            
            newSprite.name = $"{tex.name}[TpImageLib:h{tex.height},w{tex.width}]";
            return newSprite;
        }

        /// <summary>
        /// Flushes the texture importers and clears cached importers.
        /// </summary>
        private static void FlushTextureImporters()
        {
            s_MaxTextureImporters = Mathf.Max(s_TextureImporters.Count,s_MaxTextureImporters);
            foreach (var importer in s_TextureImporters.Values)
            {
                if (TpLibEditor.Informational)
                    TpLib.TpLog($"Changed IsReadable to false for asset at path: {importer.assetPath}");

                importer.isReadable = false;
                importer.SaveAndReimport();
            }
            s_TextureImporters.Clear();
        }

        /// <summary>
        /// Extract a Texture2D from a sprite. When done using this,
        /// whether one use or within a loop, call FlushTextureImporters
        /// to clean up. V important!! as this method caches importers for efficiency!!
        /// </summary>
        /// <param name="sprite">A sprite</param>
        /// <param name = "forceReadable" >defaults true: if true and texture isn't readable, fix with importer. CAN BE TIME CONSUMING!
        /// If this is false then no need to call FlushTextureImporters</param>
        /// <returns>texture</returns>
        internal static Texture2D TextureFromSprite(Sprite sprite, bool forceReadable=true)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (sprite.rect.width != sprite.texture.width)
            {
                var             texture  = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height);
                if (!sprite.texture.isReadable)
                {
                    if (forceReadable)
                    {
                        if (!s_TextureImporters.TryGetValue(sprite.texture, out var importer))
                        {
                            var assetPath = AssetDatabase.GetAssetPath(sprite.texture);
                            if (string.IsNullOrWhiteSpace(assetPath))
                            {
                                TpLib.TpLogError($"**Could not locate texture for sprite {sprite.name} ");
                                return new Texture2D(10, 10);
                            }

                            // ReSharper disable once AccessToStaticMemberViaDerivedType
                            importer = TextureImporter.GetAtPath(assetPath) as TextureImporter;
                            if (importer == null)
                            {
                                TpLib.TpLogError($"**Could not locate texture importer for sprite {sprite.name} at path {assetPath} ");
                                return new Texture2D(10, 10);
                            }

                            if (TpLibEditor.Informational)
                                TpLib.TpLog($"Changed IsReadable to true for asset at path: {assetPath}");

                            importer.isReadable = true;
                            importer.SaveAndReimport();
                            s_TextureImporters.TryAdd(sprite.texture, importer);
                        }
                    }
                }
                else
                {
                    
                }
                var colors = sprite.texture.GetPixels((int)sprite.textureRect.x,
                                                         (int)sprite.textureRect.y,
                                                         (int)sprite.textureRect.width,
                                                         (int)sprite.textureRect.height);

                texture.SetPixels(colors);
                texture.Apply();
                return texture;
            }
            else
                return sprite.texture;
        }
        
        #endregion
        
        
        
        
        #region spriteCombiner

        /// <summary>
        /// Combine a 2D array of sprites into a Texture2D of that array with all the sprites in the
        /// same arrangement as in the array. Sprites will be resized to fit into standardized sizes.
        /// IE all the textures are resized to be the same size.
        /// </summary>
        /// <param name="textures">a 2D rectangular array of Texture2D elements. Nulls would indicate empty positions.</param>
        /// <param name = "angles" >Matching array of rotation angles or null. Currently unused</param>
        /// <param name = "scales" >Matching array of scale factors or null. Currently unused</param>
        /// <returns>A Texture2D with the sprites or Null if error</returns>
        /// <remarks>
        /// One of several texture sizes is returned based on the sprites' image sizes and how many in each row and column.
        /// Mimimum sprite size assumed to be 16x16 and minimum returned texture size is 64x64.
        /// Largest is 256 x 256K. May not be square.
        /// Note that the textures array CANNOT have null tiles in it, those should be filtered out beforehand.
        /// </remarks>
        private static Texture2D? SpriteCombiner(Texture2D [,] textures, float[,]? angles = null, Vector3[,]? scales = null)
        {
            var xInputSize = textures.GetLength(0);
            var yInputSize = textures.GetLength(1);
            if (xInputSize == 0 || yInputSize == 0)
            {
                TpLib.TpLog("Sprite Combiner has Nothing to do: no input or incorrectly formatted input array. ");
                return null;
            }

            
            var scaledTextures = new Texture2D[xInputSize, yInputSize];
            var      total          = new Vector3Int();
            
            //compute the total size of all sprites
            //note that the array can validly have nulls.
            for (var rowY = 0; rowY < yInputSize; rowY++)
            {
                for (var colX = 0; colX < xInputSize; colX++)
                {
                    var tex = textures[colX, rowY];
                    if (!tex)
                        tex = DefaultTexture;
                    total.x += tex.width;
                    total.y += tex.height;
                }
            }
            
            var output = new Vector3Int {
                                            //now the variable 'total' has the total size of all the sprites.
                                            //Compute the output texture size.
                                            x = total.x switch
                                                {
                                                    //round total up to the next reasonable image size. Base size is 64x64.
                                                    <= 64  => 64,
                                                    < 128 => 128,
                                                    _ => 256   
                                                },
                                            y = total.y switch
                                                {
                                                    //round total up to the next reasonable image size. Base size is 64 x 64.
                                                    <= 64 => 64,
                                                    < 128 => 128,
                                                    _     => 256   
                                                }
                                        };

            //compute the scale factor for the textures based on the sprite sizes fitting into the
            //proposed output size (outputWidth, outputHeight)

            //check for error here. Avoid /0 exception.
            if (total.x == 0 || total.y == 0)
            {
                TpLib.TpLogError("Error computing output sizes: can't divide by 0");
                return null;
            }

            //now compute the size of one section of the output texture.
            //ie, if the dims are 32x32 and there are 4 array elements in a 2x2 arrangement
            // then the size of each sprite as placed into the output texture ought
            // to be 16x16 which may require scaling up or down.
            Vector3Int sectionSize = new Vector3Int(Mathf.FloorToInt((float)output.x / (float)xInputSize),
                                                    Mathf.FloorToInt((float)output.y / (float)yInputSize));
            
            if(TpLibEditor.Informational)
                TpLib.TpLog($"SpriteCombiner's computed section size is width: {sectionSize.x}, height: {sectionSize.y}."
                          + $" Image size is x:{output.x} y:{output.y} ");
            //now build the array of scaled textures
            for (var rowY = 0; rowY < yInputSize; rowY++)
            {
                for (var colX = 0; colX < xInputSize; colX++)
                {
                    var tex = textures[colX, rowY];
                    if (!tex)
                        tex = DefaultTexture;
                    var width  = Mathf.FloorToInt(sectionSize.x);
                    var height = Mathf.FloorToInt(sectionSize.y);
                    if(width == 0 || height == 0)
                        continue;
                    scaledTextures[colX, rowY] = ScaleTexture(tex, width, height);
                }
            }
           
            //given all the scaled sprites, copy the pixel data
            var result = new Texture2D(output.x, output.y, TextureFormat.RGBA32, -1,false); 
            
            var allClear = new Color32[output.x * output.y];
            allClear.Initialize();
            result.SetPixels32(allClear);
            for (var y = 0; y < yInputSize; y++)
            {
                for (var x = 0; x < xInputSize; x++)
                {
                    var tex  = scaledTextures[x, y];
                    if (tex == null)
                        continue;
                    result.SetPixels32(x * sectionSize.x, y * sectionSize.y, sectionSize.x, sectionSize.y, tex.GetPixels32());
                }
            }
            result.Apply();

            return result;
        }
        
        /// <summary>
        /// Scale a Texture2D. 
        /// H/T http://jon-martin.com/?p=114 but there are versions of this floating around the net so no idea of original author.
        /// Cleaned up a bit. Was MIT license.
        /// </summary>
        /// <param name="input">input Texture2D</param>
        /// <param name="outputWidth">width of desired output</param>
        /// <param name="outputHeight">height of desired output</param>
        /// <returns>output Texture2D. Will return a small but empty 4x4 texture if the input is null or if outputWidth or outputHeight is LT 4 </returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public static Texture2D ScaleTexture(Texture2D? input, int outputWidth, int outputHeight)
        {
            if (input == null || outputWidth < 4 || outputHeight < 4)
                return new Texture2D(4,4);
            if (!input.isReadable)
            {
                if(TpLib.Informational)
                    TpLib.TpLog("ScaleTexture() input wasn't readable, can't scale it.");
                return input;
            }


            var result  =new Texture2D(outputWidth, outputHeight, TextureFormat.RGBA32, false, false);
            var rpixels =result.GetPixels32(0);
            var incX    =(1.0f / (float)outputWidth);
            var incY    =(1.0f / (float)outputHeight); 
            for(var px=0; px<rpixels.Length; px++) { 
                // ReSharper disable once PossibleLossOfFraction
                rpixels[px] = input.GetPixelBilinear(incX*((float)px%outputWidth), incY*((float)Mathf.Floor(px/outputWidth))); 
            } 
            result.SetPixels32(rpixels, 0); 
            result.Apply(); 
            return result; 
        }

        //h/t https://gamedev.stackexchange.com/questions/203539/rotating-a-unity-texture2d-90-180-degrees-without-using-getpixels32-or-setpixels
        //user https://gamedev.stackexchange.com/users/39518/dmgregory
        //Cleaned up a bit
        /// <summary>
        /// Rotate an image by an arbitrary angle. Note: unused/untested 
        /// </summary>
        /// <param name="tex">the input texture</param>
        /// <param name="angleDegrees">degrees</param>
        /// <param name = "updateMips" >update mip maps if true (default=false)</param>
        private static void RotateImage(Texture2D tex, float angleDegrees, bool updateMips = false)
        {
            var   width      = tex.width;
            var   height     = tex.height;
            var halfHeight = height * 0.5f;
            var halfWidth  = width * 0.5f;

            var texels = tex.GetRawTextureData<Color32>();        
            var copy   = System.Buffers.ArrayPool<Color32>.Shared.Rent(texels.Length);
            Unity.Collections.NativeArray<Color32>.Copy(texels, copy, texels.Length);

            var phi    = Mathf.Deg2Rad * angleDegrees;
            var cosPhi = Mathf.Cos(phi);
            var sinPhi = Mathf.Sin(phi);

            var address = 0;
            for (var newY = 0; newY < height; newY++)
            {
                for (var newX = 0; newX < width; newX++)
                {
                    var cX   = newX - halfWidth;
                    var cY   = newY - halfHeight;
                    var   oldX = Mathf.RoundToInt(cosPhi * cX + sinPhi * cY + halfWidth);
                    var   oldY = Mathf.RoundToInt(-sinPhi * cX + cosPhi * cY + halfHeight);
                    var insideImageBounds = (oldX > -1) & (oldX < width)
                                                        & (oldY > -1) & (oldY < height);
            
                    texels[address++] = insideImageBounds ? copy[oldY * width + oldX] : default;
                }
            }

            // No need to reinitialize or SetPixels - data is already in-place.
            tex.Apply(updateMips);

            System.Buffers.ArrayPool<Color32>.Shared.Return(copy);
        }


        #endregion

        #region multipleSelectIcon

        
        
        /// <summary>
        /// Creates a Multiple Tiles Icon from a TileCellsWrapper instance
        /// </summary>
        /// <param name="item">A TileCellsWrapper instance</param>
        /// <returns>Tuple of (sprite, texture) for the icon. both null is an error.</returns>
        /// <remarks>if CTRL or SHIFT is held down default texture/sprite is returned..</remarks>
        internal static (Sprite? sprite, Texture2D? tex) CreateMultipleTilesIcon(TileCellsWrapper? item)
        {
            if (item == null || item.Cells == null || item.Cells.Length < 2  )
            {
                TpLib.TpLogError("error in TilePlusPainterFavorites.CreateMultipleTilesIcon! no cells or too few (< 2)");
                return (null,null);
            }
       
            Texture2D? tex   = null;
            //note that the original cells may have had its transform/color modified.
            //note that some cells may be null or have null tiles or no sprite. this is ok but we don't want to use them in the icon
            //note that all tiles which are not Tile or TilePlus-based are skipped: ie., no 'AnimatedTiles' or 'Rule Tiles'
            var iconCells = item.Cells.Where(cel => cel != null && cel.TileBase != null && cel is { IsPlainTileBase: false, HasSprite: true } ).ToArray();

            var numIconCells = iconCells.Length;
            if (numIconCells > 512)
            {
                if(TpLibEditor.Informational)
                    TpLib.TpLog("Selection is too large to create an icon. Using a default instead...");
                return (DefaultSprite, DefaultTexture);
            }

            if (TpLibEditor.Informational)
                TpLib.TpLog($"CreateMultipleTilesIcon found {numIconCells} non-null tiles with sprites to combine.");
            
            if (numIconCells == 0  )
            {
                TpLib.TpLogError("error in TilePlusPainterFavorites.CreateMultipleTilesIcon! no cells or all cells were null");
                return (null,null);
            }

            if (numIconCells == 1)
            {
                var cell           = iconCells[0];
                var possibleTex    = DefaultTexture;
                var possibleSprite = DefaultSprite;

                //note that cell.Tilebase can't be null due to filtering in LINQ above.
                // ReSharper disable once Unity.NoNullPatternMatching
                if (cell.TileBase is TilePlusBase tpb)
                {
                    if (tpb.EffectiveSprite == null)
                        return (possibleSprite, possibleTex);
                    possibleSprite = tpb.EffectiveSprite;
                    possibleTex    = TextureFromSprite(tpb.EffectiveSprite);
                }

                // ReSharper disable once Unity.NoNullPatternMatching
                else if (cell.TileBase is Tile t)
                {
                    possibleSprite = t.sprite;
                    possibleTex    = TextureFromSprite(t.sprite);
                }

                return (possibleSprite, possibleTex);
            }


            var xSize = Mathf.Min(item.m_Bounds.size.x, 64);
            var ySize = Mathf.Min(item.m_Bounds.size.y, 64);
            
            var data         = new Texture2D[xSize, ySize];
            var angles       = new float[xSize, ySize];
            var scales       = new Vector3[xSize, ySize];
            var noExceptions = true;
            try
            {
                var len = Mathf.Min(numIconCells, xSize * ySize);
                for (var i = 0; i < len; i++ )
                {
                    var cell = iconCells[i];
                    var pos = cell.m_Position;
                    if(!cell.TileBase)
                        continue;
                    // ReSharper disable once Unity.NoNullPatternMatching
                    if (cell.TileBase is TilePlusBase tpb)
                    {
                        tex = tpb.EffectiveSprite == null
                                  ? DefaultTexture
                                  : TextureFromSprite(tpb.EffectiveSprite);
                    }

                    else
                        // ReSharper disable once Unity.NoNullPatternMatching
                        tex = cell.TileBase is Tile t 
                                  ? TextureFromSprite(t.sprite)
                                  : DefaultTexture;

                    if (tex == null)
                        tex = DefaultTexture;

                    
                    //todo: note that angles and scales not used yet.
                    cell.m_Transform.rotation.ToAngleAxis(out var angle, out _);
                    angles[pos.x, pos.y] = angle;
                    scales[pos.x, pos.y] = cell.m_Transform.lossyScale;
                    data[pos.x, pos.y]   = tex!; //note the tex can't be null here.
                }
            }
            catch (Exception e)
            {
                noExceptions = false;
                TpLib.TpLogError($"Error creating Textures from sprites! {e}");
            }
            finally
            {
                FlushTextureImporters();
            }
                
            if(noExceptions)
                tex = SpriteCombiner(data,angles,scales);
                   
            
            
            Sprite? sprite;
            if(tex == null && item.Cells != null)
            {
                var firstCell = item.Cells.FirstOrDefault(cell => cell.TileBase != null);
                if(firstCell == null)
                    sprite = TpIconLib.FindIconAsSprite(TpIconType.HelpIcon);
                else if(firstCell.TileBase == null) //can't combine with || since need this null check and combining would fail if firstCell == null!
                    sprite = TpIconLib.FindIconAsSprite(TpIconType.HelpIcon);
                else
                {
                    // ReSharper disable once Unity.NoNullPatternMatching
                    sprite = firstCell.TileBase is Tile t
                                 ? t.sprite
                                 : null;
                    if (sprite == null)
                        sprite = TpIconLib.FindIconAsSprite(TpIconType.HelpIcon);
                }
            }
            else
                sprite = SpriteFromTexture(tex);

            return (sprite,tex);
        }
        
        #endregion
        
    }
}
