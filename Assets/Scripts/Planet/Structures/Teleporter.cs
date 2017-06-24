
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
    public class Teleporter : MonoBehaviour, IMouseButtons, IMouseInfo, IBuriedNotify, TwoDee.IProxy
    {
        public GameObject m_VortexPrefab;
        public bool m_CanUse = false;
        string m_TargetGuid;


        private void Start()
        {
            TwoDee.ComponentList.OnStart(this);
        }
        private void OnDestroy()
        {
            TwoDee.ComponentList.OnEnd(this);
        }

        void IMouseButtons.GetButtons(MouseButtonContext context)
        {
            if (m_CanUse)
            {
                context.m_Entries.Add(new MouseButtonEntry("Teleport", 0));
            }
        }

        void IMouseButtons.UseButton(MouseButtonContext context, MouseButtonEntry entry)
        {
            if (entry.m_Data.Equals(0))
            {
                var gop = new TwoDee.ProxyWorld.GameObjectOrProxy(m_TargetGuid);
                if (gop.Valid)
                {
                    TwoDee.EasySound.Play("teleport", gameObject);

                    Vector3 otherEndPos = gop.Position;
                    otherEndPos.z = 0.0f;
                    context.m_Player.transform.position = otherEndPos;
                }
            }
        }

        private void Update()
        {
                
        }

        void IBuriedNotify.Unburied(UnburiedArgs args)
        {
            m_CanUse = true;
            var createPos = transform.position;
            var go = GameObject.Instantiate<GameObject>(m_VortexPrefab, createPos, Quaternion.identity);

            ConnectToOther(go);
        }

        public void ConnectToOther(GameObject other)
        {
            var otherTp = other.GetComponent<Teleporter>();
            if (otherTp != null)
            {
                otherTp.m_TargetGuid = GetComponent<TwoDee.Proxied>().Guid;
            }
            m_TargetGuid = other.GetComponent<TwoDee.Proxied>().Guid;
        }

        IProxyData IProxy.CreateData()
        {
            return new Proxy();
        }

        [Serializable]
        public class Proxy : TwoDee.ProxyDataComp<Teleporter>
        {
            string m_TargetGuid;
            bool m_CanUse;

            protected override void SaveLoad(bool save, Teleporter tp)
            {
                if (save)
                {
                    m_TargetGuid = tp.m_TargetGuid;
                    m_CanUse = tp.m_CanUse;
                }
                else
                {
                    tp.m_TargetGuid = m_TargetGuid;
                    tp.m_CanUse = m_CanUse;
                }
            }
        }

        void IMouseInfo.GetMouseInfo(MouseInfoArgs args)
        {
            args.Value = Value;
        }

        public string Value
        {
            get
            {
                return "Teleporter";
            }

        }
    }
}