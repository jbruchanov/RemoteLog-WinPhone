using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteLogCore.Model
{
    public class LogItem
    {
        public int ID { get; set; }

        public string Application { get; set; }

        public string AppVersion { get; set; }

        public string AppBuild { get; set; }

        public DateTime Date { get; set; }

        public string Category { get; set; }

        public string Source { get; set; }

        public string Message { get; set; }

        public string BlobMime { get; set; }

        public int DeviceID { get; set; }
    }
}
