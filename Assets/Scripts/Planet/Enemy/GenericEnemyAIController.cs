
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
    public class GenericEnemyAIController : MonoBehaviour, TwoDee.IDamageNotification, TwoDee.IKillable
    {
        public bool m_Chase;

        // AI state
        GameObject m_Aggro;
        bool m_Divebombing;
        float m_StateT;
        Vector3 m_Offset;
        Vector3 m_HomeBase;

        GenericEnemyCharacter m_Character;
        private void Start()
        {
            m_Character = GetComponent<GenericEnemyCharacter>();
            m_HomeBase = transform.position;
        }

        public bool IsTargetValid(GameObject target)
        {
            if (!target) return false;

            var health = target.GetComponent<Health>();
            if (health == null || health.Dead)
            {
                return false;
            }

            var deaggroRange = m_Character.GetAggroRange(false);
            if (m_Chase) deaggroRange = 160.0f;
            if ((target.transform.position - transform.position).magnitude > deaggroRange)
            {
                return false;
            }
            return true;
        }

        float m_T;
        float m_Speed = 0.0f;

        Vector3 m_Target;

        void CalcNewTarget()
        {
            var offset = 3.0f * UnityEngine.Random.insideUnitCircle;
            m_Target = m_HomeBase + new Vector3(offset.x, offset.y);
        }

        public void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            m_T += dt;

            m_Character.Args.m_ShootPos = Vector3.zero;

            if (m_Aggro != null)
            {
                m_Character.Args.m_ShootPos = m_Aggro.transform.position;
                

                m_StateT += dt;
                if (m_Chase)
                {
                    m_Target = transform.position + transform.TransformDirection(Vector3.right);
                }
                else
                {
                    float nextTime = m_Character.GetPerkBool("fast") ? 1.0f : 3.0f;
                    if (m_StateT > nextTime)
                    {
                        CalcNewTarget();
                        m_StateT = 0.0f;
                    }
                }

                if (!IsTargetValid(m_Aggro))
                {
                    m_Aggro = null;
                }
                else
                {
                    var targetPos = m_Target;
                    var delta = targetPos - transform.position;

                    var rb = GetComponent<Rigidbody>();

                    var offset = transform.position;

                    m_Character.Args.m_TargetPos = m_Target;

                    m_Character.Args.m_TargetFacing = m_Aggro.transform.position;
                }

            }
            else
            {
                m_Character.Args.m_TargetFacing = transform.position + RadialGravity.GetDirectionAtPoint(transform.position);
                m_Character.Args.m_TargetPos = m_HomeBase;
                foreach (var player in TwoDee.ComponentList.GetCopiedListOfType<TwoDee.ThirdPersonUserControl>())
                {
                    if (!IsTargetValid(player.gameObject)) continue;

                    var playerpos = player.gameObject.transform.position;

                    var aggroRange = m_Character.GetAggroRange(true);
                    if (m_Chase) aggroRange = 80.0f;

                    if ((playerpos - transform.position).magnitude < aggroRange)
                    {
                        m_Aggro = player.gameObject;
                        return;
                    }
                }

            }

            m_Character.ControlUpdate();
        }

        void IDamageNotification.DamageNotification(DamageNotificationArgs args)
        {
            if (args.m_Args.m_Hitter)
            {
                if (args.m_Args.m_Hitter.GetComponent<ThirdPersonUserControl>())
                {
                    CalcNewTarget();
                    if (m_Aggro == null) m_Aggro = args.m_Args.m_Hitter;
                }
            }

            m_Speed = 0.0f;
        }

        public void Kill()
        {
        }
    }
}