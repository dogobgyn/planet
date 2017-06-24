using UnityEngine;
using System.Collections;

namespace TwoDee
{
    public class SkyPlane : MonoBehaviour
    {
        void Update()
        {
            var cam = Camera.main;
            transform.localScale = new Vector3(150.0f, 150.0f, 1.0f);
        }
    }

}