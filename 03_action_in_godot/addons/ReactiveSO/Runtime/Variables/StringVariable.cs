namespace ReactiveSO;

using Godot;

[GlobalClass]
public partial class StringVariable : Resource
{
    [Export] private string _initialValue = "";
    [Export] private string _value = "";
    [Export] private StringEventChannel _onValueChanged;

    [Signal]
    public delegate void ValueChangedEventHandler(string value);

    public string Value
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

    public string InitialValue => _initialValue;
    public StringEventChannel OnValueChanged => _onValueChanged;

    public void ResetToInitial()
    {
        Value = _initialValue;
    }

    public void SetWithoutNotify(string value)
    {
        _value = value;
    }
}
