namespace DromVehiclesParser.ResultsExporing.TextFileExporting;

public interface IExporter<T> where T : class
{
    Task Export(T item, CancellationToken ct = default);
}