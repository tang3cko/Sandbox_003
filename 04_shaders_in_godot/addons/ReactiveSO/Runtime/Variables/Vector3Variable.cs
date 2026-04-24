namespace ReactiveSO;

using Godot;

[GlobalClass]
public partial class Vector3Variable : Resource
{
    [Export] private Vector3 _initialValue;
    [Export] private Vector3 _value;
    [Export] private Vector3EventChannel _onValueChanged;

    [Signal]
    public delegate void ValueChangedEventHandler(Vector3 value);

    public Vector3 Value
    {
        get => _value;
        set
        {
            if (_value.IsEqualApprox(value)) return;
            _value = value;
            EmitSignal(SignalName.ValueChanged, _value);
            _onValueChanged?.Raise(_value);
        }
    }

    public Vector3 InitialValue => _initialValue;
    public Vector3EventChannel OnValueChanged => _onValueChanged;

    public void ResetToInitial()
    {
        Value = _initialValue;
    }

    public void SetWithoutNotify(Vector3 value)
    {
        _value = value;
    }
}
