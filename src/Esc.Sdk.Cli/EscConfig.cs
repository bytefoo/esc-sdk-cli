using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
                throw new InvalidOperationException("An error occurred while fetching Esc configuration.", ex);
            }
        }

        //public void Set(string path, string value)
        //{
        //    var fileName = _options.GetEscExecutable();

        //    if (string.IsNullOrEmpty(_options.PulumiAccessToken))
        //    {
        //        throw new InvalidOperationException(
        //            "Pulumi access token not found. Please set via environment variable as 'PULUMI_ACCESS_TOKEN' or configure via the options.");
        //    }

        //    var runProcessResult = RunProcess.Run(
        //        fileName,
        //        $"env set {_options.OrgName}/{_options.ProjectName}/{_options.EnvironmentName} {path} {value}",
        //        Environment.CurrentDirectory,
        //        new Dictionary<string, string> {["PULUMI_ACCESS_TOKEN"] = _options.PulumiAccessToken}
        //    );

        //    var standardError = runProcessResult.StandardError;

        //    if (!string.IsNullOrEmpty(standardError))
        //    {
        //        throw new InvalidOperationException(standardError);
        //    }
        //}

        internal string InnerLoadRaw()
        {
            var fileName = _options.GetEscExecutable();

            if (string.IsNullOrEmpty(_options.PulumiAccessToken))
            {
                throw new InvalidOperationException(
                    "Pulumi access token not found. Please set via environment variable as 'PULUMI_ACCESS_TOKEN' or configure via the options.");
            }

            var arguments = $"open {_options.OrgName}/{_options.ProjectName}/{_options.EnvironmentName}";

            var processStartInfo = new ProcessStartInfo(fileName, arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardErrorEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8,
                EnvironmentVariables = {["PULUMI_ACCESS_TOKEN"] = _options.PulumiAccessToken}
            };

            var process = new Process {StartInfo = processStartInfo};

            var errorBuilder = new StringBuilder();
            var outputBuilder = new StringBuilder();

            process.Start();

            var outputTask = Task.Run(() =>
            {
                while (!process.StandardOutput.EndOfStream)
                {
                    outputBuilder.AppendLine(process.StandardOutput.ReadLine());
                }
            });

            var errorTask = Task.Run(() =>
            {
                while (!process.StandardError.EndOfStream)
                {
                    errorBuilder.AppendLine(process.StandardError.ReadLine());
                }
            });

            Task.WaitAll(outputTask, errorTask);

            var successfulExit =
                process.WaitForExit((int) TimeSpan.FromSeconds(_options.Timeout + 2).TotalMilliseconds);

            if (!successfulExit)
            {
                throw new InvalidOperationException("Esc process timed out.");
            }

            var standardError = errorBuilder.ToString();
            if (!string.IsNullOrEmpty(standardError.Trim()))
            {
                throw new InvalidOperationException(standardError);
            }

            var output = outputBuilder.ToString();
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