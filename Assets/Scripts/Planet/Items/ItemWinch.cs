
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using UnityEngine;

namespace Planet
{
    public class ItemWinch : Item
    {
        public override void Use(UseContext args)
        {
            var playerPos = args.m_OriginPos;
            var closest = TwoDee.ComponentList.GetClosest<Rope>(playerPos);
            if (closest != null && (closest.transform.position - playerPos).magnitude < 2.0f)
            {
                if (closest.EnterWinching())
                {
                    UseQuantity(args, 1);
                }
            }

        }

        public override void UpdateSelected(UseContext args)
        {
        }

        public override void Unselected(UseContext args)
        {
        }
    }
}