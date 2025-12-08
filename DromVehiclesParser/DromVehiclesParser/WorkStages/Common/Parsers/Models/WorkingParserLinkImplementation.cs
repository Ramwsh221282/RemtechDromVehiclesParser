namespace DromVehiclesParser.WorkStages.Common.Parsers.Models;

public static class WorkingParserLinkImplementation
{
    extension(WorkingParserLink link)
    {
        public WorkingParserLink MarkPaginationCalculated()
        {
            if (link.PaginationCalculated)
                throw new InvalidOperationException(
                    """
                    Parser link has already pagination calculated.
                    """
                );
            return link with { PaginationCalculated = true };
        }

        public WorkingParserLink PaginationUncalculated()
        {
            return link with { PaginationCalculated = false };
        }

        public WorkingParserLink IncreaseRetryCount()
        {
            int current = link.RetryCount;
            int next = current + 1;
            return link with { RetryCount = next };
        }
    }
}