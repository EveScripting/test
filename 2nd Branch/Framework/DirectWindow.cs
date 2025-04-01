// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

extern alias SC;

using EVESharpCore.Logging;
using SC::SharedComponents.Py;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using SC::SharedComponents.Utility;
using EVESharpCore.Lookup;
using EVESharpCore.Controllers.Debug;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectWindow : DirectObject
    {
        // FROM CONST: probably better to read the values from the game
        //ID_NONE = 0
        //ID_OK = 1
        //ID_CANCEL = 2
        //ID_YES = 6
        //ID_NO = 7
        //ID_CLOSE = 8
        //ID_HELP = 9

        #region Fields

        public PyObject PyWindow;

        private static Dictionary<string, Dictionary<string, WindowType>> _windowTypeDict;

        private static WindowType[] _windowTypes = new[]
        {
            new WindowType("windowID", "AgencyWndNew", (directEve, pyWindow) => new DirectTheAgencyWindow(directEve, pyWindow)),
            new WindowType("name", "marketsellaction", (directEve, pyWindow) => new DirectMarketActionWindow(directEve, pyWindow)),
            new WindowType("name", "marketbuyaction", (directEve, pyWindow) => new DirectMarketActionWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.AgentDialogueWindow", (directEve, pyWindow) => new DirectAgentWindow(directEve, pyWindow)),
            new WindowType("__guid__", "MissionGiver", (directEve, pyWindow) => new DirectCareerAgentWindow(directEve, pyWindow)), //CareerAgent
            new WindowType("__guid__", "form.VirtualInvWindow", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.PVPOfferView", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.PVPTrade", (directEve, pyWindow) => new DirectTradeWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.SpyHangar", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "invCont.PlayerTrade", (directEve, pyWindow) => new DirectTradeWindow(directEve, pyWindow)),
            new WindowType("__guid__", "invCont.StationItems", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "invCont.StationShips", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.Inventory", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)), //default_windowID = 'Inventory'
            new WindowType("__guid__", "form.InventoryPrimary", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)), //default_windowID = 'InventoryPrimary'
            new WindowType("__guid__", "form.InventorySecondary", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.DroneView", (directEve, pyWindow) => new DirectDronesInSpaceWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.StationItems", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.StationShips", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("Caption", "StationShips", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("Caption", "Ship hangar", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.StationCorpHangars", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("Caption", "Corporation hangars", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.CorpHangarArray", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.CorpMemberHangar", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.CorpMarketHangar", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.ShipCargoView", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.ActiveShipCargo", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "invCtrl.ShipCargo", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.DockedCargoView", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.InflightCargoView", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.LootCargoView", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.StructureItemHangar", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.StructureShipHangar", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.DroneBay", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "invCtrl.ShipDroneBay", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "invCtrl.ShipFuelBay", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "invCtrl.ShipOreHold", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "invCtrl.ShipGasHold", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "invCtrl.ShipMineralHold", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "invCtrl.ShipSalvageHold", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "invCtrl.ShipShipHold", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "invCtrl.ShipAmmoHold", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "invCtrl.StationCorpDeliveries", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "invCtrl.StationItems", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "invCtrl.StationShips", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "invCtrl.POSStrontiumBay", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "invCtrl.POSFuelBay", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "invCtrl.POSJumpBridge", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "invCtrl.ItemFloatingCargo", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "invCtrl.ShipMaintenanceBay", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "invCtrl.ShipFleetHangar", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "invCtrl.ItemWreck", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.RegionalMarket", (directEve, pyWindow) => new DirectMarketWindow(directEve, pyWindow)),
            new WindowType("__guid__", "uicontrols.Window", (directEve, pyWindow) => new DirectChatWindow(directEve, pyWindow)),
            new WindowType("name", "telecom", (directEve, pyWindow) => new DirectTelecomWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.FittingMgmt", (directEve, pyWindow) => new DirectFittingManagerWindow(directEve, pyWindow)),
            //new WindowType("__guid__", "uicontrols.Window", (directEve, pyWindow) => new DirectScannerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.ReprocessingDialog", (directEve, pyWindow) => new DirectReprocessingWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.LPStore", (directEve, pyWindow) => new DirectLoyaltyPointStoreWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.RepairShopWindow", (directEve, pyWindow) => new DirectRepairShopWindow(directEve, pyWindow)), //small window with ships/items you can highlight and choose to repair
            new WindowType("__guid__", "form.Journal", (directEve, pyWindow) => new DirectJournalWindow(directEve, pyWindow)),
            new WindowType("name", "rewardsWnd", (directEve, pyWindow) => new DirectLoginRewardWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.RedeemWindowDark", (directEve, pyWindow) => new DirectRedeemItemsWindow(directEve, pyWindow)),
            //new WindowType("__guid__", "form.CraftingWindow", (directEve, pyWindow) => new DirectCraftingWindow(directEve, pyWindow)),
            //new WindowType("windowID", "CraftingWindow", (directEve, pyWindow) => new DirectCraftingWindow(directEve, pyWindow)),
            //new WindowType("name", "solar_system_map_panel", (directEve, pyWindow) => new DirectMapViewWindow(directEve, pyWindow)), //do not use: use DirectDirectionalScannerWindow and DirectProbeScannerWindow instead
            new WindowType("name", "directionalScannerWindow", (directEve, pyWindow) => new DirectDirectionalScannerWindow(directEve, pyWindow)),
            //new WindowType("name", "solar_system_map_panel", (directEve, pyWindow) => new DirectDirectionalScannerWindow(directEve, pyWindow)),//fallback for the case where the window is docked with the solarsystem mapview - crashes?
            //new WindowType("name", "probeScannerWindow", (directEve, pyWindow) => new DirectProbeScannerWindow(directEve, pyWindow)),
            new WindowType("name", "solar_system_map_panel", (directEve, pyWindow) => new DirectMapViewWindow(directEve, pyWindow)),//fallback for the case where the window is docked with the solarsystem mapview - crashes?
            //new WindowType("name", "logger", (directEve, pyWindow) => new DirectLogAndMessagesWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.KeyActivationWindow", (directEve, pyWindow) => new DirectKeyActivationWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.AbyssActivationWindow", (directEve, pyWindow) => new DirectAbyssActivationWindow(directEve, pyWindow)),
            new WindowType("name", "SellItemsWindow", (directEve, pyWindow) => new DirectMultiSellWindow(directEve, pyWindow)),
            new WindowType("__guid__", "AgencyWndNew", (directEve, pyWindow) => new DirectTheAgencyWindow(directEve, pyWindow)),
            new WindowType("__guid__", "FittingWindow", (directEve, pyWindow) => new DirectFittingWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.Logger", (directEve, pyWindow) => new DirectLogAndMessagesWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.LobbyWnd", (directEve, pyWindow) => new DirectLobbyWindow(directEve, pyWindow)),
            new WindowType("windowID", "overview", (directEve, pyWindow) => new DirectOverviewWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.ActiveItem", (directEve, pyWindow) => new DirectSelectedItemWindow(directEve, pyWindow)), // Used by old UI
            new WindowType("windowID", "selecteditemview", (directEve, pyWindow) => new DirectSelectedItemWindow(directEve, pyWindow)),// Used by PhotonUI
            new WindowType("_caption", "Drone Bay", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("name", "walletWindow", (directEve, pyWindow) => new DirectWalletWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.Industry", (directEve, pyWindow) => new DirectIndustryWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.FleetWindow", (directEve, pyWindow) => new DirectFleetWindow(directEve, pyWindow)),
            new WindowType("default_windowID", "XmppChat", (directEve, pyWindow) => new DirectChatWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.HackingWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'HackingWindow'
            new WindowType("name", "marketbuyaction", (directEve, pyWindow) => new DirectMarketActionWindow(directEve, pyWindow)), //broken?
            //new WindowType("__guid__", "form.Telecom", (directEve, pyWindow) => new DirectTelecomWindow(directEve, pyWindow)),
            //new WindowType("__guid__", "form.Industry", (directEve, pyWindow) => new DirectIndustryWindow(directEve, pyWindow)), - broken here
            //new WindowType("default_windowID", "AchievementAuraWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("__guid__", "form.AchievementTreeWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("__guid__", "windowOfOpportunity", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_caption = 'Window of Opportunity'
            //new WindowType("__guid__", "fpsMonitor2", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_caption = 'FPS Monitor'
            //new WindowType("__guid__", "missingSkillbooksWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_caption = 'FPS Monitor'
            //new WindowType("__guid__", "InstructionsConversationWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'TaskConversationWindow'
            //new WindowType("__guid__", "form.CrateRedeemedWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'CrateRedeemedWindow'
            //new WindowType("__guid__", "form.CrateWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'CrateWindow'
            //new WindowType("__guid__", "form.ActiveItem", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'selecteditemview'
            //new WindowType("__guid__", "form.CapitalNav", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'capitalnav'
            //new WindowType("__guid__", "form.DroneView", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'droneview'
            //new WindowType("__guid__", "form.InfrastructureHubWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'infrastructhubman'
            //new WindowType("__guid__", "form.ItemTraderWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'itemTrader'
            //new WindowType("__guid__", "form.MoonMining", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'moon'
            //new WindowType("default_windowID", "NotifySettingsWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("__guid__", "form.OrbitalConfigurationWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'configureOrbital'
            //new WindowType("__guid__", "form.OverView", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'overview'
            //new WindowType("__guid__", "form.OverviewSettings", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'overviewsettings'
            //new WindowType("default_windowID", "probeScannerWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("default_windowID", "directionalScannerWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("default_windowID", "MoonScanner", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'overviewsettings'
            //new WindowType("__guid__", "form.ScannerFilterEditor", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'probeScannerFilterEditor'
            //new WindowType("__guid__", "form.ShipScan", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'shipscan'
            //new WindowType("__guid__", "form.CargoScan", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'cargoScan'
            //new WindowType("__guid__", "form.SurveyScanView", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'SurveyScanView'
            //new WindowType("default_windowID", "claimSurveyRewardsWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'SurveyScanView'
            //new WindowType("__guid__", "form.MapCmdWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'MapCmdWindow'
            //new WindowType("__guid__", "SchedulingWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'mooonminingScheduling'
            //new WindowType("default_windowID", "PodGuideWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("__guid__", "form.ActivateMultiTrainingWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'ActivateMultiTrainingWindow'
            //new WindowType("__guid__", "ActivityTrackerDockablePanel", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'ActivityTracker'
            //new WindowType("__guid__", "form.AddressBook", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'addressbook'
            //new WindowType("default_windowID", "joinFleetConfirmationWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("__guid__", "form.AssetSafetyDeliverWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'AssetSafetyDeliverWindow'
            //new WindowType("__guid__", "form.AssetsWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'assets'
            //new WindowType("__guid__", "form.AuditLogSecureContainerLogViewer", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'alsclogviewer'
            //new WindowType("__guid__", "form.AutopilotSettings", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'AutopilotSettings'
            //new WindowType("__guid__", "form.BookmarkContainerWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'bookmarkFolderWindow'
            //new WindowType("__guid__", "form.BookmarkLocationWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'bookmarkLocationWindow'
            //new WindowType("__guid__", "BookmarkSubfolderWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'bookmarkSubfolderWindow'
            //new WindowType("__guid__", "form.LinkedBookmarkFolderWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'LinkedBookmarkFolderWindow'
            //new WindowType("default_windowID", "standaloneBookmarkWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("__guid__", "form.BountyWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'bounties'
            //new WindowType("__guid__", "form.BountyPicker", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'BountyPicker'
            //new WindowType("default_windowID", "careerPortal", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("default_windowID", "PortalInOtherWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'BountyPicker'
            //new WindowType("default_windowID", "localChatWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("default_windowID", "CloneUpgradeWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("default_windowID", "LapseNotifyWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("default_windowID", "MultiLoginBlockedWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("default_windowID", "ChatFilterSettings", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("__guid__", "form.CtrlTabWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'CtrlTabWindow'
            //new WindowType("__guid__", "form.LobbyWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'lobbyWnd'
            //new WindowType("__guid__", "form.CraftingWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'CraftingWindow'
            //new WindowType("__guid__", "form.eveCalendarWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'calendar'
            //new WindowType("__guid__", "form.ExportFittingsWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'ExportFittingsWindow'
            //new WindowType("__guid__", "form.ImportFittingsWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'ImportFittingsWindow'
            //new WindowType("__guid__", "form.ImportOverviewWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'ImportOverviewWindow'
            //new WindowType("__guid__", "form.ImportLegacyFittingsWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'ImportLegacyFittingsWindow'
            //new WindowType("__guid__", "form.ExportOverviewWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'ExportOverviewWindow'
            //new WindowType("__guid__", "form.FWInfrastructureHub", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'FWInfrastructureHub'
            //new WindowType("__guid__", "form.MultiFitWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'multiFitWnd'
            //new WindowType("__guid__", "form.ViewFitting", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'ViewFitting'
            //new WindowType("__guid__", "form.FittingWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'fittingWnd'
            //new WindowType("__guid__", "BuyAllMessageBox", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'buyAllMessageBox'
            //new WindowType("__guid__", "form.WatchListPanel", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'watchlistpanel'
            //new WindowType("__guid__", "form.BroadcastSettings", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'broadcastsettings'
            //new WindowType("default_windowID", "FleetFormationWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("__guid__", "form.RegisterFleetWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'RegisterFleetWindow'
            //new WindowType("__guid__", "form.FleetWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'fleetwindow'
            //new WindowType("__guid__", "form.FleetJoinRequestWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'FleetJoinRequestWindow'
            //new WindowType("__guid__", "form.FleetComposition", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'FleetComposition'
            //new WindowType("default_windowID", "StoreFleetSetupWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("default_windowID", "StoredFleetSetupListWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("__guid__", "form.infowindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'infowindow'
            //new WindowType("__guid__", "form.EntityWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'EntityWindow'
            //new WindowType("__guid__", "ContainerContentWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'containerContentWindow'
            //new WindowType("default_windowID", "FilterCreationWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("__guid__", "form.AssetSafetyContainer", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'AssetSafetyContainer'
            //new WindowType("__guid__", "form.AssetSafetyDeliveries", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'AssetSafetyDeliveries'
            //new WindowType("__guid__", "form.KillReportWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'KillReportWnd'
            //new WindowType("__guid__", "form.SellKillRightWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'SellKillRightWnd'
            //new WindowType("__guid__", "LedgerWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'ledger'
            //new WindowType("__guid__", "DailyLoginRewardsWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = LOGIN_REWARDS_WINDOW_ID
            //new WindowType("__guid__", "LogOffWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'message'
            //new WindowType("__guid__", "form.MapBrowserWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'mapbrowser'
            //new WindowType("__guid__", "form.MapsPalette", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'mapspalette'
            //new WindowType("__guid__", "form.BuyItems", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'MultiBuy'
            //new WindowType("default_windowID", "SellBuyItemsWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("__guid__", "form.MarketActionWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'marketbuyaction'
            //new WindowType("__guid__", "form.RegionalMarket", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'market'
            //new WindowType("default_windowID", "marketOrders", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("__guid__", "form.SellItems", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'SellItemsWindow'
            //new WindowType("__guid__", "form.MedalRibbonPickerWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'MedalRibbonPickerWindow'
            //new WindowType("__guid__", "form.MessageBox", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'message'
            //new WindowType("default_windowID", "moveMeWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("__guid__", "form.Calculator", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'calculator'
            //new WindowType("__guid__", "form.CharacterSheet", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'charactersheet'
            //new WindowType("__guid__", "form.TypeCompare", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'typecompare'
            //new WindowType("__guid__", "form.ContractDetailsWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'contractdetails'
            //new WindowType("__guid__", "form.ContractsWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'contracts'
            //new WindowType("__guid__", "form.CreateContract", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'createcontract'
            //new WindowType("__guid__", "form.IgnoreListWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'contractignorelist'
            //new WindowType("default_windowID", "multiContractsWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("__guid__", "form.Corporation", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'corporation'
            //new WindowType("__guid__", "form.InviteToCorpWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'InviteToCorpWnd'
            //new WindowType("__guid__", "form.AllyWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'AllyWnd'
            //new WindowType("__guid__", "form.CorporationOrAlliancePickerDailog", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'CorporationOrAlliancePickerDailog'
            //new WindowType("__guid__", "warDeclareWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'warDeclareWnd'
            //new WindowType("__guid__", "form.WarReportWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'WarReportWnd'
            //new WindowType("__guid__", "BaseNegotiationWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'BaseNegotiationWnd'
            //new WindowType("__guid__", "form.WarSurrenderWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'WarSurrenderWnd'
            //new WindowType("__guid__", "form.WarAssistanceOfferWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'warAssistanceOfferWnd'
            //new WindowType("default_windowID", "TransferMoney", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("default_windowID", "walletWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("default_windowID", "NewFeatureNotifyWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("__guid__", "form.DepletionManager", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'depletionManager'
            //new WindowType("__guid__", "form.ExpeditedTransferManagementWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'createTransfer'
            //new WindowType("__guid__", "form.PlanetaryImportExportUI", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'PlanetaryImportExportUI'
            //new WindowType("__guid__", "form.OrbitalMaterialUI", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'OrbitalMaterialUI'
            //new WindowType("default_windowID", "PlanetPinWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("__guid__", "form.PlanetWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'planetWindow'
            //new WindowType("__guid__", "form.PlanetSurvey", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'PlanetSurvey'
            //new WindowType("default_windowID", "recommendationWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("__guid__", "form.RedeemWindowDark", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = REDEEM_WINDOW_ID
            //new WindowType("__guid__", "form.ShipConfig", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'shipconfig'
            //new WindowType("__guid__", "form.assetsSelectionWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'assetsSelectionWindow'
            //new WindowType("__guid__", "form.signupCharacterWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'signupCharacterWindow'
            //new WindowType("__guid__", "form.saleCharacterWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'saleCharacterWindow'
            //new WindowType("default_windowID", "SkillPlanner", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("default_windowID", "skillImportStatusWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("default_windowID", "ApplySkillPointsWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("__guid__", "form.SkillExtractorWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'SkillExtractorWindow'
            //new WindowType("default_windowID", "SkillInjectorWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("default_windowID", "AgentTransmissionWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("default_windowID", "MissionDetails", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("__guid__", "form.MilitiaWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'factionalWarfare'
            //new WindowType("__guid__", "form.InsuranceWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'insurance'
            //new WindowType("default_windowID", "InsuranceTermsWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'SkillExtractorWindow'
            //new WindowType("default_windowID", "StructureDeploymentWndID", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("__guid__", "dropboxWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'dropboxWnd'
            //new WindowType("default_windowID", "structureHackingResultWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("default_windowID", "AutoBotWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("default_windowID", "compression_window", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("default_windowID", "decompression_window", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("default_windowID", "RewardFanfare", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("default_windowID", "NotificationSettings", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("__guid__", "PVPFilamentActivationWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'PVPFilamentActivationWindow'
            //new WindowType("__guid__", "PVPFilamentEventWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'PVPFilamentEventWindow'
            //new WindowType("__guid__", "RaffleWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'RaffleWindow'
            //new WindowType("__guid__", "RandomJumpActivationWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'RandomJumpActivationWindow'
            //new WindowType("__guid__", "ReprocessingWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'reprocessingWindow'
            //new WindowType("default_windowID", "IncomingTransmissionWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("__guid__", "VoidSpaceActivationWindow", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'VoidSpaceActivationWindow'
            //new WindowType("__guid__", "PCOwnerPickerDialog", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'PCOwnerPickerDialog'
            //new WindowType("default_windowID", "ChannelSettingsDlg", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)),
            //new WindowType("__guid__", "ReprocessingWnd", (directEve, pyWindow) => new DirectHackingWindow(directEve, pyWindow)), //default_windowID = 'reprocessingWindow'

            //new WindowType("__guid__", "form.CharacterSheet", (directEve, pyWindow) => new DirectCharacterSheetWindow(directEve, pyWindow)),
            //new WindowType("windowID", "industryWnd", (directEve, pyWindow) => new DirectIndustryWindow(directEve, pyWindow)),
        };

        private string html;

        #endregion Fields

        public string MessageKey { get; private set; }

        #region Constructors

        internal DirectWindow(DirectEve directEve, PyObject pyWindow) : base(directEve)
        {
            PyWindow = pyWindow;
            WindowId = (string) pyWindow.Attribute("windowID") ?? "";
            //Type = (string) pyWindow.Attribute("__guid__");
            Guid = (string)pyWindow.Attribute("__guid__") ?? "";
            Name = (string) pyWindow.Attribute("name") ?? "";
            IsKillable = (bool) pyWindow.Attribute("killable");
            IsDialog = (bool) pyWindow.Attribute("isDialog");
            IsModal = (bool) pyWindow.Attribute("isModal");
            Caption = (string) pyWindow.Call("GetCaption") ?? "";
            ViewMode = (string) pyWindow.Attribute("viewMode" ?? "");
            MessageKey = (string)pyWindow.Attribute("msgKey" ?? "");
        }

        #endregion Constructors

        #region Enums

        public enum ModalResultType
        {
            NONE,
            OK,
            CANCEL,
            YES,
            NO,
            CLOSE,
            HELP
        }

        #endregion Enums

        #region Properties

        private static void SetupDict()
        {
            if (_windowTypeDict == null)
            {
                _windowTypeDict = new Dictionary<string, Dictionary<string, WindowType>>();

                foreach (var k in _windowTypes)
                {
                    if (!_windowTypeDict.ContainsKey(k.Attribute))
                    {
                        var d = new Dictionary<string, WindowType>();
                        d.Add(k.Value, k);
                        _windowTypeDict.Add(k.Attribute, d);
                    }
                    else
                    {
                        var d = _windowTypeDict[k.Attribute];
                        d.Add(k.Value, k);
                    }
                }
            }
        }

        public string Caption { get; internal set; }


        public bool IsWindowActive
        {
            get
            {
                var reg = DirectEve.PySharp.Import("carbonui")["uicore"]["uicore"]["registry"];
                var active = reg.Call("GetActive");
                return active.PyRefPtr == this.PyWindow.PyRefPtr;
            }
        }


        /// <summary>
        ///     Don't call this, use DirectWindowManager
        /// </summary>
        public void SetActive()
        {
            if (IsWindowActive)
                return;

            var reg = DirectEve.PySharp.Import("carbonui")["uicore"]["uicore"]["registry"];

            var focus = reg["SetFocus"];
            if (focus.IsValid)
            {
                //var active = reg.Call("GetActive");
                var stack = this.PyWindow["sr"]["stack"];
                if (stack.IsValid)
                {
                    //var active = stack.Call("GetActiveWindow");
                    var showWnd = stack["ShowWnd"];
                    DirectEve.ThreadedCall(showWnd, this.PyWindow);
                }
                DirectEve.ThreadedCall(focus, this.PyWindow);
            }
        }
        public string Html
        {
            get
            {
                if (!string.IsNullOrEmpty(html))
                    return html;

                try
                {
                    var paragraphs = PyWindow.Attribute("edit").Attribute("sr").Attribute("paragraphs").ToList();
                    html = paragraphs.Aggregate("", (current, paragraph) => current + (string)paragraph.Attribute("text"));
                    if (String.IsNullOrEmpty(html))
                        html = (string)PyWindow.Attribute("edit").Attribute("sr").Attribute("currentTXT");
                    if (String.IsNullOrEmpty(html))
                    {
                        paragraphs = PyWindow.Attribute("sr").Attribute("messageArea").Attribute("sr").Attribute("paragraphs").ToList();
                        html = paragraphs.Aggregate("", (current, paragraph) => current + (string)paragraph.Attribute("text"));
                    }

                    if (String.IsNullOrEmpty(html))
                    {
                        html = PyWindow["_message_label"]["text"].ToUnicodeString();
                    }

                    if (String.IsNullOrEmpty(html))
                    {
                        string[] textChildPath = { "content", "main", "form", "textField", "text" };
                        var textChild = FindChildWithPath(PyWindow, textChildPath);


                        if (!textChild.IsValid)
                        {
                            if (DebugConfig.DebugAgentInteraction) DirectEve.Log("Textchild not valid!");
                            return string.Empty;
                        }

                        if (textChild["text"].IsValid)
                        {
                            html = textChild["text"].ToUnicodeString();

                        }
                    }
                }
                catch (Exception ex)
                {
                    DirectEve.Log("Exception in DirectWindow.Html: " + ex.Message);
                    return string.Empty;
                }

                if (html == null)
                    html = string.Empty;

                return html;
            }
        }

        public bool IsDialog { get; internal set; }
        public bool IsKillable { get; internal set; }
        public bool IsModal { get; internal set; }
        public string Name { get; internal set; }

        public string WindowId { get; internal set; }

        public bool Ready
        {
            get
            {
                var edit = PyWindow.Attribute("edit");
                if (edit.IsValid && edit.Attribute("_loading").ToBool())
                    return false;

                if (PyWindow.Attribute("startingup").ToBool())
                    return false;

                return true;
            }
        }

        public string Guid { get; internal set; }
        public string ViewMode { get; internal set; }

        #endregion Properties

        #region Methods

        /// <summary>
        ///     Answers a modal window
        /// </summary>
        /// <param name="button">a string indicating which button to press. Possible values are: Yes, No, Ok, Cancel, Suppress</param>
        /// <returns>true if successful</returns>
        public bool SetModalResult(string button)
        {
            if (Time.Instance.LastWindowInteraction.AddSeconds(2) > DateTime.UtcNow)
                return false;

            //string[] buttonPath = { "__maincontainer", "bottom", "btnsmainparent", "btns", "Yes_Btn" };

            if (!DirectEve.Session.IsReady)
                return false;

            if (!IsModal && Name.ToLower() != "modal".ToLower())
                return false;

            ModalResultType mr = ModalResultType.YES;

            switch (button)
            {
                case "Yes":
                    break;

                case "No":
                    mr = ModalResultType.NO;
                    break;

                case "OK":
                case "Ok":
                    if (Name == "Set Quantity")
                    {
                        if (PyWindow != null)
                        {
                            PyWindow.Call("Confirm", 12345);
                            return true;
                        }

                        return false;
                    }

                    mr = ModalResultType.OK;
                    break;

                case "Cancel":
                    mr = ModalResultType.CANCEL;
                    break;

                default:
                    return false;
            }

            //PyObject btn = FindChildWithPath(PyWindow, buttonPath);
            //if (btn != null)
            //    return DirectEve.ThreadedCall(btn.Attribute("OnClick"));
            if (SetModalResult(mr))
            {
                Time.Instance.LastWindowInteraction = DateTime.UtcNow;
                return true;
            }

            return false;
        }

        private DateTime _modalNextOperation;

        private static Random _random = new Random();

        public bool AnswerModal(string button)
        {
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects 0,1,2,3
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[0]
            //window_controls_cont
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[1]
            //Resizer
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2]
            //content
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[0]
            //__loadingParent
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[1]
            //headerParent
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2]
            //main
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0]
            //bottom
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0]
            //ButtonGroup
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects._childrenObjects[0]
            //btns
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects._childrenObjects[0].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects._childrenObjects[0].children._childrenObjects[0]
            //ButtonWrapper
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects._childrenObjects[0].children._childrenObjects[0].children._childrenObjects[0]
            //yes_digalog_button
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects._childrenObjects[0].children._childrenObjects[1]
            //ButtonWrapper
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects._childrenObjects[0].children._childrenObjects[1].children._childrenObjects[0]
            //no_dialog_button
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0].children._childrenObjects[0].children._childrenObjects._childrenObjects[0].children._childrenObjects[2]
            //OverflowButton
            //
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1]
            //topParent
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects 0,1
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0]
            //ContainerAutoSize
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0]
            //maincon
            //
            //
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[1]
            //ContainerAutoSize
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1].children._childrenObjects[1].children._childrenObjects[0]
            //EveCaptionLarge
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[2]
            //scrollContainer
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[0]
            //Scrollbar
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[1]
            //Scrollbar
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[2].children._childrenObjects[2]
            //clipCont
            //carbonui.uicore.uicore.registry.windows[8].children._childrenObjects[3]
            //underlay
            //carbonui.uicore.uicore.registry.windows[8].content.children._childrenObjects 0,1,2
            //carbonui.uicore.uicore.registry.windows[8].content.children._childrenObjects[0]
            //bottom
            //carbonui.uicore.uicore.registry.windows[8].content.children._childrenObjects[1]
            //topParent
            //carbonui.uicore.uicore.registry.windows[8].content.children._childrenObjects[2]
            //scrollContainer


            //carbonui.uicore.uicore.registry.windows[8]
            //.children._childrenObjects[2] - content
            //.children._childrenObjects[2] - main
            //.children._childrenObjects[0] - bottom
            //.children._childrenObjects[0] - ButtonGroup
            //.children._childrenObjects._childrenObjects[0] - btns
            //.children._childrenObjects[0] - ButtonWrapper
            //.children._childrenObjects[0] - yes_dialog_button
            //yes_dialog_button

            //carbonui.uicore.uicore.registry.windows[8]
            //.children._childrenObjects[2] - content
            //.children._childrenObjects[2] - main
            //.children._childrenObjects[0] - bottom
            //.children._childrenObjects[0] - ButtonGroup
            //.children._childrenObjects._childrenObjects[0] - btns
            //.children._childrenObjects[1] - ButtonWrapper
            //.children._childrenObjects[0] - no_dialog_button
            //no_dialog_button

            try
            {
                string[] buttonPath = { "content", "main", "bottom", "ButtonGroup" };
                string funcName = "OnClick";
                var btnName = "";
                switch (button.ToLower())
                {
                    case "yes":
                        btnName = "yes_dialog_button";
                        break;
                    case "no":
                        btnName = "no_dialog_button";
                        break;
                    case "ok":
                        btnName = "ok_dialog_button";
                        break;
                    case "cancel":
                        btnName = "cancel_dialog_button";
                        break;
                    //case "Suppress":
                    //    string[] suppress = { "content", "main", "suppressContainer", "suppress" };
                    //    buttonPath = suppress;
                    //    break;
                    default:
                        Log.WriteLine("Unknown button name: " + button);
                        return false;
                }

                PyObject buttonGroup = FindChildWithPath(PyWindow, buttonPath);
                PyObject btn = null;

                if (!buttonGroup.IsValid)
                {
                    Log.WriteLine("Buttongroup not valid?"); //never gets used?
                    buttonPath = new string[] { "content", "main", "bottom", "ButtonGroup", "btns" };
                    buttonGroup = FindChildWithPath(PyWindow, buttonPath);
                    if (!buttonGroup.IsValid)
                    {
                        Log.WriteLine("Buttongroup still not valid?!!");
                        return false;
                    }

                    btn = buttonGroup["buttons"].ToList().FirstOrDefault(b => b.Attribute("name").ToUnicodeString() == btnName);
                }
                else btn = buttonGroup["buttons"].ToList().FirstOrDefault(b => b.Attribute("name").ToUnicodeString() == btnName);


                if (btn == null || !btn.IsValid)
                {
                    Log.WriteLine("Modal button not found! (FindChildWithPath)");
                    return false;
                }

                //If there is a suppress checkbox and it is not checked, ensure it is being checked
                var checkBox = PyWindow["sr"]["suppCheckbox"];
                if (checkBox.IsValid && !Html.Contains("Are you sure you would like to decline this mission?"))
                {
                    if (checkBox["_checked"].ToBool() == false)
                    {
                        Log.WriteLine("There is a suppress checkbox available, checking it.");
                        _modalNextOperation = DateTime.UtcNow.AddMilliseconds(_random.Next(1200, 2500));
                        DirectEve.ThreadedCall(checkBox["ToggleState"]);
                        return false;
                    }
                }

                if (_modalNextOperation > DateTime.UtcNow)
                {
                    Log.WriteLine($"Waiting for next modal operation, _modalNextOperation [{_modalNextOperation}]");
                    return false;
                }

                if (DirectEve.Interval(400, 700))
                {
                    if (DirectEve.ThreadedCall(btn.Attribute(funcName)))
                    {
                        Log.WriteLine(Name + " Modal button found!?! Clicking it.");
                        return true;
                    }

                    Log.WriteLine(Name + " Modal button found!?! Failed!");
                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception in DirectWindow.AnswerModal: " + ex.Message);
                return false;
            }
        }

        public virtual bool Minimize()
        {
            return DirectEve.ThreadedCall(PyWindow.Attribute("Minimize"));
        }

        public virtual bool Maximize()
        {
            return DirectEve.ThreadedCall(PyWindow.Attribute("Maximize"));
        }

        /// <summary>
        ///     Closes the window
        ///     Container windows are a special case and can't be closed as we are opening them automatically while
        ///     retrieving the corresponding container. Use forceCloseContainerWnd with caution!
        /// </summary>
        /// <param name="forceCloseContainerWnd"></param>
        /// <returns></returns>
        public virtual bool Close(bool forceCloseContainerWnd = false)
        {
            if (Time.Instance.LastWindowInteraction.AddSeconds(2) > DateTime.UtcNow)
                return false;

            if (!DirectEve.Session.IsReady)
                return false;

            if (!DirectEve.Interval(3000, 4000, Guid))
                return false;

            if (!Name.Equals("NewFeatureNotifyWnd") && !Name.Equals("LoginRewardWindow") && !IsKillable)
                return false;

            if (!forceCloseContainerWnd && this.GetType() == typeof(DirectContainerWindow))
            {
                DirectEve.Log($"Container windows can't be closed.");
                return false;
            }

            if (DirectEve.ThreadedCall(PyWindow.Attribute("CloseByUser")))
            {
                Time.Instance.LastWindowInteraction = DateTime.UtcNow;
                return true;
            }

            return false;
        }

        public int GetModalResult(ModalResultType mr)
        {
            int modalResult = 0;
            switch (mr)
            {
                case ModalResultType.NONE:
                    modalResult = 0;
                    break;

                case ModalResultType.OK:
                    modalResult = 1;
                    break;

                case ModalResultType.CANCEL:
                    modalResult = 2;
                    break;

                case ModalResultType.YES:
                    modalResult = 6;
                    break;

                case ModalResultType.NO:
                    modalResult = 7;
                    break;

                case ModalResultType.CLOSE:
                    modalResult = 8;
                    break;

                case ModalResultType.HELP:
                    modalResult = 9;
                    break;
            }

            return modalResult;
        }

        public bool SetModalResult(ModalResultType mr)
        {
            if (Time.Instance.LastWindowInteraction.AddSeconds(2) > DateTime.UtcNow)
                return false;

            int modalResult = GetModalResult(mr);
            if (IsModal || Name == "modal")
            {
                Log.WriteLine("Window: Name [" + Name + "] SetModalResult [" + mr.ToString() + "] GetModalResult [" + modalResult + "]");
                if (DirectEve.ThreadedCall(PyWindow.Attribute("SetModalResult"), modalResult))
                {
                    Time.Instance.LastWindowInteraction = DateTime.UtcNow;
                    return true;
                }

                return false;
            }

            return false;
        }

        /// <summary>
        ///     Find a child object (usually button)
        /// </summary>
        /// <param name="container"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static PyObject FindChild(PyObject container, string name)
        {
            var childs = container.Attribute("children").Attribute("_childrenObjects").ToList();
            var ret = childs.Find(c => String.Compare((string)c.Attribute("name"), name) == 0) ?? PySharp.PyZero;

            if (ret == SC::SharedComponents.Py.PySharp.PyZero && container.Attribute("children").Attribute("_childrenObjects").Attribute("_childrenObjects").IsValid)
            {
                childs = container.Attribute("children").Attribute("_childrenObjects").Attribute("_childrenObjects").ToList();
                ret = childs.Find(c => String.Compare((string)c.Attribute("name"), name) == 0) ?? PySharp.PyZero;
            }
            return ret;
        }

        /// <summary>
        ///     Find a child object (using the supplied path)
        /// </summary>
        /// <param name="container"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static PyObject FindChildWithPath(PyObject container, IEnumerable<string> path)
        {
            return path.Aggregate(container, FindChild);
        }

        internal static List<DirectWindow> GetModalWindows(DirectEve directEve)
        {
            var windows = new List<DirectWindow>();

            var pySharp = directEve.PySharp;
            var carbonui = pySharp.Import("carbonui");
            var pyWindows = carbonui.Attribute("uicore").Attribute("uicore").Attribute("registry").Attribute("windows").ToList();
            foreach (var pyWindow in pyWindows)
            {
                if ((bool) pyWindow.Attribute("destroyed"))
                    continue;

                var name = pyWindow.Attribute("name");
                var nameStr = name.IsValid ? name.ToUnicodeString() : String.Empty;

                if (nameStr.Equals("modal") || (bool) pyWindow.Attribute("isModal"))
                {
                    var window = new DirectWindow(directEve, pyWindow);

                    if (windows.Any(i => i.WindowId == window.WindowId))
                        continue;

                    windows.Add(window);
                }

                if (nameStr == "telecom")
                {
                    var window = new DirectTelecomWindow(directEve, pyWindow);

                    if (windows.Any(i => i.WindowId == window.WindowId))
                        continue;

                    windows.Add(window);
                }
            }

            return windows;
        }

        internal static List<DirectWindow> GetWindows(DirectEve directEve)
        {
            try
            {
                var windows = new List<DirectWindow>();
                if (_windowTypeDict == null)
                {
                    SetupDict();
                }

                var pySharp = directEve.PySharp;
                var carbonui = pySharp.Import("carbonui");
                var pyWindows = carbonui.Attribute("uicore").Attribute("uicore").Attribute("registry")
                .Attribute("windows").ToList();
                foreach (var pyWindow in pyWindows)
                {
                    // Ignore destroyed windows
                    if ((bool)pyWindow.Attribute("destroyed"))
                        continue;

                     DirectWindow window = null;
                    try
                    {
                        foreach (var kv in _windowTypeDict)
                        {
                            var attr = pyWindow.Attribute(kv.Key).ToUnicodeString();

                            if (attr == null)
                                continue;

                            var dict = kv.Value;

                            if (dict.TryGetValue(attr, out var type))
                            {
                                window = type.Creator(directEve, pyWindow);
                                break;
                            }
                        }

                        if (window == null)
                            window = new DirectWindow(directEve, pyWindow);

                        if (windows.Any(i => i.WindowId != "message" && i.WindowId == window.WindowId))
                        {
                            if (DebugConfig.DebugWindows)
                            {
                                Log.WriteLine("Debug_Window.Name: [" + window.Name + "]");
                                Log.WriteLine("Debug_Window.Html: [" + window.Html + "]");
                                Log.WriteLine("Debug_Window.Type: [" + window.Guid + "]");
                                Log.WriteLine("Debug_Window.IsModal: [" + window.IsModal + "]");
                                Log.WriteLine("Debug_Window.IsDialog: [" + window.IsDialog + "]");
                                Log.WriteLine("Debug_Window.Caption: [" + window.Caption + "]");
                                Log.WriteLine("Debug_Window.WindowId: [" + window.WindowId + "]");
                            }

                            continue;
                        }

                        windows.Add(window);
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Exception [" + ex + "]");
                    }

                    if (window == null)
                        window = new DirectWindow(directEve, pyWindow);

                    if (windows.Any(i => i.WindowId == window.WindowId))
                        continue;

                    windows.Add(window);
                }

                return windows ?? new List<DirectWindow>();
            }
            catch (Exception)
            {
                return new List<DirectWindow>();
            }
        }

        #endregion Methods

        #region Nested type: WindowType

        private class WindowType
        {
            #region Constructors

            public WindowType(string attribute, string value, Func<DirectEve, PyObject, DirectWindow> creator)
            {
                Attribute = attribute;
                Value = value;
                Creator = creator;
            }

            #endregion Constructors

            #region Properties

            public string Attribute { get; set; }
            public Func<DirectEve, PyObject, DirectWindow> Creator { get; set; }
            public string Value { get; set; }

            #endregion Properties
        }

        #endregion Nested type: WindowType
    }
}