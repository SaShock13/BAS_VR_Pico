public class JsonGarageSaveService : IGarageSaveService
{
    private readonly JsonSaveService _projects;

    public JsonGarageSaveService(JsonSaveService projects) => _projects = projects;

    public void Save(GarageSaveData data) => _projects.SaveGarage(data);

    public GarageSaveData Load() => _projects.LoadGarage();
}
