using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RemoteLogCore;
using RemoteLogCore.Model;
using Microsoft.Silverlight.Testing;

namespace RemoteLogLibraryTest
{
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
    public class ServiceConnectorTest : WorkItemTest
    {
        private ServiceConnector Connector;

        [Microsoft.VisualStudio.TestTools.UnitTesting.TestInitialize]
        public void Startup()
        {
            Connector = new ServiceConnector("http://192.168.168.183:8080/RemoteLogWeb/");
        }

        [TestMethod]
        public void MyTestMethodX()
        {
            string json = "{\"Message\":\"OK\",\"HasError\":false,\"Context\":[],\"Count\":0}";
            SettingsRespond sr = Newtonsoft.Json.JsonConvert.DeserializeObject<SettingsRespond>(json);
            Assert.IsFalse(sr.HasError);
        }

        //[TestMethod]
        //[Asynchronous]
        //public void MyTestMethod()
        //{
        //    SettingsRespond sr = Connector.LoadSettings(1, "test");
        //    Assert.IsNotNull(sr);
        //    EnqueueTestComplete();
        //}
    }
}
