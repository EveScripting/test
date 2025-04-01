extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Controllers;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Events;
using EVESharpCore.Logging;
using EVESharpCore.Questor.Actions;
using EVESharpCore.Questor.Activities;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Questor.Caching;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Questor.Lookup;
using EVESharpCore.Questor.States;
using EVESharpCore.Questor.Stats;
using EVESharpCore.Questor.Storylines;
using EVESharpCore.Questor.Traveller;
using SC::SharedComponents.Events;

namespace EVESharpCore.Questor.Behaviors
{
    public class CombatMissionsBehavior
    {
        private static CombatMissionCtrl _combatMissionCtrl;
        public static CourierMissionCtrl _courierMissionCtrl;
        public static Storyline _storyline;


        private static DateTime _lastPulse;
        private static DateTime _lastSalvageTrip = DateTime.MinValue;

        private static double _lastX;
        private static double _lastY;
        private static double _lastZ;

        private static DateTime _nextBookmarkRefreshCheck = DateTime.MinValue;
        private static DateTime _nextBookmarksrefresh = DateTime.MinValue;

        public CombatMissionsBehavior()
        {
            _lastPulse = DateTime.MinValue;
            _courierMissionCtrl = new CourierMissionCtrl();
            _combatMissionCtrl = new CombatMissionCtrl();
            _storyline = new Storyline();
            QCache.Storyline = _storyline;
            ResetStatesToDefaults();
        }

        private static bool ResetStatesToDefaults()
        {
            Log.WriteLine("CombatMissionsBehavior.ResetStatesToDefaults: start");
            _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Idle;
            _States.CurrentAgentInteractionState = AgentInteractionState.Idle;
            _States.CurrentArmState = ArmState.Idle;
            _States.CurrentUnloadLootState = UnloadLootState.Idle;
            _States.CurrentTravelerState = TravelerState.AtDestination;
            Log.WriteLine("CombatMissionsBehavior.ResetStatesToDefaults: done");
            return true;
        }

        public static bool ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState _CMBStateToSet, bool wait = false ,string LogMessage = null)
        {
            try
            {
                if (_States.CurrentCombatMissionBehaviorState != _CMBStateToSet)
                {
                    if (_CMBStateToSet == CombatMissionsBehaviorState.GotoBase)
                    {
                        _States.CurrentTravelerState = TravelerState.Idle;
                    }

                    Log.WriteLine("New CombatMissionsBehaviorState [" + _CMBStateToSet.ToString() + "]");
                    _States.CurrentCombatMissionBehaviorState = _CMBStateToSet;
                    if (!wait) ProcessState();
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }

            return true;
        }

        private static void IdleCMBState()
        {
            if (QCache.Instance.InSpace)
            {
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase);
                return;
            }

            _States.CurrentAgentInteractionState = AgentInteractionState.Idle;
            _States.CurrentArmState = ArmState.Idle;
            _States.CurrentDroneState = DroneState.Idle;
            _States.CurrentSalvageState = SalvageState.Idle;
            _States.CurrentStorylineState = StorylineState.Idle;
            _States.CurrentTravelerState = TravelerState.AtDestination;
            _States.CurrentUnloadLootState = UnloadLootState.Idle;

            if (Settings.Instance.AutoStart)
            {
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Cleanup);
                return;
            }
        }

        private void DelayedStartCMBState()
        {
            _storyline.Reset();
            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Cleanup);
            return;
        }

        private static void DelayedGotoBaseCMBState()
        {
            Log.WriteLine("Heading back to base");
            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase);
            return;
        }

        private static void CleanupCMBState()
        {
            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Start);
            return;
        }

        private static void StartCMBState()
        {
            if (QCache.LootAlreadyUnloaded == false)
            {
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Switch);
                return;
            }

            if (QCache.Instance.InSpace)
            {
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.DelayedGotoBase);
                return;
            }

            if (NavigateOnGrid.SpeedTank && !Settings.Instance.LootWhileSpeedTanking)
            {
                if (DebugConfig.DebugTargetWrecks)
                    Log.WriteLine("Salvage.OpenWrecks = false;");
                Salvage.OpenWrecks = false;
            }
            else
            {
                Salvage.OpenWrecks = true;
            }

            if (_States.CurrentAgentInteractionState == AgentInteractionState.Idle)
            {
                QCache.Instance.Wealth = QCache.Instance.DirectEve.Me.Wealth;

                Statistics.WrecksThisMission = 0;
                if (Settings.Instance.EnableStorylines && _storyline.HasStoryline())
                {
                    Log.WriteLine("Storyline detected, doing storyline.");
                    _storyline.Reset();
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.PrepareStorylineSwitchAgents);
                    return;
                }

                Log.WriteLine("Start conversation [Start Mission]");
                _States.CurrentAgentInteractionState = AgentInteractionState.StartConversation;
                AgentInteraction.Purpose = AgentInteractionPurpose.StartMission;
            }

            AgentInteraction.ProcessState();

            if (AgentInteraction.Purpose == AgentInteractionPurpose.CompleteMission)
            {
                if (_States.CurrentAgentInteractionState == AgentInteractionState.Done)
                {
                    _States.CurrentAgentInteractionState = AgentInteractionState.Idle;
                    if (MissionSettings.CourierMission)
                    {
                        CourierMissionCtrl.ChangeCourierMissionBehaviorState(CourierMissionCtrlState.Idle);
                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.CourierMissionArm, true);
                        return;
                    }

                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.UnloadLoot);
                    return;
                }

                return;
            }

            if (_States.CurrentAgentInteractionState == AgentInteractionState.Done)
            {
                //
                // If AgentInteraction changed the state of CurrentCombatMissionBehaviorState to Idle: return
                //
                if (_States.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.Idle)
                {
                    return;
                }

                //
                // otherwise continue on and change to the Arm state
                //
                MissionSettings.UpdateMissionName(QCache.Instance.Agent.AgentId);
                _States.CurrentAgentInteractionState = AgentInteractionState.Idle;
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Arm, true);
                return;
            }

            return;
        }

        private static void SwitchCMBState()
        {
            if (!QCache.Instance.InStation)
            {
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase);
                return;
            }

            if (QCache.Instance.DirectEve.Session.StationId != null && QCache.Instance.Agent != null &&
                QCache.Instance.DirectEve.Session.StationId != QCache.Instance.Agent.StationId)
            {
                Log.WriteLine("We're not in the right station, going home.");
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase);
                return;
            }

            if (QCache.Instance.CurrentShipsCargo == null || QCache.Instance.CurrentShipsCargo.Items == null || QCache.Instance.ItemHangar == null ||
                QCache.Instance.ItemHangar.Items == null)
                return;

            if (QCache.Instance.InStation && Settings.Instance.BuyPlex && BuyPlexController.ShouldBuyPlex)
            {
                BuyPlexController.CheckBuyPlex();
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Idle);
                return;
            }

            if (QCache.Instance.ActiveShip != null && QCache.Instance.CurrentShipsCargo != null && QCache.Instance.CurrentShipsCargo.Items != null && QCache.Instance.CurrentShipsCargo.Items.Any() &&
                QCache.Instance.ActiveShip.GivenName.ToLower() != Combat.Combat.CombatShipName.ToLower())
            {
                Log.WriteLine("if(Cache.Instance.CurrentShipsCargo.Items.Any())");
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.UnloadLoot);
                return;
            }

            if (_States.CurrentArmState == ArmState.Idle)
            {
                if (QCache.Instance.Agent != null && QCache.Instance.Agent.DivisionId == 24) //24 == security
                {
                    Log.WriteLine("Begin");
                    Arm.SwitchShipsOnly = true;
                    if (MissionSettings.MissionSpecificMissionFitting != null && !string.IsNullOrEmpty(MissionSettings.MissionSpecificMissionFitting.Ship))
                    {
                        Arm.ChangeArmState(ArmState.ActivateMissionSpecificShip);
                    }
                    else
                    {
                        Arm.ChangeArmState(ArmState.ActivateCombatShip);
                    }
                }
                else
                {
                    Arm.ChangeArmState(ArmState.Done);
                }
            }

            if (DebugConfig.DebugArm) Log.WriteLine("CombatMissionBehavior.Switch is Entering Arm.Processstate");
            Arm.ProcessState();

            if (_States.CurrentArmState == ArmState.Done)
            {
                Log.WriteLine("Done");
                Arm.SwitchShipsOnly = false;
                Arm.ChangeArmState(ArmState.Idle);
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true);
                return;
            }

            return;
        }

        private static void ArmCMBState()
        {
            if (!AttemptToBuyAmmo()) return;
            if (!AttemptToBuyLpItems()) return;
            if (!AttemptToCourierContractItems()) return;

            if (_States.CurrentArmState == ArmState.Idle)
            {
                Log.WriteLine("Begin");
                Arm.ChangeArmState(ArmState.Begin);
            }

            if (!QCache.Instance.InStation) return;

            Arm.ProcessState();

            if (_States.CurrentArmState == ArmState.NotEnoughAmmo)
            {
                Log.WriteLine("Armstate.NotEnoughAmmo");
                Arm.ChangeArmState(ArmState.Idle, true);
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Error, true);
                return;
            }

            if (_States.CurrentArmState == ArmState.NotEnoughDrones)
            {
                Log.WriteLine("Armstate.NotEnoughDrones");
                Arm.ChangeArmState(ArmState.Idle, true);
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Error, true);
                return;
            }

            if (_States.CurrentArmState == ArmState.Done)
            {
                Arm.ChangeArmState(ArmState.Idle, true);

                if (Settings.Instance.BuyAmmo && BuyAmmoController.CurrentBuyAmmoState != BuyAmmoState.DisabledForThisSession)
                {
                    BuyAmmoController.CurrentBuyAmmoState = BuyAmmoState.Idle;
                    ControllerManager.Instance.RemoveController(typeof(BuyAmmoController));
                }

                _States.CurrentDroneState = DroneState.WaitingForTargets;
                if (MissionSettings.CourierMission)
                {
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.CourierMission, true);
                    return;
                }

                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.LocalWatch, true);
                return;
            }

            return;
        }

        private static bool AttemptToBuyAmmo()
        {
            if (Settings.Instance.BuyAmmo)
                if (BuyAmmoController.CurrentBuyAmmoState != BuyAmmoState.Done && BuyAmmoController.CurrentBuyAmmoState != BuyAmmoState.DisabledForThisSession)
                {
                    if (DateTime.UtcNow > QCache.Instance.EveAccount.LastAmmoBuy.AddHours(8) && DateTime.UtcNow > QCache.Instance.EveAccount.LastBuyLpItemAttempt.AddHours(1))
                    {
                        QCache.Instance.InjectorWcfClient.GetPipeProxy.SetEveAccountAttributeValue(QCache.Instance.CharName, "LastAmmoBuyAttempt", DateTime.UtcNow);
                        ControllerManager.Instance.AddController(new BuyAmmoController());
                        return false;
                    }
                }

            return true;
        }

        private static bool AttemptToBuyLpItems()
        {
            if (Settings.Instance.BuyLpItems)
                if (DateTime.UtcNow > QCache.Instance.EveAccount.LastBuyLpItems.AddDays(8) && DateTime.UtcNow > QCache.Instance.EveAccount.LastBuyLpItemAttempt.AddDays(1))
                {
                    _States.CurrentBuyLpItemsState = BuyLpItemsState.Idle;
                    QCache.Instance.InjectorWcfClient.GetPipeProxy.SetEveAccountAttributeValue(QCache.Instance.CharName, "LastBuyLpItemAttempt", DateTime.UtcNow);
                    ControllerManager.Instance.AddController(new BuyLpItemsController());
                    return false;
                }

            return true;
        }

        private static bool AttemptToCourierContractItems()
        {
            if (Settings.Instance.CreateCourierContracts)
                if (DateTime.UtcNow > QCache.Instance.EveAccount.LastCreateContract.AddDays(1) && DateTime.UtcNow > QCache.Instance.EveAccount.LastCreateContractAttempt.AddHours(4))
                {
                    _States.CurrentCourierContractState = CourierContractState.Idle;
                    QCache.Instance.InjectorWcfClient.GetPipeProxy.SetEveAccountAttributeValue(QCache.Instance.CharName, "LastCreateContractAttempt", DateTime.UtcNow);
                    ControllerManager.Instance.AddController(new CourierContractController());
                    return false;
                }

            return true;
        }

        private static void CourierMissionArmCmbState()
        {
            if (!AttemptToBuyAmmo()) return;
            if (!AttemptToBuyLpItems()) return;

            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.CourierMission);
            return;
        }

        private static void LocalWatchCMBState()
        {
            if (Settings.Instance.UseLocalWatch)
            {
                Time.Instance.LastLocalWatchAction = DateTime.UtcNow;

                if (DebugConfig.DebugArm) Log.WriteLine("Starting: Is LocalSafe check...");
                if (QCache.Instance.LocalSafe(Settings.Instance.LocalBadStandingPilotsToTolerate, Settings.Instance.LocalBadStandingLevelToConsiderBad))
                {
                    Log.WriteLine("local is clear");
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.WarpOutStation);
                    return;
                }

                Log.WriteLine("Bad standings pilots in local: We will stay 5 minutes in the station and then we will check if it is clear again");
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.WaitingforBadGuytoGoAway);
                return;
            }

            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.WarpOutStation);
            return;
        }

        private static void WaitingFoBadGuyToGoAway()
        {
            if (DateTime.UtcNow.Subtract(Time.Instance.LastLocalWatchAction).TotalMinutes <
                Time.Instance.WaitforBadGuytoGoAway_minutes + QCache.Instance.RandomNumber(1, 3))
                return;

            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.LocalWatch);
            return;
        }

        private static void WarpOutBookmarkCMBState()
        {
            if (!string.IsNullOrEmpty(Settings.Instance.UndockBookmarkPrefix))
            {
                IEnumerable<DirectBookmark> warpOutBookmarks = QCache.Instance.BookmarksByLabel(Settings.Instance.UndockBookmarkPrefix ?? "");
                if (warpOutBookmarks != null && warpOutBookmarks.Any())
                {
                    var warpOutBookmark =
                        warpOutBookmarks.OrderByDescending(b => b.CreatedOn)
                            .FirstOrDefault(b => b.LocationId == QCache.Instance.DirectEve.Session.SolarSystemId && b.DistanceFromEntity(QCache.Instance.ClosestStation._directEntity) < 10000000);

                    long solarid = QCache.Instance.DirectEve.Session.SolarSystemId ?? -1;

                    if (warpOutBookmark == null)
                    {
                        Log.WriteLine("No Bookmark");
                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoMission);
                    }
                    else if (warpOutBookmark.LocationId == solarid)
                    {
                        if (Traveler.Destination == null)
                        {
                            Log.WriteLine("Warp at " + warpOutBookmark.Title);
                            Traveler.Destination = new BookmarkDestination(warpOutBookmark);
                        }

                        Traveler.ProcessState();
                        if (_States.CurrentTravelerState == TravelerState.AtDestination)
                        {
                            Log.WriteLine("Safe!");
                            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoMission);
                            Traveler.Destination = null;
                        }
                    }
                    else
                    {
                        Log.WriteLine("No Bookmark in System");
                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoMission);
                    }

                    return;
                }
            }

            Log.WriteLine("No Bookmark in System");
            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoMission);
            return;
        }

        private static void GotoBaseCMBState()
        {
            Salvage.CurrentlyShouldBeSalvaging = false;

            if (QCache.Instance.InSpace && !QCache.Instance.InStation)
            {
                if (DebugConfig.DebugGotobase) Log.WriteLine("GotoBase: AvoidBumpingThings()");
                NavigateOnGrid.AvoidBumpingThings(QCache.Instance.BigObjectsandGates.FirstOrDefault(), "CombatMissionsBehaviorState.GotoBase");
            }

            if (DebugConfig.DebugGotobase) Log.WriteLine("GotoBase: Traveler.TravelHome()");

            Traveler.TravelHome("CombatMissionsBehavior.TravelHome");

            if (_States.CurrentTravelerState == TravelerState.AtDestination && QCache.Instance.InStation)
            {
                if (DebugConfig.DebugGotobase) Log.WriteLine("GotoBase: We are at destination");

                if (Settings.Instance.BuyPlex && BuyPlexController.ShouldBuyPlex)
                {
                    BuyPlexController.CheckBuyPlex();
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Idle);
                    return;
                }

                if (QCache.Instance.Agent.AgentId != 0)
                    try
                    {
                        MissionSettings.UpdateMissionName(QCache.Instance.Agent.AgentId);
                    }
                    catch (Exception exception)
                    {
                        Log.WriteLine("Cache.Instance.Mission = Cache.Instance.GetAgentMission(AgentID); [" + exception + "]");
                    }

                if (_States.CurrentCombatMissionCtrlState == CombatMissionCtrlState.Error)
                {
                    Log.WriteLine("CMB: if (_States.CurrentCombatMissionCtrlState == CombatMissionCtrlState.Error)");
                    Traveler.Destination = null;
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Error, true);
                    return;
                }

                if (_States.CurrentCombatState != CombatState.OutOfAmmo && MissionSettings.Mission != null &&
                    MissionSettings.Mission.State == (int) MissionState.Accepted)
                {
                    Traveler.Destination = null;
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.CompleteMission, true);
                    return;
                }

                Traveler.Destination = null;
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.UnloadLoot, true);
                return;
            }

            return;
        }

        private static void CompleteMissionCMBState()
        {
            if (!QCache.Instance.InStation) return;

            if (_States.CurrentAgentInteractionState == AgentInteractionState.Idle)
            {
                Log.WriteLine("Start Conversation [Complete Mission]");
                _States.CurrentAgentInteractionState = AgentInteractionState.StartConversation;
                AgentInteraction.Purpose = AgentInteractionPurpose.CompleteMission;
            }

            AgentInteraction.ProcessState();

            if (_States.CurrentAgentInteractionState == AgentInteractionState.Done)
            {
                _States.CurrentAgentInteractionState = AgentInteractionState.Idle;

                if (Statistics.LastMissionCompletionError.AddSeconds(10) < DateTime.UtcNow)
                {
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Statistics);
                    return;
                }
                Log.WriteLine("Skipping statistics: We have not yet completed a mission");
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.UnloadLoot, true);
                return;
            }

            return;
        }

        public static void StatisticsCMBState()
        {
            if (Drones.UseDrones && !QCache.Instance.ActiveShip.IsShipWithNoDroneBay)
            {
                var drone = QCache.Instance.DirectEve.GetInvType(Drones.DroneTypeID);
                if (drone != null)
                {
                    if (Drones.DroneBay == null) return;
                    Statistics.LostDrones = (int) Math.Floor((Drones.DroneBay.Capacity - Drones.DroneBay.UsedCapacity) / drone.Volume);
                    if (!Statistics.WriteDroneStatsLog()) return;
                }
                else
                {
                    Log.WriteLine("Could not find the drone TypeID specified in the character settings xml; this should not happen!");
                }
            }

            if (!Statistics.AmmoConsumptionStatistics()) return;
            Statistics.FinishedMission = DateTime.UtcNow;

            try
            {
                if (!Statistics.MissionLoggingCompleted)
                {
                    Statistics.WriteMissionStatistics(QCache.Instance.Agent.AgentId);
                    return;
                }
            }
            catch(Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }


            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.UnloadLoot);
            return;
        }

        private static void ExecuteMissionCMBState()
        {
            if (!QCache.Instance.InSpace)
                return;

            if (!QCache.Instance.InMission)
                return;

            _combatMissionCtrl.ProcessState();
            //Combat.Combat.ProcessState();
            //Drones.ProcessState();

            if (QCache.Instance.Agent != null && QCache.Instance.Agent.IsValid && QCache.Instance.Agent.IsAgentMissionFinished && !Drones.ActiveDrones.Any())
            {
                Log.WriteLine("Mission objectives are complete, setting state to done.");
                _States.CurrentCombatMissionCtrlState = CombatMissionCtrlState.Done;
            }

            if (_States.CurrentCombatState == CombatState.OutOfAmmo)
            {
                Log.WriteLine("Out of Ammo! - Not enough [" + MissionSettings.CurrentDamageType + "] ammo in cargohold: MinimumCharges: [" +
                              Combat.Combat.MinimumAmmoCharges +
                              "]");
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true);
                QCache.Instance.LootedContainers.Clear();
            }

            if (_States.CurrentCombatMissionCtrlState == CombatMissionCtrlState.Done)
            {
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true);
                QCache.Instance.LootedContainers.Clear();
            }

            if (_States.CurrentCombatMissionCtrlState == CombatMissionCtrlState.Error)
            {
                Log.WriteLine("Error");
                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.QUESTOR_ERROR, "Questor Error."));
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true);
                QCache.Instance.LootedContainers.Clear();
            }

            return;
        }

        private static void TravelerCMBState()
        {
            try
            {
                if (NavigateOnGrid.SpeedTank && !Settings.Instance.LootWhileSpeedTanking)
                {
                    if (DebugConfig.DebugTargetWrecks) Log.WriteLine("Salvage.OpenWrecks = false;");
                    Salvage.OpenWrecks = false;
                }

                var destination = QCache.Instance.DirectEve.Navigation.GetDestinationPath();
                if (destination == null || destination.Count == 0)
                {
                    Log.WriteLine("No destination?");
                    _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Error;
                    return;
                }

                if (destination.Count == 1 && destination.FirstOrDefault() == 0)
                    destination[0] = QCache.Instance.DirectEve.Session.SolarSystemId ?? -1;

                if (Traveler.Destination == null || Traveler.Destination.SolarSystemId != destination.LastOrDefault())
                {
                    if (QCache.Instance.AllBookmarks != null && QCache.Instance.AllBookmarks.Any())
                    {
                        IEnumerable<DirectBookmark> bookmarks = QCache.Instance.AllBookmarks.Where(b => b.LocationId == destination.LastOrDefault()).ToList();
                        if (bookmarks.FirstOrDefault() != null && bookmarks.Any())
                        {
                            Traveler.Destination = new BookmarkDestination(bookmarks.OrderBy(b => b.CreatedOn).FirstOrDefault());
                            return;
                        }

                        Log.WriteLine("Destination: [" + QCache.Instance.DirectEve.Navigation.GetLocation(destination.Last()).Name + "]");
                        var lastSolarSystemInRoute = destination.LastOrDefault();


                        Log.WriteLine("Destination: [" + lastSolarSystemInRoute + "]");
                        Traveler.Destination = new SolarSystemDestination(destination.LastOrDefault());
                        return;
                    }

                    return;
                }

                if (_States.CurrentTravelerState == TravelerState.AtDestination)
                {
                    if (_States.CurrentCombatMissionCtrlState == CombatMissionCtrlState.Error)
                    {
                        Log.WriteLine("an error has occurred");
                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Error, true);
                        return;
                    }

                    if (QCache.Instance.InSpace)
                    {
                        Log.WriteLine("Arrived at destination (in space, Questor stopped)");
                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Error, true);
                        return;
                    }

                    Log.WriteLine("Arrived at destination");
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Idle, true);
                    return;
                }

                Traveler.ProcessState();
                return;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return;
            }
        }

        private static void GotoMissionCmbState()
        {
            try
            {
                Statistics.MissionLoggingCompleted = false;
                Drones.IsMissionPocketDone = false;
                QCache.LootAlreadyUnloaded = false;

                var missionDestination = Traveler.Destination as MissionBookmarkDestination;

                if (missionDestination == null || missionDestination.AgentId != QCache.Instance.Agent.AgentId)
                {
                    var nameOfBookmark = "";
                    if (Settings.Instance.EveServerName == "Tranquility") nameOfBookmark = "Encounter";
                    if (Settings.Instance.EveServerName == "Serenity") nameOfBookmark = "遭遇战";
                    if (nameOfBookmark == "") nameOfBookmark = "Encounter";
                    if (MissionSettings.GetMissionBookmark(QCache.Instance.Agent.AgentId, nameOfBookmark) != null)
                    {
                        Log.WriteLine("Setting Destination to 1st bookmark from AgentID: " + QCache.Instance.Agent.AgentId + " with [" + nameOfBookmark +
                                      "] in the title");
                        Traveler.Destination =
                            new MissionBookmarkDestination(MissionSettings.GetMissionBookmark(QCache.Instance.Agent.AgentId, nameOfBookmark));
                        if (QCache.Instance.DirectEve.Navigation.GetLocation(Traveler.Destination.SolarSystemId) != null)
                        {
                            QCache.Instance.MissionSolarSystem = QCache.Instance.DirectEve.Navigation.GetLocation(Traveler.Destination.SolarSystemId);
                            Log.WriteLine("MissionSolarSystem is [" + QCache.Instance.MissionSolarSystem.Name + "]");
                        }
                    }
                    else
                    {
                        Log.WriteLine("We have no mission bookmark available for our current/normal agent");
                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true);
                    }
                }

                Traveler.ProcessState();

                if (_States.CurrentTravelerState == TravelerState.AtDestination)
                {
                    _States.CurrentCombatMissionCtrlState = CombatMissionCtrlState.Start;
                    Traveler.Destination = null;
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.ExecuteMission, true);
                    return;
                }

                return;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return;
            }
        }

        private static void UnloadLootCMBState()
        {
            try
            {
                if (!QCache.Instance.InStation)
                    return;

                if (_States.CurrentUnloadLootState == UnloadLootState.Idle)
                {
                    Log.WriteLine("UnloadLoot: Begin");
                    _States.CurrentUnloadLootState = UnloadLootState.Begin;
                }

                UnloadLoot.ProcessState();

                if (_States.CurrentUnloadLootState == UnloadLootState.Done)
                {
                    QCache.LootAlreadyUnloaded = true;
                    _States.CurrentUnloadLootState = UnloadLootState.Idle;
                    MissionSettings.UpdateMissionName(QCache.Instance.Agent.AgentId);

                    if (_States.CurrentCombatState == CombatState.OutOfAmmo)
                    {
                        Log.WriteLine("_States.CurrentCombatState == CombatState.OutOfAmmo");
                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Idle, true);
                        return;
                    }

                    if (MissionSettings.Mission != null && MissionSettings.Mission.State != (int) MissionState.Offered)
                    {
                        Log.WriteLine("We are on mission");
                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Idle, true);
                        return;
                    }

                    if (Salvage.AfterMissionSalvaging)
                    {
                        if (QCache.Instance.GetSalvagingBookmark == null)
                        {
                            Log.WriteLine(" No more salvaging bookmarks. Setting FinishedSalvaging Update.");
                            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Idle, true);
                            Statistics.FinishedSalvaging = DateTime.UtcNow;
                            return;
                        }
                        else
                        {
                            Log.WriteLine("There are [ " + QCache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ").Count +
                                          " ] more salvage bookmarks left to process");

                            if (Salvage.SalvageMultipleMissionsinOnePass)
                            {
                                if (DateTime.UtcNow.Subtract(Statistics.FinishedSalvaging).TotalMinutes >
                                    Time.Instance.WrecksDisappearAfter_minutes - Time.Instance.AverageTimeToCompleteAMission_minutes -
                                    Time.Instance.AverageTimetoSalvageMultipleMissions_minutes)
                                {
                                    Log.WriteLine("The last finished after mission salvaging session was [" +
                                                  DateTime.UtcNow.Subtract(Statistics.FinishedSalvaging).TotalMinutes + "] ago ");
                                    Log.WriteLine("we are after mission salvaging again because it has been at least [" +
                                                  (Time.Instance.WrecksDisappearAfter_minutes - Time.Instance.AverageTimeToCompleteAMission_minutes -
                                                   Time.Instance.AverageTimetoSalvageMultipleMissions_minutes) + "] min since the last session. ");
                                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.CheckBookmarkAge, true);
                                    Statistics.StartedSalvaging = DateTime.UtcNow;
                                }
                                else
                                {
                                    Log.WriteLine("The last finished after mission salvaging session was [" +
                                                  DateTime.UtcNow.Subtract(Statistics.FinishedSalvaging).TotalMinutes + "] ago ");
                                    Log.WriteLine("we are going to the next mission because it has not been [" +
                                                  (Time.Instance.WrecksDisappearAfter_minutes - Time.Instance.AverageTimeToCompleteAMission_minutes -
                                                   Time.Instance.AverageTimetoSalvageMultipleMissions_minutes) + "] min since the last session. ");
                                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Idle, true);
                                }
                            }
                            else
                            {
                                Log.WriteLine("The last after mission salvaging session was [" +
                                              Math.Round(DateTime.UtcNow.Subtract(Statistics.FinishedSalvaging).TotalMinutes, 0) + "min] ago ");
                                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.CheckBookmarkAge, true);
                                Statistics.StartedSalvaging = DateTime.UtcNow;
                            }
                        }
                    }
                    else
                    {
                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Idle, true);
                        _States.CurrentQuestorState = QuestorState.Idle;
                        Log.WriteLine("CharacterMode: [" + Settings.Instance.CharacterMode + "], AfterMissionSalvaging: [" + Salvage.AfterMissionSalvaging +
                                      "], CombatMissionsBehaviorState: [" + _States.CurrentCombatMissionBehaviorState + "]");
                        return;
                    }

                    return;
                }

                return;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return;
            }
        }

        private static void BeginAftermissionSalvagingCMBState()
        {
            Statistics.StartedSalvaging = DateTime.UtcNow;
            Drones.IsMissionPocketDone = false;
            Salvage.CurrentlyShouldBeSalvaging = true;

            if (DateTime.UtcNow.Subtract(_lastSalvageTrip).TotalMinutes < Time.Instance.DelayBetweenSalvagingSessions_minutes &&
                Settings.Instance.CharacterMode.ToLower() == "salvage".ToLower())
            {
                Log.WriteLine("Too early for next salvage trip");
                return;
            }

            if (DateTime.UtcNow > _nextBookmarkRefreshCheck)
            {
                _nextBookmarkRefreshCheck = DateTime.UtcNow.AddMinutes(1);
                if (QCache.Instance.InStation && DateTime.UtcNow > _nextBookmarksrefresh)
                {
                    _nextBookmarksrefresh = DateTime.UtcNow.AddMinutes(QCache.Instance.RandomNumber(2, 4));
                    Log.WriteLine("Refreshing Bookmarks Now: Next Bookmark refresh in [" +
                                  Math.Round(_nextBookmarksrefresh.Subtract(DateTime.UtcNow).TotalMinutes, 0) +
                                  "min]");
                    QCache.Instance.DirectEve.RefreshBookmarks();
                    return;
                }

                Log.WriteLine("Next Bookmark refresh in [" + Math.Round(_nextBookmarksrefresh.Subtract(DateTime.UtcNow).TotalMinutes, 0) + "min]");
            }


            Salvage.OpenWrecks = true;


            if (_States.CurrentArmState == ArmState.Idle)
                Arm.ChangeArmState(ArmState.ActivateSalvageShip);

            Arm.ProcessState();
            if (_States.CurrentArmState == ArmState.Done)
            {
                Arm.ChangeArmState(ArmState.Idle);
                var bookmark = QCache.Instance.GetSalvagingBookmark;
                if (bookmark == null && QCache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ").Any())
                {
                    bookmark = QCache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ").OrderBy(b => b.CreatedOn).FirstOrDefault();
                    if (bookmark == null)
                    {
                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Idle, true);
                        return;
                    }
                }

                _lastSalvageTrip = DateTime.UtcNow;
                Traveler.Destination = new BookmarkDestination(bookmark);
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoSalvageBookmark, true);
                return;
            }

            return;
        }

        private static void SalvageCMBState()
        {
            Salvage.SalvageAll = true;
            Salvage.OpenWrecks = true;
            Salvage.CurrentlyShouldBeSalvaging = true;

            var deadlyNPC = Combat.Combat.PotentialCombatTargets.FirstOrDefault();
            if (deadlyNPC != null)
            {
                var missionSalvageBookmarks = QCache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ");
                Log.WriteLine("could not be completed because of NPCs left in the mission: deleting on grid salvage bookmark");

                if (Salvage.DeleteBookmarksWithNPC)
                {
                    if (!QCache.Instance.DeleteBookmarksOnGrid("CombatMissionsBehavior.Salvage")) return;
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoSalvageBookmark, true);
                    var bookmark = missionSalvageBookmarks.OrderBy(i => i.CreatedOn).FirstOrDefault();
                    Traveler.Destination = new BookmarkDestination(bookmark);
                    return;
                }

                Log.WriteLine("could not be completed because of NPCs left in the mission: on grid salvage bookmark not deleted");
                Salvage.SalvageAll = false;
                Statistics.FinishedSalvaging = DateTime.UtcNow;
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase);
                return;
            }

            if (Salvage.UnloadLootAtStation)
                if (QCache.Instance.CurrentShipsCargo != null && QCache.Instance.CurrentShipsCargo.UsedCapacity > 0)
                    if (QCache.Instance.CurrentShipsCargo.Capacity - QCache.Instance.CurrentShipsCargo.UsedCapacity <
                        Salvage.ReserveCargoCapacity + 10)
                    {
                        if (Drones.DronePriorityEntities.Any(pt => pt.IsWarpScramblingMe))
                            return;

                        Log.WriteLine("We are full: My Cargo is at [" +
                                      Math.Round(QCache.Instance.CurrentShipsCargo.UsedCapacity, 2) + "m3] of[" +
                                      Math.Round(QCache.Instance.CurrentShipsCargo.Capacity, 2) + "] Reserve [" +
                                      Math.Round((double) Salvage.ReserveCargoCapacity, 2) + "m3 + 10], go to base to unload");
                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase);
                        return;
                    }

            if (!QCache.Instance.MyShipEntity.SalvagersAvailable || !QCache.Instance.UnlootedContainers.Any() ||
                DateTime.UtcNow > Time.Instance.LastInWarp.AddMinutes(Time.Instance.MaxSalvageMinutesPerPocket))
            {
                if (!QCache.Instance.DeleteBookmarksOnGrid("CombatMissionsBehavior.Salvage")) return;

                if (DateTime.UtcNow > Time.Instance.LastInWarp.AddMinutes(Time.Instance.MaxSalvageMinutesPerPocket))
                    Log.WriteLine("We have been salvaging this pocket for more than [" + Time.Instance.MaxSalvageMinutesPerPocket +
                                  "] min - moving on - something probably went wrong here somewhere. (debugSalvage might help narrow down what)");

                Log.WriteLine("Finished salvaging the pocket. UnlootedContainers [" + QCache.Instance.UnlootedContainers.Count() + "] Wrecks [" +
                              QCache.Instance.Wrecks +
                              "] Salvagers? [" + QCache.Instance.MyShipEntity.SalvagersAvailable + "]");
                Statistics.FinishedSalvaging = DateTime.UtcNow;

                if (!QCache.Instance.AfterMissionSalvageBookmarks.Any() && !QCache.Instance.GateInGrid())
                {
                    Log.WriteLine("We have salvaged all bookmarks, go to base");
                    Salvage.SalvageAll = false;
                    Statistics.FinishedSalvaging = DateTime.UtcNow;
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase);
                    return;
                }

                if (!QCache.Instance.GateInGrid())
                {
                    Log.WriteLine("Go to the next salvage bookmark");
                    DirectBookmark bookmark;
                    if (Salvage.FirstSalvageBookmarksInSystem)
                        bookmark =
                            QCache.Instance.AfterMissionSalvageBookmarks.FirstOrDefault(c => c.LocationId == QCache.Instance.DirectEve.Session.SolarSystemId) ??
                            QCache.Instance.AfterMissionSalvageBookmarks.FirstOrDefault();
                    else
                        bookmark = QCache.Instance.AfterMissionSalvageBookmarks.OrderBy(i => i.CreatedOn).FirstOrDefault() ??
                                   QCache.Instance.AfterMissionSalvageBookmarks.FirstOrDefault();

                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoSalvageBookmark, true);
                    Traveler.Destination = new BookmarkDestination(bookmark);
                    return;
                }

                if (Salvage.UseGatesInSalvage)
                {
                    Log.WriteLine("Acceleration gate found - moving to next pocket");
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.SalvageUseGate, true);
                    return;
                }

                Log.WriteLine("Acceleration gate found, useGatesInSalvage set to false - Returning to base");
                Statistics.FinishedSalvaging = DateTime.UtcNow;
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true);
                Traveler.Destination = null;
                return;
            }

            if (DebugConfig.DebugSalvage)
                Log.WriteLine("salvage: we __cannot ever__ approach in salvage.cs so this section _is_ needed");
            Salvage.MoveIntoRangeOfWrecks();
            try
            {
                Salvage.DedicatedSalvagerMaximumWreckTargets = QCache.Instance.MaxLockedTargets;
                Salvage.DedicatedSalvagerReserveCargoCapacity = 80;
                Salvage.DedicatedSalvagerLootEverything = true;
            }
            finally
            {
                Salvage.DedicatedSalvagerMaximumWreckTargets = null;
                Salvage.DedicatedSalvagerReserveCargoCapacity = null;
                Salvage.DedicatedSalvagerLootEverything = null;
            }

            return;
        }

        private static void SalvageGotoBookmarkCMBState()
        {
            Traveler.ProcessState();

            if (_States.CurrentTravelerState == TravelerState.AtDestination || QCache.Instance.GateInGrid())
            {
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Salvage, true);
                Traveler.Destination = null;
                return;
            }

            return;
        }

        private static void SalvageUseGateCMBState()
        {
            Salvage.OpenWrecks = true;

            if (QCache.Instance.AccelerationGates == null || !QCache.Instance.AccelerationGates.Any())
            {
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoSalvageBookmark, true);
                return;
            }

            _lastX = QCache.Instance.ActiveShip.Entity.X;
            _lastY = QCache.Instance.ActiveShip.Entity.Y;
            _lastZ = QCache.Instance.ActiveShip.Entity.Z;


            var closest = QCache.Instance.AccelerationGates.OrderBy(t => t.Distance).FirstOrDefault();
            if (closest != null && closest.Distance < (int) Distances.DecloakRange)
            {
                Log.WriteLine("Gate found: [" + closest.Name + "] groupID[" + closest.GroupId + "]");

                if (closest.Activate())
                {
                    Log.WriteLine("Activate [" + closest.Name + "] and change States.CurrentCombatMissionBehaviorState to 'NextPocket'");
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.SalvageNextPocket, true);
                    _lastPulse = DateTime.UtcNow;
                    return;
                }

                return;
            }

            NavigateOnGrid.NavigateToTarget(closest, "SalvageUseGates", false, 0);
            _lastPulse = DateTime.UtcNow.AddSeconds(10);
        }

        private static void SalvageNextPocketCMBState()
        {
            Salvage.OpenWrecks = true;
            var distance = QCache.Instance.DistanceFromMe(_lastX, _lastY, _lastZ);
            if (distance > (int) Distances.NextPocketDistance)
            {
                Log.WriteLine("We have moved to the next Pocket [" + Math.Round(distance / 1000, 0) + "k away]");
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Salvage, true);
                return;
            }

            if (DateTime.UtcNow.Subtract(_lastPulse).TotalMinutes > 2)
            {
                Log.WriteLine("We have timed out, retry last action");
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.SalvageUseGate, true);
                return;
            }

            return;
        }

        private static void PrepareStorylineGotoBaseCMBState()
        {
            if (DebugConfig.DebugGotobase) Log.WriteLine("PrepareStorylineGotoBase: AvoidBumpingThings()");
            NavigateOnGrid.AvoidBumpingThings(QCache.Instance.BigObjectsandGates.FirstOrDefault(), "CombatMissionsBehaviorState.PrepareStorylineGotoBase");

            if (DebugConfig.DebugGotobase) Log.WriteLine("PrepareStorylineGotoBase: Traveler.TravelHome()");

            if (Settings.Instance.StoryLineBaseBookmark != "")
            {
                if (!Traveler.TravelToBookmarkName(Settings.Instance.StoryLineBaseBookmark, "CombatMissionsBehavior.TravelHome"))
                    Traveler.TravelHome("CombatMissionsBehavior.TravelHome");
            }
            else
            {
                Traveler.TravelHome("CombatMissionsBehavior.TravelHome");
            }

            if (_States.CurrentTravelerState == TravelerState.AtDestination && QCache.Instance.InStation)
            {
                if (DebugConfig.DebugGotobase) Log.WriteLine("PrepareStorylineGotoBase: We are at destination");
                if (QCache.Instance.Agent.AgentId != 0)
                    try
                    {
                        MissionSettings.UpdateMissionName(QCache.Instance.CurrentStorylineAgentId);
                    }
                    catch (Exception exception)
                    {
                        Log.WriteLine("Cache.Instance.Mission = Cache.Instance.GetAgentMission(AgentID); [" + exception + "]");
                        return;
                    }

                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Storyline, true);
                return;
            }

            return;
        }

        private static void GotoNearestStationCMBState()
        {
            if (!QCache.Instance.InSpace || (QCache.Instance.InSpace && QCache.Instance.InWarp) || (QCache.Instance.InSpace && QCache.Instance.MyShipEntity.IsWarping)) return;
            EntityCache station = null;
            if (QCache.Instance.Stations != null && QCache.Instance.Stations.Any())
                station = QCache.Instance.Stations.OrderBy(x => x.Distance).FirstOrDefault();

            if (station != null)
            {
                if (station.Distance < (int) Distances.DockingRange)
                {
                    if (station.Dock())
                    {
                        Log.WriteLine("[" + station.Name + "] which is [" + Math.Round(station.Distance / 1000, 0) + "k away]");
                        return;
                    }

                    return;
                }
                else
                {
                    NavigateOnGrid.NavigateToTarget(station, "GotoNearestStation", false, 0);
                }

                return;
            }

            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Error, true);
            return;
        }

        private static void StorylineReturnToBaseCMBState()
        {
            _storyline.Reset();
            QCache.Instance.CurrentAgent = null;
            QCache.Instance.Agent = null;
            if (DebugConfig.DebugGotobase) Log.WriteLine("StorylineReturnToBase: AvoidBumpingThings()");
            NavigateOnGrid.AvoidBumpingThings(QCache.Instance.BigObjectsandGates.FirstOrDefault(), "CombatMissionsBehaviorState.StorylineReturnToBase");

            if (DebugConfig.DebugGotobase) Log.WriteLine("StorylineReturnToBase: TravelToStorylineBase");


            Traveler.TravelHome("CombatMissionsBehavior.TravelToStorylineBase");


            if (_States.CurrentTravelerState == TravelerState.AtDestination && QCache.Instance.InStation)
            {
                if (DebugConfig.DebugGotobase) Log.WriteLine("StorylineReturnToBase: We are at destination");
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Switch, true);
            }

            return;
        }

        private static bool CMBEveryPulse()
        {
            if (!QCache.Instance.InSpace && !QCache.Instance.InStation)
                return false;

            if (Settings.Instance.FinishWhenNotSafe && _States.CurrentCombatMissionBehaviorState != CombatMissionsBehaviorState.GotoNearestStation)
                if (QCache.Instance.InSpace &&
                    !QCache.Instance.LocalSafe(Settings.Instance.LocalBadStandingPilotsToTolerate, Settings.Instance.LocalBadStandingLevelToConsiderBad))
                {
                    EntityCache station = null;
                    if (QCache.Instance.Stations != null && QCache.Instance.Stations.Any())
                        station = QCache.Instance.Stations.OrderBy(x => x.Distance).FirstOrDefault();

                    if (station != null)
                    {
                        Log.WriteLine("Station found. Going to nearest station");
                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoNearestStation, true);
                    }
                    else
                    {
                        Log.WriteLine("Station not found. Going back to base");
                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true);
                    }
                }

            Panic.ProcessState();

            if (_States.CurrentPanicState == PanicState.Resume)
            {
                if (QCache.Instance.InSpace || QCache.Instance.InStation)
                {
                    _States.CurrentPanicState = PanicState.Normal;

                    if (_States.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.Storyline)
                    {
                        Log.WriteLine("PanicState.Resume: CMB State is Storyline");
                        if (_storyline.StorylineHandler is GenericCombatStoryline)
                        {
                            (_storyline.StorylineHandler as GenericCombatStoryline).State = GenericCombatStorylineState.GotoMission;
                            Log.WriteLine("PanicState.Resume: Setting GenericCombatStorylineState to GotoMission");
                        }

                        return true;
                    }

                    if (QCache.Instance.CurrentStorylineAgentId >= 500)
                    {
                        Log.WriteLine("PanicState.Resume: CurrentStorylineAgentId >= 500");
                        if (_storyline.StorylineHandler is GenericCombatStoryline)
                        {
                            (_storyline.StorylineHandler as GenericCombatStoryline).State = GenericCombatStorylineState.GotoMission;
                            Log.WriteLine("PanicState.Resume: Setting GenericCombatStorylineState to GotoMission");
                        }

                        return true;
                    }

                    _States.CurrentTravelerState = TravelerState.Idle;
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoMission);
                    return true;
                }

                return false;
            }

            return true;
        }

        public static void ProcessState()
        {
            try
            {
                if (!CMBEveryPulse()) return;

                if (string.IsNullOrEmpty(QCache.Instance.CurrentAgent))
                {
                    if (DebugConfig.DebugAgentInteractionReplyToAgent)
                        Log.WriteLine("if (string.IsNullOrEmpty(Cache.Instance.CurrentAgent)) return;");
                    return;
                }

                if (DebugConfig.DebugCombatMissionsBehavior) Log.WriteLine("_States.CurrentCombatMissionBehaviorState is [" + _States.CurrentCombatMissionBehaviorState + "]");

                switch (_States.CurrentCombatMissionBehaviorState)
                {
                    case CombatMissionsBehaviorState.Idle:
                        IdleCMBState();
                        break;

                    //case CombatMissionsBehaviorState.DelayedStart:
                    //    DelayedStartCMBState();
                    //    break;

                    case CombatMissionsBehaviorState.DelayedGotoBase:
                        DelayedGotoBaseCMBState();
                        break;

                    case CombatMissionsBehaviorState.Cleanup:
                        CleanupCMBState();
                        break;

                    case CombatMissionsBehaviorState.Start:
                        StartCMBState();
                        break;

                    case CombatMissionsBehaviorState.Switch:
                        SwitchCMBState();
                        break;

                    case CombatMissionsBehaviorState.Arm:
                        ArmCMBState();
                        break;

                    case CombatMissionsBehaviorState.LocalWatch:
                        LocalWatchCMBState();
                        break;

                    case CombatMissionsBehaviorState.WaitingforBadGuytoGoAway:
                        WaitingFoBadGuyToGoAway();
                        break;

                    case CombatMissionsBehaviorState.WarpOutStation:
                        //WarpOutBookmarkCMBState();
                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoMission);
                        break;

                    case CombatMissionsBehaviorState.GotoMission:
                        GotoMissionCmbState();
                        break;

                    case CombatMissionsBehaviorState.ExecuteMission:
                        ExecuteMissionCMBState();
                        break;

                    case CombatMissionsBehaviorState.GotoBase:
                        GotoBaseCMBState();
                        break;

                    case CombatMissionsBehaviorState.CompleteMission:
                        CompleteMissionCMBState();
                        break;

                    case CombatMissionsBehaviorState.Statistics:
                        StatisticsCMBState();
                        break;

                    case CombatMissionsBehaviorState.UnloadLoot:
                        UnloadLootCMBState();
                        break;

                    case CombatMissionsBehaviorState.CheckBookmarkAge:
                        if (!QCache.Instance.DeleteUselessSalvageBookmarks("RemoveOldBookmarks")) return;
                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.BeginAfterMissionSalvaging);
                        Statistics.StartedSalvaging = DateTime.UtcNow;
                        break;

                    case CombatMissionsBehaviorState.BeginAfterMissionSalvaging:
                        BeginAftermissionSalvagingCMBState();
                        break;

                    case CombatMissionsBehaviorState.GotoSalvageBookmark:
                        SalvageGotoBookmarkCMBState();
                        break;

                    case CombatMissionsBehaviorState.Salvage:
                        SalvageCMBState();
                        break;

                    case CombatMissionsBehaviorState.SalvageUseGate:
                        SalvageUseGateCMBState();
                        break;

                    case CombatMissionsBehaviorState.SalvageNextPocket:
                        SalvageNextPocketCMBState();
                        break;

                    case CombatMissionsBehaviorState.PrepareStorylineSwitchAgents:
                        DirectAgent agent = null;
                        if (_storyline.StorylineMission != null)
                            if (_storyline.StorylineMission.AgentId != 0)
                                agent = QCache.Instance.DirectEve.GetAgentById(_storyline.StorylineMission.AgentId);

                        if (agent != null)
                        {
                            QCache.Instance.CurrentAgent = agent.Name;
                            QCache.Instance.CurrentStorylineAgentId = agent.AgentId;
                            QCache.Instance.AgentStationID = agent.StationId;
                            QCache.Instance.Agent = null;
                            Log.WriteLine("new agent is [ " + QCache.Instance.CurrentAgent + " ]");

                            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.PrepareStorylineChangeFitting);
                        }
                        else
                        {
                            Log.WriteLine("Storyline agent  error.");
                            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Error);
                        }

                        break;

                    case CombatMissionsBehaviorState.PrepareStorylineChangeFitting:
                        if (!Settings.Instance.UseFittingManager)
                        {
                            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Storyline);
                        }

                        if (_States.CurrentArmState == ArmState.Idle)
                        {
                            Arm.ChangeArmState(ArmState.LoadSavedFitting);
                        }

                        Arm.LoadSavedFitting(MissionSettings.DefaultFittingName, ArmState.Done);

                        if (_States.CurrentArmState == ArmState.Done)
                        {
                            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Storyline);
                        }

                        break;

                    case CombatMissionsBehaviorState.PrepareStorylineGotoBase:
                        PrepareStorylineGotoBaseCMBState();
                        break;

                    case CombatMissionsBehaviorState.Storyline:
                        _storyline.ProcessState();
                        if (_States.CurrentStorylineState == StorylineState.Done)
                        {
                            Log.WriteLine("We have completed the storyline, resetting agent and returning to base");
                            _States.CurrentStorylineState = StorylineState.Idle;
                            QCache.Instance.CurrentAgent = null;
                            QCache.Instance.Agent = null;
                            var a = QCache.Instance.Agent;
                            _storyline.Reset();

                            if (a != null)
                            {
                            }
                            else
                            {
                                Log.WriteLine("Storyline agent  error: agent == null");
                                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Error);
                            }

                            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase, true);
                            break;
                        }
                        break;

                    case CombatMissionsBehaviorState.StorylineReturnToBase:
                        StorylineReturnToBaseCMBState();
                        break;

                    case CombatMissionsBehaviorState.CourierMissionArm:
                        CourierMissionArmCmbState();
                        break;

                    case CombatMissionsBehaviorState.CourierMission:

                        if (_States.CurrentCourierMissionCtrlState == CourierMissionCtrlState.Idle)
                            CourierMissionCtrl.ChangeCourierMissionBehaviorState(CourierMissionCtrlState.Start);

                        _courierMissionCtrl.ProcessState();
                        break;

                    case CombatMissionsBehaviorState.Traveler:
                        TravelerCMBState();
                        break;

                    case CombatMissionsBehaviorState.GotoNearestStation:
                        GotoNearestStationCMBState();
                        break;

                    case CombatMissionsBehaviorState.Default:
                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Idle);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }
    }
}