using System.IO;
using System.Text;

namespace OptiClick.Infrastructure.Install.Config;

public static class JsonTextCodec
{
    private static readonly (byte[] Bom, Encoding Encoding)[] BomEncodings =
    [
        (new UTF32Encoding(bigEndian: false, byteOrderMark: true).GetPreamble(), new UTF32Encoding(bigEndian: false, byteOrderMark: false, throwOnInvalidCharacters: true)),
        (new UTF32Encoding(bigEndian: true, byteOrderMark: true).GetPreamble(), new UTF32Encoding(bigEndian: true, byteOrderMark: false, throwOnInvalidCharacters: true)),
        (Encoding.Unicode.GetPreamble(), new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true)),
        (Encoding.BigEndianUnicode.GetPreamble(), new UnicodeEncoding(bigEndian: true, byteOrderMark: false, throwOnInvalidBytes: true)),
        (Encoding.UTF8.GetPreamble(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true))
    ];

    public static JsonTextReadResult ReadWithFallback(string filePath)
    {
        var raw = File.ReadAllBytes(filePath);
        foreach (var (bom, encoding) in BomEncodings)
        {
            if (bom.Length == 0 || raw.Length < bom.Length || !raw.AsSpan(0, bom.Length).SequenceEqual(bom))
            {
                continue;
            }

            return new JsonTextReadResult
            {
                Text = encoding.GetString(raw, bom.Length, raw.Length - bom.Length),
                EncodingInfo = new JsonEncodingInfo
                {
                    Encoding = encoding,
                    Bom = bom
                }
            };
        }

        var detectedUtf16 = DetectUtf16ByNullPattern(raw);
        if (detectedUtf16 is not null)
        {
            return new JsonTextReadResult
            {
                Text = detectedUtf16.GetString(raw),
                EncodingInfo = new JsonEncodingInfo
                {
                    Encoding = detectedUtf16
                }
            };
        }

        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        try
        {
            return new JsonTextReadResult
            {
                Text = utf8.GetString(raw),
                EncodingInfo = new JsonEncodingInfo
                {
                    Encoding = utf8
                }
            };
        }
        catch (DecoderFallbackException)
        {
            var lenientUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);
            return new JsonTextReadResult
            {
                Text = lenientUtf8.GetString(raw),
                EncodingInfo = new JsonEncodingInfo
                {
                    Encoding = lenientUtf8
                }
            };
        }
    }

    public static void WriteWithOriginalEncoding(string filePath, string text, JsonEncodingInfo encodingInfo)
    {
        var encoded = encodingInfo.Encoding.GetBytes(text ?? "");
        if (encodingInfo.Bom.Length == 0)
        {
            File.WriteAllBytes(filePath, encoded);
            return;
        }

        var output = new byte[encodingInfo.Bom.Length + encoded.Length];
        Buffer.BlockCopy(encodingInfo.Bom, 0, output, 0, encodingInfo.Bom.Length);
        Buffer.BlockCopy(encoded, 0, output, encodingInfo.Bom.Length, encoded.Length);
        File.WriteAllBytes(filePath, output);
    }

    private static Encoding? DetectUtf16ByNullPattern(byte[] raw)
    {
        var pairCount = Math.Min(raw.Length / 2, 64);
        if (pairCount < 2)
        {
            return null;
        }

        var lePairs = 0;
        var bePairs = 0;
        for (var pairIndex = 0; pairIndex < pairCount; pairIndex++)
        {
            var evenByte = raw[pairIndex * 2];
            var oddByte = raw[(pairIndex * 2) + 1];
            if (oddByte == 0 && IsLikelyJsonTextByte(evenByte))
            {
                lePairs++;
            }

            if (evenByte == 0 && IsLikelyJsonTextByte(oddByte))
            {
                bePairs++;
            }
        }

        var threshold = Math.Max(2, pairCount / 3);
        if (lePairs >= threshold && lePairs > bePairs)
        {
            return new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true);
        }

        if (bePairs >= threshold && bePairs > lePairs)
        {
            return new UnicodeEncoding(bigEndian: true, byteOrderMark: false, throwOnInvalidBytes: true);
        }

        return null;
    }

    private static bool IsLikelyJsonTextByte(byte value)
    {
        return value is 0x09 or 0x0A or 0x0D or >= 0x20 and <= 0x7E;
    }
}

public sealed record JsonTextReadResult
{
    public string Text { get; init; } = "";
    public JsonEncodingInfo EncodingInfo { get; init; } = new();
}

public sealed record JsonEncodingInfo
{
    public Encoding Encoding { get; init; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    public byte[] Bom { get; init; } = [];
}
