using System.Collections;
using System.Collections.Generic;
using TilePlus;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TilePlusDemo
{

    public class BasicDemo : MonoBehaviour
    {
        /// <summary>
        /// a reference to the tilemap in use.
        /// </summary>
        public Tilemap m_Tilemap;

        //offsets for moving the tiles
        private readonly Vector3Int[] offsets = new[]
            { Vector3Int.down, Vector3Int.right, Vector3Int.up, Vector3Int.left };

        private IEnumerator Start()
        {
            if (m_Tilemap == null)
            {
                Debug.LogError("Missing tilemap reference in BasicDemo component!");
                yield break;
            }

            yield return null;
            yield return null;
            while (!TpLib.TpLibIsInitialized)
                yield return null;

            var offsetIndex = 0;
            var first = true;
            var changeSlide = 0;
            var tiles = new List<TilePlusBase>();
            while (true)
            {
                yield return new WaitForSeconds(0.5f);

                TpLib.GetAllTilesOfType(m_Tilemap, null,
                    ref tiles); //with second param=null, this will get all TPT tiles.

                var tilesList = tiles.ToArray(); //to avoid multiple enumeration

                //note that foreach here would fail because the enumeration would be modified 
                for (var i = 0; i < tilesList.Length; i++)
                {
                    var tile = tilesList[i]; //get a tile
                    var oldPos = tile.TileGridPosition;


                    if (tile is TpSlideShow slideShow && slideShow.ParentTilemap != null)
                    {
                        if (++changeSlide == 16)
                        {
                            changeSlide = 0;
                            slideShow.SlideIndex++; //example of changing the slide yourself. Result depends on wrap mode of tile instance.
                            slideShow.ParentTilemap.SetAnimationFrame(slideShow.TileGridPosition, slideShow.SlideIndex);
                            //when you change the slide yourself you have to set the frame.
                            //note that if you call slideShow.ChangeSlide() the above two lines aren't needed.
                        }

                        continue; //don't want to move this tile.
                    }

                    var newPos = oldPos + offsets[offsetIndex];

                    //move it a little
                    TpLib.CutAndPasteTile(m_Tilemap, oldPos, newPos);
                    if (first) //make copies in just the first pass thru this.
                        TpLib.CopyAndPasteTile(m_Tilemap, tile, oldPos + Vector3Int.left);

                }

                first = false;
                //index check for offsets array
                if (++offsetIndex >= offsets.Length)
                    offsetIndex = 0;

            }



        }


    }
}
