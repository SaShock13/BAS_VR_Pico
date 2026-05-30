using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public class GarageSceneBootstrap : MonoBehaviour
{
    [SerializeField]
    private Transform[] _slots;

    [Inject]
    private IGarageService _garage;

    [Inject]
    private Clean_AssemblySystem _assembly;



    private async System.Threading.Tasks.Task RestoreGarage()
    {

        var drones = _garage.GetAll();

        int count = Mathf.Min(
            drones.Count,
            _slots.Length);

        for (int i = 0; i < count; i++)
        {
            await _assembly.SpawnAssembly(
                drones[i].Assembly,
                _slots[i].position);
        }
    }

    private void Start()
    {
        
        LoadGarage();
    }

    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.F11))
        {
            ClearGarage();
        }


        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SceneManager.LoadScene(0);  // для теста переход в конструктор
        }



    }

    private void ClearGarage()
    {
        _garage.Clear();
        _assembly.ClearCurrentAssembly();
    }

    private async void LoadGarage()
    {
        _assembly.ClearCurrentAssembly();
        await RestoreGarage();
    }

    
}
