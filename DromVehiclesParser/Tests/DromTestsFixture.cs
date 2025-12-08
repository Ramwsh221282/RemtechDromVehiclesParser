using DromVehiclesParser.WorkStages.CatalogueStage.BackgroundTask;
using DromVehiclesParser.WorkStages.ConcreteItemWorkStage.BackgroundTasks;
using DromVehiclesParser.WorkStages.PaginationStage.BackgroundTasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using RemTech.SharedKernel.Infrastructure;
using RemTech.SharedKernel.Infrastructure.Quartz;
using RemTech.Tests.Shared;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Tests.ParserRegistrationTests;
using Tests.StartParsingTests;

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
            s.AddTransient<FakeStartParserPublisher>();
            s.AddSingleton<PaginationParsingBackgroundJobDependencies>();
            s.AddSingleton<ICronScheduleJob, PaginationParsingBackgroundJob>();
            s.AddSingleton<ICronScheduleJob, CatalogueProcessingBackgroundTask>();
            s.AddSingleton<ICronScheduleJob, ConcreteItemParsingBackgroundTask>();
            s.ReconfigureQuartzHostedService();
        });
    }
}