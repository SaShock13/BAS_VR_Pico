using UnityEngine;
using Zenject;

public class TestValidateEffects : MonoBehaviour
{

    [Inject] ValidateEffectsSystem _validateEffectsSystem;
    [Inject] DroneFocusEffect _focusEffect;
    [Inject] BackgroundFadeEffect _fadeEffect;
    [Inject] ISelectionService _selection;
    [Inject] PartViewRegistry _views;


    private void Start()
    {


    }


    private void Update()
    {

        //if (Input.GetKeyDown(KeyCode.B))
        //{


        //    var selectedId = _selection.Current.Value.PartId;
        //    if (selectedId == null ) return;
        //    _views.TryGet(selectedId, out var view);

        //    if (view != null) { _focusEffect.Initialize(view.transform); }


        //    Debug.Log($"_validateEffectsSystem.Enter(); {this}");
        //    _validateEffectsSystem.Enter();
        //}
        //if (Input.GetKeyDown(KeyCode.V))
        //{
        //    _validateEffectsSystem.Exit();
        //}



    }
}
