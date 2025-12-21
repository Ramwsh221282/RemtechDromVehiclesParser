using DromVehiclesParser.Parsing.CatalogueParsing.Models;
using DromVehiclesParser.Parsing.ConcreteItemParsing.Models;
using ParsingSDK.Parsing;
using PuppeteerSharp;

namespace DromVehiclesParser.Commands.ExtractAdvertisementFromItsPage;

public sealed class WithdrawException : Exception;

public sealed class ExtractAdvertisementFromItsPageCommand(Func<Task<IPage>> pageSource)
    : IExtractAdvertisementFromItsPageCommand
{
    public async Task<DromAdvertisementFromPage> Extract(DromCatalogueAdvertisement catalogueAdvertisement)
    {
        IPage page = await pageSource();
        await NavigateToAdvertisementPage(page, catalogueAdvertisement);
        if (await IsWithdrawFromSale(page))
            throw new WithdrawException(); 
        return await ExtractUsingJavaScript(page, catalogueAdvertisement);
    }

    private async Task NavigateToAdvertisementPage(IPage page, DromCatalogueAdvertisement catalogueAdvertisement)
    {
        await page.PerformQuickNavigation(catalogueAdvertisement.Url, timeout: 5000);
    }

    private async Task<DromAdvertisementFromPage> ExtractUsingJavaScript(
        IPage page, 
        DromCatalogueAdvertisement catalogueAdvertisement)
    {
        const string javaScript = @"
            () => {
                const extractBreadCrumbsInfo = () => {
                    const breadCrumbsContainer = document.querySelector('div[data-ftid=""header_breadcrumb""]');
                    if (!breadCrumbsContainer) return null;

                    const breadCrumbsTextList = Array.from(
                        breadCrumbsContainer.querySelectorAll('div[class=""_1lj8ai61""]')
                    ).map(i => i.innerText.trim());

                    const length = breadCrumbsTextList.length;                    
                    const model = breadCrumbsTextList[length - 2];
                    const brand = breadCrumbsTextList[length - 3];
                    const category = breadCrumbsTextList[length - 4];

                    return { model, brand, category };
                };

                const extractCharacteristicsInfo = () => {
                    const characteristicsContainer = document.querySelector('table[data-ftid=""bulletin-specifications""]');
                    const tableRows = Array.from(characteristicsContainer.querySelectorAll('tr')).map(r => {
                        const name = r.querySelector('th')?.innerText;
                        const value = r.querySelector('td')?.innerText;
                        if (!name || !value) return null;
                        return { name: name, value: value };
                    });
                    return tableRows.filter(c => c !== null);
                };

                const extractPriceInfo = () => {
                    const price = document.querySelector('div[data-ftid=""bulletin-price""]').innerText;
                    const hasNds = price.includes(""НДС"");
                    const priceValue = parseInt(price.replace(/\D/g, ''), 10);
                    return { price: priceValue, isNds: hasNds };
                };

                const extractAddressInfo = () => {
                    const addressSelector = document.querySelector('div[data-ftid=""city""]');
                    const address = addressSelector.querySelector('span[data-ftid=""value""]').innerText;
                    return address;
                };

                const breadcrumbsInfo = extractBreadCrumbsInfo();
                const characteristics = extractCharacteristicsInfo();
                const priceInfo = extractPriceInfo();
                const addressInfo = extractAddressInfo();
                const title = breadcrumbsInfo.category + "" "" + breadcrumbsInfo.brand + "" "" + breadcrumbsInfo.model;
                const result = {                    
                    title: title,
                    model: breadcrumbsInfo.model,
                    brand: breadcrumbsInfo.brand,
                    category: breadcrumbsInfo.category,
                    characteristics: characteristics,
                    price: priceInfo.price,
                    isNds: priceInfo.isNds,
                    address: addressInfo,
                };
                return result;
            }
        ";
        
        AdvertisementFromPageJson? data = await page.EvaluateFunctionAsync<AdvertisementFromPageJson>(javaScript);
        
        if (data is null || !data.AllPropertiesSet())
            throw new InvalidOperationException("Invalid advertisement data");
        
        return data.ToDromAdvertisementFromPage(
            () => catalogueAdvertisement.Id,  
            () => catalogueAdvertisement.Url, 
            () => catalogueAdvertisement.Photos); 
    }

    private async Task<bool> IsWithdrawFromSale(IPage page)
    {
        const string withDrawSelector = "div.css-7akhit.e1u9wqx21";
        Maybe<IElementHandle> element = await page.GetElementRetriable(withDrawSelector, retryAmount: 5);
        return element.HasValue;
    }
    
    private sealed class AdvertisementFromPageJson
    {
        public string? Title { get; set; }
        public string? Model { get; set; }
        public string? Brand { get; set; }
        public string? Category { get; set; }
        public CharacteristicsJson[]? Characteristics { get; set; }
        public long Price { get; set; }
        public bool? IsNds { get; set; }
        public string? Address { get; set; }

        public bool AllPropertiesSet()
        {
            return !string.IsNullOrEmpty(Title) && !string.IsNullOrEmpty(Model) &&
                   !string.IsNullOrEmpty(Brand) && !string.IsNullOrEmpty(Category) &&
                   Price != 0 && IsNds.HasValue && !string.IsNullOrEmpty(Address);
        }

        public DromAdvertisementFromPage ToDromAdvertisementFromPage(
            Func<string> idSource,
            Func<string> urlSource, 
            Func<IReadOnlyList<string>> photoSource)
        {
            Dictionary<string, string> ctxDict = Characteristics!.ToDictionary(c => c.Name!, c => c.Value!);
            return new DromAdvertisementFromPage(
                Id: idSource(),
                Url: urlSource(),
                Characteristics: ctxDict,
                Price: Price,
                IsNds: IsNds!.Value,
                Title: Title!,
                Address: Address!,
                Photos: photoSource()
            );
        }
    }
    
    private sealed class CharacteristicsJson
    {
        public string? Name { get; set; }
        public string? Value { get; set; }
    }
}