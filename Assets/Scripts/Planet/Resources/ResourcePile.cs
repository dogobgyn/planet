using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using TwoDee;

namespace Planet
{
    public class ResourcePile : MonoBehaviour, IMouseInfo, TwoDee.IGridPointsDirty, IProxyData
    {
        public GameObject m_DroppedItemPrefab;

        [Serializable]
        public class ResourcePileProxy : ProxyDataComp<ResourcePile>
        {
            public Inventory m_Inventory;

            protected override void SaveLoad(bool save, ResourcePile comp)
            {
                if (save)
                {
                    if (comp.Inventory == null)
                    {
                        comp.GenerateInventory();
                    }
                    m_Inventory = comp.Inventory;
                }
                else
                {
                    comp.Inventory = m_Inventory;
                }
            }
        }

        void IProxyData.Load(GameObject go)
        {
            throw new NotImplementedException();
        }

        void IProxyData.Unload(GameObject go)
        {
            throw new NotImplementedException();
        }

        public bool AllowsCollect(InventoryEntry selectedToolSlot)
        {
            if (null == m_ItemRequiresProp || m_ItemRequiresProp.Trim().Length == 0) return true;

            if (null == selectedToolSlot) return false;
            var entry = ItemDatabase.GetEntryStatic(selectedToolSlot.m_Name);
            var propVal = entry.GetCombinedProp(m_ItemRequiresProp, selectedToolSlot.m_Properties);
            return propVal != null;
        }

        Inventory m_Inventory;
        public Inventory Inventory
        {
            set
            {
                m_Inventory = value;
            }
            get
            {
                return m_Inventory;
            }
        }

        public RanomInventoryGenEntry[] m_Entries;


        void IMouseInfo.GetMouseInfo(MouseInfoArgs args)
        {
            args.Value = Value;
        }

        public string Value
        {
            get
            {
                string result = gameObject.name;
                if (GetComponent<Proxied>().Prefab != null)
                {
                    result = GetComponent<Proxied>().Prefab.name;
                }
                result = string.Format("{0} ({1})", result, m_Inventory.CountTotalItems);

                return result;
            }
        }

        public class Pair<T1,T2>
        {
            public T1 First
            {
                get { return m_A; }
            }
            public T2 Second
            {
                get { return m_B; }
            }
            T1 m_A;
            T2 m_B;
            public Pair(T1 a, T2 b)
            {
                m_A = a;
                m_B = b;
            }
        }

        string GenerateInventory()
        {
            m_Inventory = new Inventory(10);
            return m_Inventory.RandomGenerate(m_Entries, "");
        }

        public void OnDestroy()
        {
            TwoDee.ComponentList.OnEnd(this);
        }

        public void Awake()
        {
            TwoDee.ComponentList.OnStart(this);
        }

        void Start()
        {
            string lastEntry = GenerateInventory();
        }

        public string m_ItemRequiresProp;

        List<GameObject> m_Objects = new List<GameObject>();
        void OnTriggerEnter(Collider other)
        {
            m_Objects.Add(other.gameObject);
        }

        void OnTriggerExit(Collider other)
        {
            m_Objects.Remove(other.gameObject);
        }

        void GroundDestroyed()
        {
            /*
            // Create some consolation instances
            var pos = transform.position;
            pos.z = 0.0f;
            var di = TwoDee.ProxyWorld.PostInstantiate(GameObject.Instantiate(m_DroppedItemPrefab, pos, Quaternion.identity), m_DroppedItemPrefab);
            di.GetComponent<DroppedItem>().Entry = new InventoryEntry(lastEntry, 1);
            */
            DestroyObject(gameObject);
        }

        void TwoDee.IGridPointsDirty.GridPointsDirty(TwoDee.GridPointsDirtyArgs args)
        {
            if (args.DirtyBox_ws.Contains(transform.position))
            {
                // Recheck to see if we still have a leg to stand on
                var vgen = ComponentList.GetFirst<PVoxelGenerator>();
                var probePoint = transform.position + transform.up * -0.1f;
                if (vgen.IsPointClearAt_ws(probePoint))
                {
                    GroundDestroyed();
                }
            }
        }
    }
}