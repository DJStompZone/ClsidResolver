namespace ClsidResolver;

public sealed record CliOptions(string Clsid, bool DumpSchema)
{
    public static CliOptions Parse(string[] args)
    {
        var normalized = args
            .Where(arg => !string.IsNullOrWhiteSpace(arg))
            .Select(arg => arg.Trim())
            .ToArray();

        bool dumpSchema = normalized.Any(arg =>
            arg.Equals("--DumpSchema", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("-DumpSchema", StringComparison.OrdinalIgnoreCase));

        string clsid = normalized
            .FirstOrDefault(arg => !arg.StartsWith("-", StringComparison.Ordinal)) ?? string.Empty;

        return new CliOptions(clsid, dumpSchema);
    }
}
