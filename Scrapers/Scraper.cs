using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Data.Sqlite;
using Microsoft.Playwright;
using Webscraper.DbWriter;
using Webscraper.Interfaces;
using Webscraper.Models;
using Webscraper.Utils;

namespace Webscraper.Scrapers
{
    internal abstract class Scraper : IAsyncDisposable
    {
        protected IPlaywright _playwright = null!;
        protected IBrowser _browser = null!;
        protected SqliteConnection _sqlite = null!;
        protected ILogger<ConsoleLogger> _logger = null!;

        protected ConcurrentDictionary<string, byte> completedProductHashes = [];

        protected enum WebsiteId
        {
            Heureka = 0,
            Pricemania = 1,
            Mock = 10,
        }

        public abstract Task ScrapeSite();

        protected abstract IAsyncEnumerable<Product> ExtractProductAsync(string url, IPage page, CancellationToken ct);

        protected async Task PerfomScrape(IEnumerable<string> productUrls, WebsiteId websiteId)
        {
            CancellationTokenSource tokenSource = new();
            
            await PrepareScrape(websiteId, tokenSource.Token);

            // Set up the database writer
            SQLiteDbWriter<Product> dbWriter = SQLiteDbWriter<Product>.CreateInstance<ProductDbWriter>(_sqlite, _logger);
            ChannelWriter<Product> dbChannelWriter = dbWriter.GetChannelWriter();
            var databaseWriterTask = dbWriter.StartWriting();

            // Set up graceful shutdown handlers
            ManualResetEventSlim exitBlocker = new(false);

            void ExitHandler(object? sender, EventArgs args)
            {
                tokenSource.Cancel();
                exitBlocker.Wait(5000);
            }

            AppDomain.CurrentDomain.ProcessExit += ExitHandler;
            Console.CancelKeyPress += (s, e) => { e.Cancel = true; ExitHandler(s, e); };

            try
            {
                ParallelOptions scrapeOptions = new()
                {
                    MaxDegreeOfParallelism = 3,
                    CancellationToken = tokenSource.Token
                };

                // Scrape each product page in parallel, but limit the degree of parallelism to avoid rate limiting by the target website
                await Parallel.ForEachAsync(productUrls, scrapeOptions, async (url, ct) =>
                {
                    _logger.Info($"Scraping site {url}...");

                    var urlHash = Hasher.HashString(url);
                    if (completedProductHashes.ContainsKey(urlHash))
                    {
                        _logger.Info($"Site {url} already scraped, skipping...");
                        return;
                    }

                    await using var context = await _browser.NewContextAsync();
                    await context.RouteAsync("**/*.{png,jpg,jpeg,gif,webp,svg}", r => r.AbortAsync());
                    var page = await _browser.NewPageAsync();

                    await foreach(var product in ExtractProductAsync(url, page, ct))
                    {
                        if (!completedProductHashes.TryAdd(product.UrlHash, 0))
                            continue;

                        await dbChannelWriter.WriteAsync(product, CancellationToken.None);
                    }

                    await page.CloseAsync();
                });

                _logger.Info("Scraping complete, waiting for database operations to finish...");
            }
            catch (OperationCanceledException)
            {
                _logger.Warn("Interrupted, saving progress to database...");
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
            }
            finally
            {
                dbChannelWriter.Complete();
                await databaseWriterTask;
                exitBlocker.Set();
            }
        }

        private async Task PrepareScrape(WebsiteId websiteId, CancellationToken ct)
        {
            await _sqlite.OpenAsync(ct);
            
            SqliteCommand command = _sqlite.CreateCommand();
            command.CommandText = "SELECT UrlHash, WebsiteId FROM products WHERE WebsiteId = @wid AND IsOutOfStock = 0";
            command.Parameters.AddWithValue("@wid", (int)websiteId);
            var reader = await command.ExecuteReaderAsync(ct);

            List<string> readUrls = [];
            if (reader.HasRows)
                while (await reader.ReadAsync(ct))
                    readUrls.Add(reader.GetString(0));

            else
            {
                _logger.Warn("No previously scraped products found in database, starting fresh...");
                return;
            }

            completedProductHashes = new ConcurrentDictionary<string, byte>(
                readUrls.Select(url => new KeyValuePair<string, byte>(url, 0))
            );
        }

        public async ValueTask DisposeAsync()
        {
            if (_browser != null) await _browser.CloseAsync();
            _playwright?.Dispose();
            _sqlite.Dispose();
        }

        public static async Task<T> CreateInstance<T>() where T : Scraper, new()
        {
            var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

            var instance = new T
            {
                _playwright = playwright,
                _browser = browser,
                _sqlite = new SqliteConnection($"Data Source={Path.Combine(Environment.CurrentDirectory, "productdb.sqlite")};Mode=ReadWrite"),
                _logger = new ConsoleLogger()
            };

            return instance;
        }

        public class ScrapeNotPossibleException(string message) : Exception(message)
        {
        }
    }
}
