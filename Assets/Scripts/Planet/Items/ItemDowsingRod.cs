
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using UnityEngine;

namespace Planet
{
    public class ItemDowsingRod : Item
    {
        float m_TimeBought = 0.0f;
        public GameObject m_DowsingUiPrefab;
        GameObject m_DowsingUi;

        Vector3 m_DowsedPosition;

        void DowseClosest(UseContext args)
        {
            m_DowsedPosition = Vector3.zero;

            var proxyworld = TwoDee.ComponentList.GetFirst<TwoDee.ProxyWorld>();
            var stuff = proxyworld.GetGameObjectsOrProxies("oreteleport");
            Vector3 closestPosition = Vector3.zero;
            var positions = new List<Vector3>();
            foreach (var ob in stuff)
            {
                positions.Add(ob.Position);
            }

            var closestDistance = 9999999.0f;
            foreach (var pos in positions)
            {
                var delta = (pos - args.m_OriginPos);
                if (delta.magnitude < 200 && delta.magnitude < closestDistance)
                {
                    closestDistance = delta.magnitude;
                    m_DowsedPosition = pos;
                }
            }
        }

        public override void UpdateSelected(UseContext args)
        {
            if (m_DowsingUi != null)
            {
                m_DowsingUi.GetComponentInSelfOrChildren<Renderer>().enabled = m_TimeBought > 0.0f;
            }
            if (m_TimeBought > 0.0f)
            {
                m_TimeBought -= args.m_DeltaTime;

                if (m_DowsedPosition != Vector3.zero)
                {
                    var delta = (m_DowsedPosition - args.m_OriginPos);

                    m_DowsingUi.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                    m_DowsingUi.transform.position = args.m_OriginPos + 3.0f * Vector3.back;
                    m_DowsingUi.transform.rotation = Quaternion.FromToRotation(Vector3.right, delta.normalized);
                }
                else
                {
                    m_DowsingUi.transform.localScale = Vector3.zero;
                }
            }
        }

        public override void Use(UseContext args)
        {
            base.Use(args);
            DowseClosest(args);
            m_TimeBought += 5.0f;
            UseQuantity(args, 1);
        }

        public override void Selected(UseContext args)
        {
            DowseClosest(args);
            m_DowsingUi = GameObject.Instantiate(m_DowsingUiPrefab, args.m_TargetPos, Quaternion.identity);
        }

        public override void Unselected(UseContext args)
        {
            GameObject.DestroyImmediate(m_DowsingUi);
        }
    }
}