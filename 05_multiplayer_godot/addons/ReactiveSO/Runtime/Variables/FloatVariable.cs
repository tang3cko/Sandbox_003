namespace ReactiveSO;

using Godot;

[GlobalClass]
public partial class FloatVariable : Resource
{
    [Export] private float _initialValue;
    [Export] private float _value;
    [Export] private FloatEventChannel _onValueChanged;

    [Signal]
    public delegate void ValueChangedEventHandler(float value);

    public float Value
    {
        get => _value;
        set
        {
            if (Mathf.IsEqualApprox(_value, value)) return;
            _value = value;
            EmitSignal(SignalName.ValueChanged, _value);
            _onValueChanged?.Raise(_value);
        }
    }

    public float InitialValue => _initialValue;
    public FloatEventChannel OnValueChanged => _onValueChanged;

    public void ResetToInitial()
    {
        Value = _initialValue;
    }

    public void SetWithoutNotify(float value)
    {
        _value = value;
    }
}
