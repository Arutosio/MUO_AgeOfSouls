using Org.BouncyCastle.Utilities.Collections;
using Server;
using Server.Engines.Spawners;
using Server.Guilds;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Prompts;
using Server.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;

namespace Server.CursedSoulsContent.GumpSystem
{
    public class GuildTerritoryManagementGump : Gump
    {
        private readonly GuildTerritoryStone _stone;
        private readonly PlayerMobile _viewer;
        private int _page;
        private int _guardPage = 1;
        private int _vendorPage = 0;

        // Costanti per gli ID dei bottoni NAV 1-100
        private const int BTN_NAV_MAIN = 1;
        private const int BTN_NAV_SECURITY = 2;
        private const int BTN_NAV_VENDORS = 3;
        private const int BTN_NAV_EXPAND = 4;
        private const int BTN_NAV_GATES = 5;
        private const int BTN_NAV_TEST = 6;

        // Costanti per gli ID dei bottoni MAIN 101-200
        private const int BTN_MAINPAGE_DESTROY_STONE = 101;
        private const int BTN_MAINPAGE_AllowHousing = 102;
        private const int BTN_MAINPAGE_SpawnGuards = 103;
        private const int BTN_MAINPAGE_AntiMagic = 104;
        private const int BTN_MAINPAGE_DailyTax = 105;
        private const int BTN_MAINPAGE_Treasury = 106;

        // Costanti per gli ID dei bottoni SECURITY 201-300
        private const int BTN_SECURITYPAGE_AllowNonGuildMembers = 201;
        private const int BTN_SECURITYPAGE_AllowCombat = 202;
        private const int BTN_SECURITYPAGE_AllowSpellcasting = 203;
        private const int BTN_SECURITYPAGE_GuardCount_DESCREASE = 204;
        private const int BTN_SECURITYPAGE_GuardCount_INCREASE = 205;
        private const int BTN_SECURITYPAGE_GuardStrength_DESCREASE = 206;
        private const int BTN_SECURITYPAGE_GuardStrength_INCREASE = 207;
        private const int BTN_SECURITYPAGE_SpawnWarriorGuard = 208;
        private const int BTN_SECURITYPAGE_SpawnArcherGuard = 209;
        private const int BTN_SECURITYPAGE_RemoveAllGuards = 210;
        private const int BTN_SECURITYPAGE_FramePage_Refresh = 211;
        private const int BTN_SECURITYPAGE_FramePage_Prev = 212;
        private const int BTN_SECURITYPAGE_FramePage_Next = 213;
        private const int BTN_SECURITYPAGE_FramePage_GuardRename = 240;
        private const int BTN_SECURITYPAGE_FramePage_GuardMove = 260;
        private const int BTN_SECURITYPAGE_FramePage_GuardDelete = 280;

        // Costanti per gli ID dei bottoni VENDORS 301-401
        private const int BTN_VENDORSPAGE_AddVendor = 301; // 301-315
        private const int BTN_VENDORSPAGE_FramePage_Refresh = 319;
        private const int BTN_VENDORSPAGE_FramePage_Prev = 320;
        private const int BTN_VENDORSPAGE_FramePage_Next = 321;
        private const int BTN_VENDORSPAGE_FramePage_VendorStock = 340;
        private const int BTN_VENDORSPAGE_FramePage_VendorMove = 360;
        private const int BTN_VENDORSPAGE_FramePage_VendorDelete = 380;

        public GuildTerritoryManagementGump(GuildTerritoryStone stone, PlayerMobile viewer, int page = 0, int guardPage = 0, int vendorPage = 0)
            : base(50, 50)
        {
            _stone = stone;
            _viewer = viewer;
            _page = page;
            _guardPage = guardPage;
            _vendorPage = vendorPage;

            Closable = true;
            Disposable = true;
            Draggable = true;
            Resizable = false;

            AddPage(0);
            AddBackground(0, 0, 700, 550, 0x13BE); // Più grande per il nuovo frame
            AddImageTiled(10, 10, 680, 25, 0xA40);
            AddAlphaRegion(10, 10, 680, 530);

            // Stato proprietario
            string owner = stone.OwnerGuild?.Name ?? "Unclaimed";
            // Titolo
            AddLabel(20, 12, 0x480, $"{owner} Guild Territory Management - Captured: {stone.CapturedAtUtc:g}");

            // Controllo permessi
            if (!CanManage(viewer, stone))
            {
                AddLabel(20, 90, 0x22, "You are not authorized to manage this territory.");
                return;
            }

            switch (page)
            {
                case 0: MainPage(); break;
                case 1: SecurityPage(); break;
                case 2: VendorsPage(); break;
                case 3: ExpansionPage(); break;
                case 4: GatesPage(); break;
                case 5: TestPage(); break;
            }

            // Navigation Menu
            RenderNavigationMenu();
        }

        private void RenderNavigationMenu()
        {
            int x = 18;
            int y = 510; // Posizione Y per la navigazione (più in basso per il nuovo layout)

            AddButton(x, y, 0xFA5, 0xFA7, BTN_NAV_MAIN, GumpButtonType.Reply, 0);
            AddLabel(x + 45, y, 0x480, "Main");

            AddButton(x + 100, y, 0xFA5, 0xFA7, BTN_NAV_SECURITY, GumpButtonType.Reply, 0);
            AddLabel(x + 135, y, 0x480, "Security");

            AddButton(x + 200, y, 0xFA5, 0xFA7, BTN_NAV_VENDORS, GumpButtonType.Reply, 0);
            AddLabel(x + 235, y, 0x480, "Vendors");

            AddButton(x + 300, y, 0xFA5, 0xFA7, BTN_NAV_EXPAND, GumpButtonType.Reply, 0);
            AddLabel(x + 335, y, 0x480, "Expand");

            AddButton(x + 400, y, 0xFA5, 0xFA7, BTN_NAV_GATES, GumpButtonType.Reply, 0);
            AddLabel(x + 435, y, 0x480, "Gates");

            AddButton(x + 500, y, 0xFA5, 0xFA7, BTN_NAV_TEST, GumpButtonType.Reply, 0);
            AddLabel(x + 535, y, 0x480, "Test");
        }

        private void MainPage()
        {
            int y = 38;
            AddLabel(500, y, 0x22, $"Destroy the Guild Stone:");
            AddButton(650, y, 0xFB5, 0xFB6, BTN_MAINPAGE_DESTROY_STONE, GumpButtonType.Reply, 0);

            AddLabel(20, y, 0x3B2, "Territory Status:");

            var region = _stone.GetRegion();
            if (region != null)
            {
                AddLabel(40, y + 40, 0x480, $"Radius: {_stone.Radius * 2}x{_stone.Radius * 2} tiles");
                AddLabel(40, y + 80, 0x480, $"Active Players in Territory: {GetPlayerCount(region)}");
            }

            AddLabel(20, y + 100, 0x3B2, "Quick Actions:");

            y = 210;

            // Toggle House Placement
            AddButton(40, y, _stone.AllowHousing ? 0xD3 : 0xD2,
                     _stone.AllowHousing ? 0xD2 : 0xD3, BTN_MAINPAGE_AllowHousing, GumpButtonType.Reply, 0);
            AddLabel(70, y, 0x480, $"Allow Housing: {(_stone.AllowHousing ? "YES" : "NO")}");
            y += 30;

            // Toggle Guard Spawns
            AddButton(40, y, _stone.SpawnGuards ? 0xD3 : 0xD2,
                     _stone.SpawnGuards ? 0xD2 : 0xD3, BTN_MAINPAGE_SpawnGuards, GumpButtonType.Reply, 0);
            AddLabel(70, y, 0x480, $"Spawn Guards: {(_stone.SpawnGuards ? "YES" : "NO")}");
            y += 30;

            // Toggle Anti-Magic
            AddButton(40, y, _stone.AntiMagic ? 0xD3 : 0xD2,
                     _stone.AntiMagic ? 0xD2 : 0xD3, BTN_MAINPAGE_AntiMagic, GumpButtonType.Reply, 0);
            AddLabel(70, y, 0x480, $"Anti-Magic Field: {(_stone.AntiMagic ? "ACTIVE" : "INACTIVE")}");
            y += 40;

            // Daily Tax
            AddLabel(40, y, 0x480, $"Daily Tax: {_stone.DailyTax} gold");
            AddButton(250, y, 0xFA5, 0xFA7, BTN_MAINPAGE_DailyTax, GumpButtonType.Reply, 0);
            AddLabel(285, y, 0x480, "Adjust");
            y += 30;

            // Treasury
            AddLabel(40, y, 0x480, $"Territory Treasury: {_stone.GoldTreasury:N0} gold");
            AddButton(300, y, 0xFA5, 0xFA7, BTN_MAINPAGE_Treasury, GumpButtonType.Reply, 0);
            AddLabel(335, y, 0x480, "Withdraw");
        }

        private void SecurityPage()
        {
            AddLabel(20, 90, 0x3B2, "Security Settings:");

            int y = 120;

            // Allow Non-Guild Members
            AddButton(40, y, _stone.AllowNonGuildMembers ? 0xD3 : 0xD2,
                     _stone.AllowNonGuildMembers ? 0xD2 : 0xD3, BTN_SECURITYPAGE_AllowNonGuildMembers, GumpButtonType.Reply, 0);
            AddLabel(70, y, 0x480, $"Allow Non-Guild Members: {(_stone.AllowNonGuildMembers ? "YES" : "NO")}");
            y += 30;

            // Allow Combat
            AddButton(40, y, _stone.AllowCombat ? 0xD3 : 0xD2,
                     _stone.AllowCombat ? 0xD2 : 0xD3, BTN_SECURITYPAGE_AllowCombat, GumpButtonType.Reply, 0);
            AddLabel(70, y, 0x480, $"Allow Combat: {(_stone.AllowCombat ? "YES" : "NO")}");
            y += 30;

            // Allow Spellcasting
            AddButton(40, y, _stone.AllowSpellcasting ? 0xD3 : 0xD2,
                     _stone.AllowSpellcasting ? 0xD2 : 0xD3, BTN_SECURITYPAGE_AllowSpellcasting, GumpButtonType.Reply, 0);
            AddLabel(70, y, 0x480, $"Allow Spellcasting: {(_stone.AllowSpellcasting ? "YES" : "NO")}");
            y += 30;

            // Guard Count
            AddLabel(40, y, 0x480, $"Guard Count: {_stone.GuardCount}");
            AddButton(150, y, 0xFAE, 0xFB0, BTN_SECURITYPAGE_GuardCount_DESCREASE, GumpButtonType.Reply, 0); // -Guard
            AddButton(180, y, 0xFA5, 0xFA7, BTN_SECURITYPAGE_GuardCount_INCREASE, GumpButtonType.Reply, 0); // +Guard
            y += 40;

            // Guard Strength
            AddLabel(40, y, 0x480, $"Guard Strength: {_stone.GuardStrength}/10");
            AddButton(180, y, 0xFAE, 0xFB0, BTN_SECURITYPAGE_GuardStrength_DESCREASE, GumpButtonType.Reply, 0); // -Strength
            AddButton(210, y, 0xFA5, 0xFA7, BTN_SECURITYPAGE_GuardStrength_INCREASE, GumpButtonType.Reply, 0); // +Strength
            y += 40;

            // Bottoni per spawn rapido
            AddLabel(20, y, 0x3B2, "Quick Actions:");
            y += 30;

            AddButton(40, y, 0xFA5, 0xFA7, BTN_SECURITYPAGE_SpawnWarriorGuard, GumpButtonType.Reply, 0);
            AddLabel(75, y, 0x480, "Spawn Warrior Guard");
            y += 25;

            AddButton(40, y, 0xFA5, 0xFA7, BTN_SECURITYPAGE_SpawnArcherGuard, GumpButtonType.Reply, 0);
            AddLabel(75, y, 0x480, "Spawn Archer Guard");
            y += 25;

            AddButton(40, y, 0xFB1, 0xFB3, BTN_SECURITYPAGE_RemoveAllGuards, GumpButtonType.Reply, 0);
            AddLabel(75, y, 0x22, "Remove All Guards");

            // Frame Dinamico per le Guardie
            RenderGuardsFrame();
        }

        private void RenderGuardsFrame()
        {
            var tmpFrameConfig = new GumpFrameConfig
            {
                Title = "Territory Guards",
                OnRefresh = () => _viewer.SendGump(new GuildTerritoryManagementGump(_stone, _viewer, 1, _guardPage))
            };

            var items = new List<GumpItemConfig>();
            var guards = _stone.Guards;

            for (int i = 0; i < guards.Count; i++)
            {
                var guard = guards[i];
                var tmpGumpItemConfig = new GumpItemConfig();
                tmpGumpItemConfig.Data = new GuardData { Guard = guard, Index = i };
                tmpGumpItemConfig.Title = guard.Name ?? $"Guard #{i + 1}";
                tmpGumpItemConfig.Description = $"HP: {guard.Hits}/{guard.HitsMax}";
                tmpGumpItemConfig.InfoLines = new List<string>
                {
                    $"Str: {guard.Str} Dex: {guard.Dex} Int: {guard.Int}",
                    $"Location: {guard.Location.X}, {guard.Location.Y}"
                };
                items.Add(tmpGumpItemConfig);
            }

            var tmpListItemButtons = new List<GumpButtonConfig>();

            var tmpGumpButtonConfig = new GumpButtonConfig();
            tmpGumpButtonConfig.Label = "Rename";
            tmpGumpButtonConfig.NormalId = 0xFAB;
            tmpGumpButtonConfig.PressedId = 0xFAD;
            tmpGumpButtonConfig.ButtonIdOffset = BTN_SECURITYPAGE_FramePage_GuardRename;
            tmpGumpButtonConfig.XOffset = 0;
            //tmpGumpButtonConfig.Action = (from, data) =>
            //{
            //    if (data is GuardData guardData && guardData.Index < _stone.Guards.Count)
            //    {
            //        from.SendMessage("Enter new name for the guard:");
            //        from.Prompt = new RenameGuardPrompt(_stone, guardData.Index);
            //    }
            //};
            tmpListItemButtons.Add(tmpGumpButtonConfig);

            tmpGumpButtonConfig = new GumpButtonConfig();
            tmpGumpButtonConfig.Label = "Move";
            tmpGumpButtonConfig.NormalId = 0xFA5;
            tmpGumpButtonConfig.PressedId = 0xFA7;
            tmpGumpButtonConfig.ButtonIdOffset = BTN_SECURITYPAGE_FramePage_GuardMove;
            tmpGumpButtonConfig.XOffset = 40;
            //tmpGumpButtonConfig.Action = (from, data) =>
            //{
            //    if (data is GuardData guardData && guardData.Index < _stone.Guards.Count)
            //    {
            //        var guard = _stone.Guards[guardData.Index];
            //        guard.MoveToWorld(from.Location, from.Map);
            //        from.SendMessage("Guard moved to your location.");
            //        from.SendGump(new GuildTerritoryManagementGump(_stone,
            //            from as PlayerMobile, 1, _guardPage));
            //    }
            //};
            tmpListItemButtons.Add(tmpGumpButtonConfig);

            tmpGumpButtonConfig = new GumpButtonConfig();
            tmpGumpButtonConfig.Label = "Delete";
            tmpGumpButtonConfig.NormalId = 0xFB1;
            tmpGumpButtonConfig.PressedId = 0xFB3;
            tmpGumpButtonConfig.ButtonIdOffset = BTN_SECURITYPAGE_FramePage_GuardDelete;
            tmpGumpButtonConfig.XOffset = 80;
            //tmpGumpButtonConfig.Action = (from, data) =>
            //{
            //    if (data is GuardData guardData && guardData.Index < _stone.Guards.Count)
            //    {
            //        _stone.RemoveGuard(guardData.Index);
            //        from.SendMessage("Guard removed.");
            //        from.SendGump(new GuildTerritoryManagementGump(_stone,
            //            from as PlayerMobile, 1, _guardPage));
            //    }
            //};
            tmpListItemButtons.Add(tmpGumpButtonConfig);

            var dynamicFrame = new DynamicGumpFrame(this, tmpFrameConfig, items, tmpListItemButtons,
                null, _guardPage, 0, BTN_SECURITYPAGE_FramePage_Prev, BTN_SECURITYPAGE_FramePage_Next, BTN_SECURITYPAGE_FramePage_Refresh);
            dynamicFrame.Render();
        }

        private void VendorsPage()
        {
            int y = 38;
            AddLabel(20, y, 0x3B2, "Vendor Management:");

            y = y + 20;

            // Vendor type selection
            AddLabel(40, y, 0x480, "Add New Vendor:");
            y += 30;

            for (int i = 0; i < GuildTerritoryStone.vendorTypes.Length; i++)
            {
                AddButton(40, y + (i * 30), 0xFA9, 0xFA8, BTN_VENDORSPAGE_AddVendor + i, GumpButtonType.Reply, 0);
                AddLabel(80, y + (i * 30), 0x480, $"Add {GuildTerritoryStone.vendorTypes[i]} (Cost: 5,000 gold)");
            }

            // Frame Dinamico per i Vendor
            RenderVendorsFrame();
        }

        private void RenderVendorsFrame()
        {
            var tmpFrameConfig = new GumpFrameConfig();
            tmpFrameConfig.Title = "Territory Vendors";
            tmpFrameConfig.OnRefresh = () => _viewer.SendGump(new GuildTerritoryManagementGump(_stone,
                _viewer, 2, 0, _vendorPage));

            var items = new List<GumpItemConfig>();
            var vendors = _stone.GetVendors();

            for (int i = 0; i < vendors.Count; i++)
            {
                var vendor = vendors[i];
                var tmpGumpItemConfig = new GumpItemConfig();
                tmpGumpItemConfig.Data = new VendorData { Vendor = vendor, Index = i };
                tmpGumpItemConfig.Title = vendor.Name ?? $"Vendor #{i + 1}";
                tmpGumpItemConfig.Description = $"Type: {vendor.GetType().Name}";
                tmpGumpItemConfig.InfoLines = new List<string>();
                //tmpGumpItemConfig.InfoLines.Add($"Gold: {vendor.BankBalance:N0}");
                tmpGumpItemConfig.InfoLines.Add($"Location: {vendor.Location.X}, {vendor.Location.Y}");
                items.Add(tmpGumpItemConfig);
            }

            var tmpListItemButtons = new List<GumpButtonConfig>();

            var tmpGumpButtonConfig = new GumpButtonConfig();
            tmpGumpButtonConfig.Label = "Stock";
            tmpGumpButtonConfig.NormalId = 0xFA9;
            tmpGumpButtonConfig.PressedId = 0xFA8;
            tmpGumpButtonConfig.ButtonIdOffset = BTN_VENDORSPAGE_FramePage_VendorStock;
            tmpGumpButtonConfig.XOffset = 0;
            //tmpGumpButtonConfig.Action = (from, data) =>
            //{
            //    from.SendMessage("Stock management coming soon!");
            //};
            tmpListItemButtons.Add(tmpGumpButtonConfig);

            tmpGumpButtonConfig = new GumpButtonConfig();
            tmpGumpButtonConfig.Label = "Move";
            tmpGumpButtonConfig.NormalId = 0xFA5;
            tmpGumpButtonConfig.PressedId = 0xFA7;
            tmpGumpButtonConfig.ButtonIdOffset = BTN_VENDORSPAGE_FramePage_VendorMove;
            tmpGumpButtonConfig.XOffset = 40;
            //tmpGumpButtonConfig.Action = (from, data) =>
            //{
            //    if (data is VendorData vendorData && vendorData.Index < vendors.Count)
            //    {
            //        var vendor = vendors[vendorData.Index];
            //        vendor.MoveToWorld(from.Location, from.Map);
            //        from.SendMessage("Vendor moved to your location.");
            //        from.SendGump(new GuildTerritoryManagementGump(_stone,
            //            from as PlayerMobile, 2, 0, _vendorPage));
            //    }
            //};
            tmpListItemButtons.Add(tmpGumpButtonConfig);

            tmpGumpButtonConfig = new GumpButtonConfig();
            tmpGumpButtonConfig.Label = "Delete";
            tmpGumpButtonConfig.NormalId = 0xFA2;
            tmpGumpButtonConfig.PressedId = 0xFA4;
            tmpGumpButtonConfig.ButtonIdOffset = BTN_VENDORSPAGE_FramePage_VendorDelete;
            tmpGumpButtonConfig.XOffset = 80;
            //tmpGumpButtonConfig.Action = (from, data) =>
            //{
            //    if (data is VendorData vendorData && vendorData.Index < vendors.Count)
            //    {
            //        _stone.RemoveVendor(vendorData.Index);
            //        from.SendMessage("Vendor removed.");
            //        from.SendGump(new GuildTerritoryManagementGump(_stone,
            //            from as PlayerMobile, 2, 0, _vendorPage));
            //    }
            //};
            tmpListItemButtons.Add(tmpGumpButtonConfig);

            var dynamicFrame = new DynamicGumpFrame(this, tmpFrameConfig, items, tmpListItemButtons,
                null, _vendorPage, 0, BTN_VENDORSPAGE_FramePage_Prev, BTN_VENDORSPAGE_FramePage_Next, BTN_VENDORSPAGE_FramePage_Refresh);
            dynamicFrame.Render();
        }

        private void ExpansionPage()
        {
            AddLabel(20, 90, 0x3B2, "Territory Expansion:");

            AddLabel(40, 120, 0x480, $"Current Radius: {_stone.Radius} tiles");
            AddLabel(40, 140, 0x480, $"Maximum Radius: {_stone.MaxRadius} tiles");
            AddLabel(40, 160, 0x480, $"Expansion Cost Multiplier: {_stone.ExpansionCostMultiplier:N1}x");

            int y = 200;
            int[] expansionAmounts = { 5, 10, 25 };
            int[] expansionCosts = { 10000, 18000, 40000 };

            for (int i = 0; i < expansionAmounts.Length; i++)
            {
                int newRadius = _stone.Radius + expansionAmounts[i];
                if (newRadius <= _stone.MaxRadius)
                {
                    int cost = (int)(expansionCosts[i] * _stone.ExpansionCostMultiplier);
                    AddButton(40, y, 0xFA5, 0xFA7, 70 + i, GumpButtonType.Reply, 0);
                    AddLabel(80, y, 0x480, $"Expand by {expansionAmounts[i]} tiles (Cost: {cost:N0} gold)");
                    y += 30;
                }
            }

            AddLabel(40, y + 10, 0x22, "Note: Expansion increases daily maintenance cost.");
        }

        private void GatesPage()
        {
            AddLabel(20, 90, 0x3B2, "Gate Control:");

            var gates = _stone.GetGatesInTerritory();
            AddLabel(40, 120, 0x480, $"Gates in Territory: {gates.Count}");

            int y = 150;

            // Global gate toggle
            AddButton(40, y, _stone.GatesEnabled ? 0xD3 : 0xD2,
                     _stone.GatesEnabled ? 0xD2 : 0xD3, 30, GumpButtonType.Reply, 0);
            AddLabel(70, y, 0x480, $"All Gates Enabled: {(_stone.GatesEnabled ? "YES" : "NO")}");
            y += 30;

            // Individual gate control
            if (gates.Count > 0)
            {
                AddLabel(20, y, 0x3B2, "Individual Gate Control:");
                y += 30;

                for (int i = 0; i < gates.Count && i < 8; i++)
                {
                    var gate = gates[i];
                    bool enabled = _stone.IsGateEnabled(gate);

                    AddButton(40, y, enabled ? 0xD3 : 0xD2, enabled ? 0xD2 : 0xD3,
                             40 + i, GumpButtonType.Reply, 0);

                    string gateName = GetGateName(gate);
                    AddLabel(70, y, enabled ? 0x480 : 0x22, $"{gateName} at {gate.Location}");

                    y += 25;
                }
            }
        }

        private void TestPage()
        {
            AddLabel(20, 90, 0x3B2, "Test Page");
            AddLabel(40, 120, 0x480, "This page is reserved for future testing features.");
            AddLabel(40, 150, 0x480, "Total Guards: " + _stone.Guards.Count);
            AddLabel(40, 170, 0x480, "Total Vendors: " + _stone.GetVendors().Count);
        }

        private int GetPlayerCount(Region region)
        {
            int count = 0;
            foreach (Mobile m in region.GetMobiles())
                if (m is PlayerMobile && m.Alive) count++;
            return count;
        }

        private string GetGateName(Item gate)
        {
            if (gate is Moongate) return "Moongate";
            if (gate is Teleporter) return "Teleporter";
            if (gate.Name != null && gate.Name.Contains("Gate"))
                return "Gate Spell";
            return gate.GetType().Name;
        }

        public static bool CanManage(Mobile m, GuildTerritoryStone stone)
        {
            if (m == null || stone == null || stone.Deleted)
                return false;

            if (stone.OwnerGuild != null)
            {
                var guild = m.Guild as Guild;
                if (guild == null || guild != stone.OwnerGuild)
                    return false;

                if (m == guild.Leader)
                    return true;

                var pm = m as PlayerMobile;
                if (pm != null && pm.AccessLevel >= AccessLevel.GameMaster)
                    return true;
            }

            return false;
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (from == null || _stone == null || _stone.Deleted ||
                !CanManage(from as PlayerMobile, _stone))
                return;

            int buttonId = info.ButtonID;

            // Gestione del sistema dinamico per le pagine correnti
            bool handledByDynamicSystem = false;

            if (_page == 1) // Pagina Security - Gestione Guardie
            {
                //handledByDynamicSystem = dynamicFrame.HandleResponse(from, buttonId);
                //// Aggiorna _guardPage con la nuova pagina dal frame
                //_guardPage = dynamicFrame.CurrentPage;
            }

            // Se il sistema dinamico non ha gestito il bottone, usa la logica normale
            if (!handledByDynamicSystem)
            {
                HandleStandardButtons(from, buttonId);
            }
        }

        private void HandleStandardButtons(Mobile from, int buttonId)
        {
            int index;
            switch (buttonId)
            {
                case 0: // Close
                    break;

                #region Navigation Nav 1-100
                case BTN_NAV_MAIN:
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, 0));
                    break;
                case BTN_NAV_SECURITY:
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, 1));
                    break;
                case BTN_NAV_VENDORS:
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, 2));
                    break;
                case BTN_NAV_EXPAND:
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, 3));
                    break;
                case BTN_NAV_GATES:
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, 4));
                    break;
                case BTN_NAV_TEST:
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, 5));
                    break;
                #endregion Navigation Nav 1-100

                #region Navigation MainPage 101-200
                case BTN_MAINPAGE_DESTROY_STONE:
                    _stone.Delete();
                    from.SendMessage("Guild territory stone destroyed.");
                    break;
                case BTN_MAINPAGE_AllowHousing: // Toggle Housing
                    _stone.AllowHousing = !_stone.AllowHousing;
                    from.SendMessage($"Housing is now {(_stone.AllowHousing ? "ALLOWED" : "DISALLOWED")}.");
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, _page));
                    break;

                case BTN_MAINPAGE_SpawnGuards: // Toggle Guards
                    _stone.SpawnGuards = !_stone.SpawnGuards;
                    from.SendMessage($"Guards are now {(_stone.SpawnGuards ? "SPAWNING" : "REMOVED")}.");
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, _page));
                    break;

                case BTN_MAINPAGE_AntiMagic: // Toggle Anti-Magic
                    _stone.AntiMagic = !_stone.AntiMagic;
                    from.SendMessage($"Anti-magic field is now {(_stone.AntiMagic ? "ACTIVE" : "INACTIVE")}.");
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, _page));
                    break;

                case BTN_MAINPAGE_DailyTax: // Adjust Tax
                    from.SendMessage("Enter new daily tax amount (0-10000):");
                    from.Prompt = new AdjustTaxPrompt(_stone, from);
                    break;

                case BTN_MAINPAGE_Treasury: // Withdraw Treasury
                    from.SendMessage("Enter amount to withdraw from treasury:");
                    from.Prompt = new WithdrawTreasuryPrompt(_stone, from);
                    break;
                #endregion Navigation MainPage 101-200

                #region Navigation SecurityPage 201-300
                case BTN_SECURITYPAGE_AllowNonGuildMembers: // Allow Non-Guild Members
                    _stone.AllowNonGuildMembers = !_stone.AllowNonGuildMembers;
                    from.SendMessage($"Non-guild members are now {(_stone.AllowNonGuildMembers ? "ALLOWED" : "BANNED")}.");
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, _page));
                    break;

                case BTN_SECURITYPAGE_AllowCombat: // Allow Combat
                    _stone.AllowCombat = !_stone.AllowCombat;
                    from.SendMessage($"Combat is now {(_stone.AllowCombat ? "ALLOWED" : "DISALLOWED")}.");
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, _page));
                    break;

                case BTN_SECURITYPAGE_AllowSpellcasting: // Allow Spellcasting
                    _stone.AllowSpellcasting = !_stone.AllowSpellcasting;
                    from.SendMessage($"Spellcasting is now {(_stone.AllowSpellcasting ? "ALLOWED" : "DISALLOWED")}.");
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, _page));
                    break;

                case BTN_SECURITYPAGE_GuardCount_DESCREASE: // Decrease Guard Count
                    if (_stone.GuardCount > 0)
                    {
                        _stone.GuardCount--;
                        from.SendMessage($"Guard count decreased to {_stone.GuardCount}.");
                    }
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, _page));
                    break;

                case BTN_SECURITYPAGE_GuardCount_INCREASE: // Increase Guard Count
                    if (_stone.GuardCount < 10)
                    {
                        _stone.GuardCount++;
                        from.SendMessage($"Guard count increased to {_stone.GuardCount}.");
                    }
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, _page));
                    break;

                // BTN_SECURITYPAGE_GuardStrength Guard Strength (20-29)
                case BTN_SECURITYPAGE_GuardStrength_DESCREASE:
                    _stone.GuardStrength--;
                    from.SendMessage($"Guard strength set to {_stone.GuardStrength}/10.");
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, _page));
                    break;

                case BTN_SECURITYPAGE_GuardStrength_INCREASE:
                    _stone.GuardStrength++;
                    from.SendMessage($"Guard strength set to {_stone.GuardStrength}/10.");
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, _page));
                    break;

                // Quick Spawn Actions
                case BTN_SECURITYPAGE_SpawnWarriorGuard: // Spawn Warrior Guard
                    if (_stone.SpawnGuards)
                    {
                        if (_stone.Guards.Count < _stone.GuardCount)
                        {
                            _stone.SpawnGuard(typeof(WarriorGuard), from.Location, from.Map);
                            from.SendMessage("Warrior guard spawned at your location.");
                        }
                        else
                        {
                            from.SendMessage("Maximum guard count reached. Increase the guard count first.");
                        }
                    }
                    else
                    {
                        from.SendMessage("Spawn guards is not allowed");
                    }
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, _page, _guardPage, _vendorPage));
                    break;

                case BTN_SECURITYPAGE_SpawnArcherGuard: // Spawn Archer Guard
                    if (_stone.SpawnGuards)
                    {
                        if (_stone.Guards.Count < _stone.GuardCount)
                        {
                            _stone.SpawnGuard(typeof(ArcherGuard), from.Location, from.Map);
                            from.SendMessage("Archer guard spawned at your location.");
                        }
                        else
                        {
                            from.SendMessage("Maximum guard count reached. Increase the guard count first.");
                        }
                    }
                    else
                    {
                        from.SendMessage("Spawn guards is not allowed");
                    }
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, _page, _guardPage, _vendorPage));
                    break;

                case BTN_SECURITYPAGE_RemoveAllGuards: // Remove All Guards
                    int removedCount = _stone.Guards.Count;
                    foreach (var guard in _stone.Guards.ToList())
                    {
                        guard.Delete();
                    }
                    _stone.Guards.Clear();
                    from.SendMessage($"All {removedCount} guards have been removed.");
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, _page, _guardPage, _vendorPage));
                    break;

                case BTN_SECURITYPAGE_FramePage_Refresh:
                    _stone.UpdateGuards();
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, _page, _guardPage, _vendorPage));
                    break;

                // SecurityFramePage
                case BTN_SECURITYPAGE_FramePage_Prev:
                    _guardPage--;
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, _page, _guardPage, _vendorPage));
                    break;

                case BTN_SECURITYPAGE_FramePage_Next:
                    _guardPage++;
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, _page, _guardPage, _vendorPage));
                    break;
                //private const int BTN_SECURITYPAGE_FramePage_GuardRename = 240;
                case int id when id >= BTN_SECURITYPAGE_FramePage_GuardRename && id <= (BTN_SECURITYPAGE_FramePage_GuardRename + _stone.Guards.Count):
                    index = id - BTN_SECURITYPAGE_FramePage_GuardRename;
                    from.SendMessage("Enter new name for the guard:");
                    from.Prompt = new RenameGuardPrompt(_stone, index, _page, _guardPage, _vendorPage);
                    break;

                //private const int BTN_SECURITYPAGE_FramePage_GuardMove = 260;
                case int id when id >= BTN_SECURITYPAGE_FramePage_GuardMove && id <= (BTN_SECURITYPAGE_FramePage_GuardMove + _stone.Guards.Count):
                    index = id - BTN_SECURITYPAGE_FramePage_GuardMove;
                    _stone.MoveGuardOnYourLocation(index, from);
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, _page, _guardPage, _vendorPage));
                    break;

                //private const int BTN_SECURITYPAGE_FramePage_GuardDelete = 280;
                case int id when id >= BTN_SECURITYPAGE_FramePage_GuardDelete && id <= (BTN_SECURITYPAGE_FramePage_GuardDelete + _stone.Guards.Count):
                    index = id - BTN_SECURITYPAGE_FramePage_GuardDelete;
                    _stone.RemoveGuard(index, from);
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, _page, _guardPage, _vendorPage));
                    break;
                #endregion Navigation SecurityPage 201-300

                #region Navigation VendorsPage 301-400
                // Add Vendor (60-64) 301-315
                case int id when id >= BTN_VENDORSPAGE_AddVendor && id <= (BTN_VENDORSPAGE_AddVendor + 14):
                    int typeIndex = id - BTN_VENDORSPAGE_AddVendor;
                    _stone.AddVendor(GuildTerritoryStone.vendorTypes[typeIndex], from);
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, _page));
                    break;

                case BTN_VENDORSPAGE_FramePage_Refresh:
                    _stone.UpdateGuards();
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, _page, _guardPage, _vendorPage));
                    break;

                // Vendor FramePage
                case BTN_VENDORSPAGE_FramePage_Prev:
                    _vendorPage--;
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, _page, _guardPage, _vendorPage));
                    break;

                case BTN_VENDORSPAGE_FramePage_Next:
                    _vendorPage++;
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, _page, _guardPage, _vendorPage));
                    break;

                //private const int BTN_VENDORSPAGE_FramePage_VendorStock = 340;
                case int id when id >= BTN_VENDORSPAGE_FramePage_VendorStock && id <= (BTN_VENDORSPAGE_FramePage_VendorStock + _stone.Vendors.Count):
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, _page, _guardPage, _vendorPage));
                    break;

                //private const int BTN_VENDORSPAGE_FramePage_VendorMove = 360;
                case int id when id >= BTN_VENDORSPAGE_FramePage_VendorMove && id <= (BTN_VENDORSPAGE_FramePage_VendorMove + _stone.Vendors.Count):
                    index = id - BTN_VENDORSPAGE_FramePage_VendorMove;
                    _stone.MoveVendorOnYourLocation(index, from);
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, _page, _guardPage, _vendorPage));
                    break;

                //private const int BTN_VENDORSPAGE_FramePage_VendorDelete = 380;
                case int id when id >= BTN_VENDORSPAGE_FramePage_VendorDelete && id <= (BTN_VENDORSPAGE_FramePage_VendorDelete + _stone.Vendors.Count):
                    index = id - BTN_VENDORSPAGE_FramePage_VendorDelete;
                    _stone.RemoveVendor(index);
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, _page, _guardPage, _vendorPage));
                    break;

                #endregion Navigation VendorsPage 301-400

                #region Navigation ExpandPage 401-500

                // Expand Territory (70-72)
                case int id when id >= 70 && id <= 72:
                    int[] amounts = { 5, 10, 25 };
                    int amountIndex = id - 70;
                    _stone.ExpandTerritory(amounts[amountIndex], from);
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, _page));
                    break;

                #endregion Navigation ExpandPage 401-500

                case 30: // Toggle All Gates
                    _stone.GatesEnabled = !_stone.GatesEnabled;
                    _stone.UpdateGates();
                    from.SendMessage($"All gates are now {(_stone.GatesEnabled ? "ENABLED" : "DISABLED")}.");
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, _page));
                    break;

                // Toggle Individual Gate (40-47)
                case int id when id >= 40 && id <= 47:
                    int gateIndex = id - 40;
                    _stone.ToggleGate(gateIndex);
                    from.SendMessage("Gate toggled.");
                    from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, _page));
                    break;

            }
        }
    }

    public class AdjustTaxPrompt : Prompt
    {
        private readonly GuildTerritoryStone _stone;
        private readonly Mobile _from;

        public AdjustTaxPrompt(GuildTerritoryStone stone, Mobile from)
        {
            _stone = stone;
            _from = from;
        }

        public override void OnCancel(Mobile from)
        {
            from.SendMessage("Tax adjustment cancelled.");
        }

        public override void OnResponse(Mobile from, string text)
        {
            if (int.TryParse(text, out int amount) && amount >= 0 && amount <= 10000)
            {
                _stone.DailyTax = amount;
                from.SendMessage($"Daily tax set to {amount} gold.");
            }
            else
            {
                from.SendMessage("Invalid amount. Please enter a number between 0 and 10000.");
            }
        }
    }

    public class WithdrawTreasuryPrompt : Prompt
    {
        private readonly GuildTerritoryStone _stone;
        private readonly Mobile _from;

        public WithdrawTreasuryPrompt(GuildTerritoryStone stone, Mobile from)
        {
            _stone = stone;
            _from = from;
        }

        public override void OnCancel(Mobile from)
        {
            from.SendMessage("Withdrawal cancelled.");
        }

        public override void OnResponse(Mobile from, string text)
        {
            if (int.TryParse(text, out int amount) && amount > 0)
            {
                _stone.WithdrawGoldTreasury(amount, from);
            }
            else
            {
                from.SendMessage("Invalid amount.");
            }
        }
    }

    // Aggiungi queste classi helper alla fine del file, prima delle classi Prompt
    public class GuardData
    {
        public BaseGuard Guard { get; set; }
        public int Index { get; set; }
    }

    public class VendorData
    {
        public BaseVendor Vendor { get; set; }
        public int Index { get; set; }
    }

    public class RenameGuardPrompt : Prompt
    {
        private readonly GuildTerritoryStone _stone;
        private readonly int _guardIndex;
        private readonly int _page;
        private readonly int _guardPage;
        private readonly int _vendorPage;

        public RenameGuardPrompt(GuildTerritoryStone stone, int guardIndex, int page, int guardPage, int vendorPage)
        {
            _stone = stone;
            _guardIndex = guardIndex;
            _page = page;
            _guardPage = guardPage;
            _vendorPage = vendorPage;
        }

        public override void OnCancel(Mobile from)
        {
            from.SendMessage("Rename cancelled.");
            from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, _page, _guardPage, _vendorPage));
        }

        public override void OnResponse(Mobile from, string text)
        {
            _stone.RenameGuard(_guardIndex, text, from);
            from.SendGump(new GuildTerritoryManagementGump(_stone, from as PlayerMobile, _page, _guardPage, _vendorPage));
        }
    }
}
