using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using TwoDee;

namespace Planet
{
    [Serializable]
    public class PowerConduitPower
    {
        public List<string> m_Connected = new List<string>();
        public float m_CurrentPower;
        public bool m_InfinitePower;

        public bool IsAllConnectedProxied
        {
            get
            {
                // Check if any object still is not proxied
                foreach (var conduit in m_Connected)
                {
                    var gop = new TwoDee.ProxyWorld.GameObjectOrProxy(conduit);
                    if (gop.GameObject != null)
                    {
                        return false;
                    }
                }

                return true;
            }


        }
        public void UpdatePower()
        {
            if (m_InfinitePower)
            {
                m_CurrentPower = 1.0f;
            }
            else
            {
                m_CurrentPower = Mathf.Max(m_CurrentPower - 0.1f, 0.0f);
            }

            foreach (var conduit in m_Connected)
            {
                PowerConduitPower other = null;
                var gop = new TwoDee.ProxyWorld.GameObjectOrProxy(conduit);
                if(gop.GameObject != null)
                {
                    var pc = gop.GameObject.GetComponentInParent<PowerConduit>();
                    other = pc.m_Pcp;
                }
                else if(gop.Proxy != null)
                {
                    var pc = gop.Proxy.GetData<PowerConduit.Proxy>();
                    if (pc != null)
                    {
                        other = pc.m_Pcp;
                    }
                }

                if (other != null)
                {
                    other.m_CurrentPower = Mathf.Max(other.m_CurrentPower, m_CurrentPower);
                }
            }
        }
    }

    public class PowerConduit : MonoBehaviour, IProxy, IProxyReady
    {
        public bool m_CanUnloadWithoutConnections;
        public bool m_InfinitePower;

        public PowerConduitPower m_Pcp;

        private void Start()
        {
            m_Pcp.m_InfinitePower = m_InfinitePower;
        }

        public bool HasPower
        {
            get
            {
                return m_Pcp.m_CurrentPower > 0.0f;
            }
        }

        void Update()
        {
            m_Pcp.UpdatePower();
        }

        public void Disconnect(PowerConduit other)
        {
            if (other == null || other == this) return;

            m_Pcp.m_Connected.Remove(TwoDee.Proxied.GetGuid(other.gameObject));
            other.m_Pcp.m_Connected.Remove(TwoDee.Proxied.GetGuid(gameObject));
        }
        public void Connect(PowerConduit other)
        {
            if (other == null || other == this) return;

            m_Pcp.m_Connected.Add(TwoDee.Proxied.GetGuid(other.gameObject));
            other.m_Pcp.m_Connected.Add(TwoDee.Proxied.GetGuid(gameObject));
            m_Pcp.UpdatePower();
            other.m_Pcp.UpdatePower();
        }

        public static bool DrainEnergy(GameObject gameObject, float amount, bool doIt)
        {
            var powercomp = gameObject.GetComponentInSelfOrParents<PowerConduit>();
            if (powercomp != null && powercomp.HasPower)
            {
                return true;
            }

            var inv = gameObject.GetComponent<Container>().FirstInventory();

            return inv.DrainEnergy(amount, doIt);
        }

        IProxyData IProxy.CreateData()
        {
            return new Proxy();
        }

        bool IProxyReady.ReadyToCreateProxy()
        {
            if (m_CanUnloadWithoutConnections) return true;

            if (m_Pcp.IsAllConnectedProxied) return true;

            // There still exists a connection
            return false;
        }

        public class Proxy : TwoDee.ProxyDataComp<PowerConduit>
        {
            public PowerConduitPower m_Pcp;

            protected override void SaveLoad(bool save, PowerConduit comp)
            {
                if(save)
                {
                    // Let all connected ropes know we're going away since they need to go away too (or else the physics will fail and the rope will fall from this side)
                    m_Pcp = comp.m_Pcp;
                }
                else
                {
                    comp.m_Pcp = m_Pcp;
                }
            }
        }
    }

}