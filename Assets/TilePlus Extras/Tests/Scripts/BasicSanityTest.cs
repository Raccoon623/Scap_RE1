using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TilePlus;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TilePlusDemo
{
    /// <summary>
    /// This is a basic sanity test for TpLib
    /// </summary>
    [RequireComponent(typeof(Tilemap))]
    public class BasicSanityTest : MonoBehaviour
    {
        /// <summary>
        /// Message for users
        /// </summary>
        [TextArea]
        public string  m_Message = "Note that if the Tilemap is inspected and the info foldout is open, OR if the TP Utility Window is open, the editor may run really slowly during the large scene tests.";

        /// <summary>
        /// Do the large scene tests? 
        /// </summary>
        public  bool               m_DoLargeSceneTests = true;
        /// <summary>
        /// Do animation tests?
        /// </summary>
        public  bool               m_DoAnimationTests  = true;
        
        /// <summary>
        /// A plain tileplusbase tile
        /// </summary>
        public  TilePlusBase       m_Asset;
        /// <summary>
        /// A TpAnimatedTile
        /// </summary>
        public  TpAnimatedTile     m_TpAnimatedTile;
        /// <summary>
        /// A FlexAnimatedTile
        /// </summary>
        public  TpFlexAnimatedTile m_FlexAnimatedTile;
        
        /// <summary>
        /// How long between tests?
        /// </summary>
        public  float              m_WaitTime       = 1;
        /// <summary>
        /// How many tiles moves in the prefab test
        /// </summary>
        public  int                m_NumPrefabMoves = 16;
        
        private Vector3Int         startPosition    = new Vector3Int(-200, 0, 0);
        private Vector3Int         endPosition      = new Vector3Int(200, 10, 0);
        private Tilemap            tilemap;
        private IEnumerator Start()
        {
            Camera.main!.orthographicSize = 128;
            while (!TpLib.TpLibIsInitialized)
                yield return null;
            yield return null;

            var allocData = new TpLibMemAlloc
            {
                m_TypesSize = 1024,
                m_GuidDictSize = 1024,
                m_TilemapAndTagDictsSize = 1024
            };
            TpLib.Resize(allocData,false);
            
            
            TpLib.MaxNumClonesPerUpdate = 0; //unlimited (unit.maxvalue)
            TpLib.MaxNumDeferredCallbacksPerUpdate = 0; //unlimited (uint.maxvalue)
            tilemap = GetComponent<Tilemap>();
            tilemap.ClearAllTiles();

            var tiles = new List<TilePlusBase>();

            if (m_DoLargeSceneTests)
            {

                var num = (endPosition.x - startPosition.x) * (endPosition.y - startPosition.y);    
                
                var tileChangeData = new TileChangeData[num];
                var n = 0;
                for (var x = startPosition.x; x < endPosition.x; x++)
                {
                    for (var y = startPosition.y; y < endPosition.y; y++)
                    {
                        //this cloning is NOT something you ordinarily need to do. This is just for max speed.
                        var clone = Instantiate(m_Asset); //clone the copied tile; we need a new instance.
                        clone.ResetState(TileResetOperation.MakeCopy); //reset state variables like grid position etc.
                        //note that ResetState nulls the GUID so we have a brief chance to change it before placing the tile.
                        clone.TileGuidBytes = Guid.NewGuid().ToByteArray();
                        
                        tileChangeData[n++] = new TileChangeData(new Vector3Int(x, y, 0), clone, Color.white,
                            Matrix4x4.identity);
                    }
                }

                tilemap.SetTiles(tileChangeData, true);

              

                Debug.Log($"Instantiated {n} TilePlusBase instances from {startPosition} to {endPosition}. Wait...");

                yield return new WaitForSeconds(m_WaitTime);


                var offset    = new Vector3Int(0, -20, 0);
                var positions = new HashSet<Vector3Int>();
                TpLib.GetAllPositionsForMap(tilemap, ref positions, false);  //position hashset test

                foreach (var pos in positions)
                {
                    var newPos = pos + offset;
                    TpLib.CutAndPasteTile(tilemap, pos, newPos, null,true); //cut/paste test
                }


                Debug.Log("Executed Cut and paste TilePlusBase instances 20 units below. Wait...");
                yield return new WaitForSeconds(m_WaitTime);


                offset.y = 10;
                TpLib.GetAllTiles(ref tiles); //TileTypes test

                foreach (var tile in tiles)
                    TpLib.CopyAndPasteTile(tilemap, tile, tile.TileGridPosition + offset);

                Debug.Log("Executed Copy and paste TilePlusBase instances 10 units above. Wait...");
                yield return new WaitForSeconds(2);

                TpLib.DeleteTileBlock(tilemap,TpLib.GetAllTilesRaw.ToList());

                
                Debug.Log("Deleted all tiles. Wait...");
                yield return new WaitForSeconds(m_WaitTime);
            }

            
            Camera.main.orthographicSize = 8;

            if (m_DoAnimationTests)
            {
                //create 16 tiles of each type
                for (var x = 0; x < 4; x++)
                {
                    for (var y = 0; y < 4; y++)
                    {
                        tilemap.SetTile(new Vector3Int(x, y, 0), m_TpAnimatedTile);
                        tilemap.SetTile(new Vector3Int(x, y + 6, 0), m_FlexAnimatedTile);
                    }
                }

                //it's important to note that tiles painted programmatically won't start animating even if the PlayOnStart field is set true in the inspector.
                //That's because OnEnable isn't executed in the tiles after cloning(Unity _bug? Unknown). Painted tiles (via a brush) don't have this issue.

                Debug.Log("Created AnimatedTiles and FlexAnimatedTiles. Next, enable animation on all tiles using tag 'tile'. Wait...");
                yield return new WaitForSeconds(m_WaitTime);

                //get all tags for tile type
                var tileHash = new HashSet<string>();
                TpLib.GetAllTagsUsedByTileType(typeof(TpFlexAnimatedTile), ref tileHash); //result should be "flex" and "tile"
                if(tileHash!.Count != 2)
                    Debug.LogError("TileHash for TpFlexAnimatedTile in test should have two tags!");
                Debug.Log("FlexAnimatedTile should have tags 'flex' and 'tile'");
                foreach(var s in tileHash)
                    Debug.Log($"Tilehash item: {s}");
                
                TpLib.GetTilesWithTag(tilemap, "tile", ref tiles);  //tag test: the two animated tiles have this tag in common
                
                
                Debug.Log($"Found {tiles.Count} tiles with tag 'tile', activating animation. Wait...");

                foreach (var tile in tiles)  //start animation test
                {
                    switch (tile)
                    {
                        case TpAnimatedTile t:
                            t.ActivateAnimation(true);
                            break;
                        case TpFlexAnimatedTile ft:
                            ft.ActivateAnimation(true);
                            break;
                    }
                }
                yield return new WaitForSeconds(m_WaitTime);

                TpLib.GetTilesWithTag(tilemap, "flex", ref tiles);  //tag test: the flex tile has this tag as well
                var nFlexTagTiles = tiles.Count;
                Debug.Log($"Found {nFlexTagTiles} tiles with tag 'flex', changing animation. Wait...");

                foreach (var tile in tiles)  //change animation test
                {
                    if (tile is TpFlexAnimatedTile t)
                        t.ChangeAnimation("centipede1");
                }
                
                TpLib.GetAllTilesOfType(null,typeof(TpFlexAnimatedTile),ref tiles);  //tile types test
                var secondTest = new List<TilePlusBase>();
                TpLib.GetAllTilesOfType(tilemap,typeof(TpFlexAnimatedTile), ref secondTest);
                var thirdTest = new List<TilePlusBase>();
                TpLib.GetAllTilesOfType(tilemap,null, ref thirdTest);
                
                if(secondTest.Count != tiles.Count && thirdTest.Count != tiles.Count)
                    Debug.LogError("Test failed: GetAllTilesOfType");
                
                Debug.Log($"Passed:[{(tiles.Count == nFlexTagTiles)}] Found {tiles.Count} TpFlexAnimated tiles. Should match number of tiles tagged 'flex' - that was {nFlexTagTiles}");

                //test of Generic version
                var tpflexTiles = new List<TpFlexAnimatedTile>();
                TpLib.GetAllTiles(ref tpflexTiles);
                var nTpFlexTiles = tpflexTiles.Count;

                Debug.Log($"Passed:[{(nTpFlexTiles == nFlexTagTiles)}] Found {nTpFlexTiles} TpFlexAnimated tiles via generic fetch. Should match number of tiles tagged 'flex' - that was {nFlexTagTiles}");
                Debug.Log("Wait...");
                yield return new WaitForSeconds(m_WaitTime);
                Debug.Log("Halting animation of FlexTiles");
                foreach(var tflex in tpflexTiles)
                    tflex.ActivateAnimation(false);
                
                Debug.Log("Wait...");
                yield return new WaitForSeconds(m_WaitTime);

                
                TpLib.DeleteTileBlock(tilemap,TpLib.GetAllTilesRaw.ToArray());
                
            }

            
            
            
            Debug.Log("Wait...");
            yield return new WaitForSeconds(m_WaitTime);
            Debug.Log("Done");
            #if UNITY_EDITOR
            EditorApplication.isPlaying = false;
            #endif


        }

    }
}
