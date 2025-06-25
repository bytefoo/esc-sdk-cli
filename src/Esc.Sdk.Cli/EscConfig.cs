using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using EasyCaching.Core;
using EasyCaching.Core.Serialization;
using EasyCaching.Disk;
using EasyCaching.Serialization.SystemTextJson;

namespace Esc.Sdk.Cli
{
    public class EscConfig
    {
        private static readonly string[] ValidValues = { "string", "dotenv", "json", "detailed" };
        private readonly IEasyCachingProvider _cachingProvider;
        private readonly EscOptions _options;

        public EscConfig() : this(new EscOptions())
        {
        }

        public EscConfig(EscOptions options)
        {
            _options = options;

            var diskOptions = new DiskOptions
            {
                DBConfig = new DiskDbOptions
                {
                    BasePath = Path.Join(Path.GetTempPath(), nameof(EscConfig))
                }
            };

            var defaultJsonSerializer = new DefaultJsonSerializer("disk", new JsonSerializerOptions());
            var easyCachingSerializers = new List<IEasyCachingSerializer> { defaultJsonSerializer };

            _cachingProvider = new DefaultDiskCachingProvider("disk", easyCachingSerializers, diskOptions, null);
        }

        //esc env init
        public void Init()
        {
            Init(_options.OrgName, _options.ProjectName, _options.EnvironmentName);
        }

        public void Init(string environmentName)
        {
            Init(_options.OrgName, _options.ProjectName, environmentName);
        }

        public void Init(string projectName, string environmentName)
        {
            Init(_options.OrgName, projectName, environmentName);
        }

        public void Init(string orgName, string projectName, string environmentName)
        {
            var arguments = BuildCommand("env init", orgName, projectName, environmentName);
            var process = GetProcess(arguments);

            process.Start();
            process.WaitForExit();
        }

        //esc env get
        public string Get(string path, bool isSecret = false, string value = null)
        {
            return Get(_options.OrgName, _options.ProjectName, _options.EnvironmentName, path, isSecret, value);
        }

        public string Get(string environmentName, string path, bool isSecret = false, string value = null)
        {
            return Get(_options.OrgName, _options.ProjectName, environmentName, path, isSecret, value);
        }

        public string Get(string projectName, string environmentName, string path, bool isSecret = false,
            string value = null)
        {
            return Get(_options.OrgName, projectName, environmentName, path, isSecret, value);
        }

        public string Get(string orgName, string projectName, string environmentName, string path, bool isSecret = false, string value = null)
        {
            var arguments = BuildCommand("env get", orgName, projectName, environmentName, path, value, isSecret);
            var process = GetProcess(arguments);
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var successfulExit = process.WaitForExit((int)TimeSpan.FromSeconds(_options.Timeout + 2).TotalMilliseconds);

            if (!successfulExit)
            {
                throw new InvalidOperationException("Esc process timed out.");
            }

            return output.Trim();
        }

        //esc env remove environment
        public void RemoveEnvironment()
        {
            RemoveEnvironment(_options.OrgName, _options.ProjectName, _options.EnvironmentName);
        }

        public void RemoveEnvironment(string environmentName)
        {
            RemoveEnvironment(_options.OrgName, _options.ProjectName, environmentName);
        }

        public void RemoveEnvironment(string projectName, string environmentName)
        {
            RemoveEnvironment(_options.OrgName, projectName, environmentName);
        }

        public void RemoveEnvironment(string orgName, string projectName, string environmentName)
        {
            var arguments = BuildCommand("env rm", orgName, projectName, environmentName, null, null, false, true);
            var process = GetProcess(arguments);

            process.Start();
            process.WaitForExit();
        }

        //esc env remove value
        public void RemoveValue(string path)
        {
            RemoveValue(_options.OrgName, _options.ProjectName, _options.EnvironmentName, path);
        }

        public void RemoveValue(string environmentName, string path)
        {
            RemoveValue(_options.OrgName, _options.ProjectName, environmentName, path);
        }

        public void RemoveValue(string projectName, string environmentName, string path)
        {
            RemoveValue(_options.OrgName, projectName, environmentName, path);
        }

        public void RemoveValue(string orgName, string projectName, string environmentName, string path)
        {
            var arguments = BuildCommand("env rm", orgName, projectName, environmentName, path, null, false, true);
            var process = GetProcess(arguments);

            process.Start();
            process.WaitForExit();
        }

        //esc env set
        public void Set(List<(string path, string value, bool isSecret)> values)
        {
            foreach (var t in values)
                Set(t.path, t.value, t.isSecret);
        }

        public void Set(
            List<(string projectName, string environmentName, string path, string value, bool isSecret)> values)
        {
            foreach (var t in values)
                Set(t.projectName, t.environmentName, t.path, t.value, t.isSecret);
        }

        public void Set(List<(string environmentName, string path, string value, bool isSecret)> values)
        {
            foreach (var t in values)
                Set(t.environmentName, t.path, t.value, t.isSecret);
        }

        public void Set(
            List<(string orgName, string projectName, string environmentName, string path, string value, bool isSecret)>
                values)
        {
            foreach (var t in values)
                Set(t.orgName, t.projectName, t.environmentName, t.path, t.value, t.isSecret);
        }

        public void Set(string path, string value, bool isSecret = false)
        {
            Set(_options.OrgName, _options.ProjectName, _options.EnvironmentName, path, value, isSecret);
        }

        public void Set(string environmentName, string path, string value,
            bool isSecret = false)
        {
            Set(_options.OrgName, _options.ProjectName, environmentName, path, value, isSecret);
        }

        public void Set(string projectName, string environmentName, string path, string value,
            bool isSecret = false)
        {
            Set(_options.OrgName, projectName, environmentName, path, value, isSecret);
        }

        public void Set(string orgName, string projectName, string environmentName, string path, string value,
            bool isSecret = false)
        {
            var arguments = BuildCommand("env set", orgName, projectName, environmentName, path, value, isSecret);
            var process = GetProcess(arguments);
            process.Start();
            process.WaitForExit();
        }

        public List<string> List(string projectFilter = null)
        {
            var process = GetProcess($"env ls {(projectFilter == null ? "" : $"-p {projectFilter}")}"
                .Trim());
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var successfulExit =
                process.WaitForExit((int)TimeSpan.FromSeconds(_options.Timeout + 2).TotalMilliseconds);

            if (!successfulExit)
            {
                throw new InvalidOperationException("Esc process timed out.");
            }

            return output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();
        }


        public string BuildCommand(
            string command,
            string orgName, string projectName, string environmentName,
            string path = null,
            string value = null,
            bool isSecret = false,
            bool skipConfirmation = false)
        {
            orgName ??= _options.OrgName;
            projectName ??= _options.ProjectName;
            environmentName ??= _options.EnvironmentName;

            if (string.IsNullOrEmpty(orgName))
            {
                throw new ArgumentNullException(nameof(orgName), "Organization name cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(projectName))
            {
                throw new ArgumentNullException(nameof(projectName), "Project name cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(environmentName))
            {
                throw new ArgumentNullException(nameof(environmentName), "Environment name cannot be null or empty.");
            }

            var arguments = new StringBuilder($"{command} {orgName}/{projectName}/{environmentName}");

            if (!string.IsNullOrWhiteSpace(path))
            {
                arguments.Append($" {path}");
            }

            switch (command)
            {
                case "env get":
                    AppendGetCommandArguments(arguments, value ?? "string", isSecret);
                    break;
                case "env set":
                    AppendSetCommandArguments(arguments, value, isSecret);
                    break;
                case "env rm":
                    AppendRemoveCommandArguments(arguments, skipConfirmation);
                    break;
            }

            return arguments.ToString().Trim();
        }

        private void AppendGetCommandArguments(StringBuilder arguments, string value, bool isSecret)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                if (!ValidValues.Contains(value))
                {
                    throw new ArgumentException(
                        $"Invalid value: {value}. Allowed values are: {string.Join(", ", ValidValues)}");
                }

                arguments.Append($" --value {value}");
            }

            if (isSecret)
            {
                arguments.Append(" --show-secrets");
            }
        }

        private void AppendSetCommandArguments(StringBuilder arguments, string value, bool isSecret)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                arguments.Append($" {value}");
            }

            if (isSecret)
            {
                arguments.Append(" --secret");
            }
        }

        private void AppendRemoveCommandArguments(StringBuilder arguments, bool skipConfirmation)
        {
            if (skipConfirmation)
            {
                arguments.Append(" --yes");
            }
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

        internal string InnerLoadRaw()
        {
#if DEBUG
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (_options.UseCache.GetValueOrDefault(true))
            {
                return InnerLoadRawCache();
            }
#endif
            return InnerLoadRawEsc();
        }

        internal string InnerLoadRawCache()
        {
            var cacheKey = $"{_options.OrgName}/{_options.ProjectName}/{_options.EnvironmentName}";
            var cacheValue = _cachingProvider.Get<string>(cacheKey);
            if (cacheValue.HasValue)
            {
                return cacheValue.Value;
            }

            var config = InnerLoadRawEsc();
            _cachingProvider.Set(cacheKey, config, _options.CacheExpiration);

            return config;
        }

        internal string InnerLoadRawEsc()
        {
            var arguments = $"open {_options.OrgName}/{_options.ProjectName}/{_options.EnvironmentName}";

            var process = GetProcess(arguments);
            process.Start();

            var output = process.StandardOutput.ReadToEnd();
            var successfulExit =
                process.WaitForExit((int)TimeSpan.FromSeconds(_options.Timeout + 2).TotalMilliseconds);

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

        internal Process GetProcess(string arguments)
        {
            var fileName = _options.GetEscExecutable();

            if (string.IsNullOrEmpty(_options.PulumiAccessToken))
            {
                throw new InvalidOperationException(
                    "Pulumi access token not found. Please set via environment variable as 'PULUMI_ACCESS_TOKEN' or configure via the options.");
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo(fileName, arguments)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardErrorEncoding = Encoding.UTF8,
                    StandardOutputEncoding = Encoding.UTF8,
                    EnvironmentVariables = { ["PULUMI_ACCESS_TOKEN"] = _options.PulumiAccessToken }
                }
            };

            return process;
        }

        internal Dictionary<string, string> InnerLoad()
        {
            var config = InnerLoadRaw(); // Assume this returns a JSON string
            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(config);
                if (dict == null)
                {
                    return new Dictionary<string, string>();
                }

                var stringDict = new Dictionary<string, string>();
                foreach (var kvp in dict)
                {
                    switch (kvp.Value.ValueKind)
                    {
                        case JsonValueKind.String:
                            stringDict[kvp.Key] = kvp.Value.GetString() ?? string.Empty;
                            break;
                        case JsonValueKind.Number:
                            stringDict[kvp.Key] = kvp.Value.GetDouble()
                                .ToString(System.Globalization.CultureInfo.InvariantCulture);
                            break;
                        case JsonValueKind.True:
                            stringDict[kvp.Key] = "true";
                            break;
                        case JsonValueKind.False:
                            stringDict[kvp.Key] = "false";
                            break;
                        case JsonValueKind.Null:
                            stringDict[kvp.Key] = string.Empty;
                            break;
                        case JsonValueKind.Object:
                        case JsonValueKind.Array:
                            throw new InvalidOperationException(
                                $"Invalid JSON value for key '{kvp.Key}': Objects and arrays are not allowed.");
                        default:
                            throw new InvalidOperationException(
                                $"Unsupported JSON value kind for key '{kvp.Key}': {kvp.Value.ValueKind}");
                    }
                }

                return stringDict;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to deserialize configuration.", ex);
            }
        }

        internal static void PatchEnvironmentVariables(Dictionary<string, string> config, bool forceUpdate)
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