using System.Collections;
using TilePlus;
using UnityEngine;
// ReSharper disable MissingXmlDoc

namespace TilePlusDemo
{

    public class ParticleSysColorChanger : TpSpawnLink
    {
        public ParticleSystem m_ParticleSystem;
        public Color[] m_Colors = new Color[] { Color.red, Color.white };
        public float m_Interval;
        private ParticleSystem.ColorOverLifetimeModule colorModule;
        private Coroutine task;

        /// <inheritdoc />
        public override void OnTpSpawned()
        {
            base.OnTpSpawned();
            colorModule = m_ParticleSystem.colorOverLifetime;
            task = StartCoroutine(SwapColor());
        }

        /// <inheritdoc />
        public override void OnTpDespawned()
        {
            base.OnTpDespawned();
            if (task != null)
                StopCoroutine(task);
        }

        private IEnumerator SwapColor()
        {
            if (m_Interval <= 0)
                m_Interval = 1;
            var t = new WaitForSeconds(m_Interval);
            while (true)
            {
                colorModule.color = m_Colors[0];
                yield return t;
                colorModule.color = m_Colors[1];
                yield return t;
            }
            // ReSharper disable once IteratorNeverReturns
        }

    }
}
