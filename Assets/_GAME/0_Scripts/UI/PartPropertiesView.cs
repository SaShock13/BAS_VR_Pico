using System;
using System.Linq;
using ModestTree;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Zenject;

[RequireComponent(typeof(AudioSource))]
public class PartPropertiesView : MonoBehaviour
{
    //[SerializeField] private TMP_Text nameText;
    //[SerializeField] private TMP_Text renderScaleText;
    //[SerializeField] private TMP_Dropdown materialDropdown;
    //[SerializeField] private Slider smoothnessSlider;
    //[SerializeField] private Button colorButton; 
    //[SerializeField] private Toggle isFreeCheckBox;

    //[SerializeField] private TMP_Text deletePartNameText;
    //[SerializeField] private GameObject confirmDeletePanel;
    //private IPropertiesChangable currentPart;
    //private MaterialList _materialList;
    //private Selector _selector;
    //AudioSource _audioSource;

    //[SerializeField] private AudioClip clickClip;
    //[SerializeField] private AudioClip deleteClip;
    //[SerializeField] private AudioClip dublicateClip;


    //private UniversalRenderPipelineAsset urpAsset;
    //private DynamicGraphicOptimizer _renderScaler;
    //private float currentRS =1;
    //private bool isShowing;
    //private string defaultClip = "Audio/Click";
    //private IEventBus _eventBus;

    //[Inject]
    //public void Construct(MaterialList materialList, DynamicGraphicOptimizer renderScaler, Selector selector,IEventBus eventBus)
    //{
    //    _materialList = materialList;
    //    _renderScaler = renderScaler;
    //    _selector = selector;
    //    _eventBus = eventBus;
    //}

    //private void Awake()
    //{
    //    urpAsset = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
    //    _audioSource = GetComponent<AudioSource>();
    //    _audioSource.playOnAwake = false;
    //    var defaulClickSound = Resources.Load<AudioClip>(defaultClip);
    //    if (clickClip == null) clickClip = defaulClickSound;
    //    if (deleteClip == null) deleteClip = defaulClickSound;
    //    if (dublicateClip == null) dublicateClip = defaulClickSound;
    //    confirmDeletePanel.SetActive(false);

    //}
    //private void Start()
    //{
    //    if (_selector != null ) _selector.OnPartSelected += PartSelected;
        
    //}

    //void OnEnable()
    //{
    //    //if (_selector != null ) _selector.OnPartSelected += PartSelected;
    //    if (gameObject.activeInHierarchy)  // todo Сделать чтобы выделялось чтото по умолчанию
    //    {
    //        if (currentPart != null) ShowPartPanel(currentPart);
    //        else
    //        {
    //            isShowing = true;
    //            nameText.text = "Не выбрано";
    //            Debug.Log($"currentPart == null {this}");
    //        }
    //        isFreeCheckBox.onValueChanged.AddListener(isFreeToggleChanged);
    //        isFreeCheckBox.isOn = currentPart != null ? currentPart.IsFree : false;
    //        smoothnessSlider.value = currentPart != null ? currentPart.Smoothness : 1;
    //    }
    //}

    //private void isFreeToggleChanged(bool isFree)
    //{
    //    //Debug.Log($"isFreeToggleChanged {this}");
    //    if (currentPart!=null)
    //    {
    //        //Debug.Log($"currentPart {currentPart != null}");
    //        _eventBus.Publish(new PlayOneShotRequestedEvent { SoundId = SoundId.MenuClicked, Volume = 0.4f, Timestamp = System.DateTime.UtcNow });
    //        currentPart.SetFreePositioning(isFree); 
    //    }
    //}

    //void OnDisable()
    //{
    //    if (currentPart != null) currentPart.Select(false);
    //    isShowing = false;
    //    confirmDeletePanel.SetActive(false);
    //    //if (_selector != null) _selector.OnPartSelected -= PartSelected;
    //}

    //private void OnDestroy()
    //{
    //    if (_selector != null) _selector.OnPartSelected -= PartSelected;
        
    //}

    //public void ConfirmDelete()
    //{
    //    if (currentPart == null)
    //    {
    //        confirmDeletePanel.SetActive(false);
    //        return;
    //    }
    //    _eventBus.Publish(new PlayOneShotRequestedEvent { SoundId = SoundId.DeletePart, Volume = 1.3f, Timestamp = DateTime.UtcNow });
    //    currentPart.DeletePart();
    //    confirmDeletePanel.SetActive(false);
    //    currentPart = null;
    //    nameText.text = "Не выбрано";
    //}
    //public void CancelDelete()
    //{
    //    _eventBus.Publish(new PlayOneShotRequestedEvent { SoundId = SoundId.MenuClicked, Timestamp = DateTime.UtcNow });
    //    confirmDeletePanel.SetActive(false);
    //}


    //private void PartSelected(IPropertiesChangable part)
    //{
    //    if( currentPart!=null && part == currentPart) return;
    //    smoothnessSlider.enabled = true;
    //    if (currentPart != null)currentPart.Select(false);
    //    currentPart = part;
    //    Debug.Log($"PartSelected  {currentPart.PartName}");
    //    if (isShowing)
    //    {
    //        this.enabled = false;
    //        //currentPart.Select(true);

    //        this.enabled = true;
    //        ColorPicker.Instance.SetSliders(currentPart.GetColor);
    //        ColorPicker.Show(part.GetColor, color => { if (currentPart == null) return; currentPart.SetColor(color); });
    //    }
    //}

    //public void DeletePart()
    //{
    //    if (currentPart == null)
    //    {
    //        _eventBus.Publish(new PlayOneShotRequestedEvent { SoundId = SoundId.MenuWrong, Volume = 0.8f, Timestamp = DateTime.UtcNow });
    //        return;
    //    }
    //    _eventBus.Publish(new PlayOneShotRequestedEvent { SoundId = SoundId.MenuClicked, Timestamp = DateTime.UtcNow });
    //    deletePartNameText.text = currentPart.PartName;
    //    confirmDeletePanel.SetActive(true);
    //}


    //public void DublicateObject()
    //{
    //    if (currentPart == null)
    //    {
    //        _eventBus.Publish(new PlayOneShotRequestedEvent { SoundId = SoundId.MenuWrong, Timestamp = DateTime.UtcNow });
    //        return;
    //    }
    //    _eventBus.Publish(new PlayOneShotRequestedEvent { SoundId = SoundId.DublicatePart, Timestamp = DateTime.UtcNow });
    //    currentPart.DuplicateThisObject();
    //}


    //private void ShowPartPanel(IPropertiesChangable part)   /// todo 
    //{
    //    isShowing = true;
    //    _audioSource.PlayOneShot(clickClip);
    //    //Debug.Log($"ShowForPart {this}");
    //    //currentPart.SetColor(ColorPicker.)
    //    currentPart.Select(false);

    //    currentPart = part;
    //    currentPart.Select(true);
    //    nameText.text = part.PartName;


    //    isFreeCheckBox.isOn = currentPart.IsFree;
    //    //isFreeCheckBox.onValueChanged.AddListener((bool on)=> _eventBus.Publish(new PlayOneShotRequestedEvent { SoundId = SoundId.MenuClicked, Timestamp = DateTime.UtcNow }););

    //    //Материалы
    //    materialDropdown.onValueChanged.RemoveAllListeners();
    //    materialDropdown.ClearOptions();
    //    var names = _materialList.list.Select(m => m.name).ToList();
    //    int currentMaterialIndex = _materialList.list.IndexOf(currentPart.CurrentMaterial); // индекс текущего материала детали в общем списке
    //    //var selectedPartMaterial = _materialList.list.Where(x=> x.name == currentPart.CurrentMaterial.name).FirstOrDefault(); // индекс текущего материала детали в общем списке
    //    //int currentMaterialIndex = selectedPartMaterial!= null?  _materialList.list.IndexOf(selectedPartMaterial) : 0; // индекс текущего материала детали в общем списке
    //    materialDropdown.AddOptions(names);
    //    materialDropdown.value = currentMaterialIndex;
    //    materialDropdown.onValueChanged.AddListener(i => 
    //    { 
    //        part.SetMaterial(_materialList.list[i]); 
    //        ColorPicker.Show(part.GetColor, color => currentPart.SetColor(color)); 
    //    });

    //    // Smoothness
    //    smoothnessSlider.onValueChanged.RemoveAllListeners();
    //    smoothnessSlider.value = part.Smoothness;
    //    smoothnessSlider.onValueChanged.AddListener(v => { if (currentPart == null) return; part.SetSmoothness(v); _eventBus.Publish(new PlayOneShotRequestedEvent { SoundId = SoundId.SliderMove, Volume = 0.5f, Timestamp = DateTime.UtcNow }); }) ;

    //    ColorPicker.Show(part.GetColor, color => { if (currentPart == null) return; currentPart.SetColor(color); });
    //    ColorPicker.Instance.SetSliders(currentPart.GetColor);

    //    //// Цвет
    //    //colorButton.onClick.AddListener(() =>
    //    //{
    //    //    // todo При первом нажатии нулевые слайдеры, сделать текущие цвета (при повторном работает норм)
    //    //    ColorPicker.Show(part.GetColor, color => currentPart.SetColor(color));
    //    //});

    //    if(currentPart.HasConstantMaterial)
    //    {
    //        smoothnessSlider.enabled = false;
    //        ColorPicker.Hide();
    //    }

    //    gameObject.SetActive(true);
    //}
}
