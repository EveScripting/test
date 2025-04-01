extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using EVESharpCore.Traveller;
using System;
using System.Linq;
using SC::SharedComponents.EVE;

namespace EVESharpCore.Questor.Storylines
{
    internal enum TransactionDataDeliveryArmState
    {
        MakeShuttleActive,
        MakeTransportShipActive,
        Done
    }

    public class TransactionDataDelivery : IStoryline
    {
        #region Fields

        private DateTime _nextAction;
        private TransactionDataDeliveryState _state;
        private TransactionDataDeliveryArmState currentArmState = TransactionDataDeliveryArmState.MakeShuttleActive;

        #endregion Fields

        #region Methods

        /// <summary>
        ///     Arm does nothing but get into a (assembled) shuttle
        /// </summary>
        /// <returns></returns>
        public StorylineState Arm(Storyline storyline)
        {
            if (_nextAction > DateTime.UtcNow)
                return StorylineState.Arm;

            switch (currentArmState)
            {
                case TransactionDataDeliveryArmState.MakeShuttleActive:
                    // Are we in a shuttle?  Yes, go to the agent
                    if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Shuttle)
                        return StorylineState.GotoAgent;

                    // Open the ship hangar
                    if (ESCache.Instance.ShipHangar == null) return StorylineState.Arm;

                    //  Look for a shuttle
                    DirectItem item = ESCache.Instance.ShipHangar.Items.Find(i => i.Quantity == -1 && i.GroupId == (int)Group.Shuttle && i.GivenName != null);
                    if (item != null)
                    {
                        Log.WriteLine("Switching to shuttle");

                        _nextAction = DateTime.UtcNow.AddSeconds(10);

                        item.ActivateShip();
                        return StorylineState.Arm;
                    }

                    Log.WriteLine("No shuttle found");
                    currentArmState = TransactionDataDeliveryArmState.MakeTransportShipActive;
                    break;

                case TransactionDataDeliveryArmState.MakeTransportShipActive:
                    if (Activities.Arm.ActivateShip(Settings.Instance.StorylineTransportShipName))
                        return StorylineState.GotoAgent;

                    break;
            }

            return StorylineState.Arm;
        }

        public StorylineState BeforeGotoAgent(Storyline storyline)
        {
            return StorylineState.GotoAgent;
        }

        public void ChangeTransactionDataDeliveryState(TransactionDataDeliveryState nextState)
        {
            if (_state != nextState)
            {
                Log.WriteLine("Changing TransactionDataDeliveryState to [" + nextState + "]");
                _state = nextState;
            }
        }

        /// <summary>
        ///     Goto the pickup location
        ///     Pickup the item
        ///     Goto drop off location
        ///     Drop the item
        ///     Goto Agent
        ///     Complete mission
        /// </summary>
        /// <param name="storyline"></param>
        /// <returns></returns>
        public StorylineState ExecuteMission(Storyline storyline)
        {
            if (_nextAction > DateTime.UtcNow)
                return StorylineState.ExecuteMission;

            switch (_state)
            {
                case TransactionDataDeliveryState.GotoPickupLocation:
                    if (GotoMissionBookmark(MissionSettings.StorylineMission.Agent, "Objective (Pick Up)"))
                        ChangeTransactionDataDeliveryState(TransactionDataDeliveryState.PickupItem);
                    break;

                case TransactionDataDeliveryState.PickupItem:
                    if (MoveItem(true))
                        ChangeTransactionDataDeliveryState(TransactionDataDeliveryState.GotoDropOffLocation);
                    break;

                case TransactionDataDeliveryState.GotoDropOffLocation:
                    if (GotoMissionBookmark(MissionSettings.StorylineMission.Agent, "Objective (Drop Off)"))
                        ChangeTransactionDataDeliveryState(TransactionDataDeliveryState.DropOffItem);
                    break;

                case TransactionDataDeliveryState.DropOffItem:
                    if (MoveItem(false))
                        return StorylineState.ReturnToAgent;
                    break;
            }

            return StorylineState.ExecuteMission;
        }

        /// <summary>
        ///     There are no actions before you accept the mission
        /// </summary>
        /// <param name="storyline"></param>
        /// <returns></returns>
        public StorylineState PreAcceptMission(Storyline storyline)
        {
            ChangeTransactionDataDeliveryState(TransactionDataDeliveryState.GotoPickupLocation);
            State.CurrentTravelerState = TravelerState.Idle;
            Traveler.Destination = null;

            return StorylineState.AcceptMission;
        }

        private bool GotoMissionBookmark(DirectAgent myAgent, string title)
        {
            if (myAgent == null)
            {
                Log.WriteLine("TransactionDataDelivery: GotoMissionBookmark: myAgent == null");
                return false;
            }

            if (string.IsNullOrEmpty(title))
            {
                Log.WriteLine("TransactionDataDelivery: GotoMissionBookmark: if (string.IsNullOrEmpty(title))");
                return false;
            }

            MissionBookmarkDestination destination = Traveler.Destination as MissionBookmarkDestination;
            if (destination == null || destination.AgentId != myAgent.AgentId || !destination.Title.ToLower().StartsWith(title.ToLower()))
            {
                Log.WriteLine("TransactionDataDelivery: GotoMissionBookmark: Set Destination");
                DirectAgentMissionBookmark myBookmark = MissionSettings.GetMissionBookmark(myAgent, title);
                if (myBookmark != null)
                {
                    Traveler.Destination = new MissionBookmarkDestination(myBookmark);
                }
                else
                {
                    Log.WriteLine("TransactionDataDelivery: GotoMissionBookmark: Failed to find a bookmark named [" + title + "] for myAgent [" + myAgent.Name + "]");
                    return false;
                }
            }

            if (destination == null) return false;

            //DirectAgent storylineagent = ESCache.Instance.DirectEve.GetAgentById(MissionSettings.CurrentStorylineAgentId);
            //if (storylineagent == null)
            //{
            //    Storyline.ChangeStorylineState(StorylineState.Done);
            //    return false;
            //}

            if (!Storyline.RouteToStorylineAgentIsSafe(MissionSettings.StorylineMission.Agent.StationId, destination.SolarSystemId))
            {
                if (Storyline.HighSecChecked && !ESCache.Instance.RouteIsAllHighSecBool)
                {
                    Storyline.ChangeStorylineState(StorylineState.DeclineMission);
                    return false;
                }

                return false;
            }

            Traveler.ProcessState();

            if (State.CurrentTravelerState == TravelerState.AtDestination)
            {
                Traveler.Destination = null;
                return true;
            }

            return false;
        }

        private bool MoveItem(bool pickup)
        {
            // Open the item hangar (should still be open)
            if (ESCache.Instance.ItemHangar == null) return false;

            if (ESCache.Instance.CurrentShipsCargo == null) return false;

            // 314 == Transaction And Salary Logs (all different versions)
            const int groupId = 314;
            DirectContainer from = pickup ? ESCache.Instance.ItemHangar : ESCache.Instance.CurrentShipsCargo;
            DirectContainer to = pickup ? ESCache.Instance.CurrentShipsCargo : ESCache.Instance.ItemHangar;

            // We moved the item
            if (to.Items.Any(i => i.GroupId == groupId))
                return true;

            if (!ESCache.Instance.DirectEve.NoLockedItemsOrWaitAndClearLocks("TransactionDataDelivery: MoveItem"))
                return false;

            // Move items
            foreach (DirectItem item in from.Items.Where(i => i.GroupId == groupId))
            {
                Log.WriteLine("Moving [" + item.TypeName + "][" + item.ItemId + "] to " + (pickup ? "cargo" : "hangar"));
                if (!to.Add(item)) return false;
            }
            _nextAction = DateTime.UtcNow.AddSeconds(10);
            return false;
        }

        #endregion Methods
    }
}