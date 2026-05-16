using UnityEngine;

[System.Serializable]
public class PIDController
{
    [SerializeField] private float _p = 0.2f;
    [SerializeField] private float _i = 0f;
    [SerializeField] private float _d = 0f;

    [SerializeField]
    private float _integralLimit = 1f;

    private float _integral;
    private float _lastError;

    public float Update(float error, float dt)
    {
        _integral += error * dt;

        _integral = Mathf.Clamp(
            _integral,
            -_integralLimit,
            _integralLimit);

        float derivative =
            (error - _lastError) / dt;

        _lastError = error;

        return
            error * _p +
            _integral * _i +
            derivative * _d;
    }

    public void Reset()
    {
        _integral = 0f;
        _lastError = 0f;
    }
}