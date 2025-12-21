using System.Data;
using System.Text.Json;
using Dapper;
using DromVehiclesParser.Parsing.CatalogueParsing.Models;
using DromVehiclesParser.Shared.NpgSql;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace DromVehiclesParser.Parsing.CatalogueParsing.Extensions;

public static class DromCatalogueAdvertisementStoringImplementation
{
    extension(DromCatalogueAdvertisement)
    {
        public static async Task<DromCatalogueAdvertisement[]> GetMany(NpgSqlSession session, DromCatalogueAdvertisementQuery query, CancellationToken ct)
        {
            (DynamicParameters parameters, string filterSql) = query.WhereClause();
            string lockClause = query.LockClause();
            string limitClause = query.LimitClause();
            string sql = $@"
                SELECT 
                    id as id, 
                    url as url, 
                    photos as photos, 
                    processed as processed, 
                    retry_count as retry_count 
                FROM drom_vehicles_parser.items
                {filterSql}
                {lockClause}
                {limitClause}
                ";

            CommandDefinition command = session.FormCommand(sql, parameters, ct);
            using IDataReader reader = await session.ExecuteReader(command, ct);
            List<DromCatalogueAdvertisement> advertisements = [];
            while (reader.Read())
                advertisements.Add(new(
                    reader.GetProperty<string>("id"),
                    reader.GetProperty<string>("url"),
                    JsonSerializer.Deserialize<string[]>(reader.GetProperty<string>("photos"))!.ToList(),
                    reader.GetProperty<bool>("processed"),
                    reader.GetProperty<int>("retry_count")
                ));
            return advertisements.ToArray();
        }
    }
    
    extension(IEnumerable<DromCatalogueAdvertisement> advertisements)
    {
        public async Task PersistMany(NpgSqlSession session)
        {
            const string sql = """
                               INSERT INTO drom_vehicles_parser.items (id, url, photos, processed, retry_count)
                               VALUES (@id, @url, @photos::jsonb, @processed, @retry_count)
                               ON CONFLICT (id) DO NOTHING
                               """;
            IEnumerable<object> parameters = advertisements.Select(ad => ad.ExtractParameters());
            await session.ExecuteBulk(sql, parameters);
        }
    }
    
    extension(DromCatalogueAdvertisement advertisement)
    {
        public async Task Remove(NpgSqlSession session)
        {
            const string sql = "DELETE FROM drom_vehicles_parser.items WHERE id = @id";
            object parameter = new { id = advertisement.Id };
            CommandDefinition command = new(sql, parameter, transaction: session.Transaction);
            await session.Execute(command); 
        }

        public async Task Update(NpgSqlSession session)
        {
            const string sql = """
                               UPDATE drom_vehicles_parser.items
                               SET processed = @processed, retry_count = @retry_count
                               WHERE id = @id;
                               """;
            object parameter = new { id = advertisement.Id, processed = advertisement.Processed, retry_count = advertisement.RetryCount };
            CommandDefinition command = new(sql, parameter, transaction: session.Transaction);
            await session.Execute(command);
        }
        
        private object ExtractParameters() => new
        {
            id = advertisement.Id,
            url = advertisement.Url,
            photos = JsonSerializer.Serialize(advertisement.Photos),    
            processed = advertisement.Processed,
            retry_count = advertisement.RetryCount
        };
    }

    extension(DromCatalogueAdvertisementQuery query)
    {
        private (DynamicParameters parameters, string filterSql) WhereClause()
        {
            DynamicParameters parameters = new();
            List<string> conditions = new();
            
            conditions.Add("price is null");
            conditions.Add("is_nds is null");
            conditions.Add("title is null");
            conditions.Add("address is null");
            conditions.Add("characteristics is null");
            
            if (query.UnprocessedOnly) conditions.Add("processed is false");

            if (query.RetryLimit.HasValue)
            {
                conditions.Add("retry_count <= @retry_limit");
                parameters.Add("retry_limit", query.RetryLimit.Value);
            }
            
            string filterSql = conditions.Count > 0 
                ? "WHERE " + string.Join(" AND ", conditions)
                : string.Empty;

            return (parameters, filterSql);
        }

        private string LimitClause()
        {
            return query.Limit.HasValue 
                ? $"LIMIT {query.Limit.Value}" 
                : string.Empty;
        }

        private string LockClause()
        {
            return query.WithLock 
                ? "FOR UPDATE" 
                : string.Empty;
        }
    }
}