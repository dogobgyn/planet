using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.Networking;
using TwoDee;

namespace Planet
{
    public class StoredResource : NetworkBehaviour, IMouseInfo, IStoredResource, TwoDee.IProxy
    {
        public float m_AmountLeft = 50;
        public float m_WeightScale = 1.0f;
        public float m_LossPerJostle = 0.03f;

        public string m_Type = "oreiron";
        public string[] m_ExtraTypes;
        public bool m_Harvestable = true;
        public bool m_Invunlerable = false;
        public GameObject m_JostleParticlePrefab;
        public string m_ImpactSound;

        [NonSerialized]
        Inventory m_Inventory = null;
        public Inventory Inventory
        {
            get
            {
                if (m_Inventory == null)
                {
                    m_Inventory = new Inventory(10);
                }
                return m_Inventory;
            }
            private set
            {
                m_Inventory = value;
            }
        }

        int m_ExtraTypesGiven;
        public int Deduct(int amount, out string type)
        {
            // Give anything out of 'm_ExtraTypes' first
            if (m_ExtraTypes != null && m_ExtraTypes.Length > m_ExtraTypesGiven)
            {
                type = m_ExtraTypes[m_ExtraTypesGiven];

                m_ExtraTypesGiven++;
                return 1;
            }
            // If we have items instead of ore use that instead
            if (Inventory.CountTotalItems > 0)
            {
                for(int i=0;i<Inventory.Count;i++)
                {
                    var slot = Inventory.GetSlot(i);
                    if (slot != null)
                    {
                        type = slot.m_Name;
                        Inventory.ClearSlot(i);
                        if (Inventory.CountTotalItems == 0)
                        {
                            m_Harvestable = false;
                            Destroy(gameObject);
                        }
                        return slot.m_Count;
                    }
                }
            }

            type = m_Type;
            if (m_AmountLeft == 0) return 0;
            amount = Math.Min(amount, Mathf.CeilToInt(m_AmountLeft));
            m_AmountLeft -= amount;
            if (m_AmountLeft <= 0)
            {
                m_Harvestable = false;
                Destroy(gameObject);
            }

            return amount;
        }

        void IMouseInfo.GetMouseInfo(MouseInfoArgs args)
        {
            args.Value = Value;
        }

        string Value
        {
            get
            {
                return "Resource " + m_Type + " " + Mathf.CeilToInt(m_AmountLeft);
            }
        }

        float m_LastSoundTime = -1.0f;
        Vector3 m_LastSoundTimeLocation = Vector3.zero;

        float m_LastCollisionTime = -1.0f;
        Vector3 m_LastCollisionPos = Vector3.zero;
        void OnCollisionEnter(Collision collision)
        {
            OnCollisionStay(collision);
        }
        void OnCollisionStay(Collision collisionInfo)
        {
            var rb = gameObject.GetComponent<Rigidbody>();
            if (rb != null && rb)
            {
                var hitLocation = collisionInfo.contacts[0].point;
                var otherRb = (collisionInfo.gameObject.GetComponent<Rigidbody>());
                var otherVelocity = Vector3.zero;
                if (otherRb != null)
                {
                    otherVelocity = otherRb.GetPointVelocity(hitLocation);
                }
                var pointVelocity = rb.GetPointVelocity(hitLocation) - otherVelocity;
                var pointSpeed = pointVelocity.magnitude;
                if (pointSpeed > 0.5f)
                {
                    if (Time.time - m_LastSoundTime > 0.1f && (m_LastSoundTimeLocation - hitLocation).magnitude > 0.5f)
                    {
                        TwoDee.EasySound.Play(m_ImpactSound, collisionInfo.contacts[0].point);
                        if (m_JostleParticlePrefab != null)
                        {
                            var conzero = collisionInfo.contacts[0];
                            GameObject.Instantiate<GameObject>(m_JostleParticlePrefab, conzero.point, Quaternion.FromToRotation(Vector3.up, conzero.normal));
                        }
                        m_LastSoundTime = Time.time;
                        m_LastSoundTimeLocation = hitLocation;
                    }
                }

                if (!m_Invunlerable)
                {
                    if (pointVelocity.magnitude > 10.0f)
                    {
                        if (GetJostled(m_LossPerJostle)) return;
                        if (m_JostleParticlePrefab != null)
                        {
                            var conzero = collisionInfo.contacts[0];
                            GameObject.Instantiate<GameObject>(m_JostleParticlePrefab, conzero.point, Quaternion.FromToRotation(Vector3.up, conzero.normal));
                        }
                    }

                }

                if (rb.angularVelocity.magnitude > 1.0f)
                {
                    if (Time.time - m_LastCollisionTime > 0.1f && (m_LastCollisionPos == Vector3.zero || (m_LastCollisionPos - transform.position).magnitude > 1.0f))
                    {
                        if (m_JostleParticlePrefab != null)
                        {
                            var conzero = collisionInfo.contacts[0];
                            GameObject.Instantiate<GameObject>(m_JostleParticlePrefab, conzero.point, Quaternion.FromToRotation(Vector3.up, conzero.normal));
                        }

                        if (!m_Invunlerable)
                        {
                            if (GetJostled(m_LossPerJostle)) return;
                        }
                    }
                    m_LastCollisionTime = Time.time;
                }
            }
            foreach (ContactPoint contact in collisionInfo.contacts)
            {
                //Debug.DrawRay(contact.point, contact.normal * 10, Color.white);
            }
        }

        public void OnDestroy()
        {
            TwoDee.ComponentList.OnEnd(this);
        }
        private void Start()
        {
        }

        float m_OriginalMass;
        float m_OriginalAmount;
        public Vector3 m_OriginalScale;
        public void Awake()
        {
            TwoDee.ComponentList.OnStart(this);
            var rb = gameObject.GetComponent<Rigidbody>();
            m_OriginalMass = rb.mass;
            m_OriginalAmount = m_AmountLeft;
            m_OriginalScale = transform.localScale;
        }

        public void SetOriginalAmount(float amount)
        {
            m_OriginalAmount = m_AmountLeft = amount;
        }

        void IStoredResource.OneHarvest(StoredResourceArgs args)
        {
            args.m_Count = Deduct(50, out args.m_Item);
        }

        bool IStoredResource.IsHarvestable
        {
            get
            {
                return m_Harvestable;
            }
        }

        public string m_Tags;
        string[] IStoredResource.Tags
        {
            get
            {
                return m_Tags.Split(';');
            }
        }

        bool GetJostled(float amount)
        {
            m_WeightScale -= amount;
            if (m_WeightScale < 0.0f)
            {
                Destroy(gameObject);
                return true;
            }

            UpdateWeightScale();
            return false;
        }

        void UpdateWeightScale()
        {
            float levelMultiplier = 1.0f;
            var proxied = GetComponent<Proxied>();
            if (proxied != null)
            {
                levelMultiplier += proxied.Level * 0.1f;
            }

            float finalGeoScale = Mathf.Clamp(m_WeightScale, 0.4f, 100.0f);

            float totalScale = finalGeoScale * levelMultiplier;

            m_AmountLeft = m_OriginalAmount * m_WeightScale;


            var rb = gameObject.GetComponent<Rigidbody>();
            rb.mass = m_OriginalMass * totalScale;
            transform.localScale = m_OriginalScale * Mathf.Min(Mathf.Sqrt(finalGeoScale), 2.0f); // radius falls off sqrt of mass
        }

        [Serializable]
        public class Proxy : TwoDee.ProxyDataComp<StoredResource>
        {
            public float m_AmountLeft;
            public float m_WeightScale;
            public float m_OriginalAmount;
            public Inventory m_Inventory;

            public void SetOriginalAmount(float amount)
            {
                m_AmountLeft = m_OriginalAmount = amount;
            }
            protected override void SaveLoad(bool save, StoredResource sr)
            {
                if (save)
                {
                    m_AmountLeft = sr.m_AmountLeft;
                    m_WeightScale = sr.m_WeightScale;
                    m_OriginalAmount = (sr.m_OriginalAmount == 0) ? sr.m_AmountLeft : sr.m_OriginalAmount;
                    m_Inventory = sr.Inventory;
                }
                else
                {
                    sr.m_AmountLeft = m_AmountLeft;
                    sr.m_WeightScale = m_WeightScale;
                    sr.m_OriginalAmount = m_OriginalAmount;
                    sr.UpdateWeightScale();

                    sr.Inventory = m_Inventory;
                }
            }
        }

        IProxyData TwoDee.IProxy.CreateData()
        {
            return new Proxy();
        }
    }
}