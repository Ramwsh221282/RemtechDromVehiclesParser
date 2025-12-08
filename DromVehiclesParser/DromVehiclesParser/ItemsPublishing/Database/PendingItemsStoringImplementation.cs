using System.Text.Json;
using Dapper;
using DromVehiclesParser.ItemsPublishing.Models;
using DromVehiclesParser.Shared.NpgSql;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace DromVehiclesParser.ItemsPublishing.Database;

public static class PendingItemsStoringImplementation
{
    public sealed record PendingItemsQuery(int? Limit = null, bool WithLock = false);

    extension(PendingItemsQuery query)
    {
        private string LimitClause() => query.Limit.HasValue ? $"LIMIT {query.Limit.Value}" : string.Empty;
        private string LockClause() => query.WithLock ? "FOR UPDATE" : string.Empty;
    }
    
    extension(DromPendingItem item)
    {
        private object ExtractParameters() => new
        {
            id = item.Id,
            url = item.Url,
            price = item.Price,
            is_nds = item.IsNds,
            address = item.Address,
            title = item.Title,
            photos = JsonSerializer.Serialize(item.Photos),
            description_list = JsonSerializer.Serialize(item.DescriptionList),
            characteristics = JsonSerializer.Serialize(item.Characteristics)
        };
    }

    extension(IEnumerable<DromPendingItem>)
    {
        public static async Task<IEnumerable<DromPendingItem>> GetMany(
            NpgSqlSession session, 
            PendingItemsQuery query, 
            CancellationToken ct = default)
        {
            string limitClause = query.LimitClause();
            string lockClause = query.LockClause();
            string sql = $"""
                         SELECT 
                         id as id, 
                         url as url, 
                         price as price, 
                         is_nds as is_nds, 
                         address as address, 
                         title as title, 
                         photos as photos, 
                         description_list as description_list, 
                         characteristics as characteristics
                         FROM drom_vehicles_parser.pending_items
                         {lockClause}
                         {limitClause}
                         """;
            CommandDefinition command = new(sql, cancellationToken: ct, transaction: session.Transaction);
            return await session.ReadManyUsingReader(
                command,
                r =>
                {
                    return new DromPendingItem(
                        Id: r.GetProperty<string>("id"),
                        Url: r.GetProperty<string>("url"),
                        Price: r.GetProperty<long>("price"),
                        IsNds: r.GetProperty<bool>("is_nds"),
                        Address: r.GetProperty<string>("address"),
                        Title: r.GetProperty<string>("title"),
                        Photos: r.GetProperty<string>("photos").PlainJsonArrayToStringList(),
                        DescriptionList: r.GetProperty<string>("description_list").PlainJsonArrayToStringList(),
                        Characteristics: r.GetProperty<string>("characteristics").PlainJsonArrayToStringList()
                        );
                },
                ct);
        }
    }

    extension(string input)
    {
        private IReadOnlyList<string> PlainJsonArrayToStringList()
        {
            using JsonDocument document = JsonDocument.Parse(input);
            int length = document.RootElement.GetArrayLength();
            List<string> result = new(length);
            foreach (JsonElement item in document.RootElement.EnumerateArray())
                result.Add(item.GetString()!);
            return result;
        }
    }
    
    extension(IEnumerable<DromPendingItem> items)
    {
        public async Task SaveMany(NpgSqlSession session)
        {
            const string sql = """
                               INSERT INTO drom_vehicles_parser.pending_items
                               (id, url, price, is_nds, address, title, photos, description_list, characteristics)
                               VALUES
                               (@id, @url, @price, @is_nds, @address, @title, @photos::jsonb, @description_list::jsonb, @characteristics::jsonb)
                               ON CONFLICT (id) DO NOTHING
                               """;
            IEnumerable<object> parameters = items.Select(i => i.ExtractParameters());
            await session.ExecuteBulk(sql, parameters);
        }
        
        public async Task DeleteMany(NpgSqlSession session)
        {
            const string sql = "DELETE FROM drom_vehicles_parser.pending_items WHERE id = @id;";
            IEnumerable<object> parameters = items.Select(i => i.ExtractParameters());
            await session.ExecuteBulk(sql, parameters);
        }
    }
}