using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProductScraper.DTOs
{
    /// <summary>
    /// Main DTO for JSON export with metadata
    /// </summary>
    public class ProductExportData
    {
        [JsonPropertyName("totalProducts")]
        public int TotalProducts { get; set; }

        [JsonPropertyName("totalSaleItems")]
        public int TotalSaleItems { get; set; }

        [JsonPropertyName("generatedAt")]
        public DateTime GeneratedAt { get; set; }

        [JsonPropertyName("categories")]
        public Dictionary<string, CategorySummary> Categories { get; set; }

        [JsonPropertyName("products")]
        public List<Product> Products { get; set; }
    }
}