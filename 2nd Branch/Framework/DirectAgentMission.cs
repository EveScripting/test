extern alias SC;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using EVESharpCore.Cache;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using SC::SharedComponents.EVE;
using SC::SharedComponents.EVE.ClientSettings;
using SC::SharedComponents.Extensions;
using SC::SharedComponents.IPC;
using SC::SharedComponents.Py;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public enum MissionState
    {
        Offered = 1,
        Accepted = 2,
        OfferExpired = 3,
        Unknown4 = 4,
        Unknown5 = 5
    }

    public class DirectAgentMission : DirectObject
    {
        #region Fields

        //private PyObject _pyAgentId;

        #endregion Fields

        #region Constructors

        internal DirectAgentMission(DirectEve directEve) : base(directEve)
        {
        }

        #endregion Constructors

        #region Properties

        //public int moveItemProcessRepititions;

        private int? _moveItemRetryCounter;
        public int moveItemRetryCounter
        {
            get
            {
                if (_moveItemRetryCounter == null)
                {
                    if (DirectEve.DictCurrentCourierMissionItemMoveRetries.ContainsKey(UniqueMissionId))
                    {
                        _moveItemRetryCounter = DirectEve.DictCurrentCourierMissionItemMoveRetries[UniqueMissionId];
                        return (int)_moveItemRetryCounter;
                    }

                    _moveItemRetryCounter = 0;
                    return (int)_moveItemRetryCounter;
                }

                return (int)_moveItemRetryCounter;
            }
            set
            {
                _moveItemRetryCounter = value;
                DirectEve.DictCurrentCourierMissionItemMoveRetries[UniqueMissionId] = (int)_moveItemRetryCounter;
            }
        }

        private DirectAgent _agent;

        [NonSerialized]
        private DirectItem _courierMissionItemToMove;

        public DirectAgent Agent
        {
            get
            {
                if (_agent == null)
                {
                    _agent = DirectEve.GetAgentById(AgentId);
                    return _agent;
                }

                return _agent;
            }
        }

        public long AgentId { get; internal set; }

        public List<DirectAgentMissionBookmark> Bookmarks { get; internal set; }

        public List<DirectBookmark> _storylineBookmarks = new List<DirectBookmark>();

        public List<DirectBookmark> StorylineBookmarks
        {
            get
            {
                if (_storylineBookmarks.Count == 0)
                {
                    foreach (DirectBookmark tempBookmark in DirectEve.Bookmarks)
                    {
                        if (tempBookmark.CreatedOn.Value.Hour == ExpiresOn.Hour)
                        {
                            _storylineBookmarks.Add(tempBookmark);
                            continue;
                        }
                    }

                    return _storylineBookmarks;
                }

                return _storylineBookmarks;
            }
        }

        public DirectAgentMissionBookmark CourierMissionDropoffBookmark
        {
            get
            {
                if (Bookmarks == null)
                    return null;

                if (Bookmarks != null && Bookmarks.Count > 0)
                    return Bookmarks.Find(i => i.Title.Contains("Objective (Drop Off)") && i.AgentId == AgentId);

                //if (StorylineBookmarks != null && StorylineBookmarks.Any())
                //    return new DirectAgentMissionBookmark(StorylineBookmarks.FirstOrDefault(i => i.Title.Contains("Objective (Drop Off)"));

                return null;
            }
        }

        public DirectItem CourierMissionItemToMove
        {
            get
            {
                try
                {
                    if (_courierMissionItemToMove == null)
                    {
                        if (DirectEve.DictCurrentCourierMissionItemToMove.ContainsKey(Agent.AgentId))
                        {
                            _courierMissionItemToMove = DirectEve.DictCurrentCourierMissionItemToMove[Agent.AgentId];
                            return _courierMissionItemToMove;
                        }

                        if (DirectEve.DictCurrentCourierMissionItemToMove.Count == 0)
                        {
                            DirectEve.Log("DictCurrentCourierMissionItemToMove: if (DirectEve.DictCurrentCourierMissionItemToMove.Count == 0)");
                            //DirectEve.DictCurrentCourierMissionItemToMove.AddOrUpdate(Agent.AgentId, CourierMissionCtrlState.Start);
                            return null;
                        }

                        DirectEve.Log("DictCurrentCourierMissionItemToMove has no value in the dictionary for Agent [" + Agent.Name + "]");
                        int tempStateNum = 0;
                        DirectEve.Log("--------------------------------------");
                        foreach (KeyValuePair<long, DirectItem> tempCurrentCourierMissionItemToMove in DirectEve.DictCurrentCourierMissionItemToMove)
                        {
                            tempStateNum++;
                            DirectEve.Log("[" + tempStateNum + "][" + tempCurrentCourierMissionItemToMove.Key + "][" + tempCurrentCourierMissionItemToMove.Value + "]");
                        }

                        DirectEve.Log("--------------------------------------");
                        //DirectEve.Log("Error!: !if (State.DictCurrentCourierMissionCtrlState.ContainsKey(Agent.AgentId))");
                        return null;
                    }

                    return _courierMissionItemToMove;
                }
                catch (Exception ex)
                {
                    DirectEve.Log("Exception [" + ex + "]");
                    return null;
                }
            }
            set
            {
                try
                {
                    //DirectEve.Log("Mission [" + Name + "] New CourierMissionCtrlState [" + courierMbStateToSet + "] Old CourierMissionCtrlState was [" + PreviousCourierMissionCtrlState + "] ");
                    if (DirectEve.DictCurrentCourierMissionItemToMove.ContainsKey(Agent.AgentId))
                    {
                        DirectEve.DictCurrentCourierMissionItemToMove[Agent.AgentId] = value;
                        _courierMissionItemToMove = value;
                        return;
                    }

                    //WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.CourierItemTypeId), value);
                    DirectEve.DictCurrentCourierMissionItemToMove.AddOrUpdate(Agent.AgentId, value);
                    _courierMissionItemToMove = value;
                }
                catch (Exception ex)
                {
                    DirectEve.Log("Exception [" + ex + "]");
                }
            }
        }

        public DirectAgentMissionBookmark CourierMissionPickupBookmark
        {
            get
            {
                if (Bookmarks == null)
                    return null;

                if (Bookmarks != null && Bookmarks.Count > 0)
                    return Bookmarks.Find(i => i.Title.Contains("Objective (Pick Up)") && i.AgentId == AgentId);

                //if (StorylineBookmarks != null && StorylineBookmarks.Any())
                //    return StorylineBookmarks.FirstOrDefault(i => i.Title.Contains("Objective (Pick Up)"));

                return null;
            }
        }

        public CourierMissionCtrlState CurrentCourierMissionCtrlState
        {
            get
            {
                try
                {
                    if (DirectEve.DictCurrentCourierMissionCtrlState.ContainsKey(UniqueMissionId))
                        return DirectEve.DictCurrentCourierMissionCtrlState[UniqueMissionId];

                    if (DirectEve.DictCurrentCourierMissionCtrlState.Count == 0)
                    {
                        DirectEve.Log("CurrentCourierMissionCtrlState was not found: DictCurrentCourierMissionCtrlState was empty!");
                        DirectEve.DictCurrentCourierMissionCtrlState.AddOrUpdate(UniqueMissionId, CourierMissionCtrlState.Start);
                        return CourierMissionCtrlState.Start;
                    }
                    DirectEve.Log("CurrentCourierMissionCtrlState was blank: setting [" + CourierMissionCtrlState.Start + "] for Agent [" + Agent.Name + "]");
                    int tempStateNum = 0;
                    DirectEve.Log("--------------------------------------");
                    foreach (KeyValuePair<Tuple<long, string>, CourierMissionCtrlState> tempCourierMissionCtrlState in DirectEve.DictCurrentCourierMissionCtrlState)
                    {
                        tempStateNum++;
                        DirectEve.Log("[" + tempStateNum + "][" + tempCourierMissionCtrlState.Key + "][" + tempCourierMissionCtrlState.Value + "]");
                    }

                    DirectEve.Log("--------------------------------------");

                    DirectEve.DictCurrentCourierMissionCtrlState.AddOrUpdate(UniqueMissionId, CourierMissionCtrlState.Start);
                    if (DirectEve.DictCurrentCourierMissionCtrlState.ContainsKey(UniqueMissionId))
                        return DirectEve.DictCurrentCourierMissionCtrlState[UniqueMissionId];

                    //DirectEve.Log("Error!: !if (State.DictCurrentCourierMissionCtrlState.ContainsKey(Agent.AgentId))");
                    return CourierMissionCtrlState.Start;
                }
                catch (Exception ex)
                {
                    DirectEve.Log("Exception [" + ex + "]");
                    return CourierMissionCtrlState.Idle;
                }
            }
        }

        public CourierMissionCtrlState PreviousCourierMissionCtrlState
        {
            get
            {
                try
                {
                    if (DirectEve.DictPreviousCourierMissionCtrlState.ContainsKey(UniqueMissionId))
                        return DirectEve.DictPreviousCourierMissionCtrlState[UniqueMissionId];

                    if (DirectEve.DictPreviousCourierMissionCtrlState.Count == 0)
                    {
                        DirectEve.Log("PreviousCourierMissionCtrlState was not found: DictPreviousCourierMissionCtrlState was empty!");
                        DirectEve.DictPreviousCourierMissionCtrlState.AddOrUpdate(UniqueMissionId, CourierMissionCtrlState.Start);
                        return CourierMissionCtrlState.Start;
                    }
                    DirectEve.Log("PreviousCourierMissionCtrlState was blank: setting [" + CourierMissionCtrlState.Start + "] for Agent [" + Agent.Name + "]");
                    int tempStateNum = 0;
                    DirectEve.Log("--------------------------------------");
                    foreach (KeyValuePair<Tuple<long, string>, CourierMissionCtrlState> tempCourierMissionCtrlState in DirectEve.DictCurrentCourierMissionCtrlState)
                    {
                        tempStateNum++;
                        DirectEve.Log("[" + tempStateNum + "][" + tempCourierMissionCtrlState.Key + "][" + tempCourierMissionCtrlState.Value + "]");
                    }

                    DirectEve.Log("--------------------------------------");

                    DirectEve.DictPreviousCourierMissionCtrlState.AddOrUpdate(UniqueMissionId, CourierMissionCtrlState.Start);
                    if (DirectEve.DictPreviousCourierMissionCtrlState.ContainsKey(UniqueMissionId))
                        return DirectEve.DictPreviousCourierMissionCtrlState[UniqueMissionId];

                    //DirectEve.Log("Error!: !if (State.DictCurrentCourierMissionCtrlState.ContainsKey(Agent.AgentId))");
                    return CourierMissionCtrlState.Start;
                }
                catch (Exception ex)
                {
                    DirectEve.Log("Exception [" + ex + "]");
                    return CourierMissionCtrlState.Idle;
                }
            }
        }

        public DateTime ExpiresOn { get; private set; }

        private Faction _faction { get; set; }
        public Faction Faction
        {
            get
            {
                if (_faction == null)
                {
                    try
                    {
                        if (DirectEve.DictCurrentMissionFaction.ContainsKey(UniqueMissionId))
                            return DirectEve.DictCurrentMissionFaction[UniqueMissionId];


                        if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
                            return DirectNpcInfo.DefaultFaction;

                        //if (ESCache.Instance.InSpace)
                        //    return new Faction("Default", DamageType.EM, null, null, null);

                        if (Agent == null)
                        {
                            return DirectNpcInfo.DefaultFaction;
                        }

                        if (!DirectEve.Windows.OfType<DirectAgentWindow>().Any(w => w.AgentId == AgentId))
                        {
                            if (!DirectEve.Session.IsInSpace)
                            {
                                Log.WriteLine("DirectAgentMission: Faction: if (Agent.Window == null)");
                                if (!Agent.OpenAgentWindow(true)) return null;
                            }

                            return null;
                        }

                        if (Agent.AgentWindow != null && !Agent.AgentWindow.ObjectiveEmpty)
                        {
                            Log.WriteLine("Objective: " + Agent.AgentWindow.Objective);
                            if (Agent.AgentWindow.Objective.Contains("Anomic Team"))
                            {
                                Log.WriteLine("Faction: Anomic Team found in Objective");
                                if (Agent.AgentWindow.Objective.Contains("Destroy the Enyo")) //works
                                {
                                    Log.WriteLine("Faction: Destroy the Enyo found in Objective");
                                    _faction = new Faction("Enyo",-1, DamageType.EM, null, null, null, true, "n/a", "n/a", "n/a", "n/a");
                                    DirectEve.DictCurrentMissionFaction.AddOrUpdate(UniqueMissionId, _faction);
                                    return _faction;
                                }
                                else if (Agent.AgentWindow.Objective.Contains("Destroy the Jaguar")) //works
                                {
                                    Log.WriteLine("Faction: Destroy the Jaguar found in Objective");
                                    _faction = new Faction("Jaguar", -1, DamageType.EM, null, null, null, true, "n/a", "n/a", "n/a", "n/a");
                                    DirectEve.DictCurrentMissionFaction.AddOrUpdate(UniqueMissionId, _faction);
                                    return _faction;
                                }
                                else if (Agent.AgentWindow.Objective.Contains("Destroy the Vengeance"))
                                {
                                    Log.WriteLine("Faction: Destroy the Vengeance found in Objective");
                                    _faction = new Faction("Vengeance", -1, DamageType.EM, null, null, null, true, "n/a", "n/a", "n/a", "n/a");
                                    DirectEve.DictCurrentMissionFaction.AddOrUpdate(UniqueMissionId, _faction);
                                    return _faction;
                                }
                                else if (Agent.AgentWindow.Objective.Contains("Destroy the Hawk"))
                                {
                                    Log.WriteLine("Faction: Destroy the Hawk found in Objective");
                                    _faction = new Faction("Hawk", -1, DamageType.EM, null, null, null, true, "n/a", "n/a", "n/a", "n/a");
                                    DirectEve.DictCurrentMissionFaction.AddOrUpdate(UniqueMissionId, _faction);
                                    return _faction;
                                }
                                else
                                {
                                    DirectEve.Log("Mission [ Anomic Team ] - unable to find Faction Name: this mission flavor needs to be added. pausing");
                                    DirectEve.Log(Agent.AgentWindow.Objective);
                                    DirectEve.Log("Mission [ Anomic Team ] - unable to find Faction Name: this mission flavor needs to be added. pausing");
                                    ControllerManager.Instance.SetPause(true);
                                    return DirectNpcInfo.DefaultFaction;
                                }
                            }

                            if (Agent.AgentWindow.Objective.Contains("Anomic Agent"))
                            {
                                if (Agent.AgentWindow.Objective.Contains("Destroy the Guristas Burner"))
                                {
                                    Log.WriteLine("Faction: Destroy the Guristas Burner found in Objective"); //works
                                    _faction = new Faction("Worm", -1, DamageType.EM, null, null, null, true, "n/a", "n/a", "n/a", "n/a");
                                    DirectEve.DictCurrentMissionFaction.AddOrUpdate(UniqueMissionId, _faction);
                                    return _faction;
                                }
                                else if (Agent.AgentWindow.Objective.Contains("Destroy the Blood Raiders Burner")) //works
                                {
                                    Log.WriteLine("Faction: Destroy the Blood Raiders found in Objective");
                                    _faction = new Faction("Cruor", -1, DamageType.EM, null, null, null, true, "n/a", "n/a", "n/a", "n/a");
                                    DirectEve.DictCurrentMissionFaction.AddOrUpdate(UniqueMissionId, _faction);
                                    return _faction;
                                }
                                else if (Agent.AgentWindow.Objective.Contains("Destroy the Angel Cartel Burner"))
                                {
                                    Log.WriteLine("Faction: Destroy the Dramiel found in Objective");
                                    _faction = new Faction("Dramiel", -1, DamageType.EM, null, null, null, true, "n/a", "n/a", "n/a", "n/a");
                                    DirectEve.DictCurrentMissionFaction.AddOrUpdate(UniqueMissionId, _faction);
                                    return _faction;
                                }
                                else if (Agent.AgentWindow.Objective.Contains("Destroy the Serpentis Burner"))
                                {
                                    Log.WriteLine("Faction: Destroy the Daredevil found in Objective");
                                    _faction = new Faction("Daredevil", -1, DamageType.EM, null, null, null, true, "n/a", "n/a", "n/a", "n/a");
                                    DirectEve.DictCurrentMissionFaction.AddOrUpdate(UniqueMissionId, _faction);
                                    return _faction;
                                }
                                else if (Agent.AgentWindow.Objective.Contains("Destroy the Sansha's Nation Burner"))
                                {
                                    Log.WriteLine("Faction: Destroy the Sansha's Nation found in Objective");
                                    _faction = new Faction("Succubus", -1, DamageType.EM, null, null, null, true, "n/a", "n/a", "n/a", "n/a");
                                    DirectEve.DictCurrentMissionFaction.AddOrUpdate(UniqueMissionId, _faction);
                                    return _faction;
                                }
                            }

                            if (Agent.AgentWindow.Objective.Contains("Destroy the Rogue Drones") ||
                                Agent.AgentWindow.Objective.Contains("Rogue Drone Harassment Objectives") ||
                                Agent.AgentWindow.Objective.Contains("Air Show! Objectives") ||
                                Agent.AgentWindow.Objective.Contains("Alluring Emanations Objectives") ||
                                Agent.AgentWindow.Objective.Contains("Anomaly Objectives") ||
                                Agent.AgentWindow.Objective.Contains("Attack of the Drones Objectives") ||
                                Agent.AgentWindow.Objective.Contains("Drone Detritus Objectives") ||
                                Agent.AgentWindow.Objective.Contains("Drone Infestation Objectives") ||
                                Agent.AgentWindow.Objective.Contains("Evolution Objectives") ||
                                Agent.AgentWindow.Objective.Contains("Infected Ruins Objectives") ||
                                Agent.AgentWindow.Objective.Contains("Infiltrated Outposts Objectives") ||
                                Agent.AgentWindow.Objective.Contains("Mannar Mining Colony") ||
                                Agent.AgentWindow.Objective.Contains("Missing Convoy Objectives") ||
                                Agent.AgentWindow.Objective.Contains("Onslaught Objectives") ||
                                Agent.AgentWindow.Objective.Contains("Patient Zero Objectives") ||
                                Agent.AgentWindow.Objective.Contains("Persistent Pests Objectives") ||
                                Agent.AgentWindow.Objective.Contains("Portal to War Objectives") ||
                                Agent.AgentWindow.Objective.Contains("Rogue Eradication Objectives") ||
                                Agent.AgentWindow.Objective.Contains("Rogue Hunt Objectives") ||
                                Agent.AgentWindow.Objective.Contains("Rogue Spy Objectives") ||
                                Agent.AgentWindow.Objective.Contains("Roving Rogue Drones Objectives") ||
                                Agent.AgentWindow.Objective.Contains("Soothe The Salvage Beast") ||
                                Agent.AgentWindow.Objective.Contains("Wildcat Strike Objectives"))
                            {
                                _faction = DirectNpcInfo.RogueDronesFaction;
                                DirectEve.DictCurrentMissionFaction.AddOrUpdate(UniqueMissionId, _faction);
                                return _faction;
                            }

                            if (Agent.AgentWindow.Objective.Contains("Silence The Informant Objectives"))
                            {
                                _faction = DirectNpcInfo.MercenariesFaction;
                                DirectEve.DictCurrentMissionFaction.AddOrUpdate(UniqueMissionId, _faction);
                                return _faction;
                            }

                            if (Agent.AgentWindow.Objective.Contains("Gone Berserk Objectives"))
                            {
                                _faction = DirectNpcInfo.EoM;
                                DirectEve.DictCurrentMissionFaction.AddOrUpdate(UniqueMissionId, _faction);
                                return _faction;
                            }

                            if (Agent.AgentWindow.Objective.Contains("In the Midst of Deadspace (1 of 5)")
                             || Agent.AgentWindow.Objective.Contains("In the Midst of Deadspace (2 of 5)")
                             || Agent.AgentWindow.Objective.Contains("In the Midst of Deadspace (3 of 5)")
                             || Agent.AgentWindow.Objective.Contains("In the Midst of Deadspace (4 of 5)"))
                            {
                                _faction = DirectNpcInfo.AmarrEmpireFaction;
                                DirectEve.DictCurrentMissionFaction.AddOrUpdate(UniqueMissionId, _faction);
                                return _faction;
                            }

                            if (Agent.AgentWindow.Objective.Contains("In the Midst of Deadspace (5 of 5)"))
                            {
                                _faction = DirectNpcInfo.CaldariStateFaction;
                                DirectEve.DictCurrentMissionFaction.AddOrUpdate(UniqueMissionId, _faction);
                                return _faction;
                            }

                            if (Agent.AgentWindow.Objective.Contains("The Damsel In Distress Objectives"))
                            {
                                _faction = DirectNpcInfo.EoM;
                                DirectEve.DictCurrentMissionFaction.AddOrUpdate(UniqueMissionId, _faction);
                                return _faction;
                            }

                            if (Agent.AgentWindow.Objective.Contains("Worlds Collide Objectives"))
                            {
                                _faction = DirectNpcInfo.SanshasNationFaction;
                                DirectEve.DictCurrentMissionFaction.AddOrUpdate(UniqueMissionId, _faction);
                                return _faction;
                            }

                            _faction = DetermineFactionFromFactionLogo(Agent.AgentWindow.Objective);
                            if (_faction.Name.ToLower().Contains("Default".ToLower()))
                            {
                                DirectEve.Log("Unable to find the faction for [" + Name + "] when searching through the html (listed below)");
                                DirectEve.Log(Agent.AgentWindow.Objective);
                                return DirectNpcInfo.DefaultFaction;
                            }

                            DirectEve.DictCurrentMissionFaction.AddOrUpdate(UniqueMissionId, _faction);
                            return _faction;
                        }

                        Log.WriteLine("Unable to find the faction for [" + Name + "] the objectiveHtml was blank!");
                        if (DirectEve.Session.IsInDockableLocation && Agent.StationId == DirectEve.Session.StationId)
                        {
                            if (Agent.AgentWindow != null)
                                if (Agent.AgentWindow.ViewMode == "SinglePaneView" && Agent.AgentWindow.Buttons.Any(i => i.Type == AgentButtonType.VIEW_MISSION))
                                    if (DateTime.UtcNow > Time.Instance.NextWindowAction)
                                    {
                                        if (Agent.AgentWindow.Buttons.Find(button => button.Type == AgentButtonType.VIEW_MISSION).Click())
                                        {
                                            Log.WriteLine("if (Agent.Window.Buttons.FirstOrDefault(button => button.Type == ButtonType.VIEW_MISSION).Click())");
                                            Time.Instance.NextWindowAction = DateTime.UtcNow.AddSeconds(1);
                                            return DirectNpcInfo.DefaultFaction;
                                        }
                                    }
                        }

                        return DirectNpcInfo.DefaultFaction;
                    }
                    catch (Exception ex)
                    {
                        DirectEve.Log("Exception [" + ex + "]");
                        return DirectNpcInfo.DefaultFaction;
                    }
                }

                return _faction;
            }
        }

        public List<DamageType> BestDamagesTypesToShoot
        {
            get
            {
                if (Faction != null)
                {
                    if (Faction.Name.ToLower().Contains("Default".ToLower()))
                        return new List<DamageType>();

                    if (Faction.BestDamageTypesToShoot != null && Faction.BestDamageTypesToShoot.Count > 0)
                        return Faction.BestDamageTypesToShoot;
                }

                return new List<DamageType>();
            }
        }

        public long? MissionHint_TravelToLocationId
        {
            get
            {
                try
                {
                    string tempGetMissionInfo = GetAgentMissionRawCsvHint();
                    if (tempGetMissionInfo.Contains("TravelTo"))
                    {
                        string[] stringMissionHint = tempGetMissionInfo.Split(',');
                        foreach (string stringMissionHintElement in stringMissionHint)
                        {
                            if (stringMissionHintElement.Contains("TravelTo"))
                                continue;

                            long tempLocationId = long.Parse(stringMissionHintElement);
                            return tempLocationId;
                        }
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    DirectEve.Log("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        public long? MissionHint_ApproachItemId
        {
            get
            {
                try
                {
                    string tempGetMissionInfo = GetAgentMissionRawCsvHint();
                    if (tempGetMissionInfo.Contains("Approach"))
                    {
                        string[] stringMissionHint = tempGetMissionInfo.Split(',');
                        int intCount = 0;
                        foreach (string stringMissionHintElement in stringMissionHint)
                        {
                            //
                            //Approach                                  TypeID and ItemID
                            //
                            intCount++;
                            if (2 > intCount) continue;

                            if (intCount == 2)
                            {
                                long tempLocationId = long.Parse(stringMissionHintElement);
                                return tempLocationId;
                            }
                        }
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    DirectEve.Log("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        public int? MissionHint_MissionFetchTypeId
        {
            get
            {
                try
                {
                    string tempGetMissionInfo = GetAgentMissionRawCsvHint();
                    if (tempGetMissionInfo.Contains("MissionFetch"))
                    {
                        string[] stringMissionHint = tempGetMissionInfo.Split(',');
                        foreach (string stringMissionHintElement in stringMissionHint)
                        {
                            if (stringMissionHintElement.Contains("MissionFetch"))
                                continue;

                            int tempTypeId = int.Parse(stringMissionHintElement);
                            return tempTypeId;
                        }
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    DirectEve.Log("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        public int MissionHint_TransportItemMissingTypeId
        {
            get
            {
                try
                {
                    string tempGetMissionInfo = GetAgentMissionRawCsvHint();
                    if (tempGetMissionInfo.Contains("TransportItemsMissing"))
                    {
                        string[] stringMissionHint = tempGetMissionInfo.Split(',');
                        foreach (string stringMissionHintElement in stringMissionHint)
                        {
                            if (stringMissionHintElement.Contains("TransportItemsMissing"))
                                continue;

                            int tempTypeId = int.Parse(stringMissionHintElement);
                            WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.CourierItemTypeId), tempTypeId);
                            return tempTypeId;
                        }
                    }

                    return 0;
                }
                catch (Exception ex)
                {
                    DirectEve.Log("Exception [" + ex + "]");
                    return 0;
                }
            }
        }


        public int? MissionHint_MissionFetchContainerTypeId
        {
            get
            {
                try
                {
                    string tempGetMissionInfo = GetAgentMissionRawCsvHint();
                    if (tempGetMissionInfo.Contains("MissionFetchContainer"))
                    {
                        string[] stringMissionHint = tempGetMissionInfo.Split(',');
                        int intCount = 0;
                        foreach (string stringMissionHintElement in stringMissionHint)
                        {
                            //
                            //MissionFetchContainer                     TypeID and ContainerID
                            //
                            intCount++;
                            if (2 > intCount) continue;

                            if (intCount == 2)
                            {
                                int? tempContainerTypeId = int.Parse(stringMissionHintElement);
                                return tempContainerTypeId;
                            }

                            continue;
                        }
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    DirectEve.Log("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        public long? MissionHint_MissionFetchContainerId
        {
            get
            {
                try
                {
                    string tempGetMissionInfo = GetAgentMissionRawCsvHint();
                    if (tempGetMissionInfo.Contains("MissionFetchContainer"))
                    {
                        string[] stringMissionHint = tempGetMissionInfo.Split(',');
                        long? tempContainerId = 0;
                        int intCount = 0;
                        foreach (string stringMissionHintElement in stringMissionHint)
                        {
                            //
                            //MissionFetchContainer                     TypeID and ContainerID
                            //
                            intCount++;
                            if (2 > intCount) continue;

                            if (intCount == 2)
                            {
                                tempContainerId = int.Parse(stringMissionHintElement);
                                return tempContainerId;
                            }

                            return null;
                        }
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    DirectEve.Log("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        public bool? IsMissionFinished
        {
            get
            {
                try
                {
                    if (ESCache.Instance.InSpace && ESCache.Instance.InMission && Name.ToLower().Contains("Anomic".ToLower()))
                        return false;

                    if (GetAgentMissionRawCsvHint().ToLower().Contains("AllObjectivesComplete".ToLower()))
                    {
                        //if (Name.ToLower().Contains("The Blockade".ToLower()))
                        //    return false;

                        return true;
                    }

                    if (GetAgentMissionRawCsvHint().ToLower().Contains("FetchObjectAcquiredDungeonDone".ToLower()))
                        return true;

                    if (ESCache.Instance.InStation)
                    {
                        if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
                        {
                            return false;
                        }

                        if (Agent.AgentWindow == null) return false;
                        if (Agent.AgentWindow.Objective.Contains("Objectives Complete"))
                            return true;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        public string GetAgentMissionRawCsvHint()
        {
            //TravelTo                                  LocationID
            //MissionFetch                              TypeID
            //MissionFetchContainer                     TypeID and ContainerID
            //MissionFetchMine                          TypeID and Quantity
            //MissionFetchMineTrigger                   TypeID
            //MissionFetchTarget                        TypeID and TargetTypeID
            //AllObjectivesComplete                     AgentID
            //TransportItemsPresent                     TypeID and StationID
            //TransportItemsMissing                     TypeID
            //FetchObjectAcquiredDungeonDone            TypeID, AgentID, and StationID
            //GoToGate                                  ItemID
            //KillTrigger                               TypeID, ItemID, and EventTypeName
            //DestroyLCSAndAll                          TypeID and ItemID
            //Destroy                                   TypeID and ItemID
            //Attack                                    TypeID and ItemID
            //Approach                                  TypeID and ItemID
            //Hack                                      TypeID and ItemID
            //Salvage                                   TypeID and ItemID
            //DestroyAll                                None

            string ret = string.Empty;

            if (!Agent.IsValid) return ret;

            PyObject obj = DirectEve.GetLocalSvc("missionObjectivesTracker").Attribute("currentAgentMissionInfo");

            if (obj == null || !obj.IsValid) return ret;

            Dictionary<long, PyObject> dict = obj.ToDictionary<long>();

            if (dict.ContainsKey(AgentId))
                ret = dict[AgentId].ToUnicodeString();

            return ret ?? string.Empty;
        }
        public bool ObjectivesComplete
        {
            get
            {
                bool objectivesComplete = GetAgentMissionRawCsvHint().ToLower().Contains("FetchObjectAcquiredDungeonDone".ToLower())
                                          || GetAgentMissionRawCsvHint().ToLower().Contains("AllObjectivesComplete".ToLower());

                return objectivesComplete;
            }
        }

        public bool Important { get; internal set; }

        public int? JumpsToDropoffBookmark
        {
            get
            {
                if (CourierMissionDropoffBookmark != null)
                {
                    if (CourierMissionDropoffBookmark.SolarSystemId == DirectEve.Session.SolarSystemId)
                        return 0;

                    return CourierMissionDropoffBookmark.SolarSystem.JumpsHighSecOnly;
                }

                return null;
            }
        }

        public int? JumpsToPickupBookmark
        {
            get
            {
                if (CourierMissionPickupBookmark != null)
                {
                    if (CourierMissionPickupBookmark.SolarSystemId == DirectEve.Session.SolarSystemId)
                        return 0;

                    return CourierMissionPickupBookmark.SolarSystem.JumpsHighSecOnly;
                }

                return null;
            }
        }

        public int Level => Agent.Level;

        public bool MissionInfoNeverShowsMissionComplete
        {
            get
            {
                switch (Level)
                {
                    case 1:
                        break;

                    case 2:
                        switch (Name)
                        {
                            case "Recon (1 of 3)":
                                return true;
                        }
                        break;

                    case 3:
                        break;

                    case 4:
                        break;

                    case 5:
                        break;
                }

                return false;
            }
        }

        public string Name { get; internal set; }

        public MissionState State { get; internal set; }
        //public int State { get; internal set; }

        public string Type { get; internal set; }

        public Tuple<long, string> UniqueMissionId
        {
            get
            {
                if (!string.IsNullOrEmpty(Name))
                    return new Tuple<long, string>(Agent.AgentId, Name + ExpiresOn);

                return new Tuple<long, string>(0, string.Empty);
            }
        }

        public bool WeAreDockedAtDropOffLocation
        {
            get
            {
                if (CourierMissionDropoffBookmark != null)
                {
                    if (CourierMissionDropoffBookmark.Station.Name == DirectEve.Session.Station.Name)
                        return true;

                    return false;
                }

                return false;
            }
        }

        public bool WeAreDockedAtPickupLocation
        {
            get
            {
                if (CourierMissionPickupBookmark != null)
                {
                    if (CourierMissionPickupBookmark.Station.Name == DirectEve.Session.Station.Name)
                        return true;

                    return false;
                }

                return false;
            }
        }

        private DirectContainer _courierMissionFromContainer
        {
            get
            {
                if (CourierMissionFromContainerName.ToLower() == "ItemHangar".ToLower())
                    return DirectContainer.GetItemHangar(DirectEve);

                if (CourierMissionFromContainerName.ToLower() == "CargoHold".ToLower())
                    return DirectContainer.GetShipsCargo(DirectEve);

                return null;
            }
        }

        private DirectContainer _courierMissionToContainer
        {
            get
            {
                if (CourierMissionToContainerName.ToLower() == "ItemHangar".ToLower())
                    return DirectContainer.GetItemHangar(DirectEve);

                if (CourierMissionToContainerName.ToLower() == "CargoHold".ToLower())
                    return DirectContainer.GetShipsCargo(DirectEve);

                return null;
            }
        }

        public bool ChangeCourierMissionCtrlState(CourierMissionCtrlState courierMbStateToSet)
        {
            try
            {
                //Dictionary<Tuple<long, string>, CourierMissionCtrlState> tempDictionary = DirectEve.DictCurrentCourierMissionCtrlState;
                //foreach (KeyValuePair<Tuple<long, string>, CourierMissionCtrlState> kvp in tempDictionary)
                //    //Do we have ANY mission matching the entry in the Dictionary
                //    if (DirectEve.AgentMissions.All(i => i.AgentId != kvp.Key.Item1))
                //        DirectEve.DictCurrentCourierMissionCtrlState.Remove(kvp.Key);

                if (CurrentCourierMissionCtrlState != courierMbStateToSet)
                {
                    DirectEve.DictPreviousCourierMissionCtrlState.AddOrUpdate(UniqueMissionId, CurrentCourierMissionCtrlState);
                    Log.WriteLine("Mission [" + Name + "] New CourierMissionCtrlState [" + courierMbStateToSet + "] Old CourierMissionCtrlState was [" + PreviousCourierMissionCtrlState + "] ");
                    DirectEve.DictCurrentCourierMissionCtrlState.AddOrUpdate(UniqueMissionId, courierMbStateToSet);
                    return true;
                }
            }
            catch (Exception ex)
            {
                DirectEve.Log("Exception [" + ex + "]");
                return false;
            }

            return true;
        }

        public DirectContainer CourierMissionFromContainer
        {
            get
            {
                if (_courierMissionFromContainer == null)
                {
                    if (CurrentCourierMissionCtrlState == CourierMissionCtrlState.PickupItem ||
                        CurrentCourierMissionCtrlState == CourierMissionCtrlState.GotoPickupLocation ||
                        CurrentCourierMissionCtrlState == CourierMissionCtrlState.TryToGrabPickupItemsFromHomeStation)
                    {
                        CourierMissionFromContainerName = "ItemHangar";
                        CourierMissionToContainerName = "CargoHold";
                        return _courierMissionFromContainer;
                    }

                    if (CurrentCourierMissionCtrlState == CourierMissionCtrlState.DropOffItem) //otherwise you are dropping off
                    {
                        CourierMissionFromContainerName = "CargoHold";
                        CourierMissionToContainerName = "ItemHangar";
                        return _courierMissionFromContainer;
                    }

                    CourierMissionFromContainerName = string.Empty;
                    Log.WriteLine("CourierMissionFromContainer == null: CurrentCourierMissionCtrlState: [" + CurrentCourierMissionCtrlState + "]");
                    return null;
                }

                return _courierMissionFromContainer;
            }
        }

        public bool? LowSecWarning
        {
            get
            {
                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
                {
                    return false;
                }

                if (Agent.AgentWindow == null) return false;
                return Agent.AgentWindow.LowSecWarning;
            }
        }

        public bool? RouteContainsLowSecuritySystems
        {
            get
            {
                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
                {
                    return false;
                }

                if (Agent.AgentWindow == null) return false;
                return Agent.AgentWindow.RouteContainsLowSecuritySystems;
            }
        }

        public string CourierMissionToContainerName
        {
            get
            {
                if (DirectEve.DictCurrentCourierMissionToContainer.ContainsKey(Agent.AgentId))
                {
                    string tempHangarName = DirectEve.DictCurrentCourierMissionToContainer[Agent.AgentId];
                    return tempHangarName;
                }

                return string.Empty;
            }
            set
            {
                DirectEve.DictCurrentCourierMissionToContainer.AddOrUpdate(Agent.AgentId, value);
            }
        }

        public string CourierMissionFromContainerName
        {
            get
            {
                if (DirectEve.DictCurrentCourierMissionFromContainer.ContainsKey(Agent.AgentId))
                {
                    string tempHangarName = DirectEve.DictCurrentCourierMissionFromContainer[Agent.AgentId];
                    return tempHangarName;
                }

                return string.Empty;
            }
            set
            {
                DirectEve.DictCurrentCourierMissionFromContainer.AddOrUpdate(Agent.AgentId, value);
            }
        }

        public DirectContainer CourierMissionToContainer
        {
            get
            {
                if (_courierMissionToContainer == null)
                {
                    if (Agent.Mission.CurrentCourierMissionCtrlState == CourierMissionCtrlState.PickupItem ||
                        Agent.Mission.CurrentCourierMissionCtrlState == CourierMissionCtrlState.TryToGrabPickupItemsFromHomeStation)
                    {
                        CourierMissionToContainerName = "CargoHold";
                        CourierMissionFromContainerName = "ItemHangar";
                        return _courierMissionToContainer;
                    }

                    if (Agent.Mission.CurrentCourierMissionCtrlState == CourierMissionCtrlState.DropOffItem) //otherwise you are dropping off
                    {
                        CourierMissionToContainerName = "ItemHangar";
                        CourierMissionFromContainerName = "CargoHold";
                        return _courierMissionToContainer;
                    }

                    CourierMissionToContainerName = string.Empty;
                    Log.WriteLine("CourierMissionToContainer == null: CurrentCourierMissionCtrlState: [" + Agent.Mission.CurrentCourierMissionCtrlState + "]");
                    return null;
                }

                return _courierMissionToContainer;
            }
        }

        private Faction DetermineFactionFromFactionLogo(string objectiveHtml)
        {
            //if (Agent.Window.ObjectiveEmpty)
            //{
            //    Agent.PressViewButtonIfItExists("GetFactionType");
            //    return null;
            //}

            Regex logoRegex = new Regex("img src=\"factionlogo:(?<factionlogo>\\d+)");
            Match logoMatch = logoRegex.Match(objectiveHtml);
            if (logoMatch.Success)
            {
                string factionid = logoMatch.Groups["factionlogo"].Value;
                Faction _tempfaction = null;
                DirectNpcInfo.FactionIdsToFactions.TryGetValue(factionid, out _tempfaction);
                if (_tempfaction == null)
                    return DirectNpcInfo.DefaultFaction;

                return _tempfaction;
            }

            return DirectNpcInfo.DefaultFaction;
        }

        #endregion Properties

        #region Methods



        //public bool ObjectiveEmpty => Agent.Window.Objective?.Equals("<html><body></body></html>") ?? true;

        /// <summary>
        ///     Ensure the journal mission tab is open before RemoveOffer is called
        /// </summary>
        /// <returns></returns>
        public bool RemoveOffer()
        {
            if (State != (MissionState)(int)PySharp.Import("__builtin__").Attribute("const").Attribute("agentMissionStateOffered"))
                return false;

            return DirectEve.ThreadedLocalSvcCall("agents", "RemoveOfferFromJournal", AgentId);
        }

        internal static List<DirectAgentMission> GetAgentMissions(DirectEve directEve)
        {
            List<DirectAgentMission> missions = new List<DirectAgentMission>();

            List<PyObject> pyMissions = directEve.GetLocalSvc("journal").Attribute("agentjournal").GetItemAt(0).ToList();

            foreach (PyObject pyMission in pyMissions)
            {
                DirectAgentMission mission = new DirectAgentMission(directEve)
                {
                    State = (MissionState)(int)pyMission.GetItemAt(0),
                    Important = (bool)pyMission.GetItemAt(1),
                    Type = (string)pyMission.GetItemAt(2),
                    //_pyAgentId = pyMission.Item(4),
                    AgentId = (long)pyMission.GetItemAt(4),
                    ExpiresOn = (DateTime)pyMission.GetItemAt(5),
                    Bookmarks = pyMission.GetItemAt(6).ToList().Select(b => new DirectAgentMissionBookmark(directEve, b)).ToList()
                    //7 = False
                    //8 = False
                    //9 = 3 digit int
                };

                int messageId = (int)pyMission.GetItemAt(3);
                if (messageId > 0)
                {
                    mission.Name = directEve.GetLocalizationMessageById(messageId);
                }
                else
                {
                    mission.Name = "none";
                    continue;
                }

                missions.Add(mission);
            }

            return missions;
        }

        #endregion Methods
    }
}