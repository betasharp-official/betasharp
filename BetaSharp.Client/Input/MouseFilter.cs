namespace BetaSharp.Client.Input;

public class MouseFilter
{
    private float _targetValue;
    private float _remainingValue;
    private float _lastSmoothedValue;

    public float Smooth(float input, float weight)
    {
        _targetValue += input;
        input = (_targetValue - _remainingValue) * weight;
        _lastSmoothedValue += (input - _lastSmoothedValue) * 0.5F;
        if (input > 0.0F && input > _lastSmoothedValue || input < 0.0F && input < _lastSmoothedValue)
        {
            input = _lastSmoothedValue;
        }

        _remainingValue += input;
        return input;
    }
}
