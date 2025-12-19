using DromVehiclesParser.Commands.ExtractAdvertisementsFromCatalogue;
using DromVehiclesParser.Commands.ExtractPagedUrls;
using DromVehiclesParser.Commands.HoverCatalogueImages;
using DromVehiclesParser.Parsing.CatalogueParsing.Models;
using Microsoft.Extensions.DependencyInjection;
using ParsingSDK.Parsing;
using PuppeteerSharp;

namespace Tests.ParsingCommandsTests;

public sealed class ExtractPagedUrlTests(IntegrationalTestsFixture fixture) : IClassFixture<IntegrationalTestsFixture>
{
    private IServiceProvider Services { get; } = fixture.Services;
    private Serilog.ILogger Logger { get; } = fixture.Services.GetRequiredService<Serilog.ILogger>();
    private BrowserFactory BrowserFactory { get; } = fixture.Services.GetRequiredService<BrowserFactory>();
     

    [Fact]
    private async Task Invoke_Extract_Paged_Url()
    {
        const string initialUrl = "https://auto.drom.ru/spec/john-deere/forestry/all/";
        IBrowser browser = await BrowserFactory.ProvideBrowser();
        IEnumerable<DromCataloguePage> pages = await new ExtractPagedUrlsCommandCommand(() => browser.GetPage())
            .UseLogging(Logger)
            .Extract(initialUrl);
        await browser.DestroyAsync();
        Assert.NotEmpty(pages);
    }

    [Fact]
    private async Task Invoke_Extract_Catalogue_Advertisements()
    {
        const string initialUrl = "https://auto.drom.ru/spec/john-deere/forestry/all/";
        IBrowser browser = await BrowserFactory.ProvideBrowser();
        IEnumerable<DromCataloguePage> pages = await new ExtractPagedUrlsCommandCommand(() => browser.GetPage())
            .UseLogging(Logger)
            .Extract(initialUrl);

        foreach (DromCataloguePage page in pages)
        {
            IHoverAdvertisementsCatalogueImagesCommand hoverCommand = new HoverAdvertisementsCatalogueImagesCommand(() => browser.GetPage())
                .UseLogging(Logger);
            
            IExtractAdvertisementsFromCatalogueCommand extractCommand = new ExtractAdvertisementsFromCatalogueCommand(() => browser.GetPage())
                    .UseLogging(Logger);
            
            DromCatalogueAdvertisement[] results = await extractCommand.Extract(page, hoverCommand);
            Assert.NotEmpty(results);
            break;
        }
        
        await browser.DestroyAsync();
    }
}