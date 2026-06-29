using System.Globalization;
using System.Text;

namespace MyCod.Core.Compression;

/// <summary>
/// Implements a small run-length encoding codec for strings that contain only
/// lowercase Latin letters. The compressed form stores every group as a letter
/// and, only when the group length is greater than one, the decimal count.
/// Example: <c>aaabbcccdde</c> becomes <c>a3b2c3d2e</c>.
/// </summary>
public static class RunLengthCodec
{
    /// <summary>
    /// Compresses a source string by replacing consecutive equal letters with
    /// a pair "letter + count". A single letter is written without count,
    /// because <c>a1</c> would be longer and is not required by the task.
    /// </summary>
    public static string Compress(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.Length == 0)
        {
            return string.Empty;
        }

        // StringBuilder avoids creating a new string after every appended group.
        // The initial capacity is a conservative estimate; compressed text can
        // be slightly longer only for unusual inputs such as "aaaaaaaaaa" -> "a10".
        var builder = new StringBuilder(source.Length);
        var current = source[0];
        EnsureLowercaseLatin(current, 0);
        var count = 1;

        // Scan once from left to right. When the current run ends, write it to
        // the result and start counting the next run.
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

        // The last run is not written inside the loop, so it must be flushed
        // after the scan completes.
        AppendGroup(builder, current, count);
        return builder.ToString();
    }

    /// <summary>
    /// Restores a string from the compressed representation produced by
    /// <see cref="Compress"/>. Counts may contain several digits, for example
    /// <c>a12</c>. Counts with zero or a leading zero are rejected as invalid.
    /// </summary>
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

            // After a letter there may be zero or more decimal digits. No digits
            // means that the original group contained exactly one letter.
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
