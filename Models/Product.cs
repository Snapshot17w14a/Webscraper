using System.Security.Cryptography;
using System.Text;

namespace Webscraper.Models
{
    internal readonly struct Product
    {
        public Product(string name, float price, int websiteId)
        {
            Name = name;
            Price = price;
            WebsiteId = websiteId;

            var nameHash = SHA256.HashData(Encoding.UTF8.GetBytes(name));
            NameHash = Convert.ToBase64String(nameHash);
        }

        public readonly string Name;
        public readonly float Price;
        public readonly string NameHash;
        public readonly int WebsiteId;
    }
}
