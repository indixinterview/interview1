using Indix.DataModel;
using Indix.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace Indix.WebApi.Controllers
{
    [AllowAnonymous]
    public class PriceComparisonController : ApiController
    {
        /// <summary>
        /// Gets the flat list of competitor stores in each category
        /// </summary>
        /// <returns></returns>
        public IEnumerable<PriceComparison> Get()
        {
            using (var context = new IndixEntities())
            {
                var comparisons = context.CategoryComparisonByStores
                    .ToList()
                    .Select(x => new PriceComparison
                    {
                        Category = x.toplevelcategory,
                        Store = x.name,
                        Price = x.Price
                    });

                return comparisons;
            }
        }
    }
}
