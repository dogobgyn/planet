
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using UnityEngine;

namespace Planet
{
    public class ItemWormHole : ItemBlueprint
    {
        public GameObject m_TeleporterEndPrefab;
        public override void OnPlaced(UseContext args, GameObject newObj)
        {
            if (m_TeleporterEndPrefab == null) return;

            // Find base, place somewhere near
            var homeBase = TwoDee.ComponentList.GetFirst<HomeBase>();
            /*
            var endPos = (homeBase ? homeBase.transform.position : Vector3.zero) + Vector3.left * 10.0f;
            var otherEnd = GameObject.Instantiate<GameObject>(m_TeleporterEndPrefab, endPos, Quaternion.identity);
            */

            var newObjTeleporter = newObj.GetComponent<Teleporter>();
            newObjTeleporter.ConnectToOther(homeBase.gameObject);
        }
    }
}