namespace StructureOfArraysGenerator;

internal static class EmitHelper
{
    public static string ForEachLine<T>(string indent, T[] values, Func<T, string> lineSelector)
    {
        return string.Join(Environment.NewLine, values.Select(x => indent + lineSelector(x)));
    }

    public static string ForLine(string indent, int begin, int end, Func<int, string> lineSelector)
    {
        return string.Join(Environment.NewLine, Enumerable.Range(begin, end - begin).Select(x => indent + lineSelector(x)));
    }
}
