using UnityEngine;

public static class AssemblyProjectSession
{
    public static string ActiveId { get; set; }
    public static string ActiveName { get; set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset()
    {
        ActiveId = null;
        ActiveName = null;
    }
}
