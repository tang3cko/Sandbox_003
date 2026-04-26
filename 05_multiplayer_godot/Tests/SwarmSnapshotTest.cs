using Xunit;

namespace SwarmSurvivor.Tests;

public class SwarmSnapshotTest
{
    private const int Capacity = 600;

    [Fact]
    public void CalculateBufferSize_Zero_ReturnsHeaderOnly()
    {
        Assert.Equal(2, SwarmSnapshot.CalculateBufferSize(0));
    }

    [Fact]
    public void CalculateBufferSize_Linear()
    {
        Assert.Equal(2 + 22, SwarmSnapshot.CalculateBufferSize(1));
        Assert.Equal(2 + 22 * 500, SwarmSnapshot.CalculateBufferSize(500));
    }

    [Fact]
    public void RoundTrip_EmptySnapshot_DecodesToZero()
    {
        var (px, pz, vx, vz, tx, ft, dt) = AllocSoA(Capacity);
        var buf = new byte[SwarmSnapshot.CalculateBufferSize(0)];

        int written = SwarmSnapshot.Encode(buf, px, pz, vx, vz, tx, ft, dt, 0);
        Assert.Equal(2, written);

        var (px2, pz2, vx2, vz2, tx2, ft2, dt2) = AllocSoA(Capacity);
        int decoded = SwarmSnapshot.Decode(buf, written, px2, pz2, vx2, vz2, tx2, ft2, dt2);
        Assert.Equal(0, decoded);
    }

    [Fact]
    public void RoundTrip_SingleEntity_PreservesAllFields()
    {
        var (px, pz, vx, vz, tx, ft, dt) = AllocSoA(Capacity);
        px[0] = 12.5f;
        pz[0] = -7.25f;
        vx[0] = 1.5f;
        vz[0] = -2.25f;
        tx[0] = 3;
        ft[0] = 0.075f;
        dt[0] = SwarmCalculator.AliveMarker;

        var buf = new byte[SwarmSnapshot.CalculateBufferSize(1)];
        SwarmSnapshot.Encode(buf, px, pz, vx, vz, tx, ft, dt, 1);

        var (px2, pz2, vx2, vz2, tx2, ft2, dt2) = AllocSoA(Capacity);
        int decoded = SwarmSnapshot.Decode(buf, buf.Length, px2, pz2, vx2, vz2, tx2, ft2, dt2);

        Assert.Equal(1, decoded);
        Assert.Equal(12.5f, px2[0]);
        Assert.Equal(-7.25f, pz2[0]);
        Assert.Equal(1.5f, vx2[0]);
        Assert.Equal(-2.25f, vz2[0]);
        Assert.Equal(3, tx2[0]);
        Assert.InRange(ft2[0], 0.07f, 0.08f);
        Assert.Equal(SwarmCalculator.AliveMarker, dt2[0]);
    }

    [Fact]
    public void RoundTrip_MultipleEntities_PreservesAllValues()
    {
        const int N = 16;
        var (px, pz, vx, vz, tx, ft, dt) = AllocSoA(Capacity);
        for (int i = 0; i < N; i++)
        {
            px[i] = i * 1.5f;
            pz[i] = -i * 0.25f;
            vx[i] = (i % 3) * 0.5f;
            vz[i] = -(i % 4) * 0.5f;
            tx[i] = i % 4;
            ft[i] = (i % 2) * SwarmCalculator.DefaultFlashDuration;
            dt[i] = i % 5 == 0 ? SwarmCalculator.AliveMarker : i * 0.01f;
        }

        var buf = new byte[SwarmSnapshot.CalculateBufferSize(N)];
        SwarmSnapshot.Encode(buf, px, pz, vx, vz, tx, ft, dt, N);

        var (px2, pz2, vx2, vz2, tx2, ft2, dt2) = AllocSoA(Capacity);
        int decoded = SwarmSnapshot.Decode(buf, buf.Length, px2, pz2, vx2, vz2, tx2, ft2, dt2);

        Assert.Equal(N, decoded);
        for (int i = 0; i < N; i++)
        {
            Assert.Equal(px[i], px2[i]);
            Assert.Equal(pz[i], pz2[i]);
            Assert.Equal(vx[i], vx2[i]);
            Assert.Equal(vz[i], vz2[i]);
            Assert.Equal(tx[i], tx2[i]);
            Assert.True(System.Math.Abs(ft[i] - ft2[i]) <= SwarmCalculator.DefaultFlashDuration / 256f + 1e-5f);
            Assert.Equal(dt[i], dt2[i]);
        }
    }

    [Fact]
    public void Decode_BufferSmallerThanHeader_ReturnsZero()
    {
        var (px, pz, vx, vz, tx, ft, dt) = AllocSoA(Capacity);
        Assert.Equal(0, SwarmSnapshot.Decode(new byte[1], 1, px, pz, vx, vz, tx, ft, dt));
    }

    [Fact]
    public void Decode_BufferSmallerThanExpected_ReturnsZero()
    {
        var (px, pz, vx, vz, tx, ft, dt) = AllocSoA(Capacity);
        var buf = new byte[2];
        buf[0] = 1; buf[1] = 0;
        Assert.Equal(0, SwarmSnapshot.Decode(buf, buf.Length, px, pz, vx, vz, tx, ft, dt));
    }

    [Fact]
    public void Decode_TruncatesToReceiverCapacity()
    {
        var (px, pz, vx, vz, tx, ft, dt) = AllocSoA(Capacity);
        for (int i = 0; i < Capacity; i++) px[i] = i;

        var buf = new byte[SwarmSnapshot.CalculateBufferSize(Capacity)];
        SwarmSnapshot.Encode(buf, px, pz, vx, vz, tx, ft, dt, Capacity);

        var smallPx = new float[10];
        var smallPz = new float[10];
        var smallVx = new float[10];
        var smallVz = new float[10];
        var smallTx = new int[10];
        var smallFt = new float[10];
        var smallDt = new float[10];

        int decoded = SwarmSnapshot.Decode(buf, buf.Length,
            smallPx, smallPz, smallVx, smallVz, smallTx, smallFt, smallDt);
        Assert.Equal(10, decoded);
        for (int i = 0; i < 10; i++) Assert.Equal((float)i, smallPx[i]);
    }

    [Fact]
    public void EncodeFlashTimer_ClampsBelowZero()
    {
        Assert.Equal(0, SwarmSnapshot.EncodeFlashTimer(-0.5f));
    }

    [Fact]
    public void EncodeFlashTimer_ClampsAboveMax()
    {
        Assert.Equal(255, SwarmSnapshot.EncodeFlashTimer(SwarmCalculator.DefaultFlashDuration * 2f));
    }

    [Fact]
    public void EncodeFlashTimer_ZeroEncodesToZero()
    {
        Assert.Equal(0, SwarmSnapshot.EncodeFlashTimer(0f));
    }

    [Fact]
    public void RoundTrip_TypeIndexFitsInByte()
    {
        var (px, pz, vx, vz, tx, ft, dt) = AllocSoA(Capacity);
        tx[0] = 200;
        var buf = new byte[SwarmSnapshot.CalculateBufferSize(1)];
        SwarmSnapshot.Encode(buf, px, pz, vx, vz, tx, ft, dt, 1);

        var (_, _, _, _, tx2, _, _) = AllocSoA(Capacity);
        var px2 = new float[Capacity];
        var pz2 = new float[Capacity];
        var vx2 = new float[Capacity];
        var vz2 = new float[Capacity];
        var ft2 = new float[Capacity];
        var dt2 = new float[Capacity];
        SwarmSnapshot.Decode(buf, buf.Length, px2, pz2, vx2, vz2, tx2, ft2, dt2);

        Assert.Equal(200, tx2[0]);
    }

    private static (float[] px, float[] pz, float[] vx, float[] vz, int[] tx, float[] ft, float[] dt) AllocSoA(int capacity)
    {
        return (
            new float[capacity],
            new float[capacity],
            new float[capacity],
            new float[capacity],
            new int[capacity],
            new float[capacity],
            new float[capacity]
        );
    }
}
