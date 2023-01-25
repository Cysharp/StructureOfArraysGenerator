// See https://aka.ms/neVoyagew-console-template for more information
using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.DataContracts;
using System.Security.Cryptography.X509Certificates;
using MemoryPack;
using StructureOfArraysGenerator;



//MemoryPackFormatterProvider.Register(new MultiArrayFormatter<Point3DMultiArray>());



//var list = new Point3DMultiArrayList();

//list.Add(new Point3D { X = 10, Y = 20, Z = 30 });
//list.Add(new Point3D { X = 11, Y = 21, Z = 31 });
//list.Add(new Point3D { X = 12, Y = 22, Z = 32 });
//list.Add(new Point3D { X = 13, Y = 23, Z = 33 });
//list.Add(new Point3D { X = 15, Y = 25, Z = 35 });

new Point3DMultiArray(8);


//Console.WriteLine(list.Length);



var array = new Point3DMultiArray(3);
array[0] = new Point3D { X = 10, Y = 20, Z = 30 };
array[1] = new Point3D { X = 20, Y = 40, Z = 30 };
array[2] = new Point3D { X = 30, Y = 60, Z = 30 };


foreach (var item in array)
{
    Console.WriteLine((item.X, item.Y, item.Z));
}





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




