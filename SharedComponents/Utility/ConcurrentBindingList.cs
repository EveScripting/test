using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using System.Threading;
using System.Xml.Serialization;

namespace SharedComponents.Utility
{
    [PermissionSet(SecurityAction.LinkDemand,
        XML =
            "<PermissionSet class=\"System.Security.PermissionSet\"\r\nversion=\"1\">\r\n<IPermission class=\"System.Security.Permissions.HostProtectionPermission, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\"\r\nversion=\"1\"\r\nResources=\"SharedState\"/>\r\n</PermissionSet>\r\n"
    )]
    [Serializable]
    public class ConcurrentBindingList<T> : IList<T>, ICollection<T>, IEnumerable<T>, ICollection, IEnumerable, IList, IBindingList, ICancelAddNew,
        IRaiseItemChangedEvents
    {
        private readonly IBindingList list;
        private readonly IList<ListChangedEventArgs> prevEvents = new List<ListChangedEventArgs>();

        private bool canPush = true;

        private ListChangedEventHandler listChanged;

        public ConcurrentBindingList() : this(new BindingList<T>())
        {
            RaiseListChangedEvents = true;
        }

        private ConcurrentBindingList(IBindingList bindingList)
        {
            list = bindingList ?? new BindingList<T>();
            list.ListChanged += delegate (object sender, ListChangedEventArgs e)
            {
                if (!RaiseListChangedEvents)
                    return;

                ListChangedEventArgs listChangedEventArgs = null;
                ListChangedEventHandler listChangedEventHandler = null;
                lock (list)
                {
                    if (canPush)
                    {
                        listChangedEventArgs = e;
                        listChangedEventHandler = listChanged != null ? (ListChangedEventHandler)listChanged.Clone() : null;
                    }
                    else
                    {
                        prevEvents.Add(e);
                    }
                }
                if (listChangedEventHandler != null && listChangedEventArgs != null)
                    listChangedEventHandler(this, listChangedEventArgs);
            };
        }

        public bool RaiseListChangedEvents { get; set; }

        public event ListChangedEventHandler ListChanged
        {
            add
            {
                lock (list)
                {
                    listChanged = (ListChangedEventHandler)Delegate.Combine(listChanged, value);
                }
            }
            remove
            {
                lock (list)
                {
                    listChanged = (ListChangedEventHandler)Delegate.Remove(listChanged, value);
                }
            }
        }

        public bool AllowNew
        {
            get
            {
                bool allowNew;
                lock (list)
                {
                    allowNew = list.AllowNew;
                }
                return allowNew;
            }
            set
            {
                lock (list)
                {
                    ((BindingList<T>)list).AllowNew = value;
                }
            }
        }

        public bool AllowEdit
        {
            get
            {
                bool allowEdit;
                lock (list)
                {
                    allowEdit = list.AllowEdit;
                }
                return allowEdit;
            }
            set
            {
                lock (list)
                {
                    ((BindingList<T>)list).AllowEdit = value;
                }
            }
        }

        public bool AllowRemove
        {
            get
            {
                bool allowRemove;
                lock (list)
                {
                    allowRemove = list.AllowRemove;
                }
                return allowRemove;
            }
            set
            {
                lock (list)
                {
                    ((BindingList<T>)list).AllowRemove = value;
                }
            }
        }

        public bool SupportsChangeNotification
        {
            get
            {
                bool supportsChangeNotification;
                lock (list)
                {
                    supportsChangeNotification = list.SupportsChangeNotification;
                }
                return supportsChangeNotification;
            }
        }

        public bool SupportsSearching
        {
            get
            {
                bool supportsSearching;
                lock (list)
                {
                    supportsSearching = list.SupportsSearching;
                }
                return supportsSearching;
            }
        }

        public bool SupportsSorting
        {
            get
            {
                bool supportsSorting;
                lock (list)
                {
                    supportsSorting = list.SupportsSorting;
                }
                return supportsSorting;
            }
        }

        public bool IsSorted
        {
            get
            {
                bool isSorted;
                lock (list)
                {
                    isSorted = list.IsSorted;
                }
                return isSorted;
            }
        }

        public PropertyDescriptor SortProperty
        {
            get
            {
                PropertyDescriptor sortProperty;
                lock (list)
                {
                    sortProperty = list.SortProperty;
                }
                return sortProperty;
            }
        }

        public ListSortDirection SortDirection
        {
            get
            {
                ListSortDirection sortDirection;
                lock (list)
                {
                    sortDirection = list.SortDirection;
                }
                return sortDirection;
            }
        }

        public object AddNew()
        {
            object result;
            using (new NotificationHelper(this))
            {
                result = list.AddNew();
            }
            return result;
        }

        public void AddIndex(PropertyDescriptor property)
        {
            using (new NotificationHelper(this))
            {
                list.AddIndex(property);
            }
        }

        public void ApplySort(PropertyDescriptor property, ListSortDirection direction)
        {
            using (new NotificationHelper(this))
            {
                list.ApplySort(property, direction);
            }
        }

        public int Find(PropertyDescriptor property, object key)
        {
            int result;
            lock (list)
            {
                result = list.Find(property, key);
            }
            return result;
        }

        public void RemoveIndex(PropertyDescriptor property)
        {
            using (new NotificationHelper(this))
            {
                list.RemoveIndex(property);
            }
        }

        public void RemoveSort()
        {
            using (new NotificationHelper(this))
            {
                list.RemoveSort();
            }
        }

        public void CancelNew(int itemIndex)
        {
            using (new NotificationHelper(this))
            {
                ((BindingList<T>)list).CancelNew(itemIndex);
            }
        }

        public void EndNew(int itemIndex)
        {
            using (new NotificationHelper(this))
            {
                ((BindingList<T>)list).EndNew(itemIndex);
            }
        }

        public object SyncRoot => this;

        public bool IsSynchronized => true;

        public void CopyTo(Array array, int index)
        {
            lock (list)
            {
                list.CopyTo(array, index);
            }
        }

        object IList.this[int index]
        {
            get
            {
                object result;
                lock (list)
                {
                    result = list[index];
                }
                return result;
            }
            set
            {
                using (new NotificationHelper(this))
                {
                    list[index] = value;
                }
            }
        }

        public bool IsFixedSize
        {
            get
            {
                bool isFixedSize;
                lock (list)
                {
                    isFixedSize = list.IsFixedSize;
                }
                return isFixedSize;
            }
        }

        public int Add(object value)
        {
            int result;
            using (new NotificationHelper(this))
            {
                result = list.Add(value);
            }
            return result;
        }

        public bool Contains(object value)
        {
            bool result;
            lock (list)
            {
                result = list.Contains(value);
            }
            return result;
        }

        public int IndexOf(object value)
        {
            int result;
            lock (list)
            {
                result = list.IndexOf(value);
            }
            return result;
        }

        public void Insert(int index, object value)
        {
            using (new NotificationHelper(this))
            {
                list.Insert(index, value);
            }
        }

        public void Remove(object value)
        {
            using (new NotificationHelper(this))
            {
                list.Remove(value);
            }
        }

        public int Count
        {
            get
            {
                int count;
                lock (list)
                {
                    count = list.Count;
                }
                return count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                bool isReadOnly;
                lock (list)
                {
                    isReadOnly = list.IsReadOnly;
                }
                return isReadOnly;
            }
        }

        public T this[int index]
        {
            get
            {
                T result;
                lock (list)
                {
                    result = ((BindingList<T>)list)[index];
                }
                return result;
            }
            set
            {
                using (new NotificationHelper(this))
                {
                    ((BindingList<T>)list)[index] = value;
                }
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Clear()
        {
            using (new NotificationHelper(this))
            {
                list.Clear();
            }
        }

        public void RemoveAt(int index)
        {
            using (new NotificationHelper(this))
            {
                list.RemoveAt(index);
            }
        }

        public void Add(T item)
        {
            using (new NotificationHelper(this))
            {
                ((BindingList<T>)list).Add(item);
            }
        }

        public bool Contains(T item)
        {
            bool result;
            lock (list)
            {
                result = ((BindingList<T>)list).Contains(item);
            }
            return result;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (list)
            {
                ((BindingList<T>)list).CopyTo(array, arrayIndex);
            }
        }

        public bool Remove(T item)
        {
            bool result;
            using (new NotificationHelper(this))
            {
                result = ((BindingList<T>)list).Remove(item);
            }
            return result;
        }

        public int IndexOf(T item)
        {
            int result;
            lock (list)
            {
                result = ((BindingList<T>)list).IndexOf(item);
            }
            return result;
        }

        public void Insert(int index, T item)
        {
            using (new NotificationHelper(this))
            {
                ((BindingList<T>)list).Insert(index, item);
            }
        }

        bool IRaiseItemChangedEvents.RaisesItemChangedEvents => ((IRaiseItemChangedEvents)list).RaisesItemChangedEvents;

        public ConcurrentBindingList<T> XmlDeserialize(string fileName)
        {
            using (CrossProcessLockFactory.CreateCrossProcessLock(fileName.Replace(@"\", string.Empty).Replace(@"/", string.Empty)))
            {
                var data = File.ReadAllText(fileName);
                var xmlSer = new XmlSerializer(typeof(ConcurrentBindingList<T>));

                object obj;
                try
                {
                    var value = CompressUtil.DecompressText(data);
                    TextReader reader = new StringReader(value);
                    //File.WriteAllText(fileName + ".raw", value);
                    obj = xmlSer.Deserialize(reader);
                }
                catch (Exception)
                {
                    try
                    {
                        TextReader reader = new StringReader(data);
                        obj = xmlSer.Deserialize(reader);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception: " + e.ToString());
                        return null;
                    }
                }
                return (ConcurrentBindingList<T>)obj;
            }
        }

        public void XmlSerialize(string fileName, bool compress = true, [CallerMemberName] string caller = "")
        {
            using (CrossProcessLockFactory.CreateCrossProcessLock(fileName.Replace(@"\", string.Empty).Replace(@"/", string.Empty)))
            {
                using (var file = new StreamWriter(fileName))
                {
                    var xmlSer = new XmlSerializer(GetType());
                    var textWriter = new StringWriter();
                    xmlSer.Serialize(textWriter, this);
                    xmlSer = null;
                    var value = compress ? CompressUtil.Compress(textWriter.ToString()) : textWriter.ToString();
                    file.Write(value);
                }
            }
        }

        private class Enumerator : IEnumerator<T>, IEnumerator, IDisposable
        {
            private readonly IEnumerator<T> _enumerator;

            private readonly ConcurrentBindingList<T> _owner;

            public Enumerator(ConcurrentBindingList<T> owner)
            {
                _owner = owner;
                _enumerator = ((BindingList<T>)owner.list).GetEnumerator();
            }

            public T Current
            {
                get
                {
                    T current;
                    lock (_owner.list)
                    {
                        current = _enumerator.Current;
                    }
                    return current;
                }
            }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                lock (_owner.list)
                {
                    _enumerator.Dispose();
                }
            }

            public bool MoveNext()
            {
                bool result;
                lock (_owner.list)
                {
                    result = _enumerator.MoveNext();
                }
                return result;
            }

            public void Reset()
            {
                lock (_owner.list)
                {
                    _enumerator.Reset();
                }
            }
        }

        private sealed class NotificationHelper : IDisposable
        {
            private readonly ConcurrentBindingList<T> _owner;

            public NotificationHelper(ConcurrentBindingList<T> owner)
            {
                _owner = owner;
                Monitor.Enter(_owner.list);
                _owner.canPush = false;
            }

            public void Dispose()
            {
                try
                {
                    var listChangedEventHandler = _owner.prevEvents.Count > 0 && _owner.listChanged != null
                        ? (ListChangedEventHandler)_owner.listChanged.Clone()
                        : null;
                    var listChangedEventArgs = listChangedEventHandler != null && _owner.prevEvents.Count == 1 ? _owner.prevEvents[0] : null;
                    var list = listChangedEventHandler != null && _owner.prevEvents.Count > 1 ? new List<ListChangedEventArgs>(_owner.prevEvents) : null;
                    _owner.prevEvents.Clear();
                    _owner.canPush = true;
                    Monitor.Exit(_owner.list);
                    if (listChangedEventHandler != null)
                    {
                        if (listChangedEventArgs != null)
                        {
                            listChangedEventHandler(_owner, listChangedEventArgs);
                            return;
                        }
                        if (list != null)
                            foreach (var current in list)
                                listChangedEventHandler(_owner, current);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }
    }
}