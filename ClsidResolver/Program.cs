using System;
using ClsidResolver;

if (args.Length == 0)
{
    Console.WriteLine("CLSID Resolver Utility");
    Console.WriteLine("----------------------");
    Console.WriteLine("Usage: ClsidResolver.exe <CLSID>");
    Console.WriteLine("Example: ClsidResolver.exe \"{ABCD1234-6969-0420-BEEF-1C0FFEEC0DED}\"");
    return;
}

string targetClsid = args[0];
Console.WriteLine($"Attempting to crack SRD for CLSID: {targetClsid}\n");

string result = SrdDatabaseCracker.ResolveWinRtTask(targetClsid);

Console.WriteLine("Result:");
Console.WriteLine(result);