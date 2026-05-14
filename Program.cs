using Webscraper;
using Webscraper.Scrapers;

internal class Program
{
    private static async Task Main(string[] args)
    {
        //var filteredSitemapUrls = sitemap.Descendants().Where(d => d.Name.LocalName == "loc" && d.Value.Contains("/product/")).Where(d => categories.Any(cat => d.Value.Contains(cat))).Select(d => d.Value);
        //var page = await browser.NewPageAsync();
        //await page.GotoAsync(testUrl);
        //var testmapcontent = await page.ContentAsync();
        //await page.CloseAsync();

        //productUrls = XDocument.Parse(testmapcontent).Descendants().Where(d => d.Name.LocalName == "loc" && !d.Value.Contains(".jpg")).Select(d => d.Value);

        await using var scraper = await Scraper.CreateInstance<MockScraper>();
        await scraper.ScrapeSite();

        //foreach (var item in result)
        //{
        //    Console.WriteLine($"Product:\t{item.Name}\nPrice:\t{item.Price} €\n");
        //}
    }
}