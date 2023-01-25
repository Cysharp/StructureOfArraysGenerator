using Microsoft.CodeAnalysis;

namespace StructureOfArraysGenerator;

public class MetaMember
{
    public ITypeSymbol MemberType { get; }
    public string MemberTypeFullName { get; set; }
    public string Name { get; }
    public bool IsField { get; }

    public MetaMember(ISymbol symbol)
    {
        this.Name = symbol.Name;
        if (symbol is IFieldSymbol f)
        {
            this.MemberType = f.Type;
            this.IsField = true;
        }
        else if (symbol is IPropertySymbol p)
        {
            this.MemberType = p.Type;
            this.IsField = false;
        }
        else
        {
            throw new InvalidOperationException("Symbol type is invalid. " + symbol.GetType().FullName);
        }
        this.MemberTypeFullName = this.MemberType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }
}
