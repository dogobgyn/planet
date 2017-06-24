using System;
using System.Collections.Generic;
using TwoDee;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace Planet
{
    public interface IInventoryButtonAccumulator
    {
        void AddButton(Rect r, Inventory i, int slot);
    }
    public static class IInventoryExtension
    {
        public static Inventory FirstInventory(this IInventory inv)
        {
            return inv.Inventories[0];
        }
    }

    public interface IInventory
    {
        Inventory[] Inventories
        {
            get;
        }
    }

    [System.Serializable]
    public class InventoryEntry
    {
        public string m_Name;
        public int m_Count;
        public SerializableDictionary<string, string> m_Properties = null;

        [NonSerialized]
        public float m_ChargingTime = -999;

        public InventoryEntry Clone()
        {
            return new InventoryEntry(m_Name, m_Count) { m_Properties = (null!=m_Properties ? m_Properties.CloneTyped() : null) };
        }

        public InventoryEntry()
        {
        }

        public InventoryEntry(string name, int count)
        {
            m_Name = name.ToLower();
            m_Count = count;
        }

        public float ComputeWeight()
        {
            var weight = DbEntry.GetCombinedPropFloat("weight", m_Properties);
            if (weight == 0.0f) weight = 0.1f;
            return m_Count * weight;
        }

        public string GetHelpString()
        {
            return ItemDatabase.Instance.GetHelpString(m_Name);
        }

        public string DebugString()
        {
            var mouseString = ItemDatabase.Instance.GetPrettyNameString(m_Name) + " ";
            if (m_Count > 1) mouseString += m_Count + " ";
            if (m_Properties != null)
            {
                foreach (var pair in m_Properties)
                {
                    mouseString += string.Format("[{0}={1}]", pair.Key, pair.Value);
                }
            }
            var item = ItemDatabase.GetItemStatic(m_Name);
            if (item != null && item.m_TimeCost != 0)
            {
                mouseString += string.Format("({0} min/use)", item.m_TimeCost);
            }

            return mouseString;
        }

        public ItemDatabase.Entry DbEntry
        {
            get
            {
                return ItemDatabase.GetEntryStatic(m_Name);
            }
        }

        public int Stacking
        {
            get
            {
                return DbEntry.Stacking;
            }
        }

        public bool HasProperty(string prop)
        {
            if (m_Properties != null && m_Properties.ContainsKey(prop)) return true;

            return false;
        }

        public float ChargeEnergy(float amount)
        {
            if (!HasProperty("energy")) return 0.0f;

            float oldAmount = GetProperty("energy");
            UseProperty("energy", -amount, 0, 100, true);
            float delta = GetProperty("energy") - oldAmount;
            if (delta > 0.0f)
            {
                m_ChargingTime = Time.time;
            }

            return delta;
        }

        public bool DrainEnergy(float amount, bool doIt)
        {
            if (HasProperty("energy") && (GetProperty("energy") > amount))
            {
                if (doIt)
                {
                    UseProperty("energy", amount, 0, 100, true);
                }
                return true;
            }

            return false;
        }

        public float GetProperty(string prop)
        {
            if (null == m_Properties || !m_Properties.ContainsKey(prop)) return 0.0f;
            float oldAmount;
            if (float.TryParse(m_Properties[prop], out oldAmount))
            {
                return oldAmount;
            }

            return 0.0f;
        }

        public void SetProperty(string prop, float newAmount)
        {
            if (null == m_Properties) m_Properties = new SerializableDictionary<string, string>();
            m_Properties[prop] = "" + newAmount;
        }

        public bool UseProperty(string prop, float amount, float min=0, float max=float.MaxValue, bool doIt=true)
        {
            float oldValue = GetProperty(prop);
            if (oldValue >= amount)
            {
                float newValue = Mathf.Clamp(oldValue - amount, min, max);
                if (doIt)
                {
                    SetProperty(prop, newValue);
                }
                return doIt;
            }

            return false;
        }
    }

    [Serializable]
    public class RanomInventoryGenEntry
    {
        public string m_Name;
        public float m_Chance = 1.0f;
        public string m_Item = "stick";
        public int m_Min = 10;
        public int m_Max = 10;
        public string[] m_IfChosenAlsoChoose;
    }

    [Serializable]
    public class Inventory
    {
        List<InventoryEntry> m_Inventory;

        public Inventory(int slots)
        {
            m_Inventory = new List<InventoryEntry>(new InventoryEntry[slots]);
        }

        public void ReplaceWith(Inventory other)
        {
            m_Inventory = other.m_Inventory;
        }

        public InventoryEntry GetSlot(int slot)
        {
            if (slot < 0 || slot >= m_Inventory.Count) return null;
            return m_Inventory[slot];
        }
        public InventoryEntry SetSlot(int slot, InventoryEntry entry)
        {
            m_Inventory[slot] = entry;
            return entry;
        }
        public void ClearSlot(int slot)
        {
            m_Inventory[slot] = null;
        }

        public int Count
        {
            get { return m_Inventory.Count;  }
        }

        public int CountTotalItems
        {
            get
            {
                int result = 0;
                for (int i = 0; i < m_Inventory.Count; i++)
                {
                    if (m_Inventory[i] != null)
                    {
                        result += m_Inventory[i].m_Count;
                    }
                }

                return result;
            }
        }

        public float ComputeWeight()
        {
            float result = 0.0f;

            for (int i = 0; i < m_Inventory.Count; i++)
            {
                if (m_Inventory[i] != null)
                {
                    result += m_Inventory[i].ComputeWeight();
                }
            }

            return result;
        }

        public bool Contains(string item)
        {
            for (int i = 0; i < m_Inventory.Count; i++)
            {
                if (m_Inventory[i] != null && m_Inventory[i].m_Name == item)
                {
                    return true;
                }
            }

            return false;
        }
        public bool ContainsAny(params string[] list)
        {
            bool result = false;
            foreach(var entry in list)
            {
                result |= Contains(entry);
            }
            return result;
        }

        public bool DrainEnergyInMainOrBackup(int mainSlot, float amount, bool doIt)
        {
            var slot = GetSlot(mainSlot);
            if (slot != null && slot.DrainEnergy(amount, doIt))
            {
                return true;
            }
            return DrainEnergy(amount, doIt, true);
        }

        public bool DrainEnergy(float amount, bool doIt, bool batteryOnly=false)
        {
            for (int i = 0; i < Count; i++)
            {
                var slot = GetSlot(i);
                if (slot != null)
                {
                    if (batteryOnly && slot.m_Name != "powercore") continue;

                    if (slot.DrainEnergy(amount, doIt))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void ChargeEnergy(float amount)
        {
            int thingsToDivideAmong = 0;
            for(int cycle=0;cycle<2;cycle++)
            {
                for (int i = 0; i < Count; i++)
                {
                    var slot = GetSlot(i);
                    if (slot != null && slot.HasProperty("energy") && (slot.GetProperty("energy") < 100.0f))
                    {
                        if (cycle == 0)
                        {
                            thingsToDivideAmong += 1;
                        }
                        else
                        {
                            slot.ChargeEnergy(amount / thingsToDivideAmong);
                        }
                    }
                }

                if (thingsToDivideAmong == 0)
                {
                    return;
                }
            }
        }

        public string RandomGenerate(IEnumerable<RanomInventoryGenEntry> entries, string name)
        {
            string lastEntry = null;
            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    if (entry.m_Name != name) continue;
                    float probability = UnityEngine.Random.Range(0.0f, 1.0f);
                    if (probability <= entry.m_Chance)
                    {
                        lastEntry = entry.m_Item;
                        int generatedAmount = UnityEngine.Random.Range(entry.m_Min, entry.m_Max + 1);
                        for (int i = 0; i < generatedAmount; i++)
                        {
                            AddInventory(entry.m_Item, true);
                        }

                        if(entry.m_IfChosenAlsoChoose != null)
                        {

                            foreach (string str in entry.m_IfChosenAlsoChoose)
                            {
                                var result = RandomGenerate(entries, str);
                                if (lastEntry == null) lastEntry = result;
                            } 
                            
                        }
                    }
                }
            }

            return lastEntry;
        }

        public class StaticSprites
        {
            public Sprite m_Border;
            public Sprite m_LightBorder;
            public Sprite m_Charging;
            public Sprite m_LowBattery;
            public Sprite m_PowerOff;
            public Sprite m_KeyIcon;
        }

        public void OnGuiSingleEntry( Rect r, StaticSprites sprites, bool selected, InventoryEntry entry, int slot, bool showKeys, IInventoryButtonAccumulator accum)
        {
            GUIStyle invNumberGs = new GUIStyle();
            invNumberGs.fontSize = 8;

            if (sprites.m_LightBorder != null && sprites.m_Border != null)
            {
                Rect rborder = r.Expand(3, 3);
                var btex = sprites.m_Border.texture;
                if (!selected) btex = sprites.m_LightBorder.texture;
                GUI.DrawTexture(rborder, btex);
                if(accum != null) accum.AddButton(rborder, this, slot);
            }

            // show keynum
            if (showKeys && sprites.m_KeyIcon != null && slot < 10)
            {
                Rect rcorner = r;
                rcorner.x -= rcorner.width / 2;
                rcorner.y -= rcorner.height / 2;
                GUI.DrawTexture(rcorner, sprites.m_KeyIcon.texture);
                rcorner.x += 6;
                GuiExt.Label2(rcorner, "" + ((1+slot)%10));
            }

            if (entry != null)
            {
                var tex = ItemDatabase.GetIconStatic(entry.m_Name).texture;
                GUI.DrawTexture(r, tex);

                Sprite chargingTex = null;
                if (entry.HasProperty("energy"))
                {
                    var energy = entry.GetProperty("energy");
                    if (energy <= 1.0f)
                    {
                        chargingTex = sprites.m_PowerOff;
                    }
                    else if (energy < 20.0f)
                    {
                        chargingTex = sprites.m_LowBattery;
                    }
                    
                    if (Time.time - entry.m_ChargingTime < 1.0f)
                    {
                        chargingTex = sprites.m_Charging;
                    }
                }

                if (chargingTex != null)
                {
                    if (Mathf.Sin(10.0f*Time.time) > 0.0f)
                    {
                        GUI.DrawTexture(r, chargingTex.texture);
                    }
                }

                //GUI.Button(r, new GUIContent(tex));
                // Count
                Rect r2 = new Rect(r.xMax - 5, r.yMax - 5, 200.0f, 30.0f);
                if (entry.m_Count > 1)
                {
                    invNumberGs.normal.textColor = Color.black;
                    TwoDee.GuiExt.Label2(r2, entry.m_Count + "", invNumberGs);
                    invNumberGs.normal.textColor = Color.white;
                    r2.x -= 1; r2.y -= 1;
                    TwoDee.GuiExt.Label2(r2, entry.m_Count + "", invNumberGs);
                }
            }
        }

        public int OnGuiInventory(int singleItemWithBorderGapDim, Rect r, StaticSprites sprites, int selectedSlot, bool showKeys, IInventoryButtonAccumulator accum)
        {
            GUIStyle invNumberGs = new GUIStyle();
            invNumberGs.fontSize = 8;
            float origX = r.x;
            for (int i = 0; i < m_Inventory.Count; i++)
            {
                if (i > 0 && i % 10 == 0)
                {
                    r.x = origX;
                    r.y += singleItemWithBorderGapDim;
                }

                var entry = m_Inventory[i];
                OnGuiSingleEntry(r, sprites, selectedSlot == i, entry, i, showKeys, accum);

                r.x += singleItemWithBorderGapDim;
            }
            return (m_Inventory.Count + 9) / 10;
        }

        public static bool HasAtLeast(Inventory[] inventories, string item, int count)
        {
            int total = 0;
            foreach (var inventory in inventories)
            {
                total += inventory.CountItem(item);
            }

            return total >= count;
        }

        public int CountItem(string item)
        {
            int total = 0;
            foreach (var entry in m_Inventory)
            {
                if (entry != null && entry.m_Name == item)
                {
                    total += entry.m_Count;
                }
            }

            return total;
        }

        public void AddStuff(string stuff)
        {
            AddStuff(stuff.Split(';'));
        }

        public void AddStuff(IEnumerable<string> stuff)
        {
            foreach (string itemOrig in stuff)
            {
                string item = itemOrig.ToLower();
                if (item.Trim().Length == 0) continue;

                int count = 1;
                string itemName = item;
                var foundSpace = item.IndexOf(' ');
                if (foundSpace >= 0)
                {
                    count = int.Parse(item.Substring(foundSpace + 1));
                    itemName = item.Substring(0, foundSpace);
                }
                else
                {
                    count = 1;
                }
                for (int i = 0; i < count; i++)
                {
                    AddInventory(itemName, true);
                }
            }
        }

        public static int AddInventory(Inventory[] inventories, InventoryEntry newEntry, bool doIt)
        {
            int countLeft = 0;
            foreach (var inventory in inventories)
            {
                countLeft = inventory.AddInventory(newEntry, doIt);
                if (countLeft == 0) return 0;
                newEntry.m_Count = countLeft;
            }

            return countLeft;
        }

        public int AddInventory(InventoryEntry newEntry, bool doIt)
        {
            int countLeft = newEntry.m_Count;
            if (newEntry.m_Properties != null)
            {
                // We require a full slot.
                for (int i=0;i<m_Inventory.Count;i++)
                {
                    if (m_Inventory[i] == null)
                    {
                        if (doIt)
                        {
                            m_Inventory[i] = newEntry;
                        }
                        return 0;
                    }
                }
            }
            else
            {
                // Generic item; if we have enough stacking room for it we're fine.
                countLeft = newEntry.m_Count;
                int stacking = ItemDatabase.GetStackingStatic(newEntry.m_Name);

                for (int pass=0;pass<2;pass++)
                {
                    for (int i = 0; i < m_Inventory.Count; i++)
                    {
                        var entry = m_Inventory[i];

                        int numAvail = 0;
                        // Try to stack first, if that fails go for empty slots
                        if (pass == 0 && entry != null && entry.m_Name == newEntry.m_Name)
                        {
                            numAvail = (stacking - entry.m_Count);
                        }
                        else if (pass == 1 && entry == null)
                        {
                            numAvail = stacking;
                        }

                        if (numAvail > countLeft) numAvail = countLeft;
                        countLeft -= numAvail;
                        if (doIt)
                        {
                            if (entry != null)
                            {
                                entry.m_Count += numAvail;
                            }
                            else if (numAvail > 0)
                            {
                                m_Inventory[i] = new InventoryEntry(newEntry.m_Name, numAvail);
                            }
                        }

                        if (countLeft == 0) return 0;
                    }

                }
            }

            return countLeft;
        }

        public static bool AddInventory(Inventory[] inventories, string item, bool doIt)
        {
            foreach(var inventory in inventories)
            {
                if (inventory.AddInventory(item, doIt)) return true;
            }

            return false;
        }

        public bool AddInventory(string item, bool doIt)
        {
            // Do we need to construct a new instance?
            var itemEntry = ItemDatabase.GetEntryStatic(item);
            if (itemEntry == null)
            {
                Debug.LogError("AddInventory no entry for " + item);
                return false;
            }
            var generation = ItemDatabase.GenerateProps.Generate(item, ItemDatabase.GetEntryStatic(item).m_GenerateProps);
            if (generation != null)
            {
                return 0 == AddInventory(new InventoryEntry() { m_Name = item, m_Count = 1, m_Properties = generation }, doIt);
            }

            foreach (var entry in m_Inventory)
            {
                if (entry != null && entry.m_Name == item)
                {
                    int stacking = ItemDatabase.GetStackingStatic(item);
                    if (entry.m_Count < stacking)
                    {
                        if (doIt) entry.m_Count++;
                        return true;
                    }
                }
            }

            for (int i = 0; i < m_Inventory.Count; i++)
            {
                if (m_Inventory[i] == null)
                {
                    if (doIt) m_Inventory[i] = new InventoryEntry(item, 1);
                    return true;
                }
            }

            return false;
        }

        // Try to move otherAmount items in other slot into my slot
        public int MoveFromOtherInventory(Inventory other, int otherSlot, int otherAmount, int mySlot)
        {
            var mySlotEntry = GetSlot(mySlot);
            var otherSlotEntry = other.GetSlot(otherSlot);

            // Nothing being moved.
            if (otherAmount == 0 || null == otherSlotEntry) return 0;

            // Nothing in my slot.  To simplify logic, create my slot with same type but with 0 count, which will then be picked up below.
            if (mySlotEntry == null)
            {
                mySlotEntry = SetSlot(mySlot, otherSlotEntry.Clone());
                mySlotEntry.m_Count = 0;
            }

            // Same object type.  We can directly transfer the numbers.
            if (mySlotEntry.m_Name == otherSlotEntry.m_Name)
            {
                int stacking = mySlotEntry.Stacking;
                int canFitInStack = stacking - mySlotEntry.m_Count;

                otherAmount = Math.Min(otherAmount, canFitInStack);

                mySlotEntry.m_Count += otherAmount;
                otherSlotEntry.m_Count -= otherAmount;

                // If that put other at zero, delete it.
                if (otherSlotEntry.m_Count == 0)
                {
                    other.SetSlot(otherSlot, null);
                }

                return otherAmount;
            }
            else
            {
                // Not the same object type, so we cannot move into that slot.
                return 0;
            }
        }

        public void MoveAllTo(Inventory other, bool movePersist)
        {
            for (int i = 0; i < m_Inventory.Count; i++)
            {
                if (m_Inventory[i] != null)
                {
                    var persist = GetEntryInSlot(i).GetCombinedPropBool("persist", m_Inventory[i].m_Properties);
                    if (movePersist) persist = false;
                    if (persist)
                    {
                        continue;
                    }
                    other.AddInventory(m_Inventory[i], true);
                    m_Inventory[i] = null;
                }
            }
        }

        public bool DropInventoryAll(int slot)
        {
            if (m_Inventory[slot] != null)
            {
                m_Inventory[slot] = null;
                return true;
            }
            return false;
        }

        public bool DropInventory(int slot)
        {
            if (m_Inventory[slot] != null)
            {
                m_Inventory[slot].m_Count--;
                if (m_Inventory[slot].m_Count == 0)
                {
                    m_Inventory[slot] = null;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public int DropInventoryN(string name, int count)
        {
            if (count == 0) return 0;

            for (int i = 0; i < m_Inventory.Count; i++)
            {
                if (m_Inventory[i] != null && m_Inventory[i].m_Name == name)
                {
                    bool couldDrop = false;
                    do
                    {
                        couldDrop = DropInventory(i);
                        if (couldDrop)
                        {
                            count--;
                            if (count == 0) return 0;
                        }
                    }
                    while (couldDrop);
                }
            }
            return count;
        }

        public static void DropInventoryN(Inventory[] inventories, string name, int count)
        {
            foreach(var inventory in inventories)
            {
                count = inventory.DropInventoryN(name, count);
                if (count == 0) return;
            }
        }

        public ItemDatabase.Entry GetEntryInSlot(int slot)
        {
            if (slot < 0 || slot >= m_Inventory.Count) return null;
            var entry = m_Inventory[slot];
            if (entry != null)
            {
                return ItemDatabase.GetEntryStatic(entry.m_Name);
            }
            return null;
        }

        public Item GetItemInSlot(int slot)
        {
            if (slot < 0 || slot >= m_Inventory.Count) return null;
            var entry = m_Inventory[slot];
            if (entry != null)
            {
                return ItemDatabase.GetItemStatic(entry.m_Name);
            }
            return null;
        }

        public class AnnounceCollectData
        {
            public List<InventoryEntry> m_Entries = new List<InventoryEntry>();

            public void Announce(Vector3 pos)
            {
                string totalStuffAdded = "";
                for (int i = 0; i < m_Entries.Count; i++)
                {
                    totalStuffAdded += "+ " + m_Entries[i].m_Count + " " + m_Entries[i].m_Name + " \n";
                }
                if (totalStuffAdded.Length > 0)
                {
                    TwoDee.FloatingTextManager.Instance.AddEntry(totalStuffAdded, pos, 2.0f);
                }
            }
        };
    }
}
