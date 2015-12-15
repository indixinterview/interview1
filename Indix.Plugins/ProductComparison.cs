using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indix.Plugins
{

    /// <summary>
    /// Price Comparison View Model
    /// </summary>
    public class PriceComparison
    {
        public string Category { get; set; }
        public decimal? Price { get; set; }
        public string Store { get; set; }
    }
}
