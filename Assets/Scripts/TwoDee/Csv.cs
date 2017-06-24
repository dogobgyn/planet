using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.Rendering;


namespace TwoDee
{
    public class Csv
    {
        public int Count
        {
            get
            {
                return m_Entries.Count;
            }
        }
        public List<Entry> m_Entries = new List<Entry>();
        public class Entry
        {
            Csv m_Csv;
            string[] m_Columns;
            public int ColumnCount
            {
                get
                {
                    return m_Columns.Length;
                }
            }

            public Entry(string[] columns, Csv csv)
            {
                m_Columns = columns;
                m_Csv = csv;
            }

            public string GetColumn(string columnName)
            {
                int i = m_Csv.ColumnNameToIndex(columnName);
                if (i >= m_Columns.Length) return "";
                return m_Columns[i];
            }

            public int GetColumnInt(string columnName)
            {
                int i = m_Csv.ColumnNameToIndex(columnName);
                if (i >= m_Columns.Length) return 0;
                return int.Parse(m_Columns[i]);
            }

            public float GetColumnFloat(string columnName)
            {
                int i = m_Csv.ColumnNameToIndex(columnName);
                if (i >= m_Columns.Length) return 0.0f;
                return float.Parse(m_Columns[i]);
            }

            string GetColumn(int i)
            {
                if (i >= m_Columns.Length) return "";
                return m_Columns[i];
            }

            int GetColumnInt(int i)
            {
                if (i >= m_Columns.Length) return 0;
                return int.Parse(m_Columns[i]);
            }

            float GetColumnFloat(int i)
            {
                if (i >= m_Columns.Length) return 0.0f;
                return float.Parse(m_Columns[i]);
            }
        }

        int ColumnNameToIndex(string name)
        {
            return m_NamesToColumns[name];
        }

        Dictionary<string, int> m_NamesToColumns = new Dictionary<string, int>();
        Dictionary<string, Entry> m_EntriesByName = new Dictionary<string, Entry>();

        public Entry GetEntryByName(string name)
        {
            Entry result;
            if(m_EntriesByName.TryGetValue(name, out result))
            {
                return result;
            }
            return null;
        }

        public Csv(string name) : this(Resources.Load<TextAsset>("Text/" + name))
        {
            
        }

        public Csv(TextAsset asset)
        {
            var text = asset.text;
            string[] newlineSplit = text.Split(new string[] { "\n", "\r\n" }, System.StringSplitOptions.RemoveEmptyEntries);
            for(int i=0;i< newlineSplit.Length; i++)
            {
                if (newlineSplit[i].Trim().Length == 0) continue;
                var columns = newlineSplit[i].Split(',');
                if (i == 0)
                {
                    for (int j=0;j<columns.Length;j++)
                    {
                        m_NamesToColumns[columns[j]] = j;
                    }
                }
                else
                {
                    var entry = new Entry(columns, this);
                    m_Entries.Add(entry);
                    var entryName = entry.GetColumn("name");
                    if (entryName.Length > 0)
                    {
                        m_EntriesByName[entryName] = entry;
                    }
                    
                }
            }
        }
    }
}
