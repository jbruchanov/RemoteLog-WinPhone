using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteLogCore.Model
{
    public class Device
    {
        public int DeviceID { get; set; }

        public string DevUUID { get; set; }

        public string Brand { get; set; }

        public string Platform { get; set; }

        public string Version { get; set; }

        public string Detail { get; set; }

        public string Resolution { get; set; }

        public string Owner { get; set; }

        public string OSDescription { get; set; }

        public string Description { get; set; }

        public string PushID { get; set; }

        public string Model { get; set; }
    }
}
