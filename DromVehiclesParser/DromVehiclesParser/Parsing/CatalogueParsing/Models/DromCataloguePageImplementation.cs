using DromVehiclesParser.Parsing.ConcreteItemParsing.Models;
using DromVehiclesParser.Shared;
using ParsingSDK.Parsing;
using PuppeteerSharp;

namespace DromVehiclesParser.Parsing.CatalogueParsing.Models;

public static class DromCataloguePageImplementation
{
    extension(DromCataloguePage page)
    {
        public DromCataloguePage MarkProcessed()
        {
            if (page.Processed)
                throw new InvalidOperationException(
                    """
                    The page is already processed.
                    Cannot mark processed.
                    """
                );
            return page with { Processed = true };
        }
        
        public async Task<IEnumerable<DromCatalogueItem>> ExtractItems(BrowserFactory factory)
        {
            IBrowser browser = await factory.ProvideBrowser(headless: false);
            await using IPage browserPage = await browser.GetPage();
            try
            {
                await browserPage.NavigatePage(page.Url);
                await browserPage.ScrollBottom();
                Maybe<IElementHandle> list = await browserPage.GetItemsDataList();
                if (!list.HasValue)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    return [];
                }

                IElementHandle[] elements = await list.Value.GetItems();
                foreach (IElementHandle element in elements)
                    await element.ExtractImages();
                
                list = await browserPage.GetItemsDataList();
                if (!list.HasValue) return [];
                elements = await list.Value.GetItems();

                List<DromCatalogueItem> items = [];
                foreach (IElementHandle element in elements)
                {
                    IReadOnlyList<string> images = await element.ExtractImages();
                    Maybe<string> url = await element.ExtractUrl();
                    if (!url.HasValue) continue;
                    string id = url.Value.Split('/')[^1].Split('.')[0];
                    items.Add(DromCatalogueItem.New(id, url.Value, images));
                }

                await Task.Delay(TimeSpan.FromSeconds(5));
                return items;
            }
            finally
            {
                await browser.DestroyAsync();
            }
        }
    }

    extension(IElementHandle element)
    {
        private async Task<IElementHandle[]> GetItems()
        {
            const string selector = "div[data-ftid='bulls-list_bull']";
            IElementHandle[] elements = await element.GetElements(selector);
            return elements;
        }

        private async Task<Maybe<string>> ExtractUrl()
        {
            const string titleContainerSelector = "div.css-jlnpz8.e10gq2qb0";
            const string titleSelector = "a[data-ftid='bull_title']";
            Maybe<IElementHandle> container = await element.GetElementRetriable(titleContainerSelector, retryAmount: 5);
            if (!container.HasValue) return Maybe<string>.None();
            Maybe<IElementHandle> title = await container.Value.GetElementRetriable(titleSelector, retryAmount: 5);
            if (!title.HasValue) return Maybe<string>.None();
            return await title.Value.GetAttribute("href");
        }
        
        private async Task<IReadOnlyList<string>> ExtractImages()
        {
            Maybe<IElementHandle> imageContainer =
                await element.GetElementRetriable("div[data-ftid='bull_image']", retryAmount: 5);
            if (!imageContainer.HasValue) return [];
            Maybe<IElementHandle> outerImageContainer =
                await imageContainer.Value.GetElementRetriable("div.emt6rd0.e4lamf0", retryAmount: 5);
            if (!outerImageContainer.HasValue) return [];
            Maybe<IElementHandle> innerImageContainer =
                await outerImageContainer.Value.GetElementRetriable("div.css-1h0gd61.e1lm3vns0", retryAmount: 5);
            if (!innerImageContainer.HasValue) return [];
            IElementHandle[] imageContainers = await innerImageContainer.Value.GetElements("div.css-7tf2d1.e103hojg0");

            List<string> images = [];
            foreach (IElementHandle image in imageContainers)
            {
                Maybe<IElementHandle> img = await image.GetElementRetriable("img", retryAmount: 5);
                await img.Value.HoverAsync();
                Maybe<string> srcSetAttribute = await img.Value.GetAttribute("srcset");
                if (!srcSetAttribute.HasValue) continue;
                string[] sets = srcSetAttribute.Value.Split(',', StringSplitOptions.TrimEntries);
                string highQualityImage = sets[^1].Split(' ', StringSplitOptions.TrimEntries)[0];
                images.Add(highQualityImage);
            }

            return images;
        }
    }
}