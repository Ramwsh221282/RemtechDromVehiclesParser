using DromVehiclesParser.Commands.HoverCatalogueImages;
using DromVehiclesParser.Parsing.CatalogueParsing.Models;
using ParsingSDK.Parsing;
using PuppeteerSharp;

namespace DromVehiclesParser.Commands.ExtractAdvertisementsFromCatalogue;

public sealed class ExtractAdvertisementsFromCatalogueCommand(Func<Task<IPage>> pageSource) : IExtractAdvertisementsFromCatalogueCommand
{
    public async Task<DromCatalogueAdvertisement[]> Extract(DromCataloguePage page, IHoverAdvertisementsCatalogueImagesCommand hovering)
    {
        IPage webPage = await pageSource();
        await NavigateToCataloguePage(webPage, page);
        await hovering.Hover(page);
        return await ExtractCatalogueAdvertisementsUsingJavaScript(webPage);
    }

    private async Task NavigateToCataloguePage(IPage page, DromCataloguePage cataloguePage)
    {
        string url = cataloguePage.Url;
        await page.PerformQuickNavigation(url, timeout: 5000);
    }
    
    private async Task<DromCatalogueAdvertisement[]> ExtractCatalogueAdvertisementsUsingJavaScript(IPage page)
    {
        const string javaScript = @"
            () => {
                const itemsListSelector = document.querySelector('div[data-bulletin-list=""true""]');
                const itemsSelectors = Array.from(document.querySelectorAll('div[data-ftid=""bulls-list_bull""]'));
                
                const extractHighQualityImage = (srcSet) => {
                    if (!srcSet) return null;
                    const srcSetParts = srcSet.split(',');
                    const highQualityImage = srcSetParts[srcSetParts.length - 1].trim();
                    const highQualityImageUrl = highQualityImage.split(' ')[0].trim();
                    return highQualityImageUrl;
                };

                const extractPhotos = (item) => {
                    // div[data-ftid=""bull_image""]
                    const imageSelector = item.querySelector('div[data-ftid=""bull_image""]');
                    if (!imageSelector) return [];

                    // a[href]
                    const linkContainer = imageSelector.querySelector('a[href]');
                    if (!linkContainer) return [];

                    // .emt6rd0.css-1033la6.e4lamf0
                    const container = linkContainer.querySelector('.emt6rd0.css-1033la6.e4lamf0');
                    if (!container) return [];

                    // .css-1h0gd61.e1lm3vns0
                    const groupContainer = container.querySelector('.css-1h0gd61.e1lm3vns0');
                    if (!groupContainer) return [];

                    // .e103hojg0
                    const imageGroup = Array.from(groupContainer.querySelectorAll('.e103hojg0'));

                    const photos = imageGroup.map(groupItem => {
                        const img = groupItem.querySelector('img');
                        if (!img) return null;
                        const srcSet = img.getAttribute('srcset');
                        return extractHighQualityImage(srcSet);
                    }).filter(photo => photo !== null);

                    return photos;
                };

                const getTitleSelector = (item) => {
                    return item.querySelector('a[data-ftid=""bull_title""]');
                };

                const extractHref = (selector) => {
                    return selector.getAttribute(""href"");
                };

                const extractId = (selector) => {
                    const href = selector.getAttribute(""href"");
                    const hrefParts = href.split('/');
                    const lastPart = hrefParts[hrefParts.length - 1];

                    return lastPart.split('.')[0];
                };

                const result = itemsSelectors.map(item => {
                    const titleSelector = getTitleSelector(item);
                    const id = ""drom_vehicle_"" + extractId(titleSelector);
                    const url = extractHref(titleSelector);
                    const photos = extractPhotos(item);

                    return {
                        id: id,
                        url: url,
                        photos: photos
                    };
                });

                return result;
            }
        ";
        
        CatalogueAdvertisementJson[] json = await page.EvaluateFunctionAsync<CatalogueAdvertisementJson[]>(javaScript);
        return json.Where(j => j.AllPropertiesSet()).Select(j => j.ConvertToDromCatalogueAdvertisement()).ToArray();
    }

    private sealed class CatalogueAdvertisementJson
    {
        public string? Id { get; set; }
        public string? Url { get; set; }
        public string[]? Photos { get; set; }

        public bool AllPropertiesSet()
        {
            return !string.IsNullOrEmpty(Id) && !string.IsNullOrEmpty(Url) && Photos?.Length > 0;
        }

        public DromCatalogueAdvertisement ConvertToDromCatalogueAdvertisement()
        {
            return new DromCatalogueAdvertisement(Id!, Url!, Photos!.ToList(), Processed: false, RetryCount: 0);
        }
    }
}