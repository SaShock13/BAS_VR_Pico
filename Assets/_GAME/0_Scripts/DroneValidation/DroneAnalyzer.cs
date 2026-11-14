using System;
using System.Collections.Generic;

public sealed class DroneAnalyzer : IDroneAnalyzer
{
    private readonly Clean_AssemblySystem _assembly;

    public DroneAnalyzer(Clean_AssemblySystem assembly) => _assembly = assembly;

    public bool HasPart(
    DroneValidationContext context, 
    PartType type)
    {
        return context.PartsByType.ContainsKey(type);
    }


    public int CountParts(
    DroneValidationContext context,
    PartType type)
    {
        return context.PartsByType
            .TryGetValue(type, out var parts)
                ? parts.Count
                : 0;
    }

    public IReadOnlyList<PartDomainState> GetParts(
    DroneValidationContext context,
    PartType type)
    {
        return context.PartsByType
            .TryGetValue(type, out var parts)
                ? parts
                : Array.Empty<PartDomainState>();
    }


    public IReadOnlyList<string> FindCollisions(DroneDomainState drone)
    {
        var findings = new List<string>();
        var installed = new List<(PartDomainState state, DronePartView view)>();
        foreach (var id in drone.partInstanseIds)
        {
            var state = _assembly.GetPartDomainState(id);
            if (state.LifecycleState != PartLifecycleState.Installed) continue;
            var view = _assembly.GetViewById(id);
            if (view != null && view.HasMountingBounds) installed.Add((state, view));
        }

        for (int i = 0; i < installed.Count; i++)
        for (int j = i + 1; j < installed.Count; j++)
        {
            var (first, firstView) = installed[i];
            var (second, secondView) = installed[j];
            // Socket connections are authored to overlap. Use the same bounds check as zone mounting.
            if (!first.IsAttachedToZone && !second.IsAttachedToZone) continue;
            if (firstView.transform.IsChildOf(secondView.transform) ||
                secondView.transform.IsChildOf(firstView.transform)) continue;

            var zonePartView = first.IsAttachedToZone ? firstView : secondView;
            var otherView = first.IsAttachedToZone ? secondView : firstView;
            var relative = zonePartView.transform.worldToLocalMatrix * otherView.transform.localToWorldMatrix;
            var otherBounds = MountingBounds.Transform(otherView.LocalMountingBounds, relative);
            if (MountingBounds.Overlaps(zonePartView.LocalMountingBounds, otherBounds))
                findings.Add($"Пересекаются детали: {first.PartId} и {second.PartId}");
        }
        return findings;
    }
}
