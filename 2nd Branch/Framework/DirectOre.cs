
using EVESharpCore.Cache;
using EVESharpCore.Controllers;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Events;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Actions;
using EVESharpCore.Questor.Activities;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Questor.States;
using EVESharpCore.Questor.Stats;
using System.Collections.Generic;

namespace EVESharpCore.Framework
{
    public static class OreTypes
    {
        public static List<Ore> VeldsparTypeRoids = new List<Ore> { Veldspar, DenseVeldspar, ConcentratedVeldspar, StableVeldspar };
        public static List<Ore> ScorditeTypeRoids = new List<Ore> { Scordite, MassiveScordite, CondensedScordite, GlossyScordite };
        public static List<Ore> PyroxeresTypeRoids = new List<Ore> { Pyroxeres, SolidPyroxeres, VisciousPyroxeres, OpulentPyroxeres };
        public static List<Ore> PlagioclaseTypeRoids = new List<Ore> { Plagioclase, AzurePlagioclase, RichPlagioclase, SparklingPlagioclase };
        public static List<Ore> OmberTypeRoids = new List<Ore> { Omber, GoldenOmber, SilveryOmber, PlatinoidOmber };
        public static List<Ore> KerniteTypeRoids = new List<Ore> { Kernite, FieryKernite, LuminousKernite, ResplendantKernite };
        public static List<Ore> JaspetTypeRoids = new List<Ore> { Jaspet, FieryJaspet, LuminousJaspet, ResplendantJaspet };
        public static List<Ore> HemorphiteTypeRoids = new List<Ore> { Hemorphite, RadiantHemorphite, ScintillatingHemorphite, VividHemorphite };
        public static List<Ore> HedbergiteTypeRoids = new List<Ore> { Hedbergite, GlazedHedbergite, LustrousHedbergite, VitricHedbergite };
        public static List<Ore> GneissTypeRoids = new List<Ore> { Gneiss, BrilliantGneiss, IridescentGneiss, PrismaticGneiss };
        public static List<Ore> DarkOchreTypeRoids = new List<Ore> { DarkOchre, JetOchre, ObsidianOchre, OnyxOchre };
        public static List<Ore> CrokiteTypeRoids = new List<Ore> { Crokite, CrystallineCrokite, PellucidCrokite, SharpCrokite };
        public static List<Ore> SpodumainTypeRoids = new List<Ore> { Spodumain, BrightSpodumain, DazzlingSpodumain, GleamingSpodumain };
        public static List<Ore> BistotTypeRoids = new List<Ore> { Bistot, CubicBistot, MonoclinicBistot, TriclinicBistot };
        public static List<Ore> ArkonorTypeRoids = new List<Ore> { Arkonor, CrimsonArkonor, FlawlessArkonor, PrimeArkonor };
        public static List<Ore> MercoxitTypeRoids = new List<Ore> { Mercoxit, MagmaMercoxit, VitreousMercoxit };


        public static Ore Veldspar = new Ore("Veldspar",                          462, 1230,  0.1, 100, 0, 400);
        public static Ore DenseVeldspar = new Ore("Dense Veldspar",               462, 17471, 0.1, 100, 10, 440);
        public static Ore ConcentratedVeldspar = new Ore("Concentrated Veldspar", 462, 17470, 0.1, 100, 5, 420);
        public static Ore StableVeldspar = new Ore("Stable Veldspar",             462, 46688, 0.1, 100, 15, 460);

        public static Ore Scordite = new Ore("Scordite",                          460, 1228,  0.15, 100, 0, 150, 90);
        public static Ore MassiveScordite = new Ore("Massive Scordite",           460, 17464, 0.15, 100, 10, 165, 99);
        public static Ore CondensedScordite = new Ore("Condensed Scordite",       460, 17463, 0.15, 100, 5, 158, 95);
        public static Ore GlossyScordite = new Ore("Glossy Scordite",             460, 46687, 0.15, 100, 15, 173, 114);

        public static Ore Pyroxeres = new Ore("Pyroxeres",                        459, 1224,  0.3, 100, 0, 90, 30);
        public static Ore SolidPyroxeres = new Ore("Solid Pyroxeres",             459, 17459, 0.3, 100, 0, 95, 30);
        public static Ore VisciousPyroxeres = new Ore("Viscious Pyroxeres",       459, 17460, 0.3, 100, 0, 99, 33);
        public static Ore OpulentPyroxeres = new Ore("Opulent Pyroxeres",         459, 46686, 0.3, 100, 0, 104, 33);

        public static Ore Plagioclase = new Ore("Plagioclase",                    458, 18,    0.35, 100, 0, 175, 0, 70);
        public static Ore AzurePlagioclase = new Ore("Azure Plagioclase",         458, 17455, 0.35, 100, 5, 184, 0, 74);
        public static Ore RichPlagioclase = new Ore("Rich Plagioclase",           458, 17456, 0.35, 100, 10, 193, 0, 77);
        public static Ore SparklingPlagioclase = new Ore("Sparkling Plagioclase", 458, 46685, 0.35, 100, 15, 202, 81);

        public static Ore Omber = new Ore("Omber",                                469, 1227,  0.6, 100, 0, 90, 0, 75);
        public static Ore SilveryOmber = new Ore("Silvery Omber",                 469, 17867, 0.6, 100, 0, 90, 0, 75);
        public static Ore GoldenOmber = new Ore("Golden Omber",                   469, 17868, 0.6, 100, 0, 90, 0, 75);
        public static Ore PlatinoidOmber = new Ore("Platinoid Omber",             469, 46684, 0.6, 100, 0, 90, 0, 75);

        public static Ore Kernite = new Ore("Kernite",                            457, 20,  1.2, 100, 0, 0, 60, 120);
        public static Ore LuminousKernite = new Ore("Luminous Kernite",           457, 17452, 1.2, 100, 0, 0, 60, 120);
        public static Ore FieryKernite = new Ore("Fiery Kernite",                 457, 17868, 1.2, 100, 0, 0, 60, 120);
        public static Ore ResplendantKernite = new Ore("Resplendant Kernite",     457, 46683, 1.2, 100, 0, 0, 60, 120);

        public static Ore Jaspet = new Ore("Jaspet",                              457, 20, 2, 100, 0, 0, 150, 0, 50);
        public static Ore LuminousJaspet = new Ore("Luminous Jaspet",             457, 17452, 2, 100, 0, 0, 150, 0, 50);
        public static Ore FieryJaspet = new Ore("Fiery Jaspet",                   457, 17868, 2, 100, 0, 0, 150, 0, 50);
        public static Ore ResplendantJaspet = new Ore("Resplendant Jaspet",       457, 46683, 2, 100, 0, 0, 150, 0, 50);

        public static Ore Hemorphite = new Ore("Hemorphite",                      455, 1231, 3, 100, 0, 0, 0, 240, 90);
        public static Ore VividHemorphite = new Ore("Vivid Hemorphite",           455, 17444, 3, 100, 0, 0, 0, 240, 90);
        public static Ore RadiantHemorphite = new Ore("Radiant Hemorphite",       455, 17445, 3, 100, 0, 0, 0, 240, 90);
        public static Ore ScintillatingHemorphite = new Ore("Scintillating Hemorphite", 455, 46681, 3, 100, 0, 0, 0, 240, 90);

        public static Ore Hedbergite = new Ore("Hedbergite",                      454, 21,    3, 100, 0, 450, 0, 0, 120);
        public static Ore VitricHedbergite = new Ore("Vitric Hedbergite",         454, 17440, 3, 100, 0, 450, 0, 0, 120);
        public static Ore GlazedHedbergite = new Ore("Glazed Hedbergite",         454, 17441, 3, 100, 0, 450, 0, 0, 120);
        public static Ore LustrousHedbergite = new Ore("Lustrous Hedbergite",     454, 46680, 3, 100, 0, 450, 0, 0, 120);

        public static Ore Gneiss = new Ore("Gneiss",                              467, 1229,  5, 100, 0, 2000, 1500, 800);
        public static Ore IridescentGneiss = new Ore("Iridescent Gneiss",         467, 17865, 5, 100, 0, 2000, 1500, 800);
        public static Ore PrismaticGneiss = new Ore("Prismatic Gneiss",           467, 17866, 5, 100, 0, 2000, 1500, 800);
        public static Ore BrilliantGneiss = new Ore("Brilliant Gneiss",           467, 46679, 5, 100, 0, 2000, 1500, 800);

        public static Ore DarkOchre = new Ore("Dark Ochre",                       453, 1232, 8, 100, 0, 0, 1360, 1200, 320);
        public static Ore OnyxOchre = new Ore("Onyx Ochre",                       453, 17436, 8, 100, 0, 0, 1500, 1200, 320);
        public static Ore ObsidianOchre = new Ore("Obsidian Ochre",               453, 17437, 8, 100, 0, 0, 1500, 1200, 320);
        public static Ore JetOchre = new Ore("Jet Ochre",                         453, 46675, 8, 100, 0, 0, 1500, 1200, 320);

        public static Ore Crokite = new Ore("Crokite",                            452, 1225,  16, 100, 0, 800, 2000, 0, 800);
        public static Ore SharpCrokite = new Ore("Sharp Crokite",                 452, 17432, 16, 100, 0, 800, 2000, 0, 800);
        public static Ore CrystallineCrokite = new Ore("Crystalline Crokite",     452, 17433, 16, 100, 0, 800, 2000, 0, 800);
        public static Ore PellucidCrokite = new Ore("Pellucid Crokite",           452, 46677, 16, 100, 0, 800, 2000, 0, 800);

        public static Ore Spodumain = new Ore("Spodumain",                        461, 19,    16, 100, 48000, 0, 0, 1000, 160, 80, 40);
        public static Ore BrightSpodumain = new Ore("Bright Spodumain",           461, 17466, 16, 100, 48000, 0, 0, 1000, 160, 80, 40);
        public static Ore GleamingSpodumain = new Ore("Gleaming Spodumain",       461, 17467, 16, 100, 48000, 0, 0, 1000, 160, 80, 40);
        public static Ore DazzlingSpodumain = new Ore("Dazzling Spodumain",       461, 46688, 16, 100, 48000, 0, 0, 1000, 160, 80, 40);

        public static Ore Bistot = new Ore("Bistot",                              451, 1223,  16, 100, 0, 3200, 1200, 0, 0, 160);
        public static Ore TriclinicBistot = new Ore("Triclinic Bistot",           451, 17428, 16, 100, 0, 3200, 1200, 0, 0, 160);
        public static Ore MonoclinicBistot = new Ore("Monoclinic Bistot",         451, 17429, 16, 100, 0, 3200, 1200, 0, 0, 160);
        public static Ore CubicBistot = new Ore("Cubic Bistot",                   451, 46676, 16, 100, 0, 3200, 1200, 0, 0, 160);

        public static Ore Arkonor = new Ore("Arkonor",                            450, 22,    16, 100, 0, 3200, 1200, 0, 0, 0, 120);
        public static Ore CrimsonArkonor = new Ore("Crimson Arkonor",             450, 17425, 16, 100, 0, 3200, 1200, 0, 0, 0, 120);
        public static Ore PrimeArkonor = new Ore("Prime Arkonor",                 450, 17426, 16, 100, 0, 3200, 1200, 0, 0, 0, 120);
        public static Ore FlawlessArkonor = new Ore("Flawless Arkonor",           450, 46678, 16, 100, 0, 3200, 1200, 0, 0, 0, 120);

        public static Ore Mercoxit = new Ore("Mercoxit",                          468, 11395, 40, 100, 0, 0, 0, 0, 0, 0, 0, 140);
        public static Ore MagmaMercoxit = new Ore("Magma Mercoxit",               468, 17869, 40, 100, 0, 0, 0, 0, 0, 0, 0, 140);
        public static Ore VitreousMercoxit = new Ore("Vitreous Mercoxit",         468, 17870, 40, 100, 0, 0, 0, 0, 0, 0, 0, 140);
    }

    public static class Minerals
    {
        public static Mineral Tritanium = new Mineral("Tritanium", 34);
        public static Mineral Pyerite = new Mineral("Pyerite", 35);
        public static Mineral Mexallon = new Mineral("Mexallon", 36);
        public static Mineral Isogen = new Mineral("Isogen", 37);
        public static Mineral Nocxium = new Mineral("Nocxium", 38);
        public static Mineral Zydrine = new Mineral("Zydrine", 39);
        public static Mineral Megacyte = new Mineral("Megacyte", 40);
        public static Mineral Morphite = new Mineral("Morphite", 11399);
    }

    public class Mineral
    {
        public Mineral(string name, int typeId)
        {
            Name = name;
            TypeId = typeId;
            //IskPerUnitAveragePrice = iskPerUnitAveragePrice;
            //IskPerUnitBuy = iskPerUnitBuy;
            //IskPerUnitSell = iskPerUnitSell;
        }


        public string Name;
        public int TypeId;

        //private DirectItem _directItem = null;
        public DirectItem directItem
        {
            get
            {
                //if (_directItem != null)
                //    return _directItem;

                DirectItem _directItem = new DirectItem(ESCache.Instance.DirectEve)
                {
                    TypeId = TypeId
                };

                return _directItem ?? null;
            }
        }

        public double IskPerUnitAveragePrice
        {
            get
            {
                DirectItem tempDirectItem = new DirectItem(ESCache.Instance.DirectEve)
                {
                    TypeId = TypeId
                };

                return tempDirectItem.AveragePrice();
            }
        }
    }

    public class Ore
    {
        //https://wiki.eveuniversity.org/Asteroids_and_ore

        public Ore(string name,
                        int groupId,
                        int typeId,
                        double m3,
                        int unitsInEachBatch,
                        int bonusYield = 0,
                        int tritanium = 0,
                        int pyerite = 0,
                        int mexallon = 0,
                        int isogen = 0,
                        int nocxium = 0,
                        int zydrine = 0,
                        int megacyte = 0,
                        int morphite = 0
                        )
        {
            Name = name;
            GroupId = groupId;
            TypeId = typeId;
            M3 = m3;
            UnitsInEachBatch = unitsInEachBatch;
            Tritanium = tritanium;
            Pyerite = pyerite;
            Mexallon = mexallon;
            Isogen = isogen;
            Nocxium = nocxium;
            Zydrine = zydrine;
            Megacyte = megacyte;
            Morphite = morphite;
            BonusYield = bonusYield;
        }

        public string Name;
        public int GroupId;
        public int TypeId;
        public double M3;
        public int BonusYield;
        public int UnitsInEachBatch;
        public double IskPerM3
        {
            get
            {
                return IskEachUnit / M3;
            }
        }

        public double IskEachBatch
        {
            get
            {
                return (TritaniumValuePerUnit +
                PyeriteValuePerUnit +
                MexallonValuePerUnit +
                IsogenValuePerUnit +
                NocxiumValuePerUnit +
                ZydrineValuePerUnit +
                MegacyteValuePerUnit +
                MorphiteValuePerUnit);
            }
        }

        public double IskEachUnit
        {
            get
            {
                return IskEachBatch / UnitsInEachBatch;
            }
        }

        public double OreSaleEach;
        public double OreSaleM3;
        public int Tritanium;
        public int Pyerite;
        public int Mexallon;
        public int Isogen;
        public int Nocxium;
        public int Zydrine;
        public int Megacyte;
        public int Morphite;

        public double TritaniumValuePerUnit
        {
            get
            {
                if (Tritanium == 0)
                    return 0;

                if (Minerals.Tritanium.IskPerUnitAveragePrice == 0)
                    return 0;

                return Tritanium * Minerals.Tritanium.IskPerUnitAveragePrice;
            }
        }

        public double PyeriteValuePerUnit
        {
            get
            {
                if (Pyerite == 0)
                    return 0;

                if (Minerals.Pyerite.IskPerUnitAveragePrice == 0)
                    return 0;

                return Pyerite * Minerals.Pyerite.IskPerUnitAveragePrice;
            }
        }

        public double MexallonValuePerUnit
        {
            get
            {
                if (Mexallon == 0)
                    return 0;

                if (Minerals.Mexallon.IskPerUnitAveragePrice == 0)
                    return 0;

                return Mexallon * Minerals.Mexallon.IskPerUnitAveragePrice;
            }
        }

        public double IsogenValuePerUnit
        {
            get
            {
                if (Isogen == 0)
                    return 0;

                if (Minerals.Isogen.IskPerUnitAveragePrice == 0)
                    return 0;

                return Isogen * Minerals.Isogen.IskPerUnitAveragePrice;
            }
        }

        public double NocxiumValuePerUnit
        {
            get
            {
                if (Nocxium == 0)
                    return 0;

                if (Minerals.Nocxium.IskPerUnitAveragePrice == 0)
                    return 0;

                return Nocxium * Minerals.Nocxium.IskPerUnitAveragePrice;
            }
        }

        public double ZydrineValuePerUnit
        {
            get
            {
                if (Zydrine == 0)
                    return 0;

                if (Minerals.Zydrine.IskPerUnitAveragePrice == 0)
                    return 0;

                return Zydrine * Minerals.Zydrine.IskPerUnitAveragePrice;
            }
        }

        public double MegacyteValuePerUnit
        {
            get
            {
                if (Megacyte == 0)
                    return 0;

                if (Minerals.Megacyte.IskPerUnitAveragePrice == 0)
                    return 0;

                return Megacyte * Minerals.Megacyte.IskPerUnitAveragePrice;
            }
        }

        public double MorphiteValuePerUnit
        {
            get
            {
                if (Morphite == 0)
                    return 0;

                if (Minerals.Morphite.IskPerUnitAveragePrice == 0)
                    return 0;

                return Morphite * Minerals.Morphite.IskPerUnitAveragePrice;
            }
        }
    }
}