using EVESharpCore.Controllers.Debug;
using System.Xml.Linq;

namespace EVESharpCore.Lookup
{
    public static class DebugConfig
    {
        #region Methods

        public static void LoadSettings(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            DebugAbyssalDeadspaceBehavior =
                (bool?)CharacterSettingsXml.Element("debugAbyssalDeadspaceBehavior") ??
                (bool?)CommonSettingsXml.Element("debugAbyssalDeadspaceBehavior") ?? false;
            if (DebugConfig.DebugAbyssalDeadspaceBehavior) Logging.Log.WriteLine("debugAbyssalDeadspaceBehavior is [" + DebugAbyssalDeadspaceBehavior + "]");
            DebugFourteenBattleshipSpawnRunAway =
                (bool?)CharacterSettingsXml.Element("debugFourteenBattleshipSpawnRunAway") ??
                (bool?)CommonSettingsXml.Element("debugFourteenBattleshipSpawnRunAway") ?? true;
            if (DebugConfig.DebugHighSecAnomalyBehavior) Logging.Log.WriteLine("debugHighSecAnomalyBehavior is [" + DebugHighSecAnomalyBehavior + "]");
            DebugPretendEverySpawnIsTheFourteenBattleshipSpawn =
                (bool?)CharacterSettingsXml.Element("debugPretendEverySpawnIsTheFourteenBattleshipSpawn") ??
                (bool?)CommonSettingsXml.Element("debugPretendEverySpawnIsTheFourteenBattleshipSpawn") ?? false;
            if (DebugConfig.DebugHighSecAnomalyBehavior) Logging.Log.WriteLine("debugHighSecAnomalyBehavior is [" + DebugHighSecAnomalyBehavior + "]");
            DebugHighSecAnomalyBehavior =
                (bool?)CharacterSettingsXml.Element("debugHighSecAnomalyBehavior") ??
                (bool?)CommonSettingsXml.Element("debugHighSecAnomalyBehavior") ?? false;
            if (DebugConfig.DebugHighSecAnomalyBehavior) Logging.Log.WriteLine("debugHighSecAnomalyBehavior is [" + DebugHighSecAnomalyBehavior + "]");
            DebugHighSecCombatSignaturesBehavior =
                (bool ?)CharacterSettingsXml.Element("debugHighSecCombatSignaturesBehavior") ??
                (bool?)CommonSettingsXml.Element("debugHighSecCombatSignaturesBehavior") ?? false;
            if (DebugConfig.DebugHighSecAnomalyBehavior) Logging.Log.WriteLine("debugHighSecCombatSignaturesBehavior is [" + DebugHighSecCombatSignaturesBehavior + "]");
            DebugExplorationNoWeaponsBehavior =
                (bool?)CharacterSettingsXml.Element("debugExplorationNoWeaponsBehavior") ??
                (bool?)CommonSettingsXml.Element("debugExplorationNoWeaponsBehavior") ?? false;
            if (DebugConfig.DebugExplorationNoWeaponsBehavior) Logging.Log.WriteLine("debugExplorationNoWeaponsBehavior is [" + DebugExplorationNoWeaponsBehavior + "]");
            DebugAbyssalCenterDrawDistance =
                (int?)CharacterSettingsXml.Element("debugAbyssalCenterDrawDistance") ??
                (int?)CommonSettingsXml.Element("debugAbyssalCenterDrawDistance") ?? 250;
            DebugActivateGate =
                (bool?)CharacterSettingsXml.Element("debugActivateGate") ??
                (bool?)CommonSettingsXml.Element("debugActivateGate") ?? false;
            if (DebugConfig.DebugActivateGate) Logging.Log.WriteLine("debugActivateGate is [" + DebugActivateGate + "]");
            DebugActivateBastion =
                (bool?)CharacterSettingsXml.Element("debugActivateBastion") ??
                (bool?)CommonSettingsXml.Element("debugActivateBastion") ?? false;
            if (DebugConfig.DebugActivateBastion) Logging.Log.WriteLine("debugActivateBastion is [" + DebugActivateBastion + "]");
            DebugActivateWeapons =
                (bool?)CharacterSettingsXml.Element("debugActivateWeapons") ??
                (bool?)CommonSettingsXml.Element("debugActivateWeapons") ?? false;
            if (DebugConfig.DebugActivateWeapons) Logging.Log.WriteLine("debugActivateWeapons is [" + DebugActivateWeapons + "]");
            DebugAddDronePriorityTarget =
                (bool?)CharacterSettingsXml.Element("debugAddDronePriorityTarget") ??
                (bool?)CommonSettingsXml.Element("debugAddDronePriorityTarget") ?? false;
            if (DebugConfig.DebugAddDronePriorityTarget) Logging.Log.WriteLine("debugAddDronePriorityTarget is [" + DebugAddDronePriorityTarget + "]");
            DebugAddPrimaryWeaponPriorityTarget =
                (bool?)CharacterSettingsXml.Element("debugAddPrimaryWeaponPriorityTarget") ??
                (bool?)CommonSettingsXml.Element("debugAddPrimaryWeaponPriorityTarget") ?? false;
            if (DebugConfig.DebugAddPrimaryWeaponPriorityTarget) Logging.Log.WriteLine("debugAddPrimaryWeaponPriorityTarget is [" + DebugAddPrimaryWeaponPriorityTarget + "]");
            DebugAgentInfo =
                (bool?)CharacterSettingsXml.Element("debugAgentInfo") ??
                (bool?)CommonSettingsXml.Element("debugAgentInfo") ?? false;
            DebugAgentInteraction =
                (bool?)CharacterSettingsXml.Element("debugAgentInteraction") ??
                (bool?)CommonSettingsXml.Element("debugAgentInteraction") ?? false;
            DebugAgentInteractionReplyToAgent =
                (bool?)CharacterSettingsXml.Element("debugAgentInteractionReplyToAgent") ??
                (bool?)CommonSettingsXml.Element("debugAgentInteractionReplyToAgent") ?? false;
            Alert_IsLocatedWithinFilamentCloud =
                (bool?)CharacterSettingsXml.Element("alert_IsLocatedWithinFilamentCloud") ??
                (bool?)CommonSettingsXml.Element("alert_IsLocatedWithinFilamentCloud") ?? true;
            Logging.Log.WriteLine("alert_IsLocatedWithinFilamentCloud is [" + Alert_IsLocatedWithinFilamentCloud + "]");
            Alert_IsLocatedWithinBioluminescenceCloud =
                (bool?)CharacterSettingsXml.Element("alert_IsLocatedWithinBioluminescenceCloud") ??
                (bool?)CommonSettingsXml.Element("alert_IsLocatedWithinBioluminescenceCloud") ?? true;
            Logging.Log.WriteLine("alert_IsLocatedWithinBioluminescenceCloud is [" + Alert_IsLocatedWithinBioluminescenceCloud + "]");
            Alert_IsLocatedWithinCausticCloud =
                (bool?)CharacterSettingsXml.Element("alert_IsLocatedWithinCausticCloud") ??
                (bool?)CommonSettingsXml.Element("alert_IsLocatedWithinCausticCloud") ?? true;
            Logging.Log.WriteLine("alert_IsLocatedWithinCausticCloud is [" + Alert_IsLocatedWithinCausticCloud + "]");
            Alert_IsCloseToLargeTachCloud =
                (bool?)CharacterSettingsXml.Element("alert_IsCloseToLargeTachCloud") ??
                (bool?)CommonSettingsXml.Element("alert_IsCloseToLargeTachCloud") ?? true;
            Logging.Log.WriteLine("alert_IsCloseToLargeTachCloud is [" + Alert_IsCloseToLargeTachCloud + "]");
            Alert_MWDIsWarpScrambled =
                (bool?)CharacterSettingsXml.Element("alert_MWDIsWarpScrambled") ??
                (bool?)CommonSettingsXml.Element("alert_MWDIsWarpScrambled") ?? true;
            Logging.Log.WriteLine("alert_MWDIsWarpScrambled is [" + Alert_MWDIsWarpScrambled + "]");
            Alert_ABIsWarpScrambled =
                (bool?)CharacterSettingsXml.Element("alert_ABIsWarpScrambled") ??
                (bool?)CommonSettingsXml.Element("alert_ABIsWarpScrambled") ?? true;
            Logging.Log.WriteLine("alert_ABIsWarpScrambled is [" + Alert_ABIsWarpScrambled + "]");
            Alert_IsCloseToAutomataPylon =
                (bool?)CharacterSettingsXml.Element("alert_IsCloseToAutomataPylon") ??
                (bool?)CommonSettingsXml.Element("alert_IsCloseToAutomataPylon") ?? true;
            Logging.Log.WriteLine("alert_IsCloseToAutomataPylon is [" + Alert_IsCloseToAutomataPylon + "]");
            DebugAlwaysIgnoreExtractionNodes =
                (bool ?)CharacterSettingsXml.Element("debugAlwaysIgnoreExtractionNodes") ??
                (bool?)CommonSettingsXml.Element("debugAlwaysIgnoreExtractionNodes") ?? true;
            Logging.Log.WriteLine("debugAlwaysIgnoreExtractionNodes is [" + DebugAlwaysIgnoreExtractionNodes + "]");
            DebugAmmoManagement =
                (bool?)CharacterSettingsXml.Element("debugAmmoManagement") ??
                (bool?)CommonSettingsXml.Element("debugAmmoManagement") ?? false;
            DebugArm =
                (bool?)CharacterSettingsXml.Element("debugArm") ??
                (bool?)CommonSettingsXml.Element("debugArm") ?? false;
            if (DebugConfig.DebugArm) Logging.Log.WriteLine("debugArm is [" + DebugArm + "]");
            DebugAssets =
                (bool?)CharacterSettingsXml.Element("debugAssets") ??
                (bool?)CommonSettingsXml.Element("debugAssets") ?? false;
            if (DebugConfig.DebugArm) Logging.Log.WriteLine("debugAssets is [" + DebugAssets + "]");
            DebugAnyIntersectionAtThisPosition =
                (bool?)CharacterSettingsXml.Element("debugAnyIntersectionAtThisPosition") ??
                (bool?)CommonSettingsXml.Element("debugAnyIntersectionAtThisPosition") ?? false;
            DebugBoosters =
                (bool?)CharacterSettingsXml.Element("debugBoosters") ??
                (bool?)CommonSettingsXml.Element("debugBoosters") ?? false;
            Logging.Log.WriteLine("debugBoosters is [" + DebugBoosters + "]");
            DebugBuyItems =
                (bool?)CharacterSettingsXml.Element("debugBuyItems") ??
                (bool?)CommonSettingsXml.Element("debugBuyItems") ?? false;
            DebugBuyLpItem =
                (bool?)CharacterSettingsXml.Element("debugBuyLpItem") ??
                (bool?)CommonSettingsXml.Element("debugBuyLpItem") ?? false;
            DebugCalculatePathTo =
                (bool?)CharacterSettingsXml.Element("debugCalculatePathTo") ??
                (bool?)CommonSettingsXml.Element("debugCalculatePathTo") ?? false;
            DebugCalculatePathToDrawColliders =
                (bool?)CharacterSettingsXml.Element("debugCalculatePathToDrawColliders") ??
                (bool?)CommonSettingsXml.Element("debugCalculatePathToDrawColliders") ?? false;
            DebugCalculatePathToDrawSphereAroundEntitiesWeWantToAvoid =
                (bool?)CharacterSettingsXml.Element("debugCalculatePathToDrawSphereAroundEntitiesWeWantToAvoid") ??
                (bool?)CommonSettingsXml.Element("debugCalculatePathToDrawSphereAroundEntitiesWeWantToAvoid") ?? false;
            DebugCheckSessionValid =
                (bool?)CharacterSettingsXml.Element("debugCheckSessionValid") ??
                (bool?)CommonSettingsXml.Element("debugCheckSessionValid") ?? false;
            DebugCleanup =
                (bool?)CharacterSettingsXml.Element("debugCleanup") ??
                (bool?)CommonSettingsXml.Element("debugCleanup") ?? false;
            if (DebugConfig.DebugCleanup) Logging.Log.WriteLine("debugCleanup is [" + DebugCleanup + "]");
            DebugClearPocket =
                (bool?)CharacterSettingsXml.Element("debugClearPocket") ??
                (bool?)CommonSettingsXml.Element("debugClearPocket") ?? false;
            DebugClick =
                (bool?)CharacterSettingsXml.Element("debugClick") ??
                (bool?)CommonSettingsXml.Element("debugClick") ?? false;
            DebugCombat =
                (bool?)CharacterSettingsXml.Element("debugCombat") ??
                (bool?)CommonSettingsXml.Element("debugCombat") ?? false;
            if (DebugConfig.DebugCombat) Logging.Log.WriteLine("debugCombat is [" + DebugCombat + "]");
            DebugCombatController =
                (bool?)CharacterSettingsXml.Element("debugCombatController") ??
                (bool?)CommonSettingsXml.Element("debugCombatController") ?? false;
            DebugCombatMissionCtrl =
                (bool?)CharacterSettingsXml.Element("debugCombatMissionCtrl") ??
                (bool?)CommonSettingsXml.Element("debugCombatMissionCtrl") ?? false;
            DebugCombatMissionsBehavior =
                (bool?)CharacterSettingsXml.Element("debugCombatMissionsBehavior") ??
                (bool?)CommonSettingsXml.Element("debugCombatMissionsBehavior") ?? false;
            DebugControllerManager =
                (bool?)CharacterSettingsXml.Element("debugControllerManager") ??
                (bool?)CommonSettingsXml.Element("debugControllerManager") ?? false;
            DebugCourierMissions =
                (bool?)CharacterSettingsXml.Element("debugCourierMissions") ??
                (bool?)CommonSettingsXml.Element("debugCourierMissions") ?? false;
            DebugCurrentDamageType =
                (bool?)CharacterSettingsXml.Element("debugCurrentDamageType") ??
                (bool?)CommonSettingsXml.Element("debugCurrentDamageType") ?? false;
            DebugCourierContractController =
                (bool?)CharacterSettingsXml.Element("debugCourierContractController") ??
                (bool?)CommonSettingsXml.Element("debugCourierContractController") ?? false;
            DebugDecline =
                (bool?)CharacterSettingsXml.Element("debugDecline") ??
                (bool?)CommonSettingsXml.Element("debugDecline") ?? false;
            DebugDefense =
                (bool?)CharacterSettingsXml.Element("debugDefense") ??
                (bool?)CommonSettingsXml.Element("debugDefense") ?? false;
            if (DebugConfig.DebugDefense) Logging.Log.WriteLine("debugDefense is [" + DebugDefense + "]");

            DebugDefenseSimulateLowArmor =
                (bool ?)CharacterSettingsXml.Element("debugDefenseSimulateLowArmor") ??
                (bool?)CommonSettingsXml.Element("debugDefenseSimulateLowArmor") ?? false;
            if (DebugConfig.DebugDefense) Logging.Log.WriteLine("debugDefenseSimulateLowArmor is [" + DebugDefenseSimulateLowArmor + "]");

            DebugDefenseSimulateReallyLowArmor =
                (bool?)CharacterSettingsXml.Element("debugDefenseSimulateReallyLowArmor") ??
                (bool?)CommonSettingsXml.Element("debugDefenseSimulateReallyLowArmor") ?? false;
            if (DebugConfig.DebugDefense) Logging.Log.WriteLine("debugDefenseSimulateReallyLowArmor is [" + DebugDefenseSimulateReallyLowArmor + "]");

            DebugDefenseSimulateLowShield =
                (bool?)CharacterSettingsXml.Element("debugDefenseSimulateLowShield") ??
                (bool?)CommonSettingsXml.Element("debugDefenseSimulateLowShield") ?? false;
            if (DebugConfig.DebugDefense) Logging.Log.WriteLine("debugDefenseSimulateLowShield is [" + DebugDefenseSimulateLowShield + "]");



            DebugDefensePerformance =
                (bool?)CharacterSettingsXml.Element("debugDefensePerformance") ??
                (bool?)CommonSettingsXml.Element("debugDefensePerformance") ?? false;
            DebugDirectionalScanner =
                (bool?)CharacterSettingsXml.Element("debugDirectionalScanner") ??
                (bool?)CommonSettingsXml.Element("debugDirectionalScanner") ?? false;
            DebugDefinedAmmoTypes =
                (bool?)CharacterSettingsXml.Element("debugDefinedAmmoTypes") ??
                (bool?)CommonSettingsXml.Element("debugDefinedAmmoTypes") ?? false;
            if (DebugConfig.DebugDefinedAmmoTypes) Logging.Log.WriteLine("debugDefinedAmmoTypes is [" + DebugDefinedAmmoTypes + "]");
            DebugDisableDefense =
                (bool?)CharacterSettingsXml.Element("debugDisableDefense") ??
                (bool?)CommonSettingsXml.Element("debugDisableDefense") ?? false;
            DebugDisableIgnoreBookmarkedSignatures =
                (bool?)CharacterSettingsXml.Element("debugDisableIgnoreBookmarkedSignatures") ??
                (bool?)CommonSettingsXml.Element("debugDisableIgnoreBookmarkedSignatures") ?? false;
            DebugDisableTargetCombatants =
                (bool?)CharacterSettingsXml.Element("debugDisableTargetCombatants") ??
                (bool?)CommonSettingsXml.Element("debugDisableTargetCombatants") ?? false;
            DebugDisableCleanup =
                (bool?)CharacterSettingsXml.Element("debugDisableCleanup") ??
                (bool?)CommonSettingsXml.Element("debugDisableCleanup") ?? false;
            DebugDisableAmmoManagement =
                (bool?)CharacterSettingsXml.Element("debugDisableAmmoManagement") ??
                (bool?)CommonSettingsXml.Element("debugDisableAmmoManagement") ?? false;
            DebugDisableCombat =
                (bool?)CharacterSettingsXml.Element("debugDisableCombat") ??
                (bool?)CommonSettingsXml.Element("debugDisableCombat") ?? false;
            DebugDisableDrones =
                (bool?)CharacterSettingsXml.Element("debugDisableDrones") ??
                (bool?)CommonSettingsXml.Element("debugDisableDrones") ?? false;
            DebugDisableDrugsBoosters =
                (bool?)CharacterSettingsXml.Element("debugDisableDrugsBoosters") ??
                (bool?)CommonSettingsXml.Element("debugDisableDrugsBoosters") ?? false;
            DebugDisableInJumpChecking =
                (bool?)CharacterSettingsXml.Element("debugDisableInJumpChecking") ??
                (bool?)CommonSettingsXml.Element("debugDisableInJumpChecking") ?? false;
            DebugDisableNotificationController =
                (bool?)CharacterSettingsXml.Element("debugDisableNotificationController") ??
                (bool?)CommonSettingsXml.Element("debugDisableNotificationController") ?? false;
            UseMoveToAStarWhenOrbitKeepAtRangeEtc =
                (bool?)CharacterSettingsXml.Element("useMoveToAStarWhenOrbitKeepAtRangeEtc") ??
                (bool?)CommonSettingsXml.Element("useMoveToAStarWhenOrbitKeepAtRangeEtc") ?? false;
            DebugDoneAction =
                (bool?)CharacterSettingsXml.Element("debugDoneAction") ??
                (bool?)CommonSettingsXml.Element("debugDoneAction") ?? false;
            DebugDrones =
                (bool?)CharacterSettingsXml.Element("debugDrones") ??
                (bool?)CommonSettingsXml.Element("debugDrones") ?? false;
            if (DebugConfig.DebugDrones) Logging.Log.WriteLine("debugDrones is [" + DebugDrones + "]");
            DebugDroneController =
                (bool?)CharacterSettingsXml.Element("debugDroneController") ??
                (bool?)CommonSettingsXml.Element("debugDroneController") ?? false;
            DebugEntities =
                (bool?)CharacterSettingsXml.Element("debugEntities") ??
                (bool?)CommonSettingsXml.Element("debugEntities") ?? false;
            DebugEntityCache =
                (bool?)CharacterSettingsXml.Element("debugEntityCache") ??
                (bool?)CommonSettingsXml.Element("debugEntityCache") ?? false;
            DebugFactionWarfareComplexBehavior =
                (bool?)CharacterSettingsXml.Element("debugFactionWarfareComplexBehavior") ??
                (bool?)CommonSettingsXml.Element("debugFactionWarfareComplexBehavior") ?? false;
            DebugFittingMgr =
                (bool?)CharacterSettingsXml.Element("debugFittingMgr") ??
                (bool?)CommonSettingsXml.Element("debugFittingMgr") ?? false;
            DebugFleetMgr =
                (bool?)CharacterSettingsXml.Element("debugFleetMgr") ??
                (bool?)CommonSettingsXml.Element("debugFleetMgr") ?? false;
            DebugFleetMgrPauseAndSetIsAbyssalDeadspaceTrue =
                (bool?)CharacterSettingsXml.Element("debugFleetMgrPauseAndSetIsAbyssalDeadspaceTrue") ??
                (bool?)CommonSettingsXml.Element("debugFleetMgrPauseAndSetIsAbyssalDeadspaceTrue") ?? false;
            DebugFocusFire =
                (bool?)CharacterSettingsXml.Element("debugFocusFire") ??
                (bool?)CommonSettingsXml.Element("debugFocusFire") ?? false;
            DebugFpsLimits =
                (bool?)CharacterSettingsXml.Element("debugFpsLimits") ??
                (bool?)CommonSettingsXml.Element("debugFpsLimits") ?? false;
            DebugGatherItemsBehavior =
                (bool?)CharacterSettingsXml.Element("debugGatherItemsBehavior") ??
                (bool?)CommonSettingsXml.Element("debugGatherItemsBehavior") ?? false;
            DebugGatherShipsBehavior =
                (bool?)CharacterSettingsXml.Element("debugGatherShipsBehavior") ??
                (bool?)CommonSettingsXml.Element("debugGatherShipsBehavior") ?? false;
            DebugGotobase =
                (bool?)CharacterSettingsXml.Element("debugGotobase") ??
                (bool?)CommonSettingsXml.Element("debugGotobase") ?? false;
            TryToSellAllPackagedShips =
                (bool?)CharacterSettingsXml.Element("tryToSellAllPackagedShips") ??
                (bool?)CommonSettingsXml.Element("tryToSellAllPackagedShips") ?? false;
            ProcessIndustryJobs =
                    (bool?)CharacterSettingsXml.Element("processIndustryJobs") ??
                    (bool?)CommonSettingsXml.Element("processIndustryJobs") ?? false;
            DebugHackingWindow =
                (bool?)CharacterSettingsXml.Element("debugHackingWindow") ??
                (bool?)CommonSettingsXml.Element("debugHackingWindow") ?? false;
            DebugCorrectAmmoTypeToUseByRange =
                (bool?)CharacterSettingsXml.Element("debugCorrectAmmoTypeToUseByRange") ??
                (bool?)CommonSettingsXml.Element("debugCorrectAmmoTypeToUseByRange") ?? false;
            DebugCorrectAmmoTypeInCargo =
                (bool?)CharacterSettingsXml.Element("debugCorrectAmmoTypeInCargo") ??
                (bool?)CommonSettingsXml.Element("debugCorrectAmmoTypeInCargo") ?? false;
            DebugCorrectAmmoTypeToUse =
                (bool?)CharacterSettingsXml.Element("debugCorrectAmmoTypeToUse") ??
                (bool?)CommonSettingsXml.Element("debugCorrectAmmoTypeToUse") ?? false;
            DebugEntitiesSetDisplayFalse =
                (bool?)CharacterSettingsXml.Element("debugEntitiesSetDisplayFalse") ??
                (bool?)CommonSettingsXml.Element("debugEntitiesSetDisplayFalse") ?? false;
            DebugHangars =
                (bool?)CharacterSettingsXml.Element("debugHangars") ??
                (bool?)CommonSettingsXml.Element("debugHangars") ?? false;
            DebugIgnoreBookmarkedSignatures =
                (bool?)CharacterSettingsXml.Element("debugIgnoreBookmarkedSignatures") ??
                (bool?)CommonSettingsXml.Element("debugIgnoreBookmarkedSignatures") ?? false;
            DebugIndustryBehavior =
                (bool?)CharacterSettingsXml.Element("debugIndustryBehavior") ??
                (bool?)CommonSettingsXml.Element("debugIndustryBehavior") ?? false;
            if (DebugConfig.DebugIndustryBehavior) Logging.Log.WriteLine("debugIndustryBehavior is [" + DebugIndustryBehavior + "]");
            DebugInteractWithEve =
                (bool?)CharacterSettingsXml.Element("debugInteractWithEve") ??
                (bool?)CommonSettingsXml.Element("debugInteractWithEve") ?? false;
            DebugInSpace =
                (bool?)CharacterSettingsXml.Element("debugInSpace") ??
                (bool?)CommonSettingsXml.Element("debugInSpace") ?? false;
            DebugInStation =
                (bool?)CharacterSettingsXml.Element("debugInStation") ??
                (bool?)CommonSettingsXml.Element("debugInStation") ?? false;
            DebugItemTransportController =
                (bool?)CharacterSettingsXml.Element("debugItemTransportController") ??
                (bool?)CommonSettingsXml.Element("debugItemTransportController") ?? false;
            DebugItemTransportControllerDontMoveItems =
                (bool?)CharacterSettingsXml.Element("debugItemTransportControllerDontMoveItems") ??
                (bool?)CommonSettingsXml.Element("debugItemTransportControllerDontMoveItems") ?? false;
            DebugKillTargets =
                (bool?)CharacterSettingsXml.Element("debugKillTargets") ??
                (bool?)CommonSettingsXml.Element("debugKillTargets") ?? false;
            if (DebugConfig.DebugKillTargets) Logging.Log.WriteLine("debugKillTargets is [" + DebugKillTargets + "]");
            DebugKillTargetsPerformance =
                (bool?)CharacterSettingsXml.Element("debugKillTargetsPerformance") ??
                (bool?)CommonSettingsXml.Element("debugKillTargetsPerformance") ?? false;
            if (DebugConfig.DebugKillTargetsPerformance) Logging.Log.WriteLine("debugKillTargetsPerformance is [" + DebugKillTargetsPerformance + "]");
            DebugInsuranceFraudBehavior =
                (bool?)CharacterSettingsXml.Element("debugInsuranceFraudBehavior") ??
                (bool?)CommonSettingsXml.Element("debugInsuranceFraudBehavior") ?? false;
            DebugInventoryContainers =
                (bool?)CharacterSettingsXml.Element("debugInventoryContainers") ??
                (bool?)CommonSettingsXml.Element("debugInventoryContainers") ?? false;
            DebugIsBadIdea =
                (bool?)CharacterSettingsXml.Element("debugIsBadIdea") ??
                (bool?)CommonSettingsXml.Element("debugIsBadIdea") ?? false;
            DebugIsReadyToTarget =
                (bool?)CharacterSettingsXml.Element("debugIsReadyToTarget") ??
                (bool?)CommonSettingsXml.Element("debugIsReadyToTarget") ?? false;
            DebugIsInFilamentCloud =
                (bool?)CharacterSettingsXml.Element("debugIsInFilamentCloud") ??
                (bool?)CommonSettingsXml.Element("debugIsInFilamentCloud") ?? false;
            DebugDisableIsFilamentCloud =
                (bool?)CharacterSettingsXml.Element("debugDisableIsFilamentCloud") ??
                (bool?)CommonSettingsXml.Element("debugDisableIsFilamentCloud") ?? false;
            DebugKillAction =
                (bool?)CharacterSettingsXml.Element("debugKillAction") ??
                (bool?)CommonSettingsXml.Element("debugKillAction") ?? false;
            DebugLoadScripts =
                (bool?)CharacterSettingsXml.Element("debugLoadScripts") ??
                (bool?)CommonSettingsXml.Element("debugLoadScripts") ?? false;
            DebugLoadSettings =
                (bool?)CharacterSettingsXml.Element("debugLoadSettings") ??
                (bool?)CommonSettingsXml.Element("debugLoadSettings") ?? false;
            DebugLogAndMessagesWindow =
                (bool?)CharacterSettingsXml.Element("debugLogAndMessagesWindow") ??
                (bool?)CommonSettingsXml.Element("debugLogAndMessagesWindow") ?? false;
            DebugLoginRewards =
                (bool?)CharacterSettingsXml.Element("debugLoginRewards") ??
                (bool?)CommonSettingsXml.Element("debugLoginRewards") ?? false;
            DebugLogChatMessagesToBotLogFile =
                (bool?)CharacterSettingsXml.Element("DebugLogChatMessagesToBotLogFile") ??
                (bool?)CommonSettingsXml.Element("DebugLogChatMessagesToBotLogFile") ?? true;
            DebugLogOrderOfDroneTargets =
                (bool?)CharacterSettingsXml.Element("debugLogOrderOfDroneTargets") ??
                (bool?)CommonSettingsXml.Element("debugLogOrderOfDroneTargets") ?? false;
            DebugLogOrderOfKillTargets =
                (bool?)CharacterSettingsXml.Element("debugLogOrderOfKillTargets") ??
                (bool?)CommonSettingsXml.Element("debugLogOrderOfKillTargets") ?? false;
            DebugLogOrderOfNavigateOnGridTargets =
                (bool?)CharacterSettingsXml.Element("debugLogOrderOfNavigateOnGridTargets") ??
                (bool?)CommonSettingsXml.Element("debugLogOrderOfNavigateOnGridTargets") ?? false;
            DebugLootContainer =
                (bool?)CharacterSettingsXml.Element("debugLootContainer") ??
                (bool?)CommonSettingsXml.Element("debugLootContainer") ?? false;
            DebugLootCorpHangar =
                (bool?)CharacterSettingsXml.Element("debugLootCorpHangar") ??
                (bool?)CommonSettingsXml.Element("debugLootCorpHangar") ?? false;
            DebugLootWrecks =
                (bool?)CharacterSettingsXml.Element("debugLootWrecks") ?? false;
                //(bool?)CommonSettingsXml.Element("debugLootWrecks") ?? false;
            if (DebugConfig.DebugLootWrecks) Logging.Log.WriteLine("debugLootWrecks is [" + DebugLootWrecks + "]");
            DebugMarketOrders =
                (bool?)CharacterSettingsXml.Element("debugMarketOrders") ??
                (bool?)CommonSettingsXml.Element("debugMarketOrders") ?? false;
            DebugMiningBehavior =
                (bool?)CharacterSettingsXml.Element("debugMiningBehavior") ??
                (bool?)CommonSettingsXml.Element("debugMiningBehavior") ?? false;
            DebugMobileTractor =
                (bool?)CharacterSettingsXml.Element("debugMobileTractor") ??
                (bool?)CommonSettingsXml.Element("debugMobileTractor") ?? false;
            DebugModules =
                (bool?)CharacterSettingsXml.Element("debugModules") ??
                (bool?)CommonSettingsXml.Element("debugModules") ?? false;
            DebugMoveTo =
                (bool?)CharacterSettingsXml.Element("debugMoveTo") ??
                (bool?)CommonSettingsXml.Element("debugMoveTo") ?? false;
            DebugMoveToViaAStar =
                (bool?)CharacterSettingsXml.Element("debugMoveToViaAStar") ??
                (bool?)CommonSettingsXml.Element("debugMoveToViaAStar") ?? false;
            DebugNavigateOnGrid =
                (bool?)CharacterSettingsXml.Element("debugNavigateOnGrid") ??
                (bool?)CommonSettingsXml.Element("debugNavigateOnGrid") ?? false;
            DebugNavigateOnGridImaginarySphere =
                (bool?)CharacterSettingsXml.Element("debugNavigateOnGridImaginarySphere") ??
                (bool?)CommonSettingsXml.Element("debugNavigateOnGridImaginarySphere") ?? false;
            if (DebugConfig.DebugNavigateOnGridImaginarySphere) Logging.Log.WriteLine("debugNavigateOnGridImaginarySphere is [" + DebugNavigateOnGridImaginarySphere + "]");
            DebugPretendWeAreInAFilamentCloud =
                (bool?)CharacterSettingsXml.Element("debugPretendWeAreInAFilamentCloud") ??
                (bool?)CommonSettingsXml.Element("debugPretendWeAreInAFilamentCloud") ?? false;
            if (DebugConfig.DebugPretendWeAreInAFilamentCloud) Logging.Log.WriteLine("debugPretendWeAreInAFilamentCloud is [" + DebugPretendWeAreInAFilamentCloud + "]");
            ClearDebugLines =
                (bool?)CharacterSettingsXml.Element("clearDebugLines") ??
                (bool?)CommonSettingsXml.Element("clearDebugLines") ?? true;
            Logging.Log.WriteLine("clearDebugLines is [" + ClearDebugLines + "]");
            DebugOnFrame =
                (bool?)CharacterSettingsXml.Element("debugOnFrame") ??
                (bool?)CommonSettingsXml.Element("debugOnFrame") ?? false;
            DebugOverLoadHardeners =
                (bool?)CharacterSettingsXml.Element("debugOverLoadHardeners") ??
                (bool?)CommonSettingsXml.Element("debugOverLoadHardeners") ?? false;
            DebugOverLoadWeapons =
                (bool?)CharacterSettingsXml.Element("debugOverloadWeapons") ??
                (bool?)CommonSettingsXml.Element("debugOverloadWeapons") ??
                (bool?)CharacterSettingsXml.Element("debugOverLoadWeapons") ??
                (bool?)CommonSettingsXml.Element("debugOverLoadWeapons") ?? false;
            DebugOverLoadReps =
                (bool?)CharacterSettingsXml.Element("debugOverLoadReps") ??
                (bool?)CommonSettingsXml.Element("debugOverLoadReps") ?? false;
            DebugPanic =
                (bool?)CharacterSettingsXml.Element("debugPanic") ??
                (bool?)CommonSettingsXml.Element("debugPanic") ?? false;
            DebugPickTargets =
                (bool?)CharacterSettingsXml.Element("debugPickTargets") ??
                (bool?)CommonSettingsXml.Element("debugPickTargets") ?? false;
            DebugPotentialCombatTargets =
                (bool?)CharacterSettingsXml.Element("debugPotentialCombatTargets") ??
                (bool?)CommonSettingsXml.Element("debugPotentialCombatTargets") ?? false;
            if (DebugConfig.DebugPotentialCombatTargets) Logging.Log.WriteLine("debugPotentialCombatTargets is [" + DebugPotentialCombatTargets + "]");
            DebugIsPvPAllowed =
                (bool?)CharacterSettingsXml.Element("debugIsPvPAllowed") ??
                (bool?)CommonSettingsXml.Element("debugIsPvPAllowed") ?? false;
            if (DebugConfig.DebugIsPvPAllowed) Logging.Log.WriteLine("debugIsPvPAllowed is [" + DebugIsPvPAllowed + "]");
            DebugPreferredPrimaryWeaponTarget =
                (bool?)CharacterSettingsXml.Element("debugPreferredPrimaryWeaponTarget") ??
                (bool?)CommonSettingsXml.Element("debugPreferredPrimaryWeaponTarget") ?? false;
            DebugProbeScanner =
                (bool?)CharacterSettingsXml.Element("debugProbeScanner") ??
                (bool?)CommonSettingsXml.Element("debugProbeScanner") ?? false;
            RedrawSceneColliders =
                (bool?)CharacterSettingsXml.Element("redrawSceneColliders") ??
                (bool?)CommonSettingsXml.Element("redrawSceneColliders") ?? false;
            Logging.Log.WriteLine("redrawSceneColliders is [" + RedrawSceneColliders + "]");
            DebugReduceGraphicsController =
                (bool?)CharacterSettingsXml.Element("debugReduceGraphicsController") ??
                (bool?)CommonSettingsXml.Element("debugReduceGraphicsController") ?? false;
            DebugReloadAll =
                (bool?)CharacterSettingsXml.Element("debugReloadAll") ??
                (bool?)CommonSettingsXml.Element("debugReloadAll") ?? false;
            DebugReloadorChangeAmmo =
                (bool?)CharacterSettingsXml.Element("debugReloadOrChangeAmmo") ??
                (bool?)CommonSettingsXml.Element("debugReloadOrChangeAmmo") ?? false;
            DebugRemoteReps =
                (bool?)CharacterSettingsXml.Element("debugRemoteReps") ??
                (bool?)CommonSettingsXml.Element("debugRemoteReps") ?? false;
            DebugRepairInSpace =
                (bool?)CharacterSettingsXml.Element("debugRepairInSpace") ??
                (bool?)CommonSettingsXml.Element("debugRepairInSpace") ?? false;
            DebugReShip =
                (bool?)CharacterSettingsXml.Element("debugReShip") ??
                (bool?)CommonSettingsXml.Element("debugReShip") ?? false;
            DebugDisableSalvage =
               (bool?)CharacterSettingsXml.Element("debugDisableSalvage") ??
               (bool?)CommonSettingsXml.Element("debugDisableSalvage") ?? false;
            DebugSalvage =
                (bool?)CharacterSettingsXml.Element("debugSalvage") ??
                (bool?)CommonSettingsXml.Element("debugSalvage") ?? false;
            if (DebugConfig.DebugSalvage) Logging.Log.WriteLine("debugSalvage is [" + DebugSalvage + "]");
            DebugSalvageGridBehavior =
                (bool?)CharacterSettingsXml.Element("debugSalvageGridBehavior") ??
                (bool?)CommonSettingsXml.Element("debugSalvageGridBehavior") ?? false;
            DebugSetSpeed =
                (bool?)CharacterSettingsXml.Element("debugSetSpeed") ??
                (bool?)CommonSettingsXml.Element("debugSetSpeed") ?? false;
            DebugSignaturesController =
                (bool?)CharacterSettingsXml.Element("debugSignaturesController") ??
                (bool?)CommonSettingsXml.Element("debugSignaturesController") ?? false;
            DebugSkillQueue =
                (bool?)CharacterSettingsXml.Element("debugSkillQueue") ??
                (bool?)CommonSettingsXml.Element("debugSkillQueue") ?? false;
            DebugSlaveBehavior =
                (bool?)CharacterSettingsXml.Element("debugSlaveBehavior") ??
                (bool?)CommonSettingsXml.Element("debugSlaveBehavior") ?? false;
            DebugSpeedMod =
                (bool?)CharacterSettingsXml.Element("debugSpeedMod") ??
                (bool?)CommonSettingsXml.Element("debugSpeedMod") ?? false;
            DebugSmartBombs =
                (bool?)CharacterSettingsXml.Element("debugSmartBombs") ??
                (bool?)CommonSettingsXml.Element("debugSmartBombs") ?? false;
            DebugStorylineMissions =
                (bool?)CharacterSettingsXml.Element("debugStorylineMissions") ??
                (bool?)CommonSettingsXml.Element("debugStorylineMissions") ?? false;
            DebugSubscriptionEnd =
                (bool?)CharacterSettingsXml.Element("debugSubscriptionEnd") ??
                (bool?)CommonSettingsXml.Element("debugSubscriptionEnd") ?? false;
            DebugTargetCombatants =
                (bool?)CharacterSettingsXml.Element("debugTargetCombatants") ??
                (bool?)CommonSettingsXml.Element("debugTargetCombatants") ?? false;
            if (DebugConfig.DebugTargetCombatants) Logging.Log.WriteLine("debugTargetCombatants is [" + DebugTargetCombatants + "]");
            DebugTargetCombatantsController =
                (bool?)CharacterSettingsXml.Element("debugTargetCombatantsController") ??
                (bool?)CommonSettingsXml.Element("debugTargetCombatantsController") ?? false;
            DebugTargetPainters =
                (bool?)CharacterSettingsXml.Element("debugTargetPainters") ??
                (bool?)CommonSettingsXml.Element("debugTargetPainters") ?? false;
            DebugTargetWrecks =
                (bool?)CharacterSettingsXml.Element("debugTargetWrecks") ??
                (bool?)CommonSettingsXml.Element("debugTargetWrecks") ?? false;
            DebugTraveler =
                (bool?)CharacterSettingsXml.Element("debugTraveler") ??
                (bool?)CommonSettingsXml.Element("debugTraveler") ?? false;
            if (DebugConfig.DebugTraveler) Logging.Log.WriteLine("debugTraveler is [" + DebugTraveler + "]");
            RedeemItems =
                (bool?)CharacterSettingsXml.Element("redeemItems") ??
                (bool?)CommonSettingsXml.Element("redeemItems") ?? false;
            if (DebugConfig.DebugTraveler) Logging.Log.WriteLine("redeemItems is [" + RedeemItems + "]");
            ClaimLoginRewards =
                (bool?)CharacterSettingsXml.Element("claimLoginRewards") ??
                (bool?)CommonSettingsXml.Element("claimLoginRewards") ?? false;
            if (DebugConfig.DebugTraveler) Logging.Log.WriteLine("claimLoginRewards is [" + ClaimLoginRewards + "]");
            DebugTractorBeams =
                (bool?)CharacterSettingsXml.Element("debugTractorBeams") ??
                (bool?)CommonSettingsXml.Element("debugTractorBeams") ?? false;
            DebugUndockBookmarks =
                (bool?)CharacterSettingsXml.Element("debugUndockBookmarks") ??
                (bool?)CommonSettingsXml.Element("debugUndockBookmarks") ?? false;
            DebugDockBookmarks =
                (bool?)CharacterSettingsXml.Element("debugDockBookmarks") ??
                (bool?)CommonSettingsXml.Element("debugDockBookmarks") ?? false;
            DebugUnloadLoot =
                (bool?)CharacterSettingsXml.Element("debugUnloadLoot") ??
                (bool?)CommonSettingsXml.Element("debugUnloadLoot") ?? false;
            if (DebugConfig.DebugUnloadLoot) Logging.Log.WriteLine("debugUnloadLoot is [" + DebugUnloadLoot + "]");
            DebugOverLoadModules =
                (bool?)CharacterSettingsXml.Element("debugOverLoadModules") ??
                (bool?)CommonSettingsXml.Element("debugOverLoadModules") ?? false;
            DebugUnOverLoadModules =
                (bool?)CharacterSettingsXml.Element("debugUnOverLoadModules") ??
                (bool?)CommonSettingsXml.Element("debugUnOverLoadModules") ?? false;
            DebugWarpCloakyTrick =
                (bool?)CharacterSettingsXml.Element("debugWarpCloakyTrick") ??
                (bool?)CommonSettingsXml.Element("debugWarpCloakyTrick") ?? false;
            DebugWatchForActiveWars =
                (bool?)CharacterSettingsXml.Element("debugWatchForActiveWars") ??
                (bool?)CommonSettingsXml.Element("debugWatchForActiveWars") ?? false;
            DebugWindows =
                (bool?)CharacterSettingsXml.Element("debugWindows") ??
                (bool?)CommonSettingsXml.Element("debugWindows") ?? false;
            DebugWspaceSiteBehavior =
                (bool?)CharacterSettingsXml.Element("debugWspaceSiteBehavior") ??
                (bool?)CommonSettingsXml.Element("debugWspaceSiteBehavior") ?? false;
        }

        #endregion Methods

        #region Properties

        public static bool DebugAbyssalDeadspaceBehavior { get; set; }
        public static bool DebugFourteenBattleshipSpawnRunAway { get; set; }
        public static bool DebugPretendEverySpawnIsTheFourteenBattleshipSpawn { get; set; }
        public static bool DebugActivateBastion { get; set; }
        public static int  DebugAbyssalCenterDrawDistance { get; set; }
        public static bool DebugActivateGate { get; set; }
        public static bool DebugActivateWeapons { get; set; }
        public static bool DebugAddDronePriorityTarget { get; set; }
        public static bool DebugAddPrimaryWeaponPriorityTarget { get; set; }
        public static bool DebugAgentInfo { get; set; }
        public static bool DebugAgentInteraction { get; set; }
        public static bool DebugAgentInteractionReplyToAgent { get; set; }
        public static bool Alert_IsLocatedWithinFilamentCloud { get; set; }
        public static bool Alert_IsLocatedWithinBioluminescenceCloud { get; set; }
        public static bool Alert_IsLocatedWithinCausticCloud { get; set; }
        public static bool Alert_IsCloseToLargeTachCloud { get; set; }
        public static bool Alert_IsCloseToMediumTachCloud { get; set; }
        public static bool Alert_IsCloseToSmallTachCloud { get; set; }
        public static bool Alert_IsCloseToAutomataPylon { get; set; }

        public static bool Alert_MWDIsWarpScrambled { get; set; }
        public static bool Alert_ABIsWarpScrambled { get; set; }

        public static bool DebugAlwaysIgnoreExtractionNodes { get; set; }
        public static bool DebugAmmoManagement { get; set; }
        public static bool DebugArm { get; set; }

        public static bool DebugAssets { get; set; }
        public static bool DebugAnyIntersectionAtThisPosition { get; set; }
        public static bool DebugBoosters { get; set; }
        public static bool DebugBuyLpItem { get; set; }
        public static bool DebugBuyItems { get; set; }

        public static bool DebugCalculatePathTo { get; set; }
        public static bool DebugCalculatePathToDrawColliders { get; set; }
        public static bool DebugCalculatePathToDrawSphereAroundEntitiesWeWantToAvoid { get; set; }
        public static bool DebugCheckSessionValid { get; set; }
        public static bool DebugCleanup { get; set; }
        public static bool DebugClearPocket { get; set; }
        public static bool DebugClick { get; set; }
        public static bool DebugCombat { get; set; }
        public static bool DebugCombatController { get; set; }
        public static bool DebugCombatMissionCtrl { get; set; }
        public static bool DebugCombatMissionsBehavior { get; set; }
        public static bool DebugControllerManager { get; set; }
        public static bool DebugCorrectAmmoTypeInCargo { get; set; }
        public static bool DebugCorrectAmmoTypeToUse { get; set; }
        public static bool DebugEntitiesSetDisplayFalse { get; set; }
        public static bool DebugCorrectAmmoTypeToUseByRange { get; set; }
        public static bool DebugCourierContractController { get; set; }
        public static bool DebugCourierMissions { get; set; }
        public static bool DebugCurrentDamageType { get; set; }
        public static bool DebugDecline { get; set; }
        public static bool DebugDefense { get; set; }
        public static bool DebugDefenseSimulateLowArmor { get; set; }
        public static bool DebugDefenseSimulateReallyLowArmor { get; set; }
        public static bool DebugDefenseSimulateLowShield { get; set; }

        public static bool DebugDefensePerformance { get; set; }
        public static bool DebugDirectionalScanner { get; set; }
        public static bool DebugDefinedAmmoTypes { get; set; }
        public static bool DebugDeepFlowSignaturesBehavior { get; set; }
        public static bool DebugDisableAmmoManagement { get; set; }
        public static bool DebugDisableCleanup { get; set; }
        public static bool DebugDisableCombat { get; set; }
        public static bool DebugDisableDefense { get; set; }
        public static bool DebugDisableDrones { get; set; }
        public static bool DebugDisableDrugsBoosters { get; set; }
        public static bool DebugDisableInJumpChecking { get; set; }
        public static bool DebugDisableIgnoreBookmarkedSignatures { get; set; }
        public static bool DebugDisableSalvage { get; set; }
        public static bool DebugDisableTargetCombatants { get; set; }
        public static bool DebugDockBookmarks { get; set; }
        public static bool DebugDoneAction { get; set; }
        public static bool DebugDroneController { get; set; }
        public static bool DebugDrones { get; set; }
        public static bool DebugEntities { get; set; }
        public static bool DebugEntityCache { get; set; }
        public static bool DebugFactionWarfareComplexBehavior { get; set; }
        public static bool DebugFittingMgr { get; set; }
        public static bool DebugFleetMgr { get; set; }
        public static bool DebugFleetMgrPauseAndSetIsAbyssalDeadspaceTrue { get; set; }
        public static bool DebugFocusFire { get; set; }
        public static bool DebugFpsLimits { get; set; }
        public static bool DebugGatherShipsBehavior { get; set; }
        public static bool DebugGatherItemsBehavior { get; set; }
        public static bool DebugGotobase { get; set; }
        public static bool DebugHackingWindow { get; set; }
        public static bool DebugHangars { get; set; }
        public static bool DebugHighSecAnomalyBehavior { get; set; }
        public static bool DebugHighSecCombatSignaturesBehavior { get; set; }
        public static bool DebugExplorationNoWeaponsBehavior { get; set; }
        public static bool DebugIgnoreBookmarkedSignatures { get; set; }
        public static bool DebugIndustryBehavior { get; set; }
        public static bool DebugInSpace { get; set; }
        public static bool DebugInStation { get; set; }
        public static bool DebugItemTransportController { get; set; }
        public static bool DebugItemTransportControllerDontMoveItems { get; set; }

        public static bool DebugInsuranceFraudBehavior { get; set; }
        public static bool DebugInteractWithEve { get; set; }
        public static bool DebugInventoryContainers { get; set; }
        public static bool DebugIsBadIdea { get; set; }
        public static bool DebugIsReadyToTarget { get; set; }
        public static bool DebugIsInFilamentCloud { get; set; }
        public static bool DebugDisableIsFilamentCloud { get; set; }
        public static bool DebugKillAction { get; set; }
        public static bool DebugKillTargets { get; set; }
        public static bool DebugKillTargetsPerformance { get; set; }
        public static bool DebugLoadScripts { get; set; }
        public static bool DebugLoadSettings { get; set; }
        public static bool DebugLogAndMessagesWindow { get; set; }
        public static bool DebugLoginRewards { get; set; }
        public static bool DebugLogChatMessagesToBotLogFile { get; set; }
        public static bool DebugLogOrderOfDroneTargets { get; set; }
        public static bool DebugLogOrderOfKillTargets { get; set; }
        public static bool DebugLogOrderOfNavigateOnGridTargets { get; set; }
        public static bool DebugLootContainer { get; set; }
        public static bool DebugLootCorpHangar { get; set; }
        public static bool DebugLootWrecks { get; set; }
        public static bool DebugMarketOrders { get; set; }
        public static bool DebugMiningBehavior { get; set; }
        public static bool DebugMobileTractor { get; set; }
        public static bool DebugModules { get; set; }
        public static bool DebugMoveTo { get; set; }
        public static bool DebugMoveToViaAStar { get; set; }

        public static bool DebugNavigateOnGrid { get; set; }
        public static bool DebugNavigateOnGridImaginarySphere { get; set; }

        public static bool DebugPretendWeAreInAFilamentCloud { get; set; }

        public static bool ClearDebugLines { get; set; }
        public static bool DebugOnFrame { get; set; }
        public static bool DebugOverLoadHardeners { get; set; }
        public static bool DebugOverLoadModules { get; set; }
        public static bool DebugOverLoadReps { get; set; }
        public static bool DebugOverLoadWeapons { get; set; }
        public static bool DebugPanic { get; set; }
        public static bool DebugPickTargets { get; set; }
        public static bool DebugPotentialCombatTargets { get; set; }
        public static bool DebugIsPvPAllowed { get; set; }

        public static bool DebugPreferredPrimaryWeaponTarget { get; set; }
        public static bool DebugProbeScanner { get; set; }
        public static bool RedrawSceneColliders { get; set; }
        public static bool DebugReduceGraphicsController { get; set; }
        public static bool DebugReloadAll { get; set; }
        public static bool DebugReloadorChangeAmmo { get; set; }
        public static bool DebugRemoteReps { get; set; }
        public static bool DebugRepairInSpace { get; set; }

        public static bool DebugReShip { get; set; }
        public static bool DebugSalvage { get; set; } = false;
        public static bool DebugSalvageGridBehavior { get; set; }
        public static bool DebugSetSpeed { get; set; }

        public static bool DebugSignaturesController { get; set; }
        public static bool DebugSkillQueue { get; set; }
        public static bool DebugSlaveBehavior { get; set; }
        public static bool DebugSortBlueprintsBehavior { get; set; }
        public static bool DebugSmartBombs { get; set; }
        public static bool DebugSpeedMod { get; set; }
        public static bool DebugStorylineMissions { get; set; }
        public static bool DebugSubscriptionEnd { get; set; }
        public static bool DebugTargetCombatants { get; set; }
        public static bool DebugTargetCombatantsController { get; set; }
        public static bool DebugTargetPainters { get; set; }
        public static bool DebugTargetWrecks { get; set; }
        public static bool DebugTractorBeams { get; set; }
        public static bool DebugTraveler { get; set; }
        public static bool DebugUndockBookmarks { get; set; }
        public static bool DebugUnloadLoot { get; set; }
        public static bool DebugUnOverLoadModules { get; set; }
        public static bool DebugWarpCloakyTrick { get; set; }
        public static bool DebugWatchForActiveWars { get; set; }
        public static bool DebugWindows { get; set; }
        public static bool DebugWSpaceScoutBehavior { get; set; }
        public static bool DebugWspaceSiteBehavior { get; set; }
        public static bool DebugDisableNotificationController { get; set; }

        public static bool TryToSellAllPackagedShips { get; set; }

        public static bool ProcessIndustryJobs { get; set; }
        public static bool UseMoveToAStarWhenOrbitKeepAtRangeEtc { get; set; }
        public static bool RedeemItems { get; internal set; }
        public static bool ClaimLoginRewards { get; internal set; }

        #endregion Properties
    }
}