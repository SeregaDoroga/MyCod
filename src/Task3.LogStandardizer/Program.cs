using System.Text;
using MyCod.Core.Logs;
using CoreLogStandardizer = MyCod.Core.Logs.LogStandardizer;

namespace Task3.LogStandardizer;

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
                throw new ArgumentException("Передайте входной и выходной файлы.");
            }

            var inputPath = args[0];
            var outputPath = args[1];

            // By default problems.txt is written next to the standardized output
            // file. The user can override the path with --problems.
            var problemsPath = Path.Combine(
                Path.GetDirectoryName(Path.GetFullPath(outputPath)) ?? Environment.CurrentDirectory,
                "problems.txt");
            var dateFormat = LogStandardizerOptions.Default.DateOutputFormat;

            for (var i = 2; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--problems":
                        problemsPath = ReadOptionValue(args, ref i, "--problems");
                        break;
                    case "--date-format":
                        dateFormat = ReadOptionValue(args, ref i, "--date-format");
                        break;
                    default:
                        throw new ArgumentException($"Неизвестный аргумент: {args[i]}");
                }
            }

            // The alias avoids a name collision between the console project
            // namespace Task3.LogStandardizer and the core LogStandardizer class.
            var result = CoreLogStandardizer.ConvertFile(
                inputPath,
                outputPath,
                problemsPath,
                new LogStandardizerOptions(dateFormat));

            Console.WriteLine($"Всего строк: {result.TotalLines}");
            Console.WriteLine($"Записано в стандартный лог: {result.WrittenLines}");
            Console.WriteLine($"Записано в problems.txt: {result.ProblemLines}");
            Console.WriteLine($"Выходной файл: {Path.GetFullPath(outputPath)}");
            Console.WriteLine($"Проблемные строки: {Path.GetFullPath(problemsPath)}");

            return 0;
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException or IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Ошибка: {exception.Message}");
            return 1;
        }
    }

    private static string ReadOptionValue(string[] args, ref int index, string optionName)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"После {optionName} ожидается значение.");
        }

        index++;
        return args[index];
    }

    private static bool IsHelp(string value) => value is "-h" or "--help" or "/?";

    private static void PrintUsage()
    {
        Console.WriteLine("Использование:");
        Console.WriteLine("  dotnet run --project src/Task3.LogStandardizer -- input.log output.log");
        Console.WriteLine("  dotnet run --project src/Task3.LogStandardizer -- input.log output.log --problems problems.txt --date-format yyyy-MM-dd");
    }
}
