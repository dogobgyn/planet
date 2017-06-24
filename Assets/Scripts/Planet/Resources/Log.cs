using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.Networking;
using TwoDee;

namespace Planet
{
    public class Log : MonoBehaviour
    {
        public GameObject m_FoliageBurstPrefab;

        public Vector3 m_FellLoc;
        Quaternion m_OriginalRot;
        void Start()
        {
            m_OriginalRot = transform.rotation;
            var logrb = GetComponent<Rigidbody>();
            float mult = (Vector3.Dot(m_FellLoc, transform.right) > Vector3.Dot(transform.position, transform.right)) ? 1.0f : -1.0f;
            logrb.AddTorque(new Vector3(0.0f, 0.0f, mult*300.0f));
        }

        void OnCollisionEnter(Collision collision)
        {
            if (Quaternion.Angle(m_OriginalRot, transform.rotation) > 30.0f)
            {
                var branches = transform.Search("branches");
                if (branches != null)
                {
                    if (m_FoliageBurstPrefab != null)
                    {
                        var newob = GameObject.Instantiate(m_FoliageBurstPrefab, branches.position, branches.rotation);
                        newob.transform.localScale = transform.localScale;
                    }
                    Destroy(branches.gameObject);
                    gameObject.SetLayerRecursive(LayerMask.NameToLayer("Objects"));
                }
            }
        }
    }
}