using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteLogCore.Model
{
    public class Settings
    {
        public int SettingsID { get; set; }

        public string AppName { get; set; }

        public Int32 DeviceID { get; set; }

        public string JsonValue { get; set; }
    }
}
