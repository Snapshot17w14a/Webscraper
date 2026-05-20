using Microsoft.Playwright;
using Webscraper.Models;

namespace Webscraper.Scrapers
{
    internal class MockScraper : Scraper
    {
        Random rnd = new();
        List<string> urls = [];

        public override async Task ScrapeSite()
        {
            for(int i = 0; i < 53; i++)
            {
                urls.Add(Guid.NewGuid().ToString());
            }

            await PerfomScrape(urls);
        }

        protected override async ValueTask<Product> ScrapePage(string url, string urlHash, IPage page)
        {
            await Task.Delay((int)(1000 * rnd.NextSingle()));
            return new Product(url, rnd.NextSingle(), (int)WebsiteId.Mock, urlHash);
        }
    }
}
