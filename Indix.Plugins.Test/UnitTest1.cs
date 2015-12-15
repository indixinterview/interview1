using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Indix.Plugins.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var task = Ribbon1.GetData();
            task.Wait();
            var result = task.Result;
        }
    }
}
