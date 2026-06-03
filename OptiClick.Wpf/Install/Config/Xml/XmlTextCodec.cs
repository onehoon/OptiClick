using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace OptiClick.Wpf.Install.Config.Xml;

public static class XmlTextCodec
{
    private static readonly Regex XmlDeclEncodingRegex = new(
        "<\\?xml[^>]*encoding\\s*=\\s*[\"']([A-Za-z0-9._:-]+)[\"']",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly byte[] Utf16LeXmlDeclPrefix = [0x3C, 0x00, 0x3F, 0x00];

    private static readonly (byte[] Bom, Encoding Encoding)[] BomEncodings =
    [
        (Encoding.UTF32.GetPreamble(), new UTF32Encoding(bigEndian: false, byteOrderMark: false, throwOnInvalidCharacters: true)),
        (new UTF32Encoding(bigEndian: true, byteOrderMark: true).GetPreamble(), new UTF32Encoding(bigEndian: true, byteOrderMark: false, throwOnInvalidCharacters: true)),
        (Encoding.Unicode.GetPreamble(), new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true)),
        (Encoding.BigEndianUnicode.GetPreamble(), new UnicodeEncoding(bigEndian: true, byteOrderMark: false, throwOnInvalidBytes: true)),
        (Encoding.UTF8.GetPreamble(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true))
    ];

    static XmlTextCodec()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static XmlTextReadResult ReadWithFallback(string filePath)
    {
        var raw = File.ReadAllBytes(filePath);
        foreach (var (bom, encoding) in BomEncodings)
        {
            if (bom.Length == 0 || raw.Length < bom.Length)
            {
                continue;
            }

            if (!raw.AsSpan(0, bom.Length).SequenceEqual(bom))
            {
                continue;
            }

            var text = encoding.GetString(raw.AsSpan(bom.Length));
            return new XmlTextReadResult
            {
                Text = text,
                EncodingInfo = new XmlEncodingInfo
                {
                    Encoding = encoding,
                    Bom = bom.ToArray()
                }
            };
        }

        if (raw.Length >= Utf16LeXmlDeclPrefix.Length
            && raw.AsSpan(0, Utf16LeXmlDeclPrefix.Length).SequenceEqual(Utf16LeXmlDeclPrefix))
        {
            var utf16Le = new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true);
            return new XmlTextReadResult
            {
                Text = utf16Le.GetString(raw),
                EncodingInfo = new XmlEncodingInfo
                {
                    Encoding = utf16Le
                }
            };
        }

        var encodingCandidates = BuildEncodingCandidates(raw);
        foreach (var encoding in encodingCandidates)
        {
            try
            {
                var text = encoding.GetString(raw);
                return new XmlTextReadResult
                {
                    Text = text,
                    EncodingInfo = new XmlEncodingInfo
                    {
                        Encoding = encoding
                    }
                };
            }
            catch (Exception ex) when (ex is DecoderFallbackException or ArgumentException)
            {
                _ = ex;
            }
        }

        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);
        return new XmlTextReadResult
        {
            Text = utf8.GetString(raw),
            EncodingInfo = new XmlEncodingInfo
            {
                Encoding = utf8
            }
        };
    }

    public static void WriteWithOriginalEncoding(string filePath, string text, XmlEncodingInfo encodingInfo)
    {
        var bom = encodingInfo.Bom ?? Array.Empty<byte>();
        var encoded = encodingInfo.Encoding.GetBytes(text ?? "");
        var output = new byte[bom.Length + encoded.Length];
        if (bom.Length > 0)
        {
            Buffer.BlockCopy(bom, 0, output, 0, bom.Length);
        }

        Buffer.BlockCopy(encoded, 0, output, bom.Length, encoded.Length);
        File.WriteAllBytes(filePath, output);
    }

    private static IReadOnlyList<Encoding> BuildEncodingCandidates(byte[] raw)
    {
        var candidates = new List<Encoding>();
        var declaredEncoding = TryReadXmlDeclarationEncoding(raw);
        if (!string.IsNullOrWhiteSpace(declaredEncoding))
        {
            var parsed = TryCreateEncoding(declaredEncoding!);
            if (parsed is not null)
            {
                candidates.Add(parsed);
            }
        }

        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        candidates.Add(utf8);

        try
        {
            candidates.Add(CreateStrictEncoding(Encoding.Default.WebName));
        }
        catch
        {
            // Keep fallback chain best-effort.
        }

        if (OperatingSystem.IsWindows())
        {
            try
            {
                candidates.Add(CreateStrictEncoding("mbcs"));
            }
            catch
            {
                // Keep fallback chain best-effort.
            }
        }

        try
        {
            candidates.Add(CreateStrictEncoding("cp949"));
        }
        catch
        {
            // Keep fallback chain best-effort.
        }

        var deduped = new List<Encoding>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var encoding in candidates)
        {
            if (seen.Add(encoding.WebName))
            {
                deduped.Add(encoding);
            }
        }

        return deduped;
    }

    private static string? TryReadXmlDeclarationEncoding(byte[] raw)
    {
        var maxLength = Math.Min(512, raw.Length);
        if (maxLength <= 0)
        {
            return null;
        }

        var ascii = Encoding.ASCII.GetString(raw, 0, maxLength);
        var match = XmlDeclEncodingRegex.Match(ascii);
        if (!match.Success || match.Groups.Count < 2)
        {
            return null;
        }

        var declared = match.Groups[1].Value.Trim();
        return string.IsNullOrWhiteSpace(declared) ? null : declared;
    }

    private static Encoding? TryCreateEncoding(string encodingName)
    {
        try
        {
            return CreateStrictEncoding(encodingName);
        }
        catch
        {
            try
            {
                return Encoding.GetEncoding(encodingName);
            }
            catch
            {
                return null;
            }
        }
    }

    private static Encoding CreateStrictEncoding(string encodingName)
    {
        return Encoding.GetEncoding(
            encodingName,
            EncoderFallback.ExceptionFallback,
            DecoderFallback.ExceptionFallback);
    }
}
