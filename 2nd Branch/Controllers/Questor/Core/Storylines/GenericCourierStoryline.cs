extern alias SC;

using System;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Activities;
using EVESharpCore.Traveller;
using EVESharpCore.Questor.States;
using SC::SharedComponents.EVE;

namespace EVESharpCore.Questor.Storylines
{
    public class GenericCourier : IStoryline
    {
        #region Fields

        private DateTime _nextGenericCourierStorylineAction;

        #endregion Fields

        #region Methods

        public StorylineState Arm(Storyline storyline)
        {
            if (_nextGenericCourierStorylineAction > DateTime.UtcNow) return StorylineState.Arm;

            if (ESCache.Instance.ShipHangar == null) return StorylineState.Arm;

            // Open the ship hangar
            if (ESCache.Instance.ShipHangar == null)
            {
                _nextGenericCourierStorylineAction = DateTime.UtcNow.AddSeconds(5);
                return StorylineState.Arm;
            }

            if (string.IsNullOrEmpty(Settings.Instance.StorylineTransportShipName.ToLower()))
            {
                Activities.Arm.ChangeArmState(ArmState.NotEnoughAmmo, true, MissionSettings.StorylineMission.Agent);
                Log.WriteLine("Could not find transportshipName in settings!");
                return StorylineState.BlacklistAgentForThisSession;
            }

            Activities.Arm.ActivateShip(Settings.Instance.StorylineTransportShipName);

            if (DateTime.UtcNow > Time.Instance.NextArmAction) //default 7 seconds
                if (ESCache.Instance.ActiveShip.GivenName.ToLower() == Settings.Instance.StorylineTransportShipName.ToLower())
                {
                    Log.WriteLine("Done");
                    Activities.Arm.ChangeArmState(ArmState.Done, true, MissionSettings.StorylineMission.Agent);
                    return StorylineState.BeforeGotoAgent;
                }

            return StorylineState.Arm;
        }

        public StorylineState BeforeGotoAgent(Storyline storyline)
        {
            Log.WriteLine("CourierMissionCtrlState.BeforeGotoAgent");
            if (MissionSettings.StorylineMission != null)
            {
                Log.WriteLine("CourierMissionCtrlState.BeforeGotoAgent: StorylineMission.State [" + MissionSettings.StorylineMission.State + "]");
                if (MissionSettings.StorylineMission.State == MissionState.Accepted)
                {
                    Log.WriteLine("CourierMissionCtrlState.BeforeGotoAgent [" + MissionSettings.StorylineMission.CurrentCourierMissionCtrlState + "]");
                    CourierMissionCtrl.TryToGrabPickupItemsFromHomeStation(MissionSettings.StorylineMission, MissionSettings.StorylineMission.Agent);
                    Log.WriteLine("CourierMissionCtrlState.BeforeGotoAgent.[" + MissionSettings.StorylineMission.CurrentCourierMissionCtrlState + "]");

                    if (MissionSettings.StorylineMission.CurrentCourierMissionCtrlState == CourierMissionCtrlState.GotoPickupLocation)
                    {
                        Log.WriteLine("CourierMissionCtrlState.Start");
                        CourierMissionCtrl.ChangeCourierMissionCtrlState(MissionSettings.StorylineMission, CourierMissionCtrlState.Start);
                        return StorylineState.GotoAgent;
                    }

                    return StorylineState.BeforeGotoAgent;
                }

                return StorylineState.GotoAgent;
            }

            Log.WriteLine("CourierMissionCtrlState.BeforeGotoAgent: if (MissionSettings.StorylineMission == null)");
            return StorylineState.GotoAgent;
        }

        /// <summary>
        ///     Goto the pickup location
        ///     Pickup the item
        ///     Goto drop off location
        ///     Drop the item
        ///     Complete mission
        /// </summary>
        /// <param name="storyline"></param>
        /// <returns></returns>
        public StorylineState ExecuteMission(Storyline storyline)
        {
            if (_nextGenericCourierStorylineAction > DateTime.UtcNow)
            {
                if (DebugConfig.DebugCourierMissions) Log.WriteLine("if (_nextGenericCourierStorylineAction > DateTime.UtcNow)");
                return StorylineState.ExecuteMission;
            }

            if (DebugConfig.DebugCourierMissions) Log.WriteLine("GenericCourierStoryline: CurrentCourierMissionCtrlState [" + MissionSettings.StorylineMission.CurrentCourierMissionCtrlState + "]");

            if (MissionSettings.StorylineMission == null)
            {
                Log.WriteLine("if (MissionSettings.StorylineMission == null)");
                return StorylineState.ExecuteMission;
            }

            if (MissionSettings.StorylineMission.Agent == null)
            {
                Log.WriteLine("if (MissionSettings.StorylineMission.Agent == null)");
                return StorylineState.ExecuteMission;
            }

            switch (MissionSettings.StorylineMission.CurrentCourierMissionCtrlState)
            {
                case CourierMissionCtrlState.Start:
                    if (DebugConfig.DebugCourierMissions) Log.WriteLine("GenericCourierStoryline: case CourierMissionCtrlState.Start:");
                    CourierMissionCtrl.ChangeCourierMissionCtrlState(MissionSettings.StorylineMission, CourierMissionCtrlState.GotoPickupLocation);
                    break;

                case CourierMissionCtrlState.TryToGrabPickupItemsFromHomeStation:
                    if (DebugConfig.DebugCourierMissions) Log.WriteLine("GenericCourierStoryline: case TryToGrabPickupItemsFromHomeStation.Start:");
                    CourierMissionCtrl.TryToGrabPickupItemsFromHomeStation(MissionSettings.StorylineMission, MissionSettings.StorylineMission.Agent);
                    break;

                case CourierMissionCtrlState.GotoPickupLocation:
                    if (DebugConfig.DebugCourierMissions) Log.WriteLine("GenericCourierStoryline: case GotoPickupLocation:");
                    CourierMissionCtrl.GotoPickupLocation(MissionSettings.StorylineMission);
                    break;

                case CourierMissionCtrlState.PickupItem:
                    if (DebugConfig.DebugCourierMissions) Log.WriteLine("GenericCourierStoryline: case PickupItem:");
                    if (CourierMissionCtrl.ManageCourierMission(MissionSettings.StorylineMission, MissionSettings.StorylineMission.Agent, false))
                        CourierMissionCtrl.ChangeCourierMissionCtrlState(MissionSettings.StorylineMission, CourierMissionCtrlState.GotoDropOffLocation);
                    break;

                case CourierMissionCtrlState.ItemsFoundAndBeingMoved:
                    if (DebugConfig.DebugCourierMissions) Log.WriteLine("GenericCourierStoryline: case ItemsFoundAndBeingMoved:");
                    CourierMissionCtrl.ItemsFoundAndBeingMoved(MissionSettings.StorylineMission);
                    break;

                case CourierMissionCtrlState.GotoDropOffLocation:
                    if (DebugConfig.DebugCourierMissions) Log.WriteLine("GenericCourierStoryline: case GotoDropOffLocation:");
                    CourierMissionCtrl.GotoDropoffLocation(MissionSettings.StorylineMission);
                    break;

                case CourierMissionCtrlState.DropOffItem:
                    if (DebugConfig.DebugCourierMissions) Log.WriteLine("GenericCourierStoryline: case DropOffItem:");
                    if (CourierMissionCtrl.ManageCourierMission(MissionSettings.StorylineMission, MissionSettings.StorylineMission.Agent, false))
                        return StorylineState.CompleteMission;
                    break;
            }

            return StorylineState.ExecuteMission;
        }

        /// <summary>
        ///     There are no pre-accept actions
        /// </summary>
        /// <param name="storyline"></param>
        /// <returns></returns>
        public StorylineState PreAcceptMission(Storyline storyline)
        {
            CourierMissionCtrl.ChangeCourierMissionCtrlState(MissionSettings.StorylineMission, CourierMissionCtrlState.Start);
            State.CurrentTravelerState = TravelerState.Idle;
            Traveler.Destination = null;
            return StorylineState.AcceptMission;
        }

        private bool MoveItem(bool pickup)
        {
            DirectEve directEve = ESCache.Instance.DirectEve;

            // Open the item hangar (should still be open)
            if (ESCache.Instance.ItemHangar == null) return false;

            if (ESCache.Instance.CurrentShipsCargo == null) return false;

            DirectContainer from = pickup ? ESCache.Instance.ItemHangar : ESCache.Instance.CurrentShipsCargo;
            DirectContainer to = pickup ? ESCache.Instance.CurrentShipsCargo : ESCache.Instance.ItemHangar;

            // We moved the item

            if (to.Items.Any(i => i.GroupId == (int)Group.MiscSpecialMissionItems || i.GroupId == (int)Group.Livestock))
                return true;

            if (!directEve.NoLockedItemsOrWaitAndClearLocks("MoveItem"))
                return false;

            // Move items
            foreach (DirectItem item in from.Items.Where(i => i.GroupId == (int)Group.MiscSpecialMissionItems || i.GroupId == (int)Group.Livestock))
            {
                Log.WriteLine("Moving [" + item.TypeName + "][" + item.ItemId + "] to " + (pickup ? "cargo" : "hangar"));
                if (!to.Add(item, item.Stacksize)) return false;
            }
            _nextGenericCourierStorylineAction = DateTime.UtcNow.AddSeconds(10);
            return false;
        }

        #endregion Methods
    }
}