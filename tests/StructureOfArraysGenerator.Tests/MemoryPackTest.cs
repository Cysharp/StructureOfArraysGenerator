using MemoryPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StructureOfArraysGenerator.Tests;

public class MemoryPackTest
{
    [Fact]
    public void Serialize()
    {
        var four = new ThreeVectorMultiArray(4);
        four[0] = new ThreeVector { X = 10, Y = false, Z = 324.4 };
        four[1] = new ThreeVector { X = 20, Y = true, Z = 20.4 };
        four.X[2] = 30; four.Y[2] = true; four.Z[2] = 44.99;
        four.X[3] = 40; four.Y[3] = false; four.Z[3] = 424.12;

        MemoryPackFormatterProvider.Register(new MultiArrayFormatter<ThreeVectorMultiArray>());

        var bin = MemoryPackSerializer.Serialize(four);

        var four2 = MemoryPackSerializer.Deserialize<ThreeVectorMultiArray>(bin);

        four2.X.ToArray().Should().Equal(10, 20, 30, 40);
        four2.Y.ToArray().Should().Equal(false, true, true, false);
        four2.Z.ToArray().Should().Equal(324.4, 20.4, 44.99, 424.12);
        four2.AsEnumerable().Select(x => x.X).ToArray().Should().Equal(10, 20, 30, 40);
        four2.AsEnumerable().Select(x => x.Y).ToArray().Should().Equal(false, true, true, false);
        four2.AsEnumerable().Select(x => x.Z).ToArray().Should().Equal(324.4, 20.4, 44.99, 424.12);
    }
}

internal sealed class MultiArrayFormatter<T> : MemoryPackFormatter<T>
    where T : struct, IMultiArray<T>
{
    public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref T value)
    {
        writer.WriteUnmanaged(value.Length);
        writer.WriteUnmanagedSpan(value.GetRawSpan());
    }

    public override void Deserialize(ref MemoryPackReader reader, scoped ref T value)
    {
        var length = reader.ReadUnmanaged<int>();
        var array = reader.ReadUnmanagedArray<byte>();
        value = T.Create(length, array!);
    }
}
