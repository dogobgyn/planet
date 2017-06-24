using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.Rendering;

namespace TwoDee
{
    public static class EasySound
    {
        public class Result
        {
            public Result()
            {

            }
        }

        public class Args
        {
            public Vector3 Position
            {
                set; get;
            }
            Args(Vector3 pos)
            {
                Position = pos;
            }
            public static implicit operator Args(GameObject go)
            {
                return new Args(go.transform.position);
            }
            public static implicit operator Args(Vector3 pos)
            {
                return new Args(pos);
            }
        }

        public static Result Play(string name, Args args)
        {
            if (name.IsEmpty()) return null;
            var asset = Resources.Load<AudioClip>("Sounds/"+name);
            AudioSource.PlayClipAtPoint(asset, args.Position);
            return null;
        }
    }
}
