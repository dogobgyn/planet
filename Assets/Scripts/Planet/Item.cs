using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Planet
{
    public class ThrowTrajectoryData
    {
        public float m_Value;
        public float m_MaxDistance = 10.0f;
        public float m_Gravity = 1.0f;
    }

    public class UseContext
    {
        public UseContext()
        {
        }

        public bool m_Error;
        public string m_ShowStatusMessage;
        public ThrowTrajectoryData m_ShowTrajectory;

        public bool m_Done;
        public bool m_Canceled;
        public Material m_GhostMaterialGood;
        public Material m_GhostMaterialBad;
        public int m_ButtonPressed;
        public int m_ButtonHeld;
        public GameObject m_ClickedTarget;
        public InventoryEntry m_Entry;
        public Inventory m_Inventory;
        public Inventory[] m_Inventories;
        public int m_InventorySlot;
        public bool m_Secondary;
        public GameObject m_ItemUserGameObject;
        public Item m_ItemObject;
        public Vector3 m_TargetPos;
        public Vector3 m_OriginPos;
        public Vector3 Direction
        {
            get { return (m_TargetPos - m_OriginPos).normalized; }
        }
        public float DirectionDistance
        {
            get { return (m_TargetPos - m_OriginPos).magnitude; }
        }
        public float m_DeltaTime;

        public bool m_Success = true;
        public bool m_Destroy;

        public Item GetItemObject(bool secondary)
        {
            var entry = m_Inventory.GetEntryInSlot(m_InventorySlot);
            if (entry == null) return null;
            return entry.m_Item.GetItem(secondary);
        }

        public string GetItemName()
        {
            var entry = m_Inventory.GetEntryInSlot(m_InventorySlot);
            if (entry == null) return null;
            return entry.m_Name;
        }
    }

    public class Item : MonoBehaviour
    {
        public string m_UseSound = "swing";
        public float m_ThrowMin;
        public float m_ThrowMax;

        public float m_TimeCost;

        public Item m_SecondaryItem;
        public Item SecondaryItem
        {
            get
            {
                return m_SecondaryItem;
            }
        }

        public virtual bool RequiresStanding
        {
            get
            {
                return false;
            }
        }

        public Item GetItem(bool secondary)
        {
            if (secondary && SecondaryItem != null) return SecondaryItem;
            return this;
        }

        public virtual bool JumpBoost(UseContext args)
        {
            return false;
        }

        public void UseQuantity(UseContext args, int amount)
        {
            args.m_Entry.m_Count -= amount;
            if (args.m_Entry.m_Count == 0)
            {
                args.m_Inventory.SetSlot(args.m_InventorySlot, null);
            }
        }

        public bool UseProperty(string prop, UseContext args, float amount)
        {
            return args.m_Entry.UseProperty(prop, amount);
        }

        public bool UseEnergy(UseContext args, float amount)
        {
            if (args.m_Entry.DrainEnergy(amount, true)) return true;
            foreach(var inv in args.m_Inventories)
            {
                if (inv.DrainEnergy(amount, true, true))
                {
                    return true;
                }
            }

            return false;
        }

        public void UseDurability(UseContext args, float amount)
        {
            if (null == args.m_Entry.m_Properties || !args.m_Entry.m_Properties.ContainsKey("durability")) return;
            float oldAmount;
            if(float.TryParse(args.m_Entry.m_Properties["durability"], out oldAmount))
            {
                oldAmount -= amount;
                args.m_Entry.m_Properties["durability"] = "" + oldAmount;
                if (oldAmount <= 0.0f) args.m_Destroy = true;
            }
        }

        public Item()
        {
        }

        protected virtual void VirtualBeginUse(UseContext args)
        {
        }

        protected virtual void VirtualUsing(UseContext args)
        {
            if (m_UseTime > 0.2f)
            {
                args.m_Done = true;
            }
        }

        protected virtual void VirtualEndUse(UseContext args)
        {

        }

        protected void ChargeTimeCost()
        {
            WorldState.Instance.SpeedTimeMinutes(m_TimeCost);            
        }

        protected float m_UseTime;
        public void BeginUse(UseContext args)
        {
            m_UseTime = 0.0f;
            VirtualBeginUse(args);
        }

        public void Using(UseContext args)
        {
            m_UseTime += args.m_DeltaTime;
            VirtualUsing(args);
        }

        public void EndUse(UseContext args)
        {
            VirtualEndUse(args);
        }

        public virtual void Use(UseContext args)
        {
        }

        public virtual void UpdateSelected(UseContext args)
        {
        }

        public virtual void Selected(UseContext args)
        {
        }

        public virtual void Unselected(UseContext args)
        {
        }

        public float ShowTrajectory(Vector3 start)
        {
            return 5.0f;
        }
    }
}