extern alias SC;

using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Debug;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using SC::SharedComponents.EVE;
using SC::SharedComponents.EVE.ClientSettings;
using SC::SharedComponents.Extensions;
using SC::SharedComponents.IPC;
using SC::SharedComponents.Py;
using SC::SharedComponents.Utility;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectAgent : DirectObject
    {
        #region Constructors

        internal DirectAgent(DirectEve directEve) : base(directEve)
        {
        }

        #endregion Constructors

        #region Fields

        private DateTime _agentWindowLastReady;
        private DateTime _lastAgentWindowInteraction;

        public static readonly Dictionary<long, string> Divisions = new Dictionary<long, string>
        {
            {1, "Accounting"},
            {2, "Administration"},
            {3, "Advisory"},
            {4, "Archives"},
            {5, "Astrosurveying"},
            {6, "Command"},
            {7, "Distribution"},
            {8, "Financial"},
            {9, "Intelligence"},
            {10, "Internal Security"},
            {11, "Legal"},
            {12, "Manufacturing"},
            {13, "Marketing"},
            {14, "Mining"},
            {15, "Personnel"},
            {16, "Production"},
            {17, "Public Relations"},
            {18, "R&D"},
            {19, "Security"},
            {20, "Storage"},
            {21, "Surveillance"},
            {22, "Distribution"},
            {23, "Mining"},
            {24, "Security"},
            {25, "Business"},
            {26, "Exploration"},
            {27, "Industry"},
            {28, "Military"},
            {29, "Advanced Military"}
        };

        public static readonly Dictionary<long, string> AgentTypes = new Dictionary<long, string>
        {
            {1, "agentTypeNonAgent"},
            {2, "agentTypeBasicAgent"},
            {3, "agentTypeTutorialAgent"},
            {4, "agentTypeResearchAgent"},
            {6, "agentTypeGenericStorylineMissionAgent"},
            {7, "agentTypeStorylineMissionAgent"},
            {8, "agentTypeEventMissionAgent"},
            {9, "agentTypeFactionalWarfareAgent"},
            {10, "agentTypeGenagentTypeEpicArcAgentericStorylineMissionAgent"},
            {11, "agentTypeAura"},
            {12, "agentTypeCareerAgent"}
        };

        private static Dictionary<string, long> AgentLookupDict = new Dictionary<string, long>();

        private readonly bool DebugAgentInfo = false;

        private readonly bool DebugSkillQueue = false;

        public bool CloseConversation()
        {
            if (IsValid)
            {
                DirectEve.Log("DirectAgent: CloseConversation: if(IsValid)");
                return true;
            }

            if (AgentWindow != null)
            {
                if (!AgentWindow.IsReady)
                    return false;

                if (AgentWindow.Close())
                {
                    _lastAgentWindowInteraction = DateTime.MinValue;
                    DirectEve.Log("DirectAgent: CloseConversation: Closing Agent Window");
                    return true;
                }

                return false;
            }

            if (CareerAgentWindow != null)
            {
                if (!CareerAgentWindow.IsReady)
                    return false;

                if (CareerAgentWindow.Close())
                {
                    _lastAgentWindowInteraction = DateTime.MinValue;
                    DirectEve.Log("DirectAgent: CloseConversation: Closing CareerAgentWindow Window");
                    return true;
                }

                return false;
            }

            return false;
        }

        #endregion Fields

        #region Properties

        public static readonly Dictionary<int, double> AGENT_LEVEL_REQUIRED_STANDING = new Dictionary<int, double>()
        {
            {1,-11.0},
            {2,1.0},
            {3,3.0},
            {4,5.0},
            {5,7.0},
        };

        private bool DebugAgentInteractionReplyToAgent = DebugConfig.DebugAgentInteractionReplyToAgent;
        private float? _agentCorpEffectiveStandingtoMe;
        private float? _agentEffectiveStandingtoMe;
        private float? _agentFactionEffectiveStandingtoMe;
        private float? _maximumStandingUsedToAccessAgent;
        public static float StandingsNeededToAccessLevel1Agent { get; set; } = -11;

        public static float StandingsNeededToAccessLevel2Agent { get; set; } = 1;

        public static float StandingsNeededToAccessLevel3Agent { get; set; } = 3;

        public static float StandingsNeededToAccessLevel4Agent { get; set; } = 5;

        public static float StandingsNeededToAccessLevel5Agent { get; set; } = 7;

        public float AgentCorpEffectiveStandingtoMe
        {
            get
            {
                try
                {
                    if (_agentCorpEffectiveStandingtoMe == null)
                    {
                        _agentCorpEffectiveStandingtoMe = DirectEve.Standings.EffectiveStanding(CorpId, (long)DirectEve.Session.CharacterId);
                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.AgentCorpStandings), _agentCorpEffectiveStandingtoMe);
                    }

                    return (float)_agentCorpEffectiveStandingtoMe;
                }
                catch (Exception ex)
                {
                    DirectEve.Log("Exception [" + ex + "]");
                    return 0;
                }
            }
        }

        public float AgentEffectiveStandingtoMe
        {
            get
            {
                try
                {
                    if (_agentEffectiveStandingtoMe == null)
                    {
                        _agentEffectiveStandingtoMe = DirectEve.Standings.EffectiveStanding(AgentId, (long)DirectEve.Session.CharacterId);
                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.AgentStandings), _agentEffectiveStandingtoMe);
                    }

                    return (float)_agentEffectiveStandingtoMe;
                }
                catch (Exception ex)
                {
                    DirectEve.Log("Exception [" + ex + "]");
                    return 0;
                }
            }
        }

        public string AgentEffectiveStandingtoMeText => MaximumStandingUsedToAccessAgent.ToString("0.00");

        public float AgentFactionEffectiveStandingtoMe
        {
            get
            {
                try
                {
                    if (_agentFactionEffectiveStandingtoMe == null)
                    {
                        _agentFactionEffectiveStandingtoMe = DirectEve.Standings.EffectiveStanding(FactionId, (long)DirectEve.Session.CharacterId);
                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.AgentFactionStandings), _agentFactionEffectiveStandingtoMe);
                    }

                    return (float)_agentFactionEffectiveStandingtoMe;
                }
                catch (Exception ex)
                {
                    DirectEve.Log("Exception [" + ex + "]");
                    return 0;
                }
            }
        }

        public long AgentId { get; private set; }

        public long AgentTypeId { get; private set; }

        public string AgentTypeName
        {
            get
            {
                try
                {
                    string _agentTypeName = string.Empty;
                    AgentTypes.TryGetValue(AgentTypeId, out _agentTypeName);
                    if (!string.IsNullOrEmpty(_agentTypeName))
                        return _agentTypeName;

                    return "unknown";
                }
                catch (Exception)
                {
                    return "unknown.";
                }
            }
        }

        public long BloodlineId { get; private set; }

        public bool IsLocatorAgent { get; private set; }

        public bool CanAccessAgent
        {
            get
            {
                if (AGENT_LEVEL_REQUIRED_STANDING.TryGetValue(Level, out var s))
                {
                    if (Level == 1)
                        return true;
                    double min = MinEffectiveStanding;
                    double max = MaxEffectiveStanding;

                    if (min < -1.99)
                        return false;

                    return max > s;
                }
                return false;
            }
        }

        public long CorpId { get; private set; }
        public long DivisionId { get; private set; }

        public string DivisionName { get; set; }

        public double EffectiveAgentStanding => DirectEve.Standings.EffectiveStanding(AgentId, DirectEve.Session.CharacterId ?? -1);
        public double EffectiveCorpStanding => DirectEve.Standings.EffectiveStanding(CorpId, DirectEve.Session.CharacterId ?? -1);
        public double EffectiveFactionStanding => DirectEve.Standings.EffectiveStanding(FactionId, DirectEve.Session.CharacterId ?? -1);
        public long FactionId { get; private set; }

        public string FactionName
        {
            get
            {
                try
                {
                    string _factionName = string.Empty;
                    DirectNpcInfo.FactionIdsToFactionNames.TryGetValue(FactionId.ToString(), out _factionName);
                    if (!string.IsNullOrEmpty(_factionName))
                        return _factionName;

                    return "unknown";
                }
                catch (Exception)
                {
                    return "unknown.";
                }
            }
        }

        public Faction FactionOfAgent
        {
            get
            {
                try
                {
                    Faction _faction = null;
                    DirectNpcInfo.FactionIdsToFactions.TryGetValue(FactionId.ToString(), out _faction);
                    return _faction;
                }
                catch (Exception)
                {
                    return DirectNpcInfo.DefaultFaction;
                }
            }
        }

        public bool Gender { get; private set; }

        public bool HaveStandingsToAccessToThisAgent
        {
            get
            {
                if (MaximumStandingUsedToAccessAgent > EffectiveStandingNeededToAccessAgent())
                    return true;

                return false;
            }
        }

        public bool IsAgentMissionAccepted
        {
            get
            {
                if (DirectEve.AgentMissions.Any(i => i.Agent.AgentId == AgentId && i.State == MissionState.Accepted))
                    return true;

                return false;
            }
        }

        public bool IsAgentMissionExists
        {
            get
            {
                if (DirectEve.AgentMissions.Any(i => i.Agent.AgentId == AgentId))
                    return true;

                return false;
            }
        }

        public bool IsValid { get; private set; }

        public int Level { get; private set; }

        public int? LoyaltyPoints
        {
            get
            {
                int? ret = null;
                var wallet = DirectEve.Windows.OfType<DirectWalletWindow>().FirstOrDefault();
                if (wallet == null)
                {
                    if (ESCache.Instance.InStation)
                    {
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenWallet);
                        Log.WriteLine($"Opening wallet.");
                        return ret ?? 0;
                    }

                    return ret ?? 0;
                }

                var lpSvc = DirectEve.GetLocalSvc("loyaltyPointsWalletSvc");
                if (lpSvc.IsValid)
                {
                    var mappings = lpSvc.Call("GetAllCharacterLPBalancesExcludingEvermarks").ToList();

                    foreach (var mapping in mappings)
                    {
                        if ((int)mapping.GetItemAt(0) != CorpId)
                            continue;

                        return (int)mapping.GetItemAt(1);
                    }

                    return ret ?? 0;
                }

                return ret ?? 0;
            }
        }

        public double MaxEffectiveStanding => Math.Max(Math.Max(EffectiveAgentStanding, EffectiveCorpStanding), EffectiveFactionStanding);

        public float MaximumStandingUsedToAccessAgent
        {
            get
            {
                try
                {
                    if (_maximumStandingUsedToAccessAgent == null)
                    {
                        if (AgentCorpEffectiveStandingtoMe < -2)
                            return Math.Min(AgentCorpEffectiveStandingtoMe, AgentFactionEffectiveStandingtoMe);

                        if (AgentFactionEffectiveStandingtoMe < -2)
                            return Math.Min(AgentCorpEffectiveStandingtoMe, AgentFactionEffectiveStandingtoMe);

                        _maximumStandingUsedToAccessAgent = Math.Max(AgentEffectiveStandingtoMe, Math.Max(AgentCorpEffectiveStandingtoMe, AgentFactionEffectiveStandingtoMe));
                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.StandingUsedToAccessAgent), _maximumStandingUsedToAccessAgent);
                    }

                    return (float)_maximumStandingUsedToAccessAgent;
                }
                catch (Exception ex)
                {
                    DirectEve.Log("Exception [" + ex + "]");
                    return 0;
                }
            }
        }

        public double MinEffectiveStanding => Math.Min(Math.Min(EffectiveAgentStanding, EffectiveCorpStanding), EffectiveFactionStanding);

        public DirectAgentMission Mission
        {
            get
            {
                if (DirectEve.AgentMissions != null && DirectEve.AgentMissions.Count > 0)
                {
                    if (DirectEve.AgentMissions.Any(i => i.AgentId == AgentId))
                    {
                        DirectAgentMission thisAgentMission = DirectEve.AgentMissions.Find(i => i.AgentId == AgentId);
                        return thisAgentMission;
                    }

                    return null;
                }

                return null;
            }
        }

        public string Name
        {
            get
            {
                DirectOwner owner = DirectOwner.GetOwner(DirectEve, AgentId);
                if (owner == null)
                    return string.Empty;

                return owner.Name;
            }
        }

        public int Quality { get; private set; }

        public DirectSolarSystem SolarSystem => DirectEve.SolarSystems[(int)SolarSystemId];

        public long SolarSystemId { get; private set; }

        public DirectStation Station
        {
            get
            {
                if (DirectEve.Stations.Count > 0)
                {
                    DirectStation _station = null;
                    DirectEve.Stations.TryGetValue((int)StationId, out _station);
                    if (_station != null) return _station;
                    return null;
                }

                return null;
            }
        }

        public long StationId { get; private set; }

        public string StationName => DirectEve.GetLocationName(StationId);
        public DirectSolarSystem System => DirectEve.SolarSystems[(int)SolarSystemId];

        public DirectAgentWindow AgentWindow
        {
            get
            {
                if (DebugConfig.DebugAgentInteractionReplyToAgent)
                {
                    if (DirectEve.Windows.OfType<DirectAgentWindow>().Any(i => i.IsReady))
                    {
                        foreach (var AgentWindow in DirectEve.Windows.OfType<DirectAgentWindow>().Where(i => i.IsReady))
                        {
                            if (DirectEve.Interval(15000, 15000, AgentWindow.WindowId)) Log.WriteLine("AgentWindow: GUID [" + AgentWindow.Guid + "] Name [" + AgentWindow.Name + "] WindowID [" + AgentWindow.WindowId + "] AgentID [" + AgentWindow.AgentId + "]");
                        }
                    }
                    else Log.WriteLine("No DirectAgentWindow found");
                }

                return DirectEve.Windows.OfType<DirectAgentWindow>().FirstOrDefault(w => (w.AgentId == AgentId || w.AgentId == 0) && w.IsReady);
            }
        }

        public DirectCareerAgentWindow CareerAgentWindow
        {
            get
            {
                if (DebugConfig.DebugAgentInteractionReplyToAgent)
                {
                    if (DirectEve.Windows.OfType<DirectCareerAgentWindow>().Any())
                    {
                        foreach (var AgentWindow in DirectEve.Windows.OfType<DirectCareerAgentWindow>().Where(i => i.IsReady))
                        {
                            if (DirectEve.Interval(15000, 15000, AgentWindow.WindowId)) Log.WriteLine("AgentWindow: GUID [" + AgentWindow.Guid + "] Name [" + AgentWindow.Name + "] WindowID [" + AgentWindow.WindowId + "] AgentID [" + AgentWindow.AgentId + "]");
                        }
                    }
                    else Log.WriteLine("No DirectCareerAgentWindow found");
                }

                return DirectEve.Windows.OfType<DirectCareerAgentWindow>().FirstOrDefault(w => (w.AgentId == AgentId || w.AgentId == 0) && w.IsReady);
            }
        }

        //private PyObject PyAgentId { get; set; }

        public double EffectiveStandingNeededToAccessAgent()
        {
            switch (Level)
            {
                case 1: //lvl1 agent
                    return StandingsNeededToAccessLevel1Agent;

                case 2: //lvl2 agent
                    return StandingsNeededToAccessLevel2Agent;

                case 3: //lvl3 agent
                    return StandingsNeededToAccessLevel3Agent;

                case 4: //lvl4 agent
                    return StandingsNeededToAccessLevel4Agent;

                case 5: //lvl5 agent
                    return StandingsNeededToAccessLevel5Agent;
            }

            return StandingsNeededToAccessLevel4Agent;
        }

        public DirectAgentMissionBookmark GetMissionBookmark(string startsWith)
        {
            return this?.Mission?.Bookmarks.Find(b => b.Title.ToLower().StartsWith(startsWith.ToLower()));
        }

        public void DebugWindows()
        {
            try
            {
                if (!DirectEve.Interval(10000))
                    return;

                //Log("Checkmodal windows called.");
                if (ESCache.Instance.Windows.Count == 0)
                {
                    Log.WriteLine("CheckWindows: Cache.Instance.Windows returned null or empty");
                    return;
                }

                Log.WriteLine("Checking Each window in Cache.Instance.Windows");

                int windowNum = 0;
                foreach (DirectWindow window in ESCache.Instance.Windows)
                {
                    windowNum++;
                    Log.WriteLine("[" + windowNum + "] Debug_Window.Name: [" + window.Name + "]");
                    Log.WriteLine("[" + windowNum + "] Debug_Window.Html: [" + window.Html + "]");
                    Log.WriteLine("[" + windowNum + "] Debug_Window.Type: [" + window.Guid + "]");
                    Log.WriteLine("[" + windowNum + "] Debug_Window.IsModal: [" + window.IsModal + "]");
                    Log.WriteLine("[" + windowNum + "] Debug_Window.Caption: [" + window.Caption + "]");
                    Log.WriteLine("--------------------------------------------------");
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception: " + ex);
            }
        }

        public bool OpenAgentWindow(bool WeWantToBeInStation)
        {
            if (!IsValid)
            {
                Log.WriteLine("if (!IsValid)");
                return false;
            }

            if (WeWantToBeInStation && !DirectEve.Session.IsInDockableLocation)
                return true;

            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
            {
                if (CareerAgentWindow == null)
                {
                    DebugWindows();

                    if (DateTime.UtcNow < _lastAgentWindowInteraction.AddMilliseconds(Util.GetRandom(1500, 1700))) // was 3000 ms
                    {
                        if (DebugAgentInteractionReplyToAgent)
                            DirectEve.Log("CareerAgentWindow == null: if (DateTime.UtcNow < _lastAgentAction.AddSeconds(3))");
                        return false;
                    }

                    if (DebugAgentInteractionReplyToAgent)
                        DirectEve.Log("CareerAgentWindow == null: Attempting to Interact with the agent named [" + Name + "] in [" + SolarSystem.Name + "]");

                    if (InteractWith())
                    {
                        _lastAgentWindowInteraction = DateTime.UtcNow;
                        Log.WriteLine("CareerAgentWindow == null: Agent [" + Name + "]: Opening CareerAgentWindow");
                        return false;
                    }

                    return false;
                }

                if (DebugAgentInteractionReplyToAgent)
                    if (DirectEve.Interval(10000)) Log.WriteLine("Found CareerAgentWindow");

                if (!CareerAgentWindow.IsReady)
                {
                    Log.WriteLine("if (!CareerAgentWindow.IsReady)");
                    return false;
                }

                //if (CareerAgentWindow.IsReady) //&& DateTime.UtcNow > _agentWindowLastReady.AddSeconds(_delayInSeconds + 2))
                //{
                    if (CareerAgentWindow.Buttons.Count > 0)
                    {
                        _agentWindowLastReady = DateTime.UtcNow;
                        PassAgentInfoToEveSharpLauncher(this);
                        return true;
                    }

                    Log.WriteLine("Agent: [" + Name + "] CareerAgentWindow has no buttons?");
                    return false;
                //}

                //Log.WriteLine("Agent: [" + Name + "] logic flaw?");
                //return false;
            }

            if (AgentWindow == null)
            {
                if (DateTime.UtcNow < _lastAgentWindowInteraction.AddMilliseconds(Util.GetRandom(1500, 1700))) // was 3000 ms
                {
                    if (DebugAgentInteractionReplyToAgent)
                        DirectEve.Log("if (DateTime.UtcNow < _lastAgentAction.AddSeconds(3))");
                    return false;
                }

                if (DebugAgentInteractionReplyToAgent)
                    DirectEve.Log("Attempting to Interact with the agent named [" + Name + "] in [" + SolarSystem.Name + "]");

                if (InteractWith())
                {
                    _lastAgentWindowInteraction = DateTime.UtcNow;
                    Log.WriteLine("Agent [" + Name + "]: Opening AgentWindow");
                    return false;
                }

                return false;
            }

            if (DebugAgentInteractionReplyToAgent) Log.WriteLine("Found Agent Window");

            if (!AgentWindow.IsReady && !string.IsNullOrEmpty(AgentWindow.Briefing))
            {
                _agentWindowLastReady = DateTime.UtcNow;
                PassAgentInfoToEveSharpLauncher(this);
                return true;
            }

            if (!AgentWindow.IsReady && AgentWindow.Buttons.Count > 0)
            {
                _agentWindowLastReady = DateTime.UtcNow;
                PassAgentInfoToEveSharpLauncher(this);
                return true;
            }

            if (!AgentWindow.IsReady)
            {
                Log.WriteLine("if (!Window.IsReady)");
                return false;
            }

            if (AgentWindow.IsReady) //&& DateTime.UtcNow > _agentWindowLastReady.AddSeconds(_delayInSeconds + 2))
            {
                if (AgentWindow.Buttons.Count == 0 && !string.IsNullOrEmpty(AgentWindow.Briefing))
                {
                    _agentWindowLastReady = DateTime.UtcNow;
                    PassAgentInfoToEveSharpLauncher(this);
                    return true;
                }

                if (AgentWindow.Buttons.Count > 0)
                {
                    _agentWindowLastReady = DateTime.UtcNow;
                    PassAgentInfoToEveSharpLauncher(this);
                    return true;
                }

                Log.WriteLine("Agent: [" + Name + "] Window isReady: There are no Buttons and logic flaw?");
                return false;
            }

            Log.WriteLine("Agent: [" + Name + "] logic flaw?");
            return false;
        }

        public void PassAgentInfoToEveSharpLauncher(DirectAgent agent)
        {
            try
            {
                if (DebugAgentInfo) DirectEve.Log("AgentName [" + agent.Name + "]");
                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.Agent), agent.Name);
                if (DebugAgentInfo) DirectEve.Log("Agent Level [" + agent.Level + "]");
                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.AgentLevel), agent.Level.ToString());
                if (DebugAgentInfo) DirectEve.Log("Agent CorpId [" + agent.CorpId + "]");
                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.AgentCorpId), agent.CorpId.ToString());
                try
                {
                    string agentCorpName;
                    DirectNpcInfo.NpcCorpIdsToNames.TryGetValue(agent.CorpId.ToString(), out agentCorpName);
                    if (!string.IsNullOrEmpty(agentCorpName))
                    {
                        if (DebugAgentInfo) DirectEve.Log("Agent CorpName [" + agentCorpName + "]");
                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.AgentCorp), agentCorpName);
                    }

                    if (DebugAgentInfo) DirectEve.Log("Agent FactionId [" + agent.FactionId + "]");
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.AgentFactionId), agent.FactionId.ToString());

                    if (DebugAgentInfo) DirectEve.Log("Agent Faction [" + agent.FactionName + "]");
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.AgentFaction), agent.FactionName);

                    if (agent.LoyaltyPoints > 0)
                    {
                        if (DebugAgentInfo) DirectEve.Log("Agent LoyaltyPoints [" + agent.LoyaltyPoints + "]");
                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LoyaltyPoints), (double)agent.LoyaltyPoints);
                    }

                    if (DebugAgentInfo) DirectEve.Log("Agent Division [" + agent.DivisionName + "]");
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.AgentDivision), agent.DivisionName);

                    if (ESCache.Instance.EveAccount.AutoSkillTraining)
                    {
                        TimeSpan mySkillQueueLengthTimespan = DirectEve.Skills.SkillQueueLength;
                        //if (DebugConfig.DebugSkillQueue)
                        if (DebugSkillQueue) DirectEve.Log("AgentInteraction: mySkillQueueLengthTimespan [" + Math.Round(mySkillQueueLengthTimespan.TotalHours, 0) + "] hours from now");
                        DateTime mySkillQueueEnds = DateTime.UtcNow.Add(mySkillQueueLengthTimespan);
                        //if (DebugConfig.DebugSkillQueue)
                        if (DebugSkillQueue) DirectEve.Log("AgentInteraction: mySkillQueueEnds [" + mySkillQueueEnds + "]");
                        if (DateTime.UtcNow > mySkillQueueEnds)
                        {
                            if (DebugSkillQueue) DirectEve.Log("AgentInteraction: mySkillQueueEnds [" + mySkillQueueEnds + "] is in the past.");
                            mySkillQueueEnds = DateTime.MinValue;
                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.MySkillQueueEnds), mySkillQueueEnds);
                        }
                        else if (DateTime.UtcNow != mySkillQueueEnds)
                        {
                            if (DebugSkillQueue) DirectEve.Log("AgentInteraction: mySkillQueueEnds [" + mySkillQueueEnds + "].");
                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.MySkillQueueEnds), mySkillQueueEnds);
                        }

                        if (DirectEve.Skills.MySkillQueue != null && DirectEve.Skills.MySkillQueue.Count > 0)
                        {
                            DirectSkill mySkillTraining = DirectEve.Skills.MySkillQueue.FirstOrDefault();
                            if (mySkillTraining != null)
                            {
                                if (DebugSkillQueue) DirectEve.Log("AgentInteraction: mySkillTraining [" + mySkillTraining.TypeName + "] ");
                                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.MySkillTraining), mySkillTraining.TypeName);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    DirectEve.Log("Exception [" + ex + "]");
                }
            }
            catch (Exception ex)
            {
                DirectEve.Log("Exception [" + ex + "]");
            }
        }

        #endregion Properties

        #region Methods

        public bool StorylineAgent
        {
            get
            {
                if (AgentTypeName.Contains("Storyline"))
                    return true;

                return false;
            }
        }

        public static Dictionary<string, long> GetAllAgents(DirectEve directEve)
        {
            Dictionary<string, long> ret = new Dictionary<string, long>();

            Dictionary<long, PyObject> agentsById = directEve.GetLocalSvc("agents").Attribute("allAgentsByID").Attribute("items").ToDictionary<long>();
            foreach (KeyValuePair<long, PyObject> agent in agentsById)
            {
                DirectOwner owner = DirectOwner.GetOwner(directEve, agent.Key);
                if (ret.ContainsKey(owner.Name))
                    continue;
                ret.AddOrUpdate(owner.Name, agent.Key);
            }
            return ret;
        }

        public static bool IsAgentsByIdDictionaryPopulated(DirectEve directEve)
        {
            return directEve.GetLocalSvc("agents").Attribute("allAgents").IsValid;
        }

        public static void PopulateAgentsByIdDictionary(DirectEve directEve)
        {
            if (!IsAgentsByIdDictionaryPopulated(directEve))
                directEve.ThreadedLocalSvcCall("agents", "GetAgentsByID");
        }

        public String GetAgentMissionInfo()
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

            var ret = String.Empty;

            if (!IsValid) return ret;

            var obj = DirectEve.GetLocalSvc("missionObjectivesTracker").Attribute("currentAgentMissionInfo");

            if (obj == null || !obj.IsValid) return ret;

            var dict = obj.ToDictionary<long>();

            if (dict.ContainsKey(AgentId))
                ret = dict[AgentId].ToUnicodeString();

            return ret ?? string.Empty;
        }

        public bool InteractWith()
        {
            if (Level == 4 && !DirectEve.Me.IsOmegaClone)
            {
                Log.WriteLine($"Error: Can't access a level 4 agent while being in alpha state.");
                return false;
            }

            if (!DirectEve.Interval(1200, 2000))
                return false;

            return DirectEve.ThreadedLocalSvcCall("agents", "OnInteractWith", AgentId);
        }

        internal static DirectAgent GetAgentById(DirectEve directEve, long id)
        {
            PyObject pyAgent = directEve.GetLocalSvc("agents").Attribute("allAgentsByID").Attribute("items").DictionaryItem(id);

            DirectAgent agent = new DirectAgent(directEve)
            {
                IsValid = pyAgent.IsValid,
                AgentId = (long)pyAgent.GetItemAt(0),
                AgentTypeId = (long)pyAgent.GetItemAt(1),
                DivisionId = (long)pyAgent.GetItemAt(2), //`crpNPCDivisions` VALUES (1,'Accounting','DEPRECATED DIVISION - DO NOT USE','CFO'),(2,'Administration','DEPRECATED DIVISION - DO NOT USE','CFO'),(3,'Advisory','DEPRECATED DIVISION - DO NOT USE','Chief Advisor'),(4,'Archives','DEPRECATED DIVISION - DO NOT USE','Chief Archivist'),(5,'Astrosurveying','DEPRECATED DIVISION - DO NOT USE','Survey Manager'),(6,'Command','DEPRECATED DIVISION - DO NOT USE','COO'),(7,'Distribution','DEPRECATED DIVISION - DO NOT USE','Distribution Manager'),(8,'Financial','DEPRECATED DIVISION - DO NOT USE','CFO'),(9,'Intelligence','DEPRECATED DIVISION - DO NOT USE','Chief Operative'),(10,'Internal Security','DEPRECATED DIVISION - DO NOT USE','Commander'),(11,'Legal','DEPRECATED DIVISION - DO NOT USE','Principal Clerk'),(12,'Manufacturing','DEPRECATED DIVISION - DO NOT USE','Assembly Manager'),(13,'Marketing','DEPRECATED DIVISION - DO NOT USE','Market Manager'),(14,'Mining','DEPRECATED DIVISION - DO NOT USE','Mining Coordinator'),(15,'Personnel','DEPRECATED DIVISION - DO NOT USE','Chief of Staff'),(16,'Production','DEPRECATED DIVISION - DO NOT USE','Production Manager'),(17,'Public Relations','DEPRECATED DIVISION - DO NOT USE','Chief Coordinator'),(18,'R&D','Research and development division','Chief Researcher'),(19,'Security','DEPRECATED DIVISION - DO NOT USE','Commander'),(20,'Storage','DEPRECATED DIVISION - DO NOT USE','Storage Facilitator'),(21,'Surveillance','DEPRECATED DIVISION - DO NOT USE','Chief Scout'),(22,'Distribution','New distribution division','Distribution Manager'),(23,'Mining','New mining division','Mining Coordinator'),(24,'Security','New security division','Commander'),(25,'Business','Business career','Chief Advisor'),(26,'Exploration','Exploration career','Chief Advisor'),(27,'Industry','Industry career','Chief Advisor'),(28,'Military','Military career','Chief Advisor'),(29,'Advanced Military','Advanced Military career','Chief Advisor');
                Level = int.Parse(pyAgent.GetItemAt(3).Repr),
                BloodlineId = long.Parse(pyAgent.GetItemAt(5).Repr),
                //Quality = int.Parse(pyAgent.GetItemAt(6).Repr),
                CorpId = long.Parse(pyAgent.GetItemAt(6).Repr),
                //Gender = bool.Parse(pyAgent.GetItemAt(7).Repr),
                //IsLocatorAgent = bool.Parse(pyAgent.GetItemAt(8).Repr),
                FactionId = long.Parse(pyAgent.GetItemAt(9).Repr),
                SolarSystemId = long.Parse(pyAgent.GetItemAt(10).Repr),
            };

            if (Divisions != null && Divisions.Any(x => x.Key == agent.DivisionId))
                agent.DivisionName = Divisions.FirstOrDefault(x => x.Key == agent.DivisionId).Value;

            try
            {
                //
                // ignore exceptions for agents that arent in stations
                //
                if (!string.IsNullOrEmpty(pyAgent.GetItemAt(4).Repr))
                    agent.StationId = long.Parse(pyAgent.GetItemAt(4).Repr);
            }
            catch (Exception)
            {
                //Log.WriteLine("Agent [" + agent.Name + "] must be an agent in space");
                //Log.WriteLine("Exception [" + ex + "]");
            }

            /**
            try
            {
                for (int a=1; a < 12; a++)
                {
                    Log.WriteLine("Key [ pyAgent.Item(" + a + ") ] Value [" + pyAgent.Item(a).ToString() + "] pyType [" + pyAgent.Item(a).GetPyType() + "][" + pyAgent.Item(a).Repr.ToString() + "]");
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
            **/

            return agent;
        }

        internal static DirectAgent GetAgentByName(DirectEve directEve, string name)
        {
            try
            {
                if (AgentLookupDict == null || AgentLookupDict.Count == 0)
                {
                    Dictionary<long, PyObject> agentsById = directEve.GetLocalSvc("agents").Attribute("allAgentsByID").Attribute("items").ToDictionary<long>();
                    AgentLookupDict = new Dictionary<string, long>();
                    if (agentsById.Any())
                    {
                        Log.WriteLine("agentsById contains [" + agentsById.Count + "] Agents");
                        foreach (KeyValuePair<long, PyObject> agent in agentsById)
                        {
                            try
                            {
                                DirectOwner owner = DirectOwner.GetOwner(directEve, agent.Key);
                                AgentLookupDict[owner.Name.ToLower()] = agent.Key;
                            }
                            catch (Exception ex)
                            {
                                Log.WriteLine("Exception [" + ex + "]");
                            }
                        }
                    }
                    else Log.WriteLine("agentsById contains no Agents!?");
                }

                Log.WriteLine("GetAgentByName: AgentLookupDict contains [" + AgentLookupDict.Count + "] agents");

                try
                {
                    if (AgentLookupDict.Count != 0)
                    {
                        if (AgentLookupDict.TryGetValue(name.ToLower(), out long id))
                        {
                            if (id == 0)
                            {
                                Log.WriteLine("GetAgentByName: Agent not found: if (AgentLookupDict.TryGetValue(name.ToLower(), out long id)) failed");
                            }

                            return GetAgentById(directEve, id);
                        }

                        Log.WriteLine("AgentLookupDict error. AgentLookupDict.Count [" + AgentLookupDict.Count + "] AgentName [" + name + "] not found");
                        return null;
                    }

                    Log.WriteLine("AgentLookupDict error. if (AgentLookupDict.Count =! 0)");
                    return null;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }

                return null;
            }
            catch (Exception ex)
            {
                Log.WriteLine("AgentLookupDict error. name [" + name + "] not found");
                Log.WriteLine("Exception [" + ex + "]");
                throw;
            }
        }

        #endregion Methods
    }
}