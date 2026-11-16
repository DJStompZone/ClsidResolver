using System;
using ClsidResolver;

var cliOptions = CliOptions.Parse(args);

if (args.Length == 0 || string.IsNullOrWhiteSpace(cliOptions.Clsid) || args.Any(arg => arg.Equals("--help", StringComparison.OrdinalIgnoreCase) || arg.Equals("-h", StringComparison.OrdinalIgnoreCase)))
{
    Console.WriteLine("CLSID Resolver Utility");
    Console.WriteLine("----------------------");
    Console.WriteLine("Usage: ClsidResolver.exe <CLSID> [--DumpSchema]");
    Console.WriteLine("Example: ClsidResolver.exe \"{ABCD1234-6969-0420-BEEF-1C0FFEEC0DED}\"");
    Console.WriteLine("Example: ClsidResolver.exe \"{ABCD1234-6969-0420-BEEF-1C0FFEEC0DED}\" --DumpSchema");
    return;
}

string targetClsid = cliOptions.Clsid;
Console.WriteLine($"Attempting to crack SRD for CLSID: {targetClsid}\n");

string result = SrdDatabaseCracker.ResolveWinRtTask(targetClsid, cliOptions.DumpSchema);

Console.WriteLine("Result:");
Console.WriteLine(result);