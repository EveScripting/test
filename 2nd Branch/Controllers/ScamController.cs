/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 26.06.2016
 * Time: 18:31
 *
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

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
    ///     Description of ExampleController.
    /// </summary>
    public class ScamController : BaseController
    {
        #region Fields

        private static DateTime _nextMessage;

        #endregion Fields

        #region Constructors

        public ScamController() : base()
        {
            AllowRunInSpace = false;
            IgnorePause = false;
            IgnoreModal = false;
        }

        #endregion Constructors

        #region Methods

        public override void DoWork()
        {
            try
            {
                if (_nextMessage >= DateTime.UtcNow)
                    return;

                _nextMessage = DateTime.UtcNow.AddSeconds(ESCache.Instance.RandomNumber(35, 55));

                if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                {
                    if (Time.Instance.LastInWarp.AddSeconds(4) > DateTime.UtcNow)
                        return;

                    if (ESCache.Instance.Stargates.Count > 0 && Time.Instance.LastInWarp.AddSeconds(30) > DateTime.UtcNow)
                        if (ESCache.Instance.ClosestStargate != null && ESCache.Instance.ClosestStargate.IsOnGridWithMe && ESCache.Instance.ClosestStargate.Distance < 10000)
                            return;
                }

                try
                {
                    var local = (DirectChatWindow)ESCache.Instance.DirectEve.Windows.Find(w => w.Name.StartsWith("chatchannel_local"));

                    if (local == null)
                        return;

                    local.Speak("Send me three million ISK for the best joke you've ever heard. Serving eve fellows with epic jokes since 2009.");
                }
                catch (Exception e)
                {
                    Log(string.Format("Exception {0}", e));
                }
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