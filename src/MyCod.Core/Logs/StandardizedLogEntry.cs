namespace MyCod.Core.Logs;

public sealed record StandardizedLogEntry(
    string Date,
    string Time,
    string Level,
    string Caller,
    string Message)
{
    public string ToTabSeparatedLine() => string.Join('\t', Date, Time, Level, Caller, Message);
}
