using System.Text.RegularExpressions;

namespace Shelly.Backend.Services;

public static class Slugger
{
    public static string Slugify(string input)
    {
        var lowered = (input ?? "").Trim().ToLowerInvariant();
        var replaced = Regex.Replace(lowered, @"[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrEmpty(replaced) ? "workspace" : replaced;
    }
}