namespace DromVehiclesParser.Commands.ExtractAdvertisementFromItsPage;

public static class ExtractAdvertisementFromItsPageDecorating
{
    extension(IExtractAdvertisementFromItsPageCommand command)
    {
        public IExtractAdvertisementFromItsPageCommand UseLogging(Serilog.ILogger logger)
        {
            return new ExtractAdvertisementFromItsPageLogging(command, logger);
        }
    }
}