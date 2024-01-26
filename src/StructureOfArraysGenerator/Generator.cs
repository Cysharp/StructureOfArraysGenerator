using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using static StructureOfArraysGenerator.EmitHelper;

namespace StructureOfArraysGenerator;

[Generator(LanguageNames.CSharp)]
public partial class Generator : IIncrementalGenerator
{
#if !ROSLYN3

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(EmitAttributes);

        {
            var source = context.SyntaxProvider.ForAttributeWithMetadataName(
                     "StructureOfArraysGenerator.MultiArrayAttribute",
                     static (node, token) => node is StructDeclarationSyntax,
                     static (context, token) => context);

            context.RegisterSourceOutput(source, EmitMultiArray);
        }
        {
            var source = context.SyntaxProvider.ForAttributeWithMetadataName(
                     "StructureOfArraysGenerator.MultiArrayListAttribute",
                     static (node, token) => node is StructDeclarationSyntax,
                     static (context, token) => context);

            context.RegisterSourceOutput(source, EmitMultiArrayList);
        }
    }

#endif

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

        var constructor = GetElementConstructorInfo(members, elementType);

        // Verify target
        if (!VerifyMultiArray(context, (TypeDeclarationSyntax)source.TargetNode, source.TargetSymbol, elementType, members))
        {
            return;
        }

        // Generate Code
        var code = BuildMultiArray(source.TargetSymbol, elementType, constructor, members);

        AddSource(context, source.TargetSymbol, code);
    }

    static void EmitMultiArrayList(SourceProductionContext context, GeneratorAttributeSyntaxContext source)
    {
        var multiArrayAttr = source.TargetSymbol.GetAttributes()
            .FirstOrDefault(x => x.AttributeClass?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::StructureOfArraysGenerator.MultiArrayAttribute");

        if (multiArrayAttr == null)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MultiArrayIsNotExists, ((TypeDeclarationSyntax)source.TargetNode).Identifier.GetLocation(), source.TargetSymbol.Name));
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
                if (x.IsStatic) return false;

                if (!(x.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal))
                {
                    return false;
                }

                if (x is IFieldSymbol) return true;

                if (includeProperty && x is IPropertySymbol p)
                {
                    if (p.IsIndexer) return false;
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

    static IMethodSymbol? GetElementConstructorInfo(MetaMember[] members, INamedTypeSymbol elementType)
    {
        IMethodSymbol? constructor = null;
        if (elementType.Constructors.Length != 0)
        {
            var nameDict = new HashSet<string>(members.Select(x => x.Name), StringComparer.OrdinalIgnoreCase);

            var maxMatchCount = 0;
            foreach (var ctor in elementType.Constructors)
            {
                var matchCount = 0;
                foreach (var p in ctor.Parameters)
                {
                    if (nameDict.Contains(p.Name))
                    {
                        matchCount++;
                    }
                    else
                    {
                        matchCount = -1;
                        break;
                    }
                }

                if (matchCount > maxMatchCount)
                {
                    constructor = ctor; // use this.
                    maxMatchCount = matchCount;
                }
            }
        }

        return constructor;
    }

    static bool VerifyMultiArray(SourceProductionContext context, TypeDeclarationSyntax typeSyntax, ISymbol targetType, INamedTypeSymbol elementType, MetaMember[] members)
    {
        var hasError = false;

        // require partial
        if (!typeSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MustBePartial, typeSyntax.Identifier.GetLocation(), targetType.Name));
            hasError = true;
        }

        // require readonly
        if (!typeSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.ReadOnlyKeyword)))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MustBeReadOnly, typeSyntax.Identifier.GetLocation(), targetType.Name));
            hasError = true;
        }

        // element is not valuetype
        if (!elementType.IsValueType)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ElementIsNotValueType, typeSyntax.Identifier.GetLocation(), targetType.Name, elementType.Name));
            hasError = true;
        }

        // empty member is not allowed
        if (members.Length == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MemberEmpty, typeSyntax.Identifier.GetLocation(), targetType.Name, elementType.Name));
            hasError = true;
        }

        // All target members should be unmanaged
        if (!members.All(x => x.MemberType.IsUnmanagedType))
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MemberUnmanaged, typeSyntax.Identifier.GetLocation(), targetType.Name, elementType.Name));
            hasError = true;
        }

        return !hasError;
    }

    static string BuildMultiArray(ISymbol targetType, INamedTypeSymbol elementType, IMethodSymbol? elementConstructor, MetaMember[] members)
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
{{ForEachLine("    ", members, x => $"public Span<{x.MemberTypeFullName}> {x.Name} => MemoryMarshal.CreateSpan(ref Unsafe.As<byte, {x.MemberTypeFullName}>(ref Unsafe.Add(ref GetArrayDataReference(__value), __byteOffset{x.Name})), __length);")}}

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

        this.__byteOffset{{members[0].Name}} = arrayOffset.Offset;
{{ForLine("        ", 1, members.Length, x => $"this.__byteOffset{members[x].Name} = __byteOffset{members[x - 1].Name} + (Unsafe.SizeOf<{members[x - 1].MemberTypeFullName}>() * length);")}}
        this.__byteSize = __byteOffset{{members[members.Length - 1].Name}} + (Unsafe.SizeOf<{{members[members.Length - 1].MemberTypeFullName}}>() * length) - __byteOffset{{members[0].Name}};
        this.__value = arrayOffset.Array!;
        this.__length = length;

        if (arrayOffset.Count < this.__byteSize) ThrowOutOfRangeException();
    }

    public {{elementTypeFullName}} this[int index]
    {
        get
        {
            if ((uint)index >= (uint)__length) ThrowOutOfRangeException();
{{ForEachLine("            ", members, x => $"ref var __{x.Name} = ref Unsafe.Add(ref Unsafe.As<byte, {x.MemberTypeFullName}>(ref Unsafe.Add(ref GetArrayDataReference(__value), __byteOffset{x.Name})), index);")}}
            return {{BuildElementNew(elementType, elementConstructor, members)}}
            {
{{ForEachLine("                ", members.Where(x => !x.IsConstructorParameter), x => $"{x.Name} = __{x.Name},")}}
            };
        }
        set
        {
            if ((uint)index >= (uint)__length) ThrowOutOfRangeException();
{{ForEachLine("            ", members, x => $"Unsafe.Add(ref Unsafe.As<byte, {x.MemberTypeFullName}>(ref Unsafe.Add(ref GetArrayDataReference(__value), __byteOffset{x.Name})), index) = value.{x.Name};")}}
        }
    }

    public ReadOnlySpan<byte> GetRawSpan() => __value.AsSpan(__byteOffset{{members[0].Name}}, __byteSize);

    public bool SequenceEqual({{targetTypeName}} other)
    {
        return GetRawSpan().SequenceEqual(other.GetRawSpan());
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    public System.Collections.Generic.IEnumerable<{{elementTypeFullName}}> AsEnumerable()
    {
        foreach (var item in this)
        {
            yield return item;
        }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    static ref T GetArrayDataReference<T>(T[] array)
    {
#if NET5_0_OR_GREATER
        return ref MemoryMarshal.GetArrayDataReference(array);
#else
        return ref MemoryMarshal.GetReference(array.AsSpan());
#endif
    }

    static void ThrowOutOfRangeException()
    {
        throw new ArgumentOutOfRangeException();
    }

    public struct Enumerator
    {
        {{targetTypeName}} array;
        {{elementTypeFullName}} current;
        int index;

        public Enumerator({{targetTypeName}} array)
        {
            this.array = array;
            this.current = default;
            this.index = 0;
        }

        public {{elementTypeFullName}} Current => current;

        public bool MoveNext()
        {
            if (index >= array.Length) return false;
            current = array[index];
            index++;
            return true;
        }
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
{{multiArrayType.DeclaredAccessibility.ToCode()}} sealed partial class {{targetTypeName}}
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
    private void AddWithResize({{elementTypeFullName}} item)
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
{{ForEachLine("        ", members, x => $"this.__array.{x.Name}.Slice(0, __length).CopyTo(array.{x.Name});")}}
    }

    public {{multiArrayTypeFullName}} ToArray()
    {
        var newArray = new {{multiArrayTypeFullName}}(__length);
        CopyTo(newArray);
        return newArray;
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    public System.Collections.Generic.IEnumerable<{{elementTypeFullName}}> AsEnumerable()
    {
        foreach (var item in this)
        {
            yield return item;
        }
    }

    static void ThrowOutOfRangeIndex()
    {
        throw new ArgumentOutOfRangeException();
    }

    public struct Enumerator
    {
        {{targetTypeName}} list;
        {{elementTypeFullName}} current;
        int index;

        public Enumerator({{targetTypeName}} list)
        {
            this.list = list;
            this.current = default;
            this.index = 0;
        }

        public {{elementTypeFullName}} Current => current;

        public bool MoveNext()
        {
            if (index >= list.Length) return false;
            current = list[index];
            index++;
            return true;
        }
    }
}
""";

        return code;
    }

    static string BuildElementNew(INamedTypeSymbol elementType, IMethodSymbol? constructor, MetaMember[] members)
    {
        var elementTypeFullName = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        if (constructor == null || constructor.Parameters.Length == 0)
        {
            return $"new {elementTypeFullName}()";
        }
        else
        {
            var nameDict = members.ToDictionary(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase);
            var parameters = constructor.Parameters
                .Select(x =>
                {
                    if (nameDict.TryGetValue(x.Name, out var member))
                    {
                        member.IsConstructorParameter = true;
                        return $"__{member.Name}";
                    }
                    return null; // invalid, validated.
                })
                .Where(x => x != null);

            return $"new {elementTypeFullName}({string.Join(", ", parameters)})";
        }
    }

    static void AddSource(SourceProductionContext context, ISymbol targetSymbol, string code, string fileExtension = ".g.cs")
    {
        var fullType = targetSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
          .Replace("global::", "")
          .Replace("<", "_")
          .Replace(">", "_");

        var sb = new StringBuilder();

        sb.AppendLine("""
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
""");

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
