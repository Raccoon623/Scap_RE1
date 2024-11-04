using System.Collections;
using TilePlus;
using UnityEngine;

namespace TilePlusDemo
{
    /// <summary>
    /// Shows how to load a TilePlus prefab. It's not really any different...
    /// </summary>
    public class PrefabLoadExample : MonoBehaviour
    {
        /// <summary>
        /// The prefab to use
        /// </summary>
        public GameObject m_TilePlusPrefab;

        
        // Start is called before the first frame update
        private IEnumerator Start()
        {
            while (!TpLib.TpLibIsInitialized)
                yield return null;

            var instanceOfPrefab = Instantiate(m_TilePlusPrefab, Vector3.zero, Quaternion.identity);

            if (instanceOfPrefab == null)
                Debug.LogError("Was not able to instantiate the prefab.");
        }

    }
}
