using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using SharedComponents.EVE;


namespace SharedComponents.IPC.TCP
{
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    public class WCFClientTCPCallback : IDuplexServiceCallbackTCP
    {
        private static ConcurrentDictionary<string, (bool, DateTime)> _eveInstanceRunningCache = new System.Collections.Concurrent.ConcurrentDictionary<string, (bool, DateTime)>();
        private static Object _lock = new Object();
        private static int _minSecondsCacheValid = 20;
        private static int _maxSecondsCacheValid = 35;
        private static Random _rnd = new Random();

        public bool IsEveInstanceRunning(string charName)
        {
            if (_eveInstanceRunningCache.TryGetValue(charName, out var t) && t.Item2 < DateTime.UtcNow)
                _eveInstanceRunningCache.TryRemove(charName, out _);

            if (!_eveInstanceRunningCache.TryGetValue(charName, out var tx))
            {
                lock (_lock)
                {
                    var res = Cache.Instance.EveAccountSerializeableSortableBindingList.List.ToList().Any(e => e.EveProcessExists() && e.CharacterName == charName);
                    var min = res ? _minSecondsCacheValid : _maxSecondsCacheValid / 3;
                    var max = res ? _maxSecondsCacheValid : _maxSecondsCacheValid / 3;
                    _eveInstanceRunningCache[charName] = (res, DateTime.UtcNow.AddSeconds(_rnd.Next(min, max)));
                    return res;
                }
            }
            else
            {
                return tx.Item1;
            }
        }
        #region Methods
        #endregion Methods
        public void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {
            //Debug.WriteLine($"ReceiveBroadcastMessage(TCP)  - [{broadcastMessage}]");
            WCFClient.Instance?.GetPipeProxy?.SendBroadcastMessage(broadcastMessage, true);
        }
    }
}