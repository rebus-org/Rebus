using System;
using System.IO;
using System.Linq;

namespace Rebus.Tests.Contracts;

public class TestConfig
{
    /// <summary>
    /// Gets a path in the file system which can be used for testing things
    /// </summary>
    public static string DirectoryPath()
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test");
    }

    /// <summary>
    /// Gets a suffix that can be appended to things in order to have tests run on separate sets of queues/databases/whatever
    /// </summary>
    public static string Suffix
    {
        get
        {
            var agentSpecificVariable = Environment.GetEnvironmentVariable("tcagent");

            if (agentSpecificVariable != null)
            {
                return agentSpecificVariable;
            }

            return "";
        }
    }

    /// <summary>
    /// Gets a (possibly agent-qualified) name, which allows for tests to run in parallel.
    /// Useful when there is a shared resource, like global or machine-wide queues, databases, etc.
    /// </summary>
    public static string GetName(string nameBase)
    {
        var name = GenerateName(nameBase);

        Console.WriteLine($"Generated name {name}");
            
        return name;
    }

    static string GenerateName(string nameBase)
    {
        if (nameBase.Contains("@"))
        {
            var tokens = nameBase.Split('@');

            var head = tokens.First();
            var tail = string.Join("@", tokens.Skip(1));

            return $"{head}{Suffix}@{tail}";
        }

        return $"{nameBase}{Suffix}";
    }
}