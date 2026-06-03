using System.Windows.Media;
using OptiClick.Core.Install.Summary;
using OptiClick.Core.Models;
using OptiClick.Wpf.ViewModels;

namespace OptiClick.Wpf.Services;

public sealed class ShellMockDataProvider : IShellMockDataProvider
{
    private static readonly Brush FoundStatusBrush = new SolidColorBrush(Color.FromRgb(92, 200, 167));
    private static readonly Brush AddedStatusBrush = new SolidColorBrush(Color.FromRgb(154, 208, 245));
    private static readonly Brush MissingStatusBrush = new SolidColorBrush(Color.FromRgb(168, 176, 188));

    public IReadOnlyList<GameCardViewModel> CreateGames()
    {
        return
        [
            CreateGame(
                "Cyberpunk 2077",
                "Open-world RPG",
                "Ready",
                new InstallSummaryInput
                {
                    InstallStatusCode = InstallSummaryStatusCodes.Installable,
                    CurrentDisplayVersion = "0.7.7",
                    ReframeworkUrl = "ReShade64.dll",
                    SpecialK = "plugins",
                    InstallSummaryNote = "[RED][DOT]Close the game before installation.[END]"
                },
                "#315C75",
                "#4F6FAA",
                new GameEntry { GameId = "CYBERPUNK_2077", GameNameEn = "Cyberpunk 2077", SupportAmd = true, SupportedGpu = "all", ReframeworkUrl = "ReShade64.dll" }),
            CreateGame(
                "Alan Wake 2",
                "Survival horror",
                "Latest",
                new InstallSummaryInput
                {
                    InstallStatusCode = InstallSummaryStatusCodes.UpdateAvailable,
                    InstalledVersion = "0.7.6",
                    CurrentDisplayVersion = "0.7.7",
                    UltimateAsiLoader = true,
                    InstallSummaryNote = "Existing OptiClick-managed files may be updated."
                },
                "#6B4E7D",
                "#9AD0F5",
                new GameEntry { GameId = "ALAN_WAKE_2", GameNameEn = "Alan Wake 2", SupportAmd = true, SupportedGpu = "all" }),
            CreateGame(
                "Black Myth",
                "Action RPG",
                "Update",
                new InstallSummaryInput
                {
                    InstallStatusCode = "installed",
                    InstalledVersion = "0.7.7",
                    InstallSummaryNote = "[P]Important warnings will appear before installation."
                },
                "#6D6552",
                "#E7B65E",
                new GameEntry { GameId = "BLACK_MYTH", GameNameEn = "Black Myth", SupportAmd = true, SupportedGpu = "all", Unreal5 = true }),
            CreateGame(
                "Elden Ring",
                "Fantasy action",
                "Installed",
                new InstallSummaryInput
                {
                    InstallStatusCode = InstallSummaryStatusCodes.Installable,
                    CurrentDisplayVersion = "0.7.7",
                    InstallSummaryNote = "Close the game before installation."
                },
                "#3E5B49",
                "#8BD49C",
                new GameEntry { GameId = "ELDEN_RING", GameNameEn = "Elden Ring", SupportAmd = true, SupportedGpu = "all" }),
            CreateGame(
                "Hogwarts Legacy",
                "Adventure",
                "Ready",
                new InstallSummaryInput
                {
                    InstallStatusCode = InstallSummaryStatusCodes.Installable,
                    CurrentDisplayVersion = "0.7.7",
                    SpecialK = "plugins",
                    RtssOverlay = true,
                    InstallSummaryNote = "[P]Compatibility can vary by device state."
                },
                "#5F4962",
                "#B8A6FF",
                new GameEntry { GameId = "HOGWARTS", GameNameEn = "Hogwarts Legacy", SupportAmd = true, SupportedGpu = "all", SpecialK = "plugins" }),
            CreateGame(
                "Starfield",
                "Space RPG",
                "Pre-release",
                new InstallSummaryInput
                {
                    InstallStatusCode = InstallSummaryStatusCodes.UpdateAvailable,
                    InstalledVersion = "0.7.5",
                    CurrentVersion = "0.7.7",
                    UltimateAsiLoader = true,
                    RtssOverlay = true,
                    InstallSummaryNote = "Close overlays before installation for best results."
                },
                "#35546F",
                "#77D4FF",
                new GameEntry { GameId = "STARFIELD", GameNameEn = "Starfield", SupportAmd = true, SupportedGpu = "all" }),
            CreateGame(
                "Baldur's Gate 3",
                "Party RPG",
                "Ready",
                new InstallSummaryInput
                {
                    InstallStatusCode = "already_installed",
                    InstalledVersion = "0.7.7",
                    InstallSummaryNote = "Important warnings will appear before installation."
                },
                "#714C3E",
                "#F1A77F",
                new GameEntry { GameId = "BG3", GameNameEn = "Baldur's Gate 3", SupportAmd = true, SupportedGpu = "all" }),
            CreateGame(
                "Forza Horizon 5",
                "Racing",
                "Latest",
                new InstallSummaryInput
                {
                    InstallStatusCode = InstallSummaryStatusCodes.Installable,
                    CurrentDisplayVersion = "0.7.7",
                    OptiPatcher = true,
                    RtssOverlay = true,
                    InstallSummaryNote = "Close the game before installation."
                },
                "#2F6B6A",
                "#63D7C6",
                new GameEntry { GameId = "FORZA_HORIZON_5", GameNameEn = "Forza Horizon 5", SupportAmd = true, SupportedGpu = "all" })
        ];
    }

    public IReadOnlyList<ScanFolderRowViewModel> CreateDefaultFolders()
    {
        return
        [
            CreateDefaultFolder("Steam Library", "C:/Program Files (x86)/Steam/steamapps/common", true),
            CreateDefaultFolder("Xbox Games", "C:/XboxGames", true),
            CreateDefaultFolder("Epic Games", "C:/Program Files/Epic Games", false)
        ];
    }

    public IReadOnlyList<ScanFolderRowViewModel> CreateAddedFolders()
    {
        return [];
    }

    public ScanFolderRowViewModel CreateAddedFolder(string path)
    {
        return new ScanFolderRowViewModel(
            "Custom folder",
            path,
            "Added",
            true,
            true,
            true,
            AddedStatusBrush);
    }

    private static GameCardViewModel CreateGame(
        string title,
        string subtitle,
        string badge,
        InstallSummaryInput summaryInput,
        string coverColor,
        string badgeColor,
        GameEntry gameEntry)
    {
        var summary = InstallSummaryBuilder.Build(summaryInput);
        return new GameCardViewModel(
            title,
            subtitle,
            badge,
            "",
            summary.OptiScalerText,
            summary.ComponentsText,
            summary.NoteText,
            CreateCoverBrush(coverColor),
            CreateSolidBrush(badgeColor),
            gameEntry);
    }

    private static ScanFolderRowViewModel CreateDefaultFolder(string name, string path, bool found)
    {
        return new ScanFolderRowViewModel(
            name,
            path,
            found ? "Auto detected" : "Not found",
            found,
            found,
            false,
            found ? FoundStatusBrush : MissingStatusBrush);
    }

    private static Brush CreateCoverBrush(string color)
    {
        var baseColor = (Color)ColorConverter.ConvertFromString(color);
        var light = Color.FromRgb(
            (byte)Math.Min(baseColor.R + 32, 255),
            (byte)Math.Min(baseColor.G + 32, 255),
            (byte)Math.Min(baseColor.B + 32, 255));

        return new LinearGradientBrush(baseColor, light, 45);
    }

    private static Brush CreateSolidBrush(string color)
    {
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    }
}
