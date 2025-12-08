using DromVehiclesParser.ItemsPublishing.Models;
using DromVehiclesParser.Parsing.CatalogueParsing.Models;
using DromVehiclesParser.Parsing.ConcreteItemParsing.Models;
using DromVehiclesParser.Parsing.PaginationParsing.Models;
using Microsoft.Extensions.DependencyInjection;
using ParsingSDK.Parsing;

namespace Tests.ParsingTests;

public sealed class DromParsingTests(DromTestsFixture fixture) : IClassFixture<DromTestsFixture>
{
    private readonly IServiceProvider _services = fixture.Services;
    private const string Url = "https://auto.drom.ru/spec/ponsse/";

    [Fact]
    private async Task Extract_Pagination_Success()
    {
        BrowserFactory factory = _services.GetRequiredService<BrowserFactory>();
        DromPagination pagination = await DromPagination.Extract(Url, factory);
        Assert.NotEqual(0, pagination.MaxPage);
    }

    [Fact]
    private async Task Form_Catalogue_Pages_Success()
    {
        BrowserFactory factory = _services.GetRequiredService<BrowserFactory>();
        DromPagination pagination = await DromPagination.Extract(Url, factory);
        IEnumerable<DromCataloguePage> pages = pagination.Pages(Url).ToArray();
        Assert.NotEmpty(pages);
    }

    [Fact]
    private async Task Extract_Catalogue_Items_Success()
    {
        BrowserFactory factory = _services.GetRequiredService<BrowserFactory>();
        DromPagination pagination = await DromPagination.Extract(Url, factory);
        IEnumerable<DromCataloguePage> pages = pagination.Pages(Url);
        List<DromCatalogueItem> items = [];
        foreach (DromCataloguePage page in pages)
            items.AddRange(await page.ExtractItems(factory));
        Assert.NotEmpty(items);
    }

    [Fact]
    private async Task Extract_Pending_Items_Success()
    {
        BrowserFactory factory = _services.GetRequiredService<BrowserFactory>();
        DromPagination pagination = await DromPagination.Extract(Url, factory);
        IEnumerable<DromCataloguePage> pages = pagination.Pages(Url);
        List<DromCatalogueItem> items = [];
        foreach (DromCataloguePage page in pages)
            items.AddRange(await page.ExtractItems(factory));
        Assert.NotEmpty(items);
        List<DromPendingItem> pendingItems = [];
        foreach (DromCatalogueItem item in items)
        {
            pendingItems.Add(await item.CreatePendingItem(factory));
        }
        Assert.NotEmpty(pendingItems);
    }
}