using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteLogCore
{
    public interface ILog
    {
        void I(object source, string category, string msg);

        void V(object source, string category, string msg);

        void D(object source, string category, string msg);

        void E(object source, string category, string msg);
        void E(object source, string category, Exception t);

        void N(object source, string category, string msg);

        void W(object source, string category, string msg);

        void Wtf(object source, string category, string msg);
    }
}
