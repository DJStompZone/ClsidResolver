using ClsidResolver;
using Xunit;

namespace ClsidResolver.Tests;

public class CommandLineOptionsTests
{
    [Fact]
    public void ParseArgs_AllowsDumpSchemaFlag()
    {
        var result = CliOptions.Parse(new[] { "{ABCD1234-6969-0420-BEEF-1C0FFEEC0DED}", "--DumpSchema" });

        Assert.Equal("{ABCD1234-6969-0420-BEEF-1C0FFEEC0DED}", result.Clsid);
        Assert.True(result.DumpSchema);
    }

    [Fact]
    public void ParseArgs_AllowsShortFlag()
    {
        var result = CliOptions.Parse(new[] { "{ABCD1234-6969-0420-BEEF-1C0FFEEC0DED}", "-DumpSchema" });

        Assert.Equal("{ABCD1234-6969-0420-BEEF-1C0FFEEC0DED}", result.Clsid);
        Assert.True(result.DumpSchema);
    }

    [Fact]
    public void ParseArgs_WithoutDumpSchemaFlag_DefaultsToFalse()
    {
        var result = CliOptions.Parse(new[] { "{ABCD1234-6969-0420-BEEF-1C0FFEEC0DED}" });

        Assert.Equal("{ABCD1234-6969-0420-BEEF-1C0FFEEC0DED}", result.Clsid);
        Assert.False(result.DumpSchema);
    }
}
