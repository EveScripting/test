extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Lookup;
using System;
using System.Linq;
using EVESharpCore.Framework.Events;
using SC::SharedComponents.Events;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Controllers.Abyssal;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Lookup;

namespace EVESharpCore.Controllers
{
    /// <summary>
    ///     Description of DefenseController.
    /// </summary>
    public class DefenseController : BaseController
    {
        #region Constructors

        public DefenseController()
        {
            IgnorePause = true;
            IgnoreModal = false;
            AllowRunInStation = false;
            AllowRunInAbyssalSpace = false;
        }

        #endregion Constructors

        #region Methods

        public static void ProcessState()
        {
            if (DebugConfig.DebugDisableDefense)
            {
                if (DirectEve.Interval(30000)) DirectEventManager.NewEvent(new DirectEvent(DirectEvents.KEEP_ALIVE, "Defense"));
                return;
            }

            // defense
            Defense.ProcessState();
        }

        public override void DoWork()
        {
            try
            {
                if (Time.Instance.Started_DateTime.AddSeconds(8) > DateTime.UtcNow)
                    return;

                if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                    if (ESCache.Instance.Stargates.Count > 0)
                        if (ESCache.Instance.ClosestStargate != null && ESCache.Instance.ClosestStargate.IsOnGridWithMe && ESCache.Instance.ClosestStargate.Distance < (int)Distances.JumpRange)
                        {
                            if (DebugConfig.DebugDefense) Log("We are within [" + Distances.JumpRange + "] of a stargate, do nothing while we wait to jump.");
                            return;
                        }

                if (!Settings.Instance.DefaultSettingsLoaded)
                    Settings.Instance.LoadSettings_Initialize();

                //if (DebugConfig.DebugDefenseController) Log("CombatController.DoWork()");

                ProcessState();
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
            finally
            {
                LocalPulse = UTCNowAddMilliseconds(500, 1000);

                if (!ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule || i.IsArmorRepairModule))
                    LocalPulse = UTCNowAddMilliseconds(5500, 6000);
                else if (Combat.PotentialCombatTargets.Count == 0 && ESCache.Instance.ActiveShip.ShieldPercentage == 100 && ESCache.Instance.ActiveShip.ArmorPercentage == 100)
                    LocalPulse = UTCNowAddMilliseconds(5500, 6000);
            }
        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalController)) // do not run it while an abyss controller is running
            {
                return false;
            }

            return true;
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }

        #endregion Methods
    }
}