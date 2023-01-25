using System;
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

