using System.Xml.Linq;
using Microsoft.Playwright;

string[] categories = [ "papiernictvo" ];
string workingDirectory = Environment.CurrentDirectory;

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false/*, Args = ["--headless=new"] */});

XDocument sitemap;
if (!File.Exists(Path.Combine(workingDirectory, "heureka-sitemap.xml")))
{
    var sitemapPage = await browser.NewPageAsync();
    await sitemapPage.GotoAsync("https://www.heureka.sk/sitemaps/sitemap_index.xml");
    var content = await sitemapPage.ContentAsync();
    await sitemapPage.CloseAsync();

    sitemap = XDocument.Parse(content);

    var saveStream = File.OpenWrite(Path.Combine(workingDirectory, "heureka-sitemap.xml"));
    sitemap.Save(saveStream);
    saveStream.Close();
}
else
{
    var loadStream = File.OpenRead(Path.Combine(workingDirectory, "heureka-sitemap.xml"));
    sitemap = XDocument.Load(loadStream);
    loadStream.Close();
}

var filteredSitemapUrls = sitemap.Descendants().Where(d => d.Name.LocalName == "loc" && d.Value.Contains("/product/")).Where(d => categories.Any(cat => d.Value.Contains(cat))).Select(d => d.Value);
const string testUrl = "https://www.heureka.sk/sitemaps/product/papiernictvo/farby-na-sklo/0.xml";

IEnumerable<string> productUrls;

var page = await browser.NewPageAsync();
await page.GotoAsync(testUrl);
var testmapcontent =  await page.ContentAsync();
await page.CloseAsync();

productUrls = XDocument.Parse(testmapcontent).Descendants().Where(d => d.Name.LocalName == "loc" && !d.Value.Contains(".jpg")).Select(d => d.Value);

List<ValueTuple<string, float>> productPrices = [];

var productPage = await browser.NewPageAsync();
foreach(var item in productUrls)
{
    Console.WriteLine("Start fetching page " + item);

    await productPage.GotoAsync(item);

    var priceSelector = productPage.Locator(".c-top-offer__price");
    var nameSelector = productPage.Locator("div>h1");

    var price = await priceSelector.InnerTextAsync();
    var product = await nameSelector.InnerTextAsync();

    productPrices.Add((product, (float.Parse(price[..2]))));
}
await productPage.CloseAsync();

Console.WriteLine("\nScraped data:\n");

foreach (var item in productPrices)
{
    Console.WriteLine($"Product:\t{item.Item1}\nPrice:\t{item.Item2}\n");
}

Console.ReadLine();