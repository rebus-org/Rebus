using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Rebus.Logging;

namespace Rebus.Tests.Logging
{
    [TestFixture]
    public class TestAbstractRebusLoggerFactory
    {
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
            string result = reg.Replace(input, delegate (Match m) {
                counter++;
                return strings[counter];
            });

            Console.WriteLine(input);
            Console.WriteLine(result);
        }

        static IEnumerable<InterpolationScenario> GetScenarios()
        {
            yield return new InterpolationScenario("Hej El Duderino", "Hej {0}", "El Duderino");
            yield return new InterpolationScenario("Hej El Duderino", "Hej {name}", "El Duderino");
            yield return new InterpolationScenario("Hej El Duderino og Donny", "Hej {name} og {name2}", "El Duderino", "Donny");
            yield return new InterpolationScenario("The operation took 2.46 s", "The operation took {ElapsedSeconds} s", 2.46);
            yield return new InterpolationScenario($"The date today is {DateTime.Today:o}", "The date today is {Date}", DateTime.Today);
            yield return new InterpolationScenario("What happens when you forget the placeholder?", "What happens when you forget the placeholder?", DateTime.Today, TimeSpan.FromMinutes(1));
            yield return new InterpolationScenario("What 23 00:01:00 ??? ??? too many placeholders?", "What {happens} {when} {there} {is} too many placeholders?", 23, TimeSpan.FromMinutes(1));
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
}