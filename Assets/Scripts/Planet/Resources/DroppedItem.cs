using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Planet
{
    public class DroppedItem : TwoDee.Proxied, IMouseInfo
    {
        InventoryEntry m_Entry;

        [Serializable]
        public class DroppedItemProxy : TwoDee.ProxyWorld.Proxy
        {
            public override void Unload(GameObject go)
            {
                m_Entry = go.GetComponent<DroppedItem>().m_Entry;
                base.Unload(go);
            }

            public override GameObject Load()
            {
                var result = base.Load();
                result.GetComponent<DroppedItem>().Entry = m_Entry;
                return result;
            }

            public InventoryEntry m_Entry;
        }

        public InventoryEntry Entry
        {
            get
            {
                return m_Entry;
            }
            set
            {
                m_Entry = value;
                var sprite = ItemDatabase.GetIconStatic(m_Entry.m_Name);
                foreach(var rend in this.gameObject.GetComponentsInSelfOrChildren<SpriteRenderer>())
                {
                    rend.sprite = sprite;
                }
                // Sprites are different size but the thing is it uses the pixel dimensions instead of some standard dimension so we have to scale.
                //GetComponent<SphereCollider>().radius = sprite.bounds.extents.x;
                transform.localScale = new Vector3(1.0f / sprite.bounds.extents.x, 1.0f / sprite.bounds.extents.y, 1.0f);

            }
        }

        void IMouseInfo.GetMouseInfo(MouseInfoArgs args)
        {
            args.Value = Value;
        }

        public string Value
        {
            get
            {
                if (Entry == null) return "";
                return string.Format("{0}({1})", Entry.m_Name, Entry.m_Count);
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

        public override TwoDee.ProxyWorld.Proxy CreateProxy(string prefab)
        {
            var result = new DroppedItemProxy();
            result.Init(m_DefaultBoundsWidthHeight, prefab, transform.position, transform.rotation);
            return result;
        }

        protected override void VirtualStart()
        {

        }
    }
}