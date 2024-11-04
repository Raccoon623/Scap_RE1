// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 12-29-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-29-2023
// ***********************************************************************
// <copyright file="TpPainterGridSelectionUtils.cs" company="Jeff Sasmor">
//     Copyright (c)  Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

#nullable enable
namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// Grid Selection utilities for Tile+Painter
    /// </summary>
    public static class TpPainterGridSelectionUtils
    {
        //private const string TooBigError = "Automatic icon generation limited to 64 tiles size.";
        
        /// <summary>
        /// Bundle a single tilemap from the Grid Selection pane.
        /// </summary>
        internal static async Task<bool> BundleOneMap()
        {
            await Task.Yield();
            await Task.Yield();
            await Task.Yield();
            await Task.Yield();
            
            //ensure valid grid selection
            (var valid, var map) = ValidGridSelection;
            if (!valid || map == null)
            {
                if(TpLibEditor.Informational)
                    TpLib.TpLog("Invalid Grid Selection...");
                return false;
            }

            var path = TpEditorUtilities.GetPathFromUser("Select destination folder for saving the Bundle.");
            if (path == string.Empty)
                return false;
                
            var gridselPosition = GridSelection.position; //the grid selection disappears during the process but need it later so keep a copy.
            var rootGo          = GridSelection.grid.gameObject;
            
            //get a base name for the assets.
            var wait             = true;
            var possibleFilename = "SceneBundle";
            /*var possibleFilename = (GridPaintingState.palette != null
                                    && GridPaintingState.palette.gameObject != null
                                    && !string.IsNullOrEmpty(GridPaintingState.palette.gameObject.name))
                                       ? GridPaintingState.palette.gameObject.name
                                       : "_Bundle";*/
            var dialog = ScriptableObject.CreateInstance<StringEntryDialog>();
            dialog.ShowStringEntryDialog("Choose a name",
                                                    "Enter a name for the generated assets.",
                                                    possibleFilename,
                                                    "Ok",
                                                    string.Empty,
                                                    (s) =>
                                                    {
                                                        possibleFilename = s;
                                                        wait = false;
                                                    });
            while (wait)
                await Task.Yield();
            
            //user options query
            var hide            = EditorUtility.DisplayDialog("Options", "Hide the Bundle from Painter & create TpBundle tile to use in a Palette or in Painter?", "Hide", "Don't Hide");
            
            
            
            var maps            = new[] { map };
            var result = TpPrefabUtilities.Pack("PALETTE",
                                   path,
                                   rootGo,
                                   gridselPosition,
                                   maps,
                                   false,
                                   false,
                                   TpPrefabUtilities.SelectionBundling.Target,
                                   true,
                                   false,
                                   hide,
                                   true,
                                   possibleFilename);

            if (!result.success
                || result.bundles == null
                || result.bundles.Count == 0
                || result.bundles[0].m_UnityTiles.Count == 0)
            {
                return false;
            }

            //now create the icon. Limit to max N images.
            var defaultSprite    = TpImageLib.DefaultSprite;
            var defaultSpriteTex = TpImageLib.DefaultTexture;

            if (defaultSpriteTex == null)
                // ReSharper disable once RedundantAssignment
                defaultSpriteTex = Texture2D.grayTexture;
            
            var tiles        = result.bundles[0].Tileset(TpTileBundle.TilemapRotation.Zero, FabOrBundleLoadFlags.None);
            
            //convert this to a TileCellesWrapper for use in CreateMultipleTilesIcon.
            var so = ScriptableObject.CreateInstance<TileCellsWrapper>();
            so.m_MultipleTilesSelectionBoundsInt = result.bundles[0].m_TilemapBoundsInt;
            so.m_Bounds                          = result.bundles[0].m_TilemapBoundsInt;
            so.Cells                             = tiles.Select(tsi => new TileCell(tsi.m_Tile,tsi.m_Position,tsi.m_Color,tsi.m_TransformMatrix)).ToArray();
            so.Icon                              = result.bundles[0].m_Icon;
            so.Pivot                             = Vector3Int.zero;

            (_, var tex) = TpImageLib.CreateMultipleTilesIcon(so);
            Object.DestroyImmediate(so);
            
            if (tex != null)
            {
                var bytes = tex.EncodeToPNG();
                if(tex != TpImageLib.DefaultTexture) //if not the default texture (which we can't destroy) then lose this texture as not needed anymore.
                    UnityEngine.Object.DestroyImmediate(tex);

                var iconPath = $"{path}/{result.bundles[0].name}_Icon.png";

                await File.WriteAllBytesAsync(iconPath, bytes, Application.exitCancellationToken);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                
                var tObj = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
                
                //update the importer
                // ReSharper disable once AccessToStaticMemberViaDerivedType
                var importer = TextureImporter.GetAtPath(iconPath) as TextureImporter;
                if (importer == null)
                    Debug.LogError($"**Could not locate texture importer for sprite at path {iconPath} ");
                else
                {
                    EditorUtility.SetDirty(tObj);                            
                    importer.isReadable       = true;
                    importer.textureType      = TextureImporterType.Sprite;
                    importer.mipmapEnabled    = false;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    //AssetDatabase.WriteImportSettingsIfDirty(iconPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                
                }
                await Task.Delay(1000);

                //load the sprite for later assignments to the assets
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
                if (sprite != null)
                    result.bundles[0].m_Icon = sprite;
            }
  
            if (!hide) //need to create a TpBundleTile
                return true;

            //create and set up the TpBundleTile
            var sObj = ScriptableObject.CreateInstance<TpBundleTile>();
            if (sObj == null)
            {
                Debug.LogError("Could not create TpBundle tile instance. Cancelling operation.");
                return false;
            }

            sObj.m_TileBundle = result.bundles[0];  //there's never more than one bundle created since the Palette tilemap is only one tilemap.
            var assetName = sObj.m_TileBundle.name + "_Tile";

            //populate the sprite field.
            if (result.bundles[0].m_Icon != null)
                sObj.sprite = result.bundles[0].m_Icon;
            else if (sObj.m_TileBundle.m_UnityTiles[0]?.m_UnityTile!=null &&
                     // ReSharper disable once Unity.NoNullPatternMatching
                     sObj.m_TileBundle.m_UnityTiles[0]?.m_UnityTile is Tile t && t.sprite != null) 
                sObj.sprite = t.sprite;
            else
                sObj.sprite = defaultSprite;
            
            //and save it.
            var tilePath = AssetDatabase.GenerateUniqueAssetPath($"{path}/{assetName}.asset");

            AssetDatabase.CreateAsset(sObj, tilePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
        }
        
        /// <summary>
        /// Is the Grid Selection valid
        /// </summary>
        /// <returns>A bool for valid and the tilemap (possibly null)</returns>
        internal static (bool validTarget, Tilemap? map) ValidGridSelection
        {
            get
            {
                Tilemap? map = null;
                var validTarget = GridSelection.active &&
                                  GridSelection.grid != null &&
                                  GridSelection.grid.gameObject != null &&
                                  GridSelection.target != null &&
                                  GridSelection.target.TryGetComponent<Tilemap>(out map);
                return (validTarget, map);
            }
        }
        
        
    }
}
