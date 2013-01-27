using RemoteLogCore.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace RemoteLogCore
{
    public class LogSender
    {
        private ServiceConnector mServiceConnector;

        private static Thread mThread;

        /** Items to send **/
        private readonly BlockingLinkedList<LogItem> mItems = new BlockingLinkedList<LogItem>();

        /** Coodata for items */
        private readonly Dictionary<LogItem, LogItemBlobRequest> mCoData = new Dictionary<LogItem, LogItemBlobRequest>();

        private bool mIsRunning = false;

        public LogSender(ServiceConnector serviceConnector)
        {
            mServiceConnector = serviceConnector;
            CreateWorkingThread();
        }

        private void CreateWorkingThread()
        {
            if (mThread != null)
            {
                throw new InvalidOperationException("RemoteLogThread already created");
            }
            mIsRunning = true;
            mThread = new Thread(new ThreadStart(() =>
                {
                    WorkingThreadImpl();
                }));
            mThread.Name = "RemoteLogThread";
            mThread.Start();
        }

        private void WorkingThreadImpl()
        {
            while (mIsRunning)
            {
                LogItem li = null;
                try
                {
                    li = mItems.First();
                    LogItem saved = mServiceConnector.SendItem(li).Context;
                    if (mCoData.ContainsKey(li))
                    {
                        LogItemBlobRequest blobReq = mCoData[li];
                        mCoData.Remove(li);
                        blobReq.LogItemID = saved.ID;
                        mServiceConnector.SendItem(blobReq);
                        //delete unhandled exception from storage
                        if (blobReq.IsUnhandledException)
                        {
                            RLSettings.UnhandledExceptionStack = "";
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                    //someone closed us, maybe app is closing
                }
                catch (Exception e)
                {
                    RLog.E(this, e, "Sending logitem");
                    /*ignore error and continue*/
                }
                finally
                {
                    if (li != null)
                    {
                        mItems.Remove(li);
                        mCoData.Remove(li);
                    }
                }
            }
        }

        public void AddLogItem(LogItem item, LogItemBlobRequest blob = null)
        {
            if (blob != null)
            {
                mCoData.Add(item, blob);
            }
            mItems.Add(item);
        }

        public void Stop()
        {
            mIsRunning = false;
        }

        public void WaitForEmptyQueue()
        {
            while (mItems.Count != 0)
            {
                System.Threading.Thread.Sleep(100);
            }
        }
    }
}
