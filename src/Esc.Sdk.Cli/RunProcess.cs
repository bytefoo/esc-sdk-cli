using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable AccessToDisposedClosure

//based on: https://github.com/shuruev/Atom/blob/master/Atom/Util/RunProcess/RunProcess.cs

namespace Esc.Sdk.Cli
{
    /// <summary>
    ///     Allows to run an external process getting output/error contents properly and without deadlocks.
    /// </summary>
    public static class RunProcess
    {
        public static RunProcessResult Run(
            string fileName,
            string? arguments = null,
            string? workingDirectory = null,
            IReadOnlyDictionary<string, string>? environmentVariables = null,
            Action<string>? handleOutputBlock = null,
            Action<string>? handleErrorBlock = null)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            var process = new Process();

            try
            {
                SetEnvironmentVariables(environmentVariables, process);

                process.StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments ?? string.Empty,
                    WorkingDirectory = workingDirectory ?? string.Empty,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardErrorEncoding = Encoding.UTF8,
                    StandardOutputEncoding = Encoding.UTF8
                };

                var output = new StringBuilder();
                var error = new StringBuilder();
                process.Start();

                Task.WaitAll(
                    Task.Run(() =>
                    {
                        while (!process.StandardOutput.EndOfStream)
                        {
                            var block = process.StandardOutput.ReadToEnd();
                            output.Append(block);
                            handleOutputBlock?.Invoke(block);
                        }
                    }),
                    Task.Run(() =>
                    {
                        while (!process.StandardError.EndOfStream)
                        {
                            var block = process.StandardError.ReadToEnd();
                            error.Append(block);
                            handleErrorBlock?.Invoke(block);
                        }
                    }));

                process.WaitForExit();
                return new RunProcessResult(process, output, error);
            }
            finally
            {
                process.Dispose();
            }
        }


        /// <summary>
        ///     Runs external process and returns result object.
        /// </summary>
        public static async Task<RunProcessResult> RunAsync(
            string fileName,
            string? arguments = null,
            string? workingDirectory = null,
            IReadOnlyDictionary<string, string>? environmentVariables = null,
            Action<string>? handleOutputBlock = null,
            Action<string>? handleErrorBlock = null)
        {
            using var process = new Process();

            SetEnvironmentVariables(environmentVariables, process);

            process.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments ?? string.Empty,
                WorkingDirectory = workingDirectory ?? string.Empty,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardErrorEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8
            };

            var output = new StringBuilder();
            var error = new StringBuilder();
            process.Start();

            var outputTask = Task.Run(async () =>
            {
                while (!process.StandardOutput.EndOfStream)
                {
                    var block = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                    output.Append(block);
                    handleOutputBlock?.Invoke(block);
                }
            });

            var errorTask = Task.Run(async () =>
            {
                while (!process.StandardError.EndOfStream)
                {
                    var block = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
                    error.Append(block);
                    handleErrorBlock?.Invoke(block);
                }
            });

            await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);
            await Task.Run(() => process.WaitForExit()).ConfigureAwait(false);

            return new RunProcessResult(process, output, error);
        }

        private static void SetEnvironmentVariables(IReadOnlyDictionary<string, string>? environmentVariables,
            Process process)
        {
            if (environmentVariables != null)
            {
                process.StartInfo.Environment.Clear();
                foreach (var kvp in environmentVariables)
                {
                    process.StartInfo.Environment[kvp.Key] = kvp.Value;
                }
            }
        }
    }

    /// <summary>
    ///     Represents the result of external process run.
    /// </summary>
    public class RunProcessResult
    {
        internal RunProcessResult(Process process, StringBuilder output, StringBuilder error)
        {
            FileName = process.StartInfo.FileName;
            Arguments = process.StartInfo.Arguments;
            WorkingDirectory = process.StartInfo.WorkingDirectory;
            StartTime = process.StartTime;
            EndTime = process.ExitTime;
            RunDuration = EndTime.Subtract(StartTime);
            ExitCode = process.ExitCode;
            StandardOutput = output.ToString();
            StandardError = error.ToString();
        }

        public string Arguments { get; }

        public DateTime EndTime { get; }

        public int ExitCode { get; }

        public string FileName { get; }

        public TimeSpan RunDuration { get; }

        public string StandardError { get; }

        public string StandardOutput { get; }

        public DateTime StartTime { get; }

        public string WorkingDirectory { get; }
    }
}