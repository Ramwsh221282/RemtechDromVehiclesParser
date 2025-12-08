namespace DromVehiclesParser.WorkStages.Common.Parsers.Models;

public sealed record WorkingParser(Guid Id, string Domain, string Type, IReadOnlyList<WorkingParserLink> Links);