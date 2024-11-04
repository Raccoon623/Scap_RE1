// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 11-29-2022
// ***********************************************************************
// <copyright file="TilePlusBrush.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Tile Plus Brush implementation</summary>
// ***********************************************************************

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;
using static TilePlus.Editor.TpLibEditor;

// ReSharper disable MissingXmlDoc
#nullable enable

namespace TilePlus.Editor
{
    /// <summary>
    ///     TilePlusBrush is a brush designed to work with any tiles, but adds functionality specific
    ///     to TilePlusBase-derived tiles, such as preventing overwrites and limiting painting to only certain
    ///     named tilemaps in your project.
    /// </summary>

    //[CreateAssetMenu]  //uncomment only if you delete the instance of this brush in Tileplus/Editor/Resources/TilePlus "DefaultTilePlusBrush.asset"
    
    //this next line makes this the default brush. Comment/Uncomment it if you want.
    [CustomGridBrush(false, true, true, "Tile+Brush")]
    // this next line should be commented-out if you uncomment the line just above. 
    //[CustomGridBrush(false, false, false, "Tile+Brush")]
    public class TilePlusBrush : GridBrush
    {
        //state variables used by the Brush editor.
        public        bool m_ShowHelpBar = true; //hide/show the info (top) bar in the inspector
        public        bool m_FloodFillPreview;
        public        bool m_ShowBrushToggles       = true;
        public        bool m_ShowAssetInfo          = true;

        public bool NoOverwriteFromPalette => TilePlusConfig.instance.NoOverwriteFromPalette;
        public bool AllowOverwriteOrIgnoreMap => TpEditorUtilities.AllowPaintingOverwrite; 

        //these lists will contain tilemaps which are excluded from painting and included for painting.
        private  List<string> excludedMaps = new List<string>();
        private  List<string> includedMaps = new List<string>();

        
        //BoxFill implementation
        public override void BoxFill(GridLayout gridLayout, GameObject brushTarget, BoundsInt position)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (brushTarget == null)
                return;

            var map = brushTarget.GetComponent<Tilemap>();
            if (map == null)
                return;
            if (cellCount == 0)
                return;
            
            if (!AllowOverwriteOrIgnoreMap &&  brushTarget.TryGetComponent(typeof(TpNoPaint), out _)) //141 addition
            {
                Debug.Log($"*** {map.name} has the TpNoPaint component: No painting allowed. Override with hotkey.");
                return;
            }

            (var noPaintLocked, (_, _, _, var inStage)) = NoPaint(map);
            
            if(noPaintLocked)
            {
                Debug.Log("Can't paint a locked Tilemap nor one in a Prefab stage!");
                return;
            }

            var testForLockedTileWhenPaintingInPrefabStage = !noPaintLocked &&  inStage;

            var tilemapIsPalette = TpLib.IsTilemapFromPalette(map); 
            
            var cels                             = cells; //save repeated access of the property
            var skippedBecauseSourceTileUnlocked = 0;
            //loop thru the tiles
            foreach (var gridPosition in position.allPositionsWithin)
            {
                var local = gridPosition - position.min;
                var cell  = cels[GetCellIndexWrapAround(local.x, local.y, local.z)];
                if (cell.tile == null)
                    continue;

                // ReSharper disable once Unity.NoNullPatternMatching
                if (cell.tile is ITilePlus iTilePlusInstance)
                {
                    if (testForLockedTileWhenPaintingInPrefabStage)
                    {
                        if (!iTilePlusInstance.IsLocked)
                        {
                            skippedBecauseSourceTileUnlocked++;
                            continue;
                        }
                    }

                    if (iTilePlusInstance.IsLocked)
                    {
                        Debug.Log("Cannot paint Locked TilePlus tiles!!");
                        continue;
                    }

                    /*is it an instance of the tile?
                    This determines if we use the Tile asset from the palette or one from the scene.
                    */
                    var tileIsFromPalette = !iTilePlusInstance.IsClone; //if not a clone it's from the palette
                   
                    if (tileIsFromPalette && !tilemapIsPalette) //from palette, a few extra things to do
                    {
                        //check for overwrite.
                       if (!AllowOverwriteOrIgnoreMap && NoOverwriteFromPalette && map.GetTile(gridPosition) != null)
                        {
                            if(Warnings)
                                Debug.Log($"*** skipping location {gridPosition.ToString()} due to overwrite of tile.");
                            continue;
                        }

                        //check for destination restrictions ie can this tile be painted on this tilemap?
                        var restrictions = AllowOverwriteOrIgnoreMap ? null :  iTilePlusInstance.PaintMaskList;
                        //if the paintmasklist has restrictions, separate them into included and excluded maps.
                        if (restrictions is { Count: > 0 })
                        {
                            ParsePaintMask(restrictions, ref includedMaps, ref excludedMaps);

                            //see if this tilemap is paintable for this tile instance
                            var noPaint = false;
                            var mapName = map.name.ToLowerInvariant();
                            if(excludedMaps.Count > 0 &&  excludedMaps.Contains(mapName))
                            {
                                Debug.Log($"Currently selected tilemap is excluded from painting in the tile's PaintMask, Current map is {mapName}.");
                                noPaint = true;
                            }
                            if (!noPaint && includedMaps.Count > 0 &&  !includedMaps.Contains(mapName))
                            {
                                Debug.Log($"Currently Selected tilemap isn't included in the tile's PaintMask, Current map is {mapName}.");
                                noPaint = true;
                            } 
                            if(noPaint)
                                continue;
                        }
                        //place TilePlusBase from palette.
                        map.SetTile(gridPosition, cell.tile);
                    }
                    else //place tile moved around in scene; we're using the clone.
                        map.SetTile(gridPosition, iTilePlusInstance as TileBase); 
                }
                else //place a 'normal' tile.
                {
                    //check for overwrite. 
                    if (!AllowOverwriteOrIgnoreMap && NoOverwriteFromPalette && map.GetTile(gridPosition) != null)
                    {
                        if(Warnings)
                            Debug.Log($"*** skipping location {gridPosition.ToString()} due to overwrite of tile.");
                        continue;
                    }
                   
                    map.SetTile(gridPosition, cell.tile);
                }

                map.SetTransformMatrix(gridPosition, cell.matrix);
                map.SetColor(gridPosition, cell.color);
            }

            if (skippedBecauseSourceTileUnlocked != 0)
            {
                ToolManager.SetActiveTool<SelectTool>(); 
                TpPreviewUtility.ClearPreview(); 
                TpLib.DelayedCallback(this, () =>
                                            {
                                               
                                                
                                                EditorUtility.DisplayDialog("Try again!", $"{skippedBecauseSourceTileUnlocked} position(s) were skipped: \nYou can only paint LOCKED tiles into a Tile+ Prefab!\nSee the user guide.", "Continue");
                                            },"T+Brush: warning-locked",250);
            }
        }

        //box erase implementation
        public override void BoxErase(GridLayout gridLayout, GameObject brushTarget, BoundsInt position)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (brushTarget == null)
                return;

            var map = brushTarget.GetComponent<Tilemap>();
            if (map == null)
                return;

            if (!AllowOverwriteOrIgnoreMap &&  brushTarget.TryGetComponent(typeof(TpNoPaint), out _)) //141 addition
            {
                Debug.Log($"*** {map.name} has the TpNoPaint component: No erasing allowed. Override with hotkey.");
                return;
            }
            (var noPaintLocked, _) = NoPaint(map);

            if(noPaintLocked)
            {
                Debug.Log("Can't erase tiles on a locked Tilemap nor  in a Prefab stage!");
                return;
            }
            

            base.BoxErase(gridLayout, brushTarget, position);
        }

        // ReSharper disable once AnnotateNotNullParameter
        public override void MoveStart(GridLayout gridLayout, GameObject brushTarget, BoundsInt position)
        {
            var map = brushTarget.GetComponent<Tilemap>();
            if (map == null)
                return;
            
            (var noPaintLocked, _) = NoPaint(map);

            if(noPaintLocked)
            {
                Debug.Log("Can't move tiles on a locked Tilemap nor in a Prefab stage!");
                return;
            }
            

            base.MoveStart(gridLayout, brushTarget, position);
        }


        

        // ReSharper disable once AnnotateNotNullParameter
        public override void Paint(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
        {
            var map = brushTarget.GetComponent<Tilemap>();
            if (map == null)
                return;
            
            (var noPaintLocked, _) = NoPaint(map);

            if(noPaintLocked)

            {
                Debug.Log("Can't paint a locked Tilemap nor one in a Prefab stage!");
                return;
            }
            
            
            base.Paint(gridLayout, brushTarget, position);
        }

        // ReSharper disable once AnnotateNotNullParameter
        public override void Erase(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
        {
            var map = brushTarget.GetComponent<Tilemap>();
            if (map == null)
                return;
            (var noPaintLocked, _) = NoPaint(map);

            if(noPaintLocked)
            {
                Debug.Log("Can't erase tiles on a locked Tilemap nor in a Prefab stage!");
                return;
            }

            base.Erase(gridLayout, brushTarget, position);
        }


        public override void FloodFill(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
        {
            if (cellCount == 0)
                return;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (brushTarget == null)
                return;

            var map = brushTarget.GetComponent<Tilemap>();
            if (map == null)
                return;

            if (!AllowOverwriteOrIgnoreMap &&  brushTarget.TryGetComponent(typeof(TpNoPaint), out _)) //141 addition
            {
                Debug.Log($"*** {map.name} has the TpNoPaint component: No floodfill allowed. Override with hotkey.");
                return;
            }
            
            (var noPaintLocked, _) = NoPaint(map);

            if(noPaintLocked)
            {
                Debug.Log("Can't flood-fill tiles on a locked Tilemap nor in a Prefab stage!");
                return;
            }
            

            var tile = cells[0].tile;
            
            if((tile as ITilePlus) != null) 
                Debug.Log("You can't flood-fill TilePlus tiles."); 
            else
                base.FloodFill(gridLayout, brushTarget, position);
        }

        public override void Rotate(RotationDirection direction, GridLayout.CellLayout layout)
        {
            var ok = true;
            foreach (var cel in cells)
            {
                var test = cel.tile as TilePlusBase;
                if (test == null || test.IsRotatable)
                    continue;
                ok = false;
                break;
            }

            if (ok)
                base.Rotate(direction, layout);
            else
                Debug.Log("Tiles to rotate can't have a non-rotatable TilePlus tile. ");
        }

        
    }
}
