using System;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

// One event publisher for all preview buttons in this menu.
public class MenuPartCreationView : MonoBehaviour
{
    [Serializable]
    private struct Entry
    {
        public Button button;
        public PartConfig config;
        public Transform previewTransform;
    }

    [SerializeField] private Entry[] entries;
    [SerializeField] private Transform spawnPoint;
    private IEventBus _eventBus;

    [Inject]
    public void Construct(IEventBus eventBus) => _eventBus = eventBus;

    public void RequestCreatePart(int index)
    {
        if (!isActiveAndEnabled || entries == null || index < 0 || index >= entries.Length) return;
        var entry = entries[index];
        if (entry.button == null || !entry.button.isActiveAndEnabled || !entry.button.IsInteractable() ||
            entry.config == null || string.IsNullOrWhiteSpace(entry.config.PartId)) return;
        if (_eventBus == null)
        {
            Debug.LogWarning("MenuPartCreationView: IEventBus is not injected.", this);
            return;
        }

        _eventBus.Publish(new Clean_CreatePartRequestEvent
        {
            PartId = entry.config.PartId,
            WorldPosition = spawnPoint != null ? spawnPoint.position : entry.button.transform.position,
            MenuCellHeight = entry.button.transform is RectTransform cell
                ? cell.TransformVector(Vector3.up * cell.rect.height).magnitude : (float?)null,
            Timestamp = DateTime.UtcNow
        });
    }
}
