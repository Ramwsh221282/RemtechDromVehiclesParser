using DromVehiclesParser.WorkStages.Common.Parsers.Database;
using DromVehiclesParser.WorkStages.Common.Parsers.Models;
using DromVehiclesParser.WorkStages.Common.Stages.Database;
using DromVehiclesParser.WorkStages.Common.Stages.Models;
using Microsoft.Extensions.DependencyInjection;
using ParsingSDK.Parsing;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace Tests.PaginationStageTests;

public sealed class PaginationStageTest(DromTestsFixture fixture) : IClassFixture<DromTestsFixture>
{
    private readonly IServiceProvider _sp = fixture.Services;
    private const string LinkUrl = "https://auto.drom.ru/spec/ponsse/";
    
    [Fact]
    private async Task Ensure_Pagination_Stage_Processed()
    {
        Guid parserId = Guid.NewGuid();
        string domain = "Drom";
        string type = "Техника";

        List<object> parserLinks = 
        [ 
            new { 
                id = Guid.NewGuid(),
                url = LinkUrl,
                }
        ];
        
        object message = new
        {
            id = parserId,
            parser_domain = domain,
            parser_type = type,
            parser_links = parserLinks
        };

        await _sp.PublishFakeMessage(message);
        await Task.Delay(TimeSpan.FromMinutes(1));
        
        bool hasCatalogueStage = await HasCatalogueStage();
        bool hasNoLinksWithUncalculatedPagination = await HasNoLinksWithUncalculatedPagination();
        
        Assert.True(hasCatalogueStage);
        Assert.True(hasNoLinksWithUncalculatedPagination);
    }

    private async Task<bool> HasCatalogueStage()
    {
        ParserWorkStageStoringImplementation.ParserWorkStageQuery query = new(Name: ParserWorkStageConstants.CATALOGUE);
        await using AsyncServiceScope scope = _sp.CreateAsyncScope();
        await using NpgSqlSession session = scope.ServiceProvider.GetRequiredService<NpgSqlSession>();
        Maybe<ParserWorkStage> stage = await ParserWorkStage.FromDb(session, query);
        return stage.HasValue;
    }

    private async Task<bool> HasNoLinksWithUncalculatedPagination()
    {
        WorkingParserStoringImplementation.WorkingParserQuery linksQuery = new(PaginationUncalculated: true);
        await using AsyncServiceScope scope = _sp.CreateAsyncScope();
        await using NpgSqlSession session = scope.ServiceProvider.GetRequiredService<NpgSqlSession>();
        IEnumerable<WorkingParserLink> links = await IEnumerable<WorkingParserLink>.LinksFromDb(session, linksQuery);
        return links.Any() == false;
    }
}