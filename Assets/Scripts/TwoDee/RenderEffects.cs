using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;

namespace TwoDee
{
    public class RenderEffectsOut
    {
        public Color? m_Color;
        public bool? m_Visible;
        public int m_Priority;
        public List<Renderer> m_Renderers;

        public RenderEffectsOut()
        {
            m_Renderers = new List<Renderer>();
        }
    }

    public interface IRenderEffects
    {
        void GetRenderEffects(ref RenderEffectsOut effects);
    }

    public class RenderEffects : MonoBehaviour
    {
        Renderer m_Renderer;
        List<IRenderEffects> m_RenderEffects = new List<IRenderEffects>();
        public void Start()
        {
            foreach (var ire in GetComponents<IRenderEffects>())
            {
                m_RenderEffects.Add(ire);
            }
            m_Renderer = GetComponent<Renderer>();
        }

        public void UpdateEffects()
        {
            var reouts = new List<RenderEffectsOut>();
            foreach(var ire in m_RenderEffects)
            {
                var reo = new RenderEffectsOut();
                ire.GetRenderEffects(ref reo);
                reouts.Add(reo);
            }

            var finalReout = new RenderEffectsOut();
            finalReout.m_Color = Color.white;
            finalReout.m_Visible = true;
            foreach(var renderer in GameObjectExt.GetComponentsInSelfOrChildren<Renderer>(gameObject))
            {
                finalReout.m_Renderers.Add(renderer);
            }

            reouts.Sort((x, y) => x.m_Priority.CompareTo(y.m_Priority));

            foreach(var reo in reouts)
            {
                if (reo.m_Color.HasValue) finalReout.m_Color = reo.m_Color;
                if (reo.m_Visible.HasValue) finalReout.m_Visible = reo.m_Visible;
                foreach (var renderer in reo.m_Renderers) finalReout.m_Renderers.Add(renderer);
            }

            {
                foreach (var renderer in finalReout.m_Renderers)
                {
                    Material mat = renderer.material;

                    Color finalColor = finalReout.m_Color.Value;
                    //bool renderEnabled = finalReout.m_Visible.Value;

                    mat.SetColor("_Color", finalColor);
                    //renderer.enabled = renderEnabled;
                }
            }
        }

        public void Update()
        {
            UpdateEffects();
        }
    }
}