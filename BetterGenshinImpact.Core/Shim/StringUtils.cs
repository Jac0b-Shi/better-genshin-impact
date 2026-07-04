namespace BetterGenshinImpact.Helpers;

public static class StringUtils
{
    public static bool IsNullOrEmpty(this string? s) => string.IsNullOrEmpty(s);
    public static bool IsNotNullOrEmpty(this string? s) => !string.IsNullOrEmpty(s);
    public static string RemoveAllSpace(this string s) => s.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");
}
