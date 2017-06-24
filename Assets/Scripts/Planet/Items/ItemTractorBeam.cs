
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using UnityEngine;

namespace Planet
{
    public class ItemTractorBeam : Item
    {
        private GameObject m_Tractor;
        private Vector3 m_TractorLocalOffset;

        public GameObject m_TractorEffectPrefab;
        GameObject m_TractorEffect;

        float MaxUseDistance
        {
            get { return 5.0f; }
        }

        bool m_UsingStrongBeam;

        bool InternalBeginUse(UseContext args)
        {
            m_UsingStrongBeam = args.m_ButtonPressed != 0;
            m_Tractor = null;

            var target = args.m_TargetPos;

            if ((target - args.m_OriginPos).magnitude > MaxUseDistance)
            {
                args.m_ShowStatusMessage = "Target position too far away to tractor";
                return false;
            }
            var ropes = GameObjectExt.GetNearbyObjects<Rigidbody>(target, 1.0f, GameObjectExt.GetLayerMask("Rope"));
            ropes.Sort((a, b) =>
            {
                return a.transform.position.DistanceTo(target).CompareTo(b.transform.position.DistanceTo(target));
            }
            );

            var bodies = GameObjectExt.GetNearbyObjects<Rigidbody>(target, 1.0f,
                GameObjectExt.GetLayerMask("Objects") |
                GameObjectExt.GetLayerMask("Rope") |
                GameObjectExt.GetLayerMask("Trees")
                );
            bodies.Sort((a, b) =>
                    {
                        return a.transform.position.DistanceTo(target).CompareTo(b.transform.position.DistanceTo(target));
                    }
            );
            foreach (var rope in ropes) bodies.Insert(0, rope);

            foreach (Rigidbody body in bodies)
            {
                var link = body.gameObject.GetComponent<RopeLink>();
                if (link != null && !link.CanTractor) continue;

                // whelp we're not moving this thing
                if (body.isKinematic) continue;

                m_Tractor = body.gameObject;
                m_TractorLocalOffset = body.gameObject.transform.InverseTransformPoint(args.m_TargetPos);

                if (TractorRopeLink != null)
                {
                    TractorRopeLink.ConnectJoint(null);
                }

                return true;
            }

            args.m_ShowStatusMessage = "Tractor must be used on loose object";
            return false;
        }

        protected override void VirtualBeginUse(UseContext args)
        {
            bool result = InternalBeginUse(args);
            args.m_Error = !result;
        }

        protected override void VirtualUsing(UseContext args)
        {
            if (args.m_ButtonHeld == -1 || m_Tractor == null)
            {
                args.m_Done = true;
            }
            if (m_Tractor == null) return;

            if (m_TractorEffect == null)
            {
                m_TractorEffect = GameObject.Instantiate<GameObject>(m_TractorEffectPrefab, Vector3.zero, Quaternion.identity);
            }

            var rb = m_Tractor.GetComponent<Rigidbody>();
            if (rb != null)
            {
                TwoDee.EasySound.Play("tractorbeam", args.m_TargetPos);

                float scaleForce = 500.0f;
                float maxForceRope = 200.0f;
                float maxForce = 12.0f;
                if (m_UsingStrongBeam && UseEnergy(args, args.m_DeltaTime * 1.0f))
                {
                    maxForce = 16.0f;
                    scaleForce = 600.0f;
                }

                var target = args.m_TargetPos; //MaxUseDistance
                var originDelta = (target - args.m_OriginPos);
                if (originDelta.magnitude > MaxUseDistance)
                {
                    target = args.m_OriginPos + (originDelta).normalized * MaxUseDistance;
                }


                var currentOffset_ws = m_Tractor.transform.TransformPoint(m_TractorLocalOffset);
                // Predict where tractor will go and use that point
                float predictionDt = 0.2f;
                currentOffset_ws += m_Tractor.GetComponent<Rigidbody>().velocity * predictionDt;
                var delta = (target - currentOffset_ws);
                var desiredForce = scaleForce * delta;
                var maxForceToUse = (TractorRopeLink != null) ? maxForceRope : maxForce;
                if (desiredForce.magnitude > maxForceToUse)
                {
                    desiredForce = desiredForce.normalized * maxForceToUse;
                }
                rb.AddForceAtPosition(desiredForce, currentOffset_ws);
                // Conform rotation if needed
                var oldEuler = rb.gameObject.transform.rotation.eulerAngles;
                oldEuler.x = oldEuler.y = 0.0f;
                rb.gameObject.transform.rotation = Quaternion.Euler(oldEuler);

                m_TractorEffect.GetComponent<TractorPreview>().UpdateTractor(args.m_OriginPos, target, currentOffset_ws);
            }
        }

        ConfigurableJoint CreateJoint2(Rigidbody body, Rigidbody connectedBody, float ps, float mf, float pd)
        {
            var hj = body.gameObject.AddComponent<ConfigurableJoint>();

            hj.connectedBody = connectedBody;
            hj.anchor = Vector3.zero;

            //hj.connectedAnchor = transform.position;
            //hj.configuredInWorldSpace = true;
            //hj.xMotion = ConfigurableJointMotion.Limited;
            //hj.yMotion = ConfigurableJointMotion.Limited;
            hj.axis = Vector3.forward;


            hj.xDrive = new JointDrive() { positionSpring = ps, maximumForce = mf, positionDamper = pd };
            hj.yDrive = new JointDrive() { positionSpring = ps, maximumForce = mf, positionDamper = pd };
            hj.zDrive = new JointDrive() { positionSpring = ps, maximumForce = mf, positionDamper = pd };

            return hj;
        }

        ConfigurableJoint CreateJoint(Rigidbody body, Rigidbody connectedBody, float ps, float mf, float pd)
        {
            var hj = connectedBody.gameObject.AddComponent<ConfigurableJoint>();

            hj.connectedBody = body;
            hj.anchor = Vector3.zero;

            //hj.connectedAnchor = transform.position;
            //hj.configuredInWorldSpace = true;
            //hj.xMotion = ConfigurableJointMotion.Limited;
            //hj.yMotion = ConfigurableJointMotion.Limited;
            hj.axis = Vector3.forward;


            hj.xDrive = new JointDrive() { positionSpring = ps, maximumForce = mf, positionDamper = pd };
            hj.yDrive = new JointDrive() { positionSpring = ps, maximumForce = mf, positionDamper = pd };
            hj.zDrive = new JointDrive() { positionSpring = ps, maximumForce = mf, positionDamper = pd };

            return hj;
        }

        void ConnectToRope(UseContext args)
        {
            var ropeLinkPos = TractorRopeLink.gameObject.transform.position;

            foreach (Rigidbody body in GameObjectExt.GetNearbyObjects<Rigidbody>(ropeLinkPos, 0.7f,
                GameObjectExt.GetLayerMask("Objects") | GameObjectExt.GetLayerMask("StructurePlatform") | GameObjectExt.GetLayerMask("Structure")

                ))
            {
                var go = body.gameObject;
                float ps = 322.0f;
                float mf = 322.0f;
                ps = mf = 1000.0f;
                float pd = 111.0f;

                var hj = CreateJoint(body, m_Tractor.GetComponent<Rigidbody>(), ps, mf, pd);
                // ore always connects in the center
                if (go.name.ToLower().Contains("ore"))
                {
                    hj.autoConfigureConnectedAnchor = false;
                    hj.connectedAnchor = Vector3.zero;
                    hj.targetPosition = Vector3.zero;
                }
                else
                {
                    hj.autoConfigureConnectedAnchor = false;
                    hj.anchor = go.transform.InverseTransformPoint(args.m_TargetPos);
                    hj.targetPosition = Vector3.zero;
                }

                if(TractorRopeLink.ConnectJoint(hj))
                {
                    return;
                }
            }

            // Couldn't connect, see if we assume it was a refund
            TractorRopeLink.m_Rope.CheckRefund(TractorRopeLink);
        }

        RopeLink TractorRopeLink
        {
            get
            {
                return (m_Tractor != null) ? m_Tractor.GetComponent<RopeLink>() : null;
            }
        }
        protected override void VirtualEndUse(UseContext args)
        {
            // If dragging a rope try to connect it to something
            if (m_Tractor != null && TractorRopeLink != null)
            {
                ConnectToRope(args);
            }
            DestroyImmediate(m_TractorEffect);
        }
    }
}