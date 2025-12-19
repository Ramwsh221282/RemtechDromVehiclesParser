namespace DromVehiclesParser.Commands.ExtractAdvertisementsFromCatalogue;

public static class ExtractAdvertisementsFromCatalogueDecorating
{
    extension(IExtractAdvertisementsFromCatalogueCommand command)
    {
        public IExtractAdvertisementsFromCatalogueCommand UseLogging(Serilog.ILogger logger)
        {
            return new ExtractAdvertisementsFromCatalogueLogging(logger, command);
        }        
    }
}