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
        ///     Try loading the configuration as dictionary.
        /// </summary>
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
        ///     Try loading the configuration and adds missing items to the environment variables of the current process.
        /// </summary>
        public static bool TryLoadIntoEnvironment(this EscConfig self)
        {
            return TryLoadIntoEnvironment(self, false);
        }

        /// <summary>
        ///     Try loading the configuration and adds it to the environment variables of the current process.
        /// </summary>
        /// <param name="self"></param>
        /// <param name="forceUpdate">Defines if existing environment variables should be updated.</param>
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