extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Lookup;
using System;
using System.Linq;
using EVESharpCore.Controllers.Abyssal;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;
using EVESharpCore.Framework.Lookup;

namespace EVESharpCore.Controllers
{
    /// <summary>
    ///     TargetCombatants Controller
    ///     Target NPCs and Aggressed Players as needed, respecting pause...
    ///     Note: will not move your ship, ever.
    /// </summary>
    public class TargetCombatantsController : BaseController
    {
        #region Constructors

        public TargetCombatantsController()
        {
            AllowRunInStation = false;
            IgnorePause = false;
            IgnoreModal = false;
        }

        #endregion Constructors

        #region Methods

        public static void ProcessState()
        {
            try
            {
                if (DebugConfig.DebugDisableTargetCombatants)
                {
                    if (DebugConfig.DebugTargetCombatants) Log("ProcessState: if (DebugConfig.DebugDisableTargetCombatants)");
                    return;
                }

                // target combatants
                if (!Combat.TargetCombatants()) return;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
        }

        public override void DoWork()
        {
            try
            {
                if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                {
                    if (ESCache.Instance.InWarp)
                        return;

                    if (Time.Instance.LastInWarp.AddSeconds(1) > DateTime.UtcNow)
                    {
                        if (DebugConfig.DebugTargetCombatantsController) Log("DebugTargetCombatantsController: if (Time.Instance.LastInWarp.AddSeconds(1) > DateTime.UtcNow)");
                        return;
                    }

                    if (ESCache.Instance.Stargates.Count > 0 && Time.Instance.LastInWarp.AddSeconds(10) > DateTime.UtcNow)
                        if (ESCache.Instance.ClosestStargate != null && ESCache.Instance.ClosestStargate.IsOnGridWithMe && ESCache.Instance.ClosestStargate.Distance < (int)Distances.JumpRange)
                        {
                            if (DebugConfig.DebugTargetCombatants) Log("We are within [" + Distances.JumpRange + "] of a stargate, do nothing while we wait to jump.");
                            return;
                        }
                }

                if (!Settings.Instance.DefaultSettingsLoaded)
                    Settings.Instance.LoadSettings_Initialize();

                if (DebugConfig.DebugTargetCombatantsController) Log("TargetCombatantsController.DoWork()");

                ProcessState();

                if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalController))
                {
                    IsWorkDone = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
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