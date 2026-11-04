using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class JsonSaveService : ISaveService
{
    private const string LegacyId = "00000000000000000000000000000001";
    private readonly string _root;
    private string DirectoryPath => Path.Combine(_root, "Assemblies");

    [Zenject.Inject]
    public JsonSaveService() : this(Application.persistentDataPath) { }

    public JsonSaveService(string rootDirectory)
    {
        _root = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));
    }

    public List<AssemblyProjectData> ListProjects()
    {
        ImportLegacy();
        var result = new List<AssemblyProjectData>();
        if (!Directory.Exists(DirectoryPath)) return result;
        foreach (var path in Directory.GetFiles(DirectoryPath, "*.json"))
        {
            try
            {
                var entry = Read(Path.GetFileNameWithoutExtension(path));
                entry.Assembly = null; // The menu retains metadata only, never part graphs.
                result.Add(entry);
            }
            catch (Exception exception) { Debug.LogWarning($"Cannot read assembly {path}: {exception.Message}"); }
        }
        result.Sort((a, b) => b.UpdatedAtTicks.CompareTo(a.UpdatedAtTicks));
        return result;
    }

    public AssemblyProjectData CreateProject(string name)
    {
        var project = NewProject(Guid.NewGuid().ToString("N"), name);
        Write(project);
        Select(project);
        return project;
    }

    public AssemblyProjectData SelectProject(string id)
    {
        var project = Read(id);
        Select(project);
        return project;
    }

    public void RenameProject(string id, string name)
    {
        var project = Read(id);
        project.Name = NormalizeName(name);
        project.UpdatedAtTicks = DateTime.UtcNow.Ticks;
        Write(project);
        if (AssemblyProjectSession.ActiveId == id) Select(project);
    }

    public void Save(AssemblySaveData data)
    {
        if (data == null || data.Parts == null) throw new ArgumentException("Assembly data is missing.", nameof(data));
        var project = string.IsNullOrEmpty(AssemblyProjectSession.ActiveId)
            ? NewProject(Guid.NewGuid().ToString("N"), null)
            : Read(AssemblyProjectSession.ActiveId);
        project.Assembly = data;
        project.UpdatedAtTicks = DateTime.UtcNow.Ticks;
        Write(project);
        Select(project);
    }

    public AssemblySaveData Load()
    {
        if (string.IsNullOrEmpty(AssemblyProjectSession.ActiveId)) return new AssemblySaveData();
        return SelectProject(AssemblyProjectSession.ActiveId).Assembly;
    }

    private static void Select(AssemblyProjectData project)
    {
        AssemblyProjectSession.ActiveId = project.Id;
        AssemblyProjectSession.ActiveName = project.Name;
    }

    private static AssemblyProjectData NewProject(string id, string name)
    {
        long now = DateTime.UtcNow.Ticks;
        return new AssemblyProjectData
        {
            Id = id, Name = NormalizeName(name), CreatedAtTicks = now, UpdatedAtTicks = now,
            Assembly = new AssemblySaveData()
        };
    }

    private static string NormalizeName(string name)
    {
        return string.IsNullOrWhiteSpace(name) ? "Сборка " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") : name.Trim();
    }

    private string FilePath(string id)
    {
        if (!Guid.TryParseExact(id, "N", out _)) throw new ArgumentException("Invalid assembly ID.", nameof(id));
        return Path.Combine(DirectoryPath, id + ".json");
    }

    private AssemblyProjectData Read(string id)
    {
        string path = FilePath(id);
        try { return ReadFile(path, id); }
        catch (Exception original)
        {
            if (!File.Exists(path + ".bak")) throw;
            var recovered = ReadFile(path + ".bak", id);
            Debug.LogWarning($"Recovered assembly '{recovered.Name}' from backup: {original.Message}");
            return recovered;
        }
    }

    private static AssemblyProjectData ReadFile(string path, string id)
    {
        var project = JsonUtility.FromJson<AssemblyProjectData>(File.ReadAllText(path));
        if (project == null || project.Version != 1 || project.Id != id ||
            string.IsNullOrWhiteSpace(project.Name) || project.Assembly == null || project.Assembly.Parts == null)
            throw new InvalidDataException("Invalid or unsupported assembly save.");
        return project;
    }

    private void Write(AssemblyProjectData project)
    {
        Directory.CreateDirectory(DirectoryPath);
        string path = FilePath(project.Id);
        string temporary = path + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonUtility.ToJson(project, true));
            if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
            else File.Move(temporary, path);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private void ImportLegacy()
    {
        string legacyPath = Path.Combine(_root, "assembly.json");
        if (!File.Exists(legacyPath) || File.Exists(FilePath(LegacyId))) return;
        try
        {
            var assembly = JsonUtility.FromJson<AssemblySaveData>(File.ReadAllText(legacyPath));
            if (assembly == null || assembly.Parts == null) throw new InvalidDataException("Invalid legacy assembly save.");
            var project = NewProject(LegacyId, "Старая сборка");
            project.Assembly = assembly;
            project.CreatedAtTicks = File.GetCreationTimeUtc(legacyPath).Ticks;
            project.UpdatedAtTicks = File.GetLastWriteTimeUtc(legacyPath).Ticks;
            Write(project);
        }
        catch (Exception exception) { Debug.LogWarning($"Cannot import assembly.json: {exception.Message}"); }
    }
}
