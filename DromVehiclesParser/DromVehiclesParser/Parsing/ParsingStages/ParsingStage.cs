namespace DromVehiclesParser.Parsing.ParsingStages;

public delegate Task ParsingStage(ParsingStageDependencies deps, CancellationToken ct);