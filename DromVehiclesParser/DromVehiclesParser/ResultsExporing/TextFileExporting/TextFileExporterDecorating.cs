namespace DromVehiclesParser.ResultsExporing.TextFileExporting;

public static class TextFileExporterDecorating
{
    extension(IExporter<TextFile> exporter)
    {
        public IExporter<TextFile> UseLogging(Serilog.ILogger logger)
        {
            return new TextFileExporterLogging(logger, exporter);
        }
    }
}