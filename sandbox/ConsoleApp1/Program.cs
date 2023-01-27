// See https://aka.ms/neVoyagew-console-template for more information
using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Serialization.DataContracts;
using System.Security.Cryptography.X509Certificates;
using MemoryPack;
using StructureOfArraysGenerator;



//Avx2.LoadVector256(



    

//MemoryPackFormatterProvider.Register(new MultiArrayFormatter<Point3DMultiArray>());

// Vector3(float X, float Y, float Z)


// calculate bytesize of MultiArray
var byteSize = Vector3MultiArray.GetByteSize(length: 10);

var rentArray = ArrayPool<byte>.Shared.Rent(byteSize);
try
{
    // must need to zero clear before use in MultiArray
    Array.Clear(rentArray, 0, byteSize);

    // create ArrayPool byte[] backed MultiArray
    var array = new Vector3MultiArray(10, rentArray);

    // do something...
}
finally
{
    // return after used.
    ArrayPool<byte>.Shared.Return(rentArray);
}





//array.X[0] = 10;
//array[1] = new Vector3(1.1f, 2.2f, 3.3f);

//foreach (var item in array)
//{
//    Console.WriteLine($"{item.X}, {item.Y}, {item.Z}");
//}


//var list = new Point3DMultiArrayList();

//list.Add(new Point3D { X = 10, Y = 20, Z = 30 });
//list.Add(new Point3D { X = 11, Y = 21, Z = 31 });
//list.Add(new Point3D { X = 12, Y = 22, Z = 32 });
//list.Add(new Point3D { X = 13, Y = 23, Z = 33 });
//list.Add(new Point3D { X = 15, Y = 25, Z = 35 });



new Point3DMultiArray(8);




//Console.WriteLine(list.Length);

public readonly struct Point2D
{
    public readonly int X;
    public readonly int Y;

    public Point2D(int x, int y)
    {
        X = x;
        Y = y;
    }
}

[MultiArray(typeof(Point2D))]
public readonly partial struct Point2DMultiArray { }


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




[MultiArray(typeof(Vector3), includeProperty: true)]
public readonly partial struct Vector3MultiArray
{
}





[MultiArray(typeof(System.Numerics.Vector4))]
public readonly partial struct Vector4MultiArray
{

}



