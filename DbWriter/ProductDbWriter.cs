using Microsoft.Data.Sqlite;
using Webscraper.Models;

namespace Webscraper.DbWriter
{
    internal class ProductDbWriter : SQLiteDbWriter<Product>
    {
        protected override async Task WriteLogic(SqliteCommand command, IEnumerable<Product> items)
        {
            command.CommandText = "INSERT INTO products (ProductName, UrlHash, Price, WebsiteId) VALUES ($name, $hash, $price, $wid)";

            var productNameParam = command.Parameters.Add("$name", SqliteType.Text);
            var urlHashParam = command.Parameters.Add("$hash", SqliteType.Text);
            var priceParam = command.Parameters.Add("$price", SqliteType.Real);
            var websiteIdParam = command.Parameters.Add("$wid", SqliteType.Integer);

            foreach (var product in items)
            {
                productNameParam.Value = product.Name;
                urlHashParam.Value = product.UrlHash;
                priceParam.Value = product.Price;
                websiteIdParam.Value = product.WebsiteId;
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
