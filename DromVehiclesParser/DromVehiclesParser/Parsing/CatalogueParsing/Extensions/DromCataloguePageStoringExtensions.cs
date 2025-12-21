using System.Data;
using Dapper;
using DromVehiclesParser.Parsing.CatalogueParsing.Models;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace DromVehiclesParser.Parsing.CatalogueParsing.Extensions;

public static class DromCataloguePageStoringExtensions
{
    extension(DromCataloguePage)
    {
        public static async Task<DromCataloguePage[]> GetMany(NpgSqlSession session, DromCataloguePageQuery query, CancellationToken ct)
        {
            (DynamicParameters parameters, string filterSql) = query.WhereClause();
            string lockClause = query.LockClause();
            string sql = $@"
                SELECT 
                    url as url, 
                    processed as processed, 
                    retry_count as retry_count
                FROM drom_vehicles_parser.catalogue_pages
                {filterSql}
                {lockClause}
            ";

            CommandDefinition command = session.FormCommand(sql, parameters, ct);
            using IDataReader reader = await session.ExecuteReader(command, ct);
            List<DromCataloguePage> pages = new List<DromCataloguePage>();
            
            while (reader.Read())
            {
                string url = reader.GetString(reader.GetOrdinal("url"));
                bool processed = reader.GetBoolean(reader.GetOrdinal("processed"));
                int retryCount = reader.GetInt32(reader.GetOrdinal("retry_count"));
                pages.Add(new DromCataloguePage(url, retryCount, processed));
            }
            
            return pages.ToArray();
        }
    }
    
    extension(IEnumerable<DromCataloguePage> pages)
    {
        public async Task PersistMany(NpgSqlSession session)
        {
            const string sql = @"
            INSERT INTO drom_vehicles_parser.catalogue_pages (url, processed, retry_count)
            VALUES (@url, @processed, @retry_count)
            ON CONFLICT (url) DO NOTHING";
            IEnumerable<object> parameters = pages.Select(p => p.ExtractParameters());
            await session.ExecuteBulk(sql, parameters);
        }

        public async Task UpdateMany(NpgSqlSession session)
        {
            const string sql = @"
            UPDATE drom_vehicles_parser.catalogue_pages
            SET processed = @processed,
                retry_count = @retry_count
            WHERE url = @url";

            IEnumerable<object> parameters = pages.Select(p => p.ExtractParameters()); 
            await session.ExecuteBulk(sql, parameters);
        }
    }

    extension(DromCataloguePage page)
    {
        public object ExtractParameters()
        {
            return new
            {
                url = page.Url,
                processed = page.Processed,
                retry_count = page.RetryCount
            };
        }
    }

    extension(DromCataloguePageQuery query)
    {
        private (DynamicParameters parameters, string filterSql) WhereClause()
        {
            DynamicParameters parameters = new DynamicParameters();
            List<string> conditions = new List<string>();

            if (query.UnprocessedOnly)
            {
                conditions.Add("processed = @processed");
                parameters.Add("processed", false);
            }

            if (query.RetryLimit > 0)
            {
                conditions.Add("retry_count < @retryLimit");
                parameters.Add("retryLimit", query.RetryLimit);
            }

            string filterSql = conditions.Count > 0 
                ? "WHERE " + string.Join(" AND ", conditions)
                : string.Empty;

            return (parameters, filterSql);
        }

        private string LockClause()
        {
            return query.WithLock ? "FOR UPDATE SKIP LOCKED" : string.Empty;
        }
    }
}