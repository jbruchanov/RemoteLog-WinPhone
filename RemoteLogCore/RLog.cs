﻿using RemoteLogCore.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace RemoteLogCore
{
    public class RLog
    {
        private const char SEPARATOR = '|';

        public const int TURN_OFF = 0;
        public const int INFO = 1 << 1;
        public const int VERBOSE = 1 << 2;
        public const int DEBUG = 1 << 3;
        public const int WARNING = 1 << 4;
        public const int ERROR = 1 << 5;
        public const int EXCEPTION = 1 << 6;
        public const int WTF = 1 << 7;
        public const int SCREENSHOT = 1 << 8;

        public const int ALL = INFO | VERBOSE | DEBUG | WARNING | ERROR | EXCEPTION | WTF | SCREENSHOT;

        private static int sMode = EXCEPTION;

        private static ILog sLog = null;

        public static void SetLog(ILog log)
        {
            sLog = log;
        }

        public static void N(object source, string category, string msg)
        {
            if (sMode != TURN_OFF)
            {
                Send(source, category, msg);
            }
            if (sLog != null)
            {
                sLog.N(source, msg);
            }
        }

        public static void I(object source, string msg, string category = "Info")
        {
            if ((Mode & INFO) == INFO)
            {
                Send(source, category, msg);
            }
            if (sLog != null)
            {
                sLog.I(source, msg);
            }
        }

        public static void V(object source, string msg, string category = "Verbose")
        {
            if ((Mode & VERBOSE) == VERBOSE)
            {
                Send(source, category, msg);
            }
            if (sLog != null)
            {
                sLog.V(source, msg);
            }
        }

        public static void D(object source, string msg, string category = "Debug")
        {
            if ((Mode & DEBUG) == DEBUG)
            {
                Send(source, category, msg);
            }
            if (sLog != null)
            {
                sLog.D(source, msg);
            }
        }

        public static void E(object source, Exception t, string msg = null, string category = "Error")
        {
            if ((Mode & ERROR) == ERROR)
            {
                Send(source,
                    category,
                    String.IsNullOrEmpty(msg) ? GetMessageOrClassName(t) : msg,
                    new LogItemBlobRequest(LogItemBlobRequest.MIME_TEXT_PLAIN, "error.txt", RemoteLog.GetStackTrace(t)));
            }
            if (sLog != null)
            {
                sLog.E(source, msg);
            }
        }

        public static void E(object source, string msg, string category = "Error")
        {
            if ((Mode & ERROR) == ERROR)
            {
                Send(source, category, msg);
            }
            if (sLog != null)
            {
                sLog.I(source, msg);
            }
        }

        private static string GetMessageOrClassName(Exception t)
        {
            String s = t.Message;
            if (!String.IsNullOrEmpty(s))
            {
                s = t.GetType().Name;
            }
            return s;
        }


        public static void W(object source, string msg, string category = "Warning")
        {
            if ((Mode & WARNING) == WARNING)
            {
                Send(source, category, msg);
            }
            if (sLog != null)
            {
                sLog.W(source, msg);
            }
        }

        public static void Wtf(object source, string msg, string category = "WTF")
        {
            if ((Mode & WTF) == WTF)
            {
                Send(source, category, msg);
            }
            if (sLog != null)
            {
                sLog.Wtf(source, msg);
            }
        }

        /// <summary>
        /// Take a screenshot of whole app window
        /// Handles cross-thread access problem.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="msg"></param>
        public static void TakeScreenshot(object source, string msg = null)
        {            
            Dispatcher d = Deployment.Current.Dispatcher;            
            d.BeginInvoke(new Action(() =>
                { TakeScreenshot(source, Application.Current.RootVisual as FrameworkElement, msg); }
            ));
        }

        /// <summary>
        /// Take a screenshot of particular UI element 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="element"></param>
        /// <param name="msg"></param>
        public static void TakeScreenshot(object source, FrameworkElement element, string msg = null)
        {
            // get a writable bitmap ready
            WriteableBitmap wbmp = new WriteableBitmap((int)element.ActualWidth, (int)element.ActualHeight);
            // set up a transform with no scale (ie 1x scale both sides)
            ScaleTransform transform = new System.Windows.Media.ScaleTransform();
            transform.ScaleX = 1;
            transform.ScaleY = 1;
            // render the application root into the bitmap
            wbmp.Render(element, transform);
            wbmp.Invalidate();
            // create a memory stream, save the bitmap as a JPEG byte stream into it
            MemoryStream stream = new MemoryStream();
            wbmp.SaveJpeg(stream, wbmp.PixelWidth, wbmp.PixelHeight, 0, 100);
            // setup the media library, save the JPEG bytes under a unique file name
            Send(typeof(RLog), "ScreenShot", msg, new LogItemBlobRequest(LogItemBlobRequest.MIME_IMAGE_JPEG, "screenshot.jpg", stream.ToArray()));
        }

        /// <summary>
        /// Generic implementation of sending
        /// </summary>
        /// <param name="source">Any kind of object used like source on web client</param>
        /// <param name="category">Base category for better filtering</param>
        /// <param name="msg"></param>
        /// <param name="libr">Optional blob as attachment</param>
        public static void Send(object source, string category, string msg, LogItemBlobRequest libr = null)
        {            
            if (sMode == TURN_OFF)
            {
                return;
            }
            LogItem li = RemoteLog.CreateLogItem();
            if (li == null)
            {// not yet initialized
                return;
            }
            li.Category = category;
            li.Message = msg;
            if (source != null)
            {
                string n = source.GetType().Name;
                if (String.IsNullOrEmpty(n))
                {
                    n = "AnonymousClass";
                }
                li.Source = n;
            }

            LogSender ls = RemoteLog.LogSender;
            if (ls != null)
            {
                ls.AddLogItem(li, libr);
            }
        }

        public static int Mode
        {
            get
            {
                return sMode;
            }
            set
            {
                sMode = value;
            }
        }


        /// <summary>
        /// Parse string to numeric mode related to modes
        /// </summary>
        /// <param name="values">String exactly name as mode ig. VERBOSE, can be multiple values separeted by '|' ig VERBOSE|ERROR or just decimal value of modes</param>
        /// <returns></returns>
        public static int ParseMode(String values) 
        {
	        int result = 0;
	        bool found = false;
	        values = values.Trim();
	        if (!String.IsNullOrEmpty(values)) 
            {
                //try numeric value
		        int iv = -1;
                if (int.TryParse(values, out iv) && (iv <= ALL && iv >= TURN_OFF))
                {
                    result = iv;
		            return result;
		        }

                //try string values
                int subvalue = 0;
	            // array
                string[] vs = values.Split(new char[] { SEPARATOR });
                foreach (string v in vs)
                {
                    subvalue = ParseModeValue(v);
                    if (subvalue != -1)
                    {
                        found = true;
                        result |= subvalue;
                    }
                }
	        }
	        return found ? result : -1;
        }

        protected static int ParseModeValue(String value)
        {
            switch (value)
            {
                case "TURN_OFF": return TURN_OFF;
                case "INFO": return INFO;
                case "VERBOSE": return VERBOSE;
                case "DEBUG": return DEBUG;
                case "WARNING": return WARNING;
                case "ERROR": return ERROR;
                case "EXCEPTION": return EXCEPTION;
                case "WTF": return WTF;
                case "SCREENSHOT": return SCREENSHOT;
                case "ALL": return ALL;
                default: return -1;    
            }
        }
    }
}
