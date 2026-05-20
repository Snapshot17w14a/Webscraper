using Microsoft.Playwright;
using System.Runtime.CompilerServices;
using Webscraper.Models;

namespace Webscraper.Scrapers
{
    internal class NajnakupScraper : Scraper
    {
        public override async Task ScrapeSite()
        {
            throw new NotImplementedException();
        }

        protected override async IAsyncEnumerable<Product> ExtractProductAsync(string url, IPage page, [EnumeratorCancellation] CancellationToken ct)
        {
            yield return default;
        }
    }
}
