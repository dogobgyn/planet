using UnityEngine;
using System.Collections;

namespace TwoDee
{
    public class RotateMove : MonoBehaviour
    {
        public float m_DegPerSecRot;

        void Update()
        {
            transform.rotation = transform.rotation * Quaternion.Euler(0, 0, m_DegPerSecRot * Time.deltaTime);
        }
    }

}