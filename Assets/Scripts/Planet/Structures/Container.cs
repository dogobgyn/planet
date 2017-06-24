
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using UnityEngine;

namespace Planet
{
    public class Container : MonoBehaviour, IMouseInfo, IInventory
    {
        public string[] m_InitialStuff;
        [NonSerialized]
        public Inventory m_Inventory = null;
        public int m_Slots = 10;

        public bool m_DeleteWhenEmpty;

        public Inventory[] Inventories
        {
            get
            {
                if (m_Inventory == null)
                {
                    m_Inventory = new Inventory(m_Slots);
                    if (m_InitialStuff != null)
                    {
                        m_Inventory.AddStuff(m_InitialStuff);
                    }
                }
                return new Inventory[] { m_Inventory };
            }
        }

        private void FixedUpdate()
        {
            if (m_DeleteWhenEmpty)
            {
                if (m_Inventory.CountTotalItems == 0)
                {
                    Destroy(gameObject);
                }
            }
        }

        public Container()
        {
        }

        private void Start()
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
                return "Open/Close Container";
            }
        }
    }
}