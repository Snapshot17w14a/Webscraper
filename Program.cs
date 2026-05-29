using Webscraper.Scrapers;

internal class Program
{
    private static async Task Main()
    {
        await using var scraper = await Scraper.CreateInstance<NajnakupScraper>();
        await scraper.ScrapeSite();
    }
}