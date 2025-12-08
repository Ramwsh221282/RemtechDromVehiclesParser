using ParsingSDK.Parsing;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace DromVehiclesParser.WorkStages.ConcreteItemWorkStage.BackgroundTasks;

public sealed record ConcreteItemParsingBackgroundTaskDependencies(
    NpgSqlConnectionFactory Npgsq,
    BrowserFactory BrowserFactory,
    Serilog.ILogger Logger
);