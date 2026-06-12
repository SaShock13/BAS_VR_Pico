using TMPro;
using UnityEngine;

public class WorldHintView : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _text;

    private Transform _target;

    public void Show(HintInfo hint)
    {
        gameObject.SetActive(true);

        if (hint.PartTransform != null) _target = hint.PartTransform;
        else if (hint.SoketTransform != null) _target = hint.SoketTransform;
        else
        {
            Hide();
            _target = null;
        }

        _text.text = hint.Text;
    }

    private void LateUpdate()
    {
        if (_target == null)
            return;

        transform.position =
            _target.position + Vector3.up * 0.2f;

        transform.forward =
            Camera.main.transform.forward;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}