using System.Text;
using MyCod.Core.Compression;
using MyCod.Core.ConcurrentServer;
using MyCod.Core.Logs;

namespace MyCod.Tests;

internal static class Program
{
    private static int _passed;
    private static int _failed;

    private static int Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        Run("Task1 compresses sample string", Task1CompressesSampleString);
        Run("Task1 decompresses sample string", Task1DecompressesSampleString);
        Run("Task1 handles multi-digit groups", Task1HandlesMultiDigitGroups);
        Run("Task1 rejects invalid input", Task1RejectsInvalidInput);
        Run("Task2 accumulates count under concurrency", Task2AccumulatesCountUnderConcurrency);
        Run("Task3 parses supported formats", Task3ParsesSupportedFormats);
        Run("Task3 writes invalid records to problems file", Task3WritesInvalidRecordsToProblemsFile);
        Run("Task3 supports configurable date output", Task3SupportsConfigurableDateOutput);

        Console.WriteLine();
        Console.WriteLine($"Passed: {_passed}");
        Console.WriteLine($"Failed: {_failed}");

        return _failed == 0 ? 0 : 1;
    }

    private static void Task1CompressesSampleString()
    {
        AssertEqual("a3b2c3d2e", RunLengthCodec.Compress("aaabbcccdde"));
        AssertEqual("abc", RunLengthCodec.Compress("abc"));
        AssertEqual(string.Empty, RunLengthCodec.Compress(string.Empty));
    }

    private static void Task1DecompressesSampleString()
    {
        AssertEqual("aaabbcccdde", RunLengthCodec.Decompress("a3b2c3d2e"));
        AssertEqual("abc", RunLengthCodec.Decompress("abc"));
        AssertEqual(string.Empty, RunLengthCodec.Decompress(string.Empty));
    }

    private static void Task1HandlesMultiDigitGroups()
    {
        var source = new string('a', 12) + "b" + new string('z', 10);
        var compressed = RunLengthCodec.Compress(source);

        AssertEqual("a12bz10", compressed);
        AssertEqual(source, RunLengthCodec.Decompress(compressed));
    }

    private static void Task1RejectsInvalidInput()
    {
        AssertThrows<FormatException>(() => RunLengthCodec.Compress("abc1"));
        AssertThrows<FormatException>(() => RunLengthCodec.Decompress("1abc"));
        AssertThrows<FormatException>(() => RunLengthCodec.Decompress("a0"));
        AssertThrows<FormatException>(() => RunLengthCodec.Decompress("a01"));
    }

    private static void Task2AccumulatesCountUnderConcurrency()
    {
        Server.ResetForTests();

        const int writers = 8;
        const int readers = 16;
        const int iterations = 5_000;

        using var start = new ManualResetEventSlim(initialState: false);
        var tasks = new List<Task>(writers + readers);

        for (var writer = 0; writer < writers; writer++)
        {
            tasks.Add(Task.Run(() =>
            {
                start.Wait();
                for (var i = 0; i < iterations; i++)
                {
                    Server.AddToCount(1);
                }
            }));
        }

        for (var reader = 0; reader < readers; reader++)
        {
            tasks.Add(Task.Run(() =>
            {
                start.Wait();
                var previous = 0;
                for (var i = 0; i < iterations; i++)
                {
                    var current = Server.GetCount();
                    if (current < previous)
                    {
                        throw new InvalidOperationException("Count must not go backwards.");
                    }

                    previous = current;
                }
            }));
        }

        start.Set();
        Task.WaitAll(tasks.ToArray());

        AssertEqual(writers * iterations, Server.GetCount());
    }

    private static void Task3ParsesSupportedFormats()
    {
        AssertTrue(LogStandardizer.TryStandardizeLine(
            "10.03.2025 15:14:49.523 INFORMATION Версия программы: '3.4.0.48729'",
            LogStandardizerOptions.Default.DateOutputFormat,
            out var format1));

        AssertEqual(
            "2025-03-10\t15:14:49.523\tINFO\tDEFAULT\tВерсия программы: '3.4.0.48729'",
            format1.ToTabSeparatedLine());

        AssertTrue(LogStandardizer.TryStandardizeLine(
            "2025-03-10 15:14:51.5882| INFO|11|MobileComputer.GetDeviceId| Код устройства: '@MINDEO-M40-D-410244015546'",
            LogStandardizerOptions.Default.DateOutputFormat,
            out var format2));

        AssertEqual(
            "2025-03-10\t15:14:51.5882\tINFO\tMobileComputer.GetDeviceId\tКод устройства: '@MINDEO-M40-D-410244015546'",
            format2.ToTabSeparatedLine());

        AssertTrue(LogStandardizer.TryStandardizeLine(
            "10.03.2025 16:00:00 WARNING Осторожно",
            LogStandardizerOptions.Default.DateOutputFormat,
            out var warning));

        AssertEqual("WARN", warning.Level);
    }

    private static void Task3WritesInvalidRecordsToProblemsFile()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var inputPath = Path.Combine(directory, "input.log");
            var outputPath = Path.Combine(directory, "standardized.log");
            var problemsPath = Path.Combine(directory, "problems.txt");

            File.WriteAllLines(inputPath, new[]
            {
                "10.03.2025 15:14:49.523 INFORMATION Версия программы: '3.4.0.48729'",
                "not a log record",
                "2025-03-10 15:14:51.5882| INFO|11|MobileComputer.GetDeviceId| Код устройства: '@MINDEO-M40-D-410244015546'"
            });

            var result = LogStandardizer.ConvertFile(inputPath, outputPath, problemsPath);

            AssertEqual(new LogProcessingResult(3, 2, 1), result);
            AssertSequenceEqual(new[]
            {
                "2025-03-10\t15:14:49.523\tINFO\tDEFAULT\tВерсия программы: '3.4.0.48729'",
                "2025-03-10\t15:14:51.5882\tINFO\tMobileComputer.GetDeviceId\tКод устройства: '@MINDEO-M40-D-410244015546'"
            }, File.ReadAllLines(outputPath));
            AssertSequenceEqual(new[] { "not a log record" }, File.ReadAllLines(problemsPath));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static void Task3SupportsConfigurableDateOutput()
    {
        AssertTrue(LogStandardizer.TryStandardizeLine(
            "10.03.2025 15:14:49.523 INFORMATION Message",
            "dd-MM-yyyy",
            out var entry));

        AssertEqual("10-03-2025", entry.Date);
    }

    private static string CreateTemporaryDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "MyCod.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void Run(string name, Action action)
    {
        try
        {
            action();
            _passed++;
            Console.WriteLine($"PASS {name}");
        }
        catch (Exception exception)
        {
            _failed++;
            Console.WriteLine($"FAIL {name}");
            Console.WriteLine(exception);
        }
    }

    private static void AssertEqual<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected: {expected}; Actual: {actual}");
        }
    }

    private static void AssertSequenceEqual<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual)
    {
        if (expected.Count != actual.Count)
        {
            throw new InvalidOperationException($"Expected {expected.Count} items, actual {actual.Count} items.");
        }

        for (var i = 0; i < expected.Count; i++)
        {
            if (!EqualityComparer<T>.Default.Equals(expected[i], actual[i]))
            {
                throw new InvalidOperationException($"At index {i}. Expected: {expected[i]}; Actual: {actual[i]}");
            }
        }
    }

    private static void AssertTrue(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperation("Expected condition to be true.");
        }
    }

    private static void AssertThrows<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected exception: {typeof(TException).Name}");
    }
}
