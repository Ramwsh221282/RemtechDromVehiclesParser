namespace DromVehiclesParser.Parsing.PaginationParsing.Models;

public sealed record DromPagination(
    Guid Id, 
    int MaxPage, 
    int CurrentPage);