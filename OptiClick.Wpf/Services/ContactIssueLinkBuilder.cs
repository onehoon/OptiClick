using System.Text;
using OptiClick.Core.Runtime;

namespace OptiClick.Wpf.Services;

public sealed class ContactIssueContext
{
    public string AppVersion { get; init; } = "";
    public string GpuName { get; init; } = "";
    public string Manufacturer { get; init; } = "";
    public string DeviceModel { get; init; } = "";
}

public interface IContactIssueLinkBuilder
{
    string BuildIssueUrl(ContactIssueContext context, AppLanguage language);
}

public sealed class ContactIssueLinkBuilder : IContactIssueLinkBuilder
{
    private const string GitHubIssueNewUrl = "https://github.com/onehoon/OptiClick/issues/new";

    public string BuildIssueUrl(ContactIssueContext context, AppLanguage language)
    {
        var safeContext = context ?? new ContactIssueContext();
        var title = BuildIssueTitle(language);
        var body = BuildIssueBody(safeContext, language);
        var query = $"title={Uri.EscapeDataString(title)}&body={Uri.EscapeDataString(body)}";
        return $"{GitHubIssueNewUrl}?{query}";
    }

    private static string BuildIssueTitle(AppLanguage language)
    {
        return language == AppLanguage.Korean
            ? "[게임 추가 요청/ 기타 문의] 제목을 입력해주세요"
            : "[Game Addition Request / Other Inquiry] Enter a title";
    }

    private static string BuildIssueBody(ContactIssueContext context, AppLanguage language)
    {
        var appVersion = SafeValue(context.AppVersion);
        var gpuName = SafeValue(context.GpuName);
        var manufacturer = SafeValue(context.Manufacturer);
        var deviceModel = SafeValue(context.DeviceModel);

        return language == AppLanguage.Korean
            ? BuildKoreanBody(appVersion, gpuName, manufacturer, deviceModel)
            : BuildEnglishBody(appVersion, gpuName, manufacturer, deviceModel);
    }

    private static string BuildKoreanBody(string appVersion, string gpuName, string manufacturer, string deviceModel)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## 문의 내용");
        builder.AppendLine();
        builder.AppendLine("문의 내용을 적어주세요.");
        builder.AppendLine();
        builder.AppendLine();
        builder.AppendLine();
        builder.AppendLine();
        builder.AppendLine("## 기본 정보");
        builder.AppendLine($"- OptiClick Version: {appVersion}");
        builder.AppendLine($"- GPU: {gpuName}");
        builder.AppendLine($"- Manufacturer: {manufacturer}");
        builder.AppendLine($"- Model: {deviceModel}");
        return builder.ToString().TrimEnd();
    }

    private static string BuildEnglishBody(string appVersion, string gpuName, string manufacturer, string deviceModel)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## Inquiry Details");
        builder.AppendLine();
        builder.AppendLine("Please describe your request.");
        builder.AppendLine();
        builder.AppendLine();
        builder.AppendLine();
        builder.AppendLine();
        builder.AppendLine("## Basic Information");
        builder.AppendLine($"- OptiClick Version: {appVersion}");
        builder.AppendLine($"- GPU: {gpuName}");
        builder.AppendLine($"- Manufacturer: {manufacturer}");
        builder.AppendLine($"- Model: {deviceModel}");
        return builder.ToString().TrimEnd();
    }

    private static string SafeValue(string? value)
    {
        var text = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(text) ? "Unknown" : text;
    }
}
