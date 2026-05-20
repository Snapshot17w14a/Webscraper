using System.Security.Cryptography;
using System.Text;

namespace Webscraper.Utils
{
    internal static class Hasher
    {
        public static string HashString(string input)
        {
            var nameHash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(nameHash);
        }
    }
}
