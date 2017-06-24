using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace TwoDee
{
    public interface IProxyReady
    {
        bool ReadyToCreateProxy();
    }

    public class Proxied : MonoBehaviour
    {
        protected GameObject m_Prefab;
        private string m_Guid;

        [HideInInspector]
        public string Guid
        {
            get { return m_Guid; }
            set { m_Guid = value; }
        }

        public int m_RandSeed = 0;

        public int m_Level;
        public int Level
        {
            get
            {
                return m_Level;
            }
        }
        public GameObject Prefab
        {
            get { return m_Prefab; }
            set {
                m_Prefab = value;
            }
        }
        public GameObject GameObject
        {
            get { return gameObject; }
        }

        public static string GetGuid(GameObject go)
        {
            if (go != null)
            {
                var proxied = go.GetComponent<Proxied>();
                if (proxied != null) return proxied.Guid;
                else
                {
                    Debug.LogError("Object +" + go.name + " requested guid but has no proxy component");
                }
            }

            return "";
        }

        protected virtual void VirtualOnDestroy()
        {
        }

        private void OnDestroy()
        {
            VirtualOnDestroy();
            ComponentList.OnEnd(this);
        }

        protected virtual void VirtualStart()
        {
        }

        protected virtual void VirtualAwake()
        {
        }

        // Give some random GUID on awake in case we aren't setting this thing up from a proxy.
        private void Awake()
        {
            m_Guid = ProxyWorld.CreateGuid();
            VirtualAwake();

            ComponentList.OnStart(this);
        }

        private void Start()
        {
            // Try to guess the proxy based on our name.  Sloppy but most of the time this feature is not used because we manually call PostInstantiate (it's mostly for testing)
            var pworld = ComponentList.GetFirst<ProxyWorld>();
            if (pworld != null)
            {
                string myName = gameObject.name.ToLower();
                int myParenthesis = myName.IndexOf('(');
                if (myParenthesis >= 0)
                {
                    myName = myName.Substring(0, myParenthesis);
                }

                bool found = false;
                foreach (var entry in pworld.m_Objects)
                {
                    if (entry == null) continue;
                    if (entry.gameObject ==null || entry.gameObject.name == null)
                    {
                        Debug.Log("Bad gameobject found in proxy world objects list\n");
                    }
                    var entryName = entry.gameObject.name.ToLower();

                    if (myName == entryName)
                    {
                        Prefab = entry;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    Debug.LogError("Proxied missing: " + gameObject.name);
                }
            }

            VirtualStart();
        }

        public float m_DefaultBoundsWidthHeight = 5.0f;

        public virtual bool ReadyToCreateProxy(string prefab)
        {
            foreach (var comp in GetComponents<IProxyReady>())
            {
                if (!comp.ReadyToCreateProxy()) return false;
            }

            return true;
        }

        public virtual ProxyWorld.Proxy CreateProxy(string prefab)
        {
            var result = new ProxyWorld.Proxy();
            result.Init(m_DefaultBoundsWidthHeight, prefab, transform.position, transform.rotation);
            return result;
        }
    }
}