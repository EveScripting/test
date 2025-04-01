extern alias SC;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Lookup;
using SC::SharedComponents.IPC;
using System;
using System.Linq;

namespace EVESharpCore.Controllers
{
    /// <summary>
    ///     Salvage Controller
    ///     Tractor and Salvage as needed, ignoring pause...
    ///     Note: will not move your ship, ever.
    /// </summary>
    public class SalvageGridController : BaseController
    {
        #region Constructors

        public SalvageGridController() : base()
        {
            IgnorePause = true;
            IgnoreModal = false;
        }

        #endregion Constructors

        #region Methods

        public static void ProcessState()
        {
            try
            {
                if (DebugConfig.DebugDisableSalvage)
                    return;

                // salvage grid behavior
                //SalvageGridBehavior.ProcessState();
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
        }

        public override void DoWork()
        {
            if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
            {
                if (Time.Instance.LastInWarp.AddSeconds(2) > DateTime.UtcNow)
                    return;

                if (ESCache.Instance.Stargates.Count > 0 && Time.Instance.LastInWarp.AddSeconds(30) > DateTime.UtcNow)
                    if (ESCache.Instance.ClosestStargate != null && ESCache.Instance.ClosestStargate.IsOnGridWithMe && ESCache.Instance.ClosestStargate.Distance < (int)Distances.JumpRange)
                    {
                        if (DebugConfig.DebugCleanup) Log("CheckModalWindows: We are within 10k of a stargate, do nothing while we wait to jump.");
                        return;
                    }
            }

            if (!Settings.Instance.DefaultSettingsLoaded)
                Settings.Instance.LoadSettings_Initialize();

            ProcessState();
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