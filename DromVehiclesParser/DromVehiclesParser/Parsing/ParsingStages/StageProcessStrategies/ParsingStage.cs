namespace DromVehiclesParser.Parsing.ParsingStages.StageProcessStrategies;

public delegate Task ParsingStage(ParsingStageDependencies deps, CancellationToken ct);