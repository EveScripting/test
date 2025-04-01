/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 28.05.2016
 * Time: 18:07
 *
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Stats;
using EVESharpCore.Questor.Traveller;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Extensions;
using SC::SharedComponents.IPC;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Questor.Behaviors;

namespace EVESharpCore.Controllers
{
    public class ExplorationNoWeaponsController : BaseController
    {
        #region Constructors

        public ExplorationNoWeaponsController()
        {
            IgnorePause = false;
            IgnoreModal = false;
            //State.CurrentAnomalyState = AnomalyState.Default;
            Time.Instance.NextStartupAction = DateTime.UtcNow;
            Time.Instance.Started_DateTime = DateTime.UtcNow;
        }

        #endregion Constructors

        #region Fields

        #endregion Fields

        #region Properties

        private bool RunOnceAfterStartupalreadyProcessed { get; set; }

        #endregion Properties

        #region Methods

        public static void InvalidateCache()
        {
            //_fleetMembers = null;
        }

        public static void ProcessState()
        {
            ExplortationNoWeaponsBehavior.ProcessState();
        }

        public override void DoWork()
        {
            try
            {
                if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                    return;

                if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Capsule)
                    return;

                if (!Settings.Instance.DefaultSettingsLoaded)
                {
                    if (DebugConfig.DebugCombatMissionsBehavior) Log("HighSecAnomalyController: Loading Settings");
                    Settings.Instance.LoadSettings_Initialize();
                }

                if (!RunOnceAfterStartupalreadyProcessed &&
                    ESCache.Instance.DirectEve.Session.CharacterId != null && ESCache.Instance.DirectEve.Session.CharacterId > 0)
                    if (Settings.CharacterXmlExists)
                    {
                        if (DateTime.UtcNow > Time.Instance.NextStartupAction)
                        {
                            try
                            {
                                if (!ESCache.Instance.OkToInteractWithEveNow)
                                {
                                    if (DebugConfig.DebugInteractWithEve) Log("HighSecAnomalyController: RunOnce: OpenCargoHold: !OkToInteractWithEveNow");
                                    return;
                                }

                                if (DebugConfig.DebugCombatMissionsBehavior) Log("HighSecAnomalyController: RunOnce");
                                ESCache.Instance.IterateShipTargetValues();
                                ESCache.Instance.LastInteractedWithEVE = DateTime.UtcNow;
                            }
                            catch (Exception ex)
                            {
                                Log("Exception [" + ex + "]");
                            }

                            RunOnceAfterStartupalreadyProcessed = true;
                        }
                    }
                    else
                    {
                        Log("Settings.Instance.CharacterName is still null");
                        Time.Instance.NextStartupAction = DateTime.UtcNow.AddSeconds(10);
                        RunOnceAfterStartupalreadyProcessed = false;
                        return;
                    }

                if (DateTime.UtcNow.Subtract(Time.Instance.LastUpdateOfSessionRunningTime).TotalSeconds <
                    Time.Instance.SessionRunningTimeUpdate_seconds)
                {
                    Statistics.SessionRunningTime =
                        (int)DateTime.UtcNow.Subtract(Time.Instance.Started_DateTime).TotalMinutes;
                    Time.Instance.LastUpdateOfSessionRunningTime = DateTime.UtcNow;
                }

                //if (ESCache.Instance.InWarp)
                //{
                //    if (DebugConfig.DebugCombatMissionsBehavior) Log("HydraController: if (ESCache.Instance.InWarp)");
                //    return;
                //}

                if (ESCache.Instance.InStation && ESCache.Instance.PauseAfterNextDock)
                {
                    if (DebugConfig.DebugCombatMissionsBehavior) Log("HydraController: if (ESCache.Instance.InStation && ESCache.Instance.PauseAfterNextDock)");
                    ControllerManager.Instance.SetPause(true);
                    ESCache.Instance.PauseAfterNextDock = false;
                    return;
                }

                //if (DebugConfig.DebugCombatMissionsBehavior) Log("AnomalyController: CurrentAnomalyState [" + State.CurrentAnomalyState + "]");

                ProcessState();
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            return true;
        }

        private static void ManageFleet()
        {
            try
            {
                if (Settings.Instance.UseFleetManager)
                {
                    //int slaveCharacterIdToInvite = 0;
                    if (!string.IsNullOrEmpty(ESCache.Instance.EveAccount.SlaveCharacterName1))
                    {
                        //slaveCharacterIdToInvite = int.Parse(ESCache.Instance.EveAccount.SlaveCharacter1Rep);
                        //SendFleetInvite(slaveCharacterIdToInvite);
                    }

                    if (!string.IsNullOrEmpty(ESCache.Instance.EveAccount.SlaveCharacterName2))
                    {
                        //slaveCharacterIdToInvite = int.Parse(ESCache.Instance.EveAccount.SlaveCharacter2RepairGroup);
                        //SendFleetInvite(slaveCharacterIdToInvite);
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Exception: " + ex);
            }
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }

        #endregion Methods
    }
}