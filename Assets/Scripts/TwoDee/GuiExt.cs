using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;
using System.Collections;
using System.Xml;
using System.Xml.Schema;
using System.IO;

namespace TwoDee
{
    class GlowGuiColor
    {
        Color m_OldColor;
        Color m_OldContent;
        bool m_DoIt;
        public GlowGuiColor(bool doIt = true)
        {
            m_DoIt = doIt;
            if (m_DoIt)
            {
                m_OldContent = GUI.contentColor;
                m_OldColor = GUI.color;

                float f = 0.5f + 0.5f * (1.0f + Mathf.Sin(3.0f * Time.realtimeSinceStartup));
                Color c = new Color(f, f, f, 1.0f);
                GUI.contentColor = GUI.color = c;
            }
        }
        public void End()
        {
            if (m_DoIt)
            {
                GUI.contentColor = m_OldContent;
                GUI.color = m_OldColor;
            }
        }
    }

    public class GuiExt
    {
        public static void Label2(Rect position, string text, GUIStyle gs)
        {
            Rect orig = new Rect(position);
            position.x += 1;
            position.y += 1;
            Color oldColor = gs.normal.textColor;
            gs.normal.textColor = Color.black;
            GUI.Label(position, text, gs);
            gs.normal.textColor = oldColor;
            GUI.Label(orig, text, gs);
        }

        public static void Label2(Color c, Rect position, string text)
        {
            var gs = new GUIStyle();
            gs.normal.textColor = c;
            Label2(position, text, gs);
        }
        public static void Label2(Rect position, string text)
        {
            Label2(Color.white, position, text);
        }
    }
}