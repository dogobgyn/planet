using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using UnityEngine.Networking;

namespace TwoDee
{
    public class Explosion : NetworkBehaviour
    {
        List<GameObject> m_Hit = new List<GameObject>();
        public List<DamageArgs> m_Damages = new List<DamageArgs>();
        public float m_DigRadius = 0.0f;

        [SyncVar]
        GameObject m_OwningPlayer;

        public GameObject OwningPlayer
        {
            set { m_OwningPlayer = value; }
            get { return m_OwningPlayer; }
        }

        public string m_StartSound;

        float m_Lifetime = 0.0f;
        void Update()
        {
            if (m_Lifetime == 0.0f)
            {
                TwoDee.EasySound.Play(m_StartSound, gameObject);
            }
            m_Lifetime += Time.deltaTime;
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.isTrigger) return;
            if (!isServer) return;
            if (m_Lifetime > 0.1f) return;
            if (m_Damages.Count == 0)
            {
                Debug.LogAssertion("Zero damage explosion");
            }
            if (m_DigRadius > 0.0f)
            {
                var voxelGen = TwoDee.ComponentList.GetFirst<TwoDee.VoxelGenerator>();
                voxelGen.ModifyCircle(2.0f, false, transform.position, m_DigRadius + 0.01f);
            }
            foreach(var damage in m_Damages)
            {
                damage.m_Location = other.ClosestPointOnBounds(transform.position);
            }
            var go = other.gameObject;
            if (go != null)
            {
                if (!m_Hit.Contains(go))
                {
                    m_Hit.Add(go);

                    Health.ApplyDamage(go, m_Damages);
                }
            }
        }
    }
}