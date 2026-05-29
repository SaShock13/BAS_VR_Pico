public interface IGarageSaveService
{
    void Save(GarageSaveData data);

    GarageSaveData Load();
}