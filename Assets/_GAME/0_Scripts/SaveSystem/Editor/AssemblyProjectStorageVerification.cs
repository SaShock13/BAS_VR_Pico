#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AssemblyProjectStorageVerification
{
    [MenuItem("Tools/BAS VR/Verify assembly project storage")]
    public static void Run()
    {
        string root = Path.Combine(Path.GetTempPath(), "BASVR-StorageVerification-" + Guid.NewGuid().ToString("N"));
        string previousId = AssemblyProjectSession.ActiveId;
        string previousName = AssemblyProjectSession.ActiveName;
        try
        {
            AssemblyProjectSession.Reset();
            var store = new JsonSaveService(root);
            Require(store.ListProjects().Count == 0, "Fresh store must be empty.");
            var first = store.CreateProject("Первый");
            var firstAssembly = new AssemblySaveData();
            firstAssembly.Parts.Add(new PartSaveData { InstanceId = "first-part" });
            store.Save(firstAssembly);
            var second = store.CreateProject("Второй");
            Require(first.Id != second.Id, "Projects must have independent IDs.");
            Require(store.Load().Parts.Count == 0, "New project must be empty.");
            store.SelectProject(first.Id);
            Require(store.Load().Parts[0].InstanceId == "first-part", "First project's parts must survive selection.");
            store.RenameProject(first.Id, "Переименован");
            Require(AssemblyProjectSession.ActiveName == "Переименован", "Rename must update the active name.");

            var reopened = new JsonSaveService(root);
            Require(reopened.ListProjects().Count == 2, "Both projects must survive store recreation.");
            reopened.SelectProject(second.Id);
            Require(reopened.Load().Parts.Count == 0, "Saving first must not overwrite second.");
            string secondPath = Path.Combine(root, "Assemblies", second.Id + ".json");
            reopened.Save(new AssemblySaveData());
            File.WriteAllText(secondPath, "broken json");
            Require(reopened.Load().Parts.Count == 0, "Corrupt current file must recover the backup.");
            File.WriteAllText(Path.Combine(root, "Assemblies", Guid.NewGuid().ToString("N") + ".json"), "broken json");
            Require(reopened.ListProjects().Count == 2, "Unrecoverable files must not hide healthy projects.");

            File.WriteAllText(Path.Combine(root, "assembly.json"), JsonUtility.ToJson(firstAssembly));
            Require(reopened.ListProjects().Count == 3, "Legacy save must be imported.");
            Require(reopened.ListProjects().Count == 3, "Legacy import must be idempotent.");
            Require(File.Exists(Path.Combine(root, "assembly.json")), "Legacy source must remain untouched.");
            bool rejected = false;
            try { reopened.SelectProject("../escape"); }
            catch (ArgumentException) { rejected = true; }
            Require(rejected, "File selection must reject path traversal.");
            Debug.Log("PASS: independent assembly projects, empty projects, rename, reopen, backup recovery, corrupt file isolation, legacy import, ID validation.");
        }
        finally
        {
            AssemblyProjectSession.ActiveId = previousId;
            AssemblyProjectSession.ActiveName = previousName;
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
#endif
