using DromVehiclesParser.WorkStages.Common.Parsers.Database;
using DromVehiclesParser.WorkStages.Common.Parsers.Models;
using DromVehiclesParser.WorkStages.Common.Stages.Database;
using DromVehiclesParser.WorkStages.Common.Stages.Models;
using Microsoft.Extensions.DependencyInjection;
using ParsingSDK.Parsing;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace Tests.StartParsingTests;

public sealed class StartParsingTest(DromTestsFixture fixture) : IClassFixture<DromTestsFixture>
{
    private readonly IServiceProvider _sp = fixture.Services;
    private const string LinkUrl = "https://auto.drom.ru/spec/ponsse/";

    [Fact]
    private async Task Invoke_Start_Parsing()
    {
        Guid parserId = Guid.NewGuid();
        string domain = "Drom";
        string type = "Техника";

        List<object> parserLinks = [ new
        {
            id = Guid.NewGuid(),
            url = LinkUrl,
        }];
        
        object message = new
        {
            id = parserId,
            parser_domain = domain,
            parser_type = type,
            parser_links = parserLinks
        };

        await _sp.PublishFakeMessage(message);
        await Task.Delay(TimeSpan.FromSeconds(10));
        bool hasWorkStage = await HasPaginationWorkStage();
        bool hasParser = await HasParser(parserId);
        Assert.True(hasWorkStage);
        Assert.True(hasParser);
    }
    
    private async Task<bool> HasPaginationWorkStage()
    {
        ParserWorkStageStoringImplementation.ParserWorkStageQuery query = new(Name: ParserWorkStageConstants.PAGINATION);
        await using AsyncServiceScope scope = _sp.CreateAsyncScope();
        await using NpgSqlSession session = scope.ServiceProvider.GetRequiredService<NpgSqlSession>();
        Maybe<ParserWorkStage> stage = await ParserWorkStage.FromDb(session, query);
        return stage.HasValue;
    }

    private async Task<bool> HasParser(Guid id)
    {
        WorkingParserStoringImplementation.WorkingParserQuery query = new(ParserId: id);
        await using AsyncServiceScope scope = _sp.CreateAsyncScope();
        await using NpgSqlSession session = scope.ServiceProvider.GetRequiredService<NpgSqlSession>();
        Maybe<WorkingParser> parser = await WorkingParser.FromDb(session, query);
        return parser.HasValue;
    }
}