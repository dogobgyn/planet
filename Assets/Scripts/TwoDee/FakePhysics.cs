using UnityEngine;
using System.Collections;

namespace TwoDee
{
    public class FakePhysics : MonoBehaviour
    {
        float m_AngularVelocity;
        public float AngularVelocity
        {
            set { m_AngularVelocity = value; }
            get { return m_AngularVelocity; }
        }

        Vector3 m_Velocity;
        public Vector3 Velocity
        {
            set { m_Velocity = value; }
            get { return m_Velocity; }
        }

        void FixedUpdate()
        {
            transform.rotation *= Quaternion.Euler(0.0f, 0.0f, m_AngularVelocity * Time.fixedDeltaTime);
            transform.position = transform.position + m_Velocity * Time.fixedDeltaTime;
        }
    }

}