using StructureOfArraysGenerator;

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


