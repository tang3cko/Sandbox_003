using Godot;
using VoxelTerrain.Core;

namespace VoxelTerrain.World;

public partial class VoxelChunk : Node3D
{
    [Export] public int SampleSize { get; set; } = 32;
    [Export] public StandardMaterial3D Material { get; set; }

    // Hybrid collision update strategy:
    //   - Defer up to CollisionUpdateDelay seconds since the last edit (lets very rapid taps coalesce)
    //   - But if more than MaxPendingEdits edits accumulate, force a flush so raycasts don't drift
    //     too far from the visible mesh.
    [Export] public float CollisionUpdateDelay { get; set; } = 0.06f;
    [Export] public int MaxPendingEdits { get; set; } = 2;

    public DensityField Field { get; private set; }
    public System.Func<System.Numerics.Vector3, System.Numerics.Vector4> ColorSampler { get; set; }

    private MeshInstance3D _meshInstance;
    private StaticBody3D _body;
    private CollisionShape3D _collisionShape;

    private MeshData _pendingCollisionMesh;
    private float _collisionDirtyTimer;
    private bool _collisionDirty;
    private bool _initialCollisionDone;
    private int _pendingEditCount;

    public override void _Ready()
    {
        Field = new DensityField(SampleSize);

        _meshInstance = new MeshInstance3D { Name = "Mesh" };
        AddChild(_meshInstance);

        _body = new StaticBody3D { Name = "Body" };
        _collisionShape = new CollisionShape3D { Name = "Shape" };
        _body.AddChild(_collisionShape);
        AddChild(_body);

        if (Material == null)
        {
            Material = new StandardMaterial3D
            {
                VertexColorUseAsAlbedo = true,
                Roughness = 0.85f,
            };
        }

        SetProcess(false);
    }

    public void Rebuild()
    {
        var meshData = ColorSampler != null
            ? MarchingCubesMesher.Build(Field, ColorSampler)
            : MarchingCubesMesher.Build(Field);

        // Visual mesh updates immediately so the player sees the dig instantly.
        _meshInstance.Mesh = meshData.IsEmpty ? null : ToArrayMesh(meshData);

        // Physics shape rebuild is deferred — schedule it instead of running it inline.
        _pendingCollisionMesh = meshData;
        _collisionDirty = true;
        _collisionDirtyTimer = 0f;
        _pendingEditCount++;

        // Initial build: flush collision immediately so the physics shape is ready
        // before the player can interact with the terrain.
        if (!_initialCollisionDone || _pendingEditCount >= MaxPendingEdits)
        {
            FlushCollision();
            _initialCollisionDone = true;
            return;
        }

        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        if (!_collisionDirty)
        {
            SetProcess(false);
            return;
        }

        _collisionDirtyTimer += (float)delta;
        if (_collisionDirtyTimer < CollisionUpdateDelay) return;

        FlushCollision();
        SetProcess(false);
    }

    private void FlushCollision()
    {
        _collisionShape.Shape = (_pendingCollisionMesh == null || _pendingCollisionMesh.IsEmpty)
            ? null
            : BuildCollisionShape(_pendingCollisionMesh);
        _pendingCollisionMesh = null;
        _collisionDirty = false;
        _pendingEditCount = 0;
    }

    private ArrayMesh ToArrayMesh(MeshData data)
    {
        int vertexCount = data.Vertices.Count;
        var vertices = new Vector3[vertexCount];
        var normals = new Vector3[vertexCount];
        var colors = new Color[vertexCount];
        for (int i = 0; i < vertexCount; i++)
        {
            var v = data.Vertices[i];
            var n = data.Normals[i];
            var c = data.Colors[i];
            vertices[i] = new Vector3(v.X, v.Y, v.Z);
            normals[i] = new Vector3(n.X, n.Y, n.Z);
            colors[i] = new Color(c.X, c.Y, c.Z, c.W);
        }
        var indices = data.Indices.ToArray();

        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.Normal] = normals;
        arrays[(int)Mesh.ArrayType.Color] = colors;
        arrays[(int)Mesh.ArrayType.Index] = indices;

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        mesh.SurfaceSetMaterial(0, Material);
        return mesh;
    }

    private static ConcavePolygonShape3D BuildCollisionShape(MeshData data)
    {
        var triPoints = new Vector3[data.Indices.Count];
        for (int i = 0; i < data.Indices.Count; i++)
        {
            var v = data.Vertices[data.Indices[i]];
            triPoints[i] = new Vector3(v.X, v.Y, v.Z);
        }
        return new ConcavePolygonShape3D { Data = triPoints };
    }
}
