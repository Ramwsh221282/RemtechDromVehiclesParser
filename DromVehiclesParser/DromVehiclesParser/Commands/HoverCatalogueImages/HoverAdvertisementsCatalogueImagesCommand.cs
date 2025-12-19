using DromVehiclesParser.Parsing.CatalogueParsing.Models;
using ParsingSDK.Parsing;
using PuppeteerSharp;

namespace DromVehiclesParser.Commands.HoverCatalogueImages;

public sealed class HoverAdvertisementsCatalogueImagesCommand(Func<Task<IPage>> pageSource)
    : IHoverAdvertisementsCatalogueImagesCommand
{
    public async Task Hover(DromCataloguePage page)
    {
        await Invoke(page);
        await Invoke(page); // Third time invocation, because page updates dom content after hovering and loading images.
    }

    private async Task Invoke(DromCataloguePage page)
    {
        IPage webPage = await pageSource();
        await webPage.ScrollBottom();
        Maybe<IElementHandle> itemsList = await GetCatalogueItemsList(webPage);
        Maybe<IElementHandle[]> items = await GetCatalogueItemsFromList(itemsList);
        await HoverCatalogueItemsImages(items);
    }

    private static async Task<Maybe<IElementHandle>> GetCatalogueItemsList(IPage page)
    {
        const string selector = "div.sptubg0.css-flpniz";
        return await page.GetElementRetriable(selector, retryAmount: 5);
    }

    private static async Task<Maybe<IElementHandle[]>> GetCatalogueItemsFromList(Maybe<IElementHandle> list)
    {
        if (!list.HasValue) return Maybe<IElementHandle[]>.None();
        Maybe<IElementHandle> bulletinList = await list.Value.GetElementRetriable("div[data-bulletin-list]", retryAmount: 5);
        
        if (!bulletinList.HasValue) return Maybe<IElementHandle[]>.None();
        
        const string selector = "div[data-ftid='bulls-list_bull']";
        IElementHandle[] items = await bulletinList.Value.GetElements(selector);
        return Maybe<IElementHandle[]>.Some(items);
    }

    private static async Task HoverCatalogueItemsImages(Maybe<IElementHandle[]> items)
    {
        if (!items.HasValue) return;
        for (int i = 0; i < items.Value.Length; i++)
        {
            IElementHandle item = items.Value[i];
            await HoverItemImage(item);
        }
    }

    private static async Task HoverItemImage(IElementHandle item)
    {
        const string imageSelector = "div[data-ftid='bull_image']";
        Maybe<IElementHandle> image = await item.GetElementRetriable(imageSelector, retryAmount: 5);
        if (!image.HasValue) return;
        await image.Value.HoverAsync();

        Maybe<IElementHandle> linkContainer = await image.Value.GetElementRetriable("a[href]");
        if (!linkContainer.HasValue) return;
        
        Maybe<IElementHandle> container = await linkContainer.Value.GetElementRetriable(".emt6rd0.css-1033la6.e4lamf0");
        if (!container.HasValue) return;

        Maybe<IElementHandle> groupContainer = await container.Value.GetElementRetriable(".css-1h0gd61.e1lm3vns0");
        if (!groupContainer.HasValue) return;
        
        IElementHandle[] imageGroup = await groupContainer.Value.GetElements(".e103hojg0");
        foreach (IElementHandle groupItem in imageGroup)
        {
            await groupItem.HoverAsync();
        }
    }
}