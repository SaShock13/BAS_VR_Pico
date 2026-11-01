using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
[RequireComponent(typeof(AudioSource))]
public class OldColorPicker : MonoBehaviour
{
    public static OldColorPicker Instance;

    [Header("UI References")]
    public GameObject panel;
    public Image colorPreview;
    public Slider redSlider;
    public Slider greenSlider;
    public Slider blueSlider;
    public Button applyButton;
    //public Button cancelButton;
    AudioSource _audioSource;

    [SerializeField] private AudioClip clickClip;
    private Action<Color> OnColorSelected;
    private string defaultClip = "Audio/Click";
    [Inject] private IEventBus _eventBus;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        if(clickClip == null) clickClip = Resources.Load<AudioClip>(defaultClip);
        //panel.SetActive(false);

        // Слушатели UI
        redSlider.onValueChanged.AddListener(UpdateView);
        greenSlider.onValueChanged.AddListener(UpdateView);
        blueSlider.onValueChanged.AddListener(UpdateView);

        applyButton.onClick.AddListener(OnApplyClicked);
        //cancelButton.onClick.AddListener(() => panel.SetActive(false));
    }

    private void UpdateView(float arg0)
    {
        UpdatePreview(); 
        //_eventBus.Publish(new PlayOneShotRequestedEvent { SoundId = SoundId.SliderMove, Volume = 0.4f, Timestamp = DateTime.UtcNow });
    }

    public static void Show(Color startColor, Action<Color> callback)
    {

        //Debug.Log($"startColor {startColor}");
        if (Instance == null)
        {
            Debug.LogError("ColorPicker not found in scene!");
            return;
        }

        Instance.panel.SetActive(true);
        Instance.OnColorSelected = callback;
        Instance.SetSliders(startColor);
    }

    public void SetSliders(Color color)
    {
        redSlider.SetValueWithoutNotify(color.r);
        greenSlider.SetValueWithoutNotify(color.g);
        blueSlider.SetValueWithoutNotify(color.b);
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        //_audioSource.PlayOneShot(clickClip);
        Color currentColor = new Color(
            redSlider.value,
            greenSlider.value,
            blueSlider.value,
            1
        );

        colorPreview.color = currentColor;
        OnApplyClicked();
    }

    private void OnApplyClicked()
    {

        Color selectedColor = colorPreview.color;
        _audioSource.PlayOneShot(clickClip);
        //panel.SetActive(false);
        OnColorSelected?.Invoke(selectedColor);
    }

    public static void Hide()
    {
        Instance.panel.SetActive(false);
    }
}
