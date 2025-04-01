extern alias SC;

using EVESharpCore.Cache;
using SC::SharedComponents.Utility;
using SC::SharedComponents.Utility.AsyncLogQueue;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using SC::SharedComponents.IPC;
using SC::SharedComponents.EVE;
using EVESharpCore.Lookup;
using System.Windows.Forms;
using EVESharpCore.Questor.States;

namespace EVESharpCore.Logging
{
    public static class Log
    {
        #region Fields

        public static bool ConsoleDirectoryExist = false;

        private static AsyncLogQueue _asyncLogQueue = new AsyncLogQueue();
        private static AsyncLogQueue _eachRoomOrSpawnAsyncLogQueue = new AsyncLogQueue();
        private static AsyncLogQueue _travelerAsyncLogQueue = new AsyncLogQueue();

        #endregion Fields

        #region Properties

        private static string _characterName { get; set; }
        public static AsyncLogQueue AsyncLogQueue => _asyncLogQueue;
        public static AsyncLogQueue TravelerAsyncLogQueue => _travelerAsyncLogQueue;

        public static string CharacterName
        {
            get
            {
                if (string.IsNullOrEmpty(_characterName))
                    _characterName = ESCache.Instance.EveAccount.CharacterName;
                return _characterName;
            }
        }



        private static string ConsoleLogFile
        {
            get
            {
                if (ESCache.Instance.EveAccount.ConnectToTestServer)
                {
                    return Path.Combine(ConsoleLogPath, string.Format("{0:MM-dd-yyyy}", DateTime.Today) + "-" + CharacterName + "-" + "console-SISI" + ".log");
                }

                return Path.Combine(ConsoleLogPath, string.Format("{0:MM-dd-yyyy}", DateTime.Today) + "-" + CharacterName + "-" + "console" + ".log");
            }
        }

        private static string TravelerLogFile
        {
            get
            {
                if (ESCache.Instance.EveAccount.ConnectToTestServer)
                {
                    return Path.Combine(ConsoleLogPath, string.Format("{0:MM-dd-yyyy}", DateTime.Today) + "-" + CharacterName + "-" + "console-SISI" + ".log");
                }

                return Path.Combine(ConsoleLogPath, string.Format("{0:MM-dd-yyyy}", DateTime.Today) + "-" + CharacterName + "-" + "traveler" + ".log");
            }
        }


        public static string WindowEventLogFile => Path.Combine(ConsoleLogPath,
          string.Format("{0:MM-dd-yyyy}", DateTime.Today) + "-" + CharacterName + "-" + "WndEvent" + ".log");

        public static string ConsoleLogPath
        {
            get
            {
                if (string.IsNullOrEmpty(_consoleLogPath))
                    _consoleLogPath = Path.Combine(BotLogpath, "Console\\");
                return _consoleLogPath;
            }
        }

        public static string BotLogpath
        {
            get
            {
                if (string.IsNullOrEmpty(_logPath))
                    _logPath = Util.AssemblyPath + "\\Logs\\" + Log.CharacterName + "\\";
                return _logPath;
            }
        }

        private static string _consoleLogPath { get; set; }
        private static string _logPath { get; set; }

        #endregion Properties

        #region Methods

        public static void CreateConsoleDirectory()
        {
            if (ConsoleLogPath != null && ConsoleLogFile != null)
                if (!string.IsNullOrEmpty(ConsoleLogFile))
                {
                    Directory.CreateDirectory(ConsoleLogPath);
                    if (Directory.Exists(ConsoleLogPath)) ConsoleDirectoryExist = true;
                }
        }

        public static string FilterPath(string path)
        {
            try
            {
                if (path == null)
                    return string.Empty;

                path = path.Replace("\"", "");
                path = path.Replace("?", "");
                path = path.Replace("\\", "");
                path = path.Replace("/", "");
                path = path.Replace("'", "");
                path = path.Replace("*", "");
                path = path.Replace(":", "");
                path = path.Replace(">", "");
                path = path.Replace("<", "");
                path = path.Replace(".", "");
                path = path.Replace(",", "");
                path = path.Replace("'", "");
                path = path.Replace("\'", "");
                while (path.IndexOf("  ", StringComparison.Ordinal) >= 0)
                    path = path.Replace("  ", " ");
                return path.Trim();
            }
            catch (Exception exception)
            {
                WriteLine("Exception [" + exception + "]");
                return null;
            }
        }

        public static void RemoteWriteLine(string s)
        {
            WCFClient.Instance.GetPipeProxy.RemoteLog(s);
        }

        private static string _previousLogLine = string.Empty;

        private static int _intPreviousLogLineRepeats = 0;

        public static void ReallyWriteLine(string line, Color? col = null, [CallerMemberName] string DescriptionOfWhere = "")
        {
            //Todo: fix performance
            if (!ConsoleDirectoryExist)
                CreateConsoleDirectory();

            var consoleLogFile = ConsoleLogFile;
            if (!string.IsNullOrEmpty(consoleLogFile))
            {
                _asyncLogQueue.File = consoleLogFile;
                _asyncLogQueue.Enqueue(new LogEntry(line, DescriptionOfWhere, col));
            }

            if (ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalController) ||
                ESCache.Instance.EveAccount.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController))
            {
                //_eachRoomOrSpawnAsyncLogQueue.File = abyssalSpawnConsoleLogFile;
                //_eachRoomOrSpawnAsyncLogQueue.Enqueue(new LogEntry(line, DescriptionOfWhere, col));
            }

            if (State.CurrentTravelerState == TravelerState.CalculatePath)
            {
                _travelerAsyncLogQueue.File = TravelerLogFile;
                _travelerAsyncLogQueue.Enqueue(new LogEntry(line, DescriptionOfWhere, col));
            }
        }

        private static string abyssalSpawnConsoleLogFile
        {
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return string.Empty;

                if (ESCache.Instance.EveAccount.ConnectToTestServer)
                {
                    if (AbyssalSpawn.DetectSpawn != AbyssalSpawn.AbyssalSpawnType.Undecided)
                    {
                        return Path.Combine(ConsoleLogPath, string.Format("{0:MM-dd-yyyy}", DateTime.Today) + "-" + DateTime.UtcNow.Hour + "-" + ESCache.Instance.DirectEve.Session.Character + "-DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]console-SISI" + ".log");
                    }

                    return string.Empty;
                }

                if (AbyssalSpawn.DetectSpawn != AbyssalSpawn.AbyssalSpawnType.Undecided)
                {
                    Path.Combine(ConsoleLogPath, string.Format("{0:MM-dd-yyyy}", DateTime.Today) + "-" + ESCache.Instance.DirectEve.Session.Character + "-DetectSpawn[" + AbyssalSpawn.DetectSpawn + "]console.log");
                }

                return string.Empty;
            }
        }

        public static void WriteLine(string line, Color? col = null, [CallerMemberName] string DescriptionOfWhere = "")
        {
            if (string.IsNullOrEmpty(line))
                return;

            if (string.IsNullOrWhiteSpace(line))
                return;

            if (!string.IsNullOrEmpty(_previousLogLine) && !string.IsNullOrWhiteSpace(_previousLogLine))
            {
                //
                // no need to log something that is exactly the same as the previous log line
                //
                if (line == _previousLogLine)
                {
                    _intPreviousLogLineRepeats++;
                    return;
                }

                ReallyWriteLine("[repeated x " + _intPreviousLogLineRepeats + " ]" + _previousLogLine, col, DescriptionOfWhere);
                _intPreviousLogLineRepeats = 0;
            }

            ReallyWriteLine(line, col, DescriptionOfWhere);
        }

        #endregion Methods
    }
}