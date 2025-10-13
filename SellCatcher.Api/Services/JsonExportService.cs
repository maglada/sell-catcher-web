using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using ProductScraper.DTOs;

namespace ProductScraper.Services
{
    /// <summary>
    /// Service for exporting product data to JSON format
    /// </summary>
    public class JsonExportService
    {
        private readonly JsonSerializerOptions _jsonOptions;

        public JsonExportService(bool prettyPrint = true)
        {
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = prettyPrint,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        /// <summary>
        /// Exports products to JSON with full metadata
        /// </summary>
        public void ExportWithMetadata(List<Product> products, string filename)
        {
            var exportData = new ProductExportData
            {
                TotalProducts = products.Count,
                TotalSaleItems = products.Count(p => p.IsOnSale),
                GeneratedAt = DateTime.Now,
                Categories = GetCategorySummaries(products),
                Products = products
            };

            var json = JsonSerializer.Serialize(exportData, _jsonOptions);
            File.WriteAllText(filename, json, System.Text.Encoding.UTF8);
            
            Console.WriteLine($"Exported {products.Count} products to: {filename}");
        }

        /// <summary>
        /// Exports products as simple JSON array
        /// </summary>
        public void ExportSimple(List<Product> products, string filename)
        {
            var json = JsonSerializer.Serialize(products, _jsonOptions);
            File.WriteAllText(filename, json, System.Text.Encoding.UTF8);
            
            Console.WriteLine($"Exported {products.Count} products to: {filename}");
        }

        /// <summary>
        /// Exports products grouped by category to separate files
        /// </summary>
        public void ExportByCategory(List<Product> products, string outputDirectory)
        {
            Directory.CreateDirectory(outputDirectory);

            var byCategory = products
                .GroupBy(p => p.Category ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var category in byCategory)
            {
                var fileName = SanitizeFileName(category.Key);
                var filePath = Path.Combine(outputDirectory, $"{fileName}.json");
                
                ExportWithMetadata(category.Value, filePath);
            }
        }

        /// <summary>
        /// Exports only sale items
        /// </summary>
        public void ExportSaleItems(List<Product> products, string filename)
        {
            var saleItems = products.Where(p => p.IsOnSale).ToList();
            ExportWithMetadata(saleItems, filename);
        }

        /// <summary>
        /// Exports only bulk items
        /// </summary>
        public void ExportBulkItems(List<Product> products, string filename)
        {
            var bulkItems = products.Where(p => p.IsBulk).ToList();
            ExportWithMetadata(bulkItems, filename);
        }

        /// <summary>
        /// Generates category summaries
        /// </summary>
        private Dictionary<string, CategorySummary> GetCategorySummaries(List<Product> products)
        {
            return products
                .GroupBy(p => p.Category ?? "Unknown")
                .ToDictionary(
                    g => g.Key,
                    g => new CategorySummary
                    {
                        TotalProducts = g.Count(),
                        SaleItems = g.Count(p => p.IsOnSale),
                        RegularItems = g.Count(p => !p.IsOnSale && !p.IsBulk),
                        BulkItems = g.Count(p => p.IsBulk)
                    }
                );
        }

        /// <summary>
        /// Sanitizes category name for use as filename
        /// </summary>
        private string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
            return sanitized.Replace(" ", "_").ToLower();
        }
    }
}