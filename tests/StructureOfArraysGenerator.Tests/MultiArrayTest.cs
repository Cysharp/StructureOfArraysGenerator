using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StructureOfArraysGenerator.Tests;

public class MultiArrayTest
{
    [Fact]
    public void SimpleOne()
    {
        var empty = new OneVectorMultiArray();
        empty.Length.Should().Be(0);

        var one = new OneVectorMultiArray(1);
        one[0] = new OneVector { X = 99 };
        one.X.ToArray().Should().Equal(99);

        var four = new OneVectorMultiArray(4);
        four[0] = new OneVector { X = 10 };
        four[1] = new OneVector { X = 20 };
        four.X[2] = 30;
        four.X[3] = 40;
        four.X.ToArray().Should().Equal(10, 20, 30, 40);
        four.AsEnumerable().Select(x => x.X).ToArray().Should().Equal(10, 20, 30, 40);
    }

    [Fact]
    public void SimpleTwo()
    {
        var empty = new TwoVectorMultiArray();
        empty.Length.Should().Be(0);

        var one = new TwoVectorMultiArray(1);
        one[0] = new TwoVector { X = 99, Y = true };
        one.X.ToArray().Should().Equal(99);
        one.Y.ToArray().Should().Equal(true);

        var four = new TwoVectorMultiArray(4);
        four[0] = new TwoVector { X = 10, Y = false };
        four[1] = new TwoVector { X = 20, Y = true };
        four.X[2] = 30; four.Y[2] = true;
        four.X[3] = 40; four.Y[3] = false;
        four.X.ToArray().Should().Equal(10, 20, 30, 40);
        four.Y.ToArray().Should().Equal(false, true, true, false);
        four.AsEnumerable().Select(x => x.X).ToArray().Should().Equal(10, 20, 30, 40);
        four.AsEnumerable().Select(x => x.Y).ToArray().Should().Equal(false, true, true, false);
    }

    [Fact]
    public void SimpleThree()
    {
        var empty = new ThreeVectorMultiArray();
        empty.Length.Should().Be(0);

        var one = new ThreeVectorMultiArray(1);
        one[0] = new ThreeVector { X = 99, Y = true, Z = 19.3 };
        one.X.ToArray().Should().Equal(99);
        one.Y.ToArray().Should().Equal(true);
        one.Z.ToArray().Should().Equal(19.3);

        var four = new ThreeVectorMultiArray(4);
        four[0] = new ThreeVector { X = 10, Y = false, Z = 324.4 };
        four[1] = new ThreeVector { X = 20, Y = true, Z = 20.4 };
        four.X[2] = 30; four.Y[2] = true; four.Z[2] = 44.99;
        four.X[3] = 40; four.Y[3] = false; four.Z[3] = 424.12;
        four.X.ToArray().Should().Equal(10, 20, 30, 40);
        four.Y.ToArray().Should().Equal(false, true, true, false);
        four.Z.ToArray().Should().Equal(324.4, 20.4, 44.99, 424.12);
        four.AsEnumerable().Select(x => x.X).ToArray().Should().Equal(10, 20, 30, 40);
        four.AsEnumerable().Select(x => x.Y).ToArray().Should().Equal(false, true, true, false);
        four.AsEnumerable().Select(x => x.Z).ToArray().Should().Equal(324.4, 20.4, 44.99, 424.12);
    }

    [Fact]
    public void OutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TwoVectorMultiArray(-1));

        var array = new TwoVectorMultiArray(4);
        _ = array[0];
        _ = array[1];
        _ = array[2];
        _ = array[3];
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = array[4];
        });
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = array[-1];
        });
    }

    [Fact]
    public void DifferentOffset()
    {
        var byteSize = ThreeVectorMultiArray.GetByteSize(4);
        var array = ArrayPool<byte>.Shared.Rent(byteSize + 10);
        Array.Clear(array);
        try
        {
            var four = new ThreeVectorMultiArray(4, new ArraySegment<byte>(array, 10, byteSize));
            four[0] = new ThreeVector { X = 10, Y = false, Z = 324.4 };
            four[1] = new ThreeVector { X = 20, Y = true, Z = 20.4 };
            four.X[2] = 30; four.Y[2] = true; four.Z[2] = 44.99;
            four.X[3] = 40; four.Y[3] = false; four.Z[3] = 424.12;
            four.X.ToArray().Should().Equal(10, 20, 30, 40);
            four.Y.ToArray().Should().Equal(false, true, true, false);
            four.Z.ToArray().Should().Equal(324.4, 20.4, 44.99, 424.12);
            four.AsEnumerable().Select(x => x.X).ToArray().Should().Equal(10, 20, 30, 40);
            four.AsEnumerable().Select(x => x.Y).ToArray().Should().Equal(false, true, true, false);
            four.AsEnumerable().Select(x => x.Z).ToArray().Should().Equal(324.4, 20.4, 44.99, 424.12);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(array);
        }
    }

    [Fact]
    public void SmallOffset()
    {
        var byteSize = ThreeVectorMultiArray.GetByteSize(4);

        // just: ok
        _ = new ThreeVectorMultiArray(4, new byte[byteSize]);

        // small: ng
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = new ThreeVectorMultiArray(4, new byte[byteSize - 1]);
        });
    }

    [Fact]
    public void Ctor()
    {
        var array = new CtorCheckerMultiArray(3);
        array[0] = new CtorChecker(10, 20.4) { Y = true };
        array[1] = new CtorChecker(20, 30.4) { Y = false };
        array[2] = new CtorChecker(30, 20.4) { Y = true };

        (array[0].X, array[0].Y, array[0].Z).Should().Be((10, true, 20.4));
        (array[1].X, array[1].Y, array[1].Z).Should().Be((20, false, 30.4));
        (array[2].X, array[2].Y, array[2].Z).Should().Be((30, true, 20.4));
    }
}

public struct OneVector
{
    public int X;
}

public struct TwoVector
{
    public int X;
    public bool Y;
}


public struct ThreeVector
{
    public int X;
    public bool Y;
    public double Z;
}

[MultiArray(typeof(OneVector))]
public readonly partial struct OneVectorMultiArray
{
}

[MultiArray(typeof(TwoVector))]
public readonly partial struct TwoVectorMultiArray
{
}


[MultiArray(typeof(ThreeVector))]
public readonly partial struct ThreeVectorMultiArray
{
}


public struct CtorChecker
{
    public readonly int X;
    public bool Y;
    public readonly double Z;

    public CtorChecker(int x, double z)
    {
        this.X = x;
        this.Z = z;
    }
}


[MultiArray(typeof(CtorChecker))]
public readonly partial struct CtorCheckerMultiArray
{
}
