using System;
using System.Collections.Generic;
using System.Linq;
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
                throw new InvalidOperationException("An error occurred while fetching Esc configuration.", ex);
            }
        }

        public void Set(string path, string value)
        {
            var fileName = _options.GetEscExecutable();

            if (string.IsNullOrEmpty(_options.PulumiAccessToken))
            {
                throw new InvalidOperationException(
                    "Pulumi access token not found. Please set via environment variable as 'PULUMI_ACCESS_TOKEN' or configure via the options.");
            }

            var runProcessResult = RunProcess.Run(
                fileName,
                $"env set {_options.OrgName}/{_options.ProjectName}/{_options.EnvironmentName} {path} {value}",
                Environment.CurrentDirectory,
                new Dictionary<string, string> {["PULUMI_ACCESS_TOKEN"] = _options.PulumiAccessToken}
            );

            var standardError = runProcessResult.StandardError;

            if (!string.IsNullOrEmpty(standardError))
            {
                throw new InvalidOperationException(standardError);
            }
        }

        internal string InnerLoadRaw()
        {
            var fileName = _options.GetEscExecutable();

            if (string.IsNullOrEmpty(_options.PulumiAccessToken))
            {
                throw new InvalidOperationException(
                    "Pulumi access token not found. Please set via environment variable as 'PULUMI_ACCESS_TOKEN' or configure via the options.");
            }

            var runProcessResult = RunProcess.Run(
                fileName,
                $"open {_options.OrgName}/{_options.ProjectName}/{_options.EnvironmentName}",
                Environment.CurrentDirectory,
                new Dictionary<string, string> {["PULUMI_ACCESS_TOKEN"] = _options.PulumiAccessToken}
            );

            var standardError = runProcessResult.StandardError;

            if (!string.IsNullOrEmpty(standardError))
            {
                throw new InvalidOperationException(standardError);
            }

            return runProcessResult.StandardOutput;
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