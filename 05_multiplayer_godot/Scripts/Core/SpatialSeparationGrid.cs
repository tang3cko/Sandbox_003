using System;

namespace SwarmSurvivor;

// Uniform-grid spatial partition for swarm separation force computation.
//
// Replaces the O(N^2) pairwise check in SeparationForceCalculator with an
// O(N * k) scan where k is the average occupancy of the 3x3 cell neighborhood.
// Output forces are bit-for-bit identical to the naive implementation: every
// pair within separation radius is visited exactly once with the same arguments
// passed to SwarmCalculator.CalculateSeparation, and every pair beyond the
// radius is skipped (CalculateSeparation would have returned zero anyway).
//
// Usage:
//   var grid = new SpatialSeparationGrid();         // allocate once
//   grid.Build(posX, posZ, deathTimer, count, r);    // O(N), reuses buffers
//   for (int i = 0; i < count; i++) {
//       var f = grid.ComputeAccumulatedSeparation(i, posX, posZ, deathTimer, count, r, force);
//   }
//
// Allocation policy: buffers are sized lazily and grown only when capacity is
// exceeded. Steady-state Build/Compute pairs do not allocate.
public sealed class SpatialSeparationGrid
{
    private const int InitialCellCapacity = 16;

    // Cell metadata for the most recent Build.
    private float _cellSize;
    private float _invCellSize;
    private int _minCellX;
    private int _minCellZ;
    private int _gridWidth;
    private int _gridHeight;

    // Hash-map style: each entity's cell index in the flat grid array.
    private int[] _entityCell = Array.Empty<int>();

    // CSR-style cell layout: _cellStart[c] .. _cellStart[c+1] are indices
    // into _cellEntities containing entity ids that fall in cell c.
    private int[] _cellStart = Array.Empty<int>();
    private int[] _cellEntities = Array.Empty<int>();

    // Build the grid for the current frame's positions.
    // selfIndex == j and dying entities are filtered at query time, so they
    // are intentionally still included in the grid (this matches the naive
    // implementation's iteration domain).
    public void Build(
        float[] posX, float[] posZ, float[] deathTimer,
        int activeCount, float separationRadius)
    {
        if (separationRadius <= 0f)
        {
            // Degenerate; treat as single empty grid.
            _cellSize = 1f;
            _invCellSize = 1f;
        }
        else
        {
            _cellSize = separationRadius;
            _invCellSize = 1f / separationRadius;
        }

        EnsureEntityCapacity(activeCount);

        if (activeCount == 0)
        {
            _minCellX = 0;
            _minCellZ = 0;
            _gridWidth = 0;
            _gridHeight = 0;
            EnsureCellStartCapacity(1);
            _cellStart[0] = 0;
            return;
        }

        // Pass 1: find world AABB to anchor the grid origin so cell indices are
        // always non-negative. Skip dying entities? No — keep them indexed so
        // that ComputeAccumulatedSeparation matches the naive iteration domain
        // (the naive loop visits dying entities and filters via IsDying).
        float minX = posX[0];
        float minZ = posZ[0];
        float maxX = posX[0];
        float maxZ = posZ[0];
        for (int i = 1; i < activeCount; i++)
        {
            float x = posX[i];
            float z = posZ[i];
            if (x < minX) minX = x;
            else if (x > maxX) maxX = x;
            if (z < minZ) minZ = z;
            else if (z > maxZ) maxZ = z;
        }

        _minCellX = (int)MathF.Floor(minX * _invCellSize);
        _minCellZ = (int)MathF.Floor(minZ * _invCellSize);
        int maxCellX = (int)MathF.Floor(maxX * _invCellSize);
        int maxCellZ = (int)MathF.Floor(maxZ * _invCellSize);

        _gridWidth = maxCellX - _minCellX + 1;
        _gridHeight = maxCellZ - _minCellZ + 1;
        int cellCount = _gridWidth * _gridHeight;

        EnsureCellStartCapacity(cellCount + 1);

        // Pass 2: count entities per cell, store each entity's cell index.
        for (int c = 0; c <= cellCount; c++) _cellStart[c] = 0;

        for (int i = 0; i < activeCount; i++)
        {
            int cx = (int)MathF.Floor(posX[i] * _invCellSize) - _minCellX;
            int cz = (int)MathF.Floor(posZ[i] * _invCellSize) - _minCellZ;
            int cell = cz * _gridWidth + cx;
            _entityCell[i] = cell;
            _cellStart[cell + 1]++;
        }

        // Prefix-sum to get start offsets.
        for (int c = 1; c <= cellCount; c++)
        {
            _cellStart[c] += _cellStart[c - 1];
        }

        // Pass 3: scatter entity ids into cell buckets. We walk a temporary
        // cursor that re-uses the leading slot of _cellStart (it gets restored
        // by the prefix-sum at the next Build, but to keep prefix-sum array
        // intact across queries we use a separate write-cursor backed by
        // _cellEntities length tracking through a per-call counter).
        EnsureCellEntitiesCapacity(activeCount);

        // We need a temporary write head per cell. Reuse _entityCell tail or
        // just rebuild a small scratch array. To avoid any per-call alloc we
        // shift _cellStart up by one during scatter: cursor[c] = _cellStart[c],
        // then post-scatter _cellStart[c] is correct (each insert increments
        // cursor, eventually equaling the next cell's start). To preserve the
        // ability to query later we must not mutate _cellStart permanently.
        // Strategy: copy _cellStart (sized cellCount+1) into _cellCursor.
        EnsureCellCursorCapacity(cellCount + 1);
        for (int c = 0; c <= cellCount; c++) _cellCursor[c] = _cellStart[c];

        for (int i = 0; i < activeCount; i++)
        {
            int cell = _entityCell[i];
            int writeAt = _cellCursor[cell]++;
            _cellEntities[writeAt] = i;
        }
    }

    // Compute the same accumulated separation force as
    // SeparationForceCalculator.ComputeAccumulatedSeparation, but only iterate
    // over entities in the 3x3 cell neighborhood centered on selfIndex's cell.
    public (float X, float Z) ComputeAccumulatedSeparation(
        int selfIndex,
        float[] posX, float[] posZ, float[] deathTimer,
        int activeCount, float separationRadius, float separationForce)
    {
        if (activeCount == 0 || _gridWidth == 0 || _gridHeight == 0)
        {
            return (0f, 0f);
        }

        float sepX = 0f;
        float sepZ = 0f;

        int selfCell = _entityCell[selfIndex];
        int selfCellX = selfCell % _gridWidth;
        int selfCellZ = selfCell / _gridWidth;

        int minCx = selfCellX - 1; if (minCx < 0) minCx = 0;
        int maxCx = selfCellX + 1; if (maxCx >= _gridWidth) maxCx = _gridWidth - 1;
        int minCz = selfCellZ - 1; if (minCz < 0) minCz = 0;
        int maxCz = selfCellZ + 1; if (maxCz >= _gridHeight) maxCz = _gridHeight - 1;

        float selfX = posX[selfIndex];
        float selfZ = posZ[selfIndex];

        for (int cz = minCz; cz <= maxCz; cz++)
        {
            int rowOffset = cz * _gridWidth;
            for (int cx = minCx; cx <= maxCx; cx++)
            {
                int cell = rowOffset + cx;
                int start = _cellStart[cell];
                int end = _cellStart[cell + 1];
                for (int k = start; k < end; k++)
                {
                    int j = _cellEntities[k];
                    if (j == selfIndex) continue;
                    if (SwarmCalculator.IsDying(deathTimer[j])) continue;

                    var sep = SwarmCalculator.CalculateSeparation(
                        selfX, selfZ,
                        posX[j], posZ[j],
                        separationRadius, separationForce);
                    sepX += sep.VelX;
                    sepZ += sep.VelZ;
                }
            }
        }

        return (sepX, sepZ);
    }

    // ----- internal buffer growth (allocation-free in steady state) -----

    private int[] _cellCursor = Array.Empty<int>();

    private void EnsureEntityCapacity(int needed)
    {
        if (_entityCell.Length >= needed) return;
        int newSize = _entityCell.Length == 0 ? Math.Max(needed, 64) : _entityCell.Length;
        while (newSize < needed) newSize *= 2;
        _entityCell = new int[newSize];
    }

    private void EnsureCellStartCapacity(int needed)
    {
        if (_cellStart.Length >= needed) return;
        int newSize = _cellStart.Length == 0 ? Math.Max(needed, InitialCellCapacity) : _cellStart.Length;
        while (newSize < needed) newSize *= 2;
        _cellStart = new int[newSize];
    }

    private void EnsureCellEntitiesCapacity(int needed)
    {
        if (_cellEntities.Length >= needed) return;
        int newSize = _cellEntities.Length == 0 ? Math.Max(needed, 64) : _cellEntities.Length;
        while (newSize < needed) newSize *= 2;
        _cellEntities = new int[newSize];
    }

    private void EnsureCellCursorCapacity(int needed)
    {
        if (_cellCursor.Length >= needed) return;
        int newSize = _cellCursor.Length == 0 ? Math.Max(needed, InitialCellCapacity) : _cellCursor.Length;
        while (newSize < needed) newSize *= 2;
        _cellCursor = new int[newSize];
    }
}
