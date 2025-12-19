using Dapper;
using DromVehiclesParser.Parsers.Models;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace DromVehiclesParser.Parsers.Database;

public static class WorkingParserStoringImplementation
{
    extension(WorkingParser)
    {
        public static async Task<bool> Exists(NpgSqlSession session, CancellationToken ct = default)
        {
            const string sql = """
                               SELECT EXISTS(
                                   SELECT 1 
                                   FROM drom_vehicles_parser.working_parsers
                               )
                               """;
            CommandDefinition command = session.FormCommand(sql, null, ct);
            return await session.QuerySingleRow<bool>(command);
        }
    }
    
    extension(WorkingParser parser)
    {
        public async Task Persist(NpgSqlSession session, CancellationToken ct)
        {
            const string sql = """
                               INSERT INTO drom_vehicles_parser.working_parsers
                               (id, domain, type)
                               VALUES
                               (@id, @domain, @type)
                               ON CONFLICT (id) DO NOTHING
                               """;
            CommandDefinition command = session.FormCommand(sql, parser.ExtractParameters(), ct);
            await session.Execute(command);
        }

        private object ExtractParameters() => new
        {
            id = parser.Id,
            domain = parser.Domain,
            type = parser.Type
        };
    }
}