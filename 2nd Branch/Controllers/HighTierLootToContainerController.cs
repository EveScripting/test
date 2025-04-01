
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using EVESharpCore.Questor.Actions;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Cache;
using EVESharpCore.Logging;
using EVESharpCore.Questor.Lookup;
using EVESharpCore.Questor.States;
using EVESharpCore.Questor.Activities;

namespace EVESharpCore.Controllers
{
    /// <summary>
    ///     Description of ExampleController.
    /// </summary>
    public class HighTierLootToContainerController : BaseController
    {
        public HighTierLootToContainerController()
        {
            IgnorePause = true;
            IgnoreModal = false;
            _States.CurrentHighTierLootToContainerState = HighTierLootToContainerState.Idle;
        }

        public override bool EvaluateDependencies(List<BaseController> controllerList)
        {
            var loginController = controllerList.FirstOrDefault(c => c.GetType() == typeof(LoginController));
            if (loginController == null || !loginController.IsWorkDone)
                return false;

            return true;
        }

        private static DateTime _lastUnloadAction = DateTime.MinValue;
        private static bool LootIsBeingMoved;
        private static long CurrentLootValue { get; set; }

        public override void DoWork()
        {
            try
            {
                if (!CheckSessionValid("HighTierLootToContainerController"))
                {
                    //if (DebugConfig.DebugDefenseController) Log("CheckSessionValid returned false");
                    return;
                }

                if (QCache.Instance.InSpace && !QCache.Instance.InStation)
                {
                    if (QCache.Instance.Stargates != null && QCache.Instance.Stargates.Any())
                    {
                        if (QCache.Instance.ClosestStargate != null && QCache.Instance.ClosestStargate.IsOnGridWithMe && QCache.Instance.ClosestStargate.Distance < 10000)
                        {
                            if (DebugConfig.DebugCleanup) Log("CheckModalWindows: We are within 10k of a stargate, do nothing while we wait to jump. We will wait 30 sec before resuming");
                            return;
                        }
                    }
                }

                if (!Settings.Instance.DefaultSettingsLoaded)
                {
                    Settings.Instance.LoadSettings();
                }

                //if (DebugConfig.DebugDefenseController) Log("CombatController.DoWork()");

                ProcessState();
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
        }

        public static bool ChangeHighTierLootToContainerState(HighTierLootToContainerState state, bool wait = true)
        {
            try
            {
                if (_States.CurrentHighTierLootToContainerState != state)
                {
                    //ClearDataBetweenStates();
                    Log("New UnloadLootState [" + state.ToString() + "]");
                    _States.CurrentHighTierLootToContainerState = state;
                }

                if (wait)
                    _lastUnloadAction = DateTime.UtcNow;
                else
                {
                    // ProcessState again as we have not interacted with eve thus we do not need to wait 500ms
                    // the ability to ProcessState again here is the whole reason for ChangeUnloadLootState()
                    //ProcessState();
                }

                return true;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return false;
            }
        }

        private static bool WaitForLockedItems()
        {
            if (QCache.Instance.DirectEve.GetLockedItems().Count != 0)
            {
                if (Math.Abs(DateTime.UtcNow.Subtract(_lastUnloadAction).TotalSeconds) > 15)
                {
                    Log("Moving Ammo timed out, clearing item locks");
                    QCache.Instance.DirectEve.UnlockItems();
                    _lastUnloadAction = DateTime.UtcNow.AddSeconds(-1);
                    return false;
                }

                if (DebugConfig.DebugUnloadLoot)
                    Log("Waiting for Locks to clear. GetLockedItems().Count [" + QCache.Instance.DirectEve.GetLockedItems().Count + "]");
                return false;
            }

            _lastUnloadAction = DateTime.UtcNow.AddSeconds(-1);
            if (QCache.Instance.CurrentShipsCargo.Items.Any()) Log("Done");
            return true;
        }

        private static int AttemptsToReadFromhangar;

        public static bool MoveHighTierLootToContainer(DirectContainer FromHangar, DirectContainer ToContainer)
        {
            try
            {
                if (DateTime.UtcNow < _lastUnloadAction.AddMilliseconds(2000))
                    return false;

                if ((FromHangar == null || FromHangar.Items == null) || (FromHangar.Items != null && !FromHangar.Items.Any()))
                {
                    if (AttemptsToReadFromhangar < 10)
                    {
                        AttemptsToReadFromhangar++;
                        return true;
                    }

                    Log("HighTierLootToContainer: Unable to read from for FromHagar");
                    ChangeHighTierLootToContainerState(HighTierLootToContainerState.Done);
                }

                if (LootIsBeingMoved || !FromHangar.Items.Any())
                {
                    if (!WaitForLockedItems())
                        return false;

                    if (LootIsBeingMoved)
                    {
                        Log("HighTierLoot was worth an estimated [" + CurrentLootValue.ToString("#,##0") + "] isk in buy-orders");
                        LootIsBeingMoved = false;
                    }

                    ChangeHighTierLootToContainerState(HighTierLootToContainerState.Done, false);
                    return true;
                }

                List<DirectItem> highTierLootToMove = null; //FromHangar.Items.Where(i => i.Metalevel >= 6).ToList();

                if (highTierLootToMove != null && highTierLootToMove.Any() && !LootIsBeingMoved)
                {
                    try
                    {
                        CurrentLootValue = UnloadLoot.LootValueOfItems(highTierLootToMove);
                        if (ToContainer != null)
                        {
                            Log("Moving HighTier [" + highTierLootToMove.Count + "][" + highTierLootToMove.Sum(i => (i.GetAveragePrice * Math.Min(i.Quantity, 1))) + " isk] items");
                            ToContainer.Add(highTierLootToMove);
                            LootIsBeingMoved = true;
                            _lastUnloadAction = DateTime.UtcNow;
                            return false;
                        }
                        //else if (QCache.Instance.LootCorpHangar != null)
                        //{
                        //    Log.WriteLine("Moving HighTier [" + highTierLootToMove.Count + "] items from CargoHold to HighTierLoothangar");
                        //    QCache.Instance.HighTierLootCorpHangar.Add(highTierLootToMove);
                        //    LootIsBeingMoved = true;
                        //    _lastUnloadAction = DateTime.UtcNow;
                        //    return false;
                        //}
                        else
                        {
                            Log("Moving HighTier [" + highTierLootToMove.Count + "][" + highTierLootToMove.Sum(i => (i.GetAveragePrice * Math.Min(i.Quantity, 1))) + " isk] items from CargoHold to ItemHangar");
                            QCache.Instance.ItemHangar.Add(highTierLootToMove);
                            LootIsBeingMoved = true;
                            _lastUnloadAction = DateTime.UtcNow;
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log("Exception [" + ex + "]");
                        ChangeHighTierLootToContainerState(HighTierLootToContainerState.Done, true);
                        return false;
                    }
                }
                else
                {
                    ChangeHighTierLootToContainerState(HighTierLootToContainerState.Done, false);
                }
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }


            return false;
        }

        private static bool EveryUnloadLootPulse()
        {
            try
            {
                if (!QCache.Instance.InStation)
                    return false;

                if (QCache.Instance.InSpace)
                    return false;

                return true;
            }
            catch (Exception exception)
            {
                Log("Exception [" + exception + "]");
                return false;
            }
        }

        public static void ProcessState()
        {
            try
            {
                if (!EveryUnloadLootPulse()) return;

                switch (_States.CurrentHighTierLootToContainerState)
                {
                    case HighTierLootToContainerState.Idle:
                        if (QCache.Instance.ItemHangar != null && QCache.Instance.ItemHangar.Items != null && QCache.Instance.ItemHangar.Items.Any())
                        {
                            ChangeHighTierLootToContainerState(HighTierLootToContainerState.Start);
                        }
                        break;

                    case HighTierLootToContainerState.Start:
                        AttemptsToReadFromhangar = 0;
                        ChangeHighTierLootToContainerState(HighTierLootToContainerState.MoveHighTierLoot);
                        break;

                    case HighTierLootToContainerState.MoveHighTierLoot:
                        if (!MoveHighTierLootToContainer(QCache.Instance.ItemHangar, QCache.Instance.HighTierLootContainer)) return;
                        ChangeHighTierLootToContainerState(HighTierLootToContainerState.Done);
                        break;

                    case HighTierLootToContainerState.Done:
                        Log("HighTierLootToContainer: Done. Removing Controller.");
                        ControllerManager.Instance.RemoveController(typeof(BuyAmmoController));
                        break;
                }
            }
            catch (Exception exception)
            {
                Log("Exception [" + exception + "]");
                return;
            }
        }
    }
}