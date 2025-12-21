using System.Data;
using System.Text.Json;
using Dapper;
using DromVehiclesParser.Parsing.ConcreteItemParsing.Models;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace DromVehiclesParser.Parsing.ConcreteItemParsing.Extensions;

public static class DromAdvertisementFromPageStoringImplementation
{
    extension(DromAdvertisementFromPage)
    {
        public static async Task<DromAdvertisementFromPage[]> GetMany(NpgSqlSession session, QueryDromAdvertisements query, CancellationToken ct)
        {
            string limit = LimitClause(query);
            string lockClause = LockClause(query);
            string sql = $@"
                               SELECT 
                                   id as id, 
                                   url as url, 
                                   photos as photos, 
                                   characteristics as characteristics, 
                                   price as price, 
                                   is_nds as is_nds, 
                                   title as title, 
                                   address as address 
                               FROM drom_vehicles_parser.items    
                               WHERE price is not null 
                                 AND is_nds is not null 
                                 AND title is not null 
                                 AND address is not null 
                                 AND characteristics is not null
                               {lockClause}
                               {limit}                               
                               ";
            
            CommandDefinition command = new(sql, transaction: session.Transaction, cancellationToken: ct);
            using IDataReader reader = await session.ExecuteReader(command, ct);
            List<DromAdvertisementFromPage> advertisements = [];
            
            while (reader.Read())
            {
                string id = reader.GetString(reader.GetOrdinal("id"));
                string url = reader.GetString(reader.GetOrdinal("url"));
                string[] photos = JsonSerializer.Deserialize<string[]>(reader.GetString(reader.GetOrdinal("photos")))!;
                long price = reader.GetInt64(reader.GetOrdinal("price"));
                bool isNds = reader.GetBoolean(reader.GetOrdinal("is_nds"));
                string title = reader.GetString(reader.GetOrdinal("title"));
                string address = reader.GetString(reader.GetOrdinal("address"));
                string characteristicsJson = reader.GetString(reader.GetOrdinal("characteristics"))!;
                Dictionary<string, string> ctx = JsonSerializer.Deserialize<Dictionary<string, string>>(characteristicsJson)!;
                advertisements.Add(new(
                    Id: id,
                    Url: url,
                    Photos: photos.ToList(),
                    Characteristics: ctx,
                    Price: price,
                    IsNds: isNds,
                    Title: title,
                    Address: address
                ));
            }
            return advertisements.ToArray();
        }
    }
    
    extension(IEnumerable<DromAdvertisementFromPage> advertisements)
    {
        public async Task PersistMany(NpgSqlSession session)
        {
            const string sql = """
                               UPDATE drom_vehicles_parser.items
                               SET characteristics = @characteristics::jsonb,
                                   price = @price,
                                   is_nds = @is_nds,
                                   title = @title,
                                   address = @address
                               WHERE id = @id; 
                               """;
            IEnumerable<object> parameters = advertisements.Select(ad => ad.ExtractParameters());
            await session.ExecuteBulk(sql, parameters);
        }

        public async Task RemoveMany(NpgSqlSession session, CancellationToken ct = default)
        {
            const string sql = """
                               DELETE FROM drom_vehicles_parser.items
                               WHERE id = ANY(@ids)
                               """;
            
            object parameter = new { ids = advertisements.Select(ad => ad.Id).ToArray() };
            CommandDefinition command = new(sql, parameter, cancellationToken: ct, transaction: session.Transaction);
            await session.Execute(command);
        }
    }
    
    extension(DromAdvertisementFromPage advertisement)
    {
        private object ExtractParameters() => new
        {
            id = advertisement.Id,
            characteristics = JsonSerializer.Serialize(advertisement.Characteristics),
            price = advertisement.Price,
            is_nds = advertisement.IsNds,
            title = advertisement.Title,
            address = advertisement.Address
        };
    }

    extension(QueryDromAdvertisements query)
    {
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