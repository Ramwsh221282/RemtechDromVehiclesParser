using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using RemTech.SharedKernel.Infrastructure;
using RemTech.Tests.Shared;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Tests;

public sealed class DromTestsFixture : WebApplicationFactory<DromVehiclesParser.Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder().BuildPgVectorContainer();
    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder().BuildRabbitMqContainer();
    
    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();
        Services.ApplyDatabaseMigrations();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
        await _rabbitMqContainer.StopAsync();
        await _rabbitMqContainer.DisposeAsync();
    }
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(s =>
        {
            s.ReconfigurePostgreSqlOptions(_dbContainer);
            s.ReconfigureRabbitMqOptions(_rabbitMqContainer);
            s.DontUseQuartzServices();
            s.AddHostedService<FakeParserRegistrationTicketListener>();
        });
    }
}