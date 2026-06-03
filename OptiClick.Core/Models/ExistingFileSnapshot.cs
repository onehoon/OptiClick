namespace OptiClick.Core.Models;

public sealed record ExistingFileSnapshot
{
    public IReadOnlyList<ExistingFileEntry> Files { get; init; } = Array.Empty<ExistingFileEntry>();

    public ExistingFileEntry? Find(string relativePath)
    {
        return Files.FirstOrDefault(file => file.Exists && Matches(file.RelativePath, relativePath));
    }

    public bool Contains(string relativePath)
    {
        return Find(relativePath) is not null;
    }

    private static bool Matches(string left, string right)
    {
        return NormalizePath(left).Equals(NormalizePath(right), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string value)
    {
        return value.Replace('\\', '/').Trim().TrimStart('/');
    }
}
