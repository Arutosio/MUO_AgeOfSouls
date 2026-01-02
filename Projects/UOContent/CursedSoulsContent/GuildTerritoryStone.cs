using MimeKit;
using ModernUO.Serialization;
using Org.BouncyCastle.Asn1.Cmp;
using Org.BouncyCastle.Utilities.Collections;
using Server;
using Server.CursedSoulsContent;
using Server.CursedSoulsContent.GumpSystem;
using Server.Ethics;
using Server.Factions;
using Server.Guilds;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Regions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static System.Net.Mime.MediaTypeNames;

namespace Server.CursedSoulsContent
{
    [SerializationGenerator(0, false)]
    public partial class GuildTerritoryStone : Item
    {
        public static string[] vendorTypes = {
            "Banker",
            "Blacksmith",
            "Provisioner",
            "Mage",
            "Armorer",
            "AnimalTrainer",
            "Alchemist",
            "Tailor",
            "Carpenter",
            "Tinker",
            "Bowyer",
            "Butcher",
            "Bard",
            "Herbalist"
        };

        private PGTerritoryStoneRegion _region;
        private Timer _timer;

        [Constructible]
        public GuildTerritoryStone() : base(0xED4)
        {
            Name = "Guild Territory Stone";
            Movable = true;

            // ---- Region geometry ----
            _radius = 5;
            _regionPriority = 50;

            // ---- Capture rules ----
            _captureRange = 10;
            _captureTime = TimeSpan.FromMinutes(1);
            _tickRate = TimeSpan.FromSeconds(2);

            // ---- Announcements ----
            _announceEnterExit = true;
            _announceCapture = true;
            _announceContested = true;

            // ---- Gestione territorio ----
            _allowHousing = true;
            _spawnGuards = false;
            _antiMagic = false;
            _allowNonGuildMembers = true;
            _allowCombat = true;
            _allowSpellcasting = true;
            _gatesEnabled = true;

            _guardCount = 0;
            _guardStrength = 5;
            _dailyTax = 100;
            _goldTreasury = 0;
            _maxRadius = 50;
            _expansionCostMultiplier = 1.0;

            _vendorCost = 5000;
            _guards = new();
            _vendors = new();
            _controlledGates = new();
        }

        public GuildTerritoryStone(Serial serial) : base(serial)
        {
        }

        #region PUBLIC PROPERTIES Get&Set (GM)
        // ---- Region geometry ----
        [CommandProperty(AccessLevel.GameMaster)]
        [SerializableProperty(0)]
        public int Radius
        {
            get => _radius;
            set
            {
                _radius = Math.Max(5, Math.Min(200, value));
                RebuildRegion();
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        [SerializableProperty(1)]
        public int RegionPriority
        {
            get => _regionPriority;
            set
            {
                _regionPriority = Math.Max(0, Math.Min(200, value));
                RebuildRegion();
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        // ---- Capture rules ----
        [CommandProperty(AccessLevel.GameMaster)]
        [SerializableProperty(2)]
        public int CaptureRange
        {
            get => _captureRange;
            set
            {
                _captureRange = Math.Max(1, Math.Min(30, value));
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        [SerializableProperty(3)]
        public TimeSpan CaptureTime
        {
            get => _captureTime;
            set
            {
                _captureTime = value < TimeSpan.FromSeconds(10) ? TimeSpan.FromSeconds(10) : value;
                ResetCaptureProgress();
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        [SerializableProperty(4)]
        public TimeSpan TickRate
        {
            get => _tickRate;
            set
            {
                _tickRate = value < TimeSpan.FromSeconds(1) ? TimeSpan.FromSeconds(5) : value;
                this.MarkDirty();
            }
        }

        // ---- Capture state ----
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        [SerializableProperty(5)]
        public Guild CurrentCaptorGuild
        {
            get => _currentCaptorGuild;
            set
            {
                _currentCaptorGuild = value;
                this.MarkDirty();
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        [SerializableProperty(6)]
        public TimeSpan Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                this.MarkDirty();
            }
        }

        // ---- Announcements ----
        [CommandProperty(AccessLevel.GameMaster)]
        [SerializableProperty(7)]
        public bool AnnounceEnterExit
        {
            get => _announceEnterExit;
            set {
                _announceEnterExit = value;
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        [SerializableProperty(8)]
        public bool AnnounceCapture
        {
            get => _announceCapture;
            set {
                _announceCapture = value;
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        [SerializableProperty(9)]
        public bool AnnounceContested
        {
            get => _announceContested;
            set {
                _announceContested = value;
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        // ---- Ownership ----
        [CommandProperty(AccessLevel.GameMaster)]
        [SerializableProperty(10)]
        public Guild OwnerGuild
        {
            get => _ownerGuild;
            set
            {
                if (value == null)
                {
                    ToggleOnOffCapture();
                }
                else if (value != _ownerGuild)
                {
                    StopCaptureTimer();
                }
                _ownerGuild = value;
                this.MarkDirty();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        [SerializableProperty(11)]
        public DateTime CapturedAtUtc
        {
            get => _capturedAtUtc;
            set
            {
                _capturedAtUtc = value;
                this.MarkDirty();
            }
        }

        // ---- Territory management ----
        [CommandProperty(AccessLevel.GameMaster)]
        [SerializableProperty(12)]
        public bool AllowHousing
        {
            get => _allowHousing;
            set
            {
                _allowHousing = value;
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        [SerializableProperty(13)]
        public bool SpawnGuards
        {
            get => _spawnGuards;
            set
            {
                _spawnGuards = value;
                UpdateGuards();
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        [SerializableProperty(14)]
        public bool AntiMagic
        {
            get => _antiMagic;
            set
            {
                _antiMagic = value;
                UpdateAntiMagicEffect();
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        [SerializableProperty(15)]
        public bool AllowNonGuildMembers
        {
            get => _allowNonGuildMembers;
            set {
                _allowNonGuildMembers = value;
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        [SerializableProperty(16)]
        public bool AllowCombat
        {
            get => _allowCombat;
            set {
                _allowCombat = value;
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        [SerializableProperty(17)]
        public bool AllowSpellcasting
        {
            get => _allowSpellcasting;
            set {
                _allowSpellcasting = value;
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        [SerializableProperty(18)]
        public bool GatesEnabled
        {
            get => _gatesEnabled;
            set
            {
                _gatesEnabled = value;
                UpdateGates();
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        [SerializableProperty(19)]
        public int GuardCount
        {
            get => _guardCount;
            set
            {
                _guardCount = Math.Max(0, Math.Min(10, value));
                UpdateGuards();
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        [SerializableProperty(20)]
        public int GuardStrength
        {
            get => _guardStrength;
            set
            {
                _guardStrength = Math.Max(1, Math.Min(10, value));
                UpdateGuards();
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        [SerializableProperty(21)]
        public int DailyTax
        {
            get => _dailyTax;
            set
            {
                _dailyTax = Math.Max(0, Math.Min(10000, value));
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        [SerializableProperty(22)]
        public int GoldTreasury
        {
            get => _goldTreasury;
            set
            {
                _goldTreasury = Math.Max(0, value);
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        [SerializableProperty(23)]
        public int MaxRadius
        {
            get => _maxRadius;
            set
            {
                _maxRadius = Math.Max(Radius, Math.Min(200, value));
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        [SerializableProperty(24)]
        public double ExpansionCostMultiplier
        {
            get => _expansionCostMultiplier;
            set
            {
                _expansionCostMultiplier = Math.Max(1.0, Math.Min(5.0, value));
                InvalidateProperties();
                this.MarkDirty();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        [SerializableProperty(25)]
        public int VendorCost
        {
            get => _vendorCost;
            set
            {
                _vendorCost = Math.Max(0, Math.Min(0, value));
                InvalidateProperties();
                this.MarkDirty();
            }
        }
        // ---- Vendors & Guards & Gates----
        [SerializableProperty(26)]
        public List<BaseGuard> Guards
        {
            get
            {
                _guards.RemoveAll(item => item == null);
                return _guards;
            }
            set
            {
                _guards = value;
                this.MarkDirty();
            }
        }

        [SerializableProperty(27)]
        public List<BaseVendor> Vendors
        {
            get
            {
                _vendors.RemoveAll(item => item == null);
                return _vendors;
            }
            set
            {
                _vendors = value;

                this.MarkDirty();
            }
        }

        [SerializableProperty(28)]
        public List<Item> ControlledGates
        {
            get => _controlledGates;
            set
            {
                _controlledGates = value;
                this.MarkDirty();
            }
        }

        #endregion PUBLIC PROPERTIES Get&Set

        #region ON EVENTS
        public override void OnLocationChange(Point3D oldLocation)
        {
            base.OnLocationChange(oldLocation);
            //ToggleOnOffCapture();
        }

        public override void OnMapChange()
        {
            base.OnMapChange();
            //ToggleOnOffCapture();
        }

        public override void OnAfterSpawn()
        {
            base.OnAfterSpawn();
            ToggleOnOffCapture();
        }

        public override void OnAdded(IEntity parent)
        {
            base.OnAdded(parent);
            Movable = false;
            if (Map != null && Map != Map.Internal)
            {
                ToggleOnOffCapture();
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (Movable)
            {
                from.SendMessage("This guild territory stone must be placed before it can be used.");
                return;
            }
            else
            {
                if (OwnerGuild == null)
                {
                    ToggleOnOffCapture(from);
                }
                if (from is PlayerMobile pm && GuildTerritoryManagementGump.CanManage(pm, this))
                {
                    OpenManagementGump(from);
                }
            }


            if (from == null || from.Deleted)
                return;

            if (!from.InRange(GetWorldLocation(), 2))
            {
                from.SendMessage("You are too far away.");
                return;
            }
        }

        internal void OnMobileEnterRegion(Mobile m)
        {
            if (!AnnounceEnterExit || m == null || !m.Player)
            {
                return;
            }

            if (OwnerGuild != null)
            {
                m.SendMessage(0x3B2, $"You have entered {OwnerGuild.Name} guild territory.");
            }
            else
            {
                m.SendMessage(0x3B2, "You have entered unclaimed territory.");
            }
        }

        internal void OnMobileExitRegion(Mobile m)
        {
            if (!AnnounceEnterExit || m == null || !m.Player)
            {
                return;
            }

            if (OwnerGuild != null)
            {
                m.SendMessage(0x3B2, $"You have left the {OwnerGuild.Name} guild territory.");
            }
            else
            {
                m.SendMessage(0x3B2, "You have left the unclaimed territory.");
            }
        }

        //public override void OnT
        #endregion ON EVENTS

        #region REGION/TERRITORY LIFECYCLE

        public PGTerritoryStoneRegion GetRegion()
        {
            return _region;
        }

        public void ExpandTerritory(int amount, Mobile payer)
        {
            int newRadius = Radius + amount;
            if (newRadius > MaxRadius)
            {
                payer.SendMessage($"Cannot expand beyond maximum radius of {MaxRadius}.");
                return;
            }

            int baseCost = amount * 2000;
            int cost = (int)(baseCost * _expansionCostMultiplier);

            if (payer.Backpack != null && payer.Backpack.ConsumeTotal(typeof(Gold), cost))
            {
                Radius = newRadius;
                RebuildRegion();
                payer.SendMessage($"Territory expanded by {amount} tiles. New radius: {Radius}.");

                // Aumenta il moltiplicatore del costo per le espansioni future
                _expansionCostMultiplier += 0.2;
            }
            else
            {
                payer.SendMessage($"You need {cost} gold to expand the territory.");
            }
        }

        private void RebuildRegion()
        {
            if (Deleted)
                return;

            if (Map == null || Map == Map.Internal)
            {
                UnregisterRegion();
                return;
            }

            UnregisterRegion();

            var rects = BuildArea(Location, Map, Radius);
            var regionName = $"Conquest:{Serial}";

            _region = new PGTerritoryStoneRegion(this, regionName, Map, RegionPriority, rects);
            _region.Register();
        }

        private void UnregisterRegion()
        {
            if (_region != null)
            {
                try
                {
                    _region.Unregister();
                }
                catch
                {
                    // Ignore if already removed
                }

                _region = null;
            }
        }

        private static Rectangle3D[] BuildArea(Point3D center, Map map, int radius)
        {
            int x1 = center.X - radius;
            int y1 = center.Y - radius;
            int x2 = center.X + radius;
            int y2 = center.Y + radius;

            return new[]
            {
                new Rectangle3D(x1, y1, -128, x2 - x1, y2 - y1, 256)
            };
        }

        // ------ CHECK TERRITORY --------
        private bool IsAreaClearForTerritory(Mobile from = null)
        {
            if (Map == null || Map == Map.Internal)
                return true;

            // Controllo 1: Altre pietre troppo vicine
            if (IsTooCloseToAnotherTerritoryStone())
            {
                from?.SendMessage("The guild stone need to be distance another stone.");
                return false;
            }

            // Controllo 2: Altre regioni nell'area
            if (AreaOverlapsWithAnyRegion())
            {
                return false;
            }

            // Controllo 3: Case o edifici player nell'area
            if (AreaContainsPlayerHouses())
            {
                return false;
            }

            return true;
        }

        private bool IsTooCloseToAnotherTerritoryStone()
        {
            // Calcola la distanza minima richiesta (doppio raggio massimo + buffer)
            int requiredDistance = (MaxRadius * 2) + MaxRadius; //MIN_DISTANCE_BETWEEN_STONES;

            foreach (Item item in World.Items.Values)
            {
                if (item is GuildTerritoryStone otherStone &&
                    !otherStone.Deleted &&
                    otherStone != this &&
                    otherStone.Map == this.Map)
                {
                    double distance = GetDistance(this.Location, otherStone.Location);

                    if (distance < requiredDistance)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // Metodo helper per calcolare la distanza
        private double GetDistance(Point3D a, Point3D b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private bool AreaOverlapsWithAnyRegion()
        {
            if (Map == null || Map == Map.Internal || _region == null)
                return false;

            // Calcola l'area del territorio
            int minX = Location.X - MaxRadius;
            int minY = Location.Y - MaxRadius;
            int maxX = Location.X + MaxRadius;
            int maxY = Location.Y + MaxRadius;

            // Ottieni tutte le regioni nella mappa
            var allRegions = Region.Regions.Where(r => r.Map == Map).ToList();

            foreach (var region in allRegions)
            {
                // Salta la regione della pietra stessa
                if (region == _region)
                    continue;

                // Salta le regioni di default che non contano (come il mondo intero)
                //if (region is Regions.DefaultRegion || region.Name == null)
                //    continue;

                // Controlla se si sovrappone con l'area del territorio
                if (RegionsOverlap(region, minX, minY, maxX, maxY))
                {
                    return true; // Trovata una regione sovrapposta
                }
            }

            return false;
        }

        private bool RegionsOverlap(Region otherRegion, int minX, int minY, int maxX, int maxY)
        {
            foreach (var rect in otherRegion.Area)
            {
                // Controlla se i rettangoli si sovrappongono
                if (rect.Start.X <= maxX && rect.End.X >= minX &&
                    rect.Start.Y <= maxY && rect.End.Y >= minY)
                {
                    return true;
                }
            }

            return false;
        }

        private bool AreaContainsPlayerHouses()
        {
            // Controlla se ci sono case di giocatori nell'area
            // Questo è un controllo semplificato - potresti doverlo adattare al tuo sistema di housing

            int minX = Location.X - MaxRadius;
            int minY = Location.Y - MaxRadius;
            int maxX = Location.X + MaxRadius;
            int maxY = Location.Y + MaxRadius;

            // Esempio per ModernUO - controlla tutte le case
            foreach (var house in BaseHouse.AllHouses)
            {
                if (house.Map == Map && !house.Deleted)
                {
                    // Controlla se la casa è nell'area
                    var houseLocation = house.Location;

                    if (houseLocation.X >= minX && houseLocation.X <= maxX &&
                        houseLocation.Y >= minY && houseLocation.Y <= maxY)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion REGION/TERRITORY LIFECYCLE

        #region CAPTURE ENGINE(KOTH)
        private void ToggleOnOffCapture(Mobile from = null)
        {
            if (Parent == null && Map != null && Map != Map.Internal)
            {
                // La stone è sulla mappa (nel mondo)
                if (IsAreaClearForTerritory(from))
                {
                    // La stone è su un territorio libero.
                    if (!Movable && OwnerGuild == null)
                    {
                        // La stone e bloccata e non conquistata.
                        RebuildRegion();
                        ResetCaptureProgress();
                        StartCaptureTimer();
                    }
                }
            }
            else
            {
                StopCaptureTimer();
                UnregisterRegion();
                ResetCaptureProgress();
                Movable = true;
                from?.SendMessage("Put the stone in the ground to use it.");
            }
        }

        private void StartCaptureTimer()
        {
            if (Deleted || Map == null || Map == Map.Internal)
                return;

            if (_timer == null)
            {
                _timer = Timer.DelayCall(TimeSpan.Zero, TickRate, Tick);
            }
        }

        private void StopCaptureTimer()
        {
            _timer?.Stop();
            _timer = null;
        }

        private void ResetCaptureProgress()
        {
            Progress = TimeSpan.Zero;
            _currentCaptorGuild = null;
        }

        private void Tick()
        {
            if (Deleted || Map == null || Map == Map.Internal)
            {
                return;
            }

            var contenders = GetContenders();

            if (contenders.Count == 0)
            {
                if (Progress > TimeSpan.Zero)
                {
                    Progress -= TimeSpan.FromSeconds(TickRate.TotalSeconds * 0.5);
                    if (Progress < TimeSpan.Zero)
                        Progress = TimeSpan.Zero;
                }
                return;
            }

            TickGuildCapture(contenders);
        }

        private List<PlayerMobile> GetContenders()
        {
            var list = new List<PlayerMobile>();

            foreach (Mobile m in GetMobilesInRange(CaptureRange))
            {
                if (m is PlayerMobile pm && pm.Alive && !pm.Hidden)
                    list.Add(pm);
            }

            return list;
        }

        private void TickGuildCapture(List<PlayerMobile> contenders)
        {
            Guild guild = null;

            foreach (var pm in contenders)
            {
                var g = pm.Guild as Guild;
                if (g == null)
                    continue;

                if (guild == null)
                    guild = g;
                else if (guild != g)
                {
                    if (AnnounceContested)
                        PublicOverheadMessage(MessageType.Regular, 0x22, false, "The stone is contested!");
                    return;
                }
            }

            if (guild == null)
                return;

            if (_currentCaptorGuild != guild)
            {
                _currentCaptorGuild = guild;
                Progress = TimeSpan.Zero;

                PublicOverheadMessage(MessageType.Regular, 0x3B2, false, $"Capture started by {guild.Name}!");
            }

            Progress += TickRate;

            if (Progress >= CaptureTime)
            {
                SetOwner(guild);
            }
            else if ((int)Progress.TotalSeconds % 10 == 0)
            {
                PublicOverheadMessage(MessageType.Regular, 0x3B2, false, $"Capturing... {PercentString()}");
            }
        }

        private string PercentString()
        {
            if (CaptureTime.TotalSeconds <= 0.1)
                return "100%";

            var pct = (int)Math.Min(100, (Progress.TotalSeconds / CaptureTime.TotalSeconds) * 100);
            return $"{pct}%";
        }

        private void SetOwner(Guild guild)
        {
            OwnerGuild = guild;
            _capturedAtUtc = DateTime.UtcNow;

            if (AnnounceCapture)
                PublicOverheadMessage(MessageType.Regular, 0x59, false, $"Territory captured by {guild.Name}!");

            InvalidateProperties();
            OnCaptured();
        }

        protected virtual void OnCaptured()
        {
            // Extension point
            StopCaptureTimer();
        }

        #endregion CAPTURE ENGINE (KOTH)

        public void WithdrawGoldTreasury(int amount, Mobile to)
        {
            if (amount <= 0)
                return;

            amount = Math.Min(amount, GoldTreasury);

            GoldTreasury -= amount;

            if (to.Backpack != null)
                to.Backpack.DropItem(new Gold(amount));

            to.SendMessage($"You withdraw {amount} gold from the territory treasury.");
        }

        // ----------------------------
        //   PROPERTIES / TOOLTIP
        // ----------------------------
        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            list.Add($"Owner: " + (OwnerGuild != null ? OwnerGuild.Name : "Unclaimed"));
            list.Add($"Radius: {Radius * 2}x{Radius * 2}");
            if (OwnerGuild == null)
            {
                list.Add($"Capture Range: {CaptureRange}");
                list.Add($"Capture Time: {CaptureTime.TotalMinutes:0.#} min");
                list.Add($"Capture Progress: {PercentString()}");
            }
        }

        // Metodi pubblici per la gestione
        public void OpenManagementGump(Mobile from)
        {
            if (from is PlayerMobile pm && GuildTerritoryManagementGump.CanManage(pm, this))
            {
                pm.SendGump(new GuildTerritoryManagementGump(this, pm));
            }
            else
            {
                from.SendMessage("You are not authorized to manage this territory.");
            }
        }

        private Point3D FindSpawnLocation(Region region)
        {
            // Trova una posizione valida nella regione
            Rectangle3D[] area = region.Area;
            if (area.Length > 0)
            {
                var rect = area[0];
                int x = Utility.RandomMinMax(rect.Start.X, rect.End.X);
                int y = Utility.RandomMinMax(rect.Start.Y, rect.End.Y);
                int z = Map.GetAverageZ(x, y);

                return new Point3D(x, y, z);
            }

            return Location;
        }

        public void UpdateAntiMagicEffect()
        {
            var region = GetRegion();
            if (region != null)
            {
                // Qui puoi aggiungere effetti visivi o modificare il comportamento della regione
                // per l'anti-magia
            }
        }

        private List<Item> GetItemsInRegion()
        {
            var list = new List<Item>();
            var region = GetRegion();
            if (region != null)
            {
                // ModernUO: usa GetItems()
                foreach (Item item in region.GetItems())
                    list.Add(item);
            }
            return list;
        }

        // Aggiungi metodo helper per enumerare mobiles nella regione
        private List<Mobile> GetMobilesInRegion()
        {
            var list = new List<Mobile>();
            var region = GetRegion();
            if (region != null)
            {
                // ModernUO: usa GetMobiles()
                foreach (Mobile m in region.GetMobiles())
                    list.Add(m);
            }
            return list;
        }

        #region VENDOR MANAGEMENT
        private BaseVendor CreateVendor(string type)
        {
            switch (type)
            {
                case "Banker": return new Banker();
                case "Blacksmith": return new Blacksmith();
                case "Provisioner": return new Provisioner();
                case "Mage": return new Mage();
                case "Armorer": return new Armorer();
                case "AnimalTrainer": return new AnimalTrainer();
                case "Alchemist": return new Alchemist();
                case "Tailor": return new Tailor();
                case "Carpenter": return new Carpenter();
                case "Tinker": return new Tinker();
                case "Bowyer": return new Bowyer();
                case "Butcher": return new Butcher();
                case "Bard": return new Bard();
                case "Herbalist": return new Herbalist();
                default: return null;
            }
        }

        public void AddVendor(string vendorType, Mobile from = null)
        {
            if (_vendors.Count >= 15)
            {
                from?.SendMessage("Maximum number of vendors (14) reached.");
                return;
            }

            //if (owner.Backpack != null && owner.Backpack.ConsumeTotal(typeof(Gold), cost))
            if ((GoldTreasury - VendorCost) >= 0 )
            {
                GoldTreasury -= VendorCost;
                BaseVendor vendor = CreateVendor(vendorType);
                if (vendor != null)
                {
                    vendor.MoveToWorld(Location, Map);
                    _vendors.Add(vendor);
                    from?.SendMessage($"{vendorType} added to territory.");
                }
            }
            else
            {
                from?.SendMessage($"You need {VendorCost} gold to add a vendor.");
            }
        }

        public void RemoveVendor(int index, Mobile from = null)
        {
            if (index >= 0 && index < _vendors.Count)
            {
                var vendor = _vendors[index];
                if (vendor != null && !vendor.Deleted)
                {
                    vendor.Delete();
                }
                _vendors.RemoveAt(index);
                from?.SendMessage($"Vendor {vendor.Name} removed.");
            }
        }

        public void MoveVendorOnYourLocation(int index, Mobile from)
        {
            if (index >= 0 && index < _vendors.Count)
            {
                var vendor = _vendors[index];
                if (vendor != null && !vendor.Deleted)
                {
                    if (GetRegion().Contains(from.Location))
                    {
                        vendor.MoveToWorld(from.Location, from.Map);
                        from.SendMessage($"Vendor {vendor.Name} moved to your location.");
                    }
                    else
                    {
                        from.SendMessage("You must be in the territory region to move the vendor.");
                    }
                }
            }
        }

        public List<BaseVendor> GetVendors()
        {
            // Pulisci la lista dai vendor eliminati
            _vendors.RemoveAll(v => v == null || v.Deleted);
            return new List<BaseVendor>(_vendors);
        }

        #endregion VENDOR MANAGEMENT

        #region GUARD MANAGEMENT
        // Create a guard
        private BaseGuard CreateGuard(Type type = null)
        {
            BaseGuard guard;
            // Crea una guardia con forza basata su GuardStrength
            if (type == typeof(WarriorGuard))
            {
                guard = new WarriorGuard(); // Guard();

            }
            else if (type == typeof(ArcherGuard))
            {
                guard = new ArcherGuard(); // Guard();
            }
            else
            {
                throw new ArgumentException($"Tipo di guardia non supportato: {type}");
            }

            // Imposta statistiche basate sulla forza
            //guard.HitsMax = 100 + (GuardStrength * 50);
            guard.RawStr = 300 + (GuardStrength * 50);
            guard.Hits = guard.HitsMax;
            //guard.DamageMin = 5 + GuardStrength;
            //guard.DamageMax = 10 + (GuardStrength * 2);
            //guard.de
            // Imposta la gilda proprietaria
            if (OwnerGuild != null)
            {
                guard.Guild = OwnerGuild;
                guard.GuildFealty = OwnerGuild.Leader;
            }

            return guard;
        }

        public void UpdateGuards()
        {
            // Se le guardie sono disabilitate o non c'è una gilda proprietaria, rimuovi tutte le guardie
            if (!SpawnGuards || OwnerGuild == null)
            {
                ClearAllGuards();
                return;
            }

            // Se non ci devono essere guardie, rimuovi tutte quelle esistenti
            if (GuardCount <= 0)
            {
                ClearAllGuards();
                return;
            }

            var region = GetRegion();
            if (region == null)
                return;

            int currentGuardCount = _guards.Count;

            // RIMUOVI GUARDIE IN ECCESSO (se ne abbiamo troppe)
            if (currentGuardCount > GuardCount)
            {
                int guardsToRemove = currentGuardCount - GuardCount;

                // Rimuovi le ultime guardie aggiunte (quelle con indice più alto)
                for (int i = currentGuardCount - 1; i >= currentGuardCount - guardsToRemove; i--)
                {
                    //RemoveGuardLast();
                    if (i >= 0 && i < _guards.Count)
                    {
                        var guard = _guards[i];
                        if (guard != null && !guard.Deleted)
                        {
                            guard.Delete();
                        }
                        // Nota: non rimuoviamo ancora dalla lista, lo faremo dopo il loop
                    }
                }

                // Rimuovi dalla lista (partendo dalla fine per mantenere gli indici corretti)
                Guards.RemoveRange(GuardCount, guardsToRemove);

                // Aggiorna il conteggio corrente
                currentGuardCount = Guards.Count;
            }

            // AGGIUNGI GUARDIE MANCANTI (se ne abbiamo poche)
            //if (currentGuardCount < GuardCount)
            //{
            //    int guardsToAdd = GuardCount - currentGuardCount;

            //    for (int i = 0; i < guardsToAdd; i++)
            //    {
            //        var guard = CreateGuard(typeof(WarriorGuard));
            //        if (guard != null)
            //        {
            //            Point3D location = FindSpawnLocation(region);
            //            guard.MoveToWorld(location, Map);
            //            _guards.Add(guard);

            //            // Applica la forza delle guardie se necessario
            //            ApplyGuardStrength(guard);
            //        }
            //    }
            //}

            // AGGIORNA LA FORZA DI TUTTE LE GUARDIE ESISTENTI
            foreach (var guard in Guards)
            {
                if (guard != null && !guard.Deleted)
                {
                    ApplyGuardStrength(guard);
                }
            }
        }

        // Spawn a guard
        public void SpawnGuard(Type type, Point3D location, Map map)
        {
            if (SpawnGuards)
            {
                BaseGuard guard = CreateGuard(type);

                if (guard != null)
                {
                    //guard.RangeHome = 10;
                    guard.MoveToWorld(location, map);

                    // Aggiungi alla lista
                    Guards.Add(guard);

                    // Applica la forza delle guardie
                    ApplyGuardStrength(guard);
                }
            }
        }

        // Versione alternativa più semplice del metodo RemoveGuard
        public void RemoveGuardLast()
        {
            if (Guards.Count > 0)
            {
                // Rimuovi l'ULTIMA guardia aggiunta
                int lastIndex = Guards.Count - 1;
                var guard = Guards[lastIndex];

                if (guard != null && !guard.Deleted)
                {
                    guard.Delete();
                }

                Guards.RemoveAt(lastIndex);
            }
        }

        public void RemoveGuard(int index, Mobile from = null)
        {
            if (index >= 0 && index < _guards.Count)
            {
                var guard = _guards[index];
                if (guard != null && !guard.Deleted)
                {
                    guard.Delete();
                }
                _guards.RemoveAt(index);
                from?.SendMessage($"Guard {guard.Name} removed.");
            }
        }

        // Metodo helper per rimuovere tutte le guardie
        private void ClearAllGuards()
        {
            foreach (var guard in _guards)
            {
                if (guard != null && !guard.Deleted)
                {
                    guard.Delete();
                }
            }
            _guards.Clear();
        }

        // Rename a guard
        public void RenameGuard(int index, string textNewName, Mobile from = null)
        {
            var guards = GetGuards();
            if (index >= 0 && index < guards.Count)
            {
                if (!string.IsNullOrWhiteSpace(textNewName))
                {
                    var guard = guards[index];
                    guard.Name = textNewName;
                    from?.SendMessage($"Guard renamed to: {textNewName}");
                }
                else
                {
                    from?.SendMessage($"{textNewName} is a invalid name.");
                }
            }
        }

        // Aggiungi questi metodi alla classe GuildTerritoryStone
        public List<BaseGuard> GetGuards()
        {
            // Pulisci la lista dalle guardie eliminate
            _guards.RemoveAll(g => g == null || g.Deleted);
            return new List<BaseGuard>(_guards);
        }

        // Metodo helper per applicare la forza
        private void ApplyGuardStrength(BaseGuard guard)
        {
            // Forza base moltiplicatore (1-10)
            int statsValue = (int)(100 * GuardStrength);
            guard.RawStr = statsValue;
            guard.Str = statsValue;
            guard.RawDex = statsValue;
            guard.Dex = statsValue;
            guard.RawInt = statsValue;
            guard.Int = statsValue;

            //guard.SetHits((int)(guard.HitsMax * multiplier));
            //guard.SetDamage((int)(guard.DamageMin * multiplier), (int)(guard.DamageMax * multiplier));

            guard.Skills.Swords.Base = (statsValue);
            guard.Skills.Archery.Base = (statsValue);
            guard.Skills.Tactics.Base = (statsValue);
            guard.Skills.Magery.Base = (statsValue);
            guard.Skills.EvalInt.Base = (statsValue);
            guard.Skills.MagicResist.Base = (statsValue);
            // guard.SetSkill(SkillName.Archery, guard.Skills[SkillName.Archery].Base * multiplier);
        }

        public void MoveGuardOnYourLocation(int index, Mobile from)
        {
            var guards = GetGuards();
            if (index >= 0 && index < guards.Count)
            {
                var guard = guards[index];
                if (GetRegion().Contains(from.Location))
                {
                    guard.MoveToWorld(from.Location, from.Map);
                    from.SendMessage($"Guard {guard.Name} moved to your location.");
                }
                else
                {
                    from.SendMessage("You must be in the territory region to move the guard.");
                }
            }
        }

        #endregion GUARD MANAGEMENT

        #region GATE MANAGEMENT
        public void UpdateGates()
        {
            _controlledGates.Clear();

            var region = GetRegion();
            if (region != null)
            {
                // ModernUO: usa GetItems() invece di GetEnumeratedItems()
                foreach (Item item in region.GetItems())
                {
                    if (IsGate(item))
                    {
                        _controlledGates.Add(item);

                        if (item is Moongate moongate)
                            moongate.Dispellable = !GatesEnabled;
                        else if (item is Teleporter teleporter)
                            teleporter.Active = GatesEnabled;
                    }
                }
            }
        }

        private bool IsGate(Item item)
        {
            return item is Moongate ||
                   item is Teleporter ||
                   item.GetType().Name.Contains("Gate");
        }

        public void ToggleGate(int index)
        {
            if (index >= 0 && index < _controlledGates.Count)
            {
                var gate = _controlledGates[index];
                if (gate is Moongate moongate)
                    moongate.Dispellable = !moongate.Dispellable;
                else if (gate is Teleporter teleporter)
                    teleporter.Active = !teleporter.Active;
            }
        }

        public bool IsGateEnabled(Item gate)
        {
            if (gate is Moongate moongate)
                return !moongate.Dispellable;
            if (gate is Teleporter teleporter)
                return teleporter.Active;
            return true;
        }

        public List<Item> GetGatesInTerritory()
        {
            UpdateGates(); // Aggiorna la lista
            return new List<Item>(_controlledGates);
        }

        #endregion GATE MANAGEMENT

        public void Destroy()
        {
            int vendorsRemoved = 0;
            int guardsRemoved = 0;
            //if (_region != null)
            //{
            //    using var players = _region.GetPlayersPooled();
            //}

            // 1. Remove all vendor partendo dall'ultimo per evitare problemi di indici
            for (int i = _vendors.Count - 1; i >= 0; i--)
            {
                PublicOverheadMessage(MessageType.Regular, 0x22, false, $"Destroy vendor(s) {_vendors[i].Name}");
                RemoveVendor(i);
            }

            // 2. Remove all guards partendo dall'ultimo per evitare problemi di indici
            for (int i = _guards.Count - 1; i >= 0; i--)
            {
                PublicOverheadMessage(MessageType.Regular, 0x22, false, $"Destroy guard(s) {_guards[i].Name}");
                RemoveGuard(i);
            }

            // 3. Pulisci la lista dei gate controllati
            _controlledGates.Clear();

            // Feedback al giocatore
            if (vendorsRemoved > 0 || guardsRemoved > 0)
            {
                PublicOverheadMessage(MessageType.Regular, 0x22, false, $"Destroyed: {vendorsRemoved} vendor(s) and {guardsRemoved} guard(s).");
            }

            if(OwnerGuild != null)
            {
                PublicOverheadMessage(MessageType.Regular, 0x22, false, $"Withdraw treasury to the guild leader.");
                OwnerGuild.Leader.SendMessage( $"Withdraw guild treasury ( {GoldTreasury}gold ) on your bank");
                WithdrawGoldTreasury(GoldTreasury, OwnerGuild.Leader);
            }

            if (_timer != null)
            {
                StopCaptureTimer();
            }
            UnregisterRegion();

            TimeSpan delay = TimeSpan.FromSeconds(0.35);
            // PlaySound(0x156);
            Server.Timer.Pause(delay);

            //foreach (var player in players)
            //{
            //    player.SendMessage(0x22, "The guild stone structure has been destroyed!");
            //}
        }

        public override void Delete()
        {
            Destroy();
            base.Delete();
        }

        // ----------------------------
        //   POST-DESERIALIZATION
        // ----------------------------
        [AfterDeserialization(false)]
        private void AfterDeserialization()
        {
            RebuildRegion();
            ToggleOnOffCapture();
        }
    }
}
