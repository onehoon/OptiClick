using System.Management;

namespace OptiClick.Infrastructure.Windows;

internal static class WindowsWmiQueryHelper
{
    private static readonly ManagementScope Scope = new(@"\\.\root\cimv2");
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);

    public static IReadOnlyList<T> Query<T>(
        string query,
        Func<ManagementObject, T?> projector)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(projector);

        using var searcher = new ManagementObjectSearcher(
            Scope,
            new ObjectQuery(query),
            new System.Management.EnumerationOptions
            {
                ReturnImmediately = true,
                Rewindable = false,
                Timeout = DefaultTimeout
            });

        var result = new List<T>();
        foreach (var item in searcher.Get().OfType<ManagementObject>())
        {
            var projected = projector(item);
            if (projected is not null)
            {
                result.Add(projected);
            }
        }

        return result;
    }

    public static string ReadString(ManagementBaseObject item, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(propertyName);

        return item[propertyName]?.ToString()?.Trim() ?? "";
    }
}
