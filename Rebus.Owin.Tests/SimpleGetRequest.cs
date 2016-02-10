using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;
using Owin;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests;
using Rebus.Transport.InMem;

namespace Rebus.Owin.Tests
{
    [TestFixture]
    public class SimpleGetRequest : FixtureBase
    {
        const string ListenUrl = "http://localhost";
        const string GreetingText = "hello there my friend!!";
        BuiltinHandlerActivator _activator;

        protected override void SetUp()
        {
            _activator = new BuiltinHandlerActivator();

            Using(_activator);

            Configure.With(_activator)
                .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "doesn't matter"))
                .Options(o =>
                {
                    o.AddWebHost($"{ListenUrl}:5001", GreetingStartup);
                    o.AddWebHost($"{ListenUrl}:5002", GreetingStartup);
                })
                .Start();
        }

        [Test]
        public async Task CanProcessIt()
        {
            var client = new HttpClient();

            var greeting = await client.GetStringAsync($"{ListenUrl}:5001/api/greeting")
                + " AND "
                + await client.GetStringAsync($"{ListenUrl}:5002/api/greeting");

            Assert.That(greeting, Is.EqualTo($"{GreetingText} AND {GreetingText}"));
        }

        static void GreetingStartup(IAppBuilder app)
        {
            app.Map("/api/greeting", a =>
            {
                a.Use(async (context, next) => { await context.Response.WriteAsync(GreetingText); });
            });
        }
    }
}
