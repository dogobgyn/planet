using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

namespace Planet
{
    public class Elevator : MonoBehaviour
    {
        [HideInInspector]
        public Vector3 m_StartingPos;

        Vector3 StartingPos
        {
            get
            {
                return m_StartingPos;
            }
        }

        ConfigurableJoint m_CreatedJoint;
        public GameObject m_PinPrefab;
        GameObject m_Pin;
        private void OnDestroy()
        {
            Destroy(m_Pin.gameObject);
        }
        float VerticalRange
        {
            get { return 10.0f; }
        }
        private void Start()
        {
            m_StartingPos = transform.position;

            m_Pin = GameObject.Instantiate<GameObject>(m_PinPrefab, m_StartingPos, transform.rotation);
            m_Pin.transform.localScale = new Vector3(0.1f, VerticalRange, 0.1f);
            m_Pin.transform.position = m_Pin.transform.position + m_Pin.transform.up * -0.5f * VerticalRange;

            Vector3 downDir;
            TwoDee.RadialGravity.GetDirectionAtPoint(m_StartingPos, out downDir);
            transform.rotation = Quaternion.FromToRotation(Vector3.down, downDir);

            // Create a joint that we move that the elevator has to go to.  When the elevator is too far away from the joint it can't move in that direction to prevent pushing things too hard
            var hj = m_CreatedJoint = transform.parent.gameObject.AddComponent<ConfigurableJoint>();

            //hj.autoConfigureConnectedAnchor = false;
            hj.connectedBody = null;// m_Pin.GetComponent<Rigidbody>();
            hj.anchor = Vector3.zero;
            hj.connectedAnchor = Vector3.zero;
            hj.targetPosition = Vector3.zero;
            //hj.connectedAnchor = transform.position;
            hj.configuredInWorldSpace = true;
            //hj.xMotion = ConfigurableJointMotion.Limited;
            //hj.yMotion = ConfigurableJointMotion.Limited;
            hj.axis = Vector3.forward;
            hj.xDrive = new JointDrive() { positionSpring = 222.0f, maximumForce = 222.0f, positionDamper = 111.0f };
            hj.yDrive = new JointDrive() { positionSpring = 222.0f, maximumForce = 222.0f, positionDamper = 111.0f };
            hj.zDrive = new JointDrive() { positionSpring = 222.0f, maximumForce = 222.0f, positionDamper = 111.0f };

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

        private void FixedUpdate()
        {
            float vertical = 0.0f;

            var player = TwoDee.ComponentList.GetFirst<ThirdPersonUserControl>();
            if ((player != null) && m_Objects.Contains(player.gameObject))
            {
                vertical = Input.GetAxisRaw("Vertical");
            }

            var rb = GetComponent<Rigidbody>();
            if (m_CreatedJoint != null)
            {
                if (m_StartingPos != m_Pin.transform.position)
                {
                    // Not sure how to fix this atm
                    //m_StartingPos = m_Pin.transform.position;
                    //m_CreatedJoint.connectedBody = null;
                    //m_CreatedJoint.anchor = Vector3.zero;
                    //m_CreatedJoint.connectedAnchor = Vector3.zero;
                }

                float movePerSecond = 2.0f;
                float maxAnchorOffsetDistance = 1.5f;
                var curPos = transform.position;
                var newAnchor = m_CreatedJoint.targetPosition;

                Vector3 downDir;
                TwoDee.RadialGravity.GetDirectionAtPoint(curPos, out downDir);
                float oldDownDot = Vector3.Dot(downDir, newAnchor);
                float curDownDot = Vector3.Dot(downDir, m_StartingPos - transform.position);

                newAnchor += downDir * vertical * movePerSecond * Time.fixedDeltaTime;
                float downDot = Vector3.Dot(downDir, newAnchor);

                // Preveng moving beyond our total range
                downDot = Mathf.Clamp(downDot, -VerticalRange, 0.0f);
                float delta = (downDot - curDownDot);

                // Prevent moving too far from where we are now
                delta = Mathf.Clamp(delta, -maxAnchorOffsetDistance, maxAnchorOffsetDistance);
                downDot = curDownDot + delta;
                newAnchor = downDot * downDir;
                /*
                var delta = (newAnchor - curPos);
                if (delta.magnitude > maxAnchorOffsetDistance)
                {
                    newAnchor = curPos + (delta.normalized) * maxAnchorOffsetDistance;
                }
                // put it on closest down line
                downDot = Mathf.Clamp(downDot, 0.0f, 10.0f);
                newAnchor = m_StartingPos + downDot * downDir;
                */

                newAnchor.z = 0.0f;

                m_CreatedJoint.targetPosition = newAnchor;
            }
        }
    }

}