using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StructureOfArraysGenerator.Tests;

public class MultiListTest
{
    [Fact]
    public void List()
    {
        var list = new Vector3IntMultiArrayList(3);
        list.Length.Should().Be(0);
        list.Add(new Vector3Int { ABC = 1, DEF = 10, GHI = 100 });
        list.Length.Should().Be(1);
        list.Add(new Vector3Int { ABC = 2, DEF = 20, GHI = 200 });
        list.Length.Should().Be(2);
        list.Add(new Vector3Int { ABC = 3, DEF = 30, GHI = 300 });
        list.Length.Should().Be(3);
        list.Add(new Vector3Int { ABC = 4, DEF = 40, GHI = 400 });
        list.Length.Should().Be(4);

        list.AsEnumerable().Select(x => (x.ABC, x.DEF, x.GHI)).ToArray().Should()
            .Equal((1, 10, 100), (2, 20, 200), (3, 30, 300), (4, 40, 400));
    }

    [Fact]
    public void OutOfRange()
    {
        var list = new Vector3IntMultiArrayList(4);

        list.Add(new Vector3Int { ABC = 1, DEF = 10, GHI = 100 });
        list.Add(new Vector3Int { ABC = 2, DEF = 20, GHI = 200 });


        list[1].ABC.Should().Be(2);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = list[2]);
    }

    [Fact]
    public void CopyTo()
    {
        var list = new Vector3IntMultiArrayList(0);
        list.Add(new Vector3Int { ABC = 1, DEF = 22, GHI = 333 });
        list.Add(new Vector3Int { ABC = 4, DEF = 55, GHI = 666 });

        var dest = new Vector3IntMultiArray(2);
        list.CopyTo(dest);

        dest[0].ABC.Should().Be(1);
        dest[1].ABC.Should().Be(4);
    }

    [Fact]
    public void ToArray()
    {
        var list = new Vector3IntMultiArrayList(0);
        list.Add(new Vector3Int { ABC = 1, DEF = 22, GHI = 333 });
        list.Add(new Vector3Int { ABC = 4, DEF = 55, GHI = 666 });

        var array = list.ToArray();
        array.Length.Should().Be(2);
        array[0].ABC.Should().Be(1);
        array[1].ABC.Should().Be(4);
    }
}


public struct Vector3Int
{
    public int ABC;
    public int DEF;
    public int GHI;
}

[MultiArray(typeof(Vector3Int)), MultiArrayList]
public readonly partial struct Vector3IntMultiArray { }
