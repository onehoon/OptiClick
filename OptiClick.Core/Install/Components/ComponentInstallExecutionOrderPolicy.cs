namespace OptiClick.Core.Install.Components;

public static class ComponentInstallExecutionOrderPolicy
{
    // OptiScalerCore must complete before any optional component or archive install starts.
    public static IReadOnlyList<ComponentInstallName> RequiredFirst { get; } =
        Array.AsReadOnly(new[] { ComponentInstallName.OptiScalerCore });

    // The middle order is deterministic for logs/tests; these components do not depend on each other.
    public static IReadOnlyList<ComponentInstallName> PreferredMiddleOrder { get; } =
        Array.AsReadOnly(new[]
        {
            ComponentInstallName.SpecialK,
            ComponentInstallName.ReFramework,
            ComponentInstallName.Unreal5
        });

    // ExtraBundle must run last so curated game payloads can override the base install.
    public static IReadOnlyList<ComponentInstallName> RequiredLast { get; } =
        Array.AsReadOnly(new[] { ComponentInstallName.ExtraBundle });

    public static IReadOnlyList<ComponentInstallName> FullOrder { get; } =
        Array.AsReadOnly(RequiredFirst.Concat(PreferredMiddleOrder).Concat(RequiredLast).ToArray());

    public static IReadOnlyList<ComponentInstallName> PostCoreOrder { get; } =
        Array.AsReadOnly(PreferredMiddleOrder.Concat(RequiredLast).ToArray());

    public static IReadOnlyList<ComponentInstallName> AllComponents { get; } =
        Array.AsReadOnly(FullOrder.ToArray());

    static ComponentInstallExecutionOrderPolicy()
    {
        ValidateCoreMustBeFirstAndExtraLast(nameof(FullOrder), FullOrder);
        ValidateFullOrderHasNoDuplicates();
        ValidateContainsAllSupportedComponents();
        ValidateRequiredSet(nameof(RequiredFirst), RequiredFirst, ComponentInstallName.OptiScalerCore);
        ValidateRequiredSet(nameof(RequiredLast), RequiredLast, ComponentInstallName.ExtraBundle);
        ValidateRequiredSet(nameof(PreferredMiddleOrder), PreferredMiddleOrder, null);
    }

    public static IReadOnlyList<ComponentInstallName> GetCoreThenMiddleThenExtraOrder()
    {
        ValidateCoreMustBeFirstAndExtraLast(nameof(FullOrder), FullOrder);
        return FullOrder;
    }

    private static bool ValidateCoreMustBeFirstAndExtraLast(string orderName, IReadOnlyList<ComponentInstallName> order)
    {
        if (order is null || order.Count == 0)
        {
            throw new InvalidOperationException(
                $"Component install order '{orderName}' is not configured.");
        }

        if (order[0] != ComponentInstallName.OptiScalerCore)
        {
            throw new InvalidOperationException(
                $"Component install order '{orderName}' is invalid: first component must be {ComponentInstallName.OptiScalerCore}.");
        }

        if (order[^1] != ComponentInstallName.ExtraBundle)
        {
            throw new InvalidOperationException(
                $"Component install order '{orderName}' is invalid: last component must be {ComponentInstallName.ExtraBundle}.");
        }

        return true;
    }

    private static void ValidateContainsAllSupportedComponents()
    {
        var missingComponents = AllComponents
            .Except(FullOrder)
            .ToArray();
        if (missingComponents.Length > 0)
        {
            var values = string.Join(", ", missingComponents.Select(static x => x.ToString()));
            throw new InvalidOperationException(
                $"Component install order '{nameof(FullOrder)}' is missing configured component(s): {values}.");
        }
    }

    private static void ValidateFullOrderHasNoDuplicates()
    {
        var duplicates = FullOrder
            .GroupBy(static component => component)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .ToArray();

        if (duplicates.Length > 0)
        {
            var values = string.Join(", ", duplicates.Select(static x => x.ToString()));
            throw new InvalidOperationException(
                $"Component install order '{nameof(FullOrder)}' has duplicate component(s): {values}.");
        }
    }

    private static bool ValidateRequiredSet(
        string orderName,
        IReadOnlyList<ComponentInstallName> components,
        ComponentInstallName? requiredValue)
    {
        if (components is null || components.Count == 0)
        {
            throw new InvalidOperationException(
                $"Component set '{orderName}' is not configured.");
        }

        if (components.Count != components.Distinct().Count())
        {
            throw new InvalidOperationException(
                $"Component set '{orderName}' is invalid: duplicate values are not allowed.");
        }

        if (requiredValue.HasValue && components.Any(c => c != requiredValue.Value))
        {
            throw new InvalidOperationException(
                $"Component set '{orderName}' is invalid for required slot '{requiredValue}'.");
        }

        return true;
    }
}
