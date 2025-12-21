using DromVehiclesParser.Commands.ExtractAdvertisementFromItsPage;
using DromVehiclesParser.Commands.ExtractAdvertisementsFromCatalogue;
using DromVehiclesParser.Commands.ExtractPagedUrls;
using DromVehiclesParser.Commands.HoverCatalogueImages;
using DromVehiclesParser.Parsing.CatalogueParsing.Models;
using DromVehiclesParser.Parsing.ConcreteItemParsing.Models;
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

    [Fact]
    private async Task Extract_Advertisement_From_Its_Page()
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
            List<DromAdvertisementFromPage> fromPageResults = [];
            
            foreach (DromCatalogueAdvertisement advertisement in results)
            {
                IExtractAdvertisementFromItsPageCommand extractFromItsPageCommand = new ExtractAdvertisementFromItsPageCommand(() => browser.GetPage())
                    .UseLogging(Logger);

                try
                {
                    DromAdvertisementFromPage result = await extractFromItsPageCommand.Extract(advertisement);
                    fromPageResults.Add(result);
                }
                catch(EvaluationFailedException)
                {
                    Logger.Fatal("Evaluation exception triggered. Skipping.");
                }
                catch (Exception ex)
                {
                    Logger.Fatal(ex, "Failed to extract advertisement from page {Url}", advertisement.Url);
                }
            }
            
            Assert.NotEmpty(fromPageResults);
            break;
        }
        
        await browser.DestroyAsync();
    }
}