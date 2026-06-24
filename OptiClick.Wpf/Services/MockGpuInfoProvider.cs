using OptiClick.Core.Runtime;
using OptiClick.Core.Abstractions;

namespace OptiClick.Wpf.Services;

public sealed class MockGpuInfoProvider : IGpuInfoProvider, IRuntimeHardwareDetectionInfoProvider
{
    private readonly IReadOnlyList<GpuInfo> _gpus;
    private readonly RuntimeHardwareDetectionInfo _detectionInfo;

    public MockGpuInfoProvider()
        : this(
            [
                new GpuInfo
                {
                    Name = "AMD Radeon 780M",
                    Vendor = "AMD",
                    AdapterId = "GPU-AMD-780M",
                    IsPrimary = true
                }
            ])
    {
    }

    public MockGpuInfoProvider(IReadOnlyList<GpuInfo> gpus)
        : this(
            gpus,
            new RuntimeHardwareDetectionInfo
            {
                GpuInfoSource = "mock",
                WmiGpuStatus = "success"
            })
    {
    }

    public MockGpuInfoProvider(IReadOnlyList<GpuInfo> gpus, RuntimeHardwareDetectionInfo detectionInfo)
    {
        _gpus = gpus ?? Array.Empty<GpuInfo>();
        _detectionInfo = NormalizeDetectionInfo(detectionInfo);
    }

    public static MockGpuInfoProvider CreateDualGpuSample()
    {
        return new MockGpuInfoProvider(
            [
                new GpuInfo
                {
                    Name = "AMD Radeon 780M",
                    Vendor = "AMD",
                    AdapterId = "GPU-AMD-780M",
                    IsPrimary = true
                },
                new GpuInfo
                {
                    Name = "NVIDIA GeForce RTX 4070",
                    Vendor = "NVIDIA",
                    AdapterId = "GPU-NVIDIA-4070",
                    IsPrimary = false
                }
            ]);
    }

    public IReadOnlyList<GpuInfo> GetGpus()
    {
        return _gpus;
    }

    public RuntimeHardwareDetectionInfo GetHardwareDetectionInfo()
    {
        return _detectionInfo;
    }

    private static RuntimeHardwareDetectionInfo NormalizeDetectionInfo(RuntimeHardwareDetectionInfo? detectionInfo)
    {
        var detection = detectionInfo ?? new RuntimeHardwareDetectionInfo();
        return new RuntimeHardwareDetectionInfo
        {
            GpuInfoSource = (detection.GpuInfoSource ?? "").Trim(),
            WmiGpuStatus = (detection.WmiGpuStatus ?? "").Trim(),
            WmiGpuErrorType = (detection.WmiGpuErrorType ?? "").Trim(),
            WmiGpuAttempts = Math.Max(0, detection.WmiGpuAttempts),
            DxgiGpuStatus = (detection.DxgiGpuStatus ?? "").Trim(),
            DxgiGpuCount = Math.Max(0, detection.DxgiGpuCount),
            GpuDetectionErrorType = (detection.GpuDetectionErrorType ?? "").Trim()
        };
    }
}
