using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Data.Sqlite;

namespace ClsidResolver
{
    /// <summary>
    /// Bypasses Windows file locks to extract and read the Unified Background Process Manager (UBPM) 
    /// State Repository SQLite database.
    /// </summary>
    public static class SrdDatabaseCracker
    {
        private const string SrdOriginalPath = @"\ProgramData\Microsoft\Windows\AppRepository\StateRepository-Machine.srd";

        /// <summary>
        /// Creates a Volume Shadow Copy to extract the locked SRD database, then queries it for the given CLSID.
        /// </summary>
        public static string ResolveWinRtTask(string clsid)
        {
            string tempDbPath = Path.Combine(Path.GetTempPath(), $"Extracted_SRD_{Guid.NewGuid()}.srd");
            #if DEBUG
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Running in DEBUG configuration");
            Console.ResetColor();
            #endif
            try
            {
                Console.WriteLine("Initiating Volume Shadow Copy to bypass file locks...");
                string? shadowVolumePath = CreateShadowCopy("C:\\");

                if (string.IsNullOrEmpty(shadowVolumePath))
                {
                    return "Failed to create or locate Volume Shadow Copy.";
                }

                string shadowFilePath = shadowVolumePath + SrdOriginalPath;
                File.Copy(shadowFilePath, tempDbPath, true);
                Console.WriteLine($"Successfully extracted locked DB to: {tempDbPath}");

                return QuerySrdForClsid(tempDbPath, clsid);
            }
            catch (Exception ex)
            {
                return $"Error ripping SRD database: {ex.Message}";
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                if (File.Exists(tempDbPath))
                {
                    try
                    {
                        File.Delete(tempDbPath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"\n[Warning] Could not delete temp file. The OS will clean it up later. Error: {ex.Message}");
                    }
                }
            }
        }

        private static string QuerySrdForClsid(string dbPath, string clsid)
        {
            SQLitePCL.Batteries.Init();

            string formattedClsid = clsid.Trim('{', '}').ToLower();
            var results = new System.Text.StringBuilder();
            #if DEBUG
            var schema = new System.Text.StringBuilder();
            #endif

            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            var tables = new System.Collections.Generic.List<string>();
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    tables.Add(reader.GetString(0));
                }
            }

            #if DEBUG
            schema.AppendLine("SRD database schema:");
            #endif
            foreach (var table in tables)
            {
                var columns = new System.Collections.Generic.List<string>();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = $"PRAGMA table_info(\"{table}\")";
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        columns.Add(reader.GetString(1));
                    }
                }

                #if DEBUG
                schema.AppendLine($"  {table}: {(columns.Count > 0 ? string.Join(", ", columns) : "(no columns)")}");
                #endif

                if (columns.Count == 0) continue;

                using (var cmd = connection.CreateCommand())
                {
                    var whereClauses = new System.Collections.Generic.List<string>();
                    foreach (var col in columns)
                    {
                        whereClauses.Add($"\"{col}\" LIKE $clsid");
                    }

                    cmd.CommandText = $"SELECT * FROM \"{table}\" WHERE {string.Join(" OR ", whereClauses)}";
                    cmd.Parameters.AddWithValue("$clsid", $"%{formattedClsid}%");

                    try
                    {
                        using var reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            results.AppendLine($"\n[Match found in table: {table}]");
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string colName = reader.GetName(i);
                                string val = reader.GetValue(i)?.ToString() ?? "NULL";

                                if (val.Contains(formattedClsid, StringComparison.OrdinalIgnoreCase))
                                {
                                    results.AppendLine($"  -> MATCH in [{colName}]: {val}");
                                }
                                else if (colName.Contains("Name", StringComparison.OrdinalIgnoreCase) || colName.Contains("Id", StringComparison.OrdinalIgnoreCase))
                                {
                                    string contextVal = val.Length > 100 ? val.Substring(0, 97) + "..." : val;
                                    results.AppendLine($"  -> Context [{colName}]: {contextVal}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        #if DEBUG
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine($"[DEBUG] Query failed on table '{table}': {ex.Message}");
                        Console.ResetColor();
                        #endif
                    }
                }
            }

            if (results.Length > 0)
            {
                #if DEBUG
                return schema + "\nCLSID found in State Repository DB!" + results;
                #else
                return "CLSID found in State Repository DB!" + results;
                #endif
            }

            #if DEBUG
            return schema + "\nCLSID not found anywhere in State Repository. It is a hardcoded internal UBPM system task.";
            #else
            return "CLSID not found anywhere in State Repository. It is a hardcoded internal UBPM system task.";
            #endif
        }

        private static string? CreateShadowCopy(string driveLetter)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-NoProfile -Command \"$vss = Invoke-CimMethod -ClassName Win32_ShadowCopy -MethodName Create -Arguments @{{Volume='{driveLetter}'}}; if ($vss.ReturnValue -eq 0) {{ (Get-CimInstance -ClassName Win32_ShadowCopy -Filter \\\"ID='$($vss.ShadowID)'\\\").DeviceObject }} else {{ Write-Error \\\"VSS Error Code: $($vss.ReturnValue)\\\" }}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd().Trim();
            string error = process.StandardError.ReadToEnd().Trim();
            process.WaitForExit();

            if (string.IsNullOrEmpty(output) || !output.StartsWith(@"\\?\GLOBALROOT"))
            {
                Console.WriteLine("VSS Snapshot Failed.");
                if (!string.IsNullOrWhiteSpace(error)) Console.WriteLine($"Error: {error}");
                if (!string.IsNullOrWhiteSpace(output)) Console.WriteLine($"Output: {output}");
                return null;
            }

            return output;
        }
    }
}