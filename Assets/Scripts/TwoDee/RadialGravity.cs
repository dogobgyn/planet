using UnityEngine;
using System.Collections;

namespace TwoDee
{
    public class RadialGravity : MonoBehaviour
    {
        Rigidbody m_RigidBody;
        public Vector3 m_GravityDir = Vector3.down;
        void Start()
        {
            m_RigidBody = GetComponent<Rigidbody>();
        }

        static public bool m_RadialEnabled = true;

        public static bool GetDirectionAtPoint(Vector3 pt, out Vector3 dir)
        {
            if (!m_RadialEnabled)
            {
                dir = Vector3.down;
                return true;
            }

            dir = Vector3.zero - pt;
            // Only change if not near the very center
            if (dir.magnitude < 10.0f)
            {
                return false;
            }

            dir.Normalize();

            return true;
        }

        public static float Intensity
        {
            get { return 9.8f; }
        }

        public static Vector3 GetDirectionAtPoint(Vector3 pt)
        {
            Vector3 result;
            GetDirectionAtPoint(pt, out result);

            return result;
        }
        void FixedUpdate()
        {
            if (!enabled) return;

            Vector3 dir;
            if (GetDirectionAtPoint(transform.position, out dir))
            {
                m_GravityDir = dir;
            }
            if (m_RigidBody != null)
            {
                m_RigidBody.AddForce(m_GravityDir * Intensity, ForceMode.Acceleration);
            }
        }
    }

}