using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Zenject;

public class ValidationController : MonoBehaviour
{
    
    [Inject] IAppLogger _logger;
    [Inject] private IEventBus _eventBus;
    [Inject] public ISelectionService Selection;
    [Inject] private INotificationService _notifications;
    [Inject] private IPartConfigRepository _configs;
    [Inject] private PartViewRegistry _views;
    [Inject] private Clean_AssemblySystem _assemblySystem;
    [Inject] private DroneReadinessService _readinessService;
    [Inject] private ValidateEffectsSystem _validateEffects;
    [Inject] private DroneFocusEffect _focusEffect;

    private bool inValidationMode = false;



    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.V))
        {

            if (Selection.Current != null)
            {
                Debug.Log($"SelectedPartId {Selection.Current.Value.PartId}");

                // Тестирование системы Проверки готовности

                var selectedId = Selection.Current.Value.PartId;
                if (selectedId == null) return;

                var partDomain = _assemblySystem.GetPartDomainState(selectedId);
                if (!string.IsNullOrEmpty(partDomain.DroneId))
                {
                    ValidateDrone(partDomain.DroneId);
                }
            }


        }




    }

    public void ValidateDrone(string droneId)
    {
        if (!inValidationMode)
        {

            _views.TryGet(droneId, out var view);

            if (view != null)
            {
                _focusEffect.Initialize(view.transform);

                Debug.Log($"_validateEffectsSystem.Enter(); on  {view.transform.name}");

                _validateEffects.Enter();
            }

            //_logger.Log($"*****Validate {droneId} ");
            DroneDomainState drone =
                _assemblySystem.GetDroneDomainState(droneId);

            _logger.Log($"********DroneDomain {drone != null} ");

            DroneRequirements requirements =
                CreateMissionRequirements();

            DroneValidationContext context =
                BuildContext(
                    drone,
                    requirements);

            DroneReadinessResult result =
                _readinessService.Validate(context);

            ShowValidationResult(result);
            inValidationMode = true;
        }
        else
        {
            _validateEffects.Exit();
            HideValidationResult();
            inValidationMode = false;
        }

    }

    private void HideValidationResult()
    {
        /// СКРЫТЬ UI ВАЛИДАЦИИ
    }

    /// <summary>
    /// Показать UI валидации
    /// </summary>
    /// <param name="result"></param>
    private void ShowValidationResult(DroneReadinessResult result)
    {

        foreach (var group in result.Groups)
        {
            Debug.Log(
                $"**********=== {group.GroupName} ===");

            foreach (var message in group.Messages)
            {
                Debug.Log(
                    $"***********{message.Severity}: {message.Message}");
            }
        }

        Debug.Log(
        $"******Готовность: {result.TotalScore:F0}%");

        if (result.IsReady) _logger.Log("*******Дрон готов к полету");
        else _logger.Log("******Дрон НЕ ГОТОВ к полету");

    }

    private DroneValidationContext BuildContext(DroneDomainState drone, DroneRequirements requirements)
    {
        List<PartDomainState> parts =
        drone.partInstanseIds
            .Select(id =>
                _assemblySystem.GetPartDomainState(id))
            .ToList();

        var rootView = _assemblySystem.GetViewById(drone.InstanceId);

        var partsByType = parts
            .GroupBy(x =>
                _configs.Get(x.PartId).PartType)
            .ToDictionary(
        g => g.Key,
        g => g.ToList());



        return new DroneValidationContext
        {
            Drone = drone,
            Parts = parts,
            Requirements = requirements,
            PartsByType = partsByType,
            droneTransform = rootView.transform

        };
    }

    private DroneRequirements CreateMissionRequirements()
    {
        return new DroneRequirements
        {


            MinFlightTimeMinutes = 4f,

            MinThrustToWeightRatio = 1.8f,

            MaxCenterOfMassOffset = 0.2f,

            CheckCollisions = true
        };
    }
}
