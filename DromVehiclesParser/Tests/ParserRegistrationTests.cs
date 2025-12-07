using DromVehiclesParser.ParserRegistration;
using DromVehiclesParser.ParserRegistration.BackgroundServices;
using DromVehiclesParser.ParserRegistration.Database;
using DromVehiclesParser.ParserRegistration.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

public sealed class ParserRegistrationTests(DromTestsFixture fixture) : IClassFixture<DromTestsFixture>
{
    private readonly IServiceProvider _sp = fixture.Services;
    
    [Fact]
    private async Task Register_Parser_Success()
    {
        await _sp.PublishParserRegistration();
        await Task.Delay(TimeSpan.FromSeconds(15));
        Assert.True(FakeParserRegistrationTicketListener.HadTicket);
        bool hasNoTickets = await HasNoTickets();
        Assert.True(hasNoTickets);
    }

    private async Task<bool> HasNoTickets()
    {
        await using AsyncServiceScope scope = _sp.CreateAsyncScope();
        NpgSqlParserRegistrationTicketsStorage storage = scope.ServiceProvider.GetRequiredService<NpgSqlParserRegistrationTicketsStorage>();
        ParserRegistrationTicketQuery query = new();
        IEnumerable<ParserRegistrationTicket> ticket = await storage.GetTickets(query);
        return ticket.Any() == false;
    }
}