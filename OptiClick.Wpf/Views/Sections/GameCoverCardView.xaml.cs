using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

namespace OptiClick.Wpf.Views.Sections;

public partial class GameCoverCardView : UserControl
{
    public static readonly DependencyProperty SelectGameCommandProperty = DependencyProperty.Register(
        nameof(SelectGameCommand),
        typeof(ICommand),
        typeof(GameCoverCardView),
        new PropertyMetadata(null));

    public GameCoverCardView()
    {
        InitializeComponent();
    }

    public ICommand? SelectGameCommand
    {
        get => (ICommand?)GetValue(SelectGameCommandProperty);
        set => SetValue(SelectGameCommandProperty, value);
    }
}
