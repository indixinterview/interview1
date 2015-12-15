using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Indix.DataIngestion;
using Indix.WebApi.Controllers;

namespace Indix.Test
{
    [TestClass]
    public class UnitTests
    {
        [TestMethod]
        public void LoadAndMeasureDataIngestionTime()
        {
            var start = DateTime.UtcNow;

            Program.Main(new string[] { "10000" });

            var end = DateTime.UtcNow;
            var elapsed = (end - start).TotalSeconds;
            Console.WriteLine("Total time to ingest data is: {0} sec", elapsed);
        }


        [TestMethod]
        public void GetStoreComparisons()
        {
            var start = DateTime.UtcNow;

            var comparisons = new PriceComparisonController().Get();

            var end = DateTime.UtcNow;
            var elapsed = (end - start).TotalSeconds;
            Console.WriteLine("Total time to ingest data is: {0} sec", elapsed);
        }
    }
}
