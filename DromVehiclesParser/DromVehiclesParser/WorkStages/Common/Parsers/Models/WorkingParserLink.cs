using DromVehiclesParser.CatalogueParsing.Models;

namespace DromVehiclesParser.WorkStages.Common.Parsers.Models;

public sealed record WorkingParserLink(
    Guid Id, 
    Guid ParserId, 
    string Url, 
    bool PaginationCalculated,
    int RetryCount);