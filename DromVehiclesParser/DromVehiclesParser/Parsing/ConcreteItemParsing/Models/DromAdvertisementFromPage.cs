namespace DromVehiclesParser.Parsing.ConcreteItemParsing.Models;

public sealed record DromAdvertisementFromPage(
    string Id,
    string Url,
    Dictionary<string, string> Characteristics,
    long Price,
    bool IsNds,
    string Title,
    string Address,
    IReadOnlyList<string> Photos
);