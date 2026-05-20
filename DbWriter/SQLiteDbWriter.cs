using Microsoft.Data.Sqlite;
using System.Threading.Channels;
using Webscraper.Interfaces;
using Webscraper.Utils;

namespace Webscraper.DbWriter
{
    internal abstract class SQLiteDbWriter<T> where T : struct
    {
        const int DB_SAVE_BATCH_SIZE = 10;

        private Channel<T> _channel = null!;
        private SqliteConnection _sqlite = null!;
        private ILogger _logger = null!;

        public ChannelWriter<T> GetChannelWriter() => _channel.Writer;

        protected abstract Task WriteLogic(SqliteCommand command, IEnumerable<T> items);

        public Task StartWriting()
        {
            return Task.Run(() => DatabaseChannelConsumer(_channel.Reader), CancellationToken.None);
        }

        private async Task DatabaseChannelConsumer(ChannelReader<T> itemChannel)
        {
            List<T> batch = new(DB_SAVE_BATCH_SIZE);

            try
            {
                await foreach (var item in itemChannel.ReadAllAsync())
                {
                    batch.Add(item);

                    if (batch.Count >= DB_SAVE_BATCH_SIZE)
                    {
                        await BatchWriteToDatabase(batch);
                        batch.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
            }
            finally
            {
                if (batch.Count > 0)
                {
                    await BatchWriteToDatabase(batch);
                }
            }
        }

        private async Task BatchWriteToDatabase(IEnumerable<T> batch)
        {
            _logger.Info($"Saving a batch of {batch.Count()} items to database...");

            await _sqlite.OpenAsync();
            await using var transaction = await _sqlite.BeginTransactionAsync();

            await WriteLogic(_sqlite.CreateCommand(), batch);

            await transaction.CommitAsync();
            await _sqlite.CloseAsync();

            _logger.Info($"Database save completed!");
        }

        public static SQLiteDbWriter<T> CreateInstance<P>(SqliteConnection sqlite, ILogger? logger = null) where P : SQLiteDbWriter<T>, new()
        {
            Channel<T> dbChannel = Channel.CreateBounded<T>(new BoundedChannelOptions(DB_SAVE_BATCH_SIZE * 2)
            {
                FullMode = BoundedChannelFullMode.Wait
            });

            return new P()
            {
                _channel = dbChannel,
                _sqlite = sqlite,
                _logger = logger ?? new ConsoleLogger()
            };
        }
    }
}
