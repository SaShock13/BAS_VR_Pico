using TMPro;
using UnityEngine;

public class ScreenHintView : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _text;

    public void Show(HintInfo hint)
    {
        gameObject.SetActive(true);

        _text.text = hint.Text;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}