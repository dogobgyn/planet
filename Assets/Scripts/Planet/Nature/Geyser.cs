using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using TwoDee;

namespace Planet
{
    public class Geyser : MonoBehaviour, TwoDee.IKillable
    {
        int m_EruptionState = -1;
        ParticleSystem GetPsAndAct(string str, bool act)
        {
            var hps = transform.FindChild(str).gameObject.GetComponent<ParticleSystem>();
            if (act)
            {
                hps.gameObject.SetActive(true);
                hps.Play(true);
            }
            else
            {
                //hps.gameObject.SetActive(false);
                hps.Stop();
            }

            return hps;
        }

        void ChangeEruptionState(int newState)
        {
            if (m_EruptionState != newState)
            {
                if (newState == 2)
                {
                    TwoDee.EasySound.Play("steam", gameObject);
                    m_TriggerBot = m_TriggerTop = 0.0f;
                }
                m_EruptionState = newState;
                GetPsAndAct("low", newState==0);
                GetPsAndAct("med", newState == 1);
                GetPsAndAct("hi", newState == 2);
            }
        }
        private float m_TimeTilErupt;
        void TimeNextEruption()
        {
            if (m_EruptionState != 0)
            {
                m_TimeTilErupt = UnityEngine.Random.Range(2.0f, 2.4f);
            }
            else
            {
                m_TimeTilErupt = UnityEngine.Random.Range(4.0f, 5.0f);
            }
        }

        List<GameObject> m_Objects = new List<GameObject>();
        void OnTriggerEnter(Collider other)
        {
            m_Objects.Add(other.gameObject);
        }

        void OnTriggerExit(Collider other)
        {
            m_Objects.Remove(other.gameObject);
        }

        private void Start()
        {
            TimeNextEruption();
            ChangeEruptionState(0);
        }
        float m_TriggerBot = 0.0f;
        float m_TriggerTop = 0.0f;
        float m_PopDamage = 0.0f;
        void UpdateTrigger(float dt)
        {
            float delta = dt * 10.0f;
            if (m_EruptionState == 2)
            {
                m_TriggerTop += delta;
                m_TriggerTop = Mathf.Min(m_TriggerTop, 5.0f);
            }
            else
            {
                m_TriggerBot += delta;
                m_TriggerBot = Mathf.Min(m_TriggerTop, 5.0f);
            }
            var box = GetComponent<BoxCollider>();
            var height = m_TriggerTop - m_TriggerBot;
            box.center = new Vector3(0.0f, m_TriggerBot + height / 2.0f);
            box.size = new Vector3(0.3f, height, 3.0f);

            m_PopDamage += dt;
            bool popDamage = false;
            if (m_PopDamage > 0.5f)
            {
                m_PopDamage = 0.5f;
                popDamage = true;
            }
            if (height > 0.5f)
            {
                foreach(var ob in m_Objects)
                {
                    if (!ob) continue;
                    var touchPlace = ob.GetComponent<Collider>().ClosestPointOnBounds(transform.position);
                    var rb = ob.GetComponentInSelfOrParents<Rigidbody>();

                    if (rb != null)
                    {
                        if (rb.gameObject == gameObject) continue;

                        rb.AddForceAtPosition(dt * -500.0f * TwoDee.RadialGravity.GetDirectionAtPoint(transform.position), touchPlace);
                    }
                    // periodic damage
                    if (popDamage)
                    {
                        TwoDee.Health.ApplyDamageIfEnemy(ob, gameObject, new TwoDee.DamageArgs(5.0f, TwoDee.DamageType.Physical, gameObject, touchPlace));
                    }
                    //ob.transform.position = new Vector3(ob.transform.position.x , ob.transform.position.y+ 10.0f * dt, ob.transform.position.z);
                }
            }
        }
        private void FixedUpdate()
        {
            var dt = Time.fixedDeltaTime;
            UpdateTrigger(dt);

            m_TimeTilErupt -= dt;
            int newState = m_EruptionState;
            if (m_TimeTilErupt < 0.0f)
            {
                ChangeEruptionState((m_EruptionState + 1) % 3);
                TimeNextEruption();
            }
        }

        void IKillable.Kill()
        {
            gameObject.DestroyReleaseParticles();
        }
    }

}