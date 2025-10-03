using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using ProductScraper;

namespace ProductScraper.Services
{
    /// <summary>
    /// Service for initializing and managing product scraping operations
    /// </summary>
    public class ScraperService
    {
        private readonly ScraperFactory _factory;
        private readonly ScraperConfig _config;
        private readonly string _outputDirectory;
        private readonly bool _autoSave;

        /// <summary>
        /// Initializes a new instance of the ScraperService with default configuration
        /// </summary>
        public ScraperService() : this(CreateDefaultConfig(), "output", autoSave: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ScraperService with custom configuration
        /// </summary>
        /// <param name="config">Scraper configuration settings</param>
        /// <param name="outputDirectory">Directory for saving results</param>
        /// <param name="autoSave">Whether to automatically save results after scraping</param>
        public ScraperService(ScraperConfig config, string outputDirectory = "output", bool autoSave = false)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _outputDirectory = outputDirectory ?? "output";
            _autoSave = autoSave;
            _factory = new ScraperFactory(_config);
            
            // Ensure output directory exists if auto-save is enabled
            if (_autoSave)
            {
                Directory.CreateDirectory(_outputDirectory);
            }
        }

        /// <summary>
        /// Creates default scraper configuration
        /// </summary>
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
        /// Processes all files matching the pattern in the specified directory
        /// </summary>
        /// <param name="directory">Directory containing input files</param>
        /// <param name="filePattern">File pattern to match (e.g., "NovusLinks_*.txt")</param>
        /// <param name="saveResults">Override auto-save setting for this operation</param>
        /// <returns>Dictionary of results with filename as key and product list as value</returns>
        public async Task<Dictionary<string, List<Product>>> ProcessAllFilesAsync(
            string directory = "sites", 
            string filePattern = "NovusLinks_*.txt",
            bool? saveResults = null)
        {
            Console.WriteLine($"=== Processing all {filePattern} files from {directory} ===");
            
            var results = await _factory.ProcessAllFilesAsync(directory, filePattern);
            
            PrintSummary(results);
            
            // Save if explicitly requested or if auto-save is enabled
            var shouldSave = saveResults ?? _autoSave;
            if (shouldSave)
            {
                SaveAllResults(results);
            }
            
            return results;
        }

        /// <summary>
        /// Processes a single file
        /// </summary>
        /// <param name="filePath">Path to the file to process</param>
        /// <param name="saveResults">Override auto-save setting for this operation</param>
        /// <returns>List of scraped products</returns>
        public async Task<List<Product>> ProcessSingleFileAsync(string filePath, bool? saveResults = null)
        {
            Console.WriteLine($"=== Processing file: {filePath} ===");
            
            var products = await _factory.ProcessFileAsync(filePath);
            
            Console.WriteLine($"Found {products.Count} products");
            
            // Save if explicitly requested or if auto-save is enabled
            var shouldSave = saveResults ?? _autoSave;
            if (shouldSave)
            {
                var fileName = Path.GetFileName(filePath);
                var results = new Dictionary<string, List<Product>>
                {
                    { fileName, products }
                };
                
                SaveAllResults(results);
            }
            
            return products;
        }

        /// <summary>
        /// Prints a summary of scraping results
        /// </summary>
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

        /// <summary>
        /// Saves all scraping results to files
        /// </summary>
        public void SaveAllResults(Dictionary<string, List<Product>> results)
        {
            // Ensure output directory exists
            Directory.CreateDirectory(_outputDirectory);
            
            foreach (var kvp in results)
            {
                var fileName = kvp.Key;
                var products = kvp.Value;
                
                // Create output filename based on input filename
                var outputFileName = Path.Combine(_outputDirectory, 
                    Path.GetFileNameWithoutExtension(fileName) + "_products.txt");
                
                SaveProductsToFile(products, outputFileName);
            }

            // Also create a combined file with all products
            var allProducts = results.Values.SelectMany(p => p).ToList();
            SaveProductsToFile(allProducts, Path.Combine(_outputDirectory, "all_products.txt"));
        }

        /// <summary>
        /// Saves products to a text file
        /// </summary>
        /// <param name="products">List of products to save</param>
        /// <param name="filename">Output filename</param>
        public void SaveProductsToFile(List<Product> products, string filename)
        {
            using var writer = new StreamWriter(filename);
            
            writer.WriteLine($"Total Products: {products.Count}");
            writer.WriteLine($"Generated: {DateTime.Now}");
            writer.WriteLine(new string('=', 80));
            writer.WriteLine();

            foreach (var product in products)
            {
                if (product.IsOnSale)
                {
                    writer.WriteLine($"[SALE] {product.Name}");
                    writer.WriteLine($"  Category: {product.Category ?? "Unknown"}");
                    writer.WriteLine($"  Old Price: {product.OldPrice} ₴");
                    writer.WriteLine($"  New Price: {product.Price} ₴");
                    writer.WriteLine($"  Discount: {product.Discount}");
                    writer.WriteLine($"  Valid Until: {product.ValidUntil}");
                }
                else
                {
                    writer.WriteLine($"{product.Name}");
                    writer.WriteLine($"  Category: {product.Category ?? "Unknown"}");
                    writer.WriteLine($"  Price: {product.Price} ₴");
                }
                writer.WriteLine();
            }
            
            Console.WriteLine($"Products saved to {filename}");
        }

        /// <summary>
        /// Gets products filtered by category
        /// </summary>
        public List<Product> FilterByCategory(List<Product> products, string category)
        {
            return products.Where(p => 
                p.Category?.Equals(category, StringComparison.OrdinalIgnoreCase) ?? false)
                .ToList();
        }

        /// <summary>
        /// Gets only sale products
        /// </summary>
        public List<Product> GetSaleProducts(List<Product> products)
        {
            return products.Where(p => p.IsOnSale).ToList();
        }
        
        /// <summary>
        /// Gets products grouped by category
        /// </summary>
        public Dictionary<string, List<Product>> GroupByCategory(List<Product> products)
        {
            return products
                .GroupBy(p => p.Category ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.ToList());
        }
    }
}