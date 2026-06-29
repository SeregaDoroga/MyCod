using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MyCod.Core.Logs;

public static partial class LogStandardizer
{
    private const string DefaultCaller = "DEFAULT";
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    public static LogProcessingResult ConvertFile(
        string inputPath,
        string outputPath,
        string problemsPath,
        LogStandardizerOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(problemsPath);

        options ??= LogStandardizerOptions.Default;
        EnsureParentDirectory(outputPath);
        EnsureParentDirectory(problemsPath);

        var total = 0;
        var written = 0;
        var problems = 0;

        using var reader = new StreamReader(inputPath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        using var outputWriter = new StreamWriter(outputPath, append: false, Utf8WithoutBom);
        using var problemWriter = new StreamWriter(problemsPath, append: false, Utf8WithoutBom);

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            total++;
            if (TryStandardizeLine(line, options.DateOutputFormat, out var entry))
            {
                outputWriter.WriteLine(entry.ToTabSeparatedLine());
                written++;
            }
            else
            {
                problemWriter.WriteLine(line);
                problems++;
            }
        }

        return new LogProcessingResult(total, written, problems);
    }

    public static bool TryStandardizeLine(
        string line,
        string dateOutputFormat,
        out StandardizedLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentException.ThrowIfNullOrWhiteSpace(dateOutputFormat);

        return TryParseFormat1(line, dateOutputFormat, out entry)
            || TryParseFormat2(line, dateOutputFormat, out entry);
    }

    private static bool TryParseFormat1(
        string line,
        string dateOutputFormat,
        out StandardizedLogEntry entry)
    {
        var match = Format1Regex().Match(line);
        if (!match.Success)
        {
            entry = default!;
            return false;
        }

        if (!TryFormatDate(match.Groups["date"].Value, "dd.MM.yyyy", dateOutputFormat, out var date)
            || !TryNormalizeLevel(match.Groups["level"].Value, out var level))
        {
            entry = default!;
            return false;
        }

        entry = new StandardizedLogEntry(
            date,
            match.Groups["time"].Value,
            level,
            DefaultCaller,
            match.Groups["message"].Value);
        return true;
    }

    private static bool TryParseFormat2(
        string line,
        string dateOutputFormat,
        out StandardizedLogEntry entry)
    {
        var match = Format2Regex().Match(line);
        if (!match.Success)
        {
            entry = default!;
            return false;
        }

        if (!TryFormatDate(match.Groups["date"].Value, "yyyy-MM-dd", dateOutputFormat, out var date)
            || !TryNormalizeLevel(match.Groups["level"].Value, out var level))
        {
            entry = default!;
            return false;
        }

        var caller = match.Groups["caller"].Value.Trim();
        if (caller.Length == 0)
        {
            entry = default!;
            return false;
        }

        entry = new StandardizedLogEntry(
            date,
            match.Groups["time"].Value,
            level,
            caller,
            match.Groups["message"].Value);
        return true;
    }

    private static bool TryFormatDate(
        string value,
        string inputFormat,
        string outputFormat,
        out string formattedDate)
    {
        if (DateTime.TryParseExact(value, inputFormat, Culture, DateTimeStyles.None, out var parsed))
        {
            formattedDate = parsed.ToString(outputFormat, Culture);
            return true;
        }

        formattedDate = string.Empty;
        return false;
    }

    private static bool TryNormalizeLevel(string value, out string normalized)
    {
        normalized = value.Trim().ToUpperInvariant() switch
        {
            "INFORMATION" or "INFO" => "INFO",
            "WARNING" or "WARN" => "WARN",
            "ERROR" => "ERROR",
            "DEBUG" => "DEBUG",
            _ => string.Empty
        };

        return normalized.Length > 0;
    }

    private static void EnsureParentDirectory(string path)
    {
        var parent = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(parent))
        {
            Directory.CreateDirectory(parent);
        }
    }

    [GeneratedRegex(
        @"^(?<date>\d{2}\.\d{2}\.\d{4})\s+(?<time>\d{2}:\d{2}:\d{2}(?:\.\d+)?)\s+(?<level>[A-Za-z]+)\s+(?<message>.*)$",
        RegexOptions.CultureInvariant)]
    private static partial Regex Format1Regex();

    [GeneratedRegex(
        @"^(?<date>\d{4}-\d{2}-\d{2})\s+(?<time>\d{2}:\d{2}:\d{2}(?:\.\d+)?)\|\s*(?<level>[A-Za-z]+)\s*\|\s*(?<thread>\d+)\s*\|\s*(?<caller>[^|]+)\|\s?(?<message>.*)$",
        RegexOptions.CultureInvariant)]
    private static partial Regex Format2Regex();
}
