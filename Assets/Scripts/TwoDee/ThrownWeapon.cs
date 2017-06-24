using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace TwoDee
{
    public class ThrownWeapon : MonoBehaviour
    {
        [HideInInspector]
        public GameObject m_Thrower;
        [HideInInspector]
        public float m_ThrowIntensity;

        public bool m_FaceDir;
        public GameObject m_StrikeExplosionPrefab;
        public GameObject m_TimedOrInputExplosionPrefab;
        public float m_TimedBomb = 0.0f;
        float m_TimedBombTime = 0.0f;

        public float m_SweepRadius = 0.3f;
        float m_MinDamage;
        float m_MaxDamage;
        public float m_CritMultiplier = 2.0f;

        bool m_DidExplode;

        float m_Time;
        bool m_DidSetLastKnownPosition;
        Vector3 m_LastKnownPosition;
        Vector3 m_LastKnownVelocity;
        float m_GoAwayTime = 0.0f;

        bool IsTimedBomb
        {
            get
            {
                return m_TimedBomb > 0.0f;
            }
        }
        float DamageDone
        {
            get
            {
                if (m_ThrowIntensity == 1.0f) return m_CritMultiplier * m_MaxDamage;
                return Mathf.Lerp(m_MinDamage, m_MaxDamage, m_ThrowIntensity);
            }
        }

        void DamageTarget(bool collision, Quaternion rot, Vector3 pos, GameObject go, GameObject hitter)
        {
            // sphere cast problem if the target is in the start of the sweep.
            if (pos == Vector3.zero) pos = transform.position;

            if (!m_DidExplode)
            {
                // Don't do anything else if friendly
                if (!TwoDee.Health.ApplyDamageIfEnemy(go, hitter, new TwoDee.DamageArgs(DamageDone, TwoDee.DamageType.Physical, hitter, pos)))
                {
                    return;
                }

                if (m_RigidBody != null)
                {
                    m_RigidBody.angularVelocity = 0.1f * m_RigidBody.angularVelocity;
                    if (!collision)
                    {
                        m_RigidBody.velocity = Vector3.zero;
                    }
                }

                if (m_StrikeExplosionPrefab != null)
                {
                    Instantiate(m_StrikeExplosionPrefab, pos, Quaternion.identity);
                }
                if (m_TimedOrInputExplosionPrefab != null)
                {
                    var ob = Instantiate(m_TimedOrInputExplosionPrefab, pos, Quaternion.identity);
                    NetworkServer.Spawn(ob);
                }                

                if (m_GoAwayTime == 0.0f)
                {
                    m_GoAwayTime = 0.01f;
                }

                m_DidExplode = true;
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            if (IsTimedBomb) return;

            ContactPoint contact = collision.contacts[0];
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, contact.normal);
            Vector3 pos = contact.point;

            DamageTarget(true, rot, pos, collision.gameObject, m_Thrower);

        }

        void SweepHit(Vector3 start, Vector3 end)
        {
            //@TODO not sure if always want to do this since really the rigid body should be on this plane
            start.z = end.z = 0.0f;

            Vector3 delta = (end - start);
            Vector3 fwd = delta.normalized;
            var didHitAlready = new Dictionary<GameObject, bool>();

            foreach (var hit in Physics.SphereCastAll(new Ray(start, fwd), m_SweepRadius, delta.magnitude))
            {
                var go = hit.collider.gameObject;
                if (go.layer == LayerMask.NameToLayer("Ground"))
                {
                    m_GoAwayTime = 0.01f;
                }

                if (go == gameObject) continue;
                if (m_Time < 2.0f && go == m_Thrower) continue;

                var health = go.GetComponentInSelfOrParents<TwoDee.Health>();
                if (health != null && !health.m_IgnoreThrown)
                {
                    if (!didHitAlready.ContainsKey(go))
                    {
                        didHitAlready[go] = true;

                        DamageTarget(false, transform.rotation, new Vector3(hit.point.x, hit.point.y, 0), hit.collider.gameObject, m_Thrower);
                    }
                }
            }
        }

        public float m_Range = 10.0f;
        Vector3 m_StartPoint;
        Rigidbody m_RigidBody;
        private void Start()
        {
            m_StartPoint = transform.position;
            m_RigidBody = GetComponent<Rigidbody>();
        }

        float m_DistanceTraveled;
        void FixedUpdate()
        {
            if (IsTimedBomb)
            {
                m_TimedBombTime += Time.fixedDeltaTime;
                if (m_TimedBombTime > m_TimedBomb)
                {
                    if (m_TimedOrInputExplosionPrefab != null)
                    {
                        var ob = Instantiate(m_TimedOrInputExplosionPrefab, transform.position, Quaternion.identity);
                        NetworkServer.Spawn(ob);
                    }
                    gameObject.DestroyReleaseParticles();
                }
                return;
            }
            //if ((m_StartPoint - transform.position).magnitude > m_Range)
            if (m_DistanceTraveled > m_Range)
            {
                if(!m_DidExplode)
                {
                    if (m_TimedOrInputExplosionPrefab != null)
                    {
                        var ob = Instantiate(m_TimedOrInputExplosionPrefab, transform.position, Quaternion.identity);
                        NetworkServer.Spawn(ob);
                    }
                }
                gameObject.DestroyReleaseParticles();
                return;
            }

            m_Time += Time.fixedDeltaTime;
            var curpos = transform.position;
            if (m_DidSetLastKnownPosition)
            {
                var delta = curpos - m_LastKnownPosition;
                m_DistanceTraveled += delta.magnitude;
                SweepHit(m_LastKnownPosition, curpos);
            }
            m_DidSetLastKnownPosition = true;
            m_LastKnownPosition = curpos;
            if (m_RigidBody != null)
            {
                m_LastKnownVelocity = m_RigidBody.velocity;
            }

            if (m_GoAwayTime > 0.0f)
            {
                m_GoAwayTime -= Time.fixedDeltaTime;
                if (m_GoAwayTime < 0.0f)
                {
                    gameObject.DestroyReleaseParticles();
                }
            }

            if (m_FaceDir)
            {
                var normVel = m_LastKnownVelocity.normalized;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.FromToRotation(Vector3.up, normVel), 70.0f*Time.fixedDeltaTime);
            }
        }

        public static ThrownWeapon DoThrow(GameObject thrownPrefab, Vector3 pos, Vector3 dir, GameObject thrower, float speed, float throwIntensity, float range, bool spin, float minDamage, float maxDamage)
        {
            bool flipped = (dir.x < 0.0f);
            var rot = spin ? (Quaternion.FromToRotation(flipped ? Vector3.left : Vector3.right, dir) * Quaternion.Euler(0.0f, flipped ? 180.0f : 0.0f, 0.0f))
                : Quaternion.FromToRotation(Vector3.up, dir);

            var thrownObject = GameObject.Instantiate(thrownPrefab, pos, rot);

            var rb = thrownObject.GetComponent<Rigidbody>();
            var fp = thrownObject.GetComponent<FakePhysics>();
            if (rb != null)
            {
                if (spin)
                {
                    rb.angularVelocity = new Vector3(0.0f, 0.0f, 100.0f * (flipped ? 1.0f : -1.0f));
                }
                rb.velocity = dir * speed;
            }
            else if (fp != null)
            {
                if (spin)
                {
                    fp.AngularVelocity = 100.0f * (flipped ? 1.0f : -1.0f);
                }
                fp.Velocity = dir * speed;
            }

            var tw = thrownObject.GetComponent<TwoDee.ThrownWeapon>();
            if (tw != null)
            {
                tw.m_MinDamage = minDamage;
                tw.m_MaxDamage = maxDamage;
                tw.m_Thrower = thrower;
                tw.m_ThrowIntensity = throwIntensity;
                if (range > 0.0f) tw.m_Range = range;
            }

            return tw;
        }
    }

}