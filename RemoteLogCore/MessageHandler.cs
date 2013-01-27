using Coding4Fun.Phone.Controls;
using Microsoft.Phone.Shell;
using Newtonsoft.Json;
using RemoteLogCore.Model;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace RemoteLogCore
{
    public class PushMessageHandler
    {
        private const string TAKESCREENSHOT = "TakeScreenshot";
        private const string ECHO = "Echo";
        private const string KILLAPP = "KillApp";
        private const string LASTKNOWNLOCATION = "LastKnownLocation";
        private const string RELOADSETTINGS = "ReloadSettings";


        public virtual void OnNotificiationReceived(object o, Microsoft.Phone.Notification.HttpNotificationEventArgs e)
        {
            string content = GetBody(e.Notification.Body);
            RLog.D(this, "RawNotification:" + content);
            PushMessage msg = JsonConvert.DeserializeObject<PushMessage>(content);
            switch (msg.Name)
            {
                case TAKESCREENSHOT:
                    OnTakeScreenshot(msg); break;
                case ECHO:
                    OnEcho(msg); break;
                case KILLAPP:
                    OnKillApp(msg); break;
                case LASTKNOWNLOCATION:
                    OnLastKnownLocation(msg); break;
                case RELOADSETTINGS:
                    OnReloadSettings(msg); break;
                default:
                    break;
            }

        }

        private void OnReloadSettings(PushMessage msg)
        {
            RemoteLog rl = RemoteLog.Instance();
            if (rl != null)
            {
                rl.OnLoadSettings(rl.LoadSetttingsBlocking());
            }
        }

        private void OnLastKnownLocation(PushMessage msg)
        {
            GeoCoordinateWatcher watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
            switch (watcher.Status)
            {
                case GeoPositionStatus.Disabled:
                    RLog.N(this, "Location", "Geolocation status:" + watcher.Status.ToString());
                    break;
                default:
                    {
                        watcher.MovementThreshold = 20;
                        watcher.PositionChanged += (o, args) =>
                        {
                            RLog.N(this, "Location", GetLocationSring(args.Position.Location));
                            watcher.Stop();
                        };
                        watcher.Start();
                    }
                    break;

            }
        }

        private static string GetLocationSring(GeoCoordinate l)
        {
            return String.Format("lat:{0}, lng:{1}, alt:{2}, accuracy:{3}", l.Latitude, l.Longitude, l.Altitude, l.VerticalAccuracy);

        }

        private void OnKillApp(PushMessage msg)
        {
            Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    throw new KillAppException();
                }));
        }

        private void OnEcho(PushMessage msg)
        {
            //ignore it, handled by logging in OnNotificiationReceived method
        }

        private const string WP_TEXT1 = "wp:Text1";
        private const string WP_TEXT2 = "wp:Text2";
        public virtual void OnToastNotificiationReceived(object o, Microsoft.Phone.Notification.NotificationEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var toast = new ToastPrompt
                    {
                        Title = e.Collection[WP_TEXT1],
                        Message = e.Collection.ContainsKey(WP_TEXT2) ? e.Collection[WP_TEXT2] : null
                    };

                    toast.Show();
                }));
            //RLog.D(this, "ToastNotification:" + JsonConvert.SerializeObject(e.Collection));            
        }

        private string GetBody(Stream stream)
        {
            return new StreamReader(stream).ReadToEnd();
        }

        public virtual void OnTakeScreenshot(PushMessage msg)
        {
            RLog.TakeScreenshot(this, "PushNotification");
        }
    }
}
