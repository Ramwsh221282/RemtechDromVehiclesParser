namespace DromVehiclesParser.WorkStages.CatalogueStage.Models;

public static class WorkingCataloguePageImplementation
{
    extension(WorkingCataloguePage page)
    {
        public WorkingCataloguePage MarkItemsFetched()
        {
            if (page.CatalogueItemsFetched)
                throw new InvalidOperationException(
                    """
                    Catalogue items already fetched.
                    """
                );
            return page with { CatalogueItemsFetched = true };
        }

        public WorkingCataloguePage IncreaseRetryAmount()
        {
            int current = page.RetryCount;
            int next = current + 1;
            return page with { RetryCount = next };
        }
    }
}