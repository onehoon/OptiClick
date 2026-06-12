using OptiClick.Infrastructure.Install.Config.Xml;
using System.IO;

namespace OptiClick.Infrastructure.Install.Config;

public sealed class XmlProfileEditor
{
    private readonly IProfilePathResolver _pathResolver;

    public XmlProfileEditor(IProfilePathResolver pathResolver)
    {
        _pathResolver = pathResolver;
    }

    public ConfigProfileApplySummary Apply(string targetPath, OptiClick.Core.Install.ConfigApplyProfileRows profileRows)
    {
        var skipped = new List<ConfigProfileSkippedRow>();
        var targets = CollectTargets(
            targetPath,
            (profileRows ?? OptiClick.Core.Install.ConfigApplyProfileRows.Empty).GameXmlProfileRows,
            skipped);
        return ApplyTargets(targets, skipped);
    }

    public ConfigProfileApplySummary Apply(string targetPath, IReadOnlyDictionary<string, object?> gameData)
    {
        var skipped = new List<ConfigProfileSkippedRow>();
        var targets = CollectTargets(targetPath, gameData, skipped);
        return ApplyTargets(targets, skipped);
    }

    private static ConfigProfileApplySummary ApplyTargets(
        IReadOnlyDictionary<string, List<XmlProfileRow>> targets,
        List<ConfigProfileSkippedRow> skipped)
    {
        var profileName = ConfigProfileNames.GameXmlProfile;
        var applied = new List<ConfigProfileAppliedRow>();
        var errors = new List<ConfigProfileError>();
        var changedAny = false;

        foreach (var (filePath, rows) in targets)
        {
            var fileApplied = new List<ConfigProfileAppliedRow>();
            var fileSkipped = new List<ConfigProfileSkippedRow>();
            try
            {
                OptionalFileEditRunner.ApplyExistingFileSettings(
                    filePath,
                    () =>
                    {
                        var changed = ApplyRows(filePath, rows, fileApplied, fileSkipped);
                        changedAny = changedAny || changed;
                    },
                    restoreOriginalReadonly: true);
                applied.AddRange(fileApplied);
                skipped.AddRange(fileSkipped);
            }
            catch (Exception ex)
            {
                errors.Add(new ConfigProfileError
                {
                    ProfileName = profileName,
                    ReasonCode = ConfigErrorReasons.ApplyFailed,
                    Detail = $"{Path.GetFileName(filePath)}: {ex.Message}",
                    TargetPath = filePath
                });
            }
        }

        return new ConfigProfileApplySummary
        {
            ProfileName = profileName,
            Changed = changedAny,
            Applied = applied,
            Skipped = skipped,
            Errors = errors,
            Completed = true
        };
    }

    private Dictionary<string, List<XmlProfileRow>> CollectTargets(
        string targetPath,
        IReadOnlyDictionary<string, object?> gameData,
        List<ConfigProfileSkippedRow> skipped)
    {
        var grouped = new Dictionary<string, List<XmlProfileRow>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in ConfigDataReader.ReadRows(gameData, ConfigProfileNames.GameXmlProfile))
        {
            var profilePath = ConfigDataReader.ReadString(row, "path");
            var targetRaw = ConfigDataReader.ReadValue(row, "xml_path");
            var targetText = ConfigDataReader.ReadString(row, "xml_path");
            if (string.IsNullOrWhiteSpace(profilePath) || string.IsNullOrWhiteSpace(targetText))
            {
                skipped.Add(new ConfigProfileSkippedRow
                {
                    ProfileName = ConfigProfileNames.GameXmlProfile,
                    ReasonCode = ConfigSkipReasons.InvalidRow,
                    Detail = "path/xml_path"
                });
                continue;
            }

            var resolvedPath = _pathResolver.Resolve(targetPath, profilePath, requireExisting: true);
            if (resolvedPath is null)
            {
                skipped.Add(new ConfigProfileSkippedRow
                {
                    ProfileName = ConfigProfileNames.GameXmlProfile,
                    ReasonCode = ConfigSkipReasons.MissingTargetFile,
                    Detail = profilePath
                });
                continue;
            }

            if (!grouped.TryGetValue(resolvedPath, out var list))
            {
                list = new List<XmlProfileRow>();
                grouped[resolvedPath] = list;
            }

            list.Add(new XmlProfileRow
            {
                TargetRaw = targetRaw ?? targetText,
                Value = IniProfileEditor.NormalizeProfileScalar(ConfigDataReader.ReadValue(row, "value"), "")
            });
        }

        return grouped;
    }

    private Dictionary<string, List<XmlProfileRow>> CollectTargets(
        string targetPath,
        IReadOnlyList<OptiClick.Core.Install.ConfigApplyProfileRow> profileRows,
        List<ConfigProfileSkippedRow> skipped)
    {
        var grouped = new Dictionary<string, List<XmlProfileRow>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in profileRows ?? [])
        {
            var profilePath = ConfigApplyProfileRowReader.ReadTargetPathHint(row);
            var targetText = ConfigApplyProfileRowReader.ReadValuePath(row, "xml_path");
            var targetRaw = ConfigApplyProfileRowReader.ReadRawValue(row, "xml_path", targetText);
            if (string.IsNullOrWhiteSpace(profilePath) || string.IsNullOrWhiteSpace(targetText))
            {
                skipped.Add(new ConfigProfileSkippedRow
                {
                    ProfileName = ConfigProfileNames.GameXmlProfile,
                    ReasonCode = ConfigSkipReasons.InvalidRow,
                    Detail = "path/xml_path"
                });
                continue;
            }

            var resolvedPath = _pathResolver.Resolve(targetPath, profilePath, requireExisting: true);
            if (resolvedPath is null)
            {
                skipped.Add(new ConfigProfileSkippedRow
                {
                    ProfileName = ConfigProfileNames.GameXmlProfile,
                    ReasonCode = ConfigSkipReasons.MissingTargetFile,
                    Detail = profilePath
                });
                continue;
            }

            if (!grouped.TryGetValue(resolvedPath, out var list))
            {
                list = new List<XmlProfileRow>();
                grouped[resolvedPath] = list;
            }

            list.Add(new XmlProfileRow
            {
                TargetRaw = targetRaw ?? targetText,
                Value = IniProfileEditor.NormalizeProfileScalar(ConfigApplyProfileRowReader.ReadValue(row), "")
            });
        }

        return grouped;
    }

    private static bool ApplyRows(
        string filePath,
        IReadOnlyList<XmlProfileRow> rows,
        List<ConfigProfileAppliedRow> applied,
        List<ConfigProfileSkippedRow> skipped)
    {
        if (rows.Count == 0)
        {
            return false;
        }

        var readResult = XmlTextCodec.ReadWithFallback(filePath);
        var updatedText = readResult.Text;
        var changed = false;

        foreach (var row in rows)
        {
            XmlNormalizedTarget normalizedTarget;
            try
            {
                normalizedTarget = XmlPathTargetParser.NormalizeTarget(row.TargetRaw);
                if (normalizedTarget.PathParts.Count == 0)
                {
                    skipped.Add(new ConfigProfileSkippedRow
                    {
                        ProfileName = ConfigProfileNames.GameXmlProfile,
                        ReasonCode = ConfigSkipReasons.InvalidRow,
                        Detail = row.TargetRaw?.ToString() ?? ""
                    });
                    continue;
                }
            }
            catch
            {
                skipped.Add(new ConfigProfileSkippedRow
                {
                    ProfileName = ConfigProfileNames.GameXmlProfile,
                    ReasonCode = ConfigSkipReasons.InvalidRow,
                    Detail = row.TargetRaw?.ToString() ?? ""
                });
                continue;
            }

            IReadOnlyList<XmlElementSpan> elements;
            try
            {
                elements = XmlElementSpanParser.ParseElements(updatedText);
            }
            catch
            {
                throw;
            }

            var element = XmlElementSpanParser.FindMatchingElement(elements, normalizedTarget.PathParts);
            var targetLabel = XmlPathTargetParser.FormatPathParts(normalizedTarget.PathParts);
            if (element is null)
            {
                skipped.Add(new ConfigProfileSkippedRow
                {
                    ProfileName = ConfigProfileNames.GameXmlProfile,
                    ReasonCode = ConfigSkipReasons.MissingPathTarget,
                    Detail = targetLabel
                });
                continue;
            }

            try
            {
                (string UpdatedText, bool Changed) result;
                var oldValue = ReadXmlTargetValue(
                    updatedText,
                    element,
                    normalizedTarget.AttributeName);
                if (!string.IsNullOrWhiteSpace(normalizedTarget.AttributeName))
                {
                    result = XmlSpanUpdater.UpdateAttribute(
                        updatedText,
                        element,
                        normalizedTarget.AttributeName!,
                        row.Value);
                }
                else
                {
                    result = XmlSpanUpdater.UpdateText(updatedText, element, row.Value);
                }

                if (!result.Changed)
                {
                    skipped.Add(new ConfigProfileSkippedRow
                    {
                        ProfileName = ConfigProfileNames.GameXmlProfile,
                        ReasonCode = ConfigSkipReasons.Unchanged,
                        Detail = targetLabel,
                        TargetPath = filePath,
                        TargetKey = row.TargetRaw?.ToString() ?? "",
                        OldValue = oldValue,
                        NewValue = row.Value
                    });
                    continue;
                }

                changed = true;
                updatedText = result.UpdatedText;
                applied.Add(new ConfigProfileAppliedRow
                {
                    ProfileName = ConfigProfileNames.GameXmlProfile,
                    TargetPath = filePath,
                    TargetKey = row.TargetRaw?.ToString() ?? "",
                    OldValue = oldValue,
                    NewValue = row.Value
                });
            }
            catch (InvalidOperationException ex)
            {
                skipped.Add(new ConfigProfileSkippedRow
                {
                    ProfileName = ConfigProfileNames.GameXmlProfile,
                    ReasonCode = ConfigSkipReasons.MissingPathTarget,
                    Detail = ex.Message,
                    TargetPath = filePath,
                    TargetKey = row.TargetRaw?.ToString() ?? "",
                    NewValue = row.Value
                });
            }
        }

        if (!changed)
        {
            return false;
        }

        XmlTextCodec.WriteWithOriginalEncoding(filePath, updatedText, readResult.EncodingInfo);
        return true;
    }

    private static string ReadXmlTargetValue(
        string text,
        XmlElementSpan element,
        string? attributeName)
    {
        if (!string.IsNullOrWhiteSpace(attributeName))
        {
            return element.Attributes.TryGetValue(attributeName, out var attribute)
                ? attribute.Value
                : "<missing>";
        }

        if (element.SelfClosing)
        {
            return "<missing>";
        }

        if (element.EndTagStart is null)
        {
            return "";
        }

        var currentInner = text[element.ContentStart..element.EndTagStart.Value];
        return XmlSpanUpdater.UnescapeXmlValue(currentInner).Trim();
    }

    private sealed record XmlProfileRow
    {
        public object? TargetRaw { get; init; }
        public string Value { get; init; } = "";
    }
}
