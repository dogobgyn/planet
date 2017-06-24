using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;

namespace Planet
{
    public class TrajectoryPreview : TwoDee.PolyGenerator
    {
        
        public void UpdateTrajectory(Vector3 vel, float maxDistance, float gravityMultiplier)
        {
            CreateGeoStart(0);
            Vector3 pos = new Vector3();
            float dt = 0.1f;
            float t = 0.0f;
            while (maxDistance > 0.0f)
            {
                //@TODO not sure why maybe beacuse of step difference that this is so off
                float dtg = 0.08f;
                Vector3 pos0 = pos;
                float t1 = t + dt;
                Vector3 gravity;
                TwoDee.RadialGravity.GetDirectionAtPoint(transform.TransformPoint(pos), out gravity);
                gravity *= gravityMultiplier;

                pos += vel * dt + 0.5f * gravity * dtg * dtg * 9.8f;
                vel += gravity * dtg * 9.8f;
                Vector3 posDelta = pos - pos0;
                Vector3 perp = Vector3.Cross(Vector3.back, posDelta).normalized * 0.02f;

                // Inset slightly
                pos0 += posDelta * 0.2f;
                pos -= posDelta * 0.2f;
                AddQuad(pos0 - perp,
                    pos - perp,
                    pos + perp,
                    pos0 + perp, new Color32(255, 255, 255, 88));
                t = t1;

                maxDistance -= posDelta.magnitude;
            }
            CreateGeoEnd(0);
        }

    }
}