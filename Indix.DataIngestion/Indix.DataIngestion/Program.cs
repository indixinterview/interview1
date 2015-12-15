using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;
using Indix.DataModel;

namespace Indix.DataIngestion
{
    /// <summary>
    /// Data Ingestion program
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main entry point of the program
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            var droplocation_pending = ConfigurationManager.AppSettings["DropLocation_Pending"];
            if (!Directory.Exists(droplocation_pending))
            {
                throw new ArgumentException("Drop location at '{0}' doesn't exist", droplocation_pending);
            }

            // Figure out how many lines to batch proces
            int limit = 1000;
            if (args.Length > 0) { limit = int.Parse(args[0]); } // TODO: Handle bad inputs

            var files = Directory.GetFiles(droplocation_pending, "*.csv");
            // TODO: Each file processing is blocking, so create tasks outside of this for loop for better perf
            foreach (var file in files)
            {
                // TODO: Make file reading async, takes 1.5 sec to read 300K lines
                var lines = File.ReadAllLines(file);

                // Time this
                var rawProducts = DbInsertHelper.ParseLinesIntoEntities(lines).ToList();

                // Add missing stores
                var distinctStores = rawProducts.Select(x => x.StoreName).Distinct().ToList();
                DbInsertHelper.UpdateMissingStores(distinctStores);

                // Add missing categories
                var distinctCategories = rawProducts
                            .GroupBy(x => new
                            {
                                TopLevelCategory = x.TopLevelCategory,
                                SubCategory = x.SubCategory
                            })
                            .Select(group => new Category()
                            {
                                TopLevelCategory = group.Key.TopLevelCategory,
                                SubCategory = group.Key.SubCategory
                            })
                            .ToList();
                DbInsertHelper.UpdateMissingCategories(distinctCategories);

                // Get All Stores
                var storesInDatabase = DbInsertHelper.GetStores();

                // Get All Categories
                var categoriesInDatabase = DbInsertHelper.GetCategories();

                // Update the store and category IDs as FKs
                // TODO: This is double forloop, taking a lot of time, optimize
                foreach (var prodInfo in rawProducts)
                {
                    prodInfo.StoreId = storesInDatabase
                        .Single(x => string.Equals(x.Name, prodInfo.StoreName, StringComparison.InvariantCultureIgnoreCase))
                        .Id;

                    prodInfo.CategoryId = categoriesInDatabase
                        .Single(x => string.Equals(x.TopLevelCategory, prodInfo.TopLevelCategory, StringComparison.InvariantCultureIgnoreCase) &&
                                    string.Equals(x.SubCategory, prodInfo.SubCategory, StringComparison.InvariantCultureIgnoreCase))
                        .Id;
                }

                // Insert into database
                // TODO: Need to parallelize this as shows below
                // TODO: Also, insert slows down significantly since we have normalized table structure.
                // TODO: Denormalize
                DbInsertHelper.InsertEntities(rawProducts);

                // TODO: Get better database tier to make this more effective
                /*
                var tasks = new List<Task>();
                int processed = 1; // First line is header. // TODO: Make it configurable

                while (processed < lines.Length)
                {
                    var toBeProcessed = new List<RawProductInfo>(
                        rawProducts.Skip(processed).Take(limit));
                    tasks.Add(Task.Factory.StartNew(() =>
                    {
                        DbInsertHelper.InsertEntities(toBeProcessed);
                    }));
                    processed += limit;
                }
                Task.WaitAll(tasks.ToArray());
                */

                // Clean up
                // TODO: Move the files to Processing/Finished
            }
        }
    }
}
