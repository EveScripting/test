extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Lookup;
using SC::SharedComponents.Extensions;
using SC::SharedComponents.IPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace EVESharpCore.Controllers
{
    public class MonitorGridController : BaseController
    {
        #region Constructors

        public MonitorGridController()
        {
            IgnorePause = false;
            IgnoreModal = false;
        }

        #endregion Constructors

        #region Properties

        public IEnumerable<EntityCache> PlayersOnGrid
        {
            get
            {
                try
                {
                    return ESCache.Instance.Entities.Where(entity => entity.Distance < 8000000 && entity.Id != ESCache.Instance.MyShipEntity.Id && entity.IsPlayer && !entity.IsNpc && corpTickersToNotMonitor.All(corpticker => corpticker != entity._directEntity.CorpTicker));
                }
                catch (Exception)
                {
                    return new List<EntityCache>();
                }
            }
        }

        #endregion Properties

        #region Fields

        public static List<string> corpTickersToNotMonitor = new List<string>();
        public IEnumerable<EntityCache> CachedEntitiesOnGrid = null;
        public IEnumerable<EntityCache> CachedPlayersOnGrid;
        public Dictionary<long, DateTime> dictEntitiesTrackingDateTimeOnGrid = new Dictionary<long, DateTime>();
        public List<EntityCache> HistoryOfPlayersOnGrid = new List<EntityCache>();
        public DateTime NextPulse = DateTime.UtcNow;
        public List<EntityCache> PlayersInWarpAndReported = new List<EntityCache>();
        public List<EntityCache> PlayersOnGridAndReported = new List<EntityCache>();
        public List<EntityCache> PlayersOnGridAndReportedInWarp = new List<EntityCache>();
        public List<EntityCache> tempPlayersOnGridAndReported = new List<EntityCache>();

        #endregion Fields

        #region Methods

        public static void LoadSettings(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            try
            {
                corpTickersToNotMonitor.Clear();
                XElement xmlElementCorpTickersToNotMonitorSection = CharacterSettingsXml.Element("corpTickersToNotMonitor") ?? CommonSettingsXml.Element("corpTickersToNotMonitor");
                if (xmlElementCorpTickersToNotMonitorSection != null)
                {
                    Log("Loading Corp Tickers we do not want MonitorGridController to monitor");
                    int i = 1;
                    foreach (XElement xmlCorpTicker in xmlElementCorpTickersToNotMonitorSection.Elements("corpTicker"))
                        if (corpTickersToNotMonitor.All(m => m != xmlCorpTicker.Value))
                        {
                            Log("   Any Player with a corp Ticker of [" + xmlCorpTicker.Value + "] will not be monitored");
                            corpTickersToNotMonitor.Add(xmlCorpTicker.Value);
                            i++;
                        }
                }
            }
            catch (Exception ex)
            {
                Log("Exception: [" + ex + "]");
            }
        }

        public override void DoWork()
        {
            try
            {
                if (NextPulse > DateTime.UtcNow)
                    return;

                NextPulse = DateTime.UtcNow.AddSeconds(2);

                if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                {
                    if (Time.Instance.LastInWarp.AddSeconds(4) > DateTime.UtcNow)
                        return;

                    if (ESCache.Instance.Stargates.Count > 0 && Time.Instance.LastInWarp.AddSeconds(10) > DateTime.UtcNow)
                        if (ESCache.Instance.ClosestStargate != null && ESCache.Instance.ClosestStargate.IsOnGridWithMe && ESCache.Instance.ClosestStargate.Distance < 10000)
                            return;
                }

                try
                {
                    if (ESCache.Instance.InStation || !ESCache.Instance.InSpace) return;

                    if (PlayersOnGrid != null && PlayersOnGrid.Any())
                    {
                        if (PlayersOnGrid.Count() > 50)
                            Log("On Grid: Player count on grid exceeds 50!");
                    }
                    else if (CachedPlayersOnGrid != null && CachedPlayersOnGrid.Any())
                    {
                        Log("On Grid: No Players here: previously [" + CachedPlayersOnGrid.Count() + "] players on grid");
                        CachedPlayersOnGrid = new List<EntityCache>();
                        dictEntitiesTrackingDateTimeOnGrid = new Dictionary<long, DateTime>();
                        PlayersOnGridAndReported = new List<EntityCache>();
                        PlayersInWarpAndReported = new List<EntityCache>();
                        return;
                    }

                    if (CachedPlayersOnGrid == null || PlayersOnGrid != CachedPlayersOnGrid)
                    {
                        CachedPlayersOnGrid = PlayersOnGrid;
                        //
                        // Remove any players that left grid and report people that left...
                        //
                        if (PlayersInWarpAndReported != null && PlayersOnGridAndReported.Count > 0)
                        {
                            tempPlayersOnGridAndReported = PlayersOnGridAndReported;
                            foreach (EntityCache PreviouslyOnGridPlayer in tempPlayersOnGridAndReported)
                                if (PlayersOnGrid.All(player => player.Id != PreviouslyOnGridPlayer.Id))
                                {
                                    if (dictEntitiesTrackingDateTimeOnGrid.ContainsKey(PreviouslyOnGridPlayer.Id))
                                        dictEntitiesTrackingDateTimeOnGrid.Remove(PreviouslyOnGridPlayer.Id);

                                    if (PlayersOnGridAndReported.Contains(PreviouslyOnGridPlayer))
                                        PlayersOnGridAndReported.Remove(PreviouslyOnGridPlayer);

                                    if (PlayersOnGridAndReportedInWarp.Contains(PreviouslyOnGridPlayer))
                                        PlayersOnGridAndReportedInWarp.Remove(PreviouslyOnGridPlayer);

                                    Log("On Grid: [" + PreviouslyOnGridPlayer.TypeName + "] Name [" + PreviouslyOnGridPlayer.Name + "] Is no longer visible on grid");
                                }
                        }

                        foreach (EntityCache newPlayerOnGrid in PlayersOnGrid.Where(i => corpTickersToNotMonitor.All(corpticker => corpticker != i._directEntity.CorpTicker) && (PlayersOnGridAndReported.Count == 0 || (PlayersOnGridAndReported != null && PlayersOnGridAndReported.All(reportedPlayer => reportedPlayer.Id != i.Id)))))
                        {
                            if (PlayersOnGridAndReported.All(j => j.Id != newPlayerOnGrid.Id)) PlayersOnGridAndReported.Add(newPlayerOnGrid);
                            if (HistoryOfPlayersOnGrid.All(j => j.Id != newPlayerOnGrid.Id)) HistoryOfPlayersOnGrid.Add(newPlayerOnGrid);
                            dictEntitiesTrackingDateTimeOnGrid.AddOrUpdate(newPlayerOnGrid.Id, DateTime.UtcNow);
                            Log("On Grid: [" + newPlayerOnGrid.TypeName + "] Distance [" + Math.Round(newPlayerOnGrid.Distance / 1000, 0) + "] Name [" + newPlayerOnGrid.Name + "] Corp [" + newPlayerOnGrid._directEntity.CorpTicker + "] Alliance [" + newPlayerOnGrid._directEntity.AllianceTicker + "]");
                        }
                    }

                    if (PlayersOnGrid.Any(player => player.HasInitiatedWarp))
                        foreach (EntityCache WarpDriveActivePlayer in PlayersOnGrid.Where(player => player.HasInitiatedWarp && (PlayersOnGridAndReportedInWarp == null || PlayersOnGridAndReportedInWarp.All(j => j.Id != player.Id))))
                        {
                            if (PlayersOnGridAndReportedInWarp.Count == 0 || PlayersOnGridAndReportedInWarp.All(j => j.Id != WarpDriveActivePlayer.Id)) PlayersOnGridAndReportedInWarp.Add(WarpDriveActivePlayer);
                            Log("Warp Drive Active!: [" + WarpDriveActivePlayer.TypeName + "] Distance [" + Math.Round(WarpDriveActivePlayer.Distance / 1000, 0) + "] Name [" + WarpDriveActivePlayer.Name + "]");
                        }

                    if (dictEntitiesTrackingDateTimeOnGrid != null)
                    {
                        Dictionary<long, DateTime> tempDictEntitiesTrackingDateTimeOnGrid = dictEntitiesTrackingDateTimeOnGrid;
                        foreach (KeyValuePair<long, DateTime> entityTrackingEntry in tempDictEntitiesTrackingDateTimeOnGrid)
                            if (DateTime.UtcNow > entityTrackingEntry.Value.AddMinutes(5))
                                dictEntitiesTrackingDateTimeOnGrid.Remove(entityTrackingEntry.Key);
                    }

                    foreach (EntityCache PlayerStillOnGrid in PlayersOnGrid.Where(i => dictEntitiesTrackingDateTimeOnGrid.All(trackedEntity => trackedEntity.Key != i._directEntity.Id)))
                    {
                        dictEntitiesTrackingDateTimeOnGrid.AddOrUpdate(PlayerStillOnGrid.Id, DateTime.UtcNow);
                        Log("Still On Grid: [" + PlayerStillOnGrid.TypeName + "] Distance [" + Math.Round(PlayerStillOnGrid.Distance / 1000, 0) + "] Name [" + PlayerStillOnGrid.Name + "] Corp [" + PlayerStillOnGrid._directEntity.CorpTicker + "] Alliance [" + PlayerStillOnGrid._directEntity.AllianceTicker + "]");
                    }
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
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