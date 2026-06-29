namespace MyCod.Core.Logs;

public sealed record LogStandardizerOptions(string DateOutputFormat = "yyyy-MM-dd")
{
    public static LogStandardizerOptions Default { get; } = new();
}
