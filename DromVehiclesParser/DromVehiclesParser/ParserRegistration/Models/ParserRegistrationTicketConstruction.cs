namespace DromVehiclesParser.ParserRegistration.Models;

public static class ParserRegistrationTicketConstruction
{
    extension(ParserRegistrationTicket)
    {
        public static ParserRegistrationTicket NewlySent()
        {
            ParserRegistrationTicket ticket = New();
            return ticket with { WasSent = true };
        }
        
        public static ParserRegistrationTicket New()
        {
            return new ParserRegistrationTicket(Id: Guid.NewGuid(), WasSent: false, Finished: null);
        }

        public static ParserRegistrationTicket MapFrom<T>(
            T source,
            Func<T, Guid> idMap,
            Func<T, bool> wasSentMap,
            Func<T, DateTime?> finishedMap)
        {
            return new ParserRegistrationTicket(
                Id: idMap(source),
                WasSent: wasSentMap(source),
                Finished: finishedMap(source)
            );
        }
    }
}