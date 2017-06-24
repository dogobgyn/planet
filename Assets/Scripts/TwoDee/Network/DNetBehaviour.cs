using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using UnityEngine.Networking;

namespace TwoDee
{
    public class Net
    {
        public static GameObject Instantiate(GameObject original)
        {
            var result = GameObject.Instantiate<GameObject>(original);
            NetworkServer.Spawn(result);
            return result;
        }

        public static GameObject Instantiate(GameObject original, Transform parent)
        {
            var result = GameObject.Instantiate<GameObject>(original, parent);
            NetworkServer.Spawn(result);
            return result;
        }

        public static GameObject Instantiate(GameObject original, Vector3 position, Quaternion rotation)
        {
            var result = GameObject.Instantiate<GameObject>(original, position, rotation);
            NetworkServer.Spawn(result);
            return result;
        }

        public static GameObject Instantiate(GameObject original, Transform parent, bool worldPositionStays)
        {
            var result = GameObject.Instantiate<GameObject>(original, parent, worldPositionStays);
            NetworkServer.Spawn(result);
            return result;
        }

        public static GameObject Instantiate(GameObject original, Vector3 position, Quaternion rotation, Transform parent)
        {
            var result = GameObject.Instantiate<GameObject>(original, position, rotation, parent);
            NetworkServer.Spawn(result);
            return result;
        }
    }

    public class DNetBehaviour: NetworkBehaviour
        {
    }
}