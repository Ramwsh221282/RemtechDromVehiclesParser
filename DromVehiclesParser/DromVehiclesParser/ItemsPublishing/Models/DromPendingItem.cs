namespace DromVehiclesParser.ItemsPublishing.Models;

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