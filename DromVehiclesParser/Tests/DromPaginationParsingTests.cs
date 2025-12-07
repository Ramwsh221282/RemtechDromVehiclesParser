using Microsoft.Extensions.DependencyInjection;
using ParsingSDK.Parsing;
using PuppeteerSharp;

namespace Tests;

public sealed record DromPagination(
    Guid Id, 
    int MaxPage, 
    int CurrentPage);

public sealed record DromCataloguePage(
    Guid Id,
    Guid PaginationId,
    int Number,
    string Url,
    int RetryCount,
    bool Processed);

public sealed record DromCatalogueItem(
    string Id,
    string Url,
    IReadOnlyList<string> Photos,
    bool Processed,
    int RetryCount
    );

public sealed record DromPendingItem(
    string Id,
    string Url,
    long Price,
    bool IsNds,
    string Address,
    string Title,
    IReadOnlyList<string> Photos,
    IReadOnlyList<string> DescriptionList,
    IReadOnlyList<string> Characteristics);

public static class DromPendingItemCreation
{
    extension(DromPendingItem)
    {
        public static DromPendingItem FromCatalogueItem(
            DromCatalogueItem catalogueItem,
            long price,
            bool isNds,
            string address,
            string title,
            string description,
            Dictionary<string, string> characteristics)
        {
            IReadOnlyList<string> descList = [description];
            IReadOnlyList<string> ctxList = characteristics.Select(p => $"{p.Key}:{p.Value}").ToList();
            return new DromPendingItem(
                Id: catalogueItem.Id,
                Url: catalogueItem.Url,
                Price: price,
                IsNds: isNds,
                Address: address,
                Title: title,
                Photos: catalogueItem.Photos,
                DescriptionList: descList,
                Characteristics: ctxList
            );
        }
    }
}

public static class DromCatalogueItemImplementation
{
    extension(DromCatalogueItem item)
    {
        public async Task<DromPendingItem> CreatePendingItem(BrowserFactory factory)
        {
            IBrowser browser = await factory.ProvideBrowser(headless: false);
            await using IPage page = await browser.GetPage();
            try
            {
                await page.NavigatePage(item.Url);
                await page.ScrollBottom();
                Maybe<string> title = await page.BuildTitleFromBreadcrumbs();
                if (!title.HasValue) throw new InvalidOperationException("No address found.");
                Maybe<long> price = await page.ExtractPrice();
                if (!price.HasValue) throw new InvalidOperationException("No Price found.");
                Maybe<string> description = await page.ExtractDescription();
                if (!description.HasValue) throw new InvalidOperationException("No Description found.");
                Maybe<string> address = await item.ExtractAddress(page, ExtractAddressFromRightMenu, ExtractAddressFromTitle);
                if (!address.HasValue) throw new InvalidOperationException("No address found.");
                bool isNds = await page.ExtractNds();
                if (isNds) await Task.Delay(TimeSpan.FromSeconds(5));
                Dictionary<string, string> characteristics = await page.ExtractCharacteristics();
                return DromPendingItem.FromCatalogueItem(
                    item,
                    price: price.Value,
                    isNds: isNds,
                    address: address.Value,
                    title: title.Value,
                    description: description.Value,
                    characteristics: characteristics);
            }
            catch(Exception ex)
            {
                Console.WriteLine(item.Url);
                throw;
            }
            finally
            {
                await browser.DestroyAsync();
            }
        }

        private async Task<Maybe<string>> ExtractAddress(
            IPage page, 
            params Func<IPage, Task<Maybe<string>>>[] extractors)
        {
            foreach (var extractor in extractors)
            {
                Maybe<string> address = await extractor.Invoke(page);
                if (address.HasValue) return address;
            }
            
            return Maybe<string>.None();
        }
    }

    extension(IPage page)
    {
        private async Task<Maybe<string>> ExtractDescription()
        {
            Maybe<IElementHandle> container = await page.GetElementRetriable("div[data-ftid='info-full']");
            if (!container.HasValue) return Maybe<string>.None();
            Maybe<IElementHandle> description = await container.Value.GetElementRetriable("span[data-ftid='value']");
            if (!description.HasValue) return Maybe<string>.None();
            return await description.Value.GetElementInnerText();
        }

        private async Task<Maybe<string>> ExtractAddressFromTitle()
        {
            Maybe<string> title = await page.ExtractTitle();
            if (!title.HasValue) return Maybe<string>.None();
            if (!title.Value.Contains(" в ")) return Maybe<string>.None();
            string[] parts = title.Value.Split(" в ", StringSplitOptions.TrimEntries);
            return Maybe<string>.Some(parts[^1]);
        }
        
        private async Task<Maybe<string>> ExtractAddressFromRightMenu()
        {
            Maybe<IElementHandle> container = await page.GetElementRetriable("div[data-ftid='city']");
            if (!container.HasValue) return Maybe<string>.None();
            Maybe<IElementHandle> addressElement = await container.Value.GetElementRetriable("span[data-ftid='value']");
            if (!addressElement.HasValue) return Maybe<string>.None();
            Maybe<string> address = await addressElement.Value.GetElementInnerText();
            if (!address.HasValue) return Maybe<string>.None();
            if (address.Value.Contains(","))
            {
                string city = address.Value.Split(',', StringSplitOptions.TrimEntries)[0];
                return Maybe<string>.Some(city);
            }
            return address;
        }
        
        private async Task<Dictionary<string, string>> ExtractCharacteristics()
        {
            Dictionary<string, string> ctx = [];
            Maybe<IElementHandle> ctxContainer = await page.GetElementRetriable("table[data-ftid='bulletin-specifications']");
            if (!ctxContainer.HasValue) return ctx;
            IElementHandle[] tableRows = await ctxContainer.Value.GetElements("tr");
            foreach (IElementHandle row in tableRows)
            {
                Maybe<IElementHandle> name = await row.GetElementRetriable("th[data-ftid='property']");
                if (!name.HasValue) continue;
                Maybe<IElementHandle> value = await row.GetElementRetriable("td[data-ftid='value']");
                if (!value.HasValue) continue;
                Maybe<string> nameValue = await name.Value.GetElementInnerText();
                Maybe<string> valueValue = await value.Value.GetElementInnerText();
                if (!nameValue.HasValue || !valueValue.HasValue) continue;
                ctx.Add(nameValue.Value, valueValue.Value);
            }

            return ctx;
        }
        
        private async Task<bool> ExtractNds()
        {
            Maybe<IElementHandle> element = await page.GetElementRetriable("div.css-17b8egf.ez4q5ut1", retryAmount: 5);
            return element.HasValue;
        }
        
        private async Task<Maybe<long>> ExtractPrice()
        {
            Maybe<IElementHandle> priceContainer = await page.GetElementRetriable("div[data-ftid='bulletin-price']");
            if (!priceContainer.HasValue) return Maybe<long>.None();
            Maybe<string> priceString = await priceContainer.Value.GetElementInnerText();
            if (!priceString.HasValue) return Maybe<long>.None();
            IEnumerable<char> digitsOnly = priceString.Value.Where(char.IsDigit);
            string price = new string(digitsOnly.ToArray());
            if (!long.TryParse(price, out long result)) return Maybe<long>.None();
            return Maybe<long>.Some(result);
        }

        private async Task<Maybe<string>> BuildTitleFromBreadcrumbs()
        {
            Maybe<IElementHandle> container = await page.GetElementRetriable("div[data-ftid='header_breadcrumb']");
            if (!container.HasValue) return Maybe<string>.None();
            IElementHandle[] elements = await container.Value.GetElements("div[data-ftid='header_breadcrumb-item']");
            if (elements.Length == 0) return Maybe<string>.None();
            Maybe<string> model = await elements[^1].GetElementInnerText();
            Maybe<string> brand = await elements[^2].GetElementInnerText();
            Maybe<string> category = await elements[^3].GetElementInnerText();
            if (!model.HasValue || !brand.HasValue || !category.HasValue) return Maybe<string>.None();
            string title = $"{category.Value} {brand.Value} {model.Value}";
            return Maybe<string>.Some(title);
        }
        
        private async Task<Maybe<string>> ExtractTitle()
        {
            Maybe<IElementHandle> titleContainer = await page.GetElementRetriable("h1[data-ftid='page-title']");
            if (!titleContainer.HasValue) return Maybe<string>.None();
            Maybe<IElementHandle> titleElement = await titleContainer.Value.GetElementRetriable("span");
            if (!titleElement.HasValue) return Maybe<string>.None();
            return await titleElement.Value.GetElementInnerText();
        }
    }
}

public static class DromCatalogueItemCreation
{
    extension(DromCatalogueItem)
    {
        public static DromCatalogueItem New(string id, string url, IReadOnlyList<string> photos)
        {
            return new DromCatalogueItem(id, url, photos, false, 0);
        }
    }
}

public static class DromCataloguePageCreation
{
    extension (DromCataloguePage)
    {
        public static DromCataloguePage New(DromPagination pagination, string catalogueUrl, int page)
        {
            return new(
                Guid.NewGuid(), 
                pagination.Id, 
                page, 
                $"{catalogueUrl}page{page}", 
                RetryCount: 0, 
                Processed: false);
        }
    }
}

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
                if (!list.HasValue) return [];

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

public static class DromPaginationImplementation
{
    extension(DromPagination pagination)
    {
        public IEnumerable<DromCataloguePage> Pages(string catalogueUrl)
        {
            return Enumerable.Range(pagination.CurrentPage, pagination.MaxPage)
                .Select(p => DromCataloguePage.New(pagination, catalogueUrl, p));
        }
    }
}

public static class DromPaginationCreation 
{
    extension(DromPagination)
    {
        public static async Task<DromPagination> Extract(string catalogueUrl, BrowserFactory browserFactory)
        {
            IBrowser browser = await browserFactory.ProvideBrowser(headless: false);
            await using IPage page = await browser.GetPage();
            try
            {
                int initialPage = 1;
                string pageUrl = catalogueUrl.NormalizeToPagedUrl(initialPage);
                await page.NavigatePage(pageUrl);
                await page.ScrollBottom();
            
                while ((await page.GetItemsDataList()).HasValue)
                {
                    initialPage += 1;
                    pageUrl = catalogueUrl.NormalizeToPagedUrl(initialPage);
                    await page.NavigatePage(pageUrl);
                    await page.ScrollBottom();
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    if (!(await page.GetItemsDataList()).HasValue)
                    {
                        initialPage -= 1;
                        break;
                    }
                }

                return Default(initialPage);
            }
            finally
            {
                await browser.DestroyAsync();
            }
        }

        private static DromPagination Default(int maxPage)
        {
            return new(Id: Guid.NewGuid(), MaxPage: maxPage, CurrentPage: 1);
        }
    }
    
    extension(string input)
    {
        private string NormalizeToPagedUrl(int page)
        {
            return $"{input}page{page}";
        }
    }
}

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

public sealed class DromPaginationParsingTests(DromTestsFixture fixture) : IClassFixture<DromTestsFixture>
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