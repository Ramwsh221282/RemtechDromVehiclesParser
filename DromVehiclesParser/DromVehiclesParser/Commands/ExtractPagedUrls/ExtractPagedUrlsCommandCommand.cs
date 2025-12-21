using DromVehiclesParser.Parsing.CatalogueParsing.Models;
using ParsingSDK.Parsing;
using PuppeteerSharp;

namespace DromVehiclesParser.Commands.ExtractPagedUrls;

public sealed class ExtractPagedUrlsCommandCommand(Func<Task<IPage>> pageSource) : IExtractPagedUrlsCommand
{
    public async Task<IEnumerable<DromCataloguePage>> Extract(string initialUrl)
    {
        IPage page = await pageSource();
        await NavigateToInitialUrl(page, initialUrl);
        IReadOnlyList<DromCataloguePage> pages = await CollectCataloguePages(page, initialUrl);
        return pages;
    }

    private async Task<IReadOnlyList<DromCataloguePage>> CollectCataloguePages(IPage page, string initialUrl)
    {
        List<DromCataloguePage> results = [];
        int currentPageNumber = 1;
        while (await CheckIfAdvertisementsShown(page))
        {
            string pagedUrl = $"{initialUrl}page{currentPageNumber}/";
            string nextPage = $"{initialUrl}page{currentPageNumber + 1}/";
            results.Add(DromCataloguePage.New(pagedUrl));
            currentPageNumber++;
            await page.PerformQuickNavigation(nextPage, timeout: 5000);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
        return results; 
    }
    
    private async Task NavigateToInitialUrl(IPage page, string initialUrl)
    {
        await page.PerformQuickNavigation(initialUrl, timeout: 2000);
    }

    private async Task<bool> CheckIfAdvertisementsShown(IPage page)
    {
        const string javaScript = @"
            () => {
                const itemsListSelector = document.querySelector('div[data-bulletin-list=""true""]');
                if (!itemsListSelector) return false;

                const itemsSelectors = document.querySelectorAll('div[data-ftid=""bulls-list_bull""]');
                if (!itemsSelectors) return false;

                return true;
            }
        ";
        
        return await page.EvaluateFunctionAsync<bool>(javaScript);
    }
}