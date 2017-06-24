using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.Networking;
using TwoDee;

namespace Planet
{
    public class UnburiedArgs
    {

    }
    public interface IBuriedNotify
    {
        void Unburied(UnburiedArgs args);
    }

    public class Buried : MonoBehaviour, TwoDee.IGridPointsDirty, TwoDee.IProxy
    {
        public GameObject m_LoosenParticlePrefab;
        public GameObject m_LoosenAttachPrefab;

        float Radius
        {
            get
            {
                var mf = gameObject.GetComponentInSelfOrChildren<MeshFilter>();
                var bounds = mf.mesh.bounds;
                // assuming this is mostly spherical
                return transform.TransformVector(bounds.extents).x;
            }
        }

        bool CheckClear()
        {
            // sample 8 points to see if we're clear
            var voxelGen = TwoDee.ComponentList.GetFirst<TwoDee.VoxelGenerator>();
            var radius = Radius;
            int freePoints = 0;
            int numPoints = 8;
            for (int i=0;i< numPoints; i++)
            {
                var offset = TwoDee.Math3d.FromLengthAngleDegrees2D(radius, i * (360.0f/numPoints));
                var checkPoint_ws = offset + transform.position;
                Debug.DrawLine(transform.position, checkPoint_ws, Color.magenta);
                if (voxelGen.IsPointClearAt_ws(checkPoint_ws))
                {
                    freePoints++;
                }
            }

            return freePoints >= 5;
        }

        private float m_KinematicTime = 0.0f;

        void Loosen()
        {
            if (m_LoosenParticlePrefab != null)
            {
                GameObject.Instantiate<GameObject>(m_LoosenParticlePrefab, transform.TransformPoint(Vector3.zero), Quaternion.identity).StartParticle();
            }
            if (m_LoosenAttachPrefab != null)
            {
                GameObject.Instantiate<GameObject>(m_LoosenAttachPrefab, transform);
            }

            var rb = GetComponent<Rigidbody>();
            rb.isKinematic = false;

            var args = new UnburiedArgs();
            foreach (var bn in GetComponentsInChildren<IBuriedNotify>())
            {
                bn.Unburied(args);
            }
        }

        bool m_NeedsCheck;
        public void FixedUpdate()
        {
            if (!m_NeedsCheck) return;

            var clear = CheckClear();
            var rb = GetComponent<Rigidbody>();
            if (rb == null || !rb || !rb.isKinematic) return; // Already did loosen

            if (m_KinematicTime > 0.0f)
            {
                m_KinematicTime -= Time.deltaTime;
                if (m_KinematicTime <= 0.0f)
                {
                    Loosen();
                }
            }
            else
            {
                if (clear && rb.isKinematic)
                {
                    // Guarantee clear.  Note that it will take a few frames before it gets a chance to refresh, so wait a slight amount of time before loosen.
                    var voxelGen = TwoDee.ComponentList.GetFirst<TwoDee.VoxelGenerator>();
                    voxelGen.ModifyCircle(1.0f, false, transform.position, Radius + 0.1f);
                    m_KinematicTime = 0.1f;
                }
            }
        }

        void TwoDee.IGridPointsDirty.GridPointsDirty(TwoDee.GridPointsDirtyArgs args)
        {
            m_NeedsCheck = true;
        }

        public void OnDestroy()
        {
            TwoDee.ComponentList.OnEnd(this);
        }

        public void Awake()
        {
            TwoDee.ComponentList.OnStart(this);
        }

        IProxyData IProxy.CreateData()
        {
            return new Proxy();
        }

        [Serializable]
        public class Proxy : TwoDee.ProxyDataComp<Buried>
        {
            bool m_Unburied;

            protected override void SaveLoad(bool save, Buried comp)
            {
                var rb = comp.GetComponent<Rigidbody>();
                if (save)
                {
                    m_Unburied = !rb.isKinematic;
                }
                else
                {
                    if (m_Unburied)
                    {
                        rb.isKinematic = false;
                    }
                }
            }
        }
    }
}