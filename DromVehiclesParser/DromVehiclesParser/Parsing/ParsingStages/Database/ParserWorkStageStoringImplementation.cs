using System.Data;
using Dapper;
using DromVehiclesParser.Parsing.ParsingStages.Models;
using ParsingSDK.Parsing;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace DromVehiclesParser.Parsing.ParsingStages.Database;

public static class ParserWorkStageStoringImplementation
{
    public sealed record ParserWorkStageQuery(
        Guid? Id = null, 
        string? Name = null, 
        bool Finished = false, 
        bool Unfinished = false,
        bool WithLock = false);

    private sealed class TableRow
    {
        public required Guid Id { get; init; }
        public required string StageName { get; init; }
        public required bool Finished { get; init; }
    }

    extension(TableRow row)
    {
        private ParserWorkStage ToModel() => ParserWorkStage.MapFrom
        (
            row,
            r => r.Id,
            r => r.StageName,
            r => r.Finished
        );
    }

    extension(TableRow? row)
    {
        private Maybe<ParserWorkStage> MaybeStage() => row == null 
            ? Maybe<ParserWorkStage>.None() 
            : Maybe<ParserWorkStage>.Some(row.ToModel());
    }
    
    extension(ParserWorkStage)
    {
        public static async Task ClearTable(NpgSqlSession session, CancellationToken ct = default)
        {
            const string sql = "DELETE FROM drom_vehicles_parser.work_stages";
            CommandDefinition command = new(sql, cancellationToken: ct, transaction: session.Transaction);
            await session.Execute(command);
        }

        public static async Task<Maybe<ParserWorkStage>> FromDb(NpgSqlSession session, ParserWorkStageQuery query, CancellationToken ct = default)
        {
            (DynamicParameters parameters, string filterSql) = query.WhereClause();
            string lockClause = query.LockClause();
            string sql = $"""
                          SELECT id, stage_name, finished
                          FROM drom_vehicles_parser.work_stages
                          {filterSql}
                          {lockClause}
                          LIMIT 1
                          """;
            CommandDefinition command = session.FormCommand(sql, parameters, ct);
            TableRow? row = await session.QueryMaybeRow<TableRow>(command);
            return row.MaybeStage();
        }

        public static async Task<bool> HasAny(NpgSqlSession session, ParserWorkStageQuery query, CancellationToken ct = default)
        {
            (DynamicParameters parameters, string filterSql) = query.WhereClause();
            string sql = $"SELECT EXISTS (SELECT 1 FROM drom_vehicles_parser.work_stages {filterSql})";
            CommandDefinition command = session.FormCommand(sql, parameters, ct);
            bool exists = await session.QuerySingleRow<bool>(command);
            return exists;
        }
    }
    
    extension(ParserWorkStage stage)
    {
        public async Task Save(NpgSqlSession session, CancellationToken ct = default)
        {
            const string sql = """
                               INSERT INTO drom_vehicles_parser.work_stages
                               (id, stage_name, finished)
                               VALUES (@id, @stage_name, @finished);
                               """;
            CommandDefinition command = session.FormCommand(sql, stage.ExtractParameters(), ct);
            await session.Execute(command);
        }

        public async Task Update(NpgSqlSession session, CancellationToken ct = default)
        {
            const string sql = """
                               UPDATE drom_vehicles_parser.work_stages
                               SET stage_name = @stage_name, finished = @finished
                               WHERE id = @id;
                               """;
            CommandDefinition command = session.FormCommand(sql, stage.ExtractParameters(), ct);
            await session.Execute(command);
        }
        
        private object ExtractParameters() => new
        {
            id = stage.Id,
            stage_name = stage.StageName,
            finished = stage.Finished
        };
    }

    extension(ParserWorkStageQuery query)
    {
        private (DynamicParameters parameters, string filterSql) WhereClause()
        {
            List<string> filters = [];
            DynamicParameters parameters = new();

            if (query.Id.HasValue)
            {
                filters.Add("id = @id");
                parameters.Add("@id", query.Id.Value, DbType.Guid);
            }

            if (!string.IsNullOrWhiteSpace(query.Name))
            {
                filters.Add("stage_name = @stage_name");
                parameters.Add("@stage_name", query.Name, DbType.String);
            }

            if (query.Finished) filters.Add("finished is true");
            if (query.Unfinished) filters.Add("finished is false");
            return filters.Count == 0 ? (parameters, string.Empty) : (parameters, "WHERE " + string.Join(" AND ", filters));
        }

        private string LockClause() => query.WithLock ? "FOR UPDATE" : string.Empty;
    }
}