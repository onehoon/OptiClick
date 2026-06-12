namespace OptiClick.Core.Models;

public static class AppLanguagePreference
{
    public const string Auto = "auto";
    public const string Korean = "ko";
    public const string English = "en";

    public static string Normalize(string? value)
    {
        return (value ?? string.Empty).Trim().ToLowerInvariant();
    }

    public static string NormalizeOrDefault(string? value)
    {
        return Normalize(value) switch
        {
            Korean => Korean,
            English => English,
            Auto => Auto,
            _ => Auto
        };
    }
}
