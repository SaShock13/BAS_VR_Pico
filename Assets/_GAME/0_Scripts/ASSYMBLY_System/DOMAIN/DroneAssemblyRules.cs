public static class DroneAssemblyRules
{
    public const PartType RootPartType = PartType.Rays;
    public const string BodySocketId = "mount/Body/root";

    public static bool IsRoot(PartDomainState part) =>
        part != null && part.Type == RootPartType && string.IsNullOrEmpty(part.AttachedPartInstanceId);
}
