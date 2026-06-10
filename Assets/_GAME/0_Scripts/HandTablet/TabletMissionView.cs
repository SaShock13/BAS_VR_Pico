using TMPro;
using UnityEngine;

public sealed class TabletMissionView : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _missionName;

    [SerializeField]
    private TMP_Text _currentStep;

    [SerializeField]
    private TMP_Text _progress;

    [SerializeField]
    private GameObject _content;

    public void UpdateMission(
        TabletMissionState state)
    {
        if (state == null)
        {
            //_content.SetActive(false);// Может пользователь сам будет убирать панель?
            _missionName.text = "Нет активной миссии";

            _currentStep.text = "Нет активных задач";

            _progress.text = "";



            return;
        }

        //_content.SetActive(true);

        _missionName.text =
            state.MissionName;

        _currentStep.text =
            state.CurrentStepDefinition;

        _progress.text =
            $"{state.CurrentStepIndex+1}/{state.TotalSteps}";
    }
}