using System;

namespace Archive.Data
{
    /// <summary>
    /// Provides compression parameters
    /// </summary>
    public class CompressionParams
    {
        /// <summary>
        /// Compression block size.
        /// </summary>
        public int BlockSize => 1024 * 1024;

        /// <summary>
        /// Gets command type for stream processing.
        /// </summary>
        public CommandType CommandType { get; set; }

        /// <summary>
        /// Gets input file path.
        /// </summary>
        public string InputPath { get; set; }

        /// <summary>
        /// Gets output file path
        /// </summary>
        public string OutputPath { get; set; }

        /// <summary>
        /// Attempts to parse <see cref="CompressionParams"/> from command line arguments
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <param name="params">When this method returns, result contains an object of type <see cref="CompressionParams"/> whose value is represented by value if the parse operation succeeds.
        /// If the parse operation fails, result contains the default value of the underlying type of <see cref="CompressionParams"/>. This parameter is passed uninitialized.
        /// </param>
        /// <returns>true if the value parameter was converted successfully; otherwise, false.</returns>
        public static bool TryParse(string[] args, out CompressionParams @params)
        {
            @params = default(CompressionParams);
            if (args == null)
                return false;

            if (args.Length != 3)
                return false;

            if (!Enum.TryParse<CommandType>(args[0], true, out var command))
                return false;

            @params = new CompressionParams
            {
                CommandType = command,
                InputPath = args[1],
                OutputPath = args[2]
            };
            return true;
        }
    }
}