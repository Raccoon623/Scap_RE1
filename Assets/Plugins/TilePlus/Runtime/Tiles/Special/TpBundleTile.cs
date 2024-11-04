// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 12-23-2023 
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-23-2023
// ***********************************************************************
// <copyright file="TpBundleTile.cs" company="Jeff Sasmor">
//     Copyright (c) 2024 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************

using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TilePlus
{
    /// <summary>
    /// A proxy for a TpTileBundle. Overwrites itself with the Bundle when painted.
    /// </summary>
    [CreateAssetMenu(fileName = "TpBundleTile.asset", menuName = "TilePlus/Create TpBundleTile", order = 100000)]
   public class TpBundleTile : Tile
   {
       /// <summary>
       /// The bundle that this tile is aliasing
       /// </summary>
       [Tooltip("The Bundle to use when this tile is painted")]
       public TpTileBundle m_TileBundle;

       /// <summary>
       ///Use the matrix?
       /// </summary>
       [Tooltip("Apply the Matrix to all tiles in the bundle. Ignored if Matrix is invalid")]
       public bool m_ApplyMatrix = false;

       /// <summary>
       /// Apply new GUIDs to TilePlus tiles
       /// </summary>
       [Tooltip("Apply new GUIDs to TilePlus tiles? RECOMMENDED: TRUE")] 
       public bool m_TptNewGuids = true;
       
       /// <summary>
       /// optional transform matrix
       /// </summary>
       [HideInInspector]
       public Matrix4x4 m_Matrix = Matrix4x4.identity;
       
       

       /// <inheritdoc />
       // ReSharper disable once AnnotateNotNullParameter
       public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject go)
       {
           var map = tilemap.GetComponent<Tilemap>();
           if (map == null)
               return false;
           #if UNITY_EDITOR
           if (TpLib.IsTilemapFromPalette(map))
               return false;
           #endif
           TpLib.DelayedCallback(null, ()=> 
                                       {
                                           var useTransform =  m_ApplyMatrix && m_Matrix.ValidTRS();
                                           
                                           //don't use the cache. If it were used, the same transform would always be used until a scripting reload.
                                           m_TileBundle.CacheForceFilter = true;
                                           try
                                           {
                                               TileFabLib.LoadBundle(m_TileBundle,
                                                                     map,
                                                                     position,
                                                                     TpTileBundle.TilemapRotation.Zero,
                                                                     m_TptNewGuids
                                                                         ? //apply new GUIDs or not?
                                                                         FabOrBundleLoadFlags.NewGuids | FabOrBundleLoadFlags.LoadPrefabs
                                                                         : FabOrBundleLoadFlags.LoadPrefabs,
                                                                     //apply matrix = add a filter callback or not.
                                                                     useTransform
                                                                         ? Filter
                                                                         : null);

                                               //it is possible that the position where this tile was painted was not overwritten by the bundle
                                               //if that's the case then delete it.
                                               var t = map.GetTile<TpBundleTile>(position);
                                               if (t != null && t == this)
                                                   map.SetTile(position, null);
                                           }
                                           finally
                                           {
                                               m_TileBundle.CacheForceFilter = false;
                                           }
                                       },"TpTileBundle-PlaceBundle",100);
           //note that this tile will get overwritten by the bundle or deleted as shown above.
           return true;

           //This filter modifies the transform of each Unity tile.
           bool Filter(FabOrBundleFilterType filterType, BoundsInt _, object obj)
           {
               switch (filterType)
               {
                   case FabOrBundleFilterType.Prefab:
                       return true;
                   case FabOrBundleFilterType.Unity:
                   {
                       if (obj is TpTileBundle.TilesetItem tsItem)
                           tsItem.m_TransformMatrix = m_Matrix;
                       break;
                   }
                   //has to be a TPT tile
                   case FabOrBundleFilterType.TilePlus:
                   default:
                   {
                       if (obj is TpTileBundle.TilesetItem tsItem)
                           tsItem.m_TransformMatrix = m_Matrix;
                       break;
                   }
               }

            return true;
       }
   }

       /// <inheritdoc />
       public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
       {
           base.GetTileData(position, tilemap, ref tileData);
           if (sprite != null)
               return;
           var sprt = m_TileBundle.m_Icon;
           if (sprt != null)
               tileData.sprite = sprt;
       }
       
       
   }
   
   #if UNITY_EDITOR

   
   /// <summary>
   /// Editor for TpBundleTile
   /// </summary>
   [CustomEditor(typeof (TpBundleTile))]
   public class TpBundleTileEditor : UnityEditor.Editor
   {
       private static readonly GUIContent s_ButtonLabel      = new GUIContent("Copy Icon from Bundle", "If the Bundle has an Icon you can copy it to this tile asset.");
       private static readonly GUIContent s_ResetButtonLabel = new GUIContent("Reset Matrix",          "Reset the matrix to normal (Matrix4x4.Identity");
       private static readonly GUIContent s_PosGuiContent    = new GUIContent("Position",              "Set tile sprite tilePosition for ALL tiles in the Bundle");
       private static readonly GUIContent s_RotGuiContent    = new GUIContent("Rotation",              "Set tile sprite rotation for ALL tiles in the Bundle");
       private static readonly GUIContent s_ScaleGuiContent  = new GUIContent("Scale",                 "Set tile sprite scale for ALL tiles in the Bundle");

       /// <inheritdoc />
       public override void OnInspectorGUI()
       {
           if(target == null)
               return;
           if(target is not TpBundleTile tpBundleTile)
               return;
           base.OnInspectorGUI();

           if (tpBundleTile.m_TileBundle != null && tpBundleTile.m_TileBundle.m_Icon != null)
           {
               if (GUILayout.Button(s_ButtonLabel))
                   tpBundleTile.sprite = tpBundleTile.m_TileBundle.m_Icon;
           }

           using (new EditorGUILayout.VerticalScope("box", GUILayout.ExpandWidth(false)))
           {
               if (GUILayout.Button(s_ResetButtonLabel) || !tpBundleTile.m_Matrix.ValidTRS())
                   tpBundleTile.m_Matrix = Matrix4x4.identity;

               EditorGUI.BeginChangeCheck();
               var pos      = tpBundleTile.m_Matrix.GetPosition();
               var rotation = tpBundleTile.m_Matrix.rotation.eulerAngles;
               var scale    = tpBundleTile.m_Matrix.lossyScale;

               var newPosition = EditorGUILayout.Vector3Field(s_PosGuiContent,   pos);
               var newRotation = EditorGUILayout.Vector3Field(s_RotGuiContent,   rotation);
               var newScale    = EditorGUILayout.Vector3Field(s_ScaleGuiContent, scale);
               if (!EditorGUI.EndChangeCheck() || newScale == Vector3.zero)
                   return;
               var newValue = Matrix4x4.TRS(newPosition, Quaternion.Euler(newRotation), newScale);
               if(newValue.ValidTRS())
                   tpBundleTile.m_Matrix = newValue;
           }
       }
   }
   
   #endif
   
}
