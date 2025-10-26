using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ProductScraper
{
    /// <summary>
    /// Factory class that determines which scraper to use based on the source file name
    /// </summary>
    public class ScraperFactory
    {
        private readonly ScraperConfig _config;
        private readonly Dictionary<string, Func<ScraperConfig, string, IProductScraper>> _scraperMap;

        public ScraperFactory(ScraperConfig config = null)
        {
            _config = config ?? new ScraperConfig();
            
            // Map file patterns to scraper types based on document content/category
            // The string parameter is the category name that will be assigned to products
            _scraperMap = new Dictionary<string, Func<ScraperConfig, string, IProductScraper>>(StringComparer.OrdinalIgnoreCase)
            {
                //{ "NovusLinks_fish", (cfg, cat) => new NovusProductScraper(cfg, cat) },
                //{ "NovusLinks_alcohol", (cfg, cat) => new NovusProductScraperAlc(cfg, cat) },
                //{ "NovusLinks_eggs", (cfg, cat) => new ProductScraperBread(cfg, cat) },
                //{ "NovusLinks_meat", (cfg, cat) => new ProductScraperBread(cfg, cat) },
                //{ "NovusLinks_bakery", (cfg, cat) => new ProductScraperBread(cfg, cat) },
                //{ "NovusLinks_halfmade", (cfg, cat) => new ProductScraperBread(cfg, cat) },
                //{ "NovusLinks_veg_fruit", (cfg, cat) => new ProductScraperBread(cfg, cat) },

                // Silpo: generic entry — file names like "SilpoLinks.txt" or "SilpoLinks_veg.txt"
                //{ "SilpoLinks_veg_fruit", (cfg, cat) => new SilpoProductScraper(cfg, cat) },
                //{ "SilpoLinks_cig", (cfg, cat) => new SilpoProductScraper(cfg, cat) },
                { "SilpoLinks_egg", (cfg, cat) => new SilpoProductScraper(cfg, cat) },
                { "SilpoLinks_fish", (cfg, cat) => new SilpoProductScraper(cfg, cat) },
                { "SilpoLinks_bread", (cfg, cat) => new SilpoProductScraper(cfg, cat) },
                { "SilpoLinks_meat", (cfg, cat) => new SilpoProductScraper(cfg, cat) },
                { "SilpoLinks_halfmade", (cfg, cat) => new SilpoProductScraper(cfg, cat) },

                // Add more mappings here as needed for each category
            };
        }

        /// <summary>
        /// Gets the appropriate scraper based on the file name
        /// </summary>
        /// <param name="fileName">Name of the file (e.g., "NovusLinks_alcohol.txt")</param>
        /// <returns>Instance of the appropriate scraper</returns>
        public IProductScraper GetScraper(string fileName)
        {
            // Extract the base name without extension
            var baseName = Path.GetFileNameWithoutExtension(fileName);
            
            // Determine category name from file name
            string categoryName = DetermineCategoryName(baseName);
            
            // Try exact match first (case-insensitive)
            if (_scraperMap.ContainsKey(baseName))
            {
                return _scraperMap[baseName](_config, categoryName);
            }

            // Try partial match (e.g., "NovusLinks_alcohol_001.txt" -> "NovusLinks_alcohol")
            foreach (var kvp in _scraperMap)
            {
                if (baseName.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value(_config, categoryName);
                }
            }

            // If no specific scraper found, throw exception
            throw new NotSupportedException($"No scraper found for file: {fileName}. Available scrapers: {string.Join(", ", _scraperMap.Keys)}");
        }

        /// <summary>
        /// Determines the category name from the file name
        /// </summary>
        private string DetermineCategoryName(string baseName)
        {
            // Remove known prefixes (NovusLinks_ or AtbLinks_) and convert to proper category name
            var categoryPart = baseName
                .Replace("NovusLinks_", "", StringComparison.OrdinalIgnoreCase)
                .Replace("AtbLinks_", "", StringComparison.OrdinalIgnoreCase)
                .Replace("NovusLinks", "", StringComparison.OrdinalIgnoreCase)
                .Replace("AtbLinks", "", StringComparison.OrdinalIgnoreCase)
                .Trim();

            // Map file name parts to friendly category names
            var categoryMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "fish", "Fish & Seafood" },
                { "alcohol", "Alcohol & Beverages 18+" },
                { "eggs", "Dairy & Eggs" },
                { "meat", "Meat & Poultry" },
                { "bakery", "Bakery" },
                { "halfmade", "Semi-Finished Products" },
                { "veg_fruit", "Fruits & Vegetables" },

                // example ATB-friendly names (extend as needed)
                { "groceries", "Groceries" },
                { "produce", "Produce" },
                { "dairy", "Dairy" }
            };

            return categoryMap.TryGetValue(categoryPart, out var friendlyName) 
                ? friendlyName 
                : categoryPart;
        }

        /// <summary>
        /// Processes all files in the specified directory, using the appropriate scraper for each
        /// </summary>
        /// <param name="directory">Directory containing the link files</param>
        /// <param name="filePattern">File search pattern (e.g., "NovusLinks_*.txt")</param>
        /// <returns>Dictionary mapping file names to their scraped products</returns>
        public async Task<Dictionary<string, List<Product>>> ProcessAllFilesAsync(
            string directory, 
            string filePattern = "NovusLinks_*.txt")
        {
            var results = new Dictionary<string, List<Product>>();
            var files = Directory.GetFiles(directory, filePattern);

            Console.WriteLine($"Found {files.Length} files to process");

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                Console.WriteLine($"\n=== Processing file: {fileName} ===");

                try
                {
                    // Get appropriate scraper for this file
                    var scraper = GetScraper(fileName);
                    
                    // Load URLs from file
                    var urls = File.ReadAllLines(file)
                                  .Where(url => !string.IsNullOrWhiteSpace(url))
                                  .ToList();

                    Console.WriteLine($"Loaded {urls.Count} URLs from {fileName}");
                    Console.WriteLine($"Using scraper: {scraper.GetType().Name}");

                    // Scrape products
                    var products = await scraper.ScrapeAsync(urls);
                    
                    results[fileName] = products;
                    Console.WriteLine($"Extracted {products.Count} products from {fileName}");
                }
                catch (NotSupportedException ex)
                {
                    Console.WriteLine($"WARNING: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR processing {fileName}: {ex.Message}");
                    results[fileName] = new List<Product>();
                }
            }

            return results;
        }

        /// <summary>
        /// Processes a single file with the appropriate scraper
        /// </summary>
        /// <param name="filePath">Full path to the file</param>
        /// <returns>List of scraped products</returns>
        public async Task<List<Product>> ProcessFileAsync(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            Console.WriteLine($"Processing file: {fileName}");

            // Get appropriate scraper
            var scraper = GetScraper(fileName);
            
            // Load URLs from file
            var urls = File.ReadAllLines(filePath)
                          .Where(url => !string.IsNullOrWhiteSpace(url))
                          .ToList();

            Console.WriteLine($"Loaded {urls.Count} URLs");
            Console.WriteLine($"Using scraper: {scraper.GetType().Name}");

            // Scrape and return products
            return await scraper.ScrapeAsync(urls);
        }

        /// <summary>
        /// Registers a custom scraper for a specific file pattern
        /// </summary>
        /// <param name="filePattern">The file pattern to match (e.g., "NovusLinks_Custom")</param>
        /// <param name="scraperFactory">Factory function that creates the scraper</param>
        public void RegisterScraper(string filePattern, Func<ScraperConfig, string, IProductScraper> scraperFactory)
        {
            _scraperMap[filePattern] = scraperFactory;
        }
    }

    /// <summary>
    /// Interface that all scrapers must implement
    /// </summary>
    public interface IProductScraper
    {
        Task<List<Product>> ScrapeAsync(List<string> catalogUrls);
    }
}