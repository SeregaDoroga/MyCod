using System.Text;
using MyCod.Core.Compression;

namespace Task1.Compression;

internal static class Program
{
    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintUsage();
            return args.Length == 0 ? 2 : 0;
        }

        try
        {
            if (args.Length < 2)
            {
                throw new ArgumentException("Передайте режим и строку.");
            }

            var mode = args[0].Trim().ToLowerInvariant();
            var text = args[1];

            var result = mode switch
            {
                "compress" or "c" => RunLengthCodec.Compress(text),
                "decompress" or "d" => RunLengthCodec.Decompress(text),
                _ => throw new ArgumentException($"Неизвестный режим: {args[0]}")
            };

            Console.WriteLine(result);
            return 0;
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException or OverflowException)
        {
            Console.Error.WriteLine($"Ошибка: {exception.Message}");
            return 1;
        }
    }

    private static bool IsHelp(string value) => value is "-h" or "--help" or "/?";

    private static void PrintUsage()
    {
        Console.WriteLine("Использование:");
        Console.WriteLine("  dotnet run --project src/Task1.Compression -- compress aaabbcccdde");
        Console.WriteLine("  dotnet run --project src/Task1.Compression -- decompress a3b2c3d2e");
    }
}
