using System.IO;
using UnityEngine;

public class JsonSaveService : ISaveService
{
    private string FilePath => Path.Combine(Application.persistentDataPath, "assembly.json");

    public void Save(AssemblySaveData data)
    {
        var json = JsonUtility.ToJson(data, true);
        File.WriteAllText(FilePath, json);

        Debug.Log($"Saved to: {FilePath}");
    }

    public AssemblySaveData Load()
    {
        if (!File.Exists(FilePath))
        {
            Debug.LogWarning("Save file not found");
            return new AssemblySaveData();
        }

        var json = File.ReadAllText(FilePath);
        return JsonUtility.FromJson<AssemblySaveData>(json);
    }
}