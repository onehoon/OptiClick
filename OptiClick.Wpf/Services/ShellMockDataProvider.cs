using System.Windows.Media;
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
                "#315C75",
                "#4F6FAA",
                new GameEntry { GameId = "CYBERPUNK_2077", GameNameEn = "Cyberpunk 2077", SupportAmd = true, SupportedGpu = "all", ReframeworkUrl = "ReShade64.dll" }),
            CreateGame(
                "Alan Wake 2",
                "Survival horror",
                "Latest",
                "#6B4E7D",
                "#9AD0F5",
                new GameEntry { GameId = "ALAN_WAKE_2", GameNameEn = "Alan Wake 2", SupportAmd = true, SupportedGpu = "all" }),
            CreateGame(
                "Black Myth",
                "Action RPG",
                "Update",
                "#6D6552",
                "#E7B65E",
                new GameEntry { GameId = "BLACK_MYTH", GameNameEn = "Black Myth", SupportAmd = true, SupportedGpu = "all", Unreal5 = true }),
            CreateGame(
                "Elden Ring",
                "Fantasy action",
                "Installed",
                "#3E5B49",
                "#8BD49C",
                new GameEntry { GameId = "ELDEN_RING", GameNameEn = "Elden Ring", SupportAmd = true, SupportedGpu = "all" }),
            CreateGame(
                "Hogwarts Legacy",
                "Adventure",
                "Ready",
                "#5F4962",
                "#B8A6FF",
                new GameEntry { GameId = "HOGWARTS", GameNameEn = "Hogwarts Legacy", SupportAmd = true, SupportedGpu = "all", SpecialK = "plugins" }),
            CreateGame(
                "Starfield",
                "Space RPG",
                "Pre-release",
                "#35546F",
                "#77D4FF",
                new GameEntry { GameId = "STARFIELD", GameNameEn = "Starfield", SupportAmd = true, SupportedGpu = "all" }),
            CreateGame(
                "Baldur's Gate 3",
                "Party RPG",
                "Ready",
                "#714C3E",
                "#F1A77F",
                new GameEntry { GameId = "BG3", GameNameEn = "Baldur's Gate 3", SupportAmd = true, SupportedGpu = "all" }),
            CreateGame(
                "Forza Horizon 5",
                "Racing",
                "Latest",
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
        string coverColor,
        string badgeColor,
        GameEntry gameEntry)
    {
        return new GameCardViewModel(
            title,
            subtitle,
            badge,
            "",
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
