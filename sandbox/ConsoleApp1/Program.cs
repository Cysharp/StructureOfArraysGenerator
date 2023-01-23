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

internal interface IMultiArray<T>
{
    int Length { get; }
    ReadOnlySpan<byte> GetRawSpan();
    static abstract int GetByteSize(int length);
    static abstract T Create(int length, ArraySegment<byte> arrayOffset);
}

[MultiArray(typeof(Point3D))] // SourceGenerator, it will implements below code
public readonly partial struct Point3DMultiArray : IMultiArray<Point3DMultiArray>
{
    readonly byte[] __value;
    readonly int __length;
    readonly int __byteSize;
    readonly int __byteOffsetX;
    readonly int __byteOffsetY;
    readonly int __byteOffsetZ;

    public int Length => __length;
    public Span<float> X => MemoryMarshal.CreateSpan(ref Unsafe.As<byte, float>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(__value), __byteOffsetX)), __length);
    public Span<float> Y => MemoryMarshal.CreateSpan(ref Unsafe.As<byte, float>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(__value), __byteOffsetY)), __length);
    public Span<float> Z => MemoryMarshal.CreateSpan(ref Unsafe.As<byte, float>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(__value), __byteOffsetZ)), __length);

    public static int GetByteSize(int length)
    {
        return Unsafe.SizeOf<float>() * length
             + Unsafe.SizeOf<float>() * length
             + Unsafe.SizeOf<float>() * length;
    }

    public static Point3DMultiArray Create(int length, ArraySegment<byte> arrayOffset)
    {
        return new Point3DMultiArray(length, arrayOffset);
    }

    public Point3DMultiArray(int length)
    {
        this.__byteOffsetX = 0;
        this.__byteOffsetY = __byteOffsetX + Unsafe.SizeOf<float>() * length;
        this.__byteOffsetZ = __byteOffsetY + Unsafe.SizeOf<float>() * length;

        this.__byteSize = __byteOffsetZ + Unsafe.SizeOf<float>() * length;
        this.__value = new byte[__byteSize];
        this.__length = length;
    }

    public Point3DMultiArray(int length, ArraySegment<byte> arrayOffset)
    {
        this.__byteOffsetX = arrayOffset.Offset;
        this.__byteOffsetY = __byteOffsetX + Unsafe.SizeOf<float>() * length;
        this.__byteOffsetZ = __byteOffsetY + Unsafe.SizeOf<float>() * length;

        this.__byteSize = __byteOffsetZ + Unsafe.SizeOf<float>() * length;
        this.__value = arrayOffset.Array!;
        this.__length = length;
    }

    public Point3D this[int index]
    {
        get
        {
            ref var x = ref Unsafe.Add(ref Unsafe.As<byte, float>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(__value), __byteOffsetX)), index);
            ref var y = ref Unsafe.Add(ref Unsafe.As<byte, float>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(__value), __byteOffsetY)), index);
            ref var z = ref Unsafe.Add(ref Unsafe.As<byte, float>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(__value), __byteOffsetZ)), index);
            return new Point3D
            {
                X = x,
                Y = y,
                Z = z
            };
        }
        set
        {
            Unsafe.Add(ref Unsafe.As<byte, float>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(__value), __byteOffsetX)), index) = value.X;
            Unsafe.Add(ref Unsafe.As<byte, float>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(__value), __byteOffsetY)), index) = value.Y;
            Unsafe.Add(ref Unsafe.As<byte, float>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(__value), __byteOffsetZ)), index) = value.Z;
        }
    }

    public ReadOnlySpan<byte> GetRawSpan() => __value.AsSpan(__byteOffsetX, __byteSize);

    public bool SequenceEqual(Point3DMultiArray other)
    {
        return GetRawSpan().SequenceEqual(other.GetRawSpan());
    }
}


public class Point3DMultiList
{
    byte[] __value = default!; // TODO:
    int __length;
    int __byteSize;
    int __byteOffsetX;
    int __byteOffsetY;
    int __byteOffsetZ;

    public int Length => __length;
    public Span<float> X => MemoryMarshal.CreateSpan(ref Unsafe.As<byte, float>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(__value), __byteOffsetX)), __length);
    public Span<float> Y => MemoryMarshal.CreateSpan(ref Unsafe.As<byte, float>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(__value), __byteOffsetY)), __length);
    public Span<float> Z => MemoryMarshal.CreateSpan(ref Unsafe.As<byte, float>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(__value), __byteOffsetZ)), __length);

    public void Add(Point3D value)
    {
        //var newValue = new byte[
    }

    void EnsureCapacity(int newLength)
    {
        var newArray = new byte[__value.Length * 2];

        // TODO: make nextOffset;


        // copy

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



partial struct Point3DMultiArray : IMemoryPackable<Point3DMultiArray>
{
    public static void RegisterFormatter()
    {
        if (!global::MemoryPack.MemoryPackFormatterProvider.IsRegistered<Point3DMultiArray>())
        {
            global::MemoryPack.MemoryPackFormatterProvider.Register(new global::MemoryPack.Formatters.MemoryPackableFormatter<Point3DMultiArray>());
        }
    }

    public static void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref Point3DMultiArray value)
        where TBufferWriter : IBufferWriter<byte>
    {
        writer.WriteUnmanaged(value.Length);
        writer.WriteUnmanagedSpan(value.GetRawSpan());
    }

    public static void Deserialize(ref MemoryPackReader reader, scoped ref Point3DMultiArray value)
    {
        var length = reader.ReadUnmanaged<int>();
        var array = reader.ReadUnmanagedArray<byte>();
        value = new Point3DMultiArray(length, array!);
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

    public MultiArrayAttribute2(Type type, string[] members)
    {
        this.Type = type;
    }
}

// TODO: constructor selection?
// MemoryPack serialization



