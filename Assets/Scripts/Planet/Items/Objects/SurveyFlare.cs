
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using UnityEngine;

namespace Planet
{
    public class SurveyFlare : MonoBehaviour
    {
        float m_AliveTime = 10.0f;
        float m_CurrentIntensity = 0.0f;
        private void Update()
        {
            float dt = Time.deltaTime;
            m_AliveTime -= dt;
            m_CurrentIntensity += UnityEngine.Random.Range(-dt * 10.0f, dt * 10.0f);
            m_CurrentIntensity = Mathf.Clamp(m_CurrentIntensity, 0.0f, 3.0f);

            var light = gameObject.GetComponentInSelfOrChildren<Light>();
            if (light != null)
            {
                float finalIntensity = 1.0f + m_CurrentIntensity;
                if (m_AliveTime < 1.0f)
                {
                    finalIntensity *= m_AliveTime;
                }
                light.intensity = finalIntensity;
            }
            if (m_AliveTime < 0.0f)
            {
                Destroy(gameObject);
            }
        }
    }
}