using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Esc.Sdk.Cli
{
    /// <summary>
    ///     Options for the functionality of Esc.
    /// </summary>
    public class EscOptions
    {
        /// <summary>
        ///     The environment name.
        /// </summary>
        public string EnvironmentName { get; set; }

        /// <summary>
        ///     The path to the esc.exe file.
        ///     Default path is the executing assembly path + "esc.exe".
        /// </summary>
        public string EscPath { get; set; }

        /// <summary>
        ///     The organization name.
        /// </summary>
        public string OrgName { get; set; }

        /// <summary>
        ///     The project name.
        ///     Default is "default".
        /// </summary>
        public string ProjectName { get; set; } = "default";

        /// <summary>
        ///     Overrides the environment key to use.
        ///     Default is the "PULUMI_ACCESS_TOKEN" environment variable.
        /// </summary>
        public string PulumiAccessToken { get; set; }

        /// <summary>
        ///     Timeout in seconds for http requests.
        ///     Default 15.
        /// </summary>
        public int Timeout { get; set; } = 15;

        /// <summary>
        ///     Defines if the caching option should be used.
        ///     Default true when in DEBUG mode, otherwise false.
        /// </summary>
        public bool? UseCache { get; set; }


        /// <summary>
        ///     The cache expiration duration.
        ///     Default is 4 hours.
        /// </summary>
        public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromHours(4);

        internal string GetEscExecutable()
        {
            if (EscPath != null)
            {
                return EscPath;
            }

            var searchPath = GetSearchPath();
            var architecture = GetArchitecture();

            string escExecutable;
            switch (GetOsPlatform())
            {
                case OsPlatformType.Windows:
                    escExecutable = $"esc_win-{architecture}.exe";
                    break;
                case OsPlatformType.Linux:
                    escExecutable = $"esc_linux-{architecture}";
                    break;
                case OsPlatformType.Osx:
                    escExecutable = $"esc_darwin-{architecture}";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var fullEscExePath = Path.Combine(searchPath, "esc", escExecutable);
            Trace.WriteLine($"Using '{fullEscExePath}' as esc executable.");

            if (!File.Exists(fullEscExePath))
            {
                throw new FileNotFoundException(
                    "Esc executable was not found. Please specify the full path via the options.", fullEscExePath);
            }

            return fullEscExePath;
        }

        private static string GetSearchPath()
        {
            var websiteInstanceId = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID");
            var azureWebJobsScriptRoot = Environment.GetEnvironmentVariable("AzureWebJobsScriptRoot");

            //if running an Azure Function, in Azure, return null
            if (!string.IsNullOrEmpty(websiteInstanceId))
            {
                return null;
            }

            //if running an Azure Function local, return the AzureWebJobsScriptRoot
            if (!string.IsNullOrEmpty(azureWebJobsScriptRoot))
            {
                return azureWebJobsScriptRoot;
            }

            return GetEntryAssemblyLocation() ?? Directory.GetCurrentDirectory();
        }

        private static string GetEntryAssemblyLocation()
        {
            var assembly = Assembly.GetEntryAssembly();

            return assembly == null
                ? null
                : Path.GetDirectoryName(assembly.Location);
        }

        /// <summary>
        ///     Get the current os platform.
        /// </summary>
        public static OsPlatformType GetOsPlatform()
        {
            //https://github.com/dotnet/runtime/issues/21660#issuecomment-633628590
            // For compatibility reasons with Mono, PlatformID.Unix is returned on MacOSX. PlatformID.MacOSX
            // is hidden from the editor and shouldn't be used.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return OsPlatformType.Osx;
            }

            var os = Environment.OSVersion;
            var pid = os.Platform;
            switch (pid)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                    return OsPlatformType.Windows;
                case PlatformID.Unix:
                    return OsPlatformType.Linux;
                default:
                    throw new PlatformNotSupportedException($"'{pid} is not supported.");
            }
        }

        /// <summary>
        /// Gets the current system architecture as a string.
        /// </summary>
        /// <returns>"x64" for 64-bit Intel/AMD or "arm64" for 64-bit ARM.</returns>
        private static string GetArchitecture()
        {
            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.Arm64:
                    return "arm64";
                case Architecture.X64:
                    return "x64";
                case Architecture.X86:
                    // Fallback to x64 for 32-bit processes on 64-bit Windows
                    if (Environment.Is64BitOperatingSystem)
                    {
                        return "x64";
                    }
                    throw new PlatformNotSupportedException("32-bit (x86) architecture is not supported.");
                case Architecture.Arm:
                    throw new PlatformNotSupportedException("32-bit ARM architecture is not supported.");
                default:
                    throw new PlatformNotSupportedException($"Architecture {RuntimeInformation.ProcessArchitecture} is not supported.");
            }
        }
    }
}