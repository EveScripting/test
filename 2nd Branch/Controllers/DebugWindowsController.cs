extern alias SC;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using EVESharpCore.Lookup;
using System;
using System.Linq;
using SC::SharedComponents.IPC;

namespace EVESharpCore.Controllers
{
    public class DebugWindowsController : BaseController
    {
        #region Constructors

        public DebugWindowsController()
        {
            IgnorePause = true;
            IgnoreModal = true;
        }

        #endregion Constructors

        #region Methods

        public void DebugWindows()
        {
            if (!DebugConfig.DebugWindows) return;

            try
            {
                //Log("Checkmodal windows called.");
                if (ESCache.Instance.Windows.Count == 0)
                {
                    Log("CheckWindows: Cache.Instance.Windows returned null or empty");
                    return;
                }

                Log("Checking Each window in Cache.Instance.Windows");

                int windowNum = 0;
                foreach (DirectWindow window in ESCache.Instance.Windows)
                {
                    windowNum++;
                    Log("[" + windowNum + "] Debug_Window.Name: [" + window.Name + "]");
                    Log("[" + windowNum + "] Debug_Window.Html: [" + window.Html + "]");
                    Log("[" + windowNum + "] Debug_Window.Type: [" + window.Guid + "]");
                    Log("[" + windowNum + "] Debug_Window.IsModal: [" + window.IsModal + "]");
                    Log("[" + windowNum + "] Debug_Window.Caption: [" + window.Caption + "]");
                    Log("--------------------------------------------------");
                }
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