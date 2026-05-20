using Microsoft.Data.Sqlite;
using Webscraper.Models;

namespace Webscraper.DbWriter
{
    internal class ProductDbWriter : SQLiteDbWriter<Product>
    {
        protected override async Task WriteLogic(SqliteCommand command, IEnumerable<Product> items)
        {
            command.CommandText = "INSERT INTO products (ProductName, UrlHash, Url, Price, IsOutOfStock, WebsiteId) VALUES ($name, $hash, $url, $price, $stock, $wid)";

            var productNameParam = command.Parameters.Add("$name", SqliteType.Text);
            var urlHashParam = command.Parameters.Add("$hash", SqliteType.Text);
            var urlParam = command.Parameters.Add("$url", SqliteType.Text);
            var priceParam = command.Parameters.Add("$price", SqliteType.Real);
            var stockParam = command.Parameters.Add("$stock", SqliteType.Integer);
            var websiteIdParam = command.Parameters.Add("$wid", SqliteType.Integer);

            foreach (var product in items)
            {
                productNameParam.Value = product.Name;
                urlHashParam.Value = product.UrlHash;
                urlParam.Value = product.Url;
                priceParam.Value = product.Price;
                stockParam.Value = product.IsOutOfStock ? 1 : 0;
                websiteIdParam.Value = product.WebsiteId;
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
