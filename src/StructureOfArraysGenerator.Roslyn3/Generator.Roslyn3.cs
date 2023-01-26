using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace StructureOfArraysGenerator;

public partial class Generator : ISourceGenerator
{
    const string MultiArrayAttribtue = "StructureOfArraysGenerator.MultiArrayAttribute";
    const string MultiArrayListAttribtue = "StructureOfArraysGenerator.MultiArrayListAttribute";

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForPostInitialization(x => EmitAttributes(new IncrementalGeneratorPostInitializationContext(x)));

        context.RegisterForSyntaxNotifications(() => new CompositeReceiver(
            new ForAttributeWithMetadataName(MultiArrayAttribtue, (node, token) => node is StructDeclarationSyntax),
            new ForAttributeWithMetadataName(MultiArrayListAttribtue, (node, token) => node is StructDeclarationSyntax)
        ));
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not CompositeReceiver compositeReceiver)
        {
            return;
        }

        var productionContext = new SourceProductionContext(context);

        foreach (var receiver in compositeReceiver.GetReceivers<ForAttributeWithMetadataName>())
        {
            if (receiver.FullyQualifiedMetadataName == MultiArrayAttribtue)
            {
                foreach (var item in receiver.Values)
                {
                    EmitMultiArray(productionContext, item);
                }
            }
            else if (receiver.FullyQualifiedMetadataName == MultiArrayListAttribtue)
            {
                foreach (var item in receiver.Values)
                {
                    EmitMultiArrayList(productionContext, item);
                }
            }
        }
    }
}
