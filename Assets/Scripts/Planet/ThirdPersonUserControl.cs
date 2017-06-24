using System;
using System.Collections.Generic;
using TwoDee;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace Planet
{
    public class MouseButtonEntry
    {
        public string m_Name;
        public object m_Data;
        public MouseButtonEntry(string str, int data)
        {
            m_Name = str;
            m_Data = data;
        }
    }
    public class MouseButtonContext
    {
        public bool m_CanTeleport;
        public List<MouseButtonEntry> m_Entries = new List<MouseButtonEntry>();        

        public MouseButtonContext(GameObject player)
        {
            m_Player = player;
        }

        public GameObject m_Player;
    }
    public interface IMouseButtons
    {
        void GetButtons(MouseButtonContext context);
        void UseButton(MouseButtonContext context, MouseButtonEntry entry);
    }
    public class MouseInfoArgs
    {
        public MouseInfoArgs()
        {

        }
        public string Value
        {
            set; get;
        }
        float m_AllowedDistance;
    }

    public interface IMouseInfo
    {
        void GetMouseInfo(MouseInfoArgs args);
    }
    public class ThirdPersonUserControl : TwoDee.ThirdPersonUserControl, IInventoryButtonAccumulator
    {
        string[] m_DebugToolNames =
        {
            "Press F-Key for debug tool",
            "Create/Destroy",
            "Rope",
            "Tractor",
            "Light"
        };
        enum DebugTool
        {
            Nothing = 0,
            CreateDestroyGridPoint,
            Rope,
            Tractor,
            Light,
            MAX
        };

        public GameObject m_RopePrefab;
        public GameObject m_DroppedItemPrefab;

        public Sprite m_BorderSprite;
        public Sprite m_LightBorderSprite;
        public Sprite m_ChargingSprite;
        public Sprite m_LowBatterySprite;
        public Sprite m_PowerOffSprite;
        public Sprite m_KeyIconSprite;
        public Sprite m_TargetCursorSprite;

        public Inventory.StaticSprites StaticSprites
        {
            get
            {
                return new Inventory.StaticSprites() { m_Border = m_BorderSprite, m_LightBorder = m_LightBorderSprite, m_Charging = m_ChargingSprite, m_LowBattery=m_LowBatterySprite,m_PowerOff=m_PowerOffSprite, m_KeyIcon= m_KeyIconSprite };
            }
        }

        int m_SelectedSlot = -1;
        bool m_SelectedSlotSelf = true; // My inventory or the box I have opened?
        void SetSelectedSlot(int selectedSlot, bool selectedSlotSelf)
        {
            if(m_SelectedSlot != selectedSlot)
            {
                m_StatusMessage = "";
            }
            m_SelectedSlot = selectedSlot;
            m_SelectedSlotSelf = selectedSlotSelf;

            if (m_SelectedSlotSelf)
            {
                var itemEntry = m_SelectedSlotSelf ? Inventory.GetSlot(m_SelectedSlot) : null;
                m_StatusMessage = "Space- Jump, hold to hover";
                if (itemEntry != null)
                {
                    m_StatusMessage = itemEntry.GetHelpString();
                }
            }
        }
        void ClearSelectedSlot()
        {
            SetSelectedSlot(-1, true);
        }

        string m_StatusMessage = "";
        GameObject m_ClosestObject = null;
        bool m_ClosestTouchable = false;
        Vector3 m_LastShootingTarget_ws;
        GameObject m_SelectedWorldObject;
        GameObject SelectedWorldObject
        {
            set
            {
                if (m_SelectedWorldObject != value)
                {
                    if (value != null)
                    {
                        TwoDee.EasySound.Play("uiopen", transform.position);
                    }
                    m_SelectedWorldObject = value;
                }
            }
            get
            {
                return m_SelectedWorldObject;
            }
        }

        Vector3 m_SelectedWorldObjectPos;
        Vector3 m_SelectedWorldObjectClickPos;

        public ThirdPersonCharacter PlanetCharacter
        {
            get { return m_Character as ThirdPersonCharacter; }
        }

        public Inventory Inventory
        {
            get
            {
                return PlanetCharacter.Inventory;
            }
        }
        public Inventory[] Inventories
        {
            get
            {
                return new Inventory[] { PlanetCharacter.Inventory, InventoryEquipment };
            }
        }

        public Inventory[] CraftableInventories
        {
            get
            {
                if (OtherInventory != null)
                {
                    return new Inventory[] { Inventory, OtherInventory };
                }
                return new Inventory[] { Inventory };
            }
        }

        public Inventory[] GetCraftableInventories(bool selectedSlotSelf)
        {
            if (selectedSlotSelf) return CraftableInventories;
            else
            {
                if (OtherInventory != null)
                {
                    return new Inventory[] { OtherInventory, Inventory };
                }
                return new Inventory[] { Inventory };
            }
        }

        public Inventory InventoryEquipment
        {
            get
            {
                return PlanetCharacter.InventoryEquipment;
            }
        }

        protected override void VirtualStart()
        {
        }

        class GuiContext
        {
            public Vector3 MousePosGuiSpace
            {
                get
                {
                    return new Vector3(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                }
            }

            public bool IsHoveredOver()
            {
                Vector3 mousepos_guis = MousePosGuiSpace;

                foreach (var r in m_ButtonRects)
                {
                    if (r.Contains(mousepos_guis))
                    {
                        return true;
                    }
                }
                return false;
            }

            public bool AddButton(Rect r, string name)
            {
                m_ButtonRects.Add(r);
                return GUI.Button(r, name);
            }

            public void Clear()
            {
                m_ButtonRects.Clear();
            }
            public List<Rect> m_ButtonRects = new List<Rect>();
        }
        GuiContext m_GuiContext = new GuiContext();

        Inventory OtherInventory
        {
            get
            {
                if (m_SelectedWorldObject == null) return null;
                var container = m_SelectedWorldObject.GetComponentInSelfOrParents<Container>();
                if (container != null)
                {
                    return container.FirstInventory();
                }

                return null;
            }
        }

        int[,] m_Explored;
        bool m_ExploredWasUsed;
        bool m_CanTeleport;

        public void OnGUI()
        {
            Vector3 mousepos_guis = new Vector3(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

            m_CanTeleport = false;
            m_GuiContext.Clear();

            string toolName = "";
            if (m_SelectedSlot < 0)
            {
                toolName = "Tool: " + m_DebugToolNames[-1 - m_SelectedSlot];
            }
            else
            {
                var itemEntry = m_SelectedSlotSelf ? Inventory.GetSlot(m_SelectedSlot) : null;
                if (itemEntry != null)
                {
                    toolName = itemEntry.DebugString();
                }
            }
            TwoDee.GuiExt.Label2(new Rect(5, 2, 300, 100), toolName);

            float localTime = WorldState.Instance.CurrentTime;
            int currentDay = Mathf.FloorToInt(localTime / 24.0f);
            int currentHour24hr = Mathf.FloorToInt(localTime % 24.0f);
            int currentHour = currentHour24hr;
            int currentMinute = Mathf.FloorToInt((localTime%1.0f) * 60);
            bool pm = (currentHour >= 12);
            if (currentHour == 0) currentHour = 12;
            if (currentHour >= 13) currentHour -= 12;
            string timeStr = string.Format("{0,2}:{1:00} {2}", currentHour, currentMinute, pm ? "PM" : "AM");
            string speedUpStr = (WorldState.Instance.SpeedTimeLeft > 0.0f) ? " +" : "";
            float health = Mathf.Round(GetComponent<TwoDee.Health>().m_Health);
            Color healthColor = (health < 20) ? Color.red : Color.white;
            TwoDee.GuiExt.Label2(healthColor, new Rect(5, 20, 300, 100), string.Format("health: {0:F1}", GetComponent<TwoDee.Health>().m_Health));
            var weight = Inventory.ComputeWeight();
            Color weightColor = (weight > PlanetCharacter.m_MaxWeight * 0.5) ? Color.red : Color.white;
            TwoDee.GuiExt.Label2(weightColor, new Rect(85, 20, 300, 100), string.Format("weight: {0:F1}", weight));


            float rad = Mathf.Round(GetComponent<TwoDee.Health>().m_Radiation);
            Color radColor = (rad < 20) ? Color.red : Color.white;
            TwoDee.GuiExt.Label2(radColor, new Rect(5, 40, 300, 100), string.Format("radiation: {0:F1}", GetComponent<TwoDee.Health>().m_Radiation));


            Color timeColor = Color.white;
            if (currentHour24hr < 7 || currentHour24hr > 19) timeColor = Color.yellow;
            if (currentHour24hr < 6 || currentHour24hr > 20) timeColor = Color.red;
            TwoDee.GuiExt.Label2(timeColor, new Rect(5, 60, 300, 100), string.Format("time: {1,4} (day {2}){3}", GetComponent<TwoDee.Health>().m_Health, timeStr, currentDay + 1, speedUpStr));


            // MouseInfo
            var gs = new GUIStyle();
            gs.normal.textColor = m_ClosestTouchable ? Color.white : Color.red;

            var mouseString = "";
            if (m_ClosestObject != null)
            {
                MouseInfoArgs args = new MouseInfoArgs();
                foreach(var mo in m_ClosestObject.GetComponentsInSelfOrParents<IMouseInfo>())
                {
                    mo.GetMouseInfo(args);
                    mouseString = args.Value;
                }
            }
            if (m_HoverButtonEntry != null)
            {
                gs.normal.textColor = Color.white;
                mouseString = m_HoverButtonEntry.DebugString();
            }

          

            int singleItemSize = 30;
            int singleItemWithGapAndBorder = singleItemSize+15;
            // Clear inventory buttons
            {
                InventoryButtons.Clear();

                Rect r = new Rect(Screen.width - 20, 10, singleItemSize, singleItemSize);
                r.x -= 10 * singleItemWithGapAndBorder;

                Rect requip = new Rect(r);
                requip.x -= singleItemWithGapAndBorder + InventoryEquipment.Count * singleItemWithGapAndBorder;
                // No selected slot in equipment
                InventoryEquipment.OnGuiInventory(singleItemWithGapAndBorder, requip, StaticSprites, -1, false, this);

                Inventory.OnGuiInventory(singleItemWithGapAndBorder, r, StaticSprites, m_SelectedSlotSelf ? m_SelectedSlot : -1, true, this);

                if (m_SelectedWorldObject != null)
                {
                    var sp = Camera.main.WorldToScreenPoint(m_SelectedWorldObjectClickPos);
                    r.x = sp.x;
                    r.y = Screen.height - sp.y;
                    int rowsUsed = 0;

                    var otherInventory = OtherInventory;
                    if (otherInventory != null)
                    {
                        rowsUsed = otherInventory.OnGuiInventory(singleItemWithGapAndBorder, r, StaticSprites, !m_SelectedSlotSelf ? m_SelectedSlot : -1, false, this);
                    }

                    var iMouseButtons = m_SelectedWorldObject.gameObject.GetComponentInSelfOrParents<IMouseButtons>();
                    if (iMouseButtons != null)
                    {
                        var context = new MouseButtonContext(gameObject);
                        iMouseButtons.GetButtons(context);
                        r.y += singleItemWithGapAndBorder * rowsUsed;
                        r.height = 20;
                        foreach (var button in context.m_Entries)
                        {
                            r.width = button.m_Name.Length * 14;
                            if (m_GuiContext.AddButton(r, button.m_Name))
                            {
                                iMouseButtons.UseButton(context, button);
                            }
                            r.y += 25;
                        }

                        if (context.m_CanTeleport)
                        {
                            m_CanTeleport = true;
                        }
                    }
                }

                if (m_DraggedButtonEntry != null)
                {
                    Inventory.OnGuiSingleEntry(new Rect(mousepos_guis.x, mousepos_guis.y, 20, 20),
                        StaticSprites,
                        false, m_DraggedButtonEntry, 0, false, null);
                }
            }

            // Show lower left
            var vgen = ComponentList.GetFirst<PVoxelGenerator>();
            if (vgen != null)
            {
                var map = vgen.MiniMap;
                if (map != null)
                {
                    var loc = vgen.WorldSpaceToGrid(transform.position);
                    var normalizedLoc = new Vector3(loc.x / vgen.Dimension.X, loc.y / vgen.Dimension.Y);
                    var normalizedMapHalfSize = new Vector3(0.1f, 0.1f);
                    var low = normalizedLoc - normalizedMapHalfSize;
                    var high = normalizedLoc + normalizedMapHalfSize;

                    if (m_Explored == null)
                    {
                        m_Explored = new int[vgen.Dimension.X, vgen.Dimension.Y];
                    }
                    if (!m_ExploredWasUsed)
                    {
                        m_ExploredWasUsed = true;
                        map.UpdateAll(m_Explored);
                    }
                    map.Explore(m_Explored, loc, new Vector3(10.0f, 10.0f));
                    float size = 200;
                    float padding = 5;

                    var uvRect = new Rect(low.x, low.y, 0.2f, 0.2f);
                    var playerPos_uv= new Vector2(0.5f, 0.5f);
                    if (m_CanTeleport)
                    {
                        size = 400;
                        uvRect = new Rect(0, 0, 1, 1);
                        playerPos_uv = new Vector2(normalizedLoc.x, 1.0f - normalizedLoc.y);
                    }
                    var uiUpperLeft = new Vector2(padding, Screen.height - (size + padding));
                    var mapRect = new Rect(uiUpperLeft.x, uiUpperLeft.y, size, size);

                    GUI.DrawTextureWithTexCoords(mapRect, map.Texture, uvRect);
                    GUI.DrawTexture( (uiUpperLeft + size * playerPos_uv).MakeRect(5.0f, 5.0f), StaticSprites.m_Border.texture);
                    //                    GUI.DrawTexture(new Rect(padding, Screen.height - (size + padding), size, size), map.Texture);
                    // Show each teleport
                    if (m_CanTeleport)
                    {
                        var proxyworld = TwoDee.ComponentList.GetFirst<TwoDee.ProxyWorld>();
                        foreach (var ob in proxyworld.GetGameObjectsOrProxies("teleportvortex"))
                        {

                            var teleportPos_gs = vgen.WorldSpaceToGrid(ob.Position);
                            var teleportPos_nrm = new Vector3(teleportPos_gs.x / vgen.Dimension.X, teleportPos_gs.y / vgen.Dimension.Y);
                            var teleportPos_uv = new Vector2(teleportPos_nrm.x, 1.0f - teleportPos_nrm.y);

                            if(m_GuiContext.AddButton((uiUpperLeft + size * teleportPos_uv).MakeRect(10.0f, 10.0f), "TP"))
                            {
                                TwoDee.EasySound.Play("teleport", gameObject);

                                Vector3 otherEndPos = ob.Position;
                                otherEndPos.z = 0.0f;
                                transform.position = otherEndPos;
                            }
                        }
                    }
                }
            }

            // Show HUD for item if it's got some
            var item = Inventory.GetItemInSlot(m_SelectedSlot);
            //if (item!= null)
            {
            }
            if (item == null)
            {
                Cursor.SetCursor(null, new Vector2(), CursorMode.Auto);
                TwoDee.GuiExt.Label2(new Rect(mousepos_guis.x + 16, mousepos_guis.y, 300, 100), mouseString, gs);
            }
            else
            {
                Cursor.SetCursor(m_TargetCursorSprite.texture,
                    new Vector2(m_TargetCursorSprite.texture.width/2, m_TargetCursorSprite.texture.height/2),
                    CursorMode.ForceSoftware);
            }

            // Show crafting GUI (probably will need to clean this up at some point)
            OnGuiCrafting((int)(singleItemWithGapAndBorder * 2));
        }

        bool m_ShowHelp = false;
        bool ShowHelp
        {
            set
            {
                m_ShowHelp = value;
                if (value)
                {
                    HelpData.Instance.ActivateGuiHelp();
                    m_DraggedButtonEntry = null;
                    m_DraggedButtonOrigin = null;
                    Time.timeScale = 0.0f;
                }
                else
                {
                    Time.timeScale = 1.0f;
                }
            }

            get
            {
                return m_ShowHelp;
            }
        }

        List<string> m_AvailableCraftingStations = new List<string>();
        public void AddRemoveCraftingStation(bool enter, string name)
        {
            if (enter) m_AvailableCraftingStations.Add(name);
            else m_AvailableCraftingStations.Remove(name);
        }

        bool CheckStationRequirement(string[] requiredStations)
        {
            if (requiredStations == null || requiredStations.Length == 0) return true;
            foreach(string station in requiredStations)
            {
                if (!m_AvailableCraftingStations.Contains(station)) return false;
            }

            return true;
        }

        bool m_ShowCrafting = false;

        void OnGuiCrafting(int yOffset)
        {
            GUIStyle gs = new GUIStyle();
            gs.normal.textColor = Color.white;
            Rect r = new Rect(Screen.width - 20 - 300, yOffset+20, 10, 10);

            if (m_GuiContext.AddButton(new Rect(Screen.width - 110, yOffset, 90, 20), "Crafting"))
            {
                m_ShowCrafting = !m_ShowCrafting;
            }
            if (m_GuiContext.AddButton(new Rect(Screen.width - 110, 30.0f+yOffset, 90, 20), "Sudoku"))
            {
                gameObject.GetComponent<TwoDee.Health>().RawDamage(new TwoDee.DamageArgs(20.0f, TwoDee.DamageType.Pure, gameObject, gameObject.transform.position));
            }

            // Bottom
            int yOffsetFromBot = Screen.height - 30;
            var gc = new TwoDee.GlowGuiColor(HelpData.Instance.AnyNew);
            if (m_GuiContext.AddButton(new Rect(Screen.width - 110, yOffsetFromBot, 90, 20), "Help"))
            {
                ShowHelp = !ShowHelp;
            }
            gc.End();

            yOffsetFromBot -= 30;
            gs.alignment = TextAnchor.UpperRight;
            gs.normal.textColor = Color.white;
            TwoDee.GuiExt.Label2(new Rect(Screen.width - 200, yOffsetFromBot, 200, 100), m_StatusMessage, gs);

            if (ShowHelp)
            {
                HelpData.Instance.OnGUIHelp(() => { ShowHelp = false; });
                return;
            }

            if (m_ShowCrafting)
            {
                gs.alignment = TextAnchor.UpperLeft;

                // Find recipes involved with selected item
                var otherEntry = (OtherInventory != null ? OtherInventory.GetEntryInSlot(m_SelectedSlot) : null);
                var entry = m_SelectedSlotSelf ? Inventory.GetEntryInSlot(m_SelectedSlot) : otherEntry;

                string headerText = "Select item";
                if (entry != null)
                {
                    headerText = "Recipes with " + entry.m_Name;
                }
                TwoDee.GuiExt.Label2(r, headerText, gs);
                if (entry != null)
                {
                    r.y += 20;
                    var recipes = RecipeDatabase.GetRecipesInvolvingStatic(entry.m_Name, new List<string>());
                    foreach (var recipe in recipes)
                    {
                        if (!CheckStationRequirement(recipe.m_RequiredStations)) continue;
                        bool hasEnough = true;
                        string desc = "";
                        foreach (var input in recipe.m_Input)
                        {
                            bool hasEnoughThis = Inventory.HasAtLeast(CraftableInventories, input.m_Name, input.m_Count);
                            desc += input.m_Count + " x " + input.m_Name + (hasEnoughThis ? " " : "(!) ");
                            hasEnough = hasEnough && hasEnoughThis;
                        }
                        desc += " = ";
                        foreach (var input in recipe.m_Output)
                        {
                            desc += input.m_Count + " x " + input.m_Name + " ";
                        }
                        if (recipe.m_RequiredStations != null)
                        {
                            foreach (var station in recipe.m_RequiredStations)
                            {
                                desc += "(" + station + ")";
                            }
                        }

                        TwoDee.GuiExt.Label2(r, desc, gs);
                        bool canHold = true;
                        foreach (var output in recipe.m_Output)
                        {
                            canHold = canHold && (0 == Inventory.AddInventory(GetCraftableInventories(m_SelectedSlotSelf), new InventoryEntry(output.m_Name, output.m_Count), false));
                        }

                        if (hasEnough && canHold)
                        {
                            Rect r2 = r;
                            r2.x -= 30;
                            r2.width = 20;
                            r2.height = 20;
                            if (m_GuiContext.AddButton(r2, "X"))
                            {
                                TwoDee.EasySound.Play("uiplacement", transform.position);

                                foreach (var input in recipe.m_Input)
                                {
                                    Inventory.DropInventoryN(CraftableInventories, input.m_Name, input.m_Count);
                                }
                                foreach (var output in recipe.m_Output)
                                {
                                    for (int i=0;i<output.m_Count;i++)
                                    {
                                        Inventory.AddInventory(GetCraftableInventories(m_SelectedSlotSelf), output.m_Name, true);
                                    }
                                }
                            }
                        }
                        r.y += 20;
                    }
                }
            }
        }

        public GameObject m_TrajectoryPreviewPrefab;
        GameObject m_TrajectoryPreview;
        ThrowTrajectoryData m_TrajectoryData = new ThrowTrajectoryData();

        GameObject m_Tractor;
        Vector3 m_TractorOffset;
        void UpdateTractor()
        {
            if (m_Tractor != null)
            {
                var desiredPos = transform.position + m_TractorOffset;
                Vector3 delta = desiredPos - m_Tractor.transform.position;
                m_Tractor.GetComponent<Rigidbody>().velocity = delta;
            }
        }


        void UpdateItemHud()
        {
            // Show HUD for item if it's got some
            var item = Inventory.GetItemInSlot(m_SelectedSlotSelf ? m_SelectedSlot : -1);
            bool itemNeedsTrajectory = item != null;
            itemNeedsTrajectory = itemNeedsTrajectory && m_TrajectoryData.m_Value > 0.0f;
            if (itemNeedsTrajectory)
            {
                float traj = 1.0f;
                //float traj = item.ShowTrajectory(m_LastShootingTarget_ws);
                if (traj > 0.0f)
                {
                    if (m_TrajectoryPreview == null)
                    {
                        m_TrajectoryPreview = GameObject.Instantiate<GameObject>(m_TrajectoryPreviewPrefab);
                    }
                    m_TrajectoryPreview.transform.position = UsageOrigin();
                    m_TrajectoryPreview.GetComponent<TrajectoryPreview>().UpdateTrajectory((m_LastShootingTarget_ws - UsageOrigin()).normalized * m_TrajectoryData.m_Value, m_TrajectoryData.m_MaxDistance, m_TrajectoryData.m_Gravity);
                }
            }
            if (m_TrajectoryPreview != null)
            {
                m_TrajectoryPreview.transform.localScale = itemNeedsTrajectory ? new Vector3(1.0f, 1.0f, 1.0f) : Vector3.zero;
            }
        }

        Vector3 UsageOrigin()
        {
            return transform.position + (transform.rotation * Vector3.up);
        }

        InventoryButton m_DraggedButtonOrigin;
        InventoryEntry m_DraggedButtonEntry;
        InventoryEntry m_HoverButtonEntry;
        Vector3 m_DraggedButtonPos_guis;
        float m_DraggedButtonTime;

        protected bool UpdateDrag()
        {
            InventoryButton dropOntoSlot = null;

            Vector3 mousepos_guis = new Vector3(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            m_HoverButtonEntry = null;
            foreach (var entry in InventoryButtons)
            {
                if (entry.m_Rect.Contains(mousepos_guis))
                {
                    m_HoverButtonEntry = entry.Inventory.GetSlot(entry.m_Slot);
                    dropOntoSlot = entry;

                    // If not already dragging
                    if (m_DraggedButtonOrigin == null)
                    {
                        if (Input.GetMouseButtonDown(0) && m_HoverButtonEntry != null)
                        {
                            // Start dragging
                            m_DraggedButtonTime = 0.0f;
                            m_DraggedButtonOrigin = entry;
                            m_DraggedButtonEntry = entry.Inventory.GetSlot(entry.m_Slot).Clone();
                            m_DraggedButtonPos_guis = mousepos_guis;

                            return true;
                        }
                        else if (Input.GetMouseButtonDown(1))
                        {
                            // Quick transfer of inventory.
                            var clickedEntry = entry.Inventory.GetSlot(entry.m_Slot);
                            if (clickedEntry != null && m_SelectedWorldObject != null)
                            {
                                // Transfer between inventories
                                var otherInventory = (entry.Inventory == Inventory ? OtherInventory : Inventory);
                                if (otherInventory != null)
                                {
                                    if (0 == otherInventory.AddInventory(clickedEntry, false))
                                    {
                                        TwoDee.EasySound.Play("uimoved", transform.position);

                                        otherInventory.AddInventory(clickedEntry, true);
                                        entry.Inventory.DropInventoryAll(entry.m_Slot);

                                        // Eat input
                                        return true;
                                    }
                                }
                            }
                        }
                    }


                }
            }

            if (m_DraggedButtonOrigin != null)
            {
                m_DraggedButtonTime += Time.unscaledDeltaTime;

                // Cut number in half?
                if (Input.GetMouseButtonDown(1))
                {
                    if (m_DraggedButtonEntry.m_Count > 1)
                    {
                        m_DraggedButtonEntry.m_Count /= 2;
                    }
                }

                // Done?
                if (Input.GetMouseButtonUp(0))
                {
                    bool endDragging = true;
                    if (dropOntoSlot != null)
                    {
                        var outputEntry = dropOntoSlot.Entry;
                        var inputEntry = m_DraggedButtonOrigin.Entry;

                        if (!m_DraggedButtonOrigin.IsSameSlotAs(dropOntoSlot))
                        {
                            int moved = dropOntoSlot.Inventory.MoveFromOtherInventory(m_DraggedButtonOrigin.Inventory, m_DraggedButtonOrigin.m_Slot, m_DraggedButtonEntry.m_Count, dropOntoSlot.m_Slot);
                            TwoDee.EasySound.Play("uimoved", transform.position);
                            if (moved == 0)
                            {
                                var oldSlot = m_DraggedButtonOrigin.Inventory.GetSlot(m_DraggedButtonOrigin.m_Slot);
                                m_DraggedButtonOrigin.Inventory.SetSlot(m_DraggedButtonOrigin.m_Slot, outputEntry);
                                dropOntoSlot.Inventory.SetSlot(dropOntoSlot.m_Slot, oldSlot);
                            }
                        }
                        else
                        {
                            // Dropped into same slot, assume we want to select
                            if (m_DraggedButtonTime < 0.6f)
                            {
                                if (m_DraggedButtonOrigin.Inventory == Inventory)
                                {
                                    SetSelectedSlot(m_DraggedButtonOrigin.m_Slot, true);
                                }
                                else if(m_DraggedButtonOrigin.Inventory == OtherInventory)
                                {
                                    SetSelectedSlot(m_DraggedButtonOrigin.m_Slot, false);

                                }
                            }
                        }
                    }
                    else
                    {
                        // Drop on ground if far enough away drag
                        var delta = mousepos_guis - m_DraggedButtonPos_guis;
                        if (delta.magnitude > 30.0f && !m_DraggedButtonEntry.DbEntry.GetCombinedPropBool("persist", m_DraggedButtonEntry.m_Properties))
                        {
                            var pos = UsageOrigin();
                            pos.z = 0.0f;
                            var di = TwoDee.ProxyWorld.PostInstantiate(GameObject.Instantiate(m_DroppedItemPrefab, pos, Quaternion.identity), m_DroppedItemPrefab);
                            di.GetComponent<DroppedItem>().Entry = m_DraggedButtonEntry;

                            m_DraggedButtonOrigin.Inventory.SetSlot(m_DraggedButtonOrigin.m_Slot, null);
                        }
                        else
                        {
                            m_DraggedButtonOrigin.Inventory.SetSlot(m_DraggedButtonOrigin.m_Slot, m_DraggedButtonEntry);
                        }
                    }

                    if (endDragging)
                    {
                        m_DraggedButtonOrigin = null;
                        m_DraggedButtonEntry = null;
                    }
                }
                return true;
            }
            else
            {
                // Don't allow any input this high up (actually this causes some issues since it early outs too much)
                if (Input.mousePosition.y > Screen.height - 50.0f)
                {
                    //return true;
                }

                return false;
            }
        }

        void ClearSelectedWorldObject()
        {
            SelectedWorldObject = null;
            if (!m_SelectedSlotSelf)
            {
                m_SelectedSlotSelf = true;
                ClearSelectedSlot();
            }
        }

        public Material m_GhostMaterialGood;
        public Material m_GhostMaterialBad;

        InventoryEntry m_CurrentlySelectedEntry;
        protected override void UpdateInput()
        {
            UpdateItemHud();
            UpdateTractor();
            if (UpdateDrag()) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (ShowHelp) ShowHelp = false;
                if (m_SelectedSlot != -1)
                {
                    ClearSelectedSlot();
                }
                else
                {
                    ClearSelectedWorldObject();
                }
            }
            bool f11 = Input.GetKeyDown(KeyCode.F11);
            bool f12 = Input.GetKeyDown(KeyCode.F12);
            if(f11 || f12)
            {
                foreach (var proxyworld in FindObjectsOfType<TwoDee.ProxyWorld>())
                {
                    if(f11)proxyworld.TestSaveGame();
                    if(f12)proxyworld.TestLoadGame();
                }
            }

            for (int i = 0; i < 10; i++)
            {
                var code = (i == 9) ? (KeyCode.Alpha0) : (KeyCode)((int)KeyCode.Alpha1 + i);
                var codef = (KeyCode)((int)KeyCode.F1 + i);
                if (Input.GetKeyDown(code))
                {
                    int slot = (Input.GetKey(KeyCode.LeftShift) ? i + 10 : i);
                    if (m_SelectedSlot == slot)
                    {
                        ClearSelectedSlot();
                    }
                    else
                    {
                        SetSelectedSlot(slot, true);
                    }
                }
                if (Input.GetKeyDown(codef))
                {
                    if (i < ((int)DebugTool.MAX))
                    {
                        SetSelectedSlot(-(i + 1), true);
                    }
                }
            }

            bool canUseItem = true;

            float rayDistance;
            var shootingTarget_ws = new Vector3();
            var mouseRay_ws = Camera.main.ScreenPointToRay(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.0f));
            if (new Plane(Camera.main.transform.forward, 0).Raycast(mouseRay_ws, out rayDistance))
            {
                shootingTarget_ws = mouseRay_ws.GetPoint(rayDistance);
                m_LastShootingTarget_ws = shootingTarget_ws;
            }

            // Check what's under the mouse
            bool mouseOverGui = m_GuiContext.IsHoveredOver();

            m_ClosestObject = null;
            float closestDistance = 1e10f;
            Vector3 closest_ws = new Vector3();
            foreach (var col in Physics.SphereCastAll(mouseRay_ws, 0.3f))
            {
                closest_ws = col.point;
                var dist = (col.point - transform.position).magnitude;
                var foundIinfo = col.collider.gameObject.GetComponentInSelfOrParents<IMouseInfo>();

                if (foundIinfo != null)
                {
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        m_ClosestObject = col.collider.gameObject;
                    }
                }
            }

            m_ClosestTouchable = false;
            if (m_ClosestObject != null)
            {
                Vector3 touchOrigin = transform.position + Vector3.up * 0.5f;
                Vector3 delta = (closest_ws - touchOrigin);
                Vector3 dir = delta.normalized;
                float dist = delta.magnitude;
                m_ClosestTouchable = false;
                Debug.DrawLine(touchOrigin, closest_ws);

                RaycastHit hitInfo = new RaycastHit();
                var groundHit = Physics.Raycast(new Ray(touchOrigin, dir), out hitInfo, dist, GameObjectExt.GetLayerMask("Ground"));
                if (groundHit)
                {
                    // Pull hit out slightly and if that hits this object anyway it's fine
                    Vector3 hitLocation = hitInfo.point - 0.2f * dir;
                    hitLocation.z = m_ClosestObject.transform.position.z;
                    Vector3 fwd = Vector3.forward;
                    Vector3 start = hitLocation - 10.0f * fwd;
                    Debug.DrawLine(start, start + fwd * 10.0f);
                    foreach (var hit in Physics.SphereCastAll(new Ray(start, fwd), 0.3f, 10.0f))
                    {
                        if (hit.collider.gameObject == m_ClosestObject)
                        {
                            groundHit = false;
                            break;
                        }
                    }
                }
                if (closestDistance < 4.0f && !groundHit)
                {
                    m_ClosestTouchable = true;
                }
            }

            int mouseButtonPressed = -1;
            int mouseButtonHeld = -1;

            // Can't click if mouse is over a button.
            if (!mouseOverGui)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (Input.GetMouseButtonDown(i)) mouseButtonPressed = i;
                    if (Input.GetMouseButton(i)) mouseButtonHeld = i;
                }
            }

            if (m_SelectedWorldObject != null)
            {
                float dist2 = (transform.position - m_SelectedWorldObjectPos).sqrMagnitude;
                bool d2 = (dist2 > 4.0f);
                if ( d2 )
                {
                    ClearSelectedWorldObject();
                }
            }

            // Clicking touchable
            var selectedItem = Inventory.GetEntryInSlot(m_SelectedSlotSelf ? m_SelectedSlot : -1);
            var selectedInventoryEntry = Inventory.GetSlot(m_SelectedSlotSelf ? m_SelectedSlot : -1);

            if (m_ClosestTouchable && !mouseOverGui && (selectedItem == null || selectedItem.m_Item == null) && mouseButtonPressed != -1)
            {
                var cont = m_ClosestObject.GetComponentInSelfOrParents<Container>();
                if (cont != null) m_ClosestObject = cont.gameObject;
                var imousebutton = m_ClosestObject.GetComponentInSelfOrParents<IMouseButtons>();
                if (imousebutton != null) m_ClosestObject = (imousebutton as MonoBehaviour).gameObject;

                if (cont != null || imousebutton != null)
                {
                    if (m_ClosestObject == m_SelectedWorldObject)
                    {
                        ClearSelectedWorldObject();
                    }
                    else if (SelectedWorldObject != m_ClosestObject)
                    {
                        SelectedWorldObject = m_ClosestObject;
                        m_SelectedWorldObjectPos = transform.position;
                        m_SelectedWorldObjectClickPos = shootingTarget_ws;
                    }
                }

                var rp = m_ClosestObject.GetComponentInSelfOrParents<ResourcePile>();
                var itemEntry = Inventory.GetSlot(m_SelectedSlot);
                if (rp != null)
                {
                    if (rp.AllowsCollect(null))
                    {
                        bool collectedAll = true;
                        var data = new Inventory.AnnounceCollectData();
                        for (int i=0;i<rp.Inventory.Count;i++)
                        {
                            if (rp.Inventory.GetSlot(i) == null) continue;
                            if (0 == Inventory.AddInventory(rp.Inventory.GetSlot(i), false))
                            {
                                TwoDee.EasySound.Play("takepile", gameObject);

                                data.m_Entries.Add(rp.Inventory.GetSlot(i).Clone());
                                Inventory.AddInventory(rp.Inventory.GetSlot(i), true);
                            }
                            else
                            {
                                collectedAll = false;
                                m_StatusMessage = "No room to pick up.";
                                break;
                            }
                            rp.Inventory.DropInventoryAll(i);
                        }
                        data.Announce(transform.position);
                        if (collectedAll)
                        {
                            Destroy(rp.gameObject);
                        }
                    }
                    else
                    {
                        m_StatusMessage = "Need tool selected to collect.";
                    }
                }

                var di = m_ClosestObject.GetComponentInSelfOrParents<DroppedItem>();
                if (di != null)
                {
                    if (0 == Inventory.AddInventory(di.Entry, false))
                    {
                        Inventory.AddInventory(di.Entry, true);
                        DestroyObject(di.gameObject);
                    }
                    else
                    {
                        m_StatusMessage = "No room to pick up.";
                    }
                }
                return;
            }

            // Regular item usage
            UseContext args = new UseContext()
            {
                m_GhostMaterialGood = m_GhostMaterialGood,
                m_GhostMaterialBad = m_GhostMaterialBad,
                m_ButtonPressed = mouseButtonPressed,
                m_ButtonHeld = mouseButtonPressed,
                m_ClickedTarget = m_ClosestTouchable ? m_ClosestObject : null,
                m_OriginPos = UsageOrigin(),
                m_TargetPos = m_LastShootingTarget_ws,
                m_Secondary = false,
                m_DeltaTime = Time.fixedDeltaTime,
                m_ItemUserGameObject = gameObject,
                m_Entry = selectedInventoryEntry,
                m_Inventory = this.Inventory,
                m_InventorySlot = m_SelectedSlot,
                m_Inventories = new Inventory[] {Inventory, InventoryEquipment},
                m_ShowTrajectory = m_TrajectoryData,
                m_ShowStatusMessage = null,
                m_Error = false
            };

            // Update controls in deferred item use
            var deferredUse = PlanetCharacter.DeferredItemUse;

            if (deferredUse != null)
            {
                deferredUse.m_OriginPos = args.m_OriginPos;
                deferredUse.m_TargetPos = args.m_TargetPos;
                deferredUse.m_ButtonHeld = mouseButtonHeld;
            }

            if (m_CurrentlySelectedEntry != selectedInventoryEntry)
            {
                TwoDee.EasySound.Play("uiclick", transform.position);
                if (m_CurrentlySelectedEntry != null)
                {
                    var oldSelectedItem = ItemDatabase.GetEntryStatic(m_CurrentlySelectedEntry.m_Name);
                    if (oldSelectedItem != null && oldSelectedItem.m_Item != null)
                    {
                        oldSelectedItem.m_Item.Unselected(args);
                        m_TrajectoryData.m_Value = 0.0f;
                    }
                }
                m_CurrentlySelectedEntry = selectedInventoryEntry;
                if (m_CurrentlySelectedEntry != null)
                {
                    if (selectedItem != null && selectedItem.m_Item != null)
                    {
                        selectedItem.m_Item.Selected(args);
                    }
                }
            }
            if (canUseItem && selectedItem != null)
            {
                if (selectedItem.m_Item != null)
                {
                    selectedItem.m_Item.UpdateSelected(args);
                    if (mouseButtonPressed >= 0)
                    {
                        args.m_Secondary = mouseButtonHeld == 1;
                        args.m_ItemObject = selectedItem.m_Item.GetItem(args.m_Secondary);
                        if (PlanetCharacter.TryUse(m_SelectedSlot, args))
                        {
                            m_StatusMessage = "";
                        }
                        else
                        {
                            m_StatusMessage = args.m_ShowStatusMessage != null ? args.m_ShowStatusMessage : "Item can't be used now";
                        }
                    }
                }
            }

            // rocket boots
            if (CrossPlatformInputManager.GetButton("Jump"))
            {
                m_JumpBoost = true;
            }
        
            // @TEST voxel aabb test
            foreach (var vg in TwoDee.ComponentList.GetCopiedListOfType<TwoDee.VoxelGenerator>())
            {
                var start = transform.position;
                var up = transform.up;
                vg.IsBoxClearAt(start + 5.0f * up, up, 1.0f, 4.0f);
            }
                // @TEST voxel intersect test
                foreach (var vg in TwoDee.ComponentList.GetCopiedListOfType<TwoDee.VoxelGenerator>())
            {
                var start = new Vector3(0.0f, 1000.0f, 0.0f);
                var end = shootingTarget_ws;
                TwoDee.VoxelGenerator.IntersectInfo info;
                var hitintersect = vg.IntersectSegment(start, end, out info);

                Debug.DrawLine(start, hitintersect ? info.m_Pos_ws : end, (info!= null) ? Color.red : Color.green);
                if(info != null)
                {
                    Debug.DrawLine(info.m_Pos_ws + info.m_Normal_ws, hitintersect ? info.m_Pos_ws : end, (info != null) ? Color.red : Color.green);
                }
            }
            

            // Debug tool usage
            DebugTool CurrentTool = DebugTool.Nothing;
            if (m_SelectedSlot < 0)
            {
                CurrentTool = (DebugTool)(-1 + -m_SelectedSlot);
            }
            switch (CurrentTool)
            {
                case DebugTool.CreateDestroyGridPoint:
                    {
                        if (mouseButtonHeld != -1)
                        {
                            foreach (var vg in TwoDee.ComponentList.GetCopiedListOfType<TwoDee.VoxelGenerator>())
                            {
                                //vg.DebugToolClick(mouseButtonHeld, shootingTarget_ws);
                                vg.ModifyCircle(1.0f, mouseButtonHeld == 0, shootingTarget_ws, 5.0f);
                            }
                        }
                    }
                    break;
                case DebugTool.Rope:
                    {
                        if (mouseButtonPressed != -1)
                        {
                            var goRope = GameObject.Instantiate<GameObject>(m_RopePrefab, UsageOrigin(), Quaternion.identity);
                            foreach (var rope in goRope.GetComponents<Rope>())
                            {
                                var direction = shootingTarget_ws - UsageOrigin();
                                bool grappling = mouseButtonPressed != 0;
                                var speed = 1.0f * direction.magnitude * (grappling ? 2.0f : 1.0f);
                                rope.StartUnravel(grappling, direction, speed, 20, "rope");
                            }
                        }
                    }
                    break;
                case DebugTool.Tractor:
                    {
                        if (mouseButtonPressed != -1)
                        {
                            foreach (Rigidbody body in GameObjectExt.GetNearbyObjects<Rigidbody>(shootingTarget_ws, 4.0f, GameObjectExt.GetLayerMask("Objects")))
                            {
                                /*
                                var spring = body.gameObject.AddComponent<SpringJoint>();
                                spring.connectedBody = FindObjectOfType<ThirdPersonCharacter>().gameObject.GetComponent<Rigidbody>();
                                spring.enableCollision = true;
                                spring.maxDistance = 3.0f;
                                */
                                /*
                                var hj = body.gameObject.AddComponent<HingeJoint>();
                                hj.connectedBody = FindObjectOfType<ThirdPersonCharacter>().gameObject.GetComponent<Rigidbody>();
                                */
                                /*
                                Vector3 direction = shootingTarget_ws - transform.position;
                                body.AddForceAtPosition(shootingTarget_ws, -1000.0f * direction.normalized);
                                */
                                m_Tractor = body.gameObject;
                                m_TractorOffset = Vector3.up * 2.0f + body.transform.position - transform.position;
                                return;
                            }

                            m_Tractor = null;
                        }
                    }
                    break;
                case DebugTool.Light:
                    {
                        if (mouseButtonPressed != -1)
                        {
                            foreach (var vg in TwoDee.ComponentList.GetCopiedListOfType<TwoDee.VoxelGenerator>())
                            {
                                vg.AddSunLightPoint(shootingTarget_ws);
                            }
                        }
                    }
                    break;
            }
        }

        class InventoryButton
        {
            public Rect m_Rect;
            public Inventory Inventory;
            public int m_Slot;
            public InventoryButton(Rect rect, Inventory inventory, int slot)
            {
                m_Rect = rect;
                Inventory = inventory;
                m_Slot = slot;
            }
            public bool IsSameSlotAs(InventoryButton other)
            {
                return (Inventory == other.Inventory) && (m_Slot == other.m_Slot);
            }
            public InventoryEntry Entry
            {
                get
                {
                    return Inventory.GetSlot(m_Slot);
                }
            }
        }

        List<InventoryButton> InventoryButtons = new List<InventoryButton>();
        public void AddButton(Rect r, Inventory i, int slot)
        {
            InventoryButtons.Add(new InventoryButton(r, i, slot));
        }

        [Serializable]
        class PlayerProxy : TwoDee.PlayerProxy
        {
            Inventory m_Inventory;
            Inventory m_InventoryEquipment;

            Rle2DArrayProxy m_ExploredData;

            public virtual void Save(ThirdPersonUserControl player)
            {
                m_Inventory = player.Inventory;
                m_InventoryEquipment = player.InventoryEquipment;
                m_ExploredData = new Rle2DArrayProxy();
                m_ExploredData.Save(player.m_Explored);

                base.Save(player);
            }

            public virtual void Load(ThirdPersonUserControl player)
            {
                player.m_Explored = m_ExploredData.LoadInt();
                player.m_ExploredWasUsed = false;

                player.Inventory.ReplaceWith(m_Inventory);
                player.InventoryEquipment.ReplaceWith(m_InventoryEquipment);

                base.Load(player);
            }
        }

        public override object SaveProxy()
        {
            var result = new PlayerProxy();
            result.Save(this);
            return result;
        }

        public override void LoadProxy(object proxy)
        {
            (proxy as PlayerProxy).Load(this);
        }
    }
}
