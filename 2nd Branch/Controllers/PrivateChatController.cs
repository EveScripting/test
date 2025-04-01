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
using SC::SharedComponents.Py;
using System;
using System.Linq;

namespace EVESharpCore.Controllers
{
    public class PrivateChatController : BaseController
    {
        #region Constructors

        public PrivateChatController()
        {
            IgnorePause = false;
            IgnoreModal = false;
        }

        #endregion Constructors

        #region Methods

        public override void DoWork()
        {
            if (IsWorkDone || LocalPulse > DateTime.UtcNow)
                return;

            try
            {
                ESCache c = ESCache.Instance;

                PyObject mailSvc = c.DirectEve.GetLocalSvc("mailSvc", false, false);

                if (mailSvc.IsValid && mailSvc.Attribute("blinkNeocom").ToBool())
                {
                    string msg = $"Unread evemail detected!";
                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                }

                DirectWindow InvWnd = c.DirectEve.Windows.FirstOrDefault(w => w.Type.Contains("form.ChatInviteWindow"));
                if (InvWnd != null && InvWnd.PyWindow.IsValid)
                {
                    PyObject invitorNameObj = InvWnd.PyWindow.Attribute("invitorName");
                    string invitorName = invitorNameObj.IsValid ? invitorNameObj.ToUnicodeString() : string.Empty;
                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.PRIVATE_CHAT_RECEIVED, "Private chat received. Invitor name: " + invitorName));
                }

                DirectChatWindow chatWnd = c.DirectEve.ChatWindows.FirstOrDefault(w => w.Type.Contains("uicontrols.Window") && w.Caption.Contains("Private Chat"));
                if (chatWnd != null && chatWnd.PyWindow.IsValid)
                {
                    DirectCharacter member = chatWnd.Members.FirstOrDefault(m => !m.Name.Equals(ESCache.Instance.EveAccount.CharacterName));
                    string memberName = string.Empty;
                    if (member != null)
                        memberName = member.Name;

                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.PRIVATE_CHAT_RECEIVED, "Private chat detected. Name: " + memberName));
                }
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