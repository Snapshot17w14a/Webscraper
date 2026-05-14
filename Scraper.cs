using System.Threading.Channels;
using Microsoft.Data.Sqlite;
using Microsoft.Playwright;
using Webscraper.Interfaces;
using Webscraper.Models;
using Webscraper.Utils;

namespace Webscraper
{
    internal abstract class Scraper : IAsyncDisposable
    {
        const int DB_SAVE_BATCH_SIZE = 10;

        protected IPlaywright _playwright = null!;
        protected IBrowser _browser = null!;
        protected SqliteConnection _sqlite = null!;
        protected ILogger<ConsoleLogger> _logger = null!;

        protected enum WebsiteId
        {
            Heureka = 0,
            Mock = 10,
        }

        public abstract Task ScrapeSite();

        protected async Task PerfomScrape(IEnumerable<string> productUrls, Func<string, IPage, ValueTask<Product>> scraper)
        {
            CancellationTokenSource tokenSource = new();
            ParallelOptions scrapeOptions = new()
            {
                MaxDegreeOfParallelism = 3,
                CancellationToken = tokenSource.Token
            };

            Channel<Product> dbChannel = Channel.CreateBounded<Product>(new BoundedChannelOptions(DB_SAVE_BATCH_SIZE * 2)
            {
                FullMode = BoundedChannelFullMode.Wait
            });
            var databaseWriter = Task.Run(() => DatabaseChannelConsumer(dbChannel.Reader, tokenSource.Token), tokenSource.Token);


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
                await Parallel.ForEachAsync(productUrls, scrapeOptions, async (url, ct) =>
                {
                    _logger.Info($"Scraping site {url}...");
                    await using var context = await _browser.NewContextAsync();
                    await context.RouteAsync("**/*.{png,jpg,jpeg,gif,webp,svg}", r => r.AbortAsync());
                    var page = await _browser.NewPageAsync();

                    var product = await scraper(url, page);
                    await dbChannel.Writer.WriteAsync(product, ct);

                });
            }
            catch (OperationCanceledException)
            {
                _logger.Warn("Interrupted, saving progress to database...");
            }
            finally
            {
                dbChannel.Writer.Complete();
                await databaseWriter;
                exitBlocker.Set();
            }
        }

        private async Task DatabaseChannelConsumer(ChannelReader<Product> productChannel, CancellationToken ct)
        {
            List<Product> batch = new(DB_SAVE_BATCH_SIZE);

            try
            {
                await foreach (var product in productChannel.ReadAllAsync(ct))
                {
                    batch.Add(product);

                    if (batch.Count >= DB_SAVE_BATCH_SIZE)
                    {
                        await BatchWriteToDatabase(batch, ct);
                        batch.Clear();
                    }
                }
            }
            catch(Exception ex) 
            {
                _logger.Error(ex.Message);
            }
            finally
            {
                if (batch.Count > 0)
                {
                    await BatchWriteToDatabase(batch, ct);
                }
            }
        }

        private async Task BatchWriteToDatabase(IEnumerable<Product> batch, CancellationToken ct)
        {
            _logger.Info($"Saving a batch of {batch.Count()} products to database...");

            await _sqlite.OpenAsync(ct);
            await using var transaction = await _sqlite.BeginTransactionAsync(ct);

            SqliteCommand command = _sqlite.CreateCommand();
            command.CommandText = "INSERT INTO products (ProductName, NameHash, Price, WebsiteId) VALUES ($name, $hash, $price, $wid)";

            var productNameParam = command.Parameters.Add("$name", SqliteType.Text);
            var nameHashParam = command.Parameters.Add("$hash", SqliteType.Text);
            var priceParam = command.Parameters.Add("$price", SqliteType.Real);
            var websiteIdParam = command.Parameters.Add("$wid", SqliteType.Integer);

            foreach (var product in batch)
            {
                productNameParam.Value = product.Name;
                nameHashParam.Value = product.NameHash;
                priceParam.Value = product.Price;
                websiteIdParam.Value = product.WebsiteId;
                await command.ExecuteNonQueryAsync(ct);
            }

            await transaction.CommitAsync(ct);
            await _sqlite.CloseAsync();

            _logger.Info($"Database save completed!");
        }

        public static async Task<T> CreateInstance<T>() where T : Scraper, new()
        {
            var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            var sqliteConnection = new SqliteConnection($"Data Source={Path.Combine(Environment.CurrentDirectory, "productdb.sqlite")};Mode=ReadWrite");
            var logger = new ConsoleLogger();

            var instance = new T
            {
                _playwright = playwright,
                _browser = browser,
                _sqlite = sqliteConnection,
                _logger = logger
            };

            return instance;
        }

        public async ValueTask DisposeAsync()
        {
            if (_browser != null) await _browser.CloseAsync();
            _playwright?.Dispose();
            _sqlite.Dispose();
        }
    }
}
