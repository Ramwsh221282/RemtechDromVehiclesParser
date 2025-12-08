using DromVehiclesParser.ItemsPublishing.Models;
using ParsingSDK.Parsing;
using PuppeteerSharp;

namespace DromVehiclesParser.Parsing.ConcreteItemParsing.Models;

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