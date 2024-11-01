using System;
using System.Collections.Generic;

namespace Esc.Sdk.Cli
{
    /// <summary>
    ///     Extensions to EscConfig.
    /// </summary>
    public static class EscConfigExtensions
    {
        /// <summary>
        ///     Try loading the configuration and returning it as it was downloaded.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static bool TryLoadRaw(this EscConfig self, out string config)
        {
            try
            {
                config = self.InnerLoadRaw();

                return true;
            }
            catch
            {
                config = null;

                return false;
            }
        }

        /// <summary>
        ///     Try loading the configuration and returning it as a dictionary.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static bool TryLoad(this EscConfig self, out Dictionary<string, string>? config)
        {
            try
            {
                config = self.InnerLoad();

                return true;
            }
            catch
            {
                config = null;

                return false;
            }
        }

        /// <summary>
        ///     Try loading the configuration as a dictionary, returning the exception.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="config"></param>
        /// <param name="exception"></param>
        /// <returns>bool</returns>
        public static bool TryLoad(this EscConfig self, out Dictionary<string, string>? config,
            out Exception? exception)
        {
            try
            {
                config = self.InnerLoad();

                exception = null;
                return true;
            }
            catch (Exception innerException)
            {
                config = null;
                exception = innerException;
                return false;
            }
        }

        /// <summary>
        ///     Try loading the configuration and adds missing items to the environment variables of the current process.
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static bool TryLoadIntoEnvironment(this EscConfig self)
        {
            return TryLoadIntoEnvironment(self, false);
        }

        /// <summary>
        ///     Try loading the configuration and adds it to the environment variables of the current process.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="forceUpdate"></param>
        /// <returns></returns>
        public static bool TryLoadIntoEnvironment(this EscConfig self, bool forceUpdate)
        {
            if (!self.TryLoad(out var config))
            {
                return false;
            }

            EscConfig.PatchEnvironmentVariables(config, forceUpdate);

            return true;
        }
    }
}