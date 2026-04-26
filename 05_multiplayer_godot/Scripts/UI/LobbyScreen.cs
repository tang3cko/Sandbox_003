namespace SwarmSurvivor;

using Godot;

public partial class LobbyScreen : Control
{
    private LineEdit _addressInput;
    private LineEdit _portInput;
    private Label _statusLabel;
    private Button _hostButton;
    private Button _joinButton;
    private Button _singlePlayerButton;

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        SetOffsetsPreset(LayoutPreset.FullRect);

        BuildUI();

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.ServerStarted += OnServerStarted;
            NetworkManager.Instance.ClientConnected += OnClientConnected;
            NetworkManager.Instance.ConnectionFailed += OnConnectionFailed;
        }
    }

    public override void _ExitTree()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.ServerStarted -= OnServerStarted;
            NetworkManager.Instance.ClientConnected -= OnClientConnected;
            NetworkManager.Instance.ConnectionFailed -= OnConnectionFailed;
        }
    }

    private void BuildUI()
    {
        var bg = new ColorRect
        {
            Color = new Color(0.02f, 0.03f, 0.05f, 1),
        };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        bg.SetOffsetsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var vbox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        vbox.SetAnchorsPreset(LayoutPreset.Center);
        vbox.CustomMinimumSize = new Vector2(600, 500);
        vbox.OffsetLeft = -300;
        vbox.OffsetRight = 300;
        vbox.OffsetTop = -250;
        vbox.OffsetBottom = 250;
        vbox.AddThemeConstantOverride("separation", 16);

        var title = new Label { Text = "LOBBY" };
        title.AddThemeFontSizeOverride("font_size", 64);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        vbox.AddChild(BuildLabeledInput("Address", out _addressInput, NetworkConfig.DefaultAddress));
        vbox.AddChild(BuildLabeledInput("Port", out _portInput, NetworkConfig.DefaultPort.ToString()));

        _hostButton = BuildButton("Host", OnHostPressed);
        vbox.AddChild(_hostButton);

        _joinButton = BuildButton("Join", OnJoinPressed);
        vbox.AddChild(_joinButton);

        _singlePlayerButton = BuildButton("Single Player (legacy)", OnSinglePlayerPressed);
        vbox.AddChild(_singlePlayerButton);

        _statusLabel = new Label { Text = "" };
        _statusLabel.AddThemeFontSizeOverride("font_size", 24);
        _statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _statusLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.8f));
        vbox.AddChild(_statusLabel);

        AddChild(vbox);
    }

    private static HBoxContainer BuildLabeledInput(string labelText, out LineEdit input, string defaultValue)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 16);

        var label = new Label { Text = labelText, CustomMinimumSize = new Vector2(120, 0) };
        label.AddThemeFontSizeOverride("font_size", 24);
        row.AddChild(label);

        input = new LineEdit { Text = defaultValue, CustomMinimumSize = new Vector2(280, 40) };
        input.AddThemeFontSizeOverride("font_size", 24);
        row.AddChild(input);

        return row;
    }

    private static Button BuildButton(string label, System.Action onPressed)
    {
        var button = new Button { Text = label, CustomMinimumSize = new Vector2(0, 56) };
        button.AddThemeFontSizeOverride("font_size", 28);
        button.Pressed += onPressed;
        return button;
    }

    private void OnHostPressed()
    {
        var port = ParsePort();
        SetStatus($"Starting server on port {port}...");
        var error = NetworkManager.Instance?.StartServer(port) ?? Error.Failed;
        if (error != Error.Ok)
        {
            SetStatus($"Failed: {error}");
        }
    }

    private void OnJoinPressed()
    {
        var address = _addressInput.Text;
        var port = ParsePort();
        SetStatus($"Connecting to {address}:{port}...");
        var error = NetworkManager.Instance?.StartClient(address, port) ?? Error.Failed;
        if (error != Error.Ok)
        {
            SetStatus($"Failed: {error}");
        }
    }

    private void OnSinglePlayerPressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/Main.tscn");
    }

    private int ParsePort()
    {
        if (int.TryParse(_portInput.Text, out var port) && port is > 0 and < 65536)
        {
            return port;
        }
        return NetworkConfig.DefaultPort;
    }

    private void OnServerStarted()
    {
        GetTree().ChangeSceneToFile("res://Scenes/MainMultiplayer.tscn");
    }

    private void OnClientConnected()
    {
        GetTree().ChangeSceneToFile("res://Scenes/MainMultiplayer.tscn");
    }

    private void OnConnectionFailed()
    {
        SetStatus("Connection failed");
    }

    private void SetStatus(string text)
    {
        if (_statusLabel != null) _statusLabel.Text = text;
    }
}
