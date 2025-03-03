using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Xml.Linq;
using static NET_Converter.Data.NETHelper;

namespace NET_Converter.Data
{
    internal static class Project
    {

        private static string _projectPath;
        private static NETSourceVersions? _sourceFramework;
        private static XDocument _csproj;

        internal static string ProjectPath
        {
            get => _projectPath;
            set
            {
                if (string.IsNullOrEmpty(value) || !File.Exists(value))
                    throw new ArgumentException("Invalid project path.");
                _projectPath = value;
            }
        }

        internal static NETSourceVersions? SourceFramework => _sourceFramework;

        static Project()
        {
            _projectPath = string.Empty;
            _sourceFramework = null;
            _csproj = new XDocument();
        }

        internal static async Task<string> AnalyzeProjectAsync(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath) || !File.Exists(projectPath))
                throw new ArgumentException("Invalid project path.");

            _projectPath = projectPath;

            return await Task.Run(() =>
            {
                string? targetFramework = null;
                lock (_csproj)
                {
                    _csproj = XDocument.Load(projectPath);
                    XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";

                    targetFramework = _csproj.Root?.Descendants(ns + "TargetFramework").FirstOrDefault()?.Value;

                    if (targetFramework == null)
                    {
                        targetFramework = _csproj.Root?.Descendants(ns + "TargetFrameworkVersion").FirstOrDefault()?.Value;
                        if (targetFramework == null)
                        {
                            var targetFrameworks = _csproj.Root?.Descendants(ns + "TargetFrameworks").FirstOrDefault()?.Value;
                            if (targetFrameworks != null)
                                targetFramework = targetFrameworks.Split(';').FirstOrDefault();
                        }
                    }
                }

                if (targetFramework != null)
                {
                    _sourceFramework = ParseSourceVersion(targetFramework);
                    return targetFramework;
                }

                return "Target Framework not found.";
            });
        }

        internal static async Task MigrateProjectAsync(NETTargetVersions targetVersion, string appendix = "")
        {
            if (string.IsNullOrEmpty(_projectPath) || !File.Exists(_projectPath))
                throw new ArgumentException("Invalid project path.");

            if (_sourceFramework == null)
                throw new InvalidOperationException("Source framework not determined. Please analyze the project first.");

            await Task.Run(() =>
            {
                // Backup the original project file and other files to be changed or removed
                string backupZipPath = _projectPath + ".backup.zip";
                using (var archive = ZipFile.Open(backupZipPath, ZipArchiveMode.Create))
                {
                    archive.CreateEntryFromFile(_projectPath, Path.GetFileName(_projectPath));
                    AddFileToArchive(archive, "App.config");
                    AddFileToArchive(archive, Path.Combine("Properties", "AssemblyInfo.cs"));
                    AddFileToArchive(archive, "Web.config");
                }

                XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";

                lock (_csproj)
                {
                    _csproj ??= XDocument.Load(_projectPath);

                    if (_csproj.Root == null)
                        throw new InvalidOperationException("Invalid project file.");

                    // Add SDK attribute
                    _csproj.Root.SetAttributeValue("Sdk", "Microsoft.NET.Sdk");

                    // Remove unnecessary elements
                    _csproj.Root.Elements(ns + "PropertyGroup")
                        .Elements(ns + "TargetFrameworkVersion")
                        .Remove();
                    _csproj.Root.Elements(ns + "PropertyGroup")
                        .Elements(ns + "TargetFrameworks")
                        .Remove();

                    // Set TargetFramework
                    var targetFrameworkElement = _csproj.Root.Elements(ns + "PropertyGroup")
                        .Elements(ns + "TargetFramework")
                        .FirstOrDefault();
                    if (targetFrameworkElement != null)
                    {
                        targetFrameworkElement.Value = TargetVersionToString(targetVersion, appendix);
                    }
                    else
                    {
                        _csproj.Root.Elements(ns + "PropertyGroup").FirstOrDefault()?.Add(new XElement(ns + "TargetFramework", TargetVersionToString(targetVersion, appendix)));
                    }

                    // Remove unnecessary files
                    RemoveUnnecessaryFiles();

                    _csproj.Save(_projectPath);
                }
            });
        }

        internal static async void UndoMigration()
        {
            if (string.IsNullOrEmpty(_projectPath))
                throw new Exception("Project path not set.");

            if (!File.Exists(_projectPath + ".backup.zip"))
                throw new ArgumentException("Backup not found.");

            await Task.Run(() =>
            {
                string backupZipPath = _projectPath + ".backup.zip";
#pragma warning disable CS8600 // Possible null reference argument.
                string extractPath = Path.GetDirectoryName(_projectPath);
#pragma warning restore CS8600 // Possible null reference argument.

#pragma warning disable CS8604 // Possible null reference argument.
                ZipFile.ExtractToDirectory(backupZipPath, extractPath, true);
#pragma warning restore CS8600 // Possible null reference argument.
            });
        }

        private static void AddFileToArchive(ZipArchive archive, string relativeFilePath)
        {
            string filePath = Path.Combine(Path.GetDirectoryName(_projectPath), relativeFilePath);
            if (File.Exists(filePath))
            {
                archive.CreateEntryFromFile(filePath, relativeFilePath);
            }
        }

        private static void RemoveUnnecessaryFiles()
        {
            if (_projectPath == null)
                throw new InvalidOperationException("Project path not set.");

            // Remove App.config if it exists
#pragma warning disable CS8604 // Possible null reference argument.
            string appConfigPath = Path.Combine(Path.GetDirectoryName(_projectPath), "App.config");
#pragma warning restore CS8604 // Possible null reference argument.
            if (File.Exists(appConfigPath))
            {
                File.Delete(appConfigPath);
            }

            // Remove AssemblyInfo.cs if it exists
#pragma warning disable CS8604 // Possible null reference argument.
            string assemblyInfoPath = Path.Combine(Path.GetDirectoryName(_projectPath), "Properties", "AssemblyInfo.cs");
#pragma warning restore CS8604 // Possible null reference argument.
            if (File.Exists(assemblyInfoPath))
            {
                File.Delete(assemblyInfoPath);
            }

            // Remove Web.config if it exists
#pragma warning disable CS8604 // Possible null reference argument.
            string webConfigPath = Path.Combine(Path.GetDirectoryName(_projectPath), "Web.config");
#pragma warning restore CS8604 // Possible null reference argument.
            if (File.Exists(webConfigPath))
            {
                File.Delete(webConfigPath);
            }
        }

        internal static async Task CheckNuGetPackagesAsync()
        {
            if (string.IsNullOrEmpty(_projectPath) || !File.Exists(_projectPath))
                throw new ArgumentException("Invalid project path.");

            await Task.Run(async () =>
            {
                lock (_csproj)
                {
                    _csproj ??= XDocument.Load(_projectPath);
                }

                var packageReferences = _csproj.Root?.Descendants("PackageReference").ToList();
                if (packageReferences == null || !packageReferences.Any())
                {
                    Console.WriteLine("No NuGet packages found.");
                    return;
                }

                foreach (var packageReference in packageReferences)
                {
                    var packageName = packageReference.Attribute("Include")?.Value;
                    var packageVersion = packageReference.Attribute("Version")?.Value;

                    if (!string.IsNullOrEmpty(packageName) && !string.IsNullOrEmpty(packageVersion))
                    {
                        var latestVersion = await GetLatestNuGetPackageVersionAsync(packageName);
                        if (latestVersion != null && latestVersion != packageVersion)
                        {
                            Console.WriteLine($"Package '{packageName}' has a newer version available: {latestVersion} (current: {packageVersion})");
                        }
                        else
                        {
                            Console.WriteLine($"Package '{packageName}' is up to date.");
                        }
                    }
                }
            });
        }

        private static async Task<string?> GetLatestNuGetPackageVersionAsync(string packageName)
        {
            using var httpClient = new HttpClient();
            var url = $"https://api.nuget.org/v3-flatcontainer/{packageName}/index.json";
            var response = await httpClient.GetStringAsync(url);
            using var jsonDoc = JsonDocument.Parse(response);
            var versions = jsonDoc.RootElement.GetProperty("versions").EnumerateArray().Select(v => v.GetString()).ToArray();
            return versions.LastOrDefault();
        }
    }
}
