using OptiClick.Core.Runtime;
using OptiClick.Core.Abstractions;

namespace OptiClick.Wpf.Services;

public sealed class MockRuntimeContextProvider : IRuntimeContextProvider
{
    private readonly IGpuInfoProvider _gpuProvider;
    private readonly IDeviceInfoProvider _deviceProvider;
    private readonly IWritableAppLanguageProvider _languageProvider;
    private readonly IRemoteEndpointProvider _remoteEndpointProvider;

    public MockRuntimeContextProvider()
        : this(
            new MockGpuInfoProvider(),
            new MockDeviceInfoProvider(),
            new MockLanguageProvider(),
            new MockRemoteEndpointProvider())
    {
    }

    public MockRuntimeContextProvider(
        IGpuInfoProvider gpuProvider,
        IDeviceInfoProvider deviceProvider,
        IWritableAppLanguageProvider languageProvider,
        IRemoteEndpointProvider remoteEndpointProvider)
    {
        _gpuProvider = gpuProvider;
        _deviceProvider = deviceProvider;
        _languageProvider = languageProvider;
        _remoteEndpointProvider = remoteEndpointProvider;
    }

    public IReadOnlyList<AppLanguage> SupportedLanguages => _languageProvider.SupportedLanguages;

    public void SetLanguage(AppLanguage language)
    {
        _languageProvider.SetLanguage(language);
    }

    public RuntimeContext GetRuntimeContext()
    {
        var gpus = _gpuProvider.GetGpus();
        return new RuntimeContext
        {
            Gpus = gpus,
            SelectedGpu = gpus.FirstOrDefault(static gpu => gpu.IsPrimary) ?? gpus.FirstOrDefault(),
            Device = _deviceProvider.GetDeviceInfo(),
            Language = _languageProvider.CurrentLanguage,
            RemoteData = _remoteEndpointProvider.GetRemoteDataOptions()
        };
    }
}
