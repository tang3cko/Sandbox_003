namespace ReactiveSO;

using Godot;

[GlobalClass]
public partial class BoolVariable : Resource
{
    [Export] private bool _initialValue;
    [Export] private bool _value;
    [Export] private BoolEventChannel _onValueChanged;

    [Signal]
    public delegate void ValueChangedEventHandler(bool value);

    public bool Value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            _value = value;
            EmitSignal(SignalName.ValueChanged, _value);
            _onValueChanged?.Raise(_value);
        }
    }

    public bool InitialValue => _initialValue;
    public BoolEventChannel OnValueChanged => _onValueChanged;

    public void ResetToInitial()
    {
        Value = _initialValue;
    }

    public void SetWithoutNotify(bool value)
    {
        _value = value;
    }
}
