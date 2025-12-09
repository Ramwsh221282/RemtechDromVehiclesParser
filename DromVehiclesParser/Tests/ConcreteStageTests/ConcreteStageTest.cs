using DromVehiclesParser.ItemsPublishing.Database;
using DromVehiclesParser.ItemsPublishing.Models;
using DromVehiclesParser.WorkStages.Common.Stages.Database;
using DromVehiclesParser.WorkStages.Common.Stages.Models;
using Microsoft.Extensions.DependencyInjection;
using ParsingSDK.Parsing;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace Tests.ConcreteStageTests;

public sealed class ConcreteStageTest(DromTestsFixture fixture) : IClassFixture<DromTestsFixture>
{
    private readonly IServiceProvider _sp = fixture.Services;
    private const string LinkUrl = "https://auto.drom.ru/spec/ponsse/";
    
    [Fact]
    private async Task Invoke_Concrete_Items_Stage()
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
        await Task.Delay(TimeSpan.FromMinutes(10));
        
        bool hasNoPendingItems = await HasNoPendingItems();
        Assert.True(hasNoPendingItems);
    }

    private async Task<bool> HasNoPendingItems()
    {
        await using AsyncServiceScope scope = _sp.CreateAsyncScope();
        await using NpgSqlSession session = scope.ServiceProvider.GetRequiredService<NpgSqlSession>();
        PendingItemsStoringImplementation.PendingItemsQuery query = new();
        IEnumerable<DromPendingItem> items = await IEnumerable<DromPendingItem>.GetMany(session, query);
        return items.Any() == false;
    }
}