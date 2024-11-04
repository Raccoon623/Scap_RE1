// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 02-15-2024
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 02-15-2024
// ***********************************************************************
// <copyright file="TpPainterBulkOps.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Bulk move operations</summary>
// ***********************************************************************
#nullable enable

using UnityEngine;
using UnityEngine.Tilemaps;
using static TilePlus.TpLib;

namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// Bulk-move operations for Painter support.
    /// </summary>
    public static class TpPainterBulkOps
    {
        /// <summary>
        /// Place many tiles at once
        /// </summary>
        /// <param name="map">destination Tilemap</param>
        /// <param name="tiles">array of tiles. </param>
        /// <param name="positions">array of positions</param>
        /// <param name="colors">optional array of colors or null</param>
        /// <param name="transforms">optional array of transforms or null</param>
        /// <param name="forcedTransform">an optional single transform to force on all tiles</param>
        /// <param name="forcedColor">an optonal single color to force on all tiles</param>
        /// <param name = "cellsModifiedByBrush" >set TRUE if the Clipboard Object has had its transform modded by
        /// the Clipboard's embedded GridBrush instance</param>
        /// <param name = "acceptClones" >Set this TRUE to allow cloned tiles. Used primarily with Painter's MOVE action with multiple-selections. Normally, This method will reject clone tiles in the tiles array.</param>
        /// <remarks>Note that if any of the instances in tiles[] are TPT clones,
        /// re-clone them prior to calling this! Otherwise a warning message will be sent
        /// to the console and such tiles are skipped.</remarks>
        internal static void BulkOp(Tilemap      map,
                                    TileBase[]   tiles,
                                    Vector3Int[] positions,
                                    Color[]?     colors               = null,
                                    Matrix4x4[]? transforms           = null,
                                    Matrix4x4?   forcedTransform      = null,
                                    Color?       forcedColor          = null,
                                    bool         cellsModifiedByBrush = false,
                                    bool         acceptClones         = false)

        {
            var n = tiles.Length;
            if (n == 0)
            {
                if (Errors)
                    TpLogError("T+P: BulkOp: nTiles 0!!!");
                return;
            }

            if (tiles.Length != positions.Length)
            {
                if (Errors)
                    TpLogError("T+P: BulkOp: nTiles != nPositions!!!");
                return;
            }

            if (Informational)
                TpLog($"BulkOp: {positions.Length} positions affected. CellsModifiedByBrush? {cellsModifiedByBrush}");

            var tileChangeData = new TileChangeData[n];

            //if cells were modified in the brush (rotated/flipped) then no more mods are possible (or make sense)
            var warningShown = false;
            if (cellsModifiedByBrush)
            {
                for (var i = 0; i < n; i++)
                {
                    var tile = tiles[i];
                    if (tile == null)
                        continue;
                    // ReSharper disable once Unity.NoNullPatternMatching
                    if (!acceptClones && tile is ITilePlus { IsClone: true })
                    {
                        if (!warningShown)
                        {
                            warningShown = true;
                            TpLog("BulkOp: Clone TPT tiles aren't allowed here, and are skipped. Warning shown only once.");
                        }

                        continue;
                    }

                    var pos = positions[i];
                    // ReSharper disable once Unity.NoNullPatternMatching
                    if (tile is Tile t)
                    {
                        var colr = colors != null
                                       ? colors[i]
                                       : t.color;
                        var trns = transforms != null
                                       ? transforms[i]
                                       : t.transform;
                        tileChangeData[i] = new TileChangeData(pos, tile, colr, trns);
                    }
                    else
                        tileChangeData[i] = new TileChangeData(pos, tile, Color.white, Matrix4x4.identity);
                }

                TpLib.InhibitOnTpLibChanged = true; //avoid a zillion OnTpLibChanged events.
                map.SetTiles(tileChangeData, true);
                TpLib.InhibitOnTpLibChanged = false;
                //send ONE OnTpLibChanged event.
                TpLib.ForceOnTpLibChanged(TpLibChangeType.Modified, true, Vector3Int.zero, null);
                return;
            }

            //othewise check for  a default tile wrapper and apply it. Change flags if necc.
            var forceTransform = forcedTransform.HasValue;
            var forceColor     = forcedColor.HasValue;

            var applyColor           = forceColor;
            var applyTransform       = forceTransform;
            var ignoreTileFlagsOnMap = false;
            var color = forceColor
                            ? forcedColor!.Value
                            : Color.white;
            var trans = forceTransform
                            ? forcedTransform!.Value
                            : Matrix4x4.identity;

            var wrapper = TpPainterModifiers.instance.TilesDefault;
            if (wrapper != null) //if there's an active wrapper, get its properties
            {
                applyColor     |= wrapper.AffectsColor;
                applyTransform |= wrapper.AffectsTransform;
                //note that forcedColor and forcedTransform can't be null if forceColor or forceTransform are true.
                color = forceColor
                            ? forcedColor!.Value
                            : wrapper.m_Color;
                trans = forceTransform
                            ? forcedTransform!.Value
                            : wrapper.m_Matrix;
            }

            //if wrapper isn't null and forceColor or forceTransform are false.
            //here we can use the optional transform and color arrays
            //fastest!
            if (wrapper is { m_EditActions: EditActions.None } && !forceColor && !forceTransform)
            {
                var subColor     = colors != null;
                var subTransform = transforms != null;
                for (var i = 0; i < n; i++)
                {
                    var tile = tiles[i];
                    if (tile == null)
                        continue;
                    // ReSharper disable once Unity.NoNullPatternMatching
                    if (!acceptClones && tile is ITilePlus { IsClone: true })
                    {
                        if (!warningShown)
                        {
                            warningShown = true;
                            TpLog("BulkOp: Clone TPT tiles aren't allowed here, and are skipped. Warning shown only once.");
                        }

                        continue;
                    }

                    var pos = positions[i];
                    // ReSharper disable once Unity.NoNullPatternMatching
                    if (tile is Tile t)
                    {
                        var colr = subColor
                                       ? colors![i]
                                       : t.color;
                        var matrix = subTransform
                                         ? transforms![i]
                                         : t.transform;
                        tileChangeData[i] = new TileChangeData(pos, tile, colr, matrix);
                    }
                    else
                        tileChangeData[i] = new TileChangeData(pos, tile, Color.white, Matrix4x4.identity);
                }
            }
            else //apply color/transform when building the TileChangeData array
            {
                ignoreTileFlagsOnMap = forceColor || forceTransform;

                for (var i = 0; i < n; i++)
                {
                    var tile = tiles[i];
                    if (tile == null)
                        continue;
                    // ReSharper disable once Unity.NoNullPatternMatching
                    if (!acceptClones && tile is ITilePlus { IsClone: true })
                    {
                        if (!warningShown)
                        {
                            warningShown = true;
                            TpLog("BulkOp: Clone TPT tiles aren't allowed here, and are skipped. Warning shown only once.");
                        }

                        continue;
                    }

                    var pos = positions[i];
                    // ReSharper disable once Unity.NoNullPatternMatching
                    if (tile is Tile t)
                        tileChangeData[i] = new TileChangeData(pos, tile, applyColor
                                                                              ? color
                                                                              : t.color, applyTransform
                                                                                             ? trans
                                                                                             : t.transform);
                    else
                        tileChangeData[i] = new TileChangeData(pos, tile, applyColor
                                                                              ? color
                                                                              : Color.white, applyTransform
                                                                                                 ? trans
                                                                                                 : Matrix4x4.identity);
                }
            }

            //push the TileChangeData to the tilemap.
            TpLib.InhibitOnTpLibChanged = true; //avoid a zillion OnTpLibChanged events.
            map.SetTiles(tileChangeData, ignoreTileFlagsOnMap);
            TpLib.InhibitOnTpLibChanged = false;
            //send ONE OnTpLibChanged event.
            TpLib.ForceOnTpLibChanged(TpLibChangeType.Modified, true, Vector3Int.zero, null);
        }
    }
}
