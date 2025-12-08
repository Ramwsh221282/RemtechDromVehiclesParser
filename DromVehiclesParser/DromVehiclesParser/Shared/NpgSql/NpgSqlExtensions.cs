using System.Data;
using Dapper;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace DromVehiclesParser.Shared.NpgSql;

public static class NpgSqlExtensions
{
    extension(NpgSqlSession session)
    {
        public async Task<IEnumerable<T>> ReadManyUsingReader<T>(
            CommandDefinition command,
            Func<IDataReader, T> factory, 
            CancellationToken ct = default)
        {
            using IDataReader reader = await session.ExecuteReader(command, ct);
            List<T> items = [];
            while (reader.Read())
                items.Add(factory(reader));
            return items;
        }
    }

    extension(IDataReader reader)
    {
        public T GetProperty<T>(string columnName)
        {
            Type propertyType = typeof(T);
            object property = propertyType switch
            {
                _ when propertyType == typeof(long) => reader.GetLong(columnName),
                _ when propertyType == typeof(Guid) => reader.GetGuid(columnName),
                _ when propertyType == typeof(int) => reader.GetInt32(columnName),
                _ when propertyType == typeof(bool) => reader.GetBoolean(columnName),
                _ when propertyType == typeof(string) => reader.GetString(columnName),
                _ => throw new InvalidOperationException($"IDataReader does not support property type: {propertyType.Name} for reading.")
            };
            return (T)property;
        }

        public long GetLong(string columnName) => reader.GetInt64(reader.GetOrdinal(columnName));
        public Guid GetGuid(string columnName) => reader.GetGuid(reader.GetOrdinal(columnName));
        public int GetInt32(string columnName) => reader.GetInt32(reader.GetOrdinal(columnName));
        public bool GetBoolean(string columnName) => reader.GetBoolean(reader.GetOrdinal(columnName));
        public string GetString(string columnName) => reader.GetString(reader.GetOrdinal(columnName));
    }
}