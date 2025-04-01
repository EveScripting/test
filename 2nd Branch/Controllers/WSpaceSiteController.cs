
extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using EVESharpCore.Questor.Stats;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;
using System;
using System.Collections.Generic;
using EVESharpCore.Questor.Behaviors;

namespace EVESharpCore.Controllers
{
    public class WspaceSiteController : BaseController
    {
        #region Constructors

        public WspaceSiteController()
        {
            AllowRunInStation = false;
            IgnorePause = false;
            IgnoreModal = false;
            State.CurrentCombatDontMoveBehaviorState = CombatDontMoveBehaviorState.Default;
            Time.Instance.NextStartupAction = DateTime.UtcNow;
            Time.Instance.Started_DateTime = DateTime.UtcNow;
            WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.LastQuestorStarted), DateTime.UtcNow);
            DependsOn = new List<Type>
            {
                typeof(SalvageController),
                typeof(DefenseController),
                typeof(AmmoManagementController),
            };
        }

        #endregion Constructors

        private bool RunOnceAfterStartupAlreadyProcessed { get; set; }


        #region Methods

        public override void DoWork()
        {
            try
            {
                if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                    return;

                if (!Settings.Instance.DefaultSettingsLoaded)
                {
                    if (DebugConfig.DebugWspaceSiteBehavior) Log("WSpaceSiteController: Loading Settings");
                    Settings.Instance.LoadSettings_Initialize();
                }

                if (!RunOnceAfterStartupAlreadyProcessed &&
                    ESCache.Instance.DirectEve.Session.CharacterId != null && ESCache.Instance.DirectEve.Session.CharacterId > 0)
                    if (Settings.CharacterXmlExists)
                    {
                        if (DateTime.UtcNow > Time.Instance.NextStartupAction)
                        {
                            try
                            {
                                if (!ESCache.Instance.OkToInteractWithEveNow)
                                {
                                    if (DebugConfig.DebugInteractWithEve) Log("WSpaceSiteController: RunOnce: OpenCargoHold: !OkToInteractWithEveNow");
                                    return;
                                }

                                if (DebugConfig.DebugWspaceSiteBehavior) Log("WSpaceSiteController: RunOnce");
                                ESCache.Instance.IterateShipTargetValues();
                                ESCache.Instance.LastInteractedWithEVE = DateTime.UtcNow;
                            }
                            catch (Exception ex)
                            {
                                Log("Exception [" + ex + "]");
                            }

                            RunOnceAfterStartupAlreadyProcessed = true;
                        }
                    }
                    else
                    {
                        Log("Settings.Instance.CharacterName is still null");
                        Time.Instance.NextStartupAction = DateTime.UtcNow.AddSeconds(10);
                        RunOnceAfterStartupAlreadyProcessed = false;
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
                //    if (DebugConfig.DebugCombatMissionsBehavior) Log("CombatDontMoveController: if (ESCache.Instance.InWarp)");
                //    return;
                //}

                if (ESCache.Instance.InStation && ESCache.Instance.PauseAfterNextDock)
                {
                    if (DebugConfig.DebugWspaceSiteBehavior) Log("WSpaceSiteController: if (ESCache.Instance.InStation && ESCache.Instance.PauseAfterNextDock)");
                    ControllerManager.Instance.SetPause(true);
                    ESCache.Instance.PauseAfterNextDock = false;
                    return;
                }

                ProcessState();
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
            finally
            {
                LocalPulse = UTCNowAddSeconds(2, 4);
            }
        }

        public static void ProcessState()
        {
            try
            {
                // Wspace Site Behavior
                WspaceSiteBehavior.ProcessState();
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

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }

        #endregion Methods
    }
}