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
            await ParallelScrapeSite(productsToScrape);
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

        private async Task ParallelScrapeSite(IEnumerable<string> productUrls)
        {
            //ConcurrentBag<Product> scrapedProducts = [];
            //CancellationTokenSource tokenSource = new();

            //ParallelOptions scrapeOptions = new()
            //{
            //    MaxDegreeOfParallelism = 3,
            //    CancellationToken = tokenSource.Token
            //};

            //void ExitCleanup(object? sender, EventArgs args) => tokenSource.Cancel();

            //AppDomain.CurrentDomain.ProcessExit += ExitCleanup;
            //Console.CancelKeyPress += ExitCleanup;

            //Channel<Product> channel = Channel.CreateBounded<Product>(10);

            static async ValueTask<Product> Scraper(string url, IPage page)
            {
                await page.GotoAsync(url);

                var priceSelector = page.Locator(".c-top-offer__price");
                var productSelector = page.Locator("div>h1");

                var price = await priceSelector.InnerTextAsync();
                var product = await productSelector.InnerTextAsync();

                return new(product, float.Parse(price[..2]), (int)WebsiteId.Heureka);
            }

            await PerfomScrape(productUrls, Scraper);

            //try
            //{
            //    await Parallel.ForEachAsync(productUrls, scrapeOptions, async (url, cancellationToken) =>
            //    {
            //        Console.WriteLine($"[Info] Scraping site: {url}...");
            //        await using var context = await _browser.NewContextAsync();
            //        await context.RouteAsync("**/*.{png,jpg,jpeg,gif,webp,svg}", r => r.AbortAsync());
            //        var page = await _browser.NewPageAsync();

            //        try
            //        {
            //            await page.GotoAsync(url);

            //            var priceSelector = page.Locator(".c-top-offer__price");
            //            var productSelector = page.Locator("div>h1");

            //            var price = await priceSelector.InnerTextAsync();
            //            var product = await productSelector.InnerTextAsync();

            //            scrapedProducts.Add(new(product, float.Parse(price[..2])));

            //            cancellationToken.ThrowIfCancellationRequested();
            //        }
            //        catch (OperationCanceledException) { throw; }
            //        catch (Exception ex)
            //        {
            //            Console.ForegroundColor = ConsoleColor.Red;
            //            Console.WriteLine($"[Error] Failed to scrape {url}: {ex.Message}");
            //            Console.ForegroundColor = ConsoleColor.White;
            //        }
            //    });
            //}
            //catch (OperationCanceledException)
            //{
            //    Console.ForegroundColor = ConsoleColor.Yellow;
            //    Console.WriteLine($"[Warn] Operation cancelled, saving gathered data to database");
            //    Console.ForegroundColor = ConsoleColor.White;

            //    _sqlite.Open();
            //    using var transaction = _sqlite.BeginTransaction();

            //    SqliteCommand command = new("INSERT INTO products (ProductName, NameHash, Price, WebsiteId) VALUES ($name, $hash, $price, $wid)");

            //    var productNameParam = command.Parameters.Add("$name", SqliteType.Text);
            //    var nameHashParam = command.Parameters.Add("$hash", SqliteType.Text);
            //    var priceParam = command.Parameters.Add("$price", SqliteType.Real);
            //    var websiteIdParam = command.Parameters.Add("$wid", SqliteType.Integer);

            //    foreach (var product in scrapedProducts)
            //    {
            //        productNameParam.Value = product.Name;
            //        nameHashParam.Value = product.NameHash;
            //        priceParam.Value = product.Price;
            //        websiteIdParam.Value = WebsiteId.Heureka;
            //        command.ExecuteNonQuery();
            //    }

            //    transaction.Commit();
            //    _sqlite.Close();
            //}
        }
    }
}
