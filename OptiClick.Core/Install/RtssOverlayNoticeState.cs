namespace OptiClick.Core.Install;

public interface IRtssOverlayNoticeStateProvider
{
    bool IsNoticeRequired();
}

public sealed class NoopRtssOverlayNoticeStateProvider : IRtssOverlayNoticeStateProvider
{
    public static NoopRtssOverlayNoticeStateProvider Instance { get; } = new();

    private NoopRtssOverlayNoticeStateProvider()
    {
    }

    public bool IsNoticeRequired() => false;
}
