using Microsoft.Silverlight.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RemoteLogCore;

namespace RemoteLogLibraryTest
{
    [TestClass]
    public class RLogTest : WorkItemTest 
    {
        [TestMethod]
        public void TestParseSimple()
        {
            Assert.Equals(RLog.ALL, RLog.ParseMode("ALL"));
        }

        [TestMethod]
        public void TestParseSeparator()
        {
            Assert.Equals(RLog.VERBOSE | RLog.ERROR, RLog.ParseMode("VERBOSE|ERROR"));
        }

        [TestMethod]
        public void TestParseNumber()
        {
            Assert.Equals(RLog.DEBUG , RLog.ParseMode(RLog.DEBUG.ToString()));
        }
    }
}
