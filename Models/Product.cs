namespace Webscraper.Models
{
    internal readonly struct Product
    {
        public Product(string name, double price, int websiteId, string urlHash, string url, bool isOutOfStock = false)
        {
            Name = name;
            Price = price;
            WebsiteId = websiteId;
            UrlHash = urlHash;
            Url = url;
            IsOutOfStock = isOutOfStock;
        }

        public readonly string Name;
        public readonly double Price;
        public readonly string UrlHash;
        public readonly string Url;
        public readonly bool IsOutOfStock;
        public readonly int WebsiteId;
    }
}
