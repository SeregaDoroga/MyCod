namespace MyCod.Core.Logs;

/// <summary>
/// Runtime settings for the log standardizer. The date format is configurable
/// because the task text says DD-MM-YYYY, while the examples show YYYY-MM-DD.
/// </summary>
public sealed record LogStandardizerOptions(string DateOutputFormat = "yyyy-MM-dd")
{
    public static LogStandardizerOptions Default { get; } = new();
}
