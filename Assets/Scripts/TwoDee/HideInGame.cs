using UnityEngine;
using System.Collections;

namespace TwoDee
{
    public class HideInGame : MonoBehaviour
    {
        void Start()
        {
            foreach (var renderer in gameObject.GetComponentsInSelfOrChildren<Renderer>())
            {
                renderer.enabled = false;
            }
        }
    }

}