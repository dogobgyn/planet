using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;

namespace Planet
{
    public class TractorPreview : TwoDee.PolyGenerator
    {
        List<Vector3> m_Points = new List<Vector3>();

        public void UpdateTractor(Vector3 origin_ws, Vector3 start_ws, Vector3 end_ws)
        {
            m_Points.Clear();
            m_Points.Add(origin_ws);
            m_Points.Add(start_ws);
            m_Points.Add(end_ws);
            // place at origin since we're just going to work in world space.
            transform.position = Vector3.zero;

            CreateGeoStart(0);

            for (int i=0;i<m_Points.Count-1;i++)
            {
                Vector3 pos0 = m_Points[i];
                Vector3 pos = m_Points[i+1];

                Vector3 posDelta = pos - pos0;
                Vector3 perp = Vector3.Cross(Vector3.back, posDelta).normalized * 0.02f;

                // Inset slightly
                //pos0 += posDelta * 0.2f;
                //pos -= posDelta * 0.2f;

                AddQuad(pos0 - perp,
                    pos - perp,
                    pos + perp,
                    pos0 + perp, new Color32(255, 255, 255, 88));
            }
            CreateGeoEnd(0);
        }

    }
}