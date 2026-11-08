using TMPro;
using UnityEngine;

public class WorldHintView : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _text;

    private Transform _target;
    private Transform _camera;

    public void Show(HintInfo hint)
    {
        if (hint.PartTransform != null) _target = hint.PartTransform;
        else if (hint.SoketTransform != null) _target = hint.SoketTransform;
        else
        {
            Hide();
            return;
        }

        if (_text == null)
        {
            Hide();
            return;
        }
        var camera = Camera.main;
        _camera = camera != null ? camera.transform : null;
        _text.text = hint.Text;
        transform.position = _target.position + Vector3.up * 0.2f;
        gameObject.SetActive(true);
    }

    private void LateUpdate()
    {
        if (_target == null)
        {
            Hide();
            return;
        }

        transform.position =
            _target.position + Vector3.up * 0.2f;

        if (_camera != null)
            transform.forward = _camera.forward;
    }

    public void Hide()
    {
        _target = null;
        gameObject.SetActive(false);
    }
}
