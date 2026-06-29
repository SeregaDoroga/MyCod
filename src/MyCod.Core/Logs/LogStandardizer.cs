using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MyCod.Core.Logs;

/// <summary>
/// Converts input log records from the two task formats to one tab-separated
/// format. Invalid source lines are preserved verbatim in a separate problems
/// file so that no original data is silently lost.
/// </summary>
public static partial class LogStandardizer
{
    private const string DefaultCaller = "DEFAULT";
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    // Output files are written as UTF-8 without BOM. This is friendly to Linux
    // tools and still opens normally in modern Windows editors.
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Reads the input file line by line, writes valid records to
    /// <paramref name="outputPath"/>, and writes invalid original lines to
    /// <paramref name="problemsPath"/>.
    /// </summary>
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

        // detectEncodingFromByteOrderMarks keeps the tool tolerant to UTF-8 BOM
        // files produced by some Windows editors.
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
                // The task explicitly requires the original invalid record, not
                // an error message or a normalized representation.
                problemWriter.WriteLine(line);
                problems++;
            }
        }

        return new LogProcessingResult(total, written, problems);
    }

    /// <summary>
    /// Attempts to parse a single input line as either supported source format.
    /// The method is public to make the parser easy to test without filesystem IO.
    /// </summary>
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

        // Format 1 has no caller field, therefore the output must contain
        // DEFAULT in the caller column.
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

        // Format 2 contains a caller between the thread id and message columns.
        // An empty caller means the line is malformed and should go to problems.
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

    // Format 1:
    // 10.03.2025 15:14:49.523 INFORMATION Message
    [GeneratedRegex(
        @"^(?<date>\d{2}\.\d{2}\.\d{4})\s+(?<time>\d{2}:\d{2}:\d{2}(?:\.\d+)?)\s+(?<level>[A-Za-z]+)\s+(?<message>.*)$",
        RegexOptions.CultureInvariant)]
    private static partial Regex Format1Regex();

    // Format 2:
    // 2025-03-10 15:14:51.5882| INFO|11|Caller.Method| Message
    [GeneratedRegex(
        @"^(?<date>\d{4}-\d{2}-\d{2})\s+(?<time>\d{2}:\d{2}:\d{2}(?:\.\d+)?)\|\s*(?<level>[A-Za-z]+)\s*\|\s*(?<thread>\d+)\s*\|\s*(?<caller>[^|]+)\|\s?(?<message>.*)$",
        RegexOptions.CultureInvariant)]
    private static partial Regex Format2Regex();
}
