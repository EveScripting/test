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
using EVESharpCore.Framework.Events;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Events;
using SC::SharedComponents.IPC;
using SC::SharedComponents.Py;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Framework;
using EVESharpCore.Lookup;

namespace EVESharpCore.Controllers
{
    public class NotificationController : BaseController
    {
        #region Constructors

        public NotificationController() : base()
        {
            IgnorePause = false;
            IgnoreModal = false;
            AllowRunInAbyssalSpace = false;
        }

        #endregion Constructors

        #region Fields

        private DateTime _nextSkillCheck;

        #endregion Fields

        #region Methods

        public override void DoWork()
        {
            try
            {
                if (!DirectEve.Interval(5000))
                    return;

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CombatDontMoveController))
                    return;

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.HydraController))
                    return;
                //ToDo
                //FixMe
                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController))
                    return;

                if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalController))
                    return;

                if (DebugConfig.DebugDisableNotificationController)
                    return;

                if (ESCache.Instance.DirectEve.Session.CharactersInLocal.Count > 0)
                {
                    // Local chat
                    var local = ESCache.Instance.DirectEve.ChatWindows.Find(w => w.Name.StartsWith("chatchannel_local"));

                    if (local == null || local.Messages == null || local.Messages.Count == 0)
                        return;

                    var msgs = local.Messages.Where(m => m.MessageText.Contains(Logging.Log.CharacterName));

                    if (msgs != null && msgs.Any())
                        DirectEventManager.NewEvent(
                            new DirectEvent(DirectEvents.CALLED_LOCALCHAT, "We were called in local chat by: " + msgs.FirstOrDefault().Name + " MessageText: " + msgs.FirstOrDefault().MessageText));

                    ReadChat();
                    // Private chat
                    //var c = ESCache.Instance;

                    //PyObject mailSvc = ESCache.Instance.DirectEve.GetLocalSvc("mailSvc", false, false);

                    //if (mailSvc.IsValid && mailSvc.Attribute("blinkNeocom").ToBool())
                    //{
                    //    string msg = $"Unread email detected!";
                    //    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                    //}

                    var InvWnd = ESCache.Instance.DirectEve.Windows.Find(w => w.Guid.Contains("form.ChatInviteWindow"));
                    if (InvWnd != null && InvWnd.PyWindow.IsValid)
                    {
                    var invitorNameObj = InvWnd.PyWindow.Attribute("invitorName");
                    var invitorName = invitorNameObj.IsValid ? invitorNameObj.ToUnicodeString() : String.Empty;
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.PRIVATE_CHAT_RECEIVED, "Private chat received. Invitor name: " + invitorName));
                    }

                    var chatWnd = ESCache.Instance.DirectEve.ChatWindows.Find(w => w.Guid.Contains("uicontrols.Window") && w.Caption.Contains("Private Chat"));
                    if (chatWnd != null && chatWnd.PyWindow.IsValid)
                    {
                        var member = chatWnd.Members.Find(m => !m.Name.Equals(Logging.Log.CharacterName));
                        var memberName = string.Empty;
                        if (member != null)
                            memberName = member.Name;

                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.PRIVATE_CHAT_RECEIVED, "Private chat detected. Name: " + memberName));
                    }

                    try
                    {
                        /**
                    if (ESCache.Instance.DirectEve.Me.Wealth == null)
                    {
                        // Wallet check
                        if (ESCache.Instance.MyWalletBalance != ESCache.Instance.DirectEve.Me.Wealth)
                        {
                            ESCache.Instance.MyWalletBalance = ESCache.Instance.DirectEve.Me.Wealth;
                            if (DirectEve.Interval(30000)) DirectEventManager.NewEvent(new DirectEvent(DirectEvents.KEEP_ALIVE, "Wallet has been changed."));
                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(ESCache.Instance.EveAccount.WalletBalance), Math.Round((float)ESCache.Instance.MyWalletBalance, 0));
                        }
                    }
                    **/
                    }
                    catch (Exception){}

                    if (ESCache.Instance.InSpace)
                    {
                        // locked by another player
                        int targetedByPlayer = ESCache.Instance.EntitiesNotSelf.Count(e => e.IsPlayer && e.IsTargetedBy);

                        if (targetedByPlayer > 0)
                            DirectEventManager.NewEvent(new DirectEvent(DirectEvents.LOCKED_BY_PLAYER, "Locked by another player."));
                    }
                }
            }
            catch (Exception e)
            {
                Log(e.ToString());
            }
            finally
            {
                LocalPulse = UTCNowAddSeconds(1, 2);
            }
        }

        private void ReadChat()
        {
            foreach (DirectChatWindow chatWindow in ESCache.Instance.DirectEve.ChatWindows)
            {
                if (chatWindow.Messages == null)
                    continue;

                if (!chatWindow.Messages.Any())
                    continue;

                if (chatWindow.NewMessages == null)
                    continue;

                if (!chatWindow.NewMessages.Any())
                    continue;

                foreach (DirectChatMessage message in chatWindow.NewMessages)
                {
                    if (message.directChatWindow.Name.Contains("_corp"))
                    {
                        if (!message.MessageText.Contains(ESCache.Instance.CharName))
                            continue;
                    }

                    if (message.directChatWindow.DisplayName.ToLower().Contains("Rookie Help".ToLower()))
                    {
                        if (!message.MessageText.Contains(ESCache.Instance.CharName))
                            continue;
                    }

                    if (message.directChatWindow.Name.Contains("chatchannel_local"))
                    {
                        if (message.MessageText.Contains("0xff48ff00")) //This is green!
                        {
                            //pass this message somehow to the CombatMissionController! It is a mission status update
                        }

                        if (ESCache.Instance.DirectEve.Session.SolarSystem.Name == "Jita")
                        {
                            continue;
                        }
                    }

                    if (DebugConfig.DebugLogChatMessagesToBotLogFile) Logging.Log.WriteLine("[" + message.directChatWindow.DisplayName + "][" + message.Name + "][" + message.MessageText + "][" + message.Time + "][" + message.ColorKey + "]");
                }
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