using DromVehiclesParser.Parsing.ConcreteItemParsing.Models;

namespace DromVehiclesParser.ItemsPublishing.Models;

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