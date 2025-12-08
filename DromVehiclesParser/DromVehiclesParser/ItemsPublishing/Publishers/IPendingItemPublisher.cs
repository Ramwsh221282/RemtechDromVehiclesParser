using DromVehiclesParser.ItemsPublishing.Models;

namespace DromVehiclesParser.ItemsPublishing.Publishers;

public interface IPendingItemPublisher
{
    public Task Publish(DromPendingItem item, CancellationToken ct = default);
}