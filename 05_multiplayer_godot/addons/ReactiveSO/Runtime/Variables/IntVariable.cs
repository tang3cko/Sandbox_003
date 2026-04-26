namespace ReactiveSO;

using Godot;

[GlobalClass]
public partial class IntVariable : Resource
{
    [Export] private int _initialValue;
    [Export] private int _value;
    [Export] private IntEventChannel _onValueChanged;

    [Signal]
    public delegate void ValueChangedEventHandler(int value);

    public int Value
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

    public int InitialValue => _initialValue;
    public IntEventChannel OnValueChanged => _onValueChanged;

    public void ResetToInitial()
    {
        Value = _initialValue;
    }

    public void SetWithoutNotify(int value)
    {
        _value = value;
    }
}
