using System.Data;
using Dapper;
using DromVehiclesParser.Parsers.Models;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace DromVehiclesParser.Parsers.Database;

public static class WorkingParserLinkStoringImplementation
{
    extension(WorkingParserLink)
    {
        public static async Task<WorkingParserLink[]> GetMany(NpgSqlSession session, WorkingParserLinkQuery query, CancellationToken ct = default)
        {
            (DynamicParameters parameters, string filterSql) = query.WhereClause();
            string lockClause = query.LockClause();
            
            string sql = $"""
                          SELECT
                          id as id,
                          url as url,
                          processed as processed,
                          retry_count as retry_count
                          FROM drom_vehicles_parser.working_parser_links
                          {filterSql}
                          {lockClause}
                          """;
            
            CommandDefinition command = session.FormCommand(sql, parameters, ct);
            using IDataReader reader = await session.ExecuteReader(command, ct);
            List<WorkingParserLink> links = [];
            
            while (reader.Read())
            {
                Guid id = reader.GetGuid(reader.GetOrdinal("id"));
                string url = reader.GetString(reader.GetOrdinal("url"));
                bool processed = reader.GetBoolean(reader.GetOrdinal("processed"));
                int retryCount = reader.GetInt32(reader.GetOrdinal("retry_count"));

                WorkingParserLink link = new WorkingParserLink(
                    Id: id,
                    Url: url,
                    Processed: processed, 
                    RetryCount: retryCount
                );

                links.Add(link);
            }

            return links.ToArray();
        }
    }
    
    extension(IEnumerable<WorkingParserLink> links)
    {
        public async Task PersistMany(NpgSqlSession session)
        {
            const string sql = """
                               INSERT INTO drom_vehicles_parser.working_parser_links
                               (id, url, processed, retry_count)
                               VALUES
                               (@id, @url, @processed, @retry_count)
                               ON CONFLICT (id) DO NOTHING
                               """;
            IEnumerable<object> parameters = links.Select(l => l.ExtractParameters());
            await session.ExecuteBulk(sql, parameters);
        }

        public async Task UpdateMany(NpgSqlSession session)
        {
            const string sql = """
                               UPDATE drom_vehicles_parser.working_parser_links
                               SET processed = @processed, retry_count = @retry_count
                               WHERE id = @id;
                               """;
            IEnumerable<object> parameters = links.Select(l => l.ExtractParameters());
            await session.ExecuteBulk(sql, parameters);
        }
    }

    extension(WorkingParserLink link)
    {
        private object ExtractParameters() => new
        {
            id = link.Id,
            url = link.Url,
            processed = link.Processed,
            retry_count = link.RetryCount
        };
    }

    extension(WorkingParserLinkQuery query)
    {
        private string LockClause()
        {
            return query.WithLock ? "FOR UPDATE" : string.Empty;
        }
        
        private (DynamicParameters parameters, string filterSql) WhereClause()
        {
            List<string> filters = [];
            DynamicParameters parameters = new();
            
            if (query.UnprocessedOnly)
            {
                filters.Add("processed is false");
            }

            if (query.RetryLimit.HasValue)
            {
                filters.Add("retry_count < @retryLimit");
                parameters.Add("@retryLimit", query.RetryLimit.Value, DbType.Int32);
            }

            return filters.Count == 0
                ? (parameters, string.Empty)
                : (parameters, "WHERE " + string.Join(" AND ", filters));
        }
    }
}