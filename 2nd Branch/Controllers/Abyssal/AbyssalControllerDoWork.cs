//
// (c) duketwo 2022
//

extern alias SC;
using System;
using System.Linq;
using System.Threading.Tasks;
using EVESharpCore.Cache;
//using EVESharpCore.Controllers.ActionQueue.Actions.Base;
//using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Events;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Traveller;
using EVESharpCore.Questor.States;
using SC::SharedComponents.EVE;
//using SC::SharedComponents.EVE.ClientSettings;
//using SC::SharedComponents.EVE.DatabaseSchemas;
using SC::SharedComponents.Extensions;
using SC::SharedComponents.IPC;
using SC::SharedComponents.SQLite;
using SC::SharedComponents.Utility;
using ServiceStack.OrmLite;
using SC::SharedComponents.Events;
using EVESharpCore.Questor.BackgroundTasks;
using System.Collections.Generic;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Questor.Activities;
using System.Xml;
using System.Windows.Interop;
using System.IO;
using EVESharpCore.Questor.Stats;
using System.Management.Instrumentation;
//using State = EVESharpCore.States.State;

namespace EVESharpCore.Controllers.Abyssal
{
    public partial class AbyssalController : AbyssalBaseController
    {
        public void InvalidateCache()
        {

        }



        private bool ShouldWeRestartEve
        {
            get
            {
                if (!ESCache.Instance.EveAccount.UseScheduler)
                    return false;

                if (AbyssalFilamentsActivated > ESCache.Instance.EveAccount.NumOfAbyssalSitesBeforeRestarting)
                {
                    Log("AbyssalFilamentsActivated[" + AbyssalFilamentsActivated + "] > NumOfAbyssalSitesBeforeRestarting[" + ESCache.Instance.EveAccount.NumOfAbyssalSitesBeforeRestarting + "]");
                    return true;
                }

                return false;
            }
        }

        private bool AreWeAtTheFilamentSpot
        {
            get
            {
                try
                {
                    foreach (var FilamentBookmark in ListOfFilamentBookmarks)
                    {
                        if (FilamentBookmark != null && !FilamentBookmark.IsInCurrentSystem)
                            return false;

                        if (ESCache.Instance.InSpace && FilamentBookmark != null && FilamentBookmark.DistanceTo(ESCache.Instance.ActiveShip.Entity) < 149_000)
                            return true;

                        return false;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        private bool HaveWeHitOurSitesPerSessionLimit
        {
            get
            {
                if (!DirectEve.Interval(60000))
                    return false;

                if (logoffEveAfterThisManyAbyssalRuns >= AbyssalFilamentsActivated)
                {
                    if (DirectEve.Interval(30000)) Log("AbyssalFilamentsActivated[" + AbyssalFilamentsActivated + "] is less than logoffEveAfterThisManyAbyssalRuns [" + logoffEveAfterThisManyAbyssalRuns + "] return false");
                    return false;
                }

                return true;
            }
        }

        private bool ShouldWeGoIdle()
        {
            if (!DirectEve.Interval(120000, 120000, ESCache.Instance.InStation.ToString()))
                return false;

            if (ESCache.Instance.ActiveShip != null)
            {
                if (AbyssalFilamentsActivated == 0)
                {
                    if (DirectEve.Interval(30000)) Log("AbyssalFilamentsActivated[" + AbyssalFilamentsActivated + "]  return false");
                    return false;
                }

                if (5 >= AbyssalFilamentsActivated)
                {
                    if (DirectEve.Interval(30000)) Log("AbyssalFilamentsActivated[" + AbyssalFilamentsActivated + "] is less than 6 return false");
                    return false;
                }

                if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsDestroyer)
                {
                    if (AbyssalFilamentsActivated % 6 != 0)
                    {
                        if (DirectEve.Interval(30000)) Log("AbyssalFilamentsActivated[" + AbyssalFilamentsActivated + "] is not evenly divisible by 6 return false");
                        return false;
                    }
                }
                else if (ESCache.Instance.ActiveShip.IsCruiser)
                {
                    if (AbyssalFilamentsActivated % 3 != 0)
                    {
                        if (DirectEve.Interval(30000)) Log("AbyssalFilamentsActivated[" + AbyssalFilamentsActivated + "] is not evenly divisible by 3 return false");
                        return false;
                    }
                }
            }

            var spanTotalSeconds = (DateTime.UtcNow - _abyssalControllerStarted).TotalSeconds;
            if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log($"_abyssalControllerStarted [{_abyssalControllerStarted.ToShortTimeString()}] spanTotalSeconds [{Math.Round(spanTotalSeconds, 0)}]");

            if (spanTotalSeconds < 600.0d)
                return false;

            if (_sessionChangeIdleCheck)
                return false;

            //If we went to market to buy stuff within the last hour: no need to idle
            if (Time.Instance.StartedTravelToMarketStation.AddHours(1) > DateTime.UtcNow)
                return false;

            var idleDurationMin = 60;
            var idleDurationMax = 240;
            _sessionChangeIdleCheck = true;

            var rnd = Rnd.NextDouble();

            if (rnd >= 0.20d)
            {

                rnd = Rnd.NextDouble();

                if (rnd >= 0.40d)
                {
                    _idleUntil = Time.Instance.LastActivateFilamentAttempt.AddMinutes(15).AddSeconds(Rnd.Next(idleDurationMin, idleDurationMax));
                }
                else
                {
                    _idleUntil = Time.Instance.LastActivateFilamentAttempt.AddMinutes(15).AddSeconds(Rnd.Next(idleDurationMin / 2, idleDurationMax / 2));
                }

                if (DirectEve.Interval(20000)) Log($"_abyssalControllerStarted [{_abyssalControllerStarted.ToShortTimeString()}] spanTotalSeconds [{Math.Round(spanTotalSeconds, 0)}]");
                return true;
            }
            else if (rnd >= 0.10d)
            {
                if (DirectEve.Interval(20000)) Log($"_abyssalControllerStarted [{_abyssalControllerStarted.ToShortTimeString()}] spanTotalSeconds [{Math.Round(spanTotalSeconds, 0)}]");
                _idleUntil = DateTime.UtcNow.AddSeconds(Rnd.Next(idleDurationMin * 4, idleDurationMax * 4));
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool _sessionChangeIdleCheck = false;
        private DateTime _idleUntil = DateTime.MinValue;
        private int _activationErrorTickCount = 0;
        private DateTime _nextActionAfterAbyTraceDespawn = DateTime.MinValue;
        private bool _leftInvulnAfterAbyssState = false;

        public override void DoWork()
        {
            if (DebugConfig.DebugAbyssalDeadspaceBehavior)
            {
                Log("AbyssalController: DoWork");
            }

            if (!Settings.Instance.DefaultSettingsLoaded)
            {
                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log("AbyssalController: Loading Settings");
                Settings.Instance.LoadSettings_Initialize();
            }

            if (ESCache.Instance.InStation && ESCache.Instance.PauseAfterNextDock)
            {
                Log("AbyssalController: if (ESCache.Instance.InStation && ESCache.Instance.PauseAfterNextDock)");
                ControllerManager.Instance.SetPause(true);
                ESCache.Instance.PauseAfterNextDock = false;
                return;
            }

            if (ESCache.Instance.InStation && ESCache.Instance.DeactivateScheduleAndCloseEveAfterNextDock)
            {
                Log("AbyssalController: if (ESCache.Instance.InStation && ESCache.Instance.DeactivateScheduleAndCloseAfterNextDock)");
                if (ESCache.Instance.EveAccount.UseScheduler)
                {
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.UseScheduler), false);
                    ESCache.Instance.DeactivateScheduleAndCloseEveAfterNextDock = false;
                    return;
                }

                ESCache.Instance.CloseEveReason = "DeactivateScheduleAndCloseAfterNextDock";
                ESCache.Instance.BoolCloseEve = true;
                return;
            }

            try
            {
                ProcessState();
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }


            try
            {
                if (ESCache.Instance.InSpace && !ESCache.Instance.InAbyssalDeadspace && !ESCache.Instance.InWormHoleSpace)
                {
                    if (Traveler.BoolRunEveryFrame)
                    {
                        Log("if (Traveler.BoolRunEveryFrame)");
                        LocalPulse = UTCNowAddMilliseconds(100, 150);
                        return;
                    }

                    if (20000 > ESCache.Instance.ClosestStargate.Distance && ESCache.Instance.ClosestStargate.Distance > 12000 && Defense.CovertOpsCloak != null && Defense.CovertOpsCloak.IsOnline && !Defense.CovertOpsCloak.IsActive)
                    {
                        Log("We are near a gate, JumpCloakActive and our CovertOpsCLoak Not Active: LocalPulse set to 250-300ms");
                        LocalPulse = UTCNowAddMilliseconds(250, 300);
                        return;
                    }
                }
            }
            catch (Exception){}

            LocalPulse = UTCNowAddMilliseconds(800, 1200);
        }

        public void ProcessState()
        {
            AbyssalEveryPulse();

            if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log("myAbyssalState [" + myAbyssalState + "]");

            switch (myAbyssalState)
            {
                case AbyssalState.Error:
                    AbyssalError();
                    break;

                case AbyssalState.OutOfDrones:
                    Log("AbyssalState.OutOfDrones: PauseAfterNextDock [true]");
                    ESCache.Instance.PauseAfterNextDock = true;
                    break;

                case AbyssalState.OutOfAmmo:
                    Log("AbyssalState.OutOfAmmo: PauseAfterNextDock [true]");
                    ESCache.Instance.PauseAfterNextDock = true;
                    break;

                case AbyssalState.OutOfBoosters:
                    Log("AbyssalState.OutOfBoosters: PauseAfterNextDock [true]");
                    ESCache.Instance.PauseAfterNextDock = true;
                    break;

                case AbyssalState.OutOfNaniteRepairPaste:
                    Log("AbyssalState.OutOfNaniteRepairPaste: PauseAfterNextDock [true]");
                    ESCache.Instance.PauseAfterNextDock = true;
                    break;

                case AbyssalState.OutOfFilaments:
                    Log("AbyssalState.OutOfDrones: PauseAfterNextDock [true]");
                    ESCache.Instance.PauseAfterNextDock = true;
                    break;

                case AbyssalState.InvulnPhaseAfterAbyssExit:
                    if (ESCache.Instance.InAbyssalDeadspace)
                        return;

                    if (ESCache.Instance.DirectEve.Session.InJump)
                        return;

                    if (!ESCache.Instance.InSpace)
                        return;

                    _leftInvulnAfterAbyssState = true;
                    if (IsAnyOtherNonFleetPlayerOnGrid || !CanAFilamentBeOpened(true))
                    {
                        myAbyssalState = AbyssalState.PVP;
                    }
                    else
                    {
                        myAbyssalState = AbyssalState.Start;
                    }

                    break;

                case AbyssalState.PVP:
                    _leftInvulnAfterAbyssState = false;
                    PVPState();
                    break;

                case AbyssalState.Start:
                    __shipsCargoBayList = null;
                    AbyssalStart();
                    break;

                case AbyssalState.IdleInStation:

                    if (DirectEve.Interval(15000))
                    {
                        Log($"Idle in station until [{_idleUntil}].");
                    }

                    if (_idleUntil <= DateTime.UtcNow)
                    {
                        myAbyssalState = AbyssalState.PrepareToArm;
                    }

                    break;

                case AbyssalState.PrepareToArm:
                    AbyssalPrepareToArm();
                    break;

                case AbyssalState.WaitingOnFleetMembers:
                    if (ESCache.Instance.EveAccount.UseFleetMgr)
                    {
                        if (!AreFleetMembersReady)
                        {
                            //Log("AreFleetMembersReady [ false ] - waiting"); we log this elsewhere no need for double logging
                            return;
                        }
                        Log("AreFleetMembersReady [ true ]");
                        FittingDone = false;
                        Log("PrepareToArm: LoadSavedFitting");
                        myAbyssalState = AbyssalState.LoadSavedFitting;
                    }
                    break;

                case AbyssalState.LoadSavedFitting:
                    if (Time.Instance.LastStripFitting.AddMinutes(3) > DateTime.UtcNow)
                    {
                        if (!LoadSavedFitting("Abyss", AbyssalState.Arm)) return;
                        break;
                    }

                    myAbyssalState = AbyssalState.Arm;
                    break;

                case AbyssalState.Arm:
                    if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log("case AbyssalState.Arm:");
                    AbyssalArm();
                    break;

                case AbyssalState.TravelToFilamentSpot:
                    if (!AreFleetMembersReady)
                    {
                        myAbyssalState = AbyssalState.PrepareToArm;
                        return;
                    }

                    AbyssalTravelToFilamentSpot();
                    break;

                case AbyssalState.TravelToBuyLocation:
                    break;

                case AbyssalState.TravelToHomeLocation:
                    AbyssalTravelToHomeLocation();
                    break;

                case AbyssalState.TravelToRepairLocation:
                    AbyssalTravelToRepairLocation();
                    break;

                case AbyssalState.RepairItems:

                    if (Time.Instance.RepairLedger.Count(i => i.AddMinutes(10) > DateTime.UtcNow) >= 4)
                    {
                        foreach (var entry in Time.Instance.RepairLedger)
                        {
                            Log("RepairLedger Entry [" + entry.ToLongTimeString() + "]");
                        }

                        Log("RepairLedger has 4 or more entries in the last 10 min: We have something damaged that the repair process is not fixing. strip fitting and reload fitting");
                        //Verify we have a fitting first?

                        if (ESCache.Instance.ActiveShip.StripFitting())
                        {
                            Time.Instance.LastStripFitting = DateTime.UtcNow;
                            Log("strip fitting success");
                            Time.Instance.RepairLedger.Clear();
                            myAbyssalState = AbyssalState.Start;
                            return;
                        }

                        return;
                    }

                    if (!Questor.BackgroundTasks.Cleanup.RepairItems())
                        return;

                    Traveler.Destination = null;
                    State.CurrentTravelerState = TravelerState.Idle;
                    myAbyssalState = AbyssalState.TravelToFilamentSpot;
                    Log($"Repair done.");
                    break;

                case AbyssalState.ReplaceShip:
                    break;
                case AbyssalState.ActivateShip:
                    break;

                case AbyssalState.ActivateAbyssalDeadspace:
                    ActivateAbyssalDeadspaceState();
                    break;

                case AbyssalState.UseFilament:
                    if (!AreFleetMembersInFleetAndLocal)
                    {
                        myAbyssalState = AbyssalState.TravelToHomeLocation;
                        Traveler.Destination = null;
                        State.CurrentTravelerState = TravelerState.Idle;
                        return;
                    }

                    AbyssalUseFilament();
                    break;

                case AbyssalState.AbyssalEnter:
                    AbyssalEnter();
                    break;

                case AbyssalState.AbyssalClear:
                    AbyssalClear();
                    break;

                case AbyssalState.UnloadLoot:
                    break;

                case AbyssalState.DumpSurveyDatabases:
                    DumpDatabaseSurveys();
                    break;

                case AbyssalState.BuyItems:
                    BuyItems();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }


        }

        private bool FittingDone = false;
        public bool LoadSavedFitting(string myFittingToLoad, AbyssalState nextAbyssalState) // --> ArmState.MoveDrones
        {
            try
            {
                if (!Settings.Instance.UseFittingManager)
                {
                    Log("if (!Settings.Instance.UseFittingManager)");
                    myAbyssalState = nextAbyssalState;
                    return true;
                }

                //let's check first if we need to change fitting at all
                if (string.IsNullOrEmpty(myFittingToLoad))
                {
                    Log("No fitting to load.");
                    myAbyssalState = nextAbyssalState;
                    return true;
                }

                if (ESCache.Instance.FittingManagerWindow == null)
                {
                    Log("if (ESCache.Instance.FittingManagerWindow == null)");
                    return false;
                }

                Log("Looking for saved fitting named: [" + myFittingToLoad + "]");

                if (ESCache.Instance.FittingManagerWindow.Fittings.All(i => i.Name.ToLower() != myFittingToLoad.ToLower()))
                {
                    Log("Fitting named: [" + myFittingToLoad + " ] does not exist in game. Add it.");
                    myAbyssalState = nextAbyssalState;
                    return true;
                }

                foreach (DirectFitting fitting in ESCache.Instance.FittingManagerWindow.Fittings)
                {
                    //ok found it
                    if (DirectEve.Interval(11000, 12000, fitting.Name) && myFittingToLoad.ToLower().Equals(fitting.Name.ToLower()) && fitting.ShipTypeId == ESCache.Instance.ActiveShip.TypeId)
                    {
                        Log("Found saved fitting named: [ " + fitting.Name + " ] ShipTypeID [" + fitting.ShipTypeId + "]");
                        if (DebugConfig.DebugFittingMgr)
                        {
                            Log("Modules found in this fitting:");
                            foreach (var individualModule in fitting.Modules)
                            {
                                Log("Module in fitting [" + individualModule.TypeName + "]");
                            }
                        }

                        //switch to the requested fitting for the current mission
                        if (DirectEve.Interval(6000))
                        {
                            if (!FittingDone)
                            {
                                if (fitting.Fit())
                                {
                                    Log("Changing fitting to [" + fitting.Name + "] and waiting 5 seconds for the eve client to load the fitting (and move ammo or drones as needed)");
                                    FittingDone = true;
                                    return false;
                                }

                                Log("if (!fitting.Fit())");
                                return false;
                            }

                            myAbyssalState = nextAbyssalState;
                            return true;
                        }

                        return false;
                    }
                }

                //if we did not find it, we'll set currentfit to default
                //this should provide backwards compatibility without trying to fit always

                Log("!if (Settings.Instance.UseFittingManager)...");
                myAbyssalState = nextAbyssalState;
                return true;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return false;
            }
        }

        internal void HandlePVP()
        {
            // Set the PVP state if players are attacking us
            if (Framework.Session.IsInSpace && IsAnyPlayerAttacking)
            {
                myAbyssalState = AbyssalState.PVP;
            }

            // Handle responsive mode while being engaged in PVP
            if (true)
            {
                if (myAbyssalState == AbyssalState.PVP && ControllerManager.Instance.ResponsiveMode == false)
                {
                    ControllerManager.Instance.ResponsiveMode = true;
                    Log($"Set ControllerManager ResponsiveMode to TRUE.");
                }

                if (myAbyssalState != AbyssalState.PVP && ControllerManager.Instance.ResponsiveMode == true)
                {
                    ControllerManager.Instance.ResponsiveMode = false;
                    Log($"Set ControllerManager ResponsiveMode to FALSE.");
                }
            }

            if (ESCache.Instance.DirectEve.Me.IsInvuln && AreWeAtTheFilamentSpot && myAbyssalState != AbyssalState.InvulnPhaseAfterAbyssExit && myAbyssalState != AbyssalState.PVP && _leftInvulnAfterAbyssState == false)
            {
                if (DirectEve.Interval(1000))
                {
                    Log($"We are at the abyss filament spot and we are invulnerable, changing state to [{nameof(AbyssalState.InvulnPhaseAfterAbyssExit)}]");
                }

                myAbyssalState = AbyssalState.InvulnPhaseAfterAbyssExit;
                return;
            }
        }

        internal void AbyssalEveryPulse()
        {
            if (!DirectEve.Interval(500))
            {
                return;
            }

            if (DroneDebugState && Framework.Session.IsInSpace && !Framework.Me.IsInAbyssalSpace())
            {
                var dronesIWant = GetWantedDronesInSpace();

                Log($"-- DronesIWant --");
                foreach (var drone in dronesIWant)
                {
                    Log($"TypeName {drone.TypeName}");
                }
                Log($"-- DronesIWant -- End");

                if (DronesInSpaceAreHealthyEnoughDoNotRiskPullingDrones())
                    return;

                if (LaunchDrones(dronesIWant))
                    return;

                if (ReturnDrones(dronesIWant))
                    return;

                return;
            }

            // Ensure DPS values are populated within the game (mutated drones). If we don't check before they are launched, they have no dps value while in space. So we need to check while they are still in bay.
            if (!_droneDPSUpdate)
            {
                if (!allDronesInSpace.Any())
                {
                    var droneBay = Framework.GetShipsDroneBay();
                    if (droneBay != null)
                    {
                        if (!droneBay.Items.Any())
                            _droneDPSUpdate = true;

                        foreach (var d in Framework.GetShipsDroneBay()?.Items)
                        {
                            var k = d.GetDroneDPS();
                        }
                        _droneDPSUpdate = true;
                    }
                }
            }

            HandlePVP();


            HandleNotifications();

            //ManageModules();
            //return;

            //Log("1");
            DoOnceOnStartup();

            if (DirectEve.Interval(4000, 5000))
            {
                // update current abyss stage to be able to recover from a crash/disconnect
                if (ESCache.Instance.EveAccount.AbyssStage != (int)CurrentAbyssalStage && (int)CurrentAbyssalStage != null)
                    WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.AbyssStage), (int)CurrentAbyssalStage);
            }

            //Log("2");

            if (DirectEve.Me.IsInAbyssalSpace())
                myAbyssalState = AbyssalState.AbyssalClear;

            //Log("3");
            if (!DirectEve.Session.IsAbyssalDeadspace && (DirectEve.Session.IsWspace || DirectEve.Session.IsKSpace))
            {
                if (DirectEve.Interval(10000) && _stage1DetectSpawn != string.Empty)
                {
                    Log("Clearing some variables between abyssal runs");
                    _stage1SecondsSpent = 0;
                    _stage2SecondsSpent = 0;
                    _stage3SecondsSpent = 0;
                    _stage1DetectSpawn = string.Empty;
                    _stage2DetectSpawn = string.Empty;
                    _stage3DetectSpawn = string.Empty;
                    stage1TimeLastNPCWasKilled = DateTime.UtcNow.AddHours(-1);
                    stage2TimeLastNPCWasKilled = DateTime.UtcNow.AddHours(-1);
                    stage3TimeLastNPCWasKilled = DateTime.UtcNow.AddHours(-1);


                    __stage1SecondsWastedAfterLastNPCWasKilled = null;
                    __stage2SecondsWastedAfterLastNPCWasKilled = null;
                    __stage3SecondsWastedAfterLastNPCWasKilled = null;
                }
            }

            if (DirectEve.Session.IsInSpace)
            {
                // update in space state
            }

            if (DirectEve.ActiveShip != null && DirectEve.ActiveShip.IsPod)
            {

                if (ESCache.Instance.DirectEve.Me.IsInAbyssalSpace() && IsAbyssGateOpen)
                {
                    // move to the gate and jump
                    if (DirectEntity.MoveToViaAStar(4000, distanceToTarget: 10000, forceRecreatePath: forceRecreatePath, dest: _nextGate._directEntity.DirectAbsolutePosition,
                        ignoreAbyssEntities: true,
                        ignoreTrackingPolyons: ESCache.Instance.IgnoreTrackingPolyons_ImprovesTracking_HelpsToHitSmallThings,
                        ignoreAutomataPylon: ESCache.Instance.IgnoreAutomataPylon_Kills_NPCDrones_OurDrones_And_Missiles,
                        ignoreWideAreaAutomataPylon: true,
                        ignoreBioClouds: ESCache.Instance.IgnoreBioClouds_Blue_4xSignatureRadius,
                        ignoreFilaCouds: ESCache.Instance.IgnoreFilaClouds_Orange_ShieldBoostCapacitorUsePenalty,
                        ignoreTachClouds: ESCache.Instance.IgnoreTachClouds_White_4xSpeedBoost))
                    {
                        if (DirectEve.Interval(1500, 2500))
                        {
                            if (4000 > _nextGate.Distance)
                            {
                                if (_nextGate.Distance > (int)Distances.JumpRange)
                                {
                                    if (DirectEve.Interval(5000, 7000)) Log("We are in a capsule and the abyss gate is open, moving to the gate and trying to jump");
                                    _nextGate._directEntity.MoveTo();
                                    return;
                                }

                                _nextGate.ActivateAccelerationGate();
                                return;
                            }
                        }
                    }

                    return;
                }

                if (DirectEve.Me.IsInAbyssalSpace()) // can't to jackshit there while in a capsule ---> TODO: We can move, maybe we can safe our pod? (very rare occurrence tho, except single room abysses)
                    return;

                if (DirectEve.Interval(60000) && _abyssStatEntry != null) // write the stats after we got kicked out of the abyss
                {
                    Log($"Yaaay. Congratulations! We are in a capsule.");
                    Log($"Writing stats entry. :(");
                    _abyssStatEntry.Died = true;
                    WriteStatsToDB();
                    _abyssStatEntry = null;
                }

                if (ESCache.Instance.DirectEve.Session.IsInSpace && myAbyssalState != AbyssalState.TravelToHomeLocation) // if we somehow managed to escape with a pod, let's safe it
                {
                    // TODO: What do we do about the aggression timer (i.e can we dock directly after someone popped our ship?) Edit: We can't dock 10 seconds after we got popped (session change timer, nothing else)
                    // TODO: This sucks, the traveler is very slow, we need to warp to a celestial before
                    Log($"Trying to save our pod. Going to the home station bookmark.");
                    myAbyssalState = AbyssalState.TravelToHomeLocation;
                    Traveler.Destination = null;
                    State.CurrentTravelerState = TravelerState.Idle;
                }

                try
                {
                    if (ESCache.Instance.DirectEve.Session.IsInDockableLocation)
                    {
                        Log("We are docked and in a capsule, disabling this instance.");
                        ControllerManager.Instance.SetPause(true);
                        ESCache.Instance.DisableThisInstance();

                        if (ESCache.Instance.ActiveShip != null)
                        {
                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.ShipType), ESCache.Instance.ActiveShip.TypeName);
                        }

                        if (ESCache.Instance.DirectEve.Session != null && !string.IsNullOrEmpty(ESCache.Instance.DirectEve.Session.SolarSystemName))
                        {
                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.SolarSystem), ESCache.Instance.DirectEve.Session.SolarSystemName);
                        }

                        return;
                    }
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                }
            }

            //If We cant overheat we will want to shoot things, never return false here;
            ManageOverheat();

            //If We cant turn on/off modules we still want to shoot things, manage drones, etc, never return false here;
            ManageModules();

            //If We cant take boosters we still want to shoot things, manage drones, etc, never return false here;
            ManageDrugs();

            return;
        }

        internal void AbyssalError()
        {
            if (DirectEve.Interval(30000))
            {
                string msg = "Notification: Abyss error state.";
                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                Log(msg);

                if (PlayNotificationSounds) Util.PlayNoticeSound();
                if (PlayNotificationSounds) Util.PlayNoticeSound();
            }

            if (DirectEve.Interval(480000))
            {
                try
                {
                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.ERROR, $"Abyssal error state. Current ship typename: [{DirectEve.ActiveShip.TypeName}]"));
                }
                catch { }
            }

            if (ESCache.Instance.InSpace)
            {
                if (State.CurrentTravelerState != TravelerState.AtDestination)
                {
                    Traveler.TravelToBookmark(ESCache.Instance.CachedBookmarks.FirstOrDefault(b => b.Title.ToLower() == _homeStationBookmarkName.ToLower()));
                }
                else if (State.CurrentTravelerState == TravelerState.AtDestination)
                {
                    Traveler.Destination = null;
                    State.CurrentTravelerState = TravelerState.Idle;
                    if (DebugConfig.DebugTraveler) Log($"TravelerState [ AtDestination ]");
                    myAbyssalState = AbyssalState.PrepareToArm;
                }
            }

            return;
        }

        internal bool CheckFilamentsInCargo()
        {
            var shipsCargoItems = DirectEve.GetShipsCargo();

            if (DirectEve.ActiveShip.IsFrigate || DirectEve.ActiveShip.IsDestroyer)
            {
                if (!shipsCargoItems.Items.Any(e => e.TypeId == _filamentTypeId))
                {
                    Log($"Not enough filaments in cargo. Going to re-arm.");
                    return false;
                }

                IOrderedEnumerable<DirectItem> filamentsInCargo = shipsCargoItems.Items.Where(e => e.TypeId == _filamentTypeId).OrderByDescending(e => e.Stacksize);
                if (filamentsInCargo.Any())
                {
                    DirectItem filamentStack = filamentsInCargo.FirstOrDefault();
                    if (filamentStack == null)
                    {
                        Log($"Not enough filaments in cargo: if (filamentStack == null): Going to re-arm.");
                    }

                    if (filamentStack.Stacksize < _filaStackSize)
                    {
                        Log($"Not enough filaments in cargo: [" + filamentStack.Stacksize + "] < [" + _filaStackSize + "]. Going to re-arm.");
                        return false;
                    }
                }
            }

            return true;
        }

        internal int FleetIdealCount
        {
            get
            {
                int _fleetIdealCount = 0;
                if (Settings.Instance.AbyssalFleetMemberName1 != string.Empty)
                    _fleetIdealCount++;

                if (Settings.Instance.AbyssalFleetMemberName2 != string.Empty)
                    _fleetIdealCount++;

                if (Settings.Instance.AbyssalFleetMemberName3 != string.Empty)
                    _fleetIdealCount++;

                return _fleetIdealCount;
            }
        }

        internal bool AreFleetMembersInFleetAndLocal
        {
            get
            {
                try
                {
                    if (ESCache.Instance.EveAccount.UseFleetMgr)
                    {
                        if (DebugConfig.DebugFleetMgr) Log("UseFleetMgr [" + ESCache.Instance.EveAccount.UseFleetMgr + "]");

                        if (!IsFleetReadyForAbyssals) return false;

                        //Are all the characters in the appropriate chat channel in fleet already?


                        if (FleetIdealCount > DirectEve.FleetMembers.Count())
                        {
                            Log("Waiting for FleetMembers [" + DirectEve.FleetMembers.Count() + "] to at least be FleetIdealCount [" + FleetIdealCount + "]");
                            return false;
                        }

                        if (DebugConfig.DebugFleetMgr) Log("ChatChannelToPullFleetInvitesFrom is null or empty?!");

                        //Are fleet members all in local?
                        foreach (DirectFleetMember individualFleetMember in ESCache.Instance.DirectEve.FleetMembers.Where(i => i.Name != ESCache.Instance.DirectEve.Me.Name))
                        {
                            if (DebugConfig.DebugFleetMgr) Log("foreach (DirectFleetMember individualFleetMember in ESCache.Instance.DirectEve.FleetMembers.Where(i => i.Name != ESCache.Instance.DirectEve.Me.Name))");

                            if (individualFleetMember.Character == null)
                            {
                                Log("[" + individualFleetMember.Name + "] if (individualFleetMember.Character == null)");
                                return false;
                            }

                            if (!individualFleetMember.Character.IsInLocalWithMe)
                            {
                                if (DebugConfig.DebugFleetMgr) Log("individualCharacter [" + individualFleetMember.Name + "] IsInLocalWithMe [" + individualFleetMember.Character.IsInLocalWithMe + "]!.!");
                                if (DirectEve.Interval(20000)) Log("Waiting for [" + individualFleetMember.Name + "] to get into local before proceeding...");
                                return false;
                            }
                        }

                        if (//ESCache.Instance.EveAccount.AbyssalType == "Frigate" &&
                            3 > ESCache.Instance.DirectEve.FleetMembers.Count())
                        {
                            Log("Waiting for more characters to join fleet: We have [" + ESCache.Instance.DirectEve.FleetMembers.Count() + "] and expect [ 3 ]");
                            return false;
                        }

                        return true;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        internal bool AreFleetMembersInFleetAndInStationWithMe
        {
            get
            {
                try
                {
                    if (ESCache.Instance.EveAccount.UseFleetMgr)
                    {
                        if (DebugConfig.DebugFleetMgr) Log("UseFleetMgr [" + ESCache.Instance.EveAccount.UseFleetMgr + "]");

                        if (!IsFleetReadyForAbyssals) return false;

                        if (ESCache.Instance.InSpace)
                            return false;

                        if (!ESCache.Instance.InStation)
                            return false;

                        if (FleetIdealCount > DirectEve.FleetMembers.Count())
                        {
                            Log("Waiting for FleetMembers [" + DirectEve.FleetMembers.Count() + "] to at least be [" + FleetIdealCount + "]");
                            return false;
                        }

                        if (DebugConfig.DebugFleetMgr) Log("ChatChannelToPullFleetInvitesFrom is null or empty?!");

                        //Are fleet members all in local?
                        foreach (DirectFleetMember individualFleetMember in ESCache.Instance.DirectEve.FleetMembers.Where(i => i.Name != ESCache.Instance.DirectEve.Me.Name))
                        {
                            if (DebugConfig.DebugFleetMgr) Log("foreach (DirectFleetMember individualFleetMember in ESCache.Instance.DirectEve.FleetMembers.Where(i => i.Name != ESCache.Instance.DirectEve.Me.Name))");

                            if (individualFleetMember.Character == null)
                            {
                                Log("[" + individualFleetMember.Name + "] if (individualFleetMember.Character == null)");
                                return false;
                            }

                            if (!individualFleetMember.Character.IsInStationWithMe)
                            {
                                if (DebugConfig.DebugFleetMgr) Log("individualCharacter [" + individualFleetMember.Name + "] IsInStationWithMe [" + individualFleetMember.Character.IsInStationWithMe + "]!.!");
                                if (DirectEve.Interval(10000)) Log("Waiting for [" + individualFleetMember.Name + "] to get into station before proceeding...");
                                return false;
                            }
                        }

                        return true;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        internal bool IsFleetReadyForAbyssals
        {
            get
            {
                if (ESCache.Instance.EveAccount.UseFleetMgr)
                {
                    if (ESCache.Instance.ActiveShip.GivenName == Combat.CombatShipName)
                    {
                        if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip)
                        {
                            if (3 > ESCache.Instance.DirectEve.FleetMembers.Count())
                            {
                                Log("Waiting for more characters to join fleet: We have [" + ESCache.Instance.DirectEve.FleetMembers.Count() + "] and expect [ 3 ]");
                                return false;
                            }

                            return true;
                        }

                        if (ESCache.Instance.ActiveShip.IsDestroyer) //what other ships can do this size abyssal?
                        {
                            if (2 > ESCache.Instance.DirectEve.FleetMembers.Count())
                            {
                                Log("Waiting for more characters to join fleet: We have [" + ESCache.Instance.DirectEve.FleetMembers.Count() + "] and expect [ 2 ]");
                                return false;
                            }

                            return true;
                        }

                        Log("IsFleetReadyForAbyssals: We are in a [" + ESCache.Instance.ActiveShip.TypeName + "][" + ESCache.Instance.ActiveShip.TypeId + "] Unexpected ship type! return false");
                        return false;
                    }

                    Log("IsFleetReadyForAbyssals: return false");
                    return false;
                }

                return true;
            }
        }
        internal bool AreFleetMembersReady
        {
            get
            {
                if (ESCache.Instance.EveAccount.UseFleetMgr)
                {
                    if (ESCache.Instance.EveAccount.IsLeader)
                    {
                        if (!AreFleetMembersInFleetAndLocal) return false; //wait for fleet members to get into local!
                        //if (!AreFleetMembersInFleetAndInStationWithMe) return false; //wait for fleet members to get in station!

                        return true;
                    }

                    if (!AreFleetMembersInFleetAndLocal)
                    {
                        if (DirectEve.Interval(10000)) Log("AreFleetMembersInFleetAndLocal [false]");
                        return false; //wait for fleet members to get into local!
                    }

                    if (DirectEve.Interval(10000)) Log("AreFleetMembersInFleetAndLocal [true]");
                    if (!AreFleetMembersInFleetAndInStationWithMe)
                    {
                        Log("AreFleetMembersInFleetAndInStationWithMe [false]: If leader has undocked: undock its time to go!");
                        return true; //If leader has undocked: undock its time to go!
                    }

                    Log("waiting for leader to undock");
                    return false;
                }

                return true;
            }
        }

        internal void AbyssalStart()
        {
            if (DirectEve.Interval(10000, 25000)) Log("ActiveShip [" + ESCache.Instance.DirectEve.ActiveShip.TypeName + "] TypeID [" + ESCache.Instance.DirectEve.ActiveShip.TypeId + "]");

            DirectBookmark fbmx = null;

            if (ESCache.Instance.CurrentShipsCargo == null)
            {
                Log("ActiveShip [" + ESCache.Instance.DirectEve.ActiveShip.TypeName + "] if (ESCache.Instance.CurrentShipsCargo == null)");
                return;
            }

            if (ListOfFilamentBookmarks.Any())
            {
                if (ESCache.Instance.EveAccount.UseFleetMgr)
                    fbmx = ListOfFilamentBookmarks.FirstOrDefault();
                else
                    fbmx = ListOfFilamentBookmarks.OrderBy(i => Guid.NewGuid()).FirstOrDefault();
            }

            if (fbmx == null)
            {
                Log("Missing filament spot bookmark: no bookmark found is <AbyssalDeadspaceBookmarks>abyss<AbyssalDeadspaceBookmarks> defined in your settings? Do you have bookmarks with that in the name?");
                return;
            }

            if (ESCache.Instance.Modules.Any(i => !i.IsOnline))
            {
                Log($"Offline module found: going home");
                myAbyssalState = AbyssalState.TravelToHomeLocation;
                return;
            }

            if (_shipsCargoBayList.Any(x => x.Item1 == _naniteRepairPasteTypeId) && !ESCache.Instance.CurrentShipsCargo.Items.Any(i => i.TypeId == _naniteRepairPasteTypeId && i.Stacksize > 30))
            {
                Log($"Missing enough Nanite Repair Paste: going home");
                myAbyssalState = AbyssalState.TravelToHomeLocation;
                return;
            }

            if (ESCache.Instance.InSpace)
            {
                if (fbmx == null)
                {
                    Log($"Filamentspot bookmark is null. Error.");
                    myAbyssalState = AbyssalState.Error;
                    return;
                }

                //
                // Reasons to not go into another Abyssal
                //
                bool? ShouldWeGoHomeResult = ShouldWeGoHome;
                if (ShouldWeGoHomeResult == null)
                {
                    Log("ShouldWeGoHome [null]");
                    return;
                }

                if (ShouldWeGoHomeResult.Value)
                {
                    Log("ShouldWeGoHome [true]");
                    return;
                }

                if (ListOfFilamentBookmarks.Any(x => x.DistanceTo(ESCache.Instance.DirectEve.ActiveShip.Entity) <= 15000))
                {
                    if (ESCache.Instance.CurrentShipsCargo.UsedCapacity == null)
                    {
                        Log($"if (ESCache.Instance.CurrentShipsCargo.UsedCapacity == null)");
                        return;
                    }

                    // check cargo space
                    Log($"CargoCapacity: {ESCache.Instance.CurrentShipsCargo.Capacity} UsedCapacity {ESCache.Instance.CurrentShipsCargo.UsedCapacity}");

                    if (ESCache.Instance.CurrentShipsCargo.Capacity - ESCache.Instance.CurrentShipsCargo.UsedCapacity >= ESCache.Instance.CurrentShipsCargo.Capacity * 0.25)
                    {
                        var shipBayItemCheck = true;
                        foreach (var t in _shipsCargoBayList)
                        {

                            if (t.Item1 == _filamentTypeId)
                            {
                                if (ESCache.Instance.EveAccount.UseFleetMgr)
                                {
                                    if (ESCache.Instance.EveAccount.IsLeader)
                                    {
                                        shipBayItemCheck = CheckFilamentsInCargo();
                                    }
                                }
                                else shipBayItemCheck = CheckFilamentsInCargo();

                            }

                            foreach (var individualDefinedAmmoType in DirectUIModule.DefinedAmmoTypes.Distinct())
                            {
                                if (t.Item1 == individualDefinedAmmoType.TypeId)
                                {
                                    if (!ESCache.Instance.CurrentShipsCargo.Items.Any(e => e.TypeId == individualDefinedAmmoType.TypeId) || ESCache.Instance.CurrentShipsCargo.Items.Where(e => e.TypeId == individualDefinedAmmoType.TypeId).Sum(e => e.Stacksize) < t.Item2 * 0.2d)
                                    {
                                        Log($"Not enough ammo left in the cargo. We need {t.Item2 * 0.2d} [{DirectEve.GetInvType(t.Item1).TypeName}]");
                                        shipBayItemCheck = false;
                                    }
                                }
                            }

                            if (!ESCache.Instance.CurrentShipsCargo.Items.Any(e => e.TypeId == t.Item1))
                            {
                                shipBayItemCheck = false;
                                Log($"ShipBayItemCheck missing the following item [{t.Item1}] TypeName [{ESCache.Instance.DirectEve.GetInvType(t.Item1).TypeName}], going back to the base");
                                break;
                            }
                        }

                        if (shipBayItemCheck)
                        {
                            if (ESCache.Instance.ActiveShip.HasDroneBay)
                            {
                                var db = DirectEve.GetShipsDroneBay();
                                if (db != null && db.UsedCapacity == null)
                                {
                                    Log("if (db != null && db.UsedCapacity == null)");
                                    return;
                                }

                                var remainingCap = db.Capacity - db.UsedCapacity;

                                if (db.Capacity == 0d || remainingCap <= db.Capacity * 0.1d)
                                {
                                    Log($"We are ready to go into another abyssal");
                                    Traveler.Destination = null;
                                    State.CurrentTravelerState = TravelerState.Idle;
                                    myAbyssalState = AbyssalState.TravelToFilamentSpot;
                                    return;
                                }

                                Log($"Do we have enough drones [false]");
                                myAbyssalState = AbyssalState.TravelToHomeLocation;
                                return;
                            }

                            Log($"We are ready to go into another abyssal");
                            Traveler.Destination = null;
                            State.CurrentTravelerState = TravelerState.Idle;
                            myAbyssalState = AbyssalState.TravelToFilamentSpot;
                        }
                    }
                    else
                    {
                        Log($"There is not enough cargo space left, going back to the base.");
                    }
                }
            }

            if (ESCache.Instance.InSpace)
            {
                myAbyssalState = AbyssalState.TravelToHomeLocation;
                Traveler.Destination = null;
                State.CurrentTravelerState = TravelerState.Idle;
            }
            else
                myAbyssalState = AbyssalState.PrepareToArm;

            return;
        }

        internal void AbyssalPrepareToArm()
        {
            try
            {
                if (!DirectEve.Interval(2000))
                    return;

                var homeBm = ESCache.Instance.CachedBookmarks.OrderByDescending(i => i.IsInCurrentSystem).FirstOrDefault(b => b.Title.ToLower() == _homeStationBookmarkName.ToLower());
                if (homeBm == null)
                {
                    Log($"Home bookmark name not found. [" + _homeStationBookmarkName + "] Error.");
                    myAbyssalState = AbyssalState.Error;
                    return;
                }

                long StationIDJitaP4M4 = 60003760;
                if (ESCache.Instance.InStation && ESCache.Instance.DirectEve.Session.LocationId == StationIDJitaP4M4)
                {
                    if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Capsule)
                    {
                        Log($"Looking for Ship [{Combat.CombatShipName}]");

                        var PossibleCombatShips = ESCache.Instance.DirectEve.GetShipHangar().Items.Where(i => i.IsSingleton
                                                         && i.GivenName != null
                                                         && i.GivenName == Combat.CombatShipName);
                        if (PossibleCombatShips.Count() > 1)
                        {
                            var FirstCombatShip = PossibleCombatShips.FirstOrDefault();
                            if (PossibleCombatShips.All(i => i.TypeId != FirstCombatShip.TypeId))
                            {
                                Log("We have more than one CombatShip an they are not the same type of ship. Error!");
                                return;
                            }
                        }

                        var ship = ESCache.Instance.ShipHangar.Items.FirstOrDefault(e => e.IsSingleton && e.GivenName.ToLower() == Combat.CombatShipName.ToLower());
                        if (ship != null)
                        {
                            ship.ActivateShip();
                            Log($"Ship [{Combat.CombatShipName}][" + ship.TypeName + "] activated");
                            LocalPulse = UTCNowAddMilliseconds(1500, 3500);
                            return;
                        }

                        Log("No Ships found");
                    }
                }

                if (homeBm.LocationId != ESCache.Instance.DirectEve.Session.LocationId && homeBm.ItemId != ESCache.Instance.DirectEve.Session.LocationId)
                {
                    Log($"We are not in the home station, traveling to the home station");
                    myAbyssalState = AbyssalState.TravelToHomeLocation;
                    Traveler.Destination = null;
                    State.CurrentTravelerState = TravelerState.Idle;
                    return;
                }

                if (ESCache.Instance.InSpace || !ESCache.Instance.DirectEve.Session.IsInDockableLocation)
                {
                    Log($"Error: Not in dockable location during PrepareToArm.");
                    myAbyssalState = AbyssalState.TravelToHomeLocation;
                    Traveler.Destination = null;
                    State.CurrentTravelerState = TravelerState.Idle;
                    return;
                }

                // here we are in the home station

                // activate the correct ship if necessary
                if (ESCache.Instance.DirectEve.ActiveShip.GivenName != Combat.CombatShipName)
                {
                    if (ESCache.Instance.ShipHangar == null)
                    {
                        Log("Arm: if (ESCache.Instance.ShipHangar == null)");
                        return;
                    }

                    if (!ESCache.Instance.ShipHangar.Items.Any())
                    {
                        Log("Arm: No Ships in the Shiphangar? No Ships found.");
                        return;
                    }

                    if (!ESCache.Instance.DirectEve.Session.IsWspace && ESCache.Instance.CurrentShipsCargo == null && ESCache.Instance.CurrentShipsCargo.Items.Any())
                    {
                        //empty cargo here
                        if (DirectEve.Interval(5000))
                        {
                            Log("Moving [" + ESCache.Instance.CurrentShipsCargo.Items.Count() + "] Items from CargoHold to ItemHangar");
                            ESCache.Instance.ItemHangar.Add(ESCache.Instance.CurrentShipsCargo.Items);
                        }
                        return;
                    }

                    Log($"Looking for Ship [{Combat.CombatShipName}]");

                    var PossibleCombatShips = ESCache.Instance.DirectEve.GetShipHangar().Items.Where(i => i.IsSingleton
                                                     && i.GivenName != null
                                                     && i.GivenName == Combat.CombatShipName);
                    if (PossibleCombatShips.Count() > 1)
                    {
                        var FirstCombatShip = PossibleCombatShips.FirstOrDefault();
                        if (PossibleCombatShips.All(i => i.TypeId != FirstCombatShip.TypeId))
                        {
                            Log("We have more than one CombatShip an they are not the same type of ship. Error!");
                            return;
                        }
                    }

                    var ship = ESCache.Instance.ShipHangar.Items.FirstOrDefault(e => e.IsSingleton && e.GivenName.ToLower() == Combat.CombatShipName.ToLower());
                    if (ship != null)
                    {
                        ship.ActivateShip();
                        Log($"Ship [{Combat.CombatShipName}][" + ship.TypeName + "] activated");
                        LocalPulse = UTCNowAddMilliseconds(1500, 3500);
                        return;
                    }
                    else
                    {
                        Log($"ship named [" + Combat.CombatShipName + "] was not found in the ship hangar. Did we lose our Combat ship?");

                        if (ESCache.Instance.DirectEve.ActiveShip.GivenName != Settings.Instance.TransportShipName)
                        {
                            Log($"Activating [{Settings.Instance.TransportShipName}].");
                            var transportship = ESCache.Instance.DirectEve.GetShipHangar().Items.FirstOrDefault(e => e.IsSingleton && e.GivenName.ToLower() == Settings.Instance.TransportShipName.ToLower());
                            if (transportship != null)
                            {
                                transportship.ActivateShip();
                                Log($"transportship activated.");
                                LocalPulse = UTCNowAddMilliseconds(1500, 3500);
                                return;
                            }
                            else
                            {
                                Log($"transportship named [" + Settings.Instance.TransportShipName + "] was not found in the ship hangar.");
                                myAbyssalState = AbyssalState.Error;
                                return;
                            }
                        }

                        return;
                    }
                }

                var hangar = ESCache.Instance.DirectEve.GetItemHangar();
                var shipsCargo = ESCache.Instance.DirectEve.GetShipsCargo();
                var droneBay = ESCache.Instance.DirectEve.GetShipsDroneBay();

                if (hangar == null || shipsCargo == null || droneBay == null)
                    return;

                hangar?.StartLoadingAllDynamicItems();
                shipsCargo?.StartLoadingAllDynamicItems();
                droneBay?.StartLoadingAllDynamicItems();

                // Wait for all dynamic item attributes to be loaded in the background
                if (!DirectItem.AllDynamicItemsLoaded)
                {
                    return;
                }

                bool? NeedToDumpDatabaseSurveysResult = NeedToDumpDatabaseSurveys();
                if (NeedToDumpDatabaseSurveysResult == null)
                {
                    Log("NeedToDumpDatabaseSurveys [null]");
                    return;
                }

                if (NeedToDumpDatabaseSurveysResult.Value)
                {
                    Log("NeedToDumpDatabaseSurveys [true]");
                    myAbyssalState = AbyssalState.DumpSurveyDatabases;
                    return;
                }

                try
                {
                    Log("NeedToDumpDatabaseSurveys [false]");

                    Log("bool? _DoWeNeedToBuyItems = DoWeNeedToBuyItems;");
                    bool? _DoWeNeedToBuyItems = DoWeNeedToBuyItems;
                    Log("_DoWeNeedToBuyItems [" + _DoWeNeedToBuyItems + "]");

                    if (_DoWeNeedToBuyItems == null)
                    {
                        Log("DoWeNeedToBuyItems [null]");
                        return;
                    }

                    Log("if (_DoWeNeedToBuyItems != null && (bool)_DoWeNeedToBuyItems)");
                    if (_DoWeNeedToBuyItems != null && (bool)_DoWeNeedToBuyItems)
                    {
                        Log("DoWeNeedToBuyItems [true]");
                        myAbyssalState = AbyssalState.BuyItems;
                        return;
                    }

                    Log("if (myAbyssalState == AbyssalState.DumpSurveyDatabases)");
                    if (myAbyssalState == AbyssalState.DumpSurveyDatabases)
                        return;

                    Log("DoWeNeedToBuyItems [false]");
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                }

                if (ESCache.Instance.EveAccount.UseScheduler)
                {
                    if (ESCache.Instance.EveAccount.ShouldBeStopped)
                    {
                        Log("ShouldBeStopped [true]");
                        ESCache.Instance.CloseEveReason = "if (ESCache.Instance.EveAccount.ShouldBeStopped)";
                        ESCache.Instance.BoolCloseEve = true;
                        return;
                    }
                }

                if (ShouldWeRestartEve)
                {
                    Log("ShouldWeRestartEve [true]");
                    ESCache.Instance.BoolRestartEve = true;
                    return;
                }

                Log("ShouldWeRestartEve [false]");

                if (ShouldWeGoIdle())
                {
                    Log("ShouldWeGoIdle [true]");
                    myAbyssalState = AbyssalState.IdleInStation;
                    return;
                }

                Log("ShouldWeGoIdle [false]");

                if (HaveWeHitOurSitesPerSessionLimit)
                {
                    Log("HaveWeHitOurSitesPerSessionLimit: [" + logoffEveAfterThisManyAbyssalRuns + "][true]");
                    ESCache.Instance.CloseEveReason = "HaveWeHitOurSitesPerSessionLimit: [" + logoffEveAfterThisManyAbyssalRuns + "]";
                    ESCache.Instance.BoolCloseEve = true;
                    return;
                }

                Log("HaveWeHitOurSitesPerSessionLimit [false]");

                if (IsLocalTooCrowded)
                {
                    Log("IsLocalTooCrowded [true]");
                    myAbyssalState = AbyssalState.IdleInStation;
                    return;
                }

                Log("ShouldWeGoIdle [false]");

                if (!ESCache.Instance.EveAccount.IgnoreSeralizationErrors)
                {
                    var seconds = 6 * 60 * 60;
                    if (!ESCache.Instance.DirectEve.Session.IsWspace && ESCache.Instance.DirectEve.Session != null && ESCache.Instance.DirectEve.Session.SolarSystem != null && !ESCache.Instance.DirectEve.Session.SolarSystem.IsZeroZeroSpace && ESCache.Instance.EveAccount.AbyssSecondsDaily > seconds)
                    {
                        Log($"Calm down miner. You've been running today for more than [{Math.Round(TimeSpan.FromSeconds(ESCache.Instance.EveAccount.AbyssSecondsDaily).TotalHours, 2)}] hours today.");
                        myAbyssalState = AbyssalState.Error;
                        return;
                    }
                }

                if (Time.Instance.IsItDuringDowntimeNow)
                {
                    string msg = "AbyssalController: Arm: Downtime is less than 25 minutes from now: Closing";
                    Log(msg);
                    ESCache.Instance.CloseEveReason = msg;
                    ESCache.Instance.BoolCloseEve = true;
                    return;
                }

                Log("IsItDuringDowntimeNow [false]");

                if (ESCache.Instance.EveAccount.UseFleetMgr)
                {
                    if (!AreFleetMembersReady)
                    {
                        myAbyssalState = AbyssalState.WaitingOnFleetMembers;
                        //Log("AreFleetMembersReady [ false ] - waiting"); we log this elsewhere no need for double logging
                        return;
                    }

                    Log("AreFleetMembersReady [ true ]");
                }

                FittingDone = false;
                Log("PrepareToArm: LoadSavedFitting");
                myAbyssalState = AbyssalState.LoadSavedFitting;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
        }

        public bool IsLocalTooCrowded
        {
            get
            {
                return false;

                //if (DebugConfig.IsLocalTooCrowdedNumOfToons == 0) return false;

                if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                    return true; //wait

                if (ESCache.Instance.InAbyssalDeadspace || ESCache.Instance.InWormHoleSpace)
                    return false;

                if (ESCache.Instance.DirectEve.Session == null)
                {
                    Log("if (ESCache.Instance.DirectEve.Session == null)");
                    return true; //wait
                }

                if (ESCache.Instance.DirectEve.Session.LocalChatChannel == null)
                {
                    Log("if (ESCache.Instance.DirectEve.Session.LocalChatChannel == null)");
                    return true; //wait
                }

                if (ESCache.Instance.DirectEve.Session.CharactersInLocal.Any())
                {
                    //if (ESCache.Instance.DirectEve.Session.CharactersInLocal.Count() > DebugConfig.IsLocalTooCrowdedNumOfToons)
                    //    return true; //wait!

                    return false;
                }

                return false;
            }
        }

        internal bool? CheckDroneBayForAllDronesNeeded()
        {
            var shipsCargo = ESCache.Instance.DirectEve.GetShipsCargo();
            if (shipsCargo == null)
                return null;

            var droneBay = ESCache.Instance.DirectEve.GetShipsDroneBay();

            if (!Drones.UseDrones)
            {
                Log("UseDrones [false]");
                return false;
            }

            if (droneBay == null)
                return null;

            DirectContainer itemHangar = null;

            if (ESCache.Instance.InStation)
            {
                itemHangar = ESCache.Instance.DirectEve.GetItemHangar();

                if (itemHangar == null)
                    return null;
            }

            int intCount = 0;
            // iterate over drones and check if they are enough in the dronebay

            if (_droneBayItemList != null && _droneBayItemList.Any())
            {
                foreach (var t in _droneBayItemList) // .RandomPermutation()) // change the order
                {
                    intCount++;
                    DirectItem tempItem = new DirectItem(ESCache.Instance.DirectEve);
                    tempItem.TypeId = t.Item1;
                    if (DebugConfig.DebugArm) Log("[" + intCount + "][" + tempItem.TypeName + "] TypeID [" + t.Item1 + "] Quantity [" + t.Item2 + "] Size [" + t.Item3 + "]");
                    var typeId = t.Item1;
                    var amount = t.Item2;

                    int missingInDroneBay = amount - (droneBay.Items.Where(d => d.TypeId == typeId && d.IsSingleton).Count() + droneBay.Items.Where(d => d.TypeId == typeId && !d.IsSingleton).Sum(d => d.Stacksize));
                    //override missingInDroneBay if all drones are in the dronebay and there is no free capacity left
                    if (droneBay.Items.All(d => d.TypeId == typeId) && droneBay.FreeCapacity == 0)
                    {
                        missingInDroneBay = 0;
                    }

                    int availableInHangar = 0;
                    if (ESCache.Instance.InStation)
                    {
                        availableInHangar = itemHangar.Items.Where(d => d.TypeId == typeId).Sum(d => d.Stacksize);
                    }

                    //Log($"TypeId [{typeId}] AvaiableInHangar [{avaiableInHangar}] missingInDroneBay [{missingInDroneBay}]");

                    if (ESCache.Instance.InStation)
                    {
                        if (missingInDroneBay > availableInHangar)
                        {
                            DirectItem missingItem = new DirectItem(ESCache.Instance.DirectEve);
                            missingItem.TypeId = typeId;
                            // ... not enough available
                            bool? _DoWeNeedToBuyItems = DoWeNeedToBuyItems;

                            if (_DoWeNeedToBuyItems == null)
                            {
                                if (DirectEve.Interval(60000)) Log("DoWeNeedToBuyItems [null]");
                                return false;
                            }

                            if (_DoWeNeedToBuyItems != null && (bool)_DoWeNeedToBuyItems)
                            {
                                if (DirectEve.Interval(60000)) Log("DoWeNeedToBuyItems [true]");
                                myAbyssalState = AbyssalState.BuyItems;
                                return false;
                            }

                            if (myAbyssalState == AbyssalState.DumpSurveyDatabases)
                                return false;

                            Log("DoWeNeedToBuyItems [false] - but we are out of drones?!");
                            Log($"Error: OutOfDrones: Not enough drones left available. [{typeId}][{DirectEve.GetInvType(typeId)?.TypeName}] IsDynamic {t.Item4} AvaiableInHangar [{availableInHangar}] missingInDroneBay [{missingInDroneBay}]");

                            Log($"Items viewable in in hanger with a categoryID of drones:");
                            Log($"Wanting: TypeId: {typeId}, Amount: {amount}, Size: {t.Item3}, Dynamic: {t.Item4}");
                            foreach (var i in itemHangar.Items.Where(i => i.CategoryId == (int)CategoryID.Drone))
                            {
                                Log($"TypeId: {i.TypeId}, DynamicItem {i.IsDynamicItem}:{i.OrignalDynamicItem?.TypeId} StackSize: {i.Stacksize} ");
                            }

                            myAbyssalState = AbyssalState.OutOfDrones;
                            return false;
                        }
                    }

                    if (missingInDroneBay > 0)
                    {
                        if (ESCache.Instance.InStation)
                        {
                            // move constructed item if it exists (non-repackaged)
                            DirectItem droneItemToAdd = itemHangar.Items.Where(d => d.TypeId == typeId && d.IsSingleton).FirstOrDefault();
                            if (droneItemToAdd == null)
                            {
                                //if no already constructed items exist, grab stacks smallest first
                                droneItemToAdd = itemHangar.Items.Where(d => d.TypeId == typeId).OrderBy(d => d.Stacksize).FirstOrDefault();
                            }

                            if (droneItemToAdd != null)
                            {
                                if (droneBay.FreeCapacity >= (droneItemToAdd.Volume * Math.Min(droneItemToAdd.Stacksize, missingInDroneBay)))
                                {
                                    if (droneBay.Add(droneItemToAdd, Math.Min(droneItemToAdd.Stacksize, missingInDroneBay)))
                                    {
                                        Log("[" + droneItemToAdd.TypeName + "] TypeID [" + droneItemToAdd.TypeId + "] Quantity [" + droneItemToAdd.Quantity + "] added to DroneBay");
                                    }
                                }
                                else Log("DroneBay has [" + droneBay.FreeCapacity + "] m3 left and [" + droneItemToAdd.TypeName + "] is [" + droneItemToAdd.TotalVolume + "]m3 - how are we out of room!?");
                            }

                            LocalPulse = UTCNowAddMilliseconds(500, 1500);
                            return false;
                        }
                        else if (ESCache.Instance.InSpace)
                        {
                            int droneBayItemCount = 0;
                            if (droneBay != null && droneBay.Items.Any())
                            {
                                droneBayItemCount = droneBay.Items.Count();
                            }

                            Log("missingInDroneBay [" + missingInDroneBay + "] of typeID [" + typeId + "]: We did find [" + droneBayItemCount + "] other drones in the droneBay");
                            if (droneBay != null && droneBay.Items.Any())
                            {
                                if (DebugConfig.DebugArm)
                                {
                                    int intDroneBayList = 0;
                                    Log("_droneBayItemList to Load [" + _droneBayItemList.Count() + "]");
                                    foreach (var droneBayItem in _droneBayItemList.OrderBy(i => i.Item1))
                                    {
                                        intDroneBayList++;
                                        Log("[" + intDroneBayList + "] TypeID [" + droneBayItem.Item1 + "] Quantity[" + droneBayItem.Item2 + "]");
                                    }

                                    int intDroneBayItems = 0;
                                    Log("DroneBay Items Found [" + droneBay.Items.Count() + "]");
                                    foreach (var droneBayItem in droneBay.Items.OrderBy(i => i.TypeId))
                                    {
                                        intDroneBayItems++;
                                        Log("[" + intDroneBayItems + "][" + droneBayItem.TypeName + "] TypeID [" + droneBayItem.TypeId + "] Quantity [" + Math.Abs(droneBayItem.Quantity) + "]");
                                    }
                                }
                            }

                            myAbyssalState = AbyssalState.TravelToHomeLocation;
                            Traveler.Destination = null;
                            State.CurrentTravelerState = TravelerState.Idle;
                            return false;
                        }

                        return false;
                    }

                    if (droneBay != null && droneBay.Items.Any())
                    {
                        if (DebugConfig.DebugArm)
                        {
                            int intDroneBayList = 0;
                            Log("_droneBayItemList to Load [" + _droneBayItemList.Count() + "]");
                            foreach (var droneBayItem in _droneBayItemList.OrderBy(i => i.Item1))
                            {
                                intDroneBayList++;
                                Log("[" + intDroneBayList + "] TypeID [" + droneBayItem.Item1 + "] Quantity[" + droneBayItem.Item2 + "]");
                            }

                            int intDroneBayItems = 0;
                            Log("DroneBay Items Found [" + droneBay.Items.Count() + "]");
                            foreach (var droneBayItem in droneBay.Items.OrderBy(i => i.TypeId))
                            {
                                intDroneBayItems++;
                                Log("[" + intDroneBayItems + "][" + droneBayItem.TypeName + "] TypeID [" + droneBayItem.TypeId + "] Quantity [" + Math.Abs(droneBayItem.Quantity) + "]");
                            }
                        }
                    }

                    continue;
                }
            }

            return true;
        }

        internal bool CheckCargoHoldForAllSuppliesNeeded()
        {
            var shipsCargo = ESCache.Instance.DirectEve.GetShipsCargo();
            if (shipsCargo == null)
                return false;

            DirectContainer itemHangar = null;

            if (ESCache.Instance.InStation)
            {
                itemHangar = ESCache.Instance.DirectEve.GetItemHangar();

                if (itemHangar == null)
                    return false;
            }

            // iterate over cargobay item list
            foreach (var t in _shipsCargoBayList.RandomPermutation()) // change the order
            {
                var typeId = t.Item1;
                var amount = t.Item2;

                DirectItem potentialMissingItem = new DirectItem(ESCache.Instance.DirectEve);
                potentialMissingItem.TypeId = typeId;
                if (potentialMissingItem.CategoryId == (int)CategoryID.Charge)
                {
                    if (ESCache.Instance.InSpace)
                    {
                        //account for reloading guns after undocking
                        amount = amount - 300;
                    }
                }

                if (potentialMissingItem.GroupId == (int)Group.AbyssalDeadspaceFilament)
                {
                    if (ESCache.Instance.InSpace)
                    {
                        //account for reloading guns after undocking
                        amount = 1;
                        if (ESCache.Instance.ActiveShip.IsDestroyer)
                            amount = 2;
                        if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip)
                            amount = 3;
                    }
                }

                DirectItem ItemToMove = new DirectItem(ESCache.Instance.DirectEve);
                ItemToMove.TypeId = typeId;
                //if (ItemToMove.TypeName.ToLower().Contains("filament") && ESCache.Instance.EveAccount.UseFleetMgr && !ESCache.Instance.EveAccount.IsLeader && DirectEve.FleetMembers.Count > 1)
                //{
                //    if (DirectEve.Interval(20000)) Log("We are in a fleet and are not the leader: we dont need any filaments: ignoring [" + ItemToMove.TypeName + "]: continuing");
                //    continue;
                //}

                int availableInCargo = shipsCargo.Items.Where(d => d.TypeId == typeId).Sum(d => d.Stacksize);
                int missingInShipsCargo = amount - availableInCargo;
                int availableInHangar = 0;
                if (ESCache.Instance.InStation)
                {
                    availableInHangar = itemHangar.Items.Where(d => d.TypeId == typeId).Sum(d => d.Stacksize);
                }

                //Log($"TypeId [{typeId}] AvaiableInHangar [{avaiableInHangar}] missingInShipsCargo [{missingInShipsCargo}]");

                if (ESCache.Instance.InStation)
                {
                    if (missingInShipsCargo > availableInHangar)
                    {
                        DirectItem missingItem = new DirectItem(ESCache.Instance.DirectEve);
                        missingItem.TypeId = typeId;

                        // ... not enough available
                        Log($"Error: Missing type in the hangar. TypeId [{typeId}][{missingItem.TypeName}] AvaiableInHangar [{availableInHangar}] missingInShipsCargo [{missingInShipsCargo}]");
                        myAbyssalState = AbyssalState.BuyItems;
                        return false;
                    }
                }

                if (missingInShipsCargo > 0)
                {
                    if (ESCache.Instance.InStation)
                    {
                        // move constructed item if it exists (non-repackaged)
                        DirectItem item = itemHangar.Items.Where(d => d.TypeId == typeId && d.IsSingleton).FirstOrDefault();

                        if (item == null)
                        {
                            //if no already constructed items exist, grab stacks smallest first
                            item = itemHangar.Items.Where(d => d.TypeId == typeId).OrderBy(d => d.Stacksize).FirstOrDefault();
                        }

                        if (item != null)
                        {
                            Log("shipsCargo.FreeCapacity [" + shipsCargo.FreeCapacity + "][" + item.TypeName + "] Volume per [" + item.Volume + "] Math.Min(item.Stacksize, missingInShipsCargo) [" + Math.Min(item.Stacksize, missingInShipsCargo) + "] Vol * Quantity [" + Math.Round(item.Volume * Math.Min(item.Stacksize, missingInShipsCargo), 2) + "]m3");
                            if (shipsCargo.FreeCapacity >= (item.Volume * Math.Min(item.Stacksize, missingInShipsCargo)))
                            {
                                if (shipsCargo.Add(item, Math.Min(item.Stacksize, missingInShipsCargo)))
                                {
                                    Log("[" + item.TypeName + "] TypeID [" + item.TypeId + "] Quantity [" + item.Quantity + "] added to CargoHold");
                                    LocalPulse = UTCNowAddMilliseconds(500, 1500);
                                    return false;
                                }
                            }
                            else Log("CargoHold has [" + shipsCargo.FreeCapacity + "] m3 left of [" + shipsCargo.Capacity + "] total m3 and [" + item.TypeName + "] is [" + item.TotalVolume + "]m3");
                        }

                        LocalPulse = UTCNowAddMilliseconds(500, 1500);
                        continue;
                    }
                    else if (ESCache.Instance.InSpace)
                    {
                        int cargoHoldItemCount = 0;
                        if (shipsCargo != null && shipsCargo.Items.Any())
                        {
                            cargoHoldItemCount = shipsCargo.Items.Count();
                        }

                        Log("missingInShipsCargo [" + missingInShipsCargo + "] we have [" + availableInCargo + "] of [" + potentialMissingItem.TypeName + "]: We did find [" + cargoHoldItemCount + "] other items in the cargoHold");

                        if (shipsCargo != null && shipsCargo.Items.Any())
                        {
                            if (DebugConfig.DebugArm)
                            {
                                int intCargoBayList = 0;
                                if (DirectEve.Interval(30000)) Log("shipsCargoBayList to Load [" + _shipsCargoBayList.Count() + "]");
                                foreach (var shipsCargoBayItem in _shipsCargoBayList.OrderBy(i => i.Item1))
                                {
                                    intCargoBayList++;
                                    if (DirectEve.Interval(30000)) Log("[" + intCargoBayList + "] TypeID [" + shipsCargoBayItem.Item1 + "] Quantity[" + shipsCargoBayItem.Item2 + "]");
                                }

                                int intCargoHoldItems = 0;
                                if (DirectEve.Interval(30000)) Log("CargoHold Items Found [" + shipsCargo.Items.Count() + "]");
                                foreach (var cargoHoldItem in shipsCargo.Items.OrderBy(i => i.TypeId))
                                {
                                    intCargoHoldItems++;
                                    if (DirectEve.Interval(30000)) Log("[" + intCargoHoldItems + "][" + cargoHoldItem.TypeName + "] TypeID [" + cargoHoldItem.TypeId + "] Quantity [" + Math.Abs(cargoHoldItem.Quantity) + "]");
                                }
                            }
                        }

                        myAbyssalState = AbyssalState.TravelToHomeLocation;
                        Traveler.Destination = null;
                        State.CurrentTravelerState = TravelerState.Idle;
                        return false;
                    }

                    return false;
                }

                if (shipsCargo != null && shipsCargo.Items.Any())
                {
                    if (DebugConfig.DebugArm)
                    {
                        int intCargoBayList = 0;
                        if (DirectEve.Interval(30000)) Log("shipsCargoBayList to Load [" + _shipsCargoBayList.Count() + "]");
                        foreach (var shipsCargoBayItem in _shipsCargoBayList.OrderBy(i => i.Item1))
                        {
                            try
                            {
                                DirectItem tempDirectItem = null;
                                tempDirectItem.TypeId = shipsCargoBayItem.Item1;
                                intCargoBayList++;
                                if (DirectEve.Interval(30000)) Log("[" + intCargoBayList + "][" + tempDirectItem.TypeName + "] TypeID [" + shipsCargoBayItem.Item1 + "] Quantity[" + shipsCargoBayItem.Item2 + "]");
                            }
                            catch (Exception ex)
                            {
                                Log("Exception [" + ex + "]");
                            }
                        }

                        int intCargoHoldItems = 0;
                        if (DirectEve.Interval(30000)) Log("CargoHold Items Found [" + shipsCargo.Items.Count() + "]");
                        foreach (var cargoHoldItem in shipsCargo.Items.OrderBy(i => i.TypeId))
                        {
                            intCargoHoldItems++;
                            if (DirectEve.Interval(30000)) Log("[" + intCargoHoldItems + "][" + cargoHoldItem.TypeName + "] TypeID [" + cargoHoldItem.TypeId + "] Quantity [" + Math.Abs(cargoHoldItem.Quantity) + "]");
                        }
                    }
                }

                continue;
            }

            return true;
        }

        internal void AbyssalArm()
        {
            try
            {
                if (DirectEve.Interval(60000))
                {
                    Log("AbyssalArm: MemoryOptimizer.OptimizeMemory();");
                    //Util.FlushMemIfThisProcessIsUsingTooMuchMemory(800);
                    MemoryOptimizer.OptimizeMemory();
                    return;
                }

                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log("AbyssalArm: 1");

                var itemhangar = ESCache.Instance.DirectEve.GetItemHangar();
                var shipsCargo = ESCache.Instance.DirectEve.GetShipsCargo();
                var droneBay = ESCache.Instance.DirectEve.GetShipsDroneBay();

                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log("AbyssalArm: 2");

                if (itemhangar == null)
                {
                    Log("Waiting on hangar: hangar == null");
                    return;
                }

                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log("AbyssalArm: 3");

                if (shipsCargo == null)
                {
                    Log("Waiting on shipsCargo: shipsCargo == null");
                    return;
                }

                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log("AbyssalArm: 4");

                if (ESCache.Instance.ActiveShip.HasDroneBay && droneBay == null)
                {
                    Log("Waiting on droneBay: if (ESCache.Instance.ActiveShip.HasDroneBay && droneBay == null)");
                    return;
                }

                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log("AbyssalArm: 5");

                if (itemhangar.Items.Count >= 999)
                {
                    Log($"Itemhangar is full, more than 999 items.");
                    myAbyssalState = AbyssalState.Error;
                    return;
                }

                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log("AbyssalArm: 6");

                try
                {
                    if (itemhangar.Items.Any())
                    {
                        long lootValItemHangar = UnloadLoot.CurrentLootValueInItemHangar();
                        if (lootValItemHangar > 0)
                        {
                            WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.ItemHangarValue), lootValItemHangar);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                }

                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log("AbyssalArm: 7");

                // check if there is any other type loaded in the drone bay, clear the bay if that is the case
                if (ESCache.Instance.ActiveShip.HasDroneBay)
                {
                    if (_droneBayItemList.Any())
                    {
                        if (!droneBay.Items.All(d => _droneBayItemList.Any(e => e.Item1 == d.TypeId)))
                        {
                            Log($"Wrong amount or unknown type found in the drone bay. Moving dronebay items to the itemhangar.");
                            itemhangar.Add(droneBay.Items);
                            LocalPulse = DateTime.UtcNow.AddSeconds(3);
                            return;
                        }
                    }
                }

                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log("AbyssalArm: 9");

                // check if there is any other type loaded in the ships cargo bay, clear the bay if that is the case
                if (!shipsCargo.Items.Where(i => !i.TypeName.Contains("Secure Container")).All(d => _shipsCargoBayList.Any(e => e.Item1 == d.TypeId)))
                {
                    Log($"Unknown type found in ships cargo bay. Moving ships cargo bay items to the itemhangar.");
                    var items = shipsCargo.Items.Where(i => !i.TypeName.Contains("Secure Container"));
                    itemhangar.Add(items);
                    LocalPulse = DateTime.UtcNow.AddSeconds(3);
                    return;
                }

                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log("AbyssalArm: 10");

                if (shipsCargo.Items.Any() && 1 > shipsCargo.FreeCapacity)
                {
                    Log($"CargoBay has no free space! how!? Moving ships cargo bay items to the itemhangar.");
                    var items = shipsCargo.Items.Where(i => !i.TypeName.Contains("Secure Container"));
                    itemhangar.Add(items);
                    LocalPulse = DateTime.UtcNow.AddSeconds(3);
                    return;
                }

                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log("AbyssalArm: 11");

                bool? CheckDroneBayForAllDronesNeededResult = CheckDroneBayForAllDronesNeeded();
                if (CheckDroneBayForAllDronesNeededResult == null) return;
                if (CheckDroneBayForAllDronesNeededResult == false) return;

                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log("AbyssalArm: 12");

                if (!CheckCargoHoldForAllSuppliesNeeded()) return;

                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log("AbyssalArm: 13");

                if (ESCache.Instance.ActiveShip.HasDroneBay && !droneBay.Items.All(d => _droneBayItemList.Any(e => e.Item1 == d.TypeId) && _droneBayItemList.FirstOrDefault(e => e.Item1 == d.TypeId).Item2 == droneBay.Items.Where(e => e.TypeId == d.TypeId).Sum(e => e.Stacksize)))
                {
                    // verify drone bay has the correct amount of drones loaded
                    Log($"Wrong amount or unknown type found in the drone bay. Moving dronebay items to the itemhangar.");
                    itemhangar.Add(droneBay.Items);
                    LocalPulse = DateTime.UtcNow.AddSeconds(3);
                    return;
                }

                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log("AbyssalArm: 14");

                //I am not sure whey this check was here, but its causing problems!
                //
                if (Drones.UseDrones && !droneBay.Items.Any())
                {
                   // verify drone bay has the correct amount of drones loaded
                    Log($"drone bay is empty?");
                    LocalPulse = DateTime.UtcNow.AddSeconds(3);
                    return;
                }

                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log("AbyssalArm: 15");

                if (!shipsCargo.Items.Any())
                {
                    // verify drone bay has the correct amount of drones loaded
                    Log($"cargo bay is empty?");
                    LocalPulse = DateTime.UtcNow.AddSeconds(3);
                    return;
                }

                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log("AbyssalArm: 16");

                if (_boosterFailedState)
                {
                    Log($"Error: Booster failed state, changing to error state.");
                    myAbyssalState = AbyssalState.Error;
                    return;
                }

                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log("AbyssalArm: 17");

                // at this point we are ready to go
                Log("Arm finished!.");
                if (itemhangar.CanBeStacked)
                {
                    Log("Stacking item hangar.");
                    itemhangar.StackAll();
                }

                if (DebugConfig.DebugArm)
                {
                    Log("if (DebugConfig.DebugArm): go idle here: we are done Arming: How did we do?");
                    myAbyssalState = AbyssalState.TravelToRepairLocation;
                    Traveler.Destination = null;
                    State.CurrentTravelerState = TravelerState.Idle;
                    ESCache.Instance.PauseAfterNextDock = true;
                    return;
                }

                myAbyssalState = AbyssalState.TravelToRepairLocation;
                Traveler.Destination = null;
                State.CurrentTravelerState = TravelerState.Idle;
                return;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return;
            }
        }

        internal void AbyssalTravelToLeader()
        {
            if (ESCache.Instance.EveAccount.IsLeader)
            {
                Log("IsLeader [" + ESCache.Instance.EveAccount.IsLeader + "] - error!");
                return;
            }

            if (!ESCache.Instance.EveAccount.UseFleetMgr)
            {
                Log("UseFleetMgr [" + ESCache.Instance.EveAccount.UseFleetMgr + "] - error!");
                return;
            }

            if (string.IsNullOrEmpty(ESCache.Instance.EveAccount.LeaderCharacterName))
            {
                Log("LeaderCharacterName [" + ESCache.Instance.EveAccount.LeaderCharacterName + "] - fixme!");
                return;
            }

            Log("Where is the leader [" + ESCache.Instance.EveAccount.LeaderCharacterName + "]?");
            //Is the leader in local?
            //Is the leader undocked
            if (!string.IsNullOrEmpty(ESCache.Instance.EveAccount.LeaderCharacterName))
            {
                Log("LeaderInSpace [" + ESCache.Instance.EveAccount.LeaderInSpace + "] LeaderInStation [" + ESCache.Instance.EveAccount.LeaderInStation + "] LeaderIsInSystemName [" + ESCache.Instance.EveAccount.LeaderIsInSystemName + "] ");
            }
            else Log("LeaderEveAccount is null");



            return;
        }

        internal List<DirectBookmark> _listOfFilamentBookmarks;

        internal List<DirectBookmark> ListOfFilamentBookmarks
        {
            get
            {
                if (DirectEve.Interval(60000)) _listOfFilamentBookmarks = null;

                if (_listOfFilamentBookmarks != null && _listOfFilamentBookmarks.Any())
                {
                    return _listOfFilamentBookmarks;
                }

                if (ESCache.Instance.DirectEve.Bookmarks.Any())
                {
                    _listOfFilamentBookmarks = ESCache.Instance.DirectEve.Bookmarks.Where(b => b.Title.ToLower().StartsWith(_filamentSpotBookmarkName.ToLower()))
                        .OrderByDescending(e => e.IsInCurrentSystem)
                        .ThenByDescending(e => myHomebookmark != null && e.LocationId == myHomebookmark.LocationId)
                        .ThenBy(e => e.Title)
                        .ToList();

                    if (_listOfFilamentBookmarks != null && _listOfFilamentBookmarks.Any())
                    {
                        return _listOfFilamentBookmarks;
                    }

                    return new List<DirectBookmark>();
                }

                return new List<DirectBookmark>();
            }
        }

        internal bool? ShouldWeGoHome
        {
            get
            {
                if (ESCache.Instance.EveAccount.UseScheduler)
                {
                    if (ESCache.Instance.EveAccount.ShouldBeStopped)
                    {
                        Log("ShouldBeStopped [true]");
                        myAbyssalState = AbyssalState.TravelToHomeLocation;
                        return true;
                    }

                    if (DirectEve.Interval(15000)) Log("ShouldBeStopped [false]");
                }

                if (ESCache.Instance.ActiveShip.GivenName != Combat.CombatShipName)
                {
                    Log("We are not in our combat ship: ShouldWeGoHome [true]");
                    myAbyssalState = AbyssalState.TravelToHomeLocation;
                    return true;
                }

                if (ShouldWeRestartEve)
                {
                    Log("ShouldWeRestartEve [true]");
                    myAbyssalState = AbyssalState.TravelToHomeLocation;
                    return true;
                }

                if (DirectEve.Interval(15000)) Log("ShouldWeRestartEve [false]");

                if (ShouldWeGoIdle())
                {
                    Log("ShouldWeGoIdle [true]");
                    myAbyssalState = AbyssalState.TravelToHomeLocation;
                    return true;
                }

                if (DirectEve.Interval(15000)) Log("ShouldWeGoIdle [false]");

                if (HaveWeHitOurSitesPerSessionLimit)
                {
                    Log("HaveWeHitOurSitesPerSessionLimit [true]");
                    myAbyssalState = AbyssalState.TravelToHomeLocation;
                    return true;
                }

                if (DirectEve.Interval(15000)) Log("ShouldWeGoIdle [false]");

                if (IsLocalTooCrowded)
                {
                    Log("IsLocalTooCrowded [true]");
                    myAbyssalState = AbyssalState.TravelToHomeLocation;
                    return true;
                }

                if (DirectEve.Interval(15000)) Log("IsLocalTooCrowded [false]");

                var seconds = 6 * 60 * 60;
                if (!ESCache.Instance.InWormHoleSpace && !ESCache.Instance.DirectEve.Session.SolarSystem.IsZeroZeroSpace && ESCache.Instance.EveAccount.AbyssSecondsDaily > seconds)
                {
                    myAbyssalState = AbyssalState.TravelToHomeLocation;
                    return true;
                }

                if (DirectEve.Interval(15000)) Log("AbyssSecondsDaily [false]");

                if (Time.Instance.IsItDuringDowntimeNow)
                {
                    Log("IsItDuringDowntimeNow [true]");
                    myAbyssalState = AbyssalState.TravelToHomeLocation;
                    return true;
                }

                if (DirectEve.Interval(15000)) Log("IsItDuringDowntimeNow [false]");

                if (shipNeedsRepair)
                {
                    Log("shipNeedsRepair [true]");
                    myAbyssalState = AbyssalState.TravelToHomeLocation;
                    return true;
                }

                if (DirectEve.Interval(15000)) Log("shipNeedsRepair [false]");

                if (dronesNeedRepair)
                {
                    Log("dronesNeedRepair [true]");
                    myAbyssalState = AbyssalState.TravelToHomeLocation;
                    return true;
                }

                if (DirectEve.Interval(15000)) Log("dronesNeedRepair [false]");

                bool? CheckDroneBayForAllDronesNeededResult = CheckDroneBayForAllDronesNeeded();
                if (CheckDroneBayForAllDronesNeededResult == null) return null;
                if (CheckDroneBayForAllDronesNeededResult == false)
                {
                    Log("!CheckDroneBayForAllDronesNeeded(): Go Home");
                    myAbyssalState = AbyssalState.TravelToHomeLocation;
                    return true;
                }

                Log("CheckDroneBayForAllDronesNeeded: We have what we need");

                if (ESCache.Instance.PauseAfterNextDock)
                {
                    Log("PauseAfterNextDock [true]");
                    myAbyssalState = AbyssalState.TravelToHomeLocation;
                    return true;
                }

                Log("PauseAfterNextDock [false]");

                if (Drones.UseDrones)
                {
                    if (!alldronesInBay.Any())
                    {
                        Log("!alldronesInBay.Any(): Go Home!");
                        myAbyssalState = AbyssalState.TravelToHomeLocation;
                        return true;
                    }
                }

                return false;
            }
        }

        internal void AbyssalTravelToFilamentSpot()
        {
            if (DirectEve.Interval(10000, 25000)) Log("case AbyssalState.TravelToFilamentSpot: CurrentTravelerState [" + State.CurrentTravelerState + "]");

            if (!IgnoreConcord && !ESCache.Instance.InWarp && ESCache.Instance.ActiveShip.GivenName == Combat.CombatShipName && ESCache.Instance.DirectEve.Session.SolarSystemName != "Jita")
            {
                if (ESCache.Instance.Entities.Any(i => i.IsOnGridWithMe && 80000 > i.Distance && i.Velocity > 0 && i.TypeName.ToLower().Contains("CONCORD Police".ToLower())) && DirectEve.Interval(600000))
                {
                    string msg = "Notification: Concord found on grid!.!  PauseAfterNextDock [true]";
                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                    Log(msg);

                    foreach (var ConcordEntity in ESCache.Instance.Entities.Where(i => i.IsOnGridWithMe && i.Velocity > 0 && i.TypeName.ToLower().Contains("CONCORD Police".ToLower())))
                    {
                        Log("Notification: Concord found on grid!![" + ConcordEntity.TypeName + "] TypeID [" + ConcordEntity.TypeId + "] at [" + ConcordEntity.Nearest1KDistance + "k] ClosestCelestial [" + ESCache.Instance.ClosestCelestial.Name + "][" + Math.Round(ESCache.Instance.ClosestCelestial.Distance / (double)Distances.OneAu, 1) + " AU] ClosestStation [" + ESCache.Instance.ClosestStation.Name + "][" + ESCache.Instance.ClosestStation.Nearest1KDistance + " k] PauseAfterNextDock [true]");
                    }
                    if (PlayNotificationSounds) Util.PlayNoticeSound();
                    if (PlayNotificationSounds) Util.PlayNoticeSound();
                    if (PlayNotificationSounds) Util.PlayNoticeSound();
                    if (PlayNotificationSounds) Util.PlayNoticeSound();
                    ESCache.Instance.PauseAfterNextDock = true;
                }
            }

            if (DebugConfig.DebugRepairInSpace) Log("Done checking for Concord Police");

            //Note: !AbyssalNeedRepair(true) means we are checking the ship health and the drone health, but NOT modules
            //the specific module check checks for module health. If we combine the checks and just use AbyssalNeedRepair(false)
            //we can have healthy modules but damaged drones or something and get stuck waiting to repair drones in space, which we CANT do!
            //

            if (DebugConfig.DebugRepairInSpace) Log("shipNeedsRepair [" + shipNeedsRepair + "] dronesNeedRepair [" + dronesNeedRepair + "] ESCache.Instance.DirectEve.Modules.Any(m => m.HeatDamage > 0) [" + ESCache.Instance.DirectEve.Modules.Any(m => m.HeatDamage > 0) + "] IsAnyOtherNonFleetPlayerOnGrid [" + IsAnyOtherNonFleetPlayerOnGrid + "]");

            if (ESCache.Instance.InSpace && !shipNeedsRepair && !dronesNeedRepair && ESCache.Instance.DirectEve.Modules.Any(m => m.HeatDamage > 0) && !IsAnyOtherNonFleetPlayerOnGrid)
            {
                if (DebugConfig.DebugRepairInSpace) Log("111a");
                if (!ESCache.Instance.CurrentShipsCargo.Items.Any(i => i.TypeId == _naniteRepairPasteTypeId && !i.IsSingleton && i.Quantity > 30))
                {
                    Log($"Missing enough Nanite Repair Paste to repair: going home");
                    myAbyssalState = AbyssalState.TravelToHomeLocation;
                    return;
                }

                if (DebugConfig.DebugRepairInSpace) Log("111b");

                // if we are at the spot with damaged modules and have nanite in the cargo, repair it!
                if (ESCache.Instance.DirectEve.Modules.Any(m => m.HeatDamage > 0))
                {

                    if (DebugConfig.DebugRepairInSpace) Log("111c");
                    RepairModules();
                    if (DirectEve.Interval(5000))
                    {
                        Log($"Repairing...");
                    }
                    bool WarpWhileRepairing = true;
                    if (WarpWhileRepairing && !ESCache.Instance.InWarp && 4000 > ESCache.Instance.ActiveShip.Entity.Velocity)
                    {
                        if (!DirectEve.Interval(6000))
                            return;

                        if (_listOfFilamentBookmarks.Any() && _listOfFilamentBookmarks.Count > 1)
                        {
                            if (_listOfFilamentBookmarks.Any(i => !i.IsOnGridWithMe && i.IsInCurrentSystem))
                            {
                                var _nextWarpableBookmark = _listOfFilamentBookmarks.Where(i => !i.IsOnGridWithMe && i.IsInCurrentSystem).OrderBy(x => new Guid()).FirstOrDefault();
                                _nextWarpableBookmark.WarpTo();
                                return;
                            }
                        }
                        else Log("If we had more abyssal boomkarks we would be warping between them while repairing");
                    }
                    else if (DirectEve.Interval(10000)) Log("Waiting: Our Velocity is: [" + ESCache.Instance.ActiveShip.Entity.Velocity + "]");

                    //
                    // return here so that we wait for modules to be repaired before moving on to the next site
                    //
                    return;
                }
            }

            if (DebugConfig.DebugRepairInSpace) Log("Done trying to repair modules: Did we succeed?");

            if (ESCache.Instance.InWarp)
            {
                if (ESCache.Instance.GroupWeapons())
                {
                    Log($"Grouped weapons");
                }

                return;
            }

            if (DebugConfig.DebugRepairInSpace) Log("InWarp");

            if (ESCache.Instance.InStation && !ESCache.Instance.InSpace)
            {
                TravelerDestination.Undock();
                return;
            }

            if (DebugConfig.DebugRepairInSpace) Log("InSpace");

            if (DirectEve.Interval(30000, 45000))
            {
                int intCount = 0;
                foreach (DirectUIModule mod in DirectEve.Modules.Where(i => !i.IsOnline || i.HeatDamagePercent > 0))
                {
                    intCount++;
                    Log("module [" + intCount + "][" + mod.TypeName + "] Damage% [" + mod.HeatDamagePercent + "] IsOnline [" + mod.IsOnline + "]");
                }
            }

            if (DebugConfig.DebugRepairInSpace) Log("Done listing modules");

            ManagePropMod();

            ManageDrones();

            ManageTargetLocks();

            ManageWeapons();

            ManageNOS();

            ManageRemoteReps();

            if (DebugConfig.DebugRepairInSpace) Log("Done ManagingRemoteReps");

            if (DirectEve.Modules.Any(m => !m.IsOnline))
            {
                Log("Found offline modules!");
                if (DirectEve.ActiveShip.CapacitorPercentage > 95 && !DirectEve.Modules.FirstOrDefault(m => !m.IsOnline).OnlineModule())
                {
                    return;
                }

                if (DirectEve.Interval(45000, 55000))
                    ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdStopShip);

                if (DirectEve.Interval(30000, 35000))
                    foreach (DirectUIModule mod in DirectEve.Modules.Where(m => !m.IsOnline))
                        Log("Offline module: [" + mod.TypeName + "] Damage% [" + mod.HeatDamagePercent + "]");

                return;
            }

            if (DebugConfig.DebugRepairInSpace) Log("All modules are online");

            if (DirectEve.Interval(60000))
            {
                if (!CheckCargoHoldForAllSuppliesNeeded()) return;
                bool? CheckDroneBayForAllDronesNeededResult = CheckDroneBayForAllDronesNeeded();
                if (CheckDroneBayForAllDronesNeededResult == null) return;
                if (CheckDroneBayForAllDronesNeededResult == false) return;
            }

            if (DebugConfig.DebugRepairInSpace) Log("Checked Cargo and DroneBay!");

            //
            // Reasons to not go into another Abyssal
            //
            bool? ShouldWeGoHomeResult = ShouldWeGoHome;
            if (ShouldWeGoHomeResult == null)
            {
                Log("ShouldWeGoHome [null]");
                return;
            }

            if(ShouldWeGoHomeResult.Value)
            {
                Log("ShouldWeGoHome [true]");
                return;
            }

            if (DebugConfig.DebugRepairInSpace) Log("ShouldWeGoHome [false]");

            if (DirectEve.Modules.Any(m => m.HeatDamagePercent > 0))
            {
                if (DebugConfig.DebugRepairInSpace) Log("We still have modules with damage");
                if (DirectEve.Interval(15000, 25000))
                {
                    foreach (DirectUIModule mod in DirectEve.Modules.Where(m => m.HeatDamagePercent > 0))
                    {
                        Log("Damaged module: [" + mod.TypeName + "] Damage% [" + Math.Round(mod.HeatDamagePercent, 1) + "]");
                    }
                }

                if (DebugConfig.DebugRepairInSpace) Log("Done listing damaged modules");

                if (ESCache.Instance.CurrentShipsCargo.Items.All(x => x.TypeId != _naniteRepairPasteTypeId))
                {
                    Log("Damaged modules found, going back to base trying to repair again");
                    MissionSettings.CurrentFit = string.Empty;
                    MissionSettings.DamagedModulesFound = true;
                    Cleanup.intReDockTorepairModules++;
                    ESCache.Instance.NeedRepair = true;
                    Traveler.Destination = null;
                    Traveler.ChangeTravelerState(TravelerState.Idle);
                    Log("Damaged Modules Found! We have no Nanite Repair Paste [" + _naniteRepairPasteTypeId + "] in cargo. Go repair.");
                    myAbyssalState = AbyssalState.TravelToRepairLocation;
                    return;
                }

                if (DebugConfig.DebugRepairInSpace) Log("We have damaged modules AND Nanite repair paste but didnt rep the modules?! BUG?!");
                return;
            }

            if (DebugConfig.DebugRepairInSpace) Log("Modules have no damage");

            Cleanup.intReDockTorepairModules = 0;

            //if (ESCache.Instance.EveAccount.UseFleetMgr && !ESCache.Instance.EveAccount.IsLeader && ESCache.Instance.DirectEve.FleetMembers.Count() > 1)
            //{
            //    if (DirectEve.Interval(10000)) Log("Use FleetBehavior: UseFleetMgr [" + ESCache.Instance.EveAccount.UseFleetMgr + "] IsLeader [" + ESCache.Instance.EveAccount.IsLeader + "] FleetMembers [" + ESCache.Instance.DirectEve.FleetMembers.Count() + "]");
            //    AbyssalTravelToLeader();
            //    return;
            //}

            DirectBookmark fbmx = null;

            if (ListOfFilamentBookmarks.Any())
            {
                if (DebugConfig.DebugRepairInSpace) Log("ListOfFilamentBookmarks [" + ListOfFilamentBookmarks.Count() + "]");
                if (ESCache.Instance.EveAccount.UseFleetMgr)
                {
                    fbmx = ListOfFilamentBookmarks.FirstOrDefault();
                }
                else
                    fbmx = ListOfFilamentBookmarks.OrderBy(a => a.IsInCurrentSystem).ThenBy(i => Guid.NewGuid()).FirstOrDefault();
            }
            else
            {
                Log("We dont have any bookmark with [" + _filamentSpotBookmarkName + "] in the name. Create some.");
                return;
            }

            if (DebugConfig.DebugRepairInSpace) Log("before TravelToBookMark");
            Traveler.TravelToBookmark(fbmx);

            if (ESCache.Instance.MyShipEntity.HasInitiatedWarp || ESCache.Instance.InWarp)
            {
                Log($"HasInitiatedWarp [" + ESCache.Instance.MyShipEntity.HasInitiatedWarp + "] InWarp [" + ESCache.Instance.InWarp + "]");
                return;
            }

            Log($"HasInitiatedWarp [" + ESCache.Instance.MyShipEntity.HasInitiatedWarp + "] InWarp [" + ESCache.Instance.InWarp + "]");

            if (State.CurrentTravelerState == TravelerState.AtDestination)
            {
                Traveler.Destination = null;
                Log($"Arrived at the filament spot.");

                if (DebugConfig.DebugArm)
                {
                    Log($"DebugArm: Pausing");
                    ControllerManager.Instance.SetPause(true);
                    return;
                }

                myAbyssalState = AbyssalState.UseFilament;
                return;
            }

            if (DebugConfig.DebugRepairInSpace) Log("...");
            return;
        }

        internal void AbyssalTravelToHomeLocation()
        {
            if (ESCache.Instance.InStation)
            {
                if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Capsule)
                {
                    Log("InStation [true] We are in a POD. Pause!");
                    ESCache.Instance.PauseAfterNextDock = true;
                    return;
                }
            }

            if (!IgnoreConcord && !ESCache.Instance.InWarp && ESCache.Instance.ActiveShip.GivenName == Combat.CombatShipName && ESCache.Instance.DirectEve.Session.SolarSystemName != "Jita")
            {
                if (ESCache.Instance.Entities.Any(i => i.IsOnGridWithMe && 80000 > i.Distance && i.Velocity > 0 && i.TypeName.ToLower().Contains("CONCORD Police".ToLower())) && DirectEve.Interval(600000))
                {
                    string msg = "Notification: Concord found on grid!  PauseAfterNextDock [true]";
                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                    Log(msg);
                    foreach (var ConcordEntity in ESCache.Instance.Entities.Where(i => i.IsOnGridWithMe && i.Velocity > 0 && i.TypeName.ToLower().Contains("CONCORD Police".ToLower())))
                    {
                        Log("Notification: Concord found on grid!![" + ConcordEntity.TypeName + "] TypeID [" + ConcordEntity.TypeId + "] at [" + ConcordEntity.Nearest1KDistance + "k] ClosestCelestial [" + ESCache.Instance.ClosestCelestial.Name + "][" + Math.Round(ESCache.Instance.ClosestCelestial.Distance / (double)Distances.OneAu, 1) + " AU] ClosestStation [" + ESCache.Instance.ClosestStation.Name + "][" + ESCache.Instance.ClosestStation.Nearest1KDistance + " k] PauseAfterNextDock [true]");
                    }
                    if (PlayNotificationSounds) Util.PlayNoticeSound();
                    if (PlayNotificationSounds) Util.PlayNoticeSound();
                    if (PlayNotificationSounds) Util.PlayNoticeSound();
                    if (PlayNotificationSounds) Util.PlayNoticeSound();
                    ESCache.Instance.PauseAfterNextDock = true;
                }
            }

            ManagePropMod();

            ManageDrones();

            ManageTargetLocks();

            ManageWeapons();

            ManageNOS();

            ManageRemoteReps();

            var hbm = ESCache.Instance.CachedBookmarks.FirstOrDefault(b => b.Title.ToLower() == _homeStationBookmarkName.ToLower());
            if (hbm == null)
            {
                Log($"Home bookmark name not found. [" + _homeStationBookmarkName + "] Error.");
                myAbyssalState = AbyssalState.Error;
                return;
            }

            if (State.CurrentTravelerState != TravelerState.AtDestination)
            {
                Traveler.TravelToBookmark(hbm);
            }
            else
            {
                Traveler.Destination = null;
                State.CurrentTravelerState = TravelerState.Idle;
                Log($"Arrived at the home station.");
                myAbyssalState = AbyssalState.PrepareToArm;
            }

            return;
        }

        internal void AbyssalTravelToRepairLocation()
        {
            if (ESCache.Instance.InSpace && !AbyssalNeedRepair())
            {
                Log($"Apparently we don't need to repair, skipping repair.");
                Traveler.Destination = null;
                State.CurrentTravelerState = TravelerState.Idle;
                myAbyssalState = AbyssalState.TravelToFilamentSpot;
                return;
            }

            var rbm = ESCache.Instance.CachedBookmarks.FirstOrDefault(b => b.Title.ToLower() == _repairLocationBookmarkName.ToLower());
            if (rbm == null)
            {
                Log("Missing repair bookmark. no bookmark found named [" + _repairLocationBookmarkName + "]");
                return;
            }

            if (ESCache.Instance.DirectEve.Session.IsInDockableLocation && ESCache.Instance.DirectEve.Session.LocationId == rbm.LocationId)
            {
                Traveler.Destination = null;
                State.CurrentTravelerState = TravelerState.Idle;
                Log($"Arrived at the repair location.");
                myAbyssalState = AbyssalState.RepairItems;
                return;
            }

            if (State.CurrentTravelerState != TravelerState.AtDestination)
            {
                Traveler.TravelToBookmark(rbm);
            }
            else
            {
                Traveler.Destination = null;
                State.CurrentTravelerState = TravelerState.Idle;
                Log($"Arrived at the repair location.");
                myAbyssalState = AbyssalState.RepairItems;
            }

            return;
        }

        internal List<DirectItem> filaments
        {
            get
            {
                if (ESCache.Instance.CurrentShipsCargo == null)
                    return null;

                if (ESCache.Instance.CurrentShipsCargo.Items.Any())
                {
                    return ESCache.Instance.CurrentShipsCargo.Items.Where(i => i.GroupId == (int)Group.AbyssalDeadspaceFilament && i.TypeId == _filamentTypeId).ToList();
                }

                return null;
            }
        }

        internal bool CheckForFilamentsAtFilamentBookmark()
        {
            if (ESCache.Instance.CurrentShipsCargo == null)
            {
                Log("if (ESCache.Instance.CurrentShipsCargo == null)");
                return false;
            }

            if (filaments == null || !filaments.Any())
            {
                Log($"We dont have any filaments. Going back to base to re-arm.");
                myAbyssalState = AbyssalState.Start;
                return false;
            }

            if (ESCache.Instance.ActiveShip.IsDestroyer)
            {
                if (filaments.Sum(i => i.Stacksize) < 2)
                {
                    Log($"We dont have 2 filaments. [" + filaments.Sum(i => i.Stacksize) + "] Going back to base to re-arm.");
                    myAbyssalState = AbyssalState.Start;
                    return false;
                }

                var myDestroyerFilamentStack = filaments.FirstOrDefault();
                if (myDestroyerFilamentStack.Stacksize < 3)
                {
                    Log("if (myDestroyerFilamentStack.Stacksize < 3) Stack Cargo");
                    ESCache.Instance.CurrentShipsCargo.StackShipsCargo();
                    return false;
                }

                return true;
            }

            if (ESCache.Instance.ActiveShip.IsFrigate)
            {
                if (filaments.Sum(i => i.Stacksize) < 3)
                {
                    Log($"We dont have 3 filaments. [" + filaments.Sum(i => i.Stacksize) + "] Going back to base to re-arm.");
                    myAbyssalState = AbyssalState.Start;
                    return false;
                }

                var myFrigateFilamentStack = filaments.FirstOrDefault();
                if (myFrigateFilamentStack.Stacksize < 3)
                {
                    Log("if (myFrigateFilamentStack.Stacksize < 3) Stack Cargo");
                    ESCache.Instance.CurrentShipsCargo.StackShipsCargo();
                    return false;
                }

                return true;
            }

            return true;
        }

        internal void AbyssalUseFilament()
        {
            if (ESCache.Instance.CurrentShipsCargo == null)
            {
                Log("if (ESCache.Instance.CurrentShipsCargo == null)");
                return;
            }

            //if (DebugConfig.DebugFleetMgr) Log("1");

            if (DirectEve.Session.IsInDockableLocation)
            {
                if (DirectEve.Interval(3000, 5000))
                {
                    Log($"Can't use a filament in a station");
                }

                myAbyssalState = AbyssalState.Start;
                return;
            }

            //if (DebugConfig.DebugFleetMgr) Log("2");

            if (ESCache.Instance.DirectEve.ActiveShip.GivenName != Combat.CombatShipName)
            {
                Log($"You are trying to run this in a wrong ship.");
                myAbyssalState = AbyssalState.PrepareToArm;
                return;
            }

            if (ESCache.Instance.Weapons.Any(i => i.Charge == null || i.ChargeQty == 0))
            {
                Log("Weapons are missing ammo: waiting");
                return;
            }

            //if (DebugConfig.DebugFleetMgr) Log("3");

            if (DirectEve.Me.IsInAbyssalSpace())
            {
                if (DirectEve.Interval(2000, 5000))
                {
                    Log($"Still in an abyss. Waiting.");
                    return;
                }
            }

            //if (DebugConfig.DebugFleetMgr) Log("4");

            if (DirectEve.Entities.Any(e => e.Distance < 1000000 && e.GroupId == (int)Group.AbyssalTrace))
            {
                if (ESCache.Instance.EveAccount.UseFleetMgr && !ESCache.Instance.EveAccount.IsLeader)
                {
                    Log("Leader probably used a filament: there is a trace here: the leader is gone though: waiting for the next site!?!");
                }
                else
                {
                    if (DirectEve.Interval(4000, 6000))
                    {
                        Log($"Waiting for the old abyssal trace to fade away.");
                    }
                    _nextActionAfterAbyTraceDespawn = DateTime.UtcNow.AddMilliseconds(GetRandom(2500, 4500));
                    return;
                }
            }

            //if (DebugConfig.DebugFleetMgr) Log("5");

            if (_nextActionAfterAbyTraceDespawn > DateTime.UtcNow)
            {
                Log($"Waiting until [{_nextActionAfterAbyTraceDespawn}] to continue.");
                return;
            }

            bool? ShouldWeGoHomeResult = ShouldWeGoHome;
            if (ShouldWeGoHomeResult == null)
            {
                Log("ShouldWeGoHome [null]");
                return;
            }

            if (ShouldWeGoHomeResult.Value)
            {
                Log("ShouldWeGoHome [true]");
                return;
            }

            //if (DebugConfig.DebugFleetMgr) Log("6");

            var activationWnd = DirectEve.Windows.OfType<DirectKeyActivationWindow>().FirstOrDefault();
            if (activationWnd != null && !string.IsNullOrEmpty(activationWnd.Caption))
            {
                Log($"Key activation window found.");
                myAbyssalState = AbyssalState.AbyssalEnter;
                LocalPulse = UTCNowAddMilliseconds(2500, 3000);
                return;
            }

            //if (DebugConfig.DebugFleetMgr) Log("7");

            if (39 > DirectEve.ActiveShip.CapacitorPercentage)
            {
                if (10 > DirectEve.ActiveShip.CapacitorPercentage)
                {
                    Log("Capacitor at [" + Math.Round(DirectEve.ActiveShip.CapacitorPercentage, 0) + "%] going to home station");
                    myAbyssalState = AbyssalState.TravelToHomeLocation;
                    Traveler.Destination = null;
                    State.CurrentTravelerState = TravelerState.Idle;
                    return;
                }

                if (DirectEve.Interval(3000, 5000))
                {
                    Log("Capacitor at [" + Math.Round(DirectEve.ActiveShip.CapacitorPercentage, 0) + "%] waiting... for 39+ percent");
                }

                return;
            }

            //if (DebugConfig.DebugFleetMgr) Log("8");

            if (ESCache.Instance.EveAccount.UseFleetMgr)
            {
                if (DebugConfig.DebugFleetMgr) Log("UseFleetMgr [" + ESCache.Instance.EveAccount.UseFleetMgr + "]");

                if (!IsFleetReadyForAbyssals) return;

                foreach (DirectFleetMember individualFleetMember in ESCache.Instance.DirectEve.FleetMembers.Where(i => i.Name != ESCache.Instance.DirectEve.Me.Name))
                {
                    if (DirectEve.Entities.Any(e => e.Distance < 1000000 && e.GroupId == (int)Group.AbyssalTrace))
                    {
                        if (ESCache.Instance.EveAccount.UseFleetMgr && !ESCache.Instance.EveAccount.IsLeader)
                        {
                            Log("Leader probably used a filament: there is a trace here: the leader is gone though: probably");
                            continue;
                        }
                    }

                    if (DebugConfig.DebugFleetMgr) Log("foreach (DirectFleetMember individualFleetMember in ESCache.Instance.DirectEve.FleetMembers.Where(i => i.Name != ESCache.Instance.DirectEve.Me.Name))");
                    if (individualFleetMember.Entity == null || (double)individualFleetMember.Entity.Distance > (double)Distances.OnGridWithMe)
                    {
                        if (DirectEve.Interval(7000)) Log("[" + individualFleetMember.Name + "] is not on grid with us yet! waiting.");
                        return;
                    }

                    continue;
                }

                if (DebugConfig.DebugFleetMgr) Log("[" + ESCache.Instance.DirectEve.FleetMembers.Count + "] fleet members are here on grid! continue");

                if (ESCache.Instance.EveAccount.IsLeader)
                {
                    if (DebugConfig.DebugFleetMgr) Log("IsLeader [" + ESCache.Instance.EveAccount.IsLeader + "]");
                    if (!CheckForFilamentsAtFilamentBookmark()) return;

                    ActivateFilamentNow();
                    return;
                }

                //FleetMgr: do not activate filaments if you arent the leader: the leader will do that!
                myAbyssalState = AbyssalState.AbyssalEnter;
                return;
            }

            //if (DebugConfig.DebugFleetMgr) Log("9");

            if (!CheckForFilamentsAtFilamentBookmark()) return;
            ActivateFilamentNow();
            return;
        }

        internal void ActivateFilamentNow()
        {
            var filament = filaments.FirstOrDefault();
            if (filament != null)
            {
                if (DirectEve.Interval(3000, 4000))
                {
                    Log($"Activating abyssal key.");
                    filament.ActivateAbyssalKey();

                    if (ESCache.Instance.DirectEve.ActiveShip.Entity.Velocity > 10)
                    {
                        Log($"Stopping the ship.");
                        DirectEve.ExecuteCommand(DirectCmd.CmdStopShip);
                    }
                }
            }
            else
            {
                Log($"Error: No filaments left. Changing state to start.");
                myAbyssalState = AbyssalState.Start;
            }
        }

        private void ActivateAbyssalDeadspaceState()
        {
            try
            {
                if (ESCache.Instance.InStation)
                {
                    myAbyssalState = AbyssalState.Start;
                    return;
                }

                if (!ESCache.Instance.InSpace)
                    return;

                if (ESCache.Instance.InAbyssalDeadspace)
                    return;

                if (ESCache.Instance.MyShipEntity == null)
                    return;

                if (ESCache.Instance.InWarp || ESCache.Instance.MyShipEntity.HasInitiatedWarp)
                    return;

                if (ESCache.Instance.DockableLocations.Any(station => station.IsOnGridWithMe) || ESCache.Instance.Stargates.Any(stargate => stargate.IsOnGridWithMe))
                    return;

                if (!HandleAbyssalTrace()) return; //Used with Destroyer and Frigate Abyssals - technically exists for cruiser abyssals but you auto jump so you dont usually see it
                if (!HandleAbyssalActivationWindow()) return; //Used with Destroyer and Frigate Abyssals
                if (!HandleKeyActivationWindow()) return; //Used with all abyssals to activate the gate and with cruiser abyssals you auto jump - destroyer and frigate abyssals you have 2 other steps
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return;
            }
        }

        private static DirectAbyssActivationWindow _abyssActivationWindow = null;

        public static DirectAbyssActivationWindow abyssActivationWindow
        {
            get
            {
                if (DirectEve.HasFrameChanged())
                {
                    _abyssActivationWindow = ESCache.Instance.DirectEve.Windows.OfType<DirectAbyssActivationWindow>().FirstOrDefault();
                    if (_abyssActivationWindow != null && string.IsNullOrEmpty(_abyssActivationWindow.Caption))
                        return null;

                    return _abyssActivationWindow ?? null;
                }

                return _abyssActivationWindow ?? null;
            }
        }

        private static DirectKeyActivationWindow _keyActivationWindow = null;

        public static DirectKeyActivationWindow keyActivationWindow
        {
            get
            {
                if (DirectEve.HasFrameChanged())
                {
                    _keyActivationWindow = ESCache.Instance.DirectEve.Windows.OfType<DirectKeyActivationWindow>().FirstOrDefault();
                    if (_keyActivationWindow != null && !string.IsNullOrEmpty(_keyActivationWindow.Caption))
                        return _keyActivationWindow;

                    return null;
                }

                return _keyActivationWindow ?? null;
            }
        }

        private bool HandleAbyssalTrace()
        {
            try
            {
                if (!ESCache.Instance.InSpace && ESCache.Instance.InStation)
                {
                    myAbyssalState = AbyssalState.Start;
                    return true;
                }

                if (ESCache.Instance.EntitiesOnGrid.Any(i => i.GroupId == (int)Group.AbyssalTrace) && ESCache.Instance.ClosestStation != null && !ESCache.Instance.ClosestStation.IsOnGridWithMe)
                {
                    var AbyssalTrace = ESCache.Instance.EntitiesOnGrid.FirstOrDefault(i => i.GroupId == (int)Group.AbyssalTrace);
                    if (AbyssalTrace.Distance > (double)Distances.GateActivationRange)
                    {
                        Log("AbyssalTrace.Distance [" + AbyssalTrace.Nearest1KDistance + "k] > [" + Math.Round((double)Distances.GateActivationRange / 1000, 0) + "k]");
                        AbyssalTrace.Orbit(500);
                        return false;
                    }

                    if (!AbyssalTrace.IsOrbitedByActiveShip)
                    {
                        AbyssalTrace.Orbit(500);
                        return false;
                    }

                    if (abyssActivationWindow == null)
                    {
                        if ((double)Distances.GateActivationRange > AbyssalTrace.Distance)
                        {
                            Log("abyssActivationWindow does not exist yet: AbyssalTrace.Activate();");
                            AbyssalTrace.ActivateAccelerationGate();
                            return false;
                        }
                    }

                    return true;
                }

                // detect size of ship here? filament type? need to think about it a bit...
                //if (!ESCache.Instance.InAbyssalDeadspace && ESCache.Instance.EntitiesOnGrid.All(i => i.GroupId != (int)Group.AbyssalTrace))
                //{
                //    Log("What happened? We expected there to be an abyssal trace, there wasnt. Going Home!");
                //    myAbyssalState = AbyssalState.TravelToHomeLocation;
                //}

                return true;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return true;
            }
        }

        private bool HandleAbyssalActivationWindow()
        {
            if (!ESCache.Instance.InSpace && ESCache.Instance.InStation)
            {
                myAbyssalState = AbyssalState.Start;
                return true;
            }

            try
            {
                if (abyssActivationWindow != null && abyssActivationWindow.PyWindow.IsValid)
                {
                    if (Time.Instance.LastActivateAbyssalActivationWindowAttempt.AddSeconds(20) > DateTime.UtcNow)
                    {
                        Log("We have activated the AbyssalActivationWindow: Waiting to be moved to AbyssalDeadspace");
                        return false;
                    }

                    Log("ActivateAbyssalDeadspaceState: Found abyssActivationWindow IsReady [" + abyssActivationWindow.IsReady + "] IsJumping [" + abyssActivationWindow.IsJumping + "]");
                    if (abyssActivationWindow.IsReady && !abyssActivationWindow.IsJumping)
                    {
                        if (ESCache.Instance.MyShipEntity.Velocity > 0)
                        {
                            Log("ActivateAbyssalDeadspaceState: if (abyssActivationWindow.IsReady && !abyssActivationWindow.IsJumping && ESCache.Instance.MyShipEntity.Velocity > 0)");
                            if (DateTime.UtcNow > Time.Instance.NextActivateAccelerationGate)
                            {
                                if (abyssActivationWindow.Activate())
                                {
                                    Log("ActivateAbyssalDeadspaceState: Activating abyssActivationWindow");
                                    Time.Instance.LastActivateAbyssalActivationWindowAttempt = DateTime.UtcNow;
                                    return false;
                                }

                                Log("ActivateAbyssalDeadspaceState: Activating abyssActivationWindow failed: waiting");
                                return false;
                            }
                        }
                        else
                        {
                            Log("if (ESCache.Instance.MyShipEntity.Velocity== 0)");
                            DirectEve.ExecuteCommand(DirectCmd.CmdSetShipFullSpeed);
                        }
                    }

                    if (abyssActivationWindow.IsReady && abyssActivationWindow.IsJumping)
                    {
                        Log("ActivateAbyssalDeadspaceState: Found abyssActivationWindow: if (abyssActivationWindow.IsReady && abyssActivationWindow.IsJumping)");
                        return false;
                    }

                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return true;
            }
        }
        private static int AbyssalFilamentsActivated { get; set; } = 0;

        private bool HandleKeyActivationWindow()
        {
            try
            {
                if (!ESCache.Instance.InSpace && ESCache.Instance.InStation)
                {
                    myAbyssalState = AbyssalState.Start;
                    return true;
                }

                if (keyActivationWindow != null && keyActivationWindow.PyWindow.IsValid)
                {
                    if (Time.Instance.LastActivateKeyActivationWindowAttempt.AddSeconds(20) > DateTime.UtcNow)
                    {
                        Log("We have activated the KeyActivationWindow: Waiting to be moved to AbyssalDeadspace");
                        return false;
                    }

                    if (ESCache.Instance.DirectEve.Me.IsInvuln && !IsAnyOtherNonFleetPlayerOnGrid)
                    {
                        if (DirectEve.Interval(2500, 3500))
                        {

                            //ESCache.Instance.DirectEve.ActiveShip.SetSpeedFraction(1.0f);
                            //ESCache.Instance.Star.AlignTo();
                            ESCache.Instance.ActiveShip.MoveToRandomDirection();
                            Log($"Moving into a random direction to break the abyss invuln timer.");
                            return false;
                        }
                    }

                    //if (20 > ESCache.Instance.MyShipEntity.Velocity)
                    //    return false;

                    Log("ActivateAbyssalDeadspaceState: Found KeyActivationWindow");
                    if (keyActivationWindow.IsReady && !keyActivationWindow.IsJumping)
                    {
                        Log("ActivateAbyssalDeadspaceState: if (keyWindow.IsReady && !keyWindow.IsJumping && ESCache.Instance.MyShipEntity.Velocity > 0)");
                        if (DateTime.UtcNow > Time.Instance.NextActivateAccelerationGate)
                        {
                            if (keyActivationWindow.Activate())
                            {
                                Log("ActivateAbyssalDeadspaceState: Activating Filament");
                                Time.Instance.LastActivateKeyActivationWindowAttempt = DateTime.UtcNow;
                                Time.Instance.NextActivateKeyActivationWindow = DateTime.UtcNow.AddSeconds(8);
                                AbyssalFilamentsActivated++;
                                ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.AbyssalFilamentsActivated), AbyssalFilamentsActivated);
                                Log("ActivateAbyssalDeadspaceState: AbyssalFilamentsActivated this session [" + AbyssalFilamentsActivated + "]");
                                //AbyssalSiteStarted =
                                //    Abyssal
                                return false;
                            }

                            Log("ActivateAbyssalDeadspaceState: Activating Filament failed: waiting");
                            return false;
                        }
                    }

                    if (keyActivationWindow.IsReady && keyActivationWindow.IsJumping)
                    {
                        Log("ActivateAbyssalDeadspaceState: Found KeyActivationWindow: if (keywindow.IsReady && keywindow.IsJumping)");
                        return false;
                    }

                    return false;
                }

                if (keyActivationWindow == null)
                {
                    //if (!AbyssalSitePrerequisiteCheck()) return false;

                    if (!HandleFilaments()) return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return true;
            }
        }

        private bool HandleFilaments()
        {
            try
            {
                if (!ESCache.Instance.InSpace && ESCache.Instance.InStation)
                {
                    myAbyssalState = AbyssalState.Start;
                    return true;
                }

                List<DirectItem> filaments = ESCache.Instance.CurrentShipsCargo.Items.Where(i => i.GroupId == (int)Group.AbyssalDeadspaceFilament && i.TypeId == _filamentTypeId).ToList();

                if (filaments == null || !filaments.Any())
                {
                    Log($"We dont have any filaments. Going back to base to re-arm.");
                    myAbyssalState = AbyssalState.Start;
                    return false;
                }

                if (ESCache.Instance.ActiveShip.IsDestroyer)
                {
                    if (filaments.Sum(i => i.Stacksize) < 2)
                    {
                        Log($"We dont have 2 filaments. [" + filaments.Sum(i => i.Stacksize) + "] Going back to base to re-arm.");
                        myAbyssalState = AbyssalState.Start;
                        return false;
                    }

                    var myDestroyerFilamentStack = filaments.FirstOrDefault();
                    if (myDestroyerFilamentStack.Stacksize < 3)
                    {
                        ESCache.Instance.CurrentShipsCargo.StackShipsCargo();
                        return false;
                    }
                }

                if (ESCache.Instance.ActiveShip.IsFrigate)
                {
                    if (filaments.Sum(i => i.Stacksize) < 3)
                    {
                        Log($"We dont have 3 filaments. [" + filaments.Sum(i => i.Stacksize) + "] Going back to base to re-arm.");
                        myAbyssalState = AbyssalState.Start;
                        return false;
                    }

                    var myFrigateFilamentStack = filaments.FirstOrDefault();
                    if (myFrigateFilamentStack.Stacksize < 3)
                    {
                        ESCache.Instance.CurrentShipsCargo.StackShipsCargo();
                        return false;
                    }
                }

                DirectItem fila = filaments.FirstOrDefault();
                if (fila != null)
                {
                    if (ESCache.Instance.MyShipEntity.Velocity < ESCache.Instance.ActiveShip.MaxVelocity * .25)
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdSetShipFullSpeed);

                    Log($"Activating filament.");
                    if (fila.ActivateAbyssalKey())
                    {
                        Time.Instance.LastActivateFilamentAttempt = DateTime.UtcNow;
                        Log("Activated AbyssalKey [" + fila.TypeName + "] TypeId [" + fila.TypeId + "] GroupId [" + fila.GroupId + "] Quantity [" + fila.Quantity + "] Window will open next...");
                        return false;
                    }

                    Log("Failed to ActivateAbyssalKey [" + fila.TypeName + "] TypeId [" + fila.TypeId + "] GroupId [" + fila.GroupId + "] Quantity [" + fila.Quantity + "]");
                    return false;
                }

                Log("Failed to find any filaments in your cargo");
                foreach (DirectItem item in ESCache.Instance.CurrentShipsCargo.Items)
                    Log("Item [" + item.TypeName + "] GroupId [" + item.GroupId + "] TypeId [" + item.TypeId + "]");

                Log("-------------------------------------------");
                //ChangeAbyssalDeadspaceBehaviorState(AbyssalDeadspaceBehaviorState.GotoHomeBookmark, false);
                myAbyssalState = AbyssalState.TravelToHomeLocation;
                Traveler.Destination = null;
                State.CurrentTravelerState = TravelerState.Idle;
                return false;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return true;
            }
        }

        internal void AbyssalEnter()
        {
            if (!ESCache.Instance.InSpace && ESCache.Instance.InStation)
            {
                myAbyssalState = AbyssalState.Start;
                return;
            }

            var activationWnd = DirectEve.Windows.OfType<DirectKeyActivationWindow>().FirstOrDefault();
            if (activationWnd != null)
            {
                if (IsAnyOtherNonFleetPlayerOnGrid)
                {
                    Log($"Error: There is another player on our abyss safespot, going to the base.");
                    foreach (var p in ESCache.Instance.EntitiesNotSelf.Where(e => e.IsPlayer && e.Distance < 1000000))
                    {
                        Log($"Name [{p.Name}][{p.TypeName}] Distance [{p.Distance}]");
                    }
                    myAbyssalState = AbyssalState.Error;
                    return;
                }

                if (CanAFilamentBeOpened(true))
                {
                    if (ESCache.Instance.DirectEve.Me.IsInvuln && !IsAnyOtherNonFleetPlayerOnGrid)
                    {
                        if (DirectEve.Interval(1500, 4000))
                        {
                            //ESCache.Instance.DirectEve.ActiveShip.SetSpeedFraction(1.0f);
                            //ESCache.Instance.Star.AlignTo();
                            ESCache.Instance.ActiveShip.MoveToRandomDirection();
                            Log($"Moving into a random direction to break the abyss invuln timer.");
                            LocalPulse = UTCNowAddMilliseconds(1000, 1500);
                            return;
                        }
                    }

                    if (activationWnd.AnyError)
                    {
                        _activationErrorTickCount++;
                        if (DirectEve.Interval(1500, 4000))
                        {
                            string msg = "Notification: There is an abyssal filament activation error. Waiting.";
                            DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                            Log(msg);
                            if (PlayNotificationSounds) Util.PlayNoticeSound();

                            if (DirectEve.Interval(10000, 15000) && ESCache.Instance.DirectEve.ActiveShip.Entity.Velocity > 10)
                            {
                                Log($"Stopping the ship.");
                                DirectEve.ExecuteCommand(DirectCmd.CmdStopShip);
                            }
                        }

                        if (_activationErrorTickCount > 150)
                        {
                            if (PlayNotificationSounds) Util.PlayNoticeSound();
                            string msg = "Notification: Error: _activationErrorTickCount > 150.";
                            DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                            Log(msg);
                            myAbyssalState = AbyssalState.Error;
                            _activationErrorTickCount = 0;
                        }

                        return;
                    }

                    if (activationWnd.IsReady)
                    {
                        if (ESCache.Instance.Modules.Any(m => !m.IsOnline))
                        {
                            if (PlayNotificationSounds) Util.PlayNoticeSound();
                            Log($"Error: Not all modules are online.");
                            myAbyssalState = AbyssalState.Error;
                            return;
                        }

                        Log($"Activation window is ready. Activating Filament");
                        if (DirectEve.Interval(1500, 4000) && activationWnd.Activate())
                        {
                            AbyssalFilamentsActivated++;
                            _activationErrorTickCount = 0;
                            Log($"Activating the abyss jump.");
                        }
                    }
                    return;
                }
                else
                {
                    Log($"Error: An entity on the grid is preventing us from opening a filament. Going to the home station.");
                    myAbyssalState = AbyssalState.Error;
                    _activationErrorTickCount = 0;
                    return;
                }
            }
            if (_attemptsToJumpFrigateDestroyerAbyss > 4)
            {
                Log($"Error: _attemptsToJumpFrigateDestroyerAbyss > 4 : try using AbyssalState.ActivateAbyssalDeadspace");
                myAbyssalState = AbyssalState.ActivateAbyssalDeadspace;
                _activationErrorTickCount = 0;
                return;
            }

            if (DirectEve.Me.IsInAbyssalSpace())
            {
                Log($"We are now in the abyss space!");
                _lastFilamentActivation = DateTime.UtcNow;
                myAbyssalState = AbyssalState.AbyssalClear;
                _attemptsToJumpMidgate = 0; // reset attempts to jump midgate to be able to know in which stage we are currently
                AreWeResumingFromACrash = false;
                _abyssStatEntry = null;
            }
            else
            {
                DirectEve.IntervalLog(6000, 6000, "Not yet in abyss space, waiting.");
            }

            if (ESCache.Instance.DirectEve.ActiveShip.Entity.IsFrigate || ESCache.Instance.DirectEve.ActiveShip.Entity.IsDestroyer)
            {
                var trace = ESCache.Instance.EntitiesOnGrid.FirstOrDefault(i => i.GroupId == (int)Group.AbyssalTrace);

                if (trace == null)
                {
                    Log("There is no abyssal trace yet");
                    return;
                }

                if (trace.Distance > (double)Distances.GateActivationRange)
                {
                    if (!trace.IsOrbitedByActiveShip)
                    {
                        Log($"Orbiting the abyssal trace at [{Math.Round(trace.Distance, 0)}]m.");
                        trace.Orbit(250);
                    }

                    return;
                }

                var wnd = ESCache.Instance.Windows.OfType<DirectAbyssActivationWindow>().FirstOrDefault();
                if (wnd != null)
                {
                    if (wnd.Activate())
                    {
                        Log($"Jumping into the abyss.");
                        _attemptsToJumpFrigateDestroyerAbyss++;
                    }
                    else
                    {
                        Log("if (wnd.Activate()) returned false");
                        _attemptsToJumpFrigateDestroyerAbyss++;
                    }

                    return;
                }

                if (ESCache.Instance.EveAccount.UseFleetMgr)
                {
                    if (string.IsNullOrEmpty(ESCache.Instance.EveAccount.LeaderCharacterName))
                    {
                        Log("LeaderCharacterName is required and is not set. Set it in the launcher!");
                        return;
                    }

                    if (ESCache.Instance.EveAccount.IsLeader)
                    {
                        if (ESCache.Instance.EveAccount.LeaderCharacterName != ESCache.Instance.EveAccount.CharacterName)
                        {
                            Log("LeaderCharacterName [" + ESCache.Instance.EveAccount.LeaderCharacterName + "] is not my CharacterName: Why is IsLeader [" + ESCache.Instance.EveAccount.IsLeader + "] on this toon!?");
                            return;
                        }

                        Log($"We are close enough to jump, trying to open the jump window. Distance [{Math.Round(trace.Distance, 0)}]m. Attempts [{_attemptsToJumpFrigateDestroyerAbyss}]");
                        trace.ActivateAccelerationGate();
                        _attemptsToJumpFrigateDestroyerAbyss++;
                        LocalPulse = UTCNowAddMilliseconds(1500, 3500);
                        return;
                    }

                    foreach (DirectFleetMember individualFleetMember in ESCache.Instance.DirectEve.FleetMembers.Where(i => i.Name != ESCache.Instance.DirectEve.Me.Name))
                    {
                        if (DebugConfig.DebugFleetMgr) Log("foreach (DirectFleetMember individualFleetMember in ESCache.Instance.DirectEve.FleetMembers.Where(i => i.Name != ESCache.Instance.DirectEve.Me.Name))");
                        if (individualFleetMember.Entity != null && individualFleetMember.Entity.Name == ESCache.Instance.EveAccount.LeaderCharacterName)
                        {
                            //...
                            Log("[" + individualFleetMember.Name + "] waiting for Leader to jump first!");
                            LocalPulse = UTCNowAddMilliseconds(200, 300);
                            return;
                        }

                        continue;
                    }

                    if (DebugConfig.DebugFleetMgr) Log("[" + ESCache.Instance.DirectEve.FleetMembers.Count + "] leader probably jumped! continue");

                    Log($"We are close enough to jump, trying to open the jump window. Distance [{trace.Distance}]m. Attempts [{_attemptsToJumpFrigateDestroyerAbyss}]");
                    trace.ActivateAccelerationGate();
                    _attemptsToJumpFrigateDestroyerAbyss++;
                    LocalPulse = UTCNowAddMilliseconds(1500, 3500);
                    return;
                }

                Log($"We are close enough to jump, trying to open the jump window. Distance [{trace.Distance}]m. Attempts [{_attemptsToJumpFrigateDestroyerAbyss}]");
                trace.ActivateAccelerationGate();
                _attemptsToJumpFrigateDestroyerAbyss++;
                LocalPulse = UTCNowAddMilliseconds(1500, 3500);
            }

            return;
        }

        internal bool HandleTierFiveSingleRoomAbyss()
        {
            try
            {
                if (_singleRoomAbyssal)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (_singleRoomAbyssal)");
                    if (!DebugConfig.DebugFourteenBattleshipSpawnRunAway)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (!DebugConfig.DebugFourteenBattleshipSpawnRunAway)");
                        if (_getMTUInSpace == null && !allDronesInSpace.Any())
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (_getMTUInSpace == null && !allDronesInSpace.Any())");
                            if (DirectEve.Interval(2500, 3000) && _nextGate.Distance <= 2300 && DirectSession.LastSessionChange.AddMilliseconds(4500) < DateTime.UtcNow)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (DirectEve.Interval(2500, 3000) && _nextGate.Distance <= 2300 && DirectSession.LastSessionChange.AddMilliseconds(4500) < DateTime.UtcNow)");
                                if (IsAbyssGateOpen)
                                {
                                    if (DebugConfig.DebugNavigateOnGrid) Log("if (IsAbyssGateOpen)");
                                    try
                                    {
                                        _abyssStatEntry.MTULost = _getMTUInBay == null;
                                        _attemptsToJumpMidgate = 0;
                                        WriteStatsToDB();
                                    }
                                    catch (Exception ex)
                                    {
                                        Log("Exception [" + ex + "]");
                                        return true;
                                    }

                                    if (DebugConfig.DebugNavigateOnGrid) Log("if (IsAbyssGateOpen) - 2");

                                    AreWeResumingFromACrash = false;
                                    _abyssStatEntry = null;
                                    Log($"ActivateAccelerationGate - SingleRoomAbyssal");
                                    _nextGate.ActivateAccelerationGate();
                                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.KEEP_ALIVE, "Keep alive."));
                                    myAbyssalState = AbyssalState.Start;

                                    // here we write the abyss stats
                                }

                                if (DirectEve.Interval(8000)) ESCache.Instance.AbyssalGate.Orbit(2500);
                                return true; // dont proceed to do anything in a single room abyss
                            }

                            if (DirectEve.Interval(8000)) ESCache.Instance.AbyssalGate.Orbit(2500);
                            return true; // dont proceed to do anything in a single room abyss
                        }

                        if (DirectEve.Interval(8000)) ESCache.Instance.AbyssalGate.Orbit(2500);
                        return true; // dont proceed to do anything in a single room abyss
                    }

                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return true;
            }
        }

        internal void RepairModules()
        {
            foreach (var mod in DirectEve.Modules.Where(m => m.HeatDamagePercent > 0 && !m.IsBeingRepaired))
            {

                if ((mod.IsInLimboState || mod.IsActive) && mod.IsActivatable)
                {
                    if (!mod.IsDeactivating)
                    {
                        if (DirectEve.Interval(1500, 2200, mod.ItemId.ToString()))
                        {
                            Log($"Deactivating [{mod.TypeName}].");
                            mod.Click();
                        }
                    }

                    return;
                }


                if (!mod.IsBeingRepaired)
                {
                    if (!ShouldWeRepair)
                        continue;

                    // repair
                    if (mod.Repair())
                    {
                        Log("Repairing [" + mod.TypeName + "] Damage [" + mod.HeatDamagePercent + "%] IsBeingRepaired [" + mod.IsBeingRepaired + "] IsOnline [" + mod.IsOnline + "]");
                        continue;
                    }
                }
            }

            return;
        }


        internal bool AllRatsHaveBeenCleared
        {
            get
            {
                try
                {
                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.FourteenBattleshipSpawn)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] return true: we want to run away!");
                        return true;
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))
                    {
                        if (DirectEve.Interval(15000) && !ESCache.Instance.ActiveShip.IsPod)
                        {
                            LogPriorityCombatTargetInfo();
                            LogAutomataSuppressorInfo();
                            LogTrackingTowerInfo();
                            LogFleetMemberInfo();
                        }

                        if (DirectEve.Interval(3000) && !ESCache.Instance.ActiveShip.IsPod)
                        {
                            LogMyPositionInSpace();
                            LogMyDamagedModules();
                        }

                        return false;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        internal bool abandonDrones
        {
            get
            {
                var secondsSince = (DateTime.UtcNow - _startedToRecallDronesWhileNoTargetsLeft.Value).TotalSeconds;

                if (secondsSince > 45 && IsAbyssGateOpen)
                    return true;

                if (secondsSince > 25 && CurrentStageRemainingSecondsWithoutPreviousStages - _secondsNeededToReachTheGate + 10 < 0)
                    return true;

                if (secondsSince > 19 && IsAbyssGateOpen && CurrentAbyssalStage == AbyssalStage.Stage3 && _abyssRemainingSeconds < 15)
                    return true;

                if (IsAbyssGateOpen && CurrentAbyssalStage == AbyssalStage.Stage3 && _abyssRemainingSeconds < 9)
                    return true;

                return false;
            }
        }

        private static bool HealthCheckNeeded
        {
            get
            {
                //exiting abyssal deadspace
                if (ESCache.Instance.AccelerationGates.Count > 0 && ESCache.Instance.AccelerationGates.FirstOrDefault().Name.Contains("Origin Conduit"))
                    return false;

                //abyssal timer uncomfortably low
                if (315 > ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds)
                    return false;

                if (ESCache.Instance.TriglavianConstructionSiteSpawnFoundDozenPlusBSs)
                    return false;

                return true;
            }
        }

        public static bool HealthCheck()
        {
            if (ESCache.Instance.ActiveShip != null && HealthCheckNeeded)
            {
                if (ESCache.Instance.ActiveShip.IsShieldTanked)
                {
                    if (65 > ESCache.Instance.ActiveShip.ShieldPercentage)
                    {
                        Log("HealthCheck Failed: Waiting: HealthCheckMinimumShieldPercentage [" + 65 + "] > ShieldPercentage [" + ESCache.Instance.ActiveShip.ShieldPercentage + "]");
                        return false;
                    }
                }
                else if (ESCache.Instance.ActiveShip.IsArmorTanked)
                {
                    if (65 > ESCache.Instance.ActiveShip.ArmorPercentage)
                    {
                        Log("HealthCheck Failed: Waiting: HealthCheckMinimumArmorPercentage [" + 65 + "] > ArmorPercentage [" + ESCache.Instance.ActiveShip.ArmorPercentage + "]");
                        return false;
                    }
                }

                if (!ESCache.Instance.Modules.Any(i => i.GroupId == (int)Group.CapacitorInjector) && ESCache.Instance.ActiveShip.IsActiveTanked && HealthCheckMinimumCapacitorPercentage > ESCache.Instance.ActiveShip.CapacitorPercentage)
                {
                    Log("HealthCheck Failed: Waiting: HealthCheckMinimumCapacitorPercentage [" + HealthCheckMinimumCapacitorPercentage + "] > CapacitorPercentage [" + ESCache.Instance.ActiveShip.CapacitorPercentage + "]");
                    return false;
                }

                /**
                if ((ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsFrigate) && ESCache.Instance.Modules.Any(i => !i.IsActive && i._module.IsBeingRepaired && i.IsShieldHardener && i.DamagePercent > 40))
                {
                    Log("HealthCheck Failed: Waiting: IsShieldHardener: DamagePercent [" + ESCache.Instance.Modules.FirstOrDefault(i => !i.IsActive && i.IsShieldHardener && i.DamagePercent > 40).DamagePercent + "]");
                    return false;
                }

                if ((ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsFrigate) && ESCache.Instance.Modules.Any(i => !i.IsActive && i._module.IsBeingRepaired && i.IsShieldRepairModule && i.DamagePercent > 40))
                {
                    Log("HealthCheck Failed: Waiting: IsShieldRepairModule: DamagePercent [" + ESCache.Instance.Modules.FirstOrDefault(i => !i.IsActive && i.IsShieldRepairModule && i.DamagePercent > 40).DamagePercent + "]");
                    return false;
                }
                **/
            }

            return true;
        }

        internal bool boolDoWeHaveTime
        {
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return true;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.FourteenBattleshipSpawn)
                    return false;

                if (ESCache.Instance.InAbyssalDeadspace && ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds == 0)
                {
                    //broken timer?
                    return true;
                }

                if (ESCache.Instance.Entities.Any(i => i.IsPlayer && i.IsPod))
                    return false;

                if (FleetIdealCount > ESCache.Instance.Entities.Count(i => i.IsPlayer)) //this includes self
                    return false;

                switch (CurrentAbyssalStage)
                {
                    case AbyssalStage.Stage1:
                        //14min = 60 * 14
                        if ((60 * 13) > ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds)
                        {
                            Log("Stage1: DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] AbyssalRemainingSeconds [" + Math.Round(ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds) + "] is less than [" + (60 * 14) + "]seconds - skipping looting! death == bad - hurry!");
                            return false;
                        }

                        break;
                    case AbyssalStage.Stage2:
                        if ((60 * 6) > ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds)
                        {
                            Log("Stage2: DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] AbyssalRemainingSeconds [" + Math.Round(ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds) + "] is less than [" + (60 * 7) + "]seconds - skipping looting! death == bad - hurry!");
                            return false;
                        }

                        break;
                    case AbyssalStage.Stage3:
                        if ((60 * 1) > ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds)
                        {
                            Log("Stage3: DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] AbyssalRemainingSeconds [" + Math.Round(ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds) + "] is less than [" + (60 * 1) + "]seconds - skipping looting! death == bad - hurry!");
                            return false;
                        }

                        break;
                }

                return true;
            }
        }

        internal bool boolShouldBioAdaptiveCacheBeDead
        {
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (ESCache.Instance.InAbyssalDeadspace && ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds == 0)
                {
                    //broken timer?
                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.FourteenBattleshipSpawn)
                    return false;

                switch (CurrentAbyssalStage)
                {
                    case AbyssalStage.Stage1:
                        //14min = 60 * 14
                        if ((60 * 17) > ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds)
                        {
                            //Log("Stage1:  AbyssalRemainingSeconds [" + Math.Round(ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds) + "] is less than [" + (60 * 17) + "]seconds");
                            return true;
                        }

                        break;
                    case AbyssalStage.Stage2:
                        if ((60 * 10) > ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds)
                        {
                            //Log("Stage2:  AbyssalRemainingSeconds [" + Math.Round(ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds) + "] is less than [" + (60 * 10) + "]seconds");
                            return true;
                        }

                        break;
                    case AbyssalStage.Stage3:
                        if ((60 * 4) > ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds)
                        {
                            //Log("Stage3:  AbyssalRemainingSeconds [" + Math.Round(ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds) + "] is less than [" + (60 * 4) + "]seconds");
                            return true;
                        }

                        break;
                }

                return false;
            }
        }

        internal bool boolRunAwayNoTimeLeft
        {
            get
            {
                if (ESCache.Instance.InAbyssalDeadspace && ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds == 0)
                {
                    //broken timer?
                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.FourteenBattleshipSpawn)
                    return false;

                switch (CurrentAbyssalStage)
                {
                    case AbyssalStage.Stage1:
                        //14min = 60 * 14
                        if ((60 * 12) > ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds)
                        {
                            Log("Stage1:  AbyssalRemainingSeconds [" + Math.Round(ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds) + "] is less than [" + (60 * 14) + "]seconds - skipping looting! death == bad - hurry!");
                            return false;
                        }

                        break;
                    case AbyssalStage.Stage2:
                        if ((60 * 5) > ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds)
                        {
                            Log("Stage2:  AbyssalRemainingSeconds [" + Math.Round(ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds) + "] is less than [" + (60 * 7) + "]seconds - skipping looting! death == bad - hurry!");
                            return false;
                        }

                        break;
                    case AbyssalStage.Stage3:
                        if ((45 * 1) > ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds)
                        {
                            Log("Stage3:  AbyssalRemainingSeconds [" + Math.Round(ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds) + "] is less than [" + (60 * 1) + "]seconds - skipping looting! death == bad - hurry!");
                            return false;
                        }

                        break;
                }

                return true;
            }
        }

        internal bool IsCacheDead
        {
            get
            {
                if (ESCache.Instance.EntitiesOnGrid.Any(i => i.IsAbyssalBioAdaptiveCache))
                    return false;

                return true;
            }
        }

        internal bool IsCacheWreckEmpty
        {
            get
            {
                if (!ESCache.Instance.Wrecks.Any())
                    return false;

                if (ESCache.Instance.Wrecks.Any())
                {
                    if (ESCache.Instance.Wrecks.Any(i => i.Name.Contains("Cache Wreck")))
                    {
                        if (ESCache.Instance.Wrecks.Any(i => i.Name.Contains("Cache Wreck") && !i.IsWreckEmpty))
                            return false;

                        return true;
                    }
                }

                return false;
            }
        }

        internal bool HandleTasksOnceRatsAreDead()
        {
            if (DebugConfig.DebugNavigateOnGrid) Log("HandleTasksOnceRatsAreDead");
            bool tempHaveAllRatsBeenCleared = AllRatsHaveBeenCleared;
            if (tempHaveAllRatsBeenCleared || ESCache.Instance.ActiveShip.IsPod) // no targets || remaining targets only loot || leave mtu behind
            {
                Log("AllRatsHaveBeenCleared [" + tempHaveAllRatsBeenCleared + "] Am I in a pod? [" + ESCache.Instance.ActiveShip.IsPod + "] IsCacheDead [" + IsCacheDead + "] IsCacheWreckEmpty [" + IsCacheWreckEmpty + "] boolDoWeHaveTime [" + boolDoWeHaveTime + "]");
                UpdateTimeLastNPCWasKilled();

                if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.IsPod)
                {
                    if (DirectEve.Interval(15000, 20000))
                        Log($"We are in a pod: can we save the pod?");
                }
                else if (DirectEve.Interval(15000, 20000))
                    Log($"No targets left.");


                if (ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsFrigate)
                {
                    if (DirectEve.Interval(5000))
                    {
                        if (allDronesInSpace.Any())
                            DirectEve.ExecuteCommand(DirectCmd.CmdDronesReturnToBay);
                    }

                    if (ESCache.Instance.AbyssalGate.Distance > 25000 && ESCache.Instance.Wrecks.All(i => i.IsWreckEmpty))
                    {
                        ESCache.Instance.AbyssalGate.Orbit(500);
                        return true;
                    }

                    if (ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty && i.Name == "Triglavian Biocombinative Cache"))
                    {
                        ESCache.Instance.Wrecks.FirstOrDefault(i => !i.IsWreckEmpty).Orbit(500);
                        return true;
                    }
                }

                if (!ESCache.Instance.ActiveShip.IsPod)
                {
                    if (ESCache.Instance.DirectEve.Weapons.All(i => !i.IsActive && !i.IsInLimboState))
                    {
                        if (DirectEve.Interval(20000)) ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdReloadAmmo);
                    }

                    RecallDrones();

                    if (allDronesInSpace.Any() && ESCache.Instance.ActiveShip.IsLocatedWithinSpeedCloud && ESCache.Instance.AbyssalGate._directEntity.IsLocatedWithinSpeedCloud && !ESCache.Instance.Wrecks.OrderBy(i => i.Distance).FirstOrDefault()._directEntity.IsLocatedWithinSpeedCloud)
                    {
                        Log("Drones In space [" + allDronesInSpace.Count() + "]: We are in a speed cloud: gate is in a speed cloud: wreck is not: heading to the wreck: waiting for drones to return!");
                        ESCache.Instance.Wrecks.OrderBy(i => i.Distance).FirstOrDefault()._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(1000, ESCache.Instance.Wrecks.OrderBy(i => i.Distance).FirstOrDefault()._directEntity);
                        return true;
                    }

                    if (AbyssalSpawn.DetectSpawn != AbyssalSpawn.AbyssalSpawnType.FourteenBattleshipSpawn)
                    {
                        if (ESCache.Instance.Entities.Any(i => i.IsAbyssalBioAdaptiveCache && i.IsReadyToTarget && !i.IsTarget && !i.IsTargeting))
                            ESCache.Instance.Entities.FirstOrDefault(i => i.IsAbyssalBioAdaptiveCache && i.IsReadyToTarget && !i.IsTarget && !i.IsTargeting).LockTarget("HandleTasksOnceRatsAreDead");
                    }

                    if (boolDoWeHaveTime || (!ESCache.Instance.Targets.Any() && AbyssalSpawn.DetectSpawn != AbyssalSpawn.AbyssalSpawnType.FourteenBattleshipSpawn))
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.IsAbyssalBioAdaptiveCache))
                        {
                            Log("Cache is still alive!");
                            if (Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsAbyssalBioAdaptiveCache).Distance > 10000)
                            {
                                Log("AbyssalBioAdaptiveCache.MoveToViaAStar()");
                                DirectEntity.MoveToViaAStar(2000, distanceToTarget: 10000, forceRecreatePath: forceRecreatePath, dest: Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsAbyssalBioAdaptiveCache)._directEntity.DirectAbsolutePosition,
                                    ignoreAbyssEntities: true,
                                    ignoreTrackingPolyons: ESCache.Instance.IgnoreTrackingPolyons_ImprovesTracking_HelpsToHitSmallThings,
                                    ignoreAutomataPylon: ESCache.Instance.IgnoreAutomataPylon_Kills_NPCDrones_OurDrones_And_Missiles,
                                    ignoreWideAreaAutomataPylon: true,
                                    ignoreBioClouds: ESCache.Instance.IgnoreBioClouds_Blue_4xSignatureRadius,
                                    ignoreFilaCouds: ESCache.Instance.IgnoreFilaClouds_Orange_ShieldBoostCapacitorUsePenalty,
                                    ignoreTachClouds: ESCache.Instance.IgnoreTachClouds_White_4xSpeedBoost);
                                return true;
                            }

                            Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsAbyssalBioAdaptiveCache).Approach();
                            if (ESCache.Instance.Targets.Any(i => !i.IsAbyssalBioAdaptiveCache))
                            {
                                if (DirectEve.Interval(7000)) Log("Cache is still alive!!");
                                if (Combat.PotentialCombatTargets.Any(i => i.IsAbyssalBioAdaptiveCache && i.IsReadyToTarget && !i.IsTarget && !i.IsTargeting))
                                {
                                    if (DirectEve.Interval(7000)) Log("Cache is still alive and not yet targeted! wtf: Locking target");
                                    Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsAbyssalBioAdaptiveCache && i.IsReadyToTarget && !i.IsTarget && !i.IsTargeting).LockTarget("HandleTasksOnceRatsAreDead!");
                                    return true;
                                }
                            }

                            return true;
                        }

                        if (_getMTUInSpace == null)
                        {
                            if (_mtuTypeId != 0)
                            {
                                if (_getMTUInBay != null)
                                {
                                    if (!_mtuAlreadyDroppedDuringThisStage)
                                    {
                                        if (DirectEve.Interval(4000)) Log("HandleMTU: this only runs here if the MTU hasnt dropped until after all NPCs are dead: why did this happen?");
                                        HandleMTU();
                                    }
                                    else if (_mtuAlreadyDroppedDuringThisStage)
                                    {
                                        if (!ESCache.Instance.ActiveShip.IsAssaultShip && ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty && !i.Name.ToLower().Contains("Extraction".ToLower()) && i.Name.ToLower().Contains("Cache Wreck".ToLower())))
                                        {
                                            if (DirectEve.Interval(4000)) Log("_mtuAlreadyDroppedDuringThisStage [" + _mtuAlreadyDroppedDuringThisStage + "] MTU Is In CargoHold: There is a Cache wreck that is not empty");
                                            HandleMTU();
                                        }
                                    }
                                }
                            }
                        }
                        else if (_getMTUInSpace != null)
                        {
                            if (_mtuTypeId != 0 && 11 >= _mtuScoopAttempts && ESCache.Instance.EntitiesOnGrid.All(i => !i.IsAbyssalBioAdaptiveCache)) // no targets || remaining targets only loot || leave mtu behind)
                            {
                                HandleMTU();
                                if (DirectEve.Interval(4000)) Log("MTU is still in space! _mtuScoopAttempts [" + _mtuScoopAttempts + "]");

                                if (_getMTUInSpace.Distance > 10000 && AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                                {
                                    Log("AbyssalBioAdaptiveCache.MoveToViaAStar()");
                                    if (DirectEntity.MoveToViaAStar(2000, distanceToTarget: 10000, forceRecreatePath: forceRecreatePath, dest: _getMTUInSpace._directEntity.DirectAbsolutePosition,
                                        ignoreAbyssEntities: true,
                                        ignoreTrackingPolyons: ESCache.Instance.IgnoreTrackingPolyons_ImprovesTracking_HelpsToHitSmallThings,
                                        ignoreAutomataPylon: ESCache.Instance.IgnoreAutomataPylon_Kills_NPCDrones_OurDrones_And_Missiles,
                                        ignoreWideAreaAutomataPylon: true,
                                        ignoreBioClouds: ESCache.Instance.IgnoreBioClouds_Blue_4xSignatureRadius,
                                        ignoreFilaCouds: ESCache.Instance.IgnoreFilaClouds_Orange_ShieldBoostCapacitorUsePenalty,
                                        ignoreTachClouds: ESCache.Instance.IgnoreTachClouds_White_4xSpeedBoost))
                                    {
                                        if (DirectEve.Interval(3000)) _getMTUInSpace.Approach();
                                        return true;
                                    }
                                }

                                if (DebugConfig.DebugNavigateOnGrid) Log("_getMTUInSpace Approach!");
                                _getMTUInSpace.Approach();
                                return true;
                            }
                        }

                        //if we have no MTU...
                        if (boolDoWeHaveTime && _getMTUInSpace == null)
                        {
                            Log("We have no MTU: Check for wrecks that are not empty");

                            EntityCache unlootedWreck = null;
                            //Player wrecks?!
                            if (boolDoWeHaveTime && ESCache.Instance.EveAccount.UseFleetMgr && ESCache.Instance.DirectEve.FleetMembers.Count() > 1)
                            {
                                unlootedWreck = ESCache.Instance.UnlootedContainers.FirstOrDefault(i => i.IsWreck && !i.IsWreckEmpty && !i.Name.ToLower().Contains("Extraction".ToLower()) && !i.Name.ToLower().Contains("Cache Wreck".ToLower()) && 50000 > i._directEntity.DistanceTo(ESCache.Instance.AbyssalCenter._directEntity));
                                if (unlootedWreck != null) Log("Player Wreck found!");
                            }

                            if (unlootedWreck == null)
                            {
                                //Cache wrecks
                                unlootedWreck = ESCache.Instance.UnlootedContainers.FirstOrDefault(i => i.IsWreck && !i.IsWreckEmpty && !i.Name.ToLower().Contains("Extraction".ToLower()) && i.Name.ToLower().Contains("Cache Wreck".ToLower()) && 50000 > i.Distance);
                                if (unlootedWreck != null) Log("Cache Wreck found!");
                            }

                            if (unlootedWreck != null)
                            {
                                if (unlootedWreck.Distance > 2300 && unlootedWreck._directEntity.IsWithinAbyssBounds())
                                {
                                    if (unlootedWreck.Distance > 10000)
                                    {
                                        Log($"unlootedWreck.MoveToViaAStar();");
                                        DirectEntity.MoveToViaAStar(2000, distanceToTarget: 0, forceRecreatePath: forceRecreatePath, dest: unlootedWreck._directEntity.DirectAbsolutePosition,
                                            ignoreAbyssEntities: true,
                                            ignoreTrackingPolyons: ESCache.Instance.IgnoreTrackingPolyons_ImprovesTracking_HelpsToHitSmallThings,
                                            ignoreAutomataPylon: ESCache.Instance.IgnoreAutomataPylon_Kills_NPCDrones_OurDrones_And_Missiles,
                                            ignoreWideAreaAutomataPylon: true,
                                            ignoreBioClouds: ESCache.Instance.IgnoreBioClouds_Blue_4xSignatureRadius,
                                            ignoreFilaCouds: ESCache.Instance.IgnoreFilaClouds_Orange_ShieldBoostCapacitorUsePenalty,
                                            ignoreTachClouds: ESCache.Instance.IgnoreTachClouds_White_4xSpeedBoost);
                                        return true;
                                    }

                                    if (DebugConfig.DebugNavigateOnGrid) Log("unlootedWreck Approach!");
                                    unlootedWreck.Approach();
                                    return true;
                                }

                                return true;
                            }
                        }

                        var can = ESCache.Instance.Containers.FirstOrDefault(i => !i.IsWreck);
                        if (boolDoWeHaveTime && can != null)
                        {
                            Log("can found!");
                            if (can.Distance > 2300 && can._directEntity.IsWithinAbyssBounds())
                            {
                                if (can.Distance > 10000 && AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                                {
                                    Log($"can.MoveToViaAStar();");
                                    DirectEntity.MoveToViaAStar(2000, distanceToTarget:0, forceRecreatePath: forceRecreatePath, dest: can._directEntity.DirectAbsolutePosition,
                                        ignoreAbyssEntities: true,
                                        ignoreTrackingPolyons: ESCache.Instance.IgnoreTrackingPolyons_ImprovesTracking_HelpsToHitSmallThings,
                                        ignoreAutomataPylon: ESCache.Instance.IgnoreAutomataPylon_Kills_NPCDrones_OurDrones_And_Missiles,
                                        ignoreWideAreaAutomataPylon: true,
                                        ignoreBioClouds: ESCache.Instance.IgnoreBioClouds_Blue_4xSignatureRadius,
                                        ignoreFilaCouds: ESCache.Instance.IgnoreFilaClouds_Orange_ShieldBoostCapacitorUsePenalty,
                                        ignoreTachClouds: ESCache.Instance.IgnoreTachClouds_White_4xSpeedBoost);
                                    return true;
                                }

                                if (DebugConfig.DebugNavigateOnGrid) Log("can approach!");
                                if (DirectEve.Interval(3000)) can.Approach();
                                return true;
                            }
                        }
                    }

                    if (_getMTUInSpace != null)
                    {
                        if (_lastMTUScoop.AddSeconds(1) > DateTime.UtcNow)
                        {
                            Log("We just scooped MTU: waiting a few brief seconds");
                            return true;
                        }

                        if (DirectEve.Interval(3000)) _getMTUInSpace.Approach();
                        Log($"MTU is still in space waiting. IsMTUInspace [{_getMTUInSpace != null}] DistanceToMTU[{(_getMTUInSpace != null ? _getMTUInSpace.Distance : -1)}] _lastMTUScoop.AddSeconds(3) > DateTime.UtcNow  [{_lastMTUScoop.AddSeconds(3) > DateTime.UtcNow}] _lastMTULaunch.AddSeconds(7) > DateTime.UtcNow) [{_lastMTULaunch.AddSeconds(7) > DateTime.UtcNow}]");
                        if (11 > _mtuScoopAttempts)
                        {
                            return true;
                        }

                        Log($"_mtuScoopAttempts [" + _mtuScoopAttempts + "] - not waiting on MTU!! continue on so that we dont get stuck");
                        return true;
                    }

                    if (_getMTUInSpace == null && _lastMTULaunch.AddSeconds(7) > DateTime.UtcNow)
                    {
                        Log("We just launched MTU: waiting several seconds");
                        return true;
                    }
                }

                if (ESCache.Instance.ActiveShip.IsPod)
                {
                    foreach (DirectFleetMember individualFleetMember in ESCache.Instance.DirectEve.FleetMembers.Where(i => i.Name != ESCache.Instance.DirectEve.Me.Name))
                    {
                        if (DebugConfig.DebugFleetMgr) Log("foreach (DirectFleetMember individualFleetMember in ESCache.Instance.DirectEve.FleetMembers.Where(i => i.Name != ESCache.Instance.DirectEve.Me.Name))");
                        if (individualFleetMember.Entity != null)
                        {
                            if (DirectEve.Interval(7000)) Log("[" + individualFleetMember.Name + "] waiting for all fleet members to jump first! I am in a pod, it is fragile!");
                            return true;
                        }

                        continue;
                    }
                }

                // we looted all, move to the gate and jump
                if (DirectEve.Interval(2000, 4000))
                    Log("Done. Move to the gate and jump!");

                var gate = _endGate ?? _midGate;
                if (gate.Distance > 2300)
                {
                    if (gate.Distance > 10000 && AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                    {
                        Log($"gate.MoveToViaAStar();");
                        DirectEntity.MoveToViaAStar(2000, distanceToTarget: 0, forceRecreatePath: forceRecreatePath, dest: gate._directEntity.DirectAbsolutePosition,
                            ignoreAbyssEntities: true,
                            ignoreTrackingPolyons: ESCache.Instance.IgnoreTrackingPolyons_ImprovesTracking_HelpsToHitSmallThings,
                            ignoreAutomataPylon: ESCache.Instance.IgnoreAutomataPylon_Kills_NPCDrones_OurDrones_And_Missiles,
                            ignoreWideAreaAutomataPylon: true,
                            ignoreBioClouds: ESCache.Instance.IgnoreBioClouds_Blue_4xSignatureRadius,
                            ignoreFilaCouds: ESCache.Instance.IgnoreFilaClouds_Orange_ShieldBoostCapacitorUsePenalty,
                            ignoreTachClouds: ESCache.Instance.IgnoreTachClouds_White_4xSpeedBoost);
                        return true;
                    }

                    Log($"gate.Approach();");
                    gate.Approach();
                    return true;
                }

                RecallDrones();

                if (DirectEve.ActiveDrones.Any())
                {
                    var abandonDrones = false;

                    if (DirectEve.Interval(2000, 4000))
                        Log($"Waiting for drones to return.");

                    if (DirectEve.ActiveDrones.All(d => d.DroneState == (int)Drones.DroneState.Returning || d.DroneState == (int)Drones.DroneState.Returning2) && DirectEve.ActiveDrones.Any(e => 1000 > e.Velocity && 20000 > e.Distance))
                    {
                        if (DirectEve.Interval(5000)) DirectEve.ExecuteCommand(DirectCmd.CmdDronesReturnToBay);
                    }

                    // when all drones are returning for too long, we might abandon them! TODO: any better alternative?
                    if (DirectEve.ActiveDrones.All(d => d.DroneState == (int)Drones.DroneState.Returning || d.DroneState == (int)Drones.DroneState.Returning2))
                    {
                        if (DirectEve.ActiveDrones.Any(e => e.Distance < 22000) || _abyssRemainingSeconds < 9 && _nextGate.Distance <= 3500)
                        {
                            if (_startedToRecallDronesWhileNoTargetsLeft == null)
                            {
                                _startedToRecallDronesWhileNoTargetsLeft = DateTime.UtcNow;
                                Log($"Time started recalling drones [{_startedToRecallDronesWhileNoTargetsLeft}]");
                            }

                            var secondsSince = (DateTime.UtcNow - _startedToRecallDronesWhileNoTargetsLeft.Value).TotalSeconds;

                            if (DirectEve.Interval(1000, 2500) && secondsSince >= 15)
                            {
                                if (PlayNotificationSounds) Util.PlayNoticeSound();
                                if (PlayNotificationSounds) Util.PlayNoticeSound();
                                string msg = $"Notification!: Recalling drones since: [{Math.Round(secondsSince, 0)}] seconds.";
                                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                                Log(msg);

                                if (DirectEve.Interval(1000, 2500) && secondsSince >= 25)
                                {
                                    if (abandonDrones)
                                    {
                                        if (DirectEve.Interval(3000, 4500))
                                        {
                                            Log($"We are abandoning any light drones. They took too long to recover. Lost drones: ");
                                            foreach (var lightDrone in DirectEve.ActiveDrones.Where(x => x.Volume == 5))
                                            {
                                                Log($"Drone [" + lightDrone.TypeName + "] TypeId [" + lightDrone.TypeId + "] Velocity [" +  Math.Round(lightDrone.Velocity, 0) + "]");
                                                //lightDrone.AbandonAllWrecks();
                                                //fix me
                                                continue;
                                            }
                                        }
                                    }
                                }
                            }

                        }

                    }

                    Log($"Notification!: Waiting on drones...");
                    return true;
                }

                if (DirectEve.Interval(2000, 4000))
                    Log($"gate within jump range: [" + gate.Nearest1KDistance + "]m");

                if (!HealthCheck() && ((60 * 5) > Time.Instance.SecondsSinceLastSessionChange))
                {
                    NavigateOnGrid.LogMyCurrentHealth("Activate HealthCheck Failed: Waiting");
                    return true;
                }

                if (boolDoWeHaveTime && ESCache.Instance.EveAccount.UseFleetMgr && ESCache.Instance.DirectEve.FleetMembers.Count() > 1)
                {
                    if (_midGate != null && _endGate == null) //if you are in the last room no need to wait for fleet members, just jump out.
                    {
                        foreach (DirectFleetMember individualFleetMember in ESCache.Instance.DirectEve.FleetMembers.Where(i => i.Name != ESCache.Instance.DirectEve.Me.Name))
                        {
                            if (DebugConfig.DebugFleetMgr) Log("foreach (DirectFleetMember individualFleetMember in ESCache.Instance.DirectEve.FleetMembers.Where(i => i.Name != ESCache.Instance.DirectEve.Me.Name))");
                            if (individualFleetMember.Entity != null && (double)individualFleetMember.Entity.DistanceTo(ESCache.Instance.AbyssalGate._directEntity) > 4000)
                            {
                                if (individualFleetMember.Entity.IsPod)
                                    continue;

                                Log("[" + individualFleetMember.Name + "] is not on the gate with us! waiting.");
                                return true;
                            }

                            if (individualFleetMember.Entity != null && individualFleetMember.Entity.Name == ESCache.Instance.EveAccount.LeaderCharacterName)
                            {
                                if (individualFleetMember.Entity.IsPod)
                                    continue;

                                Log("[" + individualFleetMember.Name + "] waiting for Leader to jump first!");
                                return true;
                            }

                            continue;
                        }

                        Log("[" + ESCache.Instance.DirectEve.FleetMembers.Count + "] fleet members are here on grid! continue");
                    }
                }

                // at this point we are close to the gate and can jump based on mid/end gate
                if (_isInLastRoom)
                {
                    if (DirectEve.Interval(2500, 3000))
                    {
                        if (DirectSession.LastSessionChange.AddMilliseconds(4500) < DateTime.UtcNow)
                        {
                            try
                            {
                                //UpdateStatistics();
                                Log($"Collapse in: {(_abyssRemainingSeconds > 60 ? (int)_abyssRemainingSeconds / 60 : 0)}m{Math.Round(_abyssRemainingSeconds % 60, 0)}s");
                                Log($"Stage1:      {(_stage1SecondsSpent > 60 ? (int)_stage1SecondsSpent / 60 : 0)}m{Math.Round(_stage1SecondsSpent % 60, 0)}s: [" + _stage1DetectSpawn + "] wasted [" + Math.Round(stage1SecondsWastedAfterLastNPCWasKilled, 1) + "] sec to get to gate");
                                Log($"Stage2:      {(_stage2SecondsSpent > 60 ? (int)_stage2SecondsSpent / 60 : 0)}m{Math.Round(_stage2SecondsSpent % 60, 0)}s: [" + _stage2DetectSpawn + "] wasted [" + Math.Round(_stage2SecondsWastedAfterLastNPCWasKilled, 1) + "] sec to get to gate");
                                Log($"Stage3:      {(_stage3SecondsSpent > 60 ? (int)_stage3SecondsSpent / 60 : 0)}m{Math.Round(_stage3SecondsSpent % 60, 0)}s: [" + _stage3DetectSpawn + "] wasted [" + Math.Round(_stage3SecondsWastedAfterLastNPCWasKilled, 1) + "] sec to get to gate");

                                if (DirectEve.Interval(45000)) AbyssalSpawnStatistics();
                            }
                            catch (Exception ex)
                            {
                                Log("Exception [" + ex + "]");
                            }

                            LogAbyssState();
                            Log($"ActivateAccelerationGate");
                            gate.ActivateAccelerationGate();
                            AreWeResumingFromACrash = false;
                            try
                            {
                                _abyssStatEntry.MTULost = _getMTUInBay == null;
                            }
                            catch (Exception) { }

                            WriteStatsToDB();
                            _abyssStatEntry = null;
                            _attemptsToJumpMidgate = 0;
                            _dronesRecalledWhileWeStillhaveTargets = 0;
                            myAbyssalState = AbyssalState.InvulnPhaseAfterAbyssExit;
                            Traveler.Destination = null;
                            State.CurrentTravelerState = TravelerState.Idle;
                            if (DirectEve.Interval(30000)) DirectEventManager.NewEvent(new DirectEvent(DirectEvents.KEEP_ALIVE, "Keep alive."));
                            return true;
                        }
                        else Log("Waiting for session change to finish: waiting a few seconds");
                    }
                }
                else
                {
                    if (DirectEve.Interval(2500, 3000))
                    {
                        if (DirectSession.LastSessionChange.AddMilliseconds(4500) < DateTime.UtcNow)
                        {
                            try
                            {
                                Log($"Collapse in: {(_abyssRemainingSeconds > 60 ? (int)_abyssRemainingSeconds / 60 : 0)}m{Math.Round(_abyssRemainingSeconds % 60, 0)}s");
                                if (CurrentAbyssalStage == AbyssalStage.Stage1)
                                {
                                    Log($"Stage1: {(_stage1SecondsSpent > 60 ? (int)_stage1SecondsSpent / 60 : 0)}m{Math.Round(_stage1SecondsSpent % 60, 0)}s: [" + _stage1DetectSpawn + "] We wasted [" + Math.Round(stage1SecondsWastedAfterLastNPCWasKilled, 0) + "] seconds after NPCs were dead");
                                }
                                if (CurrentAbyssalStage == AbyssalStage.Stage2)
                                {
                                    Log($"Stage2: {(_stage2SecondsSpent > 60 ? (int)_stage2SecondsSpent / 60 : 0)}m{Math.Round(_stage2SecondsSpent % 60, 0)}s: [" + _stage2DetectSpawn + "] We wasted [" + Math.Round(_stage2SecondsWastedAfterLastNPCWasKilled, 0) + "] seconds after NPCs were dead");
                                }

                                if (DirectEve.Interval(45000)) AbyssalSpawnStatistics();

                            }
                            catch (Exception ex)
                            {
                                Log("Exception [" + ex + "]");
                            }

                            LogAbyssState();
                            if (gate.ActivateAccelerationGate())
                            {
                                if (CurrentAbyssalStage == AbyssalStage.Stage1) _attemptsToJumpMidgate++;
                            }

                            if (DirectEve.Interval(30000)) DirectEventManager.NewEvent(new DirectEvent(DirectEvents.KEEP_ALIVE, "Keep alive."));
                            return true;
                        }
                        else Log("Waiting for session change to finish: waiting a few seconds");
                    }
                }
            }

            if (DebugConfig.DebugNavigateOnGrid) Log("HandleTasksOnceRatsAreDead: False");
            return false;
        }

        public static bool AbyssalSpawnStatisticsLog { get; set; } = true;

        public bool AbyssalSpawnStatistics(bool force = false)
        {
            try
            {
                Directory.CreateDirectory(Statistics.AbyssalSpawnStatisticsPath);
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return false;
            }

            try
            {
                if (AbyssalSpawnStatisticsLog || force)
                {
                    // can we somehow get the X,Y,Z coord? If we could we could use this info to build some kind of grid layout...,or at least know the distances between all the NPCs... thus be able to infer which NPCs were in which 'groups'
                    string objectline = "------ \r\n";
                    File.AppendAllText(Statistics.AbyssalSpawnStatisticsFile, objectline);

                    if (CurrentAbyssalStage == AbyssalStage.Stage1)
                    {
                        objectline = $"Tier [" + AbyssalTier + "] Stage1: {(_stage1SecondsSpent > 60 ? (int)_stage1SecondsSpent / 60 : 0)}m{Math.Round(_stage1SecondsSpent % 60, 0)}s: _stage1DetectSpawn [" + _stage1DetectSpawn + "] DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] wasted [" + Math.Round(stage1SecondsWastedAfterLastNPCWasKilled, 1) + "] sec to get to gate\r\n";
                        File.AppendAllText(Statistics.AbyssalSpawnStatisticsFile, objectline);
                        objectline = "MyHealth S[" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 0) + "%] A[" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 0) + "%] H[" + Math.Round(ESCache.Instance.ActiveShip.StructurePercentage, 0) + "%] C[" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) + "%] MyVelocity [" + Math.Round(ESCache.Instance.ActiveShip.Entity.Velocity, 0) + "m/s] FollowEntityName [" + ESCache.Instance.ActiveShip.Entity.FollowEntityName + "]\r\n";
                        File.AppendAllText(Statistics.AbyssalSpawnStatisticsFile, objectline);
                        objectline = "IsCacheDead [" + IsCacheDead + "] IsCacheWreckEmpty [" + IsCacheWreckEmpty + "] boolDoWeHaveTime [" + boolDoWeHaveTime + "]\r\n";
                        File.AppendAllText(Statistics.AbyssalSpawnStatisticsFile, objectline);
                        objectline = $"Collapse in: {(_abyssRemainingSeconds > 60 ? (int)_abyssRemainingSeconds / 60 : 0)}m{Math.Round(_abyssRemainingSeconds % 60, 0)}s - [" + DateTime.UtcNow.ToLongDateString() + "][" + DateTime.UtcNow.ToLongTimeString() + "]\r\n";
                        File.AppendAllText(Statistics.AbyssalSpawnStatisticsFile, objectline);
                    }
                    else if (CurrentAbyssalStage == AbyssalStage.Stage2)
                    {
                        objectline = $"Tier [" + AbyssalTier + "] Stage2:      {(_stage2SecondsSpent > 60 ? (int)_stage2SecondsSpent / 60 : 0)}m{Math.Round(_stage2SecondsSpent % 60, 0)}s: _stage2DetectSpawn [" + _stage2DetectSpawn + "] DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] wasted [" + Math.Round(_stage2SecondsWastedAfterLastNPCWasKilled, 1) + "] sec to get to gate\r\n";
                        File.AppendAllText(Statistics.AbyssalSpawnStatisticsFile, objectline);
                        objectline = "MyHealth S[" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 0) + "%] A[" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 0) + "%] H[" + Math.Round(ESCache.Instance.ActiveShip.StructurePercentage, 0) + "%] C[" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) + "%] MyVelocity [" + Math.Round(ESCache.Instance.ActiveShip.Entity.Velocity, 0) + "m/s] FollowEntityName [" + ESCache.Instance.ActiveShip.Entity.FollowEntityName + "]\r\n";
                        File.AppendAllText(Statistics.AbyssalSpawnStatisticsFile, objectline);
                        objectline = "IsCacheDead [" + IsCacheDead + "] IsCacheWreckEmpty [" + IsCacheWreckEmpty + "] boolDoWeHaveTime [" + boolDoWeHaveTime + "]\r\n";
                        File.AppendAllText(Statistics.AbyssalSpawnStatisticsFile, objectline);
                        objectline = $"Collapse in: {(_abyssRemainingSeconds > 60 ? (int)_abyssRemainingSeconds / 60 : 0)}m{Math.Round(_abyssRemainingSeconds % 60, 0)}s - [" + DateTime.UtcNow.ToLongDateString() + "][" + DateTime.UtcNow.ToLongTimeString() + "]\r\n";
                        File.AppendAllText(Statistics.AbyssalSpawnStatisticsFile, objectline);
                    }
                    else if (CurrentAbyssalStage == AbyssalStage.Stage3)
                    {
                        objectline = $"Tier [" + AbyssalTier + "] Stage3:      {(_stage3SecondsSpent > 60 ? (int)_stage3SecondsSpent / 60 : 0)}m{Math.Round(_stage3SecondsSpent % 60, 0)}s: _stage3DetectSpawn [" + _stage3DetectSpawn + "] DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] wasted [" + Math.Round(_stage3SecondsWastedAfterLastNPCWasKilled, 1) + "] sec to get to gate\r\n";
                        File.AppendAllText(Statistics.AbyssalSpawnStatisticsFile, objectline);
                        objectline = "MyHealth S[" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 0) + "%] A[" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 0) + "%] H[" + Math.Round(ESCache.Instance.ActiveShip.StructurePercentage, 0) + "%] C[" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) + "%] MyVelocity [" + Math.Round(ESCache.Instance.ActiveShip.Entity.Velocity, 0) + "m/s] FollowEntityName [" + ESCache.Instance.ActiveShip.Entity.FollowEntityName + "]\r\n";
                        File.AppendAllText(Statistics.AbyssalSpawnStatisticsFile, objectline);
                        objectline = "IsCacheDead [" + IsCacheDead + "] IsCacheWreckEmpty [" + IsCacheWreckEmpty + "] boolDoWeHaveTime [" + boolDoWeHaveTime + "]\r\n";
                        File.AppendAllText(Statistics.AbyssalSpawnStatisticsFile, objectline);
                        objectline = $"Collapse in: {(_abyssRemainingSeconds > 60 ? (int)_abyssRemainingSeconds / 60 : 0)}m{Math.Round(_abyssRemainingSeconds % 60, 0)}s - [" + DateTime.UtcNow.ToLongDateString() + "][" + DateTime.UtcNow.ToLongTimeString() + "]\r\n";
                        File.AppendAllText(Statistics.AbyssalSpawnStatisticsFile, objectline);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return false;
            }
        }


        internal void HandleNotifications()
        {
            try
            {
                if (!DirectEve.HasFrameChanged())
                    return;

                if (ESCache.Instance.InStation)
                    return;

                if (!ESCache.Instance.InSpace)
                    return;

                if (ESCache.Instance.ActiveShip.Entity.IsLocatedWithinFilamentCloud && DebugConfig.Alert_IsLocatedWithinFilamentCloud)
                {
                    if (DirectEve.Interval(10000))
                    {
                        string msg = "Notification!: We are in a Filament Cloud! Effects: -40% Shield Booster Duration: DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] Capacitor  ["  + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) + "]";
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                        Log(msg);
                        if (PlayNotificationSounds) Util.PlayNoticeSound();
                    }
                }
                else if (ESCache.Instance.ActiveShip.Entity.IsLocatedWithinBioluminescenceCloud && DebugConfig.Alert_IsLocatedWithinBioluminescenceCloud)
                {
                    if (DirectEve.Interval(10000))
                    {
                        string msg = "Notification!: We are in a Bioluminescence Cloud! Effects: 3 x Signature Radius: DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]"  + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) + "]";
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                        Log(msg);
                        if (PlayNotificationSounds) Util.PlayNoticeSound();
                    }
                }
                else if (ESCache.Instance.ActiveShip.Entity.IsLocatedWithinSpeedCloud && DebugConfig.Alert_IsLocatedWithinCausticCloud)
                {
                    if (DirectEve.Interval(10000))
                    {
                        string msg = "Notification!: We are in a Tachyon Cloud!: Effects: 3 x Speed! DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] myVelocity [" + Math.Round(ESCache.Instance.ActiveShip.Entity.Velocity, 0) + "]";
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                        Log(msg);
                        if (PlayNotificationSounds) Util.PlayNoticeSound();
                    }
                }
                else if (ESCache.Instance.InAbyssalDeadspace && GetCurrentStageStageSeconds / 60 > 4.5 && _getMTUInSpace == null && (ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty && i.IsAbyssalCacheWreck) || _trigItemCaches.Any()) && _nextGate.Distance > 40000)
                {
                    if (DirectEve.Interval(10000))
                    {
                        string msg = "Notification!: Current Stage Time [" + Math.Round(GetCurrentStageStageSeconds / 60, 1) + " min] > 4.5min: DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] _nextGate.Distance [" + _nextGate.Distance + "]";
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                        Log(msg);
                        if (PlayNotificationSounds) Util.PlayNoticeSound();
                    }
                }
                else if (ESCache.Instance.InAbyssalDeadspace && GetCurrentStageStageSeconds / 60 > 5 && _getMTUInBay != null && _getMTUInSpace == null && (ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty && i.IsAbyssalCacheWreck) || _trigItemCaches.Any()))
                {
                    if (DirectEve.Interval(10000))
                    {
                        string msg = "Notification!: Current Stage Time [" + Math.Round(GetCurrentStageStageSeconds / 60, 1) + " min] > 5min: DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] No MTU in Space yet and we have loot to pickup: No ding!";
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                        Log(msg);
                        //if (PlayNotificationSounds) Util.PlayNoticeSound();
                        //if (PlayNotificationSounds) Util.PlayNoticeSound();
                    }
                }
                else if (ESCache.Instance.InAbyssalDeadspace && GetCurrentStageStageSeconds / 60 > 5.5)
                {
                    if (DirectEve.Interval(10000))
                    {
                        string msg = "Notification!: Current Stage Time [" + Math.Round(GetCurrentStageStageSeconds / 60, 1) + " min] > 5.5 min: DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] Ding!";
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                        Log(msg);
                        if (PlayNotificationSounds) Util.PlayNoticeSound();
                    }
                }
                else if (ESCache.Instance.InAbyssalDeadspace && GetCurrentStageStageSeconds / 60 > 6.5)
                {
                    if (DirectEve.Interval(10000))
                    {
                        string msg = "Notification!: Current Stage Time [" + Math.Round(GetCurrentStageStageSeconds / 60, 1) + " min] > 6.5 min: DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] Ding!";
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                        Log(msg);
                        if (PlayNotificationSounds) Util.PlayNoticeSound();
                        if (PlayNotificationSounds) Util.PlayNoticeSound();
                    }
                }
                else if (ESCache.Instance.InAbyssalDeadspace && CurrentStageRemainingSecondsWithoutPreviousStages < 0)
                {
                    if (DirectEve.Interval(10000))
                    {
                        string msg = "Notification!: CurrentStageRemainingSecondsWithoutPreviousStages [" + Math.Round(CurrentStageRemainingSecondsWithoutPreviousStages, 0) + "] is less than 0: DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]";
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                        Log(msg);
                        //if (PlayNotificationSounds) Util.PlayNoticeSound();
                    }
                }
                else if (ESCache.Instance.InAbyssalDeadspace && ESCache.Instance.ActiveShip.ArmorPercentage < 50 && !ESCache.Instance.ActiveShip.IsArmorTanked)
                {
                    if (DirectEve.Interval(10000))
                    {
                        string msg = "Notification!: ArmorPercentage [" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 0) + "] is less than 50%: DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]";
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                        Log(msg);
                        //if (PlayNotificationSounds) Util.PlayNoticeSound();
                    }
                }
                else if (ESCache.Instance.InAbyssalDeadspace && ESCache.Instance.ActiveShip.CapacitorPercentage < 20 && (ESCache.Instance.ActiveShip.IsArmorTanked || ESCache.Instance.ActiveShip.IsShieldTanked))
                {
                    if (DirectEve.Interval(10000))
                    {
                        string msg = "Notification!: CapacitorPercentage [" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) + "] is less than 20%: DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]";
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                        Log(msg);
                        if (PlayNotificationSounds) Util.PlayNoticeSound();
                    }
                }
                else if (ESCache.Instance.EntitiesOnGrid.Any(i => i._directEntity.IsLargeTachCloud && 40000 > i.Distance) && DebugConfig.Alert_IsCloseToLargeTachCloud)
                {
                    if (DirectEve.Interval(15000))
                    {
                        string msg = "Notification!: IsLargeTachCloud [ True ] @ [" + ESCache.Instance.EntitiesOnGrid.FirstOrDefault(i => i._directEntity.IsLargeTachCloud).Nearest1KDistance + "] is inside 40k";
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                        Log(msg);
                        if (PlayNotificationSounds) Util.PlayNoticeSound();
                    }
                }
                else if (ESCache.Instance.EntitiesOnGrid.Any(i => i._directEntity.IsMedTachCloud && 35000 > i.Distance) && DebugConfig.Alert_IsCloseToMediumTachCloud)
                {
                    if (DirectEve.Interval(10000))
                    {
                        string msg = "Notification!: IsMedTachCloud [ True ] @ [" + ESCache.Instance.EntitiesOnGrid.FirstOrDefault(i => i._directEntity.IsMedTachCloud).Nearest1KDistance + "] is inside 35k";
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                        Log(msg);
                        if (PlayNotificationSounds) Util.PlayNoticeSound();
                    }
                }
                else if (ESCache.Instance.EntitiesOnGrid.Any(i => i._directEntity.IsSmallTachCloud && 15000 > i.Distance) && DebugConfig.Alert_IsCloseToSmallTachCloud)
                {
                    if (DirectEve.Interval(10000))
                    {
                        string msg = "Notification!: IsSmallTachCloud[True] @ [" + ESCache.Instance.EntitiesOnGrid.FirstOrDefault(i => i._directEntity.IsSmallTachCloud).Nearest1KDistance + "] is inside 15k";
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                        Log(msg);
                        if (PlayNotificationSounds) Util.PlayNoticeSound();
                    }
                }
                else if (ESCache.Instance.Modules.Any(i => i.IsMicroWarpDrive) && (ESCache.Instance.ActiveShip.IsScrambled || ESCache.Instance.ActiveShip.IsWebbed) && DebugConfig.Alert_MWDIsWarpScrambled)
                {
                    if (DirectEve.Interval(10000))
                    {
                        string msg = "Notification!: ActiveShip MWD: IsScrambled [" + ESCache.Instance.ActiveShip.IsScrambled + "]  IsWebbed [" + ESCache.Instance.ActiveShip.IsWebbed + "] myVelocity [" + Math.Round(ESCache.Instance.ActiveShip.Entity.Velocity, 0) + "]";
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                        Log(msg);
                        if (PlayNotificationSounds) Util.PlayNoticeSound();
                    }
                }
                else if (ESCache.Instance.Modules.Any(i => i._module.IsAfterburner) && ESCache.Instance.ActiveShip.IsWebbed && DebugConfig.Alert_ABIsWarpScrambled && 600 > ESCache.Instance.ActiveShip.Entity.Velocity)
                {
                    if (DirectEve.Interval(10000))
                    {
                        string msg = "Notification!: ActiveShip AB: IsWebbed [" + ESCache.Instance.ActiveShip.IsWebbed + "] myVelocity [" + Math.Round(ESCache.Instance.ActiveShip.Entity.Velocity, 0) + "]";
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                        Log(msg);
                        if (PlayNotificationSounds) Util.PlayNoticeSound();
                    }
                }
                else if (ESCache.Instance.InAbyssalDeadspace && ESCache.Instance.Weapons.Any(i => i.Charge == null) && Combat.PotentialCombatTargets.Any(i => !i.IsLargeCollidable) && ESCache.Instance.ActiveShip.TypeId != (int)TypeID.Ishtar && ESCache.Instance.ActiveShip.TypeId != (int)TypeID.Gila)
                {
                    if (DirectEve.Interval(10000))
                    {
                        string msg = "Notification!: Weapon(s) with no ammo!";
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                        Log(msg);
                        if (PlayNotificationSounds) Util.PlayNoticeSound();
                    }
                }
                else if (ESCache.Instance.InAbyssalDeadspace && _singleRoomAbyssal)
                {
                    if (DirectEve.Interval(10000))
                    {
                        string msg = "Notification: Single Room Abyssal!";
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                        Log(msg);
                        //if (PlayNotificationSounds) Util.PlayNoticeSound();
                    }
                }
                else if (ESCache.Instance.InAbyssalDeadspace && Combat.PotentialCombatTargets.Any(i => i.GroupId != (int)Group.AbyssalDeadspaceDroneEntities) && Combat.PotentialCombatTargets.Count > 10 && Combat.PotentialCombatTargets.Any(x => x.BracketType != BracketType.Large_Collidable_Structure) && ESCache.Instance.EntitiesOnGrid.Any(i => i._directEntity.IsMediumRangeAutomataPylon && 40000 > i.Distance) && DebugConfig.Alert_IsCloseToAutomataPylon && ESCache.Instance.Weapons.Any(i => i.IsMissileLauncher && !i._module.IsVortonProjector) && ESCache.Instance.ActiveShip.TypeId != (int)TypeID.Ishtar)
                {
                    if (DirectEve.Interval(10000))
                    {
                        string msg = "Notification: Medium-Range Deviant Automata Suppressor [" + ESCache.Instance.EntitiesOnGrid.FirstOrDefault(i => i._directEntity.IsMediumRangeAutomataPylon).Nearest1KDistance + "k] is within 40k!";
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                        Log(msg);
                        if (PlayNotificationSounds) Util.PlayNoticeSound();
                    }
                }
                else if (ESCache.Instance.InAbyssalDeadspace && Combat.PotentialCombatTargets.Any(i => i.GroupId != (int)Group.AbyssalDeadspaceDroneEntities) && Combat.PotentialCombatTargets.Count > 10 && Combat.PotentialCombatTargets.Any(x => x.BracketType != BracketType.Large_Collidable_Structure) && ESCache.Instance.EntitiesOnGrid.Any(i => i._directEntity.IsShortRangeAutomataPylon && 15000 > i.Distance) && DebugConfig.Alert_IsCloseToAutomataPylon && ESCache.Instance.Weapons.Any(i => i.IsMissileLauncher && !i._module.IsVortonProjector) && ESCache.Instance.ActiveShip.TypeId != (int)TypeID.Ishtar)
                {
                    if (DirectEve.Interval(10000))
                    {
                        string msg = "Notification: Short-Range Deviant Automata Suppressor [" + ESCache.Instance.EntitiesOnGrid.FirstOrDefault(i => i._directEntity.IsShortRangeAutomataPylon).Nearest1KDistance + "k] is within 15k!";
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                        Log(msg);
                        if (PlayNotificationSounds) Util.PlayNoticeSound();
                    }
                }
                else if (ESCache.Instance.InAbyssalDeadspace && AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser && i.Name.ToLower().Contains("Lucifer Cynabal".ToLower())))
                    {
                        if (AbyssalTier >= 3 && (ESCache.Instance.ActiveShip.IsAssaultShip || (ESCache.Instance.ActiveShip.IsFrigate && !ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)) && DirectEve.Interval(5000))
                        {
                            string msg = "Notification: DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] !.!";
                            DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                            Log(msg);
                            if (PlayNotificationSounds) Util.PlayNoticeSound();
                        }
                    }
                }
                else if (ESCache.Instance.InAbyssalDeadspace && AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser && i.Name.ToLower().Contains("Blastlance".ToLower())) >= 5)
                    {
                        if ((ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsFrigate) && DirectEve.Interval(5000))
                        {
                            string msg = "Notification: DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] 5+ lance !.!";
                            DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                            Log(msg);
                            if (PlayNotificationSounds) Util.PlayNoticeSound();
                        }
                    }
                }
                else if (ESCache.Instance.InAbyssalDeadspace && AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LeshakBSSpawn)
                {
                    /**
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                    {
                        if (DirectEve.Interval(5000))
                        {
                            string msg = "Notification!: DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] LeshakBSSpawn";
                            DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                            Log(msg);
                            if (PlayNotificationSounds) Util.PlayNoticeSound();
                        }
                    }
                    **/
                }
                else if (!ESCache.Instance.InAbyssalDeadspace && ESCache.Instance.InWormHoleSpace && ESCache.Instance.DockableLocations.All(i => !i.IsOnGridWithMe) && ESCache.Instance.EntitiesNotSelf.Any(i => i.IsPlayer))
                {
                    if ((ESCache.Instance.EntitiesNotSelf.Any(i => i.IsPlayer && ESCache.Instance.ActiveShip.TypeId != i.TypeId)) && DirectEve.Interval(5000))
                    {
                        string msg = "Notification: Player found [" + ESCache.Instance.EntitiesNotSelf.FirstOrDefault(i => i.IsPlayer && ESCache.Instance.ActiveShip.TypeId != i.TypeId).TypeName + "]";
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                        Log(msg);
                        if (PlayNotificationSounds)
                        {
                            Util.PlayNoticeSound();
                            Util.PlayNoticeSound();
                            Util.PlayNoticeSound();
                            Util.PlayNoticeSound();
                            Util.PlayNoticeSound();
                        }
                    }
                }
                else if (!boolDoWeHaveTime)
                {
                    if (DirectEve.Interval(1000, 2500))
                    {
                        if (PlayNotificationSounds) Util.PlayNoticeSound();
                        string msg = $"Notification!: boolDoWeHaveTime [" + boolDoWeHaveTime + "] CurrentAbyssalStage [" + CurrentAbyssalStage + "] AbyssalRemainingSeconds [" + ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds + "]";
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                        Log(msg);
                    }
                }
                else if (ESCache.Instance.InAbyssalDeadspace && ESCache.Instance.Entities.Any(i => i.IsAbyssalBioAdaptiveCache && !i.IsTarget && i.IsReadyToTarget) && boolShouldBioAdaptiveCacheBeDead)
                {
                    if (AbyssalTier >= 4)
                    {
                        if (4 > Combat.PotentialCombatTargets.Count())
                        {
                            if (DirectEve.Interval(1000, 2500))
                            {
                                if (PlayNotificationSounds) Util.PlayNoticeSound();
                                string msg = $"Notification!: AbyssalBioAdaptiveCache is not locked! CurrentAbyssalStage [" + CurrentAbyssalStage + "] AbyssalRemainingSeconds [" + ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds + "]";
                                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                                Log(msg);
                            }
                        }
                    }
                    else
                    {
                        if (1 > Combat.PotentialCombatTargets.Count())
                        {
                            if (DirectEve.Interval(1000, 2500))
                            {
                                if (PlayNotificationSounds) Util.PlayNoticeSound();
                                string msg = $"Notification!: AbyssalBioAdaptiveCache is not locked! CurrentAbyssalStage [" + CurrentAbyssalStage + "] AbyssalRemainingSeconds [" + ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds + "]";
                                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                                Log(msg);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
        }

        internal bool boolMoveOnGridWatchdog
        {
            get
            {
                if (_nextGate.Distance >= (double)Distances.GateActivationRange)
                {
                    if (Time.Instance.SecondsSinceLastSessionChange > 20)
                    {
                        if (_lastMoveOnGrid.AddSeconds(3) < DateTime.UtcNow)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        internal void ManageNOS()
        {
            try
            {
                if (ESCache.Instance.InWarp)
                    return;

                if (!Combat.PotentialCombatTargets.Any(i => i.IsTarget && i.BracketType != BracketType.Large_Collidable_Structure))
                    return;

                if (!ESCache.Instance.Modules.Any(x => x.GroupId == (int)Group.NOS))
                    return;

                foreach (var NOSModule in ESCache.Instance.Modules.Where(x => x.GroupId == (int)Group.NOS && x.IsActive && !x.IsInLimboState))
                {
                    if (NOSModule.TargetId != 0)
                    {
                        foreach (var PCT in Combat.PotentialCombatTargets.Where(i => i.IsTarget && i.BracketType != BracketType.Large_Collidable_Structure).OrderBy(y => y.Distance))
                        {
                            if (Combat.PotentialCombatTargets.Any(z => z.Id == NOSModule.TargetId))
                            {
                                if (NOSModule.TargetId != PCT.Id && Combat.PotentialCombatTargets.FirstOrDefault(z => z.Id == NOSModule.TargetId).Distance > PCT.Distance + 2000)
                                {
                                    Log("Deactivating [" + NOSModule.TypeName + "] to NOS a closer target");
                                    NOSModule.Click();
                                    return;
                                }
                            }

                            if (NOSModule.TargetId == PCT.Id && PCT.Distance > Math.Max(8000,(NOSModule.OptimalRange + (NOSModule.FallOff * 2))))
                            {
                                Log("Deactivating [" + NOSModule.TypeName + "] [" + PCT.Name + "] is too far away at [" + PCT.Nearest1KDistance + "k] going [ " + Math.Round(PCT.Velocity, 0) + " m/s]");
                                NOSModule.Click();
                                return;
                            }
                        }
                    }
                }

                foreach (var NOSModule in ESCache.Instance.Modules.Where(x => x.GroupId == (int)Group.NOS && !x.IsActive && !x.IsInLimboState))
                {
                    foreach (var PCT in Combat.PotentialCombatTargets.Where(i => i.IsTarget && i.BracketType != BracketType.Large_Collidable_Structure).OrderBy(y => y.Distance))
                    {
                        if (Math.Max(6000, (NOSModule.OptimalRange + NOSModule.FallOff)) > PCT.Distance)
                        {
                            Log("Activating [" + NOSModule.TypeName + "] on [" + PCT.Name + "] @ [" + PCT.Nearest1KDistance + "k] going [ " + Math.Round(PCT.Velocity, 0) + " m/s]");
                            NOSModule.Activate(PCT);
                            return;
                        }
                    }
                }

                return;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return;
            }
        }

        internal void ActivateRemoteRepsIfNeeded()
        {
            if (DebugConfig.DebugRemoteReps) Log("ActivateRemoteRepsIfNeeded()");
            if (ESCache.Instance.Modules.Any(x => (x.GroupId == (int)Group.RemoteShieldRepairer ||
                                                                                       x.GroupId == (int)Group.RemoteArmorRepairer ||
                                                                                       x.GroupId == (int)Group.RemoteEnergyTransfer ||
                                                                                       x.GroupId == (int)Group.AncillaryRemoteShieldBooster ||
                                                                                       x.GroupId == (int)Group.AncillaryRemoteArmorRepairer)))
            {
                if (ESCache.Instance.Modules.Any(x => (x.GroupId == (int)Group.RemoteShieldRepairer ||
                                                                                       x.GroupId == (int)Group.RemoteArmorRepairer ||
                                                                                       x.GroupId == (int)Group.RemoteEnergyTransfer ||
                                                                                       x.GroupId == (int)Group.AncillaryRemoteShieldBooster ||
                                                                                       x.GroupId == (int)Group.AncillaryRemoteArmorRepairer) && !x.IsActive && !x.IsInLimboState))
                {

                    foreach (var RemoteRepairModule in ESCache.Instance.Modules.Where(x => (x.GroupId == (int)Group.RemoteShieldRepairer ||
                                                                                           x.GroupId == (int)Group.RemoteArmorRepairer ||
                                                                                           x.GroupId == (int)Group.RemoteEnergyTransfer ||
                                                                                           x.GroupId == (int)Group.AncillaryRemoteShieldBooster ||
                                                                                           x.GroupId == (int)Group.AncillaryRemoteArmorRepairer) && !x.IsActive && !x.IsInLimboState))
                    {
                        foreach (var FleetMemberToHelp in ESCache.Instance.Targets.Where(i => !Combat.PotentialCombatTargets.Any(x => x.Id == i.Id) && i.GroupId == (int)Group.AssaultShip && .75 > i.ShieldPct).OrderBy(y => y.Distance))
                        {
                            Log("FleetMemberToHelp [" + FleetMemberToHelp.Name + "] at [" + FleetMemberToHelp.Nearest1KDistance + "k] S[" + Math.Round(FleetMemberToHelp.ShieldPct * 100, 0) + "%] is less than 75%.  Velocity [ " + Math.Round(FleetMemberToHelp.Velocity, 0) + " m/s]");
                            if (25000 > FleetMemberToHelp.Distance)
                            {
                                Log("Activating RemoteRep [" + RemoteRepairModule.TypeName + "] on [" + FleetMemberToHelp.Name + "] @ [" + FleetMemberToHelp.Nearest1KDistance + "k] going [ " + Math.Round(FleetMemberToHelp.Velocity, 0) + " m/s]");
                                RemoteRepairModule.Activate(FleetMemberToHelp);
                                return;
                            }

                            Log("We are too far away to Apply our remote reps! @ [" + Math.Round(FleetMemberToHelp.Distance / 1000, 0) + " k]");
                            return;
                        }

                        if (DebugConfig.DebugRemoteReps) Log("ActivateRemoteRepsIfNeeded() - no FleetMembers need remote reps right now");
                        continue;
                    }

                    if (DebugConfig.DebugRemoteReps) Log("ActivateRemoteRepsIfNeeded() - no remote reps needed");
                    return;
                }

                if (DebugConfig.DebugRemoteReps) Log("ActivateRemoteRepsIfNeeded() - all remote reps are active");
                return;
            }

            if (DebugConfig.DebugRemoteReps) Log("ActivateRemoteRepsIfNeeded() - no remote reps fitted?");
            return;
        }

        internal void DeactivateRemoteRepsIfNeeded()
        {
            try
            {
                if (ESCache.Instance.Modules.Any(x => (x.GroupId == (int)Group.RemoteShieldRepairer ||
                                                                           x.GroupId == (int)Group.RemoteArmorRepairer ||
                                                                           x.GroupId == (int)Group.RemoteEnergyTransfer ||
                                                                           x.GroupId == (int)Group.AncillaryRemoteShieldBooster ||
                                                                           x.GroupId == (int)Group.AncillaryRemoteArmorRepairer)))
                {

                    if (!ESCache.Instance.Modules.Any(x => (x.GroupId == (int)Group.RemoteShieldRepairer ||
                                                                                       x.GroupId == (int)Group.RemoteArmorRepairer ||
                                                                                       x.GroupId == (int)Group.RemoteEnergyTransfer ||
                                                                                       x.GroupId == (int)Group.AncillaryRemoteShieldBooster ||
                                                                                       x.GroupId == (int)Group.AncillaryRemoteArmorRepairer) && x.IsActive && !x.IsInLimboState))
                    {
                        if (DebugConfig.DebugRemoteReps) Log("ActivateRemoteRepsIfNeeded() - no Remote Reps are active yet");
                        return;
                    }

                    foreach (var RemoteAssistanceModule in ESCache.Instance.Modules.Where(x => (x.GroupId == (int)Group.RemoteShieldRepairer ||
                                                                                       x.GroupId == (int)Group.RemoteArmorRepairer ||
                                                                                       x.GroupId == (int)Group.RemoteEnergyTransfer ||
                                                                                       x.GroupId == (int)Group.AncillaryRemoteShieldBooster ||
                                                                                       x.GroupId == (int)Group.AncillaryRemoteArmorRepairer) && x.IsActive && !x.IsInLimboState))
                    {
                        if ((RemoteAssistanceModule.GroupId == (int)Group.AncillaryRemoteShieldBooster || RemoteAssistanceModule.GroupId == (int)Group.AncillaryRemoteShieldBooster) && RemoteAssistanceModule.Charge == null)
                        {
                            if (RemoteAssistanceModule._module.CanBeReloaded && !RemoteAssistanceModule.IsReloadingAmmo && !RemoteAssistanceModule.IsInLimboState)
                            {
                                var thisStackOfCapBoosters = ESCache.Instance.CheckCargoForItem(_capBoosterTypeId, 1);
                                if (thisStackOfCapBoosters != null)
                                {
                                    RemoteAssistanceModule._module.ReloadAmmo(thisStackOfCapBoosters);
                                    continue;
                                }
                            }
                        }

                        if (RemoteAssistanceModule.GroupId == (int)Group.AncillaryRemoteShieldBooster && RemoteAssistanceModule.Charge == null)
                            continue;

                        if (RemoteAssistanceModule.TargetId != 0)
                        {
                            foreach (var FleetMemberToHelp in ESCache.Instance.Targets.Where(i => !Combat.PotentialCombatTargets.Any(x => x.Id == i.Id) && i.GroupId == (int)Group.AssaultShip).OrderBy(y => y.Distance))
                            {
                                if (ESCache.Instance.Targets.Any(z => z.Id == RemoteAssistanceModule.TargetId))
                                {
                                    if (RemoteAssistanceModule.TargetId != FleetMemberToHelp.Id && ESCache.Instance.Targets.FirstOrDefault(z => z.Id == RemoteAssistanceModule.TargetId).Distance > FleetMemberToHelp.Distance + 2000)
                                    {
                                        Log("Deactivating [" + RemoteAssistanceModule.TypeName + "] target was out of range");
                                        RemoteAssistanceModule.Click();
                                        return;
                                    }
                                }

                                if (RemoteAssistanceModule.TargetId == FleetMemberToHelp.Id && FleetMemberToHelp.Distance > 30000)
                                {
                                    Log("Deactivating [" + RemoteAssistanceModule.TypeName + "] [" + FleetMemberToHelp.Name + "] is too far away at [" + FleetMemberToHelp.Nearest1KDistance + "k] going [ " + Math.Round(FleetMemberToHelp.Velocity, 0) + " m/s]");
                                    RemoteAssistanceModule.Click();
                                    return;
                                }

                                if (RemoteAssistanceModule.TargetId == FleetMemberToHelp.Id && FleetMemberToHelp.ShieldPct > .80)
                                {
                                    Log("Deactivating [" + RemoteAssistanceModule.TypeName + "] [" + FleetMemberToHelp.Name + "] is Healthy: S[" + FleetMemberToHelp.ShieldPct + "%] > .80");
                                    RemoteAssistanceModule.Click();
                                    return;
                                }

                                if (RemoteAssistanceModule.TargetId == FleetMemberToHelp.Id && .45 > ESCache.Instance.ActiveShip.CapacitorPercentage)
                                {
                                    Log("Deactivating [" + RemoteAssistanceModule.TypeName + "] [" + FleetMemberToHelp.Name + "] My Capacitor is starting to get low: .45 > [" + ESCache.Instance.ActiveShip.CapacitorPercentage + "]");
                                    RemoteAssistanceModule.Click();
                                    return;
                                }
                            }

                            if (DebugConfig.DebugRemoteReps) Log("ActivateRemoteRepsIfNeeded() - FleetMembers still need reps - repairing");
                            continue;
                        }

                        if (DebugConfig.DebugRemoteReps) Log("ActivateRemoteRepsIfNeeded() - FleetMembers still need reps - repairing");
                        continue;
                    }

                    if (DebugConfig.DebugRemoteReps) Log("ActivateRemoteRepsIfNeeded() ---");
                    return;
                }

                if (DebugConfig.DebugRemoteReps) Log("ActivateRemoteRepsIfNeeded() - no remote reps fitted?");
                return;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return;
            }
        }

        internal void RepairRemoteRepsIfNeeded()
        {
            try
            {
                if (ESCache.Instance.Modules.Any(x => (x.GroupId == (int)Group.RemoteShieldRepairer ||
                                                                       x.GroupId == (int)Group.RemoteArmorRepairer ||
                                                                       x.GroupId == (int)Group.RemoteEnergyTransfer ||
                                                                       x.GroupId == (int)Group.AncillaryRemoteShieldBooster ||
                                                                       x.GroupId == (int)Group.AncillaryRemoteArmorRepairer) && !x.IsActive && !x.IsInLimboState))
                {

                    foreach (var RemoteRepairModule in ESCache.Instance.Modules.Where(x => (x.GroupId == (int)Group.RemoteShieldRepairer ||
                                                                                           x.GroupId == (int)Group.RemoteArmorRepairer ||
                                                                                           x.GroupId == (int)Group.RemoteEnergyTransfer ||
                                                                                           x.GroupId == (int)Group.AncillaryRemoteShieldBooster ||
                                                                                           x.GroupId == (int)Group.AncillaryRemoteArmorRepairer) && !x.IsActive && !x.IsInLimboState))
                    {

                        if (RemoteRepairModule._module.IsBeingRepaired && !SafeToTurnOffRemoteRepsSoWeCanRepairThem)
                        {
                            if (DebugConfig.DebugRemoteReps) Log($"RemoteRepairModule [{RemoteRepairModule.TypeName}] IsBeingRepaired [{RemoteRepairModule._module.IsBeingRepaired}] && (!SafeToTurnOffRemoteRepSoWeCanRepairIt || _anyOverheat)).");
                            // cancel repair
                            if (RemoteRepairModule._module.CancelRepair())
                            {
                                Log($"Canceling repair: [{RemoteRepairModule.TypeName}]");
                            }
                        }

                        if (RemoteRepairModule._module.IsInLimboState)
                        {
                            if (DebugConfig.DebugRemoteReps) Log($"RemoteRepairModule [{RemoteRepairModule.TypeName}] IsInLimboState!");
                            continue;
                        }

                        if (!RemoteRepairModule._module.IsBeingRepaired && SafeToTurnOffRemoteRepsSoWeCanRepairThem)
                        {
                            if (DebugConfig.DebugRemoteReps) Log($"RemoteRepairModule [{RemoteRepairModule.TypeName}] IsBeingRepaired [{RemoteRepairModule._module.IsBeingRepaired}] && (SafeToTurnOffPropModSoWeCanRepairIt || !_anyOverheat))!!");
                            var shipsCargo = ESCache.Instance.DirectEve.GetShipsCargo();

                            if (shipsCargo != null)
                            {
                                if (shipsCargo.Items.Any(i => i.TypeId == _naniteRepairPasteTypeId))
                                {
                                    if (!ShouldWeRepair)
                                        continue;

                                    // repair
                                    if (RemoteRepairModule._module.Repair())
                                    {
                                        Log($"Repairing: [{RemoteRepairModule.TypeName}].");
                                        return;
                                    }
                                }

                                if (!shipsCargo.Items.Any(i => i.TypeId == _naniteRepairPasteTypeId))
                                {
                                    DirectEve.IntervalLog(60000, 60000, "No nanite repair paste found in cargo, can't repair.");
                                }
                            }
                        }

                        continue;
                    }

                    if (DebugConfig.DebugRemoteReps) Log("ActivateRemoteRepsIfNeeded() - no remote reps needed");
                    return;
                }


            }
            catch (Exception)
            {

                throw;
            }
        }

        internal void ManageRemoteReps()
        {
            try
            {
                if (ESCache.Instance.InWarp)
                    return;

                if (!Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))
                    return;

                if (!ESCache.Instance.Targets.Any(i => i.GroupId == (int)Group.AssaultShip))
                    return;

                if (!ESCache.Instance.Modules.Any(x => (x.GroupId == (int)Group.RemoteShieldRepairer ||
                                                        x.GroupId == (int)Group.RemoteArmorRepairer ||
                                                        x.GroupId == (int)Group.RemoteEnergyTransfer ||
                                                        x.GroupId == (int)Group.AncillaryRemoteShieldBooster ||
                                                        x.GroupId == (int)Group.AncillaryRemoteArmorRepairer)))
                    return;


                DeactivateRemoteRepsIfNeeded();

                RepairRemoteRepsIfNeeded();

                ActivateRemoteRepsIfNeeded();

                return;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return;
            }
        }

        internal void AbyssalClear()
        {
            try
            {
                if (!DirectEve.HasFrameChanged())
                    return;

                RecordDetectSpawnInfoForCurrentAbyssalStage();
                LogAbyssState();

                forceRecreatePath = false;

                // force recreate a-star path every [29,30] seconds
                if (DirectEve.Interval(29000, 38000))
                    forceRecreatePath = true;

                if (!DirectEve.Me.IsInAbyssalSpace())
                {
                    if (DirectEve.Interval(3000, 4000))
                        Log($"We are not in abyss space. Starting over again.");
                    myAbyssalState = AbyssalState.Start;
                    return;
                }

                //PrintDroneEstimatedKillTimePerStage();

                if (ESCache.Instance.ActiveShip != null && !ESCache.Instance.ActiveShip.IsPod)
                {
                    var sc = ESCache.Instance.DirectEve.GetShipsCargo();
                    if (sc != null)
                    {
                        if (sc.CanBeStacked)
                        {
                            Log($"Stacking ships cargo container.");
                            sc.StackAll();
                        }
                    }
                }

                CaptureHP();
                UpdateStrategy();
                // play notification sounds

                try
                {
                    Util.MeasureTime(() =>
                    {
                        ManageStats();
                    }, true, "ManageStats");
                }
                catch (Exception ex)
                {

                    if (DirectEve.Interval(5000))
                        Log($"ManageStats exception: {ex}");
                }

                // update window labels
                try
                {
                    Util.MeasureTime(() =>
                    {
                        if (DirectEve.Interval(4500, 4900))
                        {
                            var currentAbysStage = CurrentAbyssalStage;
                            var currentStageRemainingSecondsWithoutPreviousStages = (int)CurrentStageRemainingSecondsWithoutPreviousStages;
                            var estimatedClearGrid = GetEstimatedStageRemainingTimeToClearGrid() ?? 0;
                            var secondsNeededToRetrieveWrecks = (int)_secondsNeededToRetrieveWrecks;
                            var abyssRemainingSeconds = (int)_abyssRemainingSeconds;
                            var secondsNeededToReachTheGate = (int)_secondsNeededToReachTheGate;
                            var ignoreAbyss = IgnoreAbyssEntities;
                            Task.Run(() =>
                            {
                                try
                                {
                                    UpdateStageLabel(currentAbysStage);
                                    UpdateStageRemainingSecondsLabel(currentStageRemainingSecondsWithoutPreviousStages);
                                    UpdateStageKillEstimatedTime(estimatedClearGrid);
                                    UpdateStageEHPValues(_currentStageMaximumEhp, _currentStageCurrentEhp);
                                    UpdateWreckLootTime(secondsNeededToRetrieveWrecks);
                                    UpdateAbyssTotalTime(abyssRemainingSeconds);
                                    UpdateTimeNeededToGetToTheGate(secondsNeededToReachTheGate);
                                    UpdateIgnoreAbyssEntities(ignoreAbyss);
                                    //UpdateCurrentTargetEHPValues(currentTargetTotalArmor + currentTargetTotalShield + currentTargetTotalStructure, currentTargetArmor + currentTargetShield + currentTargetStructure);
                                }
                                catch (Exception ex)
                                {
                                    Log(ex.ToString());
                                    Console.WriteLine(ex.ToString());
                                }

                            });
                        }
                    }, true, "UpdateUI");
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                }

                if (boolMoveOnGridWatchdog)
                {
                    Log("MoveOnGridWatchdog");
                    MoveOnGrid();
                }

                ManagePropMod();

                ManageDrones();

                ManageTargetLocks();

                ManageWeapons();

                ManageNOS();

                ManageRemoteReps();

                HandleMTU();

                if (DirectEve.Interval(15000, 20000))
                {
                    try
                    {
                        var mtuDistane = _getMTUInSpace != null ? _getMTUInSpace.Distance : -1;
                        var mtuGateDistance = _getMTUInSpace != null ? _getMTUInSpace._directEntity.DirectAbsolutePosition.GetDistance(_nextGate._directEntity.DirectAbsolutePosition) : -1;
                        Log($"Collapse In: {(_abyssRemainingSeconds > 60 ? (int)_abyssRemainingSeconds / 60 : 0)}m{Math.Round(_abyssRemainingSeconds % 60, 0)}s. CurrentStageRemainingSecondsWithoutPreviousStages [{Math.Round(CurrentStageRemainingSecondsWithoutPreviousStages, 0)}] Stage [{CurrentAbyssalStage}] SecondsNeededToReachTheGate [{Math.Round(_secondsNeededToReachTheGate, 0)}] DistanceToGate [{Math.Round(_nextGate.Distance, 0)}] ActiveShip -> MTU [{Math.Round(mtuDistane, 0)}] MTU -> Gate  [{Math.Round(mtuGateDistance, 0)}] MaxVelocity [{Math.Round(ESCache.Instance.ActiveShip.MaxVelocity, 0)}] EnemiesRemaining [{Combat.PotentialCombatTargets.Count}] IsSingleRoomAbyss [{_singleRoomAbyssal}]");
                        Log($"NeutsOnGridCount [{_neutsOnGridCount}]");
                    }
                    catch (Exception ex)
                    {
                        Log("Exception [" + ex + "]");
                    }
                }

                if (DebugConfig.DebugNavigateOnGrid) LogMyHealth();
                else if (DirectEve.Interval(15000)) LogMyHealth();

                if (DebugConfig.DebugNavigateOnGrid) LogMyPositionInSpace();
                else if (DirectEve.Interval(20000)) LogMyPositionInSpace();

                if (DebugConfig.DebugNavigateOnGrid) LogCurrentTargetHealth();
                else if (DirectEve.Interval(10000)) LogCurrentTargetHealth();

                if (DebugConfig.DebugActivateWeapons) LogMyWeapons();
                else if (DirectEve.Interval(15000)) LogMyWeapons();

                if (DebugConfig.DebugDrones) LogMyDrones();
                else if (DirectEve.Interval(15000)) LogMyDrones();

                if (!DirectEve.Session.IsInSpace)
                {
                    //this also happens when doing a session change, dont assume you are in a pod ffs =)
                    return;
                }

                LogNextGateState();

                //not in use any more?
                if (HandleTierFiveSingleRoomAbyss()) return;

                if (DebugConfig.DebugNavigateOnGrid) Log("MoveOnGrid");
                MoveOnGrid(); // keep this at the end
                return;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return;
            }
        }
    }
}
