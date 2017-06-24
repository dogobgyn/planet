
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using UnityEngine;

namespace Planet
{
    public class Singularity : MonoBehaviour
    {
        public GameObject m_BossPrefab;
        float m_Time = 3.0f;

        private void Update()
        {
            bool greater = (m_Time > 0.0f);
            m_Time -= Time.deltaTime;
            if (greater && m_Time <= 0.0f)
            {
                var go = GameObject.Instantiate<GameObject>(m_BossPrefab, transform.position, Quaternion.identity);
                NetworkServer.Spawn(go);
            }
        }
    }
}