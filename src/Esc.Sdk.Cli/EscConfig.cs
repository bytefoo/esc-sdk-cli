using EasyCaching.Core;
using EasyCaching.Disk;
using System;
using System.Collections.Generic;using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using EasyCaching.Serialization.SystemTextJson;
using EasyCaching.Core.Serialization;
using Microsoft.Extensions.Logging;

namespace Esc.Sdk.Cli
{
    public class EscConfig : IEscConfig
    {
        private readonly EscOptions _options;
        private readonly IEasyCachingProvider _cachingProvider;

        public EscConfig() : this(new EscOptions())
        {
        }

        public EscConfig(EscOptions options)
        {
            _options = options;

            var diskOptions = new DiskOptions
            {
                DBConfig = new DiskDbOptions() 
                {
                    BasePath = Path.Join(Path.GetTempPath(), nameof(EscConfig)),
                }
            };

            var defaultJsonSerializer = new DefaultJsonSerializer("disk", new JsonSerializerOptions());
            var easyCachingSerializers = new List<IEasyCachingSerializer> { defaultJsonSerializer };
           
            _cachingProvider = new DefaultDiskCachingProvider("disk", easyCachingSerializers,  diskOptions, null);
            
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

        public void Set(List<Secret> secrets)
        {
            foreach (var secret in secrets)
            {
                Set(secret.Path, secret.Value, secret.IsSecret);
            }
        }

        public void Set(string path, string value, bool isSecret = false)
        {
            var process =
                GetProcess(
                    $"env set {_options.OrgName}/{_options.ProjectName}/{_options.EnvironmentName} {path} {value} {(isSecret ? "--secret" : "")}");
            process.Start();
            process.WaitForExit();
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
                    EnvironmentVariables = {["PULUMI_ACCESS_TOKEN"] = _options.PulumiAccessToken}
                }
            };

            return process;
        }

        //todo: why does reading StandardError not work on Azure Hosted Agents?
        //internal string InnerLoadRaw2()
        //{
        //    var fileName = _options.GetEscExecutable();

        //    if (string.IsNullOrEmpty(_options.PulumiAccessToken))
        //    {
        //        throw new InvalidOperationException(
        //            "Pulumi access token not found. Please set via environment variable as 'PULUMI_ACCESS_TOKEN' or configure via the options.");
        //    }

        //    var arguments = $"open {_options.OrgName}/{_options.ProjectName}/{_options.EnvironmentName}";

        //    var processStartInfo = new ProcessStartInfo(fileName, arguments)
        //    {
        //        CreateNoWindow = true,
        //        UseShellExecute = false,
        //        WindowStyle = ProcessWindowStyle.Hidden,
        //        RedirectStandardOutput = true,
        //        RedirectStandardError = true,
        //        StandardErrorEncoding = Encoding.UTF8,
        //        StandardOutputEncoding = Encoding.UTF8,
        //        EnvironmentVariables = {["PULUMI_ACCESS_TOKEN"] = _options.PulumiAccessToken}
        //    };

        //    var process = new Process {StartInfo = processStartInfo};

        //    var errorBuilder = new StringBuilder();
        //    var outputBuilder = new StringBuilder();

        //    process.Start();

        //    var outputTask = Task.Run(() =>
        //    {
        //        while (!process.StandardOutput.EndOfStream)
        //        {
        //            outputBuilder.AppendLine(process.StandardOutput.ReadLine());
        //        }
        //    });

        //    var errorTask = Task.Run(() =>
        //    {
        //        while (!process.StandardError.EndOfStream)
        //        {
        //            errorBuilder.AppendLine(process.StandardError.ReadLine());
        //        }
        //    });

        //    Task.WaitAll(outputTask, errorTask);

        //    var successfulExit =
        //        process.WaitForExit((int) TimeSpan.FromSeconds(_options.Timeout + 2).TotalMilliseconds);

        //    if (!successfulExit)
        //    {
        //        throw new InvalidOperationException("Esc process timed out.");
        //    }

        //    var standardError = errorBuilder.ToString();
        //    if (!string.IsNullOrEmpty(standardError.Trim()))
        //    {
        //        throw new InvalidOperationException(standardError);
        //    }

        //    var output = outputBuilder.ToString();
        //    if (!output.StartsWith("{"))
        //    {
        //        throw new InvalidOperationException($"Esc returned a non-config object: '{output}'.");
        //    }

        //    return output;
        //}

        internal Dictionary<string, string> InnerLoad()
        {
            var config = InnerLoadRaw();
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(config);

            return dict;
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

    public class Secret
    {
        public string Path { get; }

        public string Value { get; }

        public bool IsSecret { get; }

        public Secret(string path, string value, bool isSecret = false)
        {
            Path = path;
            Value = value;
            IsSecret = isSecret;
        }
    }
}

