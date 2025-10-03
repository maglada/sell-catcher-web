using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using ProductScraper;
using ProductScraper.Services;

namespace ProductScraper.Services
{
    /// <summary>
    /// Service for initializing and managing product scraping operations (JSON only)
    /// </summary>
    public class ScraperService
    {
        private readonly ScraperFactory _factory;
        private readonly ScraperConfig _config;
        private readonly string _outputDirectory;
        private readonly JsonExportService _jsonExporter;

        public ScraperService() : this(CreateDefaultConfig(), "output")
        {
        }

        public ScraperService(ScraperConfig config, string outputDirectory = "output")
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _outputDirectory = outputDirectory ?? "output";
            _factory = new ScraperFactory(_config);
            _jsonExporter = new JsonExportService(prettyPrint: true);
            
            Directory.CreateDirectory(_outputDirectory);
        }

        public static ScraperConfig CreateDefaultConfig()
        {
            return new ScraperConfig
            {
                Headless = true,
                SlowMo = 1000,
                EnableLogging = true,
                EnableDebugOutput = false,
                SaveDebugScreenshots = false,
                SaveErrorScreenshots = true
            };
        }

        /// <summary>
        /// Processes all files and exports to JSON
        /// </summary>
        public async Task<Dictionary<string, List<Product>>> ProcessAllFilesAsync(
            string directory = "sites", 
            string filePattern = "NovusLinks_*.txt")
        {
            Console.WriteLine($"=== Processing all {filePattern} files from {directory} ===");
            
            var results = await _factory.ProcessAllFilesAsync(directory, filePattern);
            
            PrintSummary(results);
            ExportAllToJson(results);
            
            return results;
        }

        /// <summary>
        /// Processes a single file and exports to JSON
        /// </summary>
        public async Task<List<Product>> ProcessSingleFileAsync(string filePath)
        {
            Console.WriteLine($"=== Processing file: {filePath} ===");
            
            var products = await _factory.ProcessFileAsync(filePath);
            
            Console.WriteLine($"Found {products.Count} products");
            
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var outputFile = Path.Combine(_outputDirectory, $"{fileName}_products.json");
            _jsonExporter.ExportWithMetadata(products, outputFile);
            
            return products;
        }

        /// <summary>
        /// Exports all results to JSON files
        /// </summary>
        private void ExportAllToJson(Dictionary<string, List<Product>> results)
        {
            Console.WriteLine("\n=== Exporting to JSON ===");

            // Export each file's products separately
            foreach (var kvp in results)
            {
                var fileName = Path.GetFileNameWithoutExtension(kvp.Key);
                var outputFile = Path.Combine(_outputDirectory, $"{fileName}_products.json");
                _jsonExporter.ExportWithMetadata(kvp.Value, outputFile);
            }

            // Export all products combined
            var allProducts = results.Values.SelectMany(p => p).ToList();
            var allProductsFile = Path.Combine(_outputDirectory, "all_products.json");
            _jsonExporter.ExportWithMetadata(allProducts, allProductsFile);

            // Export all sale items
            var saleProductsFile = Path.Combine(_outputDirectory, "all_sales.json");
            _jsonExporter.ExportSaleItems(allProducts, saleProductsFile);

            // Export by category
            var categoryDir = Path.Combine(_outputDirectory, "by_category");
            _jsonExporter.ExportByCategory(allProducts, categoryDir);

            Console.WriteLine("\n=== Export Complete ===");
        }

        private void PrintSummary(Dictionary<string, List<Product>> results)
        {
            Console.WriteLine($"\n=== SCRAPING COMPLETE ===");
            Console.WriteLine($"Processed {results.Count} files");
            
            int totalProducts = 0;
            int totalSaleItems = 0;
            
            foreach (var kvp in results)
            {
                var fileName = kvp.Key;
                var products = kvp.Value;
                var saleCount = products.Count(p => p.IsOnSale);
                
                totalProducts += products.Count;
                totalSaleItems += saleCount;
                
                Console.WriteLine($"\n{fileName}:");
                Console.WriteLine($"  Total products: {products.Count}");
                Console.WriteLine($"  Sale items: {saleCount}");
                Console.WriteLine($"  Regular items: {products.Count - saleCount}");
            }
            
            Console.WriteLine($"\nGRAND TOTAL:");
            Console.WriteLine($"  Total products: {totalProducts}");
            Console.WriteLine($"  Sale items: {totalSaleItems}");
            Console.WriteLine($"  Regular items: {totalProducts - totalSaleItems}");
        }

        // Helper methods for filtering
        public List<Product> FilterByCategory(List<Product> products, string category) =>
            products.Where(p => p.Category?.Equals(category, StringComparison.OrdinalIgnoreCase) ?? false).ToList();

        public List<Product> GetSaleProducts(List<Product> products) =>
            products.Where(p => p.IsOnSale).ToList();
        
        public Dictionary<string, List<Product>> GroupByCategory(List<Product> products) =>
            products.GroupBy(p => p.Category ?? "Unknown").ToDictionary(g => g.Key, g => g.ToList());
    }
}