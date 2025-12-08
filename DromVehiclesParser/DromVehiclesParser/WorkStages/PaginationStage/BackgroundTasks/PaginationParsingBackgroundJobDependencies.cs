using ParsingSDK.Parsing;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace DromVehiclesParser.WorkStages.PaginationStage.BackgroundTasks;

public sealed record PaginationParsingBackgroundJobDependencies(
    NpgSqlConnectionFactory NpgSql, 
    Serilog.ILogger Logger,
    BrowserFactory BrowserFactory);