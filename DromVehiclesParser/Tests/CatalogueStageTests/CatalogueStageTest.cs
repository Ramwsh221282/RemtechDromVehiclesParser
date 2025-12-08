using DromVehiclesParser.WorkStages.Common.Stages.Database;
using DromVehiclesParser.WorkStages.Common.Stages.Models;
using Microsoft.Extensions.DependencyInjection;
using ParsingSDK.Parsing;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace Tests.CatalogueStageTests;

public sealed class CatalogueStageTest(DromTestsFixture fixture) : IClassFixture<DromTestsFixture>
{
    private readonly IServiceProvider _sp = fixture.Services;
    private const string LinkUrl = "https://auto.drom.ru/spec/ponsse/";

    [Fact]
    private async Task Invoke_Catalogue_Parsing()
    {
        await Task.Delay(TimeSpan.FromMinutes(1));
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
        await Task.Delay(TimeSpan.FromMinutes(2));
        
        bool hasConcreteItemsStage = await HasConcreteItemsStage();
        Assert.True(hasConcreteItemsStage);
    }
    
    private async Task<bool> HasConcreteItemsStage()
    {
        ParserWorkStageStoringImplementation.ParserWorkStageQuery query = new(Name: ParserWorkStageConstants.CONCRETE);
        await using AsyncServiceScope scope = _sp.CreateAsyncScope();
        await using NpgSqlSession session = scope.ServiceProvider.GetRequiredService<NpgSqlSession>();
        Maybe<ParserWorkStage> stage = await ParserWorkStage.FromDb(session, query);
        return stage.HasValue;
    }
}