using Microsoft.CodeAnalysis;

namespace StructureOfArraysGenerator;

public static class DiagnosticDescriptors
{
    const string Category = "GenerateStructureOfArrays";

    public static readonly DiagnosticDescriptor MustBePartial = new(
        id: "SOA001",
        title: "MultiArray struct must be partial",
        messageFormat: "The MultiArray type '{0}' must be partial",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MustBeReadOnly = new(
        id: "SOA002",
        title: "MultiArray struct must be readonly",
        messageFormat: "The MultiArray struct '{0}' must be readonly",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ElementIsNotValueType = new(
        id: "SOA003",
        title: "MultiArray struct element type only allows value type",
        messageFormat: "The MultiArray struct '{0}' element '{1}' only allows value type",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MemberEmpty = new(
        id: "SOA004",
        title: "MultiArray struct element type members does not allow empty",
        messageFormat: "The MultiArray struct '{0}' element '{1}' does not allow empty",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MemberUnmanaged = new(
        id: "SOA005",
        title: "All MultiArray struct element type members must be unmanaged",
        messageFormat: "The MultiArray struct '{0}' element '{1}' members contains reference type",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MultiArrayIsNotExists = new(
        id: "SOA006",
        title: "MultiArrayList attribute require to annotate MultiArrayAttribute",
        messageFormat: "MultiArrayList annotated struct '{0}' requires MultiArrayAttribtue",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}