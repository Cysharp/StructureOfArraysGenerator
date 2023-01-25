// See https://aka.ms/new-console-template for more information
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.DataContracts;
using System.Security.Cryptography.X509Certificates;
using MemoryPack;
using StructureOfArraysGenerator;



MemoryPackFormatterProvider.Register(new MultiArrayFormatter<Point3DMultiArray>());



var list = new Point3DMultiArrayList();

list.Add(new Point3D { X = 10, Y = 20, Z = 30 });
list.Add(new Point3D { X = 11, Y = 21, Z = 31 });
list.Add(new Point3D { X = 12, Y = 22, Z = 32 });
list.Add(new Point3D { X = 13, Y = 23, Z = 33 });
list.Add(new Point3D { X = 15, Y = 25, Z = 35 });


Console.WriteLine(list.Length);







public struct Point3D
{
    public float X;
    public float Y;
    public float Z;
}

[MultiArray(typeof(Point3D), nameof(Point3D.X), nameof(Point3D.Y)), MultiArrayList]
public readonly partial struct Point3DMultiArray
{
    // TODO: check index of indexer
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