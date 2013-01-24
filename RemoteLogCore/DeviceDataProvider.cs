using RemoteLogCore.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Phone;
using Microsoft.Phone.Info;
using System.Windows;
using System.Reflection;
using Microsoft.Phone.Net.NetworkInformation;

namespace RemoteLogCore
{
    public class DeviceDataProvider
    {
        public Device GetDevice()
        {
            Device d = new Device();
            //d.DeviceID;
            d.Brand = DeviceStatus.DeviceManufacturer;// String.Concat(DeviceExtendedProperties.GetValue("DeviceManufacturer"));
            d.Description = null;
            d.Detail = GetDetails();                      
            d.DevUUID = GetDeviceUUID();
            d.Model = DeviceStatus.DeviceName;// String.Concat(DeviceExtendedProperties.GetValue("DeviceName"));
            d.OSDescription = null;
            d.Owner = null; //no way           
            d.Platform = "WindowsPhone";
            d.PushID = "";            
            d.Resolution = GetScreenResolution();
            d.Version = String.Format("{0}.{1}.{2}", System.Environment.OSVersion.Version.Major, System.Environment.OSVersion.Version.Minor, System.Environment.OSVersion.Version.Revision);
            
            return d;
        }

        private static string GetDetails()
        {
            Dictionary<string, string> dstatus = GetDeviceStatuses();  
            return Newtonsoft.Json.JsonConvert.SerializeObject(dstatus);
        }

        private static Dictionary<string, string> GetDeviceStatuses()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            PropertyInfo[] ps = typeof(DeviceStatus).GetProperties();
            foreach (PropertyInfo pi in ps)
            {
                string name = pi.Name;
                string value = String.Concat(pi.GetValue(null, null));
                result[name] = value;
            }
            return result;
        }

        private static string GetDeviceUUID()
        {
            StringBuilder sb = new StringBuilder("");
            byte[] uuid = (byte[])DeviceExtendedProperties.GetValue("DeviceUniqueId");
            foreach (byte b in uuid)
            {
                sb.Append(String.Format("{0:D3}",(int)b));                
            }
            return sb.ToString();
        }

        private static string GetScreenResolution()
        {
            
            if (System.Environment.OSVersion.Version.Major < 8)
            {
                return String.Format("{0}x{1}", Application.Current.Host.Content.ActualWidth, Application.Current.Host.Content.ActualHeight);
            }
            else
            {
                try
                {
                    /**
                     * http://www.silverlightshow.net/items/Windows-Phone-8-Multiple-Screen-Resolutions.aspx
                     * 
                     */
                    object dc = Application.Current.Host.Content;
                    PropertyInfo pi = dc.GetType().GetProperty("ScaleFactor");
                    int v = (int)pi.GetValue(dc, null);
                    switch (v)
                    {
                        case 100:
                            return "480x800";
                        case 150:
                            return "720x1280";
                        case 160:
                            return "768x1280";
                        default:
                            return "" + v;
                    }
                }
                catch (Exception e)
                {
                    return e.Message;
                }
            }
        } 
    }
}
