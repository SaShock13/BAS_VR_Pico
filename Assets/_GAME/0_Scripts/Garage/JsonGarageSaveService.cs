using System.IO;
using UnityEngine;

public class JsonGarageSaveService : IGarageSaveService
{
    private const string FileName = "garage.json";



    private string Path =>
        System.IO.Path.Combine(
            Application.persistentDataPath,
            FileName);



    public void Save(GarageSaveData data)
    {
        string json =
            JsonUtility.ToJson(data, true);

        File.WriteAllText(Path, json);

        Debug.Log($"Garage saved: {Path}");
    }



    public GarageSaveData Load()
    {
        if (!File.Exists(Path))
        {
            Debug.Log("Garage save not found");

            return new GarageSaveData();
        }

        string json =
            File.ReadAllText(Path);

        GarageSaveData data =
            JsonUtility.FromJson<GarageSaveData>(json);

        return data;
    }
}