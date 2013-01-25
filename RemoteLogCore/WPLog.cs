using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace RemoteLogCore
{
    public class WPLog : ILog
    {
        private static WPLog _self = new WPLog();


        public static WPLog Log
        {
            get
            {
                return _self;
            }
        }
        
        public void I(object source, string msg)
        {
            Debug.WriteLine(Format(source, "Info", msg));
        }

        public void V(object source, string msg)
        {
            Debug.WriteLine(Format(source, "Verbose", msg));
        }

        public void D(object source, string msg)
        {
            Debug.WriteLine(Format(source, "Debug", msg));
        }

        public void E(object source, string msg)
        {
            Debug.WriteLine(Format(source, "Error", msg));
        }

        public void E(object source, Exception t)
        {
            Debug.WriteLine(Format(source, "Error", t.Message));
            Debug.WriteLine(RemoteLog.GetStackTrace(t));
        }

        public void N(object source, string msg)
        {
            Debug.WriteLine(Format(source, "Notify", msg));
        }

        public void W(object source, string msg)
        {
            Debug.WriteLine(Format(source, "Warning", msg));
        }

        public void Wtf(object source, string msg)
        {
            Debug.WriteLine(Format(source, "WTF", msg));
        }

        public static string Format(object source, string category, string msg)
        {
            return String.Format("{0}/{1} [{2}] {3}", DateTime.Now.ToString("HH:mm:ss.fff"), category, source.GetType().Name, msg);
        }
    }
}
