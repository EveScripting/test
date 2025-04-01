using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace SharedComponents.Utility.AsyncLogQueue
{
    public class AsyncLogQueue : IDisposable
    {
        public delegate void Message(string msg, Color? col);
        private ConcurrentQueue<LogEntry> _LogEntryQueue = new ConcurrentQueue<LogEntry>();
        private BackgroundWorker _Logger = new BackgroundWorker();

        public AsyncLogQueue()
        {
            _Logger.WorkerSupportsCancellation = false;
            _Logger.DoWork += new DoWorkEventHandler(_Logger_DoWork);
        }

        ~AsyncLogQueue()
        {
            sema.Dispose();
        }

        public string File { get; set; }

        public event Message OnMessage;

        private object lockObj = new object();

        public bool IsSubscribed => OnMessage != null;

        public void Enqueue(LogEntry le, bool startWorkerOnly = false)
        {
            try
            {
                if (!startWorkerOnly)
                {
                    var msg = String.Format("[{0:dd-MMM-yy HH:mm:ss:fff}] {1}", DateTime.UtcNow, "[" + le.DescriptionOfWhere + "] " + le.Message);
                    le.Message = msg;
                    _LogEntryQueue.Enqueue(le);
                }
                lock (lockObj) // lock because it rarely can happen that two threads call runWorkerAsync
                {
                    if (!_LogEntryQueue.IsEmpty && !_Logger.IsBusy)
                        _Logger.RunWorkerAsync();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
            }
        }

        public void StartWorker()
        {
            Enqueue(null, true);
        }

        private SemaphoreSlim sema = new SemaphoreSlim(1, 1);

        private void _Logger_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                if (!IsSubscribed)
                {
                    // Console.WriteLine("Not subscried yet.");
                    return;
                }

                while (!_LogEntryQueue.IsEmpty && _LogEntryQueue.TryDequeue(out var le))
                {

                    OnMessage?.Invoke(le.Message, le.Color);
                    if (!string.IsNullOrEmpty(File))
#pragma warning disable CS4014 // Da dieser Aufruf nicht abgewartet wird, wird die Ausführung der aktuellen Methode fortgesetzt, bevor der Aufruf abgeschlossen ist
                        Util.WriteTextAsync(File, le.Message + Environment.NewLine, sema);
#pragma warning restore CS4014 // Da dieser Aufruf nicht abgewartet wird, wird die Ausführung der aktuellen Methode fortgesetzt, bevor der Aufruf abgeschlossen ist
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
            }
        }

        public void Dispose()
        {
            sema.Dispose();
        }
    }
}