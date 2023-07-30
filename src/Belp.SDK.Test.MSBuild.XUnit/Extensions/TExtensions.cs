namespace Belp.SDK.Test.MSBuild.XUnit.Extensions;

internal static class TExtensions
{
    public static SingleEnumerable<T> AsSingleEnumerable<T>(this T element)
    {
        return new SingleEnumerable<T>(element);
    }
}
