using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TwoDee
{
    public class FogPolyGenerator : PolyGenerator
    {
        float[,] m_Revealed;
        int m_XDim = 20;
        int m_YDim = 20;
        float m_ScaleX = 10.0f;
        float m_ScaleY = 10.0f;

        public static FogPolyGenerator Instance
        {
            get; private set;
        }

        void Start()
        {
            m_Revealed = new float[m_XDim, m_YDim];
            Instance = this;
        }

        void Update()
        {
            CreateGeo(null);
        }

        Color32 ColorAt(int x, int y)
        {
            return new Color32(0, 0, 0, (byte)(255 - 255 * m_Revealed[x, y]));
        }

        Vector3 PointAt(int x, int y)
        {
            return new Vector3(x * m_ScaleX, y * m_ScaleY);
        }

        public void RevealPoint(int x, int y, float r)
        {
            if (x >=0 && y>=0 && x < m_XDim && y<m_YDim)
            {
                float oldValue = m_Revealed[x, y];
                m_Revealed[x, y] = Mathf.Max(oldValue, r);
            }
        }

        public void RevealArea(Vector3 pos, float r)
        {
            var selfPos = transform.position;
            pos.x -= selfPos.x;
            pos.y -= selfPos.y;

            var intRadiusX = (int)((r / m_ScaleX) + 1.5f);
            var intRadiusY = (int)((r / m_ScaleY) + 1.5f);
            int intx = (int)(pos.x / m_ScaleX);
            int inty = (int)(pos.y / m_ScaleY);
            for (int x = intx - intRadiusX; x <= intx + intRadiusX; x++)
            {
                for(int y = inty - intRadiusY; y <= inty + intRadiusY; y++)
                {
                    float distance = (PointAt(x, y) - pos).magnitude;
                    float scaledDist = (r - distance) / m_ScaleX;
                    //if (scaledDist >= 0.0f);
                    //if (distance < r)
                    if (scaledDist >= -1.0f)
                    {
                        RevealPoint(x, y, Mathf.Clamp(1.0f + scaledDist, 0.0f, 1.0f));
                    }
                }
            }
        }

        virtual protected void CreateGeoProtected(Camera cam)
        {
            for (int x = 0; x < m_XDim-1; x++)
            {
                for (int y = 0; y < m_YDim-1; y++)
                {
                    AddQuad(
                        PointAt(x, y),
                        PointAt(x + 1, y),
                        PointAt(x + 1, y + 1),
                        PointAt(x, y + 1),
                        ColorAt(x, y),
                        ColorAt(x + 1, y),
                        ColorAt(x + 1, y + 1),
                        ColorAt(x, y + 1)
                        );
                }
            }
        }
    }
}