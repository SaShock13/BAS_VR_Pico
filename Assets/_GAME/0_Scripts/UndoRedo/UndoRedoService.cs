using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Сервис истории Снапшотов. todo Отмена повтор действия через загрузку сцены целиком.  Возможнопотом стоит переделать на стэк команд для оптиимизации
/// </summary>
public class UndoRedoService
{
    private readonly List<AssemblySaveData> _history = new();
    private int _currentIndex = -1;

    private readonly Func<AssemblySaveData> _capture;
    private readonly Action<AssemblySaveData> _restore;

    private bool _isRestoring;

    private const int MaxHistory = 10;

    public UndoRedoService(
        Func<AssemblySaveData> capture,
        Action<AssemblySaveData> restore)
    {
        _capture = capture ?? throw new ArgumentNullException(nameof(capture));
        _restore = restore ?? throw new ArgumentNullException(nameof(restore));
    }

    // =========================
    // INIT
    // =========================
    public void Initialize()
    {
        _history.Clear();
        _currentIndex = -1;

        Record(); // сохраняем начальное состояние
    }

    // =========================
    // RECORD
    // =========================
    public void Record()
    {

        Debug.Log($"----------Record snapshot {this}");
        if (_isRestoring)
            return;

        var snapshot = Clone(_capture());

        // если мы сделали undo и потом новое действие → обрезаем redo
        if (_currentIndex < _history.Count - 1)
        {
            _history.RemoveRange(_currentIndex + 1, _history.Count - _currentIndex - 1);
        }

        _history.Add(snapshot);
        _currentIndex++;

        // ограничение истории
        if (_history.Count > MaxHistory)
        {
            _history.RemoveAt(0);
            _currentIndex--;
        }
    }

    // =========================
    // UNDO
    // =========================
    public void Undo()
    {
        if (!CanUndo)
            return;
        Debug.Log($"------Undo snapshot {this}");
        _isRestoring = true;

        _currentIndex--;
        var snapshot = Clone(_history[_currentIndex]);

        _restore(snapshot);

        _isRestoring = false;
    }

    // =========================
    // REDO
    // =========================
    public void Redo()
    {
        if (!CanRedo)
            return;
        Debug.Log($"------------Redo snapshot {this}");
        _isRestoring = true;

        _currentIndex++;
        var snapshot = Clone(_history[_currentIndex]);

        _restore(snapshot);

        _isRestoring = false;
    }

    // =========================
    // HELPERS
    // =========================
    public bool CanUndo => _currentIndex > 0;
    public bool CanRedo => _currentIndex < _history.Count - 1;

    private AssemblySaveData Clone(AssemblySaveData data)
    {
        var json = JsonUtility.ToJson(data);
        return JsonUtility.FromJson<AssemblySaveData>(json);
    }
}