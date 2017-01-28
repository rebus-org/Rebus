using System;
using Rebus.Config;

namespace Rebus.Transport.FileSystem
{
    /// <summary>
    /// Configuration extensions for the file system transport
    /// </summary>
    public static class FileSystemTransportConfigurationExtensions
    {
        /// <summary>
        /// Configures Rebus to use the file system to transport messages. The specified <paramref name="baseDirectory"/> will be used as the base directory
        /// within which subdirectories will be created for each logical queue.
        /// </summary>
        public static void UseFileSystem(this StandardConfigurer<ITransport> configurer, string baseDirectory, string inputQueueName)
        {
            if (baseDirectory == null) throw new ArgumentNullException(nameof(baseDirectory));
            if (inputQueueName == null) throw new ArgumentNullException(nameof(inputQueueName));
            configurer.Register(c => new FileSystemTransport(baseDirectory, inputQueueName));
        }

        /// <summary>
        /// Configures Rebus to use the file system to transport messages, as a one-way client. The specified <paramref name="baseDirectory"/> will be used as the base directory
        /// within which subdirectories will be created for each logical queue.
        /// </summary>
        public static void UseFileSystemAsOneWayClient(this StandardConfigurer<ITransport> configurer, string baseDirectory)
        {
            if (baseDirectory == null) throw new ArgumentNullException(nameof(baseDirectory));
            configurer.Register(c => new FileSystemTransport(baseDirectory, null));
        }
    }
}