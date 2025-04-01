extern alias SC;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using EVESharpCore.Lookup;
using SC::SharedComponents.IPC;
using System;
using System.Linq;

namespace EVESharpCore.Controllers
{
    /// <summary>
    ///     Description of DefenseController.
    /// </summary>
    public class DebugLogItemsInCargoController : BaseController
    {
        #region Constructors

        public DebugLogItemsInCargoController()
        {
            IgnorePause = true;
            IgnoreModal = false;
        }

        #endregion Constructors

        #region Methods

        public static void ProcessState()
        {
            Log("Log items in cargo");

            if (ESCache.Instance.CurrentShipsCargo == null || ESCache.Instance.CurrentShipsCargo.Items.Count == 0)
                return;

            int itemNum = 0;
            foreach (DirectItem item in ESCache.Instance.CurrentShipsCargo.Items)
            {
                itemNum++;
                Log("[" + itemNum + "][" + item.TypeName + "] TypeId [" + item.TypeId + "] GroupId [" + item.GroupId + "] CategoryId [" + item.CategoryId + "]");
            }

            Log("done");

            ControllerManager.Instance.RemoveController(typeof(DebugLogItemsInCargoController));
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