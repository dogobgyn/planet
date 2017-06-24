
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
    public class FlyerAIController : MonoBehaviour, TwoDee.IDamageNotification, TwoDee.IKillable
    {

        // AI state
        GameObject m_Aggro;
        bool m_Divebombing;
        float m_StateT;
        Vector3 m_Offset;
        Vector3 m_HomeBase;

        FlyerCharacter m_Character;
        private void Start()
        {
            m_Character = GetComponent<FlyerCharacter>();
            m_HomeBase = transform.position;
        }

        float m_Speed;
        float m_Ang;
        float m_T;

        public float GetAngleAtten(Vector3 targetPos)
        {
            var delta = targetPos - transform.position;
            float desiredAngle = Mathf.Rad2Deg * Mathf.Atan2(delta.y, delta.x);
            float angleDelta = Mathf.DeltaAngle(m_Ang, desiredAngle);
            // Crazy monsters that starts and stops
            // return 3.0f*Mathf.Sin(m_T)
            float angleAttenuation = (180.0f - Mathf.Abs(angleDelta)) / 180.0f;

            return Mathf.Pow(angleAttenuation, 2.0f);
        }
        public float AllowedAccel(Vector3 targetPos)
        {
            float aa = GetAngleAtten(targetPos);

            if (m_Divebombing) return Mathf.Lerp(0.0f, 3.0f, aa);
            return 1.0f;
        }

        public float AllowedSpeed(Vector3 targetPos)
        {
            float aa = GetAngleAtten(targetPos);
            if (m_Divebombing) return 10.0f * Mathf.Lerp(0.3f, 1.0f, aa);

            return 6.0f * Mathf.Lerp(0.3f, 1.0f, aa);
        }

        static FlyerAIController ms_DivebombLock;

        void AcquireOffset()
        {
            if (m_Divebombing && ms_DivebombLock == this) ms_DivebombLock = null;

            if (!m_Divebombing && !ms_DivebombLock && UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f)
            {
                ms_DivebombLock = this;
                m_Divebombing = true;
                m_Offset = Vector3.zero;

                return;
            }
            m_Divebombing = false;

            var down = RadialGravity.GetDirectionAtPoint(transform.position);
            var forward = Vector3.Cross(down, Vector3.forward);
            m_Offset = down * UnityEngine.Random.Range(-7.0f, -5.0f) + forward * UnityEngine.Random.Range(-8.0f, 8.0f);
            // temp easily see pos
            //transform.position = m_Aggro.transform.position + m_Offset;
        }

        public bool IsTargetValid(GameObject target)
        {
            if (!target) return false;

            var health = target.GetComponent<Health>();
            if (health == null || health.Dead)
            {
                return false;
            }
            return true;
        }

        public void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            m_T += dt;

            if (m_Aggro != null)
            {
                m_StateT += dt;
                if (m_StateT > 3.0f)
                {
                    m_StateT = 0.0f;
                    AcquireOffset();
                }


                if (!IsTargetValid(m_Aggro))
                {
                    if (ms_DivebombLock == this) ms_DivebombLock = null;
                    m_Aggro = null;
                    return;
                }
                var targetPos = m_Aggro.transform.position + m_Offset;
                var delta = targetPos - transform.position;

                if (m_Divebombing && delta.magnitude < 3.0f)
                {
                    m_Character.Args.m_DoAttack = true;
                }

                var rb = GetComponent<Rigidbody>();

                if (m_Aggro)
                {
                    m_Character.Args.m_TargetPos = m_Aggro.transform.position + m_Offset;
                }
                else
                {
                    m_Character.Args.m_TargetPos = Vector3.zero;
                }
                /*
                var oldVel = rb.velocity;
                var oldSpeed = m_Speed;
                var newSpeed = oldSpeed + AllowedAccel(targetPos) * dt;
                newSpeed = Mathf.Clamp(newSpeed, 0.0f, AllowedSpeed(targetPos));
                float angPerSec = 90.0f;
                m_Ang = Mathf.MoveTowardsAngle(m_Ang, Mathf.Rad2Deg*Mathf.Atan2(delta.y, delta.x), angPerSec * dt);
                rb.velocity = TwoDee.Math3d.FromLengthAngleDegrees2D(newSpeed, m_Ang);
                */

                // m_Speed = newSpeed;
            }
            else
            {
                m_Character.Args.m_TargetPos = m_HomeBase;
                foreach (var player in TwoDee.ComponentList.GetCopiedListOfType<TwoDee.ThirdPersonUserControl>())
                {
                    if (!IsTargetValid(player.gameObject)) continue;

                    var playerpos = player.gameObject.transform.position;

                    if ((playerpos - transform.position).magnitude < 5.0f)
                    {
                        m_Aggro = player.gameObject;
                        m_Divebombing = false;
                        AcquireOffset();
                        return;
                    }
                }

            }

            m_Character.ControlUpdate();
        }

        void IDamageNotification.DamageNotification(DamageNotificationArgs args)
        {
            if(args.m_Args.m_Hitter.GetComponent<ThirdPersonUserControl>())
            {
                if (m_Aggro == null) m_Aggro = args.m_Args.m_Hitter;
                if (m_Divebombing)
                {
                    AcquireOffset();
                }
            }

            m_Speed = 0.0f;
        }

        public void Kill()
        {
            if (ms_DivebombLock == this) ms_DivebombLock = null;
        }
    }
}