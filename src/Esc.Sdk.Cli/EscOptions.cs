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

        internal string GetEscExecutable()
        {
            if (EscPath != null)
            {
                return EscPath;
            }

            var searchPath = GetSearchPath();

            string escExecutable;
            switch (GetOsPlatform())
            {
                case OsPlatformType.Windows:
                    escExecutable = "esc_win64.exe";
                    break;
                case OsPlatformType.Linux:
                    escExecutable = "esc_linux64";
                    break;
                case OsPlatformType.Osx:
                    escExecutable = "esc_darwin64";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var fullEscExePath = Path.Combine(searchPath, escExecutable);
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

            if (!string.IsNullOrEmpty(websiteInstanceId))
            {
                return null;
            }

            if (!string.IsNullOrEmpty(azureWebJobsScriptRoot))
            {
                return azureWebJobsScriptRoot;
            }

            //if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID")))
            //{
            //    searchPath = GetEntryAssemblyLocation();
            //}
            //else
            //{
            //    searchPath = Environment.GetEnvironmentVariable("AzureWebJobsScriptRoot");
            //}

            return Directory.GetCurrentDirectory();
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
#if !NET35
            //https://github.com/dotnet/runtime/issues/21660#issuecomment-633628590
            // For compatibility reasons with Mono, PlatformID.Unix is returned on MacOSX. PlatformID.MacOSX
            // is hidden from the editor and shouldn't be used.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return OsPlatformType.Osx;
            }
#endif

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
    }
}