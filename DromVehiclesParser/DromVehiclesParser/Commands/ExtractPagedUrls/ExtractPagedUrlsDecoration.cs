namespace DromVehiclesParser.Commands.ExtractPagedUrls;

public static class ExtractPagedUrlsDecoration
{
    extension(IExtractPagedUrlsCommand command)
    {
        public IExtractPagedUrlsCommand UseLogging(Serilog.ILogger logger)
        {
            return new ExtractPagedUrlsCommandLogging(logger, command);
        }
    }
}