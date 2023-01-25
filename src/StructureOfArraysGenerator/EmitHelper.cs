using Microsoft.CodeAnalysis;

namespace StructureOfArraysGenerator;

internal static class EmitHelper
{
    public static string ToCode(this Accessibility accessibility)
    {
        switch (accessibility)
        {
            case Accessibility.NotApplicable:
                return "";
            case Accessibility.Private:
                return "private";
            case Accessibility.ProtectedAndInternal:
                return "private protected";
            case Accessibility.Protected:
                return "protected";
            case Accessibility.Internal:
                return "internal";
            case Accessibility.ProtectedOrInternal:
                return "protected internal";
            case Accessibility.Public:
                return "public";
            default:
                return "";
        }
    }

    public static string ForEachLine<T>(string indent, IEnumerable<T> values, Func<T, string> lineSelector)
    {
        return string.Join(Environment.NewLine, values.Select(x => indent + lineSelector(x)));
    }

    public static string ForLine(string indent, int begin, int end, Func<int, string> lineSelector)
    {
        return string.Join(Environment.NewLine, Enumerable.Range(begin, end - begin).Select(x => indent + lineSelector(x)));
    }
}
