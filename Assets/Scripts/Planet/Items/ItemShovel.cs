
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using UnityEngine;

namespace Planet
{
    public class ItemShovel : GhostItem
    {
        public float m_DigRadius = 1.25f;
        public float DigRadius
        {
            get { return m_DigRadius; }
        }

        public override void Selected(UseContext args)
        {
            m_ValidPlacement = true;
            base.Selected(args);
            float fudgeScale = 0.9f;
            float radiusDoubleWithScale = (2.0f * DigRadius) * fudgeScale;
            m_Ghost.transform.localScale = new Vector3(radiusDoubleWithScale, radiusDoubleWithScale, radiusDoubleWithScale);
        }

        public override void Use(UseContext args)
        {
            float power = args.m_Entry.DbEntry.GetCombinedPropFloat("power", args.m_Entry.m_Properties);
            Vector3 dir = args.m_TargetPos - args.m_OriginPos;
            // Shoot a ray from the origin to the target to see the point we will 'dig' at.
            RaycastHit hitInfo;

            bool additive = false;

            // Only carving for now
            //additive = args.m_Secondary;

            //if (Physics.Raycast(new Ray(args.m_OriginPos, dir.normalized), out hitInfo, 2.0f, GameObjectExt.GetLayerMask("Ground")))
            {
                TwoDee.EasySound.Play("dig", args.m_OriginPos);

                foreach (var vg in TwoDee.ComponentList.GetCopiedListOfType<TwoDee.VoxelGenerator>())
                {
                    vg.ModifyCircle(power, additive, ClampedPos(args), DigRadius);
                }

                ChargeTimeCost();
                UseQuantity(args, 1);
            }
        }

        public Vector3 ClampedPos(UseContext args)
        {
            var pos = args.m_TargetPos + m_PlacementOffset;
            float dist = args.DirectionDistance;
            // Place it close enough that it's not really possible to create really ugly notches from intersecting circles
            dist = Mathf.Min(dist, m_DigRadius*0.85f);

            return args.m_OriginPos + args.Direction * dist;
        }

        public override void UpdateSelected(UseContext args)
        {
            base.UpdateSelected(args);

            m_Ghost.transform.position = ClampedPos(args);
        }
    }
}