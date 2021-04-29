using System;
using System.Runtime.InteropServices;
using Rebus.Config;
using Rebus.Time;
// ReSharper disable UnusedMember.Global

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
        public static FileSystemTransportOptions UseFileSystem(this StandardConfigurer<ITransport> configurer, string baseDirectory, string inputQueueName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                throw new PlatformNotSupportedException(
                    "Since file lock currently cannot be created safely in C# on linux, this FileSystemTransport is not supported");
            }
            if (baseDirectory == null) throw new ArgumentNullException(nameof(baseDirectory));
            if (inputQueueName == null) throw new ArgumentNullException(nameof(inputQueueName));

            var options = new FileSystemTransportOptions();

            configurer
                .OtherService<FileSystemTransport>()
                .Register(context => new FileSystemTransport(baseDirectory, inputQueueName, options, context.Get<IRebusTime>()));

            configurer
                .OtherService<ITransportInspector>()
                .Register(c => c.Get<FileSystemTransport>());

            configurer.Register(context => context.Get<FileSystemTransport>());

            return options;
        }

        /// <summary>
        /// Configures Rebus to use the file system to transport messages, as a one-way client. The specified <paramref name="baseDirectory"/> will be used as the base directory
        /// within which subdirectories will be created for each logical queue.
        /// </summary>
        public static void UseFileSystemAsOneWayClient(this StandardConfigurer<ITransport> configurer, string baseDirectory)
        {
            if (baseDirectory == null) throw new ArgumentNullException(nameof(baseDirectory));

            configurer.Register(context => new FileSystemTransport(baseDirectory, null, new FileSystemTransportOptions(), context.Get<IRebusTime>()));
            OneWayClientBackdoor.ConfigureOneWayClient(configurer);
        }
    }
}