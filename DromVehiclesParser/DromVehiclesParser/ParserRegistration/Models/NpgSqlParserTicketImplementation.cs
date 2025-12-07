namespace DromVehiclesParser.ParserRegistration.Models;

public static class NpgSqlParserTicketImplementation
{
    extension(ParserRegistrationTicket ticket)
    {
        public ParserRegistrationTicket Finished(DateTime finishDate)
        {
            return ticket with { Finished = finishDate };
        }
    }
}