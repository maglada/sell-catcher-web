namespace ProductScraper
{
    public class Product
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public decimal? OldPrice { get; set; }
        public string Discount { get; set; }
        public string ValidUntil { get; set; }
        public bool IsOnSale { get; set; }
        public decimal? BulkPrice { get; set; } = null;
        public bool IsBulk { get; set; } = false;
        public string Category { get; set; }
    }
}