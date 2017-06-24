using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.Serialization;
using TwoDee;

namespace Planet
{
    public class ItemDatabase : MonoBehaviour
    {
        [System.Serializable]
        public class StaticPropsEntry
        {
            public string m_Name;
            public string m_Value;
        }

        [System.Serializable]
        public class StaticProps
        {
            public StaticPropsEntry[] m_Entries;
        }

        [System.Serializable]
        public class GeneratePropsEntry
        {
            public string m_Name;
            public float m_Low;
            public float m_High;
        }

        [System.Serializable]
        public class GenerateProps
        {
            public GeneratePropsEntry[] m_Entries;
            public static SerializableDictionary<string,string> Generate(string item, GenerateProps props)
            {
                if (props == null || props.m_Entries.Length == 0) return null;

                var result = new SerializableDictionary<string, string>();
                foreach (var entry in props.m_Entries)
                {
                    result[entry.m_Name] = UnityEngine.Random.Range(entry.m_Low, entry.m_High).ToString();
                }

                return result;
            }
        }

        [System.Serializable]
        public class Entry
        {
            public string m_Name;
            public Sprite m_Icon;
            public Item m_Item;
            public string m_Props;

            public int Stacking
            {
                get
                {
                    if (m_Stacking == 0) return 100; // Used to mean the standard amount
                    return m_Stacking;
                }
            }

            [HideInInspector][NonSerialized]
            public int m_Stacking;
            [HideInInspector][NonSerialized]
            public GenerateProps m_GenerateProps;
            [HideInInspector][NonSerialized]
            public StaticProps m_StaticProps;

            public void ParseProps()
            {
                var sections = m_Props.Split(';');
                int finalStacking = 0;
                var staticProps = new List<StaticPropsEntry>();
                var generateProps = new List<GeneratePropsEntry>();
                foreach (var section in sections)
                {
                    var equalsSplit = section.Split('=');
                    // Need both sides of the equals
                    if (equalsSplit.Length < 2) continue;

                    if (equalsSplit[0] == "stacking")
                    {
                        finalStacking = int.Parse(equalsSplit[1]);
                    }
                    else // random prop
                    {
                        if(equalsSplit[1].Contains("-"))
                        {
                            var dashSplit = equalsSplit[1].Split('-');
                            if (finalStacking == 0) finalStacking = 1; //props are generated so we can't stack this item
                            generateProps.Add(new GeneratePropsEntry() { m_Name = equalsSplit[0], m_Low = float.Parse(dashSplit[0]),m_High = float.Parse(dashSplit[1])  });
                        }
                        else
                        {
                            staticProps.Add(new StaticPropsEntry() { m_Name = equalsSplit[0], m_Value = equalsSplit[1] });
                        }
                    }
                }
                m_Stacking = finalStacking;
                m_GenerateProps = new GenerateProps() { m_Entries = generateProps.ToArray() };
                m_StaticProps = new StaticProps() { m_Entries = staticProps.ToArray() };
            }

            public bool GetCombinedPropBool(string key, SerializableDictionary<string, string> generatedProps)
            {
                return GetCombinedPropFloat(key,generatedProps) != 0.0f;
            }
            public float GetCombinedPropFloat(string key, SerializableDictionary<string, string> generatedProps)
            {
                float result = 0;
                string str = GetCombinedProp(key, generatedProps);
                if (str != null)
                {
                    float.TryParse(str, out result);
                }

                return result;
            }

            public string GetCombinedProp(string key, SerializableDictionary<string,string> generatedProps)
            {
                string result = null;
                if (generatedProps != null)
                {
                    if (generatedProps.TryGetValue(key, out result))
                        return result;
                }

                if (m_StaticProps.m_Entries != null)
                {
                    foreach(var entry in m_StaticProps.m_Entries)
                    {
                        if (entry.m_Name == key) return entry.m_Value;
                    }
                }

                return result;
            }
        }

        [System.Serializable]
        public class Category
        {
            public string m_Name;
            public Entry[] m_Entries;
        }

        Csv m_HelpTable;
        Csv HelpTable
        {
            get
            {
                if (m_HelpTable == null)
                {
                    m_HelpTable = new Csv("itemhelp");
                }

                return m_HelpTable;
            }
        }

        public string GetHelpString(string itemName)
        {
            var entry = HelpTable.GetEntryByName(itemName);
            if (entry == null) return "";
            return entry.GetColumn("help");
        }

        public string GetPrettyNameString(string itemName)
        {
            var entry = HelpTable.GetEntryByName(itemName);
            if (entry == null) return "*"+itemName;
            return entry.GetColumn("prettyname");
        }

        public Category[] m_Categories;

        public static int GetStackingStatic(string item)
        {
            return Instance.GetStacking(item);
        }
        public static Sprite GetIconStatic(string item)
        {
            return Instance.GetIcon(item);
        }
        public static Item GetItemStatic(string item)
        {
            return Instance.GetItem(item);
        }
        public static Entry GetEntryStatic(string item)
        {
            if (Instance == null)
            {
                Debug.Log("whoops\n");
            }
            return Instance.GetEntry(item);
        }

        public int GetStacking(string item)
        {
            var entry = GetEntry(item);
            if (entry != null)
            {
                
                return entry.Stacking;
            }

            return 1;
        }

        public Sprite GetIcon(string item)
        {
            var entry = GetEntry(item);
            if (entry != null)
            {
                return entry.m_Icon;
            }
            return null;
        }

        public Item GetItem(string item)
        {
            var entry = GetEntry(item);
            if (entry != null)
            {
                return entry.m_Item;
            }
            return null;
        }

        public Entry GetEntry(string item)
        {
            var itemLower = item.ToLower();
            foreach(var c in m_Categories)
            {
                foreach(var e in c.m_Entries)
                {
                    if (e.m_Name.ToLower() == itemLower)
                    {
                        return e;
                    }
                }
            }

            return null;
        }

        public static ItemDatabase Instance
        {
            private set; get;
        }

        public void VerifyItems()
        {
            foreach (var cat in m_Categories)
            {
                foreach (var entry in cat.m_Entries)
                {
                    entry.ParseProps();
                }
            }
        }

        public void Awake()
        {
            Instance = this;
            VerifyItems();
        }
    }
}