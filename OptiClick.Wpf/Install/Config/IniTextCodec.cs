using System.IO;
using System.Text;

namespace OptiClick.Wpf.Install.Config;

public static class IniTextCodec
{
    private static readonly (byte[] Bom, string EncodingName)[] IniBomEncodings =
    {
        (Encoding.UTF8.GetPreamble(), "utf-8"),
        (Encoding.UTF32.GetPreamble(), "utf-32"),
        (Encoding.Unicode.GetPreamble(), "utf-16"),
        (Encoding.BigEndianUnicode.GetPreamble(), "utf-16BE")
    };

    public sealed record IniTextReadResult
    {
        public string Text { get; init; } = "";
        public Encoding Encoding { get; init; } = Encoding.UTF8;
    }

    public static IniTextReadResult ReadWithFallback(string path)
    {
        var raw = File.ReadAllBytes(path);

        foreach (var (bom, encodingName) in IniBomEncodings)
        {
            if (bom.Length == 0 || raw.Length < bom.Length)
            {
                continue;
            }

            if (!raw.AsSpan(0, bom.Length).SequenceEqual(bom))
            {
                continue;
            }

            var encoding = Encoding.GetEncoding(encodingName);
            return new IniTextReadResult
            {
                Text = encoding.GetString(raw.AsSpan(bom.Length)),
                Encoding = encoding
            };
        }

        foreach (var encoding in EnumerateFallbackEncodings())
        {
            try
            {
                return new IniTextReadResult
                {
                    Text = encoding.GetString(raw),
                    Encoding = encoding
                };
            }
            catch (DecoderFallbackException)
            {
                // Continue to next candidate encoding.
            }
        }

        return new IniTextReadResult
        {
            Text = Encoding.UTF8.GetString(raw),
            Encoding = Encoding.UTF8
        };
    }

    public static void WriteWithEncoding(string path, string text, Encoding encoding)
    {
        using var writer = new StreamWriter(path, append: false, encoding);
        writer.NewLine = "\n";
        writer.Write(text);
    }

    public static string DetectPreferredNewLine(string text)
    {
        if (text.Contains("\r\n", StringComparison.Ordinal))
        {
            return "\r\n";
        }

        if (text.Contains('\n'))
        {
            return "\n";
        }

        if (text.Contains('\r'))
        {
            return "\r";
        }

        return OperatingSystem.IsWindows() ? "\r\n" : "\n";
    }

    private static IEnumerable<Encoding> EnumerateFallbackEncodings()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var encoding in CreateFallbackEncodings())
        {
            var key = encoding.WebName;
            if (!seen.Add(key))
            {
                continue;
            }

            yield return encoding;
        }
    }

    private static IEnumerable<Encoding> CreateFallbackEncodings()
    {
        yield return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        yield return Encoding.Default;

        Encoding? cp949 = null;
        try
        {
            cp949 = Encoding.GetEncoding(949, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        }
        catch
        {
            cp949 = null;
        }

        if (cp949 is not null)
        {
            yield return cp949;
        }
    }
}
