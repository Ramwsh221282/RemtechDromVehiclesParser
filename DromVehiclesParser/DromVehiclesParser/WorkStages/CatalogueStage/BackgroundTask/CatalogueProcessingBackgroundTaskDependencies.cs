using ParsingSDK.Parsing;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace DromVehiclesParser.WorkStages.CatalogueStage.BackgroundTask;

public sealed record CatalogueProcessingBackgroundTaskDependencies(
    NpgSqlConnectionFactory NpgSql,
    Serilog.ILogger Logger,
    BrowserFactory BrowserFactory);