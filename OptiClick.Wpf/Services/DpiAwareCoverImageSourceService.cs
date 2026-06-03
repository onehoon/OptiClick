using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;

namespace OptiClick.Wpf.Services;

public static class DpiAwareCoverImageSourceService
{
    public static ImageSource CreateOrDefault(
        string? source,
        double targetWidthDip,
        double targetHeightDip,
        Visual? dpiReference = null,
        string? defaultCoverSource = null,
        string? sourceUrlForCache = null,
        string? steamAppId = null,
        bool allowOriginalCacheFallback = true)
    {
        var safeSource = (source ?? string.Empty).Trim();
        var safeSourceUrlForCache = (sourceUrlForCache ?? string.Empty).Trim();
        var safeDefault = string.IsNullOrWhiteSpace(defaultCoverSource)
            ? CoverImageCacheService.DefaultCoverImageSource
            : defaultCoverSource.Trim();

        var coverSource = ResolveLocalCoverSource(
            safeSource,
            safeSourceUrlForCache,
            steamAppId,
            allowOriginalCacheFallback);

        if (TryCreateCoverImageSource(coverSource, targetWidthDip, targetHeightDip, dpiReference, out var imageSource))
        {
            return imageSource!;
        }

        if (string.Equals(coverSource, safeDefault, StringComparison.OrdinalIgnoreCase))
        {
            return CreateFallbackSource(safeDefault);
        }

        if (TryCreateCoverImageSource(safeDefault, targetWidthDip, targetHeightDip, dpiReference, out var fallbackSource))
        {
            return fallbackSource!;
        }

        return CreateFallbackSource(safeDefault);
    }

    private static string ResolveLocalCoverSource(
        string source,
        string sourceUrlForCache,
        string? steamAppId,
        bool allowOriginalCacheFallback)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return source;
        }

        var localSource = source;
        var cacheKey = string.IsNullOrWhiteSpace(sourceUrlForCache) ? source : sourceUrlForCache;
        if (allowOriginalCacheFallback && !string.IsNullOrWhiteSpace(cacheKey))
        {
            var cachedSourcePath = CoverImageCacheService.TryGetCachedPath(cacheKey, steamAppId);
            if (!string.IsNullOrWhiteSpace(cachedSourcePath))
            {
                localSource = new Uri(cachedSourcePath, UriKind.Absolute).AbsoluteUri;
            }
        }

        return localSource;
    }

    private static bool TryCreateCoverImageSource(
        string source,
        double targetWidthDip,
        double targetHeightDip,
        Visual? dpiReference,
        out ImageSource? imageSource)
    {
        imageSource = null;
        if (string.IsNullOrWhiteSpace(source))
        {
            return false;
        }

        if (!Uri.TryCreate(source, UriKind.Absolute, out var uri))
        {
            return false;
        }

        try
        {
            var decodeSize = ResolveDecodePixelSize(targetWidthDip, targetHeightDip, dpiReference);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = uri;
            bitmap.CacheOption = IsRemoteUri(uri)
                ? BitmapCacheOption.Default
                : BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            if (decodeSize.Width > 0)
            {
                bitmap.DecodePixelWidth = decodeSize.Width;
            }

            if (decodeSize.Height > 0)
            {
                bitmap.DecodePixelHeight = decodeSize.Height;
            }

            bitmap.EndInit();
            bitmap.Freeze();
            imageSource = bitmap;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static ImageSource CreateFallbackSource(string source)
    {
        try
        {
            var bitmap = new BitmapImage(new Uri(source, UriKind.Absolute));
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return new DrawingImage();
        }
    }

    private static bool IsRemoteUri(Uri uri)
    {
        return uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
               || uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase);
    }

    private static (int Width, int Height) ResolveDecodePixelSize(
        double targetWidthDip,
        double targetHeightDip,
        Visual? dpiReference)
    {
        if (targetWidthDip <= 0 || targetHeightDip <= 0)
        {
            return (0, 0);
        }

        var dpiScale = GetDpiScale(dpiReference);
        var width = Math.Max(1, (int)Math.Ceiling(targetWidthDip * dpiScale.DpiScaleX));
        var height = Math.Max(1, (int)Math.Ceiling(targetHeightDip * dpiScale.DpiScaleY));

        if (targetWidthDip <= targetHeightDip)
        {
            return (0, height);
        }

        return (width, 0);
    }

    private static DpiScale GetDpiScale(Visual? visual)
    {
        if (visual is null)
        {
            return new DpiScale(1.0, 1.0);
        }

        try
        {
            var dispatcher = visual.Dispatcher;
            if (dispatcher is not null && !dispatcher.CheckAccess())
            {
                return dispatcher.Invoke(() => VisualTreeHelper.GetDpi(visual));
            }

            return VisualTreeHelper.GetDpi(visual);
        }
        catch
        {
            return new DpiScale(1.0, 1.0);
        }
    }
}
