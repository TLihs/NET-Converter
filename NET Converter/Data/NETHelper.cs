using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NET_Converter.Data
{
    internal static class NETHelper
    {
        internal enum NETTargetVersions
        {
            NET50,
            NET60,
            NET70,
            NET80,
            NET90
        }

        internal enum NETSourceVersions
        {
            NETFramework45,
            NETFramework451,
            NETFramework452,
            NETFramework46,
            NETFramework461,
            NETFramework462,
            NETFramework47,
            NETFramework471,
            NETFramework472,
            NETFramework48,
            NETCoreApp20,
            NETCoreApp21,
            NETCoreApp22,
            NETCoreApp30,
            NETCoreApp31,
            NETCoreApp50,
            NETCoreApp60
        }

        internal static readonly string[] Appendices = new[]
        {
                "",
                "windows",
                "linux",
                "macos",
                "android",
                "ios",
                "maccatalyst",
                "tvos",
                "browser"
            };

        internal static readonly string[] AvailableNETTargetVersions = new[]
        {
                ".NET 5.0",
                ".NET 6.0",
                ".NET 7.0",
                ".NET 8.0",
                ".NET 9.0"
            };

        internal static NETSourceVersions ParseSourceVersion(string versionString)
        {
            return versionString switch
            {
                "v4.5" => NETSourceVersions.NETFramework45,
                "v4.5.1" => NETSourceVersions.NETFramework451,
                "v4.5.2" => NETSourceVersions.NETFramework452,
                "v4.6" => NETSourceVersions.NETFramework46,
                "v4.6.1" => NETSourceVersions.NETFramework461,
                "v4.6.2" => NETSourceVersions.NETFramework462,
                "v4.7" => NETSourceVersions.NETFramework47,
                "v4.7.1" => NETSourceVersions.NETFramework471,
                "v4.7.2" => NETSourceVersions.NETFramework472,
                "v4.8" => NETSourceVersions.NETFramework48,
                "netcoreapp2.0" => NETSourceVersions.NETCoreApp20,
                "netcoreapp2.1" => NETSourceVersions.NETCoreApp21,
                "netcoreapp2.2" => NETSourceVersions.NETCoreApp22,
                "netcoreapp3.0" => NETSourceVersions.NETCoreApp30,
                "netcoreapp3.1" => NETSourceVersions.NETCoreApp31,
                "net5.0" => NETSourceVersions.NETCoreApp50,
                "net6.0" => NETSourceVersions.NETCoreApp60,
                _ => throw new ArgumentException("Invalid source version.")
            };
        }

        internal static string SourceVersionToString(NETSourceVersions version)
        {
            return version switch
            {
                NETSourceVersions.NETFramework45 => "v4.5",
                NETSourceVersions.NETFramework451 => "v4.5.1",
                NETSourceVersions.NETFramework452 => "v4.5.2",
                NETSourceVersions.NETFramework46 => "v4.6",
                NETSourceVersions.NETFramework461 => "v4.6.1",
                NETSourceVersions.NETFramework462 => "v4.6.2",
                NETSourceVersions.NETFramework47 => "v4.7",
                NETSourceVersions.NETFramework471 => "v4.7.1",
                NETSourceVersions.NETFramework472 => "v4.7.2",
                NETSourceVersions.NETFramework48 => "v4.8",
                NETSourceVersions.NETCoreApp20 => "netcoreapp2.0",
                NETSourceVersions.NETCoreApp21 => "netcoreapp2.1",
                NETSourceVersions.NETCoreApp22 => "netcoreapp2.2",
                NETSourceVersions.NETCoreApp30 => "netcoreapp3.0",
                NETSourceVersions.NETCoreApp31 => "netcoreapp3.1",
                NETSourceVersions.NETCoreApp50 => "net5.0",
                NETSourceVersions.NETCoreApp60 => "net6.0",
                _ => throw new ArgumentException("Invalid source version.")
            };
        }

        internal static NETTargetVersions ParseTargetVersion(string versionString)
        {
            // Strip any appendices like "-windows"
            var baseVersion = versionString.Split('-')[0];

            return baseVersion switch
            {
                "net5.0" => NETTargetVersions.NET50,
                "net6.0" => NETTargetVersions.NET60,
                "net7.0" => NETTargetVersions.NET70,
                "net8.0" => NETTargetVersions.NET80,
                "net9.0" => NETTargetVersions.NET90,
                _ => throw new ArgumentException("Invalid target version.")
            };
        }

        internal static string TargetVersionToString(NETTargetVersions version, string appendix = "")
        {
            var baseVersion = version switch
            {
                NETTargetVersions.NET50 => "net5.0",
                NETTargetVersions.NET60 => "net6.0",
                NETTargetVersions.NET70 => "net7.0",
                NETTargetVersions.NET80 => "net8.0",
                NETTargetVersions.NET90 => "net9.0",
                _ => throw new ArgumentException("Invalid target version.")
            };

            return string.IsNullOrEmpty(appendix) ? baseVersion : $"{baseVersion}-{appendix}";
        }

        public static async Task<string> ExecuteCommandAsync(string command, string arguments)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process
            {
                StartInfo = processStartInfo
            };

            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Command '{command} {arguments}' failed with error: {error}");
            }

            return output;
        }

        public static async Task<bool> IsDotNetInstalledAsync()
        {
            try
            {
                string version = await ExecuteCommandAsync("dotnet", "--version");
                return !string.IsNullOrEmpty(version);
            }
            catch
            {
                return false;
            }
        }

        public static async Task<string> ListDotNetSdksAsync()
        {
            try
            {
                return await ExecuteCommandAsync("dotnet", "--list-sdks");
            }
            catch
            {
                return string.Empty;
            }
        }

        public static async Task<string> ListDotNetRuntimesAsync()
        {
            try
            {
                return await ExecuteCommandAsync("dotnet", "--list-runtimes");
            }
            catch
            {
                return string.Empty;
            }
        }

        public static List<string> ParseSdkVersions(string sdkList)
        {
            var versions = new List<string>();
            var lines = sdkList.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var version = line.Split(' ')[0];
                var majorVersion = version.Split('.')[0];
                versions.Add($"net{majorVersion}.0");
            }

            return versions.Distinct().ToList();
        }
    }
}
