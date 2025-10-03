namespace ProductScraper
{
    public class ScraperConfig
    {
        public bool Headless { get; set; } = true;
        public float SlowMo { get; set; } = 1000;
        public bool EnableLogging { get; set; } = true;
        public bool EnableDebugOutput { get; set; } = false;
        public bool SaveDebugScreenshots { get; set; } = false;
        public bool SaveErrorScreenshots { get; set; } = false;
    }
}