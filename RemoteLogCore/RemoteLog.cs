using Newtonsoft.Json;
using RemoteLogCore.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;

namespace RemoteLogCore
{
    public class RemoteLog
    {
        private const string FORMAT = "yyyy-MM-dd HH:mm:ss.fff";

        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings();

        private static LogSender _logSender;

        private string _appName;

        private string _appVersion;

        private int? _deviceID;

        private static Thread _regThread;

        private static readonly RemoteLog _self = new RemoteLog();


        private static bool _resend = false;

        public static void Resend()
        {
            _resend = true;
        }

        private DeviceDataProvider _deviceDataProvider;

        private ServiceConnector _connector;

        static internal LogSender LogSender
        {
            get
            {
                return _logSender;
            }
            private set
            {
                _logSender = value;
            }
        }

        public static RemoteLog Instance()
        {
            return _logSender != null ? _self : null;
        }

        static RemoteLog()
        {
            Settings.Converters.Add(new MyDateTimeConverter());
        }

        private class MyDateTimeConverter : Newtonsoft.Json.Converters.DateTimeConverterBase
        {
            
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                string v = reader.Value.ToString();
                return DateTime.ParseExact(v, FORMAT, DateTimeFormatInfo.InvariantInfo);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteValue(((DateTime)value).ToString(FORMAT));
            }
        }

        public static void RegisterUnhandledExceptionHandler()
        {
            Application.Current.UnhandledException += new EventHandler<ApplicationUnhandledExceptionEventArgs>((object o, ApplicationUnhandledExceptionEventArgs ea)
                =>
                {                    
                    if ((RLog.Mode & RLog.ERROR) == RLog.ERROR)
                    {
                        Exception ex = ea.ExceptionObject;
                        
                        if(ex is UnhandledExceptionKillApp)
                        {                            
                            return;
                        }
                        else
                        {
                            ea.Handled = true;
                        }
                       
                        //must be in another thread, otherwise sender thread is block for infinite
                        Thread t = new Thread(new ThreadStart(() =>
                        {
                            try
                            {
                                LogItem li = RemoteLog.CreateLogItem();
                                
                                string msg = String.Format("V:{0} Date:{1}\n{2}\n{3}\n\n",
                                    ApplicationInfo.Version,                                    
                                    DateTime.Now.ToString(FORMAT),
                                    ex.Message,
                                    GetStackTrace(ea.ExceptionObject));

                                LogItemBlobRequest libr = new LogItemBlobRequest(LogItemBlobRequest.MIME_TEXT_PLAIN,
                                                                                  "fatalerror.txt",
                                                                                  msg);
                                libr.IsUnhandledException = true;
                                bool isKillApp = ex is KillAppException;

                                //save stack for case a problem during sending
                                if (!isKillApp)
                                {
                                    String oldStack = RLSettings.UnhandledExceptionStack;
                                    oldStack = msg + "\n\n" + oldStack;
                                    RLSettings.UnhandledExceptionStack = oldStack;
                                }

                                RLog.Send(typeof(RemoteLog), isKillApp ? "KillApp" : "UnhandledException", ex.Message, libr);
                                _logSender.WaitForEmptyQueue();
                            }
                            catch (Exception) 
                            {
                                /* just ignore it now */ 
                                // save it for late send
                            }
                            
                            //throw new exception for kill app
                            throw new UnhandledExceptionKillApp();
                        }));
                        t.Name = "UnhandledExceptionSendingThread";
                        t.Start();
                    }
                });
        }

        public static void Init(string appName, string serverLocation, Action callback = null)
        {
            if (_regThread != null)
            {
                throw new InvalidOperationException("Registration already started");
            }
            ServiceConnector sc = new ServiceConnector("http://192.168.168.183:8080/RemoteLogWeb/");

            _self._deviceID = RLSettings.DeviceID;
            _self._appName = ApplicationInfo.Title;
            _self._appVersion = ApplicationInfo.Version;

            // create server connector
            _self._connector = new ServiceConnector(serverLocation);

            if (_self._deviceDataProvider == null)
            {
                _self._deviceDataProvider = new DeviceDataProvider();
            }
            
            //must be called in MainThread
            Device dev = _self._deviceDataProvider.GetDevice();

            RegisterUnhandledExceptionHandler();

            _regThread = new Thread(new ThreadStart(() =>
                {
                    try
                    {
                        _self.Register(dev);
                        if (callback != null)
                        {
                            callback.Invoke();
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.StackTrace);
                    }
                    _regThread = null;
                }));
            _regThread.Name = "RemoteLogRegistration";
            _regThread.Start();
        }

        private void Register(Device dev)
        {
            if (_deviceID == null || _deviceID == 0 || _resend)
            {
                _deviceID = SendDeviceToServer(dev);
                RLSettings.DeviceID = _deviceID;
            }

            if (_self._deviceID == 0)
            {
                // only if request to server was sucesfull, but respond doesn't have
                // an ID
                _self._connector = null;
                throw new InvalidOperationException("Unable to register device!");
            }

            _logSender = new LogSender(_connector);

            //push
            //settings
            
            //unhandled exception in main thread     
            string stack = RLSettings.UnhandledExceptionStack;
            if (!String.IsNullOrEmpty(stack))
            {
                LogItem li = CreateLogItem();
                li.Message = "Unhandled exception history";
                LogItemBlobRequest libr = new LogItemBlobRequest(LogItemBlobRequest.MIME_TEXT_PLAIN, "fatalerror.txt", stack);
                libr.IsUnhandledException = true;
                RLog.Send(typeof(Application), "UnhandledException", "History stack trace", libr);
            }
        }

        private int SendDeviceToServer(Device device)
        {
            int result = 0;
            try
            {
                Respond<Device> dr = _connector.SaveDevice(device);
                if (dr == null || dr.HasError)
                {
                    Debug.WriteLine(dr.Message);
                    result = 0;
                }
                else
                {
                    result = dr.Context.DeviceID;
                }

                RLSettings.DeviceID = result;
                return result;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.StackTrace);
            }
            return result;
        }

        internal static Model.LogItem CreateLogItem()
        {
            LogItem li = new LogItem();
            li.DeviceID = _self._deviceID != null ? (int)_self._deviceID : 0;
            li.Application = _self._appName;
            li.AppVersion = _self._appVersion;
            li.Date = DateTime.Now;
            return li;
        }

        public static string GetStackTrace(Exception t)
        {
            return new System.Diagnostics.StackTrace(t).ToString();
        }
    }
}
