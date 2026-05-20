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
            await PerfomScrape(productsToScrape);
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

        protected override async ValueTask<Product> ScrapePage(string url, string urlHash, IPage page)
        {
            await page.GotoAsync(url);

            var productSelector = page.Locator("div>h1");
            var product = await productSelector.InnerTextAsync();

            string price;
            try
            {
                var priceSelector = page.Locator(".c-top-offer__price");
                await priceSelector.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
                price = await priceSelector.InnerTextAsync();
            }
            catch (Exception)
            {
                var priceSelector = page.Locator(".c-discount-price-box__body-content").Nth(0);
                await priceSelector.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
                price = await priceSelector.InnerTextAsync();
            }

            return new(product, float.Parse(price.Replace(',', '.')[..^2]), (int)WebsiteId.Heureka, urlHash);
        }
    }
}
