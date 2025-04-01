/*
 * ---------------------------------------
 * User: duketwo
 * Date: 19.03.2014
 * Time: 18:51
 * 
 * ---------------------------------------
 */

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using SharedComponents.EVE;

namespace SharedComponents.Utility
{
    /// <summary>
    ///     Description of SerializeableSortableBindingList.
    /// </summary>
    [Serializable]
    public class SerializeableSortableBindingList<T> where T : class
    {
        private string _assemblyPath;
        protected ConcurrentBindingList<T> _List;

        public SerializeableSortableBindingList(string fileName, int writeDelayMs, bool autoSave = false, int autoSaveDelayMs = 10000)
        {
            _lastSerializedLock = new Object();
            FilePathName = fileName;
            LastSerialize = DateTime.UtcNow;
            FileName = Path.GetFileName(FilePathName);

            _List = new ConcurrentBindingList<T>();
            WriteDelayMs = writeDelayMs;

            if (!File.Exists(FilePathName))
            {
                _List = new ConcurrentBindingList<T>();

                _List.XmlSerialize(FilePathName);
                _List.XmlDeserialize(FilePathName);
            }
            else
            {
                _List = _List.XmlDeserialize(FilePathName);
            }
            _List.ListChanged += OnListChangeHandler;

            if (autoSave)
                new Thread(() =>
                {
                    while (true)
                        try
                        {
                            if (Cache.IsShuttingDown)
                            {
                                Console.WriteLine("Saving XML because the laucher is terminating.");
                                OnListChange(true);
                                break;
                            }

                            if (DateTime.UtcNow >= LastSerialize.AddMilliseconds(autoSaveDelayMs))
                            {
                                LastSerialize = DateTime.UtcNow;
                                OnListChange();
                            }
                            Thread.Sleep(1000);
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e.ToString());
                            continue;
                        }
                }).Start();
        }

        public string FilePathName { get; set; }
        private string FileName { get; set; }

        private readonly Object _lastSerializedLock;
        private DateTime _lastSerialize;
        protected DateTime LastSerialize
        {
            get { lock (_lastSerializedLock) return _lastSerialize; }
            set { lock (_lastSerializedLock) _lastSerialize = value; }
        }

        private int WriteDelayMs { get; set; }

        public ConcurrentBindingList<T> List => _List;

        public string AssemblyPath
        {
            get
            {
                if (_assemblyPath == null)
                    _assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return _assemblyPath;
            }
        }

        private void OnListChangeHandler(object sender, ListChangedEventArgs e)
        {
            OnListChange();
        }

        private void OnListChange(bool force = false)
        {
            if (!force && WriteDelayMs > 0 && DateTime.UtcNow < LastSerialize.AddMilliseconds(WriteDelayMs))
                return;
            try
            {
                LastSerialize = DateTime.UtcNow;
                _List.XmlSerialize(FilePathName);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }
    }
}