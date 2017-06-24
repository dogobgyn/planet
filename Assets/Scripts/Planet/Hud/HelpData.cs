using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;

namespace Planet
{
    public class HelpData : MonoBehaviour
    {
        // Maybe load this from a file or something
        class Topic
        {
            public string m_Index;
            public string m_Topic;
            public string m_Desc;

            public bool m_Revealed;
            public bool m_New;

            public void Reveal()
            {
                if(!m_Revealed)
                {
                    m_Revealed = true;
                    m_New = true;
                }
            }
        }

        public bool AnyNew
        {
            get
            {
                foreach (var entry in m_HelpTopics)
                {
                    if (entry.m_New) return true;
                }

                return false;
            }
        }

        Topic CreateTopic(string topic, string desc)
        {
            return new Topic() { m_Index = topic.ToLower(), m_Topic = topic, m_Desc = desc };
        }

        List<Topic> m_HelpTopics = new List<Topic>();
        void LoadHelpTopics()
        {
            m_HelpTopics.Add(CreateTopic("Intro", "Oh, no!  Your ship has crashed on an unexplored planet and you will need to build another one.  Better take a look around and scout out some resources.\nUse WASD to move, click on nearby objects to use/take"));
            m_HelpTopics.Add(CreateTopic("Ship", "Your ship has crashed, but it's not totally useless, including being a safe place to rest.  Click on the sphere when nearby to sleep and restore health.  Resource collection systems are also online."));
            m_HelpTopics.Add(CreateTopic("Tools", "To begin with you have rocket boots and a tractor beam.  They both need energy to run so watch their change which can be restored by the ship or other means later.\n\nHINT:You can easily get stuck without your boots' energy and will have to put yourself out of your misery (Sudoku button)"));

            m_HelpTopics.Add(CreateTopic("Crafting", "Your inventory is in the upper right corner.  You can drag items in and out of it and craft new items if you have an available station.  Click on an item and click 'Crafting' to show the UI.  Click on the result you want to craft."));
            m_HelpTopics.Add(CreateTopic("MCU", "Your ship also contains a Matter Conversion Unit that gives extra recipes if you stand by it.  If you are short on certain materials, you can swap them... for a cost."));

            m_HelpTopics.Add(CreateTopic("Resources", "You'll need wood and metal to construct more.  Chop down trees and dig into the earth to loosen ore.  Use your tractor beam to bring the heavy resources back to your base where your ship can process them.  Later on you can build your own processors.\n\nHINT: Gravity is a hash mistress.  Construct tunnels with gentle slopes to easily move things through.\n\nHINT: Use the tractor beam around the edge of the object for extra torque power"));
            m_HelpTopics.Add(CreateTopic("Items", "Select an item with 1-9 or clicking and click to equip it.  Press escape to unselect any item.  Tools can be used by clicking (right button for alt fire) while you have them equipped."));
            m_HelpTopics.Add(CreateTopic("Time", "Extertive tool use takes time, which is listed when you equip it.  Your ship will be assaulted every 5 days, so keep expensive time costs in mind."));

            m_HelpTopics.Add(CreateTopic("Night", "Night is a more dangerous time when more creatures appear.  Sleeping may be a good way to avoid it but if you're short on time you may need to work through it."));

            m_HelpTopics.Add(CreateTopic("Wormholes", "If you dig up teleportation ore, you can use its unstable connection to teleport from it to where you found it.  If you harvest it, the link will close but valuable things can be made with that ore."));

            m_HelpTopics.Add(CreateTopic("Experience", "One common reward from creatures is experience that can be used to construct upgrades or more advanced items.  The copper former can be a good choice in order to make smaller items with copper parts."));
            m_HelpTopics.Add(CreateTopic("Energy", "Your base has a fusion reactor that generates infinite energy.  When by your base, any energy using items charge.  You can hold more energy with batteries."));
            m_HelpTopics.Add(CreateTopic("Collectors", "In addition to your base you can also make other collectors to harvest logs and ore.  Your base has infinite power but these require energy to run (connect copper cables or create batteries.)  By making new collectors you make a much easier place to collect some resources and don't jostle them around and lose part of the resources."));
        }

        int m_currentTopicCheck = 0;
        void UnlockTopicsStep()
        {
            var player = TwoDee.ComponentList.GetFirst<ThirdPersonUserControl>();
            if (player == null) return;

            var entry = m_HelpTopics[m_currentTopicCheck];
            bool unlock = false;
            if (!entry.m_Revealed)
            {
                if (entry.m_Index == "crafting" || entry.m_Index == "mcu")
                {
                    var playerInventory = player.Inventory;
                    if (playerInventory.ContainsAny("stick", "rock", "fiber", "wood")) unlock = true;
                }
                else if (entry.m_Index == "resources" || entry.m_Index == "items" || entry.m_Index == "time")
                {
                    var playerInventory = player.Inventory;
                    if (playerInventory.ContainsAny("axe", "rope", "shovel")) unlock = true;
                }
                else if (entry.m_Index == "night")
                {
                    var worldData = WorldState.Instance;
                    if (worldData.CurrentTime > 12) unlock = true;
                }
                else if (entry.m_Index == "wormholes")
                {
                    foreach (var ore in TwoDee.ComponentList.GetCopiedListOfType<Teleporter>())
                    {
                        if ((ore.transform.position - player.transform.position).magnitude < 5.0f)
                        {
                            unlock = true;
                        }
                    }
                }
                else if (entry.m_Index == "experience" || entry.m_Index == "energy" || entry.m_Index == "collectors")
                {
                    var playerInventory = player.Inventory;
                    if (playerInventory.ContainsAny("experience")) unlock = true;
                }

                if (unlock)
                {
                    entry.Reveal();
                }
            }

            m_currentTopicCheck = (m_currentTopicCheck + 1) % m_HelpTopics.Count;
        }

        private void FixedUpdate()
        {
            UnlockTopicsStep();
        }

        public HelpData()
        {
            LoadHelpTopics();
            RevealTopic("intro", "ship", "tools");

            Instance = this;
        }

        Rect m_HelpMenuRect = new Rect(20, 20, 500, 500);
        Action m_CloseButtonAction;
        public Texture2D m_TextBackground;
        void DoHelpWindow(int windowID)
        {
            GUIStyle textStyle = new GUIStyle() { wordWrap=true, border = new RectOffset(-5,-5,-5,-5) };
            textStyle.normal.background = m_TextBackground;

            Rect curPos = new Rect(10, 20, 100, 20);
            for(int i=0; i<m_HelpTopics.Count; i++)
            {
                var entry = m_HelpTopics[i];
                if (m_SelectedTopic == i)
                {
                    GUI.TextField(new Rect(120, 20, 300, 400), entry.m_Desc, textStyle);
                }
                if (entry.m_Revealed)
                {
                    var gc = new TwoDee.GlowGuiColor(entry.m_New);
                    if (GUI.Button(curPos, entry.m_Topic))
                    {
                        entry.m_New = false;
                        m_SelectedTopic = i;
                    }
                    gc.End();
                }
                curPos.y += 30;
            }
            if (GUI.Button(curPos, "Close"))
            {
                m_CloseButtonAction();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        public int m_SelectedTopic = -1;
        public void ActivateGuiHelp()
        {
            m_SelectedTopic = -1;
        }
        public void OnGUIHelp(Action closeButtonAction)
        {
            m_CloseButtonAction = closeButtonAction;

            m_HelpMenuRect = GUI.Window(0, m_HelpMenuRect, DoHelpWindow, "Help");
        }

        void RevealTopic(string index)
        {
            foreach(var entry in m_HelpTopics)
            {
                if(entry.m_Index == index)
                {
                    entry.Reveal();
                    break;
                }
            }
        }
        void RevealTopic(params string[] values)
        {
            foreach(var value in values)
            {
                RevealTopic(value);
            }
        }
        
        public static HelpData Instance
        {
            private set; get;
        }


    }
}