using OptiClick.Core.Install;
using OptiClick.Wpf.Install.Execution;

namespace OptiClick.Wpf.Install.Precheck;

public interface IInstallPrecheckHandler
{
    InstallPrecheckResult Run(InstallPrecheckRequest request, bool useKorean = false);
}

public sealed class BaseInstallPrecheckHandler : IInstallPrecheckHandler
{
    private readonly IModPrecheckScanner _modPrecheckScanner;
    private readonly ModConflictFindingBuilder _findingBuilder;
    private readonly ReFrameworkLegacyFindingService _reFrameworkLegacyFindingService;
    private readonly IProxyDllNameResolver _proxyDllNameResolver;

    public BaseInstallPrecheckHandler(
        IModPrecheckScanner modPrecheckScanner,
        ModConflictFindingBuilder findingBuilder,
        ReFrameworkLegacyFindingService reFrameworkLegacyFindingService,
        IProxyDllNameResolver proxyDllNameResolver)
    {
        _modPrecheckScanner = modPrecheckScanner;
        _findingBuilder = findingBuilder;
        _reFrameworkLegacyFindingService = reFrameworkLegacyFindingService;
        _proxyDllNameResolver = proxyDllNameResolver;
    }

    public InstallPrecheckResult Run(InstallPrecheckRequest request, bool useKorean = false)
    {
        _ = useKorean;
        var descriptor = request.GameDescriptor ?? InstallGameDescriptor.Empty;
        var targetPath = (request.TargetPath ?? "").Trim();

        var modState = _modPrecheckScanner.Scan(targetPath);
        var conflictFindings = _findingBuilder.BuildFindings(modState);
        conflictFindings = _reFrameworkLegacyFindingService.AppendLegacyFinding(conflictFindings, targetPath, descriptor);

        var preferredDll = string.IsNullOrWhiteSpace(request.PreferredDllName)
            ? descriptor.OptiScalerDllName
            : request.PreferredDllName.Trim();
        try
        {
            var resolvedDllName = _proxyDllNameResolver.Resolve(targetPath, preferredDll);
            var noticeFindings = _findingBuilder.BuildNoticeFindings(conflictFindings, descriptor, resolvedDllName);
            return new InstallPrecheckResult
            {
                Ok = true,
                ResolvedDllName = resolvedDllName,
                ConflictFindings = conflictFindings,
                NoticeFindings = noticeFindings
            };
        }
        catch (Exception ex)
        {
            var errorCode = "";
            if (string.Equals(ex.Message, ProxyDllNameResolver.InvalidTargetFolderErrorCode, StringComparison.OrdinalIgnoreCase))
            {
                errorCode = ProxyDllNameResolver.InvalidTargetFolderErrorCode;
            }
            else if (string.Equals(ex.Message, ProxyDllNameResolver.InvalidPreferredProxyNameErrorCode, StringComparison.OrdinalIgnoreCase))
            {
                errorCode = ProxyDllNameResolver.InvalidPreferredProxyNameErrorCode;
            }

            var noticeFindings = _findingBuilder.BuildNoticeFindings(conflictFindings, descriptor, preferredDll);
            return new InstallPrecheckResult
            {
                Ok = false,
                RawErrorMessage = ex.Message,
                ErrorCode = errorCode,
                ConflictFindings = conflictFindings,
                NoticeFindings = noticeFindings
            };
        }
    }
}

public sealed class InstallPrecheckHandlerRegistry
{
    private readonly IInstallPrecheckHandler _baseHandler;

    public InstallPrecheckHandlerRegistry(IInstallPrecheckHandler baseHandler)
    {
        _baseHandler = baseHandler;
    }

    public IInstallPrecheckHandler Resolve()
    {
        // RDR2-specific precheck handler is intentionally excluded in current migration scope.
        return _baseHandler;
    }
}
