using System.IO;

namespace OptiClick.Wpf.Install.Archives;

public interface IArchivePayloadValidator
{
    bool IsValid(string payloadDirectory, out string error);
}

public sealed class NonEmptyPayloadValidator : IArchivePayloadValidator
{
    public bool IsValid(string payloadDirectory, out string error)
    {
        error = "";
        if (!Directory.Exists(payloadDirectory))
        {
            error = "payload_directory_not_found";
            return false;
        }

        if (!Directory.EnumerateFileSystemEntries(payloadDirectory).Any())
        {
            error = "payload_directory_empty";
            return false;
        }

        return true;
    }
}

public sealed class RequiredFilesPayloadValidator : IArchivePayloadValidator
{
    private readonly string[] _requiredFileNames;
    private readonly bool _searchAllDirectories;

    public RequiredFilesPayloadValidator(IEnumerable<string> requiredFileNames, bool searchAllDirectories = true)
    {
        _requiredFileNames = requiredFileNames
            .Where(static fileName => !string.IsNullOrWhiteSpace(fileName))
            .Select(static fileName => fileName.Trim())
            .ToArray();
        _searchAllDirectories = searchAllDirectories;
    }

    public bool IsValid(string payloadDirectory, out string error)
    {
        var nonEmpty = new NonEmptyPayloadValidator();
        if (!nonEmpty.IsValid(payloadDirectory, out error))
        {
            return false;
        }

        foreach (var fileName in _requiredFileNames)
        {
            if (ContainsFile(payloadDirectory, fileName))
            {
                continue;
            }

            error = $"missing_required_payload_file:{fileName}";
            return false;
        }

        error = "";
        return true;
    }

    private bool ContainsFile(string payloadDirectory, string fileName)
    {
        if (!_searchAllDirectories)
        {
            return File.Exists(Path.Combine(payloadDirectory, fileName));
        }

        return Directory
            .EnumerateFiles(payloadDirectory, fileName, SearchOption.AllDirectories)
            .Any();
    }
}

public sealed class SingleExtensionPayloadValidator : IArchivePayloadValidator
{
    private readonly string _extension;

    public SingleExtensionPayloadValidator(string extension)
    {
        _extension = extension.StartsWith(".", StringComparison.Ordinal)
            ? extension
            : $".{extension}";
    }

    public bool IsValid(string payloadDirectory, out string error)
    {
        var nonEmpty = new NonEmptyPayloadValidator();
        if (!nonEmpty.IsValid(payloadDirectory, out error))
        {
            return false;
        }

        var candidates = Directory
            .EnumerateFiles(payloadDirectory, "*", SearchOption.AllDirectories)
            .Where(path => string.Equals(Path.GetExtension(path), _extension, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (candidates.Length == 0)
        {
            error = $"missing_required_payload_file:*{_extension}";
            return false;
        }

        if (candidates.Length > 1)
        {
            error = "multiple_candidates";
            return false;
        }

        error = "";
        return true;
    }
}

public sealed class OptiPatcherPayloadValidator : IArchivePayloadValidator
{
    public bool IsValid(string payloadDirectory, out string error)
    {
        var nonEmpty = new NonEmptyPayloadValidator();
        if (!nonEmpty.IsValid(payloadDirectory, out error))
        {
            return false;
        }

        var candidates = Directory
            .EnumerateFiles(payloadDirectory, "*.asi", SearchOption.AllDirectories)
            .Where(static path => IsOptiPatcherAsi(Path.GetFileName(path)))
            .ToArray();
        if (candidates.Length == 0)
        {
            error = "missing_required_payload_file:OptiPatcher.asi";
            return false;
        }

        if (candidates.Length > 1)
        {
            error = "multiple_candidates";
            return false;
        }

        error = "";
        return true;
    }

    private static bool IsOptiPatcherAsi(string fileName)
    {
        if (!string.Equals(Path.GetExtension(fileName), ".asi", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var stem = Path.GetFileNameWithoutExtension(fileName);
        return stem.StartsWith("OptiPatcher", StringComparison.OrdinalIgnoreCase);
    }
}
