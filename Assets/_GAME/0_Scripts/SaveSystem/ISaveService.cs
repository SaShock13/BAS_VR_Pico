public interface ISaveService
{
    void Save(AssemblySaveData data);
    AssemblySaveData Load();
}