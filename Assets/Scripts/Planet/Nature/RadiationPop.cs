using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Planet
{
    public class RadiationPop : MonoBehaviour
    {
        List<GameObject> m_Objects = new List<GameObject>();
        void OnTriggerEnter(Collider other)
        {
            m_Objects.Add(other.gameObject);
        }

        void OnTriggerExit(Collider other)
        {
            m_Objects.Remove(other.gameObject);
        }

        float m_PopDamage;

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;

            m_PopDamage += dt;
            bool popDamage = false;
            float POP_TIME = 0.1f;
            float POP_AMOUNT = 10.0f;
            if (m_PopDamage > POP_TIME)
            {
                m_PopDamage -= POP_TIME;
                popDamage = true;
            }

            foreach (var ob in m_Objects)
            {
                if (!ob) continue;
                var touchPlace = ob.GetComponent<Collider>().ClosestPointOnBounds(transform.position);
                var rb = ob.GetComponentInSelfOrParents<Rigidbody>();

                if (rb != null)
                {
                    if (rb.gameObject == gameObject) continue;

                    //rb.AddForceAtPosition(dt * -500.0f * TwoDee.RadialGravity.GetDirectionAtPoint(transform.position), touchPlace);
                }
                // periodic damage
                if (popDamage)
                {
                    TwoDee.Health.ApplyDamageIfEnemy(ob, gameObject, new TwoDee.DamageArgs(POP_AMOUNT, TwoDee.DamageType.Radiation, gameObject, touchPlace));
                }
                //ob.transform.position = new Vector3(ob.transform.position.x , ob.transform.position.y+ 10.0f * dt, ob.transform.position.z);
            }
        }
    }

}