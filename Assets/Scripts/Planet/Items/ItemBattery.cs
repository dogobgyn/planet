
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using UnityEngine;

namespace Planet
{
    public class ItemBattery : Item
    {
        public override void Use(UseContext args)
        {
            // Transfer 10 charge into thing next to us if we have it
            int mySlot = args.m_InventorySlot;
            var myEntry = args.m_Inventory.GetSlot(mySlot);
            var nextEntry = args.m_Inventory.GetSlot(mySlot + 1);
            if (nextEntry!=null)
            {
                float myEnergy = myEntry.GetProperty("energy");
                float amountCharged = nextEntry.ChargeEnergy(Mathf.Min(myEnergy, 10.0f));
                if (amountCharged > 0.0f)
                {
                    myEntry.ChargeEnergy(-amountCharged);
                }
            }
        }
    }
}