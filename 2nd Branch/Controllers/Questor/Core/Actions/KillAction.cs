using EVESharpCore.Cache;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Questor.Combat;
using Action = EVESharpCore.Questor.Actions.Base.Action;
using EVESharpCore.Framework;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private static void KillAction(Action action, DirectAgentMission myMission, DirectAgent myAgent)
        {
            bool ignoreAttackers;
            if (!bool.TryParse(action.GetParameterValue("ignoreattackers"), out ignoreAttackers))
                ignoreAttackers = false;

            int distance;
            if (!int.TryParse(action.GetParameterValue("distance"), out distance))
                distance = 5000;

            bool breakOnAttackers;
            if (!bool.TryParse(action.GetParameterValue("breakonattackers"), out breakOnAttackers))
                breakOnAttackers = false;

            bool notTheClosest;
            if (!bool.TryParse(action.GetParameterValue("notclosest"), out notTheClosest))
                notTheClosest = false;

            int numberToIgnore;
            if (!int.TryParse(action.GetParameterValue("numbertoignore"), out numberToIgnore))
                numberToIgnore = 0;

            int attackUntilBelowShieldPercentage;
            if (!int.TryParse(action.GetParameterValue("attackUntilBelowShieldPercentage"), out attackUntilBelowShieldPercentage))
                attackUntilBelowShieldPercentage = 0;

            int attackUntilBelowArmorPercentage;
            if (!int.TryParse(action.GetParameterValue("attackUntilBelowArmorPercentage"), out attackUntilBelowArmorPercentage))
                attackUntilBelowArmorPercentage = 0;

            int attackUntilBelowHullPercentage;
            if (!int.TryParse(action.GetParameterValue("attackUntilBelowHullPercentage"), out attackUntilBelowHullPercentage))
                attackUntilBelowHullPercentage = 0;

            List<string> targetNames = action.GetParameterValues("target");

            // No parameter? Ignore kill action
            if (targetNames.Count == 0)
            {
                Log.WriteLine("No targets defined in kill action!");
                NextAction(myMission, myAgent, true);
                return;
            }

            if (DebugConfig.DebugKillAction)
            {
                int targetNameCount = 0;
                foreach (string targetName in targetNames)
                {
                    targetNameCount++;
                    Log.WriteLine("targetNames [" + targetNameCount + "][" + targetName + "]");
                }
            }

            EntityCache killTarget = ESCache.Instance.EntitiesOnGrid.Where(e => targetNames.Contains(e.Name)).OrderBy(t => t.Nearest5kDistance).FirstOrDefault();

            if (notTheClosest)
                killTarget = ESCache.Instance.EntitiesOnGrid.Where(e => targetNames.Contains(e.Name)).OrderByDescending(t => t.Nearest5kDistance).FirstOrDefault();

            if (_singleTargetToEliminate != 0)
                killTarget = ESCache.Instance.EntitiesOnGrid.Find(e => e.Id == _singleTargetToEliminate);

            if (killTarget == null || ESCache.Instance.EntitiesOnGrid.All(i => i.Id != killTarget.Id))
            {
                Log.WriteLine("All targets killed " +
                              targetNames.Aggregate((current, next) => current + "[" + next + "] NumToIgnore [" + numberToIgnore + "]"));

                // We killed it/them !?!?!? :)
                IgnoreTargets.RemoveWhere(targetNames.Contains);
                if (ignoreAttackers)
                    foreach (EntityCache target in Combat.Combat.PotentialCombatTargets.Where(e => !targetNames.Contains(e.Name)))
                        if (target.IsTargetedBy && target.IsAttacking)
                        {
                            Log.WriteLine("UN-Ignoring [" + target.Name + "][" + target.MaskedId + "][" + Math.Round(target.Distance / 1000, 0) +
                                          "k away] due to ignoreAttackers parameter (and kill action being complete)");
                            IgnoreTargets.Remove(target.Name.Trim());
                        }
                _singleTargetToEliminate = 0;
                NextAction(myMission, myAgent, true);
                return;
            }

            if (ignoreAttackers)
                foreach (EntityCache target in Combat.Combat.PotentialCombatTargets.Where(e => !targetNames.Contains(e.Name)))
                    if (target.IsTargetedBy && target.IsAttacking)
                    {
                        Log.WriteLine("Ignoring [" + target.Name + "][" + target.MaskedId + "][" + Math.Round(target.Distance / 1000, 0) +
                                      "k away] due to ignoreAttackers parameter");
                        IgnoreTargets.Add(target.Name.Trim());
                    }

            if (breakOnAttackers &&
                Combat.Combat.TargetedBy.Count(
                    t => (!t.IsSentry || (t.IsSentry && t.KillSentries) || (t.IsSentry && t.IsEwarTarget)) && !t.IsIgnored) >
                Combat.Combat.PotentialCombatTargets.Count(e => e.IsTarget))
            {
                //
                // We are being attacked, break the kill order
                // which involves removing the named targets as PrimaryWeaponPriorityTargets, PreferredPrimaryWeaponTarget, DronePriorityTargets, and PreferredDroneTarget
                //
                Log.WriteLine("Breaking off kill order, new spawn has arrived!");
                targetNames.ForEach(t => IgnoreTargets.Add(t));

                if (killTarget != null)
                {
                    Combat.Combat.RemovePrimaryWeaponPriorityTargetsByName(killTarget.Name);
                    if (Combat.Combat.PreferredPrimaryWeaponTarget != null && killTarget.Name == Combat.Combat.PreferredPrimaryWeaponTarget.Name)
                    {
                        Combat.Combat.RemovePrimaryWeaponPriorityTargetsByName(killTarget.Name);
                        if (Drones.UseDrones)
                            Drones.RemovedDronePriorityTargetsByName(killTarget.Name);
                    }

                    if (Combat.Combat.PreferredPrimaryWeaponTargetID != null)
                        if (killTarget.Id == Combat.Combat.PreferredPrimaryWeaponTargetID)
                        {
                            Log.WriteLine("Breaking Kill Order in: [" + killTarget.Name + "][" + Math.Round(killTarget.Distance / 1000, 0) + "k][" +
                                          Combat.Combat.PreferredPrimaryWeaponTarget.MaskedId + "]");
                            Combat.Combat.PreferredPrimaryWeaponTarget = null;
                        }

                    if (Drones.PreferredDroneTargetID != null)
                        if (killTarget.Id == Drones.PreferredDroneTargetID)
                        {
                            Log.WriteLine("Breaking Kill Order in: [" + killTarget.Name + "][" + Math.Round(killTarget.Distance / 1000, 0) + "k][" +
                                          Drones.PreferredDroneTarget.MaskedId + "]");
                            Drones.PreferredDroneTarget = null;
                        }
                }

                foreach (EntityCache KillTargetEntity in ESCache.Instance.Targets.Where(e => targetNames.Contains(e.Name) && (e.IsTarget || e.IsTargeting)))
                {
                    if (Combat.Combat.PreferredPrimaryWeaponTarget != null)
                        if (KillTargetEntity.Id == Combat.Combat.PreferredPrimaryWeaponTarget.Id)
                            continue;

                    Log.WriteLine("Unlocking [" + KillTargetEntity.Name + "][" + KillTargetEntity.MaskedId + "][" +
                                  Math.Round(KillTargetEntity.Distance / 1000, 0) +
                                  "k away] due to kill order being put on hold");
                    KillTargetEntity.UnlockTarget();
                    return;
                }
            }
            else //Do not break aggression on attackers (attack normally)
            {
                //
                // then proceed to kill the target
                //
                IgnoreTargets.RemoveWhere(targetNames.Contains);

                if (killTarget != null) //if it is not null is HAS to be OnGridWithMe as all killTargets are verified OnGridWithMe
                {
                    if (attackUntilBelowShieldPercentage > 0 && killTarget.ShieldPct * 100 < attackUntilBelowShieldPercentage)
                    {
                        Log.WriteLine("Kill target [" + killTarget.Name + "] at [" +
                                        Math.Round(killTarget.Distance / 1000, 2) +
                                        "k] Armor % is [" + (killTarget.ShieldPct * 100) +
                                        "] which is less then attackUntilBelowShieldPercentage [" +
                                        attackUntilBelowShieldPercentage + "] Kill Action Complete, Next Action.");
                        Combat.Combat.RemovePrimaryWeaponPriorityTargetsByName(killTarget.Name);
                        Combat.Combat.PreferredPrimaryWeaponTarget = null;
                        NextAction(myMission, myAgent, true);
                        return;
                    }

                    if (attackUntilBelowArmorPercentage > 0 && killTarget.ArmorPct * 100 < attackUntilBelowArmorPercentage)
                    {
                        Log.WriteLine("Kill target [" + killTarget.Name + "] at [" +
                                        Math.Round(killTarget.Distance / 1000, 2) +
                                        "k] Armor % is [" + (killTarget.ArmorPct * 100) +
                                        "] which is less then attackUntilBelowArmorPercentage [" +
                                        attackUntilBelowArmorPercentage + "] Kill Action Complete, Next Action.");
                        Combat.Combat.RemovePrimaryWeaponPriorityTargetsByName(killTarget.Name);
                        Combat.Combat.PreferredPrimaryWeaponTarget = null;
                        NextAction(myMission, myAgent, true);
                        return;
                    }

                    if (attackUntilBelowHullPercentage > 0 && killTarget.ArmorPct * 100 < attackUntilBelowHullPercentage)
                    {
                        Log.WriteLine("Kill target [" + killTarget.Name + "] at [" +
                                        Math.Round(killTarget.Distance / 1000, 2) +
                                        "k] Armor % is [" + (killTarget.StructurePct * 100) +
                                        "] which is less then attackUntilBelowHullPercentage [" +
                                        attackUntilBelowHullPercentage + "] Kill Action Complete, Next Action.");
                        Combat.Combat.RemovePrimaryWeaponPriorityTargetsByName(killTarget.Name);
                        Combat.Combat.PreferredPrimaryWeaponTarget = null;
                        NextAction(myMission, myAgent, true);
                        return;
                    }

                    if (DebugConfig.DebugKillAction)
                        Log.WriteLine(" proceeding to kill [" + killTarget.Name + "] at [" +
                                        Math.Round(killTarget.Distance / 1000, 2) + "k] (this is spammy, but useful debug info)");
                    Combat.Combat.AddPrimaryWeaponPriorityTarget(killTarget, PrimaryWeaponPriority.PriorityKillTarget);
                    Combat.Combat.PreferredPrimaryWeaponTarget = killTarget;

                    if (DebugConfig.DebugKillAction)
                    {
                        if (DebugConfig.DebugKillAction)
                            Log.WriteLine("Combat.PreferredPrimaryWeaponTarget =[ " + Combat.Combat.PreferredPrimaryWeaponTarget.Name + " ][" +
                                            Combat.Combat.PreferredPrimaryWeaponTarget.MaskedId + "]");

                        if (Combat.Combat.PrimaryWeaponPriorityTargets.Count > 0)
                        {
                            if (DebugConfig.DebugKillAction)
                                Log.WriteLine("PrimaryWeaponPriorityTargets Below (if any)");
                            int icount = 0;
                            foreach (EntityCache PT in Combat.Combat.PrimaryWeaponPriorityEntities)
                            {
                                icount++;
                                if (DebugConfig.DebugKillAction)
                                    Log.WriteLine("PriorityTarget [" + icount + "] [ " + PT.Name + " ][" + PT.MaskedId + "] IsOnGridWithMe [" +
                                                    PT.IsOnGridWithMe +
                                                    "]");
                            }
                            if (DebugConfig.DebugKillAction)
                                Log.WriteLine("PrimaryWeaponPriorityTargets Above (if any)");
                        }
                    }

                    EntityCache NavigateTowardThisTarget = null;

                    if (Combat.Combat.PreferredPrimaryWeaponTarget != null)
                        NavigateTowardThisTarget = killTarget;

                    //we may need to get closer so combat will take over
                    if (DebugConfig.DebugKillAction)
                            Log.WriteLine("if (Combat.PreferredPrimaryWeaponTarget.Distance > Combat.MaxRange)");

                    //for Anomic missions we assume thre is a mission action handling navigating on the grid...
                    if (myMission.Name.Contains("Anomic") && killTarget.IsBurnerMainNPC)
                    {
                        if (killTarget.Distance > distance + 1500 || distance > killTarget.Distance)
                        {
                            killTarget.KeepAtRange(distance);
                            return;
                        }

                        return;
                    }

                    List<long> ListNavigateTowardThisTarget = new List<long>
                    {
                        NavigateTowardThisTarget.Id
                    };

                    NavigateOnGrid.NavigateIntoRange(ListNavigateTowardThisTarget, "combatMissionControl", true);
                }
            }

            // Don't use NextAction here, only if target is killed (checked further up)
        }

        #endregion Methods
    }
}