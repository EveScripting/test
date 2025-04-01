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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Framework;
using SC::SharedComponents.Utility;
using SC::SharedComponents.Extensions;

namespace EVESharpCore.Controllers
{
    public class BackgroundWorkerController : BaseController, IOnFrameController
    {
        #region Constructors

        public BackgroundWorkerController() : base()
        {
            IgnorePause = false;
            IgnoreModal = false;
        }


        const string SetAllowFleetInvitesFromMessageType = "SetAllowFleetInvitesFrom";


        #endregion Constructors

        #region Fields

        private DateTime _nextSkillCheck;
        //private static ActionQueueAction _fleetHandleAction;

        private string _allowFleetInvitesFroms = null;

        private List<string> _inviteToFleet = new List<string>();


        public void SetInviteMembers(List<string> members)
        {
            _allowFleetInvitesFroms = Framework.Session.Character.Name;
            _inviteToFleet = members;
        }

        //private List<string> FleetInviteList => ESCache.Instance.EveAccount.ClientSetting.GlobalMainSetting
        //    .AutoFleetMembers?.Split(',')?.OrderBy(e => e)?.Select(e => e.Trim())?.Distinct().ToList() ?? new List<string>();



        private Dictionary<long, (DateTime, int)> _nextInvite = new Dictionary<long, (DateTime, int)>();

        private static Random _rnd = new Random();

        #endregion Fields

        #region Methods

        public override void DoWork()
        {
            try
            {
                // Local chat
                var local = ESCache.Instance.DirectEve.ChatWindows.FirstOrDefault(w =>
                    w.Name.StartsWith("chatchannel_local"));

                if (local == null || local.Messages == null || !local.Messages.Any())
                    return;

                var msgs = local.Messages.Where(m => m.MessageText.Contains(ESCache.Instance.EveAccount.CharacterName));

                if (msgs != null && msgs.Any())
                    DirectEventManager.NewEvent(
                        new DirectEvent(DirectEvents.CALLED_LOCALCHAT,
                            "We were called in local chat by: " + msgs.FirstOrDefault().Name + " Message: " +
                            msgs.FirstOrDefault().MessageText));

                // Mail blink check
                var c = ESCache.Instance;
                var mailSvc = c.DirectEve.GetLocalSvc("mailSvc", false, false);
                if (mailSvc.IsValid && mailSvc.Attribute("blinkNeocom").ToBool())
                {
                    var msg = $"Unread email detected!";
                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                }

                // Private chat
                var invWnd = c.DirectEve.Windows.FirstOrDefault(w => w.WindowId.StartsWith("ChatInvitation_"));
                if (invWnd != null && invWnd.PyWindow.IsValid)
                {
                    var invitorNameObj = invWnd.PyWindow.Attribute("invitorName");
                    var invitorName = invitorNameObj.IsValid ? invitorNameObj.ToUnicodeString() : String.Empty;
                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.PRIVATE_CHAT_RECEIVED,
                        "Private chat received. Invitor name: " + invitorName));
                }

                var chatWnd = c.DirectEve.ChatWindows.FirstOrDefault(w =>
                    w.Guid.Contains("uicontrols.Window") && w.Caption.Contains("Private Chat"));
                if (chatWnd != null && chatWnd.PyWindow.IsValid)
                {
                    var member =
                        chatWnd.Members.FirstOrDefault(m => !m.Name.Equals(ESCache.Instance.EveAccount.CharacterName));
                    var memberName = String.Empty;
                    if (member != null)
                        memberName = member.Name;

                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.PRIVATE_CHAT_RECEIVED,
                        "Private chat detected. Name: " + memberName));
                }

                // Wallet check

                if (ESCache.Instance.DirectEve.Me.Wealth != null)
                {
                    if ((long)ESCache.Instance.MyWalletBalance != (long)ESCache.Instance.DirectEve.Me.Wealth)
                    {
                        var walletWnd = ESCache.Instance.DirectEve.Windows.OfType<DirectWalletWindow>().FirstOrDefault();

                        if (walletWnd == null)
                        {
                            if (DirectEve.Interval(360000) && ESCache.Instance.InStation)
                            {
                                ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenWallet);
                                Log($"Opening wallet.");
                                return;
                            }

                            return;
                        }

                        ESCache.Instance.MyWalletBalance = ESCache.Instance.DirectEve.Me.Wealth;
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.KEEP_ALIVE, "Wallet has been changed."));
                    }
                    else
                    {
                        var walletWnd = ESCache.Instance.DirectEve.Windows.OfType<DirectWalletWindow>().FirstOrDefault();

                        if (walletWnd != null)
                        {
                            if (DirectEve.Interval(2000))
                            {
                                Log($"Closing wallet.");
                                walletWnd.Close();
                            }
                        }
                    }
                }

                // Skill check
                if (_nextSkillCheck < DateTime.UtcNow)
                {
                    _nextSkillCheck = DateTime.UtcNow.AddMinutes(new Random().Next(10, 15));

                    if (ESCache.Instance.DirectEve.Skills.AreMySkillsReady)
                    {
                        var skillInTraining = ESCache.Instance.DirectEve.Skills.SkillInTraining;
                        WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID,nameof(EveAccount.MySkillTraining), skillInTraining);
                        if (skillInTraining)
                        {
                            var last = ESCache.Instance.DirectEve.Skills.MySkillQueue.LastOrDefault();
                            if (last != null)
                            {
                                WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID,
                                    nameof(EveAccount.MySkillQueueEnds), last.TrainingEndTime);
                            }
                        }
                    }
                }

                // Locked by another player
                var targetedByPlayer = ESCache.Instance.EntitiesNotSelf.Count(e => e.IsPlayer && e.IsTargetedBy);

                if (targetedByPlayer > 0)
                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.LOCKED_BY_PLAYER,
                        $"Locked by another player. Amount: [{targetedByPlayer}]"));
            }
            catch (Exception e)
            {
                Log(e.ToString());
            }
            finally
            {
                LocalPulse = UTCNowAddMilliseconds(900, 1500);
            }
        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            return true;
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage bc)
        {

            if (bc.Command == SetAllowFleetInvitesFromMessageType.ToString())
        {
                _allowFleetInvitesFroms = bc.Payload;
            }

            Log($"BroadcastMessage received [{bc}]");

        }

        #endregion Methods

        private void HandleDynamicItemRemoteLookup()
        {
            if (!DirectEve.Interval(50, 70))
                return;

            var dynamicItemSvc = Framework.GetLocalSvc("dynamicItemSvc");
            if (!dynamicItemSvc.IsValid)
                return;

            var req = DirectItem.RequestedDynamicItems.ToList();

            foreach (var itemId in req)
            {
                if (DirectItem.FinishedRemoteCallDynamicItems.Contains(itemId))
                    continue;

                DirectItem.FinishedRemoteCallDynamicItems.Add(itemId);
                DirectItem.RequestedDynamicItems.Remove(itemId);
                //Log($"GetDynamicItem [{itemId}]");
                Framework.ThreadedCall(dynamicItemSvc["GetDynamicItem"], itemId);
                return;
            }
        }

        public void OnFrame()
        {
            HandleDynamicItemRemoteLookup();
        }
    }
}