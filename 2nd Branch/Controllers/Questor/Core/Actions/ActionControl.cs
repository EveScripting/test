extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using EVESharpCore.Questor.Stats;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Action = EVESharpCore.Questor.Actions.Base.Action;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Framework.Events;
using SC::SharedComponents.Events;
using EVESharpCore.Framework.Lookup;

namespace EVESharpCore.Questor.Actions
{
    public static partial class ActionControl
    {
        #region Constructors

        #endregion Constructors

        #region Fields

        public static bool DeactivateIfNothingTargetedWithinRange;
        public static bool NextActionBool;
        private static int _currentActionNumber { get; set; }
        private static List<Action> _pocketActions  = new List<Action>();
        private static DateTime? _clearPocketTimeout;

        private static int _doneActionAttempts;

        private static DateTime _startedPocket = DateTime.UtcNow;
        private static DateTime _nextCombatMissionCtrlAction = DateTime.UtcNow;

        private static bool _waiting;

        private static DateTime _waitingSince;

        private static bool CargoHoldHasBeenStacked;

        private static bool ItemsHaveBeenMoved;

        private static string CurrentMissionHint(DirectAgent myAgent)
        {
            if (myAgent != null)
                return myAgent.Mission.GetAgentMissionRawCsvHint();

            return null;
        }

        public static bool PerformingClearPocketNow
        {
            get
            {
                try
                {
                    if (_pocketActions[_currentActionNumber].State == ActionState.ClearPocket)
                        return true;

                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public static bool PerformingLootActionNow
        {
            get
            {
                try
                {
                    if (_pocketActions[_currentActionNumber].State == ActionState.Loot)
                        return true;

                    if (_pocketActions[_currentActionNumber].State == ActionState.LootFactionOnly)
                        return true;

                    if (_pocketActions[_currentActionNumber].State == ActionState.LootItem)
                        return true;

                    if (_pocketActions[_currentActionNumber].State == ActionState.AbyssalLoot)
                        return true;

                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        //
        // 0 being the action, this being the 1st paramaters after the action
        //
        private static string CurrentMissionHint1stParameter(DirectAgent myAgent)
        {
            if (string.IsNullOrEmpty(CurrentMissionHint(myAgent)))
                return null;

            IEnumerable<string> query = from val in CurrentMissionHint(myAgent).Split(',').Select(sValue => sValue.Trim()) select val;
            if (query.Any() && query.ElementAt(1) != null)
                return query.ElementAt(1);

            return null;
        }

        //
        // 0 being the action, 1st being the paramaters after the action, this being the 2nd paramater
        //
        private static string CurrentMissionHint2ndParameter(DirectAgent myAgent)
        {
            if (string.IsNullOrEmpty(CurrentMissionHint(myAgent)))
                return null;

            IEnumerable<string> query = from val in CurrentMissionHint(myAgent).Split(',').Select(sValue => sValue.Trim()) select val;
            if (query.Any() && query.ElementAt(2) != null)
                return query.ElementAt(2);

            return null;
        }

        private static string CurrentMissionHintActionNeeded(DirectAgent myAgent)
        {
            if (string.IsNullOrEmpty(CurrentMissionHint(myAgent)))
                return null;

            IEnumerable<string> query = from val in CurrentMissionHint(myAgent).Split(',').Select(sValue => sValue.Trim()) select val;
            if (query.Any())
                return query.FirstOrDefault();

            return null;
        }

        #endregion Fields

        #region Properties

        /// <summary>
        ///     List of targets to ignore
        /// </summary>
        public static HashSet<string> IgnoreTargets { get; private set; } = new HashSet<string>();

        public static int PocketNumber { get; set; }

        #endregion Properties

        #region Methods

        public static bool ChangeCombatMissionCtrlState(ActionControlState state, DirectAgentMission myMission, DirectAgent myAgent, bool wait = false)
        {
            try
            {
                if (State.CurrentCombatMissionCtrlState != state)
                {
                    Log.WriteLine("New CombatMissionCtrlState [" + state + "]");
                    State.CurrentCombatMissionCtrlState = state;
                    if (wait) return true;

                    if (myMission != null && myAgent != null)
                        ProcessState(myMission, myAgent);

                    if (ESCache.Instance.InAbyssalDeadspace)
                        ProcessState(null, null);

                    return true;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        /// <summary>
        ///     Loads mission objectives from XML file
        /// </summary>
        /// <param name="agentId"> </param>
        /// <param name="pocketId"> </param>
        /// <param name="missionMode"> </param>
        /// <returns></returns>
        public static IEnumerable<Action> LoadMissionActions(DirectAgentMission myMission, int pocketId, bool missionMode)
        {
            try
            {
                if (!MissionSettings.SelectedControllerUsesCombatMissionsBehavior)
                    return new List<Action>();

                if (myMission == null && missionMode)
                    return new List<Action>();

                if (myMission != null)
                {
                    if (!File.Exists(MissionSettings.MissionXmlPath(myMission)))
                    {
                        Log.WriteLine("No mission file found:  but we need to set some cache settings");
                        MissionSettings.MissionOrbitDistance = null;
                        MissionSettings.MissionOptimalRange = null;
                        MissionSettings.MissionUseDrones = null;
                        ESCache.Instance.AfterMissionSalvaging = Salvage.AfterMissionSalvaging;
                        return new List<Action>();
                    }

                    //
                    // this loads the settings from each pocket... but NOT any settings global to the mission
                    //
                    try
                    {
                        XDocument xdoc = XDocument.Load(MissionSettings.MissionXmlPath(myMission));
                        if (xdoc.Root != null)
                        {
                            Log.WriteLine("Loaded Mission XML actions from [" + MissionSettings.MissionXmlPath(myMission) + "].");
                            XElement xElement = xdoc.Root.Element("pockets");
                            if (xElement != null)
                            {
                                IEnumerable<XElement> pockets = xElement.Elements("pocket");
                                foreach (XElement pocket in pockets)
                                    try
                                    {
                                        if ((int)pocket.Attribute("id") != pocketId)
                                            continue;

                                        if (pocket.Element("orbitentitynamed") != null)
                                            ESCache.Instance.OrbitEntityNamed = (string)pocket.Element("orbitentitynamed");

                                        if (pocket.Element("orbitdistance") != null) //Load OrbitDistance from mission.xml, if present
                                        {
                                            MissionSettings.MissionOrbitDistance = (double?)pocket.Element("orbitdistance") ?? null;
                                            Log.WriteLine("Using Mission Orbit distance [" + NavigateOnGrid.OrbitDistance + "]");
                                        }

                                        if (pocket.Element("optimalrange") != null) //Load OrbitDistance from mission.xml, if present
                                        {
                                            MissionSettings.MissionOptimalRange = (double?)pocket.Element("optimalrange") ?? null;
                                            Log.WriteLine("Using Mission OptimalRange [" + NavigateOnGrid.OptimalRange + "]");
                                        }

                                        if (pocket.Element("afterMissionSalvaging") != null) //Load afterMissionSalvaging setting from mission.xml, if present
                                            ESCache.Instance.AfterMissionSalvaging = (bool)pocket.Element("afterMissionSalvaging");

                                        if (pocket.Element("dronesKillHighValueTargets") != null) //Load afterMissionSalvaging setting from mission.xml, if present
                                            MissionSettings.MissionDronesKillHighValueTargets = (bool)pocket.Element("dronesKillHighValueTargets");

                                        try
                                        {
                                            List<Action> actions = new List<Action>();
                                            XElement elements = pocket.Element("actions");
                                            if (elements != null)
                                                foreach (XElement element in elements.Elements("action"))
                                                    try
                                                    {
                                                        Action action = new Action
                                                        {
                                                            State = (ActionState)Enum.Parse(typeof(ActionState), (string)element.Attribute("name"), true)
                                                        };
                                                        XAttribute xAttribute = element.Attribute("name");
                                                        if (xAttribute != null && xAttribute.Value == "ClearPocket")
                                                        {
                                                            action.AddParameter("", "");
                                                        }
                                                        else
                                                        {
                                                            foreach (XElement parameter in element.Elements("parameter"))
                                                                try
                                                                {
                                                                    action.AddParameter((string)parameter.Attribute("name"), (string)parameter.Attribute("value"));
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    Log.WriteLine("Exception [" + ex + "]");
                                                                }
                                                        }

                                                        actions.Add(action);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Log.WriteLine("Exception [" + ex + "]");
                                                    }

                                            return actions;
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.WriteLine("Exception [" + ex + "]");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.WriteLine("Exception [" + ex + "]");
                                    }
                            }
                            else
                            {
                                return new List<Action>();
                            }
                        }
                        else
                        {
                            return new List<Action>();
                        }

                        // if we reach this code there is no mission XML file, so we set some things -- Assail

                        MissionSettings.MissionOptimalRange = null;
                        MissionSettings.MissionOrbitDistance = null;
                        Log.WriteLine("Using Settings Orbit distance [" + NavigateOnGrid.OrbitDistance + "]");

                        return new List<Action>();
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Error loading mission XML file [" + ex.Message + "]");
                        return new List<Action>();
                    }
                }

                return new List<Action>();
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return new List<Action>();
            }
        }

        public static IEnumerable<Action> LoadSiteActions(DirectSystemScanResult myDirectSystemScanResult, int pocketId)
        {
            try
            {
                if (myDirectSystemScanResult != null && myDirectSystemScanResult.GroupName == "Combat Site")
                {
                    string mySiteXmlPath = MissionSettings.SiteXmlPath(myDirectSystemScanResult);
                    if (!File.Exists(mySiteXmlPath))
                    {
                        Log.WriteLine("No site file found: we need to set some cache settings");
                        MissionSettings.MissionOrbitDistance = null;
                        MissionSettings.MissionOptimalRange = null;
                        MissionSettings.MissionUseDrones = null;
                        ESCache.Instance.AfterMissionSalvaging = Salvage.AfterMissionSalvaging;
                        return new List<Action>();
                    }

                    //
                    // this loads the settings from each pocket... but NOT any settings global to the mission
                    //
                    try
                    {
                        XDocument xdoc = XDocument.Load(mySiteXmlPath);
                        if (xdoc.Root != null)
                        {
                            Log.WriteLine("Loaded Mission XML actions from [" + mySiteXmlPath + "].");
                            XElement xElement = xdoc.Root.Element("pockets");
                            if (xElement != null)
                            {
                                IEnumerable<XElement> pockets = xElement.Elements("pocket");
                                foreach (XElement pocket in pockets)
                                    try
                                    {
                                        if ((int)pocket.Attribute("id") != pocketId)
                                            continue;

                                        if (pocket.Element("orbitentitynamed") != null)
                                            ESCache.Instance.OrbitEntityNamed = (string)pocket.Element("orbitentitynamed");

                                        if (pocket.Element("orbitdistance") != null) //Load OrbitDistance from mission.xml, if present
                                        {
                                            MissionSettings.MissionOrbitDistance = (double?)pocket.Element("orbitdistance") ?? null;
                                            Log.WriteLine("Using Mission Orbit distance [" + NavigateOnGrid.OrbitDistance + "]");
                                        }

                                        if (pocket.Element("optimalrange") != null) //Load OrbitDistance from mission.xml, if present
                                        {
                                            MissionSettings.MissionOptimalRange = (double?)pocket.Element("optimalrange") ?? null;
                                            Log.WriteLine("Using Mission OptimalRange [" + NavigateOnGrid.OptimalRange + "]");
                                        }

                                        if (pocket.Element("afterMissionSalvaging") != null) //Load afterMissionSalvaging setting from mission.xml, if present
                                            ESCache.Instance.AfterMissionSalvaging = (bool)pocket.Element("afterMissionSalvaging");

                                        if (pocket.Element("dronesKillHighValueTargets") != null) //Load afterMissionSalvaging setting from mission.xml, if present
                                            MissionSettings.MissionDronesKillHighValueTargets = (bool)pocket.Element("dronesKillHighValueTargets");

                                        try
                                        {
                                            List<Action> actions = new List<Action>();
                                            XElement elements = pocket.Element("actions");
                                            if (elements != null)
                                            {
                                                foreach (XElement element in elements.Elements("action"))
                                                {
                                                    try
                                                    {
                                                        Action action = new Action
                                                        {
                                                            State = (ActionState)Enum.Parse(typeof(ActionState), (string)element.Attribute("name"), true)
                                                        };
                                                        XAttribute xAttribute = element.Attribute("name");
                                                        if (xAttribute != null && xAttribute.Value == "ClearPocket")
                                                        {
                                                            action.AddParameter("", "");
                                                        }
                                                        else
                                                        {
                                                            foreach (XElement parameter in element.Elements("parameter"))
                                                                try
                                                                {
                                                                    action.AddParameter((string)parameter.Attribute("name"), (string)parameter.Attribute("value"));
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    Log.WriteLine("Exception [" + ex + "]");
                                                                }
                                                        }

                                                        actions.Add(action);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Log.WriteLine("Exception [" + ex + "]");
                                                    }
                                                }
                                            }

                                            return actions;
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.WriteLine("Exception [" + ex + "]");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.WriteLine("Exception [" + ex + "]");
                                    }
                            }
                            else
                            {
                                return new List<Action>();
                            }
                        }
                        else
                        {
                            return new List<Action>();
                        }

                        // if we reach this code there is no mission XML file, so we set some things -- Assail

                        MissionSettings.MissionOptimalRange = null;
                        MissionSettings.MissionOrbitDistance = null;
                        Log.WriteLine("Using Settings Orbit distance [" + NavigateOnGrid.OrbitDistance + "]");

                        return new List<Action>();
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Error loading mission XML file [" + ex.Message + "]");
                        return new List<Action>();
                    }
                }

                return new List<Action>();
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return new List<Action>();
            }
        }

        public static void LogCurrentMissionActions(DirectAgentMission myMission, DirectAgent myAgent)
        {
            try
            {
                if (myMission == null || myAgent == null)
                    return;

                int pocketActionNumber = 0;
                Log.WriteLine("Mission Timer At: [" + Math.Round(DateTime.UtcNow.Subtract(Statistics.StartedMission).TotalMinutes, 0) + "] min. Max Range is currently: " + (Combat.Combat.MaxRange / 1000).ToString(CultureInfo.InvariantCulture) + "k");
                if (_pocketActions != null && _pocketActions.Count > 0)
                {
                    Log.WriteLine("CurrentAction: Pocket[" + PocketNumber + "] #[" + _currentActionNumber + "]");
                    foreach (Action pocketAction in _pocketActions)
                    {
                        pocketActionNumber++;
                        Log.WriteLine("[" + pocketActionNumber + "][" + pocketAction + "]");
                    }
                }

                if (!string.IsNullOrEmpty(CurrentMissionHint(myAgent)))
                {
                    Log.WriteLine("--------------------------RegularMission [" + myMission.Name + "] Objective Info----------------------------");
                    Log.WriteLine(" _currentMissionInfo [" + CurrentMissionHint(myAgent) + "]");
                    Log.WriteLine("---------------------------------------------------------------------------");
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static void ResetTheListOfPocketActionsToBlank()
        {
            _pocketActions.Clear();
            _currentActionNumber = 0;
            Drones.DronesShouldBePulled = false;
            ClearPerActionCache();
        }

        private static void LoadPocket(DirectAgentMission myMission, DirectAgent myAgent)
        {
            if (ESCache.Instance.InWarp || ESCache.Instance.MyShipEntity.HasInitiatedWarp)
            {
                Log.WriteLine("LoadPocket: InWarp [" + ESCache.Instance.InWarp + "] HasInitiatedWarp [" + ESCache.Instance.MyShipEntity.HasInitiatedWarp + " ]");
                return;
            }

            if (Time.Instance.LastInWarp.AddSeconds(3) > DateTime.UtcNow)
            {
                Log.WriteLine("LoadPocket: Waiting a few sedonds after entering the pocket (LastInWarp)");
                return;
            }

            if (!Combat.Combat.PotentialCombatTargets.Any() && Statistics.StartedPocket.AddSeconds(5) > DateTime.UtcNow)
            {
                Log.WriteLine("LoadPocket: Waiting for PotentialCombatTargets");
                return;
            }

            Log.WriteLine("LoadPocket: Started");
            ResetTheListOfPocketActionsToBlank();

            if (myMission != null && myAgent != null)
            {
                //
                // Regular Agent based Mission
                //
                Log.WriteLine("Attempt to load mission actions for [" + myMission.Name + "] PocketNumber [" + PocketNumber + "]");
                _pocketActions.AddRange(LoadMissionActions(myMission, PocketNumber, true));

                try
                {
                    if (!string.IsNullOrEmpty(CurrentMissionHint(myAgent)))
                        Statistics.SaveMissionPocketObjectives(CurrentMissionHint(myAgent), Log.FilterPath(myMission.Name), PocketNumber);
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }

                Log.WriteLine("Objectives for this pocket: [" + CurrentMissionHint(myAgent) + "]");
            }

            if (_pocketActions.Count == 0)
            {
                if (ESCache.Instance.InAnomaly)
                {
                    //
                    // Anomaly or other non-mission Site
                    //
                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.HighSecAnomalyController))
                    {
                        if (HighSecAnomalyBehavior.myDirectSystemScanResult != null)
                        {
                            Log.WriteLine("Attempt to load site actions for [" + HighSecAnomalyBehavior.myDirectSystemScanResult.Id + "][" + HighSecAnomalyBehavior.myDirectSystemScanResult.TypeName + "] PocketNumber [" + PocketNumber + "]");
                            _pocketActions.AddRange(LoadSiteActions(HighSecAnomalyBehavior.myDirectSystemScanResult, PocketNumber));
                            if (_pocketActions.Count > 0) return;
                        }

                        Log.WriteLine("LoadPocket: Finished");
                        // No Pocket action, load default actions
                        ChangeCombatMissionCtrlState(ActionControlState.LoadPocketDefaults, myMission, myAgent, false);
                        return;
                    }

                    Log.WriteLine("if (ESCache.Instance.InAnomaly)");
                    return;
                }

                bool wait = true;

                if (Combat.Combat.PotentialCombatTargets.Any())
                    wait = false;

                Log.WriteLine("LoadPocket: Finished --> LoadPocketDefaults");
                // No Pocket action, load default actions
                ChangeCombatMissionCtrlState(ActionControlState.LoadPocketDefaults, myMission, myAgent, wait);
                return;
            }

            Log.WriteLine("LoadPocket: Finished --> LoadPocketFinish");
            ChangeCombatMissionCtrlState(ActionControlState.LoadPocketFinish, myMission, myAgent, false);
        }

        private static void LoadPocketDefaults(DirectAgentMission myMission, DirectAgent myAgent)
        {
            // No Pocket action, load default actions

            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController))
            {
                Log.WriteLine("LoadPocketDefaults: Finished --> LoadAbyssalPocketDefaults");
                LoadAbyssalPocketDefaults(myMission, myAgent);
                return;
            }

            //if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.FleetAbyssalDeadspaceController))
            //{
            //    Log.WriteLine("LoadPocketDefaults: Finished --> LoadAbyssalPocketDefaults");
            //    LoadAbyssalPocketDefaults(myMission, myAgent);
            //    return;
            //}

            Log.WriteLine("LoadPocketDefaults: Finished --> LoadMissionPocketDefaults");
            LoadMissionPocketDefaults(myMission, myAgent);
            return;
        }

        private static void LoadPocketFinish(DirectAgentMission myMission, DirectAgent myAgent)
        {
            if (ESCache.Instance.InWarp || ESCache.Instance.MyShipEntity.HasInitiatedWarp)
            {
                Log.WriteLine("LoadPocketFinish: InWarp [" + ESCache.Instance.InWarp + "] HasInitiatedWarp [" + ESCache.Instance.MyShipEntity.HasInitiatedWarp + " ]");
                return;
            }

            if (Time.Instance.LastInWarp.AddSeconds(3) > DateTime.UtcNow)
            {
                Log.WriteLine("LoadPocketFinish: Waiting a few sedonds after entering the pocket (LastInWarp)");
                return;
            }

            if (!Combat.Combat.PotentialCombatTargets.Any() && Statistics.StartedPocket.AddSeconds(5) > DateTime.UtcNow)
            {
                Log.WriteLine("LoadPocketFinish: Waiting for PotentialCombatTargets");
                return;
            }

            Log.WriteLine("Starting: LoadPocketFinish");
            //Set "here" to current location so that we will have some x y z coord to compare against later to determine when we have moved to the next pocket
            SetHereToCurrentXYZCoord();

            if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior)
            {
                if (!NavigateOnGrid.SpeedTank && _pocketActions.All(a => a.State != ActionState.MoveToBackground) && MissionSettings.MyMission != null && !MissionSettings.MyMission.Name.Contains("Anomic"))
                {
                    Action backgroundAction = new Action { State = ActionState.MoveToBackground };
                    backgroundAction.AddParameter("target", "Acceleration Gate");
                    backgroundAction.AddParameter("optional", "true");
                    _pocketActions.Insert(0, backgroundAction);
                }

                //
                // if we dont already have a WaitForNPCs action in the XML...
                //
                if (_pocketActions.All(a => a.State != ActionState.WaitForNPCs))
                {
                    //
                    // and we are going to kill NPCs in this pocket
                    //
                    if (_pocketActions.Any(a => a.State == ActionState.ClearPocket) || _pocketActions.Any(a => a.State == ActionState.Kill))
                    {
                        //
                        // Add WaitForNPCs action
                        //
                        Action waitUForNPcsAction = new Action { State = ActionState.WaitForNPCs };
                        waitUForNPcsAction.AddParameter("timeout", "10");
                        _pocketActions.Insert(0, waitUForNPcsAction);
                    }
                }

                Log.WriteLine("Mission Timer Currently At: [" + Math.Round(DateTime.UtcNow.Subtract(Statistics.StartedMission).TotalMinutes, 0) +
                              "] min");
            }

            Log.WriteLine("Max Range is currently: " + (Combat.Combat.MaxRange / 1000).ToString(CultureInfo.InvariantCulture) + "k");
            NavigateOnGrid.LogMyCurrentHealth();
            Statistics.LogMyComputersHealth();
            Log.WriteLine("Pocket [" + PocketNumber + "] loaded, executing the following actions");
            Log.WriteLine("-----------------------------------------------------------------");
            int pocketActionCount = 1;
            foreach (Action a in _pocketActions)
            {
                Log.WriteLine("Action [ " + pocketActionCount + " ] " + a);
                pocketActionCount++;
            }
            Log.WriteLine("-----------------------------------------------------------------");

            // Reset pocket information
            _currentActionNumber = 0;
            Drones.DronesShouldBePulled = false;

            if (NavigateOnGrid.SpeedTank && !Salvage.LootWhileSpeedTanking)
            {
                if (DebugConfig.DebugTargetWrecks)
                    Log.WriteLine("Salvage.OpenWrecks = false;");
                Salvage.OpenWrecks = false;
            }
            else
            {
                Salvage.OpenWrecks = true;
            }

            IgnoreTargets.Clear();
            Statistics.PocketObjectStatistics(ESCache.Instance.Entities);
            Log.WriteLine("Done: LoadPocketFinish");

            if (ESCache.Instance.InAbyssalDeadspace)
                ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdSetShipFullSpeed);

            ChangeCombatMissionCtrlState(ActionControlState.ExecutePocketActions, myMission, myAgent, false);
        }

        private static void ExecutePocketActions(DirectAgentMission myMission, DirectAgent myAgent)
        {
            try
            {
                if (ESCache.Instance.InWarp || ESCache.Instance.MyShipEntity.HasInitiatedWarp) return;
                if (Time.Instance.LastInWarp.AddSeconds(1) > DateTime.UtcNow) return;

                if (_lastNormalDirectWorldPosition != null && ESCache.Instance.DistanceFromMe(_lastNormalDirectWorldPosition.PositionInSpace) > (double)Distances.MaxPocketsDistanceKm)
                {
                    Log.WriteLine("We are in the next pocket: change state to 'NextPocket'");
                    DirectSession.SetSessionNextSessionReady(7000, 9000);
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastInteractedWithEVE), DateTime.UtcNow);
                    ESCache.Instance.ClearPerPocketCache();
                    Time.Instance.LastInitiatedWarp = DateTime.UtcNow;
                    Time.Instance.NextActivateAccelerationGate = DateTime.UtcNow.AddSeconds(13);
                    if (DirectEve.Interval(30000)) DirectEventManager.NewEvent(new DirectEvent(DirectEvents.KEEP_ALIVE, "Gate Activated"));
                    _startedPocket = DateTime.UtcNow;
                    SetHereToCurrentXYZCoord();
                    Log.WriteLine("Change state to 'NextPocket' LostDrones [" + Drones.LostDrones + "] AllDronesInSpaceCount [" + Drones.AllDronesInSpaceCount + "]");
                    ChangeCombatMissionCtrlState(ActionControlState.NextPocket, myMission, myAgent);
                    return;
                }

                Action tempNextAction = null;
                try
                {
                    if (_pocketActions[_currentActionNumber] != null)
                        tempNextAction = _pocketActions[_currentActionNumber];
                }
                catch (Exception)
                {
                }

                if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior)
                    if (MissionSettings.IsMissionFinished)
                        if (tempNextAction == null || (tempNextAction.State != ActionState.Done && tempNextAction.State != ActionState.ReallyDone))
                        {
                            Log.WriteLine("Mission objectives are complete, setting state to done.");
                            _currentActionNumber = 0;
                            ResetTheListOfPocketActionsToBlank();
                            _pocketActions.Add(new Action { State = ActionState.Done });
                            return;
                        }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }

            if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior)
            {
                if (ESCache.Instance.ReplaceMissionsActions)
                {
                    Log.WriteLine("Window indicating a need for ReplaceMissionsActions Detected via CleanupController.");
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(ESCache.Instance.EveAccount.ReplaceMissionsActions), false);
                    ReplaceMissionsActions();
                    return;
                }

                if (_currentActionNumber >= _pocketActions.Count)
                {
                    // No more actions, but we're not done?!?!?!

                    if (Combat.Combat.PotentialCombatTargets.Count > 0)
                    {
                        Log.WriteLine("We're out of actions but did not process a 'Done' or 'Activate' action; Adding a ClearPocket action");
                        _currentActionNumber = 0;
                        ResetTheListOfPocketActionsToBlank();
                        _pocketActions.Add(new Action { State = ActionState.ClearPocket });
                        NextAction(myMission, myAgent, false);
                        return;
                    }

                    if (ESCache.Instance.EntitiesOnGrid.Any(i => i.IsAccelerationGate))
                    {
                        Log.WriteLine("We're out of actions but did not process a 'Done' or 'Activate' action; Adding an activate action");
                        _currentActionNumber = 0;
                        ResetTheListOfPocketActionsToBlank();
                        _pocketActions.Add(new Action { State = ActionState.Activate });
                        NextAction(myMission, myAgent, false);
                        return;
                    }
                    if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior && myAgent != null && myAgent.IsValid && myMission.IsMissionFinished != null && !(bool)myMission.IsMissionFinished)
                    {
                        Log.WriteLine("We're out of actions but did not process a 'Done' or 'Activate' action; Adding a done action");
                        DoneAction(myMission, myAgent);
                        NextAction(myMission, myAgent, false);
                        return;
                    }
                    if (ESCache.Instance.UnlootedContainers.Count > 0 || (ESCache.Instance.Wrecks.Count > 0 && Salvage.Salvagers.Count > 0 && Salvage.LootEverything))
                    {
                        Log.WriteLine("We're out of actions but did not process a 'Done' or 'Activate' action; LootEverything: Adding a loot and a done action");
                        _currentActionNumber = 0;
                        ResetTheListOfPocketActionsToBlank();
                        _pocketActions.Add(new Action { State = ActionState.Loot });
                        _pocketActions.Add(new Action { State = ActionState.Done });
                        NextAction(myMission, myAgent, false);
                    }

                    return;
                }
            }

            Action action = _pocketActions[_currentActionNumber];
            if (action.ToString() != ESCache.Instance.CurrentPocketAction)
                ESCache.Instance.CurrentPocketAction = action.ToString();
            int currentAction = _currentActionNumber;
            ESCache.Instance.TaskSetEveAccountAttribute(nameof(ESCache.Instance.EveAccount.CMBCtrlAction), action.State.ToString());

            if (ESCache.Instance.InMission && !action.ToString().ToLower().Contains("loot".ToLower()))
                if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior && myMission != null && myMission.Name.ToLower() == "Worlds Collide".ToLower())
                    if (ESCache.Instance.EntitiesOnGrid != null && ESCache.Instance.EntitiesOnGrid.Any(i => i.Name.ToLower().Contains("Damaged Heron".ToLower())))
                        Salvage.OpenWrecks = false;

            /// Log.WriteLine("PerformAction(action); [" + action.ToString() + "]");
            PerformAction(action, myMission, myAgent);

            if (currentAction != _currentActionNumber)
            {
                ClearPerActionCache(action);
            }
        }

        public static int FailedGateActivationAttempts = 0;

        private static void LoadAbyssalPocketDefaults(DirectAgentMission myMission, DirectAgent myAgent)
        {
            if (!ESCache.Instance.InAbyssalDeadspace || ESCache.Instance.AccelerationGates.Count == 0)
            {
                Log.WriteLine("LoadAbyssalPocketDefaults: no acceleration gate found yet, waiting...");
                return;
            }

            if (Statistics.StartedPocket.AddSeconds(8) > DateTime.UtcNow)
                return;

            Log.WriteLine("Start: LoadPocketDefaults");
            Log.WriteLine("No mission actions specified, loading default actions");

            Log.WriteLine("LoadAbyssalPocketDefaults");

            if (ESCache.Instance.AccelerationGates.Any(i => i.Distance > 10000) || Combat.Combat.PotentialCombatTargets.Any(i => !i.IsNPCDrone))
            {
                Salvage.ChangeSalvageState(SalvageState.TargetWrecks);
                FailedGateActivationAttempts = 0;

                // Wait for 10 seconds to be targeted
                Action waitAction = new Action { State = ActionState.AbyssalWaitUntilAggressed };
                waitAction.AddParameter("timeout", "12");
                _pocketActions.Add(waitAction);

                //Action backgroundAction = new Action { State = ActionState.MoveToBackground };
                //backgroundAction.AddParameter("target", "Transfer Conduit (Triglavian)");
                //_pocketActions.Add(backgroundAction);
                //
                // this is added twice on purpose!
                //
                //_pocketActions.Add(backgroundAction);

                // Clear the Pocket
                _pocketActions.Add(new Action { State = ActionState.ClearPocket });

                Action abyssalLootAction = new Action { State = ActionState.AbyssalLoot };
                _pocketActions.Add(abyssalLootAction);
            }
            else
            {
                FailedGateActivationAttempts++;
                Log.WriteLine("We are [" + Math.Round(ESCache.Instance.AccelerationGates.FirstOrDefault().Distance / 1000, 0) + "]k from the gate, we assume the last attempt to activate failed, trying again.");
                if (FailedGateActivationAttempts > 4)
                {
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.RestartOfEveClientNeeded), true);
                    ESCache.Instance.CloseEveReason = "if (FailedGateActivationAttempts > 4)";
                    ESCache.Instance.BoolRestartEve = true;
                }
            }

            Action activateAccelerationGateAction = new Action { State = ActionState.AbyssalActivate };
            _pocketActions.Add(activateAccelerationGateAction);

            bool wait = true;

            if (Combat.Combat.PotentialCombatTargets.Any())
                wait = false;

            Log.WriteLine("Done: LoadAbyssalPocketDefaults");
            ChangeCombatMissionCtrlState(ActionControlState.LoadPocketFinish, myMission, myAgent, wait);
        }

        private static DateTime _currentActionStarted = DateTime.UtcNow;

        private static void LoadMissionPocketDefaults(DirectAgentMission myMission, DirectAgent myAgent)
        {
            Log.WriteLine("Start: LoadPocketDefaults");
            Log.WriteLine("No mission actions specified, loading default actions");

            Log.WriteLine("LoadMissionPocketDefaults");

            if (!NavigateOnGrid.SpeedTank)
            {
                Action backgroundAction = new Action { State = ActionState.MoveToBackground };
                backgroundAction.AddParameter("target", "Acceleration Gate");
                backgroundAction.AddParameter("optional", "true");
                _pocketActions.Add(backgroundAction);
            }

            // Wait for x seconds to be targeted
            Action waitAction = new Action { State = ActionState.WaitForNPCs };
            waitAction.AddParameter("timeout", "10");
            _pocketActions.Add(waitAction);

            // Clear the Pocket
            _pocketActions.Add(new Action { State = ActionState.ClearPocket });

            Action lootFactionOnlyAction = new Action { State = ActionState.LootFactionOnly };
            _pocketActions.Add(lootFactionOnlyAction);

            Action activateAccelerationGateAction = new Action { State = ActionState.Activate };
            activateAccelerationGateAction.AddParameter("target", "Acceleration Gate");
            activateAccelerationGateAction.AddParameter("optional", "true");
            _pocketActions.Add(activateAccelerationGateAction);

            Log.WriteLine("Done: LoadMissionPocketDefaults");
            ChangeCombatMissionCtrlState(ActionControlState.LoadPocketFinish, myMission, myAgent);
        }

        private static DateTime _nextLogCurrentCombatMissionCtrlState;

        private static void ReportCurrentCombatMissionState(DirectAgentMission myMission)
        {
            try
            {
                if (DateTime.UtcNow > _nextLogCurrentCombatMissionCtrlState)
                {
                    if (!ESCache.Instance.InSpace) return;
                    if (!ESCache.Instance.InMission) return;

                    string missionName = string.Empty;

                    string myCurrentCombatMissionCtrlAction = string.Empty;
                    if (_pocketActions != null && _pocketActions.Count > 0)
                    {
                        myCurrentCombatMissionCtrlAction = _pocketActions[_currentActionNumber].State.ToString();
                    }

                    if (myMission != null)
                    {
                        missionName = myMission.Name;
                    }

                    _nextLogCurrentCombatMissionCtrlState = DateTime.UtcNow.AddSeconds(30);
                    Log.WriteLine("ReportCurrentCombatMissionState: myMission [" + missionName + "] CurrentCombatMissionCtrlState [" + State.CurrentCombatMissionCtrlState + "] Current Action [" + myCurrentCombatMissionCtrlAction + "] Action Started [" + Math.Round(DateTime.UtcNow.Subtract(_currentActionStarted).TotalMinutes, 1) + "] Pocket Started [" + Math.Round(DateTime.UtcNow.Subtract(_startedPocket).TotalMinutes, 1) + "]");
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static void ResetCurrentXYZCoord()
        {
            _lastNormalDirectWorldPosition = null;
        }

        private static void SetHereToCurrentXYZCoord()
        {
            Log.WriteLine("SetHereToCurrentXYZCoord X [" + ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition.XCoordinate + "] Y [" + ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition.YCoordinate + "] Z [" + ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition.ZCoordinate + "]");
            _lastNormalDirectWorldPosition = ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition;
        }

        public static void ProcessState(DirectAgentMission myMission, DirectAgent myAgent)
        {
            try
            {
                // There is really no combat in stations (yet)
                if (ESCache.Instance.InStation)
                    return;

                // if we are not in space yet, wait...
                if (!ESCache.Instance.InSpace)
                    return;

                // There is no combat when warping
                if (ESCache.Instance.InWarp || ESCache.Instance.MyShipEntity.HasInitiatedWarp)
                    return;

                // There is no combat when cloaked
                if (ESCache.Instance.ActiveShip.Entity.IsCloaked)
                    return;

                if (DebugConfig.DebugAbyssalDeadspaceBehavior || DebugConfig.DebugCombatMissionCtrl) Log.WriteLine("ActionControlState [" + State.CurrentCombatMissionCtrlState + "]");

                ReportCurrentCombatMissionState(myMission);

                if (!ESCache.Instance.InAbyssalDeadspace && !DirectEve.HasFrameChanged(nameof(ProcessState)))
                    return;

                switch (State.CurrentCombatMissionCtrlState)
                {
                    case ActionControlState.Idle:
                        break;

                    case ActionControlState.Done:
                        Statistics.WritePocketStatistics();
                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(ESCache.Instance.EveAccount.CMBCtrlAction), string.Empty);

                        IgnoreTargets.Clear();
                        break;

                    case ActionControlState.Error:
                        break;

                    case ActionControlState.Start:
                        if (!ESCache.Instance.InMission || ESCache.Instance.InWarp) return;
                        Log.WriteLine("ActionControlState.Start:");
                        PocketNumber = 0;
                        if (ESCache.Instance.InAbyssalDeadspace)
                            PocketNumber = ESCache.Instance.EveAccount.AbyssalPocketNumber;

                        NextPocketRoutine(PocketNumber, myMission, myAgent);
                        break;

                    case ActionControlState.LoadPocket:
                        LoadPocket(myMission, myAgent);
                        break;

                    case ActionControlState.LoadPocketDefaults:
                        LoadPocketDefaults(myMission, myAgent);
                        break;

                    case ActionControlState.LoadPocketFinish:
                        LoadPocketFinish(myMission, myAgent);
                        break;

                    case ActionControlState.ExecutePocketActions:
                        ExecutePocketActions(myMission, myAgent);
                        break;

                    case ActionControlState.NextPocket:
                        if (ESCache.Instance.InWarp || ESCache.Instance.MyShipEntity.HasInitiatedWarp) return;
                        if (Time.Instance.LastInWarp.AddSeconds(3) > DateTime.UtcNow) return;

                        NextPocketRoutine(PocketNumber + 1, myMission, myAgent);
                        Statistics.WritePocketStatistics();
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static void NextPocketRoutine(int tempPocketNumber, DirectAgentMission myMission, DirectAgent myAgent)
        {
            Log.WriteLine("NextPocketRoutine: Started");
            // Update statistic values
            ESCache.Instance.WealthAtStartOfPocket = ESCache.Instance.DirectEve.Me.Wealth ?? 0;
            Statistics.StartedPocket = DateTime.UtcNow;

            // Reset notNormalNav and onlyKillAggro to false
            ESCache.Instance.NormalNavigation = true;

            PocketNumber = tempPocketNumber;
            ESCache.Instance.TaskSetEveAccountAttribute("AbyssalPocketNumber", PocketNumber);
            bool wait = true;
            if (ESCache.Instance.InAbyssalDeadspace && AbyssalSpawn.DetectSpawn != AbyssalSpawn.AbyssalSpawnType.Undecided)
                wait = false;

            Log.WriteLine("NextPocketRoutine: Finished: wait [" + wait + "]");
            ChangeCombatMissionCtrlState(ActionControlState.LoadPocket, myMission, myAgent, wait);
        }

        private static void ClearPerActionCache(Action action = null)
        {
            if (action != null ) Log.WriteLine("Finished Action." + action);

            // now that we have completed this action revert OpenWrecks to false
            if (NavigateOnGrid.SpeedTank && !Salvage.LootWhileSpeedTanking)
            {
                if (DebugConfig.DebugTargetWrecks) Log.WriteLine("Salvage.OpenWrecks = false;");
                Salvage.OpenWrecks = false;
            }

            ESCache.Instance.NormalNavigation = true;
            MissionSettings.MissionActivateRepairModulesAtThisPerc = null;
            MissionSettings.PocketUseDrones = null;
            ItemsHaveBeenMoved = false;
            CargoHoldHasBeenStacked = false;
            NextActionBool = false;
            _waiting = false;
            _clearPocketTimeout = null;
            _waitingSince = DateTime.UtcNow;

            if (_pocketActions.Count > 0 && _currentActionNumber < _pocketActions.Count)
            {
                _currentActionStarted = DateTime.UtcNow;
                action = _pocketActions[_currentActionNumber];
                Log.WriteLine("Started Action." + action);
            }
        }

        private static void ReplaceMissionsActions()
        {
            try
            {
                Log.WriteLine("Clearing current pocketActions");
                ResetTheListOfPocketActionsToBlank();

                //
                // Adds actions specified in the Mission XML
                //
                // Clear the Pocket
                Log.WriteLine("Adding ClearPocket Action");
                _pocketActions.Add(new Action { State = ActionState.ClearPocket });
                _pocketActions.Add(new Action { State = ActionState.ClearPocket });

                //we manually add 2 ClearPockets above, then we try to load other mission XMLs for this pocket, if we fail Count will be 2 and we know we need to add an activate and/or a done action.
                if (_pocketActions.Count == 2)
                    if (ESCache.Instance.AccelerationGates.Count > 0)
                    {
                        Log.WriteLine("Adding Activate Action");
                        // Activate it (Activate action also moves to the gate)
                        _pocketActions.Add(new Action { State = ActionState.Activate });
                        _pocketActions[_pocketActions.Count - 1].AddParameter("target", "Acceleration Gate");
                    }
                    else // No, were done
                    {
                        Log.WriteLine("Adding Done Action");
                        _pocketActions.Add(new Action { State = ActionState.Done });
                    }

                NextActionBool = true;
                _clearPocketTimeout = null;
                _lootActionTimeout = null;
                _currentActionNumber++;
                Log.WriteLine("CurrentAction is now [" + _pocketActions[_currentActionNumber] + "]");
                return;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static bool ChooseNextActionsBasedOnMissionObjective(DirectAgentMission myMission, DirectAgent myAgent)
        {
            if (myMission == null || myAgent == null)
                return false;

            if (myMission.Name.Contains("Cash Flow for Capsuleers"))
                return false;

            string tempCurrentMissionAction = CurrentMissionHintActionNeeded(myAgent);
            //
            // XML might be wrong, verify everything is dead?
            //
            if (myAgent.IsValid)
            {
                if (!MissionSettings.IsMissionFinished)
                {
                    //
                    // List Of Actions and Descriptions
                    //
                    // Attack
                    // MissionFetch, TypeId
                    // MissionFetchContainer, TypeId (to scoop from the container!), ContainerID
                    // TravelTo
                    // GoToGate, itemID (or 0!)
                    // Destroy, typeID, ItemID
                    // Approach
                    // FetchObjectAcquiredDungeonDone
                    // AllObjectivesComplete
                    //

                    if (tempCurrentMissionAction == null && !myMission.MissionInfoNeverShowsMissionComplete)
                    {
                        //
                        // We are out of actions and the mission is not registering as complete: is this mission one of the broken ones that doesnt registe as 'done'?
                        //
                        Log.WriteLine("Mission Objective Action is <empty> but we are out of mission actions: Adding Actions: 1) ClearPocket 2) Done");
                        ResetTheListOfPocketActionsToBlank();
                        _pocketActions.Add(new Action { State = ActionState.ClearPocket });
                        _pocketActions.Add(new Action { State = ActionState.Done });
                        NextAction(myMission, myAgent, false);
                        return true;
                    }

                    Log.WriteLine("Mission Objective Action is [" + CurrentMissionHint(MissionSettings.MyMission.Agent) + "] but we are out of mission actions.");

                    if (CurrentMissionHintActionNeeded(myAgent).Contains("Attack"))
                    {
                        Log.WriteLine("Found Attack action: Adding Actions: 1) clearpocket, 2) activate gate (if it exists) 3) clearpocket 4) done");
                        if (DebugConfig.DebugCombatMissionCtrl) Log.WriteLine("if (MissionSettings.RegularMission.Objective.Action == Attack)");
                        ResetTheListOfPocketActionsToBlank();
                        // Clear the Pocket
                        _pocketActions.Add(new Action { State = ActionState.ClearPocket });

                        // Is there a gate?
                        if (ESCache.Instance.AccelerationGates.Count > 0)
                        {
                            // Activate it (Activate action also moves to the gate)
                            _pocketActions.Add(new Action { State = ActionState.Activate });
                            _pocketActions[_pocketActions.Count - 1].AddParameter("target", "Acceleration Gate");
                        }

                        _pocketActions.Add(new Action { State = ActionState.ClearPocket });
                        _pocketActions.Add(new Action { State = ActionState.Done });
                        NextAction(myMission, myAgent, false);
                        return true;
                    }

                    if (CurrentMissionHintActionNeeded(myAgent).Contains("MissionFetch"))
                    {
                        Log.WriteLine("Found MissionFetch action: Adding Actions: 1) clearpocket 2) Activate gate (if it exists) 4) clearpocket 4) LootItem 5) Done");
                        if (DebugConfig.DebugCombatMissionCtrl) Log.WriteLine("if (MissionSettings.RegularMission.Objective.Action == MissionFetchContainer)");
                        ResetTheListOfPocketActionsToBlank();

                        _pocketActions.Add(new Action { State = ActionState.ClearPocket });
                        // Is there a gate?
                        if (ESCache.Instance.AccelerationGates.Count > 0)
                        {
                            // Activate it (Activate action also moves to the gate)
                            _pocketActions.Add(new Action { State = ActionState.Activate });
                            _pocketActions[_pocketActions.Count - 1].AddParameter("target", "Acceleration Gate");
                            return true;
                        }

                        _pocketActions.Add(new Action { State = ActionState.ClearPocket });
                        _pocketActions.Add(new Action { State = ActionState.LootItem });
                        _pocketActions[_pocketActions.Count - 1].AddParameter("target", "Cargo Container");
                        _pocketActions[_pocketActions.Count - 1].AddParameter("ItemTypeId", CurrentMissionHint1stParameter(myAgent));
                        _pocketActions.Add(new Action { State = ActionState.Done });
                        NextAction(myMission, myAgent, false);
                        return true;
                    }

                    if (CurrentMissionHintActionNeeded(myAgent).Contains("MissionFetchContainer"))
                    {
                        Log.WriteLine("Found MissionFetchContainer action: Adding Actions 1) clearpocket 2) Activate gate (if gate exists), 3) clearpocket 4) LootItem 5) Done");
                        if (DebugConfig.DebugCombatMissionCtrl) Log.WriteLine("if (MissionSettings.RegularMission.Objective.Action == MissionFetchContainer)");
                        ResetTheListOfPocketActionsToBlank();

                        // Clear the Pocket
                        _pocketActions.Add(new Action { State = ActionState.ClearPocket });

                        // Is there a gate?
                        if (ESCache.Instance.AccelerationGates.Count > 0)
                        {
                            // Activate it (Activate action also moves to the gate)
                            _pocketActions.Add(new Action { State = ActionState.Activate });
                            _pocketActions[_pocketActions.Count - 1].AddParameter("target", "Acceleration Gate");
                        }

                        // Clear the Pocket
                        _pocketActions.Add(new Action { State = ActionState.ClearPocket });
                        _pocketActions.Add(new Action { State = ActionState.LootItem });
                        _pocketActions[_pocketActions.Count - 1].AddParameter("target", "Cargo Container");
                        _pocketActions[_pocketActions.Count - 1].AddParameter("containerid", CurrentMissionHint2ndParameter(myAgent));
                        _pocketActions[_pocketActions.Count - 1].AddParameter("ItemTypeId", CurrentMissionHint1stParameter(myAgent));
                        _pocketActions.Add(new Action { State = ActionState.Done });
                        NextAction(myMission, myAgent, false);
                        return true;
                    }

                    if (CurrentMissionHintActionNeeded(myAgent).Contains("GoToGate"))
                    {
                        Log.WriteLine("Found GoToGate action: Adding Actions: 1) moveto 2) Activate gate 3) Done");
                        if (CurrentMissionHint1stParameter(myAgent) != "0")
                        {
                            EntityCache entityToActivate = ESCache.Instance.EntitiesOnGrid.Find(i => CurrentMissionHint1stParameter(myAgent) == i.Id.ToString());
                            if (DebugConfig.DebugCombatMissionCtrl) Log.WriteLine("if (MissionSettings.RegularMission.Objective.Action == GoToGate)");
                            if (entityToActivate != null)
                            {
                                ResetTheListOfPocketActionsToBlank();
                                _pocketActions.Add(new Action { State = ActionState.MoveTo });
                                _pocketActions[_pocketActions.Count - 1].AddParameter("target", entityToActivate.Name);
                                _pocketActions.Add(new Action { State = ActionState.Activate });
                                _pocketActions[_pocketActions.Count - 1].AddParameter("target", entityToActivate.Name);
                                _pocketActions.Add(new Action { State = ActionState.Done });
                                NextAction(myMission, myAgent, false);
                                return true;
                            }
                        }

                        ResetTheListOfPocketActionsToBlank();
                        _pocketActions.Add(new Action { State = ActionState.MoveTo });
                        _pocketActions[_pocketActions.Count - 1].AddParameter("target", "Acceleration Gate");
                        _pocketActions.Add(new Action { State = ActionState.Activate });
                        _pocketActions[_pocketActions.Count - 1].AddParameter("target", "Acceleration Gate");
                        _pocketActions.Add(new Action { State = ActionState.Done });
                        NextAction(myMission, myAgent, false);
                        return true;
                    }

                    /**
                    if (_currentMissionInfo.Contains("Destroy") && !_currentMissionInfo.Contains(TypeID.Beacon.ToString()))
                    {
                        EntityCache entityToActivate = ESCache.Instance.EntitiesOnGrid.FirstOrDefault(i => i.Id == MissionSettings.RegularMission.Objective.ItemID);
                        if (DebugConfig.DebugCombatMissionCtrl) Log.WriteLine("if (MissionSettings.RegularMission.Objective.Action == Destroy)");
                        _pocketActions.Add(new Actions.Action { State = ActionState.KillByItemId });
                        _pocketActions[_pocketActions.Count - 1].AddParameter("TypeId", MissionSettings.RegularMission.Objective.TypeID.ToString());
                        _pocketActions[_pocketActions.Count - 1].AddParameter("ItemID", MissionSettings.RegularMission.Objective.ItemID.ToString());
                        _pocketActions.Add(new Actions.Action { State = ActionState.Done });
                        //_pocketActions[_pocketActions.Count - 1].AddParameter("Item", "Cargo Container"); //neds itemID?
                        return true;
                    }
                    **/

                    if (CurrentMissionHintActionNeeded(myAgent).Contains("Approach"))
                    {
                        EntityCache entityToApproach = ESCache.Instance.EntitiesOnGrid.Find(i => CurrentMissionHint(myAgent).Contains(i.Id.ToString()));
                        if (entityToApproach != null)
                        {
                            Log.WriteLine("Found Approach action: Target is on grid: Adding Actions: 1) moveto 2) Done");
                            ResetTheListOfPocketActionsToBlank();
                            _pocketActions.Add(new Action { State = ActionState.MoveTo });
                            _pocketActions[_pocketActions.Count - 1].AddParameter("target", entityToApproach.Name);
                            _pocketActions.Add(new Action { State = ActionState.Done });
                            NextAction(myMission, myAgent, false);
                            return true;
                        }
                    }

                    if (CurrentMissionHintActionNeeded(myAgent).Contains("DestroyAll"))
                    {
                        if (Combat.Combat.PotentialCombatTargets.Count > 0)
                        {
                            if (DebugConfig.DebugCombatMissionCtrl) Log.WriteLine("ChooseNextActionsBasedOnMissionObjective: if (Combat.Combat.PotentialCombatTargets.Any())");
                            _pocketActions.Add(new Action { State = ActionState.ClearPocket });
                            _pocketActions.Add(new Action { State = ActionState.Done });
                            NextAction(myMission, myAgent, false);
                            return true;
                        }

                        return true;
                    }

                    if (CurrentMissionHintActionNeeded(myAgent).Contains("Destroy"))
                    {
                        Log.WriteLine("Found Destroy action: Adding Actions 1) kill 2) Done");
                        if (DebugConfig.DebugCombatMissionCtrl) Log.WriteLine("if (MissionSettings.RegularMission.Objective.Action == Destroy)");
                        ResetTheListOfPocketActionsToBlank();

                        // Clear the Pocket
                        _pocketActions.Add(new Action { State = ActionState.Kill });
                        _pocketActions[_pocketActions.Count - 1].AddParameter("target", CurrentMissionHint2ndParameter(myAgent));

                        _pocketActions.Add(new Action { State = ActionState.Done });
                        NextAction(myMission, myAgent, false);
                        return true;
                    }

                    return false;
                }

                if (DebugConfig.DebugCombatMissionCtrl) Log.WriteLine("if (MissionSettings.RegularMission.Objective.Action == AllObjectivesComplete || FetchObjectAcquiredDungeonDone)");
                return false;
            }

            return false;
        }

        private static bool MissionRequiresAllNPCsCleared(DirectAgentMission myMission)
        {
            //
            // some missions have broken objectives and show as the objective being complete when there is another 'objective' of clearing the NPCs that is not listed.
            //
            if (myMission.Name == "Unauthorized Military Presence") return true;
            if (myMission.Name == "Smuggler Interception") return true; // lvl 1 and lvl4
            return false;
        }

        private static void NextAction(DirectAgentMission myMission, DirectAgent myAgent, bool incrimentNextAction = true)
        {
            try
            {
                _clearPocketTimeout = null;
                _lootActionTimeout = null;

                if (_pocketActions != null && _pocketActions.Count > 0)
                {
                    Log.WriteLine("ActionControl: NextAction");
                    if (!incrimentNextAction)
                        return;

                    _currentActionNumber++;
                    Log.WriteLine("CurrentAction is now [" + _pocketActions[_currentActionNumber] + "]");
                    if (myMission != null && myAgent != null)
                        ProcessState(myMission, myAgent);

                    return;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static void PerformAction(Action action, DirectAgentMission myMission, DirectAgent myAgent)
        {
            try
            {
                if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior)
                {
                    if (CurrentMissionHint(myAgent).Contains("MissionFetch") || CurrentMissionHint(myAgent).Contains("MissionFetchTarget"))
                        if (ESCache.Instance.CurrentShipsCargo != null && ESCache.Instance.CurrentShipsCargo.Items.Any(i => CurrentMissionHint2ndParameter(myAgent) == i.TypeId.ToString()))
                            if (!MissionRequiresAllNPCsCleared(myMission))
                            {
                                DirectItem matchedItem = ESCache.Instance.CurrentShipsCargo.Items.Find(i => CurrentMissionHint2ndParameter(myAgent) == i.TypeId.ToString());
                                if (matchedItem != null)
                                {
                                    Log.WriteLine("Objective [" + CurrentMissionHint(myAgent) + "] We have the item [" + matchedItem.TypeName + "]: Completing RegularMission Actions: Note: !MissionRequiresAllNPCsCleared");
                                    ReallyDoneAction(myMission, myAgent);
                                    return;
                                }
                            }

                    if (!ESCache.Instance.InMission)
                    {
                        Log.WriteLine("ESCache.Instance.InMission [" + ESCache.Instance.InMission + "] We are done with the mission, as we are no longer located in the mission (we warped?)");
                        ReallyDoneAction(myMission, myAgent);
                    }
                }

                switch (action.State)
                {
                    case ActionState.LogWhatIsOnGrid:
                        LogWhatIsOnGridAction(myMission, myAgent);
                        break;

                    case ActionState.AbyssalActivate:
                        AbyssalActivateAction(action);
                        break;

                    case ActionState.Activate:
                        ActivateAction(action, myMission, myAgent);
                        break;

                    case ActionState.ClearPocket:
                        ClearPocketAction(action, myMission, myAgent);
                        break;

                    case ActionState.SalvageBookmark:
                        BookmarkPocketForSalvaging(myMission);
                        NextAction(myMission, myAgent, true);
                        break;

                    case ActionState.Done:
                        DoneAction(myMission, myAgent);
                        break;

                    case ActionState.ReallyDone:
                        ReallyDoneAction(myMission, myAgent);
                        break;

                    case ActionState.AddEcmNpcByName:
                        AddEcmNpcByNameAction(action, myMission, myAgent);
                        break;

                    case ActionState.AddWebifierByName:
                        AddWebifierByNameAction(action, myMission, myAgent);
                        break;

                    case ActionState.Ecm:
                        EcmAction(action, myMission, myAgent);
                        break;

                    case ActionState.Kill:
                        KillAction(action, myMission, myAgent);
                        break;

                    case ActionState.KillKeepAtRange:
                        KillKeepAtRangeAction(action, myMission, myAgent);
                        break;

                    case ActionState.KillOnce:
                        KillAction(action, myMission, myAgent);
                        break;

                    case ActionState.KillNoNavigateOnGrid:
                        KillNoNavigateOnGridAction(action, myMission, myAgent);
                        break;

                    case ActionState.PickASingleTargetToKill:
                        PickASingleTargetToKillAction(action, myMission, myAgent);
                        break;

                    case ActionState.UseDrones:
                        UseDronesAction(action, myMission, myAgent);
                        break;

                    case ActionState.KillClosestByName:
                        KillClosestByNameAction(action, myMission, myAgent);
                        break;

                    case ActionState.KillClosest:
                        KillClosestAction(myMission, myAgent);
                        break;

                    case ActionState.MoveTo:
                        MoveToAction(action, myMission, myAgent);
                        break;

                    case ActionState.OrbitEntity:
                        OrbitEntityAction(action, myMission, myAgent);
                        break;

                    case ActionState.MoveToBackground:
                        MoveToBackgroundAction(action, myMission, myAgent);
                        break;

                    case ActionState.KeepAtRangeToBackground:
                        KeepAtRangeToBackgroundAction(action, myMission, myAgent);
                        break;

                    case ActionState.MoveDirection:
                        MoveDirectionAction(action, myMission, myAgent);
                        break;

                    case ActionState.ClearWithinWeaponsRangeOnly:
                        ClearWithinWeaponsRangeOnlyAction(action, myMission, myAgent);
                        break;

                    case ActionState.Salvage:
                        SalvageAction(action, myMission, myAgent);
                        break;

                    case ActionState.AbyssalLoot:
                        AbyssalLootAction(action);
                        break;

                    case ActionState.LootFactionOnly:
                        LootFactionOnlyAction(action);
                        break;

                    case ActionState.Loot:
                        LootAction(action);
                        break;

                    case ActionState.LootItem:
                        LootItemAction(action, myMission, myAgent);
                        break;

                    case ActionState.ActivateBastion:
                        ActivateBastionAction(action, myMission, myAgent);
                        break;

                    case ActionState.DropItem:
                        DropItemAction(action, myMission, myAgent);
                        break;

                    case ActionState.Ignore:
                        IgnoreAction(action, myMission, myAgent);
                        break;

                    case ActionState.WaitUntilTargeted:
                        WaitUntilTargetedAction(action, myMission, myAgent);
                        break;

                    case ActionState.WaitForWreck:
                        WaitForWreckAction(action, myMission, myAgent);
                        break;

                    case ActionState.WaitForNPCs:
                        WaitForNPCsAction(action, myMission, myAgent);
                        break;

                    case ActionState.AbyssalWaitUntilAggressed:
                        AbyssalWaitUntilAggressedAction(action, myMission, myAgent);
                        break;

                    case ActionState.WaitUntilAggressed:
                        WaitUntilAggressedAction(action, myMission, myAgent);
                        break;

                    case ActionState.DebuggingWait:
                        DebuggingWait(action, myMission, myAgent);
                        break;

                    case ActionState.ReloadAll:
                        ReloadAllAction(action, myMission, myAgent);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        #endregion Methods
    }
}