using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "BAS VR/Flight Environment Catalog")]
public sealed class FlightEnvironmentCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public string id;
        public string title;
        [TextArea] public string description;
        [Tooltip("Full scene path, included in Build Profiles Scene List")]
        public string scenePath;
    }

    public string coreScenePath = "Assets/Scenes/FlightCoreScene.unity";
    [SerializeField] private List<Entry> environments = new List<Entry>();
    public IReadOnlyList<Entry> Environments => environments;
    public Entry Find(string id) => environments.Find(e => e != null && e.id == id);
}
