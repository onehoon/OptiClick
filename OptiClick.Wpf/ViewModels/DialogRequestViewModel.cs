using System;
using System.Windows;
using System.Windows.Media;
using System.Text.RegularExpressions;
using OptiClick.Wpf.Models;
using OptiClick.Wpf.Shell.Dialogs.Markup;

namespace OptiClick.Wpf.ViewModels;

public sealed class DialogRequestViewModel : ViewModelBase
{
    private static readonly Regex StartupWarningDotTokenRegex = new(@"\[\s*DOT\s*\][ \t]*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Brush NvidiaVendorTintBrush = CreateBrush(0x1E, 0x2A, 0x1F);
    private static readonly Brush AmdVendorTintBrush = CreateBrush(0x2A, 0x17, 0x1A);
    private static readonly Brush IntelVendorTintBrush = CreateBrush(0x12, 0x24, 0x3A);
    private static readonly Brush UnknownVendorTintBrush = CreateBrush(0x1F, 0x2A, 0x37);
    private static readonly Brush NvidiaVendorBorderBrush = CreateBrush(0x76, 0xB9, 0x00);
    private static readonly Brush AmdVendorBorderBrush = CreateBrush(0xF8, 0x71, 0x71);
    private static readonly Brush IntelVendorBorderBrush = CreateBrush(0x2B, 0x8F, 0xEA);
    private static readonly Brush UnknownVendorBorderBrush = CreateBrush(0x6B, 0x72, 0x80);
    private bool _isAcknowledged;
    private readonly AppDialogResult _primaryResult;
    private readonly AppDialogResult _secondaryResult;
    private readonly PopupMarkupDialogPresentation _markupPresentation;
    private readonly IReadOnlyList<string> _bulletPlainTexts;
    private readonly string _newGameSupportTitle;
    private readonly IReadOnlyList<string> _newGameSupportItems;
    private readonly IReadOnlyList<PopupMarkupBulletItem> _startupWarningItems;

    public DialogRequestViewModel(AppDialogRequest request)
    {
        Request = request;
        _primaryResult = request.PrimaryResult == AppDialogResult.None
            ? CreateDefaultPrimaryResult(request)
            : request.PrimaryResult;
        _secondaryResult = request.SecondaryResult == AppDialogResult.None
            ? CreateDefaultSecondaryResult(request)
            : request.SecondaryResult;

        _markupPresentation = PopupMarkupDialogPresentationBuilder.Build(request);
        _bulletPlainTexts = _markupPresentation.BulletItems
            .Select(static item => item.PlainText)
            .ToArray();

        var startupSections = request.Sections ?? Array.Empty<AppDialogSection>();
        var newGameSupportSection = startupSections
            .FirstOrDefault(static section => section.Kind == AppDialogSectionKind.NewGameSupport);
        _newGameSupportTitle = (newGameSupportSection?.Title ?? "").Trim();
        _newGameSupportItems = (newGameSupportSection?.Items ?? Array.Empty<string>())
            .Select(static item => (item ?? "").Trim())
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .ToArray();

        var startupWarningSourceItems = startupSections
            .Where(static section => section.Kind == AppDialogSectionKind.StartupWarning)
            .SelectMany(static section => section.Items ?? Array.Empty<string>());
        _startupWarningItems = BuildStructuredBullets(startupWarningSourceItems);
    }

    public AppDialogRequest Request { get; }

    public string Title => Request.Title;

    public string Summary => _markupPresentation.Summary.PlainText;

    public IReadOnlyList<PopupMarkupInlineSegment> SummaryInline => _markupPresentation.Summary.Inline;

    public IReadOnlyList<PopupMarkupBulletItem> ResolvedBulletItems => _markupPresentation.BulletItems;

    public IReadOnlyList<string> BulletItems => _bulletPlainTexts;

    public bool HasBullets => ResolvedBulletItems.Count > 0;

    public bool HasSummary => !string.IsNullOrWhiteSpace(Summary);

    public string PrimaryButtonText =>
        !string.IsNullOrWhiteSpace(Request.PrimaryButtonText)
            ? Request.PrimaryButtonText
            : Request.Kind switch
            {
                AppDialogKind.Warning => "Continue",
                AppDialogKind.GpuSelection => "Select",
                AppDialogKind.InstallConfirmation => "Start install",
                AppDialogKind.ModConflict => "I understand",
                AppDialogKind.Success => "Close",
                AppDialogKind.AboutPrivacy => "Close",
                _ when IsBlocking => "OK",
                _ => "OK"
            };

    public string SecondaryButtonText =>
        !string.IsNullOrWhiteSpace(Request.SecondaryButtonText)
            ? Request.SecondaryButtonText
            : Request.Kind switch
            {
                AppDialogKind.Warning => "Cancel",
                AppDialogKind.GpuSelection => "Cancel",
                AppDialogKind.InstallConfirmation => "Cancel",
                AppDialogKind.ModConflict when !IsBlocking => "Cancel",
                _ => ""
            };

    public bool HasSecondaryButton => !string.IsNullOrWhiteSpace(SecondaryButtonText);

    public bool RequiresAcknowledgement => Request.RequiresAcknowledgement;

    public string AcknowledgementText => Request.AcknowledgementText;

    public bool IsAcknowledged
    {
        get => _isAcknowledged;
        set
        {
            if (SetProperty(ref _isAcknowledged, value))
            {
                OnPropertyChanged(nameof(IsPrimaryEnabled));
            }
        }
    }

    public bool IsPrimaryEnabled => !RequiresAcknowledgement || IsAcknowledged;

    public bool IsBlocking => Request.IsBlocking || Request.Kind == AppDialogKind.Blocking;

    public bool CanClose => Request.CanClose && !IsBlocking;

    public bool CloseOnOverlayClick => Request.CloseOnOverlayClick && CanClose;

    public bool IsGpuSelectionDialog => Request.Kind == AppDialogKind.GpuSelection;

    public bool IsStartupAnnouncementDialog => Request.Kind == AppDialogKind.StartupAnnouncement;

    public bool HasNewGameSupportSection => IsStartupAnnouncementDialog && !string.IsNullOrWhiteSpace(_newGameSupportTitle) && _newGameSupportItems.Count > 0;

    public string NewGameSupportTitle => _newGameSupportTitle;

    public IReadOnlyList<string> NewGameSupportItems => _newGameSupportItems;

    public bool HasStartupWarningItems => IsStartupAnnouncementDialog && _startupWarningItems.Count > 0;

    public IReadOnlyList<PopupMarkupBulletItem> StartupWarningItems => _startupWarningItems;

    public Brush PrimaryVendorButtonBackgroundBrush => ResolveVendorTintBrush(PrimaryButtonText);

    public Brush SecondaryVendorButtonBackgroundBrush => ResolveVendorTintBrush(SecondaryButtonText);

    public Brush PrimaryVendorButtonBorderBrush => ResolveVendorBorderBrush(PrimaryButtonText);

    public Brush SecondaryVendorButtonBorderBrush => ResolveVendorBorderBrush(SecondaryButtonText);

    public string FooterText => Request.FooterText;

    public bool HasFooterText => !string.IsNullOrWhiteSpace(FooterText);

    public string ErrorCode => Request.ErrorCode;

    public bool HasErrorCode => !string.IsNullOrWhiteSpace(ErrorCode);

    public string SeverityIconGlyph =>
        !string.IsNullOrWhiteSpace(Request.IconGlyphOverride)
            ? Request.IconGlyphOverride
            : Request.Severity switch
            {
                DialogSeverity.Warning => "\uE7BA",
                DialogSeverity.Blocking => "\uE783",
                DialogSeverity.Success => "\uE930",
                _ => "\uE946"
            };

    public string SeverityLabel => Request.Severity.ToString();

    public Brush SeverityBrush => Request.Severity switch
    {
        DialogSeverity.Warning => CreateBrush(0xE7, 0xB6, 0x5E),
        DialogSeverity.Blocking => CreateBrush(0xE0, 0x6C, 0x75),
        DialogSeverity.Success => CreateBrush(0x5C, 0xC8, 0xA7),
        _ => CreateBrush(0x9A, 0xD0, 0xF5)
    };

    public Brush SeveritySoftBrush => Request.Severity switch
    {
        DialogSeverity.Warning => CreateBrush(0x34, 0x2C, 0x20),
        DialogSeverity.Blocking => CreateBrush(0x36, 0x22, 0x27),
        DialogSeverity.Success => CreateBrush(0x1F, 0x31, 0x2B),
        _ => CreateBrush(0x1F, 0x2C, 0x35)
    };

    public Visibility AcknowledgementVisibility => RequiresAcknowledgement ? Visibility.Visible : Visibility.Collapsed;

    public AppDialogResult PrimaryResult => _primaryResult;

    public AppDialogResult SecondaryResult => _secondaryResult;

    private static SolidColorBrush CreateBrush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

    private static AppDialogResult CreateDefaultPrimaryResult(AppDialogRequest request)
    {
        if (request.IsBlocking || request.Kind == AppDialogKind.Blocking)
        {
            return AppDialogResult.Ok;
        }

        return request.Kind switch
        {
            AppDialogKind.Warning => AppDialogResult.Continue,
            AppDialogKind.GpuSelection => AppDialogResult.Continue,
            AppDialogKind.InstallConfirmation => AppDialogResult.StartInstall,
            AppDialogKind.ModConflict => AppDialogResult.Acknowledge,
            AppDialogKind.Success => AppDialogResult.Close,
            AppDialogKind.AboutPrivacy => AppDialogResult.Close,
            _ => AppDialogResult.Ok
        };
    }

    private static AppDialogResult CreateDefaultSecondaryResult(AppDialogRequest request)
    {
        if (request.IsBlocking || request.Kind == AppDialogKind.Blocking)
        {
            return AppDialogResult.None;
        }

        return request.Kind switch
        {
            AppDialogKind.Warning => AppDialogResult.Cancel,
            AppDialogKind.GpuSelection => AppDialogResult.Cancel,
            AppDialogKind.InstallConfirmation => AppDialogResult.Cancel,
            AppDialogKind.ModConflict => AppDialogResult.Cancel,
            _ => AppDialogResult.None
        };
    }

    private static IReadOnlyList<PopupMarkupBulletItem> BuildStructuredBullets(IEnumerable<string> sourceItems)
    {
        var merged = new List<PopupMarkupBulletItem>();

        foreach (var sourceItem in sourceItems)
        {
            if (string.IsNullOrWhiteSpace(sourceItem))
            {
                continue;
            }

            var normalizedSourceItem = NormalizeStartupWarningMarkup(sourceItem);
            var parsed = PopupMarkupParser.Parse(normalizedSourceItem);
            if (parsed.BulletItems.Count > 0)
            {
                merged.AddRange(parsed.BulletItems);
            }

            if (!string.IsNullOrWhiteSpace(parsed.PlainText))
            {
                merged.Add(new PopupMarkupBulletItem
                {
                    Inline = parsed.Inline,
                    PlainText = parsed.PlainText
                });
            }
        }

        return merged;
    }

    private static string NormalizeStartupWarningMarkup(string sourceItem)
    {
        var normalized = (sourceItem ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "";
        }

        return StartupWarningDotTokenRegex.Replace(normalized, "\u2022 ");
    }

    private static Brush ResolveVendorTintBrush(string? text)
    {
        return ResolveVendorKind(text) switch
        {
            GpuVendorKind.Nvidia => NvidiaVendorTintBrush,
            GpuVendorKind.Amd => AmdVendorTintBrush,
            GpuVendorKind.Intel => IntelVendorTintBrush,
            _ => UnknownVendorTintBrush
        };
    }

    private static Brush ResolveVendorBorderBrush(string? text)
    {
        return ResolveVendorKind(text) switch
        {
            GpuVendorKind.Nvidia => NvidiaVendorBorderBrush,
            GpuVendorKind.Amd => AmdVendorBorderBrush,
            GpuVendorKind.Intel => IntelVendorBorderBrush,
            _ => UnknownVendorBorderBrush
        };
    }

    private static GpuVendorKind ResolveVendorKind(string? text)
    {
        var normalized = (text ?? "").Trim();
        if (normalized.Contains("nvidia", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("geforce", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("rtx", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("gtx", StringComparison.OrdinalIgnoreCase))
        {
            return GpuVendorKind.Nvidia;
        }

        if (normalized.Contains("amd", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("radeon", StringComparison.OrdinalIgnoreCase))
        {
            return GpuVendorKind.Amd;
        }

        if (normalized.Contains("intel", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("arc", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("iris", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("xe", StringComparison.OrdinalIgnoreCase))
        {
            return GpuVendorKind.Intel;
        }

        return GpuVendorKind.Unknown;
    }

    private enum GpuVendorKind
    {
        Unknown,
        Nvidia,
        Amd,
        Intel
    }
}
