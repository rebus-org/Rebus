using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Rebus.Tests.Extensions;

static class TestExtensions
{
    public static string ToJson(this object obj) => JsonConvert.SerializeObject(obj, Formatting.Indented);

    public static string PrettifyJson(this string json)
    {
        try
        {
            return JObject.Parse(json).ToString(Formatting.Indented);
        }
        catch (Exception exception)
        {
            throw new FormatException($"Could not prettify JSON text '{json}'", exception);
        }
    }

    public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
    {
        var timeoutTask = Task.Delay(timeout);

        return await Task.WhenAny(task, timeoutTask) == timeoutTask
            ? throw new TimeoutException($"The task {task} did not finish within {timeout} timeout")
            : await task;
    }
}