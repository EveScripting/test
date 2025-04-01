/*
* Created by SharpDevelop.
* User: duketwo
* Date: 28.05.2016
* Time: 18:51
*
* To change this template use Tools | Options | Coding | Edit Standard Headers.
*/

extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SC::SharedComponents.SharedMemory;

namespace EVESharpCore.Controllers
{
    /// <summary>
    ///     Description of LoginController.
    /// </summary>
    public class LoginController : BaseController
    {
        #region Fields

        private readonly bool DebugLoginController = false;

        #endregion Fields

        #region Constructors

        public LoginController()
        {
            IgnorePause = false;
            IgnoreModal = true;
            RunBeforeLoggedIn = true;
            IgnoreValidSession = true;
        }

        private SharedArray<bool> _sharedArray;
        #endregion Constructors

        #region Properties

        public static bool LoggedIn { get; set; }

        //public DateTime DelayLogin { get; set; } = DateTime.UtcNow.AddSeconds(30);

        public DateTime LoginTimeout { get; set; } = DateTime.UtcNow.AddSeconds(120);

        private int Iterations { get; set; }

        #endregion Properties

        #region Methods

        public override void DoWork()
        {
            LoginEVE();
        }

        private void CreateToon()
        {
            try
            {
                Log("CreateToon: Start");
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return;
            }
        }

        private void LoginEVE()
        {
            try
            {
                if (string.IsNullOrEmpty(ESCache.Instance.EveAccount.CharacterName))
                {
                    ControllerManager.Instance.SetPause(true);
                    return;
                }
                //else if (IsPaused && !ESCache.Instance.EveAccount.ManuallyPausedViaUI) ControllerManager.Instance.SetPause(false);

                Iterations++;
                if (DebugLoginController) Log("LoginController: Iterations [" + Iterations + "] LoggedIn [" + LoggedIn + "] IsReady [" + ESCache.Instance.DirectEve.Session.IsReady + "]");

                if (ESCache.Instance.DirectEve.Login.IsConnecting || ESCache.Instance.DirectEve.Login.IsLoading || ESCache.Instance.DirectEve.Layers.AtLogin)
                {
                    Log("Account logged in, waiting for character selection: IsConnecting [" + ESCache.Instance.DirectEve.Login.IsConnecting + "] IsLoading [" + ESCache.Instance.DirectEve.Login.IsLoading + "] AtLogin [" + ESCache.Instance.DirectEve.Login.AtLogin + "]");
                    LocalPulse = UTCNowAddSeconds(4, 5);
                    return;
                }

                //ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastQuestorSessionReady), DateTime.UtcNow);

                // if session is ready on first iteration, we've already been logged in, hence not delaying the login
                if (ESCache.Instance.DirectEve.Session.IsReady) //|| DelayLogin > DateTime.UtcNow))
                {
                    // check if the rcode return values have been verified
                    //_sharedArray = new SharedArray<bool>(ESCache.Instance.CharName + nameof(UsedSharedMemoryNames.RcodeVerified));
                    //if (!_sharedArray[0])
                    //{
                    //    ESCache.Instance.DisableThisInstance();
                    //    ESCache.Instance.CloseEve(false, "ERROR: RCode values not verified! Disabling this instance.");
                    //}

                    //WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(ESCache.Instance.CharName,
                    //    nameof(EveAccount.LoggedIn), true);

                    IsWorkDone = true; // once we selected the char the work is done, or if the session is ready ( we already have been logged in )
                    LoggedIn = true;
                    Log("Successfully logged in. LoggedIn [" + LoggedIn + "] IsWorkDone [" + IsWorkDone + "]");
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastQuestorStarted), DateTime.UtcNow);
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastSessionReady), DateTime.UtcNow);
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.DoneLaunchingEveInstance), true);
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.RestartOfEveClientNeeded), false);
                    //ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.AllowSimultaneousLogins), false);

                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LootValueGatheredToday), 0);

                    Log("SubEnd: " + ESCache.Instance.DirectEve.Me.SubTimeEnd);
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.SubEnd), ESCache.Instance.DirectEve.Me.SubTimeEnd);
                    //if (!ESCache.Instance.EveAccount.Autostart)
                    //    ControllerManager.Instance.SetPause(true);
                    return;
                }

                //
                // if we paused the bot before login finished and manually logged in,
                // if we unpause we need the "if (ESCache.Instance.DirectEve.Session.IsReady)" check to be before this
                // LoginTimeout Check or we will immediately close eve.
                //
                if (LoginTimeout < DateTime.UtcNow)
                {
                    ESCache.Instance.CloseEveReason = "Login timed out. Exiting.";
                    ESCache.Instance.BoolRestartEve = true;
                }

                if (ESCache.Instance.DirectEve.Login.AtCharacterSelection && ESCache.Instance.DirectEve.Login.IsCharacterSelectionReady)
                {
                    /**
                    if (string.IsNullOrEmpty(ESCache.Instance.EveAccount.CharacterName))
                    {
                        CreateToon();
                        return;
                    }

                    if (ESCache.Instance.EveAccount.CharacterName.ToLower() == "none".ToLower())
                        return;

                    if (ESCache.Instance.EveAccount.CharacterName.ToLower() == "nothing".ToLower())
                        return;

                    if (ESCache.Instance.EveAccount.CharacterName.ToLower() == "blank".ToLower())
                        return;
                    **/

                    if (DateTime.UtcNow > ESCache.Instance.NextSlotActivate)
                    {
                        Log("SubEnd: " + ESCache.Instance.DirectEve.Me.SubTimeEnd);
                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.SubEnd), ESCache.Instance.DirectEve.Me.SubTimeEnd);

                        if (ESCache.Instance.DirectEve.Login.CharacterSlots.Count > 0)
                        {
                            List<string> charsOnAccount = ESCache.Instance.DirectEve.Login.CharacterSlots.Select(c => c.CharName).ToList();
                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.CharsOnAccount), charsOnAccount);
                            if (charsOnAccount.All(i => string.IsNullOrEmpty(i)))
                            {
                                Log("No characters found on account [" + ESCache.Instance.EveAccount.MaskedCharacterName + "]");
                                CreateToon();
                                return;
                            }

                            foreach (DirectLoginSlot slot in ESCache.Instance.DirectEve.Login.CharacterSlots)
                            {
                                if (slot.CharId.ToString(CultureInfo.InvariantCulture) != ESCache.Instance.EveAccount.CharacterName &&
                                    !string.Equals(slot.CharName, ESCache.Instance.EveAccount.CharacterName,
                                        StringComparison.OrdinalIgnoreCase))
                                    continue;

                                LocalPulse = UTCNowAddSeconds(1, 2);
                                if (slot.Activate())
                                {
                                    Log("Activating character [" + slot.CharName + "]");
                                    ESCache.Instance.NextSlotActivate = DateTime.UtcNow.AddSeconds(10);
                                    LoggedIn = true;
                                    return;
                                }

                                return;
                            }
                            Log("Character id/name [" + ESCache.Instance.EveAccount.MaskedCharacterName + "] not found, retrying in 10 seconds");
                        }
                        else Log("ESCache.Instance.DirectEve.Login.CharacterSlots.Count [" + ESCache.Instance.DirectEve.Login.CharacterSlots.Count + "]");
                    }
                }

                if (ESCache.Instance.DirectEve.CharacterCreation.AtCharacterCreation)
                {
                    if (DirectEve.Interval(10000)) Log("Character creation is open, waiting...");
                    ControllerManager.Instance.SetPause(true);
                    return;
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