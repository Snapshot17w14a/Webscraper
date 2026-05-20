namespace Webscraper.Models
{
    internal readonly struct Product
    {
        public Product(string name, float price, int websiteId, string urlHash)
        {
            Name = name;
            Price = price;
            WebsiteId = websiteId;
            UrlHash = urlHash;
        }

        public readonly string Name;
        public readonly float Price;
        public readonly string UrlHash;
        public readonly int WebsiteId;
    }
}
