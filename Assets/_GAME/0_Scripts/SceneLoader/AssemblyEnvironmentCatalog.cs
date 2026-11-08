using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "BAS VR/Assembly Environment Catalog")]
public sealed class AssemblyEnvironmentCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public string id;
        public string title;
        [TextArea] public string description;
        [Tooltip("Full scene path included in Build Profiles Scene List")]
        public string scenePath;
    }

    [SerializeField] private List<Entry> environments = new List<Entry>();
    public IReadOnlyList<Entry> Environments => environments;
    public Entry Find(string id) => environments.Find(e => e != null && e.id == id);
}

// Survives Single scene loads, just like FlightSession. Return from flight keeps the location.
public static class AssemblyEnvironmentSession
{
    public static string EnvironmentId;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => EnvironmentId = null;
}
