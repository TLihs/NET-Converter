using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace NET_Converter.Data
{
    public static class Project
    {
        public enum NETTargetVersions
        {
            NET50,
            NET60,
            NET70,
            NET80,
            NET90
        }

        public enum NETSourceVersions
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

        private static string _projectPath;
        private static NETSourceVersions? _sourceFramework;
        private static XDocument _csproj;

        public static string ProjectPath
        {
            get => _projectPath;
            set
            {
                if (string.IsNullOrEmpty(value) || !File.Exists(value))
                    throw new ArgumentException("Invalid project path.");
                _projectPath = value;
            }
        }

        public static NETSourceVersions? SourceFramework => _sourceFramework;

        static Project()
        {
            _projectPath = string.Empty;
            _sourceFramework = null;
            _csproj = new XDocument();
        }

        public static async Task<string> AnalyzeProjectAsync(string projectPath)
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

        public static string[] GetAvailableNETTargetVersions()
        {
            return
            [
                ".NET 5.0",
                    ".NET 6.0",
                    ".NET 7.0",
                    ".NET 8.0",
                    ".NET 9.0"
            ];
        }

        public static NETSourceVersions ParseSourceVersion(string versionString)
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

        public static string SourceVersionToString(NETSourceVersions version)
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

        public static NETTargetVersions ParseTargetVersion(string versionString)
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

        public static string TargetVersionToString(NETTargetVersions version, string appendix = "")
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

        public static async Task MigrateProjectAsync(NETTargetVersions targetVersion, string appendix = "")
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

                    var targetFrameworkElement = _csproj.Root?.Descendants(ns + "TargetFramework").FirstOrDefault();
                    if (targetFrameworkElement != null)
                    {
                        targetFrameworkElement.Value = TargetVersionToString(targetVersion, appendix);
                    }
                    else
                    {
                        var targetFrameworkVersionElement = _csproj.Root?.Descendants(ns + "TargetFrameworkVersion").FirstOrDefault();
                        if (targetFrameworkVersionElement != null)
                        {
                            targetFrameworkVersionElement.Value = TargetVersionToString(targetVersion, appendix);
                        }
                        else
                        {
                            var targetFrameworksElement = _csproj.Root?.Descendants(ns + "TargetFrameworks").FirstOrDefault();
                            if (targetFrameworksElement != null)
                            {
                                targetFrameworksElement.Value = TargetVersionToString(targetVersion, appendix);
                            }
                            else
                            {
                                // If no target framework elements are found, add a new one
                                _csproj.Root?.Add(new XElement(ns + "PropertyGroup",
                                    new XElement(ns + "TargetFramework", TargetVersionToString(targetVersion, appendix))));
                            }
                        }
                    }

                    // Remove unnecessary files
                    RemoveUnnecessaryFiles();

                    _csproj.Save(_projectPath);
                }
            });
        }

        public static async void UndoMigration()
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
    }
}
