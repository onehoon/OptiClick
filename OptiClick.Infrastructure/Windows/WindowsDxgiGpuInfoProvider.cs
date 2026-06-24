using System.Globalization;
using System.Runtime.InteropServices;
using OptiClick.Core.Runtime;
using OptiClick.Infrastructure.Logging;

namespace OptiClick.Infrastructure.Windows;

internal interface IWindowsDxgiGpuInfoQuery
{
    WindowsDxgiGpuQueryResult Query();
}

internal sealed class WindowsDxgiGpuInfoProvider : IWindowsDxgiGpuInfoQuery
{
    private const int DxgiErrorNotFound = unchecked((int)0x887A0002);
    private const uint DxgiAdapterFlagSoftware = 0x2;
    private static readonly Guid IdxgiFactory1Guid = new("770aae78-f26f-4dba-a829-253c83d1b387");

    private readonly IAppLogger _logger;

    public WindowsDxgiGpuInfoProvider(IAppLogger? logger = null)
    {
        _logger = logger ?? NullAppLogger.Instance;
    }

    public WindowsDxgiGpuQueryResult Query()
    {
        if (!OperatingSystem.IsWindows())
        {
            return new WindowsDxgiGpuQueryResult { Status = WindowsDxgiQueryStatuses.NonWindows };
        }

        var factory = IntPtr.Zero;
        var adapters = new List<GpuInfo>();
        var adapterCount = 0;
        try
        {
            var iid = IdxgiFactory1Guid;
            var createResult = CreateDXGIFactory1(ref iid, out factory);
            if (createResult < 0 || factory == IntPtr.Zero)
            {
                return new WindowsDxgiGpuQueryResult
                {
                    Status = WindowsDxgiQueryStatuses.Exception,
                    ErrorType = $"CreateDXGIFactory1:0x{createResult:X8}"
                };
            }

            var enumAdapters = GetComMethod<EnumAdapters1Delegate>(factory, 12);
            for (var index = 0u; index < 32; index++)
            {
                var enumResult = enumAdapters(factory, index, out var adapter);
                if (enumResult == DxgiErrorNotFound)
                {
                    break;
                }

                if (enumResult < 0 || adapter == IntPtr.Zero)
                {
                    continue;
                }

                try
                {
                    adapterCount++;
                    var adapterInfo = ReadAdapterInfo(adapter, (int)index);
                    LogAdapter(adapterInfo);
                    if (adapterInfo.IsCandidate)
                    {
                        adapters.Add(adapterInfo.Gpu);
                    }
                }
                finally
                {
                    Marshal.Release(adapter);
                }
            }
        }
        catch (Exception exception)
        {
            return new WindowsDxgiGpuQueryResult
            {
                Status = WindowsDxgiQueryStatuses.Exception,
                AdapterCount = adapterCount,
                ErrorType = exception.GetType().Name
            };
        }
        finally
        {
            if (factory != IntPtr.Zero)
            {
                Marshal.Release(factory);
            }
        }

        return new WindowsDxgiGpuQueryResult
        {
            Gpus = Deduplicate(adapters),
            Status = adapters.Count == 0 ? WindowsDxgiQueryStatuses.Empty : WindowsDxgiQueryStatuses.Success,
            AdapterCount = adapterCount
        };
    }

    private static WindowsDxgiAdapterInfo ReadAdapterInfo(IntPtr adapter, int index)
    {
        var getDesc = GetComMethod<GetDesc1Delegate>(adapter, 10);
        var result = getDesc(adapter, out var desc);
        if (result < 0)
        {
            return new WindowsDxgiAdapterInfo
            {
                Index = index,
                Name = "",
                Vendor = "Unknown",
                IsSoftware = false,
                IsCandidate = false
            };
        }

        var name = NormalizeSpace(desc.Description);
        var vendor = ResolveVendor(desc.VendorId);
        var isSoftware = (desc.Flags & DxgiAdapterFlagSoftware) != 0 || IsBasicOrRemoteAdapter(name);
        var isCandidate = !string.IsNullOrWhiteSpace(name)
                          && !isSoftware
                          && IsSupportedVendor(vendor);
        return new WindowsDxgiAdapterInfo
        {
            Index = index,
            Name = name,
            Vendor = vendor,
            IsSoftware = isSoftware,
            IsCandidate = isCandidate,
            DedicatedVideoMemory = desc.DedicatedVideoMemory,
            Gpu = new GpuInfo
            {
                Name = string.IsNullOrWhiteSpace(name) ? "Unknown GPU" : name,
                Vendor = vendor,
                AdapterId = BuildAdapterId(desc),
                IsPrimary = false
            }
        };
    }

    private void LogAdapter(WindowsDxgiAdapterInfo adapter)
    {
        _logger.Info(
            "runtime-gpu",
            $"gpu dxgi adapter index={adapter.Index} vendor={NormalizeLogValue(adapter.Vendor, "unknown")} name=\"{NormalizeLogValue(adapter.Name, "unknown")}\" software={adapter.IsSoftware.ToString().ToLowerInvariant()} vram_gb={FormatGiB(adapter.DedicatedVideoMemory)}");
    }

    private static IReadOnlyList<GpuInfo> Deduplicate(IReadOnlyList<GpuInfo> gpus)
    {
        if (gpus.Count == 0)
        {
            return [];
        }

        var result = new List<GpuInfo>(gpus.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var gpu in gpus)
        {
            var key = string.IsNullOrWhiteSpace(gpu.AdapterId)
                ? $"{gpu.Vendor}|{NormalizeSpace(gpu.Name)}"
                : gpu.AdapterId.Trim();
            if (seen.Add(key))
            {
                result.Add(gpu);
            }
        }

        return result;
    }

    private static string BuildAdapterId(DxgiAdapterDesc1 desc)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "DXGI\\VEN_{0:X4}&DEV_{1:X4}&SUBSYS_{2:X8}&REV_{3:X2}&LUID_{4:X8}{5:X8}",
            desc.VendorId,
            desc.DeviceId,
            desc.SubSysId,
            desc.Revision,
            desc.AdapterLuid.HighPart,
            desc.AdapterLuid.LowPart);
    }

    private static string ResolveVendor(uint vendorId)
    {
        return vendorId switch
        {
            0x10DE => "NVIDIA",
            0x1002 => "AMD",
            0x8086 => "Intel",
            _ => "Unknown"
        };
    }

    private static bool IsSupportedVendor(string vendor)
    {
        return string.Equals(vendor, "NVIDIA", StringComparison.OrdinalIgnoreCase)
               || string.Equals(vendor, "AMD", StringComparison.OrdinalIgnoreCase)
               || string.Equals(vendor, "Intel", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBasicOrRemoteAdapter(string name)
    {
        return Contains(name, "Microsoft Basic Render Driver")
               || Contains(name, "Microsoft Basic Display Adapter")
               || Contains(name, "Microsoft Remote Display Adapter")
               || Contains(name, "Remote Display")
               || Contains(name, "Indirect Display");
    }

    private static bool Contains(string source, string value)
    {
        return (source ?? "").Contains(value, StringComparison.OrdinalIgnoreCase);
    }

    private static T GetComMethod<T>(IntPtr comObject, int index)
        where T : Delegate
    {
        var vtable = Marshal.ReadIntPtr(comObject);
        var method = Marshal.ReadIntPtr(vtable, index * IntPtr.Size);
        return Marshal.GetDelegateForFunctionPointer<T>(method);
    }

    private static string NormalizeSpace(string? value)
    {
        return string.Join(" ", (value ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    private static string NormalizeLogValue(string? value, string fallback)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private static string FormatGiB(nuint bytes)
    {
        if (bytes == 0)
        {
            return "0";
        }

        var value = (double)bytes / 1024d / 1024d / 1024d;
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    [DllImport("dxgi.dll")]
    private static extern int CreateDXGIFactory1(ref Guid riid, out IntPtr ppFactory);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int EnumAdapters1Delegate(IntPtr self, uint adapter, out IntPtr ppAdapter);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetDesc1Delegate(IntPtr self, out DxgiAdapterDesc1 desc);
}

internal static class WindowsDxgiQueryStatuses
{
    public const string Success = "success";
    public const string Empty = "empty";
    public const string Exception = "exception";
    public const string NonWindows = "non_windows";
    public const string NotAttempted = "not_attempted";
    public const string UnsupportedOnly = "unsupported_only";
}

internal sealed record WindowsDxgiGpuQueryResult
{
    public IReadOnlyList<GpuInfo> Gpus { get; init; } = [];
    public string Status { get; init; } = "";
    public int AdapterCount { get; init; }
    public string ErrorType { get; init; } = "";
}

internal sealed record WindowsDxgiAdapterInfo
{
    public int Index { get; init; }
    public string Name { get; init; } = "";
    public string Vendor { get; init; } = "";
    public bool IsSoftware { get; init; }
    public bool IsCandidate { get; init; }
    public nuint DedicatedVideoMemory { get; init; }
    public GpuInfo Gpu { get; init; } = new();
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct DxgiAdapterDesc1
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string Description;
    public uint VendorId;
    public uint DeviceId;
    public uint SubSysId;
    public uint Revision;
    public nuint DedicatedVideoMemory;
    public nuint DedicatedSystemMemory;
    public nuint SharedSystemMemory;
    public Luid AdapterLuid;
    public uint Flags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Luid
{
    public uint LowPart;
    public int HighPart;
}
