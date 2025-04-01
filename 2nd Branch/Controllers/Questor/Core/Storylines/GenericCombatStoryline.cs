extern alias SC;

using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Events;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Actions;
using EVESharpCore.Questor.Activities;
using EVESharpCore.Traveller;
using EVESharpCore.Questor.States;
using SC::SharedComponents.Events;
using SC::SharedComponents.EVE.ClientSettings;

namespace EVESharpCore.Questor.Storylines
{
    public class GenericCombatStoryline : IStoryline
    {
        #region Constructors

        public GenericCombatStoryline()
        {
            _neededAmmo = new List<AmmoType>();
        }

        #endregion Constructors

        #region Properties

        public GenericCombatStorylineState CurrentGenericCombatStorylineState { get; set; }

        #endregion Properties

        #region Fields

        private readonly List<AmmoType> _neededAmmo;
        private long _agentId;

        #endregion Fields

        #region Methods

        /// <summary>
        ///     We check what ammo we need by starting a conversation with the agent and load the appropriate ammo
        /// </summary>
        /// <returns></returns>
        public StorylineState Arm(Storyline storyline)
        {
            if (_agentId != MissionSettings.StorylineMission.Agent.AgentId)
            {
                _neededAmmo.Clear();
                _agentId = MissionSettings.StorylineMission.Agent.AgentId;

                AgentInteraction.ForceAccept = true; // This makes agent interaction skip the offer-check
                State.CurrentAgentInteractionState = AgentInteractionState.Idle;
                AgentInteraction.Purpose = AgentInteractionPurpose.RemoteMissionAmmoCheck;

                State.CurrentArmState = ArmState.Idle;
                State.CurrentCombatMissionCtrlState = ActionControlState.Start;
                State.CurrentDroneControllerState = DroneControllerState.WaitingForTargets;
            }

            try
            {
                if (!Interact())
                    return StorylineState.Arm;

                if (!LoadAmmo())
                    return StorylineState.Arm;

                // We are done, reset agent id
                _agentId = 0;

                return StorylineState.BeforeGotoAgent;
            }
            catch (Exception ex)
            {
                // Something went wrong!
                Log.WriteLine("Something went wrong, blacklist this agent [" + ex.Message + "]");
                return StorylineState.BlacklistAgentForThisSession;
            }
        }

        public StorylineState BeforeGotoAgent(Storyline storyline)
        {
            CurrentGenericCombatStorylineState = GenericCombatStorylineState.WarpOutStation;
            return StorylineState.GotoAgent;
        }

        /// <summary>
        ///     Do a mini-questor here (goto mission, execute mission, goto base)
        /// </summary>
        /// <returns></returns>
        public StorylineState ExecuteMission(Storyline storyline)
        {
            switch (CurrentGenericCombatStorylineState)
            {
                case GenericCombatStorylineState.WarpOutStation:

                    DirectBookmark warpOutBookMark = null;
                    try
                    {
                        warpOutBookMark =
                            ESCache.Instance.BookmarksByLabel(Settings.Instance.UndockBookmarkPrefix ?? "")
                                .OrderByDescending(b => b.CreatedOn)
                                .FirstOrDefault(b => b.LocationId == ESCache.Instance.DirectEve.Session.SolarSystemId);
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Exception: " + ex);
                    }

                    long solarid = ESCache.Instance.DirectEve.Session.SolarSystemId ?? -1;

                    if (warpOutBookMark == null)
                    {
                        Log.WriteLine("No Bookmark");
                        CurrentGenericCombatStorylineState = GenericCombatStorylineState.GotoMission;
                        break;
                    }

                    if (warpOutBookMark.LocationId == solarid)
                    {
                        if (Traveler.Destination == null)
                        {
                            Log.WriteLine("Warp at " + warpOutBookMark.Title);
                            Traveler.Destination = new BookmarkDestination(warpOutBookMark);
                        }

                        Traveler.ProcessState();
                        if (State.CurrentTravelerState == TravelerState.AtDestination)
                        {
                            Log.WriteLine("Safe!");
                            CurrentGenericCombatStorylineState = GenericCombatStorylineState.GotoMission;
                            Traveler.Destination = null;
                        }

                        break;
                    }

                    Log.WriteLine("No Bookmark in System");
                    CurrentGenericCombatStorylineState = GenericCombatStorylineState.GotoMission;
                    break;

                case GenericCombatStorylineState.GotoMission:
                    MissionBookmarkDestination missionDestination = Traveler.Destination as MissionBookmarkDestination;
                    //
                    // if we have no destination yet... OR if missionDestination.AgentId != storyline.CurrentStorylineAgentId
                    //
                    if (missionDestination == null || missionDestination.AgentId != MissionSettings.StorylineMission.AgentId)
                    // We assume that this will always work "correctly" (tm)
                    {
                        string nameOfBookmark = "";
                        if (Settings.Instance.EveServerName == "Tranquility") nameOfBookmark = "Encounter";
                        if (Settings.Instance.EveServerName == "Serenity") nameOfBookmark = "遭遇战";
                        if (string.IsNullOrEmpty(nameOfBookmark)) nameOfBookmark = "Encounter";
                        Log.WriteLine("Setting Destination to 1st bookmark from AgentID: [" + MissionSettings.StorylineMission.AgentId + "] with [" +
                                      nameOfBookmark +
                                      "] in the title");
                        Traveler.Destination =
                            new MissionBookmarkDestination(MissionSettings.GetMissionBookmark(MissionSettings.StorylineMission.Agent, nameOfBookmark));
                    }

                    Traveler.ProcessState();
                    if (State.CurrentTravelerState == TravelerState.AtDestination)
                    {
                        CurrentGenericCombatStorylineState = GenericCombatStorylineState.ExecuteMission;

                        //State.CurrentCombatState = CombatState.CheckTargets;
                        Traveler.Destination = null;
                    }
                    break;

                case GenericCombatStorylineState.ExecuteMission:
                    ActionControl.ProcessState(MissionSettings.StorylineMission, MissionSettings.StorylineMission.Agent);

                    // If we are out of ammo, return to base, the mission will fail to complete and the bot will reload the ship
                    // and try the mission again
                    if (State.CurrentCombatState == CombatState.OutOfAmmo)
                    {
                        // Clear looted containers
                        ESCache.Instance.LootedContainers.Clear();

                        Log.WriteLine("Out of DefinedAmmoTypes! - Not enough [" + MissionSettings.CurrentDamageType + "] ammo in cargohold: MinimumCharges: [" +
                                      Combat.Combat.MinimumAmmoCharges + "]");
                        return StorylineState.ReturnToAgent;
                    }

                    if (State.CurrentCombatMissionCtrlState == ActionControlState.Done)
                    {
                        // Clear looted containers
                        ESCache.Instance.LootedContainers.Clear();
                        return StorylineState.ReturnToAgent;
                    }

                    // If in error state, just go home and stop the bot
                    if (State.CurrentCombatMissionCtrlState == ActionControlState.Error)
                    {
                        // Clear looted containers
                        ESCache.Instance.LootedContainers.Clear();

                        Log.WriteLine("Error");
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.ERROR, "Questor Error."));
                        return StorylineState.ReturnToAgent;
                    }
                    break;
            }

            return StorylineState.ExecuteMission;
        }

        /// <summary>
        ///     We have no pre-accept steps
        /// </summary>
        /// <returns></returns>
        public StorylineState PreAcceptMission(Storyline storyline)
        {
            return StorylineState.AcceptMission;
        }

        /// <summary>
        ///     Interact with the agent so we know what ammo to bring
        /// </summary>
        /// <returns>True if interact is done</returns>
        private bool Interact()
        {
            // Are we done?
            if (State.CurrentAgentInteractionState == AgentInteractionState.Done)
                return true;

            if (MissionSettings.StorylineMission.Agent == null)
                throw new Exception("Invalid agent");

            // Start the conversation
            if (State.CurrentAgentInteractionState == AgentInteractionState.Idle)
                State.CurrentAgentInteractionState = AgentInteractionState.StartConversation;

            // Interact with the agent to find out what ammo we need
            if (MissionSettings.StorylineMission != null && MissionSettings.StorylineMission.Agent != null)
            {
                AgentInteraction.ProcessState(MissionSettings.StorylineMission.Agent);
            }
            else
            {
                AgentInteraction.ChangeAgentInteractionState(AgentInteractionState.Done, null);
            }

            if (State.CurrentAgentInteractionState == AgentInteractionState.DeclineMission)
            {
                //AgentInteraction.CloseConversation();
                Log.WriteLine("Mission offer is in a Low Security System or faction blacklisted.");
                throw
                    new Exception(
                        "Low security systems"); //do storyline missions in lowsec get blacklisted by: "public StorylineState Arm(Storyline storyline)"?
            }

            return false;
        }

        /// <summary>
        ///     Load the appropriate ammo
        /// </summary>
        /// <returns></returns>
        private bool LoadAmmo()
        {
            if (State.CurrentArmState == ArmState.Done)
                return true;

            if (State.CurrentArmState == ArmState.Idle)
                Activities.Arm.ChangeArmState(ArmState.Begin, true, MissionSettings.StorylineMission.Agent);

            Activities.Arm.ProcessState(MissionSettings.StorylineMission.Agent);

            if (State.CurrentArmState == ArmState.Done)
            {
                Activities.Arm.ChangeArmState(ArmState.Idle, true, MissionSettings.StorylineMission.Agent);
                return true;
            }

            return false;
        }

        #endregion Methods
    }
}