using Microsoft.AspNetCore.Mvc;
using ProductScraper;
using SellCatcher.Api.Services;
using static SellCatcher.Api.Services.ParserProductService;

namespace SellCatcher.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScraperController : ControllerBase
    {
        private readonly ScraperFactory _scraperFactory;
        private readonly ProductService _productService;
        private readonly ILogger<ScraperController> _logger;

        // FIXED: Inject ProductService instead of creating new instance
        public ScraperController(
            ScraperFactory scraperFactory, 
            ProductService productService,  // ← Added this parameter
            ILogger<ScraperController> logger)
        {
            _scraperFactory = scraperFactory;
            _productService = productService;  // ← Use injected service
            _logger = logger;
        }

        [HttpPost("run")]
        public async Task<IActionResult> RunScraper()
        {
            try
            {
                _logger.LogInformation("Starting scraper job at {Time}", DateTime.UtcNow);

                var sitesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                    "Services", "Scraper", "sites");

                if (!Directory.Exists(sitesDirectory))
                {
                    _logger.LogError("Sites directory not found: {Path}", sitesDirectory);
                    return NotFound(new { message = "Sites directory not found", path = sitesDirectory });
                }

                // Process all Novus links
                var novusResults = await _scraperFactory.ProcessAllFilesAsync(
                    sitesDirectory, "NovusLinks_*.txt");

                int totalNovus = 0;
                foreach (var kvp in novusResults)
                {
                    _logger.LogInformation("Processing {Count} products from {File}", kvp.Value.Count, kvp.Key);
                    var count = _productService.SaveProducts(kvp.Value, "Novus");
                    totalNovus += count;
                    _logger.LogInformation("✓ Saved {Count} products from {File}", count, kvp.Key);
                }

                // Process all Silpo links
                var silpoResults = await _scraperFactory.ProcessAllFilesAsync(
                    sitesDirectory, "SilpoLinks_*.txt");

                int totalSilpo = 0;
                foreach (var kvp in silpoResults)
                {
                    _logger.LogInformation("Processing {Count} products from {File}", kvp.Value.Count, kvp.Key);
                    var count = _productService.SaveProducts(kvp.Value, "Silpo");
                    totalSilpo += count;
                    _logger.LogInformation("✓ Saved {Count} products from {File}", count, kvp.Key);
                }

                _logger.LogInformation("Scraper completed. Novus: {Novus}, Silpo: {Silpo}, Total: {Total}", 
                    totalNovus, totalSilpo, totalNovus + totalSilpo);

                return Ok(new
                {
                    message = "Scraper completed successfully",
                    novusProducts = totalNovus,
                    silpoProducts = totalSilpo,
                    totalProducts = totalNovus + totalSilpo,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scraper failed: {Message}", ex.Message);
                return StatusCode(500, new { 
                    message = "Scraper failed", 
                    error = ex.Message,
                    stackTrace = ex.StackTrace 
                });
            }
        }

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            try
            {
                var stores = _productService.GetStores();
                
                var stats = stores.Select(s => new
                {
                    store = s.Name,
                    categories = s.Categories.Count,
                    totalProducts = s.Categories.Sum(c => c.Products.Count)
                });

                return Ok(new
                {
                    stores = stats,
                    lastUpdate = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get scraper status");
                return StatusCode(500, new { message = "Failed to get status", error = ex.Message });
            }
        }
    }
}