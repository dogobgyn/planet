using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.Networking;
using TwoDee;

namespace Planet
{
    public class Tree : NetworkBehaviour, IMouseInfo, TwoDee.IGridPointsDirty, IProxy
    {
        public GameObject m_ChoppedPrefab;

        public float m_ChopsToFell = 5;
        float m_ChopsLeft;
        public float m_Wood = 15.0f;

        private bool m_Dead;
        float m_DeadTime;

        void IMouseInfo.GetMouseInfo(MouseInfoArgs args)
        {
            args.Value = Value;
        }

        string Value
        {
            get
            {
                return "Tree " + m_Wood + " Hits:" + m_ChopsLeft;
            }
        }

        private void OnDestroy()
        {
            ComponentList.OnEnd(this);
        }

        private void Start()
        {
            m_ChopsLeft = m_ChopsToFell;
            ComponentList.OnStart(this);
        }

        private void FixedUpdate()
        {
            if (m_Dead)
            {
                m_DeadTime += Time.deltaTime;
                if (m_DeadTime > 5.0f)
                {
                    Destroy(gameObject);
                }
            }
        }

        void FellTree(Vector3 hitLoc_ws)
        {
            if (m_Dead) return;
            m_Dead = true;
            gameObject.SetLayerRecursive(LayerMask.NameToLayer("Trees"));
            var top = transform.FindChild("TreeTop");
            var topPos = top.transform.position + 0.01f * transform.up;
            var topRot = top.transform.rotation;
            DestroyObject(top.gameObject);

            var log = GameObject.Instantiate(m_ChoppedPrefab, topPos, topRot);
            log.transform.localScale = transform.localScale;

            var logrb = log.GetComponent<Rigidbody>();
            logrb.isKinematic = false;
            var srcomp = log.GetComponent<StoredResource>();
            srcomp.SetOriginalAmount(m_Wood);
            srcomp.m_OriginalScale = new Vector3(transform.localScale.x * srcomp.m_OriginalScale.x, transform.localScale.y * srcomp.m_OriginalScale.y, transform.localScale.z * srcomp.m_OriginalScale.z);
            var logcomp = log.GetComponent<Log>();
            logcomp.m_FellLoc = hitLoc_ws;
//            logrb.AddTorque(new Vector3(0.0f, 0.0f, 300.0f));
//            logrb.AddTorque(new Vector3(0.0f, 0.0f, 300.0f));
//          logrb.angularVelocity = new Vector3(0.0f, 0.0f, 1.0f);
        }

        public void Chop(float damage, Vector3 chopPlace_ws)
        {
            EasySound.Play("woodchop", gameObject);

            m_ChopsLeft -= damage;
            if (m_ChopsLeft <= 0.0f)
            {
                FellTree(chopPlace_ws);
            }
        }

        void GroundDestroyed(Vector3 hitLoc_ws)
        {
            FellTree(hitLoc_ws);
            Destroy(gameObject);
        }

        void IGridPointsDirty.GridPointsDirty(GridPointsDirtyArgs args)
        {
            if (args.DirtyBox_ws.Contains(transform.position))
            {
                // Recheck to see if we still have a leg to stand on
                //@TODO this is exactly the same as resource pile for now but might change it since trees have bigger roots
                var vgen = ComponentList.GetFirst<PVoxelGenerator>();
                var probePoint = transform.position + transform.up * -0.1f;
                if (vgen.IsPointClearAt_ws(probePoint))
                {
                    GroundDestroyed(args.DirtyBox_ws.center);
                }
            }
        }

        [Serializable]
        public class Proxy : TwoDee.ProxyDataComp<Tree>
        {
            public float m_Wood;
            protected override void SaveLoad(bool save, Tree comp)
            {
                if (save)
                {
                    m_Wood = comp.m_Wood;
                }
                else
                {
                    comp.m_Wood = m_Wood;
                }
            }
        }

        IProxyData IProxy.CreateData()
        {
            return new Proxy();
        }
    }
}