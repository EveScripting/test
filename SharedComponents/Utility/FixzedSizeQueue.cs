/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 29.08.2016
 * Time: 22:03
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System.Collections.Concurrent;

namespace SharedComponents.Utility
{
    /// <summary>
    ///     Description of FixzedSizeQueue.
    /// </summary>
    public class FixedSizedQueue<T> : ConcurrentQueue<T>
    {
        private readonly object syncObject = new object();

        public FixedSizedQueue(int size)
        {
            Size = size;
        }

        public int Size { get; private set; }

        public new void Enqueue(T obj)
        {
            base.Enqueue(obj);
            lock (syncObject)
            {
                while (Count > Size)
                {
                    T outObj;
                    TryDequeue(out outObj);
                }
            }
        }
    }
}