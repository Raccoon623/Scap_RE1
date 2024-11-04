// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 02-04-2023
// ***********************************************************************
// <copyright file="TileUtil.cs" company="Jeff Sasmor">
//     Copyright Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
#nullable enable

namespace TilePlus
{
    /// <summary>
    /// Some static utility methods for tile math
    /// </summary>
    public static class TileUtil
   {
        /// <summary>
        /// Get a Matrix4x4 for an angle.
        /// </summary>
        /// <param name="angle">The angle.</param>
        /// <returns>Rotation matrix</returns>
        public static Matrix4x4 RotatationMatixZ(float angle)
       {
           return Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(new Vector3(0, 0, angle)),Vector3.one);
       }

        /// <summary>
        /// Get a Matrix4x4 for scaling
        /// </summary>
        /// <param name="scale">scale</param>
        /// <param name="position">position</param>
        /// <returns>Scaling matrix</returns>
        public static Matrix4x4 ScaleMatrix(Vector3 scale, Vector3 position)
       {
           return Matrix4x4.TRS(position, Quaternion.identity, scale);
       }

        /// <summary>
        /// Round an input Vector3 to n digits
        /// </summary>
        /// <param name="input">value to round</param>
        /// <param name="digits">number of digits of precision</param>
        /// <returns>rounded value of input</returns>
        public static Vector3 RoundVector3(Vector3 input, int digits)
       {
           return new Vector3((float)Math.Round(input.x, digits), (float)Math.Round(input.y, digits), (float)Math.Round(input.z, digits));
       }

        /// <summary>
        /// Create a BoundsInt given a center position and a size.
        /// </summary>
        /// <param name="position">position @ CENTER</param>
        /// <param name="size">size</param>
        /// <param name="forceZto1">default true: force size.z to 1</param>
        /// <returns>BoundsInt</returns>
        public static BoundsInt CreateBoundsInt(Vector3Int position, Vector3Int size, bool forceZto1=true)
       {
           if (forceZto1)
               size.z     = 1;
           //for boundsint, the first param, position, is the minimal point at bottom-left
           var boundsPos = new Vector3Int(position.x - size.x /2, position.y - size.y /2, 0);
           return new BoundsInt(boundsPos, size);
       }

        /// <summary>
        /// Get a random world position within bounds.
        /// </summary>
        /// <param name="bounds">a bounds</param>
        /// <returns>a random Vector3 position within bounds</returns>
        public static Vector3 RandomPosInBounds(Bounds bounds)
       {
           return new Vector3(
               UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
               UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
               UnityEngine.Random.Range(bounds.min.z, bounds.max.z));
       }

        /// <summary>
        /// Get a bounds representing the Camera's viewport.
        /// </summary>
        /// <param name="camera">a Camera</param>
        /// <returns>bounds, or empty bounds if cam is null or isn't orthographic</returns>
        public static Bounds BoundsFromOrthoCamera(Camera? camera)
        {
            if (camera == null || !camera.orthographic)
                return new Bounds();
            var pos      = camera.transform.position;
            var width  = camera.orthographicSize * 2 * Screen.width / Screen.height;
            return new Bounds(new Vector3(pos.x, pos.y, 0), new Vector3(width, width, 0));
        }
       
        
        /// <summary>
        /// Get the transform components for a tile. Convenience Function.
        /// </summary>
        /// <param name="map">Tilemap</param>
        /// <param name="position">position on map</param>
        /// <param name="tPosition">transform's position placed here</param>
        /// <param name="tRotation">transform's rotation placed here</param>
        /// <param name="tScale">transform's scale placed here</param>
        /// <remarks>Handy for tweening the transform (pos,scale,rot) of a tile</remarks>
        /// <remarks>No checking for whether or not a tile exists at that position</remarks>
        public static void GetTransformComponents(Tilemap  map,
                                                  Vector3Int  position,
                                                  out Vector3 tPosition,
                                                  out Vector3 tRotation,
                                                  out Vector3 tScale)
        {
            var transform   = map.GetTransformMatrix(position);
            tPosition = transform.GetPosition();
            tRotation = transform.rotation.eulerAngles;
            tScale    = transform.lossyScale;
        }


        /// <summary>
        /// Get the rotation of a tile's sprite.
        /// </summary>
        /// <param name="map">The tilemap</param>
        /// <param name="position">the position of the tile</param>
        /// <returns>the rotation of the sprite</returns>
        public static Vector3 GetTransformRotation(Tilemap map, Vector3Int position)
        {
            return map.GetTransformMatrix(position).rotation.eulerAngles;
        }

        /// <summary>
        /// Get the position of a tile's sprite
        /// </summary>
        /// <param name="map">the tilemap</param>
        /// <param name="position">position of tile</param>
        /// <returns>the position of the sprite</returns>
        public static Vector3 GetTransformPosition(Tilemap map, Vector3Int position)
        {
            return map.GetTransformMatrix(position).GetPosition();
        }

        /// <summary>
        /// Get the scale of a tile's sprite
        /// </summary>
        /// <param name="map">the tilemap</param>
        /// <param name="position">position of tile</param>
        /// <returns>the scale of the sprite</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public static Vector3 GetTransformScale(Tilemap map, Vector3Int position)
        {
            return map.GetTransformMatrix(position).lossyScale;
        }
        
        
        /// <summary>
        /// Set the transform for a tile. Convenience function.
        /// </summary>
        /// <param name="map">tilemap</param>
        /// <param name="position">position on map</param>
        /// <param name="tPosition">position for the tile transform</param>
        /// <param name="tRotation">rotation for the tile transform</param>
        /// <param name="tScale">scale for the tile transform</param>
        /// <remarks>Handy for tweening the transform (pos,scale,rot) of a tile's sprite</remarks>
        /// <remarks>No checking for whether or not a tile exists at that position</remarks>
        public static void SetTransform(Tilemap map,
                                        Vector3Int position,
                                        Vector3    tPosition,
                                        Vector3    tRotation,
                                        Vector3    tScale)
        {
            map.SetTransformMatrix(position, Matrix4x4.TRS(tPosition, Quaternion.Euler(tRotation), tScale));
        }
        
        
        /// <summary>
        /// Get a timeout for rewinding a one-shot animation when it completes
        /// </summary>
        /// <param name="numSpritesInAnimation">Number of sprites being animated</param>
        /// <param name="animationSpeed">The animation speed we want to use</param>
        /// <param name="map">The parent Tilemap: used to get the Tilemap's animation speed</param>
        /// <returns></returns>
        public static float GetOneShotTimeOut(int numSpritesInAnimation, float animationSpeed, Tilemap? map)
        {
            if(animationSpeed <= 0 || numSpritesInAnimation < 2 || map == null)
                return 1f;

            var tilemapAnimSpeed = map.animationFrameRate; 
            if (tilemapAnimSpeed <= 0)
                return 1f;

            return numSpritesInAnimation / (tilemapAnimSpeed * animationSpeed);
        }

        /// <summary>
        /// Get the true bounds for a Tile's sprite. Takes into account transform scaling.
        /// </summary>
        /// <param name="map">Source tilemap</param>
        /// <param name="position">position</param>
        /// <returns>Bounds for sprite. If map==null returns an empty Bounds </returns>
        public static Bounds GetTrueBoundsForTileSprite(Tilemap? map, Vector3Int position)
        {
            if (map == null)
                return new Bounds();

            //Need the world position of the tile to create a bounds.
            var worldPos = map.GetCellCenterWorld(position);
            
            //Get the sprite from the tilemap. If one were to obtain this from the Tile then it might be a tilesheet.
            var sprt = map.GetSprite(position);
            
            //Get the scale of the Tile sprite at that position.
            var scale = GetTransformScale(map, position);
            if(scale == Vector3.one)
                return new Bounds(worldPos, Vector3.one);

            //Get the TRUE size of the sprite
            var spriteSize = Vector3.Scale(sprt.bounds.size, scale);

            return new Bounds(worldPos, spriteSize);
        }
    
       
        
        /// <summary>
        /// Get the largest BoundsInt from a list of BoundsInts
        /// </summary>
        public static BoundsInt LargestBoundsInt(IEnumerable<BoundsInt> input)
        {
                var result = new BoundsInt();
                var area   = 0f;
                foreach (var bi in input)
                {
                    var candidateArea = bi.size.x * bi.size.y * (bi.size.z > 0 ? bi.size.z: 1);
                    if (!(candidateArea > area))
                        continue;
                    area   = candidateArea;
                    result = bi;
                }

                return result;
        }
   }
}
