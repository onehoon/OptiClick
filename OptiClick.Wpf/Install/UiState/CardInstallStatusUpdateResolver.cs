namespace OptiClick.Wpf.Install.UiState;

public sealed record CardInstallStatusUpdateInput
{
    public int? SelectedIndex { get; init; }
    public int FoundGamesCount { get; init; }
    public string SelectedPath { get; init; } = "";
    public string RefreshedPath { get; init; } = "";
    public InstallStatusSnapshot InstallStatus { get; init; } = new();
}

public sealed record CardInstallStatusUpdateDecision
{
    public bool ShouldUpdate { get; init; }
    public int SelectedIndex { get; init; } = -1;
    public InstallStatusSnapshot InstallStatus { get; init; } = new();
}

public interface ICardInstallStatusUpdateResolver
{
    CardInstallStatusUpdateDecision Resolve(CardInstallStatusUpdateInput input);
}

public sealed class CardInstallStatusUpdateResolver : ICardInstallStatusUpdateResolver
{
    public CardInstallStatusUpdateDecision Resolve(CardInstallStatusUpdateInput input)
    {
        if (!input.SelectedIndex.HasValue)
        {
            return new CardInstallStatusUpdateDecision();
        }

        var index = input.SelectedIndex.Value;
        if (index < 0 || index >= input.FoundGamesCount)
        {
            return new CardInstallStatusUpdateDecision();
        }

        var selectedPath = (input.SelectedPath ?? "").Trim();
        var refreshedPath = (input.RefreshedPath ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(selectedPath)
            && !string.IsNullOrWhiteSpace(refreshedPath)
            && !string.Equals(selectedPath, refreshedPath, StringComparison.OrdinalIgnoreCase))
        {
            return new CardInstallStatusUpdateDecision();
        }

        return new CardInstallStatusUpdateDecision
        {
            ShouldUpdate = true,
            SelectedIndex = index,
            InstallStatus = input.InstallStatus
        };
    }
}
