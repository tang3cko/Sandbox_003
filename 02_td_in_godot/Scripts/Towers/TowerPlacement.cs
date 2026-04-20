namespace TowerDefense;

using Godot;
using ReactiveSO;

public partial class TowerPlacement : Node3D
{
    [Export] private Node3DRuntimeSet _placedTowers;
    [Export] private IntVariable _gold;
    [Export] private VoidEventChannel _onTowerPlaced;

    private Camera3D _camera;
    private GameManager _gameManager;

    private TowerConfig _selectedTower;
    private MeshInstance3D _preview;
    private bool _canPlace;
    private Vector3 _placementPosition;
    private const float GridSize = 2.0f;

    public void Setup(Camera3D camera, GameManager gameManager)
    {
        _camera = camera;
        _gameManager = gameManager;
    }

    public void SelectTower(TowerConfig config)
    {
        _selectedTower = config;
        if (_preview != null)
        {
            _preview.QueueFree();
            _preview = null;
        }

        if (config == null) return;

        _preview = new MeshInstance3D();
        var box = new BoxMesh();
        box.Size = new Vector3(1.5f, 0.8f, 1.5f);
        _preview.Mesh = box;
        var mat = new StandardMaterial3D();
        mat.AlbedoColor = new Color(config.Color, 0.5f);
        mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        _preview.MaterialOverride = mat;
        AddChild(_preview);
    }

    public void CancelSelection()
    {
        _selectedTower = null;
        if (_preview != null)
        {
            _preview.QueueFree();
            _preview = null;
        }
    }

    public override void _Process(double delta)
    {
        if (_selectedTower == null || _preview == null || _camera == null) return;

        // Raycast from mouse to ground plane (Y=0)
        var mousePos = GetViewport().GetMousePosition();
        var from = _camera.ProjectRayOrigin(mousePos);
        var dir = _camera.ProjectRayNormal(mousePos);

        if (dir.Y == 0) return;
        var t = -from.Y / dir.Y;
        if (t < 0) return;

        var worldPos = from + dir * t;

        // Snap to grid
        _placementPosition = new Vector3(
            Mathf.Round(worldPos.X / GridSize) * GridSize,
            0,
            Mathf.Round(worldPos.Z / GridSize) * GridSize
        );

        _preview.GlobalPosition = _placementPosition + new Vector3(0, 0.4f, 0);

        // Check if placement is valid
        _canPlace = CanPlaceAt(_placementPosition) && _gold.Value >= _selectedTower.Cost;

        if (_preview.MaterialOverride is StandardMaterial3D previewMat)
        {
            previewMat.AlbedoColor = _canPlace
                ? new Color(_selectedTower.Color, 0.5f)
                : new Color(Colors.Red, 0.5f);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_selectedTower == null) return;

        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
        {
            if (_canPlace)
            {
                PlaceTower();
            }
        }
        else if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true })
        {
            CancelSelection();
        }
    }

    private void PlaceTower()
    {
        var towerScene = GD.Load<PackedScene>("res://Scenes/Tower.tscn");
        var tower = towerScene.Instantiate<Tower>();
        tower.Config = _selectedTower;
        tower.Position = _placementPosition;
        var result = _gameManager.TryPlaceTower(_selectedTower.Cost);
        if (!result.CanAfford)
        {
            tower.QueueFree();
            return;
        }

        GetTree().Root.GetNode("Main/Towers").AddChild(tower);
        _onTowerPlaced?.Raise();

        CancelSelection();
    }

    private bool CanPlaceAt(Vector3 position)
    {
        if (_placedTowers == null) return true;

        foreach (var item in _placedTowers.Items)
        {
            if (item is Node3D node && node.GlobalPosition.DistanceTo(position) < GridSize * 0.9f)
                return false;
        }
        return true;
    }
}
