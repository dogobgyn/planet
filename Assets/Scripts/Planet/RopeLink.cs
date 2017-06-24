
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
    public class RopeLink : MonoBehaviour
    {
        public Vector3 m_LocalSpaceTip;
        public Rope m_Rope;

        public bool CanTractor
        {
            get
            {
                return m_Rope.CanTractor(this);
            }
        }

        void OnJointBreak(float breakForce)
        {
            UnityEngine.Debug.Log("ARGH");
        }

        string m_JointGuid = null;
        Joint m_Joint;
        public bool ConnectJoint(Joint joint, bool callOnConnect=true)
        {
            if (joint != null && joint.connectedBody != null)
            {
                var newGuid = TwoDee.Proxied.GetGuid(joint.connectedBody.gameObject);
                if (newGuid.IsEmpty()) return false;
            }

            GameObject oldConnection = JointConnection;
            if (m_Joint != null)
            {
                if (m_Joint)
                {
                    DestroyObject(m_Joint);
                }
            }

            m_Joint = joint;
            m_JointGuid = TwoDee.Proxied.GetGuid(JointConnection);
            if (callOnConnect)
            {
                m_Rope.OnConnectJoint(this, joint, JointConnection, oldConnection);
            }
            return true;
        }

        public GameObject JointConnection
        {
            get
            {
                // The connected object is always the other one.
                if (m_Joint != null && m_Joint.connectedBody != null && m_Joint.connectedBody.gameObject != gameObject)
                {
                    return m_Joint.connectedBody.gameObject;
                }
                return null;
            }
        }

        public string JointGuid
        {
            get
            {
                // Check if the guid is still valid (that is, the object exists either as a proxy or a real object)
                var gop = new TwoDee.ProxyWorld.GameObjectOrProxy(m_JointGuid);
                if (!gop.Valid) m_JointGuid = null;
                return m_JointGuid;
            }
        }

        public Vector3 JointPosition
        {
            get
            {
                if(m_Joint != null)
                {
                    return transform.TransformPoint(m_Joint.anchor);
                }

                return Vector3.zero;
            }
        }
    }
}