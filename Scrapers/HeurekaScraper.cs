using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Microsoft.Playwright;
using Webscraper.Models;
using Webscraper.Utils;

namespace Webscraper.Scrapers
{
    internal class HeurekaScraper : Scraper
    {
        public override async Task ScrapeSite()
        {
            XDocument sitemap = await GetSitemap();
            var productsToScrape = await GetProductURLs();
            await PerfomScrape(productsToScrape, WebsiteId.Heureka);
        }

        protected override async Task InitializePlaywright()
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        }

        private async Task<XDocument> GetSitemap()
        {
            XDocument sitemap;

            try {
                sitemap = SitemapLoader.LoadSitemapFromDisk("heureka-sitemap.xml");
            }
            catch (FileNotFoundException) {
                sitemap = await SitemapLoader.LoadSitemapFromUrl(_browser, "https://www.heureka.sk/sitemaps/sitemap_index.xml");
            }

            return sitemap;
        }

        private async Task<IEnumerable<string>> GetProductURLs()
        {
            const string testurl = "https://www.heureka.sk/sitemaps/product/papiernictvo/farby-na-sklo/0.xml";
            var page = await _browser.NewPageAsync();
            await page.GotoAsync(testurl);

            var productUrls = XDocument.Parse(await page.ContentAsync()).Descendants().Where(d => d.Name.LocalName == "loc" && !d.Value.Contains(".jpg")).Select(d => d.Value);

            return productUrls;
        }

        protected override async IAsyncEnumerable<Product> ExtractProductAsync(string url, IPage page, [EnumeratorCancellation] CancellationToken ct)
        {
            await page.GotoAsync(url);
            var urlHash = Hasher.HashString(url);

            bool isOutOfStock = false;
            try
            {
                var outOfStockLocator = page.GetByText("Tento produkt už neponúka žiadny e-shop.");
                await outOfStockLocator.WaitForAsync(new() { Timeout = 1000 });
                if (await outOfStockLocator.IsVisibleAsync())
                    isOutOfStock = true;
            }
            catch (TimeoutException) { /* Ignore the timeout exception. Try block throws if the product is out of stock */ }

            var productSelector = page.Locator("div>h1");
            var product = await productSelector.InnerTextAsync();

            double price = -1;
            if (!isOutOfStock)
            {
                string priceString = "";
                try
                {
                    var priceSelector = page.Locator(".c-top-offer__price");
                    await priceSelector.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
                    priceString = await priceSelector.InnerTextAsync();
                }
                catch (Exception)
                {
                    var priceSelector = page.Locator(".c-discount-price-box__body-content").Nth(0);
                    await priceSelector.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
                    priceString = await priceSelector.InnerTextAsync();
                }
                finally
                {
                    double priceFloat = double.Parse(priceString.Replace(',', '.')[..^2]);
                    price = Math.Round(priceFloat, 2);
                }
            }

            yield return new(product, price, (int)WebsiteId.Heureka, urlHash, url, isOutOfStock);
        }
    }
}
