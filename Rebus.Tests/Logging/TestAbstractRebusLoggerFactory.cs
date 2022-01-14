using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Rebus.Logging;
using Rebus.Tests.Contracts.Extensions;

namespace Rebus.Tests.Logging;

[TestFixture]
public class TestAbstractRebusLoggerFactory
{
    [TestCase(true, 1000000)]
    [TestCase(false, 1000000)]
    public void CompareRenderingTimes(bool useNewMethod, int iterations)
    {
        Console.WriteLine($"Using {(useNewMethod ? "NEW method" : "OLD method")}");

        var renderMethod = useNewMethod
            ? (Func<string, object[], string>)OpenAbstractRebusLoggerFactory.Render
            : (message, args) => string.Format(message, args);

        const string messageTemplate = "Hello {0}, my name is {1} and I am {2} years old. I like to drink {3} in my {4} - you can say that it is my {5}";

        var objs = new object[] { "there my friend", "Muggie", "37", "beer", "spare time", "hobby" };

        // warm up
        10.Times(() =>
        {
            var result = renderMethod(messageTemplate, objs);
            Console.WriteLine($"'{messageTemplate}' + {string.Join(", ", objs.Select(o => $"'{o}'"))} => '{result}'");
        });

        // measure
        var stopwatch = Stopwatch.StartNew();
        iterations.Times(() =>
        {
            var result = renderMethod(messageTemplate, objs);
        });

        var elapsed = stopwatch.Elapsed;

        Console.WriteLine($"Performing {iterations} renderings took {elapsed.TotalMilliseconds:0.0} ms - that's {iterations / elapsed.TotalMilliseconds:0.0} /ms");
    }

    [Test]
    public void CheckOneParticularExample()
    {
        // _log.Debug("Initializing HTTP forwarder with URI {uri}", _client.BaseAddress);

        var message = OpenAbstractRebusLoggerFactory.Render("Initializing HTTP forwarder with URI {uri}", new Uri("http://localhustler/whambamboozle"));

        Console.WriteLine(message);

        Assert.That(message, Is.EqualTo("Initializing HTTP forwarder with URI http://localhustler/whambamboozle"));
    }

    [TestCaseSource(nameof(GetScenarios))]
    public void ItWorks(InterpolationScenario scenario)
    {
        var result = OpenAbstractRebusLoggerFactory.Render(scenario.Message, scenario.Objs);

        Console.WriteLine($"{scenario} => {result}");

        Assert.That(result, Is.EqualTo(scenario.ExpectedMessage));
    }

    [Test]
    public void CheckRegex()
    {
        string[] strings = { "dog", "cat" };
        int counter = -1;
        string input = @"/home/{value1}/something/{anotherValue}";
        Regex reg = new Regex(@"\{([a-zA-Z0-9]*)\}");
        string result = reg.Replace(input, delegate (Match m)
        {
            counter++;
            return strings[counter];
        });

        Console.WriteLine(input);
        Console.WriteLine(result);
    }

    [Test]
    public void CheckRegexForFormats()
    {
        var regex = new Regex(@"{\w*[\:(\w|\.|\d|\-)*]+}");

        CheckIt(regex, "{hej}");
        CheckIt(regex, "{hej:o}");
        CheckIt(regex, "{hej:yyyy}");
        CheckIt(regex, "{hej:yyyy-MM-dd}");
        CheckIt(regex, "{hej:0}");
        CheckIt(regex, "{hej:0.0}");
    }

    static void CheckIt(Regex regex, string input)
    {
        var isMatch = regex.IsMatch(input);
        Console.WriteLine($"{input}: match = {isMatch}");
    }

    static IEnumerable<InterpolationScenario> GetScenarios()
    {
        return new[] {
            new InterpolationScenario(@"Hej ""El Duderino""", "Hej {0}", "El Duderino"),
            new InterpolationScenario(@"Hej ""El Duderino""", "Hej {name}", "El Duderino"),
            new InterpolationScenario(@"Hej ""El Duderino"" og ""Donny""", "Hej {name} og {name2}", "El Duderino", "Donny"),
            new InterpolationScenario(@"Hej ""El Duderino"" og ""Donny""", "Hej {name} og {name}", "El Duderino", "Donny"),
            new InterpolationScenario("The operation took 2.46 s", "The operation took {ElapsedSeconds} s", 2.46),
            new InterpolationScenario($"The date today is {DateTime.Today:o}", "The date today is {Date}", DateTime.Today),
            new InterpolationScenario("What happens when you forget the placeholder?", "What happens when you forget the placeholder?", DateTime.Today, TimeSpan.FromMinutes(1)),
            new InterpolationScenario("What 23 00:01:00 ??? ??? too many placeholders?", "What {happens} {when} {there} {is} too many placeholders?", 23, TimeSpan.FromMinutes(1)),
            new InterpolationScenario(@"Sending ""msg-A"" to [""queue-a"", ""queue-b""]", "Sending {messageLabel} to {queueNames}", "msg-A", new[]{ "queue-a", "queue-b" }),
            new InterpolationScenario("A number: 2.23", "A number: {number:0.00}", 2.22678976849765),
        };
    }

    public class InterpolationScenario
    {
        public string ExpectedMessage { get; }
        public string Message { get; }
        public object[] Objs { get; }

        public InterpolationScenario(string expectedMessage, string message, params object[] objs)
        {
            ExpectedMessage = expectedMessage;
            Message = message;
            Objs = objs;
        }

        public override string ToString()
        {
            return $"'{Message}' + {string.Join(", ", Objs.Select(o => $"'{o}'"))}";
        }
    }

    class OpenAbstractRebusLoggerFactory : AbstractRebusLoggerFactory
    {
        protected override ILog GetLogger(Type type)
        {
            throw new NotImplementedException();
        }

        public static string Render(string message, params object[] objs)
        {
            return new OpenAbstractRebusLoggerFactory().RenderString(message, objs);
        }
    }
}