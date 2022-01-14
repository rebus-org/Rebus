using System;
using System.IO;

namespace Rebus.Persistence.FileSystem;

internal static class FileSystemHelpers
{
    /// <summary>
    /// Make sure the directory is writeable by the current process
    /// </summary>
    /// <param name="directoryPath">Directory path to check</param>
    /// <exception cref="IOException">Exception thrown if directory cannot be written to</exception>
    public static void EnsureDirectoryIsWritable(string directoryPath)
    {
        // Use a GUID to generate the filename, so we avoid multiple threads stomping on the same file
        // during startup.
        var filePath = Path.Combine(directoryPath, $"write-test-{Guid.NewGuid()}-DELETE-ME.tmp");

        try
        {
            File.WriteAllText(filePath, "RBS2!1");
            File.ReadAllText(filePath);
        }
        catch (Exception exception)
        {
            var message = $"Write/Read test failed for directory path '{directoryPath}' - is it writable for the {Environment.UserDomainName} / {Environment.UserName} account?";
            throw new IOException(message, exception);
        }
        finally
        {
            try
            {
                File.Delete(filePath);
            }
            catch
            {
                // ignored
            }
        }
    }
}