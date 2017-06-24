using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using System;

namespace TwoDee
{
    public class VoxelLight : MonoBehaviour, TwoDee.IGridPointsDirty
    {
        public float m_Light = 1.0f;
        public float m_LightFalloff = 0.05f;

        Dictionary<IntVector2, float> m_Contribution = new Dictionary<IntVector2, float>();

        public Dictionary<IntVector2, float> Contribution
        {
            get { return m_Contribution; }
        }


        public void RegenerateContribution()
        {
            foreach (var vg in TwoDee.ComponentList.GetCopiedListOfType<TwoDee.VoxelGenerator>())
            {
                //m_Contribution = vg.ComputeLightContribution(transform.position + -1.0f * RadialGravity.GetDirectionAtPoint(transform.position), m_Light, m_LightFalloff);
            }
        }

        bool[] m_DoClearLight = new bool[4];
        Vector3[] m_DoClearLight_gs = new Vector3[4];
        static Vector3[] m_ClearPoints = new Vector3[] { new Vector3(-1.0f, 0.0f), new Vector3(1.0f, 0.0f), new Vector3(0.0f, 1.0f), new Vector3(0.0f, -1.0f) };

        bool m_Emitting;
        public bool Emitting
        {
            get
            {
                return m_Emitting;
            }
            protected set
            {
                if (m_Emitting != value)
                {
                    m_Emitting = value;
                    if (value)
                    {
                        var vg = TwoDee.ComponentList.GetFirst<TwoDee.VoxelGenerator>();
                        if (vg != null)
                        {
                            // in case we round down into an underground square, put a light in all neighbors.
                            for (int i = 0; i < 4; i++)
                            {
                                m_DoClearLight_gs[i] = vg.WorldSpaceToGrid(transform.position + m_ClearPoints[i]);
                                m_DoClearLight[i] = vg.LightingDataInstance.SetLight(m_DoClearLight_gs[i], true);
                            }
                        }
                    }
                    else
                    {
                        var vg = TwoDee.ComponentList.GetFirst<TwoDee.VoxelGenerator>();
                        if (vg != null)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                if (m_DoClearLight[i])
                                {
                                    vg.LightingDataInstance.SetLight(m_DoClearLight_gs[i], false);
                                }
                            }
                        }
                    }
                }
            }
        }

        protected virtual void VirtualStart()
        {
            Emitting = true;
        }

        public void Start()
        {
            VirtualStart();
        }

        protected virtual void VirtualOnDestroy()
        {

        }

        public void OnDestroy()
        {
            Emitting = false;
            TwoDee.ComponentList.OnEnd(this);
        }

        public void Awake()
        {
            TwoDee.ComponentList.OnStart(this);
        }

        void TwoDee.IGridPointsDirty.GridPointsDirty(TwoDee.GridPointsDirtyArgs args)
        {
            var argsBounds = MathExt.CreateBounds(args.m_LowerLeft_ws, args.m_UpperRight_ws);
            var range = 1.0f / m_LightFalloff;
            var contributionBounds = new Bounds(transform.position, new Vector3(range, range, range));
            if (argsBounds.Intersects(contributionBounds))
            {
                RegenerateContribution();
            }
        }
    }
}
