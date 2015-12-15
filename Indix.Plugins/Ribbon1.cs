using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Office.Tools.Ribbon;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Excel = Microsoft.Office.Interop.Excel;
using System.Drawing;

namespace Indix.Plugins
{
    public partial class Ribbon1
    {
        private void Ribbon1_Load(object sender, RibbonUIEventArgs e)
        {

        }

        private void GetPriceComparisonData_Click(object sender, RibbonControlEventArgs e)
        {
            // TODO: Get the data in JSON here
            var task = Ribbon1.GetData();
            task.Wait();
            var pricecomparisons = task.Result;

            // Competitors
            var stores = pricecomparisons.Select(x => x.Store).Distinct().ToList();

            // Categories
            var categories = pricecomparisons.Select(x => x.Category).Distinct().ToList();

            // Get Excel active worksheet
            var workbook = (Excel.Workbook)Globals.ThisAddIn.Application.ActiveWorkbook;
            var activeWorksheet = (Excel.Worksheet)workbook.ActiveSheet;

            // Insert Categories and Stores
            int colCounter = InsertStores(stores, activeWorksheet);
            int rowCounter = InsertCategories(categories, activeWorksheet);

            // Insert Prices
            var variance = InsertPricesAndGetVariance(pricecomparisons, stores, categories, activeWorksheet);

            // Update the background colors
            UpdateBackgroundVariance(variance, activeWorksheet);

            // Force entire worksheet to autofit the columns
            activeWorksheet.Columns.AutoFit();
        }

        private static void UpdateBackgroundVariance(double[,] variance, Excel.Worksheet activeWorksheet)
        {
            // Find max and min
            double max = 0d;
            double min = 0d;
            for (int i = 0; i < variance.GetLength(0); i++)
            {
                for (int j = 0; j < variance.GetLength(1); j++)
                {
                    var currentVariance = variance[i, j];
                    max = (max < currentVariance) ? currentVariance : max;
                    min = (min > currentVariance) ? currentVariance : min;
                }
            }

            // TODO: Configure these magic numbers
            int maxGreen = 127;
            int maxRed = 127;

            // Update based on max and min
            for (int i = 0; i < variance.GetLength(0); i++)
            {
                for (int j = 0; j < variance.GetLength(1); j++)
                {
                    var currentVariance = variance[i, j];
                    var color = Color.FromArgb(alpha: 0, red: 255, green: 255, blue: 255);
                    // TODO: Simplify this ugly logic to pick a range
                    if (currentVariance > 0)
                    {
                        int green = (int)Math.Ceiling((maxGreen * currentVariance) / max);
                        color = Color.FromArgb(alpha: 255, red: 0, green: 128 + green, blue: 0);
                    }
                    else if (currentVariance < 0)
                    {
                        int red = (int)(Math.Abs(Math.Ceiling((maxRed * currentVariance) / min)));
                        color = Color.FromArgb(alpha: 255, red: 128 + red, green: 0, blue: 0);
                    }

                    (activeWorksheet.Cells[i + 2, j + 2] as Excel.Range).Interior.Color = ColorTranslator.ToOle(color);
                }
            }
        }

        private static double[,] InsertPricesAndGetVariance(
            IEnumerable<PriceComparison> pricecomparisons,
            IEnumerable<string> stores,
            IEnumerable<string> categories,
            Excel.Worksheet activeWorksheet)
        {
            var variance = new double[categories.Count(), stores.Count()];

            int rowCounter = 2;
            foreach (var category in categories)
            {
                var myPriceForCategory = pricecomparisons.SingleOrDefault(x => x.Category == category && x.Store == "My Store");
                var myStorePrice = (myPriceForCategory == null) ? 0 : myPriceForCategory.Price.Value;

                int colCounter = 2;
                foreach (var store in stores)
                {
                    // TODO: Improve this ugly double-for-loop
                    var competitor = pricecomparisons.SingleOrDefault(x => x.Category == category && x.Store == store);
                    var competitorprice = (competitor == null) ? 0 : competitor.Price.Value;
                    (activeWorksheet.Cells[rowCounter, colCounter] as Excel.Range).Value2 = competitorprice;

                    // Note: Variance is +ve when competitor price is higher
                    variance[rowCounter - 2, colCounter - 2] = (myStorePrice <= 0)
                        ? 0d
                        : (((double)competitorprice / (double)myStorePrice) - 1);

                    colCounter++;
                }
                rowCounter++;
            }
            return variance;
        }

        private static int InsertCategories(List<string> categories, Excel.Worksheet activeWorksheet)
        {
            // Add All Category header
            int rowCounter = 2;
            foreach (var category in categories)
            {
                (activeWorksheet.Cells[rowCounter++, 1] as Excel.Range).Value2 = category;
            }
            return rowCounter;
        }

        private static int InsertStores(List<string> stores, Excel.Worksheet activeWorksheet)
        {
            // Add All Competitor header
            int colCounter = 2;
            foreach (var store in stores)
            {
                (activeWorksheet.Cells[1, colCounter++] as Excel.Range).Value2 = store;
            }
            return colCounter;
        }

        /// <summary>
        /// Gets the data required for excel
        /// </summary>
        public static async Task<IEnumerable<PriceComparison>> GetData()
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://indix.azurewebsites.net/api/values/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // TODO: Configure hardcoded string
                var response = await client.GetAsync("https://indix.azurewebsites.net/api/pricecomparison/");
                if (response.IsSuccessStatusCode)
                {
                    // TODO: Figure out why the following line fails
                    // var readContentTask = response.Content.ReadAsAsync<List<PriceComparison>>();
                    // readContentTask.Wait();
                    // return readContentTask.Result;

                    var serializedContents = await response.Content.ReadAsStringAsync();
                    var array = JsonConvert.DeserializeObject<List<PriceComparison>>(serializedContents);
                    return array;
                }
            }
            return Enumerable.Empty<PriceComparison>();
        }
    }
}
