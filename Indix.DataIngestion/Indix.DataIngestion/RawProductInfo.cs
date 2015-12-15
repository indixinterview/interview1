namespace Indix.DataIngestion
{
    public class RawProductInfo
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
        public string ProductId { get; set; }
        public string TopLevelCategory { get; set; }
        public string SubCategory { get; set; }
        public string StoreName { get; set; }
        public int StoreId { get; set; }
        public int CategoryId { get; set; }
    }
}
