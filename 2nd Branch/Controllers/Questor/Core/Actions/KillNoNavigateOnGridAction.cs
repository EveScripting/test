using EVESharpCore.Cache;
using EVESharpCore.Logging;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Lookup;
using System;
using System.Collections.Generic;
using System.Linq;
using Action = EVESharpCore.Questor.Actions.Base.Action;
using EVESharpCore.Framework;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private static void KillNoNavigateOnGridAction(Action action, DirectAgentMission myMission, DirectAgent myAgent)
        {
            try
            {
                bool notTheClosest;
                if (!bool.TryParse(action.GetParameterValue("notclosest"), out notTheClosest))
                    notTheClosest = false;

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
                EntityCache killTarget = null;

                if (_singleTargetToEliminate != 0)
                {
                    killTarget = ESCache.Instance.EntitiesOnGrid.FirstOrDefault(e => e.Id == _singleTargetToEliminate);
                    if (killTarget == null)
                    {
                        Log.WriteLine("target named [" + targetNames.FirstOrDefault() + "] killed "); //+
                        IgnoreTargets.Clear();
                        ESCache.Instance.ListofEntitiesToEcm.Clear();
                        _singleTargetToEliminate = 0;
                        NextAction(myMission, myAgent, true);
                        return;
                    }

                    if (DebugConfig.DebugKillAction) Log.WriteLine("KillNoNavigateOnGrid: Target is [" + killTarget.Name + "][" + killTarget.MaskedId + "]");
                }

                if (_singleTargetToEliminate == 0)
                {
                    killTarget = ESCache.Instance.EntitiesOnGrid.Where(e => targetNames.Contains(e.Name)).OrderByDescending(t => t.Id).FirstOrDefault();

                    if (notTheClosest)
                        killTarget = ESCache.Instance.EntitiesOnGrid.Where(e => targetNames.Contains(e.Name)).OrderBy(t => t.Id).FirstOrDefault();

                    if (killTarget == null)
                    {
                        Log.WriteLine("target named [" + targetNames.FirstOrDefault() + "] killed "); //+
                        IgnoreTargets.Clear();
                        ESCache.Instance.ListofEntitiesToEcm.Clear();
                        _singleTargetToEliminate = 0;
                        NextAction(myMission, myAgent, true);
                        return;
                    }
                }

                List<string> ecmtargetNames = action.GetParameterValues("ecmtarget");
                EntityCache entityToEcm = null;
                // No parameter? Ignore ecm action
                if (ecmtargetNames.Count > 0 && ESCache.Instance.EntitiesOnGrid != null && ESCache.Instance.EntitiesOnGrid.Any())
                {
                    entityToEcm = ESCache.Instance.EntitiesOnGrid.Where(i => i.Name.ToLower() == ecmtargetNames.FirstOrDefault().ToLower()).OrderByDescending(j => killTarget != null && j.Id != killTarget.Id).FirstOrDefault();
                    if (entityToEcm != null)
                        if (!ESCache.Instance.ListofEntitiesToEcm.Contains(entityToEcm.Id))
                        {
                            Log.WriteLine("adding [" + entityToEcm.Name + "][" + Math.Round(entityToEcm.Distance / 1000, 0) + "k][" + entityToEcm.MaskedId +
                                          "] to the ECM List");
                            ESCache.Instance.ListofEntitiesToEcm.Add(entityToEcm.Id);
                        }
                }

                List<string> trackingDisrupttargetNames = action.GetParameterValues("trackingdisrupttarget");
                EntityCache entityToTrackingDisrupt = null;
                // No parameter? Ignore ecm action
                if (trackingDisrupttargetNames.Count > 0 && ESCache.Instance.EntitiesOnGrid != null && ESCache.Instance.EntitiesOnGrid.Any())
                {
                    entityToTrackingDisrupt = ESCache.Instance.EntitiesOnGrid.Where(i => i.Name.ToLower() == trackingDisrupttargetNames.FirstOrDefault().ToLower()).OrderByDescending(j => killTarget != null && j.Id != killTarget.Id).FirstOrDefault();
                    if (entityToTrackingDisrupt != null)
                        if (!ESCache.Instance.ListofEntitiesToTrackingDisrupt.Contains(entityToTrackingDisrupt.Id))
                        {
                            Log.WriteLine("adding [" + entityToTrackingDisrupt.Name + "][" + Math.Round(entityToTrackingDisrupt.Distance / 1000, 0) + "k][" + entityToTrackingDisrupt.MaskedId +
                                          "] to the Tracking Disruptor List");
                            ESCache.Instance.ListofEntitiesToTrackingDisrupt.Add(entityToTrackingDisrupt.Id);
                        }
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
                    if (Drones.DronesKillHighValueTargets)
                    {
                        Drones.PreferredDroneTarget = killTarget;
                        Drones.PreferredDroneTargetID = killTarget.Id;
                    }
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

                    //
                    // do not NavigateOnGrid, purposely keep doing whatever you were doing before starting this action (orbiting, sitting still ,etc)
                    //
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