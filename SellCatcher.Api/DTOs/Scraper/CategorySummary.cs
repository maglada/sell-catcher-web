using System.Text.Json.Serialization;

namespace ProductScraper.DTOs
{
    /// <summary>
    /// Category summary information for JSON export
    /// </summary>
    public class CategorySummary
    {
        [JsonPropertyName("totalProducts")]
        public int TotalProducts { get; set; }

        [JsonPropertyName("saleItems")]
        public int SaleItems { get; set; }

        [JsonPropertyName("regularItems")]
        public int RegularItems { get; set; }

        [JsonPropertyName("bulkItems")]
        public int BulkItems { get; set; }
    }
}