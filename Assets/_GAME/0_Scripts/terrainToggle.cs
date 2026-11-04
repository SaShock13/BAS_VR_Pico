using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class terrainToggle : MonoBehaviour
{

    [SerializeField] private InputActionReference toggleTerrainAction;

    [SerializeField] private GameObject terrain;

    private void OnEnable()
    {
        toggleTerrainAction.action.performed += OnTerrainToggle;
    }

    private void OnDisable()
    {
        toggleTerrainAction.action.performed -= OnTerrainToggle;
        
    }



    private void OnTerrainToggle(InputAction.CallbackContext context)
    {
        terrain.SetActive(!terrain.activeSelf);
    }
}
