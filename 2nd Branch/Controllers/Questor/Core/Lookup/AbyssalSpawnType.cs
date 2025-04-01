extern alias SC;

using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Security;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Abyssal;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Events;
using EVESharpCore.Logging;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Questor.Stats;
using Questor.Modules.Actions;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Events;

namespace EVESharpCore.Lookup
{
    public static class AbyssalSpawn
    {
        //https://wiki.eveuniversity.org/Possible_rooms_in_Abyssal_Deadspace
        //
        // keep in mind that for lower tiers the detection will be different!
        //
        public static int AbyssalTier
        {
            get
            {
                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController))
                {
                    if (AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Tranquil"))
                        return 0;

                    if (AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Calm"))
                        return 1;

                    if (AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Agitated"))
                        return 2;

                    if (AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Fierce"))
                        return 3;

                    if (AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Raging"))
                        return 4;

                    if (AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Chaotic"))
                        return 5;

                    if (AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Cataclysmic"))
                        return 6;

                    return 4;
                }

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalController))
                {
                    //DarkT1, // 47762
                    //DarkT2, // 47892
                    //DarkT3, // 47893
                    //DarkT4, // 47894
                    //DarkT5, // 47895
                    //DarkT6, // 56140
                    //GammaT1, // 47764
                    //GammaT2, // 47900
                    //GammaT3, // 47901
                    //GammaT4, // 47902
                    //GammaT5, // 47903
                    //GammaT6, // 56143
                    //FirestormT1, // 47763
                    //FirestormT2, // 47896
                    //FirestormT3, // 47897
                    //FirestormT4, // 47898
                    //FirestormT5, // 47899
                    //FirestormT6, // 56142
                    //ExoticT1, // 47761
                    //ExoticT2, // 47888
                    //ExoticT3, // 47889
                    //ExoticT4, // 47890
                    //ExoticT5, // 47891
                    //ExoticT6, // 56141
                    //ElectricalT1, // 47765
                    //ElectricalT2, // 47904
                    //ElectricalT3, // 47905
                    //ElectricalT4, // 47906
                    //ElectricalT5, // 47907
                    //ElectricalT6, // 56139
                    //if (AbyssalController._filamentTypeId == 47762)
                    //    return 0;
                    //
                    //if (AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Calm"))
                    //    return 1;
                    //
                    //if (AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Agitated"))
                    //    return 2;
                    //
                    //if (AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Fierce"))
                    //    return 3;
                    //
                    //if (AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Raging"))
                    //    return 4;
                    //
                    //if (AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Chaotic"))
                    //    return 5;
                    //
                    //if (AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Cataclysmic"))
                    //    return 6;

                    return 6;
                }

                return 0;
            }
        }

        //https://everef.net/groups/1975
        public enum AbyssalDungeonType
        {
            AsteroidDungeonCloud,
            CrystalDungeonCloud,
            PillarDungeonCloud,
            Other,
            Undecided,
        }

        public enum AbyssalFactionType
        {
            Faction_RogueDrone,
            Faction_Drifters,
            Faction_Sleepers,
            Faction_Triglavians,
            Faction_SanshasNation,
            Faction_Concord,
            Faction_AngelCartel,
        }

        public enum AbyssalSpawnType
        {
            NotInAbyssalDeadspace,
            Undecided,
            UnknownSpawn,
            AbyssalOvermindDroneBSSpawn,
            KarybdisTyrannosSpawn,
            VedmakCruiserSpawn,
            VedmakVilaCruiserSwarmerSpawn,
            DamavikVilaFrigateSwarmerSpawn,
            LucidDeepwatcherBSSpawn,
            LeshakBSSpawn,
            LuciferSpawn,
            LucidCruiserSpawn,
            CruiserSpawn,
            DevotedCruiserSpawn,
            EphialtesCruiserSpawn,
            DroneFrigateSpawn,
            LucidFrigateSpawn,
            DamavikFrigateSpawn,
            DevotedHunter,
            KikimoraDestroyerSpawn,
            HighAngleDroneBattleCruiserSpawn,
            DrekavacBattleCruiserSpawn,
            ConcordSpawn,
            RodivaSpawn,
            FourteenBattleshipSpawn,
        }

        //Concord spawn
        //Arrestor Marshal Disparu Troop
        //Drainer Marshal Disparu Troop
        //Attacker Marshal Disparu Troop

        public static AbyssalSpawnType _detectSpawn { get; set; } = AbyssalSpawnType.Undecided;
        public static AbyssalDungeonType _detectDungeon { get; set; } = AbyssalDungeonType.Undecided;

        public static DateTime LastDetectSpawn = DateTime.UtcNow.AddHours(-1);
        private static bool EntitesLoggedOnce = false;

        public static string AbyssalWeather
        {
            get
            {
                try
                {
                    if (!ESCache.Instance.InAbyssalDeadspace)
                        return string.Empty;

                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Dark_Weather_1_30Percent))
                        return nameof(TypeID.Dark_Weather_1_30Percent);

                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Dark_Weather_2_50Percent))
                        return nameof(TypeID.Dark_Weather_2_50Percent);

                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Dark_Weather_3_70Percent))
                        return nameof(TypeID.Dark_Weather_3_70Percent);

                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Electric_Weather_1_30Percent))
                        return nameof(TypeID.Electric_Weather_1_30Percent);

                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Electric_Weather_2_50Percent))
                        return nameof(TypeID.Electric_Weather_2_50Percent);

                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Electric_Weather_3_70Percent))
                        return nameof(TypeID.Electric_Weather_3_70Percent);

                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Caustic_Weather_1_30Percent))
                        return nameof(TypeID.Caustic_Weather_1_30Percent);

                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Caustic_Weather_2_50Percent))
                        return nameof(TypeID.Caustic_Weather_2_50Percent);

                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Caustic_Weather_3_70Percent))
                        return nameof(TypeID.Caustic_Weather_3_70Percent);

                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Xenon_Weather_1_30Percent))
                        return nameof(TypeID.Xenon_Weather_1_30Percent);

                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Xenon_Weather_2_50Percent))
                        return nameof(TypeID.Xenon_Weather_2_50Percent);

                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Xenon_Weather_3_70Percent))
                        return nameof(TypeID.Xenon_Weather_3_70Percent);

                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Infernal_Weather_1_30Percent))
                        return nameof(TypeID.Infernal_Weather_1_30Percent);

                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Infernal_Weather_2_50Percent))
                        return nameof(TypeID.Infernal_Weather_2_50Percent);

                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Infernal_Weather_3_70Percent))
                        return nameof(TypeID.Infernal_Weather_3_70Percent);

                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Basic_Weather))
                        return nameof(TypeID.Basic_Weather);

                    return "None";
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return "n/a";
                }
            }
        }

        public static void ClearPerPocketCache()
        {
            _detectSpawn = AbyssalSpawnType.Undecided;
            _detectDungeon = AbyssalDungeonType.Undecided;
            EntitesLoggedOnce = false;
            return;
        }

        private static bool UseCachedDetectSpawnValue
        {
            get
            {
                if (_detectSpawn != AbyssalSpawnType.Undecided)
                    return true;

                return false;
            }
        }

        private static bool UseCachedDetectDungeonValue
        {
            get
            {
                if (_detectDungeon != AbyssalDungeonType.Undecided)
                    return true;

                return false;
            }
        }

        public static AbyssalDungeonType DetectDungeon
        {
            get
            {
                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    if (UseCachedDetectDungeonValue)
                    {
                        return _detectDungeon;
                    }

                    if (!DirectEve.HasFrameChanged())
                        return AbyssalDungeonType.Undecided;

                    if (!Combat.PotentialCombatTargets.Any())
                        return AbyssalDungeonType.Undecided;

                    if (ESCache.Instance.Entities.Any(i => i.GroupId == (int)Group.AbyssalDeadspaceNonScalableCloud))
                    {
                        if (ESCache.Instance.Entities.Any(i => i.GroupId == (int)Group.AbyssalDeadspaceNonScalableCloud && i.TypeName.Contains("Crystal")))
                        {
                            _detectDungeon = AbyssalDungeonType.CrystalDungeonCloud;
                            return _detectDungeon;
                        }

                        if (ESCache.Instance.Entities.Any(i => i.GroupId == (int)Group.AbyssalDeadspaceNonScalableCloud && i.TypeName.Contains("Pillar")))
                        {
                            _detectDungeon = AbyssalDungeonType.PillarDungeonCloud;
                            return _detectDungeon;
                        }

                        if (ESCache.Instance.Entities.Any(i => i.GroupId == (int)Group.AbyssalDeadspaceNonScalableCloud && i.TypeName.Contains("Asteroid")))
                        {
                            _detectDungeon = AbyssalDungeonType.AsteroidDungeonCloud;
                            return _detectDungeon;
                        }

                        //
                        // Add other abyssal dungeon detections here
                        //

                        _detectDungeon = AbyssalDungeonType.Other;
                        return _detectDungeon;
                    }

                    Log.WriteLine("DetectDungeon: Undecided!?");
                    return AbyssalDungeonType.Undecided;
                }

                //LogSpawnEntities();
                //Log.WriteLine("DetectSpawn: IsAbyssalDeadspace [" + ESCache.Instance.InAbyssalDeadspace + "] Undecided " + NumberOfEachNPCType);
                return AbyssalDungeonType.Undecided;
            }
        }

        public static AbyssalSpawnType DetectSpawn
        {
            get
            {
                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    if (UseCachedDetectSpawnValue)
                    {
                        return _detectSpawn;
                    }

                    if (!DirectEve.HasFrameChanged())
                        return AbyssalSpawnType.Undecided;

                    if (!Combat.PotentialCombatTargets.Any())
                    {
                        //Log.WriteLine("DetectSpawn: Undecided");
                        return AbyssalSpawnType.Undecided;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) > 10)
                    {
                        Log.WriteLine("DetectSpawn: 14BattleshipSpawn.");
                        LogSpawnEntities();
                        _detectSpawn = AbyssalSpawnType.FourteenBattleshipSpawn;
                        return _detectSpawn;
                    }

                    if (AbyssalTier == 0 && Combat.PotentialCombatTargets.Any(i => i.Name.ToLower().Contains("Devoted Hunter".ToLower())))
                    {
                        Log.WriteLine("DetectSpawn: DevotedHunter.");
                        LogSpawnEntities();
                        _detectSpawn = AbyssalSpawnType.DevotedHunter;
                        return _detectSpawn;
                    }

                    if (AbyssalTier == 0 && Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
                    {
                        Log.WriteLine("DetectSpawn: EphialtesCruiserSpawn.");
                        LogSpawnEntities();
                        _detectSpawn = AbyssalSpawnType.EphialtesCruiserSpawn;
                        return _detectSpawn;
                    }

                    if (AbyssalTier == 4 && Combat.PotentialCombatTargets.Count <= 5)
                    {
                        Log.WriteLine("DetectSpawn: Undecided.");
                        return AbyssalSpawnType.Undecided;
                    }

                    if (AbyssalTier == 5 && Combat.PotentialCombatTargets.Count <= 5)
                    {
                        Log.WriteLine("DetectSpawn: Undecided!");
                        return AbyssalSpawnType.Undecided;
                    }

                    //if (AbyssalTier == 6 && Combat.PotentialCombatTargets.Count <= 5)
                    //{
                    //    Log.WriteLine("DetectSpawn: Undecided!!");
                    //    return AbyssalSpawnType.Undecided;
                    //}

                    if (IsConcordSpawn)
                    {
                        Log.WriteLine("DetectSpawn: ConcordSpawn " + NumberOfEachNPCType);
                        LogSpawnEntities();
                        _detectSpawn = AbyssalSpawnType.ConcordSpawn;
                        return _detectSpawn;
                    }

                    if (IsKikimoraDestroyerSpawn)
                    {
                        Log.WriteLine("DetectSpawn: KikimoraDestroyerSpawn " + NumberOfEachNPCType);
                        LogSpawnEntities();
                        _detectSpawn = AbyssalSpawnType.KikimoraDestroyerSpawn;
                        return _detectSpawn;
                    }

                    if (IsLucidDeepwatcherBSSpawn)
                    {
                        Log.WriteLine("DetectSpawn: LucidDeepwatcherBSSpawn " + NumberOfEachNPCType);
                        LogSpawnEntities();
                        _detectSpawn = AbyssalSpawnType.LucidDeepwatcherBSSpawn;
                        return _detectSpawn;
                    }

                    if (IsLucidCruiserSpawn)
                    {
                        Log.WriteLine("DetectSpawn: LucidCruiserSpawn " + NumberOfEachNPCType);
                        LogSpawnEntities();
                        _detectSpawn = AbyssalSpawnType.LucidCruiserSpawn;
                        return _detectSpawn;
                    }

                    if (IsKarybdisTyrannosSpawn)
                    {
                        Log.WriteLine("DetectSpawn: KarybdisTyrannosSpawn " + NumberOfEachNPCType);
                        LogSpawnEntities();
                        _detectSpawn = AbyssalSpawnType.KarybdisTyrannosSpawn;
                        return _detectSpawn;
                    }

                    if (IsVedmakVilaCruiserSwarmerSpawn)
                    {
                        Log.WriteLine("DetectSpawn: VedmakVilaCruiserSwarmerSpawn " + NumberOfEachNPCType);
                        LogSpawnEntities();
                        _detectSpawn = AbyssalSpawnType.VedmakVilaCruiserSwarmerSpawn;
                        return _detectSpawn;
                    }

                    if (IsVedmakCruiserSpawn)
                    {
                        Log.WriteLine("DetectSpawn: VedmakCruiserSpawn " + NumberOfEachNPCType);
                        LogSpawnEntities();
                        _detectSpawn = AbyssalSpawnType.VedmakCruiserSpawn;
                        return _detectSpawn;
                    }

                    if (IsHighAngleBattleCruiserSpawn)
                    {
                        Log.WriteLine("DetectSpawn: HighAngleBattleCruiserSpawn " + NumberOfEachNPCType);
                        LogSpawnEntities();
                        _detectSpawn = AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn;
                        return _detectSpawn;
                    }

                    if (IsDrekavacBattleCruiserSpawn)
                    {
                        Log.WriteLine("DetectSpawn: DrekavacBattleCruiserSpawn " + NumberOfEachNPCType);
                        LogSpawnEntities();
                        _detectSpawn = AbyssalSpawnType.DrekavacBattleCruiserSpawn;
                        return _detectSpawn;
                    }

                    if (IsAbyssalOvermindBSSpawn)
                    {
                        Log.WriteLine("DetectSpawn: AbyssalOvermindBSSpawn " + NumberOfEachNPCType);
                        LogSpawnEntities();
                        _detectSpawn = AbyssalSpawnType.AbyssalOvermindDroneBSSpawn;
                        return _detectSpawn;
                    }

                    if (IsLeshakBSSpawn)
                    {
                        Log.WriteLine("DetectSpawn: LeshakBSSpawn " + NumberOfEachNPCType);
                        LogSpawnEntities();
                        _detectSpawn = AbyssalSpawnType.LeshakBSSpawn;
                        return _detectSpawn;
                    }

                    if (IsLuciferSpawn)
                    {
                        Log.WriteLine("DetectSpawn: LuciferSpawn " + NumberOfEachNPCType);
                        LogSpawnEntities();
                        _detectSpawn = AbyssalSpawnType.LuciferSpawn;
                        return _detectSpawn;
                    }

                    if (IsDevotedCruiserSpawn)
                    {
                        Log.WriteLine("DetectSpawn: DevotedCruiserSpawn " + NumberOfEachNPCType);
                        LogSpawnEntities();
                        _detectSpawn = AbyssalSpawnType.DevotedCruiserSpawn;
                        return _detectSpawn;
                    }

                    if (IsEphialtesCruiserSpawn)
                    {
                        Log.WriteLine("DetectSpawn: EphialtesCruiserSpawn " + NumberOfEachNPCType);
                        LogSpawnEntities();
                        _detectSpawn = AbyssalSpawnType.EphialtesCruiserSpawn;
                        return _detectSpawn;
                    }

                    if (IsLucidFrigateSpawn)
                    {
                        Log.WriteLine("DetectSpawn: LucidFrigateSpawn " + NumberOfEachNPCType);
                        LogSpawnEntities();
                        _detectSpawn = AbyssalSpawnType.LucidFrigateSpawn;
                        return _detectSpawn;
                    }

                    if (IsRodivaSpawn)
                    {
                        Log.WriteLine("DetectSpawn: RodivaSpawn " + NumberOfEachNPCType);
                        LogSpawnEntities();
                        _detectSpawn = AbyssalSpawnType.RodivaSpawn;
                        return _detectSpawn;
                    }

                    if (IsDamavikFrigateSpawn)
                    {
                        Log.WriteLine("DetectSpawn: DamavikFrigateSpawn " + NumberOfEachNPCType);
                        LogSpawnEntities();
                        _detectSpawn = AbyssalSpawnType.DamavikFrigateSpawn;
                        return _detectSpawn;
                    }

                    if (IsDamavikVilaFrigateSwarmerSpawn)
                    {
                        Log.WriteLine("DetectSpawn: DamavikVilaFrigateSwarmerSpawn " + NumberOfEachNPCType);
                        LogSpawnEntities();
                        _detectSpawn = AbyssalSpawnType.DamavikVilaFrigateSwarmerSpawn;
                        return _detectSpawn;
                    }

                    if (IsDroneFrigateSpawn)
                    {
                        Log.WriteLine("DetectSpawn: DroneFrigateSpawn " + NumberOfEachNPCType);
                        LogSpawnEntities();
                        _detectSpawn = AbyssalSpawnType.DroneFrigateSpawn;
                        return _detectSpawn;
                    }

                    //
                    // Add other abyssal spawn detections here
                    //

                    if (Time.ItHasBeenThisLongSinceWeStartedThePocket.TotalSeconds > 10)
                    {
                        LogSpawnEntities();
                        _detectSpawn = AbyssalSpawnType.UnknownSpawn;
                        Log.WriteLine("DetectSpawn: UnknownSpawn: " + NumberOfEachNPCType);
                        return _detectSpawn;
                    }

                    Log.WriteLine("DetectSpawn: Undecided!?");
                    return AbyssalSpawnType.Undecided;
                }

                //LogSpawnEntities();
                //Log.WriteLine("DetectSpawn: IsAbyssalDeadspace [" + ESCache.Instance.InAbyssalDeadspace + "] Undecided " + NumberOfEachNPCType);
                return AbyssalSpawnType.Undecided;
            }
        }

        private static bool LogSpawnEntities()
        {
            if (!EntitesLoggedOnce)
            {
                if (!Combat.PotentialCombatTargets.Any())
                    return true;

                Statistics.PocketObjectStatistics(ESCache.Instance.Entities);

                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.KEEP_ALIVE, "DetectSpawn: [" + AbyssalSpawn.DetectSpawn + "] " + NumberOfEachNPCType));

                int count = 0;
                Log.WriteLine("-------");
                foreach (var potentialcombattarget in Combat.PotentialCombatTargets) //.OrderBy(i => i._directEntity.AbyssalTargetPriority))
                {
                    count++;
                    //Priority [" + potentialcombattarget._directEntity.AbyssalTargetPriority + "]
                    Log.WriteLine("Found [" + potentialcombattarget.Name + "] Distance [" + potentialcombattarget.Nearest1KDistance + "] IsNPCFrigate [" + potentialcombattarget.IsNPCFrigate + "] IsNPCDestroyer [" + potentialcombattarget.IsNPCDestroyer + "] IsNPCCruiser [" + potentialcombattarget.IsNPCCruiser + "] IsNPCBattlecruiser [" + potentialcombattarget.IsNPCBattlecruiser + "] IsNPCBattleship [" + potentialcombattarget.IsNPCBattleship + "] EWAR [" + potentialcombattarget.stringEwarTypes + "]");
                }

                Log.WriteLine("-------");
                EntitesLoggedOnce = true;
                return true;
            }

            return true;
        }

        private static string NumberOfEachNPCType
        {
            get
            {
                int npcBattleships = Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship);
                int npcBattlecruisers = Combat.PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser);
                int npcCruisers = Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser);
                int npcFrigates = Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate);
                int npcDestroyers = Combat.PotentialCombatTargets.Count(i => i.IsNPCDestroyer);

                return "[" + npcBattleships + "] BS [" + npcBattlecruisers + "] BCs [" + npcCruisers + "] Cruisers [" + npcDestroyers + "] Destroyers [" + npcFrigates + "] Frigates";
            }
        }


        private static bool IsKikimoraDestroyerSpawn
        {
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (Combat.PotentialCombatTargets.Any())
                {
                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.IsNPCBattleship))
                        return false;

                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.IsNPCBattlecruiser))
                        return false;

                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.IsNPCDestroyer && i.Name.ToLower().Contains("Kikimora".ToLower())))
                    {
                        //LogSpawnEntities();
                        return true;
                    }
                }

                return false;
            }
        }
        private static bool IsKarybdisTyrannosSpawn
        {
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (Combat.PotentialCombatTargets.Any())
                {
                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.Name.Contains("Karybdis Tyrannos")))
                    {
                        //LogSpawnEntities();
                        return true;
                    }
                }

                return false;
            }
        }

        private static bool IsVedmakVilaCruiserSwarmerSpawn
        {
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (Combat.PotentialCombatTargets.Any())
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.Name.Contains("Vila Vedmak")))
                    {
                        //LogSpawnEntities();
                        return true;
                    }

                    return false;
                }

                return false;
            }
        }

        private static bool IsDamavikVilaFrigateSwarmerSpawn
        {
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (Combat.PotentialCombatTargets.Any())
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
                    {
                        //LogSpawnEntities();
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.Name.Contains("Vila Damavik")))
                    {
                        //LogSpawnEntities();
                        return true;
                    }

                    return false;
                }

                return false;
            }
        }

        private static bool IsVedmakCruiserSpawn
        {
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (Combat.PotentialCombatTargets.Any())
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.Name.Contains("Vedmak") && !i.Name.Contains("Vila Vedmak")))
                    {
                        //LogSpawnEntities();
                        return true;
                    }

                    return false;
                }

                return false;
            }
        }

        private static bool IsLucidCruiserSpawn
        {
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (Combat.PotentialCombatTargets.Any())
                {
                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController))
                    {
                        if (AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Tranquil"))
                        {
                            //Tier 0 - this spawn does not exist at this tier
                            return false;
                        }

                        if (AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Calm"))
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.Name.Contains("Lucid Watchman")))
                            {
                                //LogSpawnEntities();
                                return true;
                            }

                            //Tier 1 - this spawn does not exist at this tier
                            return false;
                        }

                        if (AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Agitated"))
                        {
                            //Tier 2
                            if (Combat.PotentialCombatTargets.Any(i => i.Name.Contains("Lucid Watchman")))
                            {
                                //LogSpawnEntities();
                                return true;
                            }

                            return false;
                        }

                        if (AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Fierce"))
                        {
                            //Tier 3
                            if (Combat.PotentialCombatTargets.Any(i => i.Name.Contains("Lucid Watchman")))
                            {
                                //LogSpawnEntities();
                                return true;
                            }


                            if (Combat.PotentialCombatTargets.Any(i => i.Name.Contains("Lucid Sentinel")))
                            {
                                //LogSpawnEntities();
                                return true;
                            }

                            return false;
                        }

                        if (AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Raging") ||
                            AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Chaotic"))
                        {
                            //Tier 4 or Tier 5
                            if (Combat.PotentialCombatTargets.Any(i => i.Name.Contains("Lucid Watchman")))
                            {
                                //LogSpawnEntities();
                                return true;
                            }

                            if (Combat.PotentialCombatTargets.Any(i => i.Name.Contains("Lucid Sentinel")))
                            {
                                //LogSpawnEntities();
                                return true;
                            }

                            return false;
                        }
                    }

                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalController))
                    {
                        if (!Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
                            return false;

                        //Tier 4 or Tier 5 or 6?
                        if (Combat.PotentialCombatTargets.Any(i => i.Name.Contains("Lucid Watchman")))
                        {
                            //LogSpawnEntities();
                            return true;
                        }

                        if (Combat.PotentialCombatTargets.Any(i => i.Name.Contains("Lucid Upholder")))
                        {
                            //LogSpawnEntities();
                            return true;
                        }

                        if (Combat.PotentialCombatTargets.Any(i => i.Name.Contains("Lucid Sentinel")))
                        {
                            //LogSpawnEntities();
                            return true;
                        }

                        if (Combat.PotentialCombatTargets.Any(i => i.Name.Contains("Lucid Aegis")))
                        {
                            //LogSpawnEntities();
                            return true;
                        }

                        if (Combat.PotentialCombatTargets.Any(i => i.Name.Contains("Lucid ")))
                        {
                            //LogSpawnEntities();
                            return true;
                        }

                        return false;
                    }

                    return false;
                }

                return false;
            }
        }

        private static bool IsDevotedCruiserSpawn
        {
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (Combat.PotentialCombatTargets.Any())
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.Name.ToLower().Contains("Devoted".ToLower())))
                    {
                        //LogSpawnEntities();
                        return true;
                    }

                    return false;
                }

                return false;
            }
        }

        private static bool IsEphialtesCruiserSpawn
        {
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (Combat.PotentialCombatTargets.Any())
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                        return false;

                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser))
                        return false;

                    if (Combat.PotentialCombatTargets.All(i => i.IsNPCFrigate))
                        return false;

                    if (Combat.PotentialCombatTargets.Any(i => i.Name.ToLower().Contains("Vedmak".ToLower())))
                        return false;

                    if (Combat.PotentialCombatTargets.Any(i =>  i.Name.ToLower().Contains("Ephialtes".ToLower())))
                    {
                        //LogSpawnEntities();
                        return true;
                    }

                    return false;
                }

                return false;
            }
        }

        private static bool IsRodivaSpawn
        {
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (Combat.PotentialCombatTargets.Any())
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser && i.Name.Contains("Rodiva")))
                    {
                        //LogSpawnEntities();
                        return true;
                    }

                    return false;
                }

                return false;
            }
        }

        private static bool IsAllFrigateSpawn
        {
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (Combat.PotentialCombatTargets.Any())
                {
                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController))
                    {
                        if (AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Tranquil"))
                        {
                            //Tier 0 - this spawn does not exist at this tier
                            if (Combat.PotentialCombatTargets.All(i => i.IsNPCFrigate))
                            {
                                //LogSpawnEntities();
                                return true;
                            }

                            return false;
                        }

                        if (AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Calm"))
                        {
                            //Tier 1 - this spawn does not exist at this tier
                            if (Combat.PotentialCombatTargets.All(i => i.IsNPCFrigate))
                            {
                                //LogSpawnEntities();
                                return true;
                            }

                            return false;
                        }

                        if (AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Agitated"))
                        {
                            //Tier 2
                            if (Combat.PotentialCombatTargets.All(i => i.IsNPCFrigate))
                            {
                                //LogSpawnEntities();
                                return true;
                            }

                            return false;
                        }

                        if (AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Fierce"))
                        {
                            //Tier 3
                            if (Combat.PotentialCombatTargets.All(i => i.IsNPCFrigate))
                            {
                                //LogSpawnEntities();
                                return true;
                            }

                            return false;
                        }

                        if (AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Raging") ||
                            AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Chaotic"))
                        {
                            //Tier 4 or Tier 5
                            if (Combat.PotentialCombatTargets.All(i => i.IsNPCFrigate))
                            {
                                //LogSpawnEntities();
                                return true;
                            }

                            return false;
                        }
                    }

                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalController))
                    {
                        if (Combat.PotentialCombatTargets.All(i => i.IsNPCFrigate))
                        {
                            //LogSpawnEntities();
                            return true;
                        }

                        return false;
                    }

                    return false;
                }

                return false;
            }
        }

        private static bool IsHighAngleBattleCruiserSpawn
        {
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (Combat.PotentialCombatTargets.Any())
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser && i.Name.Contains("Blastgrip Tessera") || i.Name.Contains("Embergrip Tessera") || i.Name.Contains("Sparkgrip Tessera") || i.Name.Contains("Tessera")))
                    {
                        //LogSpawnEntities();
                        return true;
                    }
                }

                return false;
            }
        }

        private static bool IsDrekavacBattleCruiserSpawn
        {
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (Combat.PotentialCombatTargets.Any())
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser && i.Name.Contains("Drekavac")))
                    {
                        //LogSpawnEntities();
                        return true;
                    }
                }

                return false;
            }
        }

        private static bool IsAbyssalOvermindBSSpawn
        {
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (Combat.PotentialCombatTargets.Any())
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && i.Name.Contains("Abyssal Overmind")))
                    {
                        //LogSpawnEntities();
                        return true;
                    }

                }

                return false;
            }
        }

        private static bool IsLeshakBSSpawn
        {
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (Combat.PotentialCombatTargets.Any())
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && i.Name.Contains("Leshak")))
                    {
                        //LogSpawnEntities();
                        return true;
                    }
                }

                return false;
            }
        }

        private static bool IsLuciferSpawn
        {
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (Combat.PotentialCombatTargets.Any())
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.Name.Contains("Lucifer")))
                    {
                        //LogSpawnEntities();
                        return true;
                    }
                }

                return false;
            }
        }

        private static bool IsLucidDeepwatcherBSSpawn
        {
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (Combat.PotentialCombatTargets.Any())
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && i.Name.ToLower().Contains("Lucid Deepwatcher".ToLower())))
                    {
                        //LogSpawnEntities();
                        return true;
                    }
                }

                return false;
            }
        }

        private static bool IsLucidFrigateSpawn
        {
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (Combat.PotentialCombatTargets.Any())
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.Name.ToLower().Contains("Lucid".ToLower())))
                    {
                        //LogSpawnEntities();
                        return true;
                    }

                    return false;
                }

                return false;
            }
        }

        private static bool IsDamavikFrigateSpawn
        {
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (Combat.PotentialCombatTargets.Any())
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.Name.ToLower().Contains("Damavik".ToLower())))
                    {
                        //LogSpawnEntities();
                        return true;
                    }

                    return false;
                }

                return false;
            }
        }

        private static bool IsDroneFrigateSpawn
        {
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (Combat.PotentialCombatTargets.Any())
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate))
                    {
                        if (Combat.PotentialCombatTargets.Count(i => i.Name.ToLower().Contains("Tessella".ToLower())) > 6)
                        {
                            //LogSpawnEntities();
                            return true;
                        }

                        if (Combat.PotentialCombatTargets.Count(i => i.Name.ToLower().Contains("Tessera".ToLower())) > 2)
                        {
                            //LogSpawnEntities();
                            return true;
                        }

                        if (Combat.PotentialCombatTargets.Count(i => i.Name.ToLower().Contains("Tessella".ToLower())) > 2)
                        {
                            //LogSpawnEntities();
                            return true;
                        }

                        if (6 >= Combat.PotentialCombatTargets.Count())
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.Name.ToLower().Contains("Tessella".ToLower())))
                            {
                                //LogSpawnEntities();
                                return true;
                            }
                        }

                        return false;
                    }

                    return false;
                }

                return false;
            }
        }

        private static bool IsConcordSpawn
        {
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (Combat.PotentialCombatTargets.Any())
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && i.Name.ToLower().Contains("Marshal ".ToLower())))
                    {
                        //LogSpawnEntities();
                        return true;
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && i.Name.ToLower().Contains("Thunderchild ".ToLower())))
                    {
                        //LogSpawnEntities();
                        return true;
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser && i.Name.ToLower().Contains("Drainer Enforcer ".ToLower())))
                    {
                        //LogSpawnEntities();
                        return true;
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser && i.Name.ToLower().Contains("Assault Enforcer ".ToLower())))
                    {
                        //LogSpawnEntities();
                        return true;
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate && i.Name.ToLower().Contains("Skybreaker ".ToLower())))
                    {
                        //LogSpawnEntities();
                        return true;
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate && i.Name.ToLower().Contains("Attacker Pacifier ".ToLower())))
                    {
                        //LogSpawnEntities();
                        return true;
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate && i.Name.ToLower().Contains("Marker Pacifier ".ToLower())))
                    {
                        //LogSpawnEntities();
                        return true;
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate && i.Name.ToLower().Contains("Disparu Troop".ToLower())))
                    {
                        //LogSpawnEntities();
                        return true;
                    }
                }

                return false;
            }
        }

        public static void InvalidateCache()
        {
            _abyssalPotentialCombatTargets_Drones = null;
            _abyssalPotentialCombatTargets_Guns = null;
            _abyssalPotentialCombatTargets_Targets = null;
        }

        private static IEnumerable<EntityCache> _abyssalPotentialCombatTargets_Drones = null;
        private static IEnumerable<EntityCache> _abyssalPotentialCombatTargets_Guns = null;
        private static IEnumerable<EntityCache> _abyssalPotentialCombatTargets_Targets = null;

        public static IEnumerable<EntityCache> AbyssalPotentialCombatTargets_Drones
        {
            get
            {
                AbyssalSpawn.AbyssalSpawnType AbyssalDetectSpawnResult = AbyssalSpawn.DetectSpawn;

                if (AbyssalDetectSpawnResult != AbyssalSpawn.AbyssalSpawnType.Undecided)
                {
                    switch (AbyssalDetectSpawnResult)
                    {
                        case AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn:
                            return AbyssalPotentialCombatTargets_AbyssalOvermindSpawn(true, false);

                        case AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn:
                        case AbyssalSpawn.AbyssalSpawnType.DamavikFrigateSpawn:
                        case AbyssalSpawn.AbyssalSpawnType.LucidFrigateSpawn:
                            return AbyssalPotentialCombatTargets_AllFrigateSpawn(true, false);

                        case AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn:
                            return AbyssalPotentialCombatTargets_HighAngleBattlecruiserSpawn(true, false);

                        case AbyssalSpawn.AbyssalSpawnType.DrekavacBattleCruiserSpawn:
                            return AbyssalPotentialCombatTargets_UndetectedSpawn(true, false);

                        case AbyssalSpawn.AbyssalSpawnType.ConcordSpawn:
                            return AbyssalPotentialCombatTargets_ConcordSpawn(true, false);

                        case AbyssalSpawn.AbyssalSpawnType.CruiserSpawn:
                            return AbyssalPotentialCombatTargets_EphialtesCruiserSpawn(true, false);

                        case AbyssalSpawn.AbyssalSpawnType.DevotedCruiserSpawn:
                            return AbyssalPotentialCombatTargets_DevotedCruiserSpawn(true, false);

                        case AbyssalSpawn.AbyssalSpawnType.EphialtesCruiserSpawn:
                            return AbyssalPotentialCombatTargets_EphialtesCruiserSpawn(true, false);

                        case AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn:
                            return AbyssalPotentialCombatTargets_KarybdisTyrannosSpawn(true, false);

                        case AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn:
                            return AbyssalPotentialCombatTargets_KikimoraDestroyerSpawn(true, false);

                        case AbyssalSpawn.AbyssalSpawnType.LeshakBSSpawn:
                            return AbyssalPotentialCombatTargets_LeshakSpawn(true, false);

                        //case AbyssalSpawn.AbyssalSpawnType.LuciferSpawn:
                        //    return AbyssalPotentialCombatTargets_LuciferSpawn(true, false);

                        case AbyssalSpawn.AbyssalSpawnType.LucidDeepwatcherBSSpawn:
                            return AbyssalPotentialCombatTargets_UndetectedSpawn(true, false);

                        case AbyssalSpawn.AbyssalSpawnType.LucidCruiserSpawn:
                            return AbyssalPotentialCombatTargets_EphialtesCruiserSpawn(true, false);

                        case AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn:
                            return AbyssalPotentialCombatTargets_VedmakSpawn(true, false);

                        case AbyssalSpawn.AbyssalSpawnType.VedmakVilaCruiserSwarmerSpawn:
                            return AbyssalPotentialCombatTargets_VedmakVilaCruiserSwarmerSpawn(true, false);

                        case AbyssalSpawn.AbyssalSpawnType.RodivaSpawn:
                            return AbyssalPotentialCombatTargets_RodivaSpawn(true, false);
                    }
                }

                return AbyssalPotentialCombatTargets_UndetectedSpawn(true, false);
            }
        }

        public static IEnumerable<EntityCache> AbyssalPotentialCombatTargets_Guns
        {
            get
            {
                AbyssalSpawn.AbyssalSpawnType AbyssalDetectSpawnResult = AbyssalSpawn.DetectSpawn;

                if (AbyssalDetectSpawnResult != AbyssalSpawn.AbyssalSpawnType.Undecided)
                {
                    switch (AbyssalDetectSpawnResult)
                    {
                        case AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn:
                            return AbyssalPotentialCombatTargets_AbyssalOvermindSpawn(false, true);

                        case AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn:
                        case AbyssalSpawn.AbyssalSpawnType.DamavikFrigateSpawn:
                        case AbyssalSpawn.AbyssalSpawnType.LucidFrigateSpawn:
                            return AbyssalPotentialCombatTargets_AllFrigateSpawn(false, true);

                        case AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn:
                            return AbyssalPotentialCombatTargets_HighAngleBattlecruiserSpawn(false, true);

                        case AbyssalSpawn.AbyssalSpawnType.DrekavacBattleCruiserSpawn:
                            return AbyssalPotentialCombatTargets_UndetectedSpawn(false, true);

                        case AbyssalSpawn.AbyssalSpawnType.ConcordSpawn:
                            return AbyssalPotentialCombatTargets_ConcordSpawn(false, true);

                        case AbyssalSpawn.AbyssalSpawnType.CruiserSpawn:
                            return AbyssalPotentialCombatTargets_EphialtesCruiserSpawn(false, true);

                        case AbyssalSpawn.AbyssalSpawnType.DevotedCruiserSpawn:
                            return AbyssalPotentialCombatTargets_DevotedCruiserSpawn(false, true);

                        case AbyssalSpawn.AbyssalSpawnType.EphialtesCruiserSpawn:
                            return AbyssalPotentialCombatTargets_EphialtesCruiserSpawn(false, true);

                        case AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn:
                            return AbyssalPotentialCombatTargets_KarybdisTyrannosSpawn(false, true);

                        case AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn:
                            return AbyssalPotentialCombatTargets_KikimoraDestroyerSpawn(false, true);

                        case AbyssalSpawn.AbyssalSpawnType.LeshakBSSpawn:
                            return AbyssalPotentialCombatTargets_LeshakSpawn(false, true);

                        case AbyssalSpawn.AbyssalSpawnType.LucidDeepwatcherBSSpawn:
                            return AbyssalPotentialCombatTargets_UndetectedSpawn(false, true);

                        case AbyssalSpawn.AbyssalSpawnType.LucidCruiserSpawn:
                            return AbyssalPotentialCombatTargets_EphialtesCruiserSpawn(false, true);

                        case AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn:
                            return AbyssalPotentialCombatTargets_VedmakSpawn(false, true);

                        case AbyssalSpawn.AbyssalSpawnType.VedmakVilaCruiserSwarmerSpawn:
                            return AbyssalPotentialCombatTargets_VedmakVilaCruiserSwarmerSpawn(false, true);

                        case AbyssalSpawn.AbyssalSpawnType.RodivaSpawn:
                            return AbyssalPotentialCombatTargets_RodivaSpawn(false, true);
                    }
                }

                return AbyssalPotentialCombatTargets_UndetectedSpawn(false, true);
            }
        }

        public static IEnumerable<EntityCache> AbyssalPotentialCombatTargets_Targets
        {
            get
            {
                AbyssalSpawn.AbyssalSpawnType AbyssalDetectSpawnResult = AbyssalSpawn.DetectSpawn;

                if (AbyssalDetectSpawnResult != AbyssalSpawn.AbyssalSpawnType.Undecided)
                {
                    switch (AbyssalDetectSpawnResult)
                    {
                        case AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: AbyssalOvermindBSSpawn");
                                return AbyssalPotentialCombatTargets_AbyssalOvermindSpawn(false, false);
                            }

                        case AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn:
                        case AbyssalSpawn.AbyssalSpawnType.DamavikFrigateSpawn:
                        case AbyssalSpawn.AbyssalSpawnType.LucidFrigateSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: AllFrigateSpawn");
                                return AbyssalPotentialCombatTargets_AllFrigateSpawn(false, false);
                            }

                        case AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: HighAngleBattleCruiserSpawn");
                                return AbyssalPotentialCombatTargets_HighAngleBattlecruiserSpawn(false, false);
                            }


                        case AbyssalSpawn.AbyssalSpawnType.DrekavacBattleCruiserSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: HighAngleBattleCruiserSpawn");
                                return AbyssalPotentialCombatTargets_LeshakSpawn(false, false);
                            }

                        case AbyssalSpawn.AbyssalSpawnType.ConcordSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: ConcordBSSpawn");
                                return AbyssalPotentialCombatTargets_ConcordSpawn(false, false);
                            }

                        case AbyssalSpawn.AbyssalSpawnType.CruiserSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: CruiserSpawn");
                                return AbyssalPotentialCombatTargets_EphialtesCruiserSpawn(false, false);
                            }

                        case AbyssalSpawn.AbyssalSpawnType.DevotedCruiserSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: DevotedCruiserSpawn");
                                return AbyssalPotentialCombatTargets_DevotedCruiserSpawn(false, false);
                            }

                        case AbyssalSpawn.AbyssalSpawnType.EphialtesCruiserSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: EphialtesCruiserSpawn");
                                return AbyssalPotentialCombatTargets_EphialtesCruiserSpawn(false, false);
                            }

                        case AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: KarybdisTyrannosSpawn");
                                return AbyssalPotentialCombatTargets_KarybdisTyrannosSpawn(false, false);
                            }

                        case AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: KikimoraDestroyerSpawn");
                                return AbyssalPotentialCombatTargets_KikimoraDestroyerSpawn(false, false);
                            }

                        case AbyssalSpawn.AbyssalSpawnType.LeshakBSSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: LeshakBSSpawn");
                                return AbyssalPotentialCombatTargets_LeshakSpawn(false, false);
                            }

                        case AbyssalSpawn.AbyssalSpawnType.LucidDeepwatcherBSSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: LucidDeepwatcherBSSpawn");
                                return AbyssalPotentialCombatTargets_UndetectedSpawn(false, false);
                            }

                        case AbyssalSpawn.AbyssalSpawnType.LucidCruiserSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: LucidWatchmanCruiserSpawn");
                                return AbyssalPotentialCombatTargets_EphialtesCruiserSpawn(false, false);
                            }

                        case AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: VedmakCruiserSpawn");
                                return AbyssalPotentialCombatTargets_VedmakSpawn(false, false);
                            }

                        case AbyssalSpawn.AbyssalSpawnType.VedmakVilaCruiserSwarmerSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: VedmakVilaCruiserSwarmerSpawn");
                                return AbyssalPotentialCombatTargets_VedmakVilaCruiserSwarmerSpawn(false, false);
                            }

                        case AbyssalSpawn.AbyssalSpawnType.RodivaSpawn:
                            {
                                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: RodivaSpawn");
                                return AbyssalPotentialCombatTargets_RodivaSpawn(false, false);
                            }
                    }
                }

                if (DebugConfig.DebugTargetCombatants) Log.WriteLine("AbyssalPotentialCombatTargets_Targets: UndetectedSpawn");
                return AbyssalPotentialCombatTargets_UndetectedSpawn(false, false);
            }
        }

        private static bool ShouldRefreshEntitiesForDrones(bool IsPotentialCombatTargetForDrones)
        {
            if (IsPotentialCombatTargetForDrones && _abyssalPotentialCombatTargets_Drones == null)
                return true;

            return false;
        }

        private static bool ShouldRefreshEntitiesForGuns(bool IsPotentialCombatTargetForGuns)
        {
            if (IsPotentialCombatTargetForGuns && _abyssalPotentialCombatTargets_Drones == null)
                return true;

            return false;
        }

        private static bool ShouldRefreshEntitiesForTargeting(bool IsPotentialCombatTargetForDrones, bool IsPotentialCombatTargetForGuns)
        {
            if (!IsPotentialCombatTargetForDrones && !IsPotentialCombatTargetForGuns && _abyssalPotentialCombatTargets_Targets == null)
                return true;

            return false;
        }

        public static IEnumerable<EntityCache> AbyssalPotentialCombatTargets_RodivaSpawn(bool IsPotentialCombatTargetForDrones = false, bool IsPotentialCombatTargetForGuns = false)
        {
            IEnumerable<EntityCache> tempListOfTargets = null;

            if (ShouldRefreshEntitiesForDrones(IsPotentialCombatTargetForDrones) || ShouldRefreshEntitiesForGuns(IsPotentialCombatTargetForGuns) || ShouldRefreshEntitiesForTargeting(IsPotentialCombatTargetForDrones, IsPotentialCombatTargetForGuns))
            {
                if (ESCache.Instance.MyShipEntity.IsFrigate)
                {
                    tempListOfTargets = AbyssalPotentialCombatTargets_FrigateAbyssal(IsPotentialCombatTargetForDrones, IsPotentialCombatTargetForGuns);
                }
                else
                {
                    tempListOfTargets = Combat.PotentialCombatTargets.Where(i => (IsPotentialCombatTargetForDrones && i.IsReadyForDronesToShoot) || (IsPotentialCombatTargetForGuns && i.IsReadyToShoot) || (!IsPotentialCombatTargetForDrones && !IsPotentialCombatTargetForGuns && i.IsReadyToTarget))
                        .OrderByDescending(l => l.IsNPCFrigate && !l.IsAttacking && l.IsWebbingMe && Time.ItHasBeenThisLongSinceWeStartedThePocket.TotalSeconds > 15)
                        .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking && l.IsWarpScramblingMe && Time.ItHasBeenThisLongSinceWeStartedThePocket.TotalSeconds > 15)


                        .ThenByDescending(l => l.IsWithin5KOfOurDrones && l.IsNPCCruiser && l.NpcHasALotOfRemoteRepair)
                        .ThenByDescending(l => l.IsWithin10KOfOurDrones && l.IsNPCCruiser && l.NpcHasALotOfRemoteRepair)
                        .ThenByDescending(l => l.IsWithin15KOfOurDrones && l.IsNPCCruiser && l.NpcHasALotOfRemoteRepair)
                        .ThenByDescending(l => l.IsWithin20KOfOurDrones && l.IsNPCCruiser && l.NpcHasALotOfRemoteRepair)
                        .ThenByDescending(l => l.IsWithin25KOfOurDrones && l.IsNPCCruiser && l.NpcHasALotOfRemoteRepair)
                        .ThenByDescending(l => l.IsWithin30KOfOurDrones && l.IsNPCCruiser && l.NpcHasALotOfRemoteRepair)
                        .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasNeutralizers)

                        //Neut

                        .ThenByDescending(l => l.IsWithin5KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin10KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin15KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin20KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin25KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin30KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)

                        //
                        // webs
                        //
                        .ThenByDescending(l => l.IsWithin5KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe && 100 > l.HealthPct)
                        .ThenByDescending(l => l.IsWithin5KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin10KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe && 100 > l.HealthPct)
                        .ThenByDescending(l => l.IsWithin10KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin15KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe && 100 > l.HealthPct)
                        .ThenByDescending(l => l.IsWithin15KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin20KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe && 100 > l.HealthPct)
                        .ThenByDescending(l => l.IsWithin20KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin25KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe && 100 > l.HealthPct)
                        .ThenByDescending(l => l.IsWithin25KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin30KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe && 100 > l.HealthPct)
                        .ThenByDescending(l => l.IsWithin30KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)

                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe && 100 > l.HealthPct)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe)

                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && l.IsWithin10KOfOurDrones)
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking)
                        .ThenByDescending(l => l.IsNPCCruiser && 100 > l.HealthPct)
                        //.ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers)
                        //.ThenByDescending(l => l.IsNPCFrigate && l.NpcHasRemoteRepair)
                        //.ThenByDescending(l => l.IsNPCFrigate && l.IsSensorDampeningMe)
                        //.ThenByDescending(l => l.IsNPCFrigate && l.IsTargetPaintingMe)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsTrackingDisruptingMe && Combat.DoWeCurrentlyHaveTurretsMounted())
                        //.ThenByDescending(l => l.IsNPCFrigate)
                        //.ThenByDescending(i => i.IsEntityIShouldKeepShooting);
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWithin5KOfOurDrones)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWithin10KOfOurDrones)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWithin15KOfOurDrones)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWithin20KOfOurDrones)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWithin25KOfOurDrones)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWithin30KOfOurDrones)

                        .ThenByDescending(l => l.IsWithin5KOfOurDrones)
                        .ThenByDescending(l => l.IsWithin10KOfOurDrones)
                        .ThenByDescending(l => l.IsWithin15KOfOurDrones)
                        .ThenByDescending(l => l.IsWithin20KOfOurDrones)
                        .ThenByDescending(l => l.IsWithin25KOfOurDrones)
                        .ThenByDescending(l => l.IsWithin30KOfOurDrones)
                        .Take(ESCache.Instance.MaxLockedTargets ?? 2);
                }
            }

            if (IsPotentialCombatTargetForDrones)
            {
                if (tempListOfTargets != null && tempListOfTargets.Any())
                {
                    _abyssalPotentialCombatTargets_Drones = tempListOfTargets;
                    return _abyssalPotentialCombatTargets_Drones;
                }

                if (_abyssalPotentialCombatTargets_Drones != null && _abyssalPotentialCombatTargets_Drones.Any())
                    return _abyssalPotentialCombatTargets_Drones;

                return new List<EntityCache>();
            }

            if (IsPotentialCombatTargetForGuns)
            {
                if (tempListOfTargets != null && tempListOfTargets.Any())
                {
                    _abyssalPotentialCombatTargets_Guns = tempListOfTargets;
                    return _abyssalPotentialCombatTargets_Guns;
                }

                if (_abyssalPotentialCombatTargets_Guns != null && _abyssalPotentialCombatTargets_Guns.Any())
                    return _abyssalPotentialCombatTargets_Guns;

                return new List<EntityCache>();
            }

            if (tempListOfTargets != null && tempListOfTargets.Any())
            {
                _abyssalPotentialCombatTargets_Targets = tempListOfTargets;
                return _abyssalPotentialCombatTargets_Targets;
            }

            if (_abyssalPotentialCombatTargets_Targets != null && _abyssalPotentialCombatTargets_Targets.Any())
                return _abyssalPotentialCombatTargets_Targets;

            return new List<EntityCache>();
        }

        public static IEnumerable<EntityCache> AbyssalPotentialCombatTargets_UndetectedSpawn(bool IsPotentialCombatTargetForDrones = false, bool IsPotentialCombatTargetForGuns = false)
        {
            IEnumerable<EntityCache> tempListOfTargets = null;

            if (ShouldRefreshEntitiesForDrones(IsPotentialCombatTargetForDrones) || ShouldRefreshEntitiesForGuns(IsPotentialCombatTargetForGuns) || ShouldRefreshEntitiesForTargeting(IsPotentialCombatTargetForDrones, IsPotentialCombatTargetForGuns))
            {
                if (ESCache.Instance.MyShipEntity.IsFrigate)
                {
                    tempListOfTargets = AbyssalPotentialCombatTargets_FrigateAbyssal(IsPotentialCombatTargetForDrones, IsPotentialCombatTargetForGuns);
                }
                else
                {
                    tempListOfTargets = Combat.PotentialCombatTargets.Where(i => (IsPotentialCombatTargetForDrones && i.IsReadyForDronesToShoot) || (IsPotentialCombatTargetForGuns && i.IsReadyToShoot) || (!IsPotentialCombatTargetForDrones && !IsPotentialCombatTargetForGuns && i.IsReadyToTarget))
                        .OrderByDescending(i => !i.Name.ToLower().Contains("hadal abyssal overmind".ToLower())) //last
                        .ThenByDescending(i => !i.Name.ToLower().Contains("lucid deepwatcher".ToLower())) //last
                        .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers && l.WeWantToPrioritizeNeutralizers)
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && l.IsWebbingMe && Drones.DroneIsTooFarFromOldTarget)
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && l.IsWarpScramblingMe && Drones.DroneIsTooFarFromOldTarget)
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && l.IsLowestHealthNpcWithThisSameName && l.IsLastTargetDronesWereShooting && Drones.DroneIsTooFarFromOldTarget) //kill things shooting drones!
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && l.IsLowestHealthNpcWithThisSameName && Drones.DroneIsTooFarFromOldTarget) //kill things shooting drones!
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && !l.NpcHasALotOfRemoteRepair && Drones.DroneIsTooFarFromOldTarget) //kill things shooting drones!
                        .ThenByDescending(j => j.IsNPCCruiser && j.NpcHasNeutralizers && j.WeWantToPrioritizeNeutralizers && j.IsLowestHealthNpcWithThisSameName)
                        .ThenByDescending(j => j.IsNPCCruiser && j.NpcHasNeutralizers && j.WeWantToPrioritizeNeutralizers && !j.IsAttacking)
                        .ThenByDescending(j => j.IsNPCCruiser && j.NpcHasNeutralizers && j.WeWantToPrioritizeNeutralizers)
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking) //kill things shooting drones!
                        .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking && l.IsWebbingMe && l.IsLowestHealthNpcWithThisSameName && Drones.DroneIsTooFarFromOldTarget)
                        .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking && l.IsWebbingMe && Drones.DroneIsTooFarFromOldTarget)
                        .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking && l.IsWarpScramblingMe && Drones.DroneIsTooFarFromOldTarget)
                        .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking && l.IsWarpScramblingMe && l.IsLowestHealthNpcWithThisSameName && Drones.DroneIsTooFarFromOldTarget)
                        .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking && l.IsLowestHealthNpcWithThisSameName)
                        .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking) // dont kill all non-attacking frigates until we have dealt with BCs (see above)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsLowestHealthNpcWithThisSameName)
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking) //kill things shooting drones!
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe && l.WeWantToPrioritizeWebs)
                        .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasNeutralizers && l.WeWantToPrioritizeNeutralizers)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWithinOptimalOfDrones)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsCloseToDrones)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWithin15KOfOurDrones)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWithin20KOfOurDrones)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWithin25KOfOurDrones)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWithin30KOfOurDrones)
                        .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasRemoteRepair && l.WeWantToPrioritizeRemoteRepair)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsSensorDampeningMe && l.WeWantToPrioritizeSensorDamps)
                        .ThenByDescending(j => j.IsNPCCruiser && j.IsHighDps)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsTargetPaintingMe && l.WeWantToPrioritizeTargetPainters)
                        .ThenByDescending(i => i.IsNPCCruiser && i.IsTrackable && ESCache.Instance.ActiveShip.HasTurrets)
                        .ThenByDescending(i => i.IsNPCCruiser && i.IsInOptimalRange && ESCache.Instance.ActiveShip.HasTurrets)
                        .ThenByDescending(l => l.IsNPCCruiser)
                        .ThenByDescending(l => l.IsNPCFrigate && l.IsLowestHealthNpcWithThisSameName)
                        .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe && l.WeWantToPrioritizeWebs)
                        .ThenByDescending(l => l.IsNPCFrigate && l.IsWithinOptimalOfDrones)
                        .ThenByDescending(l => l.IsNPCFrigate && l.IsCloseToDrones)
                        .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers && l.WeWantToPrioritizeNeutralizers)
                        .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasRemoteRepair && l.WeWantToPrioritizeRemoteRepair)
                        .ThenByDescending(l => l.IsNPCFrigate && l.IsSensorDampeningMe && l.WeWantToPrioritizeSensorDamps)
                        .ThenByDescending(l => l.IsNPCFrigate && l.IsTargetPaintingMe && l.WeWantToPrioritizeTargetPainters)
                        .ThenByDescending(l => l.IsNPCFrigate && l.IsTrackingDisruptingMe && l.WeWantToPrioritizeTrackingDistuptors)
                        .ThenByDescending(l => l.IsNPCFrigate)
                        .ThenByDescending(i => i.IsEntityIShouldKeepShooting)
                        .Take(ESCache.Instance.MaxLockedTargets ?? 2);
                }
            }

            if (IsPotentialCombatTargetForDrones)
            {
                if (tempListOfTargets != null && tempListOfTargets.Any())
                {
                    _abyssalPotentialCombatTargets_Drones = tempListOfTargets;
                    return _abyssalPotentialCombatTargets_Drones;
                }

                if (_abyssalPotentialCombatTargets_Drones != null && _abyssalPotentialCombatTargets_Drones.Any())
                    return _abyssalPotentialCombatTargets_Drones;

                return new List<EntityCache>();
            }

            if (IsPotentialCombatTargetForGuns)
            {
                if (tempListOfTargets != null && tempListOfTargets.Any())
                {
                    _abyssalPotentialCombatTargets_Guns = tempListOfTargets;
                    return _abyssalPotentialCombatTargets_Guns;
                }

                if (_abyssalPotentialCombatTargets_Guns != null && _abyssalPotentialCombatTargets_Guns.Any())
                    return _abyssalPotentialCombatTargets_Guns;

                return new List<EntityCache>();
            }

            if (tempListOfTargets != null && tempListOfTargets.Any())
            {
                _abyssalPotentialCombatTargets_Targets = tempListOfTargets;
                return _abyssalPotentialCombatTargets_Targets;
            }

            if (_abyssalPotentialCombatTargets_Targets != null && _abyssalPotentialCombatTargets_Targets.Any())
                return _abyssalPotentialCombatTargets_Targets;

            return new List<EntityCache>();
        }

        public static IEnumerable<EntityCache> AbyssalPotentialCombatTargets_LeshakSpawn(bool IsPotentialCombatTargetForDrones = false, bool IsPotentialCombatTargetForGuns = false)
        {
            IEnumerable<EntityCache> tempListOfTargets = null;

            if (ShouldRefreshEntitiesForDrones(IsPotentialCombatTargetForDrones) || ShouldRefreshEntitiesForGuns(IsPotentialCombatTargetForGuns) || ShouldRefreshEntitiesForTargeting(IsPotentialCombatTargetForDrones, IsPotentialCombatTargetForGuns))
            {
                if (ESCache.Instance.MyShipEntity.IsFrigate)
                {
                    tempListOfTargets = AbyssalPotentialCombatTargets_FrigateAbyssal(IsPotentialCombatTargetForDrones, IsPotentialCombatTargetForGuns);
                }
                else
                {
                    tempListOfTargets = Combat.PotentialCombatTargets.Where(i => (IsPotentialCombatTargetForDrones && i.IsReadyForDronesToShoot) || (IsPotentialCombatTargetForGuns && i.IsReadyToShoot) || (!IsPotentialCombatTargetForDrones && !IsPotentialCombatTargetForGuns && i.IsReadyToTarget))
                        .OrderByDescending(i => !i.Name.ToLower().Contains("hadal abyssal overmind".ToLower())) //last
                        .ThenByDescending(i => !i.Name.ToLower().Contains("lucid deepwatcher".ToLower())) //last
                        //
                        //Neuts
                        //
                        .ThenByDescending(l => 100 > l.HealthPct && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin5KOfOurDrones && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin10KOfOurDrones && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin15KOfOurDrones && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin20KOfOurDrones && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin25KOfOurDrones && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin30KOfOurDrones && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)

                        //
                        // webs
                        //
                        .ThenByDescending(l => 100 > l.HealthPct && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin5KOfOurDrones && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin10KOfOurDrones && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin15KOfOurDrones && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin20KOfOurDrones && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin25KOfOurDrones && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin30KOfOurDrones && l.IsWebbingMe)

                        .ThenByDescending(l => 100 > l.HealthPct && l.IsNPCBattleship)
                        .ThenByDescending(l => l.IsNPCBattleship)

                        .Take(ESCache.Instance.MaxLockedTargets ?? 2);
                }
            }

            if (IsPotentialCombatTargetForDrones)
            {
                if (tempListOfTargets != null && tempListOfTargets.Any())
                {
                    _abyssalPotentialCombatTargets_Drones = tempListOfTargets;
                    return _abyssalPotentialCombatTargets_Drones;
                }

                if (_abyssalPotentialCombatTargets_Drones != null && _abyssalPotentialCombatTargets_Drones.Any())
                    return _abyssalPotentialCombatTargets_Drones;

                return new List<EntityCache>();
            }

            if (IsPotentialCombatTargetForGuns)
            {
                if (tempListOfTargets != null && tempListOfTargets.Any())
                {
                    _abyssalPotentialCombatTargets_Guns = tempListOfTargets;
                    return _abyssalPotentialCombatTargets_Guns;
                }

                if (_abyssalPotentialCombatTargets_Guns != null && _abyssalPotentialCombatTargets_Guns.Any())
                    return _abyssalPotentialCombatTargets_Guns;

                return new List<EntityCache>();
            }

            if (tempListOfTargets != null && tempListOfTargets.Any())
            {
                _abyssalPotentialCombatTargets_Targets = tempListOfTargets;
                return _abyssalPotentialCombatTargets_Targets;
            }

            if (_abyssalPotentialCombatTargets_Targets != null && _abyssalPotentialCombatTargets_Targets.Any())
                return _abyssalPotentialCombatTargets_Targets;

            return new List<EntityCache>();
        }

        public static IEnumerable<EntityCache> AbyssalPotentialCombatTargets_AbyssalOvermindSpawn(bool IsPotentialCombatTargetForDrones = false, bool IsPotentialCombatTargetForGuns = false)
        {
            IEnumerable<EntityCache> tempListOfTargets = null;

            if (ShouldRefreshEntitiesForDrones(IsPotentialCombatTargetForDrones) || ShouldRefreshEntitiesForGuns(IsPotentialCombatTargetForGuns) || ShouldRefreshEntitiesForTargeting(IsPotentialCombatTargetForDrones, IsPotentialCombatTargetForGuns))
            {
                if (ESCache.Instance.MyShipEntity.IsFrigate)
                {
                    tempListOfTargets = AbyssalPotentialCombatTargets_FrigateAbyssal(IsPotentialCombatTargetForDrones, IsPotentialCombatTargetForGuns);
                }
                else
                {
                    tempListOfTargets = Combat.PotentialCombatTargets.Where(i => (IsPotentialCombatTargetForDrones && i.IsReadyForDronesToShoot) || (IsPotentialCombatTargetForGuns && i.IsReadyToShoot) || (!IsPotentialCombatTargetForDrones && !IsPotentialCombatTargetForGuns && i.IsReadyToTarget))
                        .OrderByDescending(i => !i.Name.ToLower().Contains("abyssal overmind".ToLower()))
                        .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking && l.IsWebbingMe && Time.ItHasBeenThisLongSinceWeStartedThePocket.TotalSeconds > 15)
                        .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking && l.IsWarpScramblingMe && Time.ItHasBeenThisLongSinceWeStartedThePocket.TotalSeconds > 15)

                        //Neut

                        .ThenByDescending(l => l.IsWithin5KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin10KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin15KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin20KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin25KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin30KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)

                        //
                        // webs
                        //
                        .ThenByDescending(l => l.IsWithin5KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin10KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin15KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin20KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin25KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin30KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)

                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe)

                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsTrackingDisruptingMe && Combat.DoWeCurrentlyHaveTurretsMounted())
                        .ThenByDescending(l => l.IsWithin5KOfOurDrones)
                        .ThenByDescending(l => l.IsWithin10KOfOurDrones)
                        .ThenByDescending(l => l.IsWithin15KOfOurDrones)
                        .ThenByDescending(l => l.IsWithin20KOfOurDrones)
                        .ThenByDescending(l => l.IsWithin25KOfOurDrones)
                        .ThenByDescending(l => l.IsWithin30KOfOurDrones)
                        .Take(ESCache.Instance.MaxLockedTargets ?? 2);
                }
            }

            if (IsPotentialCombatTargetForDrones)
            {
                if (tempListOfTargets != null && tempListOfTargets.Any())
                {
                    _abyssalPotentialCombatTargets_Drones = tempListOfTargets;
                    return _abyssalPotentialCombatTargets_Drones;
                }

                if (_abyssalPotentialCombatTargets_Drones != null && _abyssalPotentialCombatTargets_Drones.Any())
                    return _abyssalPotentialCombatTargets_Drones;

                return new List<EntityCache>();
            }

            if (IsPotentialCombatTargetForGuns)
            {
                if (tempListOfTargets != null && tempListOfTargets.Any())
                {
                    _abyssalPotentialCombatTargets_Guns = tempListOfTargets;
                    return _abyssalPotentialCombatTargets_Guns;
                }

                if (_abyssalPotentialCombatTargets_Guns != null && _abyssalPotentialCombatTargets_Guns.Any())
                    return _abyssalPotentialCombatTargets_Guns;

                return new List<EntityCache>();
            }

            if (tempListOfTargets != null && tempListOfTargets.Any())
            {
                _abyssalPotentialCombatTargets_Targets = tempListOfTargets;
                return _abyssalPotentialCombatTargets_Targets;
            }

            if (_abyssalPotentialCombatTargets_Targets != null && _abyssalPotentialCombatTargets_Targets.Any())
                return _abyssalPotentialCombatTargets_Targets;

            return new List<EntityCache>();
        }

        public static IEnumerable<EntityCache> AbyssalPotentialCombatTargets_AllFrigateSpawn(bool IsPotentialCombatTargetForDrones = false, bool IsPotentialCombatTargetForGuns = false)
        {
            IEnumerable<EntityCache> tempListOfTargets = null;

            if (ShouldRefreshEntitiesForDrones(IsPotentialCombatTargetForDrones) || ShouldRefreshEntitiesForGuns(IsPotentialCombatTargetForGuns) || ShouldRefreshEntitiesForTargeting(IsPotentialCombatTargetForDrones, IsPotentialCombatTargetForGuns))
            {
                if (ESCache.Instance.MyShipEntity.IsFrigate)
                {
                    tempListOfTargets = AbyssalPotentialCombatTargets_FrigateAbyssal(IsPotentialCombatTargetForDrones, IsPotentialCombatTargetForGuns);
                }
                else
                {
                    tempListOfTargets = Combat.PotentialCombatTargets.Where(i => (IsPotentialCombatTargetForDrones && i.IsReadyForDronesToShoot) || (IsPotentialCombatTargetForGuns && i.IsReadyToShoot) || (!IsPotentialCombatTargetForDrones && !IsPotentialCombatTargetForGuns && i.IsReadyToTarget))
                        .OrderByDescending(l => l.IsNPCFrigate && !l.IsAttacking && l.IsWebbingMe && Time.ItHasBeenThisLongSinceWeStartedThePocket.TotalSeconds > 15)
                        .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking && l.IsWarpScramblingMe && Time.ItHasBeenThisLongSinceWeStartedThePocket.TotalSeconds > 15)

                        //Neut
                        .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)

                        //
                        // webs
                        //

                        .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsNPCFrigate && l.TypeName.Contains("Sparklance Tessella")) //Elite Frigate - Deals More Damage
                        .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking)
                        .ThenByDescending(l => l.IsNPCFrigate && l.IsTrackingDisruptingMe && Combat.DoWeCurrentlyHaveTurretsMounted())
                        .ThenByDescending(l => l.IsWithin5KOfOurDrones && IsPotentialCombatTargetForDrones && 50 > l.HealthPct)
                        .ThenByDescending(l => l.IsWithin10KOfOurDrones && IsPotentialCombatTargetForDrones && 50 > l.HealthPct)
                        .ThenByDescending(l => l.IsWithin15KOfOurDrones && IsPotentialCombatTargetForDrones && 50 > l.HealthPct)
                        .ThenByDescending(l => l.IsWithin20KOfOurDrones && IsPotentialCombatTargetForDrones && 50 > l.HealthPct)
                        .ThenByDescending(l => l.IsWithin25KOfOurDrones && IsPotentialCombatTargetForDrones && 50 > l.HealthPct)
                        .ThenByDescending(l => l.IsWithin30KOfOurDrones && IsPotentialCombatTargetForDrones && 50 > l.HealthPct)
                        .ThenByDescending(l => l.IsWithin5KOfOurDrones && IsPotentialCombatTargetForDrones && 100 > l.HealthPct)
                        .ThenByDescending(l => l.IsWithin10KOfOurDrones && IsPotentialCombatTargetForDrones && 100 > l.HealthPct)
                        .ThenByDescending(l => l.IsWithin15KOfOurDrones && IsPotentialCombatTargetForDrones && 100 > l.HealthPct)
                        .ThenByDescending(l => l.IsWithin20KOfOurDrones && IsPotentialCombatTargetForDrones && 100 > l.HealthPct)
                        .ThenByDescending(l => l.IsWithin25KOfOurDrones && IsPotentialCombatTargetForDrones && 100 > l.HealthPct)
                        .ThenByDescending(l => l.IsWithin30KOfOurDrones && IsPotentialCombatTargetForDrones && 100 > l.HealthPct)
                        .ThenByDescending(l => l.IsWithin5KOfOurDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsWithin10KOfOurDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsWithin15KOfOurDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsWithin20KOfOurDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsWithin25KOfOurDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsWithin30KOfOurDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => !l.IsDroneKillTarget && IsPotentialCombatTargetForGuns)
                        .ThenBy(l => 100 > l.HealthPct)
                        .Take(ESCache.Instance.MaxLockedTargets ?? 2);
                }
            }

            if (IsPotentialCombatTargetForDrones)
            {
                if (tempListOfTargets != null && tempListOfTargets.Any())
                {
                    _abyssalPotentialCombatTargets_Drones = tempListOfTargets;
                    return _abyssalPotentialCombatTargets_Drones;
                }

                if (_abyssalPotentialCombatTargets_Drones != null && _abyssalPotentialCombatTargets_Drones.Any())
                    return _abyssalPotentialCombatTargets_Drones;

                return new List<EntityCache>();
            }

            if (IsPotentialCombatTargetForGuns)
            {
                if (tempListOfTargets != null && tempListOfTargets.Any())
                {
                    _abyssalPotentialCombatTargets_Guns = tempListOfTargets;
                    return _abyssalPotentialCombatTargets_Guns;
                }

                if (_abyssalPotentialCombatTargets_Guns != null && _abyssalPotentialCombatTargets_Guns.Any())
                    return _abyssalPotentialCombatTargets_Guns;

                return new List<EntityCache>();
            }

            if (tempListOfTargets != null && tempListOfTargets.Any())
            {
                _abyssalPotentialCombatTargets_Targets = tempListOfTargets;
                return _abyssalPotentialCombatTargets_Targets;
            }

            if (_abyssalPotentialCombatTargets_Targets != null && _abyssalPotentialCombatTargets_Targets.Any())
                return _abyssalPotentialCombatTargets_Targets;

            return new List<EntityCache>();
        }

        public static IEnumerable<EntityCache> AbyssalPotentialCombatTargets_ConcordSpawn(bool IsPotentialCombatTargetForDrones = false, bool IsPotentialCombatTargetForGuns = false)
        {
            IEnumerable<EntityCache> tempListOfTargets = null;

            if (ShouldRefreshEntitiesForDrones(IsPotentialCombatTargetForDrones) || ShouldRefreshEntitiesForGuns(IsPotentialCombatTargetForGuns) || ShouldRefreshEntitiesForTargeting(IsPotentialCombatTargetForDrones, IsPotentialCombatTargetForGuns))
            {
                if (ESCache.Instance.MyShipEntity.IsFrigate)
                {
                    tempListOfTargets = AbyssalPotentialCombatTargets_FrigateAbyssal(IsPotentialCombatTargetForDrones, IsPotentialCombatTargetForGuns);
                }
                else
                {
                    tempListOfTargets = Combat.PotentialCombatTargets.Where(i => (IsPotentialCombatTargetForDrones && i.IsReadyForDronesToShoot) || (IsPotentialCombatTargetForGuns && i.IsReadyToShoot) || i.IsReadyToTarget)
                        .OrderByDescending(l => ESCache.Instance.ActiveShip.HasSpeedMod && l.IsNPCDestroyer && !l.IsAttacking && l.IsWebbingMe && Drones.DroneIsTooFarFromOldTarget)
                        .ThenByDescending(l => ESCache.Instance.ActiveShip.HasSpeedMod && l.IsNPCDestroyer && !l.IsAttacking && l.IsWarpScramblingMe && Drones.DroneIsTooFarFromOldTarget)
                        .ThenByDescending(l => ESCache.Instance.ActiveShip.HasSpeedMod && l.IsNPCDestroyer && !l.IsAttacking && l.IsWarpScramblingMe)
                        .ThenByDescending(l => ESCache.Instance.ActiveShip.HasSpeedMod && l.IsNPCFrigate && !l.IsAttacking && l.IsWebbingMe && Drones.DroneIsTooFarFromOldTarget)
                        .ThenByDescending(l => ESCache.Instance.ActiveShip.HasSpeedMod && l.IsNPCFrigate && !l.IsAttacking && l.IsWebbingMe)
                        .ThenByDescending(l => ESCache.Instance.ActiveShip.HasSpeedMod && l.IsNPCFrigate && !l.IsAttacking && l.IsWarpScramblingMe && Drones.DroneIsTooFarFromOldTarget)
                        .ThenByDescending(l => ESCache.Instance.ActiveShip.HasSpeedMod && l.IsNPCFrigate && !l.IsAttacking && l.IsWarpScramblingMe)
                        .ThenByDescending(l => ESCache.Instance.ActiveShip.HasSpeedMod && l.IsNPCFrigate && l.IsWebbingMe && ESCache.Instance.ActiveShip.IsActiveTanked && AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.ToLower().Contains("chaotic".ToLower()))
                        .ThenByDescending(l => ESCache.Instance.ActiveShip.HasSpeedMod && l.IsNPCFrigate && l.IsWebbingMe && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => ESCache.Instance.ActiveShip.HasSpeedMod && l.IsNPCDestroyer && l.IsWebbingMe && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(i => i.Name.ToLower().Contains("drainer marshal disparu troop".ToLower()))//neuts?
                        .ThenByDescending(i => i.Name.ToLower().Contains("thunderchild".ToLower()))//first
                        .ThenByDescending(i => i.Name.ToLower().Contains("arrester marshal disparu troop".ToLower()))//neuts?
                        .ThenByDescending(l => l.IsNPCDestroyer && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && l.IsWebbingMe && Drones.DroneIsTooFarFromOldTarget)
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && l.IsWarpScramblingMe && Drones.DroneIsTooFarFromOldTarget)
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && !l.NpcHasALotOfRemoteRepair && Drones.DroneIsTooFarFromOldTarget) //kill things shooting drones!
                        .ThenByDescending(j => j.IsNPCCruiser && j.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked && !j.IsAttacking)
                        .ThenByDescending(j => j.IsNPCCruiser && j.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && l.IsWebbingMe && ESCache.Instance.ActiveShip.IsActiveTanked && AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.ToLower().Contains("chaotic".ToLower()))
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe && ESCache.Instance.ActiveShip.IsActiveTanked && AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.ToLower().Contains("chaotic".ToLower()))
                        .ThenByDescending(i => i.IsNPCCruiser && i.WarpScrambleChance >= 1 && !i.IsAttacking && Combat.PotentialCombatTargets.Any(x => x.Name.ToLower().Contains("karybdis tyrannos".ToLower())))
                        .ThenByDescending(i => i.IsNPCCruiser && i.WarpScrambleChance >= 1 && Combat.PotentialCombatTargets.Any(x => x.Name.ToLower().Contains("karybdis tyrannos".ToLower())))
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking) //kill things shooting drones!
                        .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers)
                        .ThenByDescending(j => j.IsNPCBattleship && j.IsWebbingMe)
                        .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasRemoteRepair)
                        .ThenByDescending(j => j.IsNPCBattleship && j.IsSensorDampeningMe)
                        .ThenByDescending(j => j.IsNPCBattleship && j.IsTargetPaintingMe)
                        .ThenByDescending(l => l.IsNPCBattleship && l.IsTrackingDisruptingMe && Combat.DoWeCurrentlyHaveTurretsMounted())
                        .ThenByDescending(j => j.IsNPCBattleship)
                        .ThenByDescending(l => l.IsNPCDestroyer && !l.IsAttacking)
                        .ThenByDescending(k => k.IsNPCDestroyer)
                        .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking) // dont kill all non-attacking frigates until we have dealt with BCs (see above)
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking) //kill things shooting drones!
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasNeutralizers)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWithinOptimalOfDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsCloseToDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasRemoteRepair)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsSensorDampeningMe)
                        .ThenByDescending(j => j.IsNPCCruiser && j.IsHighDps)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsTargetPaintingMe)
                        .ThenByDescending(i => i.IsNPCCruiser && i.IsTrackable && ESCache.Instance.ActiveShip.HasTurrets)
                        .ThenByDescending(i => i.IsNPCCruiser && i.IsInOptimalRange && ESCache.Instance.ActiveShip.HasTurrets)
                        .ThenByDescending(l => l.IsAbyssalDeadspaceTriglavianBioAdaptiveCache && ESCache.Instance.Weapons.Count == 0)
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsKillTarget && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsNPCCruiser)
                        .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsNPCFrigate && l.IsWithin10KOfOurDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsNPCFrigate && l.IsWithin15KOfOurDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsNPCFrigate && l.IsWithin20KOfOurDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsNPCFrigate && l.IsWithin25KOfOurDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsNPCFrigate && l.IsCloseToDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers)
                        .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasRemoteRepair)
                        .ThenByDescending(l => l.IsNPCFrigate && l.IsSensorDampeningMe)
                        .ThenByDescending(l => l.IsNPCFrigate && l.IsTargetPaintingMe)
                        .ThenByDescending(l => l.IsNPCFrigate && l.IsTrackingDisruptingMe && !l.IsKillTarget && Combat.DoWeCurrentlyHaveTurretsMounted())
                        .ThenByDescending(l => l.IsNPCFrigate && !l.IsKillTarget && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsNPCFrigate && !l.IsDroneKillTarget && IsPotentialCombatTargetForGuns)
                        .ThenByDescending(l => l.IsNPCFrigate)
                        .ThenByDescending(i => i.IsEntityIShouldKeepShooting)
                        .Take(ESCache.Instance.MaxLockedTargets ?? 2);
                }
            }

            if (IsPotentialCombatTargetForDrones)
            {
                if (tempListOfTargets != null && tempListOfTargets.Any())
                {
                    _abyssalPotentialCombatTargets_Drones = tempListOfTargets;
                    return _abyssalPotentialCombatTargets_Drones;
                }

                if (_abyssalPotentialCombatTargets_Drones != null && _abyssalPotentialCombatTargets_Drones.Any())
                    return _abyssalPotentialCombatTargets_Drones;

                return new List<EntityCache>();
            }

            if (IsPotentialCombatTargetForGuns)
            {
                if (tempListOfTargets != null && tempListOfTargets.Any())
                {
                    _abyssalPotentialCombatTargets_Guns = tempListOfTargets;
                    return _abyssalPotentialCombatTargets_Guns;
                }

                if (_abyssalPotentialCombatTargets_Guns != null && _abyssalPotentialCombatTargets_Guns.Any())
                    return _abyssalPotentialCombatTargets_Guns;

                return new List<EntityCache>();
            }

            if (tempListOfTargets != null && tempListOfTargets.Any())
            {
                _abyssalPotentialCombatTargets_Targets = tempListOfTargets;
                return _abyssalPotentialCombatTargets_Targets;
            }

            if (_abyssalPotentialCombatTargets_Targets != null && _abyssalPotentialCombatTargets_Targets.Any())
                return _abyssalPotentialCombatTargets_Targets;

            return new List<EntityCache>();
        }

        public static IEnumerable<EntityCache> AbyssalPotentialCombatTargets_EphialtesCruiserSpawn(bool IsPotentialCombatTargetForDrones = false, bool IsPotentialCombatTargetForGuns = false)
        {
            IEnumerable<EntityCache> tempListOfTargets = null;

            if (ShouldRefreshEntitiesForDrones(IsPotentialCombatTargetForDrones) || ShouldRefreshEntitiesForGuns(IsPotentialCombatTargetForGuns) || ShouldRefreshEntitiesForTargeting(IsPotentialCombatTargetForDrones, IsPotentialCombatTargetForGuns))
            {
                if (ESCache.Instance.MyShipEntity.IsFrigate)
                {
                    tempListOfTargets = AbyssalPotentialCombatTargets_FrigateAbyssal(IsPotentialCombatTargetForDrones, IsPotentialCombatTargetForGuns);
                }
                else
                {
                    tempListOfTargets = Combat.PotentialCombatTargets.Where(i => (IsPotentialCombatTargetForDrones && i.IsReadyForDronesToShoot) || (IsPotentialCombatTargetForGuns && i.IsReadyToShoot) || (!IsPotentialCombatTargetForDrones && !IsPotentialCombatTargetForGuns && i.IsReadyToTarget))
                        .OrderByDescending(i => !i.Name.ToLower().Contains("karybdis tyrannos".ToLower()))
                        .ThenByDescending(i => !i.Name.ToLower().Contains("lucid upholder".ToLower()))
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && l.IsWebbingMe && Time.ItHasBeenThisLongSinceWeStartedThePocket.TotalSeconds > 15)
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && l.IsWarpScramblingMe && Time.ItHasBeenThisLongSinceWeStartedThePocket.TotalSeconds > 15)

                        //Scrams
                        .ThenByDescending(l => l.IsWithin5KOfOurDrones && l.IsNPCCruiser && l.IsWarpScramblingMe)
                        .ThenByDescending(l => l.IsWithin10KOfOurDrones && l.IsNPCCruiser && l.IsWarpScramblingMe)
                        .ThenByDescending(l => l.IsWithin15KOfOurDrones && l.IsNPCCruiser && l.IsWarpScramblingMe)
                        .ThenByDescending(l => l.IsWithin20KOfOurDrones && l.IsNPCCruiser && l.IsWarpScramblingMe)
                        .ThenByDescending(l => l.IsWithin25KOfOurDrones && l.IsNPCCruiser && l.IsWarpScramblingMe)
                        .ThenByDescending(l => l.IsWithin30KOfOurDrones && l.IsNPCCruiser && l.IsWarpScramblingMe)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWarpScramblingMe)

                        .ThenByDescending(i => i.Name.ToLower().Contains("Ephialtes Spearfisher".ToLower()))

                        //Neut
                        .ThenByDescending(l => l.IsWithin5KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin10KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin15KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin20KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin25KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin30KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)

                        //
                        // webs
                        //
                        .ThenByDescending(l => l.IsWithin5KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin10KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin15KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin20KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin25KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin30KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsTrackingDisruptingMe && Combat.DoWeCurrentlyHaveTurretsMounted())
                        .ThenByDescending(l => l.IsWithin5KOfOurDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsWithin10KOfOurDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsWithin15KOfOurDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsWithin20KOfOurDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsWithin25KOfOurDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsWithin30KOfOurDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsNPCFrigate && IsPotentialCombatTargetForGuns)
                        .ThenByDescending(l => l.IsNPCDestroyer && IsPotentialCombatTargetForGuns)
                        .ThenByDescending(l => l.IsNPCCruiser && IsPotentialCombatTargetForGuns)
                        .ThenByDescending(l => l.IsNPCCruiser)
                        .ThenByDescending(l => l.IsNPCFrigate)
                        .Take(ESCache.Instance.MaxLockedTargets ?? 2);
                }
            }

            if (IsPotentialCombatTargetForDrones)
            {
                if (tempListOfTargets != null && tempListOfTargets.Any())
                {
                    _abyssalPotentialCombatTargets_Drones = tempListOfTargets;
                    return _abyssalPotentialCombatTargets_Drones;
                }

                if (_abyssalPotentialCombatTargets_Drones != null && _abyssalPotentialCombatTargets_Drones.Any())
                    return _abyssalPotentialCombatTargets_Drones;

                return new List<EntityCache>();
            }

            if (IsPotentialCombatTargetForGuns)
            {
                if (tempListOfTargets != null && tempListOfTargets.Any())
                {
                    _abyssalPotentialCombatTargets_Guns = tempListOfTargets;
                    return _abyssalPotentialCombatTargets_Guns;
                }

                if (_abyssalPotentialCombatTargets_Guns != null && _abyssalPotentialCombatTargets_Guns.Any())
                    return _abyssalPotentialCombatTargets_Guns;

                return new List<EntityCache>();
            }

            if (tempListOfTargets != null && tempListOfTargets.Any())
            {
                _abyssalPotentialCombatTargets_Targets = tempListOfTargets;
                return _abyssalPotentialCombatTargets_Targets;
            }

            if (_abyssalPotentialCombatTargets_Targets != null && _abyssalPotentialCombatTargets_Targets.Any())
                return _abyssalPotentialCombatTargets_Targets;

            return new List<EntityCache>();
        }

        public static IEnumerable<EntityCache> AbyssalPotentialCombatTargets_KarybdisTyrannosSpawn(bool IsPotentialCombatTargetForDrones = false, bool IsPotentialCombatTargetForGuns = false)
        {
            IEnumerable<EntityCache> tempListOfTargets = null;

            if (ShouldRefreshEntitiesForDrones(IsPotentialCombatTargetForDrones) || ShouldRefreshEntitiesForGuns(IsPotentialCombatTargetForGuns) || ShouldRefreshEntitiesForTargeting(IsPotentialCombatTargetForDrones, IsPotentialCombatTargetForGuns))
            {
                if (ESCache.Instance.MyShipEntity.IsFrigate)
                {
                    tempListOfTargets = AbyssalPotentialCombatTargets_FrigateAbyssal(IsPotentialCombatTargetForDrones, IsPotentialCombatTargetForGuns);
                }
                else
                {
                    tempListOfTargets = Combat.PotentialCombatTargets.Where(i => (IsPotentialCombatTargetForDrones && i.IsReadyForDronesToShoot) || (IsPotentialCombatTargetForGuns && i.IsReadyToShoot) || (!IsPotentialCombatTargetForDrones && !IsPotentialCombatTargetForGuns && i.IsReadyToTarget))
                        .OrderByDescending(i => !i.Name.ToLower().Contains("karybdis tyrannos".ToLower()))
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && l.IsWebbingMe && Time.ItHasBeenThisLongSinceWeStartedThePocket.TotalSeconds > 15)
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && l.IsWarpScramblingMe && Time.ItHasBeenThisLongSinceWeStartedThePocket.TotalSeconds > 15)

                        //Neut

                        .ThenByDescending(l => 100 > l.HealthPct && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin5KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin10KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin15KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin20KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin25KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin30KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)

                        //
                        // webs
                        //
                        .ThenByDescending(l => 100 > l.HealthPct && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin5KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin10KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin15KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin20KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin25KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin30KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)

                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe)

                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking)
                        //.ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers)
                        //.ThenByDescending(l => l.IsNPCFrigate && l.NpcHasRemoteRepair)
                        //.ThenByDescending(l => l.IsNPCFrigate && l.IsSensorDampeningMe)
                        //.ThenByDescending(l => l.IsNPCFrigate && l.IsTargetPaintingMe)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsTrackingDisruptingMe && Combat.DoWeCurrentlyHaveTurretsMounted())
                        //.ThenByDescending(l => l.IsNPCFrigate)
                        //.ThenByDescending(i => i.IsEntityIShouldKeepShooting);

                        .ThenByDescending(l => 100 > l.HealthPct && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin5KOfOurDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsWithin5KOfOurDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsWithin10KOfOurDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsWithin15KOfOurDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsWithin20KOfOurDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsWithin25KOfOurDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsWithin30KOfOurDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsNPCFrigate && IsPotentialCombatTargetForGuns)
                        .ThenByDescending(l => l.IsNPCDestroyer && IsPotentialCombatTargetForGuns)
                        .ThenByDescending(l => l.IsNPCCruiser && IsPotentialCombatTargetForGuns)
                        .ThenByDescending(l => l.IsNPCCruiser)
                        .ThenByDescending(l => l.IsNPCFrigate)
                        .Take(ESCache.Instance.MaxLockedTargets ?? 2);
                }
            }

            if (IsPotentialCombatTargetForDrones)
            {
                if (tempListOfTargets != null && tempListOfTargets.Any())
                {
                    _abyssalPotentialCombatTargets_Drones = tempListOfTargets;
                    return _abyssalPotentialCombatTargets_Drones;
                }

                if (_abyssalPotentialCombatTargets_Drones != null && _abyssalPotentialCombatTargets_Drones.Any())
                    return _abyssalPotentialCombatTargets_Drones;

                return new List<EntityCache>();
            }

            if (IsPotentialCombatTargetForGuns)
            {
                if (tempListOfTargets != null && tempListOfTargets.Any())
                {
                    _abyssalPotentialCombatTargets_Guns = tempListOfTargets;
                    return _abyssalPotentialCombatTargets_Guns;
                }

                if (_abyssalPotentialCombatTargets_Guns != null && _abyssalPotentialCombatTargets_Guns.Any())
                    return _abyssalPotentialCombatTargets_Guns;

                return new List<EntityCache>();
            }

            if (tempListOfTargets != null && tempListOfTargets.Any())
            {
                _abyssalPotentialCombatTargets_Targets = tempListOfTargets;
                return _abyssalPotentialCombatTargets_Targets;
            }

            if (_abyssalPotentialCombatTargets_Targets != null && _abyssalPotentialCombatTargets_Targets.Any())
                return _abyssalPotentialCombatTargets_Targets;

            return new List<EntityCache>();
        }

        public static IEnumerable<EntityCache> AbyssalPotentialCombatTargets_KikimoraDestroyerSpawn(bool IsPotentialCombatTargetForDrones = false, bool IsPotentialCombatTargetForGuns = false)
        {
            IEnumerable<EntityCache> tempListOfTargets = null;

            if (ShouldRefreshEntitiesForDrones(IsPotentialCombatTargetForDrones) || ShouldRefreshEntitiesForGuns(IsPotentialCombatTargetForGuns) || ShouldRefreshEntitiesForTargeting(IsPotentialCombatTargetForDrones, IsPotentialCombatTargetForGuns))
            {
                if (ESCache.Instance.MyShipEntity.IsFrigate)
                {
                    tempListOfTargets = AbyssalPotentialCombatTargets_FrigateAbyssal(IsPotentialCombatTargetForDrones, IsPotentialCombatTargetForGuns);
                }
                else
                {
                    tempListOfTargets = Combat.PotentialCombatTargets.Where(i => (IsPotentialCombatTargetForDrones && i.IsReadyForDronesToShoot) || (IsPotentialCombatTargetForGuns && i.IsReadyToShoot) || (!IsPotentialCombatTargetForDrones && !IsPotentialCombatTargetForGuns && i.IsReadyToTarget))
                        .OrderByDescending(l => l.IsNPCCruiser && !l.IsAttacking && l.IsWebbingMe && Time.ItHasBeenThisLongSinceWeStartedThePocket.TotalSeconds > 15)
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && l.IsWarpScramblingMe && Time.ItHasBeenThisLongSinceWeStartedThePocket.TotalSeconds > 15)

                        .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)

                        //Neut
                        .ThenByDescending(l => l.IsNPCDestroyer && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)

                        //
                        // webs
                        //
                        .ThenByDescending(l => l.IsNPCDestroyer && l.IsWebbingMe)

                        .ThenByDescending(l => l.IsNPCDestroyer && !l.IsAttacking)
                        .ThenByDescending(l => l.IsNPCDestroyer && l.IsTrackingDisruptingMe && Combat.DoWeCurrentlyHaveTurretsMounted())
                        .ThenByDescending(l => l.IsNPCDestroyer && l.IsWithin5KOfOurDrones)
                        .ThenByDescending(l => l.IsNPCDestroyer && l.IsWithin10KOfOurDrones)
                        .ThenByDescending(l => l.IsNPCDestroyer && l.IsWithin15KOfOurDrones)
                        .ThenByDescending(l => l.IsNPCDestroyer && l.IsWithin20KOfOurDrones)
                        .ThenByDescending(l => l.IsNPCDestroyer && l.IsWithin25KOfOurDrones)
                        .ThenByDescending(l => l.IsNPCDestroyer && l.IsWithin30KOfOurDrones)

                        .ThenByDescending(l => l.IsWithin10KOfOurDrones)
                        .ThenByDescending(l => l.IsWithin15KOfOurDrones)
                        .ThenByDescending(l => l.IsWithin20KOfOurDrones)
                        .ThenByDescending(l => l.IsWithin25KOfOurDrones)
                        .ThenByDescending(l => l.IsWithin30KOfOurDrones)
                        .Take(ESCache.Instance.MaxLockedTargets ?? 2);
                }
            }

            if (IsPotentialCombatTargetForDrones)
            {
                if (tempListOfTargets != null && tempListOfTargets.Any())
                {
                    _abyssalPotentialCombatTargets_Drones = tempListOfTargets;
                    return _abyssalPotentialCombatTargets_Drones;
                }

                if (_abyssalPotentialCombatTargets_Drones != null && _abyssalPotentialCombatTargets_Drones.Any())
                    return _abyssalPotentialCombatTargets_Drones;

                return new List<EntityCache>();
            }

            if (IsPotentialCombatTargetForGuns)
            {
                if (tempListOfTargets != null && tempListOfTargets.Any())
                {
                    _abyssalPotentialCombatTargets_Guns = tempListOfTargets;
                    return _abyssalPotentialCombatTargets_Guns;
                }

                if (_abyssalPotentialCombatTargets_Guns != null && _abyssalPotentialCombatTargets_Guns.Any())
                    return _abyssalPotentialCombatTargets_Guns;

                return new List<EntityCache>();
            }

            if (tempListOfTargets != null && tempListOfTargets.Any())
            {
                _abyssalPotentialCombatTargets_Targets = tempListOfTargets;
                return _abyssalPotentialCombatTargets_Targets;
            }

            if (_abyssalPotentialCombatTargets_Targets != null && _abyssalPotentialCombatTargets_Targets.Any())
                return _abyssalPotentialCombatTargets_Targets;

            return new List<EntityCache>();
        }

        public static IEnumerable<EntityCache> AbyssalPotentialCombatTargets_VedmakSpawn(bool IsPotentialCombatTargetForDrones = false, bool IsPotentialCombatTargetForGuns = false)
        {
            IEnumerable<EntityCache> tempListOfTargets = null;

            if (ShouldRefreshEntitiesForDrones(IsPotentialCombatTargetForDrones) || ShouldRefreshEntitiesForGuns(IsPotentialCombatTargetForGuns) || ShouldRefreshEntitiesForTargeting(IsPotentialCombatTargetForDrones, IsPotentialCombatTargetForGuns))
            {
                if (ESCache.Instance.MyShipEntity.IsFrigate)
                {
                    tempListOfTargets = AbyssalPotentialCombatTargets_FrigateAbyssal(IsPotentialCombatTargetForDrones, IsPotentialCombatTargetForGuns);
                }
                else
                {
                    tempListOfTargets = Combat.PotentialCombatTargets.Where(i => (IsPotentialCombatTargetForDrones && i.IsReadyForDronesToShoot) || (IsPotentialCombatTargetForGuns && i.IsReadyToShoot) || (!IsPotentialCombatTargetForDrones && !IsPotentialCombatTargetForGuns && i.IsReadyToTarget))
                        .OrderByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking && l.IsWebbingMe && Drones.DroneIsTooFarFromOldTarget)
                        .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking && l.IsWarpScramblingMe && Drones.DroneIsTooFarFromOldTarget)
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && l.IsWebbingMe && Time.ItHasBeenThisLongSinceWeStartedThePocket.TotalSeconds > 15)
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && l.IsWarpScramblingMe && Time.ItHasBeenThisLongSinceWeStartedThePocket.TotalSeconds > 15)

                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && l.IsWebbingMe && Drones.DroneIsTooFarFromOldTarget)
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && l.IsWarpScramblingMe && Drones.DroneIsTooFarFromOldTarget)
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && !l.NpcHasALotOfRemoteRepair && Drones.DroneIsTooFarFromOldTarget) //kill things shooting drones!
                        .ThenByDescending(j => j.IsNPCCruiser && j.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked && !j.IsAttacking)
                        .ThenByDescending(j => j.IsNPCCruiser && j.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsNPCCruiser && l.WeShouldFocusFire && !l.IsAttacking)
                        .ThenByDescending(l => l.IsNPCCruiser && l.WeShouldFocusFire)
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && l.IsWebbingMe && ESCache.Instance.ActiveShip.IsActiveTanked && AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.ToLower().Contains("chaotic".ToLower()))
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe && ESCache.Instance.ActiveShip.IsActiveTanked && AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.ToLower().Contains("chaotic".ToLower()))
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking) //kill things shooting drones!

                        .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe && ESCache.Instance.ActiveShip.IsActiveTanked && AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.ToLower().Contains("chaotic".ToLower()))
                        .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking) // dont kill all non-attacking frigates until we have dealt with BCs (see above)

                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking) //kill things shooting drones!
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasNeutralizers)
                        .ThenByDescending(l => l.IsNPCCruiser)
                        .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers)
                        .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasRemoteRepair)
                        .ThenByDescending(l => l.IsNPCFrigate && l.IsSensorDampeningMe)
                        .ThenByDescending(l => l.IsNPCFrigate && l.IsTargetPaintingMe)
                        .ThenByDescending(l => l.IsNPCFrigate && l.IsTrackingDisruptingMe && Combat.DoWeCurrentlyHaveTurretsMounted())
                        .ThenByDescending(l => l.IsNPCFrigate)
                        .ThenByDescending(i => i.IsEntityIShouldKeepShooting)
                        .Take(ESCache.Instance.MaxLockedTargets ?? 2);
                }
            }

            if (IsPotentialCombatTargetForDrones)
            {
                if (tempListOfTargets != null && tempListOfTargets.Any())
                {
                    _abyssalPotentialCombatTargets_Drones = tempListOfTargets;
                    return _abyssalPotentialCombatTargets_Drones;
                }

                if (_abyssalPotentialCombatTargets_Drones != null && _abyssalPotentialCombatTargets_Drones.Any())
                    return _abyssalPotentialCombatTargets_Drones;

                return new List<EntityCache>();
            }

            if (IsPotentialCombatTargetForGuns)
            {
                if (tempListOfTargets != null && tempListOfTargets.Any())
                {
                    _abyssalPotentialCombatTargets_Guns = tempListOfTargets;
                    return _abyssalPotentialCombatTargets_Guns;
                }

                if (_abyssalPotentialCombatTargets_Guns != null && _abyssalPotentialCombatTargets_Guns.Any())
                    return _abyssalPotentialCombatTargets_Guns;

                return new List<EntityCache>();
            }

            if (tempListOfTargets != null && tempListOfTargets.Any())
            {
                _abyssalPotentialCombatTargets_Targets = tempListOfTargets;
                return _abyssalPotentialCombatTargets_Targets;
            }

            if (_abyssalPotentialCombatTargets_Targets != null && _abyssalPotentialCombatTargets_Targets.Any())
                return _abyssalPotentialCombatTargets_Targets;

            return new List<EntityCache>();
        }

        public static IEnumerable<EntityCache> AbyssalPotentialCombatTargets_VedmakVilaCruiserSwarmerSpawn(bool IsPotentialCombatTargetForDrones = false, bool IsPotentialCombatTargetForGuns = false)
        {
            IEnumerable<EntityCache> tempListOfTargets = null;

            if (ShouldRefreshEntitiesForDrones(IsPotentialCombatTargetForDrones) || ShouldRefreshEntitiesForGuns(IsPotentialCombatTargetForGuns) || ShouldRefreshEntitiesForTargeting(IsPotentialCombatTargetForDrones, IsPotentialCombatTargetForGuns))
            {
                if (ESCache.Instance.MyShipEntity.IsFrigate)
                {
                    tempListOfTargets = AbyssalPotentialCombatTargets_FrigateAbyssal(IsPotentialCombatTargetForDrones, IsPotentialCombatTargetForGuns);
                }
                else
                {
                    tempListOfTargets = Combat.PotentialCombatTargets.Where(i => (IsPotentialCombatTargetForDrones && i.IsReadyForDronesToShoot) || (IsPotentialCombatTargetForGuns && i.IsReadyToShoot) || (!IsPotentialCombatTargetForDrones && !IsPotentialCombatTargetForGuns && i.IsReadyToTarget))
                        .OrderByDescending(k => k.IsAbyssalDeadspaceTriglavianBioAdaptiveCache)
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && l.IsWebbingMe && Time.ItHasBeenThisLongSinceWeStartedThePocket.TotalSeconds > 15)
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && l.IsWarpScramblingMe && Time.ItHasBeenThisLongSinceWeStartedThePocket.TotalSeconds > 15)

                        .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking && l.IsWebbingMe && Time.ItHasBeenThisLongSinceWeStartedThePocket.TotalSeconds > 15)
                        .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking && l.IsWarpScramblingMe && Time.ItHasBeenThisLongSinceWeStartedThePocket.TotalSeconds > 15)

                        .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsNPCFrigate && l.IsWarpScramblingMe)

                        //Neut
                        .ThenByDescending(l => l.IsWithin5KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin10KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin15KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin20KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin25KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin30KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)

                        //
                        // webs
                        //
                        .ThenByDescending(l => l.IsWithin5KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin10KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin15KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin20KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin25KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin30KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)

                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe)

                        //
                        // scrams
                        //
                        .ThenByDescending(l => l.IsWithin5KOfOurDrones && l.IsNPCCruiser && l.IsWarpScramblingMe)
                        .ThenByDescending(l => l.IsWithin10KOfOurDrones && l.IsNPCCruiser && l.IsWarpScramblingMe)
                        .ThenByDescending(l => l.IsWithin15KOfOurDrones && l.IsNPCCruiser && l.IsWarpScramblingMe)
                        .ThenByDescending(l => l.IsWithin20KOfOurDrones && l.IsNPCCruiser && l.IsWarpScramblingMe)
                        .ThenByDescending(l => l.IsWithin25KOfOurDrones && l.IsNPCCruiser && l.IsWarpScramblingMe)
                        .ThenByDescending(l => l.IsWithin30KOfOurDrones && l.IsNPCCruiser && l.IsWarpScramblingMe)

                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWarpScramblingMe)

                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking)
                        //.ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers)
                        //.ThenByDescending(l => l.IsNPCFrigate && l.NpcHasRemoteRepair)
                        //.ThenByDescending(l => l.IsNPCFrigate && l.IsSensorDampeningMe)
                        //.ThenByDescending(l => l.IsNPCFrigate && l.IsTargetPaintingMe)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsTrackingDisruptingMe && Combat.DoWeCurrentlyHaveTurretsMounted())
                        //.ThenByDescending(l => l.IsNPCFrigate)
                        //.ThenByDescending(i => i.IsEntityIShouldKeepShooting);
                        .ThenByDescending(l => l.IsWithin5KOfOurDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsWithin10KOfOurDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsWithin15KOfOurDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsWithin20KOfOurDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsWithin25KOfOurDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsWithin30KOfOurDrones && IsPotentialCombatTargetForDrones)
                        .ThenByDescending(l => l.IsNPCFrigate && IsPotentialCombatTargetForGuns)
                        .ThenByDescending(l => l.IsNPCDestroyer && IsPotentialCombatTargetForGuns)
                        .ThenByDescending(l => l.IsNPCCruiser && IsPotentialCombatTargetForGuns)
                        .ThenByDescending(l => l.IsNPCCruiser)
                        .ThenByDescending(l => l.IsNPCFrigate)
                        .Take(ESCache.Instance.MaxLockedTargets ?? 2);
                }
            }

            if (IsPotentialCombatTargetForDrones)
            {
                if (tempListOfTargets != null && tempListOfTargets.Any())
                {
                    _abyssalPotentialCombatTargets_Drones = tempListOfTargets;
                    return _abyssalPotentialCombatTargets_Drones;
                }

                if (_abyssalPotentialCombatTargets_Drones != null && _abyssalPotentialCombatTargets_Drones.Any())
                    return _abyssalPotentialCombatTargets_Drones;

                return new List<EntityCache>();
            }

            if (IsPotentialCombatTargetForGuns)
            {
                if (tempListOfTargets != null && tempListOfTargets.Any())
                {
                    _abyssalPotentialCombatTargets_Guns = tempListOfTargets;
                    return _abyssalPotentialCombatTargets_Guns;
                }

                if (_abyssalPotentialCombatTargets_Guns != null && _abyssalPotentialCombatTargets_Guns.Any())
                    return _abyssalPotentialCombatTargets_Guns;

                return new List<EntityCache>();
            }

            if (tempListOfTargets != null && tempListOfTargets.Any())
            {
                _abyssalPotentialCombatTargets_Targets = tempListOfTargets;
                return _abyssalPotentialCombatTargets_Targets;
            }

            if (_abyssalPotentialCombatTargets_Targets != null && _abyssalPotentialCombatTargets_Targets.Any())
                return _abyssalPotentialCombatTargets_Targets;

            return new List<EntityCache>();
        }

        public static IEnumerable<EntityCache> AbyssalPotentialCombatTargets_DevotedCruiserSpawn(bool IsPotentialCombatTargetForDrones = false, bool IsPotentialCombatTargetForGuns = false)
        {
            IEnumerable<EntityCache> tempListOfTargets = null;

            if (ShouldRefreshEntitiesForDrones(IsPotentialCombatTargetForDrones) || ShouldRefreshEntitiesForGuns(IsPotentialCombatTargetForGuns) || ShouldRefreshEntitiesForTargeting(IsPotentialCombatTargetForDrones, IsPotentialCombatTargetForGuns))
            {
                if (ESCache.Instance.MyShipEntity.IsFrigate)
                {
                    tempListOfTargets = AbyssalPotentialCombatTargets_FrigateAbyssal(IsPotentialCombatTargetForDrones, IsPotentialCombatTargetForGuns);
                }
                else
                {
                    if (DebugConfig.DebugTargetCombatants)
                    {
                        Log.WriteLine("--- MaxLockedTargets [" + ESCache.Instance.MaxLockedTargets + "] MaxRange [" + Combat.MaxRange + "] ---");
                    }

                    tempListOfTargets = Combat.PotentialCombatTargets.Where(i => (IsPotentialCombatTargetForDrones && i.IsReadyForDronesToShoot) || (IsPotentialCombatTargetForGuns && i.IsReadyToShoot) || (!IsPotentialCombatTargetForDrones && !IsPotentialCombatTargetForGuns && i.IsReadyToTarget))
                        .OrderByDescending(i => !i.Name.ToLower().Contains("karybdis tyrannos".ToLower()))
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && l.IsWebbingMe && Time.ItHasBeenThisLongSinceWeStartedThePocket.TotalSeconds > 15)
                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking && l.IsWarpScramblingMe && Time.ItHasBeenThisLongSinceWeStartedThePocket.TotalSeconds > 15)

                        //Neut

                        .ThenByDescending(l => l.IsWithin5KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin10KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin15KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin20KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin25KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin30KOfOurDrones && l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)

                        // Neuting Frigs should be the only neuting things left
                        .ThenByDescending(l => l.IsWithin5KOfOurDrones && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin10KOfOurDrones && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin15KOfOurDrones && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin20KOfOurDrones && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin25KOfOurDrones && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsWithin30KOfOurDrones && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)
                        .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasNeutralizers && ESCache.Instance.ActiveShip.IsActiveTanked)

                        //
                        // webs
                        //
                        .ThenByDescending(l => l.IsWithin5KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin10KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin15KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin20KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin25KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin30KOfOurDrones && l.IsNPCCruiser && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe)

                        .ThenByDescending(l => l.IsWithin5KOfOurDrones && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin10KOfOurDrones && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin15KOfOurDrones && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin20KOfOurDrones && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin25KOfOurDrones && l.IsWebbingMe)
                        .ThenByDescending(l => l.IsWithin30KOfOurDrones && l.IsWebbingMe)

                        .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking)
                        //.ThenByDescending(l => l.IsNPCFrigate && l.NpcHasNeutralizers)
                        //.ThenByDescending(l => l.IsNPCFrigate && l.NpcHasRemoteRepair)
                        //.ThenByDescending(l => l.IsNPCFrigate && l.IsSensorDampeningMe)
                        //.ThenByDescending(l => l.IsNPCFrigate && l.IsTargetPaintingMe)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsTrackingDisruptingMe && Combat.DoWeCurrentlyHaveTurretsMounted())
                        //.ThenByDescending(l => l.IsNPCFrigate)
                        //.ThenByDescending(i => i.IsEntityIShouldKeepShooting);

                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWithin5KOfOurDrones)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWithin10KOfOurDrones)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWithin15KOfOurDrones)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWithin20KOfOurDrones)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWithin25KOfOurDrones)
                        .ThenByDescending(l => l.IsNPCCruiser && l.IsWithin30KOfOurDrones)

                        .ThenByDescending(l => !l.IsAttacking && l.IsWebbingMe) //this is here for targeting mainly, so that we will target cruisers first (above this) then these yellow boxing NPCs
                        .ThenByDescending(l => !l.IsAttacking && l.IsWarpScramblingMe) //this is here for targeting mainly, so that we will target cruisers first (above this) then these yellow boxing NPCs

                        .ThenByDescending(l => l.IsWithin5KOfOurDrones)
                        .ThenByDescending(l => l.IsWithin10KOfOurDrones)
                        .ThenByDescending(l => l.IsWithin15KOfOurDrones)
                        .ThenByDescending(l => l.IsWithin20KOfOurDrones)
                        .ThenByDescending(l => l.IsWithin25KOfOurDrones)
                        .ThenByDescending(l => l.IsWithin30KOfOurDrones)
                        .Take(ESCache.Instance.MaxLockedTargets ?? 2);
                }
            }

            if (DebugConfig.DebugTargetCombatants)
            {
                if (tempListOfTargets != null && tempListOfTargets.Any())
                {
                    int intNum = 0;
                    foreach (var tempTarget in tempListOfTargets)
                    {
                        intNum++;
                        Log.WriteLine("[" + intNum + "][" + tempTarget.Name + "] at [" + tempTarget.Nearest1KDistance + "k] ID [" + tempTarget.MaskedId + "]");
                    }
                }
            }

            if (IsPotentialCombatTargetForDrones)
            {
                if (tempListOfTargets != null && tempListOfTargets.Any())
                {
                    _abyssalPotentialCombatTargets_Drones = tempListOfTargets;
                    return _abyssalPotentialCombatTargets_Drones;
                }

                if (_abyssalPotentialCombatTargets_Drones != null && _abyssalPotentialCombatTargets_Drones.Any())
                    return _abyssalPotentialCombatTargets_Drones;

                return new List<EntityCache>();
            }

            if (IsPotentialCombatTargetForGuns)
            {
                if (tempListOfTargets != null && tempListOfTargets.Any())
                {
                    _abyssalPotentialCombatTargets_Guns = tempListOfTargets;
                    return _abyssalPotentialCombatTargets_Guns;
                }

                if (_abyssalPotentialCombatTargets_Guns != null && _abyssalPotentialCombatTargets_Guns.Any())
                    return _abyssalPotentialCombatTargets_Guns;

                return new List<EntityCache>();
            }

            if (tempListOfTargets != null && tempListOfTargets.Any())
            {
                _abyssalPotentialCombatTargets_Targets = tempListOfTargets;
                return _abyssalPotentialCombatTargets_Targets;
            }

            if (_abyssalPotentialCombatTargets_Targets != null && _abyssalPotentialCombatTargets_Targets.Any())
                return _abyssalPotentialCombatTargets_Targets;

            return new List<EntityCache>();
        }

        public static IEnumerable<EntityCache> AbyssalPotentialCombatTargets_HighAngleBattlecruiserSpawn(bool IsPotentialCombatTargetForDrones = false, bool IsPotentialCombatTargetForGuns = false)
        {
            IEnumerable<EntityCache> tempListOfTargets = null;

            if (ShouldRefreshEntitiesForDrones(IsPotentialCombatTargetForDrones) || ShouldRefreshEntitiesForGuns(IsPotentialCombatTargetForGuns) || ShouldRefreshEntitiesForTargeting(IsPotentialCombatTargetForDrones, IsPotentialCombatTargetForGuns))
            {
                if (ESCache.Instance.MyShipEntity.IsFrigate)
                {
                    tempListOfTargets = AbyssalPotentialCombatTargets_FrigateAbyssal(IsPotentialCombatTargetForDrones, IsPotentialCombatTargetForGuns);
                }
                else
                {
                    tempListOfTargets = Combat.PotentialCombatTargets.Where(i => (IsPotentialCombatTargetForDrones && i.IsReadyForDronesToShoot) || (IsPotentialCombatTargetForGuns && i.IsReadyToShoot) || (!IsPotentialCombatTargetForDrones && !IsPotentialCombatTargetForGuns && i.IsReadyToTarget))
                        .OrderByDescending(i => !i.Name.ToLower().Contains("karybdis tyrannos".ToLower()))
                        .ThenByDescending(l => !l.IsAttacking && l.IsWebbingMe && 100 > l.HealthPct)
                        .ThenByDescending(l => !l.IsAttacking && l.IsWebbingMe && Time.ItHasBeenThisLongSinceWeStartedThePocket.TotalSeconds > 15)
                        .ThenByDescending(l => !l.IsAttacking && l.IsWarpScramblingMe && 100 > l.HealthPct)
                        .ThenByDescending(l => !l.IsAttacking && l.IsWarpScramblingMe && Time.ItHasBeenThisLongSinceWeStartedThePocket.TotalSeconds > 15)

                        .ThenByDescending(l => l.Name.Contains("Sparkgrip") && 50 > l.HealthPct) // EM
                        .ThenByDescending(l => l.Name.Contains("Sparkgrip") && 100 > l.HealthPct) // EM
                        .ThenByDescending(l => l.Name.Contains("Sparkgrip")) // EM

                        .ThenByDescending(l => l.Name.Contains("Strikegrip") && 50 > l.HealthPct) // KIN
                        .ThenByDescending(l => l.Name.Contains("Strikegrip") && 100 > l.HealthPct) // KIN
                        .ThenByDescending(l => l.Name.Contains("Strikegrip")) // KIN

                        .ThenByDescending(l => l.Name.Contains("Blastgrip") && 50 > l.HealthPct) // EXP
                        .ThenByDescending(l => l.Name.Contains("Blastgrip") && 100 > l.HealthPct) // EXP
                        .ThenByDescending(l => l.Name.Contains("Blastgrip")) // EXP

                        .ThenByDescending(l => l.Name.Contains("Embergriprip") && 50 > l.HealthPct) //Therm
                        .ThenByDescending(l => l.Name.Contains("Embergriprip") && 100 > l.HealthPct) //Therm
                        .ThenByDescending(l => l.Name.Contains("Embergriprip")) //Therm

                        .ThenByDescending(l => l.IsNPCBattlecruiser && 100 > l.HealthPct)
                        .ThenByDescending(l => l.IsNPCBattlecruiser)

                        .ThenByDescending(l => l.IsNPCCruiser && 100 > l.HealthPct)
                        .ThenByDescending(l => l.IsNPCCruiser)
                        .ThenByDescending(l => l.IsNPCFrigate && 100 > l.HealthPct)
                        .ThenByDescending(l => l.IsNPCFrigate)
                        .Take(ESCache.Instance.MaxLockedTargets ?? 2);
                }
            }

            if (IsPotentialCombatTargetForDrones)
            {
                if (tempListOfTargets != null && tempListOfTargets.Any())
                {
                    _abyssalPotentialCombatTargets_Drones = tempListOfTargets;
                    return _abyssalPotentialCombatTargets_Drones;
                }

                if (_abyssalPotentialCombatTargets_Drones != null && _abyssalPotentialCombatTargets_Drones.Any())
                    return _abyssalPotentialCombatTargets_Drones;

                return new List<EntityCache>();
            }

            if (IsPotentialCombatTargetForGuns)
            {
                if (tempListOfTargets != null && tempListOfTargets.Any())
                {
                    _abyssalPotentialCombatTargets_Guns = tempListOfTargets;
                    return _abyssalPotentialCombatTargets_Guns;
                }

                if (_abyssalPotentialCombatTargets_Guns != null && _abyssalPotentialCombatTargets_Guns.Any())
                    return _abyssalPotentialCombatTargets_Guns;

                return new List<EntityCache>();
            }

            if (tempListOfTargets != null && tempListOfTargets.Any())
            {
                _abyssalPotentialCombatTargets_Targets = tempListOfTargets;
                return _abyssalPotentialCombatTargets_Targets;
            }

            if (_abyssalPotentialCombatTargets_Targets != null && _abyssalPotentialCombatTargets_Targets.Any())
                return _abyssalPotentialCombatTargets_Targets;

            return new List<EntityCache>();
        }

        private static IEnumerable<EntityCache> AbyssalPotentialCombatTargets_FrigateAbyssal(bool IsPotentialCombatTargetForDrones = false, bool IsPotentialCombatTargetForGuns = false)
        {
            IEnumerable<EntityCache> tempListOfTargets = new List<EntityCache>();

            if (ShouldRefreshEntitiesForDrones(IsPotentialCombatTargetForDrones) || ShouldRefreshEntitiesForGuns(IsPotentialCombatTargetForGuns) || ShouldRefreshEntitiesForTargeting(IsPotentialCombatTargetForDrones, IsPotentialCombatTargetForGuns))
            {
                tempListOfTargets = Combat.PotentialCombatTargets.Where(i => (IsPotentialCombatTargetForDrones && i.IsReadyForDronesToShoot) || (IsPotentialCombatTargetForGuns && i.IsReadyToShoot) || (!IsPotentialCombatTargetForDrones && !IsPotentialCombatTargetForGuns && i.IsReadyToTarget))
                    .OrderByDescending(k => k.IsAbyssalDeadspaceTriglavianBioAdaptiveCache)
                    .ThenByDescending(l => l.IsNPCFrigate && l.IsWebbingMe)
                    .ThenByDescending(l => l.IsNPCFrigate && !l.IsAttacking)
                    .ThenByDescending(j => j.IsNPCFrigate && j.TriglavianDamage != null && j.TriglavianDamage > 0 && j.NpcHasRemoteRepair)
                    .ThenByDescending(j => j.IsNPCFrigate && j.TriglavianDamage != null && j.TriglavianDamage > 0)
                    .ThenByDescending(l => l.IsNPCFrigate && l.NpcHasRemoteRepair)
                    .ThenByDescending(l => l.IsNPCFrigate && l.IsSensorDampeningMe)
                    .ThenByDescending(l => l.IsNPCFrigate && l.IsTargetPaintingMe)
                    .ThenByDescending(j => j.IsNPCFrigate && j.IsHighDps)
                    .ThenByDescending(i => i.IsNPCFrigate && i.IsTrackable)
                    .ThenByDescending(i => i.IsNPCFrigate && i.IsInOptimalRange)
                    .ThenByDescending(j => j.IsNPCCruiser && j.WeShouldFocusFire)
                    .ThenByDescending(l => l.IsNPCCruiser && !l.IsAttacking)
                    .ThenByDescending(l => l.IsNPCCruiser && l.IsWebbingMe)
                    .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasNeutralizers)
                    .ThenByDescending(l => l.IsNPCCruiser && l.NpcHasRemoteRepair)
                    .ThenByDescending(l => l.IsNPCCruiser && l.IsSensorDampeningMe)
                    .ThenByDescending(l => l.IsNPCCruiser && l.IsTargetPaintingMe)
                    .ThenByDescending(l => l.IsNPCCruiser && l.IsTrackingDisruptingMe && Combat.DoWeCurrentlyHaveTurretsMounted())
                    .ThenByDescending(l => l.IsNPCCruiser)
                    .ThenByDescending(j => j.IsNPCCruiser && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule) && !j.IsAttacking)
                    .ThenByDescending(j => j.IsNPCCruiser && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                    .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 0 && !j.IsAttacking)
                    .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 0 && j.NpcHasRemoteRepair)
                    .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 0)
                    .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 400 && j.NpcHasRemoteRepair)
                    .ThenByDescending(j => j.IsNPCCruiser && j.TriglavianDamage != null && j.TriglavianDamage > 400)
                    .ThenByDescending(n => n.IsNeutralizingMe && ESCache.Instance.Modules.Any(module => module.IsShieldRepairModule || module.IsArmorRepairModule))
                    //.ThenByDescending(k => k.IsAbyssalDeadspaceTriglavianExtractionNode)
                    .ThenByDescending(l => l.IsNPCCruiser && l.IsWithinOptimalOfDrones)
                    .ThenByDescending(l => l.IsNPCCruiser && l.IsCloseToDrones)
                    .ThenByDescending(l => l.IsNPCBattlecruiser && !l.IsAttacking)
                    .ThenByDescending(k => k.IsNPCBattlecruiser)
                    .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                    .ThenByDescending(l => l.IsNPCBattleship && l.IsWithinOptimalOfDrones)
                    .ThenByDescending(l => l.IsNPCBattleship && l.IsCloseToDrones)
                    .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasRemoteRepair)
                    .ThenByDescending(j => j.IsNPCBattleship && j.IsSensorDampeningMe)
                    .ThenByDescending(j => j.IsNPCBattleship && j.IsWebbingMe)
                    .ThenByDescending(j => j.IsNPCBattleship && j.NpcHasNeutralizers)
                    .ThenByDescending(j => j.IsNPCBattleship && j.IsTargetPaintingMe)
                    .ThenByDescending(l => l.IsNPCBattleship && l.IsTrackingDisruptingMe && Combat.DoWeCurrentlyHaveTurretsMounted())
                    .ThenByDescending(j => j.IsNPCBattleship)
                    .ThenBy(p => p.StructurePct)
                    .ThenBy(q => q.ArmorPct)
                    .ThenBy(r => r.ShieldPct)
                    .Take(ESCache.Instance.MaxLockedTargets ?? 2);
            }

            if (IsPotentialCombatTargetForDrones)
            {
                if (tempListOfTargets != null && tempListOfTargets.Any())
                {
                    _abyssalPotentialCombatTargets_Drones = tempListOfTargets;
                    return _abyssalPotentialCombatTargets_Drones;
                }

                if (_abyssalPotentialCombatTargets_Drones != null && _abyssalPotentialCombatTargets_Drones.Any())
                    return _abyssalPotentialCombatTargets_Drones;

                return new List<EntityCache>();
            }

            if (IsPotentialCombatTargetForGuns)
            {
                if (tempListOfTargets != null && tempListOfTargets.Any())
                {
                    _abyssalPotentialCombatTargets_Guns = tempListOfTargets;
                    return _abyssalPotentialCombatTargets_Guns;
                }

                if (_abyssalPotentialCombatTargets_Guns != null && _abyssalPotentialCombatTargets_Guns.Any())
                    return _abyssalPotentialCombatTargets_Guns;

                return new List<EntityCache>();
            }

            if (tempListOfTargets != null && tempListOfTargets.Any())
            {
                _abyssalPotentialCombatTargets_Targets = tempListOfTargets;
                return _abyssalPotentialCombatTargets_Targets;
            }

            if (_abyssalPotentialCombatTargets_Targets != null && _abyssalPotentialCombatTargets_Targets.Any())
                return _abyssalPotentialCombatTargets_Targets;

            return new List<EntityCache>();

        }
    }
}