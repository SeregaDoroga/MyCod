namespace MyCod.Core.Logs;

/// <summary>
/// Summary of one log conversion run. It is returned by the core logic and
/// printed by the console app so the user can immediately see how many records
/// were accepted and how many were sent to the problems file.
/// </summary>
public readonly record struct LogProcessingResult(int TotalLines, int WrittenLines, int ProblemLines);
