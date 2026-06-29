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

        Check("Task1 compression", TestCompression);
        Check("Task2 concurrent server", TestConcurrentServer);
        Check("Task3 log standardizer", TestLogStandardizer);

        Console.WriteLine();
        Console.WriteLine($"Passed: {_passed}");
        Console.WriteLine($"Failed: {_failed}");

        return _failed == 0 ? 0 : 1;
    }

    private static void TestCompression()
    {
        AssertEqual("a3b2c3d2e", RunLengthCodec.Compress("aaabbcccdde"));
        AssertEqual("aaabbcccdde", RunLengthCodec.Decompress("a3b2c3d2e"));
        AssertEqual("a12bz10", RunLengthCodec.Compress(new string('a', 12) + "b" + new string('z', 10)));
        AssertThrows<FormatException>(() => RunLengthCodec.Compress("abc1"));
        AssertThrows<FormatException>(() => RunLengthCodec.Decompress("a0"));
    }

    private static void TestConcurrentServer()
    {
        Server.ResetForTests();

        const int writers = 8;
        const int readers = 16;
        const int iterations = 5_000;

        using var start = new ManualResetEventSlim(initialState: false);
        var tasks = new List<Task>(writers + readers);

        for (var i = 0; i < writers; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                start.Wait();
                for (var j = 0; j < iterations; j++)
                {
                    Server.AddToCount(1);
                }
            }));
        }

        for (var i = 0; i < readers; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                start.Wait();
                var previous = 0;
                for (var j = 0; j < iterations; j++)
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

    private static void TestLogStandardizer()
    {
        AssertTrue(LogStandardizer.TryStandardizeLine(
            "10.03.2025 15:14:49.523 INFORMATION Version",
            LogStandardizerOptions.Default.DateOutputFormat,
            out var first));
        AssertEqual("2025-03-10\t15:14:49.523\tINFO\tDEFAULT\tVersion", first.ToTabSeparatedLine());

        AssertTrue(LogStandardizer.TryStandardizeLine(
            "2025-03-10 15:14:51.5882| WARNING|11|MobileComputer.GetDeviceId| Message",
            LogStandardizerOptions.Default.DateOutputFormat,
            out var second));
        AssertEqual("2025-03-10\t15:14:51.5882\tWARN\tMobileComputer.GetDeviceId\tMessage", second.ToTabSeparatedLine());

        AssertTrue(LogStandardizer.TryStandardizeLine(
            "10.03.2025 15:14:49.523 INFO Message",
            "dd-MM-yyyy",
            out var customDate));
        AssertEqual("10-03-2025", customDate.Date);

        var directory = CreateTemporaryDirectory();
        try
        {
            var inputPath = Path.Combine(directory, "input.log");
            var outputPath = Path.Combine(directory, "standardized.log");
            var problemsPath = Path.Combine(directory, "problems.txt");

            File.WriteAllLines(inputPath, new[]
            {
                "10.03.2025 15:14:49.523 INFORMATION Version",
                "broken line"
            });

            var result = LogStandardizer.ConvertFile(inputPath, outputPath, problemsPath);
            AssertEqual(new LogProcessingResult(2, 1, 1), result);
            AssertSequenceEqual(new[] { "2025-03-10\t15:14:49.523\tINFO\tDEFAULT\tVersion" }, File.ReadAllLines(outputPath));
            AssertSequenceEqual(new[] { "broken line" }, File.ReadAllLines(problemsPath));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "MyCod.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void Check(string name, Action test)
    {
        try
        {
            test();
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
        AssertEqual(expected.Count, actual.Count);
        for (var i = 0; i < expected.Count; i++)
        {
            AssertEqual(expected[i], actual[i]);
        }
    }

    private static void AssertTrue(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Expected condition to be true.");
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
