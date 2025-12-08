using DromVehiclesParser.Shared;
using ParsingSDK.Parsing;
using PuppeteerSharp;

namespace DromVehiclesParser.Parsing.PaginationParsing.Models;

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