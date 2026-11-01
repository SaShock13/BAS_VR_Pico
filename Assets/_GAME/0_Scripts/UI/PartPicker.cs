using System;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class PartPicker : MonoBehaviour
{
    //[Header("UI Кнопки (6)")]
    //public Button[] buttons = new Button[6];

    //[Header("Объекты для переключения (6)")]
    //public GameObject[] objects = new GameObject[6];


    //public Button previousButton ;
    //public Button nextButton ;

    //private int currentIndex = -1;

    //private ObjectCarousel currentCorousel;

    //[Inject] private IEventBus _eventBus;
    //void Start()
    //{
    //    // Проверка
    //    if (buttons.Length != objects.Length)
    //        Debug.LogWarning("Количество кнопок и объектов должно совпадать.");

    //    // Инициализация обработчиков
    //    for (int i = 0; i < buttons.Length; i++)
    //    {
    //        int index = i; // локальная копия для лямбды
    //        buttons[i].onClick.AddListener(() => OnButtonClicked(index));
    //    }

    //    // Скрыть все объекты в начале
    //    for (int j = 0; j < objects.Length; j++)
    //    {
    //        if (objects[j] != null)
    //            objects[j].SetActive(false);
    //    }

    //    previousButton.onClick.AddListener(OnPreviousClicked);
    //    nextButton.onClick.AddListener(OnNextClicked);
    //}

    //private void OnNextClicked()
    //{
    //    _eventBus.Publish(new PlayOneShotRequestedEvent { SoundId = SoundId.MenuClicked, Timestamp = DateTime.UtcNow });
    //}

    //private void OnPreviousClicked()
    //{
    //    _eventBus.Publish(new PlayOneShotRequestedEvent { SoundId = SoundId.MenuClicked, Timestamp = DateTime.UtcNow });
    //}

    //public void NextClicked()
    //{
    //    if (currentCorousel == null) return;
    //    _eventBus.Publish(new PlayOneShotRequestedEvent { SoundId = SoundId.MenuClicked, Timestamp = DateTime.UtcNow });
    //    currentCorousel.RotateCarousel(-1);
    //}
    // public void PreviousClicked()
    //{
    //    if (currentCorousel == null) return;
    //    _eventBus.Publish(new PlayOneShotRequestedEvent { SoundId = SoundId.MenuClicked, Timestamp = DateTime.UtcNow });
    //    currentCorousel.RotateCarousel(1);
    //}


    //private void OnButtonClicked(int index)
    //{
    //    // Если тот же индекс — ничего не делаем
    //    if (index == currentIndex)
    //        return;

    //    _eventBus.Publish(new PlayOneShotRequestedEvent { SoundId = SoundId.MenuClicked, Timestamp = DateTime.UtcNow });
    //    // Отключаем предыдущий
    //    if (currentIndex >= 0 && currentIndex < objects.Length && objects[currentIndex] != null)
    //        objects[currentIndex].SetActive(false);

    //    // Включаем новый
    //    if (index >= 0 && index < objects.Length && objects[index] != null)
    //        objects[index].SetActive(true);

    //    currentIndex = index;
    //    currentCorousel = objects[currentIndex].GetComponent<ObjectCarousel>();
    //}
}
