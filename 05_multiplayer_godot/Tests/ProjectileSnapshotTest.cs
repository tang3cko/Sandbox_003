using Xunit;

namespace SwarmSurvivor.Tests;

public class ProjectileSnapshotTest
{
    private const int Capacity = 200;

    [Fact]
    public void CalculateBufferSize_Zero_ReturnsHeaderOnly()
    {
        Assert.Equal(3, ProjectileSnapshot.CalculateBufferSize(0));
    }

    [Fact]
    public void CalculateBufferSize_Linear()
    {
        Assert.Equal(3 + 24, ProjectileSnapshot.CalculateBufferSize(1));
        Assert.Equal(3 + 24 * 100, ProjectileSnapshot.CalculateBufferSize(100));
    }

    [Fact]
    public void RoundTrip_EmptySnapshot_DecodesToZero()
    {
        var (px, pz, dx, dz, lt, oid, cidx, pid) = AllocSoA(Capacity);
        var buf = new byte[ProjectileSnapshot.CalculateBufferSize(0)];

        int written = ProjectileSnapshot.Encode(buf, px, pz, dx, dz, lt, oid, cidx, pid, 0);
        Assert.Equal(3, written);

        var (px2, pz2, dx2, dz2, lt2, oid2, cidx2, pid2) = AllocSoA(Capacity);
        int decoded = ProjectileSnapshot.Decode(buf, written, px2, pz2, dx2, dz2, lt2, oid2, cidx2, pid2);
        Assert.Equal(0, decoded);
    }

    [Fact]
    public void RoundTrip_SingleEntity_PreservesAllFields()
    {
        var (px, pz, dx, dz, lt, oid, cidx, pid) = AllocSoA(Capacity);
        px[0] = 12.5f;
        pz[0] = -7.25f;
        dx[0] = 0.5f;
        dz[0] = -0.75f;
        lt[0] = 1.5f;
        oid[0] = 42;
        cidx[0] = 3;
        pid[0] = 7;

        var buf = new byte[ProjectileSnapshot.CalculateBufferSize(1)];
        ProjectileSnapshot.Encode(buf, px, pz, dx, dz, lt, oid, cidx, pid, 1);

        var (px2, pz2, dx2, dz2, lt2, oid2, cidx2, pid2) = AllocSoA(Capacity);
        int decoded = ProjectileSnapshot.Decode(buf, buf.Length, px2, pz2, dx2, dz2, lt2, oid2, cidx2, pid2);

        Assert.Equal(1, decoded);
        Assert.Equal(12.5f, px2[0]);
        Assert.Equal(-7.25f, pz2[0]);
        Assert.Equal(0.5f, dx2[0]);
        Assert.Equal(-0.75f, dz2[0]);
        Assert.Equal(1.5f, lt2[0]);
        Assert.Equal(42, oid2[0]);
        Assert.Equal(3, cidx2[0]);
        Assert.Equal(7, pid2[0]);
    }

    [Fact]
    public void RoundTrip_FullCapacity()
    {
        const int N = 200;
        var (px, pz, dx, dz, lt, oid, cidx, pid) = AllocSoA(Capacity);
        for (int i = 0; i < N; i++)
        {
            px[i] = i * 0.5f;
            pz[i] = -i * 0.25f;
            dx[i] = (i % 8) * 0.125f;
            dz[i] = -(i % 4) * 0.25f;
            lt[i] = i * 0.0625f;
            oid[i] = i % 256;
            cidx[i] = i % 16;
            pid[i] = i + 1;
        }

        var buf = new byte[ProjectileSnapshot.CalculateBufferSize(N)];
        ProjectileSnapshot.Encode(buf, px, pz, dx, dz, lt, oid, cidx, pid, N);

        var (px2, pz2, dx2, dz2, lt2, oid2, cidx2, pid2) = AllocSoA(Capacity);
        int decoded = ProjectileSnapshot.Decode(buf, buf.Length, px2, pz2, dx2, dz2, lt2, oid2, cidx2, pid2);

        Assert.Equal(N, decoded);
        for (int i = 0; i < N; i++)
        {
            Assert.Equal(px[i], px2[i]);
            Assert.Equal(pz[i], pz2[i]);
            Assert.Equal(dx[i], dx2[i]);
            Assert.Equal(dz[i], dz2[i]);
            Assert.Equal(lt[i], lt2[i]);
            Assert.Equal(oid[i], oid2[i]);
            Assert.Equal(cidx[i], cidx2[i]);
            Assert.Equal(pid[i], pid2[i]);
        }
    }

    [Fact]
    public void Encode_BufferTooSmall_Throws()
    {
        var (px, pz, dx, dz, lt, oid, cidx, pid) = AllocSoA(Capacity);
        var buf = new byte[10];
        Assert.Throws<System.ArgumentException>(() =>
            ProjectileSnapshot.Encode(buf, px, pz, dx, dz, lt, oid, cidx, pid, 1));
    }

    [Fact]
    public void Encode_NegativeCount_Throws()
    {
        var (px, pz, dx, dz, lt, oid, cidx, pid) = AllocSoA(Capacity);
        var buf = new byte[ProjectileSnapshot.CalculateBufferSize(1)];
        Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            ProjectileSnapshot.Encode(buf, px, pz, dx, dz, lt, oid, cidx, pid, -1));
    }

    [Fact]
    public void Decode_TruncatedHeader_ReturnsZero()
    {
        var (px, pz, dx, dz, lt, oid, cidx, pid) = AllocSoA(Capacity);
        Assert.Equal(0, ProjectileSnapshot.Decode(new byte[1], 1, px, pz, dx, dz, lt, oid, cidx, pid));
        Assert.Equal(0, ProjectileSnapshot.Decode(new byte[2], 2, px, pz, dx, dz, lt, oid, cidx, pid));
    }

    [Fact]
    public void Decode_TruncatedBody_ReturnsZero()
    {
        var (px, pz, dx, dz, lt, oid, cidx, pid) = AllocSoA(Capacity);
        var buf = new byte[ProjectileSnapshot.CalculateBufferSize(2)];
        buf[0] = ProjectileSnapshot.Version; buf[1] = 5; buf[2] = 0;
        Assert.Equal(0, ProjectileSnapshot.Decode(buf, buf.Length, px, pz, dx, dz, lt, oid, cidx, pid));
    }

    [Fact]
    public void Decode_OutputBufferSmaller_CapsAtOutputLength()
    {
        var (px, pz, dx, dz, lt, oid, cidx, pid) = AllocSoA(Capacity);
        for (int i = 0; i < 100; i++) px[i] = i;

        var buf = new byte[ProjectileSnapshot.CalculateBufferSize(100)];
        ProjectileSnapshot.Encode(buf, px, pz, dx, dz, lt, oid, cidx, pid, 100);

        var smallPx = new float[50];
        var smallPz = new float[50];
        var smallDx = new float[50];
        var smallDz = new float[50];
        var smallLt = new float[50];
        var smallOid = new int[50];
        var smallCidx = new int[50];
        var smallPid = new int[50];

        int decoded = ProjectileSnapshot.Decode(buf, buf.Length,
            smallPx, smallPz, smallDx, smallDz, smallLt, smallOid, smallCidx, smallPid);
        Assert.Equal(50, decoded);
        for (int i = 0; i < 50; i++) Assert.Equal((float)i, smallPx[i]);
    }

    [Fact]
    public void OwnerColorAndProjectileIdClampedToWord()
    {
        var (px, pz, dx, dz, lt, oid, cidx, pid) = AllocSoA(Capacity);
        oid[0] = 256;
        cidx[0] = 257;
        pid[0] = 65536;
        pid[1] = 65537;

        var buf = new byte[ProjectileSnapshot.CalculateBufferSize(2)];
        ProjectileSnapshot.Encode(buf, px, pz, dx, dz, lt, oid, cidx, pid, 2);

        var (px2, pz2, dx2, dz2, lt2, oid2, cidx2, pid2) = AllocSoA(Capacity);
        ProjectileSnapshot.Decode(buf, buf.Length, px2, pz2, dx2, dz2, lt2, oid2, cidx2, pid2);

        Assert.Equal(0, oid2[0]);
        Assert.Equal(1, cidx2[0]);
        Assert.Equal(0, pid2[0]);
        Assert.Equal(1, pid2[1]);
    }

    [Fact]
    public void RoundTrip_ProjectileId_PreservedAcrossEntities()
    {
        var (px, pz, dx, dz, lt, oid, cidx, pid) = AllocSoA(Capacity);
        pid[0] = 1;
        pid[1] = 65535;
        pid[2] = 12345;

        var buf = new byte[ProjectileSnapshot.CalculateBufferSize(3)];
        ProjectileSnapshot.Encode(buf, px, pz, dx, dz, lt, oid, cidx, pid, 3);

        var (px2, pz2, dx2, dz2, lt2, oid2, cidx2, pid2) = AllocSoA(Capacity);
        ProjectileSnapshot.Decode(buf, buf.Length, px2, pz2, dx2, dz2, lt2, oid2, cidx2, pid2);

        Assert.Equal(1, pid2[0]);
        Assert.Equal(65535, pid2[1]);
        Assert.Equal(12345, pid2[2]);
    }

    [Fact]
    public void Decode_WrongVersion_ReturnsZero()
    {
        var (px, pz, dx, dz, lt, oid, cidx, pid) = AllocSoA(Capacity);
        px[0] = 1.0f;
        var buf = new byte[ProjectileSnapshot.CalculateBufferSize(1)];
        ProjectileSnapshot.Encode(buf, px, pz, dx, dz, lt, oid, cidx, pid, 1);

        buf[0] = (byte)(ProjectileSnapshot.Version + 1);

        var (px2, pz2, dx2, dz2, lt2, oid2, cidx2, pid2) = AllocSoA(Capacity);
        int decoded = ProjectileSnapshot.Decode(buf, buf.Length, px2, pz2, dx2, dz2, lt2, oid2, cidx2, pid2);
        Assert.Equal(0, decoded);
    }

    [Fact]
    public void Encode_AlwaysWritesCurrentVersion()
    {
        var (px, pz, dx, dz, lt, oid, cidx, pid) = AllocSoA(Capacity);
        var buf = new byte[ProjectileSnapshot.CalculateBufferSize(1)];
        ProjectileSnapshot.Encode(buf, px, pz, dx, dz, lt, oid, cidx, pid, 1);
        Assert.Equal(ProjectileSnapshot.Version, buf[0]);
    }

    private static (float[] px, float[] pz, float[] dx, float[] dz, float[] lt, int[] oid, int[] cidx, int[] pid) AllocSoA(int capacity)
    {
        return (
            new float[capacity],
            new float[capacity],
            new float[capacity],
            new float[capacity],
            new float[capacity],
            new int[capacity],
            new int[capacity],
            new int[capacity]);
    }
}
