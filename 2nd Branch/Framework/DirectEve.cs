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

using EVESharpCore.Cache;
using EVESharpCore.Framework.Events;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Events;
using SC::SharedComponents.Extensions;
using SC::SharedComponents.Py;
using SC::SharedComponents.Py.Frameworks;
using SC::SharedComponents.WinApiUtil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Media.Effects;
using EVESharpCore.Lookup;
using SC::SharedComponents.IPC;
using SC::SharedComponents.Py.D3DDetour;
using EVESharpCore.Logging;
using SC::SharedComponents.Utility;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.DXGI;
using Device = SharpDX.DXGI.Device;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
using SC::SharedComponents.SharedMemory;
using EVESharpCore.Framework.Hooks;
using EVESharpCore.Framework.Lookup;
using System.Windows;
using System.Runtime.InteropServices;
using EVESharpCore.Questor.Combat;
//using ServiceStack;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectEveEventArgs : EventArgs
    {
        public DirectEveEventArgs(ulong lastFrameTook)
        {
            LastFrameTook = lastFrameTook;
        }

        public ulong LastFrameTook { get; }
    }

    public class DirectEve : IDisposable
    {
        private static Dictionary<String, UInt64> _hasFrameChangedCallerDictionary = new Dictionary<string, UInt64>();

        private static Dictionary<String, UInt64> _ignoreCurrentFrameExecutionCallerDict = new Dictionary<string, UInt64>();

        /// <summary>
        /// shield, armor, hull (x,y,z) range 0 ... 1.0
        /// </summary>
        public static Dictionary<long, (double, double, double)> _entityHealthPercOverrides = new Dictionary<long, (double, double, double)>();

        private static Stopwatch eveFrameSt = new Stopwatch();

        private static long _ballParkCount;

        private static List<string> _servicesAdditionalRequirementsCallOnlyOnce = new List<string>();
        //private DirectEveSecurity _security;
        //private bool _securityCheckFailed;

        /// <summary>
        ///     ActiveShip cache
        /// </summary>
        private DirectActiveShip _activeShip;

        /// <summary>
        ///     Cache the Agent Missions
        /// </summary>
        private List<DirectAgentMission> _agentMissions;

        /// <summary>
        ///     Cache the Bookmark Folders
        /// </summary>
        private List<DirectBookmarkFolder> _bookmarkFolders;

        /// <summary>
        ///     Cache the Bookmarks
        /// </summary>
        private List<DirectBookmark> _bookmarks;

        /// <summary>
        ///     Const cache
        /// </summary>
        private DirectConst _const;

        /// <summary>
        ///     Cache the GetConstellations call
        /// </summary>
        private Dictionary<long, DirectConstellation> _constellations;

        /// <summary>
        ///     Item container cache
        /// </summary>
        private Dictionary<long, DirectContainer> _containers;

        private DirectContract _directContract;

        private DirectPlexVault _directPlexVault;

        private bool _enableStatisticsModifying;

        /// <summary>
        ///     Cache the Entities
        /// </summary>
        private Dictionary<long, DirectEntity> _entitiesById;

        private double _frameTimeAbove100ms;
        private double _frameTimeAbove200ms;
        private double _frameTimeAbove300ms;
        private double _frameTimeAbove400ms;
        private double _frameTimeAbove500ms;

        /// <summary>
        ///     The framework object that wraps OnFrame and Log
        /// </summary>
        private IFramework _framework;
        /// <summary>
        ///     Item Hangar container cache
        /// </summary>
        private DirectContainer _itemHangar;

        /// <summary>
        ///     Info on when a certain target was last targeted
        /// </summary>
        private Dictionary<long, DateTime> _lastKnownTargets;

        ////Statistic variables
        private long _lastOnframeTook;

        /// <summary>
        ///     Global Assets cache
        /// </summary>
        private List<DirectItem> _listGlobalAssets;

        /// <summary>
        ///     Cache the LocalSvc objects
        /// </summary>
        private Dictionary<string, PyObject> _localSvcCache;

        /// <summary>
        ///     Login cache
        /// </summary>
        private DirectLogin _login;

        private DirectCharacterCreation _characterCreation;

        private DirectLayers _layers;
        private DirectGameLogs _gameLogs;

        /// <summary>
        ///     Market Window cache
        /// </summary>
        private DirectMarketWindow _marketWindow;

        /// <summary>
        ///     Me cache
        /// </summary>
        private DirectMe _me;

        private List<DirectWindow> _modalWindows;

        /// <summary>
        ///     Cache the Windows
        /// </summary>
        private List<DirectUIModule> _modules;

        /// <summary>
        ///     Navigation cache
        /// </summary>
        private DirectNavigation _navigation;

        private Dictionary<DirectCmd, DateTime> _nextDirectCmdExec;

        private double _prevFrameTimeAbove100ms;
        private double _prevFrameTimeAbove200ms;
        private double _prevFrameTimeAbove300ms;
        private double _prevFrameTimeAbove400ms;
        private double _prevFrameTimeAbove500ms;
        private double _prevtimesliceWarnings;

        /// <summary>
        ///     Cache the GetRegions call
        /// </summary>
        private Dictionary<long, DirectRegion> _regions;

        /// <summary>
        ///     Session cache
        /// </summary>
        private DirectSession _session;

        /// <summary>
        ///     Ship Hangar container cache
        /// </summary>
        private DirectContainer _shipHangar;

        /// <summary>
        ///     Ship's cargo container cache
        /// </summary>
        private DirectContainer _shipsCargo;

        /// <summary>
        ///     Ship's drone bay cache
        /// </summary>
        private DirectContainer _shipsDroneBay;

        /// <summary>
        ///     Ship's fleet hangar cache
        /// </summary>
        private DirectContainer _shipsFleetHangar;

        /// <summary>
        ///     Ship's modules container cache
        /// </summary>
        private DirectContainer _shipsModules;

        /// <summary>
        ///     Ship's ore hold container cache
        /// </summary>
        private DirectContainer _shipsAmmoHold;
        private DirectContainer _shipsOreHold;
        private DirectContainer _shipsMineralHold;
        private DirectContainer _shipsGeneralMiningHold;
        private DirectContainer _shipsGasHold;
        private DirectContainer _shipsIceHold;

        private DirectSkills _skills;

        private DirectWallet _wallet;

        /// <summary>
        ///     Cache the GetRegions call
        /// </summary>
        private Dictionary<int, DirectSolarSystem> _solarSystems;

        /// <summary>
        ///     Standings cache
        /// </summary>
        private DirectStandings _standings;

        /// <summary>
        ///     Cache the GetStations call
        /// </summary>
        private Dictionary<int, DirectStation> _stations;

        /// <summary>
        ///     Info on when a target was in targetsBeingRemoved set
        /// </summary>
        private Dictionary<long, DateTime> _targetsBeingRemoved;

        private double _timesliceWarnings;
        private DateTime _waitForLockedItemsUntil;

        /// <summary>
        ///     Cache the GetWindows call
        /// </summary>
        private List<DirectWindow> _windows;


        private DirectHooking _directHooking;
        public DirectHooking Hooking => _directHooking ??= new DirectHooking(this);

        private DirectSharedMemory _directSharedMemory;
        public DirectSharedMemory DirectSharedMemory => _directSharedMemory ??= new DirectSharedMemory(this);

        private List<string> _servicesToLoad = new List<string>() { "agents", "standing" };

        // Import user32.dll for PostMessage
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private List<DirectAgent> _listDirectAgentsTypeDistribution;

        //public HashSet<long> DronesWeHaveLaunched = new HashSet<long>();

        /// <summary>
        ///     Create a DirectEve object
        /// </summary>
        public DirectEve(IFramework framework = null, bool enableStatisticModifying = true)
        {
            ForegroundWindowBoolSharedArray = new SharedArray<bool>(ESCache.Instance.EveAccount.GUID.ToString() + nameof(UsedSharedMemoryNames.ForegroundWindowBool));

            _enableStatisticsModifying = enableStatisticModifying;

            // create an instance of IFramework
            if (framework != null)
                _framework = framework;

            try
            {
                _localSvcCache = new Dictionary<string, PyObject>();
                _containers = new Dictionary<long, DirectContainer>();
                _lastKnownTargets = new Dictionary<long, DateTime>();
                _targetsBeingRemoved = new Dictionary<long, DateTime>();
                _nextDirectCmdExec = new Dictionary<DirectCmd, DateTime>();
                _lastSeenEffectActivating = new Dictionary<long, DateTime>();

                // Setup packing handling hooks
                PacketRecvHook.OnPacketRecv += PacketRecvHookOnOnPacketRecv;
                PacketSendHook.OnPacketSend += PacketSendHookOnOnPacketSend;

#if DEBUG
                //Log("Registering OnFrame event");
#endif
                _framework.RegisterFrameHook(FrameworkOnFrame);
            }
            catch (Exception)
            {
                throw;
            }
        }

        ~DirectEve()
        {
            try
            {
                // Remove packing handling hooks
                PacketRecvHook.OnPacketRecv -= PacketRecvHookOnOnPacketRecv;
                PacketSendHook.OnPacketSend -= PacketSendHookOnOnPacketSend;
                Dispose();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        void PacketRecvHookOnOnPacketRecv(byte[] packetbytes)
        {
            try
            {

            }
            catch (Exception e)
            {
                Log(e.ToString());
            }
        }

        void PacketSendHookOnOnPacketSend(byte[] packetbytes)
        {
            try
            {

            }
            catch (Exception e)
            {
                Log(e.ToString());
            }
        }

        private void Resize(object o, EventArgs e)
        {
            try
            {
                DirectX.Resize(this, e);
                DirectDraw.Resize(this, e);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

        }

        public double GetInactivitySecondsSinceLastInput()
        {
            var lastInt = GetLocalSvc("gameui")["_last_ui_interaction_timestamp"]
                .ToDateTimeFromPythonDateTime();
            var gametime = PySharp.Import("gametime");
            var now = gametime.Call("now").ToDateTimeFromPythonDateTime();
            //Log($"_last_ui_interaction_timestamp [{lastInt}] now [{now}] diffSeconds {Math.Abs((lastInt - now).TotalSeconds)}");
            return Math.Abs((lastInt - now).TotalSeconds);
        }

        private void HandleGRPCTimeouts()
        {
            try
            {
                if (!Interval(1000))
                    return;

                if (_userInformationGatheredSharedArray == null)
                    _userInformationGatheredSharedArray = new SharedArray<bool>(ESCache.Instance.EveAccount.GUID.ToString() + nameof(UsedSharedMemoryNames.GRPCUserInformationGathered), 1);

                // if we didn't receive the grpc request yet, prevent code below from setting publicGatewaySvc.publicGateway to PyNone
                if (!_userInformationGatheredSharedArray[0])
                    return;

                //Log("Ping");

                var resp = GetLocalSvc("cosmeticsLicenseSvc")["_ship_emblems_licenses_controller"]["_ship_license_gateway"]["_entitlementRequestMessenger"]["public_gateway"]["grpc_requests_broker"]["router"]["response_handlers"];

                if (!resp.IsValid)
                {
                    Log("HandleGRPCTimeouts: response_handlers is not valid");
                    return;
                }

                var d = resp.ToDictionary<PyObject>();
                //var deadlineThreashold = DateTime.UtcNow.AddSeconds(1);
                foreach (var kv in d)
                {
                    //Log($"kv.Value[\"deadline\"].ToDateTimeFromPythonTime() {kv.Value["deadline"].ToDateTimeFromPythonTime()} deadlineThreashold {deadlineThreashold}");
                    //if (kv.Value["deadline"].ToDateTimeFromPythonTime() > deadlineThreashold)
                    //{
                    //Log("Popping one timeout.");
                    kv.Value.Call("timeout");
                    resp.Call("pop", kv.Key);
                    //}
                }

                if (GetLocalSvc("publicGatewaySvc")["publicGateway"].IsValid)
                    GetLocalSvc("publicGatewaySvc").SetAttribute("publicGateway", PySharp.PyNone);

                if (GetLocalSvc("cosmeticsSvc")["_structure_cosmetic_states_controller"]["_killswitch"].IsValid)
                    GetLocalSvc("cosmeticsSvc")["_structure_cosmetic_states_controller"].SetAttribute<bool>("_killswitch", true);
                else
                    Log("GetLocalSvc(\"cosmeticsSvc\")[\"_structure_cosmetic_states_controller\"][\"_killswitch\"].IsValid - not valid!!");
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
        }

        //public void SetRequestMessengerTimeout()
        //{
        //    //PySharp.Import("eve.client.script.ui.shared.cosmetics.messengers.entitlements.character.ship.requestMessenger").SetAttribute("TIMEOUT_SECONDS", 10);
        //    //GetLocalSvc("publicGatewaySvc").SetAttribute("publicGateway", PySharp.PyNone);
        //}

        /// <summary>
        ///     Return a DirectConst object
        /// </summary>
        internal DirectConst Const => _const ?? (_const = new DirectConst(this));

        /// <summary>
        ///     Return a DirectNavigation object
        /// </summary>
        public DirectLogin Login => _login ?? (_login = new DirectLogin(this));

        public DirectCharacterCreation CharacterCreation => _characterCreation ?? (_characterCreation = new DirectCharacterCreation(this));

        public DirectLayers Layers => _layers ?? (_layers = new DirectLayers(this));
        public DirectGameLogs GameLogs => _gameLogs ?? (_gameLogs = new DirectGameLogs(this));

        public ulong GetLastFrameExecutionDuration { get; private set; }

        /// <summary>
        ///     Return a DirectNavigation object
        /// </summary>
        public DirectNavigation Navigation => _navigation ?? (_navigation = new DirectNavigation(this));

        public DirectContract DirectContract => _directContract ?? (_directContract = new DirectContract(this));

        /// <summary>
        ///     Return a DirectMe object
        /// </summary>
        public DirectMe Me => _me ?? (_me = new DirectMe(this));

        public DirectPlexVault PlexVault => _directPlexVault ?? (_directPlexVault = new DirectPlexVault(this));

        /// <summary>
        ///     Return a DirectStandings object
        /// </summary>
        public DirectStandings Standings => _standings ?? (_standings = new DirectStandings(this));

        /// <summary>
        ///     Return a DirectActiveShip object
        /// </summary>
        public DirectActiveShip ActiveShip => _activeShip ?? (_activeShip = new DirectActiveShip(this));

        /// <summary>
        ///     Return a DirectSession object
        /// </summary>
        public DirectSession Session => _session ?? (_session = new DirectSession(this));

        /// <summary>
        ///     Return a DirectSkills object
        /// </summary>
        public DirectSkills Skills => _skills ?? (_skills = new DirectSkills(this));

        /// <summary>
        ///     Return a DirectWallet object
        /// </summary>
        public DirectWallet Wallet => _wallet ?? (_wallet = new DirectWallet(this));

        /// <summary>
        ///     Internal reference to the PySharp object that is used for the frame
        /// </summary>
        /// <remarks>
        ///     This reference is only valid while in an OnFrame event
        /// </remarks>
        public PySharp PySharp { get; private set; }

        /// <summary>
        ///     Return a list of entities
        /// </summary>
        /// <value></value>
        /// <remarks>
        ///     Only works in space
        /// </remarks>
        public List<DirectEntity> Entities => EntitiesById.Values.ToList();

        /// <summary>
        ///     Return a dictionary of entities by id
        /// </summary>
        /// <value></value>
        /// <remarks>
        ///     Only works in space
        /// </remarks>
        public Dictionary<long, DirectEntity> EntitiesById
        {
            get
            {
                if (_entitiesById == null)
                    _entitiesById = DirectEntity.GetEntities(this);

                if (DebugConfig.DebugEntities)
                {
                    foreach (var ent in _entitiesById)
                    {
                        Logging.Log.WriteLine("ID [" + ent.Value.Id + "] Name [" + ent.Value.Name + "] Distance [" + Math.Round(ent.Value.Distance/1000, 0) + "k] TypeID [" + ent.Value.TypeId + "] GroupID [" + ent.Value.GroupId + "] Created [" + ent.Value._thisEntityCreated + "]");
                    }
                }

                return _entitiesById;
            }
        }

        public List<int> GetTypeIdsByGroupId(int groupId)
        {
            var res = PySharp.Import("evetypes").Call("GetTypeIDsByGroup", groupId).ToList<int>();
            return res;
        }
        public List<int> GetGroupIdsByCategoryId(int catId)
        {
            var res = PySharp.Import("evetypes").Call("GetGroupIDsByCategory", catId).ToList<int>();
            return res;
        }

        public static int? LastKeepAtRangeDistance { get; set; } = null;
        public static double? LastSetSpeedSpeedToSet { get; set; } = null;
        public string GetAttributeDisplayName(int attributeID)
        {
            //dogma.data.get_attribute_display_name
            var res = this.PySharp.Import("dogma.data").Call("get_attribute_display_name", attributeID)?.ToUnicodeString();
            return res;
        }

        public DirectStaticDataLoader DirectStaticDataLoader => _directStaticDataLoader ?? (_directStaticDataLoader = new DirectStaticDataLoader(this));

        public static Dictionary<long, double> DictEntitiesXPositionInfoCachedAcrossFrames { get; set; } = new Dictionary<long, double>();
        public static Dictionary<long, double> DictEntitiesYPositionInfoCachedAcrossFrames { get; set; } = new Dictionary<long, double>();
        public static Dictionary<long, double> DictEntitiesZPositionInfoCachedAcrossFrames { get; set; } = new Dictionary<long, double>();

        /// <summary>
        ///     The last bookmark update
        /// </summary>
        public DateTime LastBookmarksUpdate => DirectBookmark.GetLastBookmarksUpdate(this) ?? new DateTime(0, 0, 0);

        /// <summary>
        ///     Return a list of bookmarks
        /// </summary>
        /// <value></value>
        public List<DirectBookmark> Bookmarks => _bookmarks ?? (_bookmarks = DirectBookmark.GetBookmarks(this));

        /// <summary>
        ///     Return a list of bookmark folders
        /// </summary>
        /// <value></value>
        public List<DirectBookmarkFolder> BookmarkFolders => _bookmarkFolders ?? (_bookmarkFolders = DirectBookmark.GetFolders(this));

        /// <summary>
        ///     Return a list of agent missions
        /// </summary>
        /// <value></value>
        public List<DirectAgentMission> AgentMissions => _agentMissions ?? (_agentMissions = DirectAgentMission.GetAgentMissions(this));

        /// <summary>
        ///     Return a list of all open windows
        /// </summary>
        /// <value></value>
        public List<DirectWindow> Windows
        {
            get
            {
                try
                {
                    if (_windows != null)
                    {
                        return _windows;
                    }

                    _windows = DirectWindow.GetWindows(this) ?? new List<DirectWindow>();
                    return _windows ?? new List<DirectWindow>();
                }
                catch (InvalidOperationException)
                {
                    return new List<DirectWindow>();
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return new List<DirectWindow>();
                }
            }
        }

        public List<DirectChatWindow> ChatWindows
        {
            get
            {
                try
                {
                    List<DirectWindow> windows = new List<DirectWindow>();
                    List<DirectChatWindow> chatChannels = new List<DirectChatWindow>();
                    windows.AddRange(Windows.Where(w => w.Name.StartsWith("chatchannel_")));

                    foreach (DirectWindow w in windows)
                    {
                        DirectChatWindow c = (DirectChatWindow)w;
                        chatChannels.Add(c);
                    }

                    return chatChannels;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return new List<DirectChatWindow>();
                }
            }
        }

        /// <summary>
        ///  Gets a live attribute value (i.e non static, including skills and implants and other modifications)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param> ItemId, characterID
        /// <param name="attributeId"></param> AttributeId, i.e Const["attributeMaxActiveDrones"]
        /// <returns></returns>
        public T GetLiveAttribute<T>(long id, int attributeId)
        {

            //DirectEve.Log("Id = " + id);
            var dmLoc = GetLocalSvc("clientDogmaIM").Attribute("dogmaLocation");
            var item = dmLoc.Attribute("dogmaItems").DictionaryItem(id);
            if (item.IsValid)
            {
                var hasAttribute = dmLoc.Call("ItemHasAttribute", id, attributeId);

                if (!hasAttribute.IsValid || !hasAttribute.ToBool())
                {
                    Log($"[GetLiveAttribute] ItemId [{id}] does not have attribute [{attributeId}]");
                    return default(T);
                }

                var attribute = item.Attribute("attributes").DictionaryItem(attributeId);
                if (attribute.IsValid)
                {
                    var currentVal = attribute.Attribute("currentValue");
                    //DirectEve.Log("GetLiveAttribute: " + attribute.LogObject());
                    if (!currentVal.GetValue(out var obj, out var type))
                        return default(T);
                    try
                    {
                        var r = (T)obj;
                        bool isDefault = EqualityComparer<T>.Default.Equals(r, default(T));

                        // TODO: Currently only for dynamic items, maybe there are other edge cases where the attributes are incomplete.
                        if (isDefault && IsDynamicItem(id))
                        {
                            return GetAccurateAttributeValue<T>(id, attributeId);
                        }

                        return r;
                    }
                    catch (Exception ex)
                    {
                        Log($"{ex.ToString()}");
                        return default(T);
                    }
                }
            }

            // This are the raw values of the drones, not including the ships + character specific values :( Need a workaround
            //if (IsDynamicItem(id))
            //{
            //    return GetDynamicItemAttribute<T>(id, attributeId);
            //}

            return default(T);
        }

        public T GetAccurateAttributeValue<T>(long id, int attributeId)
        {
            if (id <= 0 || attributeId <= 0)
                return default(T);

            //DirectEve.Log("Id = " + id);
            var dmLoc = GetLocalSvc("clientDogmaIM").Attribute("dogmaLocation");
            if (dmLoc.IsValid)
            {
                // Dogmaitems has keys which are not long/int type @_@
                var dogmaItems = dmLoc.Attribute("dogmaItems");
                if (!dogmaItems.IsValid)
                {
                    return default(T);
                }

                var item = dogmaItems.DictionaryItem(id);

                if (!item.IsValid)
                    {
                    return default(T);
                }

                var attributes = item.Attribute("attributes");

                if (!attributes.IsValid)
                {
                    return default(T);
                }

                var attribute = attributes.DictionaryItem(attributeId);

                if (!attribute.IsValid)
                {
                    return default(T);
                }

                bool isDocked = Session.IsInDockableLocation;
                // __builtin__.sm.services[godma].stateManager.invitems

                bool itemExistsInGodma = false;
                var godma = GetLocalSvc("godma");
                if (godma.IsValid)
                {
                    var stateManger = godma.Attribute("stateManager");
                    if (stateManger.IsValid)
                    {
                        var invItems = stateManger.Attribute("invItems");
                        if (invItems.IsValid)
                        {
                            itemExistsInGodma = invItems.DictionaryItem(id).IsValid;
                        }
                    }
                }

                PyObject attributeValue = PySharp.PyNone;

                if (isDocked || !itemExistsInGodma)
                {
                    attributeValue = dmLoc.Call("GetAttributeValue", id, attributeId);
                }
                else
                {
                    attributeValue = dmLoc.Call("GetGodmaAttributeValue", id, attributeId);
                }

                if (attributeValue.IsValid)
                {
                    if (!attributeValue.GetValue(out var obj, out var type))
                        return default(T);
                    try
                    {
                        var r = (T)obj;
                        return r;
                    }
                    catch (Exception ex)
                    {
                        Log($"{ex.ToString()}");
                        return default(T);
                    }
                }
            }
            return default(T);
        }

        public bool IsDynamicItem(long itemId)
        {
            return GetLocalSvc("dynamicItemSvc")?["dynamicItemCache"]?.DictionaryItem(itemId)?.IsValid ?? false;
        }

        // __builtin__.sm.services[dynamicItemSvc].dynamicItemCache[13371337].attributes
        public T GetDynamicItemAttribute<T>(long id, int attributeId)
        {
            //DirectEve.Log("Id = " + id);
            var item = GetLocalSvc("dynamicItemSvc").Attribute("dynamicItemCache").DictionaryItem(id);
            if (item.IsValid)
            {
                var attribute = item.Attribute("attributes").DictionaryItem(attributeId);
                if (attribute.IsValid)
                {
                    if (!attribute.GetValue(out var obj, out var type))
                        return default(T);
                    try
                    {
                        return (T)obj;
                    }
                    catch (Exception ex)
                    {
                        Log($"{ex.ToString()}");
                        return default(T);
                    }
                }
            }
            return default(T);
        }
        public T GetTypeAttribute<T>(long typeId, int attributeId)
        {
            var typeAttr = GetLocalSvc("godma").Call("GetTypeAttribute", typeId, attributeId, PySharp.PyNone);
            if (typeAttr.IsValid)
            {
                if (!typeAttr.GetValue(out var obj, out var type))
                    return default(T);
                try
                {
                    return (T)obj;
                }
                catch (Exception ex)
                {
                    Log($"{ex.ToString()}");
                    return default(T);
                }
            }
            return default(T);
        }

        public T GetPref<T>(string key)
        {
            // eveprefs.prefs.ini
            PySharp.Import("eveprefs")["prefs"]["ini"].Call("GetValue", key).GetValue(out var obj, out var type);
            try
            {
                //return (T)Convert.ChangeType(obj, typeof(T));
                return (T)obj;
            }
            catch (Exception)
            {
                return default(T);
            }
        }

        //https://w.atwiki.jp/evenewsjapan/pages/29.html

        public void SetPref<T>(string key, T value)
        {
            // eveprefs.prefs.ini
            PySharp.Import("eveprefs")["prefs"]["ini"].Call("SetValue", key, (T)value);
        }

        public void BracketsAlwaysShowShipText(bool alwaysShow = true)
        {
            SetPref<bool>("bracketsAlwaysShowShipText", alwaysShow);
            GetLocalSvc("bracket").Call("Reload");
        }

        public void NewbieTutorialDisable()
        {
            SetPref<int>("newbie", 1);
        }

        public string GetLocalizationMessageById(int id)
        {
            if (_localizationMessageByIdStorage.TryGetValue(id, out var val))
            {
                return val;
            }
            var r = PySharp.Import("localization").Call("GetByMessageID", id).ToUnicodeString();
            _localizationMessageByIdStorage[id] = r;
            return r;
        }

        private static Dictionary<int, string> _localizationMessageByIdStorage = new Dictionary<int, string>();

        public string GetLocalizationMessageByLabel(string label)
        {
            if (_localizationMessageByLabelStorage.TryGetValue(label, out var val))
            {
                return val;
            }
            var r = PySharp.Import("localization").Call("GetByLabel", label).ToUnicodeString();
            _localizationMessageByLabelStorage[label] = r;
            return r;
        }

        public void OpenFakeQtyModal(int itemQuantity = 1)
        {
            //"uix.QtyPopup(itemQuantity, 1, 1, None, localization.GetByLabel('UI/Inventory/ItemActions/DivideItemStack'))"
            var uix = PySharp.Import("eve.client.script.ui.util.uix")["QtyPopup"];
            var localByLabel = GetLocalizationMessageByLabel("UI/Inventory/ItemActions/DivideItemStack");
            if (uix.IsValid)
            {
                Log($"localByLabel {localByLabel}");
                ThreadedCall(uix, itemQuantity, 1, 1, PySharp.PyNone, localByLabel);
            }
        }

        private static Dictionary<string, string> _localizationMessageByLabelStorage = new Dictionary<string, string>();

        /// <summary>
        ///     Return a list of all open modal/dialog windows
        /// </summary>
        /// <value></value>
        public List<DirectWindow> ModalWindows => _modalWindows ?? (_modalWindows = DirectWindow.GetModalWindows(this));

        /// <summary>
        ///     Return a list of all modules
        /// </summary>
        /// <value></value>
        /// <remarks>
        ///     Only works inspace and does not return hidden modules
        /// </remarks>
        public List<DirectUIModule> Modules => _modules ??= DirectUIModule.GetModules(this);


        private DirectWindowManager _dwm;
        public DirectWindowManager DWM => _dwm ??= new DirectWindowManager(this);

        public void enableFullLogging()
        {
            Log("Enabling full logging!");
            var lg = PySharp.Import("blue").Attribute("LogControl");
            Log("LogtypeInfoIsPrivilegedOnly is " + (bool)lg.Attribute("LogtypeInfoIsPrivilegedOnly"));
            var call = PySharp.Import("__builtin__");
            call.Call("setattr", lg, "LogtypeInfoIsPrivilegedOnly", false);
        }


        private List<DirectEntity> _activeDrones;
        /// <summary>
        ///     Return active drone id's
        /// </summary>
        /// <value></value>
        public List<DirectEntity> ActiveDrones
        {
            get
            {
                // Dumping attributes of {id: <KeyVal: {'typeID': 2203, 'droneID': long, 'activityState': 0, 'controllerID': long, 'targetID': None, 'locationID': x, 'ownerID': x, 'controllerOwnerID': x}>}...
                var droneIds = GetLocalSvc("michelle").Call("GetDrones").ToDictionary<long>().Keys;
                _activeDrones = Entities.Where(e => droneIds.Any(d => d == e.Id)).ToList();
                return _activeDrones;
            }
        }

        private CoreHookManager _coreHookManager;
        public CoreHookManager CoreHookManager => _coreHookManager ??= new CoreHookManager(this);

        //private WindowRecorder _windowRecorder;
        //public WindowRecorder WindowRecorder => _windowRecorder ??= new WindowRecorder();

        private DirectNewEdenStore _newEdenStore;
        public DirectNewEdenStore NewEdenStore => _newEdenStore ?? (_newEdenStore = new DirectNewEdenStore(this));

        /// <remarks>
        ///     This is cached throughout the existence of this DirectEve Instance
        /// </remarks>
        public Dictionary<int, DirectStation> Stations => _stations ?? (_stations = DirectStation.GetStations(this));

        public int GetSolarSystemIdByName(string name)
        {
            if (DirectSolarSystem._solarSystemByName == null)
            {
                SolarSystems.FirstOrDefault();
            }
            if (DirectSolarSystem._solarSystemByName.TryGetValue(name, out var id))
                return id;
            return -1;
        }
        /// <summary>
        ///     Return a dictionary of solar systems
        /// </summary>
        /// <remarks>
        ///     This is cached throughout the existence of this DirectEve Instance
        /// </remarks>
        public Dictionary<int, DirectSolarSystem> SolarSystems => _solarSystems ?? (_solarSystems = DirectSolarSystem.GetSolarSystems(this));

        /// <summary>
        ///     Return a dictionary of solar systems
        /// </summary>
        /// <remarks>
        ///     This is cached throughout the existence of this DirectEve Instance
        /// </remarks>
        public Dictionary<long, DirectConstellation> Constellations => _constellations ?? (_constellations = DirectConstellation.GetConstellations(this));

        /// <summary>
        ///     Return a dictionary of solar systems
        /// </summary>
        /// <remarks>
        ///     This is cached throughout the existence of this DirectEve Instance
        /// </remarks>
        public Dictionary<long, DirectRegion> Regions => _regions ?? (_regions = DirectRegion.GetRegions(this));

        public static Dictionary<Tuple<long, string>, int> DictCurrentCourierMissionItemMoveRetries { get; set; } = new Dictionary<Tuple<long, string>, int>();

        public static Dictionary<Tuple<long, string>, CourierMissionCtrlState> DictCurrentCourierMissionCtrlState { get; set; } = new Dictionary<Tuple<long, string>, CourierMissionCtrlState>();

        public static Dictionary<Tuple<long, string>, CourierMissionCtrlState> DictPreviousCourierMissionCtrlState { get; set; } = new Dictionary<Tuple<long, string>, CourierMissionCtrlState>();

        public static Dictionary<Tuple<long, string>, Faction> DictCurrentMissionFaction { get; set; } = new Dictionary<Tuple<long, string>, Faction>();

        public static Dictionary<long, DirectItem> DictCurrentCourierMissionItemToMove { get; set; } = new Dictionary<long, DirectItem>();
        public static Dictionary<long, string> DictCurrentCourierMissionToContainer { get; set; } = new Dictionary<long, string>();
        public static Dictionary<long, string> DictCurrentCourierMissionFromContainer { get; set; } = new Dictionary<long, string>();

        /// <summary>
        ///     Is EVE rendering 3D, you can enable/disable rendering by setting this value to true or false
        /// </summary>
        /// <remarks>
        ///     This used SceneName to determine what 'scene' you are in to enable/disable 3d for that scene: inspace or instation
        /// </remarks>
        public bool Rendering3D
        {
            get
            {
                var rendering1 = (bool)GetLocalSvc("sceneManager").Attribute("registeredScenes").DictionaryItem(SceneName).Attribute("display");
                return rendering1;
            }
            set => GetLocalSvc("sceneManager").Attribute("registeredScenes").DictionaryItem(SceneName).SetAttribute("display", value);
        }

        public string SceneName
        {
            get
            {
                //
                // options are:
                // default: means we have in space
                // characterCreation: character creation
                // starmap
                // systemmap
                // planet
                // ShipTree
                // DockPanel
                // doesnt include station or citadel!?!

                string sceneName = "default";
                //if (Login.AtCharacterSelection)
                //...
                if (Session.IsInSpace)
                {
                    sceneName = "default";
                }
                else if (Session.InJump)
                {
                    sceneName = "transition";
                }
                else if (Session.IsInDockableLocation)
                {
                    sceneName = "hangar";
                }

                return sceneName;
            }
        }

        /// <summary>
        ///     Is EVE loading textures, you can enable/disable texture loading by setting this value to true or false
        /// </summary>
        /// <remarks>
        ///     Use at own risk!
        /// </remarks>
        public bool ResourceLoad
        {
            get
            {
                var disableGeometryLoad = (bool)PySharp.Import("trinity").Attribute("device").Attribute("disableGeometryLoad");
                var disableEffectLoad = (bool)PySharp.Import("trinity").Attribute("device").Attribute("disableEffectLoad");
                var disableTextureLoad = (bool)PySharp.Import("trinity").Attribute("device").Attribute("disableTextureLoad");
                return disableGeometryLoad || disableEffectLoad || disableTextureLoad;
            }
            set
            {
                PySharp.Import("trinity").Attribute("device").SetAttribute("disableGeometryLoad", value);
                PySharp.Import("trinity").Attribute("device").SetAttribute("disableEffectLoad", value);
                PySharp.Import("trinity").Attribute("device").SetAttribute("disableTextureLoad", value);
            }
        }

        public void SetResourceCacheSize(int size)
        {
            var motherLode = PySharp.Import("blue")["motherLode"];
            if (motherLode.IsValid)
            {
                motherLode.SetAttribute<int>("maxMemUsage", size);
                Log($"Set resource cache size to [{size}] mb.");
            }
        }

        public void ClearResourceCache()
        {
            var motherLode = PySharp.Import("blue")["motherLode"];
            if (motherLode.IsValid)
            {
                motherLode.Call("ClearCached");
                Log($"Cleared resource cache.");
            }
        }

        public static UInt64 FrameCount { get; private set; }

        private Int64 EveHWnd => ESCache.Instance.EveAccount.EveHWnd;

        public bool FormNewFleet()
        {
            if (!Session.IsInSpace && !Session.IsInDockableLocation)
            {
                Log("if (!IsInSpace && !IsInDockableLocation)");
                return false;
            }

            if (Session.InFleet)
            {
                Log("FormNewFleet: Session.FleetId is [" + Session.FleetId + "]");
                return true;
            }

            PyObject fleetsvc = GetLocalSvc("fleet");
            if (fleetsvc == null || !fleetsvc.IsValid)
            {
                Log("FormNewFleet: if (fleetsvc == null || !fleetsvc.IsValid)");
                return false;
            }

            Log("FormNewFleet");
            Time.Instance.LastFormFleetAttempt = DateTime.UtcNow;
            if (ThreadedCall(fleetsvc.Attribute("StartNewFleet"), Session.CharacterId))
            {
                Log("FormNewFleet: True");
                return true;
            }

            Log("FormNewFleet: False");
            return false;
        }

        public bool Regroup()
        {
            //Not Implemented!
            return false;
        }

        public bool CreateFleetAdvert()
        {
            if (!Session.IsInSpace && !Session.IsInDockableLocation)
            {
                Log("if (!IsInSpace && !IsInDockableLocation)");
                return false;
            }

            if (!Session.InFleet)
            {
                Log("FormNewFleet: Session.FleetId is [" + Session.FleetId + "]");
                return false;
            }

            PyObject fleetsvc = GetLocalSvc("fleet");
            if (fleetsvc == null || !fleetsvc.IsValid)
            {
                Log("FormNewFleet: if (fleetsvc == null || !fleetsvc.IsValid)");
                return false;
            }

            //if (ThreadedCall(fleetsvc.Attribute("RegisterFleet"), Session.CharacterId))
            //    return true;

            return false;
        }

        public bool OpenRegisterFleetWindow()
        {
            if (!Session.IsInSpace && !Session.IsInDockableLocation)
            {
                Log("if (!IsInSpace && !IsInDockableLocation)");
                return false;
            }

            if (!Session.InFleet)
            {
                Log("FormNewFleet: Session.FleetId is [" + Session.FleetId + "]");
                return false;
            }

            PyObject fleetsvc = GetLocalSvc("fleet");
            if (fleetsvc == null || !fleetsvc.IsValid)
            {
                Log("FormNewFleet: if (fleetsvc == null || !fleetsvc.IsValid)");
                return false;
            }

            if (ThreadedCall(fleetsvc.Attribute("OpenRegisterFleetWindow"), Session.CharacterId))
                return true;

            return false;
        }

        public bool RemoveFleetAdvert()
        {
            if (!Session.IsInSpace && !Session.IsInDockableLocation)
            {
                Log("if (!IsInSpace && !IsInDockableLocation)");
                return false;
            }

            if (!Session.InFleet)
            {
                Log("FormNewFleet: Session.FleetId is [" + Session.FleetId + "]");
                return false;
            }

            PyObject fleetsvc = GetLocalSvc("fleet");
            if (fleetsvc == null || !fleetsvc.IsValid)
            {
                Log("FormNewFleet: if (fleetsvc == null || !fleetsvc.IsValid)");
                return false;
            }

            if (ThreadedCall(fleetsvc.Attribute("UnRegisterFleet"), Session.CharacterId))
                return true;

            return false;
        }

        public bool ApplyToJoinFleetID()
        {
            if (!Session.IsInSpace && !Session.IsInDockableLocation)
            {
                Log("if (!IsInSpace && !IsInDockableLocation)");
                return false;
            }

            if (Session.InFleet)
            {
                Log("ApplyToJoinFleetID: We are in a fleet already! Session.FleetId is [" + Session.FleetId + "]");
                return true;
            }

            if (string.IsNullOrEmpty(ESCache.Instance.EveAccount.LeaderCharacterName))
            {
                Log("ApplyToJoinFleetID: LeaderCharacterName is empty: Do we have IsLeader checked on the toon that is supposed to be the leader?");
                return true;
            }

            if (ESCache.Instance.EveAccount.LeaderFleetID == 0)
            {
                Log("ApplyToJoinFleetID: Waiting for Leader to create the fleet");
                return true;
            }

            PyObject fleetsvc = GetLocalSvc("fleet");
            if (fleetsvc == null || !fleetsvc.IsValid)
            {
                Log("FormNewFleet: if (fleetsvc == null || !fleetsvc.IsValid)");
                return false;
            }

            if (ThreadedCall(fleetsvc.Attribute("AskJoinFleetFromLink"), ESCache.Instance.EveAccount.LeaderFleetID))
                return true;

            return false;
        }

        private List<DirectFleetMember> _fleetMembers = new List<DirectFleetMember>();

        public List<DirectFleetMember> GetFleetMembers
        {
            get
            {
                try
                {
                    if (_fleetMembers != null)
                        return _fleetMembers;
                    //if (!Session.IsReady)
                    //    _fleetMembers = new List<DirectFleetMember>();

                    _fleetMembers = new List<DirectFleetMember>();
                    Dictionary<long, PyObject> pyMembers = GetLocalSvc("fleet").Attribute("members").ToDictionary<long>();
                    if (pyMembers != null && pyMembers.Any())
                    {
                        foreach (KeyValuePair<long, PyObject> pyMember in pyMembers)
                        {
                            _fleetMembers.Add(new DirectFleetMember(this, pyMember.Value));
                            continue;
                        }

                        return _fleetMembers ?? new List<DirectFleetMember>();
                    }

                    return new List<DirectFleetMember>();
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return new List<DirectFleetMember>();
                }
            }
        }

        public List<DirectFleetMember> FleetMembers
        {
            get
            {
                return GetFleetMembers;
            }
        }
        public List<long> GetStationGuests
        {
            get
            {
                if (!Session.IsInDockableLocation)
                    return new List<long>();

                List<long> charIds = new List<long>();
                Dictionary<PyObject, PyObject> pyCharIds = GetLocalSvc("station").Attribute("guests").ToDictionary();
                foreach (KeyValuePair<PyObject, PyObject> pyChar in pyCharIds)
                    charIds.Add((long)pyChar.Key);
                return charIds;
            }
        }

        private bool ServicesLoaded { get; set; }
        private bool HooksLoaded { get; set; }

        private bool _shuttingDown { get; set; }
        private bool _shutDown { get; set; }

        //public bool IsProbeScannerNonDockedWindowOpen => Windows.Any(w => w.Name.Equals("probeScannerWindow"));

        public bool IsDirectionalScannerNonDockedWindowOpen => Windows.Any(w => w.Name.Equals("directionalScannerWindow"));

        public DateTime LastBookmarkAction { get; set; }



        public void SetAudioEnabled()
        {
            bool tempAudioEnabled = GetLocalSvc("station").Attribute("public").Attribute("audio").Call("get", "audioEnabled", 1).ToBool();
            if (!tempAudioEnabled)
            {
                //ExecuteCommand(DirectCmd.CmdToggleAudio);
                GetLocalSvc("station").Attribute("public").Attribute("audio").Call("set", "audioEnabled", true);
            }
        }

        public void SetAudioDisabled()
        {
            bool tempAudioEnabled = GetLocalSvc("station").Attribute("public").Attribute("audio").Call("get", "audioEnabled", 1).ToBool();
            if (!tempAudioEnabled)
            {
                //ExecuteCommand(DirectCmd.CmdToggleAudio);
                GetLocalSvc("station").Attribute("public").Attribute("audio").Call("set", "audioEnabled", false);
            }
        }

        public void SetAutoRejectDuelInvitationsEnabled()
        {
            //GetService('characterSettings').Save(const.autoRejectDuelSettingsKey, str(int(checkbox.checked)))
            bool tempAutoRejectDuelInvitations = GetLocalSvc("characterSettings").Call("Get", "autoRejectDuelInvitations").ToBool();
            if (!tempAutoRejectDuelInvitations)
            {
                //ExecuteCommand(DirectCmd.CmdToggleAudio);
                GetLocalSvc("characterSettings").Call("Save", "autoRejectDuelInvitations", "1");
            }
        }

        public void SetAutoRejectDuelInvitationsDisabled()
        {
            //GetService('characterSettings').Save(const.autoRejectDuelSettingsKey, str(int(checkbox.checked)))
            bool tempAutoRejectDuelInvitations = GetLocalSvc("characterSettings").Call("Get", "autoRejectDuelInvitations").ToBool();
            if (tempAutoRejectDuelInvitations)
            {
                //ExecuteCommand(DirectCmd.CmdToggleAudio);
                GetLocalSvc("characterSettings").Call("Save", "autoRejectDuelInvitations", "0");
            }
        }

        public IEnumerable<DirectUIModule> Weapons => Modules.Where(m => m.IsOnline && m.IsWeapon && m.IsMasterOrIsNotGrouped);

        public List<DirectAgent> ListDirectAgentsTypeDistribution
        {
            get
            {
                try
                {
                    if (_listDirectAgentsTypeDistribution == null || !_listDirectAgentsTypeDistribution.Any())
                        _listDirectAgentsTypeDistribution = GetAllAgents().Select(a => a.Value).Select(a => GetAgentById(a)).Where(i => i.AgentTypeName.Contains("BasicAgent") && i.DivisionName.Contains("Distribution")).ToList();

                    return _listDirectAgentsTypeDistribution;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return new List<DirectAgent>();
                }
            }
        }

        #region IDisposable Members

        /// <summary>
        ///     Dispose of DirectEve
        /// </summary>
        public void Dispose()
        {
            _shuttingDown = true;
            try
            {
                DirectX.Dispose();
                DirectDraw.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            //WindowRecorder.Dispose();
            CoreHookManager.CleanupAll();
            Hooking.Dispose();
            Console.WriteLine($"DirectEve initiating shutdown.");
            var timestamp = DateTime.UtcNow;
            while (!_shutDown && timestamp.AddSeconds(2) > DateTime.UtcNow) SpinWait.SpinUntil(() => false, 1);
            Console.WriteLine($"Onframe handler finished the last frame. Disposing framework.");
            if (_framework != null)
                _framework.Dispose();
            _framework = null;
        }

        #endregion IDisposable Members

        public bool AnyModalWindows()
        {
            PySharp pySharp = PySharp;
            PyObject carbonui = pySharp.Import("carbonui");
            List<PyObject> pyWindows = carbonui.Attribute("uicore").Attribute("uicore").Attribute("registry").Attribute("windows").ToList();
            foreach (PyObject pyWindow in pyWindows)
            {
                if ((bool)pyWindow.Attribute("destroyed"))
                    continue;
                PyObject name = pyWindow.Attribute("name");
                string nameStr = name.IsValid ? name.ToUnicodeString() : string.Empty;
                if (nameStr.Equals("modal") || (bool)pyWindow.Attribute("isModal") || nameStr == "telecom") return true;
            }

            return false;
        }

        /// <summary>
        ///     Set destination without fetching DirectLocation ~ CPU Intensive
        /// </summary>
        /// <param name="locationId"></param>
        /// <returns></returns>
        public bool SetDestination(long locationId)
        {
            return DirectNavigation.SetDestination(locationId, this);
        }

        public int GetDistanceBetweenSolarsystems(int solarsystem1, int solarsystem2)
        {
            return DirectSolarSystem.GetDistanceBetweenSolarsystems(solarsystem1, solarsystem2, this);
        }

        public DirectInvType GetInvType(int typeId)
        {
            return DirectInvType.GetInvType(this, typeId);
        }

        public void CreateFakeTelecomWindow()
        {
            var telecom = PySharp.Import("eve")["client"]["script"]["parklife"]["transmissionMgr"]["Telecom"];
            var inst = PySharp.CreateInstance(telecom);
        }

        /// <summary>
        ///     Refresh the bookmark cache (if needed)
        /// </summary>
        /// <returns></returns>
        public bool RefreshBookmarks()
        {
            return DirectBookmark.RefreshBookmarks(this);
        }

        public bool IsTargetStillValid(long id)
        {
            //if (!Session.IsInSpace)
            //    return false;

            //dynamic ps = PySharp;
            var targetSvc = GetLocalSvc("target");
            var target = targetSvc.Attribute("targetsByID").DictionaryItem(id);
            if (target.IsValid)
                return true;
            //var targets = ps.__builtin__.sm.services["target"].targetsByID.ToDictionary<long>();
            //if (targets.ContainsKey(id))
            //return true;

            return false;
        }

        public bool IsTargetBeingRemoved(long id)
        {
            //if (!Session.IsInSpace)
            //    return true;

            var target = GetLocalSvc("target");
            var targetsBeingRemoved = target.Attribute("deadShipsBeingRemoved"); // set object
            if ((targetsBeingRemoved.IsValid && targetsBeingRemoved.PySet_Contains<long>(id)) || _targetsBeingRemoved.ContainsKey(id))
                return true;

            return false;
        }

        public Dictionary<long, DateTime> GetTargetsBeingRemoved()
        {
            return _targetsBeingRemoved;
        }

        //		private static Dictionary<int, DirectInvType> _invTypes = null;

        /// <summary>
        ///     OnFrame event, use this to do your eve-stuff
        /// </summary>
        public event EventHandler<DirectEveEventArgs> OnFrame;


        /// <summary>
        /// Care this is only using the CallerMemberName to determine if the frame has changed
        /// </summary>
        /// <param name="caller"></param>
        /// <returns></returns>
        public static bool HasFrameChanged([CallerMemberName] string caller = null)
        {
            if (!_hasFrameChangedCallerDictionary.ContainsKey(caller))
            {
                _hasFrameChangedCallerDictionary[caller] = FrameCount;
                return true;
            }
            if (_hasFrameChangedCallerDictionary[caller] == FrameCount)
                return false;
            _hasFrameChangedCallerDictionary[caller] = FrameCount;
            return true;
        }

        public static bool IgnoreCurrentFrameExecution([CallerMemberName] string caller = null)
        {
            if (!_ignoreCurrentFrameExecutionCallerDict.ContainsKey(caller))
            {
                _ignoreCurrentFrameExecutionCallerDict[caller] = FrameCount;
                return true;
            }
            else
            {
                if (_ignoreCurrentFrameExecutionCallerDict[caller] < FrameCount)
                {
                    if (!_ignoreCurrentFrameExecutionCallerDict.Remove(caller))
                    {
                        Console.WriteLine("ERROR: Couldn't remove key from _ignoreCurrentFrameExecutionCallerDict.");
                    }
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        /// <summary>
        ///  Log a message at a given interval (ms_min, ms_max) and once at most each frame.
        /// </summary>
        /// <param name="delayMs"></param>
        /// <param name="delayMsMax"></param>
        /// <param name="message"></param>
        /// <param name="ln"></param>
        /// <param name="caller"></param>
        /// <param name="callerFilePath"></param>
        public static void IntervalLog(int delayMs, int delayMsMax = 0, string message = "", [CallerLineNumber] int ln = 0, [CallerMemberName] string caller = null, [CallerFilePath] string callerFilePath = null)
        {
            if (Interval(delayMs, delayMsMax, ln: ln, caller: caller, callerFilePath: callerFilePath))
            {
                Logging.Log.WriteLine(message, null, caller);
            }
        }

        /// <summary>
        ///     Check to run certain code at a given interval and once at most each frame.
        ///     Limitation: Due the fact the line number is used as part of the reference, this
        ///     method can only be used once per line.
        /// </summary>
        /// <param name="delayMs">The requested interval.</param>
        /// <param name="delayMsMax">If set the interval will be randomized between (delayMs, delayMsMax). Max val is exclusive!</param>
        /// <param name="uniqueName">Instead of the combination of CallerMemberName and CallerLineNumber a unique string can be used. That
        /// way the Interval can be used at multiple locations.</param>
        /// <param name="IgnoreCurrentFrameExec">Ignore current frame exec, e.g. runs the code if the frame counter val is higher than it was called at first.</param>
        /// <param name="ln">Internal use only</param>
        /// <param name="caller">Internal use only</param>
        /// <returns></returns>
        public static bool Interval(int delayMs, int delayMsMax = 0, string uniqueName = null, bool IgnoreCurrentFrameExec = false, [CallerLineNumber] int ln = 0, [CallerMemberName] string caller = null, [CallerFilePath] string callerFilePath = null)
        {
            caller = uniqueName ?? caller + ln.ToString() + callerFilePath;
            if (caller == uniqueName)
                caller = uniqueName + ln.ToString() + callerFilePath;

            if (!HasFrameChanged(caller))
                return false;

            if (IgnoreCurrentFrameExec && IgnoreCurrentFrameExecution(caller))
            {
                return false;
            }

            var now = DateTime.UtcNow;
            var delay = delayMsMax == 0 ? delayMs : _random.Next(delayMs, delayMsMax);

            if (_intervalDict.TryGetValue(caller, out var dt) && dt > now)
                return false;

            _intervalDict[caller] = now.AddMilliseconds(delay);
            return true;
        }

        private static Dictionary<string, DateTime> _intervalDict = new Dictionary<string, DateTime>();
        private static Random _random = new Random();


        private static Dictionary<string, (DateTime, int)> _randomCacheDict = new Dictionary<string, (DateTime, int)>();

        public static int CachedRandom(int min, int max, TimeSpan minCacheDuration, TimeSpan? maxCacheDuration = null, string globalUniqueName = null, string localUniqueName = null, [CallerLineNumber] int ln = 0, [CallerMemberName] string caller = null, [CallerFilePath] string callerFilePath = null)
        {
            caller = globalUniqueName ?? caller + ln.ToString() + callerFilePath + (localUniqueName ?? "");

            var now = DateTime.UtcNow;
            if (_randomCacheDict.TryGetValue(caller, out var cachedValue) && cachedValue.Item1 > now)
            {
                return cachedValue.Item2;
            }

            var cacheDurationMs = maxCacheDuration == null ?  (int)minCacheDuration.TotalMilliseconds : _random.Next((int)minCacheDuration.TotalMilliseconds, (int)maxCacheDuration.Value.TotalMilliseconds);

            var expiry = now.AddMilliseconds(cacheDurationMs);

            var newRandomValue = _random.Next(min, max);

            _randomCacheDict[caller] = (expiry, newRandomValue);

            return newRandomValue;

        }

        public bool IsEffectActivating(DirectUIModule m)
        {
            if (_lastSeenEffectActivating.TryGetValue(m.ItemId, out _))
            {
                if (m.IsActive || m.IsReloadingAmmo || m.IsBeingRepaired || m.IsDeactivating)
                {
                    _lastSeenEffectActivating.Remove(m.ItemId);
                    return false;
                }
                return true;
            }
            return false;
        }

        public void AddEffectTimer(DirectUIModule m)
        {
            _lastSeenEffectActivating[m.ItemId] = DateTime.UtcNow;
        }

        //private bool? _fpsLimit;
        //private bool FpsLimit => _fpsLimit ??= ESCache.Instance.EveAccount.ClientSetting.GlobalMainSetting.FPSLimit;

        private Dictionary<long, DateTime> _lastSeenEffectActivating;

        DirectSceneManager _sceneManager;

        public DirectSceneManager SceneManager => _sceneManager ??= new DirectSceneManager(this);


        public IntPtr? SwapChainPtr;

        private DirectX.DirectX _directX;
        public DirectX.DirectX DirectX => _directX ??= new DirectX.DirectX(this);

        private DirectX.DirectDraw _directDraw;
        public DirectX.DirectDraw DirectDraw => _directDraw ??= new DirectX.DirectDraw(this);

        private DateTime _last500MsTimeStamp;


        //private bool _videoRecordInitialized;
        /**
        private void HandleVideoCapture()
        {
            if (_videoRecordInitialized)
                return;

            _videoRecordInitialized = true;

            if (!ESCache.Instance.EveSetting.RecordingEnabled)
            {
                Debug.WriteLine("Video recording disabled in global settings.");
                return;
            }

            if (!ESCache.Instance.EveAccount.CS.GlobalMainSetting.RecordVideo)
            {
                Debug.WriteLine("Video recording disabled in client settings.");
                return;
            }

            var recordingDirectory = ESCache.Instance.EveSetting.RecordingDirectory;

            if (string.IsNullOrEmpty(recordingDirectory) || !Directory.Exists(recordingDirectory))
            {
                Log($"Video recording directory '{recordingDirectory}' does not exist. Video recording disabled.");
                return;
            }

            var maximumSizeGigabyts = ESCache.Instance.EveSetting.VideoRotationMaximumSizeGB;

            if (maximumSizeGigabyts > 0)
            {
                var files = Directory.GetFiles(recordingDirectory).ToList();
                var totalSizeBytes = files.Sum(file => new FileInfo(file).Length);
                var totalSizeGB = totalSizeBytes / (1024 * 1024 * 1024.0); // Convert to gigabytes

                while (totalSizeGB > maximumSizeGigabyts)
                {
                    string oldestFile = files.OrderBy(file => new FileInfo(file).LastWriteTime).FirstOrDefault();
                    if (oldestFile == null)
                    {
                        Log("No more files to delete. The video recording directory is still above the maximum size limit.");
                        break;
                    }

                    try
                    {
                        var fileInfo = new FileInfo(oldestFile);
                        totalSizeBytes -= fileInfo.Length;
                        totalSizeGB = totalSizeBytes / (1024 * 1024 * 1024.0);
                        File.Delete(oldestFile);
                        Log($"Deleted file: '{oldestFile}'");
                        files.Remove(oldestFile);
                    }
                    catch (Exception ex)
                    {
                        Log($"Error deleting file '{oldestFile}': {ex.Message}");
                        // Handle the exception if needed
                        break;
                    }
                }
            }

            Debug.WriteLine($"Starting the video recording.");
            var windowRecorderSession = ESCache.Instance.DirectEve.StartWindowRecording("Capture");
        }
        **/

        private long WaitMilliSecondsBackgroundFps
        {
            get
            {
                return (800 - eveFrameSt.ElapsedMilliseconds) / Settings.Instance.BackgroundFramesPerSecondLimit;
            }
        }

        private long WaitMilliSecondsForegroundFps
        {
            get
            {
                return (800 - eveFrameSt.ElapsedMilliseconds) / Settings.Instance.FramesPerSecondLimit;
            }
        }

        /// <summary>
        ///     Internal "OnFrame" handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        ///
        private SharedArray<bool> _userInformationGatheredSharedArray = null;
        public SharedArray<bool> ForegroundWindowBoolSharedArray;

        private void FrameworkOnFrame(object sender, EventArgs e)
        {
            if (!_shuttingDown)
            {
                try
                {
                    var directFrameSt = new Stopwatch();
                    directFrameSt.Start();

                    using (var pySharp = new PySharp(true))
                    {
                        if (SwapChainPtr == null)
                        {
                            SwapChainPtr = ((D3DEventArgs)e).SwapChain;
                            Log($"SwapChainPtr [{SwapChainPtr}]");

                        }
                        // Make the link to the instance
                        PySharp = pySharp;

                        // Get current target list
                        //dynamic ps = pySharp;

                        //HandleGRPCTimeouts();
                        //
                        try
                        {

                        }
                        catch (Exception ex)
                        {
                            Log($"Error (Infopanel): {ex}");
                        }

                        var builtin = pySharp.Import("__builtin__");
                        // targetsByID and targeting are now dictionaries
                        List<long> targets = builtin["sm"]["services"]["target"]["targetsByID"].Call("keys").ToList<long>();
                        targets.AddRange(builtin["sm"]["services"]["target"]["targeting"].Call("keys").ToList<long>());
                        List<long> targetsBeingRemoved = builtin["sm"]["services"]["target"]["deadShipsBeingRemoved"].ToList<long>();
                        var now = DateTime.UtcNow;

                        foreach (var t in targetsBeingRemoved)
                        {
                            _targetsBeingRemoved.AddOrUpdate(t, now);
                        }

                        foreach (var eff in GetLocalSvc("godma").Attribute("stateManager").Attribute("activatingEffects").ToList())
                        {
                            var moduleId = eff.GetItemAt(0).ToLong();
                            if (moduleId > 0)
                            {
                                _lastSeenEffectActivating[moduleId] = now;
                            }
                        }

                        foreach (var eff in _lastSeenEffectActivating.ToList())
                        {
                            if (eff.Value.AddSeconds(3) < now)
                            {
                                _lastSeenEffectActivating.Remove(eff.Key);
                            }
                        }

                        // Update currently locked targets
                        targets.ForEach(t => _lastKnownTargets[t] = now);
                        // Remove all targets that have not been locked for 3 seconds
                        foreach (var t in _lastKnownTargets.Keys.ToArray())
                        {
                            if (now.AddSeconds(-3) >= _lastKnownTargets[t])
                                _lastKnownTargets.Remove(t);
                        }

                        _hasFrameChangedCallerDictionary = new Dictionary<string, UInt64>();

                        foreach (var kv in _targetsBeingRemoved.ToArray())
                            if (now.AddSeconds(-10) >= kv.Value)
                                _targetsBeingRemoved.Remove(kv.Key);

                        ////Populate the statistic variables

                        try
                        {
                            if (_enableStatisticsModifying)
                                CheckStatistics();
                        }
                        catch (Exception ex)
                        {
                            var msg = $"Restarting eve with reason [Stats modifying error.].";
                            try
                            {
                                Log(msg);
                                WCFClient.Instance.GetPipeProxy.RemoteLog(msg);
                            }
                            catch (Exception e1)
                            {

                            }
                            finally
                            {
                                if (!ESCache.Instance.InAbyssalDeadspace)
                                    ESCache.Instance.RestartBot(msg);
                            }
                        }

                        directFrameSt.Stop();
                        _lastOnframeTook = directFrameSt.ElapsedMilliseconds;
                        GetLastFrameExecutionDuration = Util.ElapsedMicroSeconds(directFrameSt);
                        try
                        {
                            DirectX.OnFrame();
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine(exception);
                        }

                        try
                        {
                            DirectDraw.OnFrame();
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine(exception);
                        }

                        if (ServicesLoaded && !HooksLoaded && Session.IsReady)
                        {
                            LoadHooks();
                        }

                        if (!ServicesLoaded && Session.IsReady)
                        {
                            //HandleVideoCapture();
                            LoadServices();
                        }
                        else
                        {
                            if (_last500MsTimeStamp.AddMilliseconds(500) < DateTime.UtcNow)
                            {
                                DirectActiveShip.UpdateShieldArmorStrucValues(this);
                                _last500MsTimeStamp = DateTime.UtcNow;
                            }

                            OnFrame?.Invoke(this, new DirectEveEventArgs(GetLastFrameExecutionDuration));
                        }

                        //if (Session.IsReady)
                        //    DirectInvasionManager.OnFrame();
                    }


                    if (eveFrameSt.IsRunning)
                    {
                        if (DebugConfig.DebugFpsLimits) DirectEve.IntervalLog(1000, 1000, "FPSLimits: if (eveFrameSt.IsRunning)");
                        if (Settings.Instance.EnableFpsLimits)
                        {
                            if (DebugConfig.DebugFpsLimits) DirectEve.IntervalLog(1000, 1000, "FPSLimits: if (Settings.Instance.EnableFpsLimits)");
                            //var limitFpsBackground = 800 / Settings.Instance.BackgroundFramesPerSecondLimit;
                            //var limitFpsForeground = 800 / Settings.Instance.FramesPerSecondLimit;

                            if (DebugConfig.DebugFpsLimits) DirectEve.IntervalLog(1000, 1000, "FPSLimits: IsForegroundWindow: ForegroundWindowBoolSharedArray[0] [" + ForegroundWindowBoolSharedArray[0]+ "]");

                            var currentFpsLimit = WaitMilliSecondsBackgroundFps;
                            //if (ESCache.Instance.EveAccount.IsForegroundWindow)
                            bool IsForegroundWindow = ForegroundWindowBoolSharedArray[0];
                            if (IsForegroundWindow)
                            {
                                currentFpsLimit = WaitMilliSecondsForegroundFps;
                                if (DebugConfig.DebugFpsLimits) DirectEve.IntervalLog(1000, 1000, "FPSLimits: IsForegroundWindow: Foreground: limit [" + currentFpsLimit + "ms] ");
                            }
                            else
                            {
                                if (DebugConfig.DebugFpsLimits) DirectEve.IntervalLog(5000, 5000, "FPSLimits: !IsForegroundWindow: Background: limit [" + currentFpsLimit + "ms] ");
                            }

                            //
                            // Process FPS limits here
                            //

                            while (eveFrameSt.ElapsedMilliseconds < currentFpsLimit)
                            {
                                if (DebugConfig.DebugFpsLimits) DirectEve.IntervalLog(1000, 1000, "FPSLimits: while (eveFrameSt.ElapsedMilliseconds [" + eveFrameSt.ElapsedMilliseconds + "] < currentFpsLimit [" + currentFpsLimit + "])");
                                SpinWait.SpinUntil(() => false, 1);
                            }
                        }

                        eveFrameSt.Stop();
                    }

                    eveFrameSt.Restart();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("DirectEve Exception:" + ex);
                    Debug.WriteLine("DirectEve Exception:" + ex.ToString());
                }
                finally
                {
                	InvalidateCache();
                }
            }
            else
            {
                _shutDown = true;
            }
        }

        private void InvalidateCache()
        {
            // Clear any cache that we had during this frame
            _localSvcCache.Clear();
            _entitiesById = null;
            _windows = null;
            _modules = null;
            _const = null;
            if (DateTime.UtcNow > Time.Instance.LastRefreshBookmarksTimeStamp.AddMinutes(1)) _bookmarks = null;
            _agentMissions = null;
            _dwm = null;
            DirectEntity._listOfEntitiesThatDontMove = null;

            _containers.Clear();
            _fleetMembers = null;
            _itemHangar = null;
            _marketWindow = null;
            _shipHangar = null;
            _agencyWindow = null;
            _shipsAmmoHold = null;
            _shipsCargo = null;
            _shipsOreHold = null;
            _shipsMineralHold = null;
            _shipsGeneralMiningHold = null;
            _shipsModules = null;
            _shipsDroneBay = null;
            _shipsFleetHangar = null;
            _listGlobalAssets = null;
            _modalWindows = null;
            _bookmarkFolders = null;
            _sceneManager = null;
            _fittingWindow = null;
            _me = null;
            _activeShip = null;
            _standings = null;
            _navigation = null;
            _session = null;
            _characterCreation = null;
            _login = null;
            _layers = null;
            _skills = null;
            // Remove the link
            PySharp = null;
            FrameCount++;
        }

        private void LoadServices()
        {
            foreach (var svc in _servicesToLoad)
            {
                if (!IsServiceRunning(svc)) // start the service if it's not running already
                {
                    GetLocalSvc(svc);
                    return;
                }

                if (IsServiceRunning(svc)) // additional startup requirements for different services
                    switch (svc)
                    {
                        case "agents":

                            if (!IsAgentsByIdDictionaryPopulated())
                            {
                                if (!_servicesAdditionalRequirementsCallOnlyOnce.Any(k => k.Equals(nameof(PopulateAgentsByIdDictionary))))
                                {
                                    _servicesAdditionalRequirementsCallOnlyOnce.Add(nameof(PopulateAgentsByIdDictionary));
                                    PopulateAgentsByIdDictionary();
                                }

                                return;
                            }

                            break;

                        default:
                            break;
                    }
            }

            ServicesLoaded = true;
        }

        private void LoadHooks()
        {
            try
            {
                CoreHookManager.RegisterDefaults();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex}");
            }
            HooksLoaded = true;
        }

        /// <summary>
        ///     Open the corporation hangar
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        ///     Only works in a station!
        /// </remarks>
        private bool OpenCorporationHangar()
        {
            return ExecuteCommand(DirectCmd.OpenCorpHangar);
        }

        public bool OpenInventory()
        {
            return ExecuteCommand(DirectCmd.OpenInventory);
        }

        public long GetBallParkCount()
        {
            if (!HasFrameChanged())
                return _ballParkCount;

            _ballParkCount = DirectEntity.GetBallparkCount(this);
            return _ballParkCount;
        }

        public bool AnyEntities()
        {
            return GetBallParkCount() != 0;
        }

        public int GetCorpHangarId(string divisionName)
        {
            var divisions = GetLocalSvc("corp").Call("GetDivisionNames");
            for (var i = 0; i < 7; i++)
                if (string.Compare(divisionName, (string)divisions.DictionaryItem(i), true) == 0)
                    return i;
            return -1;
        }

        public bool OpenCorpHangarArray(long itemID)
        {
            return ThreadedLocalSvcCall("menu", "OpenCorpHangarArray", itemID, PySharp.PyNone);
        }

        public bool OpenShipMaintenanceBay(long itemID)
        {
            var OpenShipMaintenanceBayShip = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.openFunctions")
                .Attribute("OpenShipMaintenanceBayShip");
            return ThreadedCall(OpenShipMaintenanceBayShip, itemID, PySharp.PyNone);
        }

        public bool OpenStructure(long itemID)
        {
            return ThreadedLocalSvcCall("menu", "OpenStructure", itemID, PySharp.PyNone);
        }

        public bool OpenStructureCharges(long itemID, bool hasCapacity)
        {
            return ThreadedLocalSvcCall("menu", "OpenStructureCharges", itemID, PySharp.PyNone, hasCapacity);
        }

        public bool OpenStructureCargo(long itemID)
        {
            return ThreadedLocalSvcCall("menu", "OpenStructureCargo", itemID, PySharp.PyNone);
        }

        public bool OpenStrontiumBay(long itemID)
        {
            return ThreadedLocalSvcCall("menu", "OpenStrontiumBay", itemID, PySharp.PyNone);
        }

        /// <summary>
        ///     Execute a command
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public bool ExecuteCommand(DirectCmd cmd)
        {
            Tuple<int, int> throttle;

            if (cmd == DirectCmd.CmdZoomOut && Session.IsAbyssalDeadspace)
            {
                return false;
            }

            if (cmd == DirectCmd.CmdZoomOut && Session.IsVoidSpace)
            {
                return false;
            }

            if (cmd == DirectCmd.CmdStopShip && ActiveShip.IsImmobile)
            {
                return false;
            }

            if (cmd == DirectCmd.CmdAccelerate && ActiveShip.IsImmobile)
            {
                return false;
            }

            if (cmd == DirectCmd.CmdDockOrJumpOrActivateGate && ActiveShip.IsImmobile || Me.WeaponsTimerExists)
            {
                return false;
            }

            if (cmd == DirectCmd.CmdAlignToItem && ActiveShip.IsImmobile)
            {
                return false;
            }

            if (cmd == DirectCmd.CmdApproachItem && ActiveShip.IsImmobile)
            {
                return false;
            }

            switch (cmd)
            {
                case DirectCmd.CmdZoomOut:
                    if (!Session.IsInSpace)
                        return false;

                    throttle = new Tuple<int, int>(500, 600);
                    break;

                case DirectCmd.CmdDronesReturnToBay:
                    if (!Session.IsInSpace)
                        return false;

                    throttle = new Tuple<int, int>(7000, 10000);
                    break;

                case DirectCmd.CmdStopShip:
                    if (!Session.IsInSpace)
                        return false;

                    throttle = new Tuple<int, int>(800, 1500);
                    break;

                case DirectCmd.OpenJournal:
                    if (!Session.IsInSpace && !Session.IsInDockableLocation)
                        return false;

                    Log("Calling DirectCmd [" + cmd + "]");
                    throttle = new Tuple<int, int>(1500, 2000);
                    break;

                case DirectCmd.CmdExitStation:
                    if (!Session.IsInDockableLocation)
                        return false;

                    Log("Calling DirectCmd [" + cmd + "]");
                    throttle = new Tuple<int, int>(1500, 2000);
                    break;

                case DirectCmd.OpenShipHangar:
                case DirectCmd.OpenHangarFloor:
                case DirectCmd.OpenCorpHangar:
                case DirectCmd.OpenRepairShop:
                    if (!Session.IsInDockableLocation)
                        return false;

                    Log("Calling DirectCmd [" + cmd + "]");
                    throttle = new Tuple<int, int>(4000, 5000);
                    break;

                case DirectCmd.OpenAgencyNew:
                case DirectCmd.OpenCargoHoldOfActiveShip:
                case DirectCmd.OpenDroneBayOfActiveShip:
                case DirectCmd.OpenFleetHangarOfActiveShip:
                case DirectCmd.OpenLoginRewardWindow:
                case DirectCmd.OpenMarket:
                case DirectCmd.OpenGeneralMiningHoldOfActiveShip:
                case DirectCmd.ToggleRedeemItems:
                    if (!Session.IsInSpace && !Session.IsInDockableLocation)
                        return false;

                    Log("Calling DirectCmd [" + cmd + "]");
                    throttle = new Tuple<int, int>(5000, 6000);
                    break;

                case DirectCmd.CmdDronesReturnAndOrbit:
                    if (!Session.IsInSpace)
                        return false;

                    Log("Calling DirectCmd [" + cmd + "]");
                    throttle = new Tuple<int, int>(2500, 3500);
                    break;

                case DirectCmd.CmdDronesEngage:
                    if (!Session.IsInSpace)
                        return false;

                    Log("Calling DirectCmd [" + cmd + "]");
                    throttle = new Tuple<int, int>(1500, 2000);
                    break;

                case DirectCmd.CmdRefreshProbeScan:
                    if (!Session.IsInSpace)
                        return false;

                    Log("Calling DirectCmd [" + cmd + "]");
                    //int delay1 = 6200;
                    //int delay2 = 7000;
                    //
                    // ToDo: Add setting for this delay to be custom?
                    //
                    throttle = new Tuple<int, int>(1500, 2000);
                    //throttle = new Tuple<int, int>(delay1, delay2);
                    break;

                case DirectCmd.CmdReconnectToDrones:
                case DirectCmd.CmdLaunchFavoriteDrones:
                    if (!Session.IsInSpace)
                        return false;

                    Log("Calling DirectCmd [" + cmd + "]");
                    throttle = new Tuple<int, int>(8000, 12000);
                    break;

                case DirectCmd.CmdReloadAmmo:
                    if (!Session.IsInSpace)
                        return false;

                    throttle = new Tuple<int, int>(25000, 27000);
                    break;

                case DirectCmd.OpenWallet:
                    if (!Session.IsInSpace && !Session.IsInDockableLocation)
                        return false;

                    throttle = new Tuple<int, int>(3000, 4000);
                    break;

                case DirectCmd.CmdQuitGame:
                    throttle = new Tuple<int, int>(10000, 15000);
                    break;

                default:
                    Log("Calling DirectCmd [" + cmd + "]");
                    throttle = new Tuple<int, int>(1000, 1500);
                    break;
            }

            if (_nextDirectCmdExec.TryGetValue(cmd, out var dt) && dt > DateTime.UtcNow)
            {
                Log($"Calling DirectCmd [{cmd}] too frequently!");
                return false;
            }

            _nextDirectCmdExec[cmd] = DateTime.UtcNow.AddMilliseconds(new Random().Next(throttle.Item1, throttle.Item2));

            switch (cmd)
            {
                case DirectCmd.CmdExitStation:
                    var lobby = Windows.OfType<DirectLobbyWindow>().FirstOrDefault();
                    if (lobby == null)
                    {
                        Log($"Error: Lobby window is null? Can't undock.");
                        return false;
                    }
                    DWM.ActivateWindow(typeof(DirectLobbyWindow), true, true);
                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.UNDOCK, "Undocking from station."));
                    DirectSession.SetSessionNextSessionReady(5500, 6500);
                    break;

                case DirectCmd.CmdQuitGame:
                    Log("Calling DirectCmd [" + cmd + "]");
                    //ESCache.Instance.LastCmdQuitGame = DateTime.UtcNow;
                    break;

                default:
                    Log("Calling DirectCmd [" + cmd + "]");
                    break;
            }

            ESCache.Instance.LastInteractedWithEVE = DateTime.UtcNow;
            return ThreadedLocalSvcCall("cmd", cmd.ToString());
        }

        /// <summary>
        ///     Return a list of locked items
        /// </summary>
        /// <returns></returns>
        public List<long> GetLockedItems()
        {
            var locks = GetLocalSvc("invCache").Attribute("lockedItems").ToDictionary<long>();
            return locks.Keys.ToList();
        }

        /// <summary>
        ///     Remove all item locks
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        ///     Do not abuse this, the client probably placed them for a reason!
        /// </remarks>
        public bool UnlockItems()
        {
            return GetLocalSvc("invCache").Attribute("lockedItems").Clear();
        }

        /// <summary>
        ///     Item hangar container
        /// </summary>
        /// <returns></returns>
        public DirectContainer GetItemHangar()
        {
            if ((bool)!Session.IsInDockableLocation || Session.IsInSpace)
                return null;

            if (_itemHangar == null)
            {
                DirectContainer itemHangar = DirectContainer.GetItemHangar(this);

                if (!itemHangar.IsValid)
                {
                    if (DebugConfig.DebugArm) Log("if (!itemHangar.IsValid)");
                    return null;
                }

                if (itemHangar.Window == null)
                {
                    if (DebugConfig.DebugArm) Log("if (itemHangar.Window == null)");
                    if (DirectEve.Interval(1000))
                        ExecuteCommand(DirectCmd.OpenHangarFloor);

                    return null;
                }

                if (!itemHangar.IsReady)
                {
                    if (DebugConfig.DebugArm) Log("if (!itemHangar.IsReady)");
                    return null;
                }

                _itemHangar = itemHangar;
            }

            return _itemHangar;
        }

        public DirectFittingManagerWindow FittingManagerWindow
        {
            get
            {
                if ((bool)!Session.IsInDockableLocation || Session.IsInSpace)
                    return null;

                DirectFittingManagerWindow fittingManagerWindow = Windows.OfType<DirectFittingManagerWindow>().FirstOrDefault();

                if (fittingManagerWindow == null)
                {
                    if (DirectEve.Interval(1000))
                        OpenFittingManager();

                    return null;
                }
                if (!fittingManagerWindow.IsReady)
                    return null;

                return fittingManagerWindow;
            }
        }


        /// <summary>
        ///     Ship hangar container
        /// </summary>
        /// <returns></returns>
        public DirectContainer GetShipHangar()
        {
            if ((bool)!Session.IsInDockableLocation || Session.IsInSpace)
                return null;

            if (_shipHangar == null)
            {
                if (DebugConfig.DebugArm) Log("if (_shipHangar == null)");
                DirectContainer shipHangar = DirectContainer.GetShipHangar(this);
                if (!shipHangar.IsValid)
                {
                    if (DebugConfig.DebugHangars) Log("if (!shipHangar.IsValid)");
                    return null;
                }

                if (shipHangar.Window == null)
                {
                    if (DebugConfig.DebugHangars) Log("if (shipHangar.Window == null)");
                    if (DirectEve.Interval(1000))
                        ExecuteCommand(DirectCmd.OpenShipHangar);

                    return null;
                }

                if (!shipHangar.IsReady)
                {
                    if (DebugConfig.DebugHangars) Log("if (!shipHangar.IsReady)");
                    return null;
                }

                if (DebugConfig.DebugHangars) Log("_shipHangar = shipHangar;");
                _shipHangar = shipHangar;
            }

            if (DebugConfig.DebugHangars) Log("return _shipHangar;");
            return _shipHangar;
        }

        public bool IsProbeScannerWindowOpen => Windows.Any(w => w.Name.Equals("probeScannerWindow"));

        // Broadcasts are only available for X seconds after the broadcast was made
        public Dictionary<long, List<long>> GetTargetBroadcasts()
        {
            var ret = new Dictionary<long, List<long>>();
            var dict = GetLocalSvc("fleet").Attribute("targetBroadcasts").ToDictionary<long>();
            foreach (var kv in dict)
            {
                var list = kv.Value.ToList<long>();
                ret[kv.Key] = list;
            }
            return ret;
        }

        public bool IsDirectionalScannerWindowOpen => Windows.Any(w => w.Name.Equals("directionalScannerWindow"));

        /// <summary>
        ///     Ship's cargo container
        /// </summary>
        /// <returns></returns>
        public DirectContainer GetShipsCargo()
        {
            if (_shipsCargo == null)
            {
                DirectContainer shipsCargo = DirectContainer.GetShipsCargo(this);
                if (shipsCargo == null) return null;

                if (!shipsCargo.IsValid)
                    return null;
                if (shipsCargo.Window == null)
                {
                    if (DirectEve.Interval(1000))
                        ExecuteCommand(DirectCmd.OpenCargoHoldOfActiveShip);

                    return null;
                }
                if (!shipsCargo.IsReady)
                    return null;

                _shipsCargo = shipsCargo;
            }

            return _shipsCargo;
        }

        private DirectFittingWindow _fittingWindow;

        private DirectTheAgencyWindow _agencyWindow;

        public DirectTheAgencyWindow GetAgencyWindow()
        {
            if (_agencyWindow == null)
            {
                var wnd = Windows.OfType<DirectTheAgencyWindow>().FirstOrDefault();
                if (wnd == null || !wnd.PyWindow.IsValid)
                {
                    if (DirectEve.Interval(1000))
                        ExecuteCommand(DirectCmd.OpenTriglavianInvasionInAgency);

                    return null;
                }
                _agencyWindow = wnd;
            }

            return _agencyWindow;
        }


        public DirectFittingWindow GetFittingWindow(bool openSimulated = false)
        {
            if (_fittingWindow == null)
            {
                var wnd = Windows.OfType<DirectFittingWindow>().FirstOrDefault();
                if (wnd == null || !wnd.PyWindow.IsValid)
                {
                    if (!openSimulated)
                    {
                        if (DirectEve.Interval(1000))
                            ExecuteCommand(DirectCmd.OpenFitting);
                    }
                    else
                    {
                        if (DirectEve.Interval(1000))
                            Me.SimulateCurrentFit();
                    }
                    return null;
                }
                _fittingWindow = wnd;
            }

            return _fittingWindow;
        }

        /// <summary>
        /// Ship's Ammo hold container: This is the AmmoHold for special haulers!
        /// </summary>
        /// <returns></returns>
        public DirectContainer GetShipsAmmoHold()
        {
            if (_shipsAmmoHold == null)
            {
                if (!ActiveShip.HasAmmoHold)
                    return null;

                DirectContainer shipsAmmoHold = DirectContainer.GetShipsMineralHold(this);
                if (shipsAmmoHold == null)
                {
                    if (DebugConfig.DebugMiningBehavior) Log("if (shipsAmmoHold == null)");
                    return null;
                }

                if (!shipsAmmoHold.IsValid)
                {
                    if (DebugConfig.DebugMiningBehavior) Log("if (!shipsAmmoHold.IsValid)");
                    return null;
                }

                if (shipsAmmoHold.Window == null)
                {
                    if (DebugConfig.DebugMiningBehavior) Log("if (shipsAmmoHold.Window == null)");
                    if (DirectEve.Interval(1000))
                        ExecuteCommand(DirectCmd.OpenMineralHoldOfActiveShip);

                    return null;
                }

                if (!_shipsAmmoHold.IsReady)
                {
                    if (DebugConfig.DebugMiningBehavior) Log("if (!shipsAmmoHold.IsReady)");
                    return null;
                }

                if (DebugConfig.DebugMiningBehavior) Log("if (shipsAmmoHold.IsReady) !!!");
                _shipsAmmoHold = shipsAmmoHold;
            }

            return _shipsMineralHold;
        }

        /// <summary>
        ///     Ship's ore hold container: this is NOT the General Mining Hold that for instance a Hulk has. This is the OreHold for special haulers!
        /// </summary>
        /// <returns></returns>
        public DirectContainer GetShipsOreHold()
        {
            if (_shipsOreHold == null)
            {
                if (!ActiveShip.HasOreHold)
                    return null;

                DirectContainer shipsOreHold = null; //DirectContainer.GetShipsOreHold(this);
                if (shipsOreHold == null)
                {
                    if (DebugConfig.DebugMiningBehavior) Log("if (shipsOreHold == null)");
                    return null;
                }

                if (!shipsOreHold.IsValid)
                {
                    if (DebugConfig.DebugMiningBehavior) Log("if (!shipsOreHold.IsValid)");
                    return null;
                }

                if (shipsOreHold.Window == null)
                {
                    if (DebugConfig.DebugMiningBehavior) Log("if (shipsOreHold.Window == null)");
                    //if (DirectEve.Interval(1000))
                    //    ExecuteCommand(DirectCmd.OpenOreHoldOfActiveShip);

                    return null;
                }

                if (!shipsOreHold.IsReady)
                {
                    if (DebugConfig.DebugMiningBehavior) Log("if (!shipsOreHold.IsReady)");
                    return null;
                }

                if (DebugConfig.DebugMiningBehavior) Log("if (shipsOreHold.IsReady) !!!");
                _shipsOreHold = shipsOreHold;
            }

            return _shipsOreHold;
        }

        /// <summary>
        /// Ship's Mineral hold container: this is NOT the General Mining Hold that for instance a Hulk has. This is the OreHold for special haulers!
        /// </summary>
        /// <returns></returns>
        public DirectContainer GetShipsMineralHold()
        {
            if (_shipsMineralHold == null)
            {
                if (!ActiveShip.HasMineralHold)
                    return null;

                DirectContainer shipsMineralHold = DirectContainer.GetShipsMineralHold(this);
                if (shipsMineralHold == null)
                {
                    if (DebugConfig.DebugMiningBehavior) Log("if (shipsMineralHold == null)");
                    return null;
                }

                if (!shipsMineralHold.IsValid)
                {
                    if (DebugConfig.DebugMiningBehavior) Log("if (!shipsMineralHold.IsValid)");
                    return null;
                }

                if (shipsMineralHold.Window == null)
                {
                    if (DebugConfig.DebugMiningBehavior) Log("if (shipsMineralHold.Window == null)");
                    if (DirectEve.Interval(1000))
                        ExecuteCommand(DirectCmd.OpenMineralHoldOfActiveShip);

                    return null;
                }

                if (!shipsMineralHold.IsReady)
                {
                    if (DebugConfig.DebugMiningBehavior) Log("if (!shipsMineralHold.IsReady)");
                    return null;
                }

                if (DebugConfig.DebugMiningBehavior) Log("if (shipsMineralHold.IsReady) !!!");
                _shipsMineralHold = shipsMineralHold;
            }

            return _shipsMineralHold;
        }

        /// <summary>
        ///     Assets list
        /// </summary>
        /// <returns></returns>
        public List<DirectItem> GetAssets()
        {
            if (DateTime.UtcNow > Time.Instance.LastAssetRefresh.AddMinutes(8))
            {
                if (DirectEve.Interval(15000))
                {
                    RefreshAssets();
                    Log("Asset list might be stale: refreshing: it might be better to note when we are moving items and only refresh if the bot moved something? ");
                }

                return new List<DirectItem>();
            }

            if (_listGlobalAssets == null)
            {
                _listGlobalAssets = new List<DirectItem>();
                Dictionary<long, PyObject> pyItemDict = GetLocalSvc("invCache").Attribute("containerGlobal").Attribute("cachedItems").ToDictionary<long>();
                foreach (KeyValuePair<long, PyObject> pyItem in pyItemDict)
                {
                    DirectItem item = new DirectItem(this)
                    {
                        PyItem = pyItem.Value
                    };

                    _listGlobalAssets.Add(item);
                }
            }

            return _listGlobalAssets;
        }

        /// <summary>
        ///     Refresh global assets list (note: 5min delay in assets)
        /// </summary>
        /// <returns></returns>
        public bool RefreshAssets()
        {
            if (Time.Instance.LastAssetRefresh.AddMinutes(5) > DateTime.UtcNow)
            {
                if (DebugConfig.DebugAssets) Log("if (Time.Instance.LastAssetRefresh.AddMinutes(5) > DateTime.UtcNow)");
                return true;
            }

            if (ThreadedCall(GetLocalSvc("invCache").Call("GetInventory", Const.ContainerGlobal).Attribute("List")))
            {
                Time.Instance.LastAssetRefresh = DateTime.UtcNow;
                return true;
            }

            return false;
        }

        public DirectMarketWindow GetMarketWindow()
        {
            if (_marketWindow == null)
            {
                DirectMarketWindow marketWindow = Windows.OfType<DirectMarketWindow>().FirstOrDefault();

                if (marketWindow == null)
                {
                    if (DirectEve.Interval(1000))
                        ExecuteCommand(DirectCmd.OpenMarket);

                    return null;
                }

                if (!marketWindow.IsReady)
                {
                    if (DebugConfig.DebugArm || DebugConfig.DebugMarketOrders) Log("if (!marketWindow.IsReady)");
                    return null;
                }

                _marketWindow = marketWindow;
            }

            return _marketWindow;
        }

        /// <summary>
        ///     Ship's modules container
        /// </summary>
        /// <returns></returns>
        public DirectContainer GetShipsModules()
        {
            return _shipsModules ?? (_shipsModules = DirectContainer.GetShipsModules(this));
        }

        /// <summary>
        ///     Ship's drone bay
        /// </summary>
        /// <returns></returns>
        public DirectContainer GetShipsDroneBay()
        {
            if (_shipsDroneBay == null)
            {
                if (ActiveShip.GroupId == (int)Group.Capsule)
                {
                    if (DebugConfig.DebugArm && DirectEve.Interval(30000)) Log("IsShipWithNoDroneBay: if (ActiveShip.Entity.GroupId == (int)Group.Capsule)");
                    return null;
                }

                if (ActiveShip.GroupId == (int)Group.Dreadnaught)
                {
                    if (DebugConfig.DebugArm && DirectEve.Interval(30000)) Log("IsShipWithNoDroneBay: if (ActiveShip.Entity.GroupId == (int)Group.Dreadnaught)");
                    return null;
                }

                if (Settings.Instance.TransportShipName.ToLower() == ESCache.Instance.ActiveShip.GivenName.ToLower() && Settings.Instance.TransportShipName != Combat.CombatShipName)
                {
                    if (DebugConfig.DebugArm && DirectEve.Interval(30000)) Log("IsShipWithNoDroneBay: assume our transportship has no drones (bad assumption?) TransportShipName [" + Settings.Instance.TransportShipName + "]");
                    return null;
                }

                if (ESCache.Instance.ActiveShip.DroneBandwidth == 0)
                {
                    if (DebugConfig.DebugArm && DirectEve.Interval(30000)) Log("IsShipWithNoDroneBay: if (ESCache.Instance.ActiveShip.DroneBandwidth == 0)");
                    return null;
                }

                DirectContainer shipsDroneBay = DirectContainer.GetShipsDroneBay(this);

                if (shipsDroneBay == null)
                {
                    if (DebugConfig.DebugArm && DirectEve.Interval(30000)) Log("IsShipWithNoDroneBay: if (shipsDroneBay == null)!");
                    return null;
                }

                if (!shipsDroneBay.IsValid)
                {
                    if (DebugConfig.DebugArm && DirectEve.Interval(30000)) Log("if (!shipsDroneBay.IsValid)");
                    return null;
                }

                if (shipsDroneBay.Window == null)
                {
                    if (DirectEve.Interval(1000))
                        ExecuteCommand(DirectCmd.OpenDroneBayOfActiveShip);

                    if (DebugConfig.DebugArm) Log("if (shipsDroneBay.Window == null) ExecuteCommand(DirectCmd.OpenDroneBayOfActiveShip);");
                    return null;
                }

                if (!shipsDroneBay.IsReady)
                {
                    if (DebugConfig.DebugArm) Log("if (!shipsDroneBay.IsReady)");
                    return null;
                }

                _shipsDroneBay = shipsDroneBay;
            }

            return _shipsDroneBay;
        }

        /// <summary>
        ///     Ship's drone bay
        /// </summary>
        /// <returns></returns>
        public DirectContainer GetShipsFleetHangar()
        {
            if (_shipsFleetHangar == null)
            {
                if (!ActiveShip.HasFleetHangar)
                    return null;

                DirectContainer shipsFleetHangar = DirectContainer.GetShipsFleetHangar(this);

                if (shipsFleetHangar == null) return null;

                if (!shipsFleetHangar.IsValid)
                    return null;

                if (shipsFleetHangar.Window == null)
                {
                    if (DirectEve.Interval(1000))
                        ExecuteCommand(DirectCmd.OpenFleetHangarOfActiveShip);

                    return null;
                }

                if (!shipsFleetHangar.IsReady)
                    return null;

                _shipsFleetHangar = shipsFleetHangar;
            }

            return _shipsFleetHangar;
        }

        public DirectContainer GetShipsGeneralMiningHold()
        {
            if (_shipsGeneralMiningHold == null)
            {
                if (!ActiveShip.HasGeneralMiningHold)
                    return null;

                DirectContainer shipsGeneralMiningHold = DirectContainer.GetShipsGeneralMiningHold(this);

                if (shipsGeneralMiningHold == null) return null;

                if (!shipsGeneralMiningHold.IsValid)
                    return null;

                if (shipsGeneralMiningHold.Window == null)
                {
                    if (DirectEve.Interval(1000))
                        ExecuteCommand(DirectCmd.OpenGeneralMiningHoldOfActiveShip);

                    return null;
                }

                if (!shipsGeneralMiningHold.IsReady)
                    return null;

                _shipsGeneralMiningHold = shipsGeneralMiningHold;
            }

            return _shipsGeneralMiningHold;
        }

        /// <summary>
        ///     Item container
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public DirectContainer GetContainer(long itemId)
        {
            if (!_containers.ContainsKey(itemId))
                _containers[itemId] = DirectContainer.GetContainer(this, itemId);

            if (_containers[itemId] == null) return null;

            return _containers[itemId];
        }

        /// <summary>
        ///     Get the corporation hangar container based on division name
        /// </summary>
        /// <param name="divisionName"></param>
        /// <returns></returns>
        public DirectContainer GetCorporationHangar(string divisionName)
        {
            return DirectContainer.GetCorporationHangar(this, divisionName);
        }

        public bool DoesCorpHangarExistHere
        {
            get
            {
                if (Session.IsInSpace) return false;
                if (!Session.IsInDockableLocation) return false;
                if (Me.CorpName == "Viziam") return false; //Amarr
                if (Me.CorpName == "Ministry of War") return false; //Amarr
                if (Me.CorpName == "Imperial Shipment ") return false; //Amarr
                if (Me.CorpName == "Perkone") return false; //Caldari
                if (Me.CorpName == "Caldari Provisions") return false; //Caldari
                if (Me.CorpName == "Deep Core Mining Inc.") return false; //Caldari
                if (Me.CorpName == "The Scope") return false; //Gallente
                if (Me.CorpName == "Aliastra") return false; //Gallente
                if (Me.CorpName == "Garoun Investment Bank") return false; //Gallente
                if (Me.CorpName == "Brutor Tribe") return false; //Minmatar
                if (Me.CorpName == "Sebiestor Tribe") return false; //Minmatar
                if (Me.CorpName == "Native Freshfood") return false; //Minmatar

                long itemId = 0;
                try
                {
                    itemId = (long)GetLocalSvc("officeManager").Call("GetCorpOfficeAtLocation").Attribute("officeID");
                    if (itemId != 0) return true;
                    return false;
                }
                catch (Exception ex)
                {
                    Logging.Log.WriteLine("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        /// <summary>
        ///     Get the corporation hangar container based on division id (1-7)
        /// </summary>
        /// <param name="divisionId"></param>
        /// <returns></returns>
        public DirectContainer GetCorporationHangar(int divisionId = 1)
        {
            return DirectContainer.GetCorporationHangar(this, divisionId);
        }

        public DirectContainer GetCorporationHangarArray(long itemId, string divisionName)
        {
            return DirectContainer.GetCorporationHangarArray(this, itemId, divisionName);
        }

        public DirectContainer GetCorporationHangarArray(long itemId, int divisionId)
        {
            return DirectContainer.GetCorporationHangarArray(this, itemId, divisionId);
        }

        /// <summary>
        ///     Return the entity by it's id
        /// </summary>
        /// <param name="entityId"></param>
        /// <returns></returns>
        public DirectEntity GetEntityById(long entityId)
        {
            if (entityId == -1)
                return null;

            if (EntitiesById.TryGetValue(entityId, out var entity))
                return entity;

            return null;
        }


        /// <summary>
        ///     Bookmark the current location
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="name"></param>
        /// <param name="comment"></param>
        /// <param name="folderId"></param>
        /// <returns></returns>
        internal bool BookmarkCurrentLocation(string name, long? folderId = null, string comment = "")
        {
            if (name == null)
            {
                name = Me?.CurrentSolarSystem?.Name;
                if (name == null)
                    name = new Random().Next(10, 99).ToString();
                else
                    name = $"spot in {name} solar system";
            }

            if (folderId == null)
            {
                var activeFolders = BookmarkFolders.Where(f => f.IsActive).OrderByDescending(e => e.IsPersonal).ThenByDescending(e => e.Name == Session.Character.Name);
                if (activeFolders.Any())
                {
                    var firstActiveFolder = activeFolders.FirstOrDefault();
                    folderId = firstActiveFolder.Id;
                }
            }

            if (folderId == null)
                return false;

            if (Session.StationId.HasValue)
            {
                var station = GetLocalSvc("station").Attribute("station");
                if (!station.IsValid)
                    return false;

                var itemId = (long)station.Attribute("stationID");
                var typeId = (int)station.Attribute("stationTypeID");

                if (Stations.TryGetValue(Session.StationId.Value, out var st))
                {
                    name = st.Name;
                }

                return DirectBookmark.BookmarkLocation(this, itemId, folderId.Value, name, typeId);
            }

            if (ActiveShip.Entity != null && ActiveShip.Entity.IsValid && Session.SolarSystemId.HasValue)
            {
                var itemId = ActiveShip.Entity.Id;
                var typeId = ActiveShip.Entity.TypeId;
                return DirectBookmark.BookmarkLocation(this, itemId, folderId.Value, name, typeId);
            }

            return false;
        }

        /// <summary>
        ///     Bookmark an entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="name"></param>
        /// <param name="comment"></param>
        /// <param name="folderId"></param>
        /// <param name="corp"></param>
        /// <returns></returns>
        public bool BookmarkEntity(DirectEntity entity, string name, long? folderId, string comment = "")
        {
            if (!entity.IsValid)
                return false;

            if (Session.CharacterId == null)
                return false;

            if (Session.CorporationId == null)
                return false;

            if (folderId == null)
            {
                var activeFolders = BookmarkFolders.Where(f => f.IsActive);
                if (activeFolders.Any())
                {
                    var firstActiveFolder = activeFolders.FirstOrDefault();
                    folderId = firstActiveFolder.Id;
                }
            }

            if (folderId == null)
                return false;

            return DirectBookmark.BookmarkLocation(this, entity.Id, folderId.Value, name, entity.TypeId, 0, comment);
        }


        /// <summary>
        ///     Create a bookmark folder
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool CreatePersonalBookmarkFolder(string name, string description = "")
        {
            return DirectBookmark.CreatePersonalBookmarkFolder(this, name, description);
        }

        /// <summary>
        ///     Drop bookmarks into people &amp; places
        /// </summary>
        /// <param name="bookmarks"></param>
        /// <returns></returns>
        public bool DropInPeopleAndPlaces(IEnumerable<DirectItem> bookmarks)
        {
            return DirectItem.DropInPlaces(this, bookmarks);
        }

        /// <summary>
        ///     Refine items from the hangar floor
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public bool ReprocessStationItems(IEnumerable<DirectItem> items)
        {
            if (items == null)
                return false;

            if (items.Any(i => !i.PyItem.IsValid))
                return false;

            if ((bool)!Session.IsInDockableLocation)
                return false;

            if (items.Any(i => i.LocationId != Session.StationId))
                return false;

            var Refine = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.invItemFunctions").Attribute("Refine");
            return ThreadedCall(Refine, items.Select(i => i.PyItem));
        }

        /// <summary>
        ///     Return an owner
        /// </summary>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        public DirectOwner GetOwner(long ownerId)
        {
            return DirectOwner.GetOwner(this, ownerId);
        }

        /// <summary>
        ///     Return a location
        /// </summary>
        /// <param name="locationId"></param>
        /// <returns></returns>
        public DirectLocation GetLocation(int locationId)
        {
            return DirectLocation.GetLocation(this, locationId);
        }

        /// <summary>
        ///     Return the name of a location
        /// </summary>
        /// <param name="locationId"></param>
        /// <returns></returns>
        public string GetLocationName(long locationId)
        {
            return DirectLocation.GetLocationName(this, locationId);
        }

        /// <summary>
        ///     Return the agent by id
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns></returns>
        public DirectAgent GetAgentById(long agentId)
        {
            return DirectAgent.GetAgentById(this, agentId);
        }

        /// <summary>
        ///     Return the agent by name
        /// </summary>
        /// <param name="agentName"></param>
        /// <returns></returns>
        public DirectAgent GetAgentByName(string agentName)
        {
            return DirectAgent.GetAgentByName(this, agentName);
        }


        // TODO: Stage information -- __builtin__.sm.services[corruptionSuppressionSvc].GetSystemSuppression.im_func.func_closure[0].cell_contents
        public List<DirectSolarSystem> GetInsurgencyInfestedSystems()
        {

            // __builtin__.sm.services[insurgencyCampaignSvc].GetCurrentCampaignSnapshots_Memoized.im_func.func_closure[0].cell_contents.items()[0][0][1][0][1]._coveredSolarsystemIDs

            try
            {
                var res = new List<DirectSolarSystem>();
                var insurgencyCampaignSvc = GetLocalSvc("insurgencyCampaignSvc");
                var dict =
                    insurgencyCampaignSvc.Attribute("GetCurrentCampaignSnapshots_Memoized")["im_func"]["func_closure"]
                        .ToList()[0]["cell_contents"]["_cache"].ToDictionary();


                //Log($"dict count [{dict.Count}]");

                var _coveredSolarsystemIDs = dict.Values.ToList()[0].GetItemAt(0).ToList();

                foreach (var k in _coveredSolarsystemIDs)
                {
                    var ids = k["coveredSolarsystemIDs"].ToList<int>();
                    foreach (var id in ids)
                    {
                        if (SolarSystems.ContainsKey(id))
                        {
                            res.Add(SolarSystems[id]);
                        }
                    }
                }

                return res;
            }
            catch (Exception e)
            {
                Log(e.Message);
            }

            return new List<DirectSolarSystem>();
        }

        public bool TrashItems(List<DirectItem> items)
        {

            //Log($"Trying to trash {items.Count} items.");
            if (!Interval(4000, 8000))
                return false;

            var itemsLeft = items.Where(i => i.IsTrashable()).Select(i => i.PyItem).ToList();

            //Log($"Items left to trash: {itemsLeft.Count} items.");

            if (itemsLeft.Count == 0)
                return false;

            ThreadedCall(GetLocalSvc("menu")["TrashInvItems"], itemsLeft.ToList());

            return true;
        }

        public Dictionary<string, long> GetAllAgents()
        {
            return DirectAgent.GetAllAgents(this);
        }

        public bool IsAgentsByIdDictionaryPopulated()
        {
            return DirectAgent.IsAgentsByIdDictionaryPopulated(this);
        }

        public void PopulateAgentsByIdDictionary()
        {
            DirectAgent.PopulateAgentsByIdDictionary(this);
        }

        private Dictionary<string, int> _groupTypeIdDict = new Dictionary<string, int>();

        public Dictionary<string, int> GetGroupsDictByName()
        {
            PopulateGroupsIdByNameDict();
            return _groupTypeIdDict;
        }

        private List<int> _abyssLootGroups = new List<int>();

        public List<int> GetAbyssLootGroups()
        {
            try
            {
                if (_abyssLootGroups.Any())
                    return _abyssLootGroups;

                List<string> itemsList = new List<string>
                {
                    "Triglavian Data",
                    "Abyssal Materials",
                    "Abyssal Filaments",
                    "Precursor Weapon Blueprint",
                    "Entropic Radiation Sink Blueprint",
                    "Exotic Plasma Charge Blueprint",
                    "Mutaplasmids",
                    "Frigate Blueprint",
                    "Destroyer Blueprint",
                    "Abyssal Proving Filaments",
                    "Cruiser Blueprint",
                    "Battlecruiser Blueprint",
                    "Mutadaptive Remote Armor Repairer Blueprint",
                    "Battleship Blueprint",
                    "Gunnery",
                    "Spaceship Command",
                    "Trinary Data Vaults",
                    "Outer"
                };

                foreach (var kv in GetGroupsDictByName())
                {
                    if (itemsList.Any(e => e.ToLower() == kv.Key))
                    {
                        _abyssLootGroups.Add(kv.Value);
                    }
                }
                return _abyssLootGroups;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return new List<int>();
            }
        }

        private void PopulateGroupsIdByNameDict()
        {
            try
            {
                if (!_groupTypeIdDict.Any())
                {

                    var types = PySharp.Import("evetypes");
                    if (types.IsValid)
                    {
                        var dict = types.Call("GetGroupIDByGroupNameDict").ToDictionary<string>();
                        foreach (var kv in dict)
                        {
                            _groupTypeIdDict.Add(kv.Key, kv.Value.ToInt());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
        }

        public int? GetGroupIdByName(string name)
        {
            try
            {
                PopulateGroupsIdByNameDict();

                if (_groupTypeIdDict.ContainsKey(name))
                    return _groupTypeIdDict[name];

                return null;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return null;
            }
        }

        private static HashSet<string> _classKeys;
        /// <summary>
        ///     Return what "eve.LocalSvc" would return, unless the service wasn't started yet
        /// </summary>
        /// <param name="svc"></param>
        /// <returns></returns>
        /// <remarks>Use at your own risk!</remarks>
        public PyObject GetLocalSvc(string svc, bool addCache = true, bool startService = true)
        {
            PyObject service;
            // Do we have a cached version (this is to stop overloading the LocalSvc call)
            if (_localSvcCache.TryGetValue(svc, out service))
                return service;

            // First try to get it from services
            service = PySharp.Import("__builtin__").Attribute("sm").Attribute("services").DictionaryItem(svc);

            // Add it to the cache (it doesn't matter if its not valid)
            if (addCache)
                _localSvcCache.AddOrUpdate(svc, service);

            // If its valid, return the service
            if (service.IsValid)
                return service;

            if (!startService)
                return PySharp.PyZero;

            if (_classKeys == null)
            {
                var classKeys = PySharp.Import("__builtin__").Attribute("sm").Attribute("classmapWithReplacements");
                if (!classKeys.IsValid)
                {
                    Log($"ERROR: __builtin__.sm.classmapWithReplacements is not valid! --- This usually means, that you tried to access Python from a non GIL thread.");
                    return PySharp.PyZero;
                }
                var keys = classKeys.ToDictionary<string>();
                _classKeys = new HashSet<string>(keys.Keys);
            }

            if (!_classKeys.Contains(svc))
            {
                Log($"FIX ME: Service [{svc}] does not exist in svc.py.");
                return PySharp.PyZero;
            }

            // Start the service in a ThreadedCall
            var localSvc = PySharp.Import("__builtin__").Attribute("sm").Attribute("GetService");
            ThreadedCall(localSvc, svc);

            // Return an invalid PyObject (so that LocalSvc can start the service)
            return PySharp.PyZero;
        }

        public bool IsServiceRunning(string svc)
        {
            if (!_localSvcCache.TryGetValue(svc, out var obj))
                obj = PySharp.Import("__builtin__").Attribute("sm").Attribute("services").DictionaryItem(svc);

            if (obj == null || !obj.IsValid)
                return false;

            _localSvcCache.AddOrUpdate(svc, obj);

            return obj.Attribute("state").ToInt() == 4;
        }

        public Dictionary<string, PyObject> GetAllRunningServices()
        {
            var ret = new Dictionary<string, PyObject>();

            var services = PySharp.Import("__builtin__").Attribute("sm").Attribute("services").ToDictionary<string>();

            if (services != null && services.Any())
                ret = services;

            return ret;
        }

        public void StartService(string svc)
        {
            GetLocalSvc(svc);
        }

        /// <summary>
        ///     Perform a uthread.new(pyCall, parms) call
        /// </summary>
        /// <param name="pyCall"></param>
        /// <param name="parms"></param>
        /// <returns></returns>
        /// <remarks>Use at your own risk!</remarks>
        public bool ThreadedCall(PyObject pyCall, params object[] parms)
        {
            return ThreadedCallWithKeywords(pyCall, null, parms);
        }

        /// <summary>
        ///     Perform a uthread.new(pyCall, parms) call
        /// </summary>
        /// <param name="pyCall"></param>
        /// <param name="keywords"></param>
        /// <param name="parms"></param>
        /// <returns></returns>
        /// <remarks>Use at your own risk!</remarks>
        public bool ThreadedCallWithKeywords(PyObject pyCall, Dictionary<string, object> keywords, params object[] parms)
        {
            // Check specifically for this, as the call has to be valid (e.g. not null or none)
            if (!pyCall.IsValid)
                return false;

            if (!pyCall.IsCallable(pyCall.PyRefPtr))
            {
                Log($"Error: Not a callable.");
                return false;
            }

            //RegisterAppEventTime();
            return !PySharp.Import("uthread").CallWithKeywords("new", keywords, new object[] { pyCall }.Concat(parms).ToArray()).IsNull;
        }

        /// <summary>
        ///     Perform a uthread.new(svc.call, parms) call
        /// </summary>
        /// <param name="svc"></param>
        /// <param name="call"></param>
        /// <param name="parms"></param>
        /// <returns></returns>
        /// <remarks>Use at your own risk!</remarks>
        public bool ThreadedLocalSvcCall(string svc, string call, params object[] parms)
        {
            var pyCall = GetLocalSvc(svc).Attribute(call);
            return ThreadedCall(pyCall, parms);
        }

        /// <summary>
        ///     Return's true if the entity has not been a target in the last 3 seconds
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal bool CanTarget(long id)
        {
            return !_lastKnownTargets.ContainsKey(id);
        }

        /// <summary>
        ///     Remove's the target from the last known targets
        /// </summary>
        /// <param name="id"></param>
        internal void ClearTargetTimer(long id)
        {
            _lastKnownTargets.Remove(id);
        }

        /// <summary>
        ///     Set the target's last target time
        /// </summary>
        /// <param name="id"></param>
        internal void SetTargetTimer(long id)
        {
            _lastKnownTargets[id] = DateTime.UtcNow;
        }

        /// <summary>
        ///     Register app event time -- DEPRECATED
        /// </summary>
        private void RegisterAppEventTime()
        {
            //PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("uilib").Call("RegisterAppEventTime");
        }

        /// <summary>
        ///     Open the fitting management window
        /// </summary>
        public void OpenFittingManager()
        {
            if (!Interval(4500, 6500))
                return;
            Log($"Opening the fitting management window.");
            var form = PySharp.Import("form");
            ThreadedCall(form.Attribute("FittingMgmt").Attribute("Open"));
        }

        /// <summary>
        ///     Open the repairshop window
        /// </summary>
        public void OpenRepairShop()
        {
            if ((bool)!Session.IsInDockableLocation)
                Log("OpenRepairShop: IsInDockableLocation is null or false: waiting");
            var form = PySharp.Import("form");
            ThreadedCall(form.Attribute("RepairShopWindow").Attribute("Open"));
        }

        internal long? GetServiceMask()
        {
            if (!Session.IsInDockableLocation)
                return -1;

            var stationService = GetLocalSvc("station");
            if (stationService.IsValid)
            {
                var stationItem = stationService["stationItem"];
                var header = stationItem["header"].ToList<string>();
                if (header.Contains("serviceMask"))
                {
                    var index = header.IndexOf("serviceMask");
                    var line = stationItem["line"].ToList<int>();
                    return line[index];
                }
            }
            return -1;
        }

        /// <summary>
        /// This does not respect standings!
        /// </summary>
        /// <returns></returns>
        public bool HasRepairFacility()
        {
            if (!Session.IsInDockableLocation || !Session.IsReady)
                return false;

            var stationService = GetLocalSvc("station");
            if (Session.HasStationId && stationService.IsValid && stationService["IsStationServiceAvailable"].IsCallable())
            {
                if (stationService.Call("IsStationServiceAvailable", 13).ToBool())
                    return true;
            }

            var structureServices = GetLocalSvc("structureServices");
            if (Session.HasStructureId && structureServices.IsValid && structureServices["IsServiceAvailableForCharacter"].IsCallable())
            {
                if (structureServices.Call("IsServiceAvailableForCharacter", 8).ToBool())
                {
                    return true;
                }
            }

            return false;

            // appConst.stationServiceRepairFacilities  = 13
            // structures.SERVICE_REPAIR = 8

            //var cmd = GetLocalSvc("cmd");

            //    if session.stationid and sm.GetService('station').IsStationServiceAvailable(appConst.stationServiceRepairFacilities):
            //    if self.HasServiceAccess('repairshop'):
            //        return True
            //if session.structureid:
            //    if sm.GetService('structureServices').IsServiceAvailableForCharacter(structures.SERVICE_REPAIR):

            //if (cmd.IsValid)
            //{
            //    var repairAvail = cmd["_IsRepairServiceAvailable"];

            //    if (repairAvail.IsValid)
            //    {
            //        return cmd.Call("_IsRepairServiceAvailable").ToBool();
            //    }
            //}


            //if (SolarSystems[Session.SolarSystemId.Value].GetSecurity() >= 0.45)
            //    return true;

            //return true;

            //var serviceMask = getServiceMask();

            //if (serviceMask < 0)
            //    return false;

            //return (serviceMask & (long)Const["stationServiceRepairFacilities"]) != 0;
        }

        /// <summary>
        ///     Broadcast scatter events.  Use with caution.
        /// </summary>
        /// <param name="evt">The event name.</param>
        /// <returns></returns>
        public bool ScatterEvent(string evt)
        {
            var scatterEvent = PySharp.Import("__builtin__").Attribute("sm").Attribute("ScatterEvent");

            return ThreadedCall(scatterEvent, evt);
        }

        /// <summary>
        ///     Log a message.
        /// </summary>
        /// <param name="msg">A string to output to the loggers.</param>
        public void Log(string msg, [CallerMemberName] string caller = null)
        {
            Logging.Log.WriteLine(msg, null, caller);
        }

        public bool MultiSell(List<DirectItem> items)
        {
            if (Session.IsInSpace || !Session.IsInDockableLocation)
                return false;

            if (items.Any(i => !i.PyItem.IsValid))
                return false;

            items.RemoveAll(i => i.IsSingleton);

            var list = new List<PyObject>();

            foreach (var item in items)
            {
                list.Add(item.PyItem);
            }

            return ThreadedLocalSvcCall("menu", "SellItems", list);
        }

        internal PyObject GetRange(DirectOrderRange range)
        {
            switch (range)
            {
                case DirectOrderRange.SolarSystem:
                    return Const.RangeSolarSystem;

                case DirectOrderRange.Constellation:
                    return Const.RangeConstellation;

                case DirectOrderRange.Region:
                    return Const.RangeRegion;

                default:
                    return Const.RangeStation;
            }
        }

        private IntPtr? _eveMainHandle = null;
        public IntPtr EveMainHandle
        {
            get
            {
                if (_eveMainHandle != null && _eveMainHandle.HasValue)
                    return _eveMainHandle.Value;
                var eveHwnd = WinApiUtil.GetEveHWnd(Process.GetCurrentProcess().Id);
                if (eveHwnd != IntPtr.Zero)
                {
                    _eveMainHandle = eveHwnd;
                    return _eveMainHandle.Value;
                }
                return IntPtr.Zero;
            }
        }

        // Windows message constants
        const uint WM_KEYDOWN = 0x0100;
        const uint WM_KEYUP = 0x0101;
        const int VK_PAUSE = 0x13; // Pause/Break key
        const int VK_F24 = 0x87;

        public void SendFakeInputToPreventIdle()
        {
            PostMessage(EveMainHandle, WM_KEYDOWN, (IntPtr)VK_F24, IntPtr.Zero);
            PostMessage(EveMainHandle, WM_KEYUP, (IntPtr)VK_F24, IntPtr.Zero);
        }

        public bool Buy(int StationId, int TypeId, double Price, int quantity, DirectOrderRange range, int minVolume, int duration) //, bool useCorp)
        {
            var pyRange = GetRange(range);
            //def BuyStuff(self, stationID, typeID, price, quantity, orderRange = None, minVolume = 1, duration = 0, useCorp = False):
            return ThreadedLocalSvcCall("marketQuote", "BuyStuff", StationId, TypeId, Price, quantity, pyRange, minVolume, duration); //, useCorp);
        }

        public bool IsInFleet => FleetMembers.Any();

        public bool InviteToFleet(long charId)
        {
            if (!Interval(1500, 2500))
                return false;

            return ThreadedLocalSvcCall("menu", "InviteToFleet", charId);
        }

        public bool FormFleetWithSelf()
        {
            if (!Interval(1500, 2500))
                return false;

            return ThreadedLocalSvcCall("menu", "InviteToFleet", Session.CharacterId);
        }

        public bool KickMember(long charId)
        {
            if (!IsInFleet)
                return false;

            if (!Interval(1500, 2500))
                return false;

            return ThreadedLocalSvcCall("menu", "KickMember", charId);
        }

        public bool LeaveFleet()
        {
            if (!IsInFleet)
                return false;

            if (!Interval(1500, 2500))
                return false;

            return ThreadedLocalSvcCall("menu", "LeaveFleet");
        }

        public bool MakeFleetBoss(long charId)
        {

            if (!IsInFleet)
                return false;

            if (!Interval(1500, 2500))
                return false;

            return ThreadedLocalSvcCall("menu", "MakeLeader", charId);
        }

        public bool WarpToFleetMember(long charId, double range = 0)
        {
            if (!IsInFleet)
                return false;

            if (!Interval(1500, 2500))
                return false;

            // Check if the member is actually part of the fleet
            if (!FleetMembers.Any(e => e.CharacterId == charId))
                return false;

            if ((FleetMembers.First(e => e.CharacterId == charId)?.Entity?.Distance ?? double.MaxValue) < 150000)
            {
                Log($"Can't warp that entity, it's too close.");
                return false;
            }

            if (Session != null && Session.CharactersInLocal != null && Session.CharactersInLocal.All(i => i.CharacterId != charId) && !Session.IsWspace)
            {
                Log($"charid [" + charId + "] not found in local");
                return false;
            }

            if (Session.IsWspace && (FleetMembers.First(e => e.CharacterId == charId)?.Entity?.Distance ?? double.MaxValue) < (double)Distances.FourHundredAu)
            {
                Log($"Can't warp that entity, charid [" + charId + "] is probably not in local: more than 400AU?");
                return false;
            }

            if (range != 0)
                return ThreadedLocalSvcCall("menu", "WarpFleetToMember", charId, range);

            return ThreadedLocalSvcCall("menu", "WarpFleetToMember", charId);
        }
        /// <summary>
        ///     Initiates trade window
        /// </summary>
        /// <param name="charId"></param>
        /// <returns>Fails if char is not in station, if charId is not in station and if the service is not active yet</returns>
        public bool InitiateTrade(long charId)
        {
            if ((bool)!Session.IsInDockableLocation)
                return false;

            if (!GetStationGuests.Any(i => i == charId))
                return false;

            var tradeService = GetLocalSvc("pvptrade");
            if (!tradeService.IsValid)
                return false;

            return ThreadedCall(tradeService.Attribute("StartTradeSession"), charId);
        }

        public bool AddToAddressbook(int charid)
        {
            return ThreadedLocalSvcCall("addressbook", "AddToPersonalMulti", new List<int> { charid });
        }

        private enum LockedItemState
        {
            NoLockedItems,
            WaitingToClear
        }

        private LockedItemState _lockedItemState;
        //private DateTime _waitForLockedItemsUntil;
        private DirectStaticDataLoader _directStaticDataLoader;

        public bool NoLockedItemsOrWaitAndClearLocks(string calledFrom)
        {
            if (GetLockedItems().Count == 0)
            {
                //Log("No items locked.");
                _lockedItemState = LockedItemState.NoLockedItems;
                return true;
            }

            switch (_lockedItemState)
            {
                case LockedItemState.NoLockedItems:
                    _waitForLockedItemsUntil = DateTime.UtcNow.AddSeconds(new Random().Next(2, 4));
                    _lockedItemState = LockedItemState.WaitingToClear;
                    return false;

                case LockedItemState.WaitingToClear:
                    if (_waitForLockedItemsUntil < DateTime.UtcNow)
                    {
                        UnlockItems();
                        Log("Clearing item locks.");
                        _lockedItemState = LockedItemState.NoLockedItems;
                    }

                    Log("[" + calledFrom + "][" + GetLockedItems().Count + "] Items currently moving (locked): Waiting");
                    return false;
            }

            return false;
        }

        private void GetProcessMemory()
        {
            //From processhealthsvc.py
            //
            //var line = subprocess.Popen('tasklist /FO csv /FI "PID eq %s" /NH' % self._pid, stdout=subprocess.PIPE)
            //var memory = line.stdout.read().split('"')[9]
            //return int (memory.split(' ')[0].replace(',', '').replace('.', '')) * 1024
        }

        /// <summary>
        ///     Reset DE caused freezes ~ Will be expanded later
        /// </summary>
        private void CheckStatistics()
        {
            var clientStatsSvc = GetLocalSvc("clientStatsSvc");
            if (!clientStatsSvc.IsValid)
                return;

            Dictionary<string, PyObject> StatsDict = new Dictionary<string, PyObject>();

            if (clientStatsSvc.HasAttrString("statsEntries"))
            {
                StatsDict = GetLocalSvc("clientStatsSvc").Attribute("statsEntries").ToDictionary<string>();
                if (StatsDict.Any())
                {
                    //We detect change frameTimeAbove100msStat and other in python functions by client side
                    // eve\client\script\sys\clientStatsSvc.py
                    _frameTimeAbove100ms = (double)StatsDict["frameTimeAbove100ms"].Attribute("value");
                    _frameTimeAbove200ms = (double)StatsDict["frameTimeAbove200ms"].Attribute("value");
                    _frameTimeAbove300ms = (double)StatsDict["frameTimeAbove300ms"].Attribute("value");
                    _frameTimeAbove400ms = (double)StatsDict["frameTimeAbove400ms"].Attribute("value");
                    _frameTimeAbove500ms = (double)StatsDict["frameTimeAbove500ms"].Attribute("value");
                    _timesliceWarnings = (double)StatsDict["timesliceWarnings"].Attribute("value");

                    if (GetLastFrameExecutionDuration > 80)
                    {
                        StatsDict["frameTimeAbove100ms"].Call("Set", _prevFrameTimeAbove100ms);
                        StatsDict["frameTimeAbove200ms"].Call("Set", _prevFrameTimeAbove200ms);
                        StatsDict["frameTimeAbove300ms"].Call("Set", _prevFrameTimeAbove300ms);
                        StatsDict["frameTimeAbove400ms"].Call("Set", _prevFrameTimeAbove400ms);
                        StatsDict["frameTimeAbove500ms"].Call("Set", _prevFrameTimeAbove500ms);
                        StatsDict["timesliceWarnings"].Call("Set", _prevtimesliceWarnings);
                    }

                    _prevFrameTimeAbove100ms = _frameTimeAbove100ms;
                    _prevFrameTimeAbove200ms = _frameTimeAbove200ms;
                    _prevFrameTimeAbove300ms = _frameTimeAbove300ms;
                    _prevFrameTimeAbove400ms = _frameTimeAbove400ms;
                    _prevFrameTimeAbove500ms = _frameTimeAbove500ms;
                    _prevtimesliceWarnings = _timesliceWarnings;
                }
            }
        }
        /**
        public WindowRecorderSession StartWindowRecording(string recordingName)
        {
            var recordingDirectory = ESCache.Instance.EveSetting.RecordingDirectory;
            if (recordingDirectory == null)
            {
                Log("Recording directory is not set");
                return null;
            }

            var pid = Process.GetCurrentProcess().Id;
            var window = WinApiUtil.GetEveHWnd(pid);
            if (window == IntPtr.Zero)
            {
                Log("Failed to get eve window handle to start recording");
                return null;
            }

            var windowName = Util.GetWidowTitle(window);
            if (string.IsNullOrEmpty(windowName))
            {
                Log("Failed to get eve window title to start recording");
                return null;
            }

            if (windowName == "EVE")
            {
                Log("Cannot start recording while in character selection");
                return null;
            }

            var characterName = ESCache.Instance.CharName;

            recordingName ??= "recording";
            var path = Path.Combine(recordingDirectory,
                $"{characterName}_{recordingName}_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.mkv");
            Log($"Starting recording to {path}");

            // Check if file exists under the path and if it does, append a random number to the end of the file name
            if (File.Exists(path))
            {
                path = Path.Combine(recordingDirectory,
                                $"{characterName}_{recordingName}_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}_{_random.Next(1, 999)}.mkv");
            }

            // Use mkv container format to prevent corruptions from process terminations
            var fileName = Path.Combine(recordingDirectory, path);
            var session = WindowRecorder.Start(
                fileName,
                windowName,
                new WindowRecorderOptions()
                {
                    Framerate = 6,
                    EncoderSetting = ESCache.Instance.EveSetting.RecorderEncoderSetting
                });
            if (session == null)
            {
                Log("Failed to start recording");
                return null;
            }

            return session;
        }
        **/
    }

    public enum CourierMissionCtrlState
    {
        Start,
        ActivateTransportShip,
        ItemsFoundAndBeingMoved,
        TryToGrabPickupItemsFromHomeStation,
        GotoPickupLocation,
        PickupItem,
        GotoDropOffLocation,
        DropOffItem,
        Idle,
        CompleteMission,
        Done,
        Error,
        Statistics,
        NotEnoughCargoRoom,
        Unused
    }
}