namespace MyCod.Core.Logs;

public readonly record struct LogProcessingResult(int TotalLines, int WrittenLines, int ProblemLines);
