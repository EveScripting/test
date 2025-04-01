extern alias SC;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Actions;
using EVESharpCore.Questor.Activities;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Questor.States;
using EVESharpCore.Questor.Storylines;
using SC::SharedComponents.EVE;
using SC::SharedComponents.EVE.ClientSettings;
using SC::SharedComponents.IPC;

namespace EVESharpCore.Cache
{
    public static class MissionSettings
    {
        #region Constructors

        static MissionSettings()
        {
            StorylineInstance = new Storyline();
            ChangeMissionShipFittings = false;
            DefaultFittingName = null;
            FactionBlacklist = new List<string>();
            ListOfAgents = new List<AgentsList>();
            ListofFactionFittings = new List<FactionFitting>();
            _listOfMissionFittings = new List<MissionFitting>();
            MissionBlacklist = new List<string>();
            MissionObjectivePhraseBlacklist = new List<string>();
            MissionGreylist = new List<string>();
            MissionItems = new List<string>();
            MissionUseDrones = null;
            _currentBestDamageTypes = new List<DamageType>();
            UseMissionShip = false;
        }

        #endregion Constructors

        #region Fields

        public static List<long> AgentBlacklist;
        public static long IskReward = 0;
        public static string LastReasonMissionAttemptedToBeDeclined = string.Empty;
        public static HashSet<long> MissionBoosterTypes = new HashSet<long>();
        public static bool? MissionDronesKillHighValueTargets;
        public static double? MissionOptimalRange;
        public static double? MissionOrbitDistance;
        public static int MissionsThisSession = 0;
        public static Storyline StorylineInstance;
        public static XDocument UnloadLootTheseItemsAreLootItems;
        public static bool WaitDecline = false;
        private static readonly List<MissionFitting> _listOfMissionFittings;
        public static int MissionCompletionErrors = 0;
        private static DirectAgent _agent { get; set; }

        private static string _agentName = string.Empty;

        private static List<DamageType> _currentBestDamageTypes;
        private static FactionFitting _defaultFitting;
        private static string _defaultFittingName;
        private static string _factionFittingNameForThisMissionsFaction;
        private static string _fittingToLoad;
        private static List<FactionFitting> _listofFactionFittings;
        private static IEnumerable<DirectAgentMission> _missionsInJournalFromNotBlacklistedAgents;
        private static string _missionXmlPath { get; set; }
        private static string _siteXmlPath { get; set; }
    private static List<DirectItem> _modulesInAllInGameFittings;
        private static DateTime _nextSecondBestDamageTypeSwap = DateTime.MinValue;
        private static DateTime _nextThirdBestDamageTypeSwap = DateTime.MinValue;
        private static List<DirectAgentMission> _storylineMissionsInJournal;
        private static IEnumerable<DirectAgentMission> _storylineMissionsInJournalThatQuestorKnowsHowToDo;
        private static IEnumerable<DirectAgentMission> _storylineMissionsInJournalThatQuestorKnowsHowToDoNotBlacklisted;
        public static XDocument MissionXml;

        #endregion Fields

        #region Properties

        public static void ClearDamageTypeCache()
        {
            _currentDamageType = null;
            _currentBestDamageTypes = null;
            _nextSecondBestDamageTypeSwap = DateTime.MinValue;
            _nextThirdBestDamageTypeSwap = DateTime.MinValue;
            _previousDamageType = null;
        }

        private static DamageType? _previousDamageType;

        private static DateTime LastStoryLineMissionLogging = DateTime.UtcNow.AddHours(-1);

        private static DirectAgentMission regularMission;

        private static bool StorylineMissionsLogged;

        public static DirectAgentMission _storylineMission { get; private set; }

        private static DirectAgent _agentToPullNextRegularMissionFrom = null;

        public static DirectAgent AgentToPullNextRegularMissionFrom
        {
            get
            {
                try
                {
                    if (_agentToPullNextRegularMissionFrom != null)
                        return _agentToPullNextRegularMissionFrom ?? null;

                    if (Settings.CharacterXmlExists)
                    {
                        try
                        {
                            if (ESCache.Instance.SelectedController.Contains("Abyssal"))
                            {
                                return null;
                            }

                            if (!SelectedControllerUsesCombatMissionsBehavior && ESCache.Instance.DirectEve.AgentMissions.Count > 0)
                            {
                                return null;
                            }

                            if (_agent == null)
                            {
                                Log.WriteLine("if (_agent == null)");
                                //
                                // use strCurrentAgentName to find the agent
                                //
                                if (string.IsNullOrEmpty(StrCurrentAgentName))
                                {
                                    Log.WriteLine("StrCurrentAgentName is null or empty");
                                }

                                if (string.IsNullOrEmpty(_agentName))
                                {
                                    Log.WriteLine("_agentName is null or empty");
                                }

                                if (string.IsNullOrEmpty(_agentName) || !StrCurrentAgentName.Equals(_agentName))
                                {
                                    _agent = ESCache.Instance.DirectEve.GetAgentByName(StrCurrentAgentName);
                                    if (_agent == null)
                                        Log.WriteLine("Agent == null, strCurrentAgentName was set to [ " + StrCurrentAgentName + " ] couldnt find this agent");

                                    if (ESCache.Instance.SelectedController.Contains("CareerAgentController"))
                                        PickAgentToUseNext();
                                }

                                //
                                // use _agentName to find the agent
                                //
                                if (_agent == null && StrCurrentAgentName.Equals(_agentName))
                                    _agent = ESCache.Instance.DirectEve.GetAgentByName(_agentName);
                            }

                            if (_agent != null && !StrCurrentAgentName.Equals(_agentName))
                            {
                                Log.WriteLine("New AgentToPullNextRegularMissionFrom [" + _agent.Name + "]");
                                _agentName = StrCurrentAgentName;
                            }

                            //todo, fixme
                            if (_agent == null)
                            {
                                //...
                            }

                            _agentToPullNextRegularMissionFrom = _agent;
                            return _agentToPullNextRegularMissionFrom;
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine("Unable to process agent section of [" + Settings.CharacterSettingsPath +
                                          "] make sure you have a valid agent listed! Pausing so you can fix it.");
                            Log.WriteLine("Exception [" + ex + "]");
                            ControllerManager.Instance.SetPause(true);
                        }
                    }
                    else
                    {
                        Log.WriteLine("if (!Settings.Instance.CharacterXMLExists)");
                    }

                    return null;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return null;
                }
            }
            set => _agent = value;
        }

        public static bool AllowNonStorylineCourierMissionsInLowSec { get; set; }

        public static bool AllowRemovingNoCompatibleStorylines { get; set; }

        public static bool ChangeMissionShipFittings { get; set; }

        private static bool AllowDamageTypeChanges
        {
            get
            {
                if (ESCache.Instance.InMission || ESCache.Instance.InWormHoleSpace)
                {
                    if ((ESCache.Instance.Weapons.Count > 0 && ESCache.Instance.Weapons.Any(i => i.GroupId == (int)Group.RapidHeavyMissileLaunchers)) ||
                        ESCache.Instance.Weapons.Any(i => i.GroupId == (int) Group.RapidLightMissileLaunchers))
                    {
                        //
                        // long reload times. wait to reload until the launchers are low on ammo already
                        // this probably works best with launchers in stack(s)
                        //
                        if (ESCache.Instance.Weapons.Count > 0 && ESCache.Instance.Weapons.All(i => i.ChargeQty > 7 && !i.IsReloadingAmmo))
                            return false;
                    }

                    return true;
                }

                return false;
            }
        }

        private static bool IsBigEnoughTargetWeShouldSwapNow
        {
            get
            {
                if (Combat._pickPrimaryWeaponTarget != null && (Combat._pickPrimaryWeaponTarget.IsBattleship || Combat._pickPrimaryWeaponTarget.IsNPCBattleship))
                {
                    if (ESCache.Instance.Weapons.Count > 0)
                    {
                        if (ESCache.Instance.Weapons.All(i => i.GroupId != (int)Group.RapidHeavyMissileLaunchers && i.GroupId != (int)Group.RapidLightMissileLaunchers))
                            return false;
                    }
                    return true;
                }

                return false;
            }
        }

        private static DamageType? _currentDamageType { get; set; }

        public static DamageType CurrentDamageType
        {
            get
            {
                try
                {
                    //if (ESCache.Instance.InWormHoleSpace)
                    //    return DefaultDamageType;

                    if (ESCache.Instance.InWormHoleSpace)
                    {
                        if (ESCache.Instance.Targets.Any(i => i._directEntity.IsNPCWormHoleSpaceDrifter))
                        {
                            if (1 > ESCache.Instance.Targets.FirstOrDefault(i => i._directEntity.IsNPCWormHoleSpaceDrifter).ArmorPct)
                            {
                                //shoot any damage type that will hit the drifter: most damage please!
                                _currentDamageType = DamageType.EM;
                                return _currentDamageType ?? DamageType.EM;
                            }

                            _currentDamageType = DamageType.Explosive;
                            return _currentDamageType ?? DamageType.Explosive;
                        }
                    }

                    if (DirectEve.HasFrameChanged(nameof(CurrentDamageType)) || !_currentDamageType.HasValue)
                    {
                        _currentBestDamageTypes = GetBestDamageTypes;
                        var bestDamageType = _currentBestDamageTypes.FirstOrDefault();

                        if (bestDamageType != _previousDamageType)
                        {
                            if (AllowDamageTypeChanges && _previousDamageType.HasValue && _currentBestDamageTypes.Count > 0)
                            {
                                // get the index of the previous damage type to choose when to swap the damage type
                                int indexPreviousDamageType = _currentBestDamageTypes.IndexOf(_previousDamageType.Value);
                                switch (indexPreviousDamageType)
                                {
                                    case 0: // don't swap
                                        if (DebugConfig.DebugCurrentDamageType) Log.WriteLine($"Index 0. (this case shouldn't happen at all)");
                                        break;

                                    case 1: // swap every 2 minutes?
                                        if (DebugConfig.DebugCurrentDamageType) Log.WriteLine($"Index 1. (2nd best)");
                                        if (_nextSecondBestDamageTypeSwap < DateTime.UtcNow)
                                        {
                                            //if (DebugConfig.DebugCurrentDamageType)
                                            if (ESCache.Instance.Weapons.Any(i => i.GroupId == (int)Group.RapidHeavyMissileLaunchers || i.GroupId == (int)Group.RapidLightMissileLaunchers))
                                            {
                                                _nextSecondBestDamageTypeSwap = DateTime.UtcNow.AddSeconds(360);
                                                Log.WriteLine("[bestDamageType][1] [" + bestDamageType + "] next 2nd best swap will in 6 minutes.");
                                            }
                                            else
                                            {
                                                _nextSecondBestDamageTypeSwap = DateTime.UtcNow.AddSeconds(120);
                                                Log.WriteLine("[bestDamageType][1] [" + bestDamageType + "] next 2nd best swap will in 2 minutes.");
                                            }
                                        }
                                        else
                                        {
                                            bestDamageType = _previousDamageType.Value;
                                            if (DebugConfig.DebugCurrentDamageType) Log.WriteLine($"Keeping previous damage type {bestDamageType}");
                                        }

                                        break;

                                    case 2: // swap every 1 minute?
                                        if (DebugConfig.DebugCurrentDamageType) Log.WriteLine($"Index 2. (3rd best)");
                                        if (_nextThirdBestDamageTypeSwap < DateTime.UtcNow || IsBigEnoughTargetWeShouldSwapNow)
                                        {
                                            Log.WriteLine("[bestDamageType][2] [" + bestDamageType + "] next 3rd best swap: 60 seconds");
                                            _nextThirdBestDamageTypeSwap = DateTime.UtcNow.AddSeconds(60);
                                        }
                                        else
                                        {
                                            bestDamageType = _previousDamageType.Value;
                                            if (DebugConfig.DebugCurrentDamageType) Log.WriteLine($"Keeping previous damage type {bestDamageType}");
                                        }

                                        break;

                                    case 3: // swap now
                                        if (DebugConfig.DebugCurrentDamageType) Log.WriteLine($"Index 3. (4th best)");
                                        break;
                                }
                            }

                            _previousDamageType = bestDamageType;
                        }

                        if (ESCache.Instance.InSpace && !AnyAmmoOfTypeLeft(bestDamageType)) // pick the next best ammo if there is nothing left of the best ammo
                            if (_currentBestDamageTypes.Count > 0)
                                for (var j = 0; j < _currentBestDamageTypes.Count; j++)
                                {
                                    var ammo = DirectUIModule.DefinedAmmoTypes.Where(x => !x.OnlyUseAsOverrideAmmo).ToList().Find(a => a.DamageType == _currentBestDamageTypes[j]);
                                    if (ammo != null)
                                        if (AnyAmmoOfTypeLeft(ammo.DamageType))
                                        {
                                            bestDamageType = ammo.DamageType;
                                            _previousDamageType = bestDamageType;
                                            break;
                                        }
                                        else
                                        {
                                            if (DebugConfig.DebugCurrentDamageType) Log.WriteLine(
                                                $"We DON'T have more ammo of type {ammo.DamageType} left in our cargo / launchers. Picking next best ammo.");
                                            continue;
                                        }
                                }

                        _currentDamageType = bestDamageType;
                    }

                    return _currentDamageType ?? DefaultDamageType;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return DefaultDamageType;
                }
            }
        }

        public static string CurrentFit
        {
            get => ESCache.Instance.EveAccount.CurrentFit;
            set => ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.CurrentFit), value);
        }

        public static bool DeclineMissionsWithTooManyMissionCompletionErrors { get; set; }

        public static DamageType DefaultDamageType { get; set; } = DamageType.Kinetic;

        public static string DefaultFittingName
        {
            get
            {
                if (ListofFactionFittings != null && ListofFactionFittings.Count > 0)
                {
                    _defaultFitting = ListofFactionFittings.Find(m => m.FactionName.ToLower() == "default");
                    _defaultFittingName = _defaultFitting.FittingName;
                    return _defaultFittingName;
                }

                Log.WriteLine("DefaultFittingName - no fitting found for the faction named [ default ], assuming a fitting name of [ default ] exists");
                return "default";
            }

            set => _defaultFittingName = value;
        }

        public static List<string> DistributionAgentsAllowedCorporations { get; }

        public static int? FactionActivateRepairModulesAtThisPerc { get; set; }

        public static List<string> FactionBlacklist { get; }

        public static int? FactionDroneTypeID { get; set; }

        public static string MissionSpecificShipName { get; set; }
        public static string FactionSpecificShip { get; set; }

        public static DirectAgentMission FirstAgentMission { get; set; }

        private static List<DamageType> _defaultDamgeTypes;

        private static List<DamageType> DefaultDamageTypes
        {
            get
            {
                if (_defaultDamgeTypes == null)
                {
                    _defaultDamgeTypes = new List<DamageType> {DefaultDamageType};
                }

                return _defaultDamgeTypes;
            }
        }

        //
        // todo: go through each entity on grid and get BestDamageTypes + HP info so we can determine best damagetype for the grid
        //
        /**
        public static double? test_damageTypesOfResistedHpForGrid
        {
            get
            {
                double resistedHpEmForGrid = 0;
                double resistedHpExplosiveForGrid = 0;
                double resistedHpKineticForGrid = 0;
                double resistedHpThermalForGrid = 0;
                foreach (EntityCache potentialCombatTarget in Combat.PotentialCombatTargets.Where(i => i.IsOnGridWithMe))
                {
                    resistedHpEmForGrid = resistedHpEmForGrid + potentialCombatTarget.EffectiveHitpointsViaEM;
                    resistedHpExplosiveForGrid = resistedHpExplosiveForGrid + potentialCombatTarget.EffectiveHitpointsViaExplosive;
                    resistedHpKineticForGrid = resistedHpKineticForGrid + potentialCombatTarget.EffectiveHitpointsViaKinetic;
                    resistedHpThermalForGrid = resistedHpThermalForGrid + potentialCombatTarget.EffectiveHitpointsViaThermal;
                }

                List<double?> damageTypesOfResistedHpForGrid = new List<double?>()
                {
                    resistedHpEmForGrid,
                    resistedHpExplosiveForGrid,
                    resistedHpKineticForGrid,
                    resistedHpThermalForGrid
                };

                //switch (damageTypesOfResistedHpForGrid.Where(i => i.HasValue).OrderBy(i => i.Value).FirstOrDefault())
                //{
                    //case resistedHpEmForGrid:
                    //case resistedHpExplosiveForGrid:
                    //case resistedHpKineticForGrid:
                    //case resistedHpThermalForGrid:
                //        break;
                //}

                //if ( )

                return 0; //fixme
            }
        }
        **/

        public static List<DamageType> CalcBestDamageTypeForBattleshipTargets
        {
            get
            {
                var bestDamageTypes = Combat.PotentialCombatTargets.Where(i => i.IsTarget && !i.IsNPCFrigate && !i.IsNPCCruiser).FirstOrDefault(e => e.IsCurrentTarget)?.BestDamageTypes
                                      ?? Combat.PotentialCombatTargets.Where(i => i.IsTarget && !i.IsNPCFrigate && !i.IsNPCCruiser).OrderByDescending(i => i.IsBattleship || i.IsNPCBattleship).FirstOrDefault(e =>
                                                                                      e.BracketType == BracketType.NPC_Battlecruiser
                                                                                      || e.BracketType == BracketType.NPC_Battleship)?.BestDamageTypes
                                      ?? Combat.PotentialCombatTargets.OrderByDescending(i => i.IsBattleship || i.IsNPCBattleship).FirstOrDefault(e =>
                                                                                             e.BracketType == BracketType.NPC_Battlecruiser
                                                                                             || e.BracketType == BracketType.NPC_Battleship)?.BestDamageTypes
                                      ?? BestDamageTypesForCurrentMission ?? DefaultDamageTypes;
                return bestDamageTypes;
            }
        }

        public static List<DamageType> CalcBestDamageTypeForCruiserTargets
        {
            get
            {
                var bestDamageTypes = Combat.PotentialCombatTargets.Where(i => i.IsTarget).FirstOrDefault(e => e.IsCurrentTarget)?.BestDamageTypes
                                      ?? Combat.PotentialCombatTargets.Find(i => i.IsTarget)?.BestDamageTypes
                                      ?? Combat.PotentialCombatTargets.FirstOrDefault()?.BestDamageTypes
                                      ?? BestDamageTypesForCurrentMission ?? DefaultDamageTypes;
                return bestDamageTypes;
            }
        }

        public static List<DamageType> CalcBestDamageTypeForFrigateTargets
        {
            get
            {
                var bestDamageTypes = Combat.PotentialCombatTargets.Where(i => i.IsTarget).FirstOrDefault(e => e.IsCurrentTarget)?.BestDamageTypes
                                      ?? Combat.PotentialCombatTargets.Find(i => i.IsTarget)?.BestDamageTypes
                                      ?? Combat.PotentialCombatTargets.FirstOrDefault()?.BestDamageTypes
                                      ?? BestDamageTypesForCurrentMission ?? DefaultDamageTypes;

                if (DebugConfig.DebugCurrentDamageType)
                {
                    int intCount = 0;
                    foreach (var damageType in bestDamageTypes)
                    {
                        intCount++;
                        Log.WriteLine("CalcBestDamageTypeForFrigateTargets [" + intCount + "][" + damageType +"]");
                    }
                }

                return bestDamageTypes;
            }
        }

        public static List<DamageType> GetBestDamageTypes
        {
            get
            {
                if (ESCache.Instance.InStation)
                    return DefaultDamageTypes;

                if (!ESCache.Instance.InSpace)
                    return DefaultDamageTypes;

                if (ESCache.Instance.MyShipEntity == null)
                    return DefaultDamageTypes;

                if (ESCache.Instance.MyShipEntity.IsBattleship)
                    return CalcBestDamageTypeForBattleshipTargets ?? DefaultDamageTypes;

                if (ESCache.Instance.MyShipEntity.IsBattlecruiser)
                    return CalcBestDamageTypeForBattleshipTargets ?? DefaultDamageTypes;

                if (ESCache.Instance.MyShipEntity.IsCruiser)
                    return CalcBestDamageTypeForCruiserTargets ?? DefaultDamageTypes;

                if (ESCache.Instance.MyShipEntity.IsFrigate)
                {
                    if (DebugConfig.DebugCurrentDamageType) Log.WriteLine("if (ESCache.Instance.MyShipEntity.IsFrigate)");
                    return CalcBestDamageTypeForFrigateTargets ?? DefaultDamageTypes;
                }

                return DefaultDamageTypes;
            }
        }

        private static DirectAgentMission _myMission { get; set; }

        public static DirectAgentMission MyMission
        {
            get
            {
                if (!SelectedControllerUsesCombatMissionsBehavior)
                    return null;

                if (_myMission == null)
                {
                    if (RegularMission != null)
                        _myMission = RegularMission;
                    if (StorylineMission != null && State.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.Storyline)
                        _myMission = StorylineMission;

                    return _myMission;
                }

                return _myMission;
            }
        }

        public static List<DamageType> BestDamageTypesForCurrentMission
        {
            get
            {
                if (ESCache.Instance.InAbyssalDeadspace || ESCache.Instance.InWormHoleSpace)
                    return new List<DamageType>();

                if (ESCache.Instance.ActiveShip != null && Combat.CombatShipName == ESCache.Instance.ActiveShip.GivenName)
                {
                    if (MyMission != null && MyMission.State == MissionState.Accepted)
                    {
                        if (MyMission.Type.Contains("Courier") || MyMission.Type.Contains("Trade"))
                            return new List<DamageType>();

                        return MyMission.BestDamagesTypesToShoot;
                    }
                }

                return new List<DamageType>();
            }
        }

        public static bool IsMissionFinished
        {
            get
            {
                if (SelectedControllerUsesCombatMissionsBehavior && MyMission != null && MyMission.Agent != null)
                {
                    if (MyMission.Agent.IsValid)
                    {
                        if (MyMission.IsMissionFinished != null && (bool)MyMission.IsMissionFinished)
                        {
                            if (Salvage.LootEverything)
                                return false;

                            if (ESCache.Instance.MyShipIsHealthy && Combat.PotentialCombatTargets.Any(i => i.IsWarpScramblingMe || (i.IsAttacking && i.IsInOptimalRange && i.IsNPCBattleship && i.WarpScrambleChance == 0)))
                            {
                                if (MyMission != null && MyMission.Agent.Level == 4)
                                {
                                    switch (MyMission.Name)
                                    {
                                        case "Recon (1 of 3)":
                                        case "Recon (2 of 3)":
                                        case "Recon (3 of 3)":
                                        case "Cargo Delivery":
                                        case "Smash the Supplier":
                                            return true;
                                    }

                                    return false;
                                }

                                return false;
                            }

                            return true;
                        }

                        return false;
                    }

                    return false;
                }

                return false;
            }
        }

        public static List<AgentsList> ListOfAgents { get; set; }

        public static List<FactionFitting> ListofFactionFittings
        {
            get
            {
                try
                {
                    if (Settings.Instance.UseFittingManager)
                    {
                        if (_listofFactionFittings != null && _listofFactionFittings.Count > 0)
                            return _listofFactionFittings;

                        _listofFactionFittings = new List<FactionFitting>();

                        XElement factionFittings = Settings.CharacterSettingsXml.Element("factionFittings") ??
                                                   Settings.CharacterSettingsXml.Element("factionfittings") ??
                                                   Settings.CommonSettingsXml.Element("factionFittings") ??
                                                   Settings.CommonSettingsXml.Element("factionfittings");

                        if (factionFittings != null)
                        {
                            string factionFittingXmlElementName = "";
                            if (factionFittings.Elements("factionFitting").Any())
                                factionFittingXmlElementName = "factionFitting";
                            else
                                factionFittingXmlElementName = "factionfitting";

                            int i = 0;
                            foreach (XElement factionfitting in factionFittings.Elements(factionFittingXmlElementName))
                            {
                                i++;
                                _listofFactionFittings.Add(new FactionFitting(factionfitting));
                                if (DebugConfig.DebugFittingMgr)
                                    Log.WriteLine("[" + i + "] Faction Fitting [" + factionfitting + "]");
                            }

                            return _listofFactionFittings;
                        }

                        if (Settings.Instance.UseFittingManager)
                        {
                            Log.WriteLine("No faction fittings specified. UseFittingManager is now false");
                            Settings.Instance.UseFittingManager = false;
                        }

                        return new List<FactionFitting>();
                    }

                    return new List<FactionFitting>();
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Error Loading Faction Fittings Settings [" + exception + "]");
                    return new List<FactionFitting>();
                }
            }

            private set => _listofFactionFittings = value;
        }

        public static List<MissionFitting> ListOfMissionFittings
        {
            get
            {
                try
                {
                    if (_listOfMissionFittings != null && _listOfMissionFittings.Count > 0) return _listOfMissionFittings;

                    XElement xmlElementMissionFittingsSection = Settings.CharacterSettingsXml.Element("missionfittings") ??
                                                                Settings.CommonSettingsXml.Element("missionfittings");
                    if (Settings.Instance.UseFittingManager)
                    {
                        if (xmlElementMissionFittingsSection != null)
                        {
                            if (xmlElementMissionFittingsSection.Elements("missionfitting").Any())
                                Log.WriteLine("Loading Mission Fittings");

                            int i = 0;
                            foreach (XElement missionfitting in xmlElementMissionFittingsSection.Elements("missionfitting"))
                            {
                                i++;
                                try
                                {
                                    Log.WriteLine("[" + i + "] Mission Fitting [" + missionfitting + "]");
                                    _listOfMissionFittings.Add(new MissionFitting(missionfitting));
                                }
                                catch (Exception ex)
                                {
                                    Log.WriteLine("Exception [" + ex + "]");
                                }
                            }

                            if (DebugConfig.DebugFittingMgr)
                                Log.WriteLine("        Mission Fittings now has [" + _listOfMissionFittings.Count + "] entries");
                            return _listOfMissionFittings;
                        }

                        return new List<MissionFitting>();
                    }

                    return new List<MissionFitting>();
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Error Loading Mission Fittings Settings [" + exception + "]");
                    return new List<MissionFitting>();
                }
            }
        }

        public static int MaterialsForWarOreID { get; set; }

        public static int MaterialsForWarOreQty { get; set; }

        public static float MinAgentGreyListStandings { get; set; }

        public static int? MinimumArmorPctMissionSetting { get; set; }

        public static int? MinimumCapacitorPctMissionSetting { get; set; }

        public static int? MinimumShieldPctMissionSetting { get; set; }

        public static DirectAgentMission RegularMission
        {
            get
            {
                if (ESCache.Instance.DirectEve.AgentMissions.Count > 0)
                {
                    if (AgentToPullNextRegularMissionFrom == null)
                    {
                        if (SelectedControllerUsesCombatMissionsBehavior) Log.WriteLine("RegularMission: AgentToPullNextRegularMissionFrom is null");
                        return null;
                    }
                    //
                    // purposely do not cache this beyond the frame we are in.
                    //
                    regularMission = ESCache.Instance.DirectEve.AgentMissions.Find(m => m.AgentId == AgentToPullNextRegularMissionFrom.AgentId);

                    if (regularMission != null)
                    {
                        if (regularMission.State != MissionState.Accepted)
                            return regularMission;

                        return regularMission;
                    }

                    if (ESCache.Instance.DirectEve.AgentMissions.Count > 0)
                    {
                        int intMissionNum = 0;
                        foreach (DirectAgentMission AgentMission in ESCache.Instance.DirectEve.AgentMissions)
                        {
                            intMissionNum++;
                            //Log.WriteLine("RegularMission: AgentMission [" + intMissionNum + "][" + AgentMission.Name + "] AgentId [" + AgentMission.Agent.Name + "] State [" + AgentMission.State + "]");
                        }
                    }

                    return null;
                }

                return null;
            }
        }

        public static int? MissionActivateRepairModulesAtThisPerc { get; set; }

        public static bool MissionAllowOverLoadOfEcm { get; set; }

        public static bool MissionAllowOverLoadOfReps { get; set; }

        public static bool MissionAllowOverLoadOfSpeedMod { get; set; }

        public static bool MissionAllowOverLoadOfWeapons { get; set; }

        public static bool MissionAllowOverLoadOfWebs { get; set; }

        public static bool MissionAlwaysActivateSpeedMod { get; set; }

        public static List<string> MissionBlacklist { get; }

        public static int? MissionCapacitorInjectorScript { get; set; }

        public static int? MissionDroneTypeID { get; set; }

        public static int? MissionEcmOverloadDamageAllowed { get; set; }

        public static List<string> MissionGreylist { get; }

        public static int? MissionInjectCapPerc { get; set; }

        public static List<string> MissionItems { get; }

        public static bool? MissionKillSentries { get; set; }

        public static bool? MissionLootEverything { get; set; }

        public static string MissionNameforLogging { get; set; }

        public static int? MissionNumberOfCapBoostersToLoad { get; set; }

        public static List<string> MissionObjectivePhraseBlacklist { get; }

        public static int? MissionSafeDistanceFromStructure { get; set; }

        public static string MissionsPath { get; set; }

        public static int? MissionSpeedModOverloadDamageAllowed { get; set; }

        public static int? MissionTooCloseToStructure { get; set; }

        public static bool? MissionUseDrones { get; set; }

        public static double MissionWarpAtDistanceRange { get; set; }

        public static int? MissionWeaponOverloadDamageAllowed { get; set; }

        public static int? MissionWebOverloadDamageAllowed { get; set; }

        public static bool MissionXMLIsAvailable { get; set; }

        public static List<DirectItem> ModulesInAllInGameFittings
        {
            get
            {
                try
                {
                    if (!Settings.Instance.UseFittingManager)
                    {
                        _modulesInAllInGameFittings = new List<DirectItem>();
                        return _modulesInAllInGameFittings;
                    }

                    if (ESCache.Instance.InSpace) return new List<DirectItem>();

                    if (ESCache.Instance.InStation && ESCache.Instance.FittingManagerWindow != null)
                    {
                        if (_modulesInAllInGameFittings == null)
                        {
                            _modulesInAllInGameFittings = new List<DirectItem>();
                            foreach (DirectFitting fitting in ESCache.Instance.FittingManagerWindow.Fittings)
                            {
                                foreach (DirectItem moduleInFitting in fitting.Modules)
                                {
                                    if (_modulesInAllInGameFittings.Count == 0 || (_modulesInAllInGameFittings.Count > 0 && _modulesInAllInGameFittings.All(i => i.TypeId != moduleInFitting.TypeId)))
                                        _modulesInAllInGameFittings.Add(moduleInFitting);
                                }
                            }

                            if (_modulesInAllInGameFittings.Count == 0)
                                return new List<DirectItem>();

                            return _modulesInAllInGameFittings ?? new List<DirectItem>();
                        }

                        return _modulesInAllInGameFittings ?? new List<DirectItem>();
                    }

                    return new List<DirectItem>();
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return new List<DirectItem>();
                }
            }
        }

        public static string MoveMissionItems { get; set; }

        public static int MoveMissionItemsQuantity { get; set; }

        public static int MoveOptionalMissionItemQuantity { get; set; }

        public static string MoveOptionalMissionItems { get; set; }

        public static bool OfflineModulesFound { get; set; }

        public static bool DamagedModulesFound { get; set; } = false;

        public static int? PocketActivateRepairModulesAtThisPerc { get; set; }

        public static int? PocketDroneTypeID { get; set; }

        public static bool? PocketKillSentries { get; set; }

        public static bool? PocketUseDrones { get; set; }

        public static bool RequireMissionXML { get; set; }

        public static bool? _selectedControllerUsesCombatMissionsBehavior = null;

        public static bool SelectedControllerUsesCombatMissionsBehavior
        {
            get
            {
                if (_selectedControllerUsesCombatMissionsBehavior != null)
                    return _selectedControllerUsesCombatMissionsBehavior ?? true;

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.QuestorController))
                {
                    _selectedControllerUsesCombatMissionsBehavior = true;
                    return _selectedControllerUsesCombatMissionsBehavior ?? true;
                }

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CourierMissionsController))
                {
                    _selectedControllerUsesCombatMissionsBehavior = true;
                    return _selectedControllerUsesCombatMissionsBehavior ?? true;
                }

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
                {
                    _selectedControllerUsesCombatMissionsBehavior = true;
                    return _selectedControllerUsesCombatMissionsBehavior ?? true;
                }

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.StandingsGrindController))
                {
                    _selectedControllerUsesCombatMissionsBehavior = true;
                    return _selectedControllerUsesCombatMissionsBehavior ?? true;
                }

                _selectedControllerUsesCombatMissionsBehavior = false;
                return _selectedControllerUsesCombatMissionsBehavior ?? false;
            }
        }

        public static DirectAgentMission StorylineMission
        {
            get
            {
                try
                {
                    if (_storylineMission != null && !ESCache.Instance.DirectEve.AgentMissions.Any(m => m.Important && m.UniqueMissionId == _storylineMission.UniqueMissionId))
                    {
                        _storylineMission = null;
                    }

                    if (_storylineMission == null)
                    {
                        if (ESCache.Instance.DirectEve.AgentMissions.Any(m => m.Important))
                        {
                            //
                            // do we need to grab the storyline mission?
                            //
                            if (DebugConfig.DebugStorylineMissions) Log.WriteLine("StorylineMission: if (ESCache.Instance.DirectEve.AgentMissions.Any(m => m.Important))");
                            if (StorylineMissionsInJournalThatQuestorKnowsHowToDoNotBlacklisted != null && StorylineMissionsInJournalThatQuestorKnowsHowToDoNotBlacklisted.Any())
                            {
                                if (DebugConfig.DebugStorylineMissions)
                                {
                                    int MissionNum = 0;
                                    foreach (DirectAgentMission _tempStorylineMission in StorylineMissionsInJournalThatQuestorKnowsHowToDoNotBlacklisted)
                                    {
                                        MissionNum++;
                                        if (DebugConfig.DebugStorylineMissions) Log.WriteLine("StorylineMission: in Journal: [ " + MissionNum + " ][ " + _tempStorylineMission.Name + " ][ " + _tempStorylineMission.Agent.Name + " ][ " + _tempStorylineMission.Type + " ] Jumps [ " + _tempStorylineMission.Agent.SolarSystem.JumpsHighSecOnly + " ][ " + _tempStorylineMission.Agent.StationName + " ]");
                                    }
                                }

                                foreach (DirectAgentMission tempMission in StorylineMissionsInJournalThatQuestorKnowsHowToDoNotBlacklisted)
                                {
                                    if (tempMission.Type.Contains("Trade") && Settings.Instance.StorylineDoNotTryToDoTradeMissions)
                                        continue;

                                    if (tempMission.Type.Contains("Mining") && Settings.Instance.StorylineDoNotTryToDoMiningMissions)
                                        continue;

                                    if (tempMission.Type.Contains("Courier") && Settings.Instance.StorylineDoNotTryToDoCourierMissions)
                                        continue;

                                    if (tempMission.Type.Contains("Encounter") && Settings.Instance.StorylineDoNotTryToDoEncounterMissions)
                                        continue;

                                    _storylineMission = tempMission;
                                    if (DebugConfig.DebugStorylineMissions) Log.WriteLine("StorylineMission: Closest Mission [ " + _storylineMission.Name + " ][ " + _storylineMission.Agent.Name + " ][ " + _storylineMission.Type + " ] Jumps [ " + _storylineMission.Agent.SolarSystem.JumpsHighSecOnly + " ][ " + _storylineMission.Agent.StationName + " ]");
                                    return _storylineMission;
                                }

                                if (DebugConfig.DebugStorylineMissions) Log.WriteLine("StorylineMission: if (!StorylineMissionsInJournalThatQuestorKnowsHowToDoNotBlacklisted.Any()).");
                                return null;
                            }

                            if (DebugConfig.DebugStorylineMissions) Log.WriteLine("StorylineMission: if (!StorylineMissionsInJournalThatQuestorKnowsHowToDoNotBlacklisted.Any())");
                            return null;
                        }

                        return null;
                    }

                    return _storylineMission;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        public static IEnumerable<DirectAgentMission> StorylineMissionsInJournalThatQuestorKnowsHowToDoNotBlacklisted
        {
            get
            {
                try
                {
                    if (_storylineMissionsInJournalThatQuestorKnowsHowToDoNotBlacklisted == null)
                        try
                        {
                            if (StorylineMissionsInJournalThatQuestorKnowsHowToDo != null && StorylineMissionsInJournalThatQuestorKnowsHowToDo.Any())
                            {
                                _storylineMissionsInJournalThatQuestorKnowsHowToDoNotBlacklisted = StorylineMissionsInJournalThatQuestorKnowsHowToDo.Where(i => MissionBlacklist.All(j => i.Name != null && Log.FilterPath(i.Name).ToLower() != j.ToLower())).OrderBy(i => i.Agent.SolarSystem.JumpsHighSecOnly).ToList() ?? new List<DirectAgentMission>();
                                if (_storylineMissionsInJournalThatQuestorKnowsHowToDoNotBlacklisted.Any())
                                    return _storylineMissionsInJournalThatQuestorKnowsHowToDoNotBlacklisted;

                                if (DebugConfig.DebugStorylineMissions)
                                    Log.WriteLine("if (_storylineMissionsInJournalThatQuestorKnowsHowToDo == null || !_storylineMissionsInJournalThatQuestorKnowsHowToDo.Any())");

                                return new List<DirectAgentMission>();
                            }

                            if (DebugConfig.DebugStorylineMissions) Log.WriteLine("!if (StorylineMissionsInJournalThatQuestorKnowsHowToDo != null && StorylineMissionsInJournalThatQuestorKnowsHowToDo.Any())");
                            return new List<DirectAgentMission>();
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine("Exception [" + ex + "]");
                            return new List<DirectAgentMission>();
                        }

                    return _storylineMissionsInJournalThatQuestorKnowsHowToDoNotBlacklisted;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return new List<DirectAgentMission>();
                }
            }
        }

        public static string StrCurrentAgentName
        {
            get
            {
                try
                {
                    if (Settings.CharacterXmlExists)
                    {
                        if (string.IsNullOrEmpty(_currentAgent))
                            try
                            {
                                if (ListOfAgents != null && ListOfAgents.Count > 0)
                                {
                                    _currentAgent = ListOfAgents.FirstOrDefault().Name;
                                    Log.WriteLine("Current Agent is [" + _currentAgent + "]");
                                }
                                else
                                {
                                    Log.WriteLine("MissionSettings.ListOfAgents == null ");
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.WriteLine("Exception [" + ex + "]");
                                return string.Empty;
                            }

                        return _currentAgent;
                    }

                    Log.WriteLine("Unable to find an agent. CharacterXMLExists is [" + Settings.CharacterXmlExists + "]");
                    return string.Empty;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return "";
                }
            }
            set
            {
                try
                {
                    _currentAgent = value;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }
            }
        }

        public static bool UseMissionShip { get; set; }

        private static string _currentAgent { get; set; }

        private static MissionFitting _missionSpecificMissionFitting { get; set; }

        private static FactionFitting FactionFittingForThisMissionsFaction { get; set; }

        private static IEnumerable<DirectAgentMission> MissionsInJournalFromNotBlacklistedAgents
        {
            get
            {
                try
                {
                    if (_missionsInJournalFromNotBlacklistedAgents == null)
                        try
                        {
                            _missionsInJournalFromNotBlacklistedAgents = ESCache.Instance.DirectEve.AgentMissions.Where(m => !AgentBlacklist.Contains(m.AgentId)).ToList() ?? new List<DirectAgentMission>();
                            if (_missionsInJournalFromNotBlacklistedAgents.Any())
                                return _missionsInJournalFromNotBlacklistedAgents;

                            if (DebugConfig.DebugStorylineMissions)
                                Log.WriteLine("if (_missionsInJournalFromNotBlacklistedAgents == null || !_missionsInJournalFromNotBlacklistedAgents.Any())");

                            return new List<DirectAgentMission>();
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine("Exception [" + ex + "]");
                            return new List<DirectAgentMission>();
                        }

                    return _missionsInJournalFromNotBlacklistedAgents;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return new List<DirectAgentMission>();
                }
            }
        }

        private static bool IsThisAStorylineMissionTypeWeDo(DirectAgentMission tempMissionInJournal)
        {
            if (!tempMissionInJournal.Important) return true;

            if (tempMissionInJournal.Type.Contains("Trade") && Settings.Instance.StorylineDoNotTryToDoTradeMissions)
            {
                if (DebugConfig.DebugStorylineMissions) Log.WriteLine("Removing [" + tempMissionInJournal + "] from the list of storyline missions because it is a [ Trade ] mission");
                return false;
            }

            if (tempMissionInJournal.Type.Contains("Mining") && Settings.Instance.StorylineDoNotTryToDoMiningMissions)
            {
                if (DebugConfig.DebugStorylineMissions) Log.WriteLine("Removing [" + tempMissionInJournal + "] from the list of storyline missions because it is a [ Mining ] mission");
                return false;
            }

            if (tempMissionInJournal.Type.Contains("Courier") && Settings.Instance.StorylineDoNotTryToDoCourierMissions)
            {
                if (DebugConfig.DebugStorylineMissions) Log.WriteLine("Removing [" + tempMissionInJournal + "] from the list of storyline missions because it is a [ Courier ] mission");
                return false;
            }

            if (tempMissionInJournal.Type.Contains("Encounter") && Settings.Instance.StorylineDoNotTryToDoEncounterMissions)
            {
                if (DebugConfig.DebugStorylineMissions) Log.WriteLine("Removing [" + tempMissionInJournal + "] from the list of storyline missions because it is a [ Encounter ] mission");
                return false;
            }

            return true;
        }

        private static IEnumerable<DirectAgentMission> StorylineMissionsInJournal
        {
            get
            {
                try
                {
                    if (_storylineMissionsInJournal == null)
                        try
                        {
                            if (MissionsInJournalFromNotBlacklistedAgents != null && MissionsInJournalFromNotBlacklistedAgents.Any())
                            {
                                _storylineMissionsInJournal = MissionsInJournalFromNotBlacklistedAgents.Where(m => m.Type.ToLower().Contains("Storyline".ToLower()) && !m.Name.Contains("Cash Flow for Capsuleers")).ToList() ?? new List<DirectAgentMission>();
                                IEnumerable<DirectAgentMission> tempMissions = null;
                                tempMissions = _storylineMissionsInJournal.Where(IsThisAStorylineMissionTypeWeDo);

                                if (tempMissions.Any())
                                    return tempMissions;

                                if (DebugConfig.DebugStorylineMissions)
                                    Log.WriteLine("StorylineMissionsInJournal: if (_storylineMissionsInJournal == null || !_storylineMissionsInJournal.Any())");

                                return new List<DirectAgentMission>();
                            }

                            if (DebugConfig.DebugStorylineMissions)
                                Log.WriteLine("StorylineMissionsInJournal: if (MissionsInJournalFromNotBlacklistedAgents == null || !MissionsInJournalFromNotBlacklistedAgents.Any())");

                            return new List<DirectAgentMission>();
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine("Exception [" + ex + "]");
                            return new List<DirectAgentMission>();
                        }

                    return _storylineMissionsInJournal;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return new List<DirectAgentMission>();
                }
            }
        }

        private static IEnumerable<DirectAgentMission> StorylineMissionsInJournalThatQuestorKnowsHowToDo
        {
            get
            {
                try
                {
                    if (_storylineMissionsInJournalThatQuestorKnowsHowToDo == null)
                        try
                        {
                            if (StorylineMissionsInJournal != null)
                            {
                                if (StorylineMissionsInJournal.Any(m => StorylineInstance._storylines.ContainsKey(Log.FilterPath(m.Name.ToLower()))))
                                {
                                    _storylineMissionsInJournalThatQuestorKnowsHowToDo = StorylineMissionsInJournal.Where(m => StorylineInstance._storylines.ContainsKey(Log.FilterPath(m.Name.ToLower()))).ToList() ?? new List<DirectAgentMission>();
                                    if (_storylineMissionsInJournalThatQuestorKnowsHowToDo.Any())
                                        return _storylineMissionsInJournalThatQuestorKnowsHowToDo;

                                    if (DebugConfig.DebugStorylineMissions)
                                        Log.WriteLine("if (_storylineMissionsInJournalThatQuestorKnowsHowToDo == null || !_storylineMissionsInJournalThatQuestorKnowsHowToDo.Any())");

                                    return new List<DirectAgentMission>();
                                }

                                int StorylineNum = 0;
                                foreach (DirectAgentMission StorylineMissionInJournal in StorylineMissionsInJournal)
                                {
                                    StorylineNum++;
                                    if (DebugConfig.DebugStorylineMissions && !StorylineMissionsLogged)
                                        Log.WriteLine("StorylineMissionsInJournalThatQuestorKnowsHowToDo: [" + StorylineNum + "][" + StorylineMissionInJournal.Name + "] Agent [" + StorylineMissionInJournal.Agent.Name + "]");
                                }
                                StorylineMissionsLogged = true;

                                return new List<DirectAgentMission>();
                            }

                            return new List<DirectAgentMission>();
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine("Exception [" + ex + "]");
                            return new List<DirectAgentMission>();
                        }

                    return _storylineMissionsInJournalThatQuestorKnowsHowToDo;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return new List<DirectAgentMission>();
                }
            }
        }

        public static bool CourierMission(DirectAgentMission myMission = null)
        {
            if (myMission != null)
            {
                if (!string.IsNullOrEmpty(myMission.Type))
                {
                    if (DebugConfig.DebugCourierMissions) Log.WriteLine("Mission.Type [" + myMission.Type + "]");

                    if (myMission.Name.ToLower().Contains("Mysterious Sightings (2 of 4)".ToLower()))
                        return true;

                    if (myMission.Type.ToLower().Contains("Courier".ToLower()))
                        return true;

                    if (myMission.Type.ToLower().Contains("Encounter".ToLower()))
                        return false;

                    if (myMission.Type.ToLower().Contains("Mining".ToLower()))
                        return false;

                    return false;
                }

                if (DebugConfig.DebugCourierMissions) Log.WriteLine("myAgent.RegularMission.Type is [ null ]");
                return false;
            }

            if (DebugConfig.DebugCourierMissions) Log.WriteLine("myAgent is [ null ]");
            return false;
        }

        public static string FactionFittingNameForThisMissionsFaction(DirectAgentMission myMission)
        {
            if (_factionFittingNameForThisMissionsFaction == null)
            {
                if (myMission.Faction == null)
                    return null;

                if (ListofFactionFittings.Any(i => myMission.Faction != null && i.FactionName.ToLower() == myMission.Faction.Name.ToLower()))
                {
                    if (ListofFactionFittings.Find(m => myMission.Faction != null && m.FactionName.ToLower() == myMission.Faction.Name.ToLower()) != null)
                    {
                        FactionFittingForThisMissionsFaction = ListofFactionFittings.Find(m => myMission.Faction.Name != null && m.FactionName.ToLower() == myMission.Faction.Name.ToLower());
                        if (FactionFittingForThisMissionsFaction != null)
                        {
                            _factionFittingNameForThisMissionsFaction = FactionFittingForThisMissionsFaction.FittingName;
                            if (FactionFittingForThisMissionsFaction.Dronetype != 0)
                            {
                                Drones.FactionDroneTypeID = (int)FactionFittingForThisMissionsFaction.Dronetype;
                                FactionDroneTypeID = (int)FactionFittingForThisMissionsFaction.Dronetype;
                            }

                            Log.WriteLine("Faction fitting [" + FactionFittingForThisMissionsFaction.FactionName + "] DroneTypeID [" + Drones.DroneTypeID +
                                          "]");
                            return _factionFittingNameForThisMissionsFaction;
                        }

                        return null;
                    }

                    return null;
                }

                if (ListofFactionFittings.Any(i => i.FactionName.ToLower() == "Default".ToLower()))
                    if (ListofFactionFittings.Find(m => m.FactionName.ToLower() == "Default".ToLower()) != null)
                    {
                        FactionFittingForThisMissionsFaction = ListofFactionFittings.Find(m => m.FactionName.ToLower() == "Default".ToLower());
                        if (FactionFittingForThisMissionsFaction != null)
                        {
                            _factionFittingNameForThisMissionsFaction = FactionFittingForThisMissionsFaction.FittingName;
                            if (FactionFittingForThisMissionsFaction.Dronetype != 0)
                            {
                                Drones.FactionDroneTypeID = (int)FactionFittingForThisMissionsFaction.Dronetype;
                                FactionDroneTypeID = (int)FactionFittingForThisMissionsFaction.Dronetype;
                            }

                            Log.WriteLine("Faction fitting [" + FactionFittingForThisMissionsFaction.FactionName + "] Using DroneTypeID [" +
                                          Drones.DroneTypeID + "]");
                            return _factionFittingNameForThisMissionsFaction;
                        }

                        return null;
                    }

                return null;
            }

            return _factionFittingNameForThisMissionsFaction;
        }

        public static string FittingToTryToLoad(DirectAgentMission myMission)
        {
            try
            {
                //if (!string.IsNullOrEmpty(_fittingToLoad))
                //{
                //    return _fittingToLoad;
                //}

                if (State.CurrentQuestorState == QuestorState.CombatMissionsBehavior)
                {
                    if (MyMission.Name.Contains("Anomic"))
                        return myMission.Name;

                    if (string.IsNullOrEmpty(MissionFittingNameForThisMissionName(myMission)))
                    {
                        if (string.IsNullOrEmpty(FactionFittingNameForThisMissionsFaction(myMission)))
                        {
                            Log.WriteLine("[FittingToLoad] Using DefaultFittingName [" + DefaultFittingName + "] MissionFittingNameForThisMissionName [" + MissionFittingNameForThisMissionName(myMission) + "] FactionFittingNameForThisMissionsFaction [" + FactionFittingNameForThisMissionsFaction(myMission) + "]");
                            _fittingToLoad = DefaultFittingName.ToLower();
                        }

                        Log.WriteLine("[FittingToLoad] Using FactionFittingNameForThisMissionsFaction [" + FactionFittingNameForThisMissionsFaction(myMission) + "]");
                        _fittingToLoad = FactionFittingNameForThisMissionsFaction(myMission);
                        return _fittingToLoad;
                    }

                    Log.WriteLine("[FittingToLoad] Using MissionFittingNameForThisMissionName [" + MissionFittingNameForThisMissionName(myMission) + "]");
                    _fittingToLoad = MissionFittingNameForThisMissionName(myMission);
                    return _fittingToLoad;
                }

                if (State.CurrentAbyssalDeadspaceBehaviorState == AbyssalDeadspaceBehaviorState.Arm)
                {
                    //Settings.Instance.
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return null;
            }
        }

        public static bool HasStoryline()
        {
            // Do we have a registered storyline?
            if (StorylineMission != null)
            {
                if (DateTime.UtcNow > LastStoryLineMissionLogging.AddSeconds(30))
                {
                    LastStoryLineMissionLogging = DateTime.UtcNow;
                    Log.WriteLine("---------------------------- Storyline Mission Info --------------------------");
                    Log.WriteLine("[" + ESCache.Instance.DirectEve.AgentMissions.Count + "] missions available.");
                    Log.WriteLine("[" + MissionsInJournalFromNotBlacklistedAgents.Count() + "] missions available that are not from blacklisted agents");
                    Log.WriteLine("[" + StorylineMissionsInJournal.Count() + "] storyline missions available");
                    int intStorylineMissionNum = 0;
                    foreach (DirectAgentMission mission in StorylineMissionsInJournal)
                    {
                        intStorylineMissionNum++;
                        Log.WriteLine("[" + intStorylineMissionNum + "] Storyline Mission Name [" + Log.FilterPath(mission.Name) + "] Type [" + mission.Type + "]");
                    }

                    Log.WriteLine("[" + StorylineMissionsInJournalThatQuestorKnowsHowToDo.Count() + "] storyline missions questor knows how to do");
                    Log.WriteLine("[" + StorylineMissionsInJournalThatQuestorKnowsHowToDoNotBlacklisted.Count() + "] storyline missions questor knows how to do and are not blacklisted");
                    Log.WriteLine("---------------------------- Storyline Mission Info. -------------------------");
                    return true;
                }

                return true;
            }

            return false;
        }

        public static string MissionFittingNameForThisMissionName(DirectAgentMission myMission)
        {
            try
            {
                if (MissionSpecificMissionFitting(myMission) != null && !string.IsNullOrEmpty(MissionSpecificMissionFitting(myMission).FittingName))
                    return MissionSpecificMissionFitting(myMission).FittingName;

                if (DebugConfig.DebugFittingMgr) Log.WriteLine("DebugFittingMgr: [MissionFittingNameForThisMissionName] failed: if (MissionSpecificMissionFitting != null && !string.IsNullOrEmpty(MissionSpecificMissionFitting.Ship))");
                return null;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return null;
            }
        }

        public static MissionFitting MissionSpecificMissionFitting(DirectAgentMission myMission)
        {
            if (_missionSpecificMissionFitting == null)
            {
                if (myMission == null)
                    return null;

                //if (myMission.Faction == null)
                //    return null;

                if (!string.IsNullOrEmpty(myMission.Name))
                {
                    //if (UseMissionSpecificShip)
                    {
                        MissionFitting tempFittingWeFound = null;
                        string lookForFittingNamed = string.Empty;
                        if (ListOfMissionFittings != null && ListOfMissionFittings.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(myMission.Faction.Name))
                            {
                                lookForFittingNamed = Log.FilterPath(myMission.Name.ToLower() + "-" + Log.FilterPath(myMission.Faction.Name).ToLower() + "-" + Log.FilterPath(ESCache.Instance.EveAccount.CharacterName).ToLower());
                                tempFittingWeFound = LookForFitting(lookForFittingNamed.ToLower());
                                if (tempFittingWeFound != null)
                                {
                                    _missionSpecificMissionFitting = tempFittingWeFound;
                                    return _missionSpecificMissionFitting;
                                }

                                lookForFittingNamed = Log.FilterPath(myMission.Name.ToLower() + "-" + Log.FilterPath(myMission.Faction.Name).ToLower());
                                tempFittingWeFound = LookForFitting(lookForFittingNamed.ToLower());
                                if (tempFittingWeFound != null)
                                {
                                    _missionSpecificMissionFitting = tempFittingWeFound;
                                    return _missionSpecificMissionFitting;
                                }

                                lookForFittingNamed = Log.FilterPath(myMission.Name.ToLower() + "-" + myMission.Faction.Name.ToLower());
                                tempFittingWeFound = LookForFitting(lookForFittingNamed.ToLower());
                                if (tempFittingWeFound != null)
                                {
                                    _missionSpecificMissionFitting = tempFittingWeFound;
                                    return _missionSpecificMissionFitting;
                                }
                            }

                            lookForFittingNamed = Log.FilterPath(myMission.Name.ToLower() + "-" + Log.FilterPath(ESCache.Instance.EveAccount.CharacterName).ToLower());
                            tempFittingWeFound = LookForFitting(lookForFittingNamed);
                            if (tempFittingWeFound != null)
                            {
                                _missionSpecificMissionFitting = tempFittingWeFound;
                                return _missionSpecificMissionFitting;
                            }

                            lookForFittingNamed = Log.FilterPath(myMission.Name.ToLower());
                            tempFittingWeFound = LookForFitting(lookForFittingNamed);
                            if (tempFittingWeFound != null)
                            {
                                _missionSpecificMissionFitting = tempFittingWeFound;
                                return _missionSpecificMissionFitting;
                            }

                            lookForFittingNamed = myMission.Name.ToLower();
                            tempFittingWeFound = LookForFitting(lookForFittingNamed);
                            if (tempFittingWeFound != null)
                            {
                                _missionSpecificMissionFitting = tempFittingWeFound;
                                return _missionSpecificMissionFitting;
                            }

                            return null;
                        }

                        return null;
                    }
                }

                return null;
            }

            return _missionSpecificMissionFitting;
        }

        public static string MissionXmlPath(DirectAgentMission myMission)
        {
            try
            {
                if (_missionXmlPath != null) return _missionXmlPath;

                if (myMission == null) return string.Empty;

                if (myMission.Faction == null)
                    return null;

                if (!string.IsNullOrEmpty(myMission.Faction.Name))
                {
                    _missionXmlPath = Path.Combine(MissionsPath, Log.FilterPath(myMission.Name) + "-" + Log.FilterPath(Log.CharacterName) + ".xml");
                    if (!File.Exists(_missionXmlPath))
                    {
                        _missionXmlPath = Path.Combine(MissionsPath, Log.FilterPath(myMission.Name) + "-" + Log.FilterPath(myMission.Faction.Name) + ".xml");
                        if (!File.Exists(_missionXmlPath))
                        {
                            _missionXmlPath = Path.Combine(MissionsPath, Log.FilterPath(myMission.Name) + "-" + myMission.Faction.Name + ".xml");
                            if (!File.Exists(_missionXmlPath))
                            {
                                Log.WriteLine("[" + _missionXmlPath + "] not found.");
                                _missionXmlPath = Path.Combine(MissionsPath, Log.FilterPath(myMission.Name) + ".xml");
                                if (!File.Exists(_missionXmlPath)) Log.WriteLine("[" + _missionXmlPath + "] not found");
                                if (File.Exists(_missionXmlPath)) Log.WriteLine("[" + _missionXmlPath + "] found!");
                                return _missionXmlPath;
                            }

                            if (File.Exists(_missionXmlPath)) Log.WriteLine("[" + _missionXmlPath + "] found!");
                            return _missionXmlPath;
                        }

                        if (File.Exists(_missionXmlPath)) Log.WriteLine("[" + _missionXmlPath + "] found!");
                        return _missionXmlPath;
                    }

                    if (File.Exists(_missionXmlPath)) Log.WriteLine("[" + _missionXmlPath + "] found!");
                    return _missionXmlPath;
                }

                Log.WriteLine("MissionXMLpath: if (!string.IsNullOrEmpty(myMission.Faction.Name)) myMission.Name [" + myMission.Name + "]");
                _missionXmlPath = Path.Combine(MissionsPath, Log.FilterPath(Log.FilterPath(myMission.Name)) + ".xml");
                if (File.Exists(_missionXmlPath)) Log.WriteLine("[" + _missionXmlPath + "] found!");
                return _missionXmlPath;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return string.Empty;
            }
        }

        public static string SiteXmlPath(DirectSystemScanResult myDirectSystemScanResult)
        {
            try
            {
                if (_siteXmlPath != null) return _siteXmlPath;

                if (_siteXmlPath == null) return string.Empty;

                if (myDirectSystemScanResult.Faction == null)
                    return null;

                if (!string.IsNullOrEmpty(myDirectSystemScanResult.Faction.Name))
                {
                    _siteXmlPath = Path.Combine(MissionsPath, Log.FilterPath(myDirectSystemScanResult.TypeName) + "-" + Log.FilterPath(ESCache.Instance.EveAccount.CharacterName) + ".xml");
                    if (!File.Exists(_siteXmlPath))
                    {
                        _siteXmlPath = Path.Combine(MissionsPath, Log.FilterPath(myDirectSystemScanResult.TypeName) + "-" + Log.FilterPath(myDirectSystemScanResult.Faction.Name) + ".xml");
                        if (!File.Exists(_siteXmlPath))
                        {
                            _siteXmlPath = Path.Combine(MissionsPath, Log.FilterPath(myDirectSystemScanResult.TypeName) + "-" + myDirectSystemScanResult.Faction.Name + ".xml");
                            if (!File.Exists(_siteXmlPath))
                            {
                                Log.WriteLine("[" + _siteXmlPath + "] not found.");
                                _siteXmlPath = Path.Combine(MissionsPath, Log.FilterPath(myDirectSystemScanResult.TypeName) + ".xml");
                                if (!File.Exists(_siteXmlPath)) Log.WriteLine("[" + _siteXmlPath + "] not found");
                                if (File.Exists(_siteXmlPath)) Log.WriteLine("[" + _siteXmlPath + "] found!");
                                return _siteXmlPath;
                            }

                            if (File.Exists(_siteXmlPath)) Log.WriteLine("[" + _siteXmlPath + "] found!");
                            return _siteXmlPath;
                        }

                        if (File.Exists(_siteXmlPath)) Log.WriteLine("[" + _siteXmlPath + "] found!");
                        return _siteXmlPath;
                    }

                    if (File.Exists(_siteXmlPath)) Log.WriteLine("[" + _siteXmlPath + "] found!");
                    return _siteXmlPath;
                }

                Log.WriteLine("MissionXMLpath: if (!string.IsNullOrEmpty(myDirectSystemScanResult.Faction.Name)) myDirectSystemScanResult.TypeName [" + myDirectSystemScanResult.TypeName + "]");
                _siteXmlPath = Path.Combine(MissionsPath, Log.FilterPath(Log.FilterPath(myDirectSystemScanResult.TypeName)) + ".xml");
                if (File.Exists(_siteXmlPath)) Log.WriteLine("[" + _siteXmlPath + "] found!");
                return _siteXmlPath;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return string.Empty;
            }
        }

        public static bool StorylineMissionDetected()
        {
            if (Settings.Instance.EnableStorylines)
            {
                if (HasStoryline())
                {
                    Log.WriteLine("EnableStorylines [" + Settings.Instance.EnableStorylines + "]: Storyline detected");
                    return true;
                }

                if (DebugConfig.DebugStorylineMissions) Log.WriteLine("EnableStorylines [" + Settings.Instance.EnableStorylines + "]: No storyline missions detected");
                return false;
            }

            if (DebugConfig.DebugStorylineMissions) Log.WriteLine("EnableStorylines [" + Settings.Instance.EnableStorylines + "]");
            return false;
        }

        public static bool SwitchAgents(string agentShortDescription, string agentToAttemptToSwitchTo, string nameOfEveAccountSetting = "")
        {
            DirectAgent tempAgent = null;
            Log.WriteLine("Attempting to use agent [" + agentShortDescription + "][" + agentToAttemptToSwitchTo + "]");
            try
            {
                tempAgent = ESCache.Instance.DirectEve.GetAgentByName(agentToAttemptToSwitchTo);
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }

            if (tempAgent != null)
            {
                Log.WriteLine("SwitchAgents: Agent [" + tempAgent.Name + "] AgentID [" + tempAgent.AgentId + "] Faction [" + tempAgent.FactionName + "]");
                AgentToPullNextRegularMissionFrom = null;
                StrCurrentAgentName = tempAgent.Name;
                _agent = tempAgent;
                //State.CurrentQuestorState = QuestorState.CombatMissionsBehavior;
                return true;
            }

            if (string.IsNullOrEmpty(nameOfEveAccountSetting))
            {
                Log.WriteLine("Marking Agent [" + agentShortDescription + "][" + StrCurrentAgentName + "] done: we could find no agent with that name!");
                ESCache.Instance.TaskSetEveAccountAttribute(nameOfEveAccountSetting, true);
                return false;
            }

            Log.WriteLine("Marking Agent [" + agentShortDescription + "][" + StrCurrentAgentName + "] done: we could find no agent with that name! tempagent == null");
            ESCache.Instance.TaskSetEveAccountAttribute(nameOfEveAccountSetting, true);
            return false;
        }

        #endregion Properties

        #region Methods

        public static bool AnyAmmoOfTypeLeft(DamageType t)
        {
            AmmoType ammo = DirectUIModule.DefinedAmmoTypes.Find(a => a.DamageType == t);
            if (ammo != null)
            {
                if (ESCache.Instance.CurrentShipsCargo?.Items.Any(i => !i.IsSingleton && i.TypeId == ammo.TypeId && i.Quantity > Combat.MinimumAmmoCharges)
                    ?? ESCache.Instance.Weapons.Where(w => w.Charge != null && w.Charge.TypeId == ammo.TypeId).Sum(w => w.ChargeQty) > 0)
                    return true;
            }

            return false;
        }

        public static void ClearFactionSpecificSettings()
        {
            FactionActivateRepairModulesAtThisPerc = null;
            FactionDroneTypeID = null;
            _listofFactionFittings.Clear();
        }

        private const string constCareerAgentAmarr1 = "Chakh Madafe";
        private const string constCareerAgentAmarr2 = "Zafarara Fari";
        private const string constCareerAgentAmarr3 = "Joas Alathema";
        private const string constCareerAgentCaldari1 = "Ikonaiki Ebora";
        private const string constCareerAgentCaldari2 = "Yamonen Petihainen";
        private const string constCareerAgentCaldari3 = "Ranta Tarumo";
        private const string constCareerAgentGallente1 = "Berlimaute Remintgarnes";
        private const string constCareerAgentGallente2 = "Hasier Parcie";
        private const string constCareerAgentGallente3 = "Seville Eyron";
        private const string constCareerAgentMinmatar1 = "Fykalia Adaferid";
        private const string constCareerAgentMinmatar2 = "Arninald Beinarakur";
        private const string constCareerAgentMinmatar3 = "Stird Odetlef";

        public static void TrackCareerAgentsWithNoMissionsAvailable(DirectAgent myAgent)
        {
            //
            // confirm agent has no missions?
            //
            if (AgentInteraction.boolNoMissionsAvailable)
            {
                //Amarr Agents
                if (myAgent != null && myAgent.Name == constCareerAgentAmarr1)
                {
                    Log.WriteLine("Marking Agent [" + myAgent.Name + "] complete as they have no more missions available to us.");
                    StrCurrentAgentName = null;
                    State.CurrentQuestorState = QuestorState.Start;
                    State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.CareerAgentAmarr1MissionsComplete), true);
                    //why would this be necessary?!
                    ESCache.Instance.EveAccount.CareerAgentAmarr1MissionsComplete = true;
                    AgentInteraction.boolNoMissionsAvailable = false;
                    return;
                }

                //Amarr Agents
                if (myAgent != null && myAgent.Name == constCareerAgentAmarr2)
                {
                    Log.WriteLine("Marking Agent [" + myAgent.Name + "] complete as they have no more missions available to us.");
                    StrCurrentAgentName = null;
                    State.CurrentQuestorState = QuestorState.Start;
                    State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.CareerAgentAmarr2MissionsComplete), true);
                    //why would this be necessary?!
                    ESCache.Instance.EveAccount.CareerAgentAmarr2MissionsComplete = true;
                    AgentInteraction.boolNoMissionsAvailable = false;
                    return;
                }

                //Amarr Agents
                if (myAgent != null && myAgent.Name == constCareerAgentAmarr3)
                {
                    Log.WriteLine("Marking Agent [" + myAgent.Name + "] complete as they have no more missions available to us.");
                    StrCurrentAgentName = null;
                    State.CurrentQuestorState = QuestorState.Start;
                    State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.CareerAgentAmarr3MissionsComplete), true);
                    //why would this be necessary?!
                    ESCache.Instance.EveAccount.CareerAgentAmarr3MissionsComplete = true;
                    AgentInteraction.boolNoMissionsAvailable = false;
                }

                //Caldari Agents
                if (myAgent != null && myAgent.Name == constCareerAgentCaldari1)
                {
                    Log.WriteLine("Marking Agent [" + myAgent.Name + "] complete as they have no more missions available to us.");
                    StrCurrentAgentName = null;
                    State.CurrentQuestorState = QuestorState.Start;
                    State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.CareerAgentCaldari1MissionsComplete), true);
                    //why would this be necessary?!
                    ESCache.Instance.EveAccount.CareerAgentCaldari1MissionsComplete = true;
                    AgentInteraction.boolNoMissionsAvailable = false;
                    return;
                }

                //Caldari Agents
                if (myAgent != null && myAgent.Name == constCareerAgentCaldari2)
                {
                    Log.WriteLine("Marking Agent [" + myAgent.Name + "] complete as they have no more missions available to us.");
                    StrCurrentAgentName = null;
                    State.CurrentQuestorState = QuestorState.Start;
                    State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.CareerAgentCaldari2MissionsComplete), true);
                    //why would this be necessary?!
                    ESCache.Instance.EveAccount.CareerAgentCaldari2MissionsComplete = true;
                    AgentInteraction.boolNoMissionsAvailable = false;
                    return;
                }

                //Caldari Agents
                if (myAgent != null && myAgent.Name == constCareerAgentCaldari3)
                {
                    Log.WriteLine("Marking Agent [" + myAgent.Name + "] complete as they have no more missions available to us.");
                    StrCurrentAgentName = null;
                    State.CurrentQuestorState = QuestorState.Start;
                    State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.CareerAgentCaldari3MissionsComplete), true);
                    //why would this be necessary?!
                    ESCache.Instance.EveAccount.CareerAgentCaldari3MissionsComplete = true;
                    AgentInteraction.boolNoMissionsAvailable = false;
                    return;
                }

                //Minmatar Agents
                if (myAgent != null && myAgent.Name == constCareerAgentMinmatar1)
                {
                    Log.WriteLine("Marking Agent [" + myAgent.Name + "] complete as they have no more missions available to us.");
                    StrCurrentAgentName = null;
                    State.CurrentQuestorState = QuestorState.Start;
                    State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.CareerAgentMinmatar1MissionsComplete), true);
                    //why would this be necessary?!
                    ESCache.Instance.EveAccount.CareerAgentMinmatar1MissionsComplete = true;
                    AgentInteraction.boolNoMissionsAvailable = false;
                    return;
                }

                //Minmatar Agents
                if (myAgent != null && myAgent.Name == constCareerAgentMinmatar2)
                {
                    Log.WriteLine("Marking Agent [" + myAgent.Name + "] complete as they have no more missions available to us.");
                    StrCurrentAgentName = null;
                    State.CurrentQuestorState = QuestorState.Start;
                    State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.CareerAgentMinmatar2MissionsComplete), true);
                    //why would this be necessary?!
                    ESCache.Instance.EveAccount.CareerAgentMinmatar2MissionsComplete = true;
                    AgentInteraction.boolNoMissionsAvailable = false;
                    return;
                }

                //Minmatar Agents
                if (myAgent != null && myAgent.Name == constCareerAgentMinmatar3)
                {
                    Log.WriteLine("Marking Agent [" + myAgent.Name + "] complete as they have no more missions available to us.");
                    StrCurrentAgentName = null;
                    State.CurrentQuestorState = QuestorState.Start;
                    State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.CareerAgentMinmatar3MissionsComplete), true);
                    //why would this be necessary?!
                    ESCache.Instance.EveAccount.CareerAgentMinmatar3MissionsComplete = true;
                    AgentInteraction.boolNoMissionsAvailable = false;
                    return;
                }

                //Gallente Agents
                if (myAgent != null && myAgent.Name == constCareerAgentGallente1)
                {
                    Log.WriteLine("Marking Agent [" + myAgent.Name + "] complete as they have no more missions available to us.");
                    StrCurrentAgentName = null;
                    State.CurrentQuestorState = QuestorState.Start;
                    State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.CareerAgentGallente1MissionsComplete), true);
                    //why would this be necessary?!
                    ESCache.Instance.EveAccount.CareerAgentGallente1MissionsComplete = true;
                    AgentInteraction.boolNoMissionsAvailable = false;
                    return;
                }

                //Gallente Agents
                if (myAgent != null && myAgent.Name == constCareerAgentGallente2)
                {
                    Log.WriteLine("Marking Agent [" + myAgent.Name + "] complete as they have no more missions available to us.");
                    StrCurrentAgentName = null;
                    State.CurrentQuestorState = QuestorState.Start;
                    State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.CareerAgentGallente2MissionsComplete), true);
                    //why would this be necessary?!
                    ESCache.Instance.EveAccount.CareerAgentGallente2MissionsComplete = true;
                    AgentInteraction.boolNoMissionsAvailable = false;
                    return;
                }

                //Gallente Agents
                if (myAgent != null && myAgent.Name == constCareerAgentGallente3)
                {
                    Log.WriteLine("Marking Agent [" + myAgent.Name + "] complete as they have no more missions available to us.");
                    StrCurrentAgentName = null;
                    State.CurrentQuestorState = QuestorState.Start;
                    State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.CareerAgentGallente3MissionsComplete), true);
                    //why would this be necessary?!
                    ESCache.Instance.EveAccount.CareerAgentGallente3MissionsComplete = true;
                    AgentInteraction.boolNoMissionsAvailable = false;
                    return;
                }


            }
        }

        public static void PickAgentToUseNext()
        {
            Log.WriteLine("PickAgentToUseNext: Start");

            if (!ESCache.Instance.EveAccount.CareerAgentAmarr1MissionsComplete)
            {
                Log.WriteLine("PickAgentToUseNext: if (!ESCache.Instance.EveAccount.CareerAgentAmarr1MissionsComplete) use [" + constCareerAgentAmarr1 + "]");
                if (SwitchAgents("A1", constCareerAgentAmarr1, nameof(EveAccount.CareerAgentAmarr1MissionsComplete))) return;
            }

            if (!ESCache.Instance.EveAccount.CareerAgentAmarr2MissionsComplete)
            {
                Log.WriteLine("PickAgentToUseNext: if (!ESCache.Instance.EveAccount.CareerAgentAmarr2MissionsComplete) use [" + constCareerAgentAmarr2 + "]");
                if (SwitchAgents("A2", constCareerAgentAmarr2, nameof(EveAccount.CareerAgentAmarr2MissionsComplete))) return;
            }

            if (!ESCache.Instance.EveAccount.CareerAgentAmarr3MissionsComplete)
            {
                Log.WriteLine("PickAgentToUseNext: if (!ESCache.Instance.EveAccount.CareerAgentAmarr3MissionsComplete) use [" + constCareerAgentAmarr3 + "]");
                if (SwitchAgents("A3", constCareerAgentAmarr3, nameof(EveAccount.CareerAgentAmarr3MissionsComplete))) return;
            }

            if (!ESCache.Instance.EveAccount.CareerAgentCaldari1MissionsComplete)
            {
                Log.WriteLine("PickAgentToUseNext: if (!ESCache.Instance.EveAccount.CareerAgentCaldari1MissionsComplete) use [" + constCareerAgentCaldari1 + "]");
                if (SwitchAgents("C1", constCareerAgentCaldari1, nameof(EveAccount.CareerAgentCaldari1MissionsComplete))) return;
            }

            if (!ESCache.Instance.EveAccount.CareerAgentCaldari2MissionsComplete)
            {
                Log.WriteLine("PickAgentToUseNext: if (!ESCache.Instance.EveAccount.CareerAgentCaldari2MissionsComplete) use [" + constCareerAgentCaldari2 + "]");
                if (SwitchAgents("C2", constCareerAgentCaldari2, nameof(EveAccount.CareerAgentCaldari2MissionsComplete))) return;
            }

            if (!ESCache.Instance.EveAccount.CareerAgentCaldari3MissionsComplete)
            {
                Log.WriteLine("PickAgentToUseNext: if (!ESCache.Instance.EveAccount.CareerAgentCaldari3MissionsComplete) use [" + constCareerAgentCaldari3 + "]");
                if (SwitchAgents("C3", constCareerAgentCaldari3, nameof(EveAccount.CareerAgentCaldari3MissionsComplete))) return;
            }

            if (!ESCache.Instance.EveAccount.CareerAgentMinmatar1MissionsComplete)
            {
                Log.WriteLine("PickAgentToUseNext: if (!ESCache.Instance.EveAccount.CareerAgentMinmatar1MissionsComplete) use [" + constCareerAgentMinmatar1 + "]");
                if (SwitchAgents("M1", constCareerAgentMinmatar1, nameof(EveAccount.CareerAgentMinmatar1MissionsComplete))) return;
            }

            if (!ESCache.Instance.EveAccount.CareerAgentMinmatar2MissionsComplete)
            {
                Log.WriteLine("PickAgentToUseNext: if (!ESCache.Instance.EveAccount.CareerAgentMinmatar2MissionsComplete) use [" + constCareerAgentMinmatar2 + "]");
                if (SwitchAgents("M2", constCareerAgentMinmatar2, nameof(EveAccount.CareerAgentMinmatar2MissionsComplete))) return;
            }

            if (!ESCache.Instance.EveAccount.CareerAgentMinmatar3MissionsComplete)
            {
                Log.WriteLine("PickAgentToUseNext: if (!ESCache.Instance.EveAccount.CareerAgentMinmatar3MissionsComplete) use [" + constCareerAgentMinmatar3 + "]");
                if (SwitchAgents("M3", constCareerAgentMinmatar3, nameof(EveAccount.CareerAgentMinmatar3MissionsComplete))) return;
            }

            if (!ESCache.Instance.EveAccount.CareerAgentGallente1MissionsComplete)
            {
                Log.WriteLine("PickAgentToUseNext: if (!ESCache.Instance.EveAccount.CareerAgentGallente1MissionsComplete) use [" + constCareerAgentGallente1 + "]");
                if (SwitchAgents("G1", constCareerAgentGallente1, nameof(EveAccount.CareerAgentGallente1MissionsComplete))) return;
            }

            if (!ESCache.Instance.EveAccount.CareerAgentGallente2MissionsComplete)
            {
                Log.WriteLine("PickAgentToUseNext: if (!ESCache.Instance.EveAccount.CareerAgentGallente2MissionsComplete) use [" + constCareerAgentGallente2 + "]");
                if (SwitchAgents("G2", constCareerAgentGallente2, nameof(EveAccount.CareerAgentGallente2MissionsComplete))) return;
            }

            if (!ESCache.Instance.EveAccount.CareerAgentGallente3MissionsComplete)
            {
                Log.WriteLine("PickAgentToUseNext: if (!ESCache.Instance.EveAccount.CareerAgentGallente3MissionsComplete) use [" + constCareerAgentGallente3 + "]");
                if (SwitchAgents("G3", constCareerAgentGallente3, nameof(EveAccount.CareerAgentGallente3MissionsComplete))) return;
            }

            ControllerManager.Instance.SetPause(true);
            Log.WriteLine("Pausing: There are no more career agents left to process.");
        }

        public static void ClearMissionSpecificSettings()
        {
            try
            {
                StrCurrentAgentName = null;

                AgentToPullNextRegularMissionFrom = null;
                //if (State.CurrentStorylineState != StorylineState.Idle)
                //{
                //    CombatMissionsBehavior.ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true, null);
                //    _storylineMission = null;
                //    StorylineInstance.Reset();
                //}
                DirectUIModule._definedAmmoTypes = new List<AmmoType>();
                ClearFactionSpecificSettings();
                Defense.ClearPerMissionSettings();
                _myMission = null;
                _missionXmlPath = null;
                _siteXmlPath = null;
                _missionSpecificMissionFitting = null;
                MissionDronesKillHighValueTargets = null;
                MissionWarpAtDistanceRange = 0;
                MissionXMLIsAvailable = true;
                MissionDroneTypeID = null;
                MissionKillSentries = null;
                MissionUseDrones = null;
                MissionOrbitDistance = null;
                MissionOptimalRange = null;
                _factionFittingNameForThisMissionsFaction = null;
                FactionFittingForThisMissionsFaction = null;
                _fittingToLoad = null;
                LastReasonMissionAttemptedToBeDeclined = string.Empty;
                MinimumShieldPctMissionSetting = null;
                MinimumArmorPctMissionSetting = null;
                MinimumCapacitorPctMissionSetting = null;
                NavigateOnGrid.SpeedTankMissionSetting = null;
                MissionNumberOfCapBoostersToLoad = 0;
                MissionCapacitorInjectorScript = 0;
                MissionAllowOverLoadOfWeapons = false;
                MissionAllowOverLoadOfReps = false;
                ActionControl.IgnoreTargets.Clear();
                _modulesInAllInGameFittings = null;
                MissionSpecificShipName = string.Empty;
                MissionCompletionErrors = 0;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public static void ClearPerPocketCache()
        {
            ClearDamageTypeCache();
            _selectedControllerUsesCombatMissionsBehavior = null;
            PocketActivateRepairModulesAtThisPerc = null;
            PocketKillSentries = null;
            PocketUseDrones = null;
            DirectUIModule._definedAmmoTypes = new List<AmmoType>();
        }

        public static DirectAgentMissionBookmark GetMissionBookmark(DirectAgent myAgent, string startsWith)
        {
            try
            {
                if (myAgent == null)
                {
                    Log.WriteLine("GetMissionBookmark: myAgent == null");
                    return null;
                }

                if (string.IsNullOrEmpty(startsWith))
                {
                    Log.WriteLine("GetMissionBookmark: if (string.IsNullOrEmpty(startsWith))");
                    return null;
                }

                DirectAgentMission missionForBookmarkInfo = ESCache.Instance.DirectEve.AgentMissions.Find(m => m.Agent.AgentId == myAgent.AgentId);
                if (missionForBookmarkInfo == null)
                {
                    Log.WriteLine("missionForBookmarkInfo == null - No Mission found: myAgent [" + myAgent.Name + "] startswith [" + startsWith + "]");
                    int intMission = 0;
                    foreach (DirectAgentMission mission in ESCache.Instance.DirectEve.AgentMissions)
                    {
                        intMission++;
                        Log.WriteLine("[" + intMission + "][" + mission.Name + "][" + mission.Agent.Name + "] lvl [" + mission.Agent.Level + "] Type [" + mission.Type + "] Important [" + mission.Important + "]");
                    }

                    return null;
                }

                if (missionForBookmarkInfo.State != MissionState.Accepted)
                {
                    Log.WriteLine("missionForBookmarkInfo.State: [" + missionForBookmarkInfo.State + "]");
                    if (missionForBookmarkInfo.State != MissionState.Offered)
                    {
                        Log.WriteLine("MissionState is [" + MissionState.Offered + "] resetting AgentInteractionState to Idle to try to accept the offered mission! (this is a bug!)");
                        AgentInteraction.ChangeAgentInteractionState(AgentInteractionState.Idle, myAgent);
                        if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.QuestorController))
                            CombatMissionsBehavior.ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Idle, false, null);
                    }
                }

                if (missionForBookmarkInfo.Agent.AgentId != myAgent.AgentId)
                {
                    Log.WriteLine("missionForBookmarkInfo.Agent.Name: [" + missionForBookmarkInfo.Agent.Name + "] ID [" + missionForBookmarkInfo.Agent.AgentId + "] and myAgent [" + myAgent.Name + "][" + myAgent.AgentId + "]");
                    return null;
                }

                if (missionForBookmarkInfo.Bookmarks != null && missionForBookmarkInfo.Bookmarks.Any(b => b.Title.ToLower().StartsWith(startsWith.ToLower())))
                {
                    Log.WriteLine("MissionBookmark Found");
                    return missionForBookmarkInfo.Bookmarks.Find(b => b.Title.ToLower().StartsWith(startsWith.ToLower()));
                }

                if (ESCache.Instance.DirectEve.Bookmarks != null)
                {
                    ESCache.Instance._cachedBookmarks = ESCache.Instance.DirectEve.Bookmarks;
                    if (ESCache.Instance.CachedBookmarks.Any(b => b.Title.ToLower().StartsWith(startsWith.ToLower())))
                    {
                        Log.WriteLine("MissionBookmark From your Agent Not Found, but we did find a bookmark for a mission");
                        return (DirectAgentMissionBookmark)ESCache.Instance.CachedBookmarks.Find(b => b.Title.ToLower().StartsWith(startsWith.ToLower()));
                    }
                }

                Log.WriteLine("MissionBookmark From your Agent Not Found: and as a fall back we could not find any bookmark starting with [" + startsWith +
                              "] either... ");
                return null;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return null;
            }
        }

        public static void InvalidateCache()
        {
            _agentToPullNextRegularMissionFrom = null;
            _missionsInJournalFromNotBlacklistedAgents = null;
            _storylineMissionsInJournal = null;
            _storylineMissionsInJournalThatQuestorKnowsHowToDo = null;
            _storylineMissionsInJournalThatQuestorKnowsHowToDoNotBlacklisted = null;
        }

        public static bool IsFactionBlacklisted(DirectAgentMission myMission)
        {
            try
            {
                if (myMission == null) return false;

                if (myMission.Type.Contains("Trade"))
                    return false;

                if (myMission.Type.Contains("Courier"))
                    return false;

                if (myMission.Faction == null)
                    return false;

                if (FactionBlacklist.Any(m => m.ToLower().Contains(myMission.Faction.Name.ToLower())))
                    return true;

                return false;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return false;
            }
        }

        public static bool IsMissionBlacklisted(DirectAgentMission myMission)
        {
            if (myMission == null)
                return false;

            if (CourierMission(myMission))
                return false;

            if (MissionBlacklist == null)
                return false;

            if (MissionBlacklist.Count == 0)
                return false;

            if (MissionBlacklist != null && !string.IsNullOrEmpty(myMission.Name) && MissionBlacklist.Any(m => myMission.Name.ToLower().Contains(m.ToLower())))
                return true;

            foreach (string phrase in MissionObjectivePhraseBlacklist)
                if (!string.IsNullOrEmpty(phrase) && !string.IsNullOrWhiteSpace(phrase))
                    if (MyMission.Agent.AgentWindow.Objective.ToLower().Contains(phrase.ToLower()))
                    {
                        Log.WriteLine("IsMissionBlacklisted found phrase [" + phrase + "] in the mission objective.");
                        Log.WriteLine(myMission.Agent.AgentWindow.Objective);
                        Log.WriteLine("IsMissionBlacklisted found phrase [" + phrase + "] in the mission objective.");
                        return true;
                    }

            return false;
        }

        public static void LoadDistributionAgentsAllowedCorporations(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            try
            {
                DistributionAgentsAllowedCorporations.Clear();
                XElement xmlDistributionAgentsAllowedCorporationSection = CharacterSettingsXml.Element("distributionAgentsAllowedCorporations") ?? CommonSettingsXml.Element("distributionAgentsAllowedCorporations");
                if (xmlDistributionAgentsAllowedCorporationSection != null)
                {
                    Log.WriteLine("Loading DistributionAgents Allowed Corporation List");
                    int i = 1;
                    foreach (XElement xmlDistributionAgentsAllowedCorporation in xmlDistributionAgentsAllowedCorporationSection.Elements("distributionAgentsAllowedCorporation"))
                        if (DistributionAgentsAllowedCorporations.All(m => m != xmlDistributionAgentsAllowedCorporation.Value))
                        {
                            Log.WriteLine("   Any Corporation containing [" + Log.FilterPath(xmlDistributionAgentsAllowedCorporation.Value) + "] in the name will be included in the list of corporations we look for agents");
                            DistributionAgentsAllowedCorporations.Add(Log.FilterPath(xmlDistributionAgentsAllowedCorporation.Value));
                            i++;
                        }

                    Log.WriteLine("DistributionAgents Allowed Corporation List now has [" + MissionBlacklist.Count + "] entries");
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception: [" + ex + "]");
            }
        }

        public static void LoadFactionBlacklist(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            try
            {
                FactionBlacklist.Clear();
                XElement factionblacklist = CharacterSettingsXml.Element("factionblacklist") ?? CommonSettingsXml.Element("factionblacklist");
                if (factionblacklist != null)
                {
                    Log.WriteLine("Loading Faction Blacklist");
                    foreach (XElement faction in factionblacklist.Elements("faction"))
                    {
                        Log.WriteLine("        Missions against the faction [" + (string)faction + "] will be declined");
                        FactionBlacklist.Add((string)faction);
                    }

                    Log.WriteLine(" Faction Blacklist now has [" + FactionBlacklist.Count + "] entries");
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception: [" + ex + "]");
            }
        }

        public static void LoadMissionBlackList(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            try
            {
                MissionBlacklist.Clear();
                XElement xmlElementBlackListSection = CharacterSettingsXml.Element("blacklist") ?? CommonSettingsXml.Element("blacklist");
                if (xmlElementBlackListSection != null)
                {
                    Log.WriteLine("Loading Mission Blacklist");
                    int i = 1;
                    foreach (XElement xmlBlacklistedMission in xmlElementBlackListSection.Elements("mission"))
                        if (MissionBlacklist.All(m => m != xmlBlacklistedMission.Value))
                        {
                            Log.WriteLine("   Any Mission containing [" + Log.FilterPath(xmlBlacklistedMission.Value) + "] in the name will be declined");
                            MissionBlacklist.Add(Log.FilterPath(xmlBlacklistedMission.Value));
                            i++;
                        }

                    Log.WriteLine("Mission Blacklist now has [" + MissionBlacklist.Count + "] entries");
                }

                MissionObjectivePhraseBlacklist.Clear();
                XElement xmlElementObjectivePhraseBlackListSection = CharacterSettingsXml.Element("objectivePhraseBlacklist") ?? CommonSettingsXml.Element("objectivePhraseBlacklist");
                if (xmlElementObjectivePhraseBlackListSection != null)
                {
                    Log.WriteLine("Loading Mission Objective Phrase Blacklist");
                    int i = 1;
                    foreach (XElement xmlPhrase in xmlElementObjectivePhraseBlackListSection.Elements("phraseToNotAllow"))
                        if (MissionObjectivePhraseBlacklist.All(m => m != xmlPhrase.Value && !string.IsNullOrEmpty(xmlPhrase.Value) && !string.IsNullOrWhiteSpace(xmlPhrase.Value)))
                        {
                            Log.WriteLine("   Any Mission containing [" + Log.FilterPath(xmlPhrase.Value) + "] in the objective will be declined");
                            MissionObjectivePhraseBlacklist.Add(Log.FilterPath(xmlPhrase.Value));
                            i++;
                        }

                    Log.WriteLine("Mission Objective Phrase Blacklist now has [" + MissionObjectivePhraseBlacklist.Count + "] entries");
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception: [" + ex + "]");
            }
        }

        public static void LoadMissionGreyList(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            try
            {
                MissionGreylist.Clear();
                XElement xmlElementGreyListSection = CharacterSettingsXml.Element("greylist") ?? CommonSettingsXml.Element("greylist");

                if (xmlElementGreyListSection != null)
                {
                    Log.WriteLine("Loading Mission GreyList");
                    int i = 1;
                    foreach (XElement GreylistedMission in xmlElementGreyListSection.Elements("mission"))
                    {
                        Log.WriteLine("   Any Mission containing [" + Log.FilterPath(GreylistedMission.Value) + "] in the name will be declined if our standings are high enough");
                        MissionGreylist.Add(Log.FilterPath((string)GreylistedMission));
                        i++;
                    }
                    Log.WriteLine("        Mission GreyList now has [" + MissionGreylist.Count + "] entries");
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception: [" + ex + "]");
            }
        }

        public static void LoadMissionXmlData(DirectAgentMission myMission)
        {
            Log.WriteLine("Loading mission xml [" + myMission.Name + "] from [" + MissionXmlPath(myMission) + "]");
            ClearMissionSpecificSettings();

            try
            {
                MissionXml = XDocument.Load(MissionXmlPath(myMission));

                if (MissionXml.Root != null)
                {
                    Defense.LoadMissionXmlData(MissionXml);

                    IEnumerable<string> items =
                        ((IEnumerable)
                            MissionXml.XPathEvaluate(
                                "//action[(translate(@name, 'LOT', 'lot')='loot') or (translate(@name, 'LOTIEM', 'lotiem')='lootitem')]/parameter[translate(@name, 'TIEM', 'tiem')='item']/@value")
                        )
                        .Cast<XAttribute>()
                        .Select(a => ((string)a ?? string.Empty).ToLower());
                    MissionItems.AddRange(items);

                    MissionCapacitorInjectorScript = (int?)MissionXml.Root.Element("capacitorInjectorScript") ?? null;
                    MissionNumberOfCapBoostersToLoad = (int?)MissionXml.Root.Element("numberOfCapBoostersToLoad") ?? (int?)MissionXml.Root.Element("capacitorInjectorToLoad") ?? null;
                    MissionUseDrones = (bool?)MissionXml.Root.Element("useDrones") ?? (bool?)MissionXml.Root.Element("UseDrones") ?? (bool?)MissionXml.Root.Element("usedrones") ?? (bool?)MissionXml.Root.Element("missionUseDrones") ?? null;
                    MissionDronesKillHighValueTargets = (bool?)MissionXml.Root.Element("dronesKillHighValueTargets") ?? null;
                    MissionKillSentries = (bool?)MissionXml.Root.Element("killSentries") ?? null;
                    MissionWarpAtDistanceRange = (int?)MissionXml.Root.Element("missionWarpAtDistanceRange") ?? 0;
                    MissionDroneTypeID = (int?)MissionXml.Root.Element("droneTypeId") ?? (int?)MissionXml.Root.Element("DroneTypeId") ?? null;
                    MissionSpecificShipName = (string)MissionXml.Root.Element("missionSpecificShipName") ?? string.Empty;
                    MissionAllowOverLoadOfWeapons = (bool?)MissionXml.Root.Element("missionAllowOverLoadOfWeapons") ?? false;
                    MissionAllowOverLoadOfEcm = (bool?)MissionXml.Root.Element("missionAllowOverLoadOfEcm") ?? false;
                    MissionAllowOverLoadOfSpeedMod = (bool?)MissionXml.Root.Element("missionAllowOverLoadOfSpeedMod") ?? false;
                    MissionAllowOverLoadOfWebs = (bool?)MissionXml.Root.Element("missionAllowOverLoadOfWebs") ?? false;
                    MissionAllowOverLoadOfReps = (bool?)MissionXml.Root.Element("missionAllowOverLoadOfReps") ?? false;
                    NavigateOnGrid.SpeedTankMissionSetting = (bool?)MissionXml.Root.Element("speedTank") ?? (bool?)MissionXml.Root.Element("speedtank") ?? null;
                    MissionOrbitDistance = (double?)MissionXml.Root.Element("orbitdistance") ?? (double?)MissionXml.Root.Element("orbitDistance") ?? null;
                    MissionOptimalRange = (double?)MissionXml.Root.Element("optimalrange") ?? (double?)MissionXml.Root.Element("optimalRange") ?? null;
                    MinimumShieldPctMissionSetting = (int?)MissionXml.Root.Element("minimumShieldPct") ?? null;
                    MinimumArmorPctMissionSetting = (int?)MissionXml.Root.Element("minimumArmorPct") ?? null;
                    MinimumCapacitorPctMissionSetting = (int?)MissionXml.Root.Element("minimumCapacitorPct") ?? null;
                    MissionAlwaysActivateSpeedMod = (bool?)MissionXml.Root.Element("missionAlwaysActivateSpeedMod") ?? false;
                    MissionInjectCapPerc = (int?)MissionXml.Root.Element("missionInjectCapPerc") ?? null;
                    MissionTooCloseToStructure = (int?)MissionXml.Root.Element("missionTooCloseToStructure") ?? null;
                    MissionSafeDistanceFromStructure = (int?)MissionXml.Root.Element("missionSafeDistanceFromStructure") ?? null;
                    MissionWeaponOverloadDamageAllowed = (int?)MissionXml.Root.Element("missionWeaponOverloadDamageAllowed") ?? null;
                    MissionEcmOverloadDamageAllowed = (int?)MissionXml.Root.Element("missionEcmOverloadDamageAllowed") ?? null;
                    MissionSpeedModOverloadDamageAllowed = (int?)MissionXml.Root.Element("missionSpeedModOverloadDamageAllowed") ?? null;
                    MissionWebOverloadDamageAllowed = (int?)MissionXml.Root.Element("missionWebOverloadDamageAllowed") ?? null;
                    MissionLootEverything = (bool?)MissionXml.Root.Element("missionLootEverything") ?? null;

                    MoveMissionItems = (string)MissionXml.Root.Element("bring") ?? string.Empty;
                    MoveMissionItems = MoveMissionItems.ToLower();
                    if (!string.IsNullOrEmpty(MoveMissionItems))
                        Log.WriteLine("bring XML [" + MissionXml.Root.Element("bring") + "] BringMissionItem [" + MoveMissionItems + "]");

                    MoveMissionItemsQuantity = (int?)MissionXml.Root.Element("bringquantity") ?? 1;
                    if (MoveMissionItemsQuantity > 0)
                        Log.WriteLine("bringquantity XML [" + MissionXml.Root.Element("bringquantity") + "] BringMissionItemQuantity [" + MoveMissionItemsQuantity + "]");

                    MoveOptionalMissionItems = (string)MissionXml.Root.Element("trytobring") ?? string.Empty;
                    MoveOptionalMissionItems = MoveOptionalMissionItems.ToLower();
                    if (!string.IsNullOrEmpty(MoveOptionalMissionItems))
                        Log.WriteLine("trytobring XML [" + MissionXml.Root.Element("trytobring") + "] BringOptionalMissionItem [" + MoveOptionalMissionItems + "]");

                    MoveOptionalMissionItemQuantity = (int?)MissionXml.Root.Element("trytobringquantity") ?? 1;
                    if (MoveOptionalMissionItemQuantity > 0)
                        Log.WriteLine("trytobringquantity XML [" + MissionXml.Root.Element("trytobringquantity") + "] BringOptionalMissionItemQuantity [" + MoveOptionalMissionItemQuantity + "]");

                    try
                    {
                        MissionBoosterTypes = new HashSet<long>();
                        XElement missionBoostersToInjectXml = MissionXml.Root.Element("missionBoosterTypes") ?? MissionXml.Root.Element("boosterTypes");

                        if (missionBoostersToInjectXml != null)
                            foreach (XElement xmlBoosterToInject in missionBoostersToInjectXml.Elements("boosterType"))
                                if (xmlBoosterToInject.Value != "0")
                                {
                                    long booster = int.Parse(xmlBoosterToInject.Value);
                                    DirectInvType boosterInvType = ESCache.Instance.DirectEve.GetInvType(int.Parse(xmlBoosterToInject.Value));
                                    Log.WriteLine("Adding booster [" + boosterInvType.TypeName + "] to the list of boosters that will attempt to be injected during arm.");
                                    MissionBoosterTypes.Add(booster);
                                }
                    }
                    catch (Exception exception)
                    {
                        Log.WriteLine("Error Loading Booster Settings [" + exception + "]");
                    }

                    if (MissionSpecificMissionFitting(myMission) != null)
                        Log.WriteLine("MissionSettings.MissionSpecificFitting.Ship is [" + MissionSpecificMissionFitting(myMission).ShipName.ToLower() + "]");
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Error in mission (not pocket) specific XML tags [" + myMission.Name + "], " + ex);
            }
            finally
            {
                MissionXml = null;
            }
        }

        public static void LoadSettings(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            //
            // Agent Standings and Mission Settings
            //
            try
            {
                Log.WriteLine("MissionSettings");

                Settings.Instance.EnableStorylines = (bool?)CharacterSettingsXml.Element("enableStorylines") ??
                                       (bool?)CommonSettingsXml.Element("enableStorylines") ?? false;
                Log.WriteLine("Settings: enableStorylines [" + Settings.Instance.EnableStorylines + "]");
                Settings.Instance.StorylineDoNotTryToDoEncounterMissions = (bool?)CharacterSettingsXml.Element("storylineDoNotTryToDoEncounterMissions") ??
                                                         (bool?)CommonSettingsXml.Element("storylineDoNotTryToDoEncounterMissions") ?? false;
                Log.WriteLine("Settings: storylineDoNotTryToDoEncounterMissions [" + Settings.Instance.StorylineDoNotTryToDoEncounterMissions + "]");
                Settings.Instance.StorylineDoNotTryToDoCourierMissions = (bool?)CharacterSettingsXml.Element("storylineDoNotTryToDoCourierMissions") ??
                                                       (bool?)CommonSettingsXml.Element("storylineDoNotTryToDoCourierMissions") ?? false;
                Log.WriteLine("Settings: storylineDoNotTryToDoCourierMissions [" + Settings.Instance.StorylineDoNotTryToDoCourierMissions + "]");
                Settings.Instance.StorylineDoNotTryToDoTradeMissions = (bool?)CharacterSettingsXml.Element("storylineDoNotTryToDoTradeMissions") ??
                                                     (bool?)CommonSettingsXml.Element("storylineDoNotTryToDoTradeMissions") ?? false;
                Settings.Instance.DoNotTryToDoEncounterMissions = (bool?)CharacterSettingsXml.Element("doNotTryToDoEncounterMissions") ??
                                                     (bool?)CommonSettingsXml.Element("doNotTryToDoEncounterMissions") ?? false;
                Log.WriteLine("Settings: storylineDoNotTryToDoTradeMissions [" + Settings.Instance.StorylineDoNotTryToDoTradeMissions + "]");
                Settings.Instance.StorylineDoNotTryToDoMiningMissions = (bool?)CharacterSettingsXml.Element("storylineDoNotTryToDoMiningMissions") ??
                                                      (bool?)CommonSettingsXml.Element("storylineDoNotTryToDoMiningMissions") ?? false;
                Log.WriteLine("Settings: storylineDoNotTryToDoMiningMissions [" + Settings.Instance.StorylineDoNotTryToDoMiningMissions + "]");
                Settings.Instance.StoryLineBaseBookmark = (string)CharacterSettingsXml.Element("storyLineBaseBookmark") ??
                                        (string)CommonSettingsXml.Element("storyLineBaseBookmark") ?? string.Empty;


                MinAgentGreyListStandings = (float?)CharacterSettingsXml.Element("minAgentGreyListStandings") ??
                                            (float?)CommonSettingsXml.Element("minAgentGreyListStandings") ?? (float)0.0;
                Log.WriteLine("MissionSettings: MinAgentGreyListStandings [" + MinAgentGreyListStandings + "]");
                string relativeMissionsPath = (string)CharacterSettingsXml.Element("missionsPath") ??
                                              (string)CommonSettingsXml.Element("missionsPath") ?? "Caldari L4";
                Log.WriteLine("MissionSettings: relativeMissionsPath [" + relativeMissionsPath + "]");
                MissionsPath = Path.Combine(Settings.Instance.Path, "QuestorMissions", relativeMissionsPath);
                Log.WriteLine("MissionSettings: MissionsPath [" + MissionsPath + "]");

                RequireMissionXML = (bool?)CharacterSettingsXml.Element("requireMissionXML") ??
                                    (bool?)CommonSettingsXml.Element("requireMissionXML") ?? false;
                Log.WriteLine("MissionSettings: RequireMissionXML [" + RequireMissionXML + "]");
                DeclineMissionsWithTooManyMissionCompletionErrors =
                    (bool?)CharacterSettingsXml.Element("DeclineMissionsWithTooManyMissionCompletionErrors") ??
                    (bool?)CommonSettingsXml.Element("DeclineMissionsWithTooManyMissionCompletionErrors") ?? false;
                Log.WriteLine("MissionSettings: DeclineMissionsWithTooManyMissionCompletionErrors [" + DeclineMissionsWithTooManyMissionCompletionErrors + "]");
                AllowNonStorylineCourierMissionsInLowSec = (bool?)CharacterSettingsXml.Element("LowSecMissions") ??
                                                           (bool?)CommonSettingsXml.Element("LowSecMissions") ?? false;
                Log.WriteLine("MissionSettings: AllowNonStorylineCourierMissionsInLowSec [" + AllowNonStorylineCourierMissionsInLowSec + "]");
                MaterialsForWarOreID = (int?)CharacterSettingsXml.Element("MaterialsForWarOreID") ??
                                       (int?)CommonSettingsXml.Element("MaterialsForWarOreID") ?? 20;
                Log.WriteLine("MissionSettings: MaterialsForWarOreID [" + MaterialsForWarOreID + "]");
                MaterialsForWarOreQty = (int?)CharacterSettingsXml.Element("MaterialsForWarOreQty") ??
                                        (int?)CommonSettingsXml.Element("MaterialsForWarOreQty") ?? 8000;
                Log.WriteLine("MissionSettings: MaterialsForWarOreQty [" + MaterialsForWarOreQty + "]");
                AllowRemovingNoCompatibleStorylines = (bool?)CharacterSettingsXml.Element("AllowRemovingNoCompatibleStorylines") ??
                                                      (bool?)CommonSettingsXml.Element("AllowRemovingNoCompatibleStorylines") ?? true;
                Log.WriteLine("MissionSettings: AllowRemovingNoCompatibleStorylines [" + AllowRemovingNoCompatibleStorylines + "]");

                //
                // Loading Mission Blacklists/GreyLists
                //
                try
                {
                    LoadMissionBlackList(CharacterSettingsXml, CommonSettingsXml);
                    LoadMissionGreyList(CharacterSettingsXml, CommonSettingsXml);
                    LoadFactionBlacklist(CharacterSettingsXml, CommonSettingsXml);
                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CourierMissionsController))
                        LoadDistributionAgentsAllowedCorporations(CharacterSettingsXml, CommonSettingsXml);
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Error Loading Mission Blacklists/GreyLists [" + exception + "]");
                }

                try
                {
                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CourierMissionsController))
                        LoadDistributionAgentsAllowedCorporations(CharacterSettingsXml, CommonSettingsXml);
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Error Loading DistributionAgentsAllowedCorporations [" + exception + "]");
                }

                //
                // List of Agents we should use
                //
                try
                {
                    ListOfAgents = new List<AgentsList>();
                    XElement agentList = CharacterSettingsXml.Element("agentsList") ?? CommonSettingsXml.Element("agentsList");

                    if (agentList != null)
                    {
                        if (agentList.HasElements)
                        {
                            foreach (XElement agent in agentList.Elements("agentList"))
                            {
                                Log.WriteLine("Add agent XElement [" + agent + "]");
                                ListOfAgents.Add(new AgentsList(agent));
                            }
                        }
                        else
                        {
                            Log.WriteLine("agentList exists in your characters config but no agents were listed.");
                        }
                    }
                    else
                    {
                        Log.WriteLine("Error! No Agents List specified.");
                    }
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Error Loading Agent Settings [" + exception + "]");
                }
            }
            catch (Exception exception)
            {
                Log.WriteLine("Error Loading Agent Standings and Mission Settings: Exception [" + exception + "]");
            }
        }

        public static long? M3NeededForCargo(DirectAgentMission myMission)
        {
            if (myMission.Agent.AgentWindow == null) return null;

            System.Text.RegularExpressions.Regex m3Regex = new System.Text.RegularExpressions.Regex(@"([0-9]+)((\.([0-9]+))*) m", System.Text.RegularExpressions.RegexOptions.Compiled);
            int m3 = 0;
            foreach (System.Text.RegularExpressions.Match itemMatch in m3Regex.Matches(myMission.Agent.AgentWindow.Objective))
            {
                const string thousandsSeperator = ",";
                int.TryParse(System.Text.RegularExpressions.Regex.Match(itemMatch.Value.Replace(thousandsSeperator, ""), @"\d+").Value, out m3);
            }

            return m3;
        }

        public static void ResetInStationSettingsWhenExitingStation()
        {
            MissionNameforLogging = "none";
            if (MyMission != null)
                MissionNameforLogging = MyMission.Name;
        }

        public static bool ThisMissionIsNotWorthSalvaging(DirectAgentMission myMission)
        {
            if (myMission.Name != null)
            {
                if (myMission.Name.ToLower().Contains("Attack of the Drones".ToLower()))
                {
                    Log.WriteLine("Do not salvage a drones mission as they are crap now");
                    return true;
                }

                if (myMission.Name.ToLower().Contains("Infiltrated Outposts".ToLower()))
                {
                    Log.WriteLine("Do not salvage a drones mission as they are crap now");
                    return true;
                }

                if (myMission.Name.ToLower().Contains("Rogue Drone Harassment".ToLower()))
                {
                    Log.WriteLine("Do not salvage a drones mission as they are crap now");
                    return true;
                }

                return false;
            }

            return false;
        }

        private static MissionFitting LookForFitting(string lookForFittingNamed)
        {
            if (DebugConfig.DebugFittingMgr)
                Log.WriteLine("MissionSpecificMissionFitting: looking for a (filtered) fitting matching [" + lookForFittingNamed.ToLower() + "]");

            if (ListOfMissionFittings.Any(i => Log.FilterPath(i.MissionName.ToLower()) == lookForFittingNamed.ToLower()))
            {
                _missionSpecificMissionFitting = ListOfMissionFittings.Find(i => Log.FilterPath(i.MissionName.ToLower()) == lookForFittingNamed.ToLower());
                if (_missionSpecificMissionFitting != null)
                {
                    Log.WriteLine("MissionSpecificMissionFitting [" + _missionSpecificMissionFitting.ShipName.ToLower() + "] MissionName [" + _missionSpecificMissionFitting.MissionName.ToLower() + "] FittingName [" + _missionSpecificMissionFitting.FittingName.ToLower() + "]");
                    return _missionSpecificMissionFitting;
                }

                return null;
            }

            return null;
        }

        #endregion Methods
    }
}