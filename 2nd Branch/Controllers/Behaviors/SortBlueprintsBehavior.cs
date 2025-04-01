extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using EVESharpCore.Traveller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SC::SharedComponents.EVE;

namespace EVESharpCore.Questor.Behaviors
{
    public class SortBlueprintsBehavior
    {
        #region Constructors

        public SortBlueprintsBehavior()
        {
            ResetStatesToDefaults();
        }

        #endregion Constructors

        #region Fields

        #endregion Fields

        #region Properties

        public static string HomeBookmarkName { get; set; }
        //public static bool RunAbyssalDeadspaceSitesUntilOurCargoContainsNoMoreCorrectFilaments { get; set; }

        #endregion Properties

        #region Methods

        public static bool ChangeSortBlueprintsBehaviorState(SortBlueprintsBehaviorState _StateToSet, bool wait = false, string LogMessage = null)
        {
            try
            {
                if (State.CurrentSortBlueprintsBehaviorState != _StateToSet)
                {
                    Log.WriteLine("New SortBlueprintsBehaviorState [" + _StateToSet + "]");
                    State.CurrentSortBlueprintsBehaviorState = _StateToSet;
                    //if (!wait) ProcessState();
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }

            return true;
        }

        public static void LoadSettings(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            Log.WriteLine("LoadSettings: SortBlueprintsBehavior");

            HomeBookmarkName =
                (string)CharacterSettingsXml.Element("HomeBookmark") ?? (string)CharacterSettingsXml.Element("HomeBookmark") ??
                (string)CommonSettingsXml.Element("HomeBookmark") ?? (string)CommonSettingsXml.Element("HomeBookmark") ?? "HomeBookmark";
            Log.WriteLine("SortBlueprintsBehavior: HomeBookmarkName [" + HomeBookmarkName + "]");
            NameOfSourceCansStartWith =
                (string)CharacterSettingsXml.Element("NameOfSourceCansStartWith") ?? (string)CharacterSettingsXml.Element("NameOfSourceCansStartWith") ??
                (string)CommonSettingsXml.Element("NameOfSourceCansStartWith") ?? (string)CommonSettingsXml.Element("NameOfSourceCansStartWith") ?? "Input";
            Log.WriteLine("SortBlueprintsBehavior: NameOfSourceCansStartWith [" + NameOfSourceCansStartWith + "]");
        }

        public static void GoGetCansState()
        {

            return;
        }

        public static void SortItemsState()
        {
            if (Time.Instance.NextArmAction > DateTime.UtcNow)
                return;

            if (SourceCanContainer == null)
                return;

            if (SourceCanContainer.Items == null)
                return;

            if (!SourceCanContainer.Items.Any())
            {
                Log.WriteLine("SortItemsState: if (!SourceCanContainer.Items.Any())");
                return;
            }

            if (DestinationCan != null && SourceCan.ItemId == DestinationCan.ItemId)
            {
                Log.WriteLine("SortItemsState: if (SourceCan.ItemId == DestinationCan.ItemId) - we have to have 2 separate cans to do any sorting");
                return;
            }

            DirectItem _myItem = SourceCanContainer.Items.FirstOrDefault(i => i.CategoryId == (int)CategoryID.Blueprint);
            if (_myItem != null)
            {
                NameOfDestinationCansStartWith = _myItem.TypeName;
                IEnumerable<DirectItem> listOfItemsToMove = SourceCanContainer.Items.Where(i => i.CategoryId == (int)CategoryID.Blueprint && i.TypeId == _myItem.TypeId);

                if (DestinationCanContainer == null)
                {
                    Log.WriteLine("Missing can named [" + NameOfDestinationCansStartWith + "] - waiting");
                    return;
                }

                if (DestinationCanContainer.WaitingForLockedItems()) return;

                if (DestinationCanContainer.Items.Count() == 1000)
                {
                    Log.WriteLine("DestinationCan is full - renaming can");
                    DestinationCan.SetName(listOfItemsToMove.FirstOrDefault().GivenName + " - Full");
                    Time.Instance.NextArmAction = DateTime.UtcNow.AddSeconds(6);
                    return;
                }

                int numberOfItemsToMove = Math.Max(1000, 1000 - DestinationCanContainer.Items.Count());
                numberOfItemsToMove = Math.Min(numberOfItemsToMove, listOfItemsToMove.Count());
                listOfItemsToMove = listOfItemsToMove.Take(numberOfItemsToMove);
                if (listOfItemsToMove.Any())
                {
                    Log.WriteLine("SortItemsState: Moving from [" + SourceCan.GivenName + "][" + listOfItemsToMove.Count() + "] of [" + listOfItemsToMove.FirstOrDefault().TypeName + "] to [" + DestinationCan.GivenName + "]");
                    DestinationCanContainer.Add(listOfItemsToMove);
                    NameOfDestinationCansStartWith = string.Empty;
                    Time.Instance.NextArmAction = DateTime.UtcNow.AddSeconds(4);
                    return;
                }

                Log.WriteLine("SortItemsState: we had no items to move? is the destination full?");
                return;
            }

            Log.WriteLine("No blueprints left to sort");
            ChangeSortBlueprintsBehaviorState(SortBlueprintsBehaviorState.Done);
        }

        private static string NameOfSourceCansStartWith = "Input";

        private static string _nameOfDestinationCansStartWith;
        private static string NameOfDestinationCansStartWith
        {
            get
            {
                return _nameOfDestinationCansStartWith;
            }
            set
            {
                _destinationCan = null;
                _nameOfDestinationCansStartWith = value;
            }
        }

        private static DirectContainer _sourceCanContainer;
        private static DirectContainer SourceCanContainer
        {
            get
            {
                if (SourceCan != null)
                {
                    _sourceCanContainer = ESCache.Instance.DirectEve.GetContainer(SourceCan.ItemId);
                    if (_sourceCanContainer.Window == null)
                        _sourceCan.OpenContainer();

                    if (_sourceCanContainer.Window != null && _sourceCanContainer.IsReady)
                        return _sourceCanContainer ?? null;

                    return null;
                }

                return null;
            }
        }

        private static DirectContainer _destinationCanContainer;
        private static DirectContainer DestinationCanContainer
        {
            get
            {
                if (DestinationCan != null && !string.IsNullOrEmpty(NameOfDestinationCansStartWith))
                {
                    _destinationCanContainer = ESCache.Instance.DirectEve.GetContainer(DestinationCan.ItemId);
                    return _destinationCanContainer ?? null;
                }

                return null;
            }
        }


        private static DirectItem _sourceCan;
        private static DirectItem SourceCan
        {
            get
            {
                if (_sourceCan == null)
                {
                    if (SourceCans != null && SourceCans.Any())
                    {
                        _sourceCan = SourceCans.FirstOrDefault();
                    }

                    return _sourceCan ?? null;
                }

                return _sourceCan;
            }
        }
        private static List<DirectItem> SourceCans
        {
            get
            {
                if (ESCache.Instance.ItemHangar == null)
                    return new List<DirectItem>();

                if (ESCache.Instance.ItemHangar.Items == null)
                    return new List<DirectItem>();

                if (!ESCache.Instance.ItemHangar.Items.Any())
                    return new List<DirectItem>();

                if (ESCache.Instance.ItemHangar.Items.Any(i => i.IsContainerUsedToSortItemsInStations && i.IsSingleton && i.GivenName.StartsWith(NameOfSourceCansStartWith)))
                {
                    try
                    {
                        List<DirectItem> tempSourceCans = ESCache.Instance.ItemHangar.Items.Where(i => i.IsContainerUsedToSortItemsInStations && i.IsSingleton && i.GivenName.StartsWith(NameOfSourceCansStartWith)).ToList();
                        return tempSourceCans ?? new List<DirectItem>();
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Exception [" + ex + "]");
                    }
                }

                return new List<DirectItem>();
            }
        }

        private static List<DirectItem> DestinationCansUnassembled
        {
            get
            {
                if (ESCache.Instance.ItemHangar == null)
                    return new List<DirectItem>();

                if (ESCache.Instance.ItemHangar.Items == null)
                    return new List<DirectItem>();

                if (!ESCache.Instance.ItemHangar.Items.Any())
                    return new List<DirectItem>();

                if (ESCache.Instance.ItemHangar.Items.Any(i => i.IsContainerUsedToSortItemsInStations && !i.IsSingleton))
                {
                    List<DirectItem> tempSourceCans = ESCache.Instance.ItemHangar.Items.Where(i => i.IsContainerUsedToSortItemsInStations && !i.IsSingleton).ToList();
                    return tempSourceCans ?? new List<DirectItem>();
                }

                return new List<DirectItem>();
            }
        }

        private static DirectItem _destinationCan;
        private static DirectItem DestinationCan
        {
            get
            {
                if (_destinationCan == null)
                {
                    if (DestinationCans != null && !string.IsNullOrEmpty(NameOfDestinationCansStartWith) && DestinationCans.Any(i => i.GivenName.StartsWith(NameOfDestinationCansStartWith)))
                    {
                        _destinationCan = DestinationCans.FirstOrDefault(i => i.GivenName.StartsWith(NameOfDestinationCansStartWith));
                        return _destinationCan ?? null;
                    }

                    if (string.IsNullOrWhiteSpace(NameOfDestinationCansStartWith))
                        Log.WriteLine("NameOfDestinationCansStartWith is empty");

                    return null;
                }

                return _destinationCan;
            }
        }
        private static List<DirectItem> DestinationCans
        {
            get
            {
                if (ESCache.Instance.ItemHangar == null)
                    return new List<DirectItem>();

                if (ESCache.Instance.ItemHangar.Items == null)
                    return new List<DirectItem>();

                if (!ESCache.Instance.ItemHangar.Items.Any())
                    return new List<DirectItem>();

                if (string.IsNullOrEmpty(NameOfDestinationCansStartWith))
                    return new List<DirectItem>();

                if (ESCache.Instance.ItemHangar.Items.Any(i => i.IsContainerUsedToSortItemsInStations && i.IsSingleton && i.GivenName.ToLower().StartsWith(NameOfDestinationCansStartWith.ToLower()) && !i.GivenName.ToLower().StartsWith("full".ToLower())))
                {
                    List<DirectItem> tempSourceCans = ESCache.Instance.ItemHangar.Items.Where(i => i.IsContainerUsedToSortItemsInStations && i.IsSingleton && i.GivenName.ToLower().StartsWith(NameOfDestinationCansStartWith.ToLower()) && !i.GivenName.ToLower().StartsWith("full".ToLower())).ToList();
                    return tempSourceCans ?? new List<DirectItem>();
                }

                return new List<DirectItem>();
            }
        }

        public static bool CheckPrerequisitesState()
        {
            if (!ESCache.Instance.InStation && ESCache.Instance.InSpace)
            {
                ChangeSortBlueprintsBehaviorState(SortBlueprintsBehaviorState.Done);
                return false;
            }

            if (ESCache.Instance.ItemHangar == null)
                return false;

            if (ESCache.Instance.ItemHangar.Items == null || !ESCache.Instance.ItemHangar.Items.Any())
                return false;

            /**
            if (!SourceCans.Any())
            {
                Log.WriteLine("CheckPrerequisitesState: if (!SourceCans.Any())");
                return false;
            }
            **/

            //if (!DestinationCans.Any())
            //{
            //    Log.WriteLine("CheckPrerequisitesState: if (!DestinationCans.Any())");
            //    return false;
            //}

            if (!DestinationCansUnassembled.Any())
            {
                Log.WriteLine("CheckPrerequisitesState: if (!DestinationCansUnassembled.Any())");
                return false;
            }

            ChangeSortBlueprintsBehaviorState(SortBlueprintsBehaviorState.SortItems);
            return true;
        }

        public static void ProcessState()
        {
            try
            {
                if (!EveryPulse()) return;

                if (DebugConfig.DebugSortBlueprintsBehavior) Log.WriteLine("State.CurrentSortBlueprintsState is [" + State.CurrentSortBlueprintsBehaviorState + "]");

                switch (State.CurrentSortBlueprintsBehaviorState)
                {
                    case SortBlueprintsBehaviorState.Idle:
                        IdleCMBState();
                        break;

                    case SortBlueprintsBehaviorState.Start:
                        StartCMBState();
                        break;

                    case SortBlueprintsBehaviorState.CheckPrerequisites:
                        if (!CheckPrerequisitesState()) return;
                        break;

                    case SortBlueprintsBehaviorState.SortItems:
                        SortItemsState();
                        break;

                    case SortBlueprintsBehaviorState.GoGetCans:
                        GoGetCansState();
                        break;

                    case SortBlueprintsBehaviorState.Default:
                        ChangeSortBlueprintsBehaviorState(SortBlueprintsBehaviorState.Idle);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static bool EveryPulse()
        {
            InvalidateCache();

            if (!ESCache.Instance.InStation)
            {
                if (DebugConfig.DebugSortBlueprintsBehavior) Log.WriteLine("SortBlueprintsBehavior: EveryPulse: if (!ESCache.Instance.InStation)");
                return false;
            }

            return true;
        }

        private static void GotoHomeBookmarkState()
        {
            if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
            {
                if (DebugConfig.DebugGotobase) Log.WriteLine("GotoHomeBookmark: AvoidBumpingThings()");
                NavigateOnGrid.AvoidBumpingThings(ESCache.Instance.BigObjects.FirstOrDefault(), "AbyssalDeadspaceBehaviorState.GotoBase", NavigateOnGrid.TooCloseToStructure, NavigateOnGrid.AvoidBumpingThingsBool());
            }

            if (DebugConfig.DebugGotobase) Log.WriteLine("GotoHomeBookmark: Traveler.TravelToBookmarkName(" + HomeBookmarkName + ");");

            Traveler.TravelToBookmarkName(HomeBookmarkName);

            if (State.CurrentTravelerState == TravelerState.AtDestination)
            {
                if (ESCache.Instance.InSpace)
                {
                    Log.WriteLine("HomeBookmark is defined as [" + HomeBookmarkName + "] and should be a bookmark of a station or citadel we can dock at: why are we still in space?!");
                    //Log.WriteLine("Pausing!");
                    //ControllerManager.Instance.SetPause(true);
                    return;
                }

                Traveler.Destination = null;
                ChangeSortBlueprintsBehaviorState(SortBlueprintsBehaviorState.Start, true);
            }
        }

        private static void IdleCMBState()
        {
            ChangeSortBlueprintsBehaviorState(SortBlueprintsBehaviorState.Start);
        }

        private static DateTime LastInvalidateCache = DateTime.UtcNow;
        public static void InvalidateCache()
        {
            if (DateTime.UtcNow > LastInvalidateCache)
            {
                LastInvalidateCache = DateTime.UtcNow;
                _sourceCan = null;
                _destinationCan = null;
            }
        }

        private static bool ResetStatesToDefaults()
        {
            Log.WriteLine("SortBlueprintsBehavior.ResetStatesToDefaults: start");
            State.CurrentSortBlueprintsBehaviorState = SortBlueprintsBehaviorState.Idle;
            Log.WriteLine("SortBlueprintsBehavior.ResetStatesToDefaults: done");
            return true;
        }

        private static void StartCMBState()
        {
            //
            // It takes 20 minutes (potentially) to do an abyssal site: if it is within 25min of Downtime (10:35 evetime) pause
            //
            if (Time.Instance.IsItDuringDowntimeNow)
            {
                Log.WriteLine("SortBlueprintsController: Arm: Downtime is less than 25 minutes from now: Pausing");
                ControllerManager.Instance.SetPause(true);
                return;
            }

            ChangeSortBlueprintsBehaviorState(SortBlueprintsBehaviorState.CheckPrerequisites);
        }

        private static void TravelerCMBState()
        {
            try
            {
                if (NavigateOnGrid.SpeedTank && !Salvage.LootWhileSpeedTanking)
                {
                    if (DebugConfig.DebugTargetWrecks) Log.WriteLine("Salvage.OpenWrecks = false;");
                    Salvage.OpenWrecks = false;
                }

                List<long> destination = ESCache.Instance.DirectEve.Navigation.GetDestinationPath();
                if (destination == null || destination.Count == 0)
                {
                    Log.WriteLine("No destination?");
                    State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Error;
                    return;
                }

                if (destination.Count == 1 && destination.FirstOrDefault() == 0)
                    destination[0] = ESCache.Instance.DirectEve.Session.SolarSystemId ?? -1;

                if (Traveler.Destination == null || Traveler.Destination.SolarSystemId != destination.LastOrDefault())
                {
                    if (ESCache.Instance.CachedBookmarks != null && ESCache.Instance.CachedBookmarks.Any())
                    {
                        IEnumerable<DirectBookmark> bookmarks = ESCache.Instance.CachedBookmarks.Where(b => b.LocationId == destination.LastOrDefault()).ToList();
                        if (bookmarks.FirstOrDefault() != null && bookmarks.Any())
                        {
                            Traveler.Destination = new BookmarkDestination(bookmarks.OrderBy(b => b.CreatedOn).FirstOrDefault());
                            return;
                        }

                        Log.WriteLine("Destination: [" + ESCache.Instance.DirectEve.Navigation.GetLocation(destination.Last()).Name + "]");
                        long lastSolarSystemInRoute = destination.LastOrDefault();

                        Log.WriteLine("Destination: [" + lastSolarSystemInRoute + "]");
                        Traveler.Destination = new SolarSystemDestination(destination.LastOrDefault());
                        return;
                    }

                    return;
                }

                Traveler.ProcessState();

                if (State.CurrentTravelerState == TravelerState.AtDestination)
                {
                    if (State.CurrentCombatMissionCtrlState == ActionControlState.Error)
                    {
                        Log.WriteLine("an error has occurred");
                        ChangeSortBlueprintsBehaviorState(SortBlueprintsBehaviorState.Error, true);
                        return;
                    }

                    if (ESCache.Instance.InSpace)
                    {
                        Log.WriteLine("Arrived at destination (in space, Questor stopped)");
                        ChangeSortBlueprintsBehaviorState(SortBlueprintsBehaviorState.Error, true);
                        return;
                    }

                    Log.WriteLine("Arrived at destination");
                    ChangeSortBlueprintsBehaviorState(SortBlueprintsBehaviorState.Idle, true);
                }
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        #endregion Methods
    }
}