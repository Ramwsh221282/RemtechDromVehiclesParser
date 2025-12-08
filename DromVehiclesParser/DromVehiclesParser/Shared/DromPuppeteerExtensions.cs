using ParsingSDK.Parsing;
using PuppeteerSharp;

namespace DromVehiclesParser.Shared;

public static class DromPuppeteerExtensions
{
    extension(IPage page)
    {
        public async Task<Maybe<IElementHandle>> GetItemsDataList()
        {
            const string containersSelector = "div.sptubg0.css-flpniz";
            Maybe<IElementHandle> container = await page.GetElementRetriable(containersSelector, retryAmount: 5);
            if (!container.HasValue) return Maybe<IElementHandle>.None();
            
            const string dataListSelector = "div[data-bulletin-list='true']";
            Maybe<IElementHandle> itemsList = await container.Value.GetElementRetriable(dataListSelector, retryAmount: 5);
            return itemsList;
        }
    }
}