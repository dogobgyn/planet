
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using UnityEngine;

namespace Planet
{
    public class StoredResourceArgs
    {
        public string m_Item;
        public int m_Count;
    }

    public interface IStoredResource
    {
        void OneHarvest(StoredResourceArgs args);
        bool IsHarvestable
        {
            get;
        }
        string[] Tags
        {
            get;
        }
    }

    public class Refiner : MonoBehaviour, IMouseInfo
    {
        public string m_Tags;
        public string[] Tags
        {
            get
            {
                return m_Tags.Split(';');
            }
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

        public bool MatchesTag(IStoredResource resource)
        {
            // Untagged refiner can do anything.
            if (m_Tags == null || m_Tags.Length == 0) return true;

            // Check each resource tag and if we don't have that then return false.
            var resourceTags = resource.Tags;
            var myTags = new List<string>(Tags);
            foreach (var resourceTag in resourceTags)
            {
                if (!myTags.Contains(resourceTag)) return false;
            }

            return true;
        }

        Vector3 m_SawBladeOrigin;
        public void Start()
        {
        }


        public GameObject m_SawBlade;
        float m_SawingTime = 0.0f;

        public bool m_RequiresEnergy;
        public float m_Radius = 30.0f;

        private void Update()
        {
            var inv = GetComponent<Container>().FirstInventory();
            IStoredResource closestLog = null;
            GameObject closestLogGo = null;
            foreach (var log in TwoDee.ComponentList.GetCopiedListOfType<IStoredResource>())
            {
                if (!log.IsHarvestable) continue;
                if (!MatchesTag(log)) continue;

                var logGo = log as MonoBehaviour;
                var tp = logGo.gameObject.GetTopParent();

                if ((logGo.transform.position - transform.position).magnitude > m_Radius)
                {
                    continue;
                }

                // Cannot process if it's flying by
                float spd = 0.0f;
                var rb = logGo.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    spd = rb.velocity.magnitude;
                }
                if (spd > 3.0f) continue;

                var top = logGo.gameObject.GetTopParent();
                foreach(var ob in m_Objects)
                {
                    if(ob.GetTopParent() == top)
                    {
                        closestLog = log;
                        closestLogGo = logGo.gameObject;
                        break;
                    }
                }
            }

            if (m_SawBladeOrigin  == Vector3.zero)
            {
                m_SawBladeOrigin = m_SawBlade.transform.position;
            }

            Vector3 sawPos = m_SawBladeOrigin;
            if (closestLog != null && (!m_RequiresEnergy || DrainEnergy(10.0f, false)))
            {
                var logGo = closestLogGo;
                var rb = logGo.GetComponent<Rigidbody>();
                if (m_SawingTime == 0.0f)
                {
                    TwoDee.EasySound.Play("extractresource", sawPos);
                }

                if (rb != null)
                {
                    // Prevent further movement
                    Destroy(rb);
                    // This is a problem for the Buried component since it relies on kinematic
                    //rb.isKinematic = true;
                }
                sawPos = logGo.transform.position;

                var col = logGo.gameObject.GetComponentInSelfOrChildren<Collider>();
                if (col != null)
                {
                    sawPos = col.bounds.center;
                }
                sawPos.z = -1.0f;

                m_SawingTime += Time.deltaTime;
                sawPos += logGo.transform.up * Mathf.Sin(m_SawingTime * Mathf.PI * 1.0f);
                if (m_SawingTime > 2.0f && (!m_RequiresEnergy || DrainEnergy(10.0f, true)))
                {
                    m_SawingTime = 0.0f;
                    StoredResourceArgs args = new StoredResourceArgs();
                    bool moreHarvest = true;
                    var data = new Inventory.AnnounceCollectData();
                    while (closestLog.IsHarvestable && moreHarvest)
                    {
                        moreHarvest = false;
                        closestLog.OneHarvest(args);
                        if (args.m_Count > 0)
                        {
                            moreHarvest = true;
                            var ent = new InventoryEntry(args.m_Item, args.m_Count);
                            data.m_Entries.Add(ent.Clone());
                            data.Announce(transform.position);
                            inv.AddInventory(ent, true);
                        }
                    }
                    data.Announce(transform.position);
                }
            }
            m_SawBlade.transform.position = sawPos;
            if (!m_RequiresEnergy || DrainEnergy(10.0f, false))
            {
                m_SawBlade.transform.rotation = m_SawBlade.transform.rotation * Quaternion.Euler(0, 0, Time.deltaTime * 100.0f);
            }
        }

        bool DrainEnergy(float amount, bool doIt)
        {
            return PowerConduit.DrainEnergy(gameObject, amount, doIt);
        }

        string GetMouseInfo()
        {
            string result = "";
            var myTags = new List<string>(Tags);
            if (myTags.Contains("level0")) result = "Tier1";
            result += "(" + (DrainEnergy(10.0f, false) ? "powered" : "unpowered") + ")";

            return result;
        }

        void IMouseInfo.GetMouseInfo(MouseInfoArgs args)
        {
            args.Value += GetMouseInfo();
        }
    }
}