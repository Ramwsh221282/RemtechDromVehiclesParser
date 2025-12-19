using DromVehiclesParser.Parsing.CatalogueParsing.Models;

namespace DromVehiclesParser.Commands.ExtractPagedUrls;

public interface IExtractPagedUrlsCommand
{
    Task<IEnumerable<DromCataloguePage>> Extract(string initialUrl);
}