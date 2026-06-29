using System.Diagnostics;
using System.Text;
using MyCod.Core.ConcurrentServer;

namespace Task2.ConcurrentServer;

internal static class Program
{
    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        if (args.Length > 0 && IsHelp(args[0]))
        {
            PrintUsage();
            return 0;
        }

        try
        {
            var readers = ReadIntOption(args, "--readers", 24);
            var writers = ReadIntOption(args, "--writers", 6);
            var iterations = ReadIntOption(args, "--iterations", 10_000);

            if (readers < 0 || writers <= 0 || iterations <= 0)
            {
                throw new ArgumentException("readers должен быть >= 0, writers и iterations должны быть > 0.");
            }

            var stopwatch = Stopwatch.StartNew();
            var start = new ManualResetEventSlim(initialState: false);
            var tasks = new List<Task>(readers + writers);

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
                    var lastSeen = 0;
                    for (var i = 0; i < iterations; i++)
                    {
                        var current = Server.GetCount();
                        if (current < lastSeen)
                        {
                            throw new InvalidOperationException("Значение count уменьшилось между чтениями.");
                        }

                        lastSeen = current;
                    }
                }));
            }

            start.Set();
            Task.WaitAll(tasks.ToArray());
            stopwatch.Stop();

            var expected = checked(writers * iterations);
            var actual = Server.GetCount();

            Console.WriteLine($"Readers: {readers}");
            Console.WriteLine($"Writers: {writers}");
            Console.WriteLine($"Iterations per writer: {iterations}");
            Console.WriteLine($"Expected count: {expected}");
            Console.WriteLine($"Actual count: {actual}");
            Console.WriteLine($"Elapsed: {stopwatch.Elapsed}");

            return actual == expected ? 0 : 2;
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException or OverflowException or AggregateException)
        {
            Console.Error.WriteLine($"Ошибка: {exception.Message}");
            return 1;
        }
    }

    private static int ReadIntOption(string[] args, string name, int defaultValue)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (!string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (i + 1 >= args.Length || !int.TryParse(args[i + 1], out var value))
            {
                throw new FormatException($"После {name} ожидается целое число.");
            }

            return value;
        }

        return defaultValue;
    }

    private static bool IsHelp(string value) => value is "-h" or "--help" or "/?";

    private static void PrintUsage()
    {
        Console.WriteLine("Использование:");
        Console.WriteLine("  dotnet run --project src/Task2.ConcurrentServer");
        Console.WriteLine("  dotnet run --project src/Task2.ConcurrentServer -- --readers 50 --writers 8 --iterations 20000");
    }
}
