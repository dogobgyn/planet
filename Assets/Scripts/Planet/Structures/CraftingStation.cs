
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using UnityEngine;

namespace Planet
{
    public class CraftingStation : MonoBehaviour, IMouseInfo
    {
        public string m_StationName = "MCU";

        public CraftingStation()
        {
        }

        void IMouseInfo.GetMouseInfo(MouseInfoArgs args)
        {
            args.Value = Value;
        }

        public string Value
        {
            get
            {
                return m_StationName;
            }
        }

        void EnterExit(bool enter, Collider other)
        {
            var tpc = other.GetComponent<ThirdPersonUserControl>();
            if (tpc != null)
            {
                tpc.AddRemoveCraftingStation(enter, m_StationName.ToLower());
            }
        }
        void OnTriggerEnter(Collider other)
        {
            EnterExit(true, other);
        }

        void OnTriggerExit(Collider other)
        {
            EnterExit(false, other);
        }
    }
}