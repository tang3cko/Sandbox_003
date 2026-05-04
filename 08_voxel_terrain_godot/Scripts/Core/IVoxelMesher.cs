namespace VoxelTerrain.Core;

public interface IVoxelMesher
{
    MeshData Build(ChunkData data);
}

public sealed class BlockyVoxelMesher : IVoxelMesher
{
    public MeshData Build(ChunkData data) => BlockyMeshBuilder.Build(data);
}
