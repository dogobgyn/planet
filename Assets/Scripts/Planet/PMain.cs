using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using UnityEngine.Networking;
using TwoDee;

namespace Planet
{
    public class PMain : TwoDee.Main
    {
        public GameObject m_TestStuff;
        public void Start()
        {
        }

        bool m_StartMoved = false;
        public void Update()
        {
            if(!m_StartMoved)
            {
                float yStart = 0.0f;
                foreach (var vg in ComponentList.GetCopiedListOfType<PVoxelGenerator>())
                {
                    if (vg.DoneGenerating)
                    {
                        m_StartMoved = true;
                        yStart = vg.StartingPoint.y;

                        foreach (var obj in m_TestStuff.transform.GetChildren())
                        {
                            obj.transform.parent = null;
                            var pos = obj.transform.position;
                            obj.transform.position = pos + new Vector3(0.0f, yStart);

                            // Spawn moved, so move player too
                            if (obj.GetComponent<NetworkStartPosition>() != null)
                            {
                                foreach (var player in TwoDee.ComponentList.GetCopiedListOfType<TwoDee.ThirdPersonUserControl>())
                                {
                                    player.transform.position = obj.transform.position;
                                }

                            }
                        }

                        var nm = GetComponent<NetworkManager>();
                        nm.StartHost();
                    }
                }


            }
        }
    }
}