using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.Serialization;

namespace Planet
{
    public class RecipeDatabase : MonoBehaviour
    {
        [System.Serializable]
        public class Ingredient
        {
            public string m_Name;
            public int m_Count;
        }

        [System.Serializable]
        public class Entry
        {
            public string m_Name;
            [HideInInspector][NonSerialized]
            public string[] m_RequiredStations;
            [HideInInspector][NonSerialized]
            public Ingredient[] m_Input;
            [HideInInspector][NonSerialized]
            public Ingredient[] m_Output;
            public string m_Desc;

            public void FromString(string desc)
            {
                if (desc == null) return;

                var sections = desc.Split(';');
                foreach(var section in sections)
                {
                    var equalsSplit = section.Split('=');
                    if (equalsSplit[0] == "station")
                    {
                        var requiredStations = new List<string>();
                        m_RequiredStations = equalsSplit[1].Split('+');
                    }
                    else
                    {
                        // Assume it's the normal recipe part
                        var inputSplit = equalsSplit[0].Split('+');
                        var outputSplit = equalsSplit[1].Split('+');
                        var inputs = new List<Ingredient>();
                        var outputs = new List<Ingredient>();
                        foreach(var input in inputSplit)
                        {
                            var spaceSplit = input.Split(' ');
                            inputs.Add(new Ingredient() { m_Name = spaceSplit[1], m_Count = int.Parse(spaceSplit[0]) });
                        }
                        foreach (var output in outputSplit)
                        {
                            var spaceSplit = output.Split(' ');
                            outputs.Add(new Ingredient() { m_Name = spaceSplit[1], m_Count = int.Parse(spaceSplit[0]) });
                        }
                        m_Input = inputs.ToArray();
                        m_Output = outputs.ToArray();
                    }
                }
            }
            public override string ToString()
            {
                string desc = "";
                for (int i=0;i<m_Input.Length;i++)
                {
                    var input = m_Input[i];
                    desc += input.m_Count + " " + input.m_Name;
                    if (i != (m_Input.Length - 1))
                    {
                        desc += "+";
                    }
                }
                desc += "=";
                for (int i = 0; i < m_Output.Length; i++)
                {
                    var output = m_Output[i];
                    desc += output.m_Count + " " + output.m_Name;
                    if (i != (m_Output.Length - 1))
                    {
                        desc += "+";
                    }
                }
                if (m_RequiredStations.Length > 0)
                {
                    desc += ";station=";
                    for (int i = 0; i < m_RequiredStations.Length; i++)
                    {
                        var station = m_RequiredStations[i];
                        desc += station;
                        if (i != (m_RequiredStations.Length - 1))
                        {
                            desc += ",";
                        }
                    }
                }
                return desc;
            }
        }

        [System.Serializable]
        public struct Category
        {
            public string m_Name;
            public Entry[] m_Entries;
        }

        public Category[] m_Categories;

        public static RecipeDatabase m_Instance;
        public RecipeDatabase()
        {
            m_Instance = this;
        }

        public static List<Entry> GetRecipesInvolvingStatic(string name, List<string> availableStations)
        {
            return m_Instance.GetRecipesInvolving(name, availableStations);
        }

        public List<Entry> GetRecipesInvolving(string name, List<string> availableStations)
        {
            List<Entry> result = new List<Entry>();

            foreach(var cat in m_Categories)
            {
                foreach(var entry in cat.m_Entries)
                {
                    bool isInvolved = false;
                    foreach(var input in entry.m_Input)
                    {
                        if (input.m_Name.ToLower() == name.ToLower()) isInvolved = true;
                    }

                    if (isInvolved)
                    result.Add(entry);
                }
            }

            return result;
        }

        void VerifyItem(string cat, string name, string item)
        {
            var entry = ItemDatabase.GetEntryStatic(item);
            if (entry == null)
            {
                Debug.LogError(String.Format("BAD RECIPE: {0}/{1} NOT FOUND:{2}", cat, name, item));
            }
        }

        public void VerifyRecipes()
        {
            foreach (var cat in m_Categories)
            {
                foreach (var entry in cat.m_Entries)
                {
                    entry.FromString(entry.m_Desc);
                    foreach (var input in entry.m_Input)
                    {
                        VerifyItem(cat.m_Name, entry.m_Name, input.m_Name);
                    }
                    foreach (var output in entry.m_Output)
                    {
                        VerifyItem(cat.m_Name, entry.m_Name, output.m_Name);
                    }
                }
            }
        }

        public void Start()
        {
            VerifyRecipes();
        }
    }
}