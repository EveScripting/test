/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 09.09.2016
 * Time: 14:35
 *
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Events;
using SC::SharedComponents.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Controllers
{
    public class LocalChatController : BaseController
    {
        #region Constructors

        public LocalChatController()
        {
            IgnorePause = false;
            IgnoreModal = false;
        }

        #endregion Constructors

        #region Methods

        public override void DoWork()
        {
            try
            {
                DirectChatWindow local = ESCache.Instance.DirectEve.ChatWindows.FirstOrDefault(w => w.Name.StartsWith("chatchannel_local"));

                if (local == null || local.Messages == null || !local.Messages.Any())
                    return;

                IEnumerable<DirectChatMessage> msgs = local.Messages.Where(m => m.Message.Contains(ESCache.Instance.EveAccount.CharacterName));

                if (msgs != null && msgs.Any())
                    DirectEventManager.NewEvent(
                        new DirectEvent(DirectEvents.CALLED_LOCALCHAT, "We were called in local chat by: " + msgs.FirstOrDefault().Name + " Message: " + msgs.FirstOrDefault().Message));
            }
            catch (Exception e)
            {
                Log(e.ToString());
            }
            finally
            {
                LocalPulse = GetUTCNowDelaySeconds(25, 45);
            }
        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            return true;
        }

        #endregion Methods
    }
}