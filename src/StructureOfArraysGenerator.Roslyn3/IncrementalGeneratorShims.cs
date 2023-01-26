using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace StructureOfArraysGenerator;

public interface IIncrementalGenerator
{
}

public struct IncrementalGeneratorPostInitializationContext
{
    GeneratorPostInitializationContext context;

    public IncrementalGeneratorPostInitializationContext(GeneratorPostInitializationContext context)
    {
        this.context = context;
    }

    public void AddSource(string hintName, string source)
    {
        context.AddSource(hintName, source);
    }
}

public struct SourceProductionContext
{
    GeneratorExecutionContext context;

    public SourceProductionContext(GeneratorExecutionContext context)
    {
        this.context = context;
    }

    public CancellationToken CancellationToken => context.CancellationToken;

    public void ReportDiagnostic(Diagnostic diagnostic)
    {
        context.ReportDiagnostic(diagnostic);
    }

    public void AddSource(string hintName, string source)
    {
        context.AddSource(hintName, source);
    }
}

public struct GeneratorAttributeSyntaxContext
{
    public SyntaxNode TargetNode { get; }
    public ISymbol TargetSymbol { get; }
    public SemanticModel SemanticModel { get; }
    public ImmutableArray<AttributeData> Attributes { get; }

    public GeneratorAttributeSyntaxContext(
        SyntaxNode targetNode,
        ISymbol targetSymbol,
        SemanticModel semanticModel,
        ImmutableArray<AttributeData> attributes)
    {
        TargetNode = targetNode;
        TargetSymbol = targetSymbol;
        SemanticModel = semanticModel;
        Attributes = attributes;
    }
}

public class CompositeReceiver : ISyntaxContextReceiver
{
    ISyntaxContextReceiver[] receivers;

    public IEnumerable<T> GetReceivers<T>() => receivers.OfType<T>();

    public CompositeReceiver(params ISyntaxContextReceiver[] receivers)
    {
        this.receivers = receivers;
    }

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        foreach (var item in receivers)
        {
            item.OnVisitSyntaxNode(context);
        }
    }
}

public class ForAttributeWithMetadataName : ISyntaxContextReceiver
{
    string fullyQualifiedMetadataName;
    Func<SyntaxNode, CancellationToken, bool> predicate;

    public string FullyQualifiedMetadataName => fullyQualifiedMetadataName;

    public ForAttributeWithMetadataName(string fullyQualifiedMetadataName, Func<SyntaxNode, CancellationToken, bool> predicate)
    {
        this.fullyQualifiedMetadataName = fullyQualifiedMetadataName;
        this.predicate = predicate;
    }

    public List<GeneratorAttributeSyntaxContext> Values { get; } = new();

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (!predicate(context.Node, CancellationToken.None)) return;

        var targetNode = context.Node;
        var semanticModel = context.SemanticModel;

        var targetSymbol = semanticModel.GetDeclaredSymbol(targetNode);
        if (targetSymbol is null) return;

        var attributes = targetSymbol.GetAttributes();
        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass == null) continue;

            var attrFullName = attribute.AttributeClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");

            if (attrFullName == fullyQualifiedMetadataName)
            {
                Values.Add(new GeneratorAttributeSyntaxContext(targetNode, targetSymbol, semanticModel, ImmutableArray.Create(attribute)));
            }
        }
    }
}