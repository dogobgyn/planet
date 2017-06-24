using UnityEngine;
using System.Collections;
using System;

namespace TwoDee
{
    public class VoxelGeneratorMiniMap
    {
        public Texture2D Texture
        {
            get
            {
                return m_Texture;
            }
        }

        Texture2D m_Texture;
        VoxelGenerator m_Generator;
        public VoxelGeneratorMiniMap(VoxelGenerator generator)
        {
            m_Generator = generator;
            m_Texture = new Texture2D(generator.Dimension.X, generator.Dimension.Y);
            m_Texture.wrapMode = TextureWrapMode.Clamp;

        }

        public void UpdateAll(int[,] explored)
        {
            Update(explored, 0, 0, m_Generator.Dimension.X, m_Generator.Dimension.Y);
        }

        Vector3 m_LastExploreLocation_gs = new Vector3(-100.0f, -100.0f);
        public void Explore(int[,] explored, Vector3 pos_gs, Vector3 dim_gs)
        {
            if ((m_LastExploreLocation_gs - pos_gs).sqrMagnitude < 10.0f) return;
            m_LastExploreLocation_gs = pos_gs;

            int x0 = Mathf.FloorToInt(pos_gs.x - dim_gs.x);
            int y0 = Mathf.FloorToInt(pos_gs.y - dim_gs.y);
            int x1 = Mathf.FloorToInt(pos_gs.x + dim_gs.x);
            int y1 = Mathf.FloorToInt(pos_gs.y + dim_gs.y);
            x0=x0.Clamp(0, m_Generator.Dimension.X - 1);
            x1=x1.Clamp(0, m_Generator.Dimension.X - 1);
            y0=y0.Clamp(0, m_Generator.Dimension.Y - 1);
            y1=y1.Clamp(0, m_Generator.Dimension.Y - 1);
            for (int x=x0;x<=x1;x++)
            {
                for (int y = y0; y <= y1; y++)
                {
                    explored[x, y] = 1;
                }
            }
            Update(explored, x0, y0, x1+1, y1 + 1);
        }

        public void Update(int[,] explored, int x0, int y0, int x1, int y1)
        {
            x0 = x0.Clamp(0, m_Generator.Dimension.X - 1);
            x1 = x1.Clamp(0, m_Generator.Dimension.X - 1);
            y0 = y0.Clamp(0, m_Generator.Dimension.Y - 1);
            y1 = y1.Clamp(0, m_Generator.Dimension.Y - 1);

            VoxelGenerator.Layer[] layers = new VoxelGenerator.Layer[m_Generator.GetLayerCount()];
            for (int i = 0; i < layers.Length; i++)
            {
                layers[i] = m_Generator.GetLayer(i);
            }

            IntVector2 xy = new IntVector2();
            for(int x=x0;x<x1;x++)
            {
                for(int y=y0;y<y1;y++)
                {
                    xy.Reinit(x, y);
                    var color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                    if (0 != explored[x,y])
                    {
                        color = Color.clear;
                        bool bg = false;
                        bool fg = false;
                        for (int i = 0; i < layers.Length; i++)
                        {
                            float value = layers[i].GetValue(xy);
                            if (value > VoxelGenerator.ZERO_CROSSING_VALUE)
                            {
                                if (layers[i].Background) bg = true;
                                else fg = true;
                            }
                        }
                        if (fg) color = Color.white;
                        else if (bg) color = Color.black;
                    }
                    m_Texture.SetPixel(x, y, color);
                }
            }

            m_Texture.Apply();
        }
    }

}