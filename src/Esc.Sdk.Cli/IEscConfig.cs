﻿using System;
using System.Data.SqlTypes;

namespace Esc.Sdk.Cli
{
    /// <summary>
    ///     The Esc interface.
    /// </summary>
    public interface IEscConfig
    {
        /// <summary>
        ///     Loads the configuration and adds missing items to the environment variables of the current process.
        /// </summary>
        /// <exception cref="InvalidOperationException">In case of any error. See inner exception for more details.</exception>
        void Load();

        /// <summary>
        ///     Loads the configuration and adds it to the environment variables of the current process.
        /// </summary>
        /// <param name="forceUpdate">Defines if existing environment variables should be updated.</param>
        /// <exception cref="InvalidOperationException">In case of any error. See inner exception for more details.</exception>
        void Load(bool forceUpdate);

        void Set(string path, string value, bool isSecret = false);
    }
}