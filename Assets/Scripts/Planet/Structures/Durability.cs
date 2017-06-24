
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using UnityEngine;
using TwoDee;

namespace Planet
{
    public class Durability : MonoBehaviour, IProxy, IMouseInfo
    {
        float m_Durability = 1.0f;
        public float m_Lifetime = 10.0f;       

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            m_Durability -= (dt / m_Lifetime);
            if (m_Durability < 0.0f)
            {
                Destroy(gameObject);
            }
        }

        IProxyData IProxy.CreateData()
        {
            return new Proxy();
        }

        void IMouseInfo.GetMouseInfo(MouseInfoArgs args)
        {
            args.Value = string.Format("Durability {0:F1}%", 100.0f*m_Durability);
        }

        public class Proxy : TwoDee.ProxyDataComp<Durability>
        {
            public float m_Durability;

            protected override void SaveLoad(bool save, Durability comp)
            {
                if (save)
                {
                    m_Durability = comp.m_Durability;
                }
                else
                {
                    comp.m_Durability = m_Durability;
                }
            }
        }
    }
}