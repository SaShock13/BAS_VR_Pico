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

    [Serializable]
    public sealed class MissionEntry
    {
        public MissionDefinition mission;
        public string environmentId;
    }

    [SerializeField] private List<MissionEntry> missions = new();
    public IReadOnlyList<MissionEntry> Missions => missions;
    public MissionEntry FindMission(string id) =>
        missions.Find(entry => entry?.mission != null && entry.mission.MissionId == id);

    public string coreScenePath = "Assets/Scenes/FlightCoreScene.unity";
    [SerializeField] private List<Entry> environments = new List<Entry>();
    public IReadOnlyList<Entry> Environments => environments;
    public Entry Find(string id) => environments.Find(e => e != null && e.id == id);
}
