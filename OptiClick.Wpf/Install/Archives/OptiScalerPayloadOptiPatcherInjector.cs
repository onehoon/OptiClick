using System.IO;
using OptiClick.Core.Install;

namespace OptiClick.Wpf.Install.Archives;

public interface IOptiScalerPayloadOptiPatcherInjector
{
    OptiScalerPayloadOptiPatcherInjectionResult Inject(OptiScalerPayloadOptiPatcherInjectionRequest request);
}

public sealed record OptiScalerPayloadOptiPatcherInjectionRequest
{
    public string OptiPatcherPayloadDirectory { get; init; } = "";
    public IReadOnlyList<OptiScalerPayloadOptiPatcherInjectionTarget> Targets { get; init; } = [];
}

public sealed record OptiScalerPayloadOptiPatcherInjectionTarget
{
    public string Variant { get; init; } = "";
    public string CacheEntryName { get; init; } = "";
    public string PayloadDirectory { get; init; } = "";
}

public sealed record OptiScalerPayloadOptiPatcherInjectionResult
{
    public bool HasTargets => Targets.Count > 0;
    public bool IsReady => HasTargets && Targets.All(static target => target.Ready);
    public string SourcePath { get; init; } = "";
    public string SourceErrorCode { get; init; } = "";
    public IReadOnlyList<OptiScalerPayloadOptiPatcherInjectionTargetResult> Targets { get; init; } = [];
}

public sealed record OptiScalerPayloadOptiPatcherInjectionTargetResult
{
    public string Variant { get; init; } = "";
    public string CacheEntryName { get; init; } = "";
    public string PayloadDirectory { get; init; } = "";
    public string DestinationPath { get; init; } = "";
    public bool Ready { get; init; }
    public bool Injected { get; init; }
    public bool UsedExisting { get; init; }
    public string ErrorCode { get; init; } = "";
}

public sealed class OptiScalerPayloadOptiPatcherInjector : IOptiScalerPayloadOptiPatcherInjector
{
    public const string SourceMissing = "optipatcher_source_missing";
    public const string PayloadMissing = "optiscaler_payload_missing";
    public const string PayloadInvalid = "optiscaler_payload_invalid";
    public const string OptiPatcherMissing = "optipatcher_payload_missing";
    public const string MultipleCandidates = "optipatcher_multiple_candidates";
    public const string CopyFailed = "optipatcher_copy_failed";

    public OptiScalerPayloadOptiPatcherInjectionResult Inject(OptiScalerPayloadOptiPatcherInjectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var source = ResolveOptiPatcherSource(request.OptiPatcherPayloadDirectory, out var sourceError);
        var results = (request.Targets ?? [])
            .Select(target => InjectTarget(target, source, sourceError))
            .ToArray();

        return new OptiScalerPayloadOptiPatcherInjectionResult
        {
            SourcePath = source,
            SourceErrorCode = sourceError,
            Targets = results
        };
    }

    private static OptiScalerPayloadOptiPatcherInjectionTargetResult InjectTarget(
        OptiScalerPayloadOptiPatcherInjectionTarget target,
        string sourcePath,
        string sourceError)
    {
        var payloadDirectory = (target.PayloadDirectory ?? "").Trim();
        var destination = Path.Combine(
            payloadDirectory,
            OptiScalerInstallLayout.LibraryDirectory,
            "plugins",
            OptiScalerInstallLayout.OptiPatcherFileName);

        if (string.IsNullOrWhiteSpace(payloadDirectory) || !Directory.Exists(payloadDirectory))
        {
            return Result(target, destination, ready: false, injected: false, usedExisting: false, PayloadMissing);
        }

        if (!HasRequiredOptiScalerPayload(payloadDirectory))
        {
            return File.Exists(destination)
                ? Result(target, destination, ready: true, injected: false, usedExisting: true, PayloadInvalid)
                : Result(target, destination, ready: false, injected: false, usedExisting: false, PayloadInvalid);
        }

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return File.Exists(destination)
                ? Result(target, destination, ready: true, injected: false, usedExisting: true, sourceError)
                : Result(target, destination, ready: false, injected: false, usedExisting: false, sourceError);
        }

        try
        {
            var pluginsDirectory = Path.GetDirectoryName(destination)!;
            Directory.CreateDirectory(pluginsDirectory);
            CleanupExistingOptiPatcherFiles(pluginsDirectory, destination);
            if (File.Exists(destination))
            {
                EnsureWritable(destination);
            }

            File.Copy(sourcePath, destination, overwrite: true);
            return Result(target, destination, ready: true, injected: true, usedExisting: false, "");
        }
        catch
        {
            return File.Exists(destination)
                ? Result(target, destination, ready: true, injected: false, usedExisting: true, CopyFailed)
                : Result(target, destination, ready: false, injected: false, usedExisting: false, CopyFailed);
        }
    }

    private static OptiScalerPayloadOptiPatcherInjectionTargetResult Result(
        OptiScalerPayloadOptiPatcherInjectionTarget target,
        string destination,
        bool ready,
        bool injected,
        bool usedExisting,
        string errorCode)
    {
        return new OptiScalerPayloadOptiPatcherInjectionTargetResult
        {
            Variant = target.Variant,
            CacheEntryName = target.CacheEntryName,
            PayloadDirectory = target.PayloadDirectory,
            DestinationPath = destination,
            Ready = ready,
            Injected = injected,
            UsedExisting = usedExisting,
            ErrorCode = errorCode
        };
    }

    private static string ResolveOptiPatcherSource(string sourceDirectory, out string errorCode)
    {
        errorCode = "";
        var normalized = (sourceDirectory ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized) || !Directory.Exists(normalized))
        {
            errorCode = SourceMissing;
            return "";
        }

        var candidates = Directory
            .EnumerateFiles(normalized, "*.asi", SearchOption.AllDirectories)
            .Where(static path => IsOptiPatcherAsi(Path.GetFileName(path)))
            .ToArray();
        if (candidates.Length == 0)
        {
            errorCode = OptiPatcherMissing;
            return "";
        }

        var exact = candidates
            .Where(static path => string.Equals(
                Path.GetFileName(path),
                OptiScalerInstallLayout.OptiPatcherFileName,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (exact.Length == 1)
        {
            return exact[0];
        }

        if (candidates.Length == 1)
        {
            return candidates[0];
        }

        errorCode = MultipleCandidates;
        return "";
    }

    private static bool HasRequiredOptiScalerPayload(string payloadDirectory)
    {
        return File.Exists(Path.Combine(payloadDirectory, OptiScalerInstallLayout.RootDllFileName))
               && File.Exists(Path.Combine(payloadDirectory, OptiScalerInstallLayout.RootIniFileName))
               && Directory.Exists(Path.Combine(payloadDirectory, OptiScalerInstallLayout.LibraryDirectory));
    }

    private static void CleanupExistingOptiPatcherFiles(string pluginsDirectory, string destination)
    {
        if (!Directory.Exists(pluginsDirectory))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(pluginsDirectory, "*.asi", SearchOption.TopDirectoryOnly))
        {
            if (string.Equals(Path.GetFullPath(file), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!IsOptiPatcherAsi(Path.GetFileName(file)))
            {
                continue;
            }

            EnsureWritable(file);
            File.Delete(file);
        }
    }

    private static bool IsOptiPatcherAsi(string fileName)
    {
        if (!string.Equals(Path.GetExtension(fileName), ".asi", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var stem = Path.GetFileNameWithoutExtension(fileName);
        return stem.Contains("optipatcher", StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureWritable(string path)
    {
        var attributes = File.GetAttributes(path);
        if ((attributes & FileAttributes.ReadOnly) != 0)
        {
            File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
        }
    }
}
