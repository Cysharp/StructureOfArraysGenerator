using Microsoft.CodeAnalysis;

namespace StructureOfArraysGenerator;

[Generator(LanguageNames.CSharp)]
public partial class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static context =>
        {
            context.AddSource("MultiArrayAttribute.cs", """
namespace StructureOfArraysGenerator;

using System;

[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
internal sealed class MultiArrayAttribute : Attribute
{
    public Type Type { get; }

    public MultiArrayAttribute(Type type)
    {
        this.Type = type;
    }
}
""");
        });


        var source = context.SyntaxProvider.ForAttributeWithMetadataName(
                 "StructureOfArraysGenerator.MultiArrayAttribute",
                 static (node, token) => true,
                 static (context, token) => context);

        context.RegisterSourceOutput(source, Emit);
    }

    static void Emit(SourceProductionContext context, GeneratorAttributeSyntaxContext source)
    {
        var targetType = (INamedTypeSymbol)source.Attributes[0].ConstructorArguments[0].Value!;

        var members = targetType.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(x => x.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal)
            .ToArray();

        // All target members should be unamanged


        // context.AddSource($"{fullType}.SampleGenerator.g.cs", code);
    }
}

public static class DiagnosticDescriptors
{
    const string Category = "SampleGenerator";

    public static readonly DiagnosticDescriptor ExistsOverrideToString = new(
        id: "SAMPLE001",
        title: "ToString override",
        messageFormat: "The GenerateToString class '{0}' has ToString override but it is not allowed.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}