
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using UnityEngine;

namespace Planet
{
    public class ItemRocketBoots : Item
    {
        public override bool JumpBoost(UseContext args)
        {
            if(UseEnergy(args, args.m_DeltaTime * 4.0f))
            {
                return true;
            }

            return false;
        }
    }
}