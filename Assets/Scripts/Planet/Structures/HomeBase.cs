
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using UnityEngine;
using TwoDee;

namespace Planet
{
    public class HomeBase : MonoBehaviour, IMouseButtons, TwoDee.IProxyReady
    {
        void Start()
        {
            TwoDee.ComponentList.OnStart(this);
        }
        void OnDestroy()
        {
            TwoDee.ComponentList.OnEnd(this);
        }

        List<GameObject> m_Objects = new List<GameObject>();
        void OnTriggerEnter(Collider other)
        {
            m_Objects.Add(other.gameObject);
        }
        void OnTriggerExit(Collider other)
        {
            m_Objects.Remove(other.gameObject);
        }

        void UpdateGlobe(float dt)
        {
            var globe = transform.FindChild("Geo");
            if (globe != null)
            {
                globe.rotation *= Quaternion.Euler(10.0f*dt, 20.0f*dt, 0);
            }
        }

        float lastUpdateTime = -1.0f;
        private void FixedUpdate()
        {
            UpdateGlobe(Time.fixedDeltaTime);
            float deltaWorldTime = 0.0f;
            var worldState = WorldState.Instance;
            if (worldState != null)
            {
                float currentTimeGmt = worldState.CurrentTimeGMT;
                if (lastUpdateTime > 0.0f)
                {
                    deltaWorldTime = currentTimeGmt - lastUpdateTime;
                }
                lastUpdateTime = currentTimeGmt;
            }

            var contList = new List<IInventory>();
            contList.Add(GetComponent<IInventory>());
            foreach(var ob in m_Objects)
            {
                if (!ob) continue;
                contList.Add(ob.GetComponent<IInventory>());
            }
            foreach(var cont in contList)
            {
                if (cont != null)
                {
                    foreach(var inv in cont.Inventories)
                    {
                        inv.ChargeEnergy(30.0f * deltaWorldTime);
                    }
                }
            }
        }

        void IMouseButtons.GetButtons(MouseButtonContext context)
        {
            context.m_CanTeleport = true;
            context.m_Entries.Add(new MouseButtonEntry("Sleep", 0));
            //context.m_Entries.Add(new MouseButtonEntry("Chop Wood", 1));
        }

        void IMouseButtons.UseButton(MouseButtonContext context, MouseButtonEntry entry)
        {
            if (entry.m_Data.Equals(0))
            {
                var health = context.m_Player.GetComponent<TwoDee.Health>();
                //if (health.m_Health != health.m_MaxHealth)
                {
                    float hoursToSleep = 1;
                    float healPerHour = 10.0f;
                    health.RawDamage(new TwoDee.DamageArgs(-(hoursToSleep*healPerHour), TwoDee.DamageType.Pure, gameObject, context.m_Player.transform.position));
                    WorldState.Instance.SpeedTime(hoursToSleep);
                }
            }
            /*
            else if (entry.m_Data.Equals(1))
            {
                foreach (var log in TwoDee.ComponentList.GetCopiedListOfType<Log>())
                {
                    if ((log.transform.position - transform.position).magnitude < 20.0f)
                    {
                        GetComponent<Container>().Inventory.AddInventory(new InventoryEntry("wood", log.m_WoodLeft), true);
                        Destroy(log.gameObject);
                    }
                }
            }
            */
        }

        bool IProxyReady.ReadyToCreateProxy()
        {
            return false; // Never create proxy for base except when saving game
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