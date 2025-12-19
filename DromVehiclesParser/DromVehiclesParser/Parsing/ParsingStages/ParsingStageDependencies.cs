using ParsingSDK.Parsing;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace DromVehiclesParser.Parsing.ParsingStages;

public sealed record ParsingStageDependencies(
    BrowserFactory Browsers, 
    NpgSqlConnectionFactory NpgSql, 
    Serilog.ILogger Logger
);