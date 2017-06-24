
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using UnityEngine;

namespace Planet
{
    public class ItemRope : Item
    {
        public GameObject m_RopePrefab;
        public bool m_Grappling;
        public int m_RopeLinks = 20;

        public override void Use(UseContext args)
        {
            if(args.m_Secondary)
            {
                m_Grappling = !m_Grappling;
                return;
            }

            var goRope = GameObject.Instantiate<GameObject>(m_RopePrefab, args.m_OriginPos, Quaternion.identity);
            foreach (var rope in goRope.GetComponents<Rope>())
            {
                var direction = args.Direction;
                bool grappling = m_Grappling;
                //@TEMP not sure if want to use this charging or not

                TwoDee.EasySound.Play("swing", gameObject);
                var speed = 1.0f * args.m_ShowTrajectory.m_Value;
                rope.StartUnravel(grappling, direction, speed, m_RopeLinks, args.GetItemName());
            }

            UseQuantity(args, 1);
        }

        public override void UpdateSelected(UseContext args)
        {
            args.m_ShowTrajectory.m_Value = 11.0f * (m_Grappling ? 1.5f : 1.0f); 
            args.m_ShowTrajectory.m_MaxDistance = 30.0f;
            args.m_ShowTrajectory.m_Gravity = 1.0f;
        }

        public override void Unselected(UseContext args)
        {
        }
    }
}