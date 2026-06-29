using System.IO;
using OptiClick.Core.Install;
using OptiClick.Infrastructure.Archives;

namespace OptiClick.Wpf.Install.Archives;

public interface IOptiScalerPayloadAmdxc64Provisioner
{
    Task<Amdxc64ProvisionResult> EnsureAsync(
        Amdxc64ProvisionRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record Amdxc64ProvisionRequest
{
    public string ArchiveCacheRoot { get; init; } = "";
    public ModuleDownloadLinkEntry Descriptor { get; init; } = ModuleDownloadLinkEntry.Empty;
    public IReadOnlyList<Amdxc64ProvisionTarget> Targets { get; init; } = [];
}

public sealed record Amdxc64ProvisionTarget
{
    public string Variant { get; init; } = "";
    public string CacheEntryName { get; init; } = "";
    public string PayloadDirectory { get; init; } = "";
}

public sealed record Amdxc64ProvisionResult
{
    public bool DidRun { get; init; }
    public bool IsSuccess { get; init; }
    public string ErrorCode { get; init; } = "";
    public IReadOnlyList<Amdxc64ProvisionTargetResult> Targets { get; init; } = [];
}

public sealed record Amdxc64ProvisionTargetResult
{
    public string Variant { get; init; } = "";
    public string CacheEntryName { get; init; } = "";
    public string PayloadDirectory { get; init; } = "";
    public string DestinationPath { get; init; } = "";
    public bool AlreadyReady { get; init; }
    public bool Copied { get; init; }
    public bool Failed { get; init; }
    public string ErrorCode { get; init; } = "";
}

public sealed class OptiScalerPayloadAmdxc64Provisioner : IOptiScalerPayloadAmdxc64Provisioner
{
    public const string DescriptorMissing = "amdxc64_descriptor_missing";
    public const string InvalidFilename = "amdxc64_invalid_filename";
    public const string InvalidUrl = "amdxc64_invalid_url";
    public const string InvalidSha256 = "amdxc64_invalid_sha256";
    public const string ArchiveCacheRootMissing = "archive_cache_root_missing";
    public const string PayloadMissing = "optiscaler_payload_missing";
    public const string CopyFailed = "amdxc64_copy_failed";
    public const string DownloadFailed = "amdxc64_download_failed";
    public const string ShaMismatch = "amdxc64_sha_mismatch";

    private const string Amdxc64FileName = "amdxc64.dll";
    private const string Amdxc64CacheDirectoryName = "amdxc64";
    private const int MaxDownloadAttempts = 2;
    private static readonly TimeSpan DownloadRetryDelay = TimeSpan.FromMilliseconds(750);

    private readonly IArchiveDownloader _downloader;
    private readonly ArchivePreparationOptions _options;

    public OptiScalerPayloadAmdxc64Provisioner(
        IArchiveDownloader downloader,
        ArchivePreparationOptions? options = null)
    {
        _downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
        _options = options ?? new ArchivePreparationOptions();
    }

    public async Task<Amdxc64ProvisionResult> EnsureAsync(
        Amdxc64ProvisionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var targets = (request.Targets ?? [])
            .Where(static target => !string.IsNullOrWhiteSpace(target.PayloadDirectory))
            .ToArray();
        if (targets.Length == 0)
        {
            return new Amdxc64ProvisionResult
            {
                DidRun = true,
                IsSuccess = true,
                Targets = []
            };
        }

        var descriptor = request.Descriptor ?? ModuleDownloadLinkEntry.Empty;
        var descriptorError = ValidateDescriptor(descriptor);
        if (!string.IsNullOrWhiteSpace(descriptorError))
        {
            return new Amdxc64ProvisionResult
            {
                DidRun = true,
                IsSuccess = false,
                ErrorCode = descriptorError,
                Targets = targets.Select(target => TargetResult(target, "", failed: true, errorCode: descriptorError)).ToArray()
            };
        }

        var archiveCacheRoot = (request.ArchiveCacheRoot ?? "").Trim();
        if (string.IsNullOrWhiteSpace(archiveCacheRoot))
        {
            return new Amdxc64ProvisionResult
            {
                DidRun = true,
                IsSuccess = false,
                ErrorCode = ArchiveCacheRootMissing,
                Targets = targets.Select(target => TargetResult(target, "", failed: true, errorCode: ArchiveCacheRootMissing)).ToArray()
            };
        }

        var localCachePath = Path.Combine(archiveCacheRoot, Amdxc64CacheDirectoryName, Amdxc64FileName);
        var localCacheReady = IsSha256Match(localCachePath, descriptor.Sha256);
        var localCacheAttempted = localCacheReady;
        var localCacheError = "";
        var results = new List<Amdxc64ProvisionTargetResult>(targets.Length);

        foreach (var target in targets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var destination = ResolveDestinationPath(target.PayloadDirectory);
            if (IsSha256Match(destination, descriptor.Sha256))
            {
                results.Add(TargetResult(target, destination, alreadyReady: true));
                continue;
            }

            if (!Directory.Exists(target.PayloadDirectory))
            {
                results.Add(TargetResult(target, destination, failed: true, errorCode: PayloadMissing));
                continue;
            }

            if (!localCacheReady)
            {
                if (!localCacheAttempted)
                {
                    var download = await DownloadLocalCacheAsync(
                        descriptor,
                        localCachePath,
                        cancellationToken);
                    localCacheAttempted = true;
                    if (download.IsSuccess)
                    {
                        localCacheReady = true;
                    }
                    else
                    {
                        localCacheError = download.ErrorCode;
                    }
                }

                if (!localCacheReady)
                {
                    results.Add(TargetResult(
                        target,
                        destination,
                        failed: true,
                        errorCode: string.IsNullOrWhiteSpace(localCacheError) ? DownloadFailed : localCacheError));
                    continue;
                }
            }

            results.Add(CopyLocalCacheToTarget(target, localCachePath, destination, descriptor.Sha256));
        }

        return new Amdxc64ProvisionResult
        {
            DidRun = true,
            IsSuccess = results.All(static result => !result.Failed),
            ErrorCode = results.FirstOrDefault(static result => result.Failed)?.ErrorCode ?? "",
            Targets = results
        };

        async Task<(bool IsSuccess, string ErrorCode)> DownloadLocalCacheAsync(
            ModuleDownloadLinkEntry link,
            string destination,
            CancellationToken ct)
        {
            var lastError = DownloadFailed;
            for (var attempt = 1; attempt <= MaxDownloadAttempts; attempt++)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    if (File.Exists(destination))
                    {
                        EnsureWritable(destination);
                    }

                    var result = await _downloader.DownloadAsync(
                        link.Url,
                        destination,
                        _options.DownloadTimeout,
                        ct,
                        link.Sha256);
                    if (!result.IsSuccess)
                    {
                        lastError = string.IsNullOrWhiteSpace(result.ErrorCode) ? DownloadFailed : result.ErrorCode;
                    }
                    else if (IsSha256Match(destination, link.Sha256))
                    {
                        return (true, "");
                    }
                    else
                    {
                        lastError = ShaMismatch;
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch
                {
                    lastError = DownloadFailed;
                }

                if (attempt < MaxDownloadAttempts)
                {
                    await Task.Delay(DownloadRetryDelay, ct);
                }
            }

            return (false, lastError);
        }
    }

    private static string ValidateDescriptor(ModuleDownloadLinkEntry descriptor)
    {
        if (descriptor is null
            || string.IsNullOrWhiteSpace(descriptor.Url)
            || string.IsNullOrWhiteSpace(descriptor.Filename)
            || string.IsNullOrWhiteSpace(descriptor.Sha256))
        {
            return DescriptorMissing;
        }

        if (!string.Equals(Path.GetFileName(descriptor.Filename), Amdxc64FileName, StringComparison.OrdinalIgnoreCase))
        {
            return InvalidFilename;
        }

        if (!Uri.TryCreate(descriptor.Url.Trim(), UriKind.Absolute, out var uri)
            || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return InvalidUrl;
        }

        return NormalizeSha256(descriptor.Sha256).Length == 64 ? "" : InvalidSha256;
    }

    private static Amdxc64ProvisionTargetResult CopyLocalCacheToTarget(
        Amdxc64ProvisionTarget target,
        string source,
        string destination,
        string expectedSha256)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            if (File.Exists(destination))
            {
                EnsureWritable(destination);
            }

            File.Copy(source, destination, overwrite: true);
            return IsSha256Match(destination, expectedSha256)
                ? TargetResult(target, destination, copied: true)
                : TargetResult(target, destination, failed: true, errorCode: CopyFailed);
        }
        catch
        {
            return TargetResult(target, destination, failed: true, errorCode: CopyFailed);
        }
    }

    private static string ResolveDestinationPath(string payloadDirectory)
    {
        return Path.Combine(
            (payloadDirectory ?? "").Trim(),
            OptiScalerInstallLayout.LibraryDirectory,
            Amdxc64FileName);
    }

    private static Amdxc64ProvisionTargetResult TargetResult(
        Amdxc64ProvisionTarget target,
        string destination,
        bool alreadyReady = false,
        bool copied = false,
        bool failed = false,
        string errorCode = "")
    {
        return new Amdxc64ProvisionTargetResult
        {
            Variant = target.Variant,
            CacheEntryName = target.CacheEntryName,
            PayloadDirectory = target.PayloadDirectory,
            DestinationPath = destination,
            AlreadyReady = alreadyReady,
            Copied = copied,
            Failed = failed,
            ErrorCode = errorCode
        };
    }

    private static bool IsSha256Match(string path, string expectedSha256)
    {
        var expected = NormalizeSha256(expectedSha256);
        if (string.IsNullOrWhiteSpace(path)
            || string.IsNullOrWhiteSpace(expected)
            || !File.Exists(path))
        {
            return false;
        }

        try
        {
            var actual = ArchiveFileVerifier.ComputeFileSha256(path);
            return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizeSha256(string? value)
    {
        var normalized = (value ?? "").Trim();
        const string Prefix = "sha256:";
        if (normalized.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[Prefix.Length..].Trim();
        }

        normalized = normalized.ToLowerInvariant();
        return normalized.Length == 64 && normalized.All(IsHex) ? normalized : "";
    }

    private static bool IsHex(char value)
    {
        return value is >= '0' and <= '9' or >= 'a' and <= 'f';
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
