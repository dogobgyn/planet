
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using UnityEngine;

namespace Planet
{
    public class ItemThrown : Item
    {
        public float m_ThrowRange = 10.0f;
        public GameObject m_ThrownPrefab;
        public float m_TimedBomb = 0.0f;
        public float m_FullyChargeTime = 1.0f;
        public float m_Gravity = 1.0f;
        public bool m_Spin = true;
        public float m_MinDamage;
        public float m_MaxDamage;

        public void FinallyThrow(UseContext args)
        {
            var pos = args.m_OriginPos;
            var dir = args.Direction;

            TwoDee.EasySound.Play(m_UseSound, args.m_OriginPos);
            var thrown = TwoDee.ThrownWeapon.DoThrow(m_ThrownPrefab, pos, dir, args.m_ItemUserGameObject, args.m_ShowTrajectory.m_Value, ThrowIntensity, m_ThrowRange, m_Spin, m_MinDamage, m_MaxDamage);
            if (!m_Spin) thrown.m_FaceDir = true;

            if (m_TimedBomb > 0.0f)
            {
                thrown.m_TimedBomb = m_TimedBomb;
            }

            UseQuantity(args, 1);
        }

        protected override void VirtualBeginUse(UseContext args)
        {
            args.m_ShowTrajectory.m_Value = m_ThrowMin;
            args.m_ShowTrajectory.m_MaxDistance = m_ThrowRange;
            args.m_ShowTrajectory.m_Gravity = m_Gravity;
        }

        float MinUseTime
        {
            get
            {
                float minUseTime = 0.4f;
                return minUseTime;
            }
        }
        float ThrowIntensity
        {
            get
            {
                float t = (m_UseTime - MinUseTime) / m_FullyChargeTime;
                return Mathf.Clamp01(t);
            }
        }
        protected override void VirtualUsing(UseContext args)
        {
            float minUseTime = MinUseTime;
            if (m_UseTime > minUseTime)
            {
                
                args.m_ShowTrajectory.m_Value = m_ThrowMin + (m_ThrowMax - m_ThrowMin) * ThrowIntensity;

                if (args.m_ButtonHeld == -1)
                {
                    args.m_Done = true;
                }
            }
        }

        protected override void VirtualEndUse(UseContext args)
        {
            FinallyThrow(args);
            args.m_ShowTrajectory.m_Value = 0.0f;
        }
    }
}