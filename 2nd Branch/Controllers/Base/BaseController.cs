/*
* Created by SharpDevelop.
* User: duketwo
* Date: 28.05.2016
* Time: 17:07
*
* To change this template use Tools | Options | Coding | Edit Standard Headers.
*/

extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Hooks;
using EVESharpCore.Lookup;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;
using SC::SharedComponents.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EVESharpCore.Controllers.Base
{
    /// <summary>
    ///     Description of BaseController.
    /// </summary>
    [Serializable]
    public abstract class BaseController : ViewModelBase, IController, IDisposable
    {
        private ulong _executionCount;
        public BaseController()
        {
            Rnd = new Random();
            RandomFactor = 1;
            _dependsOn = new List<Type>();
            LastControllerExecTimestamp = DateTime.MinValue;
            if (this is IPacketHandlingController p)
            {
                PacketRecvHook.OnPacketRecv += p.HandleRecvPacket;
                PacketSendHook.OnPacketSend += p.HandleSendPacket;
            }
            AllowRunInSpace = true;
            AllowRunInStation = true;
            AllowRunInAbyssalSpace = true;
        }

        ~BaseController()
        {
            Dispose();
        }
		protected Random Rnd { get; set; }
        protected int RandomFactor { get; set; }

        protected DirectEve Framework => ESCache.Instance.DirectEve;

        public ulong Max // max controller execution duration
        {
            get { return GetValue(() => Max); }
            set { SetValue(() => Max, value); }
        }

        [Browsable(false)]
        public double AvgNumeric // average controller execution duration
        {
            get { return GetValue(() => AvgNumeric); }
            set
            {
                SetValue(() => AvgNumeric, value == 0 ? 1d : value);
                Avg = Math.Round(value, 2).ToString();
            }
        }

        public string Avg
        {
            get { return GetValue(() => Avg); }
            set { SetValue(() => Avg, value); }
        }

        //public ulong Last // last controller execution duration
        //{
        //    get { return GetValue(() => Last); }
        //    set { SetValue(() => Last, value); }
        //}


        public ulong Interval // the internval of the current controller
        {
            get { return GetValue(() => Interval); }
            set { SetValue(() => Interval, value); }
        }

        public DateTime LastControllerExecTimestamp;

        public String Name => GetType().Name;

        [Browsable(false)]
        public TabPage TabPage { get; set; }

        [Browsable(false)]
        public bool IgnoreValidSession
        {
            get { return GetValue(() => IgnoreValidSession); }
            set { SetValue(() => IgnoreValidSession, value); }
        }

        [Browsable(false)]
        public bool RunBeforeLoggedIn
        {
            get { return GetValue(() => RunBeforeLoggedIn); }
            set { SetValue(() => RunBeforeLoggedIn, value); }
        }

        [Browsable(false)]
        public Form Form { get; set; }


        public bool IsPaused
        {
            get { return GetValue(() => IsPaused); }
            set { SetValue(() => IsPaused, value); }
        }


        public bool IgnorePause
        {
            get { return GetValue(() => IgnorePause); }
            set { SetValue(() => IgnorePause, value); }
        }

        public bool IgnoreModal
        {
            get { return GetValue(() => IgnoreModal); }
            set { SetValue(() => IgnoreModal, value); }
        }

        public bool IsWorkDone
        {
            get { return GetValue(() => IsWorkDone); }
            set { SetValue(() => IsWorkDone, value); }
        }

        public abstract void DoWork();

        public abstract bool EvaluateDependencies(ControllerManager cm);
        public bool IsReady
        {
            get
            {
                //SessionCheck();
                if (DirectEve.HasFrameChanged())
                {
                    ESCache.Instance.DirectEve.Session.SetSessionReady();
                    ESCache.Instance.InvalidateCache();
                }

                // if the work is done, do nothing
                if (IsWorkDone)
                {
                    SetDuration(0);
                    return false;
                }

                if (IsPaused) return false;
                //if (DebugConfig.DebugOnFrame) Log.WriteLine($"Pulse. Current controller: {controller.GetType()} is not paused");


                if (!IgnoreValidSession && !CheckSessionValid())
                    return false;

                //if (DebugConfig.DebugOnFrame) Log.WriteLine($"Pulse. Current controller: {controller.GetType()} IsReady: {ESCache.Instance.DirectEve.Session.IsReady}");


                if (LocalPulse > DateTime.UtcNow)
                    return false;

                //if AllowRunInAbyssalSpace is false don't run that controller in Abyssal space
                if (ESCache.Instance.DirectEve.Session.IsAbyssalDeadspace && !AllowRunInAbyssalSpace)
                {
                    if (DebugConfig.DebugOnFrame) Log($"Pulse. Current controller: {GetType()} does not run in AbyssalSpace and that is where we are: skipping");
                    return false;
                }

                return true;
            }
        }


        public virtual void Dispose()
        {
            //GC.SuppressFinalize(this);
            Log($"Controller {this.Name} disposed.");

            if (this is IPacketHandlingController p)
            {
                try
                {
                    PacketRecvHook.OnPacketRecv -= p.HandleRecvPacket;
                    PacketSendHook.OnPacketSend -= p.HandleSendPacket;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        public bool CheckSessionValid()
        {
            try
            {

                if (!ESCache.Instance.DirectEve.Session.IsReady || !ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                    return false;

                if (DirectEve.Interval(2000))
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
                Log("Exception: " + ex.ToString());
                return false;
            }
        }

        public bool AllowRunInSpace
        {
            get { return GetValue(() => AllowRunInSpace); }
            set { SetValue(() => AllowRunInSpace, value); }
        }

        public bool AllowRunInStation
        {
            get { return GetValue(() => AllowRunInStation); }
            set { SetValue(() => AllowRunInStation, value); }
        }

        public bool AllowRunInAbyssalSpace
        {
            get { return GetValue(() => AllowRunInAbyssalSpace); }
            set { SetValue(() => AllowRunInAbyssalSpace, value); }
        }

        private List<Type> _dependsOn;

        public List<Type> DependsOn
        {
            get => _dependsOn;
            set => _dependsOn = value;
        }

        #region Implementation of IController

        [Browsable(false)]
        public DateTime LocalPulse
        {
            get { return GetValue(() => LocalPulse); }
            set { SetValue(() => LocalPulse, value); }
        }

        #endregion

        public void SetDuration(ulong d)
        {
            Last = d;
            if (d > Max)
                Max = d;
            AvgNumeric = (AvgNumeric * _executionCount + d) / (_executionCount + 1);
            _executionCount++;
        }

        protected int GetRandom(int minValue, int maxValue)
        {
            return Rnd.Next(minValue, maxValue) * RandomFactor;
        }

        protected DateTime UTCNowAddSeconds(int minDelayInSeconds, int maxDelayInSeconds)
        {
            return DateTime.UtcNow.AddMilliseconds(GetRandom(minDelayInSeconds * 1000, maxDelayInSeconds * 1000));
        }

        protected DateTime UTCNowAddMilliseconds(int minDelayInMilliseconds, int maxDelayInMilliseconds)
        {
            return DateTime.UtcNow.AddMilliseconds(GetRandom(minDelayInMilliseconds, maxDelayInMilliseconds));
        }

        public ulong Last // last controller execution duration
        {
            get { return GetValue(() => Last); }
            set { SetValue(() => Last, value); }
        }

        public static void Log(string msg, Color? col = null, [CallerMemberName] string DescriptionOfWhere = "")
        {
            Logging.Log.WriteLine(msg, col, DescriptionOfWhere);
        }

        public static void RemoteLog(string msg)
        {
            WCFClient.Instance.GetPipeProxy.RemoteLog(msg);
        }

        public static void LocalAndRemoteLog(string msg, Color? col = null, [CallerMemberName] string DescriptionOfWhere = "")
        {
            Log(msg, col, DescriptionOfWhere);
            RemoteLog(msg);
        }

        public static void MeasureTime(Action a, bool disableLog = false)
        {
            using (new DisposableStopwatch(t =>
            {
                if (!disableLog)
                {
                    Log($"{1000000 * t.Ticks / Stopwatch.Frequency}  µs elapsed.");
                    Log($"{1000000 * t.Ticks / Stopwatch.Frequency / 1000} ms elapsed.");
                }
            }))
            {
                a.Invoke();
            }
        }
        /// <summary>
        /// Usage example: SendBroadcastMessage("*", nameof(BackgroundWorkerController), "Hello", "World")
        /// </summary>
        /// <param name="receiver">Wildcard '*' or charname</param>
        /// <param name="targetController">nameof(BackgroundWorkerController)</param>
        /// <param name="command">any string</param>
        /// <param name="parameter">any string</param>
        public void SendBroadcastMessage(string receiver, string targetController, string command, string parameter)
        {
            Task.Run(() =>
            {
                try
                {
                    var bm = new BroadcastMessage(ESCache.Instance.CharName, receiver, targetController, command, parameter);
                    WCFClient.Instance.GetPipeProxy.SendBroadcastMessage(bm);
                }
                catch (Exception ex)
                {
                    Logging.Log.WriteLine(ex.ToString());
                }
            });
        }

        public void SendBroadcastMessage<T>(string receiver, string targetController, string command, T parameter)
        {
            Task.Run(() =>
            {
                try
                {
                    if (parameter is string)
                    {
                        SendBroadcastMessage(receiver, targetController, command, parameter as string);
                        return;
                    }

                    var bm = new BroadcastMessage(ESCache.Instance.CharName, receiver, targetController, command, parameter);
                    WCFClient.Instance.GetPipeProxy.SendBroadcastMessage(bm);
                }
                catch (Exception ex)
                {
                    Logging.Log.WriteLine(ex.ToString());
                }
            });
        }

        public abstract void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage);
    }
}