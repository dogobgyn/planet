using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using UnityEngine.Networking;

namespace TwoDee
{
    public interface IKillable
    {
        void Kill();
    }

    public class Killable : NetworkBehaviour, IKillable
    {
        public GameObject m_Explosion;

        public void Kill()
        {
            if (m_Explosion != null)
            {
                var spawn = GameObject.Instantiate<GameObject>(m_Explosion, transform.position, Quaternion.identity);
                NetworkServer.Spawn(spawn);
            }
            DestroyObject(gameObject);
        }
    }
}