using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Xml.Serialization;
using System.Xml;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using UnityEngine.Networking;

namespace TwoDee
{
    public interface IProxyDataRequiresOthers
    {
        void GetRequires(List<string> otherGuids);
    }
    public interface IProxyDataReady
    {
        bool ReadyToLoad();
    }
    public interface IProxyData
    {
        void Load(GameObject go);
        void Unload(GameObject go);
    }

    [Serializable]
    public class ProxyDataComp<T> : IProxyData
    {
        protected virtual void SaveLoad(bool save, T comp)
        {
        }

        void SaveLoad(bool save, GameObject go)
        {
            var comp = go.GetComponent<T>();
            SaveLoad(save, comp);
        }

        void IProxyData.Load(GameObject go)
        {
            SaveLoad(false, go);
        }

        void IProxyData.Unload(GameObject go)
        {
            SaveLoad(true, go);
        }
    }

    public interface IProxy
    {
        IProxyData CreateData();
    }

    public class ProxyWorld : MonoBehaviour
    {
        // Each zone is this size squared
        public const float ZONE_SIZE_DIM = 20.0f;

        public GameObject[] m_Objects;

        [Serializable]
        public class Proxy
        {
            [NonSerialized]
            [IgnoreDataMember]
            ProxyWorld m_World;

            [XmlIgnore]
            public ProxyWorld World
            {
                get { return m_World; }
                set {
                    m_World = value;
                }
            }

            private string m_Guid;
            public string Guid
            {
                get { return m_Guid; }
            }

            private int m_Level;
            public int Level
            {
                get { return m_Level; }
                set { m_Level = value; }
            }

            private int m_RandSeed;
            public int RandSeed
            {
                get { return RandSeed; }
                set { m_RandSeed = value; }
            }

            public float m_BoundsDim;
            Bounds m_Bounds;
            public string m_Prefab;
            public Vector3 m_Scale;
            public Vector3 m_Position;
            public Quaternion m_Rotation;
            public List<IProxyData> m_Data = new List<IProxyData>();

            public T GetData<T>() where T:class
            {
                foreach(var data in m_Data)
                {
                    if (data is T)
                    {
                        return (T)data;
                    }
                }

                return null;
            }

            public Bounds Bounds
            {
                get { return m_Bounds; }
                set
                {
                    if (m_World != null) m_World.RemoveFromWorld(this);
                    m_Bounds = value;
                    if (m_World != null) m_World.AddToWorld(this);
                }
            }

            public void MoveTo(Vector3 pos, Quaternion rot)
            {
                m_Position = pos;
                m_Rotation = rot;
                Bounds = new Bounds(pos, new Vector3(m_BoundsDim, m_BoundsDim));
            }

            public void Init(float boundsDim, string prefab, Vector3 pos, Quaternion rot)
            {
                m_BoundsDim = boundsDim;
                m_Prefab = prefab;

                MoveTo(pos, rot);
            }

            public virtual void Unload(GameObject go)
            {
                // save relevant data for this gameobject before we purge it
                // @TEMP
                m_Bounds = new Bounds(go.transform.position, new Vector3(5.0f, 5.0f));
                m_Scale = go.transform.localScale;
                m_Position = go.transform.position;
                m_Rotation = go.transform.rotation;
                m_Guid = go.GetComponent<Proxied>().Guid;
                m_Level = go.GetComponent<Proxied>().Level;
                m_RandSeed = go.GetComponent<Proxied>().m_RandSeed;
                if (m_Guid == null || m_Guid.Length==0)
                {
                    m_Guid = ProxyWorld.CreateGuid();
                }

                // Generate proxy data for each component
                foreach(var comp in go.GetComponents<IProxy>())
                {
                    var data = comp.CreateData();
                    data.Unload(go);
                    m_Data.Add(data);
                }
            }

            public virtual bool ReadyToLoad()
            {
                // Check if ready to load each component
                foreach (var data in m_Data)
                {
                    var readyData = data as IProxyDataReady;
                    if (readyData != null && !readyData.ReadyToLoad()) return false;
                }

                return true;
            }

            public virtual GameObject Load()
            {
                var prefab = m_World.FindPrefabByName(m_Prefab);
                var go = GameObject.Instantiate<GameObject>(prefab, m_Position, m_Rotation);
                if (go == null || go.GetComponent<Proxied>() == null)
                {
                    Debug.Log("Whoops?");
                }
                go.transform.localScale = m_Scale;
                go.GetComponent<Proxied>().Prefab = prefab;
                go.GetComponent<Proxied>().Guid = Guid;
                go.GetComponent<Proxied>().m_Level = m_Level;
                go.GetComponent<Proxied>().m_RandSeed = m_RandSeed;

                // Load proxy data for each component
                foreach (var data in m_Data)
                {
                    data.Load(go);
                }
                if (go.GetComponent<NetworkIdentity>() != null)
                {
                    NetworkServer.Spawn(go);
                }

                return go;
            }
        }

        Dictionary<IntVector2, List<Proxy>> m_ZoneContents = new Dictionary<IntVector2, List<Proxy>>();

        List<Proxy> GetZoneContents(IntVector2 zone)
        {
            List<Proxy> result;
            if (!m_ZoneContents.TryGetValue(zone, out result))
            {
                result = new List<Proxy>();
                m_ZoneContents[zone] = result;
            }

            return result;
        }

        IntVector2 WorldSpaceToZone(Vector3 pos_ws)
        {
            return new IntVector2(Mathf.FloorToInt(pos_ws.x / ZONE_SIZE_DIM), Mathf.FloorToInt(pos_ws.y / ZONE_SIZE_DIM));
        }

        void AddRemoveFromWorldHelper(Proxy proxy, bool add)
        {
            var lbzone = WorldSpaceToZone(proxy.Bounds.min);
            var ubzone = WorldSpaceToZone(proxy.Bounds.max);
            IntVector2 zone = new IntVector2();
            for (int x = lbzone.X; x <= ubzone.X; x++)
            {
                for (int y = lbzone.Y; y <= ubzone.Y; y++)
                {
                    zone.X = x;
                    zone.Y = y;
                    var zc = GetZoneContents(zone);
                    if (add)
                    {
                        if (zc.Contains(proxy))
                        {
                                Debug.Log("ZONE ALREADY CONTAINS PROXY!");
                        }
                        zc.Add(proxy);
                    }
                    else
                    {
                        zc.Remove(proxy);
                    }
                }
            }
        }

        void AddToWorld(Proxy proxy)
        {
            if (proxy.World != null)
            {
                Debug.Log("ALREADY ADDED PROXY ADDED TO WORLD");
            }
            proxy.World = this;
            AddRemoveFromWorldHelper(proxy, true);
        }
        void RemoveFromWorld(Proxy proxy)
        {
            AddRemoveFromWorldHelper(proxy, false);
            proxy.World = null;
        }

        void AddRemoveVerify(Proxy proxy)
        {
            proxy.World = this;
            AddRemoveFromWorldHelper(proxy, true);
            AddRemoveFromWorldHelper(proxy, false);
            proxy.World = null;

            // @TEST Debug check to make sure there isn't some weird issue where the proxy still exists somewhere else
            foreach (var entry in m_ZoneContents)
            {
                foreach (var single in entry.Value)
                {
                    if (single.World == null)
                    {
                        Debug.Log("Severe problem\n");
                    }
                }
            }
        }

        Vector3 m_PlayerPosition = new Vector3();

        void UpdatePlayerPosition()
        {
            foreach (var player in TwoDee.ComponentList.GetCopiedListOfType<TwoDee.ThirdPersonUserControl>())
            {
                m_PlayerPosition = player.transform.position;
            }
        }

        void UpdateLoadProxies()
        {
            var playerZone = WorldSpaceToZone(m_PlayerPosition);
            IntVector2 zone = new IntVector2();
            for (int x = playerZone.X - 1; x <= playerZone.X + 1; x++)
            {
                for (int y = playerZone.Y - 1; y <= playerZone.Y + 1; y++)
                {
                    zone.X = x;
                    zone.Y = y;

                    // need to make a copy here since RemoveFromWorld will modify it
                    var zc = GetZoneContents(zone).ToArray();
                    foreach (var proxy in zc)
                    {
                        if (proxy.ReadyToLoad())
                        {
                            proxy.Load();
                            RemoveFromWorld(proxy);
                        }
                    }
                }
            }

        }

        void UpdateUnloadProxies(bool unloadAll)
        {
            var playerZone = WorldSpaceToZone(m_PlayerPosition);
            var foundProxied = TwoDee.ComponentList.GetCopiedListOfType<Proxied>();
            foreach (var proxied in foundProxied)
            {
                var proxiedZone = WorldSpaceToZone(proxied.gameObject.transform.position);
                int manhattanDistFromPlayer = (proxiedZone - playerZone).Manhattan;

                if (unloadAll || manhattanDistFromPlayer > 5)
                {
                    var pfab = proxied.Prefab;
                    if (pfab == null) continue;

                    // Some proxies may be either not ready to go if they depend on others for instance
                    var prefabName = proxied.Prefab.name.ToLower();
                    if (proxied.ReadyToCreateProxy(prefabName))
                    {
                        var proxy = proxied.CreateProxy(prefabName);
                        // It's possible the proxied object decided to not create a proxy if it thinks it needs to just be thrown away.
                        if (proxy != null)
                        {
                            proxy.Unload(proxied.gameObject);
                        }
                        DestroyImmediate(proxied.gameObject);
                        if (proxy != null)
                        {
                            AddToWorld(proxy);
                        }
                    }
                }
            }
        }

        void Update()
        {
            UpdatePlayerPosition();

            // Add everything from nearby zones where the player currently is.
            UpdateLoadProxies();

            // Unload everything too far from the player.
            UpdateUnloadProxies(false);
        }

        GameObject FindPrefabByName(string name)
        {
            foreach(var entry in m_Objects)
            {
                if (entry == null) continue;
                if (entry.name.ToLower() == name.ToLower())
                {
                    return entry;
                }
            }

            Debug.LogError("Could not find prefab for proxy: " + name);

            return null;
        }

        public static GameObject PostInstantiate(GameObject ob, GameObject prefab)
        {
            var proxied = ob.GetComponent<Proxied>();
            proxied.Prefab = prefab;

            return ob;
        }

        public Proxy AddProxyByName(string name, Vector3 position, Quaternion rotation)
        {
            foreach (var ob in m_Objects)
            {
                if (ob == null) continue;
                if (ob.name.ToLower() == name.ToLower())
                {
                    var proxied = ob.GetComponent<Proxied>();
                    var prefab = FindPrefabByName(name);
                    var proxy = proxied.CreateProxy(name);
                    proxy.Unload(prefab);
                    proxy.MoveTo(position, rotation);
                    AddToWorld(proxy);
                    return proxy;
                }
            }

            UnityEngine.Debug.LogError("AddProxyByName: BAD name " + name);

            return null;
        }

        [Serializable]
        public class SaveData
        {
            public VoxelGenerator.VoxelGeneratorProxy Voxel { get; set; }
            public object Player { get; set; }
            public List<Proxy> Proxies { get; set; }
        }

        XmlSerializer m_CachedXmlSerializer = null;
        void CreateXmlSerializer()
        {
            if (m_CachedXmlSerializer == null)
            {
                // Figure out what types we need
                Dictionary<Type, bool> types = new Dictionary<Type, bool>();

                foreach (var ob in m_Objects)
                {
                    if (ob == null) continue;
                    // We need the proxy type itself (if it's inherited)
                    var exampleProxy = ob.GetComponent<Proxied>().CreateProxy(ob.name.ToLower());
                    bool value;
                    if (!types.TryGetValue(exampleProxy.GetType(), out value))
                    {
                        types[exampleProxy.GetType()] = true;
                    }

                    // We need all the subdata types from the example
                    foreach(var data in exampleProxy.m_Data)
                    {
                        if (!types.TryGetValue(data.GetType(), out value))
                        {
                            types[data.GetType()] = true;
                        }
                    }
                }

                var typesList = new List<Type>();
                foreach (var entry in types)
                {
                    typesList.Add(entry.Key);
                }

                var serializer = new XmlSerializer(typeof(SaveData), typesList.ToArray());

                m_CachedXmlSerializer = serializer;
            }
        }

        string XmlSerialize(SaveData proxies)
        {
            CreateXmlSerializer();

            var str = "";
            using (var textWriter = new StringWriterWithEncoding(new StringBuilder(), new System.Text.ASCIIEncoding()))
            {
                using (XmlTextWriter xmlWriter = new XmlTextWriter(textWriter))
                {
                    xmlWriter.Formatting = Formatting.Indented;
                    xmlWriter.Indentation = 4;

                    m_CachedXmlSerializer.Serialize(xmlWriter, proxies);
                }
                str = textWriter.ToString();
            }

            return str;
        }

        SaveData XmlDeserialize(string str)
        {
            CreateXmlSerializer();

            using (StringReader textReader = new StringReader(str))
            {
                using (XmlTextReader xmlReader = new XmlTextReader(textReader))
                {
                    SaveData result = (SaveData)m_CachedXmlSerializer.Deserialize(xmlReader);
                    return result;
                }
            }
        }

        public void TestLoadGame()
        {
            var formatter = SaveLoad.CreateBinaryFormatterWithSurrogates();

            using (Stream filestream = File.Open("testsave.bin", FileMode.Open))
            {
                var data = (SaveData)formatter.Deserialize(filestream);
                SetupLoadGame(data);
            }
        }

        public void TestSaveGame()
        {
            var data = SetupSaveGame();
            var formatter = SaveLoad.CreateBinaryFormatterWithSurrogates();
            using (Stream filestream = File.Open("testsave.bin", FileMode.Create))
            {
                //serialize directly into that stream.
                formatter.Serialize(filestream, data);
            }

        }

        public void TestLoadGameXml()
        {
            string str = "";
            var formatter = SaveLoad.CreateBinaryFormatterWithSurrogates();
            using (Stream filestream = File.Open("testsave.txt", FileMode.Open))
            {
                //serialize directly into that stream.
                str = (string)formatter.Deserialize(filestream);
            }

            SaveData data = XmlDeserialize(str);

            SetupLoadGame(data);
        }

        void SetupLoadGame(SaveData data)
        {
            // Blow up all proxies since they will be in the save game and will get loaded.
            foreach (var proxied in TwoDee.ComponentList.GetCopiedListOfType<Proxied>())
            {
                DestroyImmediate(proxied.gameObject);
            }

            foreach (var vg in TwoDee.ComponentList.GetCopiedListOfType<TwoDee.VoxelGenerator>())
            {
                data.Voxel.Load(vg);
            }
            foreach (var player in TwoDee.ComponentList.GetCopiedListOfType<TwoDee.ThirdPersonUserControl>())
            {
                player.LoadProxy(data.Player);
            }

            m_ZoneContents = new Dictionary<IntVector2, List<Proxy>>();

            foreach (var entry in data.Proxies)
            {
                entry.World = null;
                AddToWorld(entry);
            }
        }

        SaveData SetupSaveGame()
        {
            UpdateUnloadProxies(true);
            var allProxies = GetProxies();

            for (int i = 0; i < allProxies.Count; i++)
            {
                for (int j = i + 1; j < allProxies.Count; j++)
                {
                    if (allProxies[i] == allProxies[j])
                    {
                        Debug.Log("Duplicate proxy found whoops");
                    }
                }
            }

            var voxelProxy = new VoxelGenerator.VoxelGeneratorProxy();
            foreach (var vg in TwoDee.ComponentList.GetCopiedListOfType<TwoDee.VoxelGenerator>())
            {
                voxelProxy.Save(vg);
            }
            object playerProxy = null;
            foreach (var player in TwoDee.ComponentList.GetCopiedListOfType<TwoDee.ThirdPersonUserControl>())
            {
                playerProxy = player.SaveProxy();
            }
            SaveData data = new SaveData() { Proxies = allProxies, Voxel = voxelProxy, Player = playerProxy };

            return data;
        }

        public void TestSaveGameXml()
        {
            SaveData data = SetupSaveGame();

            var str = XmlSerialize(data);
            Debug.Log(str.Substring(0, 1000));
            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            using (Stream filestream = File.Open("testsave.txt", FileMode.Create))
            {
                //serialize directly into that stream.
                formatter.Serialize(filestream, str);
            }
        }

        void TestXmlSerializer()
        {
            SaveData exampleProxies = new SaveData() { Proxies = new List<Proxy>() };
            foreach (var ob in m_Objects)
            {
                if (ob == null) continue;
                var exampleProxy = ob.GetComponent<Proxied>().CreateProxy(ob.name.ToLower());
                exampleProxies.Proxies.Add(exampleProxy);
            }
            var str = XmlSerialize(exampleProxies);
            Debug.Log(str);
            var loadedData = XmlDeserialize(str);
            str = XmlSerialize(loadedData);
            Debug.Log(str);
            loadedData = XmlDeserialize(str);
            str = XmlSerialize(loadedData);
            Debug.Log(str);
        }

        void Start()
        {
            //@TEST serializer
            //TestXmlSerializer();
        }

        private void OnDestroy()
        {
            ComponentList.OnEnd(this);
        }

        private void Awake()
        {
            ComponentList.OnStart(this);
        }

        public static string CreateGuid()
        {
            return Guid.NewGuid().ToString();
        }

        public GameObject FindGameObjectWithGuid(string guid)
        {
            var proxieds = ComponentList.GetCopiedListOfType<Proxied>();
            foreach (var proxycomp in proxieds)
            {
                if (proxycomp.Guid == guid)
                {
                    return proxycomp.gameObject;
                }
            }

            return null;
        }

        public Proxy FindProxyWithGuid(string guid)
        {
            foreach (var zone in m_ZoneContents)
            {
                foreach (var entry in zone.Value)
                {
                    if (entry.Guid == guid)
                    {
                        return entry;
                    }
                }
            }

            return null;
        }

        public class GameObjectOrProxy
        {
            GameObject m_Ob;
            Proxy m_Proxy;
            string m_Guid;

            public GameObjectOrProxy(string guid)
            {
                m_Guid = guid;
                LoadFromGuid();
            }
            public GameObjectOrProxy(GameObject ob, Proxy proxy)
            {
                m_Ob = ob;
                if (ob != null)
                {
                    m_Guid = ob.GetComponent<Proxied>().Guid;
                }
                m_Proxy = proxy;

                if (proxy != null)
                {
                    if ((ob == null && proxy == null) || (ob != null && proxy != null))
                    {
                        Debug.LogError("GameObjectOrProxy created with both or none");
                    }

                    m_Guid = proxy.Guid;
                }                
            }

            public GameObject GameObject
            {
                get
                {
                    CheckValidity();
                    return m_Ob;
                }
            }

            public Proxy Proxy
            {
                get
                {
                    CheckValidity();
                    return m_Proxy;
                }
            }

            void LoadFromGuid()
            {
                string guid = m_Guid;
                var pw = ComponentList.GetFirst<ProxyWorld>();
                m_Ob = pw.FindGameObjectWithGuid(guid);
                m_Proxy = pw.FindProxyWithGuid(guid);
            }

            void CheckValidity()
            {
                bool badob = (m_Ob == null || !m_Ob);
                bool badproxy = (m_Proxy == null || m_Proxy.World == null);

                int badCount = 0;
                if (badob) { m_Ob = null; badCount++; }
                if (badproxy) { m_Proxy = null; badCount++; }
                if (badCount == 2)
                {
                    LoadFromGuid();
                }
            }

            public bool Valid
            {
                get
                {
                    CheckValidity();

                    bool badob = (m_Ob == null || !m_Ob);
                    bool badproxy = (m_Proxy == null || m_Proxy.World == null);

                    return !badob || !badproxy;
                }
            }

            public Vector3 Position
            {
                get
                {
                    CheckValidity();
                    if (m_Ob != null)
                    {
                        return m_Ob.transform.position;
                    }
                    else if (m_Proxy != null)
                    {
                        return m_Proxy.m_Position;
                    }

                    return Vector3.zero;
                }
            }

        }
           
        public List<Proxy> GetProxies(Func<Proxy, bool> filter=null)
        {
            var proxies = new List<Proxy>();
            foreach (var zone in m_ZoneContents)
            {
                foreach (var entry in zone.Value)
                {
                    bool add = true;
                    if (filter != null)
                    {
                        add = filter(entry);
                    }
                    if (add && !proxies.Contains(entry))
                    {
                        proxies.Add(entry);
                    }
                }
            }

            return proxies;
        }

        public List<TwoDee.ProxyWorld.GameObjectOrProxy> GetGameObjectsOrProxies(string prefabName)
        {
            var gop = new List<TwoDee.ProxyWorld.GameObjectOrProxy>();

            var proxieds = ComponentList.GetCopiedListOfType<Proxied>();
            foreach(var proxycomp in proxieds)
            {
                if (proxycomp.Prefab == null)
                {
                    UnityEngine.Debug.LogError("Missing prefab in proxy world: " + proxycomp.gameObject.name);
                    continue;
                }
                if (proxycomp.Prefab.name.ToLower() == prefabName)
                {
                    gop.Add(new GameObjectOrProxy(proxycomp.gameObject, null));
                }
            }

            var proxies = GetProxies(proxy=>(proxy.m_Prefab == prefabName));
            foreach (var proxy in proxies)
            {
                if (proxy.m_Prefab == prefabName)
                {
                    gop.Add(new GameObjectOrProxy(null, proxy));
                }
            }

            return gop;
        }
    }
}