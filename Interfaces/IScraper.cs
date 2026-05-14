using Microsoft.Playwright;

namespace Webscraper.Interfaces
{
    internal interface IScraper
    {
        public Task ScrapeSite();
    }
}
