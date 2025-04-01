using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;

namespace SharedComponents.Events
{
    public static class DirectEventHandler
    {
        public delegate void directEventHandler(string charName, DirectEvent directEvent);

        public static ConcurrentDictionary<string, ConcurrentDictionary<DirectEvents, DateTime?>> lastEventByType;
        public static ConcurrentDictionary<string, ConcurrentDictionary<DirectEvents, DateTime?>> rateLimitStorage;

        static DirectEventHandler()
        {
            lastEventByType = new ConcurrentDictionary<string, ConcurrentDictionary<DirectEvents, DateTime?>>();
            rateLimitStorage = new ConcurrentDictionary<string, ConcurrentDictionary<DirectEvents, DateTime?>>();
        }

        public static event directEventHandler OnDirectEvent;

        public static DateTime? GetLastEventReceived(string charName, DirectEvents directevent)
        {
            try
            {
                if (lastEventByType.ContainsKey(charName) && lastEventByType[charName].ContainsKey(directevent))
                    return lastEventByType[charName][directevent];
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return null;
            }
        }

        private static void SetLastEventReceived(ConcurrentDictionary<string, ConcurrentDictionary<DirectEvents, DateTime?>> dict, string charName,
            DirectEvents directEvent)
        {
            try
            {
                if (!dict.ContainsKey(charName))
                    dict[charName] = new ConcurrentDictionary<DirectEvents, DateTime?>();

                dict[charName][directEvent] = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        public static void ClearEvents(string charName)
        {
            try
            {
                lastEventByType.TryRemove(charName, out _);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        private static bool CheckRateLimit(string charName, DirectEvents directEvent)
        {
            try
            {
                ConcurrentDictionary<DirectEvents, DateTime?> lastEventDict;
                rateLimitStorage.TryGetValue(charName, out lastEventDict);

                if (lastEventDict != null && lastEventDict.ContainsKey(directEvent))
                {
                    DateTime? lastEventTime;
                    lastEventDict.TryGetValue(directEvent, out lastEventTime);
                    if (lastEventTime != null &&
                        lastEventTime.HasValue &&
                        lastEventTime.Value.AddSeconds(15) > DateTime.UtcNow)
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return false;
            }
        }

        public static void OnNewDirectEvent(string charName, DirectEvent directEvent)
        {
            try
            {
                SetLastEventReceived(lastEventByType, charName, directEvent.type);

                switch (directEvent.type)
                {
                    case DirectEvents.ACCEPT_MISSION:
                        break;
                    case DirectEvents.COMPLETE_MISSION:
                        break;
                    case DirectEvents.DECLINE_MISSION:
                        break;
                    case DirectEvents.DOCK_JUMP_ACTIVATE:
                        break;
                    case DirectEvents.LOCK_TARGET:
                        break;
                    case DirectEvents.UNDOCK:
                        break;
                    case DirectEvents.WARP:
                        break;
                    case DirectEvents.PANIC:
                        break;
                    case DirectEvents.CALLED_LOCALCHAT:
                    case DirectEvents.LOCKED_BY_PLAYER:
                    case DirectEvents.MISSION_INVADED:
                    case DirectEvents.PRIVATE_CHAT_RECEIVED:
                    case DirectEvents.CAPSULE:
                    case DirectEvents.ERROR:
                    case DirectEvents.NOTICE:

                        directEvent.color = Color.Red;
                        directEvent.warning = true;
                        break;
                }

                if (OnDirectEvent != null && CheckRateLimit(charName, directEvent.type))
                {
                    OnDirectEvent(charName, directEvent);
                    SetLastEventReceived(rateLimitStorage, charName, directEvent.type);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}