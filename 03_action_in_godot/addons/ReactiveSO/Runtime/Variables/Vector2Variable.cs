namespace ReactiveSO;

using Godot;

[GlobalClass]
public partial class Vector2Variable : Resource
{
    [Export] private Vector2 _initialValue;
    [Export] private Vector2 _value;
    [Export] private Vector2EventChannel _onValueChanged;

    [Signal]
    public delegate void ValueChangedEventHandler(Vector2 value);

    public Vector2 Value
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

    public Vector2 InitialValue => _initialValue;
    public Vector2EventChannel OnValueChanged => _onValueChanged;

    public void ResetToInitial()
    {
        Value = _initialValue;
    }

    public void SetWithoutNotify(Vector2 value)
    {
        _value = value;
    }
}
