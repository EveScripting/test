extern alias SC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Activities;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Questor.Stats;
using EVESharpCore.Questor.States;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Extensions;
using SC::SharedComponents.IPC;
using SC::SharedComponents.Utility;
using System.Management.Instrumentation;
using System.Windows.Media.TextFormatting;
using EVESharpCore.Controllers;
using System.Collections.ObjectModel;

namespace EVESharpCore.Questor.BackgroundTasks
{
    public static class Cleanup
    {
        #region Constructors

        static Cleanup()
        {
        }

        #endregion Constructors

        #region Fields

        private static bool doneUsingRepairWindow;
        private static bool LogEVEWindowDetails;
        private static int _droneBayClosingAttempts;
        public static int intReDockTorepairModules = 0;

        #endregion Fields

        #region Methods

        public static bool RepairItems()
        {
            try
            {
                if (DateTime.UtcNow < Time.Instance.NextRepairItemsAction)
                    return false;

                if (ESCache.Instance.Windows.Count == 0)
                    return false;

                if (ESCache.Instance.InStation)
                {
                    foreach (DirectWindow window in ESCache.Instance.Windows.OrderBy(i => Guid.NewGuid()))
                        if (window.Name == "modal")
                            if (!string.IsNullOrEmpty(window.Html))
                            {
                                if (window.Html.Contains("Repairing these items will cost"))
                                {
                                    if (window.Html != null)
                                        Log.WriteLine("Content of modal window (HTML): [" + window.Html.Replace("\n", "").Replace("\r", "") + "]");
                                    Log.WriteLine("Closing Quote for Repairing All with YES");
                                    window.AnswerModal("Yes");
                                    Time.Instance.NextRepairItemsAction = DateTime.UtcNow.AddSeconds(Settings.Instance.RandomNumber(1, 2));
                                    doneUsingRepairWindow = true;
                                    return false;
                                }

                                if (window.Html.Contains("How much would you like to repair?"))
                                {
                                    if (window.Html != null)
                                        Log.WriteLine("Content of modal window (HTML): [" + window.Html.Replace("\n", "").Replace("\r", "") + "]");
                                    Log.WriteLine("Closing Quote for Repairing All with OK");
                                    window.AnswerModal("OK");
                                    Time.Instance.NextRepairItemsAction = DateTime.UtcNow.AddSeconds(Settings.Instance.RandomNumber(1, 2));
                                    doneUsingRepairWindow = true;
                                    return false;
                                }
                            }


                    if (Time.Instance.RepairLedger.Count(i => i.AddMinutes(10) > DateTime.UtcNow) >= 6)
                    {
                        foreach (var entry in Time.Instance.RepairLedger)
                        {
                            Log.WriteLine("RepairLedger Entry [" + entry.ToLongTimeString() + "]");
                        }

                        Log.WriteLine("RepairLedger has 6 or more entries in the last 10 min: Do we have a module that cant be repaired? strip fitting and reload fitting?");
                        ESCache.Instance.BoolRestartEve = true;
                        return true;
                    }

                    if (!ESCache.Instance.NeedRepair) return true;

                    //if (ESCache.Instance.DirectEve.hasRepairFacility() == null)
                    //    return false;

                    /**
                    if (!(bool)ESCache.Instance.DirectEve.hasRepairFacility())
                    {
                        Log("This station does not have repair facilities to use! aborting attempt to use non-existent repair facility.");
                        if (MissionSettings.SelectedControllerUsesCombatMissionsBehavior && MissionSettings.AgentToPullNextRegularMissionFrom != null && ESCache.Instance.DirectEve.Session.LocationId != MissionSettings.AgentToPullNextRegularMissionFrom.StationId)
                        {
                            CombatMissionsBehavior.ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase);
                            Panic.HeadedToRepairStation = true;
                            Log("CleanupController setting GotoBase");
                            return true;
                        }

                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.NeedRepair), false);
                        return true;
                    }
                    **/

                    //if (MissionSettings.MissionSpecificMissionFitting != null && !string.IsNullOrEmpty(MissionSettings.MissionSpecificMissionFitting.Ship))
                    //    return true;

                    DirectRepairShopWindow repairWindow = ESCache.Instance.Windows.OfType<DirectRepairShopWindow>().FirstOrDefault();

                    DirectWindow repairQuote = ESCache.Instance.GetWindowByName("Set Quantity");

                    if (doneUsingRepairWindow)
                    {
                        if (Time.Instance.RepairLedger.All(i => DateTime.UtcNow > i.AddSeconds(15)))
                        {
                            Time.Instance.RepairLedger.Add(DateTime.UtcNow);
                        }

                        doneUsingRepairWindow = false;
                        if (repairWindow != null) repairWindow.Close();
                        Log.WriteLine("doneUsingRepairWindow [" + doneUsingRepairWindow + "]");
                        return true;
                    }

                    if (repairQuote != null && repairQuote.Html != null && repairQuote.IsModal && repairQuote.IsKillable)
                    {
                        Log.WriteLine("Content of modal window (HTML): [" + repairQuote.Html.Replace("\n", "").Replace("\r", "") + "]");
                        Log.WriteLine("Closing Quote for Repairing All with OK");
                        repairQuote.AnswerModal("OK");
                        Time.Instance.NextRepairItemsAction = DateTime.UtcNow.AddSeconds(Settings.Instance.RandomNumber(1, 2));
                        ESCache.Instance.NeedRepair = false;
                        doneUsingRepairWindow = true;
                        return false;
                    }

                    if (repairWindow == null)
                    {
                        Log.WriteLine("Opening repairshop window");
                        ESCache.Instance.DirectEve.OpenRepairShop();
                        Statistics.LogWindowActionToWindowLog("RepairWindow", "Opening RepairWindow");
                        Time.Instance.NextRepairItemsAction = DateTime.UtcNow.AddSeconds(Settings.Instance.RandomNumber(1, 3));
                        return false;
                    }

                    if (ESCache.Instance.AmmoHangar == null)
                    {
                        Log.WriteLine("if (Cache.Instance.ItemHangar == null)");
                        return false;
                    }
                    if (ESCache.Instance.ShipHangar == null)
                    {
                        Log.WriteLine("if (Cache.Instance.ShipHangar == null)");
                        return false;
                    }

                    if (Drones.UseDrones && ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.GivenName.ToLower() == Combat.Combat.CombatShipName.ToLower() && !ESCache.Instance.ActiveShip.IsShipWithNoDroneBay)
                        if (Drones.DroneBay == null)
                        {
                            Log.WriteLine("RepairItems: if (Drones.DroneBay == null)");
                            return false;
                        }

                    if (ESCache.Instance.ShipHangar.Items == null)
                    {
                        Log.WriteLine("Cache.Instance.ShipHangar.Items == null");
                        return false;
                    }

                    List<DirectItem> repairAllItems = ESCache.Instance.ShipHangar.Items;

                    repairAllItems.AddRange(ESCache.Instance.ItemHangar.Items);
                    if (Drones.UseDrones && Drones.DroneBay != null && Drones.DroneBay.Items.Any())
                    {
                        repairAllItems.AddRange(Drones.DroneBay.Items);
                    }

                    if (repairAllItems.Count > 0)
                    {
                        if (string.IsNullOrEmpty(repairWindow.AvgDamage()))
                        {
                            Log.WriteLine("Add items to repair list");
                            if (!repairWindow.RepairItems(repairAllItems)) return false;
                            return false;
                        }

                        Log.WriteLine("Repairing Items: repairWindow.AvgDamage: " + repairWindow.AvgDamage());
                        if (repairWindow.AvgDamage().Equals("Avg: 0.0 % Damaged") || repairWindow.AvgDamage().Equals("Avg: 0,0 % Damaged"))
                        {
                            Log.WriteLine("Repairing Items: Zero Damage: skipping repair.");
                            repairWindow.Close();
                            ESCache.Instance.NeedRepair = false;
                            Statistics.LogWindowActionToWindowLog("RepairWindow", "Closing RepairWindow");
                            if (Cleanup.intReDockTorepairModules >= 4000)
                            {
                                Log.WriteLine("We should have damaged modules, but the repair window says we dont");
                                intReDockTorepairModules = intReDockTorepairModules + 1000;
                            }

                            if (Time.Instance.RepairLedger.All(i => DateTime.UtcNow > i.AddSeconds(15)))
                            {
                                Time.Instance.RepairLedger.Add(DateTime.UtcNow);
                            }

                            return true;
                        }

                        if (!ESCache.Instance.OkToInteractWithEveNow)
                        {
                            Log.WriteLine("CleanupController: RepairItems: RepairAll: !OkToInteractWithEveNow");
                            return false;
                        }

                        if (!repairWindow.RepairAll())
                        {
                            Log.WriteLine("CleanupController: RepairItems: RepairAll: Failed!");
                            return false;
                        }

                        Log.WriteLine("CleanupController: RepairItems: RepairAll: Success!");
                        ESCache.Instance.NeedRepair = false;
                        ESCache.Instance.LastInteractedWithEVE = DateTime.UtcNow;
                        Time.Instance.NextRepairItemsAction = DateTime.UtcNow.AddMilliseconds(Settings.Instance.RandomNumber(200, 400));
                        repairWindow.Close();
                        return false;
                    }

                    if (Time.Instance.RepairLedger.All(i => DateTime.UtcNow > i.AddSeconds(15)))
                    {
                        Time.Instance.RepairLedger.Add(DateTime.UtcNow);
                    }

                    Log.WriteLine("No items available, nothing to repair.");
                    ESCache.Instance.NeedRepair = false;
                    return true;
                }

                Log.WriteLine("Not in station.");
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception:" + ex);
                return false;
            }
        }

        public static void CheckOmegaClone()
        {
            if (ESCache.Instance.InStation && ESCache.Instance.EveAccount.RequireOmegaClone)
            {
                if (ESCache.Instance.DirectEve.Me.IsOmegaClone)
                {
                    Log.WriteLine("IsOmegaClone is true");
                }

                Log.WriteLine("RequireOmegaClone is true and IsOmegaClone is false! Pausing!");
                ControllerManager.Instance.SetPause(true);
            }

            if (ESCache.Instance.InStation && ESCache.Instance.DirectEve.Me.IsOmegaClone)
            {
                string TextToLog = "SubEnd: NotInitialized: SubTimeEnd [" + ESCache.Instance.DirectEve.Me.SubTimeEnd + "]";
                //
                // if saved value is in the past
                //

                if (ESCache.Instance.EveAccount.SubEnd > DateTime.UtcNow)
                    TextToLog = ESCache.Instance.EveAccount.SubEnd.ToString();

                if (DateTime.UtcNow.AddDays(2) > ESCache.Instance.DirectEve.Me.SubTimeEnd)
                {
                    //
                    // if we have not checked in the last hour or so
                    //
                    if (DateTime.UtcNow > Time.Instance.LastSubscriptionTimeLeftCheckAttempt.AddMinutes(ESCache.Instance.RandomNumber(60, 120)))
                    {
                        if (ESCache.Instance.DirectEve.Me.SubTimeEnd != DateTime.MinValue)
                        {
                            Time.Instance.LastSubscriptionTimeLeftCheckAttempt = DateTime.UtcNow;
                            TextToLog = ESCache.Instance.DirectEve.Me.SubTimeEnd.ToString();
                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.SubEnd), ESCache.Instance.DirectEve.Me.SubTimeEnd);
                        }
                    }
                }

                if (DebugConfig.DebugSubscriptionEnd) Log.WriteLine("SubEnd: " + TextToLog);
            }
        }

        //private static Stopwatch CheckWindowsStopWatch = new Stopwatch();

        public static bool ClaimLoginReward()
        {
            if (!DebugConfig.ClaimLoginRewards) return false;
            if (ESCache.Instance.Paused) return true;

            if (ESCache.Instance.EveAccount.LastLoginRewardClaim.AddHours(5) > DateTime.UtcNow)
            {
                if (DirectEve.Interval(1800000)) Log.WriteLine("less than 5 hours have passed since our last LoginRewards check: waiting ~30min");
                return true;
            }

            if (!ESCache.Instance.InStation)
            {
                Log.WriteLine("We are not in station?!");
                return false;
            }

            if (ESCache.Instance.InStation)
            {
                if (!OpenLoginRewardWindow()) return false;
                if (DebugConfig.DebugWindows) Log.WriteLine("OpenLoginRewardWindow true");
                var loginRewardWindow = ESCache.Instance.Windows.OfType<DirectLoginRewardWindow>().FirstOrDefault();
                if (loginRewardWindow != null)
                {
                    if (loginRewardWindow.Buttons.Any())
                    {
                        if (loginRewardWindow.Buttons.Any(i => i.Type == LoginRewardButtonType.CLAIM))
                        {
                            if (DirectEve.Interval(5000, 7000))
                            {
                                Log.WriteLine("Press CLAIM Button");
                                if (loginRewardWindow.Buttons.FirstOrDefault(i => i.Type == LoginRewardButtonType.CLAIM).Click())
                                {
                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastLoginRewardClaim), DateTime.UtcNow);
                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastRewardRedeem), DateTime.UtcNow.AddDays(-2));
                                    return true;
                                }

                                Log.WriteLine("Press CLAIM Button - returned false!");
                                return false;
                            }

                            return false;
                        }

                        if (DebugConfig.DebugWindows) Log.WriteLine("No CLAIM button found");
                        return false;
                    }

                    if (DebugConfig.DebugWindows) Log.WriteLine("No loginRewardWindow.Buttons found");
                    return false;
                }

                if (DebugConfig.DebugWindows) Log.WriteLine("No loginRewardWindow found");
                return true;
            }

            return true;
        }

        public static bool RedeemItems()
        {
            if (!DebugConfig.RedeemItems) return false;
            if (ESCache.Instance.Paused) return true;

            if (ESCache.Instance.EveAccount.LastRewardRedeem.AddHours(5) > DateTime.UtcNow)
            {
                if (DirectEve.Interval(1800000)) Log.WriteLine("less than 5 hours have passed since our last Redeem Items check: waiting ~30min");
                return true;
            }

            if (!ESCache.Instance.InStation)
            {
                Log.WriteLine("We are not in station?!");
                return false;
            }

            if (ESCache.Instance.InStation)
            {
                if (!OpenRedeemWindow()) return false;
                if (DebugConfig.DebugWindows) Log.WriteLine("OpenRedeemWindow true");
                var redeemItemsWindow = ESCache.Instance.Windows.OfType<DirectRedeemItemsWindow>().FirstOrDefault();
                if (redeemItemsWindow != null)
                {
                    if (redeemItemsWindow.Buttons.Any())
                    {
                        if (redeemItemsWindow.Buttons.Any(i => i.Type == RedeemItemsButtonType.RedeemToHomeStation))
                        {
                            if (DirectEve.Interval(5000, 9000))
                            {
                                Log.WriteLine("Press RedeemToHomeStation Button");
                                if (redeemItemsWindow.Buttons.FirstOrDefault(i => i.Type == RedeemItemsButtonType.RedeemToHomeStation).Click())
                                {
                                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastRewardRedeem), DateTime.UtcNow);
                                    return true;
                                }

                                Log.WriteLine("Press RedeemToHomeStation Button - returned false!");
                                return false;
                            }

                            return true;
                        }

                        /**
                        if (redeemItemsWindow.Buttons.Any(i => i.Type == RedeemItemsButtonType.RedeemToCurrentStation))
                        {
                            Log.WriteLine("Press RedeemToCurrentStation Button");
                            if (DirectEve.Interval(5000, 9000)) redeemItemsWindow.Buttons.FirstOrDefault(i => i.Type == RedeemItemsButtonType.RedeemToCurrentStation).Click();
                            return true;
                        }
                        **/

                        if (DebugConfig.DebugWindows) Log.WriteLine("No redeem buttons found");
                        return false;
                    }

                    if (DebugConfig.DebugWindows) Log.WriteLine("No redeemItemsWindow.Buttons found");
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastRewardRedeem), DateTime.UtcNow);

                    if (DirectEve.Interval(4000, 7000)) redeemItemsWindow.Close();
                    return false;
                }

                if (DebugConfig.DebugWindows) Log.WriteLine("No redeemItemsWindow found");
                return true;
            }

            return true;
        }

        public static bool OpenLoginRewardWindow()
        {
            if (!ESCache.Instance.InStation)
            {
                Log.WriteLine("We are not in station?!");
                return false;
            }

            if (ESCache.Instance.InStation)
            {
                if (DirectEve.Interval(5000, 7000))
                {
                    var loginRewardWindow = ESCache.Instance.Windows.OfType<DirectLoginRewardWindow>().FirstOrDefault();
                    if (loginRewardWindow == null)
                    {
                        if (ESCache.Instance.EveAccount.LastLoginRewardClaim.AddHours(5) > DateTime.UtcNow)
                        {
                            if (DirectEve.Interval(1800000)) Log.WriteLine("less than 5 hours have passed since our last LoginRewards check: waiting ~30min");
                            return false;
                        }

                        if (DirectEve.Interval(5000, 7000))
                        {
                            if(ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenLoginRewardWindow))
                            {
                                Log.WriteLine("Opening Login Reward Window");
                            }

                            return false;
                        }

                        return false;
                    }

                    if (DebugConfig.DebugWindows) Log.WriteLine("loginRewardWindow != null");
                    return true;
                }

                return false;
            }

            return true;
        }

        public static bool OpenRedeemWindow()
        {
            if (!ESCache.Instance.InStation)
            {
                Log.WriteLine("We are not in station?!");
                return false;
            }

            if (ESCache.Instance.InStation)
            {
                if (DirectEve.Interval(5000, 7000))
                {
                    var redeemItemsWindow = ESCache.Instance.Windows.OfType<DirectRedeemItemsWindow>().FirstOrDefault();
                    if (redeemItemsWindow == null)
                    {
                        if (ESCache.Instance.EveAccount.LastRewardRedeem.AddHours(5) > DateTime.UtcNow)
                        {
                            if (DirectEve.Interval(1800000)) Log.WriteLine("less than 5 hours have passed since our last Redeem Items check: waiting ~30min");
                            return false;
                        }

                        if (DirectEve.Interval(5000, 7000))
                        {
                            if (ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.ToggleRedeemItems))
                            {
                                Log.WriteLine("Opening Redeem Items Window");
                            }

                            return false;
                        }

                        return false;
                    }

                    Log.WriteLine("redeemItemsWindow != null");
                    return true;
                }

                return false;
            }

            return true;
        }

        public static void CheckWindows()
        {
            if (DebugConfig.DebugCleanup) Log.WriteLine("CheckWindows");

            if (ESCache.Instance.DirectEve.Login.AtLogin)
            {
                Log.WriteLine("if (ESCache.Instance.DirectEve.Login.AtLogin)");
                foreach (var regularWindow in ESCache.Instance.Windows)
                {
                    Log.WriteLine("Window:: Name [" + regularWindow.Name + "] Type [" + regularWindow.Guid + "] IsKillable [" + regularWindow.IsKillable + "] isDialog [" + regularWindow.IsDialog + "] isModal [" + regularWindow.IsModal + "] Id [" + regularWindow.WindowId + "] Html [" + regularWindow.Html + "]");

                }

                CheckModalWindows();
                return;
            }

            if (ESCache.Instance.DirectEve.Login.AtCharacterSelection)
            {
                if (DirectEve.Interval(3000, 3000, ESCache.Instance.Windows.Count().ToString()))
                {
                    Log.WriteLine("if (ESCache.Instance.DirectEve.Login.AtCharacterSelection)");
                    foreach (var regularWindow in ESCache.Instance.Windows)
                    {
                        if (DirectEve.Interval(1000)) Log.WriteLine("Window::: Name [" + regularWindow.Name + "] Type [" + regularWindow.Guid + "] IsKillable [" + regularWindow.IsKillable + "] isDialog [" + regularWindow.IsDialog + "] isModal [" + regularWindow.IsModal + "] Id [" + regularWindow.WindowId + "] Html [" + regularWindow.Html + "]");
                    }
                }

                CheckModalWindows();
                return;
            }

            if (Time.Instance.LastJumpAction.AddSeconds(3) > DateTime.UtcNow)
            {
                if (DebugConfig.DebugCleanup) Log.WriteLine("if (DateTime.UtcNow > Time.Instance.LastJumpAction.AddSeconds(5))");
                return;
            }

            if (Time.Instance.LastUndockAction.AddSeconds(3) > DateTime.UtcNow)
            {
                if (DebugConfig.DebugCleanup) Log.WriteLine("if (DateTime.UtcNow > Time.Instance.LastUndockAction.AddSeconds(5))");
                return;
            }

            if (Time.Instance.LastDockAction.AddSeconds(3) > DateTime.UtcNow)
            {
                if (DebugConfig.DebugCleanup) Log.WriteLine("if (DateTime.UtcNow > Time.Instance.LastDockAction.AddSeconds(5))");
                return;
            }

            if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                if (ESCache.Instance.DirectEve.Me.IsSessionChangeActive)
                {
                    if (DebugConfig.DebugCleanup) Log.WriteLine("CheckModalWindows: IsSessionChangeActive do nothing");
                    return;
                }


            if (ESCache.Instance.Windows.Count == 0)
            {
                if (DebugConfig.DebugCleanup)
                    Log.WriteLine("CheckModalWindows: Cache.Instance.Windows returned null or empty");
                State.CurrentCleanupState = CleanupState.Idle;
                return;
            }

            if (DirectEve.Interval(5000, 8000)) ClaimLoginReward();
            if (DirectEve.Interval(5000, 8000)) RedeemItems();


            var loginRewardWindow = ESCache.Instance.Windows.OfType<DirectLoginRewardWindow>().FirstOrDefault();
            if (loginRewardWindow != null)
            {
                //
                // Window exists!
                //
                if (loginRewardWindow.Buttons.All(i => i.Type != LoginRewardButtonType.CLAIM))
                {
                    if (DirectEve.Interval(10000, 15000))
                    {
                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastLoginRewardClaim), DateTime.UtcNow);
                        Log.WriteLine("Closing DirectLoginRewardWindow [" + loginRewardWindow.Caption + "] no CLAIM button found! Buttons [" + loginRewardWindow.Buttons.Count() + "]");
                        loginRewardWindow.Close();
                        return;
                    }
                }

                if (!loginRewardWindow.Buttons.Any())
                {
                    if (DirectEve.Interval(10000, 15000))
                    {
                        ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastLoginRewardClaim), DateTime.UtcNow);
                        Log.WriteLine("Closing DirectLoginRewardWindow [" + loginRewardWindow.Caption + "] no buttons found!");
                        loginRewardWindow.Close();
                        return;
                    }
                }
            }

            if (DebugConfig.DebugCleanup) Log.WriteLine("About to: foreach (DirectWindow regularWindow in ESCache.Instance.DirectEve.Windows)");
            foreach (DirectWindow regularWindow in ESCache.Instance.Windows)
            {
                if (DebugConfig.DebugCleanup) Log.WriteLine("Window:: Name [" + regularWindow.Name + "] Type [" + regularWindow.Guid + "] IsKillable [" + regularWindow.IsKillable + "] isDialog [" + regularWindow.IsDialog + "] isModal [" + regularWindow.IsModal + "] Id [" + regularWindow.WindowId + "] HTML [" + regularWindow.Html + "]");

                //
                // we need to deal with this window even when paused! traveler needs to dismiss this window
                //
                if (regularWindow.Html.ToLower().Contains("Warning! This star system has been secured by EDENCOM forces".ToLower()))
                {
                    Log.WriteLine("regularWindow: Warning! This star system has been secured by EDENCOM forces: Pressing OK");
                    regularWindow.AnswerModal("OK");
                    continue;
                }

                if (regularWindow.Html.ToLower().Contains("Danger! This star system has been invaded by Triglavian forces".ToLower()))
                {
                    Log.WriteLine("regularWindow: Danger! This star system has been invaded by Triglavian forces: Pressing OK");
                    regularWindow.AnswerModal("OK");
                    continue;
                }

                if (ESCache.Instance.Paused) continue;

                if (DebugConfig.DebugCleanup) Log.WriteLine("1");
                if (!ESCache.Instance.Paused && ESCache.Instance.InSpace && (regularWindow.Guid == "form.AgentDialogueWindow" || regularWindow.Guid == "MissionGiver"))
                {
                    Log.WriteLine("Closing Agent Window [" + regularWindow.Caption + "]");
                    regularWindow.Close();
                    continue;
                }

                if (DebugConfig.DebugCleanup) Log.WriteLine("2");
                if (!ESCache.Instance.Paused && ESCache.Instance.InSpace && regularWindow.Name == "buyAllMessageBox")
                {
                    Log.WriteLine("Closing buyAllMessageBox Window [" + regularWindow.Caption + "]");
                    regularWindow.Close();
                    continue;
                }

                if (DebugConfig.DebugCleanup) Log.WriteLine("3");
                if (ESCache.Instance.InSpace && regularWindow.Guid == "form.FittingMgmt")
                {
                    Log.WriteLine("Closing FittingMgmt Window [" + regularWindow.Caption + "]");
                    regularWindow.Close();
                    continue;
                }

                //
                // Quote window for repair: press OK
                //
                if (DebugConfig.DebugCleanup) Log.WriteLine("4");
                if (regularWindow.Guid.Contains("form.HybridWindow") && regularWindow.Caption.Contains("Set Quantity"))
                {
                    regularWindow.AnswerModal("OK");
                    continue;
                }


                //new WindowType("__guid__", "form.FittingMgmt", (directEve, pyWindow) => new DirectFittingManagerWindow(directEve, pyWindow)),

                //if (ESCache.Instance.InSpace && regularWindow.Type == "form.Overview")
                //{
                //    bool minimized = regularWindow.PyWindow.Attribute("_minimized").ToBool();
                //    if (!minimized)
                //    {
                //        Log("Minimizing Overview Window [" + regularWindow.Caption + "]");
                //        regularWindow.Minimize();
                //    }
                //}

                //if (!LoginController.LoggedIn && !ESCache.Instance.DirectEve.Session.IsReady && ESCache.Instance.DirectEve.Login.AtCharacterSelection && ESCache.Instance.DirectEve.Login.IsCharacterSelectionReady && regularWindow.Type == "uicontrols.Window" && regularWindow.Name == "LoginRewardWindow")
                //
                //
                //


                if (!ESCache.Instance.Paused && !ESCache.Instance.InWarp && ESCache.Instance.InSpace && regularWindow.Name == "walletWindow")
                {
                    Log.WriteLine("Closing Wallet Window [" + regularWindow.Caption + "]");
                    regularWindow.Close();
                    continue;
                }

                //
                // new feature notify window: new campaigns...
                //
                if (regularWindow.Guid == "uicontrols.Window" && regularWindow.Name == "NewFeatureNotifyWnd")
                {
                    Log.WriteLine("NewFeatureNotifyWnd Window [" + regularWindow.Caption + "]");
                    //regularWindow.Minimize();
                    regularWindow.Close();
                    continue;
                }

                if (regularWindow.Name == "telecom" || regularWindow.Guid.ToLower() == "form.Telecom".ToLower())
                {
                    Log.WriteLine("Closing telecom message...");
                    Log.WriteLine("Content of telecom window (HTML): [" + (regularWindow.Html ?? string.Empty).Replace("\n", "").Replace("\r", "") +
                        "]");
                    regularWindow.Close();
                    continue;
                }

                try
                {
                    if (regularWindow.Name == "modal" && regularWindow.Html.Contains("wants you to join their fleet, do you accept"))
                    {
                        if ((ESCache.Instance.EveAccount.LeaderCharacterName != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.LeaderCharacterName)) ||
                            (ESCache.Instance.EveAccount.SlaveCharacterName1 != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.SlaveCharacterName1)) ||
                            (ESCache.Instance.EveAccount.SlaveCharacterName2 != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.SlaveCharacterName2)) ||
                            (ESCache.Instance.EveAccount.SlaveCharacterName3 != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.SlaveCharacterName3)) ||
                            (ESCache.Instance.EveAccount.SlaveCharacterName4 != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.SlaveCharacterName4)) ||
                            (ESCache.Instance.EveAccount.SlaveCharacterName5 != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.SlaveCharacterName5)) ||
                            (ESCache.Instance.EveAccount.SlaveCharacterName6 != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.SlaveCharacterName6)) ||
                            (ESCache.Instance.EveAccount.SlaveCharacterName7 != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.SlaveCharacterName7)) ||
                            (ESCache.Instance.EveAccount.SlaveCharacterName8 != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.SlaveCharacterName8)) ||
                            (ESCache.Instance.EveAccount.SlaveCharacterName9 != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.SlaveCharacterName9)) ||
                            (ESCache.Instance.EveAccount.SlaveCharacterName10 != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.SlaveCharacterName10)) ||
                            (ESCache.Instance.EveAccount.SlaveCharacterName11 != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.SlaveCharacterName11)) ||
                            (ESCache.Instance.EveAccount.SlaveCharacterName12 != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.SlaveCharacterName12)) ||
                            (ESCache.Instance.EveAccount.SlaveCharacterName13 != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.SlaveCharacterName13)) ||
                            (ESCache.Instance.EveAccount.SlaveCharacterName14 != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.SlaveCharacterName14)) ||
                            (ESCache.Instance.EveAccount.SlaveCharacterName15 != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.SlaveCharacterName15)) ||
                            (ESCache.Instance.EveAccount.SlaveCharacterName16 != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.SlaveCharacterName16)) ||
                            (ESCache.Instance.EveAccount.SlaveCharacterName17 != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.SlaveCharacterName17)) ||
                            (ESCache.Instance.EveAccount.SlaveCharacterName18 != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.SlaveCharacterName18)) ||
                            (ESCache.Instance.EveAccount.SlaveCharacterName19 != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.SlaveCharacterName19)) ||
                            (ESCache.Instance.EveAccount.SlaveCharacterName20 != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.SlaveCharacterName20)) ||
                            (ESCache.Instance.EveAccount.SlaveCharacterName21 != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.SlaveCharacterName21)) ||
                            (ESCache.Instance.EveAccount.SlaveCharacterName22 != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.SlaveCharacterName22)) ||
                            (ESCache.Instance.EveAccount.SlaveCharacterName23 != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.SlaveCharacterName23)) ||
                            (ESCache.Instance.EveAccount.SlaveCharacterName24 != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.SlaveCharacterName24)) ||
                            (ESCache.Instance.EveAccount.SlaveCharacterName25 != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.SlaveCharacterName25)) ||
                            (ESCache.Instance.EveAccount.SlaveCharacterName26 != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.SlaveCharacterName26)) ||
                            (ESCache.Instance.EveAccount.SlaveCharacterName27 != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.SlaveCharacterName27)) ||
                            (ESCache.Instance.EveAccount.SlaveCharacterName28 != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.SlaveCharacterName28)) ||
                            (ESCache.Instance.EveAccount.SlaveCharacterName29 != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.SlaveCharacterName29)) ||
                            (ESCache.Instance.EveAccount.SlaveCharacterName30 != null && regularWindow.Html.Contains(ESCache.Instance.EveAccount.SlaveCharacterName30)))
                        {
                            Log.WriteLine("CleanupController: Found a fleet invite that we should accept");
                            Log.WriteLine("FleetInviteWindow.Name: [" + regularWindow.Name + "]");
                            Log.WriteLine("FleetInviteWindow.Html: [" + regularWindow.Html + "]");
                            Log.WriteLine("FleetInviteWindow.Type: [" + regularWindow.Guid + "]");
                            Log.WriteLine("FleetInviteWindow.IsModal: [" + regularWindow.IsModal + "]");
                            Log.WriteLine("FleetInviteWindow.Caption: [" + regularWindow.Caption + "]");
                            Log.WriteLine("--------------------------------------------------");
                            //regularWindow.AnswerModal("Yes"); //this does not work for fleet invite windows! why?
                            continue;
                        }

                        Log.WriteLine("CleanupController: Found a fleet invite that did not contain LeaderCharacterName [" + ESCache.Instance.EveAccount.LeaderCharacterName + "]");
                        Log.WriteLine("CleanupController: Found a fleet invite HTML: [ " + regularWindow.Html + " ]");
                        //regularWindow.AnswerModal("No"); //this does not work for fleet invite windows! why?
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }
            }

            if (DebugConfig.DebugCleanup) Log.WriteLine("Checking Each window in Cache.Instance.Windows");

            CheckModalWindows();
        }

        private static void CheckModalWindows()
        {
            foreach (DirectWindow modalWindow in ESCache.Instance.DirectEve.ModalWindows)
            {
                if (DebugConfig.DebugCleanup && DirectEve.Interval(15000)) Log.WriteLine("ModalWindow: Name [" + modalWindow.Name + "] Type [" + modalWindow.Guid + "] IsKillable [" + modalWindow.IsKillable + "] isDialog [" + modalWindow.IsDialog + "] isModal [" + modalWindow.IsModal + "] Id [" + modalWindow.WindowId + "] Html [" + modalWindow.Html + "]");
                if (ESCache.Instance.Paused)
                {
                    continue;
                }

                if (modalWindow.WindowId == "DisconnectNotice")
                {
                    WCFClient.Instance.GetPipeProxy.RemoteLog("[" + ESCache.Instance.CharName + "]" + modalWindow.Html);
                    Log.WriteLine("Found Window with ID of DisconnectNotice: restarting the eve client");
                    ESCache.Instance.CloseEveReason = "DisconnectNotice";

                    if (ESCache.Instance.EveAccount.EndTime.AddMinutes(20) > DateTime.UtcNow)
                    {
                        ESCache.Instance.BoolCloseEve = true;
                        return;
                    }

                    ESCache.Instance.BoolRestartEve = true;
                    return;
                }

                if (DebugConfig.DebugCleanup) Log.WriteLine("ModalWindow: Name [" + modalWindow.Name + "] Type [" + modalWindow.Guid + "] IsKillable [" + modalWindow.IsKillable + "] isDialog [" + modalWindow.IsDialog + "] isModal [" + modalWindow.IsModal + "] Id [" + modalWindow.WindowId + "] Html [" + modalWindow.Html + "]");
                bool close = false;
                bool restart = false;
                bool gotoBaseNow = false;
                bool sayYes = false;
                bool sayNo = false;
                bool sayOk = false;
                bool pause = false;
                bool quit = false;
                bool stackHangars = false;
                bool clearPocket = false;
                bool disableInstance = false;
                bool needHumanIntervention = false;
                bool notAllItemsCouldBeFitted = false;

                if (!string.IsNullOrEmpty(modalWindow.Html) && modalWindow.Html.Contains("You are too far away from the acceleration gate to activate it"))
                {
                    Time.Instance.NextActivateAccelerationGate = DateTime.MinValue;
                    Log.WriteLine("Closing message about the gate being too far away...");
                    modalWindow.Close();
                }

                if (!string.IsNullOrEmpty(modalWindow.Html) && modalWindow.Html.Contains("Not all the items could be fitted."))
                {
                    Time.Instance.NextActivateAccelerationGate = DateTime.MinValue;
                    Log.WriteLine("Closing message about fitting items missing");
                    modalWindow.Close();
                }

                if (modalWindow.WindowId.Contains("ChatInvitation") && DirectEve.Interval(ESCache.Instance.RandomNumber(36000, 120000)))
                {
                    Log.WriteLine("Closing chat invitation");
                    modalWindow.Close();
                }

                /**
                if (window.Name == "telecom")
                {
                    Log("Closing telecom message...");
                    Log("Content of telecom window (HTML): [" + (window.Html ?? string.Empty).Replace("\n", "").Replace("\r", "") +
                        "]");
                    window.Close();
                }
                **/

                if (modalWindow.IsModal || modalWindow.Name == "modal")
                    if (!string.IsNullOrEmpty(modalWindow.Html))
                    {
                        gotoBaseNow |= modalWindow.Html.ContainsIgnoreCase("for a short unscheduled reboot");
                        disableInstance |= modalWindow.Html.ContainsIgnoreCase("banned");
                        if (modalWindow.Html.ContainsIgnoreCase("banned"))
                        {
                            WCFClient.Instance.GetPipeProxy.RemoteLog("[" + ESCache.Instance.CharName + "]" + modalWindow.Html);
                        }

                        disableInstance |= modalWindow.Html.ContainsIgnoreCase("The authentication token provided by the launcher");
                        pause |= modalWindow.Html.ContainsIgnoreCase("Cannot move");

                        if (modalWindow.Guid == "form.MessageBox" && modalWindow.IsDialog && modalWindow.IsModal && modalWindow.IsKillable)
                        {
                            if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
                            {
                                sayYes |= modalWindow.Html.ContainsIgnoreCase(
                                    "The star system you are about to enter is in a pirate insurgency zone and under the effects of maximum suppression");
                            }

                            sayYes |=
                                modalWindow.Html.ContainsIgnoreCase(
                                    "If you decline of fail a mission from an agent he/she might become displeased and lower your standing towards him/her. You can decline a mission every four hours without penalty");
                        }
                        if (DebugConfig.RedeemItems)
                        {
                            sayYes |= (modalWindow.Html.ContainsIgnoreCase("item will be moved to") &&
                                        modalWindow.Html.ContainsIgnoreCase("Do you wish to proceed")
                                        );
                            sayYes |= (modalWindow.Html.ContainsIgnoreCase("directly injected to") &&
                                        modalWindow.Html.ContainsIgnoreCase("Do you wish to proceed")
                                        );

                            sayYes |= (modalWindow.Html.ContainsIgnoreCase("The following items will be redeemed") &&
                                        modalWindow.Html.ContainsIgnoreCase("Do you wish to proceed")
                                        );
                        }

                        sayNo |= modalWindow.Html.ContainsIgnoreCase("priced well below the average:"); //selling items at a price that is too low!
                        close |= modalWindow.Html.ContainsIgnoreCase("Do you really want to quit now?");
                        close |= modalWindow.Html.ContainsIgnoreCase("Please make sure your characters are out of harm");
                        close |= modalWindow.Html.ContainsIgnoreCase("the servers are down for 30 minutes each day for maintenance and updates");
                        close |= modalWindow.Html.ContainsIgnoreCase("Item cannot be moved back to a loot container.");
                        close |= modalWindow.Html.ContainsIgnoreCase("you do not have the cargo space");
                        close |= modalWindow.Html.ContainsIgnoreCase("You are too far away from the acceleration gate to activate it!");
                        close |= modalWindow.Html.ContainsIgnoreCase("maximum distance is 2500 meters");
                        close |= modalWindow.Html.ContainsIgnoreCase("Broker found no match for your order");
                        close |= modalWindow.Html.ContainsIgnoreCase("All the weapons in this group are already full");
                        //close |=
                        //    modalWindow.Html.Contains(
                        //        "If you decline of fail a mission from an agent he/she might become displeased and lower your standing towards him/her. You can decline a mission every four hours without penalty");
                        close |= modalWindow.Html.ContainsIgnoreCase("Do you wish to proceed with this dangerous action?");
                        close |= modalWindow.Html.ContainsIgnoreCase("weapons in that group are already full");
                        close |= modalWindow.Html.ContainsIgnoreCase("No rigs were added to or removed from the ship");
                        close |= modalWindow.Html.ContainsIgnoreCase("You can't fly your active ship into someone else's hangar");
                        close |= modalWindow.Html.ContainsIgnoreCase("You can't do this quite so fast");
                        clearPocket |= modalWindow.Html.ContainsIgnoreCase("This gate is locked!");

                        close |= modalWindow.Html.ContainsIgnoreCase("The Zbikoki's Hacker Card");
                        close |= modalWindow.Html.ContainsIgnoreCase(" units free.");
                        close |= modalWindow.Html.ContainsIgnoreCase("already full");
                        close |= modalWindow.Html.ContainsIgnoreCase("All the weapons in this group are already full");
                        close |=
                            modalWindow.Html.ContainsIgnoreCase(
                                "At any time you can log in to the account management page and change your trial account to a paying account");
                        close |= modalWindow.Html.ContainsIgnoreCase("please make sure your characters are out of harms way");
                        restart |= modalWindow.Html.ContainsIgnoreCase("accepting connections");
                        restart |= modalWindow.Html.ContainsIgnoreCase("could not connect") && !modalWindow.Html.ContainsIgnoreCase("to chat server");
                        restart |= modalWindow.Html.ContainsIgnoreCase("the connection to the server was closed");
                        restart |= modalWindow.Html.ContainsIgnoreCase("Connection to server was lost".ToLower());
                        restart |= modalWindow.Html.ContainsIgnoreCase("server was closed");
                        close |= modalWindow.Html.ContainsIgnoreCase("make sure your characters are out of harm");
                        restart |= modalWindow.Html.ContainsIgnoreCase("connection to server lost");
                        restart |= modalWindow.Html.ContainsIgnoreCase("the socket was closed");
                        restart |= modalWindow.Html.ContainsIgnoreCase("the specified proxy or server node");
                        close |= modalWindow.Html.ContainsIgnoreCase("starting up");
                        restart |= modalWindow.Html.ContainsIgnoreCase("unable to connect to the selected server");
                        restart |= modalWindow.Html.ContainsIgnoreCase("could not connect to the specified address");
                        restart |= modalWindow.Html.ContainsIgnoreCase("connection timeout");
                        restart |= modalWindow.Html.ContainsIgnoreCase("the cluster is not currently accepting connections");
                        close |= modalWindow.Html.ContainsIgnoreCase("your character is located within");
                        close |= modalWindow.Html.ContainsIgnoreCase("the transport has not yet been connected");
                        close |= modalWindow.Html.ContainsIgnoreCase("the user's connection has been usurped");
                        close |= modalWindow.Html.ContainsIgnoreCase("the EVE cluster has reached its maximum user limit");
                        restart |= modalWindow.Html.ContainsIgnoreCase("the connection to the server was closed");
                        close |= modalWindow.Html.ContainsIgnoreCase("client is already connecting to the server");
                        close |= modalWindow.Html.ContainsIgnoreCase("client update is available and will now be installed");
                        close |= modalWindow.Html.ContainsIgnoreCase("change your trial account to a paying account");
                        close |= modalWindow.Html.ContainsIgnoreCase("Not all the items could be fitted");
                        close |= modalWindow.Html.ContainsIgnoreCase("You must be docked in a station or a structure to redeem items");
                        close |= modalWindow.Html.ContainsIgnoreCase("You do not have permission to execute that command");

                        if (modalWindow.Html.ContainsIgnoreCase("You are trying to sell") && modalWindow.Html.ContainsIgnoreCase("when you only have"))
                        {
                            close = true;
                            ESCache.Instance.SellError = true;
                        }

                        restart |= modalWindow.Html.ContainsIgnoreCase("The user's connection has been usurped on the proxy");
                        restart |= modalWindow.Html.ContainsIgnoreCase("The connection to the server was closed");
                        restart |= modalWindow.Html.ContainsIgnoreCase("server was closed");
                        restart |= modalWindow.Html.ContainsIgnoreCase("The socket was closed");
                        restart |= modalWindow.Html.ContainsIgnoreCase("The connection was closed");
                        restart |= modalWindow.Html.ContainsIgnoreCase("Connection to server lost");
                        restart |= modalWindow.Html.ContainsIgnoreCase("The user connection has been usurped on the proxy");
                        restart |= modalWindow.Html.ContainsIgnoreCase("The transport has not yet been connected, or authentication was not successful");
                        restart |= modalWindow.Html.ContainsIgnoreCase("Your client has waited");
                        restart |= modalWindow.Html.ContainsIgnoreCase("This could mean the server is very loaded");
                        restart |= modalWindow.Html.ContainsIgnoreCase("Local cache is corrupt");
                        restart |= modalWindow.Html.ContainsIgnoreCase("Local session information is corrupt");
                        restart |= modalWindow.Html.ContainsIgnoreCase("you are already performing a");
                        restart |= modalWindow.Html.ContainsIgnoreCase("the socket was closed");
                        restart |= modalWindow.Html.ContainsIgnoreCase("the connection was closed");
                        restart |= modalWindow.Html.ContainsIgnoreCase("connection to server lost.");
                        restart |= modalWindow.Html.ContainsIgnoreCase("local cache is corrupt");
                        restart |= modalWindow.Html.ContainsIgnoreCase("the client's local session");
                        restart |= modalWindow.Html.ContainsIgnoreCase("restart the client prior to logging in");

                        quit |= modalWindow.Html.ContainsIgnoreCase("the cluster is shutting down");

                        sayYes |= modalWindow.Html.ContainsIgnoreCase("eject from the ship?");
                        sayYes |= modalWindow.Html.ContainsIgnoreCase("objectives requiring a total capacity");
                        sayYes |= modalWindow.Html.ContainsIgnoreCase("your ship only has space for");
                        sayYes |= modalWindow.Html.ContainsIgnoreCase("Are you sure you want to remove location");
                        sayYes |= modalWindow.Html.ContainsIgnoreCase("Are you sure you would like to decline this mission");
                        sayYes |= modalWindow.Html.ContainsIgnoreCase("has no other missions to offer right now. Are you sure you want to decline");
                        sayYes |= modalWindow.Html.ContainsIgnoreCase("You are about to remove a storyline mission from your journal");
                        sayYes |= modalWindow.Html.ContainsIgnoreCase("If you quit this mission you will lose standings with your agent");
                        sayYes |= modalWindow.Html.ContainsIgnoreCase("could not connect to chat server");

                        //These are handled elsewhere becausr they need to be handled by traveler even when paused!
                        //sayOk |= modalWindow.Html.ContainsIgnoreCase("Warning! This star system has been secured by EDENCOM forces"); //this window isnt a modalWindow for some odd reason? FixMe
                        //sayOk |= modalWindow.Html.ContainsIgnoreCase("Danger! This star system has been invaded by Triglavian forces"); //this window isnt a modalWindow for some odd reason? FixMe
                        sayOk |= modalWindow.Html.ContainsIgnoreCase("Are you sure you want to accept this offer?");
                        sayOk |= modalWindow.Html.ContainsIgnoreCase("You do not have an outstanding invitation to this fleet.");
                        sayOk |= modalWindow.Html.ContainsIgnoreCase("You have already selected a character for this session.");
                        sayOk |= modalWindow.Html.ContainsIgnoreCase("The transport has not yet been connected, or authentication was not successful");
                        sayOk |= modalWindow.Html.ContainsIgnoreCase("local session information is corrupt");
                        sayOk |= modalWindow.Html.ContainsIgnoreCase("has rejected the invitation");
                        sayOk |= modalWindow.Html.ContainsIgnoreCase("You do not appear to be in a fleet");

                        //errors that are repeatable and unavoidable even after a restart of eve/questor
                        needHumanIntervention |= modalWindow.Html.ContainsIgnoreCase("One or more mission objectives have not been completed");
                        needHumanIntervention |= modalWindow.Html.ContainsIgnoreCase("Please check your mission journal for further information");
                        sayOk |= modalWindow.Html.ContainsIgnoreCase("You have to be at the drop off location to deliver the items in person");
                        needHumanIntervention |= modalWindow.Html.ContainsIgnoreCase("cargo units would be required to complete this operation.");

                        stackHangars |= modalWindow.Html.ContainsIgnoreCase("as there are simply too many items here already");
                    }
                //else //non-modal windows
                //{
                //    notAllItemsCouldBeFitted |= window.Html.ToLower().Contains("Not all the items could be fitted".ToLower());
                //}

                if (disableInstance)
                {
                    Log.WriteLine("Restarting eve...");
                    Log.WriteLine("Content of modal window (HTML): [" + modalWindow.Html.Replace("\n", "").Replace("\r", "") + "]");
                    string msg = string.Format("Connection lost on account [{0}]", ESCache.Instance.EveAccount.MaskedAccountName);
                    Log.WriteLine(msg);
                    WCFClient.Instance.GetPipeProxy.RemoteLog(msg);
                    ESCache.Instance.CloseEveReason = "disableInstance";
                    ESCache.Instance.BoolCloseEve = true;
                    return;
                }

                if (restart)
                {
                    Log.WriteLine("Restarting eve...");
                    Log.WriteLine("Content of modal window (HTML): [" + modalWindow.Html.Replace("\n", "").Replace("\r", "") + "]");
                    string msg = string.Format("Connection lost on account [{0}]", ESCache.Instance.EveAccount.MaskedAccountName);
                    Log.WriteLine(msg);
                    WCFClient.Instance.GetPipeProxy.RemoteLog(msg);
                    ESCache.Instance.CloseEveReason = "restart: [" + modalWindow.Html.Replace("\n", "").Replace("\r", "") + "]";

                    if (ESCache.Instance.Paused || (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.CombatDontMoveController) ||
                                                    ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.WspaceSiteController)))
                    {
                        ESCache.Instance.BoolCloseEve = true;
                        return;
                    }

                    ESCache.Instance.BoolRestartEve = true;
                    return;
                }

                if (sayYes)
                {
                    Log.WriteLine("[sayYes] Found a window that needs 'yes' chosen...");
                    Log.WriteLine("Name [" + modalWindow.Name + "] Type [" + modalWindow.Guid + "] Content of modal window (HTML): [" + modalWindow.Html.Replace("\n", "").Replace("\r", "") + "]");
                    if (!modalWindow.AnswerModal("Yes"))
                    {
                        Log.WriteLine("[sayYes] We can only press buttons on modal windows. trying to close instead");
                        modalWindow.Close();
                    }
                    continue;
                }

                if (sayNo)
                {
                    Log.WriteLine("[sayNo] Found a window that needs 'no' chosen...");
                    Log.WriteLine("Name [" + modalWindow.Name + "] Type [" + modalWindow.Guid + "] Content of modal window (HTML): [" + modalWindow.Html.Replace("\n", "").Replace("\r", "") + "]");
                    if (!modalWindow.AnswerModal("No"))
                    {
                        Log.WriteLine("[sayNo] We can only press buttons on modal windows. trying to close instead");
                        modalWindow.Close();
                    }
                    continue;
                }

                if (sayOk)
                {
                    Log.WriteLine("[sayOk] Found a window that needs 'ok' chosen...");

                    if (modalWindow.Html == null)
                    {
                        Log.WriteLine("WINDOW HTML == NULL");
                    }
                    else
                    {
                        Log.WriteLine("Content of modal window (HTML): [" + modalWindow.Html.Replace("\n", "").Replace("\r", "") + "]");

                        if (modalWindow.Html.Contains("Repairing these items will cost"))
                            doneUsingRepairWindow = true;

                        if (ESCache.Instance.DirectEve.Session.Structureid.HasValue)
                        {
                            if (!modalWindow.AnswerModal("Yes"))
                            {
                                Log.WriteLine("[sayYes] We can only press buttons on modal windows. trying to close instead!");
                                modalWindow.Close();
                            }
                        }
                        else if (!modalWindow.AnswerModal("OK"))
                        {
                            Log.WriteLine("[sayOk] We can only press buttons on modal windows. trying to close instead!");
                            modalWindow.Close();
                        }
                    }

                    continue;
                }

                if (stackHangars)
                {
                    if (!ESCache.Instance.ItemHangar.StackItemHangar()) return;
                    if (Settings.Instance.UseCorpAmmoHangar)
                        if (!ESCache.Instance.AmmoHangar.StackAmmoHangar()) return;
                    if (Settings.Instance.UseCorpLootHangar && Settings.Instance.LootCorpHangarDivisionNumber != Settings.Instance.AmmoCorpHangarDivisionNumber)
                        if (!ESCache.Instance.LootHangar.StackLootHangar()) return;
                    continue;
                }

                if (gotoBaseNow)
                {
                    Log.WriteLine("[gotoBaseNow] Evidently the cluster is dieing... and CCP is restarting the server");
                    Log.WriteLine("Content of modal window (HTML): [" + modalWindow.Html.Replace("\n", "").Replace("\r", "") + "] PauseAfterNextDock [true]");
                    State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                    ESCache.Instance.PauseAfterNextDock = true;
                    modalWindow.Close();
                    continue;
                }

                if (pause)
                {
                    Log.WriteLine("This window indicates an error fitting the ship. pausing");
                    ControllerManager.Instance.SetPause(true);
                }

                if (close)
                {
                    Log.WriteLine("[close] Closing modal window...");
                    Log.WriteLine("Content of modal window (HTML): [" + modalWindow.Html.Replace("\n", "").Replace("\r", "") + "]");
                    modalWindow.Close();
                    continue;
                }

                if (quit)
                {
                    Log.WriteLine("[quit] Closing modal window...");
                    Log.WriteLine("Content of modal window (HTML): [" + modalWindow.Html.Replace("\n", "").Replace("\r", "") + "]");
                    if (!modalWindow.AnswerModal("Quit"))
                    {
                        Log.WriteLine("[sayOk] We can only press buttons on modal windows. trying to close instead");
                        modalWindow.Close();
                    }
                    continue;
                }

                if (clearPocket)
                {
                    Log.WriteLine("[clearPocket] Closing modal window...");
                    Log.WriteLine("Content of modal window (HTML): [" + modalWindow.Html.Replace("\n", "").Replace("\r", "") + "]");
                    modalWindow.Close();
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.ReplaceMissionsActions), true);
                    continue;
                }

                if (notAllItemsCouldBeFitted)
                {
                    Log.WriteLine("[notAllItemsCouldBeFitted] Closing modal window...");
                    Log.WriteLine("Content of modal window (HTML): [" + modalWindow.Html.Replace("\n", "").Replace("\r", "") + "]");
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.NotAllItemsCouldBeFitted), true);
                    modalWindow.Close();
                    return;
                }

                if (needHumanIntervention)
                {
                    Log.WriteLine("[needHumanIntervention] Closing modal window...");
                    Log.WriteLine("Content of modal window (HTML): [" + modalWindow.Html.Replace("\n", "").Replace("\r", "") + "]");
                    ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.NeedHumanIntervention), true);
                    modalWindow.Close();
                    return;
                }

                // Debug LOG
                //                Logging.Log("window.Name is: " + window.Name);
                //                Logging.Log("window.Html is: " + window.Html);
                //                Logging.Log("window.Caption is: " + window.Caption);
                //                Logging.Log("window.Type is: " + window.Type);
                //                Logging.Log("window.ID is: " + window.Id);
                //                Logging.Log("window.IsDialog is: " + window.IsDialog);
                //                Logging.Log("window.IsKillable is: " + window.IsKillable);
                //                Logging.Log("window.Viewmode is: " + window.ViewMode);

                if (ESCache.Instance.InSpace)
                {
                    if (modalWindow.IsDialog && modalWindow.IsModal && modalWindow.Caption == "Duel Invitation")
                    {
                        // maybe close?
                    }

                    //sayYes |= window.Html.Contains(Settings.Instance.CharacterToAcceptInvitesFrom + " wants you to join their fleet");

                    if (modalWindow.Name.Contains("ShipDroneBay") && modalWindow.Caption == "Drone Bay")
                    {
                        if (Drones.UseDrones && ESCache.Instance.ActiveShip.GroupId != (int)Group.Shuttle &&
                            ESCache.Instance.ActiveShip.GroupId != (int)Group.Industrial &&
                            ESCache.Instance.ActiveShip.GroupId != (int)Group.TransportShip && _droneBayClosingAttempts <= 1)
                        {
                            _droneBayClosingAttempts++;
                            modalWindow.Close();
                        }
                    }
                    else
                    {
                        _droneBayClosingAttempts = 0;
                    }
                }
            }
        }

        public static void DebugModalWindows()
        {
            if (!LogEVEWindowDetails) return;

            try
            {
                //Log("Checkmodal windows called.");
                if (ESCache.Instance.Windows.Count == 0)
                {
                    Log.WriteLine("CheckModalWindows: Cache.Instance.Windows returned null or empty");
                    return;
                }

                Log.WriteLine("Checking Each window in Cache.Instance.Windows");

                int windowNum = 0;
                foreach (DirectWindow window in ESCache.Instance.Windows)
                {
                    windowNum++;
                    Log.WriteLine("[" + windowNum + "] Debug_Window.Name: [" + window.Name + "]");
                    Log.WriteLine("[" + windowNum + "] Debug_Window.Html: [" + window.Html + "]");
                    Log.WriteLine("[" + windowNum + "] Debug_Window.Type: [" + window.Guid + "]");
                    Log.WriteLine("[" + windowNum + "] Debug_Window.IsModal: [" + window.IsModal + "]");
                    Log.WriteLine("[" + windowNum + "] Debug_Window.Caption: [" + window.Caption + "]");
                    Log.WriteLine("--------------------------------------------------");
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception: " + ex);
            }
            finally
            {
                LogEVEWindowDetails = false;
            }
        }

        public static void FormFrigateAbyssalFleet()
        {
            if (!ESCache.Instance.InSpace)
                return;

            if (Time.Instance.LastInWarp.AddSeconds(15) > DateTime.UtcNow)
            {
                if (DebugConfig.DebugFleetMgr) Log.WriteLine("if (Time.Instance.LastInWarp.AddSeconds(15) > DateTime.UtcNow)");
                return;
            }

            if (Time.Instance.LastFormFleetAttempt.AddSeconds(15) > DateTime.UtcNow)
            {
                if (DebugConfig.DebugFleetMgr) Log.WriteLine("if (Time.Instance.LastFormFleetAttempt.AddSeconds(15) > DateTime.UtcNow)");
                return;
            }

            if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController) &&
                (ESCache.Instance.ActiveShip.Entity.IsFrigate || ESCache.Instance.ActiveShip.Entity.IsDestroyer) && //|| ESCache.Instance.ActiveShip.Entity.IsAssaultShip) &&
                (AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Tranquil") || AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Calm") || AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Agitated"))
                )
            {
                if (!ESCache.Instance.DirectEve.Session.InFleet)
                {
                    Log.WriteLine("FormFleet: Frigate Abyssals");
                    ESCache.Instance.DirectEve.FormNewFleet();
                    return;
                }

                if (DebugConfig.DebugFleetMgr) Log.WriteLine("FormFleet: we are already in a fleet!");
                return;
            }

            if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalController) &&
                ESCache.Instance.Weapons.Any() &&
                (ESCache.Instance.ActiveShip != null && (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Worm || ESCache.Instance.ActiveShip.Entity.IsFrigate || ESCache.Instance.ActiveShip.Entity.IsDestroyer)))
            {
                if (!ESCache.Instance.DirectEve.Session.InFleet)
                {
                    Log.WriteLine("FormFleet: Frigate Abyssals");
                    ESCache.Instance.DirectEve.FormNewFleet();
                    return;
                }

                if (DebugConfig.DebugFleetMgr) Log.WriteLine("FormFleet: we are already in a fleet!");
                return;
            }

            if (DebugConfig.DebugFleetMgr) Log.WriteLine("False: !if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController) &&\r\n    (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsDestroyer || ESCache.Instance.ActiveShip.IsAssaultShip) &&\r\n    (AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains(\"Tranquil\") || AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains(\"Calm\") || AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains(\"Agitated\"))\r\n    )");
            return;
        }

        public static void FormFleet()
        {
            if (!ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                return;

            if (Time.Instance.Started_DateTime.AddSeconds(25) > DateTime.UtcNow)
                return;

            if (Time.Instance.LastInWarp.AddSeconds(15) > DateTime.UtcNow)
                return;

            if (Time.Instance.LastFormFleetAttempt.AddSeconds(15) > DateTime.UtcNow)
                return;

            if (!ESCache.Instance.EveAccount.IsLeader)
            {
                if (DebugConfig.DebugFleetMgr) Log.WriteLine("FormFleet: if (!ESCache.Instance.EveAccount.IsLeader)");
                return;
            }

            if (ESCache.Instance.EveAccount.UseFleetMgr)
            {
                if (DebugConfig.DebugFleetMgr) Log.WriteLine("FormFleet: if (ESCache.Instance.EveAccount.UseFleetMgr)");
                if (ESCache.Instance.DirectEve.Session.CharactersInLocal.Any(i => SlaveCharacterIds.Contains(i.CharacterId.ToString()) && FleetMembers.All(fleetMember => fleetMember.CharacterId != i.CharacterId)))
                {
                    if (DebugConfig.DebugFleetMgr) Log.WriteLine("FormFleet: if (ESCache.Instance.DirectEve.Session.CharactersInLocal.Any(i => SlaveCharacterIds.Contains(i.CharacterId.ToString()) && FleetMembers.All(fleetMember => fleetMember.CharacterId != i.CharacterId)))");
                    DirectCharacter charToInviteToFleet = ESCache.Instance.DirectEve.Session.CharactersInLocal.FirstOrDefault(i => SlaveCharacterIds.Contains(i.CharacterId.ToString()) && FleetMembers.All(fleetMember => fleetMember.CharacterId != i.CharacterId));
                    if (Time.Instance.LastFleetInvite.ContainsKey(charToInviteToFleet.CharacterId))
                    {
                        if (DateTime.UtcNow < Time.Instance.LastFleetInvite[charToInviteToFleet.CharacterId].AddSeconds(ESCache.Instance.RandomNumber(1, 3)))
                        {
                            Log.WriteLine("FormFleet: Fleet Invite for [" + charToInviteToFleet.CharacterId + "] has been sent less than 4 minutes, skipping");
                            return;
                        }
                    }

                    if (DebugConfig.DebugFleetMgr) Log.WriteLine("FormFleet: charToInviteToFleet.InviteToFleet();");
                    charToInviteToFleet.InviteToFleet();
                    return;
                }

                if (DebugConfig.DebugFleetMgr) Log.WriteLine("FormFleet: no characters found to invite");
                return;
            }

            if (DebugConfig.DebugFleetMgr) Log.WriteLine("FormFleet: UseFleetMgr is False");
            return;
        }

        private static List<string> _slaveCharacterIds = null;

        public static List<string> SlaveCharacterIds
        {
            get
            {
                if (_slaveCharacterIds != null)
                    return _slaveCharacterIds;

                _slaveCharacterIds = new List<string>();
                string tempCharacterId = ESCache.Instance.EveAccount.SlaveCharacter1ChracterId;
                string tempCharacterName = ESCache.Instance.EveAccount.SlaveCharacterName1;
                if (!string.IsNullOrEmpty(tempCharacterId))
                {
                    Log.WriteLine("SlaveCharacterName1 [" + tempCharacterName + "] SlaveCharacter1ChracterId [" + tempCharacterId + "]");
                    _slaveCharacterIds.Add(tempCharacterId);
                }

                tempCharacterId = ESCache.Instance.EveAccount.SlaveCharacter2ChracterId;
                if (!string.IsNullOrEmpty(tempCharacterId))
                {
                    Log.WriteLine("SlaveCharacterName2 [" + tempCharacterName + "] SlaveCharacter2ChracterId [" + tempCharacterId + "]");
                    _slaveCharacterIds.Add(tempCharacterId);
                }

                tempCharacterId = ESCache.Instance.EveAccount.SlaveCharacter3ChracterId;
                if (!string.IsNullOrEmpty(tempCharacterId))
                {
                    Log.WriteLine("SlaveCharacterName3 [" + tempCharacterName + "] SlaveCharacter3ChracterId [" + tempCharacterId + "]");
                    _slaveCharacterIds.Add(tempCharacterId);
                }

                tempCharacterId = ESCache.Instance.EveAccount.SlaveCharacter4ChracterId;
                if (!string.IsNullOrEmpty(tempCharacterId))
                {
                    Log.WriteLine("SlaveCharacterName4 [" + tempCharacterName + "] SlaveCharacter4ChracterId [" + tempCharacterId + "]");
                    _slaveCharacterIds.Add(tempCharacterId);
                }

                tempCharacterId = ESCache.Instance.EveAccount.SlaveCharacter5ChracterId;
                if (!string.IsNullOrEmpty(tempCharacterId))
                {
                    Log.WriteLine("SlaveCharacterName5 [" + tempCharacterName + "] SlaveCharacter5ChracterId [" + tempCharacterId + "]");
                    _slaveCharacterIds.Add(tempCharacterId);
                }

                tempCharacterId = ESCache.Instance.EveAccount.SlaveCharacter6ChracterId;
                if (!string.IsNullOrEmpty(tempCharacterId))
                {
                    Log.WriteLine("SlaveCharacterName6 [" + tempCharacterName + "] SlaveCharacter6ChracterId [" + tempCharacterId + "]");
                    _slaveCharacterIds.Add(tempCharacterId);
                }

                tempCharacterId = ESCache.Instance.EveAccount.SlaveCharacter7ChracterId;
                if (!string.IsNullOrEmpty(tempCharacterId))
                {
                    Log.WriteLine("SlaveCharacterName7 [" + tempCharacterName + "] SlaveCharacter7ChracterId [" + tempCharacterId + "]");
                    _slaveCharacterIds.Add(tempCharacterId);
                }

                tempCharacterId = ESCache.Instance.EveAccount.SlaveCharacter8ChracterId;
                if (!string.IsNullOrEmpty(tempCharacterId))
                {
                    Log.WriteLine("SlaveCharacterName8 [" + tempCharacterName + "] SlaveCharacter8ChracterId [" + tempCharacterId + "]");
                    _slaveCharacterIds.Add(tempCharacterId);
                }

                tempCharacterId = ESCache.Instance.EveAccount.SlaveCharacter9ChracterId;
                if (!string.IsNullOrEmpty(tempCharacterId))
                {
                    Log.WriteLine("SlaveCharacterName9 [" + tempCharacterName + "] SlaveCharacter9ChracterId [" + tempCharacterId + "]");
                    _slaveCharacterIds.Add(tempCharacterId);
                }

                tempCharacterId = ESCache.Instance.EveAccount.SlaveCharacter10ChracterId;
                if (!string.IsNullOrEmpty(tempCharacterId))
                {
                    Log.WriteLine("SlaveCharacterName10 [" + tempCharacterName + "] SlaveCharacter10ChracterId [" + tempCharacterId + "]");
                    _slaveCharacterIds.Add(tempCharacterId);
                }

                tempCharacterId = ESCache.Instance.EveAccount.SlaveCharacter11ChracterId;
                if (!string.IsNullOrEmpty(tempCharacterId))
                {
                    Log.WriteLine("SlaveCharacterName11 [" + tempCharacterName + "] SlaveCharacter11ChracterId [" + tempCharacterId + "]");
                    _slaveCharacterIds.Add(tempCharacterId);
                }

                tempCharacterId = ESCache.Instance.EveAccount.SlaveCharacter12ChracterId;
                if (!string.IsNullOrEmpty(tempCharacterId))
                {
                    Log.WriteLine("SlaveCharacterName12 [" + tempCharacterName + "] SlaveCharacter12ChracterId [" + tempCharacterId + "]");
                    _slaveCharacterIds.Add(tempCharacterId);
                }

                tempCharacterId = ESCache.Instance.EveAccount.SlaveCharacter13ChracterId;
                if (!string.IsNullOrEmpty(tempCharacterId))
                {
                    Log.WriteLine("SlaveCharacterName13 [" + tempCharacterName + "] SlaveCharacter13ChracterId [" + tempCharacterId + "]");
                    _slaveCharacterIds.Add(tempCharacterId);
                }

                tempCharacterId = ESCache.Instance.EveAccount.SlaveCharacter14ChracterId;
                if (!string.IsNullOrEmpty(tempCharacterId))
                {
                    Log.WriteLine("SlaveCharacterName14 [" + tempCharacterName + "] SlaveCharacter14ChracterId [" + tempCharacterId + "]");
                    _slaveCharacterIds.Add(tempCharacterId);
                }

                tempCharacterId = ESCache.Instance.EveAccount.SlaveCharacter15ChracterId;
                if (!string.IsNullOrEmpty(tempCharacterId))
                {
                    Log.WriteLine("SlaveCharacterName15 [" + tempCharacterName + "] SlaveCharacter15ChracterId [" + tempCharacterId + "]");
                    _slaveCharacterIds.Add(tempCharacterId);
                }

                tempCharacterId = ESCache.Instance.EveAccount.SlaveCharacter16ChracterId;
                if (!string.IsNullOrEmpty(tempCharacterId))
                {
                    Log.WriteLine("SlaveCharacterName16 [" + tempCharacterName + "] SlaveCharacter16ChracterId [" + tempCharacterId + "]");
                    _slaveCharacterIds.Add(tempCharacterId);
                }

                tempCharacterId = ESCache.Instance.EveAccount.SlaveCharacter17ChracterId;
                if (!string.IsNullOrEmpty(tempCharacterId))
                {
                    Log.WriteLine("SlaveCharacterName17 [" + tempCharacterName + "] SlaveCharacter17ChracterId [" + tempCharacterId + "]");
                    _slaveCharacterIds.Add(tempCharacterId);
                }

                tempCharacterId = ESCache.Instance.EveAccount.SlaveCharacter18ChracterId;
                if (!string.IsNullOrEmpty(tempCharacterId))
                {
                    Log.WriteLine("SlaveCharacterName18 [" + tempCharacterName + "] SlaveCharacter18ChracterId [" + tempCharacterId + "]");
                    _slaveCharacterIds.Add(tempCharacterId);
                }

                tempCharacterId = ESCache.Instance.EveAccount.SlaveCharacter19ChracterId;
                if (!string.IsNullOrEmpty(tempCharacterId))
                {
                    Log.WriteLine("SlaveCharacterName19 [" + tempCharacterName + "] SlaveCharacter19ChracterId [" + tempCharacterId + "]");
                    _slaveCharacterIds.Add(tempCharacterId);
                }

                tempCharacterId = ESCache.Instance.EveAccount.SlaveCharacter20ChracterId;
                if (!string.IsNullOrEmpty(tempCharacterId))
                {
                    Log.WriteLine("SlaveCharacterName20 [" + tempCharacterName + "] SlaveCharacter20ChracterId [" + tempCharacterId + "]");
                    _slaveCharacterIds.Add(tempCharacterId);
                }

                tempCharacterId = ESCache.Instance.EveAccount.SlaveCharacter21ChracterId;
                if (!string.IsNullOrEmpty(tempCharacterId))
                {
                    Log.WriteLine("SlaveCharacterName21 [" + tempCharacterName + "] SlaveCharacter21ChracterId [" + tempCharacterId + "]");
                    _slaveCharacterIds.Add(tempCharacterId);
                }

                tempCharacterId = ESCache.Instance.EveAccount.SlaveCharacter22ChracterId;
                if (!string.IsNullOrEmpty(tempCharacterId))
                {
                    Log.WriteLine("SlaveCharacterName22 [" + tempCharacterName + "] SlaveCharacter22ChracterId [" + tempCharacterId + "]");
                    _slaveCharacterIds.Add(tempCharacterId);
                }

                tempCharacterId = ESCache.Instance.EveAccount.SlaveCharacter23ChracterId;
                if (!string.IsNullOrEmpty(tempCharacterId))
                {
                    Log.WriteLine("SlaveCharacterName23 [" + tempCharacterName + "] SlaveCharacter23ChracterId [" + tempCharacterId + "]");
                    _slaveCharacterIds.Add(tempCharacterId);
                }

                tempCharacterId = ESCache.Instance.EveAccount.SlaveCharacter24ChracterId;
                if (!string.IsNullOrEmpty(tempCharacterId))
                {
                    Log.WriteLine("SlaveCharacterName24 [" + tempCharacterName + "] SlaveCharacter24ChracterId [" + tempCharacterId + "]");
                    _slaveCharacterIds.Add(tempCharacterId);
                }

                tempCharacterId = ESCache.Instance.EveAccount.SlaveCharacter25ChracterId;
                if (!string.IsNullOrEmpty(tempCharacterId))
                {
                    Log.WriteLine("SlaveCharacterName25 [" + tempCharacterName + "] SlaveCharacter25ChracterId [" + tempCharacterId + "]");
                    _slaveCharacterIds.Add(tempCharacterId);
                }

                tempCharacterId = ESCache.Instance.EveAccount.SlaveCharacter26ChracterId;
                if (!string.IsNullOrEmpty(tempCharacterId))
                {
                    Log.WriteLine("SlaveCharacterName26 [" + tempCharacterName + "] SlaveCharacter26ChracterId [" + tempCharacterId + "]");
                    _slaveCharacterIds.Add(tempCharacterId);
                }

                tempCharacterId = ESCache.Instance.EveAccount.SlaveCharacter27ChracterId;
                if (!string.IsNullOrEmpty(tempCharacterId))
                {
                    Log.WriteLine("SlaveCharacterName27 [" + tempCharacterName + "] SlaveCharacter27ChracterId [" + tempCharacterId + "]");
                    _slaveCharacterIds.Add(tempCharacterId);
                }

                tempCharacterId = ESCache.Instance.EveAccount.SlaveCharacter28ChracterId;
                if (!string.IsNullOrEmpty(tempCharacterId))
                {
                    Log.WriteLine("SlaveCharacterName28 [" + tempCharacterName + "] SlaveCharacter28ChracterId [" + tempCharacterId + "]");
                    _slaveCharacterIds.Add(tempCharacterId);
                }

                tempCharacterId = ESCache.Instance.EveAccount.SlaveCharacter29ChracterId;
                if (!string.IsNullOrEmpty(tempCharacterId))
                {
                    Log.WriteLine("SlaveCharacterName29 [" + tempCharacterName + "] SlaveCharacter29ChracterId [" + tempCharacterId + "]");
                    _slaveCharacterIds.Add(tempCharacterId);
                }

                tempCharacterId = ESCache.Instance.EveAccount.SlaveCharacter30ChracterId;
                if (!string.IsNullOrEmpty(tempCharacterId))
                {
                    Log.WriteLine("SlaveCharacterName30 [" + tempCharacterName + "] SlaveCharacter30ChracterId [" + tempCharacterId + "]");
                    _slaveCharacterIds.Add(tempCharacterId);
                }

                return _slaveCharacterIds ?? new List<string>();
            }
        }

        private static int _cachedFleetMemberCount;

        private static int _fleetMemberCount;

        private static List<DirectFleetMember> _fleetMembers = new List<DirectFleetMember>();

        public static List<DirectFleetMember> FleetMembers
        {
            get
            {
                try
                {
                    if (_fleetMembers == null)
                    {
                        _fleetMembers = ESCache.Instance.DirectEve.GetFleetMembers;
                        _fleetMemberCount = _fleetMembers.Count;
                        if (_fleetMemberCount > _cachedFleetMemberCount)
                            _cachedFleetMemberCount = _fleetMemberCount;

                        Time.Instance.LastFleetMemberTimeStamp = new Dictionary<long, DateTime>();
                        foreach (DirectFleetMember fleetmember in _fleetMembers)
                            Time.Instance.LastFleetMemberTimeStamp.AddOrUpdate(fleetmember.CharacterId, DateTime.UtcNow);

                        return _fleetMembers ?? new List<DirectFleetMember>();
                    }

                    return _fleetMembers ?? new List<DirectFleetMember>();
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception: " + ex);
                    return new List<DirectFleetMember>();
                }
            }
        }

        public static bool IsInLocalWithMe(string CharacterToLookFor)
        {
            var local = ESCache.Instance.DirectEve.ChatWindows.FirstOrDefault(w => w.Name.StartsWith("chatchannel_local"));

            if (local == null)
            {
                if (DebugConfig.DebugFleetMgr) Log.WriteLine("if (local == null) return false;");
                return false;
            }

            //if in wspace we cant see local members and thus have to assume they are in local! Can we ask the launcher?!
            if (ESCache.Instance.DirectEve.Session.IsWspace)
            {
                if (DebugConfig.DebugFleetMgr) Log.WriteLine("if (DirectEve.Session.IsWspace) return true;");
                return true;
            }

            if (local.Members != null && !local.Members.Any())
            {
                if (DebugConfig.DebugFleetMgr) Log.WriteLine("if (local.Members != null && local.Members.Any()) return false;");
                return false;
            }

            if (local.Members.Any(i => i.Name.ToLower() == CharacterToLookFor.ToLower()))
            {
                return true;
            }

            return false;
        }

        public static DirectCharacter FindCharacterInLocalWithMe(string CharacterToLookFor)
        {
            var local = ESCache.Instance.DirectEve.ChatWindows.FirstOrDefault(w => w.Name.StartsWith("chatchannel_local"));

            if (local == null)
            {
                if (DebugConfig.DebugFleetMgr) Log.WriteLine("if (local == null) return false;");
                return null;
            }

            //if in wspace we cant see local members and thus have to assume they are in local! Can we ask the launcher?!
            if (ESCache.Instance.DirectEve.Session.IsWspace)
            {
                if (DebugConfig.DebugFleetMgr) Log.WriteLine("if (DirectEve.Session.IsWspace) return true;");
                return null;
            }

            if (local.Members != null && !local.Members.Any())
            {
                if (DebugConfig.DebugFleetMgr) Log.WriteLine("if (local.Members != null && local.Members.Any()) return false;");
                return null;
            }

            if (local.Members.Any(i => i.Name.ToLower() == CharacterToLookFor.ToLower()))
            {
                return local.Members.FirstOrDefault(i => i.Name.ToLower() == CharacterToLookFor.ToLower());
            }

            return null;
        }

        public static  void DoFleetInvites(Dictionary<string, string> PotentialInviteees)
        {
            try
            {
                if (!DirectEve.Interval(10000))
                    return;

                if (Time.Instance.LastDockAction.AddSeconds(20) > DateTime.UtcNow)
                    return;

                if (Time.Instance.LastActivateAccelerationGate.AddSeconds(20) > DateTime.UtcNow)
                    return;

                if (Time.Instance.LastJumpAction.AddSeconds(20) > DateTime.UtcNow)
                    return;

                if (!ESCache.Instance.EveAccount.IsLeader)
                    return;

                if (DebugConfig.DebugFleetMgr) Log.WriteLine("Leader [" + ESCache.Instance.EveAccount.IsLeader + "]");

                if (!PotentialInviteees.Any())
                    return;

                if (DebugConfig.DebugFleetMgr) Log.WriteLine("PotentialInviteees [" + PotentialInviteees.Count() + "]");
                /**
                if (!ESCache.Instance.DirectEve.IsInFleet)
                {
                    if (DebugConfig.DebugFleetMgr) Log.WriteLine("if (!ESCache.Instance.DirectEve.IsInFleet) FormNewFleet");
                    //this doesnt create squads and wings and probably needs to be fixed
                    ESCache.Instance.DirectEve.FormNewFleet();
                    return;
                }
                **/

                if (DebugConfig.DebugFleetMgr) Log.WriteLine("Fleet has [" + ESCache.Instance.DirectEve.FleetMembers.Count() + "] members");

                //Log("We are the fleet leader.");
                // We are the leader, so we send invites to all other members
                // Get member list of the channel to invite: this is pulled from the launcher setting: ChatChannelToPullFleetInvitesFrom
                foreach (var individualEveAccoutForThisInvitee in PotentialInviteees)
                {
                    if (DebugConfig.DebugFleetMgr) Log.WriteLine("[" + individualEveAccoutForThisInvitee + "]");
                    if (ESCache.Instance.DirectEve.FleetMembers.All(i => i.Name.ToLower() != individualEveAccoutForThisInvitee.Key.ToLower()))
                    {
                        if (DebugConfig.DebugFleetMgr) Log.WriteLine("if (ESCache.Instance.DirectEve.FleetMembers.All(i => i.Name.ToLower() != individualCharacter.ToLower()))");
                        bool _isInLocalWithMe = IsInLocalWithMe(individualEveAccoutForThisInvitee.Key);
                        if (_isInLocalWithMe)
                        {
                            if (DebugConfig.DebugFleetMgr) Log.WriteLine("individualCharacter [" + individualEveAccoutForThisInvitee.Key + "] IsInLocalWithMe [" + _isInLocalWithMe + "]!.!");
                            if (DirectEve.Interval(20000, 30000, individualEveAccoutForThisInvitee.Key))
                            {
                                Log.WriteLine("Inviting [" + individualEveAccoutForThisInvitee.Key + "] to fleet");
                                var _directCharacter = new DirectCharacter(ESCache.Instance.DirectEve);
                                _directCharacter.CharacterId = long.Parse(individualEveAccoutForThisInvitee.Value);
                                _directCharacter.InviteToFleet();
                                continue;
                            }

                            Log.WriteLine("Invite sent to [" + individualEveAccoutForThisInvitee.Key + "] waiting...");
                            continue;
                        }

                        if (ESCache.Instance.InAbyssalDeadspace || ESCache.Instance.InWormHoleSpace)
                        {
                            if (DebugConfig.DebugFleetMgr) Log.WriteLine("individualCharacter [" + individualEveAccoutForThisInvitee.Key + "] IsInLocalWithMe [" + _isInLocalWithMe + "]!.!");
                            if (DirectEve.Interval(20000, 30000, individualEveAccoutForThisInvitee.Key))
                            {
                                Log.WriteLine("Inviting [" + individualEveAccoutForThisInvitee.Key + "] to fleet");
                                var _directCharacter = new DirectCharacter(ESCache.Instance.DirectEve);
                                _directCharacter.CharacterId = long.Parse(individualEveAccoutForThisInvitee.Value);
                                _directCharacter.InviteToFleet();
                                continue;
                            }

                            Log.WriteLine("Invite sent to [" + individualEveAccoutForThisInvitee.Key + "] waiting...");
                            continue;
                        }

                        Log.WriteLine("individualCharacter [" + individualEveAccoutForThisInvitee.Key + "] IsInLocalWithMe ![" + _isInLocalWithMe + "]!");
                        continue;
                    }

                    if (DebugConfig.DebugFleetMgr) Log.WriteLine("individualCharacter [" + individualEveAccoutForThisInvitee + "] IsInFleetWithMe !![ true? ]!!");
                    continue;
                }

                if (DebugConfig.DebugFleetMgr) Log.WriteLine("after ForEach Loop");
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return;
            }
        }

        public static void DropFleetIfNeeded(Dictionary<string, string> PotentialInviteees)
        {
            try
            {
                if (ESCache.Instance.EveAccount.IsLeader) //Leader never drops fleet (never?)
                    return;

                if (DebugConfig.DebugFleetMgr) Log.WriteLine("Leader [" + ESCache.Instance.EveAccount.IsLeader + "]");

                if (!PotentialInviteees.Any()) //if there is no one other than ourselves logged in no need to drop fleet
                    return;

                if (DebugConfig.DebugFleetMgr) Log.WriteLine("PotentialInviteees [" + PotentialInviteees.Count() + "]");

                if (!ESCache.Instance.DirectEve.IsInFleet) //if we are not in a fleet we dont need to drop fleet
                {
                    if (DebugConfig.DebugFleetMgr) Log.WriteLine("if (!ESCache.Instance.DirectEve.IsInFleet) No need to drop fleet: we arent in one");
                    return;
                }

                //We probably want to detect who is fleet boss at this point and just re-invite the damn leader.
                //The character designated leader in the launcher must have disconnected/reconnected
                //if we cant detect that then, yeah, we can drop and recreate fleet but thats a bug hammer for a small problem

                if (DebugConfig.DebugFleetMgr) Log.WriteLine("LeaderCharacterName [" + ESCache.Instance.EveAccount.LeaderCharacterName + "]");
                if (string.IsNullOrEmpty(ESCache.Instance.EveAccount.LeaderCharacterName))
                {
                    //Assume its the other guy in fleet if its not us ffs!
                    if (ESCache.Instance.DirectEve.FleetMembers.Count == 2 && !ESCache.Instance.EveAccount.IsLeader)
                    {
                        //what will this do if both are not  the leader and waiting on the real leader to login or something?
                        var defaultLeader = ESCache.Instance.DirectEve.FleetMembers.FirstOrDefault(i => i.Name != ESCache.Instance.EveAccount.CharacterName);
                        if (defaultLeader != null)
                        {
                            Log.WriteLine("LeaderCharacterName was blank. setting it to [" + defaultLeader.Name + "] - fixme!");
                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderCharacterName), defaultLeader.Name);
                            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LeaderCharacterId), defaultLeader.CharacterId);
                            return;
                        }
                    }
                }

                if (ESCache.Instance.DirectEve.FleetMembers.All(e => e.Name != ESCache.Instance.EveAccount.LeaderCharacterName && !string.IsNullOrEmpty(ESCache.Instance.EveAccount.LeaderCharacterName)))
                {
                    if (DebugConfig.DebugFleetMgr) Log.WriteLine("if (ESCache.Instance.DirectEve.FleetMembers.All(e => e.Name != ESCache.Instance.EveAccount.LeaderCharacterName))");
                    if (ESCache.Instance.DirectEve.IsInFleet)
                    {
                        Log.WriteLine("We are not the fleet leader, and the leader [" + ESCache.Instance.EveAccount.LeaderCharacterName + "] is not within our fleet. Dropping fleet.");
                        ESCache.Instance.DirectEve.LeaveFleet();
                        return;
                    }

                    if (DebugConfig.DebugFleetMgr) Log.WriteLine("!if (ESCache.Instance.DirectEve.IsInFleet)");
                    return;
                }

                if (DebugConfig.DebugFleetMgr) Log.WriteLine("!if (ESCache.Instance.DirectEve.FleetMembers.All(e => e.Name != ESCache.Instance.EveAccount.LeaderCharacterName))");
                return;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return;
            }
        }

        public static void HandleFleetInvitesFromOthers(Dictionary<string, string> PotentialInviteees)
        {


            // Fleet receive invite handling
            if (ESCache.Instance.DirectEve.ModalWindows.Any(w => w.MessageKey == "AskJoinFleet"))
            {
                try
                {
                    var fleetInviteWindow =
                        ESCache.Instance.DirectEve.ModalWindows.FirstOrDefault(w => w.MessageKey == "AskJoinFleet");

                    if (fleetInviteWindow == null)
                        return;

                    var invitorHtml =
                        fleetInviteWindow.Html.Substring(0,
                            fleetInviteWindow.Html.IndexOf(" wants you to join", StringComparison.Ordinal));

                    Log.WriteLine("A fleet invite request was made from [" + invitorHtml + "]");
                    //Log.WriteLine("ESCache.Instance.EveAccount.LeaderCharacterName [" + ESCache.Instance.EveAccount.LeaderCharacterName + "]");

                    if (!ESCache.Instance.EveAccount.IsLeader)
                    {
                        if (DebugConfig.DebugFleetMgr) Log.WriteLine("if (!ESCache.Instance.EveAccount.IsLeader)");
                        if (!ESCache.Instance.DirectEve.IsInFleet)
                        {
                            if (DebugConfig.DebugFleetMgr) Log.WriteLine("if (!ESCache.Instance.DirectEve.IsInFleet)");
                            // Check if the invite is from a character on our invite list and cancel if not
                            //if (PotentialInviteees.Any(n => ESCache.Instance.EveAccount.LeaderCharacterName == n.Name && invitorHtml.Contains($">{n.Name}<")))
                            if (PotentialInviteees.Any(n => invitorHtml.Contains($">{n.Key}<")))
                            {
                                // Accept the invite
                                Log.WriteLine("A fleet invite request was made from [" + invitorHtml + "]. Accepting now.");
                                fleetInviteWindow.AnswerModal("Yes");
                                return;
                            }

                            if (DebugConfig.DebugFleetMgr) Log.WriteLine("if (PotentialInviteees.Any(n => ESCache.Instance.EveAccount.LeaderCharacterName == n.Name && invitorHtml.Contains($\">{n.Name}<\")))");
                            // Close the invite
                            Log.WriteLine($"Window.Html: {fleetInviteWindow.Html}");
                            Log.WriteLine($"Fleet invite from {invitorHtml}. Will be closed now.");
                            //DirectEventManager.NewEvent(new DirectEvent(DirectEvents.PRIVATE_CHAT_RECEIVED, "Fleet invite received from: " + invitorHtml));
                            Log.WriteLine($"Closed fleet invitation from [{invitorHtml}]");
                            fleetInviteWindow.Close();
                            return;
                        }

                        if (DebugConfig.DebugFleetMgr) Log.WriteLine("We are already in a fleet");
                        // Close the invite
                        Log.WriteLine($"Window.Html: {fleetInviteWindow.Html}");
                        Log.WriteLine($"Fleet invite from {invitorHtml}. Will be closed now.");
                        //DirectEventManager.NewEvent(new DirectEvent(DirectEvents.PRIVATE_CHAT_RECEIVED, "Fleet invite received from: " + invitorHtml));
                        Log.WriteLine($"Closed fleet invitation from [{invitorHtml}]");
                        fleetInviteWindow.Close();
                        return;
                    }

                    if (DebugConfig.DebugFleetMgr) Log.WriteLine("IsLeader [" + ESCache.Instance.EveAccount.IsLeader + "]");
                    // Close the invite
                    Log.WriteLine($"Window.Html: {fleetInviteWindow.Html}");
                    Log.WriteLine($"Fleet invite from {invitorHtml}. Will be closed now.");
                    //DirectEventManager.NewEvent(new DirectEvent(DirectEvents.PRIVATE_CHAT_RECEIVED, "Fleet invite received from: " + invitorHtml));
                    Log.WriteLine($"Closed fleet invitation from [{invitorHtml}]");
                    fleetInviteWindow.Close();
                    return;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return;
                }
            }
        }

        public static void FleetManager()
        {
            try
            {
                // Fleet send invite handling
                if (ESCache.Instance.EveAccount.UseFleetMgr)
                {
                    if (!DirectEve.Interval(5000))
                        return;

                    if (DebugConfig.DebugFleetMgr && DirectEve.Interval(10000))
                    {
                        Log.WriteLine("UseFleetMgr [" + ESCache.Instance.EveAccount.UseFleetMgr + "]");
                        Log.WriteLine("IsInFleet [" + ESCache.Instance.DirectEve.IsInFleet + "]");
                        if (ESCache.Instance.DirectEve.IsInFleet)
                        {
                            Log.WriteLine("Fleet Contains [" + ESCache.Instance.DirectEve.FleetMembers.Count() + "] Members");
                        }
                    }

                    Dictionary<string, string> PotentialInviteees = new Dictionary<string, string>();
                    if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalController))
                    {
                        if (Settings.Instance.AbyssalFleetMemberName1 != string.Empty || Settings.Instance.AbyssalFleetMemberName2 != string.Empty || Settings.Instance.AbyssalFleetMemberName3 != string.Empty)
                        {
                            if (DebugConfig.DebugFleetMgr) Log.WriteLine("if (channelToPullFleetInvitesFrom != null && channelToPullFleetInvitesFrom.Members.Any(i => i.Name != ESCache.Instance.CharName))");
                            PotentialInviteees = new Dictionary<string, string>();
                            if (Settings.Instance.AbyssalFleetMemberName1 != string.Empty && Settings.Instance.AbyssalFleetMemberCharacterId1 != string.Empty)
                                PotentialInviteees.Add(Settings.Instance.AbyssalFleetMemberName1, Settings.Instance.AbyssalFleetMemberCharacterId1);
                            if (Settings.Instance.AbyssalFleetMemberName2 != string.Empty && Settings.Instance.AbyssalFleetMemberCharacterId2 != string.Empty)
                                PotentialInviteees.Add(Settings.Instance.AbyssalFleetMemberName2, Settings.Instance.AbyssalFleetMemberCharacterId2);
                            if (Settings.Instance.AbyssalFleetMemberName3 != string.Empty && Settings.Instance.AbyssalFleetMemberCharacterId2 != string.Empty)
                                PotentialInviteees.Add(Settings.Instance.AbyssalFleetMemberName3, Settings.Instance.AbyssalFleetMemberCharacterId3);

                            if (PotentialInviteees.Any())
                            {
                                DoFleetInvites(PotentialInviteees);
                                DropFleetIfNeeded(PotentialInviteees);
                                HandleFleetInvitesFromOthers(PotentialInviteees);
                                return;
                            }

                            if (DebugConfig.DebugFleetMgr) Log.WriteLine("!if (PotentialInviteees.Any())");
                            return;
                        }

                        return;
                    }

                    /**
                    PotentialInviteees = new Dictionary<string, string>();
                    if (DebugConfig.DebugFleetMgr) Log.WriteLine("ESCache.Instance.MyLeaderAndSlaveNamesFromLauncher count [" + ESCache.Instance.MyLeaderAndSlaveNamesFromLauncher.Count() + "]");
                    foreach (var member in ESCache.Instance.MyLeaderAndSlaveNamesFromLauncher)
                    {
                        if (member == ESCache.Instance.CharName)
                            continue;

                        foreach (var player in ESCache.Instance.Entities.Where(i => i.IsPlayer))
                        {
                            if (member == player.Name)
                            {
                                PotentialInviteees.Add(player.Name, player.Id.ToString());
                                continue;
                            }

                            continue;
                        }

                        continue;
                    }

                    if (PotentialInviteees.Any())
                    {
                        DoFleetInvites(PotentialInviteees);
                        DropFleetIfNeeded(PotentialInviteees);
                        HandleFleetInvitesFromOthers(PotentialInviteees);
                        return;
                    }
                    **/

                    if (DebugConfig.DebugFleetMgr) Log.WriteLine("PotentialInviteees is empty?!");
                    return;
                }
                else
                {
                    if (DebugConfig.DebugFleetMgr) Log.WriteLine("UseFleetMgr [" + ESCache.Instance.EveAccount.UseFleetMgr + "]");
                    if (ESCache.Instance.InSpace &&
                        ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController) &&
                        ESCache.Instance.Weapons.Any() &&
                        (ESCache.Instance.ActiveShip != null && (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Worm || ESCache.Instance.ActiveShip.Entity.IsFrigate || ESCache.Instance.ActiveShip.Entity.IsDestroyer)) && //|| ESCache.Instance.ActiveShip.Entity.IsAssaultShip) &&
                        (AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Tranquil") || AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Calm") || AbyssalDeadspaceBehavior.AbyssalDeadspaceFilamentName.Contains("Agitated"))
                        )
                    {
                        FormFrigateAbyssalFleet();
                    }

                    if (ESCache.Instance.InSpace &&
                    ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalController) &&
                    ESCache.Instance.Weapons.Any() &&
                    (ESCache.Instance.ActiveShip != null && (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Worm || ESCache.Instance.ActiveShip.Entity.IsFrigate || ESCache.Instance.ActiveShip.Entity.IsDestroyer)))
                    {
                        FormFrigateAbyssalFleet();
                    }
                }
            }
            catch (Exception ex)
            {
                if (DebugConfig.DebugFleetMgr) Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public static string GetExternalIPAddress()
        {
            string result = string.Empty;
            try
            {
                using (var client = new System.Net.WebClient())
                {
                    client.Headers["User-Agent"] =
                    "Mozilla/4.0 (Compatible; Windows NT 5.1; MSIE 6.0) " +
                    "(compatible; MSIE 6.0; Windows NT 5.1; " +
                    ".NET CLR 1.1.4322; .NET CLR 2.0.50727)";

                    try
                    {
                        byte[] arr = client.DownloadData("http://checkip.amazonaws.com/");

                        string response = System.Text.Encoding.UTF8.GetString(arr);

                        result = response.Trim();
                    }
                    catch (WebException)
                    {
                        //ignore this exception
                    }
                }
            }
            catch
            {
                //ignore this exception
            }

            if (string.IsNullOrEmpty(result))
            {
                try
                {
                    result = new WebClient().DownloadString("https://ipinfo.io/ip").Replace("\n", "");
                }
                catch
                {
                    //ignore this exception
                }
            }

            if (string.IsNullOrEmpty(result))
            {
                try
                {
                    result = new WebClient().DownloadString("https://api.ipify.org").Replace("\n", "");
                }
                catch
                {
                    //ignore this exception
                }
            }

            if (string.IsNullOrEmpty(result))
            {
                try
                {
                    result = new WebClient().DownloadString("https://icanhazip.com").Replace("\n", "");
                }
                catch
                {
                    //ignore this exception
                }
            }

            if (string.IsNullOrEmpty(result))
            {
                try
                {
                    result = new WebClient().DownloadString("https://wtfismyip.com/text").Replace("\n", "");
                }
                catch
                {
                    //ignore this exception
                }
            }

            if (string.IsNullOrEmpty(result))
            {
                try
                {
                    result = new WebClient().DownloadString("http://bot.whatismyipaddress.com/").Replace("\n", "");
                }
                catch
                {
                    //ignore this exception
                }
            }

            if (string.IsNullOrEmpty(result))
            {
                try
                {
                    const string url = "http://checkip.dyndns.org";
                    System.Net.WebRequest req = System.Net.WebRequest.Create(url);
                    System.Net.WebResponse resp = req.GetResponse();
                    System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream());
                    string response = sr.ReadToEnd().Trim();
                    string[] a = response.Split(':');
                    string a2 = a[1].Substring(1);
                    string[] a3 = a2.Split('<');
                    result = a3[0];
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return "Firewall Rule is Working: Traffic Blocked";
                }
            }

            return result;
        }

        public static void ProcessState()
        {
            if (DebugConfig.DebugDisableCleanup)
                return;

            FleetManager();

            CheckWindows();
            //FormFleet();
        }

            #endregion Methods
        }
    }