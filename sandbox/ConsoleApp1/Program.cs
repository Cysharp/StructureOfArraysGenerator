// See https://aka.ms/new-console-template for more information
using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.DataContracts;
using System.Security.Cryptography.X509Certificates;
using MemoryPack;
using StructureOfArraysGenerator;



var array = new Point3DMultiArray(10);

array[3] = new Point3D { X = 99, Y = 100, Z = 9999 };



var array2 = new Point3D[10];
//MemoryExtensions.
array.X[4] = 9999;


Console.WriteLine("foo");


public struct Point3D
{
    public float X;
    public float Y;
    public float Z;
}

[MultiArray(typeof(Point3D))]
[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Auto)]
public readonly partial struct Point3DMultiArray
{
    // TODO: check index of indexer
}


public class Point3DMultiArrayList
{
    Point3DMultiArray array;
    int length;

    public int Length => length;
    public Span<float> X => array.X.Slice(0, length);
    public Span<float> Y => array.Y.Slice(0, length);
    public Span<float> Z => array.Z.Slice(0, length);

    public Point3DMultiArrayList()
        : this(4)
    {
    }

    public Point3DMultiArrayList(int capacity)
    {
        if (capacity < 0) capacity = 1;
        array = new Point3DMultiArray(capacity);
    }

    public Point3D this[int index]
    {
        get
        {
            // TODO: check index of indexer
            return array[index];
        }
        set
        {
            array[index] = value;
        }
    }

    public void Add(Point3D value)
    {
        // TODO: EnsureCapacity
        array[length++] = value;
    }

    void EnsureCapacity(int newLength)
    {
        var newArray = new Point3DMultiArray(array.Length * 2);
        array.X.CopyTo(newArray.X);
        array.Y.CopyTo(newArray.Y);
        array.Z.CopyTo(newArray.Z);
    }

    //public ReadOnlySpan<byte> GetRawSpan() => __value.AsSpan(__byteOffsetX, __byteSize);

    //public bool SequenceEqual(Point3DMultiArray other)
    //{
    //    return GetRawSpan().SequenceEqual(other.GetRawSpan());
    //}
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







[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
internal sealed class MultiArrayAttribute2 : Attribute
{
    public Type Type { get; }

    public MultiArrayAttribute2(Type type, bool includeProperty = false)
    {
        this.Type = type;
    }
}

// TODO: constructor selection?
// MemoryPack serialization

