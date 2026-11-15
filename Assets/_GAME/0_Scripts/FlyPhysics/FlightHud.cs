using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class FlightHud
{
    public enum View { Player, FirstPerson, ThirdPerson }
    internal static readonly string[] FieldNames =
    {
        "Напряжение батареи", "Заряд батареи", "Режим полёта", "Скорость",
        "Высота", "Дата и время", "Перекрестье", "Горизонт", "Положение стиков"
    };

    private readonly Rigidbody _body;
    private readonly DroneDebugInput _input;
    private readonly RectTransform _leftStick;
    private readonly RectTransform _rightStick;
    private const int StickField = 8;
    private readonly BatteryPhysicsData _battery;
    private readonly float _launchHeight;
    private readonly Canvas _canvas;
    private readonly RawImage _vignette;
    private Texture2D _vignetteTexture;
    private readonly TMP_Text _charge;
    private readonly TMP_Text _voltage;
    private readonly TMP_Text _speed;
    private readonly TMP_Text _altitude;
    private readonly TMP_Text _clock;
    private readonly TMP_Text _mode;
    private FlightControlMode? _lastMode;
    private readonly RectTransform _horizon;
    private readonly TMP_Text _crosshair;
    private readonly GameObject[] _fields;
    private View _view;
    private float _nextRefresh;
    private int _lastCharge = -2;
    private int _lastVoltage = int.MinValue;
    private int _lastSpeed = int.MinValue;
    private int _lastAltitude = int.MinValue;
    private long _lastClockSecond = -1;

    public FlightHud(Rigidbody body, BatteryPhysicsData battery, float launchHeight, DroneDebugInput input)
    {
        _body = body;
        _input = input;
        _battery = battery;
        _launchHeight = launchHeight;
        var root = new GameObject("Flight HUD", typeof(RectTransform), typeof(Canvas));
        _canvas = root.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        ((RectTransform)root.transform).sizeDelta = new Vector2(600f, 400f);
        root.transform.localScale = Vector3.one * 0.002f;
        _vignetteTexture = CreateVignetteTexture(0.56f);
        _vignette = new GameObject("FPV Vignette", typeof(RectTransform)).AddComponent<RawImage>();
        _vignette.rectTransform.SetParent(root.transform, false);
        _vignette.texture = _vignetteTexture;
        _vignette.raycastTarget = false;

        // At 1.2 m these corners sit about 20 degrees sideways and 13 degrees vertically.
        _voltage = CreateLabel("Voltage", new Vector2(-215f, 145f), 27f);
        _charge = CreateLabel("Charge", new Vector2(-215f, 110f), 27f);
        _mode = CreateLabel("Flight Mode", new Vector2(215f, 145f), 27f);
        _mode.rectTransform.sizeDelta = new Vector2(245f, 36f);
        _clock = CreateLabel("Date and Time", new Vector2(215f, 110f), 27f);
        _speed = CreateLabel("Speed", new Vector2(-215f, -145f), 27f);
        _altitude = CreateLabel("Altitude", new Vector2(215f, -145f), 27f);

        _horizon = (RectTransform)new GameObject("Artificial Horizon", typeof(RectTransform)).transform;
        _horizon.SetParent(_canvas.transform, false);
        CreateHorizonBar(-52f);
        CreateHorizonBar(52f);
        _crosshair = CreateLabel("Crosshair", Vector2.zero, 32f);
        _crosshair.text = "+";
        var sticks = new GameObject("Stick Positions", typeof(RectTransform)).transform;
        sticks.SetParent(_canvas.transform, false);
        _leftStick = CreateStick(sticks, -49f);
        _rightStick = CreateStick(sticks, 49f);
        _fields = new[] { _voltage.gameObject, _charge.gameObject, _mode.gameObject, _speed.gameObject,
            _altitude.gameObject, _clock.gameObject, _crosshair.gameObject, _horizon.gameObject, sticks.gameObject };
        FlightSession.HudVisibilityChanged += ApplyVisibility;
    }

    public void SetVignetteRadius(float radius)
    {
        var previous = _vignetteTexture;
        _vignetteTexture = CreateVignetteTexture(radius);
        _vignette.texture = _vignetteTexture;
        UnityEngine.Object.Destroy(previous);
    }

    private static Texture2D CreateVignetteTexture(float radius)
    {
        const int size = 96;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "FPV Vignette",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        var pixels = new Color32[size * size];
        float clearRadius = radius * 0.70f;
        float edge = clearRadius + 0.11f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            var uv = new Vector2(x, y) * (2f / (size - 1)) - Vector2.one;
            float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(clearRadius, edge, uv.magnitude));
            pixels[y * size + x] = new Color32(0, 0, 0, (byte)(alpha * 255f));
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }

    public void SetVignetteAppearance(float opacity, float scale)
    {
        _vignette.color = new Color(1f, 1f, 1f, opacity);
        float inset = (1f - scale) * 0.5f;
        _vignette.uvRect = new Rect(inset, inset, scale, scale);
    }

    private RectTransform CreateStick(Transform parent, float x)
    {
        var panel = (RectTransform)new GameObject("Stick Axes", typeof(RectTransform)).transform;
        panel.SetParent(parent, false);
        panel.anchoredPosition = new Vector2(x, -145f);
        var axisColor = AmberUiTheme.Muted;
        CreateStickGraphic(panel, "Horizontal Axis", Vector2.zero, new Vector2(78f, 1.5f), axisColor);
        CreateStickGraphic(panel, "Vertical Axis", Vector2.zero, new Vector2(1.5f, 78f), axisColor);
        var marker = CreateLabel("Stick Position", Vector2.zero, 48f);
        marker.transform.SetParent(panel, false);
        marker.rectTransform.anchoredPosition = Vector2.zero;
        marker.text = "●";
        return marker.rectTransform;
    }

    private static RectTransform CreateStickGraphic(Transform parent, string name, Vector2 position,
        Vector2 size, Color color)
    {
        var graphic = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
        graphic.rectTransform.SetParent(parent, false);
        graphic.rectTransform.anchoredPosition = position;
        graphic.rectTransform.sizeDelta = size;
        graphic.color = color;
        graphic.raycastTarget = false;
        return graphic.rectTransform;
    }

    private void CreateHorizonBar(float x)
    {
        var bar = new GameObject("Horizon Bar", typeof(RectTransform)).AddComponent<Image>();
        bar.rectTransform.SetParent(_horizon, false);
        bar.rectTransform.anchoredPosition = new Vector2(x, 0f);
        bar.rectTransform.sizeDelta = new Vector2(64f, 3f);
        bar.color = AmberUiTheme.Gold;
        bar.raycastTarget = false;
    }

    private TMP_Text CreateLabel(string name, Vector2 position, float size)
    {
        var label = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        label.transform.SetParent(_canvas.transform, false);
        label.rectTransform.sizeDelta = new Vector2(185f, 36f);
        label.rectTransform.anchoredPosition = position;
        label.fontSize = size * 0.5f;
        label.color = AmberUiTheme.Text;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    public void SetView(Camera camera, View view, int layer = -1)
    {
        _view = view;
        _canvas.worldCamera = camera;
        if (layer < 0) layer = camera.gameObject.layer;
        _canvas.gameObject.layer = layer;
        foreach (var graphic in _canvas.GetComponentsInChildren<Graphic>(true))
            graphic.gameObject.layer = layer;
        _canvas.transform.SetParent(camera.transform, false);
        _canvas.transform.localPosition = Vector3.forward * 1.2f;
        var projection = camera.projectionMatrix;
        _canvas.transform.localScale = camera.targetTexture != null
            ? new Vector3(1.2f / (projection.m00 * 365f), 1.2f / (projection.m11 * 190f), 0.002f)
            : Vector3.one * 0.002f;
        bool showVignette = view == View.FirstPerson && camera.targetTexture == null;
        _vignette.gameObject.SetActive(showVignette);
        if (showVignette)
        {
            float depth = Mathf.Max(camera.nearClipPlane + 0.02f, 0.18f);
            var viewSpan = camera.ViewportToWorldPoint(new Vector3(1f, 1f, depth))
                - camera.ViewportToWorldPoint(new Vector3(0f, 0f, depth));
            var localSpan = camera.transform.InverseTransformVector(viewSpan);
            _vignette.rectTransform.localPosition = Vector3.forward * ((depth - 1.2f) / 0.002f);
            _vignette.rectTransform.sizeDelta = new Vector2(
                (localSpan.x + 0.09f) / 0.002f, localSpan.y * 1.06f / 0.002f);
        }
        ApplyVisibility();
        _nextRefresh = 0f;
        Refresh();
    }

    public void SetVisible(bool visible)
    {
        if (_canvas != null) _canvas.gameObject.SetActive(visible);
    }

    public void Destroy()
    {
        FlightSession.HudVisibilityChanged -= ApplyVisibility;
        if (_canvas != null) UnityEngine.Object.Destroy(_canvas.gameObject);
        UnityEngine.Object.Destroy(_vignetteTexture);
    }

    private void ApplyVisibility()
    {
        if (_canvas == null) return;
        for (int i = 0; i < _fields.Length; i++)
            _fields[i].SetActive(FlightSession.IsHudFieldVisible(i) &&
                ((i != 6 && i != 7) || _view != View.Player));
    }

    public void Refresh()
    {
        if (_canvas == null) return;

        if (_body == null)
        {
            SetVisible(false);
            return;
        }

        if (_view != View.Player && FlightSession.IsHudFieldVisible(7)) UpdateHorizon();
        if (FlightSession.IsHudFieldVisible(StickField) && _input != null)
        {
            var input = _input.CurrentInput;
            MoveStickIndicator(_leftStick, new Vector2(-input.Yaw, _input.Vertical));
            MoveStickIndicator(_rightStick, new Vector2(input.Roll, input.Pitch));
        }
        if (Time.unscaledTime < _nextRefresh) return;
        _nextRefresh = Time.unscaledTime + 0.2f;
        if (_input != null && _lastMode != _input.ControlMode)
        {
            _lastMode = _input.ControlMode;
            string model = FlightSession.Mode == FlightMode.Arcade ? "ARCADE" : "SIM";
            string control = _lastMode == FlightControlMode.AcroTrainer ? "Acro Trainer" : _lastMode.ToString();
            _mode.text = $"{model} / {control}";
        }

        var now = DateTime.Now;
        long clockSecond = now.Ticks / TimeSpan.TicksPerSecond;
        if (clockSecond != _lastClockSecond)
        {
            _lastClockSecond = clockSecond;
            _clock.text = now.ToString("dd.MM.yyyy  HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        }

        int charge = _battery != null && _battery.CapacityMah > 0f
            ? Mathf.RoundToInt(Mathf.Clamp01(_battery.CurrentChargeMah / _battery.CapacityMah) * 100f)
            : -1;
        if (charge != _lastCharge)
        {
            _lastCharge = charge;
            if (charge < 0) _charge.text = "BAT  —";
            else _charge.SetText("BAT  {0}%", charge);
        }

        int voltage = _battery == null ? -1 : Mathf.RoundToInt(_battery.ActualVoltage * 10f);
        if (voltage != _lastVoltage)
        {
            _lastVoltage = voltage;
            if (voltage < 0) _voltage.text = "VOLT  —";
            else _voltage.SetText("VOLT  {0:1} V", voltage * 0.1f);
        }

        int speed = Mathf.RoundToInt(_body.linearVelocity.magnitude * 3.6f);
        if (speed != _lastSpeed)
        {
            _lastSpeed = speed;
            _speed.SetText("SPD  {0} km/h", speed);
        }

        int altitude = Mathf.RoundToInt(_body.position.y - _launchHeight);
        if (altitude != _lastAltitude)
        {
            _lastAltitude = altitude;
            _altitude.SetText("ALT  {0} m", altitude);
        }
    }

    private static void MoveStickIndicator(RectTransform marker, Vector2 input)
    {
        var position = input * 34f;
        if (marker.anchoredPosition != position) marker.anchoredPosition = position;
    }

    private void UpdateHorizon()
    {
        // Show the world horizon in drone coordinates, independently of head rotation.
        var up = Quaternion.Inverse(_body.transform.rotation) * Vector3.up;
        var normal = new Vector2(up.x, up.y);
        float magnitude = normal.magnitude;
        _horizon.gameObject.SetActive(magnitude > 0.001f);
        if (magnitude <= 0.001f) return;

        var scale = _canvas.transform.localScale;
        var canvasNormal = new Vector2(up.x * scale.x, up.y * scale.y);
        float offset = Mathf.Clamp(-1.2f * up.z / canvasNormal.magnitude, -85f, 85f);
        _horizon.anchoredPosition = canvasNormal.normalized * offset;
        _horizon.localRotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(-canvasNormal.x, canvasNormal.y) * Mathf.Rad2Deg);
    }
}
