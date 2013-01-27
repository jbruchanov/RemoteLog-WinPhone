using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteLogCore.Model
{
    public class PushMessage
    {
        public string TimeStamp { get; set; }
        public string Params { get; set; }
        public string Name { get; set; }
        public string Context { get; set; }
    }
}
