using Microsoft.Playwright;
using System.Runtime.CompilerServices;
using Webscraper.Models;
using Webscraper.Utils;

namespace Webscraper.Scrapers
{
    internal class PricemaniaScraper : Scraper
    {
        private static readonly string[] categoryStrings = ["https://www.pricemania.sk/bloky/", "https://www.pricemania.sk/peracniky/", "https://www.pricemania.sk/desiatove-boxy/", "https://www.pricemania.sk/obaly-a-dosky-na-zosity/", "https://www.pricemania.sk/zosity/", "https://www.pricemania.sk/skolske-sety/", "https://www.pricemania.sk/skolske-tasky/"];
        private const string BASE_URL = "https://www.pricemania.sk";

        public override async Task ScrapeSite()
        {
            await PerfomScrape(categoryStrings, WebsiteId.Pricemania);
        }

        protected override async Task InitializePlaywright()
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
        }

        protected override async IAsyncEnumerable<Product> ExtractProductAsync(string url, IPage page, [EnumeratorCancellation] CancellationToken ct)
        {
            await page.GotoAsync(url);

            while (true)
            {
                try
                {
                    var button = page.Locator(".plViewMore");
                    await button.ClickAsync(new() { Timeout = 1000 });
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                }
                catch (Exception) { break; }
            }

            var rows = await page.Locator(".product-row__item").AllAsync();
            foreach (var row in rows)
            {
                var href = await row.GetAttributeAsync("data-href");
                var productUrl = BASE_URL + href;
                var urlHash = Hasher.HashString(productUrl);
                var name = (await row.Locator("h2 a").InnerTextAsync() ?? "Name not found!").Trim();
                var price = await row.Locator(".product-row__price-link").InnerTextAsync() ?? "0.0";

                yield return new Product(name, double.Parse(price[3..^2].Replace(',', '.')), (int)WebsiteId.Pricemania, urlHash, productUrl, false);
            }
        }
    }
}
