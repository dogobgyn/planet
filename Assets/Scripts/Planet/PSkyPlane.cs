using UnityEngine;
using System.Collections;

namespace Planet
{
    public class PSkyPlane : TwoDee.SkyPlane
    {
        void Update()
        {
            var worldState = WorldState.Instance;
            if (worldState == null) return;

            var cam = Camera.main;
            transform.localScale = new Vector3(5.0f, 5.0f, 1.0f);

            float intensity = worldState.SunlightIntensity;

            foreach (var renderer in GameObjectExt.GetComponentsInSelfOrChildren<Renderer>(gameObject))
            {
                Material mat = renderer.material;

                mat.SetColor("_EmissionColor", new Color(intensity, intensity, intensity, 1.0f));
            }
        }
    }

}