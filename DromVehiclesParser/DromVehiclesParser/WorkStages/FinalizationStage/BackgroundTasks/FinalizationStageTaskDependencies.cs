using ParsingSDK.Publishing.BrokerPublishing;
using ParsingSDK.Publishing.TextPublishing;
using ParsingSDK.TextProcessing;
using RemTech.SharedKernel.Infrastructure.NpgSql;
using RemTech.SharedKernel.Infrastructure.RabbitMq;

namespace DromVehiclesParser.WorkStages.FinalizationStage.BackgroundTasks;

public sealed record FinalizationStageTaskDependencies(
    NpgSqlConnectionFactory NpgSql,
    Serilog.ILogger Logger,
    TextTransformerBuilder TextTransformerBuilder);