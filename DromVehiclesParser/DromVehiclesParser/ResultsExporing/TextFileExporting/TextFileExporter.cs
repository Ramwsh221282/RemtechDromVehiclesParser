namespace DromVehiclesParser.ResultsExporing.TextFileExporting;

public sealed class TextFileExporter : IExporter<TextFile>
{
    public async Task Export(TextFile item, CancellationToken ct = default)
    {
        await using StreamWriter writer = File.CreateText(item.Path);
        await writer.WriteAsync(item.Content);
        await writer.FlushAsync();
    }
}