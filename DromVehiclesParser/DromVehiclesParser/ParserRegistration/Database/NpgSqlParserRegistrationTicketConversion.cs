using System.Data;
using Dapper;
using DromVehiclesParser.ParserRegistration.Models;
using ParsingSDK.Parsing;

namespace DromVehiclesParser.ParserRegistration.Database;

public static class NpgSqlParserRegistrationTicketConversion
{
    extension(ParserRegistrationTicket ticket)
    {
        public object ExtractParameters() => new
        {
            id = ticket.Id,
            was_sent = ticket.WasSent,
            finished = ticket.Finished
        };
    }

    extension(NpgSqlParserRegistrationTicket? ticket)
    {
        public Maybe<ParserRegistrationTicket> MaybeTicket() =>
            ticket == null
                ? Maybe<ParserRegistrationTicket>.None()
                : Maybe<ParserRegistrationTicket>.Some(ticket.ToModel());
    }
    
    extension(NpgSqlParserRegistrationTicket ticket)
    {
        public ParserRegistrationTicket ToModel() => ParserRegistrationTicket.MapFrom
        (
            ticket,
            idMap: t => t.Id,
            wasSentMap: t => t.WasSent,
            finishedMap: t => t.Finished
        );
    }

    extension(ParserRegistrationTicketQuery query)
    {
        public (DynamicParameters parameters, string filterSql) WhereClause()
        {
            List<string> filters = [];
            DynamicParameters parameters = new();

            if (query.Id.HasValue)
            {
                filters.Add("id = @id");
                parameters.Add("@id", query.Id.Value, DbType.Guid);
            }

            if (query.FinishedOnly) filters.Add("finished is not null");
            if (query.SentOnly) filters.Add("was_sent is TRUE");
            
            return filters.Count == 0
                ? (parameters, string.Empty)
                : (parameters, "WHERE " + string.Join(" AND ", filters));
        }
    }
}