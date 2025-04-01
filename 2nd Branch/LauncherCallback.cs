extern alias SC;
using System;
using System.ServiceModel;
using System.Threading;
using EVESharpCore.Cache;
using EVESharpCore.Controllers;
using EVESharpCore.Controllers.ActionQueue.Actions;
using EVESharpCore.Logging;
using EVESharpCore.Traveller;
using EVESharpCore.Questor.States;
using SC::SharedComponents.IPC;

using System.Reflection;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Documents;
using SC::SharedComponents.Utility;

namespace EVESharpCore
{
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant, UseSynchronizationContext = false)]
    public class LauncherCallback : IDuplexServiceCallback
    {
        #region Methods

        public void GotoHomebaseAndIdle()
        {
            Log.WriteLine("Settings.Instance.AutoStart = false, CurrentCombatMissionBehaviorState  = CombatMissionsBehaviorState.GotoBase: PauseAfterNextDock [true]");
            ESCache.Instance.PauseAfterNextDock = true;
            string msg = string.Format("Set [{0}] going to homebase and idle.", ESCache.Instance.EveAccount.MaskedCharacterName);
            if (ESCache.Instance.InAbyssalDeadspace)
            {
                msg = string.Format("Set [{0}] We are in AbyssalDeadspace: setting PauseAfterNextDock to true instead", ESCache.Instance.EveAccount.MaskedCharacterName);
                WCFClient.Instance.GetPipeProxy.RemoteLog(msg);
                return;
            }

            State.CurrentQuestorState = QuestorState.Start;
            State.CurrentTravelerState = TravelerState.Idle;
            State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
            Traveler.Destination = null;
            WCFClient.Instance.GetPipeProxy.RemoteLog(msg);
        }

        [DllImport("MemMan.dll", EntryPoint = "RestartEveSharpCore")]
        public static extern void RestartESCore();

        public void RestartEveSharpCore()
        {
            RestartESCore();
        }

        public void GotoJita()
        {
            new Thread(() =>
            {
                try
                {
                    if (ControllerManager.Instance.GetController<ActionQueueController>().IsActionQueueEmpty)
                    {
                        string msg = string.Format("Set [{0}] going to Jita and pause.", ESCache.Instance.EveAccount.MaskedCharacterName);
                        WCFClient.Instance.GetPipeProxy.RemoteLog(msg);
                        Log.WriteLine("Adding GotoJitaAction");
                        new GotoJitaAction().Initialize().QueueAction();
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception:" + ex);
                }
            }).Start();
        }

        public void OnCallback()
        {
            // Method intentionally left empty.
        }

        public void PauseAfterNextDock()
        {
            Log.WriteLine("PauseAfterNextDock: was called via the EveSharpLauncher!");
            ESCache.Instance.PauseAfterNextDock = true;
            string msg = string.Format("PauseAfterNextDock [true]", ESCache.Instance.EveAccount.MaskedCharacterName);
            WCFClient.Instance.GetPipeProxy.RemoteLog(msg);
        }

        public void LogoffAfterNextDock()
        {
            Log.WriteLine("LogoffAfterNextDock: was called via the EveSharpLauncher!");
            ESCache.Instance.DeactivateScheduleAndCloseEveAfterNextDock = true;
            string msg = string.Format("LogoffAfterNextDock [true]", ESCache.Instance.EveAccount.MaskedCharacterName);
            WCFClient.Instance.GetPipeProxy.RemoteLog(msg);
        }

        public void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {
            var controller = ControllerManager.Instance.GetController(broadcastMessage.TargetController);
            if (controller != null)
            {
                controller.ReceiveBroadcastMessage(broadcastMessage);
            }
            else
            {
                Logging.Log.WriteLine($"Controller could not be found [{broadcastMessage.TargetController}]");
            }
        }

        #endregion Methods
    }
}