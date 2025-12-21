namespace DromVehiclesParser.Parsers.Models;

public static class WorkingParserLinkImplementation
{
    extension(WorkingParserLink link)
    {
        public WorkingParserLink MarkProcessed()
        {
            if (link.Processed)
                throw new InvalidOperationException(
                    """
                    Parser link has already been processed.
                    """
                );
            return link with { Processed = true };
        }

        public WorkingParserLink IncreaseRetryCount()
        {
            int current = link.RetryCount;
            int next = current + 1;
            return link with { RetryCount = next };
        }
    }
}