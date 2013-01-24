using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RemoteLogCore;
using RemoteLogCore.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteLogLibraryTest
{
    [TestClass]
    public class DevideDataProviderTest : WorkItemTest
    {
        [TestMethod]
        public void TestGetDevice()
        {
            Device d = new DeviceDataProvider().GetDevice();
            Assert.IsNotNull(d.Brand);
            Assert.IsNotNull(d.DevUUID);
            Assert.IsNotNull(d.Model);
        }
    }
}
