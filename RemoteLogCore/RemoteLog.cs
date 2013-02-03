using Microsoft.Phone.Notification;
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
        #region members
        private const string FORMAT = "yyyy-MM-dd HH:mm:ss.fff";

        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings();

        private static LogSender _logSender;

        private string _appName;

        private string _appVersion;

        private int? _deviceID;

        private static Thread _regThread;

        private static readonly RemoteLog _self = new RemoteLog();

        private DeviceDataProvider _deviceDataProvider;

        private ServiceConnector _connector;

        public event EventHandler<Respond<Settings[]>> SettingsLoaded;

        public static event EventHandler RegistrationFinished;

        private PushMessageHandler _pushMessageHandler;

        #endregion

        private static bool _resend = false;
        public static void Resend()
        {
            _resend = true;
        }

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

        /// <summary>
        /// Return instance
        /// </summary>
        /// <returns>Reference only if Initialization succeeded</returns>
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

        private static string _userName;
        private static string _password;

        public static void SetCredentials(string username, string password)
        {
            if (String.IsNullOrEmpty(username) || String.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Invalid username or password");
            }
            _userName = username;
            _password = password;
        }

        private static string _owner;

        public static void SetOwner(string owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// Call it for register unhandled exception handler
        /// </summary>
        public static void RegisterUnhandledExceptionHandler()
        {
            Application.Current.UnhandledException += new EventHandler<ApplicationUnhandledExceptionEventArgs>((object o, ApplicationUnhandledExceptionEventArgs ea)
                =>
                {
                    if ((RLog.Mode & RLog.ERROR) == RLog.ERROR)
                    {
                        Exception ex = ea.ExceptionObject;

                        if (ex is UnhandledExceptionKillApp)
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

        private static bool _pushNotifications = false;
        private static string _pushNotificationServiceName = null;
        
        private HttpNotificationChannel _httpChannel;

        /// <summary>
        /// HttpNotificationChannel is initialized when push notifications feature is registered
        /// </summary>
        public HttpNotificationChannel HttpNotificationChannel
        {
            get
            {
                return _httpChannel;
            }

            private set
            {
                _httpChannel = value;
            }
        }

        /// <summary>
        /// Register library for accepting push notifications
        /// ID_CAP_PUSH_NOTIFICATION capability required!
        /// </summary>
        /// <param name="serviceName">
        /// When creating an HttpNotificationChannel, titles should use the fully qualified domain name (FQDN) as the service name. For authenticated notifications, this FQDN must match the registered certificate's subject name (the CN attribute), for example, www.contoso.com. For more information about authenticated notifications, see: Push Notifications (Windows Phone).
        /// </param>
        public static void RegisterForPushNotifications(string serviceName)
        {
            if (String.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentException("service name is null or empty!");
            }
            _pushNotificationServiceName = serviceName;
            _pushNotifications = true;

        }

        public static void Init(string appName, string serverLocation, Action callback = null)
        {
            if (_regThread != null)
            {
                throw new InvalidOperationException("Registration already started");
            }            

            _self._deviceID = RLSettings.DeviceID;
            _self._appName = ApplicationInfo.Title;
            _self._appVersion = ApplicationInfo.Version;

            // create server connector
            _self._connector = new ServiceConnector(serverLocation, _userName, _password);

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
            dev.App = _appName;
            dev.Owner = _owner;
            if (_deviceID == null || _deviceID == 0 || _resend)
            {
                dev = SendDeviceToServer(dev);
                if(dev != null)
                {
                    _deviceID = dev.DeviceID;
                    RLSettings.DeviceID = _deviceID;
                }
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
            if (_pushNotifications)
            {
                OnRegisterPushNotifications(String.IsNullOrEmpty(dev.PushID));
            }

            //settings
            Respond<Settings[]> settings = LoadSetttingsBlocking();
            OnLoadSettings(settings);
            if (SettingsLoaded != null)
            {
                SettingsLoaded.Invoke(this, settings);
            }

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

            if (RegistrationFinished != null)
            {
                RegistrationFinished.Invoke(this, EventArgs.Empty);
            }
        }

        internal Respond<Settings[]> LoadSetttingsBlocking()
        {
            return _self._connector.LoadSettings((int)_self._deviceID, _self._appName);
        }

        protected void OnRegisterPushNotifications(bool updateOnServer)
        {
            _pushMessageHandler = new PushMessageHandler();
            // Try to find an existing channel
            HttpNotificationChannel = HttpNotificationChannel.Find(_appName);

            if (null == HttpNotificationChannel)
            {
                HttpNotificationChannel = new HttpNotificationChannel(_appName, _pushNotificationServiceName);
                // handle Uri notification events
                
                HttpNotificationChannel.Open();

                HttpNotificationChannel.ChannelUriUpdated +=
                        new EventHandler<NotificationChannelUriEventArgs>((o, e) =>
                        {
                            UpdatePushUri(e.ChannelUri);
                        });
            }
            else
            {
                
                // the channel already exists.  httpChannel.ChannelUri contains the device’s
                // unique locator	

                if (updateOnServer)
                {
                    UpdatePushUri(HttpNotificationChannel.ChannelUri);
                }
            }

            HttpNotificationChannel.HttpNotificationReceived += new EventHandler<HttpNotificationEventArgs>((o, e) =>
            {
                _pushMessageHandler.OnNotificiationReceived(o, e);
            });

            
            if (!HttpNotificationChannel.IsShellToastBound) { HttpNotificationChannel.BindToShellToast(); }

            _httpChannel.ShellToastNotificationReceived += new EventHandler<NotificationEventArgs>((o, e) =>
                {
                    _pushMessageHandler.OnToastNotificiationReceived(o, e);
                });
            
            if (!HttpNotificationChannel.IsShellTileBound) { HttpNotificationChannel.BindToShellTile(); }            
            // handle error events
            _httpChannel.ErrorOccurred +=
                new EventHandler<NotificationChannelErrorEventArgs>((o, e) =>
            {                
                RLog.E(this, e.Message);
            });
        }

        private void UpdatePushUri(Uri uri)
        {
            //RLog.V(this, "New Push URI detected:" + uri != null ? uri.ToString() : "null");
            if (uri != null)
            {
                Respond<String> resppond = _self._connector.UpdatePushToken((int)_deviceID, HttpNotificationChannel.ChannelUri.ToString());
                string result = resppond.Context;
            }
        }

        internal void OnLoadSettings(Respond<Model.Settings[]> settings)
        {
            try
            {
                if (settings.Count > 0)
                {
                    // going from end, where should be device specific
                    foreach (Settings s in settings.Context)
                    {
                        Dictionary<String, Object> vs = JsonConvert.DeserializeObject<Dictionary<string, object>>(s.JsonValue);
                        if (vs != null && vs.ContainsKey("RLog"))
                        {
                            string logMode = vs["RLog"].ToString();
                            int parsedValue = RLog.ParseMode(logMode);
                            if (parsedValue != -1)
                            {
                                RLog.Mode = parsedValue;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                RLog.E(this, e);
                // ignore any error and let the code continue	        
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        private Device SendDeviceToServer(Device device)
        {
            Device result = null;
            try
            {
                Respond<Device> dr = _connector.SaveDevice(device);
                if (dr == null || dr.HasError)
                {
                    Debug.WriteLine(dr.Message);                    
                }
                else
                {
                    result = dr.Context;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.StackTrace);
            }
            return result;
        }

        /// <summary>
        /// Create new pre-filled LogItem
        /// </summary>
        /// <returns></returns>
        internal static Model.LogItem CreateLogItem()
        {
            LogItem li = new LogItem();
            li.DeviceID = _self._deviceID != null ? (int)_self._deviceID : 0;
            li.Application = _self._appName;
            li.AppVersion = _self._appVersion;
            li.Date = DateTime.Now;
            return li;
        }

        /// <summary>
        /// Get stack trace as readable string
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static string GetStackTrace(Exception t)
        {
            return new System.Diagnostics.StackTrace(t).ToString();
        }

        /// <summary>
        /// Async calling for lTo catch settings loaded event register eventhander for SettingsLoaded event
        /// </summary>
        public void LoadSettings()
        {
            if (_self._deviceID == null || String.IsNullOrEmpty(_self._appName))
            {
                throw new InvalidProgramException("Remote log not initialized!");
            }
            Thread t = new Thread(new ThreadStart(() =>
                {
                    Respond<Settings[]> ret = _self._connector.LoadSettings((int)_self._deviceID, _self._appName);
                    if (SettingsLoaded != null)
                    {
                        SettingsLoaded.Invoke(this, ret);
                    }
                }));
            t.Start();
        }
    }
}
