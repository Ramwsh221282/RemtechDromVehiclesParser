namespace DromVehiclesParser.Commands.HoverCatalogueImages;

public static class HoverAdvertisementsCatalogueImagesDecorating
{
    extension(IHoverAdvertisementsCatalogueImagesCommand command)
    {
        public IHoverAdvertisementsCatalogueImagesCommand UseLogging(Serilog.ILogger logger)
        {
            return new HoverAdvertisementsCatalogueImagesLogging(logger, command);
        }
    }
}