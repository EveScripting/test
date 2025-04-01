/*
* Created by SharpDevelop.
* User: duketwo
* Date: 28.05.2016
* Time: 17:38
*
* To change this template use Tools | Options | Coding | Edit Standard Headers.
*/

extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers;
using EVESharpCore.Controllers.Abyssal;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using SC::SharedComponents.Extensions;
using SC::SharedComponents.IPC;
using SC::SharedComponents.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SC::SharedComponents.EVE;
using EVESharpCore.Framework.Events;
using SC::SharedComponents.Events;
using System.Threading.Tasks;
using System.Management;
using System.IO;
using System.Threading;
using System.Reflection;
using EVESharpCore.Lookup;

namespace EVESharpCore
{
    /// <summary>
    ///     Description of ControllerManager.
    /// </summary>
    public sealed partial class ControllerManager : IDisposable
    {
        #region Fields

        public static readonly List<Type> DEFAULT_CONTROLLERS = new List<Type>() // controllers which are enabled by default
        {
            typeof(LoginController),
            typeof(TargetCombatantsController),
            typeof(CombatController),
            typeof(DroneController),
            //typeof(SkillQueueController),
            typeof(CleanupController),
            typeof(ActionQueueController),
            typeof(NotificationController),
            typeof(ReduceGraphicLoadController),
            typeof(UITravellerController),
            //typeof(WndEventLogController),
            typeof(DefenseController),
            typeof(SalvageController),
            typeof(AmmoManagementController),
            //typeof(ScannerController),
        };

        //Can we / should we unload controllers not in use when in abyssal space?
        public static readonly List<Type> ABYSSAL_CONTROLLERS = new List<Type>() // controllers which are enabled by default
        {
            //typeof(SkillQueueController),
            typeof(CleanupController),
            typeof(ReduceGraphicLoadController),
            typeof(SalvageController),
            typeof(AmmoManagementController),
            //typeof(ScannerController),
        };

        private static List<Type> _controllersThatAreNotNeededInAbyssalDeadspace = new List<Type>();
        public static List<Type> ControllersThatAreNotNeededInAbyssalDeadspace
        {
            get
            {
                try
                {
                    if (_controllersThatAreNotNeededInAbyssalDeadspace.Any())
                        return _controllersThatAreNotNeededInAbyssalDeadspace;

                    _controllersThatAreNotNeededInAbyssalDeadspace = new List<Type>();
                    foreach (var defaultController in DEFAULT_CONTROLLERS)
                    {
                        if (!ABYSSAL_CONTROLLERS.Contains(defaultController))
                        {
                            _controllersThatAreNotNeededInAbyssalDeadspace.Add(defaultController);
                            continue;
                        }

                        continue;
                    }

                    return _controllersThatAreNotNeededInAbyssalDeadspace ?? new List<Type>();
                }
                catch (Exception)
                {
                    return new List<Type>();
                }
            }
        }


        public static readonly List<Type> HIDDEN_CONTROLLERS = new List<Type>()
        {
            typeof(AmmoManagementController),
            typeof(BuyPlexController),
            typeof(BuyItemsController),
            typeof(DefenseController),
            typeof(SalvageController),
            //typeof(PanicController),
            typeof(AbyssalBaseController),
        };

        private static readonly ControllerManager _instance = new ControllerManager();

        #endregion Fields

        #region Constructors

        private ControllerManager(int pulseDelayMilliseconds = 150)
        {
            PulseDelayMilliseconds = pulseDelayMilliseconds;
            ControllerManagerPulse = DateTime.MinValue;
            Rnd = new Random();
            ControllerList = new ConcurrentBindingList<BaseController>();
            ControllerDict = new ConcurrentDictionary<Type, IController>();
        }

        #endregion Constructors

        #region Properties

        public static ControllerManager Instance => _instance;
        public ConcurrentDictionary<Type, IController> ControllerDict { get; private set; }
        public ConcurrentBindingList<BaseController> ControllerList { get; private set; }
        public BaseController CurrentController => Enumerator != null ? Enumerator.Current : null;

        public DateTime NextPulse
        {
            get
            {
                if (DateTime.UtcNow >= ControllerManagerPulse)
                {
                    ControllerManagerPulse = DateTime.UtcNow.AddMilliseconds(PulseDelayMilliseconds);
                }

                return ControllerManagerPulse;
            }
        }

        private IEnumerator<BaseController> Enumerator { get; set; }

        private DateTime ControllerManagerPulse { get; set; }

        private int PulseDelayMilliseconds { get; set; }

        private Random Rnd { get; set; }

        #endregion Properties

        #region Methods

        private static Stopwatch _getNextControllerStopwatch = new Stopwatch();

        public void AddController(IController controller) // main add method
        {
            try
            {
                if (controller == null)
                    return;

                if (ControllerList.All(c => c.GetType() != controller.GetType()))
                {
                    //Log.WriteLine("AddController [" + controller.GetType() + "]");
                    ControllerList.Add(controller);
                    ControllerDict.AddOrUpdate(controller.GetType(), controller, (key, oldValue) => controller);
                }

                foreach (var depControllerType in controller.DependsOn) // add dependent controllers
                {
                    if (ControllerList.All(c => c.GetType() != depControllerType))
                    {
                        AddController(depControllerType);
                    }
                }

                Program.EveSharpCoreFormInstance.AddControllerTab(controller);
            }
            catch (Exception ex)
            {
                Log.WriteLine(ex.ToString());
            }
        }

        private Queue<Action> _controllerAddQueue = new Queue<Action>();

        private static HashSet<Type> _controllerTypes = typeof(CleanupController).Assembly.GetTypes()
            .Where(e => !e.IsAbstract)
            .Where(e => typeof(IController).IsAssignableFrom(e)).ToHashSet();

        private Dictionary<string, Type> _controllerTypeByString = null;

        public IController GetController(string type)
        {
            if (_controllerTypeByString == null)
            {
                _controllerTypeByString = new Dictionary<string, Type>();
                foreach (var controllerType in _controllerTypes)
                {
                    _controllerTypeByString[controllerType.Name] = controllerType;
                }
            }

            if (_controllerTypeByString.TryGetValue(type, out var r))
                if (ControllerDict.TryGetValue(r, out var contr))
                    return contr;

            return null;
        }

        public void AddController(Type t)
            {
                var inst = Activator.CreateInstance(t);
                AddController((IController)inst);
        }

        public void AddController(string n)
            {
            	try
	            {
	                if (string.IsNullOrEmpty(n) || n.Equals("None"))
	                    return;

	                var t = Type.GetType($"{nameof(EVESharpCore)}.{nameof(Controllers)}." + n);
	                if (t != null)
	                {
	                    var inst = Activator.CreateInstance(t);
	                    AddController((IController)inst);
	                    return;
	                }
	                else
	                {
	                    t = Type.GetType($"{nameof(EVESharpCore)}.{nameof(Controllers)}.{nameof(Controllers.Abyssal)}." + n);
	                    if (t != null)
	                    {
	                        var inst = Activator.CreateInstance(t);
	                        AddController((IController)inst);
	                        return;
	                    }

	                    Log.WriteLine("AddController failed: [" + n + "] is not a valid controller name");
	                }
	            }
	            catch (Exception ex)
	            {
	                Log.WriteLine(ex.ToString());
	            }
        }

        public T GetController<T>()
        {
            if (ControllerDict.TryGetValue(typeof(T), out var c))
            {
                return (T)c;
            }
            return default(T);
        }

        private void IsEveClientHealthy(object obj, EventArgs eventArgs)
        {
            //
            // reasons to not be healthy: last onframe was a long time ago
            //
            if (DebugConfig.DebugControllerManager)
                Log.WriteLine("IsEveClientHealthy");


            if (ESCache.Instance.InStation)
            {
                if (DateTime.UtcNow > LastOnFrameEvent.AddSeconds(5))
                {
                    Log.WriteLine("LastOnFrameEvent was more than 5 seconds ago.");
                }

                if (DateTime.UtcNow > LastOnFrameEvent.AddSeconds(25))
                {
                    Log.WriteLine("LastOnFrameEvent was more than 25 seconds ago.");
                    ESCache.Instance.CloseEveReason = "LastOnFrameEvent was more than 25 seconds ago!";
                    ESCache.Instance.BoolRestartEve = true;
                    if (ESCache.Instance.EveAccount.EndTime.AddMinutes(30) > DateTime.UtcNow && (!ESCache.Instance.EveAccount.InMission && !ESCache.Instance.EveAccount.IsInAbyss))
                    {
                        Log.WriteLine("LastOnFrameEvent was more than 25 seconds ago.");
                        ESCache.Instance.CloseEve(false, ESCache.Instance.CloseEveReason);
                        return;
                    }

                    ESCache.Instance.CloseEve(true, ESCache.Instance.CloseEveReason);
                    return;
                }
            }

            if (DateTime.UtcNow > LastOnFrameEvent.AddSeconds(5))
            {
                Log.WriteLine("LastOnFrameEvent was more than 5 seconds ago.");
            }

            if (DateTime.UtcNow > LastOnFrameEvent.AddSeconds(15))
            {
                Log.WriteLine("LastOnFrameEvent was more than 15 seconds ago.");
                ESCache.Instance.CloseEveReason = "LastOnFrameEvent was more than 15 seconds ago!";
                ESCache.Instance.BoolRestartEve = true;
                if (ESCache.Instance.EveAccount.EndTime.AddMinutes(30) > DateTime.UtcNow && (!ESCache.Instance.EveAccount.InMission && !ESCache.Instance.EveAccount.IsInAbyss))
                {
                    Log.WriteLine("LastOnFrameEvent was more than 15 seconds ago.");
                    ESCache.Instance.CloseEve(false, ESCache.Instance.CloseEveReason);
                    return;
                }

                ESCache.Instance.CloseEve(true, ESCache.Instance.CloseEveReason);
                return;
            }

            return;
        }

        public void Initialize()
        {
            try
            {
                // set callback
                Console.WriteLine("SetCallBackService LauncherCallBack: GUID [" + WCFClient.Instance.GUID + "]");
                WCFClient.Instance.SetCallBackService(new LauncherCallback(), WCFClient.Instance.GUID);

                if (!ESCache.LoadDirectEveInstance()) return;


                Log.WriteLine("Registering Onframe Event.");
                Console.WriteLine("Registering Onframe Event.");
                ESCache.Instance.DirectEve.OnFrame += EVEOnFrame;
                System.Windows.Forms.Timer watchDogTimer1 = new System.Windows.Forms.Timer
                {
                    Enabled = true,
                    Interval = 1000
                };

                //while (true)
                //{
                //    Thread.Sleep(100);
                //}

                watchDogTimer1.Tick += IsEveClientHealthy;

                Log.WriteLine("Start: Adding Global Default Controllers");
                foreach (Type c in DEFAULT_CONTROLLERS)
                {
                    Log.WriteLine("Adding Controller: [" + c.Name + "]");
                    AddController(c);
                }
                Log.WriteLine("Done: Adding Global Default Controllers");

                Log.WriteLine("Adding selected controller [" + ESCache.Instance.SelectedController + "] from EVESharpLauncher");
                Instance.AddController(ESCache.Instance.SelectedController);

                //_watchForStartedProcessess = new ManagementEventWatcher("SELECT ProcessID, ProcessName FROM Win32_ProcessStartTrace");
                //_watchForStartedProcessess.EventArrived += ProcessStarted;
                //_watchForStartedProcessess.Start();
            }
            catch (Exception e)
            {
                Log.WriteLine(e.ToString());
            }
        }

        public void CheckForConnectionLost()
        {
            //ToDo: Implement
            return;
        }

        public void RemoveAllControllers()
        {
            try
            {
                ControllerList.Clear();
                ControllerDict.Clear();
                Log.WriteLine("Removed all controllers.");
            }
            catch (Exception ex)
            {
                Log.WriteLine(ex.ToString());
            }
        }

        public void RemoveController(string n)
        {
            try
            {
                var t = Type.GetType($"{nameof(EVESharpCore)}.{nameof(Controllers)}." + n);
                RemoveController(t);
            }
            catch (Exception ex)
            {
                Log.WriteLine(ex.ToString());
            }
        }

        public void RemoveController(Type t)
        {
            try
            {
                if (ControllerList.Any(c => c.GetType().Equals(t)))
                    RemoveController(ControllerList.FirstOrDefault(c => c.GetType().Equals(t)));
            }
            catch (Exception ex)
            {
                Log.WriteLine(ex.ToString());
            }
        }

        public void RemoveController(IController controller) // main remove method
        {
            try
            {
                if (controller == null)
                    return;

                if (DEFAULT_CONTROLLERS.Any(t => t == controller.GetType()))
                {
                    Log.WriteLine("Default type controllers can't be removed.");
                    return;
                }

                if (ControllerList.Any(c => c.GetType().Equals(controller.GetType())))
                {
                    // remove dependent controllers
                    var controllerToBeRemoved = ControllerList.FirstOrDefault(c => c.GetType().Equals(controller.GetType()));
                    foreach (var depControllerType in controllerToBeRemoved.DependsOn)
                    {
                        // check if any other controller depends on that we are trying to remove before removing it completely
                        var allDepsExceptSelf = ControllerList.Where(c => c != controllerToBeRemoved).SelectMany(k => k.DependsOn);
                        if (!allDepsExceptSelf.Contains(depControllerType))
                            RemoveController(depControllerType);
                    }
                    controller.Dispose();
                    ControllerList.Remove(controller);
                    ControllerDict.TryRemove(controller.GetType(), out _);
                    Log.WriteLine("RemoveController [" + controller.GetType() + "]");
                }

                Program.EveSharpCoreFormInstance.RemoveControllerTab(controller);

                Instance.SetPause(false);
            }
            catch (Exception ex)
            {
                Log.WriteLine(ex.ToString());
            }
        }

        public void SetPause(bool val = true)
        {
            foreach (var controller in ControllerList.ToList())
            {
                if (controller.IgnorePause)
                    continue;

                controller.IsPaused = val;
            }

            ESCache.Instance.Paused = val;
            if (DirectEve.Interval(7000)) DirectEventManager.NewEvent(new DirectEvent(DirectEvents.KEEP_ALIVE, "SetPause [" + val + "]"));
            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.ManuallyPausedViaUI), val);
            ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastSessionReady), DateTime.UtcNow);
            Log.WriteLine("SetPause [" + val + "]");
        }

        public bool TryGetController<T>(out T controller)
        {
            controller = default(T);
            if (ControllerDict.TryGetValue(typeof(T), out var c))
            {
                controller = (T)c;
                return true;
            }
            return false;
        }

        private DateTime LastOnFrameEvent = DateTime.UtcNow;

        public DateTime LastSessionValidFailure = DateTime.UtcNow;

        public static ManagementEventWatcher _watchForStartedProcessess;

        private static bool _isEveSharpLauncherRunning = true;
        public static void ProcessStarted(object sender, EventArrivedEventArgs e)
        {
            GetProcessList();
        }

        public static void GetProcessList()
        {
            Process[] ProcessList = Process.GetProcesses();
            foreach (Process _process in ProcessList)
            {
                Log.WriteLine(_process.ProcessName + " PID [" + _process.Id + "]");
                if (_process.ProcessName.Contains("EVESharpLauncher"))
                {
                    Log.WriteLine("GetProcessList: if (_process.ProcessName.Contains(EveSharpLauncher.exe))");
                    _isEveSharpLauncherRunning = true;
                    EveSharpLauncherPid = _process.Id;
                    return;
                }
            }

            Log.WriteLine("GetProcessList: EveSharpLauncher.exe not found");
            _isEveSharpLauncherRunning = false;
            return;
        }

        public static int EveSharpLauncherPid = 0;
        public static bool IsEveSharpLauncherRunning
        {
            get
            {
                if (EveSharpLauncherPid != 0)
                {
                    try
                    {
                        Process EveSharpLauncherProcess = Process.GetProcessById(EveSharpLauncherPid);
                        if (EveSharpLauncherProcess == null)
                        {
                            Log.WriteLine("IsEveSharpLauncherRunning: if (EveSharpProcess == null)");
                            _isEveSharpLauncherRunning = false;
                            return _isEveSharpLauncherRunning;
                        }

                        return _isEveSharpLauncherRunning;
                    }
                    catch (Exception ex)
                    {
                        EveSharpLauncherPid = 0;
                        Log.WriteLine("Exception [" + ex + "]");
                        return false;
                    }
                }

                Log.WriteLine("IsEveSharpLauncherRunning: if (EveSharpLauncherPid == 0)!");
                GetProcessList();

                if (EveSharpLauncherPid != 0)
                {
                    Process EveSharpProcess = Process.GetProcessById(EveSharpLauncherPid);
                    if (EveSharpProcess == null)
                    {
                        Log.WriteLine("IsEveSharpLauncherRunning: if (EveSharpProcess == null)");
                        _isEveSharpLauncherRunning = false;
                        return _isEveSharpLauncherRunning;
                    }

                    return _isEveSharpLauncherRunning;
                }

                Log.WriteLine("IsEveSharpLauncherRunning: if (EveSharpLauncherPid == 0)");
                return false;
            }
        }

        public static bool StartEveSharpLauncher()
        {
            string FileTToLaunch = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\EVESharplauncher.exe";
            if (File.Exists(FileTToLaunch))
            {
                Log.WriteLine("StartEveSharpLauncher: FileTToLaunch [" + FileTToLaunch + "]");
                Process.Start(FileTToLaunch);
                return true;
            }

            return false;
        }

        public static bool CheckSessionValid()
        {
            try
            {

                if (!ESCache.Instance.DirectEve.Session.IsReady || !ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                    return false;

                if (DirectEve.Interval(1000))
                    Task.Run(() =>
                    {
                        try
                        {
                            WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.LastSessionReady),
                                DateTime.UtcNow);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }

                    });

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception: " + ex.ToString());
                return false;
            }
        }

        private void EVEOnFrame(object sender, DirectEveEventArgs e)
        {
            try
            {
                //if (DebugConfig.DebugOnFrame) Log.WriteLine("EVEOnFrame Firing Again");
                LastOnFrameEvent = DateTime.UtcNow;

                TryGetController<LoginController>(out var loginController);
                if (TryGetController<ActionQueueController>(out var aQC))
                {
                    if (loginController != null && loginController.IsReady && aQC.IsReady)
                        if (!aQC.IsFastActionQueueEmpty)
                        {
                            aQC.DoWorkEveryFrame();
                        }
                }

                var isLoggedIn = loginController != null && loginController.IsWorkDone;

                // handle all controllers here which run every frame
                HandleOnFrameControllers(isLoggedIn, e);
                HandleNextPulseController(isLoggedIn, e);
            }
            catch (Exception ex)
            {
                Log.WriteLine("EVEOnFrame Exception: " + ex);
            }
        }

        private void HandleNextPulseController(bool isLoggedIn, DirectEveEventArgs e)
        {
            try
            {
                //200ms
                if (ControllerManagerPulse > DateTime.UtcNow)
                    return;

                var controller = GetNextPulseController();
                if (controller == null)
                    return;

                //if (DebugConfig.DebugOnFrame) Log.WriteLine($"Pulse. Current controller: {controller.GetType()} Frame count: {DirectEve.FrameCount}");
                if (controller.LastControllerExecTimestamp != DateTime.MinValue)
                {
                    var diff = DateTime.UtcNow - controller.LastControllerExecTimestamp;
                    controller.Interval = (ulong)diff.TotalMilliseconds;
                }

                controller.LastControllerExecTimestamp = DateTime.UtcNow;

                // start stopwatch
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                // if there is a modal and not ignoring modal windows
                //if (ESCache.Instance.DirectEve.ModalWindows.Any() && !controller.IgnoreModal)
                //    return;

                // evaluate dependencies
                if (!controller.EvaluateDependencies(this))
                    return;

                //check if the controller should only run after we successfully logged in
                if (!controller.RunBeforeLoggedIn)
                {
                    if (!isLoggedIn)
                    {
                        if (DebugConfig.DebugOnFrame) Log.WriteLine($"Pulse. if (!isLoggedIn)");
                        return;
                    }

                    //if (DebugConfig.DebugOnFrame) Log.WriteLine($"Pulse. Current controller: {controller.GetType()} LoginController.isWorkDone is true");
                }

                // start stopwatch
                Stopwatch _stopwatch = new Stopwatch();
                _stopwatch.Start();

                // execute controller
                controller.DoWork();
                _stopwatch.Stop();
                controller.SetDuration(Util.ElapsedMicroSeconds(stopwatch) + (ulong)e.LastFrameTook);
                if (DebugConfig.DebugOnFrame) Log.WriteLine($"Pulse. Current controller: {controller.GetType()} DoWork: Done LastFrameTook [" + (ulong)e.LastFrameTook + "] ElapsedMicroSeconds [" + Util.ElapsedMicroSeconds(stopwatch) + "]");
            }
            catch (Exception ex)
            {
                Log.WriteLine("EVEOnFrame Exception: " + ex);
            }
            finally
            {
                ControllerManagerPulse = NextPulse;
            }
        }

        private void HandleOnFrameControllers(bool isLoggedIn, DirectEveEventArgs e)
        {
            try
            {
                foreach (var controller in ControllerList.Where(i => i.IsReady))
                {
                    try
                    {
                        if (controller is IOnFrameController frameController)
                        {
                            //SessionCheck();
                            if (DirectEve.HasFrameChanged())
                            {
                                ESCache.Instance.DirectEve.Session.SetSessionReady();
                                ESCache.Instance.InvalidateCache();
                            }

                            if (!CheckSessionValid())
                                continue;

                            //don't run any paused controller
                            if (controller.IsPaused)
                                continue;

                            //check if the controller should only run after we successfully logged in
                            if (!controller.RunBeforeLoggedIn)
                            {
                                if (!isLoggedIn)
                                    continue;
                            }

                            // if the work is done, do nothing
                            if (controller.IsWorkDone)
                                continue;

                            // if there is a modal and not ignoring modal windows
                            if (ESCache.Instance.DirectEve.AnyModalWindows() && !controller.IgnoreModal)
                                continue;

                            // evaluate dependencies
                            if (!controller.EvaluateDependencies(this))
                                continue;

                            // Handle the OnFrame
                            frameController.OnFrame();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("HandleOnFrameControllers: Exception [" + ex + "]");
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private bool ChooseNextController(BaseController myController)
        {
            if (myController.IsPaused)
                return true;

            if (myController.IsWorkDone)
                return true;

            if (myController.LocalPulse > DateTime.UtcNow)
                return true;

            if (!myController.AllowRunInStation && ESCache.Instance.InStation)
                return true;

            if (!myController.AllowRunInSpace && ESCache.Instance.InSpace)
                return true;

            return false;
        }

        public bool ResponsiveMode { get; set; } = false;

        private BaseController GetNextPulseController()
        {
            try
            {
                if (Enumerator == null)
                {
                    if (_getNextControllerStopwatch.IsRunning)
                        _getNextControllerStopwatch.Stop();

                    _getNextControllerStopwatch.Restart();
                    Enumerator = ControllerList.GetEnumerator();
                }

                while (Enumerator.MoveNext())
                {
                    if (!Enumerator.Current.IsReady)
                        continue;

                    if (ESCache.Instance.DirectEve.Session.IsAbyssalDeadspace && !Enumerator.Current.AllowRunInAbyssalSpace)
                        continue;

                    if (ChooseNextController(Enumerator.Current)) continue;

                    return Enumerator.Current;
                }
            }
            catch (Exception)
            {
                //Log.WriteLine($"ControllerList was changed during processing.");
                // thrown if list was changed during iteration
            }
            finally
            {
                //Thread.Sleep(1);
            }

            var rnd = _rnd.Next(1, 650);
            var duration = _getNextControllerStopwatch.ElapsedMilliseconds; // The duration all controllers took.
            var nextPulseDuration = PulseDelayMilliseconds - duration; // Reduce that duration from the next pulse delay to ensure we run the controllers at the given interval.
            nextPulseDuration = Math.Max(1, nextPulseDuration);

            var nextDura = nextPulseDuration + rnd;

            if (ResponsiveMode && nextDura > 200)
                nextDura = nextDura / 2;

            ControllerManagerPulse = DateTime.UtcNow.AddMilliseconds(nextDura);
            //ControllerManagerPulse = NextPulse; //DateTime.UtcNow.AddMilliseconds(nextPulseDuration);
            Enumerator = null; // reset at the end of the iterator or if an exception was thrown
            return null;
        }

        #endregion Methods

        #region IDisposable implementation

        private bool m_Disposed = false;

        private static Random _rnd = new Random();

        ~ControllerManager()
        {
            Dispose();
        }

        public void Dispose()
        {
            try
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            catch (Exception ex)
            {
                Log.WriteLine(ex.ToString());
            }
        }

        private void Dispose(bool disposing)
        {
            try
            {
                if (!m_Disposed)
                {
                    if (disposing)
                        if (ESCache.Instance.DirectEve != null)
                        {
                            ESCache.Instance.DirectEve.OnFrame -= EVEOnFrame;
                            ESCache.Instance.DirectEve.Dispose();
                        }
                    m_Disposed = true;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine(ex.ToString());
            }
        }

        #endregion IDisposable implementation
    }
}
