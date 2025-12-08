using System.Data;
using Dapper;
using DromVehiclesParser.WorkStages.Common.Parsers.Models;
using ParsingSDK.Parsing;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace DromVehiclesParser.WorkStages.Common.Parsers.Database;

public static class WorkingParserStoringImplementation
{
    public sealed record WorkingParserQuery(
        Guid? Id = null, 
        bool PaginationCalculated = false, 
        bool PaginationUncalculated = false, 
        bool WithLock = false,
        int? RetryCountThreshold = null);

    private sealed class ParserRow
    {
        public required Guid Id { get; init; }
        public required string Domain { get; init; }
        public required string Type { get; init; }
        public List<LinkRow> Links { get; init; } = [];
    }

    private sealed class LinkRow
    {
        public required Guid Id { get; init; }
        public required Guid ParserId { get; init; }
        public required string Url { get; init; }
        public required bool PaginationCalculated { get; init; } 
        public required int RetryCount { get; init; }
    }

    extension(ParserRow row)
    {
        private WorkingParser ToModel() => WorkingParser.MapFrom
        (
            row,
            idMap: r => r.Id,
            domainMap: r => r.Domain,
            typeMap: r => r.Type,
            linksMap: r => r.Links.Select(l => new WorkingParserLink(l.Id, l.ParserId, l.Url, l.PaginationCalculated, l.RetryCount)).ToList()
        );
    }
    
    extension(WorkingParserQuery query)
    {
        private (DynamicParameters parameters, string filterSql) WhereClause()
        {
            List<string> filters = [];
            DynamicParameters parameters = new();

            if (query.Id.HasValue)
            {
                filters.Add("l.parser_id = @parserId");
                parameters.Add("@parserId", query.Id.Value, DbType.Guid);
            }

            if (query.RetryCountThreshold.HasValue)
            {
                filters.Add("l.retry_count < @retryCountThreshold");
                parameters.Add("@retryCountThreshold", query.RetryCountThreshold.Value, DbType.Int32);
            }
            
            if (query.PaginationCalculated) filters.Add("l.pagination_calculated is true");
            if (query.PaginationUncalculated) filters.Add("l.l.pagination_calculated is false");

            return filters.Count == 0
                ? (parameters, string.Empty)
                : (parameters, "WHERE " + string.Join(" AND ", filters));
        }

        private string LockClause() => query.WithLock ? "FOR UPDATE" : string.Empty;
    }

    extension(IEnumerable<WorkingParserLink>)
    {
        public static async Task<IEnumerable<WorkingParserLink>> LinksFromDb(NpgSqlSession session, WorkingParserQuery query, CancellationToken ct = default)
        {
            (DynamicParameters parameters, string filterSql) = query.WhereClause();
            string lockClause = query.LockClause();
            
            string sql = $"""
                          SELECT
                          l.id as link_id,
                          l.parser_id as parser_id,
                          l.url as link_url,
                          l.pagination_calculated as pagination_calculated,
                          l.retry_count as retry_count
                          FROM drom_vehicles_parser.working_parser_links l
                          {filterSql}
                          {lockClause}
                          """;
            
            CommandDefinition command = session.FormCommand(sql, parameters, ct);
            IDataReader reader = await session.ExecuteReader(command, ct);
            List<WorkingParserLink> links = [];
            
            while (reader.Read())
            {
                Guid parserId = reader.GetGuid(reader.GetOrdinal("parser_id"));
                Guid linkId = reader.GetGuid(reader.GetOrdinal("link_id"));
                string linkUrl = reader.GetString(reader.GetOrdinal("link_url"));
                bool pagination = reader.GetBoolean(reader.GetOrdinal("pagination_calculated"));
                int retryCount = reader.GetInt32(reader.GetOrdinal("retry_count"));

                WorkingParserLink row = new WorkingParserLink(
                    Id: linkId,
                    ParserId: parserId,
                    Url: linkUrl,
                    PaginationCalculated: pagination,
                    RetryCount: retryCount
                );

                links.Add(row);
            }

            return links;
        }
    }
    
    extension(WorkingParser)
    {
        public static async Task<Maybe<WorkingParser>> FromDb(NpgSqlSession session, WorkingParserQuery query, CancellationToken ct = default)
        {
            (DynamicParameters parameters, string filterSql) = query.WhereClause();
            string lockClause = query.LockClause();
            string sql = $"""
                          SELECT 
                          p.domain as parser_domain, 
                          p.type as parser_type,
                          l.id as link_id,
                          l.parser_id as parser_id,
                          l.url as link_url,
                          l.pagination_calculated as pagination_calculated,
                          l.retry_count as retry_count
                          FROM drom_vehicles_parser.working_parser_links l
                          INNER JOIN drom_vehicles_parser.working_parsers p ON l.parser_id = p.id
                          {filterSql}
                          {lockClause}
                          """;
            
            CommandDefinition command = session.FormCommand(sql, parameters, ct);
            IDataReader reader = await session.ExecuteReader(command, ct);
            
            Dictionary<Guid, ParserRow> data = [];
            while (reader.Read())
            {
                Guid parserId = reader.GetGuid(reader.GetOrdinal("parser_id"));
                if (!data.TryGetValue(parserId, out ParserRow? row))
                {
                    string domain = reader.GetString(reader.GetOrdinal("parser_domain"));
                    string type = reader.GetString(reader.GetOrdinal("parser_type"));
                    row = new ParserRow()
                    {
                        Id = parserId,
                        Domain = domain,
                        Type = type
                    };
                    data.Add(parserId, row);
                }
                
                Guid linkId = reader.GetGuid(reader.GetOrdinal("link_id"));
                string linkUrl = reader.GetString(reader.GetOrdinal("link_url"));
                bool pagination = reader.GetBoolean(reader.GetOrdinal("pagination_calculated"));
                int retryCount = reader.GetInt32(reader.GetOrdinal("retry_count"));
                    
                data[parserId].Links.Add(new LinkRow()
                {
                    Id = linkId,
                    ParserId = parserId,
                    Url = linkUrl,
                    PaginationCalculated = pagination,
                    RetryCount = retryCount
                });
            }

            return data.Count == 0 ? Maybe<WorkingParser>.None() : Maybe<WorkingParser>.Some(data.First().Value.ToModel());
        }
    }
    
    extension(WorkingParser parser)
    {
        public async Task Save(NpgSqlSession session, bool withLinks = false, CancellationToken ct = default)
        {
            const string sql = """
                               INSERT INTO drom_vehicles_parser.working_parsers
                               (id, domain, type)
                               VALUES
                               (@id, @domain, @type)
                               """;
            CommandDefinition command = session.FormCommand(sql, parser.ExtractParameters(), ct);
            await session.Execute(command);
            if (withLinks) await parser.SaveLinks(session);
        }

        private async Task SaveLinks(NpgSqlSession session)
        {
            const string sql = """
                               INSERT INTO drom_vehicles_parser.working_parser_links
                               (id, parser_id, url, pagination_calculated, retry_count)
                               VALUES
                               (@id, @parser_id, @url, @pagination_calculated, @retry_count)
                               """;
            object parameters = parser.Links.ExtractParameters();
            await session.ExecuteBulk(sql, parameters);
        }
        
        private object ExtractParameters() => new
        {
            id = parser.Id,
            domain = parser.Domain,
            type = parser.Type
        };
    }

    extension(IEnumerable<WorkingParserLink> links)
    {
        private object ExtractParameters() => links.Select(l => l.ExtractParameters());
    }

    extension(WorkingParserLink link)
    {
        private object ExtractParameters() => new
        {
            id = link.Id,
            parser_id = link.ParserId,
            url = link.Url,
            pagination_calculated = link.PaginationCalculated,
            retry_count = link.RetryCount
        };
    }
}