using System.Xml.Linq;
using Microsoft.Playwright;

namespace Webscraper.Utils
{
    internal class SitemapLoader
    {
        public static XDocument LoadSitemapFromDisk(string filename)
        {
            var loadStream = File.OpenRead(Path.Combine(Environment.CurrentDirectory, filename));
            var sitemap = XDocument.Load(loadStream);
            loadStream.Close();

            return sitemap;
        }

        public static async Task<XDocument> LoadSitemapFromUrl(IBrowser browserInstance, string url)
        {
            var page = await browserInstance.NewPageAsync();
            await page.GotoAsync(url);
            var sitemap = XDocument.Load(await page.ContentAsync());

            return sitemap;
        }
    }
}
