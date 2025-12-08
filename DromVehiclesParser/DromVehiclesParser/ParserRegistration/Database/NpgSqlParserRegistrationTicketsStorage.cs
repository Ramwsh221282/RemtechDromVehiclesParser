using Dapper;
using DromVehiclesParser.ParserRegistration.Models;
using ParsingSDK.Parsing;
using RemTech.SharedKernel.Infrastructure.NpgSql;

namespace DromVehiclesParser.ParserRegistration.Database;

public sealed class NpgSqlParserRegistrationTicketsStorage(NpgSqlSession session)
{
    public async Task Save(ParserRegistrationTicket ticket, CancellationToken ct = default)
    {
        const string sql = """
                           INSERT INTO drom_vehicles_parser.registration_tickets
                           (id, was_sent, finished)
                           VALUES
                           (@id, @was_sent, @finished)
                           """;
        CommandDefinition command = session.FormCommand(sql, ticket.ExtractParameters(), ct);
        await session.Execute(command);
    }

    public async Task Delete(ParserRegistrationTicket ticket, CancellationToken ct = default)
    {
        const string sql = """
                           DELETE FROM drom_vehicles_parser.registration_tickets
                           WHERE id = @id;
                           """;
        CommandDefinition command = session.FormCommand(sql, ticket.ExtractParameters(), ct);
        await session.Execute(command);
    }

    public async Task<Maybe<ParserRegistrationTicket>> GetTicket(ParserRegistrationTicketQuery query, CancellationToken ct = default)
    {
        (DynamicParameters parameters, string filterSql) = query.WhereClause();
        string sql = $"""
                      SELECT id, was_sent, finished
                      FROM drom_vehicles_parser.registration_tickets
                      {filterSql}
                      LIMIT 1
                      """;
        CommandDefinition command = session.FormCommand(sql, parameters, ct);
        NpgSqlParserRegistrationTicket? ticket = await session.QueryMaybeRow<NpgSqlParserRegistrationTicket>(command);
        return ticket.MaybeTicket();
    }

    public async Task<IEnumerable<ParserRegistrationTicket>> GetTickets(ParserRegistrationTicketQuery query, CancellationToken ct = default)
    {
        (DynamicParameters parameters, string filterSql) = query.WhereClause();
        string sql = $"""
                      SELECT id, was_sent, finished
                      FROM drom_vehicles_parser.registration_tickets
                      {filterSql}
                      """;
        CommandDefinition command = session.FormCommand(sql, parameters, ct);
        IEnumerable<NpgSqlParserRegistrationTicket> tickets = await session.QueryMultipleRows<NpgSqlParserRegistrationTicket>(command);
        return tickets.Select(t => t.ToModel());
    }
}