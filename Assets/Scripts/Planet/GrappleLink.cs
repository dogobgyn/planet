
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using UnityEngine;

namespace Planet
{
    public class GrappleLink : MonoBehaviour
    {
        bool didGrapple = false;
        void OnCollisionEnter(Collision collision)
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                var hj = Rope.StandardInitHinge(gameObject);
                hj.anchor = transform.InverseTransformPoint(contact.point);
                GetComponent<RopeLink>().ConnectJoint(hj);
                Destroy(this);

                return;
            }
        }

    }
}