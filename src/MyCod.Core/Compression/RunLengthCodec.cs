using System.Globalization;
using System.Text;

namespace MyCod.Core.Compression;

public static class RunLengthCodec
{
    public static string Compress(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(source.Length);
        var current = source[0];
        EnsureLowercaseLatin(current, 0);
        var count = 1;

        for (var i = 1; i < source.Length; i++)
        {
            var symbol = source[i];
            EnsureLowercaseLatin(symbol, i);

            if (symbol == current)
            {
                count++;
                continue;
            }

            AppendGroup(builder, current, count);
            current = symbol;
            count = 1;
        }

        AppendGroup(builder, current, count);
        return builder.ToString();
    }

    public static string Decompress(string compressed)
    {
        ArgumentNullException.ThrowIfNull(compressed);

        var builder = new StringBuilder(compressed.Length);
        var index = 0;

        while (index < compressed.Length)
        {
            var symbol = compressed[index];
            EnsureLowercaseLatin(symbol, index);
            index++;

            var countStart = index;
            while (index < compressed.Length && IsAsciiDigit(compressed[index]))
            {
                index++;
            }

            var count = 1;
            if (index > countStart)
            {
                if (compressed[countStart] == '0')
                {
                    throw new FormatException($"Invalid zero or leading-zero count at position {countStart}.");
                }

                var countText = compressed.AsSpan(countStart, index - countStart);
                if (!int.TryParse(countText, NumberStyles.None, CultureInfo.InvariantCulture, out count) || count <= 0)
                {
                    throw new FormatException($"Invalid count at position {countStart}.");
                }
            }

            builder.Append(symbol, count);
        }

        return builder.ToString();
    }

    private static void AppendGroup(StringBuilder builder, char symbol, int count)
    {
        builder.Append(symbol);

        if (count > 1)
        {
            builder.Append(count.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static void EnsureLowercaseLatin(char symbol, int position)
    {
        if (symbol < 'a' || symbol > 'z')
        {
            throw new FormatException($"Expected a lowercase Latin letter at position {position}.");
        }
    }

    private static bool IsAsciiDigit(char symbol) => symbol >= '0' && symbol <= '9';
}
