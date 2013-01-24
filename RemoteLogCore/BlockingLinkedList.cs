using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace RemoteLogCore
{
    public class BlockingLinkedList<T>
    {
        private List<T> mData = new List<T>(64);

        public void Add(T item)
        {
            lock (mData)
            {
                mData.Add(item);
                Monitor.PulseAll(mData);
            }
        }

        public void Clean()
        {
            lock (mData)
            {
                mData.Clear();
            }
        }

        public T First()
        {
            T item = default(T);
            lock (mData)
            {
                if (mData.Count == 0)
                {
                    Monitor.Wait(mData);
                }
                item = mData.First();                
            }
            return item;
        }

        public bool Remove(T item)
        {
            lock (mData)
            {
                return mData.Remove(item);
            }
        }

        public int Count
        {
            get
            {
                int size = 0;
                lock (mData)
                {
                    size = mData.Count();
                }
                return size;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }        
    }
}
