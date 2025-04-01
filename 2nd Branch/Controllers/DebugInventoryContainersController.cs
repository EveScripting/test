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
    public class DebugInventoryContainersController : BaseController
    {
        #region Constructors

        public DebugInventoryContainersController()
        {
            IgnorePause = true;
            IgnoreModal = true;
        }

        #endregion Constructors

        #region Methods

        public void DebugWindows()
        {
            if (!DebugConfig.DebugInventoryContainers) return;
            return;
            try
            {
                if (ESCache.Instance.Windows.Count == 0)
                {
                    Log("CheckWindows: Cache.Instance.Windows returned null or empty");
                    return;
                }

                if (ESCache.Instance.HighTierLootCorpHangar == null)
                {
                    Log("Waiting for Corporate Hangar to open");
                    return;
                }

                Log("Corporate Hangar Division [" + Settings.Instance.HighTierLootCorpHangarDivisionNumber + "] has [" + ESCache.Instance.HighTierLootCorpHangar.Items.Count + "] items");
            }
            catch (Exception ex)
            {
                Log("Exception: " + ex);
            }
        }

        public override void DoWork()
        {
            try
            {
                if (!Settings.Instance.DefaultSettingsLoaded)
                    Settings.Instance.LoadSettings_Initialize();

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

        public void ProcessState()
        {
            DebugWindows();
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }

        #endregion Methods
    }
}