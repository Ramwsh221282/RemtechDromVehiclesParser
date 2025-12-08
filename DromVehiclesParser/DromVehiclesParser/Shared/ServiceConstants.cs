namespace DromVehiclesParser.Shared;

public static class ServiceConstants
{
    public const string CurrentServiceDomain = "Drom";
    public const string CurrentServiceType = "Техника";
    public const string CreateParsersQueue = "create.parsers";
    public const string CreateParserExchange = "parsers";
    public const string CreateParserRoutingKey = "parsers.creation";
    public static readonly string CurrentServiceExchange = $"{CurrentServiceDomain}.{CurrentServiceType}";
}