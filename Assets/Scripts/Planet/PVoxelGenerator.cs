
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
    public class PVoxelGenerator : TwoDee.VoxelGenerator
    {
        Csv m_AlienLootTable;
        Csv AlienLootTable
        {
            get
            {
                if(m_AlienLootTable == null)
                {
                    m_AlienLootTable = new Csv("alienloottable");
                }

                return m_AlienLootTable;
            }
        }

        Inventory GenerateAlienLoot(TwoDee.RandomGeneratorUnity rng, Inventory inv, int baseLevel)
        {
            int level = baseLevel;
            if (level < 1) level = 1;
            if (level > 1) level = 1;

            int entriesToAdd = 3;
            float totalEntries = 0.0f;
            foreach(var entry in AlienLootTable.m_Entries)
            {
                if(entry.GetColumnInt("level") == level)
                {
                    totalEntries += entry.GetColumnFloat("chance");
                }
            }

            while(entriesToAdd > 0)
            {
                float currentRangeStart = 0.0f;
                float chance = rng.Range(0.0f, totalEntries);
                foreach (var entry in AlienLootTable.m_Entries)
                {
                    if (entry.GetColumnInt("level") == level)
                    {
                        float currentRangeEnd = currentRangeStart + entry.GetColumnFloat("chance");
                        if (chance >= currentRangeStart && chance < currentRangeEnd)
                        {
                            int numberToAdd = rng.Range(entry.GetColumnInt("low"), entry.GetColumnInt("high")+1);
                            inv.AddInventory(new InventoryEntry(entry.GetColumn("name"), numberToAdd), true);

                            break;
                        }
                        currentRangeStart = currentRangeEnd;
                    }
                }

                entriesToAdd--;
            }

            return inv;
        }

        protected override float SunlightIntensity
        {
            get {
                var worldState = WorldState.Instance;
                if (worldState == null) return 0.0f;
                return WorldState.Instance.SunlightIntensity;
            }
        }

        class GridValues<T>
        {
            int m_BlockSize = 500;
            T[,] values = new T[1000, 1000];

            public T GetValue(int x, int y)
            {
                return values[x, y];
                T result;
                if (m_Values.TryGetValue(new IntVector2(x, y), out result))
                {
                    return result;
                }

                return default(T);
            }

            public void SetValue(int x, int y, T value)
            {
                if (x < 0 || y < 0) return;
                if (x >= 1000 || y >= 1000) return;
                values[x, y] = value;
                m_Values[new IntVector2(x, y)] = value;
            }

            public void PopulateBoxDim(int x0, int y0, int rx, int ry, T value)
            {
                for (int x = x0 - rx; x <= x0 + rx; x++)
                {
                    for (int y = y0 - ry; y <= y0 + ry; y++)
                    {
                        SetValue(x, y, value);
                    }
                }
            }

            Dictionary<IntVector2, T> m_Values = new Dictionary<IntVector2, T>();
        }

        EarthGenerateRound m_Generator;
        public bool IsStartPoint(Vector3 pt_ws)
        {
            var pt_gs = WorldSpaceToGrid(pt_ws);
            var pti_gs = IntVector2.FromRound(pt_gs);
            return 0 != (m_Generator.GetFlagsAt(pti_gs.X, pti_gs.Y) & EarthGenerateRound.FLAG_START_REGION);
        }

        public GameObject m_BiomeMarkerPrefab;

        class EarthGenerateRound
        {
            // Slots for placing stuff
            class StuffSlot
            {
                public Vector3 m_Pos_ws;
                public Quaternion m_Rot;
                public Biome m_Biome;

                public StuffSlot(Vector3 pos_ws, Quaternion rot, Biome biome)
                {
                    m_Pos_ws = pos_ws;
                    m_Rot = rot;
                    m_Biome = biome;
                }
            }

            Dictionary<int, List<StuffSlot>> m_OreSlots = new Dictionary<int, List<StuffSlot>>();
            void AddOreSlot(int x, int y, StuffSlot slot)
            {
                Biome biome, biomeb;
                float distDelta = GetTwoBiomeFromLoc_gs(x, y, out biome, out biomeb);
                List<StuffSlot> stuffs = m_OreSlots.GetValueOrCreate(biome.m_Index);
                stuffs.Add(slot);
            }

            // Cache some general flags for speed.
            public const int FLAG_START_REGION = 1;
            public const int FLAG_START_REGION_CLOSE = 2;
            byte[,] m_Flags;
            public byte GetFlagsAt(int x, int y)
            {
                if (x < 0 || y < 0) return 0;
                if (x >= m_Gen.m_DimensionX || y >= m_Gen.m_DimensionY) return 0;
                return m_Flags[x, y];
            }

            class CollectEdge
            {
                public int mask;
                public SinglePhysicsOnlyZoneOutput zo = new SinglePhysicsOnlyZoneOutput();
                public CollectEdge(VoxelGenerator gen, List<Layer> layers, ref IntVector2 zone)
                {
                    m_Gen = gen;
                    mask = gen.GenerateQuadForZone(layers[0], zone, true, zo);
                }

                public void Compute()
                {
                    var edge = zo.m_Edges[0];
                    p0_ws = m_Gen.transform.TransformPoint(zo.m_Points[edge.X]);
                    p1_ws = m_Gen.transform.TransformPoint(zo.m_Points[edge.Y]);
                    delta_ws = (p1_ws - p0_ws);
                    negNrm = Vector3.Cross(delta_ws, Vector3.forward).normalized;
                    gravityDir = RadialGravity.GetDirectionAtPoint(m_Gen.transform.TransformPoint(p0_ws));
                }

                public VoxelGenerator m_Gen;
                public Vector3 p0_ws;
                public Vector3 p1_ws;
                public Vector3 delta_ws;
                public Vector3 negNrm;
                public Vector3 gravityDir;

            }

            ProxyWorld m_ProxyWorld;
            ProxyWorld.Proxy AddProxyByName(string name, Vector3 pt_ws, Quaternion rot, Biome biome)
            {
                var proxy = m_ProxyWorld.AddProxyByName(name, pt_ws, rot);
                proxy.Level = biome.Level;
                proxy.RandSeed = m_Rng.IntValue;
                return proxy;
            }

            // Place stuff in world
            public void PlaceStuff()
            {
                GridValues<bool> excludePlaceTree = new GridValues<bool>();
                GridValues<bool> excludePlacePiles = new GridValues<bool>();
                GridValues<bool> excludePlaceHazard = new GridValues<bool>();
                GridValues<bool> excludePlaceOre = new GridValues<bool>();
                GridValues<bool> excludePlaceEnemy = new GridValues<bool>();

                var distToAir = new GridValues<int>();

                var layer0 = m_Layers[0];
                var depthCount = new int[m_Gen.m_DimensionX, m_Gen.m_DimensionY, 2];
                int bufferSwap = 0;
                for (int x = 0; x < m_Gen.m_DimensionX; x++)
                {
                    for (int y = 0; y < m_Gen.m_DimensionY; y++)
                    {
                        depthCount[x, y, bufferSwap] = (layer0.GetValue(x, y) > ZERO_CROSSING_VALUE) ? 1 : -1;
                    }
                }

                for (int d = 1; d < 7; d++)
                {
                    int lastBufferSwap = bufferSwap;
                    bufferSwap = (bufferSwap + 1) % 2;
                    for (int x = 1; x < m_Gen.m_DimensionX - 1; x++)
                    {
                        for (int y = 1; y < m_Gen.m_DimensionY - 1; y++)
                        {
                            int newValue = depthCount[x, y, lastBufferSwap];
                            for (int pn = -1; pn <= 1; pn += 2)
                            {
                                int dpn = d * pn;
                                if (dpn == depthCount[x, y, lastBufferSwap] &&
                                    dpn == depthCount[x - 1, y, lastBufferSwap] &&
                                    dpn == depthCount[x + 1, y, lastBufferSwap] &&
                                    dpn == depthCount[x, y - 1, lastBufferSwap] &&
                                    dpn == depthCount[x, y + 1, lastBufferSwap])
                                {
                                    newValue = dpn + pn;
                                    if (pn > 0)
                                    {
                                        layer0.m_DebugMaterials[x, y] = (byte)(d + 1);
                                    }
                                }
                            }
                            depthCount[x, y, bufferSwap] = newValue;
                        }
                    }
                }

                // Clean edges of map
                int EDGE_VALUE = -100;
                for (int x = 0; x < m_Gen.m_DimensionX; x++)
                {
                    for (int i=0;i<10;i++)
                    {
                        depthCount[x, 1+i, bufferSwap] = EDGE_VALUE;
                        depthCount[x, m_Gen.m_DimensionY - (1+i), bufferSwap] = EDGE_VALUE;
                    }
                }
                for (int y = 0; y < m_Gen.m_DimensionY; y++)
                {
                    depthCount[1, y, bufferSwap] = EDGE_VALUE;
                    depthCount[m_Gen.m_DimensionX - 2, y, bufferSwap] = EDGE_VALUE;
                }

                // shovel depth to debug materials
                for (int x = 1; x < m_Gen.m_DimensionX - 1; x++)
                {
                    for (int y = 1; y < m_Gen.m_DimensionY - 1; y++)
                    {
                        layer0.m_DebugMaterials[x, y] = (byte)((layer0.m_DebugMaterials[x, y] + depthCount[x, y, bufferSwap]) % 256);
                    }
                }

                m_ProxyWorld = FindObjectOfType<ProxyWorld>();
                /* ray casting tree method
                for (int i = 0; i < 60; i++)
                {
                    var start = TwoDee.Math3d.FromLengthAngleDegrees2D(400.0f, i * 3);
                    var end = Vector3.zero;
                    IntersectInfo info;
                    if (m_Gen.IntersectSegment(start, end, out info))
                    {
                        var reverseDir = (start - end).normalized;
                        float compDir = Vector3.Dot(reverseDir, info.m_Normal_ws);
                        if (compDir > 0.8f)
                        {
                            var pt_ws = info.m_Pos_ws;
                            pt_ws.z = 1.0f;
                            PlaceObject("tree", proxyWorld, pt_ws, Quaternion.FromToRotation(Vector3.up, reverseDir));

                            excludePlace.PopulateBoxDim(info.m_Gx, info.m_Gy, 4, 4, true);
                        }
                    }
                }
                */

                float w = m_Gen.ZoneWidth;
                float h = m_Gen.ZoneHeight;

                IntVector2 zone = new IntVector2();

                int x0 = 1;
                int x1 = m_Gen.m_DimensionX - 1;
                int y0 = 1;
                int y1 = m_Gen.m_DimensionY - 1;
                if (x0 < m_Gen.m_ClipX0) x0 = m_Gen.m_ClipX0;
                if (x1 > m_Gen.m_ClipX1) x1 = m_Gen.m_ClipX1;
                if (y0 < m_Gen.m_ClipY0) y0 = m_Gen.m_ClipY0;
                if (y1 > m_Gen.m_ClipY1) y1 = m_Gen.m_ClipY1;

                IntVector2 pos = new IntVector2();

                // Tree placement first
                for (int x = x0; x < x1; x++)
                {
                    for (int y = y1 - 1; y >= y0; y--)
                    {
                        Biome biome, biomeb;
                        float distDelta = GetTwoBiomeFromLoc_gs(x, y, out biome, out biomeb);

                        float dis = GetDepthIntoSurface_gs(x, y);

                        if (dis > 30.0f) continue;
                        if (0 != (m_Flags[x, y] & FLAG_START_REGION)) continue;
                        if (excludePlaceTree.GetValue(x, y) == true) continue;

                        zone.Reinit(x, y);
                        var collectEdge = new CollectEdge(m_Gen, m_Layers, ref zone);

                        // if (mask == SIDE_BOTTOM)
                        if (collectEdge.zo.m_Edges.Count == 1)
                        {
                            collectEdge.Compute();

                            //                                if (collectEdge.delta_ws.magnitude > 0.5f)
                            {
                                Vector3 pt_ws = Vector3.Lerp(collectEdge.p0_ws, collectEdge.p1_ws, 0.5f);
                                pt_ws.z = 0.0f;

                                var dot = Vector3.Dot(collectEdge.negNrm, collectEdge.gravityDir);
                                string addThing = null;
                                if (dot > 0.9f && m_Rng.Value > 0.8f)
                                {
                                    // Check space above for clear
                                    if (m_Gen.IsBoxClearAt(collectEdge.gravityDir * -6.0f + pt_ws, -collectEdge.gravityDir, 1.0f, 4.0f))
                                    {
                                        // Bury it slightly under
                                        pt_ws += 0.25f * collectEdge.gravityDir;
                                        addThing = "tree";
                                        m_Gen.ModifyCircle(2.0f, true, pt_ws + 1.25f * collectEdge.gravityDir, 1.5f);
                                        excludePlaceTree.PopulateBoxDim(x, y, 7, 7, true);
                                        excludePlacePiles.PopulateBoxDim(x, y, 2, 2, true);
                                        pt_ws.z = 1.0f;
                                    }
                                }

                                if (addThing != null)
                                {
                                    var proxy = AddProxyByName(addThing, pt_ws, Quaternion.FromToRotation(Vector3.down, collectEdge.gravityDir), biome);
                                    if (proxy != null)
                                    {
                                        proxy.Level = biome.Level;
                                        var proxyComp = proxy.GetData<Tree.Proxy>();
                                        if (proxyComp != null)
                                        {
                                            float scale = 0.75f + 0.04f * biome.Level;
                                            proxy.m_Scale = new Vector3(scale, scale, scale);
                                            proxyComp.m_Wood = proxyComp.m_Wood * (1 + biome.Level);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                var playerStart_gs = m_StartPos;
                for (int x = x0; x < x1; x++)
                {
                    for (int y = y1 - 1; y >= y0; y--)
                    {
                        pos.Reinit(x, y);
                        var dist = m_StartPos.Distance(pos);

                        // Can't place anything near start area
                        if (0 != (m_Flags[x, y] & FLAG_START_REGION_CLOSE)) continue;

                        bool ignore = false;
                        // Ignore if some other layer has crap in it
                        for (int ll = 1; ll < m_Layers.Count; ll++)
                        {
                            var layer = m_Layers[ll];
                            if (!layer.Background && !m_Layers[ll].IsZoneClear(new IntVector2(x, y)))
                            {
                                ignore = true;
                                break;
                            }
                        }
                        if (ignore) continue;

                        Biome biome, biomeb;
                        float distDelta = GetTwoBiomeFromLoc_gs(x, y, out biome, out biomeb);

                        // Underground ore
                        if (excludePlaceOre.GetValue(x, y) != true && (0 == (m_Flags[x, y] & FLAG_START_REGION)))
                        {
                            int earthDepth = depthCount[x, y, bufferSwap];
                            if (earthDepth >= 4)
                            {
                                if (m_Rng.Value > 0.7f)
                                {
                                    var pt_ws = m_Gen.transform.TransformPoint(new Vector3(x * m_Gen.ZoneWidth, y * m_Gen.ZoneHeight, 0.0f));
                                    AddOreSlot(x, y, new StuffSlot(pt_ws, Quaternion.identity, biome));

                                    excludePlaceOre.PopulateBoxDim(x, y, 6, 6, true);
                                }
                            }
                            if (!excludePlaceEnemy.GetValue(x, y) && earthDepth == -4)
                            {
                                // choose what kind to place based on surroundings
                                if (dist > 30.0f)
                                {
                                    var pt_ws = m_Gen.transform.TransformPoint(new Vector3(x * m_Gen.ZoneWidth, y * m_Gen.ZoneHeight, 0.0f));
                                    var proxy = AddProxyByName("monsterprototurret", pt_ws, Quaternion.identity, biome);
                                    var gecproxy = proxy.GetData<GenericEnemyCharacter.Proxy>();
                                    gecproxy.AddPerk(m_Rng.Value);

                                    IntersectInfo info = new IntersectInfo();
                                    var end = pt_ws + 10.0f * RadialGravity.GetDirectionAtPoint(pt_ws);
                                    var groundPlace = m_Gen.IntersectSegment(pt_ws, end, out info, true);

                                    UnityEngine.Debug.DrawLine(pt_ws, groundPlace ? info.m_Pos_ws : end, (info != null) ? Color.red : Color.green, 300.0f);

                                    if (groundPlace)
                                    {
                                        var outPlace = info.m_Pos_ws;
                                        var dell = (outPlace - pt_ws);
                                        var mag = dell.magnitude;
                                        if (mag > 20.0f)
                                        {
                                            UnityEngine.Debug.LogError("WTF");
                                        }
                                        //proxy = AddProxyByName("monsterprototurret", outPlace, Quaternion.identity, biome);
                                    }

                                    excludePlaceEnemy.PopulateBoxDim(x, y, 15, 15, true);
                                }

                            }
                        }

                        zone.Reinit(x, y);
                        var collectEdge = new CollectEdge(m_Gen, m_Layers, ref zone);

                        // Radiation shards
                        if (distDelta < 3.0f)
                        {
                            if (!excludePlaceHazard.GetValue(x, y))
                            {
                                var pt_ws = m_Gen.transform.TransformPoint(new Vector3(x * m_Gen.ZoneWidth, y * m_Gen.ZoneHeight, 0.0f));

                                //AddProxyByName("geyser", pt_ws, Quaternion.identity, biome);
                                //excludePlaceHazard.PopulateBoxDim(x, y, 4, 4, true);
                            }
                        }

                        // if (mask == SIDE_BOTTOM)
                        if (collectEdge.zo.m_Edges.Count == 1)
                        {
                            bool resourceAdded = false;
                            collectEdge.Compute();
                            {
                                Vector3 pt_ws = Vector3.Lerp(collectEdge.p0_ws, collectEdge.p1_ws, 0.5f);
                                pt_ws.z = 0.0f;

                                var dot = Vector3.Dot(collectEdge.negNrm, collectEdge.gravityDir);
                                string addThing = null;

                                // Place certain things at certain heights
                                float dfs = GetDepthIntoSurface_gs(x, y);

                                // Piles
                                if (!excludePlacePiles.GetValue(x, y))
                                {
                                    bool isUnderground = (dfs > 10.0f);
                                    if (dot > 0.91f) //0.9 is about 25 degrees off
                                    {
                                        switch (m_Rng.Range(0, 3))
                                        {
                                            case 0:
                                                addThing = "stickpile";
                                                break;
                                            case 1:
                                                addThing = "rockpile";
                                                break;
                                            case 2:
                                                addThing = isUnderground ? "mushroom" : "hemp";
                                                break;
                                        }
                                    }
                                    else if (dot < -0.5f)
                                    {
                                        //addThing = "mushroom";
                                    }                                   

                                    if (addThing != null && addThing.Length > 0)
                                    {
                                        AddProxyByName(addThing, pt_ws, Quaternion.FromToRotation(Vector3.right, collectEdge.delta_ws.normalized), biome);
                                        excludePlacePiles.PopulateBoxDim(x, y, 3, 3, true);
                                        resourceAdded = true;
                                    }
                                }

                                // Hazards
                                if (!resourceAdded && !excludePlaceHazard.GetValue(x,y) && dfs < 10.0f && dist > 30.0f)
                                {
                                    AddProxyByName("geyser", pt_ws, Quaternion.FromToRotation(Vector3.right, collectEdge.delta_ws.normalized), biome);
                                    excludePlaceHazard.PopulateBoxDim(x, y, 10, 10, true);
                                }

                            }
                        }
                    }
                }

                // Place deferred stuff
                foreach (var biomeEntry in m_OreSlots)
                {
                    var list = biomeEntry.Value;
                    //m_Rng.Shuffle(list);
                    // Place the one teleport ore somewhere near the top of the zone
                    list.Sort((a, b) => GetDepthIntoSurface_ws(a.m_Pos_ws).CompareTo(GetDepthIntoSurface_ws(b.m_Pos_ws)));
                    int swapTeleport = m_Rng.Range(0, Math.Min(list.Count, 6));
                    list.Swap(swapTeleport, 0);

                    if (list.Count > 0)
                    {
                        int desiredIndex = list.Count / 2;
                        if (list[0].m_Biome.Level < 2)
                        {
                            desiredIndex = 0;
                        }
                        var entry = list[desiredIndex];
                        CreateOre(m_ProxyWorld, "oreteleport", entry.m_Pos_ws, entry.m_Rot, entry.m_Biome);
                        list.RemoveAt(desiredIndex);
                    }
                    m_Rng.Shuffle(list);
                    // Two alien box per zone
                    for(int i=0;i<2;i++)
                    {
                        if (list.Count > 0)
                        {
                            var entry = list[0];
                            CreateOre(m_ProxyWorld, "orealienbox", entry.m_Pos_ws, entry.m_Rot, entry.m_Biome);
                            list.RemoveAt(0);
                        }
                    }
                    foreach (var entry in list)
                    {
                        var randomZeroOne = m_Rng.Value;
                        CreateOre(m_ProxyWorld, ChooseOreForBiome(randomZeroOne, entry.m_Biome),
                            entry.m_Pos_ws, entry.m_Rot, entry.m_Biome);
                    }
                }

            }

            string ChooseOreForBiome(float randomZeroOne, Biome biome)
            {
                if(biome.Level <= 1)
                {
                    return new string[] { "orecopper", "oresulfur", "orestone", "radiationstone" }.RandomlyPick(randomZeroOne, new float[] { 0.25f, 0.25f, 0.25f, 0.25f });
                }
                else
                {
                    return new string[] { "oreiron", "orecopper", "oresulfur", "orestone", "radiationstone" }.RandomlyPick(randomZeroOne, new float[] { 0.1f, 0.2f, 0.2f, 0.2f, 0.25f });
                }
            }
                


            void CreateOre(ProxyWorld proxyWorld, string name, Vector3 pos_ws, Quaternion rot, Biome biome)
            {
                float surfacePercent = GetNormalizedPercentToSurface_ws(pos_ws) + m_Rng.Range(-0.05f, 0.05f);
                var normalizedOreDepth = Mathf.Clamp(1.0f - surfacePercent, 0.0f, 1.0f);
                float scale = 0.9f + 4.0f * normalizedOreDepth;

                var proxy = AddProxyByName(name, pos_ws, rot, biome);
                if (proxy != null)
                {
                    proxy.Level = biome.Level;
                    proxy.m_Scale = new Vector3(scale, scale, scale);
                    var proxyResource = proxy.GetData<StoredResource.Proxy>();
                    if (proxyResource != null)
                    {
                        proxyResource.m_WeightScale = scale;
                        if (name == "orealienbox")
                        {
                            // Loot in box is seeded by level
                            var newInv = new Inventory(10);
                            m_Gen.GenerateAlienLoot(m_Rng, newInv, biome.Level);
                            proxyResource.m_Inventory = newInv;
                        }
                    }
                }

                // Carve it out of the hardrock since the challenge is usually getting there in the first place
                m_Gen.ModifyCircle(2.0f, false, pos_ws, 2.0f, true);
            }

            class Biome
            {
                public enum Type
                {
                    Starting = 0,
                    Plains,
                    Mountain,
                    Canyon,
                    Gulch,
                    MAX_SURFACE,

                    // underground types
                    Solid,
                    Hollow,
                };
                public bool IsSurface
                {
                    get
                    {
                        return (int)m_Type <= (int)Type.MAX_SURFACE;
                    }
                }
                public List<Biome> m_NeighborBiomes = new List<Biome>();
                public void SetNeighbors(IEnumerable<Biome> neighbors)
                {
                    foreach(var biome in neighbors)
                    {
                        m_NeighborBiomes.Add(biome);
                        if (m_NeighborBiomes.Count >= 4) return; // only allow N nearest
                    }
                }

                public List<Biome> m_ConnectedZones = new List<Biome>();
                public bool Connected(Biome other)
                {
                    return m_ConnectedZones.Contains(other);
                }
                public bool Connect(Biome other)
                {
                    if (m_ConnectedZones.Contains(other)) return false;

                    m_ConnectedZones.Add(other);
                    other.m_ConnectedZones.Add(this);
                    return true;
                }
                public IntVector2 m_Position;
                public Type m_Type;
                public int m_Index;
                public int m_Level;
                public int Level
                {
                    get { return m_Level; }
                    set
                    {
                        m_Level = value;
                    }
                }

                public float GetBaseUndergroundThickness(int x, int y, float distFromSurface)
                {
                    var thickness = (distFromSurface < 10.0f) ? 0.45f : 0.1f;

                    float surfaceAdjustedThickness = Mathf.Clamp(0.7f - distFromSurface * 0.03f, 0.0f, 1.0f);
                    switch (m_Type)
                    {
                        case Biome.Type.Starting: thickness = surfaceAdjustedThickness; break;
                        case Biome.Type.Canyon: thickness = 0.05f; break;
                        case Biome.Type.Mountain: thickness = surfaceAdjustedThickness; break;
                        case Biome.Type.Plains: thickness = surfaceAdjustedThickness; break;
                        case Biome.Type.Gulch:
                            {
                                float distanceFromCenter = m_Position.Distance(x, y);
                                float normalizedDistance = Mathf.Clamp(distanceFromCenter / 50.0f, 0.0f, 1.0f);
                                thickness = Mathf.Lerp(-0.25f, 0.15f, normalizedDistance);
                                break;
                            }

                        case Biome.Type.Solid: thickness = 0.2f; break;
                        case Biome.Type.Hollow: thickness = 0.05f; break;
                    }

                    return thickness;
                }

                public float GetBaseAboveGroundThickness(int x, int y, float distFromSurface, float distFromCenter, float allowedRadius2, float instability)
                {
                    var thickness = 0.05f;
                    thickness += Mathf.Lerp(-1.0f, 0.0f, instability);

                    bool floatingIslands = true;
                    switch(m_Type)
                    {
                        case Biome.Type.Starting: thickness = -1.0f; floatingIslands = false; break;
                        case Biome.Type.Mountain: thickness = -1.0f; break;
                        case Biome.Type.Plains: thickness = -1.0f; floatingIslands = false; break;
                        case Biome.Type.Canyon: thickness = -1.0f; break;
                        case Biome.Type.Gulch: thickness = -1.0f; floatingIslands = false; break;

                        case Biome.Type.Hollow: thickness = -1.0f; break;
                        case Biome.Type.Solid: thickness = -1.0f; break;
                    }

                    if (floatingIslands && (allowedRadius2 - distFromCenter) > 0.1f)
                    {
                        thickness = 0.0f;
                    }

                    return thickness;
                }

                public Biome(int index, IntVector2 pos, Type t, int level)
                {
                    m_Index = index;
                    m_Position = pos;
                    m_Type = t;
                    m_Level = level;
                }
            }
            List<Biome> m_Biomes = new List<Biome>();
            List<Biome> GetClosetBiomes(Biome selfBiome, float threshold)
            {
                IntVector2 selfPos = selfBiome.m_Position;

                var result = new List<Biome>();
                for (int i = 0; i < m_Biomes.Count; i++)
                {
                    var biome = m_Biomes[i];
                    if (biome == selfBiome) continue;
                    float dist = selfPos.Distance(biome.m_Position);
                    if (dist < threshold)
                    {
                        result.Add(biome);
                    }
                }

                result.Sort((a, b) => (selfPos.Distance(a.m_Position)).CompareTo(selfPos.Distance(b.m_Position)));
                return result;
            }

            float GetTwoBiomeFromLoc_gs(int x, int y, out Biome a, out Biome b, Biome excludeBiome=null)
            {
                Biome closestBiome = null;
                Biome closestBiome2 = null;
                float closestDist = 99999999.0f;
                float closestDist2 = 99999999.0f;
                for (int i = 0; i < m_Biomes.Count; i++)
                {
                    var biome = m_Biomes[i];
                    if (excludeBiome != null && excludeBiome == biome) continue;

                    float dist = new IntVector2(x, y).Distance(biome.m_Position);
                    if (dist < closestDist)
                    {
                        closestBiome2 = closestBiome;
                        closestDist2 = closestDist;

                        closestDist = dist;
                        closestBiome = biome;
                    }
                    else if (dist < closestDist2)
                    {
                        closestBiome2 = biome;
                        closestDist2 = dist;
                    }
                }

                a = closestBiome;
                b = closestBiome2;
                return closestDist2 - closestDist;
            }

            IntVector2 GetSurfaceBiomePosition(int i, int numSurfaceZones)
            {
                float anglePercent = i / (numSurfaceZones * 1.0f);
                float normalizedHeight = AnglePercentToHeightArray(anglePercent, m_Heights);
                return GetGridAtPercentAngle(normalizedHeight, anglePercent);
            }

            IEnumerable<float> GenerateBiomes()
            {
                int numSurfaceZonesAtLeast = Mathf.FloorToInt(m_Circumference / 80.0f);
                int numSurfaceZones = 4;
                while(numSurfaceZones < numSurfaceZonesAtLeast)
                {
                    numSurfaceZones += 4;
                }
                int numSurfaceZonesHalf = numSurfaceZones / 2;

                var surfaceZones = new List<Biome>(new Biome[numSurfaceZones]);

                // Create surface biomes.  Starting biome is always the same. 
                var startBiome = new Biome(m_Biomes.Count, GetSurfaceBiomePosition(0, numSurfaceZones), Biome.Type.Starting, 0);
                surfaceZones[0] = startBiome;

                int canyonIndex = numSurfaceZones-3;
                bool isGulch = true;
                while (canyonIndex > 0)
                {
                    surfaceZones[canyonIndex] = new Biome(canyonIndex, GetSurfaceBiomePosition(canyonIndex, numSurfaceZones), isGulch ? Biome.Type.Gulch : Biome.Type.Canyon, 0);
                    canyonIndex -= m_Rng.Range(2, 3);
                    isGulch = !isGulch;
                }

                Biome startLeft = null, startRight = null;

                for (int i = 1; i < numSurfaceZones; i++)
                {
                    // Immediate left and right are always mountain and canyon.
                    if (i == 1)
                    {
                        surfaceZones[i] = startRight = new Biome(i, GetSurfaceBiomePosition(i, numSurfaceZones), Biome.Type.Mountain, 1);
                    }
                    if (i == (numSurfaceZones - 1))
                    {
                        surfaceZones[i] = startLeft = new Biome(i, GetSurfaceBiomePosition(i, numSurfaceZones), Biome.Type.Canyon, 1);
                    }
                    // Else create some canyons and then surround by random mountains and plains.
                    if (surfaceZones[i] == null)
                    {
                        surfaceZones[i] = new Biome(i, GetSurfaceBiomePosition(i, numSurfaceZones), Biome.Type.Mountain, 1);
                    }

                    int dist = (i < numSurfaceZonesHalf) ? i : (numSurfaceZones - i);
                    surfaceZones[i].Level = dist;
                }

                foreach(var biome in surfaceZones)
                {
                    m_Biomes.Add(biome);
                }
                
                // Create underground zones.  Loop over grid and make new zones that are far away enough from existing.
                int xGridSize = m_Gen.m_DimensionX;
                int yGridSize = m_Gen.m_DimensionY;
                IntVector2 zone = new IntVector2();
                for (int x = 0; x < xGridSize; x += 10)
                {
                    for (int y = 0; y < yGridSize; y += 10)
                    {
                        zone.Reinit(x, y);
                        var depth = GetDepthIntoSurface_gs(x, y);
                        if (depth > 70.0f)
                        {
                            Biome a, b;
                            GetTwoBiomeFromLoc_gs(x, y, out a, out b);
                            if (a.m_Position.Distance(zone) > 60.0f)
                            {
                                m_Biomes.Add(new Biome(m_Biomes.Count, zone, m_Rng.Value > 0.5f ? Biome.Type.Hollow : Biome.Type.Solid, 1));
                            }
                        }
                    }
                }

                // Connect biomes.  Start biome always connects to left and right.  Anything else is fair game.
                startBiome.Connect(startLeft);
                startBiome.Connect(startRight);

                // Compute neighbors pass
                foreach (var biome in m_Biomes)
                {
                    biome.SetNeighbors(GetClosetBiomes(biome, 130.0f));
                }

                yield return ComputePercentDone(1, 1, 2);

                // Connect some neighbors
                int ii = 0;
                foreach (var biome in m_Biomes)
                {
                    ii++;

                    // Canyon always gets a connection going into one non-surface biome
                    if (biome.m_Type == Biome.Type.Canyon || biome.m_Type == Biome.Type.Gulch)
                    {
                        var neighbors = biome.m_NeighborBiomes;
                        foreach (var neighborBiome in neighbors)
                        {
                            if (neighborBiome.IsSurface) continue;
                            else
                            {
                                biome.Connect(neighborBiome);
                                break;
                            }
                        }
                    }
                    // Don't connect any more surface biome
                    else if (biome.IsSurface) continue;
                    else if (biome == startBiome) continue;
                    else
                    {
                        // Underground
                        var neighbors = GetClosetBiomes(biome, 100.0f);
                        foreach (var neighborBiome in neighbors)
                        {
                            if (biome.Connect(neighborBiome)) break;
                        }
                    }
                }

                // Compute levels pass based on zone distance to start
                {
                    var explored = new Dictionary<Biome, bool>();
                    var queue = new List<Biome>();

                    // Surface is already good.
                    foreach (var surfaceZone in surfaceZones)
                    {
                        queue.Add(surfaceZone);
                        explored[surfaceZone] = true;
                    }

                    while (queue.Count > 0)
                    {
                        var first = queue[0];
                        queue.RemoveAt(0);
                        int nextLevel = first.Level + 1;
                        foreach (var neighbor in first.m_ConnectedZones)
                        {
                            if (!explored.ContainsKey(neighbor))
                            {
                                neighbor.Level = nextLevel;
                                explored[neighbor] = true;
                                queue.Add(neighbor);
                            }
                        }
                    }
                }

                yield return ComputePercentDone(1, 2, 2);

                // @TEST show biome positions
                /*
                foreach(var biome in m_Biomes)
                {
                    GameObject.Instantiate<GameObject>((m_Gen as PVoxelGenerator).m_BiomeMarkerPrefab, m_Gen.GridSpaceToWorld(new Vector3(biome.m_Position.X, biome.m_Position.Y)), Quaternion.identity);
                }
                */

                /*

                    // Create some random ones
                    int nextCanyonRight = m_Rng.Range(1, 3);
                int nextCanyonLeft = m_Rng.Range(2, 4);
                for (int i = 2; i < 10; i++)
                {
                    var type = Biome.Type.Mountain;
                    if (i % 2 == 0) type = Biome.Type.Plains;
                    else
                    if (nextCanyonRight-- == 0)
                    {
                        type = Biome.Type.Canyon;
                        nextCanyonRight = m_Rng.Range(2, 4);
                    }
                    m_Biomes.Add(new Biome(m_Biomes.Count, GetVecAtPercentAngle(1.0f, i * 200.0f / m_Circumference), type, i));
                    m_Biomes.Add(new Biome(m_Biomes.Count, GetVecAtPercentAngle(0.6f, i * 200.0f / m_Circumference), type, i));
                    type = Biome.Type.Mountain;
                    if (i % 2 == 0) type = Biome.Type.Plains;
                    else
                    if (nextCanyonLeft-- == 0)
                    {
                        type = Biome.Type.Canyon;
                        nextCanyonLeft = m_Rng.Range(2, 4);
                    }
                    m_Biomes.Add(new Biome(m_Biomes.Count, GetVecAtPercentAngle(0.6f, i * -200.0f / m_Circumference), type, i));
                    m_Biomes.Add(new Biome(m_Biomes.Count, GetVecAtPercentAngle(1.0f, i * -200.0f / m_Circumference), type, i));
                }
                //                m_Biomes.Add(new Biome(m_Biomes.Count, new IntVector2(m_Radius/4, m_Radius), Biome.Type.Mountain));
                //              m_Biomes.Add(new Biome(m_Biomes.Count, new IntVector2(-m_Radius / 4, m_Radius), Biome.Type.Canyon));
                */
            }

            float startingAreaHeight = 0.9f;
            float startingAreaAngleCircum = 200.0f;

            IntVector2 m_StartPos;
            TwoDee.RandomGeneratorUnity m_Rng;
            List<Layer> m_Layers;
            public EarthGenerateRound(VoxelGenerator gen, List<Layer> layers)
            {
                m_Rng = new TwoDee.RandomGeneratorUnity(UnityEngine.Random.Range(0, 99999999));
                m_Gen = gen as PVoxelGenerator;
                m_Layers = layers;
            }

            IEnumerable<float> GenerateRound(Layer layer, Layer layerRock, Layer layerBg, float xOrg, float yOrg, float xOrg2, float yOrg2)
            {
                var playerStart_gs = m_StartPos;

                //float bigPerlinFactorX = 20 / 1000.0f;
                //float bigPerlinFactorY = 20 / 1000.0f;
                float perlinFactorX = 50.0f / 1000.0f;
                float perlinFactorY = 50.0f / 1000.0f;
                float perlinSmallFactorX = 180.0f / 1000.0f;
                float perlinSmallFactorY = 180.0f / 1000.0f;

                // Can't put values on the very edges because they will have incomplete physics- the leftmost zone for example would be interior if all its grid points were set.
                float halfDimX = m_Gen.Dimension.X / 2;
                float halfDimY = m_Gen.Dimension.Y / 2;

                var values = new float[m_Gen.Dimension.X, m_Gen.Dimension.Y];
                {
                    for (int y = 0; y < values.GetLength(1); y++)
                        for (int x = 0; x < values.GetLength(0); x++)
                            values[x, y] = -1.0f;
                }

                int x0 = 1;
                int x1 = values.GetLength(0) - 1;
                int y0 = 1;
                int y1 = values.GetLength(1) - 1;
                if (x0 < m_Gen.m_ClipX0) x0 = m_Gen.m_ClipX0;
                if (x1 > m_Gen.m_ClipX1) x1 = m_Gen.m_ClipX1;
                if (y0 < m_Gen.m_ClipY0) y0 = m_Gen.m_ClipY0;
                if (y1 > m_Gen.m_ClipY1) y1 = m_Gen.m_ClipY1;

                for (int cycle = 0; cycle < 3; cycle++)
                {
                    for (int y = y0; y < y1; y++)
                    {
                        float ynorm = (y * 1.0f) / values.GetLength(1);
                        if (cycle == 2 && (y%20 == 0))
                        {
                            yield return ComputePercentDone(2, y - y0, (y1 - y0));
                        }

                        for (int x = x0; x < x1; x++)
                        {
                            if (cycle == 0)
                            {
                                // Cycle 1, create crust layer and in cycle 2 expand it out.
                                continue;
                            }
                            else if (cycle == 1)
                            {
                                continue;
                            }

                            float manhattanFromStart = Mathf.Abs(playerStart_gs.X - x) + Mathf.Abs(playerStart_gs.Y - y);
                            if (manhattanFromStart < 16.0f) m_Flags[x, y] |= FLAG_START_REGION;
                            if (manhattanFromStart < 8.0f) m_Flags[x, y] |= FLAG_START_REGION_CLOSE;

                            // Final cycle, determine thickness of each block
                            layer.m_DebugMaterials[x, y] = (byte)m_Rng.Range(0, 256);
                            float xCoord = xOrg + x; // We don't want to scale the size of features by how big the world is / (1.0f * values.GetLength(0));
                            float yCoord = yOrg + y; // / (1.0f * values.GetLength(1));
                            float xCoord2 = xOrg2 + x; // / (1.0f * values.GetLength(0));
                            float yCoord2 = yOrg2 + y; // / (1.0f * values.GetLength(1));

                            // Decide what to do based on distance from planet surface.
                            float theta, distFromCenter;
                            float normalizedHeight = RadialSampleNormalizedHeightAt_gs(m_Heights, x, y, out theta, out distFromCenter);
                            float normalizedHeight2 = RadialSampleNormalizedHeightAt_gs(m_UpperHeights, x, y, out theta, out distFromCenter);
                            float allowedRadius = halfDimX * normalizedHeight;
                            float allowedRadius2 = halfDimX * normalizedHeight2;

                            float distFromSurface = allowedRadius - distFromCenter;
                            float distFromSurfaceNormalized = distFromSurface / halfDimX;

                            float thickness = 1.0f;

                            float mediumNoise = ZERO_CROSSING_VALUE - 0.5f + Mathf.PerlinNoise(perlinFactorX * xCoord, perlinFactorY * yCoord);
                            float mediumNoise2 = ZERO_CROSSING_VALUE - 0.5f + Mathf.PerlinNoise(perlinFactorX * xCoord2, perlinFactorY * yCoord2);
                            float smallNoise = ZERO_CROSSING_VALUE - 0.5f + Mathf.PerlinNoise(perlinSmallFactorX * xCoord, perlinSmallFactorY * yCoord);
                            float smallNoise2 = ZERO_CROSSING_VALUE - 0.5f + Mathf.PerlinNoise(perlinSmallFactorX * xCoord2, perlinSmallFactorY * yCoord2);
                            // hardrock
                            if (layer.DigPower > 1.0f)
                            {
                                smallNoise2 = 1.0f - smallNoise2;
                            }


                            float finalValue = -1.0f;
                            float finalValueBackground = -1.0f;
                            float finalValueRock = -1.0f;

                            Biome biome, biomeb;
                            float distDelta = GetTwoBiomeFromLoc_gs(x, y, out biome, out biomeb);
                            layer.m_DebugMaterials[x, y] = (byte)biome.m_Index;
                            layerBg.m_DebugMaterials[x, y] = (byte)biome.m_Index;
                            layerRock.m_DebugMaterials[x, y] = (byte)(254);

                            // Underground
                            if (distFromSurface > 1.0f)
                            {

                                thickness = biome.GetBaseUndergroundThickness(x, y, distFromSurface);
                                // Add variation
                                float finalNoiseValue = Mathf.Clamp(smallNoise, -1.0f, 1.0f);

                                finalValue = finalNoiseValue + thickness;
                                finalValueBackground = smallNoise + 0.45f;
                                // Burn out bigger noise except near start
                                if (mediumNoise < -0.27f && manhattanFromStart > 20.0f)
                                {
                                    finalValue = -0.5f;
                                }
                            }
                            // Aboveground
                            else
                            {
                                thickness = biome.GetBaseAboveGroundThickness(x, y, distFromSurface, distFromCenter, allowedRadius2, GetSurfaceInstability(theta));
                               
                                // Maximum radius
                                if (distFromCenter > halfDimX)
                                {
                                    thickness = -1.0f;
                                }

                                finalValue = smallNoise + thickness;
                            }

                            // intersect test
                            //finalValue = Mathf.Min(finalValue, smallNoise2);

                            // Lower rock value closer to surface
                            float hardRockThickness = thickness;
                            if (distFromSurface < 5.0f)
                            {
                                hardRockThickness = -0.2f;
                            }

                            finalValueRock = smallNoise2 + (Mathf.Min(hardRockThickness, 0.3f) - 0.3f);

                            // biome tunnels
                            float dist = 0.0f;
                            float tunnelDist = 100.0f;
                            if (biome != null && biomeb != null)
                            {
                                foreach (var connection in biome.m_ConnectedZones)
                                {
                                    if (connection.IsSurface && biome.IsSurface) continue;

                                    dist = Math3d.DistanceToLineSegment(biome.m_Position, connection.m_Position, new Vector3(x, y));
                                    tunnelDist = Mathf.Min(dist, tunnelDist);
                                }
                                if (tunnelDist < 4.0f)
                                {
                                    finalValue = smallNoise - 0.4f;
                                    finalValueRock = smallNoise2 - 0.4f;
                                }
                            }

                            // biome borders
                            if (distDelta < 8.0f && distFromSurface > 10.0f)
                            {
                                if(!biome.Connected(biomeb) || dist > 10.0f)
                                {
                                    if (tunnelDist > 3.0f)
                                    {
                                        finalValueRock = smallNoise2 + 0.1f;
                                    }
                                }
                            }

                            // Core
                            if (distFromCenter < 100)
                            {
                                finalValue = ZERO_CROSSING_VALUE + 0.5f;
                            }

                            layerRock.m_Values[x, y] = Mathf.Clamp(finalValueRock, -1.0f, 1.0f);
                            layerBg.m_Values[x, y] = Mathf.Clamp(finalValueBackground, -1.0f, 1.0f);

                            values[x, y] = Mathf.Clamp(finalValue, -1.0f, 1.0f);

                            // Place sunlight seed if above surface and also if not in dirt or rock
                            if (distFromSurface < 0.0f && (values[x, y] < ZERO_CROSSING_VALUE && layerRock.m_Values[x, y] < ZERO_CROSSING_VALUE))
                            {
                                m_Gen.LightingDataInstance.AddSunSeedPoint(x, y);
                            }
                        }
                    }
                }

                layer.m_Values = values;
            }

            float GetSurfaceInstability(float theta)
            {
                float distFromStart = Mathf.Abs(0.25f - theta);

                return Mathf.Pow(distFromStart * 50.0f, 2.0f);
            }

            float[] m_Heights;
            float[] m_UpperHeights;


            float GetNormalizedPercentToSurface_ws(Vector3 v)
            {
                var pos_gs = m_Gen.WorldSpaceToGrid(v);
                return GetNormalizedPercentToSurface_gs((int)pos_gs.x, (int)pos_gs.y);
            }

            float GetNormalizedPercentToSurface_gs(int x, int y)
            {
                float halfDimX = m_Gen.m_DimensionX / 2;
                float theta, distFromCenter;
                float normalizedHeight = RadialSampleNormalizedHeightAt_gs(m_Heights, x, y, out theta, out distFromCenter);
                float hundredPercent = halfDimX * normalizedHeight;
                return (distFromCenter / hundredPercent);
            }

            float GetDepthIntoSurface_ws(Vector3 v, int n=0)
            {
                var pos_gs = m_Gen.WorldSpaceToGrid(v);
                return GetDepthIntoSurface_gs((int)pos_gs.x, (int)pos_gs.y, n);
            }

                // Greater than zero- into surface.  Less than zero, above surface
            float GetDepthIntoSurface_gs(int x, int y, int n=0)
            {
                float halfDimX = m_Gen.m_DimensionX / 2;
                float theta, distFromCenter;
                float normalizedHeight = RadialSampleNormalizedHeightAt_gs(m_Heights, x, y, out theta, out distFromCenter);
                float normalizedHeight2 = RadialSampleNormalizedHeightAt_gs(m_UpperHeights, x, y, out theta, out distFromCenter);
                float allowedRadius = halfDimX * normalizedHeight;
                float allowedRadius2 = halfDimX * normalizedHeight2;

                float distFromSurface = allowedRadius - distFromCenter;

                return distFromSurface;
            }

            float AnglePercentToHeightArray(float normalizedAngle, float[] heightArray)
            {
                int angleIndex = Mathf.FloorToInt(normalizedAngle * heightArray.Length);
                angleIndex = angleIndex.Clamp(0, heightArray.Length - 1);

                return heightArray[angleIndex];
            }
            float RadialSampleNormalizedHeightAt_gs(float[] heightArray, int x, int y, out float normalizedAngle, out float normalizedHeight)
            {
                int dx = x - (m_Gen.Dimension.X / 2);
                int dy = y - (m_Gen.Dimension.Y / 2);
                if (dx == 0 && dy == 0)
                {
                    normalizedAngle = 0.0f;
                    normalizedHeight = 0.0f;
                    return 1.0f;
                }
                float normalizedAngleNegPos = Mathf.Atan2(-dx, dy);
                float twopi = Mathf.PI * 2.0f;
                normalizedAngle = (normalizedAngleNegPos > 0.0f ? (normalizedAngleNegPos / twopi) : (twopi + normalizedAngleNegPos) / twopi);
                normalizedHeight = Mathf.Sqrt(dx * dx + dy * dy);

                return AnglePercentToHeightArray(normalizedAngle, heightArray);
            }

            IEnumerable<float> GenerateHeightsRound(float perlinSeed0, float perlinseed1)
            {
                float startingAreaAnglePercent = startingAreaAngleCircum / m_Circumference;

                m_Heights = new float[1000];
                var perlinFactor = 0.005f * m_Circumference / m_Heights.Length;
                var perlinFactorU = 0.015f * m_Circumference / m_Heights.Length;

                for (int i = 0; i < m_Heights.Length; i++)
                {
                    m_Heights[i] = 0.8f + 0.1f * Mathf.PerlinNoise(perlinFactor * i, perlinSeed0);
                }

                int startingCenter = m_Heights.Length / 4;

                // for (int i = 0; i < m_Heights.Length / 4; i++) m_Heights[i + startingCenter] = 0.4f;
                // for (int i = 0; i < m_Heights.Length / 4; i++) m_Heights[-i + startingCenter] = 0.9f;

                // Flatten out the starting area                
                int flattenSize = Mathf.FloorToInt(startingAreaAnglePercent * m_Heights.Length * 0.5f);
                for (int i = 0; i < flattenSize; i++)
                {
                    var t = Mathf.Pow((i * 1.0f) / flattenSize, 2.0f);
                    m_Heights[i] = Mathf.Lerp(startingAreaHeight, m_Heights[i], t);
                    m_Heights[m_Heights.Length - 1 - i] = Mathf.Lerp(startingAreaHeight, m_Heights[m_Heights.Length - 1 - i], t);
                }

                m_UpperHeights = new float[1000];
                for (int i = 0; i < m_Heights.Length; i++)
                {
                    m_UpperHeights[i] = m_Heights[i] + 0.1f * Mathf.PerlinNoise(perlinFactorU * i, perlinseed1);
                }
                yield return ComputePercentDone(0, 1, 1);

                // Flatten out the starting area    (extra height)             
                for (int i = 0; i < flattenSize / 2; i++)
                {
                    var t = Mathf.Pow((i * 1.0f) / flattenSize, 2.0f);
                    m_UpperHeights[i] = Mathf.Lerp(startingAreaHeight, m_UpperHeights[i], t);
                    m_UpperHeights[m_Heights.Length - 1 - i] = Mathf.Lerp(startingAreaHeight, m_UpperHeights[m_Heights.Length - 1 - i], t);
                }
            }

            /*
            int m_ThicknessBlockSize = 20;
            float[,] m_Thickness;
            void GenerateThickness()
            {
                m_Thickness = new float[m_Gen.m_DimensionX / m_ThicknessBlockSize, m_Gen.m_DimensionY / m_ThicknessBlockSize];

                var perlinFactor = 10.0f;
                for (int x=0;x< m_Thickness.GetLength(0);x++)
                    for (int y = 0; y < m_Thickness.GetLength(1); y++)
                    {
                        float xCoord = x / (1.0f*m_Thickness.GetLength(0));
                        float yCoord = y / (1.0f * m_Thickness.GetLength(1));
                        m_Thickness[x, y] = Mathf.Lerp(-0.1f, 0.1f, Mathf.PerlinNoise(perlinFactor * xCoord, perlinFactor * yCoord));
                    }

                // Starting area will be very thicccc
                int tx = (m_Gen.m_DimensionX / 2) / m_ThicknessBlockSize;
                int ty = ((3* m_Gen.m_DimensionY) / 4) / m_ThicknessBlockSize;

                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = 0; dy <= 0; dy++)
                        m_Thickness[dx + tx, dy + ty] = 0.2f;

            }
            */


            /*
        // First biome always starts at theta=m_BiomeThetaOffset, each end border is when that one ends.
        float m_BiomeThetaOffset;
        class Biome
        {
            public enum Type
            {
                Starting = 0,
                Mountain,
                Canyon
            };
            public Type m_Type;
            public float m_End;

            public Biome()
            {
            }
        }
        Biome[] m_Biomes;
        int m_NumBiomes;
        void GenerateBiomes()
        {
            float startingAreaAnglePercent = startingAreaAngleCircum / m_Circumference;

            m_NumBiomes = Mathf.FloorToInt(m_Circumference / 100.0f);
            m_BiomeThetaOffset = 0.0f;
            m_Biomes = new Biome[m_NumBiomes];
            int startingIndex = m_NumBiomes / 4;

            for (int i = 0; i < m_NumBiomes; i++)
            {
                var biome = new Biome();

                // Starting area
                if (i == startingIndex) biome.m_Type = Biome.Type.Starting;
                else biome.m_Type = Biome.Type.Mountain;
                //else if(i == startingIndex+1) biome.m_Type = Biome.Type.Canyon;
                //else if (i == startingIndex - 1) biome.m_Type = Biome.Type.Mountain;

                if (i == 0) biome.m_Type = Biome.Type.Canyon;

                m_Biomes[i] = biome;
            }
            // Create canyons- don't want that many of them because it's the easiest way to go downwards
            m_Biomes[startingIndex + 1].m_Type = Biome.Type.Canyon;
            m_Biomes[startingIndex - 3].m_Type = Biome.Type.Canyon;
            m_Biomes[startingIndex + 4].m_Type = Biome.Type.Canyon;


            // Adjust biome ends based on starting biome placement
            float startingBiomeBegin = 0.25f - startingAreaAnglePercent / 2.0f;
            float startingBiomeEnd = 0.25f + startingAreaAnglePercent / 2.0f;
            m_Biomes[startingIndex].m_End = startingBiomeEnd;
            for (int i=0;i< startingIndex; i++)
            {
                m_Biomes[i].m_End = startingBiomeBegin * ((i + 1.0f) / startingIndex);
            }

            int indexAfterStart = startingIndex + 1;
            int biomesLeft = m_NumBiomes - indexAfterStart;
            for (int i=0;i< biomesLeft; i++)
            {
                m_Biomes[i + indexAfterStart].m_End = startingBiomeEnd + ((i + 1.0f) / biomesLeft);
            }
        }
        Biome GetBiomeFromPercentTheta(float inputPct)
        {
            var pct = Mathf.Clamp01((inputPct + 1.0f + m_BiomeThetaOffset) % 1.0f);
            float lastPct = 0.0f;
            for (int i = 0; i < m_NumBiomes; i++)
            {
                if (pct > lastPct && pct <= m_Biomes[i].m_End)
                {
                    return m_Biomes[i];
                }

                lastPct = m_Biomes[i].m_End;
            }

            // Should not happen if inputPct is between 0 and 1.
            return m_Biomes[0];
        }
        */

            public IntVector2 GetGridAtPercentAngle(float radpct, float pct)
            {
                int y = m_Radius + Mathf.RoundToInt(1.0f * radpct * m_Radius * Mathf.Cos(pct * Mathf.PI * 2.0f));
                int x = m_Radius + Mathf.RoundToInt(1.0f * radpct * m_Radius * Mathf.Sin(pct * Mathf.PI * 2.0f));

                return new IntVector2(x, y);
            }

            void AddLayers()
            {
                float middleZ = 1.0f;
                var layer = new Layer(m_Gen) { DigPower = 1.0f, Z = middleZ + 0.0f, Background = false };
                m_Layers.Add(layer);
                m_Layers.Add(new Layer(m_Gen) { DigPower = 2.0f, Z = middleZ + -0.1f, Background = false });
                m_Layers.Add(new Layer(m_Gen) { DigPower = 3.0f, Z = middleZ + 1.1f, Background = true });
            }

            float ComputePercentDone(int stage, int iteration, int iterationMax)
            {
                var result = (stage + (1.0f * iteration) / iterationMax) / 4.0f;
                if (result > 1.0f)
                {
                    UnityEngine.Debug.Log("ASDF");
                }
                return result;
            }

            float m_Circumference;
            int m_Radius;
            public IEnumerable<float> Generate()
            {
                AddLayers();

                //  Guess initial starting point (it will get moved down due to smoothing but this initial value is useful for marking before hand)
                m_StartPos = new IntVector2(m_Gen.Dimension.X / 2, Mathf.RoundToInt((0.5f + 0.5f * startingAreaHeight) * m_Gen.Dimension.Y));

                // Create our flags for marking each cell for whatever reason.
                m_Flags = new byte[m_Gen.m_DimensionX, m_Gen.m_DimensionY];

                m_Circumference = Mathf.PI * m_Gen.m_DimensionX;
                m_Radius = m_Gen.m_DimensionX / 2;

                // Allows perlin noise to be shifted a random amount so we get different pattern each time
                float xOrg = m_Rng.Range(1.0f, 10.0f) * 10.0f;
                float yOrg = m_Rng.Range(1.0f, 10.0f) * 10.0f;
                float xOrg2 = m_Rng.Range(1.0f, 10.0f) * 10.0f;
                float yOrg2 = m_Rng.Range(1.0f, 10.0f) * 10.0f;

                foreach (var f in GenerateHeightsRound(xOrg, yOrg)) yield return f;
                foreach (var f in GenerateBiomes()) yield return f;



                {
                    RadialGravity.m_RadialEnabled = true;
                    foreach (var f in GenerateRound(m_Layers[0], m_Layers[1], m_Layers[2], xOrg, yOrg, xOrg2, yOrg2)) yield return f;

                    for(int i=0;i<3;i++)
                    {
                        var layer = m_Layers[i];
                        var filter = new Gaussian5x5BlurFilter();
                        var values2 = layer.m_Values.ConvolutionFilter<float, Gaussian5x5BlurFilter>(m_Gen.m_ClipX0, m_Gen.m_ClipY0, m_Gen.m_ClipX1, m_Gen.m_ClipY1, filter);
                        var values3 = values2.ConvolutionFilter<float, Gaussian5x5BlurFilter>(m_Gen.m_ClipX0, m_Gen.m_ClipY0, m_Gen.m_ClipX1, m_Gen.m_ClipY1, filter);
                        layer.m_Values = values3;
                        yield return ComputePercentDone(3, i, 3);
                    }
                }

                // Put start pos one up of actual ground.
                int timeout = 100;
                while (timeout>0 && m_Gen.IsPointClearAt_gs(m_StartPos))
                {
                    timeout--;
                    m_StartPos.Y = m_StartPos.Y - 1;
                }
                m_Gen.StartingPoint = m_Gen.GridSpaceToWorld(new Vector3(m_StartPos.X, m_StartPos.Y));

                // Everything is dirty now
                m_Gen.ResetBlocksClean();
            }

            PVoxelGenerator m_Gen;
        }

        bool m_PlacedStuff = false;
        public override void VirtualUpdate()
        {
            if(m_GeneratorProgress != null)
            {
                if(!m_GeneratorProgress.MoveNext())
                {
                    m_GeneratorProgress = null;
                }
            }

            if (DoneGenerating && !m_PlacedStuff)
            {
                m_PlacedStuff = true;
                m_Generator.PlaceStuff();
                LightingDataInstance.ComputeInitialLighting();
            }
        }

        public void OnGUI()
        {
            if (m_GeneratorProgress != null)
            {
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GuiExt.Label2(new Rect(100, 100, 100, 100), string.Format("Generating level {0}%", m_GeneratorProgress.Current));
            }
        }

        public bool DoneGenerating
        {
            get
            {
                return m_GeneratorProgress == null;
            }
        }

        IEnumerator<float> m_GeneratorProgress;
        protected override void VirtualAwake()
        {
            m_Generator = new EarthGenerateRound(this, m_Layers);
            m_GeneratorProgress = m_Generator.Generate().GetEnumerator();
            /*
            do
            {
                var value = m_GeneratorProgress.Current;
            }
            while(m_GeneratorProgress.MoveNext());
            m_GeneratorProgress = null;
            */
        }

    }
}