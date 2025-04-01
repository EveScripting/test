extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Lookup;
using System;
using System.Linq;
using SC::SharedComponents.EVE;
using EVESharpCore.Controllers.Abyssal;
using SC::SharedComponents.IPC;
using EVESharpCore.Framework.Lookup;

namespace EVESharpCore.Controllers
{
    /// <summary>
    ///     Combat Controller
    ///     Kill things as needed, respecting pause...
    ///     Note: will not move your ship, ever.
    /// </summary>
    public class CombatController : BaseController
    {
        #region Constructors

        public CombatController()
        {
            IgnorePause = false;
            IgnoreModal = false;
            AllowRunInStation = false;
            AllowRunInAbyssalSpace = false;
        }

        #endregion Constructors

        #region Methods

        public static void ProcessState()
        {
            if (DebugConfig.DebugDisableCombat)
                return;

            if (!Combat.KillTargets()) return;
        }

        public override void DoWork()
        {
            try
            {
                if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                {
                    if (Time.Instance.LastInWarp.AddSeconds(1) > DateTime.UtcNow)
                        return;

                    if (ESCache.Instance.Stargates.Count > 0 && Time.Instance.LastInWarp.AddSeconds(10) > DateTime.UtcNow)
                        if (ESCache.Instance.ClosestStargate != null && ESCache.Instance.ClosestStargate.IsOnGridWithMe && ESCache.Instance.ClosestStargate.Distance < (int)Distances.JumpRange)
                        {
                            if (DebugConfig.DebugCombat) Log("We are within [" + Distances.JumpRange + "] of a stargate, do nothing while we wait to jump.");
                            return;
                        }
                }

                if (!Settings.Instance.DefaultSettingsLoaded)
                    Settings.Instance.LoadSettings_Initialize();

                if (DebugConfig.DebugCombatController) Log("CombatController.DoWork()");

                ProcessState();
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
            finally
            {
                if (ESCache.Instance.InWarp)
                    LocalPulse = UTCNowAddMilliseconds(5500, 7500);
                else if (Combat.PotentialCombatTargets.Count == 0 && ESCache.Instance.SelectedController != nameof(EveAccount.AvailableControllers.HydraController))
                    LocalPulse = UTCNowAddMilliseconds(5500, 7500);
                else if (ESCache.Instance.Weapons.All(i => i.IsActive))
                    LocalPulse = UTCNowAddMilliseconds(2200, 2400);
                else
                    LocalPulse = UTCNowAddMilliseconds(600, 800);
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