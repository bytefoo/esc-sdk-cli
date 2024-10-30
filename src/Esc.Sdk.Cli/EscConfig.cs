using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Esc.Sdk.Cli
{
    public class EscConfig : IEscConfig
    {
        private readonly EscOptions _options;

        public EscConfig() : this(new EscOptions())
        {
        }

        public EscConfig(EscOptions options)
        {
            _options = options;
        }

        public void Load()
        {
            Load(false);
        }

        public void Load(bool forceUpdate)
        {
            try
            {
                var config = InnerLoad();
                PatchEnvironmentVariables(config, forceUpdate);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw new InvalidOperationException("An error occurred while fetching Esc configuration.", ex);
            }
        }

        internal string InnerLoadRaw()
        {
            var fullEscExePath = _options.GetEscExecutable();

            if (fullEscExePath == null || !File.Exists(fullEscExePath))
            {
                throw new FileNotFoundException(
                    "Esc executable was not found. Please specify the full path via the options.",
                    fullEscExePath);
            }

            var esc = _options.PulumiAccessToken;

            if (esc == null)
            {
                throw new InvalidOperationException(
                    "Access token not found. Please add the 'PULUMI_ACCESS_TOKEN' environment variable or configure the access token via the options.");
            }

            var arguments = $"open {_options.OrgName}/{_options.ProjectName}/{_options.EnvironmentName}";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo(fullEscExePath, arguments)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardErrorEncoding = Encoding.UTF8,
                    StandardOutputEncoding = Encoding.UTF8,
                    EnvironmentVariables =
                    {
                        ["PULUMI_ACCESS_TOKEN"] = esc
                    }
                }
            };

            process.Start();

            var output = process.StandardOutput.ReadToEnd();
            var successfulExit =
                process.WaitForExit((int) TimeSpan.FromSeconds(_options.Timeout + 2).TotalMilliseconds);

            if (!successfulExit)
            {
                throw new InvalidOperationException("Esc process timed out.");
            }

            if (!output.StartsWith("{"))
            {
                throw new InvalidOperationException($"Esc returned a non-config object: '{output}'.");
            }

            return output;
        }

        internal Dictionary<string, string>? InnerLoad()
        {
            var config = InnerLoadRaw();
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(config);

            return dict;
        }

        internal static void PatchEnvironmentVariables(Dictionary<string, string>? config, bool forceUpdate)
        {
            var skipExisting = !forceUpdate;

            var existingKeys = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process).Keys
                .Cast<object>().Select(k => k.ToString().ToUpper()).ToArray();

            foreach (var kvp in config)
            {
                if (skipExisting && existingKeys.Contains(kvp.Key.ToUpper()))
                {
                    continue;
                }

                Environment.SetEnvironmentVariable(kvp.Key, kvp.Value, EnvironmentVariableTarget.Process);
            }
        }
    }
}