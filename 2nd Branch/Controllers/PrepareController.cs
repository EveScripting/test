extern alias SC;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Lookup;
using SC::SharedComponents.IPC;
using System;
using System.Linq;

namespace EVESharpCore.Controllers
{
    /// <summary>
    ///     Description of PrepareController.
    ///     Runs before the "main" bot to gather things needed during use.
    ///     Ships
    ///     Station Containers
    ///     Fittings (modules and drones)
    ///     DefinedAmmoTypes
    ///     Etc.
    /// </summary>
    public class PrepareController : BaseController
    {
        #region Constructors

        public PrepareController()
        {
            IgnorePause = false;
            IgnoreModal = false;
        }

        #endregion Constructors

        #region Methods

        public static void ProcessState()
        {
            //
            // Make a list of items we need
            //

            //
            // Go Buy any NPC items we need
            //
            //BuyNpcItems.ProcessState();

            //
            // Go Buy any market (jita?) items we need
            //
        }

        public override void DoWork()
        {
            try
            {
                if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                    if (ESCache.Instance.Stargates.Count > 0)
                        if (ESCache.Instance.ClosestStargate != null && ESCache.Instance.ClosestStargate.IsOnGridWithMe && ESCache.Instance.ClosestStargate.Distance < 10000)
                        {
                            if (DebugConfig.DebugCleanup) Log("CheckModalWindows: We are within 10k of a stargate, do nothing while we wait to jump.");
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