using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using OptiClick.Wpf.Shell.Dialogs.Markup;

namespace OptiClick.Wpf.Controls;

public sealed class PopupMarkupTextBlock : TextBlock
{
    private static readonly Brush DefaultEmphasisBrush = CreateDefaultEmphasisBrush();

    public static readonly DependencyProperty SegmentsProperty = DependencyProperty.Register(
        nameof(Segments),
        typeof(IReadOnlyList<PopupMarkupInlineSegment>),
        typeof(PopupMarkupTextBlock),
        new PropertyMetadata(Array.Empty<PopupMarkupInlineSegment>(), OnRenderPropertyChanged));

    public static readonly DependencyProperty EmphasisForegroundProperty = DependencyProperty.Register(
        nameof(EmphasisForeground),
        typeof(Brush),
        typeof(PopupMarkupTextBlock),
        new PropertyMetadata(DefaultEmphasisBrush, OnRenderPropertyChanged));

    public IReadOnlyList<PopupMarkupInlineSegment> Segments
    {
        get => (IReadOnlyList<PopupMarkupInlineSegment>)(GetValue(SegmentsProperty) ?? Array.Empty<PopupMarkupInlineSegment>());
        set => SetValue(SegmentsProperty, value ?? Array.Empty<PopupMarkupInlineSegment>());
    }

    public Brush EmphasisForeground
    {
        get => (Brush)(GetValue(EmphasisForegroundProperty) ?? DefaultEmphasisBrush);
        set => SetValue(EmphasisForegroundProperty, value);
    }

    private static void OnRenderPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs _)
    {
        if (dependencyObject is PopupMarkupTextBlock textBlock)
        {
            textBlock.RenderSegments();
        }
    }

    private void RenderSegments()
    {
        Inlines.Clear();

        var segments = Segments;
        if (segments is null || segments.Count == 0)
        {
            return;
        }

        foreach (var segment in segments)
        {
            if (segment.Kind == PopupMarkupInlineKind.LineBreak)
            {
                Inlines.Add(new LineBreak());
                continue;
            }

            if (string.IsNullOrEmpty(segment.Text))
            {
                continue;
            }

            var run = new Run(segment.Text);
            if (segment.IsEmphasis)
            {
                run.Foreground = EmphasisForeground;
                run.FontWeight = FontWeights.Bold;
            }

            Inlines.Add(run);
        }
    }

    private static Brush CreateDefaultEmphasisBrush()
    {
        var brush = new SolidColorBrush(Color.FromRgb(0xFF, 0xCB, 0x62));
        brush.Freeze();
        return brush;
    }
}
