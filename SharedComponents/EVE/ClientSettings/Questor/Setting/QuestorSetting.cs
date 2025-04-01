using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SharedComponents.EVE.ClientSettings
{
    [Serializable]
    public class QuestorSetting
    {
        public QuestorSetting()
        {
        }


        [Description("EnableStoryLines is either true or false and enables questor to run storyline missions.")]
        public bool EnableStorylines { get; set; } = true;
        public bool UseFittingManager { get; set; } = true;
        public bool RequireMissionXML { get; set; } = false;
        public bool RemoveNotCompatibleStorylines { get; set; } = true;
        public bool DeclineMissionsWithTooManyMissionCompletionErrors { get; set; } = false;
        public float MinAgentGreyListStandings { get; set; } = 0.0f;
        [Description("Missionspath is a string and is used for the path in EVESharp/QuestorMissions to point questor to where you want it to use the mission xmls from.")]
        public MissionType MissionLevel { get; set; }
        
        public bool UnloadLootAtStation { get; set; } = false;
        public int ReserveCargoCapacity { get; set; } = 0;
        public int MaximumWreckTargets { get; set; } = 0;
        public string UndockBookmarkPrefix { get; set; } = "Undock:";
        public int TractorBeamMinimumCapacitor { get; set; } = 0;
        public int SalvagerMinimumCapacitor { get; set; } = 0;
        [Browsable(false)]
        [Description("Currently not in use.")]
        public string B64Implants { get; set; }
        [Browsable(false)]
        public BindingList<ShipFitting> Shipfittings { get; set; } = new BindingList<ShipFitting>();
        public BindingList<FactionFitting> Factionfittings { get; set; } = new BindingList<FactionFitting>();
        public BindingList<MissionFitting> Missionfittings { get; set; } = new BindingList<MissionFitting>();
        public BindingList<AmmoType> AmmoTypes { get; set; } = new BindingList<AmmoType>();
        //public DamageType DefaultDamageType { get; set; }
        public int MinimumAmmoCharges { get; set; } = 8;
        public bool PreventWeaponAmmoZero { get; set; } = false;
        public int CapacitorInjectorToLoad { get; set; } = 15;
        public int SpeedNPCFrigatesShouldBeIgnoredByPrimaryWeapons { get; set; } = 300;
        public int DistanceNPCFrigatesShouldBeIgnoredByPrimaryWeapons { get; set; } = 7000;
        public bool ArmLoadCapBoosters { get; set; } = false;
        public int AncillaryShieldBoosterScript { get; set; } = 11289;
        public int CapacitorInjectorScript { get; set; } = 11289;
        public int InsideThisRangeIsHardToTrack { get; set; } = 15000;
        public int MaximumHighValueTargets { get; set; } = 2;
        public int MaximumLowValueTargets { get; set; } = 2;
        public bool SpeedTank { get; set; } = false;
        public bool LootWhileSpeedTanking { get; set; } = false;
        public int OrbitDistance { get; set; } = 5000;
        public bool OrbitStructure { get; set; } = false;
        public int MinimumPropulsionModuleDistance { get; set; } = 5000;
        public int MinimumPropulsionModuleCapacitor { get; set; } = 0;
        public int OptimalRange { get; set; } = 10000;
        public int ActivateRepairModules { get; set; } = 65;
        public int DeactivateRepairModules { get; set; } = 95;
        public int Injectcapperc { get; set; } = 60;
        public int MinimumShieldPct { get; set; } = 100;
        public int MinimumArmorPct { get; set; } = 100;
        public int MinimumCapacitorPct { get; set; } = 50;
        public int SafeShieldPct { get; set; } = 90;
        public int SafeArmorPct { get; set; } = 90;
        public int SafeCapacitorPct { get; set; } = 80;
        public bool UseDrones { get; set; } = true;
        public int DroneTypeId { get; set; } = 0;
        public int DroneControlRange { get; set; } = 0;
        public int DroneMinimumShieldPct { get; set; } = 50;
        public int DroneMinimumArmorPct { get; set; } = 50;
        public int DroneMinimumCapacitorPct { get; set; } = 0;
        public int DroneRecallShieldPct { get; set; } = 0;
        public int DroneRecallArmorPct { get; set; } = 0;
        public int DroneRecallCapacitorPct { get; set; } = 0;
        public int LongRangeDroneRecallShieldPct { get; set; } = 0;
        public int LongRangeDroneRecallArmorPct { get; set; } = 0;
        public int LongRangeDroneRecallCapacitorPct { get; set; } = 0;
        public bool DronesKillHighValueTargets { get; set; } = false;
        public BindingList<QuestorFaction> Factionblacklist { get; set; } = new BindingList<QuestorFaction>();
        public BindingList<QuestorMission> Blacklist { get; set; } = new BindingList<QuestorMission>();
        public BindingList<QuestorMission> Greylist { get; set; } = new BindingList<QuestorMission>();

        // below hidden default attributes
        [Browsable(false)]
        public int NumberOfModulesToActivateInCycle { get; set; } = 4;

        [Browsable(false)]
        public double LocalBadStandingLevelToConsiderBad { get; set; } = -0.1;
        [Browsable(false)]
        public bool LootItemRequiresTarget { get; set; } = false;
        [Browsable(true)]
        public bool KeepWeaponsGrouped { get; set; } = false;
        [Browsable(false)]
        public int DoNotSwitchTargetsIfTargetHasMoreThanThisArmorDamagePercentage { get; set; } = 60;
        [Browsable(false)]
        public int MinimumTargetValueToConsiderTargetAHighValueTarget { get; set; } = 2;
        [Browsable(false)]
        public int MaximumTargetValueToConsiderTargetALowValueTarget { get; set; } = 1;

        [Browsable(false)]
        public bool AddDampenersToDronePriorityTargetList { get; set; } = true;
        [Browsable(false)]
        public bool AddECMsToDroneTargetList { get; set; } = true;
        [Browsable(false)]
        public bool AddNeutralizersToDronePriorityTargetList { get; set; } = true;
        [Browsable(false)]
        public bool AddTargetPaintersToDronePriorityTargetList { get; set; } = true;
        [Browsable(false)]
        public bool AddTrackingDisruptorsToDronePriorityTargetList { get; set; } = true;
        [Browsable(false)]
        public bool AddWarpScramblersToDronePriorityTargetList { get; set; } = true;
        [Browsable(false)]
        public bool AddWebifiersToDronePriorityTargetList { get; set; } = true;
        [Browsable(false)]
        public int TrackingDisruptorScript { get; set; } = 29007;
        [Browsable(false)]
        public int TrackingComputerScript { get; set; } = 29001;
        [Browsable(false)]
        public int TrackingLinkScript { get; set; } = 29001;
        [Browsable(false)]
        public int SensorBoosterScript { get; set; } = 29009;
        [Browsable(false)]
        public int SensorDampenerScript { get; set; } = 29015;

        [Browsable(false)]
        public bool UseStationRepair { get; set; } = true;
    }

}