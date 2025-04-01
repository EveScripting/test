
extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Stats;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;
using System;

namespace EVESharpCore.Controllers
{
    public class TransportItemsTypesController : BaseController
    {
        #region Constructors

        public TransportItemsTypesController()
        {
            IgnorePause = false;
            IgnoreModal = false;
            Time.Instance.NextStartupAction = DateTime.UtcNow;
            Time.Instance.Started_DateTime = DateTime.UtcNow;
        }

        #endregion Constructors

        #region Properties

        private bool RunOnceAfterStartupalreadyProcessed { get; set; }

        #endregion Properties

        #region Methods

        public static void ProcessState()
        {
            //Industrial Behavior
            TransportItemTypesBehavior.ProcessState();
        }

        public override void DoWork()
        {
            try
            {
                if (!Settings.Instance.DefaultSettingsLoaded)
                {
                    if (DebugConfig.DebugCombatMissionsBehavior) Log("Industrial: Loading Settings");
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
                                    if (DebugConfig.DebugInteractWithEve) Log("Industrial: RunOnce: OpenCargoHold: !OkToInteractWithEveNow");
                                    return;
                                }

                                if (DebugConfig.DebugCombatMissionsBehavior) Log("Industrial: RunOnce");
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

                if (ESCache.Instance.InWarp)
                {
                    if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log("Industrial: if (ESCache.Instance.InWarp)");
                    return;
                }

                if (ESCache.Instance.InStation && ESCache.Instance.PauseAfterNextDock)
                {
                    Log("Industrial: if (ESCache.Instance.InStation && ESCache.Instance.PauseAfterNextDock)");
                    ControllerManager.Instance.SetPause(true);
                    ESCache.Instance.PauseAfterNextDock = false;
                    return;
                }

                if (ESCache.Instance.InStation && ESCache.Instance.DeactivateScheduleAndCloseEveAfterNextDock)
                {
                    Log("Industrial: if (ESCache.Instance.InStation && ESCache.Instance.DeactivateScheduleAndCloseAfterNextDock)");
                    if (ESCache.Instance.EveAccount.UseScheduler)
                    {
                        WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.UseScheduler), false);
                        ESCache.Instance.DeactivateScheduleAndCloseEveAfterNextDock = false;
                        return;
                    }

                    ESCache.Instance.CloseEveReason = "DeactivateScheduleAndCloseAfterNextDock";
                    ESCache.Instance.BoolCloseEve = true;
                    return;
                }

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

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }

        #endregion Methods
    }
}