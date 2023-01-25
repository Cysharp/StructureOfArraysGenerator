using Microsoft.CodeAnalysis;

namespace StructureOfArraysGenerator;

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