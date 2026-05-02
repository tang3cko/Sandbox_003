namespace Persistence;

using Godot;
using System;
using System.Collections.Generic;

public partial class MainScreen : Control
{
    // ----- Systems / data ------------------------------------------------
    private readonly SaveSystem _saveSystem = new();
    private readonly SettingsSystem _settingsSystem = new();
    private InventoryData _inventory = new();
    private SettingsData _settings = SettingsData.CreateDefault();

    // SaveSequence is monotonic per session; persisted via SaveData.SaveSequence.
    private long _lastLoadedSequence = 0;
    private double _lastSavedAtUnix = 0;

    // ----- Status colors -------------------------------------------------
    private static readonly Color ColorSuccess = new(0.50f, 0.92f, 0.55f);
    private static readonly Color ColorError   = new(0.95f, 0.45f, 0.45f);
    private static readonly Color ColorInfo    = new(0.94f, 0.94f, 0.94f);
    private static readonly Color ColorWarn    = new(0.98f, 0.85f, 0.40f);

    // ----- Widgets (programmatic, no NodePath / no [Export]) -------------
    private Label _headerPathLabel;

    private SpinBox _moneySpin;
    private SpinBox _daySpin;
    private Label _saveMetaLabel;
    private ItemList _inventoryList;
    private LineEdit _addItemIdEdit;
    private SpinBox _addItemCountSpin;
    private Button _addItemButton;
    private Button _removeSelectedButton;

    private HSlider _bgmSlider;
    private Label _bgmValueLabel;
    private HSlider _sfxSlider;
    private Label _sfxValueLabel;
    private OptionButton _displayOption;

    private Button _saveGameBtn;
    private Button _loadGameBtn;
    private Button _deleteSaveBtn;
    private Button _saveSettingsBtn;
    private Button _loadSettingsBtn;
    private Button _deleteSettingsBtn;
    private Button _resetAllBtn;
    private Button _quitBtn;

    private Label _statusLabel;

    // =====================================================================
    public override void _Ready()
    {
        BuildUi();

        // 1) Settings load (silent on missing file).
        var (settingsLoaded, settingsErr) = _settingsSystem.Load();
        if (settingsErr == Error.Ok)
        {
            _settings = settingsLoaded;
        }
        else
        {
            _settings = SettingsData.CreateDefault();
        }
        ApplySettingsToUi(_settings);

        // 2) Save load (auto-attempt, fail-soft).
        var (saveLoaded, saveErr, saveSource) = _saveSystem.Load();
        switch (saveErr)
        {
            case SaveSystem.LoadError.Ok:
                ApplySaveToUi(saveLoaded);
                _lastLoadedSequence = saveLoaded.SaveSequence;
                _lastSavedAtUnix = saveLoaded.SavedAtUnix;
                SetStatus(
                    $"Loaded: source={saveSource}, save#={saveLoaded.SaveSequence}, money={saveLoaded.Money}, day={saveLoaded.Day}",
                    ColorSuccess);
                break;
            case SaveSystem.LoadError.NoSaveFound:
                SetStatus("No save found. Press Save Game to create initial save.", ColorInfo);
                break;
            case SaveSystem.LoadError.AllSourcesCorrupt:
                // Animal Well style fail-soft: warn, but continue with defaults.
                SetStatus(
                    "All save sources corrupt; starting fresh.",
                    ColorWarn);
                break;
        }
    }

    // =====================================================================
    // UI construction
    // =====================================================================
    private void BuildUi()
    {
        // Make this Control fill its parent (in case scene anchors aren't set).
        AnchorRight = 1f;
        AnchorBottom = 1f;
        OffsetRight = 0f;
        OffsetBottom = 0f;

        var root = new VBoxContainer
        {
            AnchorRight = 1f,
            AnchorBottom = 1f,
            OffsetLeft = 12,
            OffsetTop = 12,
            OffsetRight = -12,
            OffsetBottom = -12,
        };
        root.AddThemeConstantOverride("separation", 8);
        AddChild(root);

        // ----- Header -----------------------------------------------------
        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 12);
        var title = new Label { Text = "Persistence Sandbox" };
        title.AddThemeFontSizeOverride("font_size", 18);
        header.AddChild(title);

        var spacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        header.AddChild(spacer);

        _headerPathLabel = new Label
        {
            Text = $"user:// = {ProjectSettings.GlobalizePath("user://")}",
        };
        _headerPathLabel.AddThemeColorOverride("font_color", ColorInfo);
        header.AddChild(_headerPathLabel);
        root.AddChild(header);

        root.AddChild(new HSeparator());

        // ----- Two-column body --------------------------------------------
        var body = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        body.AddThemeConstantOverride("separation", 16);
        root.AddChild(body);

        body.AddChild(BuildGameStateColumn());
        body.AddChild(new VSeparator());
        body.AddChild(BuildSettingsColumn());

        root.AddChild(new HSeparator());

        // ----- Action rows ------------------------------------------------
        root.AddChild(BuildActionRow1());
        root.AddChild(BuildActionRow2());
        root.AddChild(BuildActionRow3());

        root.AddChild(new HSeparator());

        // ----- Status -----------------------------------------------------
        _statusLabel = new Label
        {
            Text = "Ready.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _statusLabel.AddThemeColorOverride("font_color", ColorInfo);
        root.AddChild(_statusLabel);
    }

    private Control BuildGameStateColumn()
    {
        var col = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        col.AddThemeConstantOverride("separation", 6);

        var heading = new Label { Text = "GAME STATE" };
        heading.AddThemeFontSizeOverride("font_size", 14);
        col.AddChild(heading);

        // Money row
        var moneyRow = new HBoxContainer();
        moneyRow.AddChild(new Label { Text = "Money:", CustomMinimumSize = new Vector2(64, 0) });
        _moneySpin = new SpinBox
        {
            MinValue = 0,
            MaxValue = 9_999_999,
            Step = 1,
            Value = 0,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        moneyRow.AddChild(_moneySpin);
        col.AddChild(moneyRow);

        // Day row
        var dayRow = new HBoxContainer();
        dayRow.AddChild(new Label { Text = "Day:", CustomMinimumSize = new Vector2(64, 0) });
        _daySpin = new SpinBox
        {
            MinValue = 1,
            MaxValue = 9_999,
            Step = 1,
            Value = 1,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        dayRow.AddChild(_daySpin);
        col.AddChild(dayRow);

        // Save# / Saved-at meta
        _saveMetaLabel = new Label { Text = "Save#: 0   Saved: -" };
        _saveMetaLabel.AddThemeColorOverride("font_color", ColorInfo);
        col.AddChild(_saveMetaLabel);

        // Inventory list
        var invHeading = new Label { Text = "Inventory" };
        col.AddChild(invHeading);

        _inventoryList = new ItemList
        {
            SelectMode = ItemList.SelectModeEnum.Single,
            CustomMinimumSize = new Vector2(0, 160),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        col.AddChild(_inventoryList);

        // Add row
        var addLabel = new Label { Text = "Add:" };
        col.AddChild(addLabel);

        var addRow = new HBoxContainer();
        addRow.AddThemeConstantOverride("separation", 6);
        _addItemIdEdit = new LineEdit
        {
            PlaceholderText = "item_id",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        addRow.AddChild(_addItemIdEdit);

        _addItemCountSpin = new SpinBox
        {
            MinValue = 1,
            MaxValue = 9999,
            Step = 1,
            Value = 1,
        };
        addRow.AddChild(_addItemCountSpin);
        col.AddChild(addRow);

        // Inventory action row
        var invButtons = new HBoxContainer();
        invButtons.AddThemeConstantOverride("separation", 6);
        _addItemButton = new Button
        {
            Text = "Add Item",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _addItemButton.Pressed += OnAddItemPressed;
        invButtons.AddChild(_addItemButton);

        _removeSelectedButton = new Button
        {
            Text = "Remove Selected",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _removeSelectedButton.Pressed += OnRemoveSelectedPressed;
        invButtons.AddChild(_removeSelectedButton);
        col.AddChild(invButtons);

        return col;
    }

    private Control BuildSettingsColumn()
    {
        var col = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        col.AddThemeConstantOverride("separation", 6);

        var heading = new Label { Text = "SETTINGS" };
        heading.AddThemeFontSizeOverride("font_size", 14);
        col.AddChild(heading);

        // BGM volume row
        var bgmRow = new HBoxContainer();
        bgmRow.AddChild(new Label { Text = "BGM Volume", CustomMinimumSize = new Vector2(96, 0) });
        _bgmSlider = new HSlider
        {
            MinValue = 0,
            MaxValue = 1,
            Step = 0.01,
            Value = 0.8,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _bgmSlider.ValueChanged += OnBgmChanged;
        bgmRow.AddChild(_bgmSlider);
        _bgmValueLabel = new Label { Text = "0.80", CustomMinimumSize = new Vector2(48, 0) };
        bgmRow.AddChild(_bgmValueLabel);
        col.AddChild(bgmRow);

        // SFX volume row
        var sfxRow = new HBoxContainer();
        sfxRow.AddChild(new Label { Text = "SFX Volume", CustomMinimumSize = new Vector2(96, 0) });
        _sfxSlider = new HSlider
        {
            MinValue = 0,
            MaxValue = 1,
            Step = 0.01,
            Value = 1.0,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _sfxSlider.ValueChanged += OnSfxChanged;
        sfxRow.AddChild(_sfxSlider);
        _sfxValueLabel = new Label { Text = "1.00", CustomMinimumSize = new Vector2(48, 0) };
        sfxRow.AddChild(_sfxValueLabel);
        col.AddChild(sfxRow);

        // Display mode row
        var dispRow = new HBoxContainer();
        dispRow.AddChild(new Label { Text = "Display", CustomMinimumSize = new Vector2(96, 0) });
        _displayOption = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _displayOption.AddItem("Windowed", SettingsData.DisplayModeWindowed);
        _displayOption.AddItem("Borderless", SettingsData.DisplayModeBorderless);
        _displayOption.AddItem("Fullscreen", SettingsData.DisplayModeFullscreen);
        _displayOption.ItemSelected += OnDisplayModeSelected;
        dispRow.AddChild(_displayOption);
        col.AddChild(dispRow);

        // Filler so column heights stay balanced.
        var filler = new Control { SizeFlagsVertical = SizeFlags.ExpandFill };
        col.AddChild(filler);

        return col;
    }

    private Control BuildActionRow1()
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);

        _saveGameBtn = MakeExpandButton("Save Game");
        _saveGameBtn.Pressed += OnSaveGamePressed;
        row.AddChild(_saveGameBtn);

        _loadGameBtn = MakeExpandButton("Load Game");
        _loadGameBtn.Pressed += OnLoadGamePressed;
        row.AddChild(_loadGameBtn);

        _deleteSaveBtn = MakeExpandButton("Delete Save");
        _deleteSaveBtn.Pressed += OnDeleteSavePressed;
        row.AddChild(_deleteSaveBtn);

        return row;
    }

    private Control BuildActionRow2()
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);

        _saveSettingsBtn = MakeExpandButton("Save Settings");
        _saveSettingsBtn.Pressed += OnSaveSettingsPressed;
        row.AddChild(_saveSettingsBtn);

        _loadSettingsBtn = MakeExpandButton("Load Settings");
        _loadSettingsBtn.Pressed += OnLoadSettingsPressed;
        row.AddChild(_loadSettingsBtn);

        _deleteSettingsBtn = MakeExpandButton("Delete Settings");
        _deleteSettingsBtn.Pressed += OnDeleteSettingsPressed;
        row.AddChild(_deleteSettingsBtn);

        return row;
    }

    private Control BuildActionRow3()
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);

        _resetAllBtn = MakeExpandButton("Reset All");
        _resetAllBtn.Pressed += OnResetAllPressed;
        row.AddChild(_resetAllBtn);

        _quitBtn = MakeExpandButton("Quit");
        _quitBtn.Pressed += OnQuitPressed;
        row.AddChild(_quitBtn);

        return row;
    }

    private static Button MakeExpandButton(string text)
    {
        return new Button
        {
            Text = text,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
    }

    // =====================================================================
    // UI <-> data sync
    // =====================================================================
    private void ApplySaveToUi(SaveData data)
    {
        _moneySpin.Value = data.Money;
        _daySpin.Value = data.Day;

        _inventory.ReplaceAll(data.InventoryCounts);
        RefreshInventoryList();

        UpdateSaveMetaLabel(data.SaveSequence, data.SavedAtUnix);
    }

    private void ApplySettingsToUi(SettingsData data)
    {
        var c = data.Clamped();
        _bgmSlider.SetValueNoSignal(c.BgmVolume);
        _bgmValueLabel.Text = c.BgmVolume.ToString("0.00");
        _sfxSlider.SetValueNoSignal(c.SfxVolume);
        _sfxValueLabel.Text = c.SfxVolume.ToString("0.00");
        _displayOption.Select(c.DisplayMode);
        _settings = c;
    }

    private void RefreshInventoryList()
    {
        _inventoryList.Clear();
        foreach (var kv in _inventory.Counts)
        {
            int idx = _inventoryList.AddItem($"{kv.Key} x {kv.Value}");
            _inventoryList.SetItemMetadata(idx, kv.Key);
        }
    }

    private void UpdateSaveMetaLabel(long sequence, double savedAtUnix)
    {
        string savedAtText = "-";
        if (savedAtUnix > 0)
        {
            try
            {
                savedAtText = DateTimeOffset
                    .FromUnixTimeSeconds((long)savedAtUnix)
                    .ToLocalTime()
                    .ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch (ArgumentOutOfRangeException)
            {
                savedAtText = "(invalid)";
            }
        }
        _saveMetaLabel.Text = $"Save#: {sequence}   Saved: {savedAtText}";
    }

    private SaveData BuildSaveDataFromUi()
    {
        var data = new SaveData
        {
            Money = (int)_moneySpin.Value,
            Day = (int)_daySpin.Value,
            InventoryCounts = new Dictionary<string, int>(),
        };
        foreach (var kv in _inventory.Counts)
            data.InventoryCounts[kv.Key] = kv.Value;
        return data;
    }

    // =====================================================================
    // Status
    // =====================================================================
    private void SetStatus(string text, Color color)
    {
        _statusLabel.Text = $"Status: {text}";
        _statusLabel.AddThemeColorOverride("font_color", color);
    }

    // =====================================================================
    // Settings handlers (idempotent updates to _settings)
    // =====================================================================
    private void OnBgmChanged(double value)
    {
        float f = (float)value;
        _settings.BgmVolume = f;
        _bgmValueLabel.Text = f.ToString("0.00");
    }

    private void OnSfxChanged(double value)
    {
        float f = (float)value;
        _settings.SfxVolume = f;
        _sfxValueLabel.Text = f.ToString("0.00");
    }

    private void OnDisplayModeSelected(long index)
    {
        _settings.DisplayMode = (int)index;
    }

    // =====================================================================
    // Inventory handlers
    // =====================================================================
    private void OnAddItemPressed()
    {
        string id = _addItemIdEdit.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(id))
        {
            SetStatus("Add Item: item_id is empty.", ColorError);
            return;
        }
        int count = (int)_addItemCountSpin.Value;
        if (count < 1) count = 1;

        _inventory.Add(id, count);
        RefreshInventoryList();
        SetStatus($"Added {id} x {count}.", ColorSuccess);
    }

    private void OnRemoveSelectedPressed()
    {
        var selected = _inventoryList.GetSelectedItems();
        if (selected.Length == 0)
        {
            SetStatus("Remove Selected: no item selected.", ColorError);
            return;
        }
        int idx = selected[0];
        var meta = _inventoryList.GetItemMetadata(idx);
        string id = meta.AsString();
        if (string.IsNullOrEmpty(id))
        {
            SetStatus("Remove Selected: missing item id metadata.", ColorError);
            return;
        }
        bool removed = _inventory.Remove(id, 1);
        RefreshInventoryList();
        if (removed)
            SetStatus($"Removed {id} x 1.", ColorSuccess);
        else
            SetStatus($"Remove Selected: {id} not present.", ColorError);
    }

    // =====================================================================
    // Game save / load handlers
    // =====================================================================
    private void OnSaveGamePressed()
    {
        var data = BuildSaveDataFromUi();
        long nextSequence = _lastLoadedSequence + 1;
        data.SaveSequence = nextSequence;
        data.SavedAtUnix = (double)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var saveErr = _saveSystem.Save(data);
        if (saveErr == SaveSystem.SaveError.Ok)
        {
            _lastLoadedSequence = nextSequence;
            _lastSavedAtUnix = data.SavedAtUnix;
            UpdateSaveMetaLabel(nextSequence, data.SavedAtUnix);
            SetStatus($"Saved game (save#={nextSequence}).", ColorSuccess);
        }
        else
        {
            SetStatus($"Save Game failed: {saveErr}.", ColorError);
        }
    }

    private void OnLoadGamePressed()
    {
        var (data, loadErr, source) = _saveSystem.Load();
        switch (loadErr)
        {
            case SaveSystem.LoadError.Ok:
                ApplySaveToUi(data);
                _lastLoadedSequence = data.SaveSequence;
                _lastSavedAtUnix = data.SavedAtUnix;
                SetStatus(
                    $"Loaded game (source={source}, save#={data.SaveSequence}, money={data.Money}, day={data.Day}).",
                    ColorSuccess);
                break;
            case SaveSystem.LoadError.NoSaveFound:
                SetStatus("Load Game: no save file found.", ColorError);
                break;
            case SaveSystem.LoadError.AllSourcesCorrupt:
                ResetGameStateToDefault();
                SetStatus(
                    "All save sources corrupt; restored defaults.",
                    ColorWarn);
                break;
        }
    }

    private void OnDeleteSavePressed()
    {
        bool removed = _saveSystem.DeleteAllSaveFiles();
        if (removed)
            SetStatus("Deleted save file(s).", ColorSuccess);
        else
            SetStatus("Delete Save: nothing to delete or partial failure.", ColorInfo);
    }

    // =====================================================================
    // Settings save / load handlers
    // =====================================================================
    private void OnSaveSettingsPressed()
    {
        var clamped = _settings.Clamped();
        var err = _settingsSystem.Save(clamped);
        if (err == Error.Ok)
        {
            _settings = clamped;
            ApplySettingsToUi(_settings);
            SetStatus("Saved settings.", ColorSuccess);
        }
        else
        {
            SetStatus($"Save Settings failed: {err}.", ColorError);
        }
    }

    private void OnLoadSettingsPressed()
    {
        var (data, err) = _settingsSystem.Load();
        if (err == Error.Ok)
        {
            _settings = data;
            ApplySettingsToUi(_settings);
            SetStatus("Loaded settings.", ColorSuccess);
        }
        else if (err == Error.FileNotFound)
        {
            SetStatus("Load Settings: no settings file found.", ColorError);
        }
        else
        {
            SetStatus($"Load Settings failed: {err}.", ColorError);
        }
    }

    private void OnDeleteSettingsPressed()
    {
        bool removed = _settingsSystem.DeleteSettingsFile();
        if (removed)
            SetStatus("Deleted settings file.", ColorSuccess);
        else
            SetStatus("Delete Settings: nothing to delete.", ColorInfo);
    }

    // =====================================================================
    // Reset / Quit
    // =====================================================================
    private void OnResetAllPressed()
    {
        ResetGameStateToDefault();
        _settings = SettingsData.CreateDefault();
        ApplySettingsToUi(_settings);
        SetStatus("Reset all in-memory state to defaults (no files deleted).", ColorInfo);
    }

    private void ResetGameStateToDefault()
    {
        _inventory.Clear();
        RefreshInventoryList();
        _moneySpin.Value = 0;
        _daySpin.Value = 1;
        _lastSavedAtUnix = 0;
        UpdateSaveMetaLabel(_lastLoadedSequence, 0);
    }

    private void OnQuitPressed()
    {
        GetTree().Quit();
    }
}
