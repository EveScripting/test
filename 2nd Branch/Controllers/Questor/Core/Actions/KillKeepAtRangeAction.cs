using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using System;
using System.Collections.Generic;
using System.Linq;
using Action = EVESharpCore.Questor.Actions.Base.Action;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private static void KillKeepAtRangeAction(Action action, DirectAgentMission myMission, DirectAgent myAgent)
        {
            try
            {
                bool notTheClosest;
                if (!bool.TryParse(action.GetParameterValue("notclosest"), out notTheClosest))
                    notTheClosest = false;

                int distanceToKeepAtRange;
                if (!int.TryParse(action.GetParameterValue("distance"), out distanceToKeepAtRange))
                    distanceToKeepAtRange = 1000;

                List<string> targetNames = action.GetParameterValues("target");

                // No parameter? Ignore kill action
                if (!targetNames.Any())
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

                if (_singleTargetToEliminate != 0 && ESCache.Instance.EntitiesOnGrid.Any(e => e.Id == _singleTargetToEliminate))
                    _singleTargetToEliminate = killTarget.Id;

                if (killTarget == null || ESCache.Instance.EntitiesOnGrid.All(i => i.Id != killTarget.Id))
                {
                    Log.WriteLine("All targets named [" + targetNames.FirstOrDefault() + "] killed "); //+
                    //targetNames.Aggregate((current, next) => current + "[" + next + "] NumToIgnore [" + numberToIgnore + "]"));

                    // We killed it/them !?!?!? :)
                    IgnoreTargets.RemoveWhere(targetNames.Contains);
                    _singleTargetToEliminate = 0;
                    NextAction(myMission, myAgent, true);
                    return;
                }

                //
                // then proceed to kill the target
                //
                IgnoreTargets.RemoveWhere(targetNames.Contains);

                if (killTarget != null) //if it is not null is HAS to be OnGridWithMe as all killTargets are verified OnGridWithMe
                {
                    if (DebugConfig.DebugKillAction)
                        Log.WriteLine(" proceeding to kill [" + killTarget.Name + "] at [" +
                                      Math.Round(killTarget.Distance / 1000, 2) + "k] (this is spammy, but useful debug info)");
                    //if (Combat.PreferredPrimaryWeaponTarget == null || String.IsNullOrEmpty(Cache.Instance.PreferredDroneTarget.Name) || Combat.PreferredPrimaryWeaponTarget.IsOnGridWithMe && Combat.PreferredPrimaryWeaponTarget != currentKillTarget)
                    //{
                    //Logging.Log("CombatMissionCtrl[" + PocketNumber + "]." + _pocketActions[_currentActionNumber], "Adding [" + currentKillTarget.Name + "][" + Math.Round(currentKillTarget.Distance / 1000, 0) + "][" + Cache.Instance.MaskedID(currentKillTarget.Id) + "] groupID [" + currentKillTarget.GroupId + "] TypeID[" + currentKillTarget.TypeId + "] as PreferredPrimaryWeaponTarget");
                    Combat.Combat.AddPrimaryWeaponPriorityTarget(killTarget, PrimaryWeaponPriority.PriorityKillTarget);
                    Combat.Combat.PreferredPrimaryWeaponTarget = killTarget;
                    //}
                    //else
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

                    try
                    {
                        killTarget.KeepAtRange(distanceToKeepAtRange);
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Exception [" + ex + "]");
                        return;
                    }
                }

                //if (Combat.Combat.PreferredPrimaryWeaponTarget != killTarget)
                //    Combat.Combat.GetBestPrimaryWeaponTarget(Combat.Combat.MaxRange, false, "Combat");

                // Don't use NextAction here, only if target is killed (checked further up)
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        #endregion Methods
    }
}