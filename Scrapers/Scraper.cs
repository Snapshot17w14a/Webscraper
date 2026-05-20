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

        private HashSet<string> completedUrls = []; 

        protected enum WebsiteId
        {
            Heureka = 0,
            Mock = 10,
        }

        public abstract Task ScrapeSite();

        protected abstract ValueTask<Product> ScrapePage(string url, string urlHash, IPage page);

        protected async Task PerfomScrape(IEnumerable<string> productUrls)
        {
            CancellationTokenSource tokenSource = new();
            
            await PrepareScrape(tokenSource.Token);

            // Set up the database writer
            SQLiteDbWriter<Product> dbWriter = SQLiteDbWriter<Product>.CreateDbWriter<ProductDbWriter>(_sqlite, _logger);
            ChannelWriter<Product> dbChannelWriter = dbWriter.GetChannelWriter();
            var databaseWriterTask = dbWriter.StartWriting();

            // Set up a channel for database writes
            //Channel<Product> dbChannel = Channel.CreateBounded<Product>(new BoundedChannelOptions(DB_SAVE_BATCH_SIZE * 2)
            //{
            //    FullMode = BoundedChannelFullMode.Wait
            //});
            //var databaseWriter = Task.Run(() => DatabaseChannelConsumer(dbChannel.Reader), CancellationToken.None);

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
                    if (completedUrls.Contains(urlHash))
                    {
                        _logger.Info($"Site {url} already scraped, skipping...");
                        return;
                    }

                    await using var context = await _browser.NewContextAsync();
                    await context.RouteAsync("**/*.{png,jpg,jpeg,gif,webp,svg}", r => r.AbortAsync());
                    var page = await _browser.NewPageAsync();

                    var product = await ScrapePage(url, urlHash, page);
                    await dbChannelWriter.WriteAsync(product, CancellationToken.None);
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

        //private async Task DatabaseChannelConsumer(ChannelReader<Product> productChannel)
        //{
        //    List<Product> batch = new(DB_SAVE_BATCH_SIZE);

        //    try
        //    {
        //        await foreach (var product in productChannel.ReadAllAsync())
        //        {
        //            batch.Add(product);

        //            if (batch.Count >= DB_SAVE_BATCH_SIZE)
        //            {
        //                await BatchWriteToDatabase(batch);
        //                batch.Clear();
        //            }
        //        }
        //    }
        //    catch(Exception ex) 
        //    {
        //        _logger.Error(ex.Message);
        //    }
        //    finally
        //    {
        //        if (batch.Count > 0)
        //        {
        //            await BatchWriteToDatabase(batch);
        //        }
        //    }
        //}

        //private async Task BatchWriteToDatabase(IEnumerable<Product> batch)
        //{
        //    _logger.Info($"Saving a batch of {batch.Count()} products to database...");

        //    await _sqlite.OpenAsync();
        //    await using var transaction = await _sqlite.BeginTransactionAsync();

        //    SqliteCommand command = _sqlite.CreateCommand();
        //    command.CommandText = "INSERT INTO products (ProductName, UrlHash, Price, WebsiteId) VALUES ($name, $hash, $price, $wid)";

        //    var productNameParam = command.Parameters.Add("$name", SqliteType.Text);
        //    var urlHashParam = command.Parameters.Add("$hash", SqliteType.Text);
        //    var priceParam = command.Parameters.Add("$price", SqliteType.Real);
        //    var websiteIdParam = command.Parameters.Add("$wid", SqliteType.Integer);

        //    foreach (var product in batch)
        //    {
        //        productNameParam.Value = product.Name;
        //        urlHashParam.Value = product.UrlHash;
        //        priceParam.Value = product.Price;
        //        websiteIdParam.Value = product.WebsiteId;
        //        await command.ExecuteNonQueryAsync();
        //    }

        //    await transaction.CommitAsync();
        //    await _sqlite.CloseAsync();

        //    _logger.Info($"Database save completed!");
        //}

        private async Task PrepareScrape(CancellationToken ct)
        {
            await _sqlite.OpenAsync(ct);
            
            SqliteCommand command = _sqlite.CreateCommand();
            command.CommandText = "SELECT UrlHash, WebsiteId FROM products WHERE WebsiteId = @wid";
            command.Parameters.AddWithValue("@wid", (int)WebsiteId.Heureka);
            var reader = await command.ExecuteReaderAsync(ct);

            List<string> readUrls = [];
            if (reader.HasRows)
                while (await reader.ReadAsync())
                    readUrls.Add(reader.GetString(0));

            else
            {
                _logger.Warn("No previously scraped products found in database, starting fresh...");
                return;
            }

            completedUrls = [.. readUrls];
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
    }
}
