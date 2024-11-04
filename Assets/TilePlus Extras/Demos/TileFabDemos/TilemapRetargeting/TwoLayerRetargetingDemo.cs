using System.Collections;
using System.Collections.Generic;
using TilePlus;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TilePlusDemo
{
    /// <summary>
    /// An illustration of how to load a TileFab archive.
    /// Note that the tilemap names and order-in-layer in the demo scene match those of the original development scene.
    /// If you're comparing the elapsed time value in the debug message then ensure that TilePlus informational and warning messages
    /// are shut off (use the Configuration editor and be sure to click Reload).
    ///
    /// This is exactly the same as LoadATileFab except it uses a mapping to allow for
    /// Tilemaps with different names in the scene than in the TileFab asset.
    /// </summary>

    public class TwoLayerRetargetingDemo : MonoBehaviour
    {
        /// <summary>
        /// The TileFab reference
        /// </summary>
        public TpTileFab m_TileFab;
        /// <summary>
        /// Offset the placement. Note that the result may not be visible by the camera.
        /// </summary>
        public Vector3Int m_Offset = Vector3Int.zero;
        /// <summary>
        /// Offset each iteration from the original offset
        /// </summary>
        public Vector3Int m_Delta = new Vector3Int(30, 0, 0);

        /// <summary>
        /// How many times to place the tiles.
        /// </summary>
        public int m_Iterations = 4;


        /// <summary>
        /// Refresh all tiles after loading. Note this is done by using Tilemap.RefreshAllTiles.
        /// </summary>
        public bool m_ForceRefresh = true;
        /// <summary>
        /// Filter out tiles with the BlueThing tag
        /// </summary>
        public bool m_FilterOutBlueThings;
        /// <summary>
        /// Apply new GUIDs to all TPT tiles. 
        /// </summary>
        public bool m_NewGuids;
        /// <summary>
        /// Tilemap reference
        /// </summary>
        public Tilemap m_UsedToBeTop;
        /// <summary>
        /// Tilemap reference
        /// </summary>
        public Tilemap m_UsedToBeBottom;
        
        
        IEnumerator Start()
        {
            while (!TpLib.TpLibIsInitialized)
                yield return null;

            var mappingDict = new Dictionary<string, Tilemap>(2)
                      {
                          //note that the key is case-sensitive. "top" != "Top"
                          { "Top", m_UsedToBeTop }, 
                          { "Bottom", m_UsedToBeBottom }
                      };

            
            var loadFlags = FabOrBundleLoadFlags.None;
            if (m_ForceRefresh)
                loadFlags |= FabOrBundleLoadFlags.ForceRefresh;
            if (m_NewGuids)
                loadFlags |= FabOrBundleLoadFlags.NewGuids;
            
            for (var i = 0; i < m_Iterations; i++)
            {
                var offset = m_Offset + (m_Delta * i);

                var result = TileFabLib.LoadTileFab(null,
                                                    m_TileFab,
                                                    offset,
                                                    TpTileBundle.TilemapRotation.Zero,
                                                    loadFlags,
                                                    m_FilterOutBlueThings ? BlueThingFilter : null, 
                                                    mappingDict);               
               if(result != null)
                    Debug.Log(result.ToString());

            }



        }
        
        private bool BlueThingFilter(FabOrBundleFilterType filterType, BoundsInt bounds, object obj)
        {
            if (obj == null)
                return false;
            
            switch (filterType)
            {
                case FabOrBundleFilterType.Prefab:
                case FabOrBundleFilterType.Unity:
                    return true;
            }

            // ReSharper disable once Unity.NoNullPatternMatching
            if (obj is not TpTileBundle.TilePlusItem { m_Tile: ITilePlus itp }) return true;
            (var count, var tags) = itp.TrimmedTags;
            if (count == 0)
                return true;
            //here the 'blue thing' has only one tag so this works. Not a general case.
            return !tags[0].ToLower().Contains("bluething"); //note that tags are supposed to be case insensitive so be careful 
        }    

        
    }
    
    
    #if UNITY_EDITOR
    

    /// <summary>
    /// Editor
    /// </summary>
    [CustomEditor(typeof(TwoLayerRetargetingDemo))]
    public class TwoLayerRetargetingDemoEditor : Editor
    {
        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();
            
            var obj = target as TwoLayerRetargetingDemo;
            if (obj == null)
                return;
            EditorGUILayout.HelpBox("The TileFab reference should go here", MessageType.None);
            obj.m_TileFab = EditorGUILayout.ObjectField("TileFab", obj.m_TileFab, typeof(TpTileFab), obj) as TpTileFab;
            EditorGUILayout.HelpBox("Offset the placement of the TileFab. Note that the result may place the tiles outside of the camera visible area.", MessageType.None);
            obj.m_Offset = EditorGUILayout.Vector3IntField("Offset", obj.m_Offset);
            EditorGUILayout.HelpBox("Add a delta to each offset per-iteration. Note that the result may place the tiles outside of the camera visible area.", MessageType.None);
            obj.m_Delta = EditorGUILayout.Vector3IntField("Delta per iteration", obj.m_Delta);
            EditorGUILayout.HelpBox("How many iteration?", MessageType.None);
            obj.m_Iterations = EditorGUILayout.DelayedIntField("Iterations", obj.m_Iterations);
            EditorGUILayout.HelpBox("Force refresh all Tiles after loading?", MessageType.None);
            obj.m_ForceRefresh = EditorGUILayout.Toggle("Force Refresh", obj.m_ForceRefresh);
            EditorGUILayout.HelpBox("Filter out 'Blue Thing' tiles if checked.", MessageType.None);
            obj.m_FilterOutBlueThings = EditorGUILayout.Toggle("Filter out 'Blue Thing' tiles", obj.m_FilterOutBlueThings);
            EditorGUILayout.HelpBox("Update the GUIDs of all TilePlus tiles with new GUIDs", MessageType.None);
            obj.m_NewGuids = EditorGUILayout.Toggle("New GUIDs", obj.m_NewGuids);
            EditorGUILayout.Separator();
            EditorGUILayout.HelpBox("The UsedToBeTop Tilemap reference", MessageType.None);
            obj.m_UsedToBeTop = EditorGUILayout.ObjectField("Used to be Top", obj.m_UsedToBeTop, typeof(Tilemap), obj) as Tilemap;
            EditorGUILayout.HelpBox("The UsedToBeBottom Tilemap reference", MessageType.None);
            obj.m_UsedToBeBottom = EditorGUILayout.ObjectField("Used to be Bottom", obj.m_UsedToBeBottom, typeof(Tilemap), obj) as Tilemap;
            

        }
    }
    #endif
}
