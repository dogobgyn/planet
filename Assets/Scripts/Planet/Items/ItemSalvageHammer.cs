
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using UnityEngine;

namespace Planet
{
    // Salvage hammer
    public class ItemSalvageHammer : Item
    {
        public override void Use(UseContext args)
        {
            float power = args.m_Entry.DbEntry.GetCombinedPropFloat("power", args.m_Entry.m_Properties);
            if (args.m_ClickedTarget)
            {
                // Harvest target
                UseDurability(args, 5.0f);
            }
        }
    }
}