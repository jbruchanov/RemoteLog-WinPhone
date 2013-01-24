using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteLogCore.Model
{
    public class LogItemBlob
    {
        public int ID { get; set; }

        public byte[] Data { get; set; }
    }
}
