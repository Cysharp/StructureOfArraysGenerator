using Microsoft.CodeAnalysis;
using System.Text;
using static StructureOfArraysGenerator.EmitHelper;

namespace StructureOfArraysGenerator;

[Generator(LanguageNames.CSharp)]
public partial class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(EmitAttributes);

        {
            var source = context.SyntaxProvider.ForAttributeWithMetadataName(
                     "StructureOfArraysGenerator.MultiArrayAttribute",
                     static (node, token) => true,
                     static (context, token) => context);

            context.RegisterSourceOutput(source, EmitMultiArray);
        }
        {
            var source = context.SyntaxProvider.ForAttributeWithMetadataName(
                     "StructureOfArraysGenerator.MultiArrayListAttribute",
                     static (node, token) => true,
                     static (context, token) => context);

            context.RegisterSourceOutput(source, EmitMultiArrayList);
        }
    }

    static void EmitAttributes(IncrementalGeneratorPostInitializationContext context)
    {
        context.AddSource("StructureOfArraysGeneratorAttributes.cs", """
using System;

namespace StructureOfArraysGenerator
{
    internal interface IMultiArray<T>
    {
        int Length { get; }
        ReadOnlySpan<byte> GetRawSpan();
#if NET7_0_OR_GREATER
        static abstract int GetByteSize(int length);
        static abstract T Create(int length, ArraySegment<byte> arrayOffset);
#endif
    }

    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    internal sealed class MultiArrayAttribute : Attribute
    {
        public Type Type { get; }
        public string[] Members { get; }
        public bool IncludeProperty { get; }

        public MultiArrayAttribute(Type type, bool includeProperty = false)
        {
            this.Type = type;
            this.IncludeProperty = includeProperty;
            this.Members = Array.Empty<string>();
        }

        public MultiArrayAttribute(Type type, params string[] members)
        {
            this.Type = type;
            this.IncludeProperty = false;
            this.Members = members;
        }
    }

    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    internal sealed class MultiArrayListAttribute : Attribute
    {
        public string TypeName { get; }

        public MultiArrayListAttribute()
        {
            this.TypeName = "";
        }

        public MultiArrayListAttribute(string typeName)
        {
            this.TypeName = typeName;
        }
    }
}
""");
    }

    static void EmitMultiArray(SourceProductionContext context, GeneratorAttributeSyntaxContext source)
    {
        var attr = source.Attributes[0]; // allowMultiple:false
        var members = GetMultiArrayMembers(attr, out var elementType);

        // TODO: choose constructor

        // Verify target
        if (!VerifyMultiArray(source.TargetSymbol, elementType, members))
        {
            return;
        }

        // Generate Code
        var code = BuildMultiArray(source.TargetSymbol, elementType, members);

        AddSource(context, source.TargetSymbol, code);
    }

    static void EmitMultiArrayList(SourceProductionContext context, GeneratorAttributeSyntaxContext source)
    {
        var multiArrayAttr = source.TargetSymbol.GetAttributes()
            .FirstOrDefault(x => x.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::StructureOfArraysGenerator.MultiArrayAttribute");

        if (multiArrayAttr == null)
        {
            // TODO: Verify
            return;
        }

        string targetTypeName;
        if (source.Attributes[0].ConstructorArguments.Length == 0)
        {
            if (source.TargetSymbol.Name.EndsWith("MultiArray"))
            {
                targetTypeName = source.TargetSymbol.Name + "List";
            }
            else
            {
                targetTypeName = source.TargetSymbol.Name + "MultiArrayList";
            }
        }
        else
        {
            targetTypeName = (string)source.Attributes[0].ConstructorArguments[0].Value!;
        }

        var members = GetMultiArrayMembers(multiArrayAttr, out var elementType);

        var code = BuildMultiArrayList(targetTypeName, source.TargetSymbol, elementType, members);

        AddSource(context, source.TargetSymbol, code, ".MultiArrayList.g.cs");
    }

    static MetaMember[] GetMultiArrayMembers(AttributeData attr, out INamedTypeSymbol elementType)
    {
        // Extract attribtue parameter
        // public MultiArrayAttribute(Type type, bool includeProperty = false)
        // public MultiArrayAttribute(Type type, params string[] members)

        elementType = (INamedTypeSymbol)attr.ConstructorArguments[0].Value!;
        var includeProperty = false;
        string[]? targetMembers = null;
        if (attr.AttributeConstructor!.Parameters[1].Type.SpecialType == SpecialType.System_Boolean)
        {
            // bool includeProperty
            includeProperty = (bool)attr.ConstructorArguments[1].Value!;
            targetMembers = null;
        }
        else
        {
            // string[] members
            includeProperty = false;
            targetMembers = attr.ConstructorArguments[1].Values.Select(x => (string)x.Value!).ToArray();
        }

        // choose target members
        var members = elementType.GetMembers()
            .Where(x =>
            {
                if (!(x.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal))
                {
                    return false;
                }

                if (x is IFieldSymbol) return true;

                if (includeProperty && x is IPropertySymbol)
                {
                    return true;
                }

                return false;
            })
            .Where(x =>
            {
                if (targetMembers == null || targetMembers.Length == 0) return true;

                return targetMembers.Any(y => x.Name == y);
            })
            .Select(x => new MetaMember(x))
            .ToArray();

        return members;
    }

    static bool VerifyMultiArray(ISymbol targetType, INamedTypeSymbol elementType, MetaMember[] members)
    {
        // All target members should be unamanged
        if (!members.All(x => x.MemberType.IsUnmanagedType))
        {
            // TODO: dianogstics error.
            return false;
        }

        // TODO: empty struct
        // TODO: partial
        // TODO: readonly

        return true;
    }

    static string BuildMultiArray(ISymbol targetType, INamedTypeSymbol elementType, MetaMember[] members)
    {
        var targetTypeName = targetType.Name;
        var elementTypeFullName = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var code = $$"""
[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Auto)]
partial struct {{targetTypeName}} : global::StructureOfArraysGenerator.IMultiArray<{{targetTypeName}}>
{
    readonly byte[] __value;
    readonly int __length;
    readonly int __byteSize;
{{ForEachLine("    ", members, x => $"readonly int __byteOffset{x.Name};")}}

    public int Length => __length;
{{ForEachLine("    ", members, x => $"public Span<{x.MemberTypeFullName}> {x.Name} => MemoryMarshal.CreateSpan(ref Unsafe.As<byte, {x.MemberTypeFullName}>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(__value), __byteOffset{x.Name})), __length);")}}

    public static int GetByteSize(int length)
    {
        return 0
{{ForEachLine("             ", members, x => $"+ Unsafe.SizeOf<{x.MemberTypeFullName}>() * length")}}
             ;
    }

    public static {{targetTypeName}} Create(int length, ArraySegment<byte> arrayOffset)
    {
        return new {{targetTypeName}}(length, arrayOffset);
    }

    public {{targetTypeName}}(int length)
    {
        if (length < 0) ThrowOutOfRangeException();

        this.__byteOffset{{members[0].Name}} = 0;
{{ForLine("        ", 1, members.Length, x => $"this.__byteOffset{members[x].Name} = __byteOffset{members[x - 1].Name} + (Unsafe.SizeOf<{members[x - 1].MemberTypeFullName}>() * length);")}}
        this.__byteSize = __byteOffset{{members[members.Length - 1].Name}} + (Unsafe.SizeOf<{{members[members.Length - 1].MemberTypeFullName}}>() * length);
        this.__value = new byte[__byteSize];
        this.__length = length;
    }

    public {{targetTypeName}}(int length, ArraySegment<byte> arrayOffset)
    {
        if (length < 0) ThrowOutOfRangeException();

        this.__byteOffsetX = arrayOffset.Offset;
{{ForLine("        ", 1, members.Length, x => $"this.__byteOffset{members[x].Name} = __byteOffset{members[x - 1].Name} + (Unsafe.SizeOf<{members[x - 1].MemberTypeFullName}>() * length);")}}
        this.__byteSize = __byteOffset{{members[members.Length - 1].Name}} + (Unsafe.SizeOf<{{members[members.Length - 1].MemberTypeFullName}}>() * length);
        this.__value = arrayOffset.Array!;
        this.__length = length;
    }

    public {{elementTypeFullName}} this[int index]
    {
        get
        {
            if ((uint)index >= (uint)__length) ThrowOutOfRangeException();
{{ForEachLine("            ", members, x => $"ref var __{x.Name} = ref Unsafe.Add(ref Unsafe.As<byte, {x.MemberTypeFullName}>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(__value), __byteOffset{x.Name})), index);")}}
            return new {{elementTypeFullName}}
            {
{{ForEachLine("                ", members, x => $"{x.Name} = __{x.Name},")}}
            };
        }
        set
        {
            if ((uint)index >= (uint)__length) ThrowOutOfRangeException();
{{ForEachLine("            ", members, x => $"Unsafe.Add(ref Unsafe.As<byte, {x.MemberTypeFullName}>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(__value), __byteOffset{x.Name})), index) = value.{x.Name};")}}
        }
    }

    public ReadOnlySpan<byte> GetRawSpan() => __value.AsSpan(__byteOffsetX, __byteSize);

    public bool SequenceEqual({{targetTypeName}} other)
    {
        return GetRawSpan().SequenceEqual(other.GetRawSpan());
    }

    static void ThrowOutOfRangeException()
    {
        throw new ArgumentOutOfRangeException();
    }
}
""";

        return code;
    }

    static string BuildMultiArrayList(string targetTypeName, ISymbol multiArrayType, INamedTypeSymbol elementType, MetaMember[] members)
    {
        var multiArrayTypeFullName = multiArrayType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var elementTypeFullName = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var code = $$"""
partial class {{targetTypeName}}
{
    const int DefaultCapacity = 4;

    {{multiArrayTypeFullName}} __array;
    int __length;

    public int Length => __length;
{{ForEachLine("    ", members, x => $"public Span<{x.MemberTypeFullName}> {x.Name} => __array.{x.Name}.Slice(0, __length);")}}

    public {{targetTypeName}}()
        : this(DefaultCapacity)
    {
    }

    public {{targetTypeName}}(int capacity)
    {
        if (capacity < 0) ThrowOutOfRangeIndex();
        __array = new {{multiArrayTypeFullName}}(capacity);
    }

    public {{elementTypeFullName}} this[int index]
    {
        get
        {
            if ((uint)index >= (uint)__length) ThrowOutOfRangeIndex();
            return __array[index];
        }
        set
        {
            if ((uint)index >= (uint)__length) ThrowOutOfRangeIndex();
            __array[index] = value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add({{elementTypeFullName}} item)
    {
        var array = __array;
        var size = __length;
        if ((uint)size < (uint)array.Length)
        {
            __length = size + 1;
            array[size] = item;
        }
        else
        {
            AddWithResize(item);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AddWithResize(Point3D item)
    {
        var size = __length;
        EnsureCapacity(size + 1);
        __length = size + 1;
        __array[size] = item;
    }

    void EnsureCapacity(int capacity)
    {
        int newCapacity = __array.Length == 0 ? DefaultCapacity : unchecked(2 * __array.Length);
        if ((uint)newCapacity > Array.MaxLength) newCapacity = Array.MaxLength;
        if (newCapacity < capacity) newCapacity = capacity;

        var newArray = new {{multiArrayTypeFullName}}(newCapacity);
        CopyTo(newArray);
        __array = newArray;
    }

    public void CopyTo({{multiArrayTypeFullName}} array)
    {
{{ForEachLine("        ", members, x => $"this.__array.{x.Name}.CopyTo(array.{x.Name});")}}    
    }

    public {{multiArrayTypeFullName}} ToArray()
    {
        var newArray = new {{multiArrayTypeFullName}}(__length);
        CopyTo(newArray);
        return newArray;
    }

    static void ThrowOutOfRangeIndex()
    {
        throw new ArgumentOutOfRangeException();
    }
}
""";

        return code;
    }

    static void AddSource(SourceProductionContext context, ISymbol targetSymbol, string code, string fileExtension = ".g.cs")
    {
        var fullType = targetSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
          .Replace("global::", "")
          .Replace("<", "_")
          .Replace(">", "_");

        var sb = new StringBuilder();

        sb.AppendLine(@"
// <auto-generated/>
#nullable enable
#pragma warning disable CS0108
#pragma warning disable CS0162
#pragma warning disable CS0164
#pragma warning disable CS0219
#pragma warning disable CS8600
#pragma warning disable CS8601
#pragma warning disable CS8602
#pragma warning disable CS8604
#pragma warning disable CS8619
#pragma warning disable CS8620
#pragma warning disable CS8631
#pragma warning disable CS8765
#pragma warning disable CS9074
#pragma warning disable CA1050

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
");

        var ns = targetSymbol.ContainingNamespace;
        if (!ns.IsGlobalNamespace)
        {
            sb.AppendLine($"namespace {ns} {{");
        }
        sb.AppendLine();

        sb.AppendLine(code);

        if (!ns.IsGlobalNamespace)
        {
            sb.AppendLine($"}}");
        }

        var sourceCode = sb.ToString();
        context.AddSource($"{fullType}{fileExtension}", sourceCode);
    }
}
