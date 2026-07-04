using System.Security.Cryptography;
using System.Text;

namespace OptiClick.Wpf.Shell.RemoteV2;

public static class AuthV2RequestSigner
{
    public static string CreateSignature(
        string method,
        string path,
        string clientId,
        string appStartId,
        long timestamp,
        string nonce,
        string rawBody,
        string clientSecret)
    {
        var bodyHash = Sha256Hex(rawBody);
        var canonical = string.Join(
            "\n",
            (method ?? "").Trim().ToUpperInvariant(),
            string.IsNullOrWhiteSpace(path) ? "/" : path.Trim(),
            (clientId ?? "").Trim(),
            (appStartId ?? "").Trim(),
            timestamp.ToString(System.Globalization.CultureInfo.InvariantCulture),
            (nonce ?? "").Trim(),
            bodyHash);
        var secretBytes = Base64UrlDecode(clientSecret);
        using var hmac = new HMACSHA256(secretBytes);
        return Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical)));
    }

    public static string Sha256Hex(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text ?? ""));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var normalized = (value ?? "").Trim().Replace('-', '+').Replace('_', '/');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return [];
        }

        var padded = normalized.PadRight((normalized.Length + 3) / 4 * 4, '=');
        return Convert.FromBase64String(padded);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

