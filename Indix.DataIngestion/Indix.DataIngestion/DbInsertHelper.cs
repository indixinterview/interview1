using Indix.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Indix.DataIngestion
{
    internal class DbInsertHelper
    {
        internal static void InsertEntities(IEnumerable<RawProductInfo> products)
        {
            var productsToAdd = products.Select(x => new ProductInformation()
            {
                CategoryId = x.CategoryId,
                Price = x.Price,
                ProductId = x.ProductId,
                StoreId = x.StoreId,
                Title = x.Title,
            });

            int processed = 0;
            int limit = 1000; // TODO: Find optimal entities to take for batch processing

            while (processed < products.Count())
            {
                Console.WriteLine();
                // TODO: Move the database to West US from Germany
                // Network is taking too long to process these entities
                using (var context = new IndixEntities())
                {
                    context.Configuration.AutoDetectChangesEnabled = false;
                    context.ProductInformations.AddRange(productsToAdd.Skip(processed).Take(limit));
                    try
                    {
                        var entitiesAdded = context.SaveChanges();
                        // TODO: verify entitiesAdded vs. entitiesToAdd and throw warning/log if not same
                    }
                    // TODO: Use AzureSqlExecutionStrategy and Retry
                    finally { processed += limit; }
                }
            }
        }

        internal static void UpdateMissingCategories(IEnumerable<Category> distinctCategories)
        {
            var categoriesInDatabase = DbInsertHelper.GetCategories();

            using (var context = new IndixEntities())
            {
                foreach (var category in distinctCategories)
                {
                    if (!categoriesInDatabase.Any(x =>
                            string.Equals(x.TopLevelCategory, category.TopLevelCategory, StringComparison.InvariantCultureIgnoreCase) &&
                            string.Equals(x.SubCategory, category.SubCategory, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        context.Categories.Add(category);
                    }
                }
                context.SaveChanges();
            }
        }

        internal static void UpdateMissingStores(IEnumerable<string> distinctStores)
        {
            var storesInDatabase = DbInsertHelper.GetStores().ToDictionary(key => key.Name);

            using (var context = new IndixEntities())
            {
                foreach (var store in distinctStores)
                {
                    if (!storesInDatabase.ContainsKey(store))
                    {
                        context.Stores.Add(new Store() { Name = store });
                    }
                }
                context.SaveChanges();
            }
        }

        internal static IEnumerable<Store> GetStores()
        {
            using (var context = new IndixEntities())
            {
                return context.Stores.ToList();
            }
        }

        internal static IEnumerable<RawProductInfo> ParseLinesIntoEntities(IEnumerable<string> lines)
        {
            var result = new List<RawProductInfo>();
            foreach (var line in lines)
            {
                var tokens = line.Split(new char[] { ',' }).ToArray();
                // int tokenCount = tokens.Length;

                int priceIndex = -1;
                decimal price = -1m;
                for (int i = 0; i < tokens.Length; i++)
                {
                    if (decimal.TryParse(tokens[i], out price)) { priceIndex = i; }
                }

                if (priceIndex == -1) { continue; }

                var itemToAdd = new RawProductInfo()
                {
                    ProductId = tokens.First(),
                    StoreName = tokens[priceIndex - 1],
                    Price = decimal.Parse(tokens[priceIndex]),
                    SubCategory = tokens.Last(),
                };

                // For this, take everything between priceIndex and subcategory (last Column)
                itemToAdd.TopLevelCategory = string.Join(",",
                    tokens
                        .Skip(priceIndex + 1)
                        .Reverse()
                        .Skip(1) // Skip subcategory
                        .Reverse()
                )
                .Replace("\"", string.Empty)
                .Trim();

                itemToAdd.Title = string.Join(",",
                    tokens
                        .Take(priceIndex + 1)
                        .Skip(1) // Skip ID
                        .Reverse()
                        .Skip(2) // Skip Price and Store
                        .Reverse()
                )
                .Replace("\"", string.Empty)
                .Trim();

                result.Add(itemToAdd);
            }
            return result;
        }

        internal static IEnumerable<Category> GetCategories()
        {
            using (var context = new IndixEntities())
            {
                return context.Categories.ToList();
            }
        }
    }
}
