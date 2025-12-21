namespace DromVehiclesParser.ResultsExporing.TextFileExporting;

public sealed class TextFileExporterLogging(Serilog.ILogger logger, IExporter<TextFile> exporter) : IExporter<TextFile>
{
    private Serilog.ILogger Logger { get; } = logger.ForContext<IExporter<TextFile>>();
    
    public async Task Export(TextFile item, CancellationToken ct = default)
    {
        Logger.Information("Exporting text file to {Path}", item.Path);

        try
        {
            await exporter.Export(item, ct);
        }
        catch(Exception e)
        {
            Logger.Fatal(e, "Failed to export text file to {Path}", item.Path);
            throw;
        }
    }
}