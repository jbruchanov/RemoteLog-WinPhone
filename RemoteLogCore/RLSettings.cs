using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;

namespace RemoteLogCore
{
    internal class RLSettings 
    {
        private const string DEVICE_ID = "DEVICE_ID";

        public static int? DeviceID
        {
            get
            {
                int? v = null;
                IsolatedStorageSettings.ApplicationSettings.TryGetValue(DEVICE_ID, out v);
                return v;
            }
            set
            {
                IsolatedStorageSettings.ApplicationSettings[DEVICE_ID] = value;
                IsolatedStorageSettings.ApplicationSettings.Save();
            }
        }
    }
}
