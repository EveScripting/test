extern alias SC;

using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Configuration;
using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Actions;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Questor.States;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;

namespace EVESharpCore.Questor.Combat
{
    public static partial class Combat
    {
        private static bool? _activateBastion { get; set; }

        private static bool ActivateBastion_
        {
            get
            {
                try
                {
                    if (_activateBastion == null)
                    {
                        if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.IsImmobile) return false;

                        if (Time.Instance.NextBastionAction > DateTime.UtcNow)
                        {
                            if (DebugConfig.DebugActivateBastion) Log.WriteLine("activateBastion: waiting for NextBastionAction");
                            _activateBastion = false;
                            return _activateBastion ?? false;
                        }

                        if (PotentialCombatTargets.Any(e => e.Distance < MaxRange && e.IsPlayer && e.IsTargetedBy && e.IsAttacking) &&
                        !ESCache.Instance.InWormHoleSpace)
                        {
                            Log.WriteLine("activateBastion: We are being attacked by a player we should activate bastion");
                            _activateBastion = true;
                            return _activateBastion ?? false;
                        }

                        if (ESCache.Instance.MyFleetMembersAsEntities.Any(i => i.IsApproaching || i.IsOrbiting))
                        {
                            Log.WriteLine("activateBastion: We are approaching [" + ESCache.Instance.MyFleetMembersAsEntities.FirstOrDefault(i => i.IsApproaching || i.IsOrbiting).Name + "] canceling _activateBastion");
                            _activateBastion = false;
                            return _activateBastion ?? false;
                        }

                        if (PotentialCombatTargets.Any(e => !e.IsSentry && e.Distance < MaxRange) &&
                            PotentialCombatTargets.All(j => !j.IsPlayer) &&
                            ESCache.Instance.InWormHoleSpace && !ESCache.Instance.Paused &&
                            ESCache.Instance.MyShipEntity != null && 15 > ESCache.Instance.MyShipEntity.Velocity &&
                            ESCache.Instance.Wrecks.All(i => !i.Name.ToLower().Contains("Drifter".ToLower())))
                        {
                            Log.WriteLine("activateBastion: We have targets to shoot. We should activate siege");
                            _activateBastion = true;
                            return _activateBastion ?? false;
                        }

                        _activateBastion = false;
                        return _activateBastion ?? false;
                    }

                    return _activateBastion ?? false;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        private static bool? _deActivateBastion;

        private static bool DeActivateBastion
        {
            get
            {
                try
                {
                    if (_deActivateBastion == null)
                    {
                        if (ESCache.Instance.ActiveShip != null && !ESCache.Instance.ActiveShip.IsImmobile) return false;

                        if (!PotentialCombatTargets.Any(e => e.Distance < MaxRange && (e.IsTarget || e.IsTargeting)))
                        {
                            if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.GroupId == (int)Group.Marauder)
                            {
                                if (ActionControl.DeactivateIfNothingTargetedWithinRange)
                                {
                                    Log.WriteLine("NextBastionModeDeactivate set to 2 sec ago: We have no targets in range and DeactivateIfNothingTargetedWithinRange [ " + ActionControl.DeactivateIfNothingTargetedWithinRange + " ]");
                                    _deActivateBastion = true;
                                    return _deActivateBastion ?? false;
                                }

                                if (DateTime.UtcNow > Time.Instance.NextBastionModeDeactivate)
                                {
                                    Log.WriteLine("NextBastionModeDeactivate set to 2 sec ago: We have no targets in range and DeactivateIfNothingTargetedWithinRange [ " + ActionControl.DeactivateIfNothingTargetedWithinRange + " ]");
                                    _deActivateBastion = true;
                                    return _deActivateBastion ?? false;
                                }
                            }

                            if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.GroupId == (int)Group.Dreadnaught && ESCache.Instance.Wrecks.Any(i => i.Name.Contains("Drifter")))
                            {
                                Log.WriteLine("NextBastionModeDeactivate set to 2 sec ago: We have no targets in range and the Drifter Battleship is dead");
                                _deActivateBastion = true;
                                return _deActivateBastion ?? false;
                            }
                        }

                        if ((!ESCache.Instance.InWormHoleSpace && State.CurrentPanicState == PanicState.Panicking) || State.CurrentPanicState == PanicState.StartPanicking)
                        {
                            if (DebugConfig.DebugActivateBastion)
                                Log.WriteLine("NextBastionModeDeactivate set to 2 sec ago: We are in panic!");
                            _deActivateBastion = true;
                            return _deActivateBastion ?? false;
                        }

                        if (!ESCache.Instance.InWormHoleSpace && ESCache.Instance.InMission && State.CurrentCombatMissionCtrlState != ActionControlState.ExecutePocketActions)
                        {
                            Log.WriteLine("BastionAndSiegeModules: Deactivating BastionAndSiegeModules: In a Mission but CurrentCombatMissionCtrlState is [" + State.CurrentCombatMissionCtrlState + "]");
                            _deActivateBastion = true;
                            return _deActivateBastion ?? false;
                        }

                        if (State.CurrentPanicState == PanicState.Panic)
                        {
                            Log.WriteLine("BastionAndSiegeModules: Deactivating BastionAndSiegeModules: In a Mission: CurrentPanicState is [" + State.CurrentPanicState + "]");
                            _deActivateBastion = true;
                            return _deActivateBastion ?? false;
                        }

                        if (State.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.GotoBase)
                        {
                            Log.WriteLine("BastionAndSiegeModules: Deactivating BastionAndSiegeModules: In a Mission: CurrentCombatMissionBehaviorState is [" + State.CurrentCombatMissionBehaviorState + "]");
                            _deActivateBastion = true;
                            return _deActivateBastion ?? false;
                        }

                        return false;
                    }

                    return _deActivateBastion ?? false;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        #region Methods

        private static bool ActivateBastionIfNeeded(ModuleCache bastionMod, bool forceActivateBastion, DateTime SiegeUntilDateTime)
        {
            if (!bastionMod.IsActive && (ActivateBastion_ || forceActivateBastion) && !ESCache.Instance.MyShipEntity.HasInitiatedWarp)
            {
                if (bastionMod.Click())
                {
                    Log.WriteLine("Activating bastion [" + _weaponNumber + "]");
                    if (ESCache.Instance.ActiveShip != null)
                    {
                        if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Marauder)
                           Time.Instance.NextBastionAction = DateTime.UtcNow.AddSeconds(ESCache.Instance.RandomNumber(25, 35));

                        if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Dreadnaught)
                            Time.Instance.NextBastionAction = DateTime.UtcNow.AddSeconds(ESCache.Instance.RandomNumber(180, 210));
                    }

                    Time.Instance.NextBastionModeDeactivate = SiegeUntilDateTime;
                    return true;
                }

                return false;
            }

            return false;
        }

        private static bool DeactivateBastionIfNeeded(ModuleCache bastionMod)
        {
            if (!bastionMod.IsActive) return false;

            if (bastionMod.IsDeactivating)
            {
                Time.Instance.LastBastionDeactivate = DateTime.UtcNow;
                Time.Instance.NextBastionAction = DateTime.UtcNow.AddSeconds(ESCache.Instance.RandomNumber(15, 20));
                return true;
            }

            if (!bastionMod.IsDeactivating)
            {
                if (DeActivateBastion)
                {
                    Log.WriteLine("BastionAndSiegeModules: Deactivating BastionAndSiegeModules: time");
                    if (bastionMod.Click())
                    {
                        Time.Instance.LastBastionDeactivate = DateTime.UtcNow;
                        Time.Instance.NextBastionAction = DateTime.UtcNow.AddSeconds(ESCache.Instance.RandomNumber(5, 8));
                        return true;
                    }

                    return true;
                }
            }

            if (DebugConfig.DebugActivateBastion) Log.WriteLine("IsActive [" + bastionMod.IsActive + "] bastionMod.IsDeactivating [" + bastionMod.IsDeactivating + "]");
            return true;
        }

        public static bool ActivateBastion(DateTime SiegeUntilDateTime, bool forceActivateBastion = false)
        {
            if (ESCache.Instance.ActiveShip.IsDread && !Combat.AllowUsingSeigeModules)
                return true;

            if (ESCache.Instance.ActiveShip.IsMarauder && !Combat.AllowUsingBastionModules)
                return true;

            List<ModuleCache> bastionModules = null;
            bastionModules = ESCache.Instance.Modules.Where(m => m.GroupId == (int)Group.BastionAndSiegeModules && m.IsOnline).ToList();
            if (bastionModules == null || bastionModules.Count == 0) return true;
            if (bastionModules.Any(i => i.IsActive && i.IsDeactivating)) return true;
            ModuleCache bastionMod = bastionModules.FirstOrDefault();

            if (Time.Instance.LastInWarp.AddSeconds(10) > DateTime.UtcNow && ESCache.Instance.InWormHoleSpace && !ESCache.Instance.InsidePosForceField)
                if (ESCache.Instance.MyShipEntity.Velocity > 20)
                    ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdStopShip);

            if (DebugConfig.DebugActivateBastion)
                Log.WriteLine("BastionModule: IsActive [" + bastionMod.IsActive + "] IsDeactivating [" +
                                bastionMod.IsDeactivating +
                                "] InLimboState [" + bastionMod.InLimboState + "] Duration [" + bastionMod.Duration + "] TypeId [" + bastionMod.TypeId +
                                "]");

            if (DeactivateBastionIfNeeded(bastionMod)) return false;
            ActivateBastionIfNeeded(bastionMod, forceActivateBastion, SiegeUntilDateTime);
            return true;
        }

        private static void ActivateEcm()
        {
            try
            {
                //order by ships I am not shooting first, useful for Anomic Team missions
                foreach (EntityCache target in PotentialCombatTargets.OrderBy(i => i.IsPrimaryWeaponPriorityTarget).Where(i => ESCache.Instance.ListofEntitiesToEcm.Contains(i.Id)))
                {
                    Log.WriteLine("ECM: target [" + target.Name + "] at [" + Math.Round(target.Distance / 1000, 0) + "k] IsTarget [" + target.IsTarget + "] IsTargeting [" + target.IsTargeting + "] IsTargetedBy [" + target.IsTargetedBy + "]");
                    if (target.IsEwarImmune)
                    {
                        if (DebugConfig.DebugKillTargets)
                            Log.WriteLine("ECM: Ignoring ECM Activation on [" + target.Name + "]IsEwarImmune[" + target.IsEwarImmune + "]");
                        return;
                    }

                    if (!target.IsTarget && !target.IsTargeting && MaxRange > target.Distance && ESCache.Instance.MaxLockedTargets > ESCache.Instance.TotalTargetsAndTargeting.Count)
                    {
                        if (DebugConfig.DebugKillTargets)
                            Log.WriteLine("ECM: Attempt to lock target [" + target.Name + "]");
                        if (target.LockTarget("ECM"))
                            Log.WriteLine("ECM: Locking [" + target.Name + "]");
                        return;
                    }

                    if (DateTime.UtcNow < Time.Instance.LastEcmAttempt.AddSeconds(8))
                    {
                        if (DebugConfig.DebugKillTargets)
                            Log.WriteLine("Waiting to activate ECM: LastEcmAttempt [ " + Time.Instance.LastEcmAttempt.ToLongTimeString() + "]");
                        return;
                    }

                    List<ModuleCache> EcmModules = ESCache.Instance.Modules.Where(m => m.GroupId == (int)Group.Ecm).ToList();

                    _weaponNumber = 0;
                    foreach (ModuleCache ecm in EcmModules)
                    {
                        if (Time.Instance.LastActivatedTimeStamp != null && Time.Instance.LastActivatedTimeStamp.ContainsKey(ecm.ItemId))
                            if (Time.Instance.LastActivatedTimeStamp[ecm.ItemId].AddMilliseconds(Time.Instance.PainterDelay_milliseconds) > DateTime.UtcNow)
                            {
                                if (DebugConfig.DebugKillTargets)
                                    Log.WriteLine("ECM: LastActivated was too soon, skipping");
                                continue;
                            }

                        _weaponNumber++;

                        if (ecm.IsActive)
                        {
                            if (ecm.TargetId != target.Id)
                            {
                                if (DebugConfig.DebugKillTargets)
                                    Log.WriteLine("ECM: Wrong Target: deactivating");
                                ecm.Click();
                                return;
                            }

                            continue;
                        }

                        //if (!target.IsTargetedBy)
                        //{
                        //    return;
                        //}

                        if (ecm.IsDeactivating)
                        {
                            if (DebugConfig.DebugKillTargets)
                                Log.WriteLine("ECM: ECM is deactivating, next module...");
                            continue;
                        }

                        if (CanActivate(ecm, target, false))
                        {
                            if (ecm.Activate(target))
                            {
                                Log.WriteLine("Activating [" + ecm.TypeName + "][" + _weaponNumber + "] on [" + target.Name + "][" + target.MaskedId +
                                              "][" +
                                              Math.Round(target.Distance / 1000, 0) + "k away]");
                                Time.Instance.LastEcmAttempt = DateTime.UtcNow;
                                return;
                            }

                            if (DebugConfig.DebugKillTargets)
                                Log.WriteLine("ECM: Activate failed...");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static void ActivateNos(EntityCache target)
        {
            List<ModuleCache> noses = ESCache.Instance.Modules.Where(m => m.GroupId == (int)Group.NOS).ToList();

            _weaponNumber = 0;
            foreach (ModuleCache nos in noses)
            {
                _weaponNumber++;

                if (Time.Instance.LastActivatedTimeStamp != null && Time.Instance.LastActivatedTimeStamp.ContainsKey(nos.ItemId))
                    if (Time.Instance.LastActivatedTimeStamp[nos.ItemId].AddMilliseconds(Time.Instance.NosDelay_milliseconds) > DateTime.UtcNow)
                        continue;

                if (nos.IsActive)
                {
                    if (nos.TargetId != target.Id && ((nos.OptimalRange + nos.FalloffEffectiveness) > target.Distance))
                    {
                        if (nos.Click()) return;

                        return;
                    }

                    continue;
                }

                if (nos.IsDeactivating)
                    continue;

                //if (target.Distance >= nos.PowerTransferRange)
                //    continue;

                EntityCache nosTarget = null;
                if (PotentialCombatTargets.Any(i => i.IsTarget && !i.IsEwarImmune && !i.IsBadIdea && ((nos.OptimalRange + nos.FalloffEffectiveness) >= i.Distance)))
                {
                    nosTarget = PotentialCombatTargets.Where(i => i.IsTarget && !i.IsEwarImmune && !i.IsBadIdea && ((nos.OptimalRange + nos.FalloffEffectiveness) >= i.Distance))
                        .OrderByDescending(i => i.Id == target.Id)
                        .ThenByDescending(i => i.IsNPCBattleship)
                        .ThenByDescending(i => i.IsNPCBattlecruiser)
                        .ThenByDescending(i => i.IsNPCCruiser)
                        .ThenByDescending(i => i.IsNPCFrigate)
                        .ThenByDescending(i => i.IsNPCBattlecruiser)
                        .FirstOrDefault();

                    if (CanActivate(nos, nosTarget, false))
                    {
                        if (nos.Activate(nosTarget))
                        {
                            Log.WriteLine("Activating [" + nos.TypeName + "][" + _weaponNumber + "] on [" + nosTarget.Name + "][" + nosTarget.MaskedId + "][" +
                                          Math.Round(nosTarget.Distance / 1000, 0) + "k away]");
                            return;
                        }

                        continue;
                    }

                    Log.WriteLine("Cannot Activate [" + nos.TypeName + "][" + _weaponNumber + "] on [" + nosTarget.Name + "][" + nosTarget.MaskedId + "][" +
                                  Math.Round(nosTarget.Distance / 1000, 0) + "k away]");
                }
            }
        }

        private static void ActivateSensorDampeners(EntityCache target)
        {
            if (target.IsEwarImmune)
            {
                if (DebugConfig.DebugKillTargets)
                    Log.WriteLine("Ignoring SensorDamps Activation on [" + target.Name + "]IsEwarImmune[" + target.IsEwarImmune + "]");
                return;
            }

            List<ModuleCache> sensorDampeners = ESCache.Instance.Modules.Where(m => m.GroupId == (int)Group.SensorDampener).ToList();

            _weaponNumber = 0;
            foreach (ModuleCache sensorDampener in sensorDampeners)
            {
                if (Time.Instance.LastActivatedTimeStamp != null && Time.Instance.LastActivatedTimeStamp.ContainsKey(sensorDampener.ItemId))
                    if (Time.Instance.LastActivatedTimeStamp[sensorDampener.ItemId].AddMilliseconds(Time.Instance.PainterDelay_milliseconds) > DateTime.UtcNow)
                        continue;

                _weaponNumber++;

                if (sensorDampener.IsActive)
                {
                    if (sensorDampener.TargetId != target.Id)
                    {
                        if (sensorDampener.Click()) return;
                        return;
                    }

                    continue;
                }

                if (sensorDampener.IsDeactivating)
                    continue;

                if (CanActivate(sensorDampener, target, false))
                    if (sensorDampener.Activate(target))
                    {
                        Log.WriteLine("Activating [" + sensorDampener.TypeName + "][" + _weaponNumber + "] on [" + target.Name + "][" + target.MaskedId +
                                      "][" +
                                      Math.Round(target.Distance / 1000, 0) + "k away]");
                        return;
                    }
            }
        }

        private static void ActivateStasisWeb(EntityCache target)
        {
            if (target.IsEwarImmune)
            {
                if (DebugConfig.DebugKillTargets)
                    Log.WriteLine("Ignoring StasisWeb Activation on [" + target.Name + "]IsEwarImmune[" + target.IsEwarImmune + "]");
                return;
            }

            List<ModuleCache> webs = ESCache.Instance.Modules.Where(m => m.GroupId == (int)Group.StasisWeb || m.GroupId == (int)Group.StasisGrappler).ToList();

            if (DebugConfig.DebugActivateWeapons)
            {
                Log.WriteLine("Start: Listing All Fitted Modules - ");
                int modulenum = 0;
                foreach (ModuleCache _module in ESCache.Instance.Modules)
                {
                    modulenum++;
                    Log.WriteLine("[" + modulenum + "][" + _module.TypeName + "] typeId [" + _module.TypeId + "] groupId [" + _module.GroupId + "]");
                }
                Log.WriteLine("Done: Listing All Fitted Modules - ");
            }

            _weaponNumber = 0;
            foreach (ModuleCache web in webs)
            {
                _weaponNumber++;

                if (Time.Instance.LastActivatedTimeStamp != null && Time.Instance.LastActivatedTimeStamp.ContainsKey(web.ItemId))
                    if (Time.Instance.LastActivatedTimeStamp[web.ItemId].AddMilliseconds(Time.Instance.WebDelay_milliseconds) > DateTime.UtcNow)
                        continue;

                if (web.IsActive)
                {
                    if (web.TargetId != target.Id)
                    {
                        if (web.Click()) return;

                        return;
                    }

                    continue;
                }

                if (web.IsDeactivating)
                    continue;

                if (target.Distance >= web.OptimalRange && web.GroupId == (int)Group.StasisWeb)
                    continue;

                if (target.Distance >= 25000 && web.GroupId == (int)Group.StasisGrappler)
                    continue;

                if (CanActivate(web, target, false))
                    if (web.Activate(target))
                    {
                        Log.WriteLine("Activating [" + web.TypeName + "][" + _weaponNumber + "] on [" + target.Name + "][" + target.MaskedId + "]");
                        return;
                    }
            }
        }

        private static void ActivateTargetPainters(EntityCache target)
        {
            if (target.IsEwarImmune)
            {
                if (DebugConfig.DebugTargetPainters)
                    Log.WriteLine("Ignoring TargetPainter Activation on [" + target.Name + "]IsEwarImmune[" + target.IsEwarImmune + "]");

                return;
            }

            List<ModuleCache> targetPainters = ESCache.Instance.Modules.Where(m => m.GroupId == (int)Group.TargetPainter).ToList();

            _weaponNumber = 0;
            foreach (ModuleCache painter in targetPainters)
            {
                if (Time.Instance.LastActivatedTimeStamp != null && Time.Instance.LastActivatedTimeStamp.ContainsKey(painter.ItemId))
                    if (Time.Instance.LastActivatedTimeStamp[painter.ItemId].AddMilliseconds(Time.Instance.PainterDelay_milliseconds) > DateTime.UtcNow)
                    {
                        if (DebugConfig.DebugTargetPainters)
                            Log.WriteLine("TargetPainter [" + painter.ItemId + "] was activated recently, skipping");

                        continue;
                    }

                _weaponNumber++;

                if (painter.IsActive)
                {
                    if (DebugConfig.DebugTargetPainters)
                        Log.WriteLine("if (painter.IsActive)");

                    if (painter.TargetId != target.Id)
                    {
                        if (DebugConfig.DebugTargetPainters)
                            Log.WriteLine("if (painter.TargetId != target.Id)");

                        if (painter.Click()) return;

                        return;
                    }

                    continue;
                }

                if (painter.IsDeactivating)
                {
                    if (DebugConfig.DebugTargetPainters)
                        Log.WriteLine("if (painter.IsDeactivating)");

                    continue;
                }

                if (CanActivate(painter, target, false))
                {
                    if (DebugConfig.DebugTargetPainters)
                        Log.WriteLine("if (CanActivate(painter, target, false))");

                    if (painter.Activate(target))
                    {
                        Log.WriteLine("Activating [" + painter.TypeName + "][" + _weaponNumber + "] on [" + target.Name + "][" + target.MaskedId + "][" +
                                      Math.Round(target.Distance / 1000, 0) + "k away]");
                        return;
                    }
                }
                else if (DebugConfig.DebugTargetPainters)
                {
                    Log.WriteLine("if (!CanActivate(painter, target, false))");
                }
            }

            if (DebugConfig.DebugTargetPainters)
                Log.WriteLine("Done with all TargetPainters.");
        }

        private static void ActivateTrackingDisruptors(EntityCache killtarget)
        {
            HashSet<long> tempListofEntitiesToTrackingDisrupt = ESCache.Instance.ListofEntitiesToTrackingDisrupt;

            try
            {
                foreach (long entityId in tempListofEntitiesToTrackingDisrupt)
                    if (ESCache.Instance.EntitiesOnGrid.All(i => i.Id != entityId))
                    {
                        ESCache.Instance.ListofEntitiesToTrackingDisrupt.Remove(entityId);
                        break;
                    }
            }
            catch (Exception)
            {
                //ignore this exception
            }

            EntityCache target = PotentialCombatTargets.OrderBy(i => i.IsPrimaryWeaponPriorityTarget).FirstOrDefault(i => ESCache.Instance.ListofEntitiesToTrackingDisrupt.Contains(i.Id))
                ?? killtarget;

            if (target != null)
            {
                if (target.IsEwarImmune)
                {
                    if (DebugConfig.DebugKillTargets)
                        Log.WriteLine("Ignoring TrackingDisruptor Activation on [" + target.Name + "]IsEwarImmune[" + target.IsEwarImmune + "]");
                    return;
                }

                List<ModuleCache> trackingDisruptors = ESCache.Instance.Modules.Where(m => m.GroupId == (int)Group.TrackingDisruptor).ToList();

                _weaponNumber = 0;
                foreach (ModuleCache trackingDisruptor in trackingDisruptors)
                {
                    if (Time.Instance.LastActivatedTimeStamp != null && Time.Instance.LastActivatedTimeStamp.ContainsKey(trackingDisruptor.ItemId))
                        if (Time.Instance.LastActivatedTimeStamp[trackingDisruptor.ItemId].AddMilliseconds(Time.Instance.PainterDelay_milliseconds) > DateTime.UtcNow)
                            continue;

                    _weaponNumber++;

                    if (trackingDisruptor.IsActive)
                    {
                        if (trackingDisruptor.TargetId != target.Id)
                        {
                            if (trackingDisruptor.Click()) return;

                            return;
                        }

                        continue;
                    }

                    if (trackingDisruptor.IsDeactivating)
                        continue;

                    if (CanActivate(trackingDisruptor, target, false))
                        if (trackingDisruptor.Activate(target))
                        {
                            Log.WriteLine("Activating [" + trackingDisruptor.TypeName + "][" + _weaponNumber + "] on [" + target.Name + "][" + target.MaskedId + "][" +
                                          Math.Round(target.Distance / 1000, 0) + "k away]");
                            return;
                        }
                }
            }
        }

        private static void ActivateWarpDisruptor(EntityCache target)
        {
            if (target.IsEwarImmune)
            {
                if (DebugConfig.DebugKillTargets)
                    Log.WriteLine("Ignoring WarpDisruptor Activation on [" + target.Name + "]IsEwarImmune[" + target.IsEwarImmune + "]");
                return;
            }

            if (ESCache.Instance.InWormHoleSpace && ESCache.Instance.ActiveShip.IsDread && !target._directEntity.IsNPCWormHoleSpaceDrifter)
            {
                if (DebugConfig.DebugKillTargets)
                    Log.WriteLine("Ignoring WarpDisruptor Activation on [" + target.Name + "] IsNpcCapitalEscalation [" + target.IsNpcCapitalEscalation + "]");
                return;
            }

            List<ModuleCache> WarpDisruptors = ESCache.Instance.Modules.Where(m => m.GroupId == (int)Group.WarpDisruptor).ToList();

            _weaponNumber = 0;
            foreach (ModuleCache WarpDisruptor in WarpDisruptors)
            {
                _weaponNumber++;

                if (Time.Instance.LastActivatedTimeStamp != null && Time.Instance.LastActivatedTimeStamp.ContainsKey(WarpDisruptor.ItemId))
                    if (Time.Instance.LastActivatedTimeStamp[WarpDisruptor.ItemId].AddMilliseconds(Time.Instance.WebDelay_milliseconds) > DateTime.UtcNow)
                        continue;

                if (WarpDisruptor.IsActive)
                {
                    if (WarpDisruptor.TargetId != target.Id)
                    {
                        if (WarpDisruptor.Click()) return;

                        return;
                    }

                    continue;
                }

                if (WarpDisruptor.IsDeactivating)
                    continue;

                if (target.Distance >= WarpDisruptor.OptimalRange)
                    continue;

                if (CanActivate(WarpDisruptor, target, false))
                    if (WarpDisruptor.Activate(target))
                    {
                        Log.WriteLine("Activating [" + WarpDisruptor.TypeName + "][" + _weaponNumber + "] on [" + target.Name + "][" + target.MaskedId +
                                      "]");
                        return;
                    }
            }
        }

        private static bool DeactivateWeaponsAsNeeded(EntityCache target)
        {
            try
            {
                _weaponNumber = 0;
                if (DebugConfig.DebugActivateWeapons)
                    Log.WriteLine("ActivateWeapons: deactivate: Do we need to deactivate any weapons?");

                foreach (ModuleCache weapon in ESCache.Instance.Weapons.Where(i => i.IsActive).OrderBy(i => i.ChargeQty == 0))
                {
                    try
                    {
                        _weaponNumber++;
                        if (DebugConfig.DebugActivateWeapons)
                            Log.WriteLine("ActivateWeapons: deactivate: for each weapon [" + _weaponNumber + "] in weapons");

                        if (Time.Instance.LastActivatedTimeStamp != null && Time.Instance.LastActivatedTimeStamp.ContainsKey(weapon.ItemId))
                            if (Time.Instance.LastActivatedTimeStamp[weapon.ItemId].AddMilliseconds(Time.Instance.WeaponDelay_milliseconds) > DateTime.UtcNow)
                                continue;

                        if (weapon.IsReloadingAmmo)
                        {
                            if (DebugConfig.DebugActivateWeapons)
                                Log.WriteLine("ActivateWeapons: deactivate: [" + weapon.TypeName + "][" + _weaponNumber + "] is reloading ammo: waiting");
                            continue;
                        }

                        if (weapon.IsDeactivating)
                        {
                            if (DebugConfig.DebugActivateWeapons)
                                Log.WriteLine("ActivateWeapons: deactivate: [" + weapon.TypeName + "][" + _weaponNumber + "] is deactivating: waiting");
                            continue;
                        }

                        if (weapon.InLimboState)
                        {
                            if (DebugConfig.DebugActivateWeapons)
                                Log.WriteLine("ActivateWeapons: deactivate: [" + weapon.TypeName + "][" + _weaponNumber + "] is InLimboState: waiting");
                            continue;
                        }

                        if (!weapon.WeaponNeedsAmmo || weapon.ChargeQty >= 1)
                        {
                            if (!ESCache.Instance.InWarp)
                            {
                                if (weapon.ChargeQty > 0)
                                {
                                    if (weapon.Charge.DefinedAsAmmoType != null)
                                    {
                                        if (target.Distance <= weapon.Charge.DefinedAsAmmoType.Range)
                                        {
                                            if (weapon.TargetId == target.Id)
                                            {
                                                if (target.IsTarget)
                                                {
                                                    if (DebugConfig.DebugActivateWeapons)
                                                        Log.WriteLine("ActivateWeapons: deactivate: target is in range: do nothing, wait until it is dead");
                                                    continue;
                                                }
                                                else if (DebugConfig.DebugActivateWeapons)
                                                {
                                                    Log.WriteLine("ActivateWeapons: deactivate: if (!target.IsTarget)");
                                                }
                                            }
                                            else if (DebugConfig.DebugActivateWeapons)
                                            {
                                                Log.WriteLine("ActivateWeapons: deactivate: if (weapon.TargetId != target.Id)");
                                            }
                                        }
                                        else if (DebugConfig.DebugActivateWeapons)
                                        {
                                            Log.WriteLine("ActivateWeapons: deactivate: if (target.Distance [" + target.Distance + "] is greater than weapon.Charge.DefinedAsAmmoType [" + weapon.Charge.DefinedAsAmmoType.Description + "].Range [" + weapon.Charge.DefinedAsAmmoType.Range + "])");
                                        }
                                    }
                                    else if (DebugConfig.DebugActivateWeapons)
                                    {
                                        Log.WriteLine("ActivateWeapons: deactivate: if (weapon.Charge [" + weapon.Charge.TypeName + "].DefinedAsAmmoType == null)");
                                    }
                                }
                                else if (DebugConfig.DebugActivateWeapons)
                                {
                                    Log.WriteLine("ActivateWeapons: deactivate: if (weapon.Charge == null)");
                                }
                            }
                            //We are in warp
                        }

                        if (weapon.GroupId == (int)Group.PrecursorWeapon || weapon.IsCivilianWeapon)
                            if (weapon.TargetId == target.Id && target.IsTarget)
                                continue;

                        if (DebugConfig.DebugActivateWeapons)
                        {
                            if (weapon.TargetId != target.Id && target.IsTarget && Combat.PotentialCombatTargets.Count > 1)
                                Log.WriteLine("ActivateWeapons: deactivate: we have the wrong target, stop firing");

                            if (weapon.WeaponNeedsAmmo && weapon.ChargeQty < 2)
                                Log.WriteLine("ActivateWeapons: deactivate: we are out of ammo, stop firing");

                            if (weapon.GroupId != (int)Group.PrecursorWeapon && !weapon.IsCivilianWeapon)
                                if (target.Distance >= MaxRange)
                                    Log.WriteLine("ActivateWeapons: deactivate: target distance [" + Math.Round(target.Distance / 1000, 0)  + "] is out of range [" + MaxRange + "], stop firing");

                            Log.WriteLine("ActivateWeapons: deactivate: true");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Exception [" + ex + "]");
                    }

                    //
                    // deactivate
                    //

                    if (weapon.Click()) return true;
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

        private static bool ActivateWeaponsAsNeeded(EntityCache target)
        {
            if (ESCache.Instance.Weapons.Count == 0) return false;

            if (!ESCache.Instance.Weapons.Any(w => w.IsEnergyWeapon))
            {
                MaxCharges = Math.Max(MaxCharges, ESCache.Instance.Weapons.Max(l => l.MaxCharges));
                MaxCharges = Math.Max(MaxCharges, ESCache.Instance.Weapons.Max(l => l.ChargeQty));
            }

            if (Combat.PotentialCombatTargets.All(i => !i.IsReadyToShoot))
            {
                Log.WriteLine("ActivateWeapons: activate: if (Combat.PotentialCombatTargets.All(i => !i.IsReadyToShoot))");
                if (Combat.PotentialCombatTargets.Any())
                {
                    Log.WriteLine("ActivateWeapons: activate: if (Combat.PotentialCombatTargets.Any()) ChangeAmmoIfNeeded()");
                    //ChangeAmmoIfNeeded();
                }
            }

            int weaponsActivatedThisTick = 0;
            //int weaponsToActivateThisTick = ESCache.Instance.RandomNumber(3, 5);

            _weaponNumber = 0;
            if (DebugConfig.DebugActivateWeapons)
                Log.WriteLine("ActivateWeapons: activate: Do we need to activate any weapons?");

            if (ESCache.Instance.Weapons.Count == 0) return false;

            foreach (ModuleCache weapon in ESCache.Instance.Weapons)
            {
                _weaponNumber++;

                if (!weapon.IsEnergyWeapon && Time.Instance.LastActivatedTimeStamp != null && Time.Instance.LastActivatedTimeStamp.ContainsKey(weapon.ItemId))
                    if (Time.Instance.LastActivatedTimeStamp[weapon.ItemId].AddMilliseconds(Time.Instance.WeaponDelay_milliseconds) > DateTime.UtcNow)
                        continue;

                if (weapon.InLimboState)
                {
                    Log.WriteLine("ActivateWeapons: Activate: [" + weapon.TypeName + "][" + _weaponNumber + "] is InLimboState, waiting.");
                    continue;
                }

                if (weapon.IsReloadingAmmo)
                {
                    if (DebugConfig.DebugActivateWeapons)
                        Log.WriteLine("ActivateWeapons: Activate: [" + weapon.TypeName + "][" + _weaponNumber + "] is reloading, waiting.");
                    continue;
                }

                if (weapon.IsDeactivating)
                {
                    if (DebugConfig.DebugActivateWeapons)
                        Log.WriteLine("ActivateWeapons: Activate: [" + weapon.TypeName + "][" + _weaponNumber + "] is deactivating, waiting.");
                    continue;
                }

                if (!target.IsTarget)
                {
                    if (DebugConfig.DebugActivateWeapons)
                        Log.WriteLine("ActivateWeapons: Activate: [" + weapon.TypeName + "][" + _weaponNumber + "] is [" + target.Name +
                                      "] is not locked, waiting.");
                    continue;
                }

                if (weapon.IsActive)
                {
                    if (DebugConfig.DebugActivateWeapons)
                        Log.WriteLine("ActivateWeapons: Activate: [" + weapon.TypeName + "][" + _weaponNumber + "] is active already");
                    if (weapon.TargetId != target.Id && target.IsTarget)
                    {
                        if (DebugConfig.DebugActivateWeapons)
                            Log.WriteLine("ActivateWeapons: Activate: [" + weapon.TypeName + "][" + _weaponNumber +
                                          "] is shooting at the wrong target: deactivating");
                        if (weapon.Click()) continue;
                    }

                    if (!weapon.IsEnergyWeapon) continue;
                }

                if (weapon.ChargeQty == 0 && weapon.WeaponNeedsAmmo && !weapon.InLimboState && weapon.IsOnline && !weapon.IsReloadingAmmo)
                {
                    if (!ESCache.Instance.ActiveShip.IsDread && !ESCache.Instance.ActiveShip.IsMarauder && Combat.KillTarget._directEntity.IsNPCWormHoleSpaceDrifter)
                    {
                        Log.WriteLine("ActivateWeaponsAsNeeded: if (!ESCache.Instance.ActiveShip.IsDread && !ESCache.Instance.ActiveShip.IsMarauder && Combat.KillTarget._directEntity.IsNPCWormHoleSpaceDrifter)");
                        if (!ReloadAmmo(weapon, _weaponNumber)) continue;
                    }
                    else
                    {
                        Log.WriteLine("ActivateWeaponsAsNeeded: if (weapon.ChargeQty == 0 && weapon.WeaponNeedsAmmo && !weapon.InLimboState && weapon.IsOnline && !weapon.IsReloadingAmmo)");
                        if (!DebugConfig.DebugDisableAmmoManagement)
                        {
                            AmmoManagementBehavior.ChangeAmmoManagementBehaviorState(States.AmmoManagementBehaviorState.HandleWeaponsNeedReload);
                        }
                        else ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdReloadAmmo);
                    }

                    Log.WriteLine("ActivateWeapons: Activate: [" + weapon.TypeName + "][" + _weaponNumber + "] charge qty == 0.");
                    continue;
                }

                if (CanActivate(weapon, target, true))
                {
                    if (DebugConfig.DebugActivateWeapons)
                        Log.WriteLine("ActivateWeapons: Activate: [" + weapon.TypeName + "][" + _weaponNumber + "] has the correct ammo [" + weapon.ChargeName + "]: activate");
                    if (weapon.Activate(target))
                    {
                        if (!Drones.DronesKillHighValueTargets)
                        {
                            if (DirectEve.Interval(10000, 10000, target.Id.ToString())) ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.IsInAbyss), true); ESCache.Instance.TaskSetEveAccountAttribute(nameof(ESCache.Instance.EveAccount.LastEntityIdEngaged), target.Id);
                            if (DirectEve.Interval(10000, 10000, target.Id.ToString())) ESCache.Instance.TaskSetEveAccountAttribute(nameof(ESCache.Instance.EveAccount.DateTimeLastEntityIdEngaged), DateTime.UtcNow);
                        }

                        weaponsActivatedThisTick++;
                        Log.WriteLine("Activating weapon[" + _weaponNumber + "][" + weapon.ChargeName + "] ChargeQty [" + weapon.ChargeQty + "] on [" + target.Name + "] TypeName [" + target.TypeName + "] EWAR [" + target.stringEwarTypes + "][" + target.MaskedId + "][" +
                                      Math.Round(target.Distance / 1000, 0) + "k] away");
                    }

                    continue;
                }

                if (DebugConfig.DebugActivateWeapons)
                    Log.WriteLine("ActivateWeapons: ReloadReady [" + ReloadAmmo(weapon, _weaponNumber) + "] CanActivateReady [" +
                                  CanActivate(weapon, target, true) + "]");
            }

            if (weaponsActivatedThisTick > 0) return true;
            return false;
        }

        private static void ActivateWeapons(EntityCache target)
        {
            if (ESCache.Instance.InSpace && ESCache.Instance.InWarp)
            {
                if (PrimaryWeaponPriorityEntities.Count > 0)
                    RemovePrimaryWeaponPriorityTargets(PrimaryWeaponPriorityEntities);

                if (Drones.UseDrones && Drones.DronePriorityEntities.Count > 0)
                    Drones.RemoveDronePriorityTargets(Drones.DronePriorityEntities);

                if (DebugConfig.DebugActivateWeapons)
                    Log.WriteLine("ActivateWeapons: deactivate: we are in warp! doing nothing");
                return;
            }

            if (DebugConfig.DebugActivateWeapons)
                Log.WriteLine("ActivateWeapons: deactivate: after navigate into range...");

            if (ESCache.Instance.Weapons.Count > 0)
            {
                DeactivateWeaponsAsNeeded(target);
                ActivateWeaponsAsNeeded(target);
            }
            else
            {
                Log.WriteLine("ActivateWeapons: you have no weapons");
                icount = 0;
                foreach (ModuleCache __module in ESCache.Instance.Modules.Where(e => e.IsOnline && e.IsActivatable))
                {
                    icount++;
                    Log.WriteLine("[" + icount + "] Module TypeID [ " + __module.TypeId + " ] ModuleGroupID [ " + __module.GroupId +
                                  " ] EveCentral Link [ http://eve-central.com/home/quicklook.html?typeid=" + __module.TypeId + " ]");
                }
            }
        }

        private static bool CanActivate(ModuleCache module, EntityCache entity, bool isWeapon)
        {
            if (module.InLimboState)
                return false;

            if (module.IsActive)
                return false;

            if (isWeapon && !entity.IsTarget)
            {
                Log.WriteLine("We attempted to shoot [" + entity.Name + "][" + Math.Round(entity.Distance / 1000, 2) +
                              "] which is currently not locked!");
                return false;
            }

            if (isWeapon && entity.Distance > MaxWeaponRange)
            {
                if (DebugConfig.DebugActivateWeapons)
                    Log.WriteLine("We attempted to shoot [" + entity.Name + "][" + Math.Round(entity.Distance / 1000, 2) +
                                  "] which is out of weapons range!");
                return false;
            }

            if (module.GroupId == (int)Group.NOS && entity.Distance > (module.OptimalRange + module.FalloffEffectiveness))
            {
                if (DebugConfig.DebugActivateWeapons)
                    Log.WriteLine("We attempted to use NOS on [" + entity.Name + "][" + Math.Round(entity.Distance / 1000, 2) +
                                  "] which is out of NOS optimal range! [" + module.OptimalRange + "]");
                return false;
            }

            if (entity.Id != module.LastTargetId)
                return true;

            if (isWeapon && module.ChargeQty == MaxCharges)
                return true;

            return true;
        }

        #endregion Methods
    }
}