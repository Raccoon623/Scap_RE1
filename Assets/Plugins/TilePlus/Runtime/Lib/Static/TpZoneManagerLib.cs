// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 03-25-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 03-31-2023
// ***********************************************************************
// <copyright file="TpZoneManagerLib.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TilePlus
{
    /// <summary>
    /// Utility methods for working with the Zone Manager
    /// </summary>
    public static class TpZoneManagerLib
    {

        /// <summary>
        /// Given an asset registration loaded from a file system, after unpacking from JSON
        /// (or equivalent), take the GUIDs from the last run of the app and find matching
        /// tiles in a new asset reg returned from LoadTileFab. Add matches to forward and reverse
        /// lookup tables, the latter is used to delete entries when a chunk is unloaded.
        /// If you're trying to do this some other way then use this as a guide and note that
        /// there's a property in TpLib that lets you add your own GUID lookup tables if you want.
        /// </summary>
        /// <param name="loadedZoneReg">"OLD" asset registration from saved asset registrations</param>
        /// <param name="newGuidZoneReg">"NEW" asset registrations as output from LoadTileFab.</param>
        /// <param name="forwardLookup">ref to forward oldGuid-to-newGuid mapping</param>
        /// <param name="reverseLookup">ref to reverse newGuid-to-oldGuid reverse mapping</param>
        public static void UpdateGuidLookup(ZoneReg       loadedZoneReg, 
                               ZoneReg                    newGuidZoneReg, 
                               ref Dictionary<Guid,Guid>? forwardLookup,
                               ref Dictionary<Guid,Guid>? reverseLookup)
        {
            
            if(forwardLookup == null  || reverseLookup == null)
                return;
            
            //previous mapping of GUIDs to TPT tile positions
            var oldPosToGuidMap = loadedZoneReg.ptgm;
            //loaded-in mapping of GUIDs to TPT tile positions
            var newPosToGuidMap = newGuidZoneReg.ptgm;

            foreach (var olditem in oldPosToGuidMap)
            {
                var numItems = olditem.PosToTileGuidMap.Count;
                if(numItems == 0) //if no mapping
                    continue;
                
                var oldAssetGuid = olditem.m_AssetGuid;
                var match        = newPosToGuidMap.FirstOrDefault(x => x.m_AssetGuid == oldAssetGuid);
                if(match == null)
                    continue;
                foreach (var oldItemKvp in olditem.PosToTileGuidMap)
                {
                    //here we have oldItemKvp, which is a V3I,string
                    //where the V3I is the position of the tile when it was last placed and string is the tile's GUID
                    //we want to find a matching position in the 'match' where the match is the tileToGuidMap reflecting
                    //where the Bundle was placed when the scene was restored from the saved registration data.
                    var positionMatch = match.PosToTileGuidMap.FirstOrDefault(newkvp => newkvp.Key == oldItemKvp.Key);
                    if (positionMatch.Key == Vector3Int.zero || string.IsNullOrEmpty(positionMatch.Value))
                        continue;
                    var oldItemGuid = new Guid(oldItemKvp.Value);
                    var newItemGuid = new Guid(positionMatch.Value);
                    forwardLookup.Add(oldItemGuid,newItemGuid);
                    reverseLookup.Add(newItemGuid,oldItemGuid);
                }
            }
        }


        /// <summary>
        /// Create a Vector2Int with the size of the camera view dimensions, rounded up to the nearest
        /// EVEN integer values. The EVEN values part is very important if you decide to implement
        /// this in some other fashion for a non-ortho camera.
        /// </summary>
        /// <param name="cam">The camera to use when calculating.</param>
        /// <param name="sizeOfChunks">The size of chunks. Obtain from ZoneManager.DefaultLocator.size.x if possible.</param>
        /// <param name="padding">Enlarge the area by multiples of the chunk size (as obtained from ZoneManager) if not null. Note that if the result is
        /// not EVEN numbers then an additional one unit will be added</param>
        /// <returns>the camera view dimensions with optional padding. Vector2Int.zero is
        /// returned if the cam isn't orthographic. .</returns>
        public static Vector2Int OrthographicCameraViewDimensions(Camera     cam,
                                                                  int         sizeOfChunks,
                                                                  Vector2Int? padding = null)
        {
            if (!cam.orthographic)
            {
                Debug.LogError("Camera must be orthographic!!");
                return Vector2Int.zero;
            }

            var viewHeight = cam.orthographicSize * 2; //this is the 0.5 * height of the view in world space.
            var viewWidth  = viewHeight * cam.aspect;  //this is the width/height.

            if (padding.HasValue)
            {
                var pad = padding.Value;
                viewHeight += (float)(pad.y * sizeOfChunks);
                viewWidth  += (float)(pad.x * sizeOfChunks);
            }

            //compute the cameraViewDimensions. Note that they are assumed constant for this example.
            //this is the dimensions we'll use when using ChunkLocators
            var width  = Mathf.CeilToInt(viewWidth);
            var height = Mathf.CeilToInt(viewHeight);
            
            //it's SUPER important that the height and width of the return value are EVEN numbers.
            //this is needed so that SubdivideRectInt works correctly.
            if (width % 2 != 0) //remainder should be zero if this is a multiple of 2.
                width++;
            if (height % 2 != 0)
                height++;
            return new Vector2Int(width, height);
        }

        /// <summary>
        /// Calculate the number of Chunks of chunkSize in the Camera view
        /// </summary>
        /// <param name="cameraViewDims">Return value from OrthographicCameraViewDimensions</param>
        /// <param name="chunkSize">Size of chunks (4,6,8....n) where N is always even.</param>
        /// <returns>-1 for error</returns>
        public static int NumberOfChunksInCameraView(Vector2Int cameraViewDims, int chunkSize)
        {
            var camDimX = (float)cameraViewDims.x;
            var camDimY = (float)cameraViewDims.y;
            
            if (chunkSize < 4) //this would be a 4x4 TileFab which is ridiculously (?) small.
                chunkSize = 4;
            //test for an even number. 
            if (chunkSize % 2 != 0) //remainder should be zero if this is a multiple of 2.
                chunkSize++;
            
            var chunkf  = (float)chunkSize;
            if (chunkf != 0)
                return Mathf.CeilToInt((camDimX / chunkf) + (camDimY / chunkf));
            Debug.LogError("Cannot have TileFab chunk size of 0");
            return 0;

        }


        /// <summary>
        /// Given an input RectInt subdivide it into RectInts of a specific size.
        /// </summary>
        /// <param name="input">The source RectInt</param>
        /// <param name="subdivisionSize">the size of each new RectInt. If not even, this value is rounded up WITHOUT a warning.
        /// Normally this should just be the chunk size which is easily obtained via a TpZoneManager property</param>
        /// <param name="output">a ref List of RectInts where the output is placed. Cleared on entry after error checks.</param>
        /// <param name="zoneManager">Zone manager reference.</param>
        /// <returns>the actual subdivision size used after rounding (if any). If value is 0 then that's an error.</returns>
        /// <remarks>Note that this works correctly since chunks have have even-number size. It errs on the side of returning
        /// too many locations rather than fewer which is fine for the ZoneManager system.</remarks>
        public static int SubdivideRectInt(RectInt input, int subdivisionSize, ref List<RectInt>? output, TpZoneManager? zoneManager)
        {
            if (output == null || zoneManager == null)
            {
                Debug.LogError("Call to SubdivideRectInt has null output list or param zoneManager was null.");
                return 0;
            }

            var modulo    = zoneManager.DefaultLocator.size.x;
            var remainder = subdivisionSize % modulo;
            if (remainder != 0)
                subdivisionSize += remainder;  //rounding up the size to a multiple of the chunk size.

            output.Clear();
            var size     = new Vector2Int(subdivisionSize, subdivisionSize); //this is the size of all the RectInts
            var starting = input.position;
            var current  =  starting; //recall that the position of a RectInt is the LL corner.
            //create a list of RectInts of size 'subdivisionSize' where only the position is different.
            do
            {   //there's always going to be at least one subdivision even in the degenerate case where input.size = size (subdivisionSize)
                output.Add(zoneManager.GetLocatorForGridPosition((Vector3Int) current,size));
                current.x += subdivisionSize; //next position in X
                if (input.Contains(current))  //if this is still inside the input RectInt then keep going.
                    continue;
                //restore starting.x, ie go back to starting COLUMN coord X
                current.x = starting.x; 
                //advance to next Higher row. This is because we start
                //at the lowest Y coord since a RectInt origin is in the lower-left corner
                current.y += subdivisionSize; //note that the test on the next line ends the loop
                                     //when Y gets outside of the upper border of the input RectInt.
            } while (input.Contains(current));

            return subdivisionSize;
        }
    }
}
