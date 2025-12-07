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

        public ScraperController(
            ScraperFactory scraperFactory, 
            ILogger<ScraperController> logger)
        {
            _scraperFactory = scraperFactory;
            _productService = new ProductService();
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
                    return NotFound(new { message = "Sites directory not found" });
                }

                // Process all Novus links
                var novusResults = await _scraperFactory.ProcessAllFilesAsync(
                    sitesDirectory, "NovusLinks_*.txt");

                int totalNovus = 0;
                foreach (var kvp in novusResults)
                {
                    var count = _productService.SaveProducts(kvp.Value, "Novus");
                    totalNovus += count;
                    _logger.LogInformation("Saved {Count} products from {File}", count, kvp.Key);
                }

                // Process all Silpo links
                var silpoResults = await _scraperFactory.ProcessAllFilesAsync(
                    sitesDirectory, "SilpoLinks_*.txt");

                int totalSilpo = 0;
                foreach (var kvp in silpoResults)
                {
                    var count = _productService.SaveProducts(kvp.Value, "Silpo");
                    totalSilpo += count;
                    _logger.LogInformation("Saved {Count} products from {File}", count, kvp.Key);
                }

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
                _logger.LogError(ex, "Scraper failed");
                return StatusCode(500, new { message = "Scraper failed", error = ex.Message });
            }
        }

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            var productService = new ProductService();
            var stores = productService.GetStores();
            
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
    }
}