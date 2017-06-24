using UnityEngine;
using System.Collections;

namespace TwoDee
{
    public class ParticleAutoDestroy : MonoBehaviour
    {
        private ParticleSystem ps;
        float m_TimeLeft;

        public void Start()
        {
            ps = GetComponent<ParticleSystem>();
            m_TimeLeft = ps.main.duration;
        }

        public void Update()
        {
            m_TimeLeft -= Time.deltaTime;
            if (ps)
            {
                if (!ps.IsAlive() || m_TimeLeft < 0.0f)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}