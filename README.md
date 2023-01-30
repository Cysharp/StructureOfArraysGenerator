# StructureOfArraysGenerator
[![GitHub Actions](https://github.com/Cysharp/StructureOfArraysGenerator/workflows/Build-Debug/badge.svg)](https://github.com/Cysharp/StructureOfArraysGenerator/actions) [![Releases](https://img.shields.io/github/release/Cysharp/StructureOfArraysGenerator.svg)](https://github.com/Cysharp/StructureOfArraysGenerator/releases)

Structure of arrays source generator to make CPU Cache and SIMD friendly data structure for high-performance code in .NET and Unity.

![image](https://user-images.githubusercontent.com/46207/214814782-fd341e09-731a-4e2f-ba53-ef789a19160e.png)

As described in Wikipedia [AoS and SoA](https://en.wikipedia.org/wiki/AoS_and_SoA), standard C# array is **array of structures(AoS)**, however the **structure of arrays(SoA)** is suitable for utilizing the CPU cache, which is faster than the main memory, and for ultra-fast parallel processing by SIMD. StructureOfArraysGenerator is inspired by [Zig language](https://ziglang.org/)'s `MultiArrayList`. See the great session [A Practical Guide to Applying Data-Oriented Design](https://vimeo.com/649009599) that talked by Andrew Kelley who is the Zig language author.

Here is the simple Max .Y of Vector3(float X, float Y, float Z) array calculate result.

![image](https://user-images.githubusercontent.com/46207/215027253-6f94739f-b827-46ba-a395-690d1df89d46.png)

MultiArray is x2 faster and SIMD version(SoA is easy to write SIMD) is x10 faster.

StructureOfArraysGenerator actually generates not arrays, just a struct with a single `byte[]` field and `int` offsets of each fields to provide `Span<T>` view, it minimizes memory and heap usage in C#. Source Generator generates `Span<T>` property corresponding to each struct members so SoA structure can be realized with the same ease of use as a regular `T[]`.

If you want to do aggregate operations to `Span<T>`, you can use [Cysharp/SimdLinq](https://github.com/Cysharp/SimdLinq/) that easy to calculate faster SIMD way.

Installation
---
This library is distributed via NuGet.

> PM> Install-Package [StructureOfArraysGenerator](https://www.nuget.org/packages/StructureOfArraysGenerator)

And also a code editor requires Roslyn 4.3.1 support, for example Visual Studio 2022 version 17.3, .NET SDK 6.0.401. For details, see the [Roslyn Version Support](https://learn.microsoft.com/en-us/visualstudio/extensibility/roslyn-version-support) document. For Unity, the requirements and installation process are completely different. See the [Unity](#unity) section for details.

Package provides only analyzer and generated code does not dependent any other libraries.

Quick Start
---
Make the `readonly partial struct` type with `[MultiArray]` attribute.

```csharp
using StructureOfArraysGenerator;

[MultiArray(typeof(Vector3))]
public readonly partial struct Vector3MultiArray
{
}
```

C# Source Generator recognize `MultiArray` attribute and will generate code like below.

```csharp
partial struct Vector3MultiArray
{
    // constructor
    public Vector3MultiArray(int length)

    // Span<T> properties for Vector3 each fields
    public Span<float> X => ...;
    public Span<float> Y => ...;
    public Span<float> Z => ...;

    // indexer
    public Vector3 this[int index] { get{} set{} }

    // foreach
    public Enumerator GetEnumerator()
}
```

You can use this array-like type like this.

```csharp
var array = new Vector3MultiArray(4);

array.X[0] = 10;
array[1] = new Vector3(1.1f, 2.2f, 3.3f);

// multiply Y
foreach (ref var item in v.Y)
{
    item *= 2;
}

// iterate Vector3
foreach (var item in array)
{
    Console.WriteLine($"{item.X}, {item.Y}, {item.Z}");
}
```

StructureOfArraysGenerator has two attributes [MultiArray](#multiarray) and [MultiArrayList](#multiarraylist), `MultiArray` is like a `T[]`, `MultiArrayList` is like a `List<T>`.

MultiArray
---
StructureOfArraysGenerator emits this internal attribute to referenced assembly.

```csharp
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
internal sealed class MultiArrayAttribute : Attribute
{
    public MultiArrayAttribute(Type type, bool includeProperty = false)
    public MultiArrayAttribute(Type type, params string[] members)
}
```

Default target members are public or internal field, if you want to include property, use `[MultiArray(typeof(), includeProperty: true]`. `params string[] members` overload can choose which member to create array member.

All target members are must be unmanaged type(struct which contains no reference types).

`MultiArray` generates these methods.

```csharp
// static methods
static int GetByteSize(int length)
static Vector3MultiArray Create(int length, ArraySegment<byte> arrayOffset)

// constructor
ctor(int length)
ctor(int length, ArraySegment<byte> arrayOffset)

// properties(length and target members Span<T> MemberName)
int Length => ...;
Span<T> ... => ...;

// indexer
T this[int index] { get{} set{} }

// methods
ReadOnlySpan<byte> GetRawSpan()
bool SequenceEqual(TMultiArray other)
IEnumerable<T> AsEnumerable()

// foreach
Enumerator GetEnumerator()
```

`MultiArray` only allows `readonly` struct so recommend to use `in` to method for avoid copy.

```csharp
void DoSomething(in Vector3MultiArray array)
{
}
```

`MultiArray` is single `byte[]` backed structure, if you use `ctor(int length, ArraySegment<byte> arrayOffset)` `with ArrayPool`, enable to avoid `byte[]` allocation.

```csharp
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
```

`MultiArray` can use `foreach` directly that can be iterated with zero-allocation via a special struct. However does not implements `IEnumerable<T>` so can't use LINQ and can't pass to other , if you want to do so use `AsEnumerable()`.

`MultiArray` target struct supports immutable(readonly) struct.

```csharp
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
```

Target struct's constructor is choosed most matched by **name**(case insensitive).

`MultiArray` is single `byte[]` backed data structure, `GetRawSpan()` can get inner data directly. `SequenceEqual` compares inner bytes so can check equality very fast.

In addition, [Cysharp/MemoryPack](https://github.com/Cysharp/MemoryPack) enables fast serialization. Since MemoryPack is not supported by default, use the following helper class.

```csharp
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
```

And register formatter in startup to each generated `MultiArray`.

```csharp
MemoryPackFormatterProvider.Register(new MultiArrayFormatter<Vector3MultiArray>());
```

MultiArrayList
---
`MultiArrayList` is expandable `MultiArray` like `List<T>`. Annotate the `MultiArrayListAttribute` at the same place with the `MultiArrayAttribute`.

```csharp
[MultiArray(typeof(Vector3), MultiArrayList)]
public readonly partial struct Vector3MultiArray
{
}
```

It generates `***MultiArrayList` class besides `MultiArray`. You can use `Add` method and does not need set capacity before use.

```csharp
var list = new Vector3MultiArrayList();
list.Add(new Vector3());

var zeroX = list.X[0];
```

In default, generated class name is `***MultiArrayList`. You can configure name via `(string typeName)` constructor parameter.

```csharp
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
internal sealed class MultiArrayListAttribute : Attribute
{
    public MultiArrayListAttribute()
    public MultiArrayListAttribute(string typeName)
}
```

`MultiArrayList` has generate these methods.

```csharp
// constructor
ctor()
ctor(int capacity)

// properties(length and target members Span<T> MemberName)
int Length => ...;
Span<T> ... => ...;

// indexer
T this[int index] { get{} set{} }

// methods
void Add(T item)
void CopyTo(TMultiArray array)
TMultiArray ToArray()
IEnumerable<T> AsEnumerable()

// foreach
Enumerator GetEnumerator()
```

You can convert to `MultiArray` by `ToArray()`.

Unity
---
Install via UPM git URL package or asset package (StructureOfArraysGenerator.*.*.*.unitypackage) available in [StructureOfArraysGenerator/releases](https://github.com/Cysharp/StructureOfArraysGenerator/releases) page.

* https://github.com/Cysharp/StructureOfArraysGenerator.git?path=src/StructureOfArraysGenerator.Unity/Assets/Plugins/StructureOfArraysGenerator

If you want to set a target version, StructureOfArraysGenerator uses the `*.*.*` release tag, so you can specify a version like #1.0.0. For example `https://github.com/Cysharp/StructureOfArraysGenerator.git?path=src/StructureOfArraysGenerator.Unity/Assets/Plugins/StructureOfArraysGenerator#1.0.0`.

Minimum supported Unity version is `2021.3`. The dependency managed DLL `System.Runtime.CompilerServices.Unsafe/6.0.0` is included with unitypackage. For git references, you will need to add them in another way as they are not included to avoid unnecessary dependencies; either extract the dll from unitypackage or download it from the [NuGet page](https://www.nuget.org/packages/System.Runtime.CompilerServices.Unsafe/6.0.0).

As with the .NET version, the code is generated by a code generator (`StructureOfArraysGenerator.Generator.Roslyn3.dll`). For more information on Unity and Source Generator, please refer to the [Unity documentation](https://docs.unity3d.com/Manual/roslyn-analyzers.html).

License
---
This library is licensed under the MIT License.
