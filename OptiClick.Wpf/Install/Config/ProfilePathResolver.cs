using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace OptiClick.Wpf.Install.Config;

public sealed class ProfilePathResolver : IProfilePathResolver
{
    private const string DocumentsEnvToken = "%DOCUMENTS%";
    private static readonly Regex PathSplitPattern = new(@"[\\/]+", RegexOptions.Compiled);

    private readonly IDocumentsPathProvider _documentsPathProvider;
    private readonly IEnvironmentPathExpander _environmentPathExpander;

    public ProfilePathResolver(
        IDocumentsPathProvider? documentsPathProvider = null,
        IEnvironmentPathExpander? environmentPathExpander = null)
    {
        _environmentPathExpander = environmentPathExpander ?? new EnvironmentPathExpander();
        _documentsPathProvider = documentsPathProvider ?? new WindowsDocumentsPathProvider(_environmentPathExpander);
    }

    public string? Resolve(string targetPath, string configuredPath, bool requireExisting)
    {
        var rawPath = (configuredPath ?? "").Trim();
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return null;
        }

        var expandedPath = _environmentPathExpander.ExpandEnvironmentVariables(
            _environmentPathExpander.ExpandUserHome(rawPath));
        if (Path.IsPathRooted(expandedPath))
        {
            var absolutePath = Path.GetFullPath(expandedPath);
            if (requireExisting && !File.Exists(absolutePath))
            {
                return null;
            }

            return absolutePath;
        }

        var firstPart = SplitRelativePathParts(rawPath).FirstOrDefault()?.Trim().ToLowerInvariant() ?? "";
        if (firstPart is "documents" or "document" or "my games"
            || rawPath.StartsWith(DocumentsEnvToken, StringComparison.OrdinalIgnoreCase))
        {
            var documentsCandidate = ResolveDocumentsCandidatePath(rawPath);
            if (documentsCandidate is not null && (!requireExisting || File.Exists(documentsCandidate)))
            {
                return documentsCandidate;
            }

            return null;
        }

        var directCandidate = Path.GetFullPath(Path.Combine(targetPath ?? "", rawPath));
        if (!requireExisting || File.Exists(directCandidate))
        {
            return directCandidate;
        }

        var documentsRawCandidate = ResolveDocumentsCandidatePath(rawPath);
        if (documentsRawCandidate is not null && File.Exists(documentsRawCandidate))
        {
            return documentsRawCandidate;
        }

        var documentsFallback = ResolveDocumentsCandidatePath($"Documents\\{rawPath}");
        if (documentsFallback is not null && File.Exists(documentsFallback))
        {
            return documentsFallback;
        }

        return null;
    }

    public string? ResolveIngameIniPath(string targetPath, string ingameIniName)
    {
        return Resolve(targetPath, ingameIniName, requireExisting: true);
    }

    private string? ResolveDocumentsCandidatePath(string relativePath)
    {
        var matches = ResolveDocumentsMatches(relativePath);
        if (matches.Count > 0)
        {
            return matches[0];
        }

        var relativeParts = TrimDocumentsPrefix(relativePath);
        if (relativeParts.Count == 0)
        {
            return null;
        }

        foreach (var documentsDir in _documentsPathProvider.GetDocumentsCandidates())
        {
            return Path.Combine(documentsDir, Path.Combine(relativeParts.ToArray()));
        }

        return null;
    }

    private IReadOnlyList<string> ResolveDocumentsMatches(string relativePath)
    {
        var relativeParts = TrimDocumentsPrefix(relativePath);
        if (relativeParts.Count == 0)
        {
            return Array.Empty<string>();
        }

        var hasWildcard = relativeParts.Any(HasPathWildcard);
        var matches = new List<string>();
        foreach (var documentsDir in _documentsPathProvider.GetDocumentsCandidates())
        {
            if (hasWildcard)
            {
                matches.AddRange(MatchDocumentsRelativePath(documentsDir, relativeParts));
                continue;
            }

            var candidate = Path.Combine(documentsDir, Path.Combine(relativeParts.ToArray()));
            if (File.Exists(candidate))
            {
                matches.Add(candidate);
            }
        }

        return DedupePaths(matches);
    }

    private IReadOnlyList<string> MatchDocumentsRelativePath(string baseDir, IReadOnlyList<string> relativeParts)
    {
        if (relativeParts.Count == 0)
        {
            return Array.Empty<string>();
        }

        var currentPaths = new List<string> { baseDir };
        for (var index = 0; index < relativeParts.Count; index++)
        {
            var rawPart = relativeParts[index];
            var isLastPart = index == relativeParts.Count - 1;
            var nextPaths = new List<string>();
            var pattern = rawPart.ToLowerInvariant();
            var partHasWildcard = HasPathWildcard(rawPart);

            foreach (var currentPath in currentPaths)
            {
                if (!Directory.Exists(currentPath))
                {
                    continue;
                }

                if (partHasWildcard)
                {
                    IEnumerable<string> children;
                    try
                    {
                        children = Directory.EnumerateFileSystemEntries(currentPath);
                    }
                    catch (IOException)
                    {
                        continue;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        continue;
                    }

                    foreach (var child in children)
                    {
                        var name = Path.GetFileName(child).ToLowerInvariant();
                        if (!FnMatch(name, pattern))
                        {
                            continue;
                        }

                        if (isLastPart && File.Exists(child))
                        {
                            nextPaths.Add(child);
                        }
                        else if (!isLastPart && Directory.Exists(child))
                        {
                            nextPaths.Add(child);
                        }
                    }

                    continue;
                }

                var childPath = Path.Combine(currentPath, rawPart);
                if (isLastPart && File.Exists(childPath))
                {
                    nextPaths.Add(childPath);
                }
                else if (!isLastPart && Directory.Exists(childPath))
                {
                    nextPaths.Add(childPath);
                }
            }

            currentPaths = DedupePaths(nextPaths).ToList();
            if (currentPaths.Count == 0)
            {
                break;
            }
        }

        return currentPaths;
    }

    private IReadOnlyList<string> DedupePaths(IEnumerable<string> paths)
    {
        var uniquePaths = new List<string>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths)
        {
            var normalized = NormalizeCandidatePath(path);
            if (!seenPaths.Add(normalized))
            {
                continue;
            }

            uniquePaths.Add(path);
        }

        return uniquePaths;
    }

    private string NormalizeCandidatePath(string path)
    {
        var expanded = _environmentPathExpander.ExpandUserHome(path ?? "");
        try
        {
            return Path.GetFullPath(expanded).ToLowerInvariant();
        }
        catch
        {
            return expanded.ToLowerInvariant();
        }
    }

    private static IReadOnlyList<string> SplitRelativePathParts(string pathText)
    {
        if (string.IsNullOrWhiteSpace(pathText))
        {
            return Array.Empty<string>();
        }

        return PathSplitPattern
            .Split(pathText)
            .Where(static part => !string.IsNullOrWhiteSpace(part) && !string.Equals(part, ".", StringComparison.Ordinal))
            .ToArray();
    }

    private static IReadOnlyList<string> TrimDocumentsPrefix(string relativePath)
    {
        var normalized = (relativePath ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Array.Empty<string>();
        }

        if (normalized.StartsWith(DocumentsEnvToken, StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[DocumentsEnvToken.Length..].TrimStart('\\', '/');
        }

        var parts = SplitRelativePathParts(normalized).ToList();
        if (parts.Count > 0)
        {
            var first = parts[0].Trim();
            if (first.Equals("documents", StringComparison.OrdinalIgnoreCase)
                || first.Equals("document", StringComparison.OrdinalIgnoreCase))
            {
                parts.RemoveAt(0);
            }
        }

        return parts;
    }

    private static bool HasPathWildcard(string pathPart)
    {
        return pathPart.IndexOfAny(new[] { '*', '?', '[' }) >= 0;
    }

    private static bool FnMatch(string text, string pattern)
    {
        var regex = GlobToRegex(pattern);
        return Regex.IsMatch(text, regex, RegexOptions.CultureInvariant);
    }

    private static string GlobToRegex(string pattern)
    {
        var builder = new StringBuilder();
        builder.Append('^');
        for (var index = 0; index < pattern.Length; index++)
        {
            var current = pattern[index];
            switch (current)
            {
                case '*':
                    builder.Append(".*");
                    break;
                case '?':
                    builder.Append('.');
                    break;
                case '[':
                    {
                        var endBracketIndex = pattern.IndexOf(']', index + 1);
                        if (endBracketIndex <= index + 1)
                        {
                            builder.Append(@"\[");
                            break;
                        }

                        var characterClass = pattern[(index + 1)..endBracketIndex];
                        if (characterClass.StartsWith("!", StringComparison.Ordinal))
                        {
                            characterClass = $"^{Regex.Escape(characterClass[1..])}";
                        }
                        else
                        {
                            characterClass = Regex.Escape(characterClass);
                        }

                        characterClass = characterClass.Replace(@"\-", "-");
                        builder.Append('[').Append(characterClass).Append(']');
                        index = endBracketIndex;
                        break;
                    }
                default:
                    builder.Append(Regex.Escape(current.ToString()));
                    break;
            }
        }

        builder.Append('$');
        return builder.ToString();
    }
}
