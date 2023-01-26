using StructureOfArraysGenerator;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");



public struct Point3D
{
    public float X;
    public float Y;
    public float Z;

    public Point3D(float x, float y)
    {
        this.X = x;
        this.Y = y;
    }
}

[MultiArray(typeof(Point3D)), MultiArrayList]
public readonly partial struct Point3DMultiArray
{

}




public class MyClass
{

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    static ref T GetArrayDataReference<T>(T[] array)
    {
#if NET5_0_OR_GREATER
        return ref MemoryMarshal.GetArrayDataReference(array);
#else
        return ref MemoryMarshal.GetReference(array.AsSpan());
#endif
    }
}