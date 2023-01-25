using MemoryPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StructureOfArraysGenerator.Tests;

internal class MemoryPackTest
{
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