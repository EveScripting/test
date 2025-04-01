extern alias SC;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EVESharpCore.Cache;
using EVESharpCore.Logging;
using SC::SharedComponents.EVE;
using SC::SharedComponents.EVE.Types;
using SC::SharedComponents.IPC;

namespace EVESharpCore.Framework
{
    enum InvasionManagerState
    {
        OpenAgencyWindow,
        SaveData,
        CloseAgencyWindow
    }

    public class DirectInvasionManager
    {
        private readonly DirectEve _directEve;
        private DateTime _lastCheck;
        private InvasionManagerState _state;
        private bool _doUpdate;
        private DateTime _nextFrame;
        public DirectInvasionManager(DirectEve directEve)
        {
            _directEve = directEve;
            _lastCheck = DateTime.MinValue;
            _state = InvasionManagerState.OpenAgencyWindow;
            _nextFrame = DateTime.UtcNow;
            _trigStanding = (double?)null;
            _lastTrigStandingUpdate = DateTime.MinValue;
            _lastGetSolarSystemToAvoidUpdate = DateTime.MinValue;
            _getSolarSystemsToAvoid = null;
            _lastInvasionDateDictUpdate = DateTime.MinValue;
        }

        private double? _trigStanding;
        private double? _edenComStanding;
        private DateTime _lastEdenComStandingUpdate;
        private DateTime _lastTrigStandingUpdate;
        private DateTime _lastGetSolarSystemToAvoidUpdate;

        public double? GetTrigStanding()
        {
            if (!_trigStanding.HasValue || _lastTrigStandingUpdate.AddMinutes(5) < DateTime.UtcNow)
            {
                _lastTrigStandingUpdate = DateTime.UtcNow;
                var s = _directEve.Standings.GetStandingToFaction(FactionType.Triglavian_Collective);
                _trigStanding = s;
            }
            return _trigStanding;
        }

        public double? GetEdenComStanding()
        {
            if (!_edenComStanding.HasValue || _lastEdenComStandingUpdate.AddMinutes(5) < DateTime.UtcNow)
            {
                _lastEdenComStandingUpdate = DateTime.UtcNow;
                var s = _directEve.Standings.GetStandingToFaction(FactionType.EDENCOM);
                _edenComStanding = s;
            }
            return _edenComStanding;
        }

        private DateTime _lastInvasionDateDictUpdate;
        private Dictionary<int, InvasionDataItem> _invasionDataDict;
        public Dictionary<int, InvasionDataItem> InvasionDataDict
        {
            get
            {
                var last = _directEve.DirectSharedMemory.GetLastSuccessfulUpdate();
                if (_invasionDataDict == null || _lastInvasionDateDictUpdate.AddMinutes(5) < DateTime.UtcNow ||
                    last > _lastInvasionDateDictUpdate)
                {
                    _lastInvasionDateDictUpdate = DateTime.UtcNow;
                    Dictionary<int, InvasionDataItem> ret = new Dictionary<int, InvasionDataItem>();
                    var invasionData = WCFClient.Instance.GetPipeProxy.GetInvasionData();
                    foreach (var item in invasionData)
                    {

                        if (ret.ContainsKey(item.SolarSystemId))
                        {
                            var k = ret[item.SolarSystemId];
                            ret[item.SolarSystemId] = k.LastUpdate > item.LastUpdate ? k : item;
                            //Log.WriteLine(k.ToString());
                            //Log.WriteLine(item.ToString());
                            continue;
                        }

                        ret.Add(item.SolarSystemId, item);
                    }

                    _invasionDataDict = ret;
                }   
                return _invasionDataDict;
            }
        }

        private HashSet<DirectSolarSystem> _getSolarSystemsToAvoid;
        public HashSet<DirectSolarSystem> GetSolarSystemsToAvoid()
        {
            var last = _directEve.DirectSharedMemory.GetLastSuccessfulUpdate();
            if (_getSolarSystemsToAvoid == null || _lastGetSolarSystemToAvoidUpdate.AddMinutes(5) < DateTime.UtcNow || last > _lastGetSolarSystemToAvoidUpdate)
            {
                _lastGetSolarSystemToAvoidUpdate = DateTime.UtcNow;
                HashSet<InvasionDataItem> ret = null;
                var solarSystems = new HashSet<DirectSolarSystem>();
                var trigStanding = GetTrigStanding();
                var edenComStanding = GetEdenComStanding();

                if (trigStanding.HasValue && trigStanding.Value > 0)
                {
                    ret = WCFClient.Instance.GetPipeProxy.GetInvasionData();
                }

                if (edenComStanding.HasValue && edenComStanding >= 0)
                {
                    ret = WCFClient.Instance.GetPipeProxy.GetInvasionData().Where(i =>
                        i.InvasionType != InvasionType.INVASION_CHAPTER_THREE_EMPIRE_VICTORY).ToHashSet();
                }

                if (ret != null)
                {
                    foreach (var s in ret)
                    {
                        var solarSys = _directEve.SolarSystems[s.SolarSystemId];
                        solarSystems.Add(solarSys);
                    }
                }

                _getSolarSystemsToAvoid = solarSystems;
            }

            return _getSolarSystemsToAvoid;
        }

        public bool HaveListsBeenUpdatedAtleastOnce()
        {
            return _directEve.DirectSharedMemory.GetLastSuccessfulUpdate() != DateTime.MinValue;
        }

        public void OnFrame()
        {
            if (_doUpdate)
            {

                if (_nextFrame > DateTime.UtcNow)
                    return;

                _nextFrame = DateTime.UtcNow.AddMilliseconds(500);

                var agencyWnd = _directEve.GetAgencyWindow();
                switch (_state)
                {
                    case InvasionManagerState.OpenAgencyWindow:
                        if (agencyWnd == null)
                        {
                            _directEve.Log("Waiting for the agency window to open.");
                        }
                        else
                        {
                            if (!agencyWnd.Ready)
                            {
                                _directEve.Log("Agency not ready yet.");
                                return;
                            }

                            _directEve.Log("Agency window is open.");
                            _state = InvasionManagerState.SaveData;
                            _nextFrame = DateTime.UtcNow.AddMilliseconds(3500);
                        }
                        break;
                    case InvasionManagerState.SaveData:

                        if (agencyWnd.IsInvasionDataPopulated)
                        {
                            _directEve.Log("Updating invasion data.");
                            var data = agencyWnd.GetInvasionData().ToHashSet();
                            WCFClient.Instance.GetPipeProxy.SetInvasionData(data);
                            _nextFrame = DateTime.UtcNow.AddMilliseconds(1500);
                            _state = InvasionManagerState.CloseAgencyWindow;
                        }
                        else
                        {
                            _directEve.Log($"Invasion data not yet populated.");
                            _nextFrame = DateTime.UtcNow.AddMilliseconds(3500);
                        }

                        break;
                    case InvasionManagerState.CloseAgencyWindow:
                        _directEve.Log("Update finished. Closing agency window.");
                        agencyWnd.Close();
                        _directEve.DirectSharedMemory.SetLastSuccessfulUpdate();
                        _doUpdate = false;
                        _state = InvasionManagerState.OpenAgencyWindow;
                        break;
                }
                return;
            }


            if (_lastCheck.AddMinutes(5) < DateTime.UtcNow)
            {
                if (true)
                {
                    //_directEve.Log($"Update invasion setting = false.");
                    return;
                }

                _lastCheck = DateTime.UtcNow;
                _directEve.Log($"Checking for required invasion updates.");

                if (_directEve.DirectSharedMemory.IsInvasionDataUpToDate())
                {
                    _directEve.Log("Invasion list is update to date.");
                }
                else
                {
                    _directEve.Log("This client was selected to do the update.");
                    _doUpdate = true;
                }
            }
        }
    }
}
