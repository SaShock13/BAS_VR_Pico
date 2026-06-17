using System.Collections.Generic;
using UnityEngine;
using Zenject;

/// <summary>
/// Система активаци эффектов для режима Validation Предполетной проверки дрона
/// </summary>
public class ValidateEffectsSystem 
{
    private readonly List<IPreflightEffect> _effects;

    private bool isActive = false;

    public ValidateEffectsSystem(
        DroneFocusEffect droneFocus
        ,BackgroundFadeEffect backgroundFade
        )
    {
        _effects = new()
        {
            droneFocus
            ,backgroundFade
        };
        isActive = false;
    }

    public void Enter() // Эффекты должны быть на GO
    {
        if(isActive) return;


        Debug.Log($"*******_effects.Count {_effects.Count}");
        foreach (var effect in _effects)
            effect.Enter();
        isActive = true;
    }

    public void Exit()
    {
        if(!isActive) return;
        foreach (var effect in _effects)
            effect.Exit();
        isActive = false;
    }
}
