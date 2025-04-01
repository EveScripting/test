extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Logging;
using SC::SharedComponents.IPC;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace EVESharpCore
{
    public static class Program
    {
        #region Fields

        private static ThreadExceptionEventHandler tHandler = new ThreadExceptionEventHandler(Application_ThreadException);

        #endregion Fields

        #region Properties

        public static EVESharpCoreForm EveSharpCoreFormInstance { get; private set; }

        public static bool IsShuttingDown { get; set; }

        #endregion Properties

        #region Methods

        //[STAThread]
        public static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Starting E# Core");
                Console.WriteLine("E# Core: args[0]:[" + args[0] + "]args[1]:[" + args[1] + "]args[2]:[" + args[2] + "]");
                Console.WriteLine("E# Core: Setting ESCache.Instance.CharName to [" + args[0] + "]");

                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                Application.ThreadException += tHandler;

                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

                if (3 > args.Length)
                {
                    Console.WriteLine("if (3 > args.Length)");
                    //Util.TaskKill(Process.GetCurrentProcess().Id, false);
                    return;
                }

                ESCache.Instance.CharName = args[0];
                WCFClient.Instance.CharName = args[0];

                Console.WriteLine("E# Core: WCFClient.Instance.pipeName to [" + args[1] + "]");
                WCFClient.Instance.pipeName = args[1];

                Console.WriteLine("E# Core: WCFClient.Instance.GUID to [" + args[2] + "]");
                WCFClient.Instance.GUID = args[2];

                try
                {
                    WCFClient.Instance.GetPipeProxy.Ping();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception [" + ex + "]");
                }

                if (ESCache.Instance.EveAccount == null)
                {
                    Console.WriteLine("EveAccount not found! How?!");
                    //Util.TaskKill(Process.GetCurrentProcess().Id, false);
                    return;
                }

                Console.WriteLine("Found EveAccount");

                if (string.IsNullOrEmpty(ESCache.Instance.EveAccount.AccountName))
                {
                    Console.WriteLine("if (string.IsNullOrEmpty(ESCache.Instance.EveAccount.AccountName)) !");
                    return;
                }

                if (string.IsNullOrEmpty(ESCache.Instance.EveAccount.CharacterName))
                {
                    Console.WriteLine("if (string.IsNullOrEmpty(ESCache.Instance.EveAccount.CharacterName)) !");
                    return;
                }

                //ESCache.Instance.LoadDirectEVEInstance();

                Console.WriteLine("Launching EVESharpCoreForm");
                EveSharpCoreFormInstance = new EVESharpCoreForm();

                Application.Run(EveSharpCoreFormInstance);
                Console.WriteLine("Exiting.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception [" + ex + "]");
            }

            Console.WriteLine("EVESharpCore is terminating.");
            AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
            Application.ThreadException -= tHandler;
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            HandleException(e.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleException((Exception) e.ExceptionObject);
        }

        private static void HandleException(Exception e)
        {
            Console.WriteLine(e);
            Debug.WriteLine(e);
        }

        #endregion Methods
    }
}