
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using UnityEngine;
using TwoDee;

namespace Planet
{
    public class Rope : MonoBehaviour, IProxy
    {
        public enum State
        {
            Initializing,
            Unraveling,
            Idle,
            Winching
        };
        public State m_State = State.Initializing;

        public GameObject m_LinkPrefab;
        public GameObject m_RopeHandlePrefab;
        public bool m_Grappling;

        void DisconnectAndDestroy()
        {
            foreach(var link in m_Links)
            {
                link.GetComponent<RopeLink>().ConnectJoint(null);
            }
            Destroy(gameObject);
        }

        string m_RefundItem;
        public void CheckRefund(RopeLink link)
        {
            if (m_RefundItem.IsEmpty()) return;

            foreach(var player in ComponentList.GetCopiedListOfType<ThirdPersonUserControl>())
            {
                if( (player.transform.position - link.transform.position).magnitude < 1.0f)
                {
                    int amountCouldntGive = player.Inventory.AddInventory(new InventoryEntry(m_RefundItem, 1), true);
                    if (amountCouldntGive == 0)
                    {
                        DisconnectAndDestroy();
                        return;
                    }
                }
            }

            return;
        }

        int m_MaxLinks;
        Vector3 m_Direction;
        float m_Speed;

        int MaxLinks
        {
            get
            {
                return m_MaxLinks;
            }
        }

        float m_UnravelTimeout = 0.0f;

        public float FindClosestRopePosition(Vector3 pos_ws)
        {
            float closestDistance = 99999.0f;
            float bestPosition = -1.0f;

            for (int i = 0; i < m_Links.Count; i++)
            {
                var link = m_Links[i];
                Vector3 linkStart = link.transform.TransformPoint(Vector3.up);
                Vector3 linkEnd = link.transform.TransformPoint(Vector3.down);

                Vector3 closestPointToPos = GameObjectExt.NearestPointOnFiniteLine(linkStart, linkEnd, pos_ws);
                float dist = (closestPointToPos - pos_ws).magnitude;
                if (dist < closestDistance)
                {
                    closestDistance = dist;

                    // Find placement along link
                    float t = Vector3.Dot(link.transform.TransformDirection(Vector3.down), closestPointToPos - linkStart) / (linkEnd - linkStart).magnitude;
                    bestPosition = i + Mathf.Clamp(t, 0.0f, 0.99f);
                }
            }

            return bestPosition;
        }

        public float RopePositionFromLink(RopeLink rl)
        {
            for (int i = 0; i < m_Links.Count; i++)
            {
                if (m_Links[i] == rl.gameObject) return i;
            }
            return 0.0f;
        }

        public Vector3 RopePositionDirectionAt(float ropePosition)
        {
            ropePosition = ClampRopePosition(ropePosition);
            int rp0 = Mathf.FloorToInt(ropePosition);

            return m_Links[rp0].transform.up;
        }
        public float ClampRopePosition(float ropePosition)
        {
            return Mathf.Clamp(ropePosition, 0.0f, m_Links.Count - 0.1f);
        }

        public GameObject GetLinkFromRopePosition(float ropePosition)
        {
            ropePosition = ClampRopePosition(ropePosition);
            int rp0 = Mathf.FloorToInt(ropePosition);
            if (rp0 < 0 || rp0 >= m_Links.Count) return null;
            var link = m_Links[rp0];
            return link;
        }

        public Vector3 RopePositionToWorld(float ropePosition)
        {
            var link = GetLinkFromRopePosition(ropePosition);
            if (link == null)
            {
                return new Vector3();
            }

            Vector3 linkStart = link.transform.TransformPoint(Vector3.up);
            Vector3 linkEnd = link.transform.TransformPoint(Vector3.down);

            return Vector3.Lerp(linkStart, linkEnd, ropePosition % 1.0f);
        }

        List<GameObject> m_Links = new List<GameObject>();
        public GameObject CreateLink(Vector3 initPos, Quaternion rot, bool withVelocity)
        {
            var link = GameObject.Instantiate<GameObject>(m_LinkPrefab, initPos, rot, transform);
            if (withVelocity)
                link.GetComponent<Rigidbody>().velocity = m_Direction * m_Speed;
            var linkComp = link.AddComponent<RopeLink>();
            linkComp.m_Rope = this;
            if (m_Links.Count == 0)
            {
                linkComp.m_LocalSpaceTip = new Vector3(0.0f, -LinkAttachmentLocalY);
            }
            else
            {
                linkComp.m_LocalSpaceTip = new Vector3(0.0f, LinkAttachmentLocalY);
            }
            m_Links.Add(link);
            return link;
        }
        public GameObject CreateLink(Vector3 initPos, Vector3 direction, bool withVelocity)
        {
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, direction);
            return CreateLink(initPos, rot, withVelocity);
        }
        public void StartUnravel(bool grappling, Vector3 direction, float speed, int maxLinks, string refundItem)
        {
            if (m_State != State.Initializing) return;

            m_State = State.Unraveling;
            m_Grappling = grappling;
            direction.Normalize();
            m_Speed = speed;
            m_MaxLinks = maxLinks;
            m_RefundItem = refundItem;
            m_Direction = direction;
            // Arc first link?
            Vector3 dir = m_Direction + Vector3.up * -0.0f;
            var firstLink = CreateLink(transform.position, dir.normalized, true);
            if (m_Grappling)
            {
                firstLink.AddComponent<GrappleLink>();
            }
        }

        public bool m_CanTractorFirstLink;
        public bool m_CanTractorLastLink;
        public bool CanTractor(RopeLink link)
        {
            if (m_State != State.Idle) return false;

            var go = link.gameObject;
            if (null == m_Links || m_Links.Count < 5) return false;

            if (go == m_Links[0])
            {
                return m_CanTractorFirstLink;
            }
            if (go == m_Links[m_Links.Count - 1])
            {
                return m_CanTractorLastLink;
            }

            return false;
        }

        public void OnConnectJoint(RopeLink link, Joint joint, GameObject newConnection, GameObject oldConnection)
        {
            var myPC = GetComponent<PowerConduit>();
            if (myPC != null)
            {
                if (oldConnection != null)
                {
                    var oldPC = oldConnection.GetComponent<PowerConduit>();
                    myPC.Disconnect(oldPC);
                }
                if (newConnection != null)
                {
                    var yourPC = newConnection.GetComponentInSelfOrParents<PowerConduit>();
                    if (yourPC != null)
                    {
                        myPC.Connect(yourPC);
                    }
                }
            }
        }

        public bool HasEnergy
        {
            get
            {
                var myPC = GetComponent<PowerConduit>();
                if (myPC != null)
                {
                    return myPC.HasPower;
                }
                return false;
                /*
                    if (m_Links == null || m_Links.Count == 0) return false;

                // Check connections from first and last links.
                var linksToCheck = new RopeLink[] { m_Links[0].GetComponent<RopeLink>(), m_Links[m_Links.Count - 1].GetComponent<RopeLink>() };
                foreach(var link in linksToCheck)
                {
                    var connectedGo = link.JointConnection;
                    if (connectedGo != null && connectedGo.GetComponentInSelfOrParents<HomeBase>() != null)
                    {
                        return true;
                    }
                }

                return false;
                */
            }
        }

        private void OnDestroy()
        {
            TwoDee.ComponentList.OnEnd(this);
        }

        private void Start()
        {
            TwoDee.ComponentList.OnStart(this);
//            StartUnravel((new Vector3(1.0f, 2.0f)).normalized, 15.0f, 20);
//            StartUnravel((new Vector3(1.0f, 1.0f)).normalized, 3.0f, 20);
        }

        public static HingeJoint StandardInitHinge(GameObject gameObject)
        {
            var hj = gameObject.AddComponent<HingeJoint>();

            hj.axis = Vector3.forward;

            // This tends to be a bit more stable than the standard
            hj.useSpring = true;
            var oldsp = hj.spring;
            oldsp.spring = 5.0f;
            oldsp.damper = 5.0f;
            hj.spring = oldsp;

            return hj;
        }

        float LinkAttachmentLocalY
        {
            get { return 1.0f; }
        }

        void SetEmissiveColor(float emission)
        {
            foreach (var link in m_Links)
            {
                Renderer renderer = link.GetComponent<Renderer>();
                Material mat = renderer.material;

                Color baseColor = Color.yellow; //Replace this with whatever you want for your base color at emission level '1'

                Color finalColor = baseColor * Mathf.LinearToGammaSpace(emission);

                mat.SetColor("_EmissionColor", finalColor);
            }
        }

        public bool EnterWinching()
        {
            if (m_State == State.Idle)
            {
                m_State = State.Winching;
                return true;
            }

            return false;
        }

        float m_ShowRopePosDebug = 0.0f;
        float m_WinchScale = 1.0f;
        Vector3 m_OriginalLinkScale = Vector3.zero;
        bool m_LastHadEnergy = false;
        public void Update()
        {
            // @Test show energy
            var newHasEnergy = HasEnergy;
            if (newHasEnergy)
            {
                SetEmissiveColor(Mathf.PingPong(Time.time, 1.0f));
            }
            else
            {
                SetEmissiveColor(0.0f);
            }
            if (m_LastHadEnergy != newHasEnergy)
            {
                TwoDee.EasySound.Play(newHasEnergy ? "poweron" : "poweroff", transform.position);
            }
            m_LastHadEnergy = newHasEnergy;

            if (m_State == State.Winching && m_Links.Count > 0)
            {
                if (m_WinchScale < 0.075f)
                {
                    // Too short, we're done
                    Destroy(gameObject);
                    return;
                }
                else
                {
                    if (m_OriginalLinkScale == Vector3.zero)
                    {
                        m_OriginalLinkScale = m_Links[0].transform.localScale;
                    }
                    m_WinchScale -= 0.06f * Time.deltaTime;
                    foreach (var link in m_Links)
                    {
                        link.transform.localScale = m_OriginalLinkScale * m_WinchScale;
                        var hj = link.GetComponent<HingeJoint>();
                        if (hj != null)
                        {
                            hj.connectedAnchor = new Vector3(0.0f, m_WinchScale);
                            hj.anchor = new Vector3(0.0f, -m_WinchScale);
                        }
                    }
                }
            }
            //@TEST show rope position
            /*
            if(m_Links.Count > 0)
            {
              m_ShowRopePosDebug += 0.01f;
              if (m_ShowRopePosDebug > m_Links.Count) m_ShowRopePosDebug = 0.0f;
              //UnityEngine.Debug.DrawLine(RopePositionToWorld(m_ShowRopePosDebug), RopePositionToWorld(m_ShowRopePosDebug + 0.1f), Color.red, 0.1f, false);
              UnityEngine.Debug.DrawLine(RopePositionToWorld(m_ShowRopePosDebug), 0.1f*Vector3.forward + RopePositionToWorld(FindClosestRopePosition(RopePositionToWorld(m_ShowRopePosDebug))), Color.red, 0.1f, false);
            }
            */

            //@TEST run slowly to see it working
            //Time.timeScale = 0.1f;

            if (m_State == State.Unraveling)
            {
                if (m_Links.Count >= MaxLinks)
                {
                    // Successfully unraveled all
                    m_State = State.Idle;
                    return;
                }

                var lastLink = m_Links[m_Links.Count - 1];
                var linkScale = lastLink.transform.localScale.y;
                var attachmentPoint = lastLink.transform.TransformPoint(new Vector3(0.0f, -1.0f * LinkAttachmentLocalY));
                var newPos = transform.position + m_Direction * LinkAttachmentLocalY * lastLink.transform.localScale.y; //

                m_UnravelTimeout += Time.deltaTime;

                if (m_UnravelTimeout > 0.4f)
                {
                    // Uh oh... the rope got stuck.  @TODO refund it, destroy if it's too small
                    if (m_Links.Count < 10)
                    {
                        Destroy(gameObject);
                    }
                    else
                    {
                        FinishUnravel();
                    }
                    return;
                }


                if ((attachmentPoint - transform.position).magnitude > 2.2f * LinkAttachmentLocalY * lastLink.transform.localScale.y)
                {
                    m_UnravelTimeout = 0.0f;
                    var newDirection = attachmentPoint - transform.position;
                    UnityEngine.Debug.DrawLine(transform.position, attachmentPoint, Color.red, 10.0f);

                    newDirection.Normalize();
                    if (m_Links.Count == MaxLinks)
                    {
                        //newPos = transform.position + m_Direction * halfLinkDistance;                    
                    }
                    var newLink = CreateLink(newPos, newDirection, true);

                    AttachNewLink(newLink, lastLink);

                    if (m_Links.Count == MaxLinks)
                    {
                        FinishUnravel();
                    }
                }

            }

        }

        void PlaceHandles()
        {
            foreach(var link in m_Links)
            {
                if(CanTractor(link.GetComponent<RopeLink>()))
                {
                    if(m_RopeHandlePrefab != null)
                    {
                        var newHandle = GameObject.Instantiate(m_RopeHandlePrefab, link.transform);
                    }
                }
            }
        }

        void FinishUnravel()
        {
            if (!m_Grappling)
            {
                var newLink = m_Links[m_Links.Count - 1];
                var hj2 = StandardInitHinge(newLink);
                hj2.anchor = new Vector3(0.0f, -LinkAttachmentLocalY);
                newLink.GetComponent<RopeLink>().ConnectJoint(hj2);
            }

            m_State = State.Idle;
            PlaceHandles();
        }
    
        HingeJoint AttachNewLink(GameObject newLink, GameObject lastLink)
        {
            if (lastLink == null) return null;
            var hj = StandardInitHinge(lastLink);

            hj.autoConfigureConnectedAnchor = false;
            hj.connectedBody = newLink.GetComponent<Rigidbody>();
            hj.connectedAnchor = new Vector3(0.0f, LinkAttachmentLocalY);
            hj.anchor = new Vector3(0.0f, -LinkAttachmentLocalY);

            return hj;
        }

        IProxyData IProxy.CreateData()
        {
            return new Proxy();
        }
        
        [Serializable]
        public class LinkProxy
        {
            public Vector3 m_Position;
            public Quaternion m_Rotation;
            public string m_ConnectedObject;
            public Vector3 m_ConnectedPosition;

            public void Save(GameObject go)
            {
                var link = go.GetComponent<RopeLink>();

                m_Position = go.transform.position;
                m_Rotation = go.transform.rotation;
                m_ConnectedPosition = link.JointPosition;
                m_ConnectedObject = link.JointGuid;
            }
        }

        [Serializable]
        public class Proxy : TwoDee.ProxyDataComp<Rope>, IProxyDataReady
        {
            string m_RefundItem;
            LinkProxy[] m_Links;

            protected override void SaveLoad(bool save, Rope rope)
            {
                if (save)
                {
                    m_RefundItem = rope.m_RefundItem;
                    m_Links = new LinkProxy[rope.m_Links.Count];
                    for (int i = 0; i < rope.m_Links.Count; i++)
                    {
                        m_Links[i] = new LinkProxy();
                        m_Links[i].Save(rope.m_Links[i]);
                    }
                }
                else
                {
                    rope.m_RefundItem = m_RefundItem;
                    //Setup links
                    GameObject lastLink = null;
                    foreach (var link in m_Links)
                    {
                        var go = rope.CreateLink(link.m_Position, link.m_Rotation, false);
                        rope.AttachNewLink(go, lastLink);
                        lastLink = go;

                        if (link.m_ConnectedPosition != Vector3.zero)
                        {
                            var hj2 = StandardInitHinge(go);
                            if (!link.m_ConnectedObject.IsEmpty())
                            {
                                var gop = new TwoDee.ProxyWorld.GameObjectOrProxy(link.m_ConnectedObject);
                                if (gop.Valid && gop.GameObject != null)
                                {
                                    hj2.connectedBody = gop.GameObject.GetComponent<Rigidbody>();
                                }
                                else
                                {
                                    UnityEngine.Debug.LogError("A rope was loaded in before an object it was connected to was loaded in.");
                                }
                            }

                            hj2.anchor = go.transform.InverseTransformPoint(link.m_ConnectedPosition);
                            // The extra parameter here is since the connection is already set up we don't want to do it again.
                            go.GetComponent<RopeLink>().ConnectJoint(hj2, false);
                        }
                    }

                    //Setup state
                    rope.m_State = State.Idle;
                    rope.PlaceHandles();

                }
            }

            bool IProxyDataReady.ReadyToLoad()
            {
                // We need whatever we are connected to be loaded
                foreach (var link in m_Links)
                {
                    if (link.m_ConnectedObject == null || link.m_ConnectedObject.Length == 0) continue;
                    var gop = new TwoDee.ProxyWorld.GameObjectOrProxy(link.m_ConnectedObject);
                    if(gop.Valid && gop.GameObject == null)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}