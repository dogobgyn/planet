using System;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using UnityStandardAssets.CrossPlatformInput;
using UnityEngine.EventSystems;

namespace TwoDee
{
    public class Team : NetworkBehaviour
    {
        [SyncVar]
        public int m_CurrentTeam = -1;

        public int CurrentTeam
        {
            get { return m_CurrentTeam; }
            set
            {
                if (value != m_CurrentTeam)
                {
                    m_CurrentTeam = value;
                }
            }
        }

        [SyncVar]
        GameObject m_Shooter;

        public GameObject Shooter
        {
            get
            {
                return m_Shooter;
            }
            set
            {
                m_Shooter = value;
                CurrentTeam = m_Shooter.GetComponent<TwoDee.Team>().CurrentTeam;
            }
        }

        public static bool IsEnemy(GameObject a, GameObject b)
        {
            if (a == null || b == null) return false;
            var ateam = a.ComponentCache().Team;
            var bteam = b.ComponentCache().Team;
            int ateami = (ateam != null && ateam ? ateam.CurrentTeam : -1);
            int bteami = (bteam != null && bteam ? bteam.CurrentTeam : -1);
            if (ateami != bteami)
            {
                return true;
            }

            return false;
        }

    }

}
