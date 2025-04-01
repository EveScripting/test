using EVESharpCore.Questor.States;

namespace EVESharpCore.Questor.States
{
    public static class State
    {
        #region Properties

        public static DecideWhatToDoBehaviorState CurrentDecideWhatToDoBehaviorState { get; set; }
        public static AbyssalDeadspaceBehaviorState CurrentAbyssalDeadspaceBehaviorState { get; set; }
        public static HighSecAnomalyBehaviorState CurrentHighSecAnomalyBehaviorState { get; set; }
        public static HighSecCombatSignaturesBehaviorState CurrentHighSecCombatSignaturesBehaviorState { get; set; }

        public static GatherItemsBehaviorState CurrentGatherItemsBehaviorState { get; set; }
        public static GatherShipsBehaviorState CurrentGatherShipsBehaviorState { get; set; }
        public static DeepFlowSignaturesBehaviorState CurrentDeepFlowSignaturesBehaviorState { get; set; }
        public static ExplorationNoWeaponsBehaviorState CurrentExplorationNoWeaponsBehaviorState { get; set; }
        public static WspaceSiteBehaviorState CurrentWspaceSiteBehaviorState { get; set; }
        public static SalvageGridBehaviorState CurrentSalvageGridBehaviorState { get; set; }
        public static AgentInteractionState CurrentAgentInteractionState { get; set; }
        public static ArmState CurrentArmState { get; set; }
        public static BuyLpItemsState CurrentBuyLpItemsState { get; set; }
        public static BuyNpcItemsState CurrentBuyNpcItemsState { get; set; }
        public static CleanupState CurrentCleanupState { get; set; }

        public static CombatDontMoveBehaviorState CurrentCombatDontMoveBehaviorState { get; set; } = CombatDontMoveBehaviorState.Idle;
        public static CombatMissionsBehaviorState CurrentCombatMissionBehaviorState { get; set; }
        public static FactionWarfareComplexBehaviorState CurrentFactionWarfareComplexBehaviorState { get; set; }
        public static TransportItemTypesBehaviorState CurrentTransportHangarToMarketBehaviorState { get; set; }
        public static ActionControlState CurrentCombatMissionCtrlState { get; set; }
        public static CombatState CurrentCombatState { get; set; }
        public static CourierContractState CurrentCourierContractState { get; set; }
        public static CourierMissionsBehaviorState CurrentCourierMissionBehaviorState { get; set; }
        public static DroneControllerState CurrentDroneControllerState { get; set; }
        public static HighTierLootToContainerState CurrentHighTierLootToContainerState { get; set; }
        public static HydraState CurrentHydraState { get; set; } = HydraState.Idle;

        public static WormHoleAnomalyState CurrentWormHoleAnomalyState { get; set; } = WormHoleAnomalyState.Idle;
        public static IndustryBehaviorState CurrentIndustryBehaviorState { get; set; }
        public static InstaStationDockState CurrentInstaStationDockState { get; set; }
        public static InstaStationUndockState CurrentInstaStationUndockState { get; set; }
        public static LocalWatchState CurrentLocalWatchState { get; set; }
        public static MarketAdjustBehaviorState CurrentMarketAdjustBehaviorState { get; set; }
        public static MiningMissionCtrlState CurrentMiningMissionCtrlState { get; set; }

        public static MiningBehaviorState CurrentMiningBehaviorState { get; set; }
        public static PanicState CurrentPanicState { get; set; }
        public static ProbeScanBehaviorState CurrentProbeScanBehaviorState { get; set; }
        public static AmmoManagementBehaviorState CurrentAmmoManagementBehaviorState { get; set; }
        public static InsuranceFraudBehaviorState CurrentInsuranceFraudBehaviorState { get; set; }
        public static QuestorState CurrentQuestorState { get; set; }
        public static ReShipState CurrentReShipState { get; set; }
        public static SalvageState CurrentSalvageState { get; set; }
        public static SignaturesState CurrentSignaturesState { get; set; } = SignaturesState.Idle;
        public static SkillTrainThenLogoffBehaviorState CurrentSkillTrainThenLogoffBehaviorState { get; set; }
        public static SkillQueueState CurrentSkillQueueState { get; set; }
        public static SortBlueprintsBehaviorState CurrentSortBlueprintsBehaviorState { get; set; }
        public static StatisticsState CurrentStatisticsState { get; set; }
        public static StorylineState CurrentStorylineState { get; set; }
        public static TravelerState CurrentTravelerState { get; set; }
        public static UnloadLootState CurrentUnloadLootState { get; set; }
        public static LoadItemsToHaulState CurrentLoadItemsToHaulState { get; set; }
        public static WSpaceScoutBehaviorState CurrentWSpaceScoutBehaviorState { get; set; }

        #endregion Properties
    }
}