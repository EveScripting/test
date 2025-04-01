extern alias SC;
using System.Collections.Generic;
using EVESharpCore.Cache;
using SC::SharedComponents.EVE.ClientSettings;

namespace EVESharpCore.Framework
{
    public static class DirectNpcInfo
    {
        #region Fields

        private const long AmarrEmpireFactionId = 500003;
        private const long AmmatarMandateFactionId = 500007;
        private const long AngelCartelFactionId = 500011;
        private const long BloodRaiderCovenantFactionId = 500012;
        private const long CaldariStateFactionId = 500001;
        private const long ConcordAssemblyFactionId = 500006;
        private const long DeadspaceOverseerFactionId = -1;
        private const long DefaultFactionId = -1;
        private const long GallenteFederationFactionId = 500004;
        private const long GenericFactionId = -1;
        private const long GuristasPiratesFactionId = 500010;
        private const long JoveEmpireFactionId = 500005;
        private const long KhanidKingdomFactionId = 500008;
        private const long MercenatiesFactionId = -1;
        private const long MinmatarRepublicFactionId = 500002;
        private const long MordusLegionCommandFactionId = 500018;
        private const long OuterRingExcivationsFactionId = 500014;
        private const long RogueDronesFactionId = -1;
        private const long SanshasNationFactionId = 500019;
        private const long SerpentisFactionId = 500020;
        private const long SistersOfEveFactionId = 500016;
        private const long SleepersFactionId = -1;
        private const long TheInterbusFactionId = 500013;
        private const long TheSocietyofConsciousThoughtFactionId = 500017;
        private const long TheSyndicateFactionId = 500009;
        private const long ThukkerTribeFactionId = 500015;
        private const long TriglavianFactionId = -1;

        public static Faction AmarrEmpireFaction = new Faction("Amarr Empire", AmarrEmpireFactionId, DamageType.EM, DamageType.Thermal, null, null, false, "n/a", "n/a", "n/a", "n/a");
        public static Faction AmmatarMandateFaction = new Faction("Ammatar Mandate",AmmatarMandateFactionId, DamageType.EM, DamageType.Thermal, null, null, false, "n/a", "n/a", "n/a", "n/a");
        public static Faction AngelCartelFaction = new Faction("Angel Cartel",AngelCartelFactionId, DamageType.Explosive, DamageType.Kinetic, null, null, true, "Domination ", "Gistii ", "Gistum ", "Gist ");
        public static Faction BloodRaiderCovenantFaction = new Faction("Blood Raider Covenant",BloodRaiderCovenantFactionId, DamageType.EM, DamageType.Thermal, null, null, true, "Dark Blood ", "Corpii ", "Corpum ", "Corpus ");
        public static Faction CaldariStateFaction = new Faction("Caldari State",CaldariStateFactionId, DamageType.Kinetic, DamageType.Thermal, null, null, false, "n/a", "n/a", "n/a", "n/a");
        public static Faction ConcordAssemblyFaction = new Faction("CONCORD Assembly",ConcordAssemblyFactionId, DamageType.Kinetic, DamageType.Thermal, null, null, false, "n/a", "n/a", "n/a", "n/a");
        public static Faction DefaultFaction = new Faction("Default", GuristasPiratesFactionId, MissionSettings.DefaultDamageType, DamageType.Thermal, null, null, true, "n/a", "n/a", "n/a", "n/a");
        public static Faction EoM = new Faction("EoM",MercenatiesFactionId, DamageType.Kinetic, null, null, null, true, "n/a", "n/a", "n/a", "n/a");
        public static Faction GallenteFederationFaction = new Faction("Gallente Federation",GallenteFederationFactionId, DamageType.Kinetic, DamageType.Thermal, null, null, false, "n/a", "n/a", "n/a", "n/a");
        public static Faction GuristasPiratesFaction = new Faction("Guristas Pirates",GuristasPiratesFactionId, DamageType.Kinetic, DamageType.Thermal, null, null, true, "Dread Guristas ", "Pithi ", "Pithum ", "Pith ");
        //public Faction JoveEmpireFaction = new Faction("Jove", DamageType.EM, DamageType.Thermal);
        public static Faction KhanidKingdomFaction = new Faction("Khanid Kingdom",KhanidKingdomFactionId, DamageType.EM, DamageType.Thermal, null, null, false, "n/a", "n/a", "n/a", "n/a");
        public static Faction MercenariesFaction = new Faction("Mercenaries",MercenatiesFactionId, DamageType.Kinetic, DamageType.Thermal, null, null, true, "n/a", "n/a", "n/a", "n/a");
        public static Faction MinmatarRepublicFaction = new Faction("Minmater Republic",MinmatarRepublicFactionId, DamageType.Explosive, DamageType.Kinetic, null, null, false, "n/a", "n/a", "n/a", "n/a");
        public static Faction MordusLegionCommandFaction = new Faction("Mordus Legion",MordusLegionCommandFactionId, DamageType.Kinetic, DamageType.EM, null, null, true, "n/a", "n/a", "n/a", "n/a");
        //public Faction OuterRingExcivationsFaction = new Faction("OuterRing", DamageType.EM, DamageType.Thermal);
        public static Faction RogueDronesFaction = new Faction("RogueDrones",RogueDronesFactionId, DamageType.EM, DamageType.Thermal, null, null, true, "n/a", "n/a", "n/a", "n/a");
        public static Faction SanshasNationFaction = new Faction("Sansha",SanshasNationFactionId, DamageType.EM, DamageType.Thermal, null, null, true, "True Sansha ", "Centii ", "Centum ", "Centus ");
        public static Faction SerpentisFaction = new Faction("Serpentis",SerpentisFactionId, DamageType.Kinetic, DamageType.Thermal, null, null, true, "Shadow Serpentis ", "Corelia", "Corelum ", "Core ");
        public static Faction TriglavianFaction = new Faction("Triglavian",TriglavianFactionId, DamageType.Kinetic, DamageType.Thermal, DamageType.EM, DamageType.Explosive, true, "Veles", "Zorya", "Zorya", "Zorya");
        //public Faction SistersOfEveFaction = new Faction("SistersOfEve", DamageType.EM);
        //public Faction TheInterbusFaction = new Faction("Interbus", DamageType.EM, DamageType.Thermal);
        //public Faction TheSocietyofConsciousThoughtFaction = new Faction("SocietyOfConsciousThought", DamageType.EM, DamageType.Thermal);
        //public Faction TheSyndicateFaction = new Faction("Syndicate", DamageType.EM, DamageType.Thermal);
        //public Faction ThukkerTribeFaction = new Faction("ThukkerTribe", DamageType.EM, DamageType.Thermal);

        public static Dictionary<string, string> FactionIdsToFactionNames = new Dictionary<string, string>
        {
            {"500001", "Caldari State"},
            {"500002", "Minmatar Republic"},
            {"500003", "Amarr Empire"},
            {"500004", "Gallente Federation"},
            {"500005", "Jove Empire"},
            {"500006", "Concord Assembly"},
            {"500007", "Ammatar Mandate"},
            {"500008", "Khanid Kingdom"},
            {"500009", "The Syndicate"},
            {"500010", "Guristas Pirates"},
            {"500011", "Angel Cartel"},
            {"500012", "Blood Raider Covenant"},
            {"500013", "The InterBus"},
            {"500014", "Outer Ring Excavations"},
            {"500015", "Thukker Tribe"},
            {"500016", "Sisters of EVE"},
            {"500017", "The Society of Conscious Thought"},
            {"500018", "Mordu's Legion Command"},
            {"500019", "Sansha's Nation"},
            {"500020", "Serpentis"},
            {"500021", "unknown"}
        };

        public static Dictionary<string, Faction> FactionIdsToFactions = new Dictionary<string, Faction>
        {
            {"0", DefaultFaction},
            {"500001", CaldariStateFaction},
            {"500002", MinmatarRepublicFaction},
            {"500003", AmarrEmpireFaction},
            {"500004", GallenteFederationFaction},
            //{"500005", "Jove Empire"},
            {"500006", ConcordAssemblyFaction },
            {"500007", AmmatarMandateFaction},
            {"500008", KhanidKingdomFaction},
            //{"500009", "The Syndicate"},
            {"500010", GuristasPiratesFaction},
            {"500011", AngelCartelFaction},
            {"500012", BloodRaiderCovenantFaction},
            //{"500013", "The InterBus"},
            //{"500014", "Outer Ring Excavations"},
            //{"500015", "Thukker Tribe"},
            //{"500016", "Sisters of EVE"},
            //{"500017", "The Society of Conscious Thought"},
            {"500018", MordusLegionCommandFaction},
            {"500019", SanshasNationFaction},
            {"500020", SerpentisFaction},
            //{"500021", "unknown"}
        };

        public static Dictionary<string, string> NpcCorpIdsToFactionIDs = new Dictionary<string, string>
        {
            {"1000001", "0"},
            {"1000002", "500001"},
            {"1000003", "500001"},
            {"1000004", "500001"},
            {"1000005", "500001"},
            {"1000006", "500001"},
            {"1000007", "500001"},
            {"1000008", "500001"},
            {"1000009", "500001"},
            {"1000010", "500001"},
            {"1000011", "500001"},
            {"1000012", "500001"},
            {"1000013", "500001"},
            {"1000014", "500001"},
            {"1000015", "500001"},
            {"1000016", "500001"},
            {"1000017", "500001"},
            {"1000018", "500001"},
            {"1000019", "500001"},
            {"1000020", "500001"},
            {"1000021", "500001"},
            {"1000022", "500001"},
            {"1000023", "500001"},
            {"1000024", "500001"},
            {"1000025", "500001"},
            {"1000026", "500001"},
            {"1000027", "500001"},
            {"1000028", "500001"},
            {"1000029", "500001"},
            {"1000030", "500001"},
            {"1000031", "500001"},
            {"1000032", "500001"},
            {"1000033", "500001"},
            {"1000034", "500001"},
            {"1000035", "500001"},
            {"1000036", "500001"},
            {"1000037", "500001"},
            {"1000038", "500001"},
            {"1000039", "500001"},
            {"1000040", "500001"},
            {"1000041", "500001"},
            {"1000042", "500001"},
            {"1000043", "500001"},
            {"1000044", "500001"},
            {"1000045", "500001"},
            {"1000046", "500002"},
            {"1000047", "500002"},
            {"1000048", "500002"},
            {"1000049", "500002"},
            {"1000050", "500002"},
            {"1000051", "500002"},
            {"1000052", "500002"},
            {"1000053", "500002"},
            {"1000054", "500002"},
            {"1000055", "500002"},
            {"1000056", "500002"},
            {"1000057", "500002"},
            {"1000058", "500002"},
            {"1000059", "500002"},
            {"1000060", "500002"},
            {"1000061", "500002"},
            {"1000062", "500002"},
            {"1000063", "500003"},
            {"1000064", "500003"},
            {"1000065", "500003"},
            {"1000066", "500003"},
            {"1000067", "500003"},
            {"1000068", "500003"},
            {"1000069", "500003"},
            {"1000070", "500003"},
            {"1000071", "500003"},
            {"1000072", "500003"},
            {"1000073", "500003"},
            {"1000074", "500003"},
            {"1000075", "500003"},
            {"1000076", "500003"},
            {"1000077", "500003"},
            {"1000078", "500003"},
            {"1000079", "500003"},
            {"1000080", "500003"},
            {"1000081", "500003"},
            {"1000082", "500003"},
            {"1000083", "500003"},
            {"1000084", "500003"},
            {"1000085", "500003"},
            {"1000086", "500003"},
            {"1000087", "500003"},
            {"1000088", "500003"},
            {"1000089", "500003"},
            {"1000090", "500003"},
            {"1000091", "500003"},
            {"1000092", "500003"},
            {"1000093", "500003"},
            {"1000094", "500004"},
            {"1000095", "500004"},
            {"1000096", "500004"},
            {"1000097", "500004"},
            {"1000098", "500004"},
            {"1000099", "500004"},
            {"1000100", "500004"},
            {"1000101", "500004"},
            {"1000102", "500004"},
            {"1000103", "500004"},
            {"1000104", "500004"},
            {"1000105", "500004"},
            {"1000106", "500004"},
            {"1000107", "500004"},
            {"1000108", "500004"},
            {"1000109", "500004"},
            {"1000110", "500004"},
            {"1000111", "500004"},
            {"1000112", "500004"},
            {"1000113", "500004"},
            {"1000114", "500004"},
            {"1000115", "500004"},
            {"1000116", "500004"},
            {"1000117", "500004"},
            {"1000118", "500004"},
            {"1000119", "500004"},
            {"1000120", "500004"},
            {"1000121", "500004"},
            {"1000122", "500004"},
            {"1000123", "500007"},
            {"1000124", "500011"},
            {"1000125", "500006"},
            {"1000126", "500007"},
            {"1000127", "500010"},
            {"1000128", "500018"},
            {"1000129", "500014"},
            {"1000130", "500016"},
            {"1000131", "500017"},
            {"1000132", "500006"},
            {"1000133", "500011"},
            {"1000134", "500012"},
            {"1000135", "500020"},
            {"1000136", "500011"},
            {"1000137", "500006"},
            {"1000138", "500011"},
            {"1000139", "500016"},
            {"1000140", "500005"},
            {"1000141", "500010"},
            {"1000142", "500005"},
            {"1000143", "500006"},
            {"1000144", "500009"},
            {"1000145", "500009"},
            {"1000146", "500009"},
            {"1000147", "500009"},
            {"1000148", "500013"},
            {"1000149", "500005"},
            {"1000150", "500005"},
            {"1000151", "500008"},
            {"1000152", "500008"},
            {"1000153", "500008"},
            {"1000154", "500007"},
            {"1000155", "500005"},
            {"1000156", "500008"},
            {"1000157", "500020"},
            {"1000158", "500005"},
            {"1000159", "500016"},
            {"1000160", "500015"},
            {"1000161", "500019"},
            {"1000162", "500019"},
            {"1000163", "500015"},
            {"1000164", "500005"},
            {"1000165", "500003"},
            {"1000166", "500003"},
            {"1000167", "500001"},
            {"1000168", "500004"},
            {"1000169", "500004"},
            {"1000170", "500002"},
            {"1000171", "500002"},
            {"1000172", "500002"},
            {"1000177", "500005"},
            {"1000178", "500005"},
            {"1000179", "500003"},
            {"1000180", "500001"},
            {"1000181", "500004"},
            {"1000182", "500002"},
            {"1000193", "500021"},
            {"1000197", "500001"},
            {"1000198", "500003"},
            {"1000205", "500003"},
            {"1000206", "500003"},
            {"1000207", "500003"},
            {"1000208", "500001"},
            {"1000213", "500001"},
            {"1000214", "500001"},
            {"1000215", "500004"},
            {"1000216", "500004"},
            {"1000217", "500004"},
            {"1000218", "500002"},
            {"1000219", "500002"},
            {"1000220", "500002"},
            {"1000261", "500001"},
            {"1000262", "500004"},
            {"1000263", "500002"}
        };

        public static Dictionary<string, string> NpcCorpIdsToFactionNames = new Dictionary<string, string>
        {
            {"1000001", "n/a"},
            {"1000002", "Caldari State"},
            {"1000003", "Caldari State"},
            {"1000004", "Caldari State"},
            {"1000005", "Caldari State"},
            {"1000006", "Caldari State"},
            {"1000007", "Caldari State"},
            {"1000008", "Caldari State"},
            {"1000009", "Caldari State"},
            {"1000010", "Caldari State"},
            {"1000011", "Caldari State"},
            {"1000012", "Caldari State"},
            {"1000013", "Caldari State"},
            {"1000014", "Caldari State"},
            {"1000015", "Caldari State"},
            {"1000016", "Caldari State"},
            {"1000017", "Caldari State"},
            {"1000018", "Caldari State"},
            {"1000019", "Caldari State"},
            {"1000020", "Caldari State"},
            {"1000021", "Caldari State"},
            {"1000022", "Caldari State"},
            {"1000023", "Caldari State"},
            {"1000024", "Caldari State"},
            {"1000025", "Caldari State"},
            {"1000026", "Caldari State"},
            {"1000027", "Caldari State"},
            {"1000028", "Caldari State"},
            {"1000029", "Caldari State"},
            {"1000030", "Caldari State"},
            {"1000031", "Caldari State"},
            {"1000032", "Caldari State"},
            {"1000033", "Caldari State"},
            {"1000034", "Caldari State"},
            {"1000035", "Caldari State"},
            {"1000036", "Caldari State"},
            {"1000037", "Caldari State"},
            {"1000038", "Caldari State"},
            {"1000039", "Caldari State"},
            {"1000040", "Caldari State"},
            {"1000041", "Caldari State"},
            {"1000042", "Caldari State"},
            {"1000043", "Caldari State"},
            {"1000044", "Caldari State"},
            {"1000045", "Caldari State"},
            {"1000046", "Minmatar Republic"},
            {"1000047", "Minmatar Republic"},
            {"1000048", "Minmatar Republic"},
            {"1000049", "Minmatar Republic"},
            {"1000050", "Minmatar Republic"},
            {"1000051", "Minmatar Republic"},
            {"1000052", "Minmatar Republic"},
            {"1000053", "Minmatar Republic"},
            {"1000054", "Minmatar Republic"},
            {"1000055", "Minmatar Republic"},
            {"1000056", "Minmatar Republic"},
            {"1000057", "Minmatar Republic"},
            {"1000058", "Minmatar Republic"},
            {"1000059", "Minmatar Republic"},
            {"1000060", "Minmatar Republic"},
            {"1000061", "Minmatar Republic"},
            {"1000062", "Minmatar Republic"},
            {"1000063", "Amarr Empire"},
            {"1000064", "Amarr Empire"},
            {"1000065", "Amarr Empire"},
            {"1000066", "Amarr Empire"},
            {"1000067", "Amarr Empire"},
            {"1000068", "Amarr Empire"},
            {"1000069", "Amarr Empire"},
            {"1000070", "Amarr Empire"},
            {"1000071", "Amarr Empire"},
            {"1000072", "Amarr Empire"},
            {"1000073", "Amarr Empire"},
            {"1000074", "Amarr Empire"},
            {"1000075", "Amarr Empire"},
            {"1000076", "Amarr Empire"},
            {"1000077", "Amarr Empire"},
            {"1000078", "Amarr Empire"},
            {"1000079", "Amarr Empire"},
            {"1000080", "Amarr Empire"},
            {"1000081", "Amarr Empire"},
            {"1000082", "Amarr Empire"},
            {"1000083", "Amarr Empire"},
            {"1000084", "Amarr Empire"},
            {"1000085", "Amarr Empire"},
            {"1000086", "Amarr Empire"},
            {"1000087", "Amarr Empire"},
            {"1000088", "Amarr Empire"},
            {"1000089", "Amarr Empire"},
            {"1000090", "Amarr Empire"},
            {"1000091", "Amarr Empire"},
            {"1000092", "Amarr Empire"},
            {"1000093", "Amarr Empire"},
            {"1000094", "Gallente Federation"},
            {"1000095", "Gallente Federation"},
            {"1000096", "Gallente Federation"},
            {"1000097", "Gallente Federation"},
            {"1000098", "Gallente Federation"},
            {"1000099", "Gallente Federation"},
            {"1000100", "Gallente Federation"},
            {"1000101", "Gallente Federation"},
            {"1000102", "Gallente Federation"},
            {"1000103", "Gallente Federation"},
            {"1000104", "Gallente Federation"},
            {"1000105", "Gallente Federation"},
            {"1000106", "Gallente Federation"},
            {"1000107", "Gallente Federation"},
            {"1000108", "Gallente Federation"},
            {"1000109", "Gallente Federation"},
            {"1000110", "Gallente Federation"},
            {"1000111", "Gallente Federation"},
            {"1000112", "Gallente Federation"},
            {"1000113", "Gallente Federation"},
            {"1000114", "Gallente Federation"},
            {"1000115", "Gallente Federation"},
            {"1000116", "Gallente Federation"},
            {"1000117", "Gallente Federation"},
            {"1000118", "Gallente Federation"},
            {"1000119", "Gallente Federation"},
            {"1000120", "Gallente Federation"},
            {"1000121", "Gallente Federation"},
            {"1000122", "Gallente Federation"},
            {"1000123", "Ammatar Mandate"},
            {"1000124", "Angel Cartel"},
            {"1000125", "Concord Assembly"},
            {"1000126", "Ammatar Mandate"},
            {"1000127", "Guristas Pirates"},
            {"1000128", "Mordu's Legion Command"},
            {"1000129", "Outer Ring Excavations"},
            {"1000130", "Sisters of EVE"},
            {"1000131", "The Society of Conscious Thought"},
            {"1000132", "Concord Assembly"},
            {"1000133", "Angel Cartel"},
            {"1000134", "Blood Raider Covenant"},
            {"1000135", "Serpentis"},
            {"1000136", "Angel Cartel"},
            {"1000137", "Concord Assembly"},
            {"1000138", "Angel Cartel"},
            {"1000139", "Sisters of EVE"},
            {"1000140", "Jove Empire"},
            {"1000141", "Guristas Pirates"},
            {"1000142", "Jove Empire"},
            {"1000143", "Concord Assembly"},
            {"1000144", "The Syndicate"},
            {"1000145", "The Syndicate"},
            {"1000146", "The Syndicate"},
            {"1000147", "The Syndicate"},
            {"1000148", "The InterBus"},
            {"1000149", "Jove Empire"},
            {"1000150", "Jove Empire"},
            {"1000151", "Khanid Kingdom"},
            {"1000152", "Khanid Kingdom"},
            {"1000153", "Khanid Kingdom"},
            {"1000154", "Ammatar Mandate"},
            {"1000155", "Jove Empire"},
            {"1000156", "Khanid Kingdom"},
            {"1000157", "Serpentis"},
            {"1000158", "Jove Empire"},
            {"1000159", "Sisters of EVE"},
            {"1000160", "Thukker Tribe"},
            {"1000161", "Sansha's Nation"},
            {"1000162", "Sansha's Nation"},
            {"1000163", "Thukker Tribe"},
            {"1000164", "Jove Empire"},
            {"1000165", "Amarr Empire"},
            {"1000166", "Amarr Empire"},
            {"1000167", "Caldari State"},
            {"1000168", "Gallente Federation"},
            {"1000169", "Gallente Federation"},
            {"1000170", "Minmatar Republic"},
            {"1000171", "Minmatar Republic"},
            {"1000172", "Minmatar Republic"},
            {"1000177", "Jove Empire"},
            {"1000178", "Jove Empire"},
            {"1000179", "Amarr Empire"},
            {"1000180", "Caldari State"},
            {"1000181", "Gallente Federation"},
            {"1000182", "Minmatar Republic"},
            {"1000193", "unknown"},
            {"1000197", "Caldari State"},
            {"1000198", "Amarr Empire"},
            {"1000205", "Amarr Empire"},
            {"1000206", "Amarr Empire"},
            {"1000207", "Amarr Empire"},
            {"1000208", "Caldari State"},
            {"1000213", "Caldari State"},
            {"1000214", "Caldari State"},
            {"1000215", "Gallente Federation"},
            {"1000216", "Gallente Federation"},
            {"1000217", "Gallente Federation"},
            {"1000218", "Minmatar Republic"},
            {"1000219", "Minmatar Republic"},
            {"1000220", "Minmatar Republic"},
            {"1000261", "Caldari State"},
            {"1000262", "Gallente Federation"},
            {"1000263", "Minmatar Republic"}
        };

        public static Dictionary<string, string> NpcCorpIdsToNames = new Dictionary<string, string>
        {
            {"1000001", "Doomheim"},
            {"1000002", "CBD Corporation"},
            {"1000003", "Prompt Delivery"},
            {"1000004", "Ytiri"},
            {"1000005", "Hyasyoda Corporation"},
            {"1000006", "Deep Core Mining Inc."},
            {"1000007", "Poksu Mineral Group"},
            {"1000008", "Minedrill"},
            {"1000009", "Caldari Provisions"},
            {"1000010", "Kaalakiota Corporation"},
            {"1000011", "Wiyrkomi Corporation"},
            {"1000012", "Top Down"},
            {"1000013", "Rapid Assembly"},
            {"1000014", "Perkone"},
            {"1000015", "Caldari Steel"},
            {"1000016", "Zainou"},
            {"1000017", "Nugoeihuvi Corporation"},
            {"1000018", "Echelon Entertainment"},
            {"1000019", "Ishukone Corporation"},
            {"1000020", "Lai Dai Corporation"},
            {"1000021", "Zero-G Research Firm"},
            {"1000022", "Propel Dynamics"},
            {"1000023", "Expert Distribution"},
            {"1000024", "CBD Sell Division"},
            {"1000025", "Sukuuvestaa Corporation"},
            {"1000026", "Caldari Constructions"},
            {"1000027", "Expert Housing"},
            {"1000028", "Caldari Funds Unlimited"},
            {"1000029", "State and Region Bank"},
            {"1000030", "Modern Finances"},
            {"1000031", "Chief Executive Panel"},
            {"1000032", "Mercantile Club"},
            {"1000033", "Caldari Business Tribunal"},
            {"1000034", "House of Records"},
            {"1000035", "Caldari Navy"},
            {"1000036", "Internal Security"},
            {"1000037", "Lai Dai Protection Service"},
            {"1000038", "Ishukone Watch"},
            {"1000039", "Home Guard"},
            {"1000040", "Peace and Order Unit"},
            {"1000041", "Spacelane Patrol"},
            {"1000042", "Wiyrkomi Peace Corps"},
            {"1000043", "Corporate Police Force"},
            {"1000044", "School of Applied Knowledge"},
            {"1000045", "Science and Trade Institute"},
            {"1000046", "Sebiestor Tribe"},
            {"1000047", "Krusual Tribe"},
            {"1000048", "Vherokior Tribe"},
            {"1000049", "Brutor Tribe"},
            {"1000050", "Republic Parliament"},
            {"1000051", "Republic Fleet"},
            {"1000052", "Republic Justice Department"},
            {"1000053", "Urban Management"},
            {"1000054", "Republic Security Services"},
            {"1000055", "Minmatar Mining Corporation"},
            {"1000056", "Core Complexion Inc."},
            {"1000057", "Boundless Creation"},
            {"1000058", "Eifyr and Co."},
            {"1000059", "Six Kin Development"},
            {"1000060", "Native Freshfood"},
            {"1000061", "Freedom Extension"},
            {"1000062", "The Leisure Group"},
            {"1000063", "Amarr Constructions"},
            {"1000064", "Carthum Conglomerate"},
            {"1000065", "Imperial Armaments"},
            {"1000066", "Viziam"},
            {"1000067", "Zoar and Sons"},
            {"1000068", "Noble Appliances"},
            {"1000069", "Ducia Foundry"},
            {"1000070", "HZO Refinery"},
            {"1000071", "Inherent Implants"},
            {"1000072", "Imperial Shipment"},
            {"1000073", "Amarr Certified News"},
            {"1000074", "Joint Harvesting"},
            {"1000075", "Nurtura"},
            {"1000076", "Further Foodstuffs"},
            {"1000077", "Royal Amarr Institute"},
            {"1000078", "Imperial Chancellor"},
            {"1000079", "Amarr Civil Service"},
            {"1000080", "Ministry of War"},
            {"1000081", "Ministry of Assessment"},
            {"1000082", "Ministry of Internal Order"},
            {"1000083", "Amarr Trade Registry"},
            {"1000084", "Amarr Navy"},
            {"1000085", "Court Chamberlain"},
            {"1000086", "Emperor Family"},
            {"1000087", "Kador Family"},
            {"1000088", "Sarum Family"},
            {"1000089", "Kor-Azor Family"},
            {"1000090", "Ardishapur Family"},
            {"1000091", "Tash-Murkon Family"},
            {"1000092", "Civic Court"},
            {"1000093", "Theology Council"},
            {"1000094", "TransStellar Shipping"},
            {"1000095", "Federal Freight"},
            {"1000096", "Inner Zone Shipping"},
            {"1000097", "Material Acquisition"},
            {"1000098", "Astral Mining Inc."},
            {"1000099", "Combined Harvest"},
            {"1000100", "Quafe Company"},
            {"1000101", "CreoDron"},
            {"1000102", "Roden Shipyards"},
            {"1000103", "Allotek Industries"},
            {"1000104", "Poteque Pharmaceuticals"},
            {"1000105", "Impetus"},
            {"1000106", "Egonics Inc."},
            {"1000107", "The Scope"},
            {"1000108", "Chemal Tech"},
            {"1000109", "Duvolle Laboratories"},
            {"1000110", "FedMart"},
            {"1000111", "Aliastra"},
            {"1000112", "Bank of Luminaire"},
            {"1000113", "Pend Insurance"},
            {"1000114", "Garoun Investment Bank"},
            {"1000115", "University of Caille"},
            {"1000116", "President"},
            {"1000117", "Senate"},
            {"1000118", "Supreme Court"},
            {"1000119", "Federal Administration"},
            {"1000120", "Federation Navy"},
            {"1000121", "Federal Intelligence Office"},
            {"1000122", "Federation Customs"},
            {"1000123", "Ammatar Fleet"},
            {"1000124", "Archangels"},
            {"1000125", "CONCORD"},
            {"1000126", "Ammatar Consulate"},
            {"1000127", "Guristas"},
            {"1000128", "Mordu's Legion"},
            {"1000129", "Outer Ring Excavations"},
            {"1000130", "Sisters of EVE"},
            {"1000131", "Society of Conscious Thought"},
            {"1000132", "Secure Commerce Commission"},
            {"1000133", "Salvation Angels"},
            {"1000134", "Blood Raiders"},
            {"1000135", "Serpentis Corporation"},
            {"1000136", "Guardian Angels"},
            {"1000137", "DED"},
            {"1000138", "Dominations"},
            {"1000139", "Food Relief"},
            {"1000140", "Genolution"},
            {"1000141", "Guristas Production"},
            {"1000142", "Impro"},
            {"1000143", "Inner Circle"},
            {"1000144", "Intaki Bank"},
            {"1000145", "Intaki Commerce"},
            {"1000146", "Intaki Space Police"},
            {"1000147", "Intaki Syndicate"},
            {"1000148", "InterBus"},
            {"1000149", "Jove Navy"},
            {"1000150", "Jovian Directorate"},
            {"1000151", "Khanid Innovation"},
            {"1000152", "Khanid Transport"},
            {"1000153", "Khanid Works"},
            {"1000154", "Nefantar Miner Association"},
            {"1000155", "Prosper"},
            {"1000156", "Royal Khanid Navy"},
            {"1000157", "Serpentis Inquest"},
            {"1000158", "Shapeset"},
            {"1000159", "The Sanctuary"},
            {"1000160", "Thukker Mix"},
            {"1000161", "True Creations"},
            {"1000162", "True Power"},
            {"1000163", "Trust Partners"},
            {"1000164", "X-Sense"},
            {"1000165", "Hedion University"},
            {"1000166", "Imperial Academy"},
            {"1000167", "State War Academy"},
            {"1000168", "Federal Navy Academy"},
            {"1000169", "Center for Advanced Studies"},
            {"1000170", "Republic Military School"},
            {"1000171", "Republic University"},
            {"1000172", "Pator Tech School"},
            {"1000177", "Material Institute"},
            {"1000178", "Academy of Aggressive Behaviour"},
            {"1000179", "24th Imperial Crusade"},
            {"1000180", "State Protectorate"},
            {"1000181", "Federal Defense Union"},
            {"1000182", "Tribal Liberation Force"},
            {"1000193", "Arkombine"},
            {"1000197", "Templis Dragonaurs"},
            {"1000198", "Imperial Guard"},
            {"1000205", "Amarr Templars"},
            {"1000206", "Royal Uhlans"},
            {"1000207", "Bragian Order"},
            {"1000208", "Zumari Force Projection"},
            {"1000213", "Osmon Surveillance"},
            {"1000214", "Seituoda Taskforce Command"},
            {"1000215", "Algintal Core"},
            {"1000216", "Crux Special Tasks Group"},
            {"1000217", "Villore Sec Ops"},
            {"1000218", "Circle of Huskarl"},
            {"1000219", "Tronhadar Free Guard"},
            {"1000220", "Sanmatar Kelkoons"},
            {"1000261", "State Peacekeepers"},
            {"1000262", "Federal Marines"},
            {"1000263", "Republic Command"}
        };

        #endregion Fields
    }

    public class Faction
    {
        public Faction(string myFactionName,
                        long id,
                        DamageType? myBestDamageTypeToShoot,
                        DamageType? mySecondBestDamageTypeToShoot,
                        DamageType? myThirdDamageTypeToShoot,
                        DamageType? myFourthBestDamageTypeToShoot,
                        //DamageType? myBestDamageTypeToResist,
                        //DamageType? mySecondBestDamageTypeToResist,
                        //DamageType? myThirdBestDamageTypeToResist,
                        //DamageType? myFourthBestDamageTypeToResist,
                        //bool myUsesECM,
                        //bool myUsesEnergyVampires,
                        //bool myUsesSensorDampeners,
                        //bool myUsesTargetPainters,
                        //bool myUsesTrackingDistruptors
                        bool pirateFaction,
                        string highTierNameWillContain,
                        string deadspaceFrigateNameWillContain,
                        string deadspaceCruiserNameWillContain,
                        string deadspaceBattleshipNameWillContain
                        )
        {
            Name = myFactionName;
            Id = id;
            if (BestDamageTypeToShoot != null)
                BestDamageTypeToShoot = myBestDamageTypeToShoot;

            if (SecondBestDamageTypeToShoot != null)
                SecondBestDamageTypeToShoot = mySecondBestDamageTypeToShoot;

            if (ThirdBestDamageTypeToShoot != null)
                ThirdBestDamageTypeToShoot = mySecondBestDamageTypeToShoot;

            if (FourthBestDamageTypeToShoot != null)
                FourthBestDamageTypeToShoot = mySecondBestDamageTypeToShoot;

            //if (BestDamageTypeToResist != null)
            //    BestDamageTypeToResist = myBestDamageTypeToResist;

            //if (SecondBestDamageTypeToResist != null)
            //    SecondBestDamageTypeToResist = mySecondBestDamageTypeToResist;

            //UsesECM = myUsesECM;
            //UsesEnergyVampires = myUsesEnergyVampires;
            //UsesSensorDampeners = myUsesSensorDampeners;
            //UsesTargetPainters = myUsesTargetPainters;
            //UsesTrackingDistruptors = myUsesTrackingDistruptors;
            PirateFaction = pirateFaction;
            HighTierNameWillContain = highTierNameWillContain;
            DeadspaceFrigateNameWillContain = deadspaceFrigateNameWillContain;
            DeadspaceCruiserNameWillContain = deadspaceCruiserNameWillContain;
            DeadspaceBattleshipNameWillContain = deadspaceBattleshipNameWillContain;
        }

        public long Id;
        public bool PirateFaction;
        public string HighTierNameWillContain;
        public string HighTierNameWillNot;
        public string DeadspaceFrigateNameWillContain;
        public string DeadspaceCruiserNameWillContain;
        public string DeadspaceBattleshipNameWillContain;

        public string Name;
        public DamageType? BestDamageTypeToShoot;
        public DamageType? SecondBestDamageTypeToShoot;
        public DamageType? ThirdBestDamageTypeToShoot;
        public DamageType? FourthBestDamageTypeToShoot;
        public DamageType? BestDamageTypeToResist;
        public DamageType? SecondBestDamageTypeToResist;

        public List<DamageType> BestDamageTypesToShoot
        {
            get
            {
                if (BestDamageTypeToShoot == null)
                    return new List<DamageType>();

                if (SecondBestDamageTypeToShoot == null)
                    return  new List<DamageType>()
                    {
                        (DamageType)BestDamageTypeToShoot
                    };

                if (ThirdBestDamageTypeToShoot == null)
                    return new List<DamageType>()
                    {
                        (DamageType)BestDamageTypeToShoot,
                        (DamageType)SecondBestDamageTypeToShoot
                    };

                if (FourthBestDamageTypeToShoot == null)
                    return new List<DamageType>()
                    {
                        (DamageType)BestDamageTypeToShoot,
                        (DamageType)SecondBestDamageTypeToShoot,
                        (DamageType)ThirdBestDamageTypeToShoot
                    };

                return new List<DamageType>()
                {
                    (DamageType)BestDamageTypeToShoot,
                    (DamageType)SecondBestDamageTypeToShoot,
                    (DamageType)ThirdBestDamageTypeToShoot,
                    (DamageType)FourthBestDamageTypeToShoot
                };
            }
        }

        public bool UsesECM;
        public bool UsesEnergyVampires;
        public bool UsesSensorDampeners;
        public bool UsesTargetPainters;
        public bool UsesTrackingDistruptors;
    }

    public static class Factions
    {
        #region Properties

        public static string GetFactionsXML => @"<factions>
						  <faction logo=""500001"" name=""Caldari State"" damagetype=""Kinetic"" />
						  <faction logo=""500002"" name=""Minmatar Republic"" damagetype=""Explosive"" />
						  <faction logo=""500003"" name=""Amarr Empire"" damagetype=""EM"" />
						  <faction logo=""500004"" name=""Gallente Federation"" damagetype=""Kinetic"" />
						  <faction logo=""500006"" name=""CONCORD Assembly"" damagetype=""Thermal"" />
						  <faction logo=""500007"" name=""Ammatar Mandate"" damagetype=""EM"" />
						  <faction logo=""500008"" name=""Khanid Kingdom"" damagetype=""Thermal"" />
						  <faction logo=""500015"" name=""Thukker Tribe"" damagetype=""Thermal"" />
						  <faction logo=""500018"" name=""Mordu's Legion Command"" damagetype=""Kinetic"" />
						  <faction logo=""500010"" name=""Guristas Pirates"" damagetype=""Kinetic"" />
						  <faction logo=""500011"" name=""Angel Cartel"" damagetype=""Explosive"" />
						  <faction logo=""500012"" name=""Blood Raiders"" damagetype=""EM"" />
						  <faction logo=""500019"" name=""Sansha's Nation"" damagetype=""EM"" />
						  <faction logo=""500020"" name=""Serpentis"" damagetype=""Kinetic"" />
						  <faction logo=""500005"" name=""Jovian Directorate"" damagetype=""Kinetic"" />
						  <faction logo=""500009"" name=""Intaki Syndicate"" damagetype=""Kinetic"" />
						  <faction logo=""500013"" name=""Interbus"" damagetype=""Kinetic"" />
						  <faction logo=""500014"" name=""ORE"" damagetype=""Kinetic"" />
						  <faction logo=""500016"" name=""Sisters of Eve"" damagetype=""Kinetic"" />
						  <faction logo=""500017"" name=""Society of Conscious Thought"" damagetype=""Kinetic"" />
						</factions>";

        #endregion Properties
    }
}