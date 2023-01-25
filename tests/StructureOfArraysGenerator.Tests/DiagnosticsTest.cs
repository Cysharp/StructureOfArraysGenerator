using MemoryPack.Tests.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StructureOfArraysGenerator.Tests;

public class DiagnosticsTest
{
    void Compile(int id, string code, bool allowMultipleError = false)
    {
        var diagnostics = CSharpGeneratorRunner.RunGenerator(code);
        if (!allowMultipleError)
        {
            diagnostics.Length.Should().Be(1);
            diagnostics[0].Id.Should().Be("SOA" + id.ToString("000"));
        }
        else
        {
            diagnostics.Select(x => x.Id).Should().Contain("SOA" + id.ToString("000"));
        }
    }

    [Fact]
    public void SOA001_MustBePartial()
    {
        Compile(1, """
using StructureOfArraysGenerator;

public struct Foo
{
    public int X;
}

[MultiArray(typeof(Foo))]
public readonly struct FooMultiArray
{
}
""");
    }

    [Fact]
    public void SOA002_MustBeReadonly()
    {
        Compile(2, """
using StructureOfArraysGenerator;

public struct Foo
{
    public int X;
}

[MultiArray(typeof(Foo))]
public partial struct FooMultiArray
{
}
""");
    }

    [Fact]
    public void SOA003_ElementIsNotValueType()
    {
        Compile(3, """
using StructureOfArraysGenerator;

public class Foo
{
    public int X;
}

[MultiArray(typeof(Foo))]
public readonly partial struct FooMultiArray
{
}
""");
    }

    [Fact]
    public void SOA004_MemberEmpty()
    {
        Compile(4, """
using StructureOfArraysGenerator;

public struct Foo
{
}

[MultiArray(typeof(Foo))]
public readonly partial struct FooMultiArray
{
}
""");
    }

    [Fact]
    public void SOA005_MemberUnmanaged()
    {
        Compile(5, """
using StructureOfArraysGenerator;

public struct Foo
{
    public int X;
    public string Y;
}

[MultiArray(typeof(Foo))]
public readonly partial struct FooMultiArray
{
}
""");
    }

    [Fact]
    public void SOA006_MultiArrayIsNotExists()
    {
        Compile(6, """
using StructureOfArraysGenerator;

public struct Foo
{
    public int X;
}

[MultiArrayList]
public readonly partial struct FooMultiArray
{
}
""");
    }
}
