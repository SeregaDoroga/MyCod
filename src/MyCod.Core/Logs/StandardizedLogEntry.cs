namespace MyCod.Core.Logs;

/// <summary>
/// In-memory representation of one valid output record. Keeping fields as a
/// record makes the parser easy to test before the line is serialized to text.
/// </summary>
public sealed record StandardizedLogEntry(
    string Date,
    string Time,
    string Level,
    string Caller,
    string Message)
{
    /// <summary>
    /// Serializes the entry exactly as required by the task: fields are separated
    /// by tab characters, and the message is not additionally escaped or trimmed.
    /// </summary>
    public string ToTabSeparatedLine() => string.Join('\t', Date, Time, Level, Caller, Message);
}
