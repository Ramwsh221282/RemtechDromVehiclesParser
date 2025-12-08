namespace DromVehiclesParser.WorkStages.CatalogueStage.Models;

public sealed record WorkingCataloguePage(Guid Id, string Url, bool CatalogueItemsFetched, int RetryCount);