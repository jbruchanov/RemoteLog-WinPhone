using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteLogCore
{
    public interface ILog
    {
        void I(object source, string msg);

        void V(object source, string msg);

        void D(object source, string msg);

        void E(object source, string msg);
        void E(object source, Exception t);

        void N(object source, string msg);

        void W(object source, string msg);

        void Wtf(object source, string msg);
    }
}
