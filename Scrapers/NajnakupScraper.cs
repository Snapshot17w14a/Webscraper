using Microsoft.Playwright;
using System.Runtime.CompilerServices;
using Webscraper.Models;
using Webscraper.Utils;

namespace Webscraper.Scrapers
{
    internal class NajnakupScraper : Scraper
    {
        private static readonly string[] pages = ["https://www.najnakup.sk/kruzidla", "https://www.najnakup.sk/kruzidla/strana-2", "https://www.najnakup.sk/kruzidla/strana-3", "https://www.najnakup.sk/kruzidla/strana-4", "https://www.najnakup.sk/kruzidla/strana-5"];

        public override async Task ScrapeSite()
        {
            await PerfomScrape(pages, WebsiteId.Najnakup);
        }

        protected override async Task InitializePlaywright()
        {
            //const string zenPath = @"C:\Program Files\Zen Browser\zen.exe";
            //const string zenProfilePath = @"C:\Users\kevin\AppData\Roaming\zen\Profiles\c6vk1f5m.Default (release)-1";

            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                ExecutablePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                Headless = false,
                
            });
            //var ctx = await _playwright.Firefox.LaunchPersistentContextAsync(zenProfilePath, new()
            //{
            //    ExecutablePath = zenPath,
            //    Headless = false,
            //    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:151.0) Gecko/20100101 Firefox/151.0"
            //});
        }

        protected override async IAsyncEnumerable<Product> ExtractProductAsync(string url, IPage page, [EnumeratorCancellation] CancellationToken ct)
        {
            await page.GotoAsync(url);

            var rows = await page.Locator(".describe-product").AllAsync();
            foreach(var row in rows)
            {
                var nameAnchor = row.Locator(".item-title").First;
                var productUrl = await nameAnchor.GetAttributeAsync("href") ?? "https://www.najnakup.sk/";
                var urlHash = Hasher.HashString(productUrl);
                var name = await nameAnchor.InnerTextAsync();
                var price = await row.Locator(".cost").Locator("strong").InnerTextAsync();

                yield return new Product(name, double.Parse(price), (int)WebsiteId.Najnakup, urlHash, productUrl, false);
            }
        }
    }
}
