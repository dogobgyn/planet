using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TwoDee
{
    public class FloatingTextManager : MonoBehaviour
    {
        class Entry
        {
            public string m_Text;
            public float m_TimeLeft;
            public Vector3 m_Pos_ws;
        }

        public static FloatingTextManager Instance
        {
            set; get;
        }

        FloatingTextManager()
        {
            Instance = this;
        }

        List<Entry> m_Entries = new List<Entry>();

        public void AddEntry(string text, Vector3 pos_ws, float timeLeft=1.0f)
        {
            // Limit number of entries
            if (m_Entries.Count > 10)
            {
                m_Entries.RemoveAt(0);
            }

            m_Entries.Add(new Entry { m_Pos_ws = pos_ws, m_Text = text, m_TimeLeft = timeLeft });
        }

        public void OnGUI()
        {
            foreach(var entry in m_Entries)
            {
                GUIStyle gs = new GUIStyle();
                gs.normal.textColor = Color.white;
                Rect r = new Rect();
                var sp = Camera.main.WorldToScreenPoint(entry.m_Pos_ws);
                r.x = sp.x;
                r.y = Screen.height - sp.y;
                r.width = 800.0f;
                r.height = 400.0f;
                TwoDee.GuiExt.Label2(r, entry.m_Text, gs);
            }
        }

        void Update()
        {            
            var newEntries = new List<Entry>();

            foreach (var entry in m_Entries)
            {
                entry.m_TimeLeft -= Time.deltaTime;
                if (entry.m_TimeLeft > 0.0f)
                {
                    newEntries.Add(entry);
                }
            }

            m_Entries = newEntries;
        }
    }

}