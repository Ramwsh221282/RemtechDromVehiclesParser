using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTech.SharedKernel.Infrastructure.RabbitMq;

namespace DromVehiclesParser.WorkStages.StartParser.BackgroundTasks;

public sealed record StartParserWorkStageListenerDependencies(
    RabbitMqConnectionSource RabbitMq, 
    NpgSqlConnectionFactory NpgSql, 
    Serilog.ILogger Logger);